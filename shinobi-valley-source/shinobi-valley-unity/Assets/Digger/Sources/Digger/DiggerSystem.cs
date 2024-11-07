using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Digger.HeightFeeders;
using Digger.TerrainCutters;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Digger
{
    public class DiggerSystem : MonoBehaviour
    {
        public const string VoxelFileExtension = "vox";
        private const string VoxelMetadataFileExtension = "vom";
        private const string VersionFileExtension = "ver";
        private const int UndoStackSize = 15;
        public static int MaxTextureCountSupported => SystemInfo.supports2DArrayTextures ? 8 : 6;

        private Dictionary<Vector3i, Chunk> chunks;
        private IHeightFeeder heightFeeder;
        private HashSet<VoxelChunk> chunksToPersist;
        private float[] alphamaps;
        private int alphamapCount;
        private Dictionary<Collider, int> colliderStates;

        [SerializeField] private DiggerMaster master;
        [SerializeField] private string guid;
        [SerializeField] private long version = 1;

        [SerializeField] private TerrainCutter cutter;

        [SerializeField] private Vector3 heightmapScale;
        [SerializeField] private Vector3 controlMapScale;

        [SerializeField] public Terrain Terrain;
        [SerializeField] public Material Material;
        [SerializeField] private TerrainMaterialType materialType;
        [SerializeField] private Texture2D[] terrainTextures;
        [SerializeField] public LayerMask Layer;
        [SerializeField] public bool ShowDebug;

        public string Guid => guid;

        public Vector3 HeightmapScale => heightmapScale;
        public Vector3 ControlMapScale => controlMapScale;

        public Vector3 CutMargin => new Vector3(Math.Max(2f, 2.1f * controlMapScale.x),
                                                Math.Max(2f, 2.1f * controlMapScale.y),
                                                Math.Max(2f, 2.1f * controlMapScale.z));

        public TerrainCutter Cutter => cutter;
        public IHeightFeeder HeightFeeder => heightFeeder;
        public float[] Alphamaps => alphamaps;

        public int AlphamapCount => alphamapCount;

        public Texture2D[] TerrainTextures {
            set => terrainTextures = value;
            get => terrainTextures;
        }

        public float ScreenRelativeTransitionHeightLod0 => master.ScreenRelativeTransitionHeightLod0;
        public float ScreenRelativeTransitionHeightLod1 => master.ScreenRelativeTransitionHeightLod1;
        public int ColliderLodIndex => master.ColliderLodIndex;

        public Dictionary<Collider, int> ColliderStates => colliderStates;

        public TerrainMaterialType MaterialType {
            get => materialType;
            set => materialType = value;
        }

        private string BaseFolder => $"{guid}";
        public string BasePathData => Path.Combine(master.SceneDataPath, BaseFolder);
        private string InternalPathData => Path.Combine(BasePathData, ".internal");

        public long Version => version;
        public long PreviousVersion => version - 1;

        private int TerrainChunkWidth => Terrain.terrainData.heightmapResolution * master.ResolutionMult / Chunk.SizeOfMesh - 1;
        private int TerrainChunkHeight => Terrain.terrainData.heightmapResolution * master.ResolutionMult / Chunk.SizeOfMesh - 1;

        public bool IsInitialized => Terrain != null && chunks != null && cutter != null && heightFeeder != null && chunksToPersist != null && alphamaps != null;


        private string GetPathCurrentVersionFile()
        {
            return Path.Combine(BasePathData, "current_version.asset");
        }

        private string GetPathVersionFile(long v)
        {
            return Path.Combine(InternalPathData, $"version_{v}.{VersionFileExtension}");
        }

        public string GetPathVoxelFile(Vector3i chunkPosition)
        {
            return Path.Combine(InternalPathData, $"{Chunk.GetName(chunkPosition)}.{VoxelFileExtension}");
        }

        public string GetPathVoxelMetadataFile(Vector3i chunkPosition)
        {
            return Path.ChangeExtension(GetPathVoxelFile(chunkPosition), VoxelMetadataFileExtension);
        }

        public string GetPathVersionedVoxelFile(Vector3i chunkPosition, long v)
        {
            return Path.ChangeExtension(GetPathVoxelFile(chunkPosition), $"vox_v{v}");
        }

        public string GetPathVersionedVoxelMetadataFile(Vector3i chunkPosition, long v)
        {
            return Path.ChangeExtension(GetPathVoxelMetadataFile(chunkPosition), $"vom_v{v}");
        }

        public string GetTransparencyMapPath()
        {
            return Path.Combine(BasePathData, "TransparencyMap.asset");
        }

        public string GetVersionedTransparencyMapPath(long v)
        {
            return Path.Combine(InternalPathData, $"TransparencyMap.asset_{v}");
        }

        private void Awake()
        {
            colliderStates = new Dictionary<Collider, int>(new ColliderComparer());
        }


        public void PersistAndRecordUndo()
        {
#if UNITY_EDITOR
            CreateDirs();

            foreach (var chunkToPersist in chunksToPersist) {
                chunkToPersist.Persist();
            }

            chunksToPersist.Clear();

            DeleteOtherVersions(true, version - UndoStackSize);

            var versionInfo = new VersionInfo
            {
                Version = version,
                AliveChunks = chunks.Keys.ToList()
            };

            File.WriteAllText(GetPathVersionFile(version), JsonUtility.ToJson(versionInfo));

            cutter.SaveTo(GetVersionedTransparencyMapPath(version));

            Undo.RegisterCompleteObjectUndo(this, "Digger edit");
            ++version;
            PersistVersion();
#endif
        }

        public void DoUndo()
        {
#if UNITY_EDITOR
            if (!Terrain || !cutter || !Directory.Exists(InternalPathData) || !File.Exists(GetPathVersionFile(PreviousVersion))) {
                ++version;
                PersistVersion();
                Undo.ClearUndo(this);
                return;
            }

            var versionInfo = JsonUtility.FromJson<VersionInfo>(File.ReadAllText(GetPathVersionFile(PreviousVersion)));

            UndoRedoFiles();
            Reload(true, false);
            SyncChunksWithVersion(versionInfo);
#endif
        }

        private void PersistVersion()
        {
#if UNITY_EDITOR
            CreateDirs();
            EditorUtils.CreateOrReplaceAsset(new TextAsset(version.ToString()), GetPathCurrentVersionFile());
#endif
        }

        public void ReloadVersion()
        {
#if UNITY_EDITOR
            var verAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(GetPathCurrentVersionFile());
            if (verAsset) {
                version = Convert.ToInt64(verAsset.text);
            }
#endif
        }

        private void UndoRedoFiles()
        {
            var dir = new DirectoryInfo(InternalPathData);
            foreach (var file in dir.EnumerateFiles($"*.vox_v{PreviousVersion}")) {
                var bytesFilePath = Path.ChangeExtension(file.FullName, VoxelFileExtension);
                File.Copy(file.FullName, bytesFilePath, true);
            }

            foreach (var file in dir.EnumerateFiles($"*.vom_v{PreviousVersion}")) {
                var bytesFilePath = Path.ChangeExtension(file.FullName, VoxelMetadataFileExtension);
                File.Copy(file.FullName, bytesFilePath, true);
            }

            cutter.LoadFrom(GetVersionedTransparencyMapPath(PreviousVersion));
        }

        private void SyncChunksWithVersion(VersionInfo versionInfo)
        {
            // Check for missing chunks
            foreach (var vChunk in versionInfo.AliveChunks) {
                if (!chunks.ContainsKey(vChunk)) {
                    Debug.LogError("Chunk is missing " + vChunk);
                }
            }

            // Remove chunks that don't exist in this version
            var chunksToRemove = new List<Chunk>();
            foreach (var chunk in chunks) {
                if (!versionInfo.AliveChunks.Contains(chunk.Key)) {
                    chunksToRemove.Add(chunk.Value);
                }
            }

            foreach (var chunk in chunksToRemove) {
                RemoveChunk(chunk);
            }
        }


        private void DeleteOtherVersions(bool lower, long comparandVersion)
        {
            if (!Directory.Exists(InternalPathData))
                return;

            Utils.Profiler.BeginSample("[Dig] DeleteOtherVersions");

            var dir = new DirectoryInfo(InternalPathData);
            foreach (var verFile in dir.EnumerateFiles($"version_*.{VersionFileExtension}")) {
                long versionToRemove;
                if (long.TryParse(verFile.Name.Replace("version_", "").Replace($".{VersionFileExtension}", ""),
                                  out versionToRemove)
                    && (!lower && versionToRemove >= comparandVersion || lower && versionToRemove <= comparandVersion)) {
                    foreach (var voxFile in dir.EnumerateFiles($"*.vox_v{versionToRemove}")) {
                        voxFile.Delete();
                    }

                    foreach (var voxMetadataFile in dir.EnumerateFiles($"*.vom_v{versionToRemove}")) {
                        voxMetadataFile.Delete();
                    }

                    if (File.Exists(GetVersionedTransparencyMapPath(versionToRemove))) {
                        File.Delete(GetVersionedTransparencyMapPath(versionToRemove));
                    }

                    verFile.Delete();
                }
            }

            Utils.Profiler.EndSample();
        }

        /// <summary>
        /// PreInit setup mandatory fields Terrain, Master and Guid, and it create directories.
        /// This is idempotent and can be called several times.
        /// </summary>
        public void PreInit()
        {
            Terrain = transform.parent.GetComponent<Terrain>();
            if (!Terrain) {
                Debug.LogError("DiggerSystem component can only be added as a child of a terrain.");
                return;
            }

            master = FindObjectOfType<DiggerMaster>();
            if (!master) {
                Debug.LogError("A DiggerMaster is required in the scene.");
                return;
            }

#if UNITY_EDITOR
            if (string.IsNullOrEmpty(guid)) {
                guid = GUID.Generate().ToString();
            }
#endif

            CreateDirs();
        }

        /// <summary>
        /// Initialize Digger and eventually reloads chunks
        /// </summary>
        /// <param name="forceRefresh"></param>
        public void Init(bool forceRefresh)
        {
            var terrainData = Terrain.terrainData;
            heightmapScale = terrainData.heightmapScale / master.ResolutionMult;
            heightmapScale.y = 1f / master.ResolutionMult;
            controlMapScale = new Vector3(terrainData.size.x / terrainData.alphamapWidth, 1f, terrainData.size.z / terrainData.alphamapHeight);
            Reload(forceRefresh, forceRefresh);
        }


        public void Reload(bool rebuildMeshes, bool removeUselessChunks)
        {
            Utils.Profiler.BeginSample("[Dig] Reload");
            CreateDirs();

            if (!cutter) {
                cutter = GetComponent<TerrainCutter>();
                if (!cutter) {
                    cutter = TerrainCutter.Create(Terrain, this);
                }
            }

            cutter.Refresh();
            cutter.Apply(true);
            chunks = new Dictionary<Vector3i, Chunk>(100, new Vector3iComparer());
            heightFeeder = new TerrainHeightFeeder(Terrain.terrainData, master.ResolutionMult);
            chunksToPersist = new HashSet<VoxelChunk>();
            GrabAlphamaps();
            var children = transform.Cast<Transform>().ToList();
            foreach (var child in children) {
                var chunk = child.GetComponent<Chunk>();
                if (chunk) {
                    if (chunk.Digger != this) {
                        Debug.LogError("Chunk is badly defined. Missing/wrong cutter and/or digger reference.");
                    }

                    if (!rebuildMeshes) {
                        chunks.Add(chunk.ChunkPosition, chunk);
                    } else {
                        DestroyImmediate(child.gameObject);
                    }
                }
            }

            LoadChunks(rebuildMeshes);

            if (removeUselessChunks) {
                RemoveUselessChunks();
            }

            Utils.Profiler.EndSample();
        }

        public void GrabAlphamaps()
        {
            var tData = Terrain.terrainData;
            var tAlphamaps = tData.GetAlphamaps(0, 0, tData.alphamapWidth, tData.alphamapHeight);

            var sx = tAlphamaps.GetLength(0);
            var sz = tAlphamaps.GetLength(1);
            alphamapCount = tAlphamaps.GetLength(2);
            alphamaps = new float[sx * sz * alphamapCount];
            for (var x = 0; x < sx; ++x) {
                for (var z = 0; z < sz; ++z) {
                    for (var map = 0; map < alphamapCount; ++map) {
                        var a = tAlphamaps[z, x, map];
                        alphamaps[x * sz * alphamapCount + z * alphamapCount + map] = a;
                    }
                }
            }
        }

        private bool GetOrCreateChunk(Vector3i position, out Chunk chunk)
        {
            if (!chunks.TryGetValue(position, out chunk)) {
                chunk = Chunk.CreateChunk(position, this, Terrain, Material, Layer);
                chunks.Add(position, chunk);
                return false;
            }

            return true;
        }

        public void Modify(BrushType brush, ActionType action, float intensity, Vector3 operationWorldPosition,
                           float radius, float coneHeight, bool upsideDown, int textureIndex)
        {
            Utils.Profiler.BeginSample("[Dig] Modify");
            CreateDirs();

            DeleteOtherVersions(false, version);

            var operationTerrainPosition = operationWorldPosition - Terrain.transform.position;
            var p = operationTerrainPosition;
            p.x /= heightmapScale.x;
            p.z /= heightmapScale.z;
            var voxelRadius = (int) ((radius + Math.Max(CutMargin.x, CutMargin.z)) / Math.Min(heightmapScale.x, heightmapScale.z)) + 1;
            var voxMargin = new Vector3i(voxelRadius, (int) (radius + CutMargin.y) + 1, voxelRadius);
            var voxMin = new Vector3i(p) - voxMargin;
            var voxMax = new Vector3i(p) + voxMargin;

            var minMaxHeight = GetMinMaxHeightWithin(voxMin, voxMax);
            voxMin.y = Math.Min(voxMin.y, (int) minMaxHeight.x - 1);
            voxMax.y = Math.Max(voxMax.y, (int) minMaxHeight.y + 1);

            var min = voxMin / Chunk.SizeOfMesh;
            var max = voxMax / Chunk.SizeOfMesh;
            if (voxMin.x < 0)
                min.x--;
            if (voxMin.y < 0)
                min.y--;
            if (voxMin.z < 0)
                min.z--;
            if (voxMax.x < 0)
                max.x--;
            if (voxMax.y < 0)
                max.y--;
            if (voxMax.z < 0)
                max.z--;

            if (min.x < 0)
                min.x = 0;
            if (min.z < 0)
                min.z = 0;
            if (max.x > TerrainChunkWidth)
                max.x = TerrainChunkWidth;
            if (max.z > TerrainChunkHeight)
                max.z = TerrainChunkHeight;

            for (var x = min.x; x <= max.x; ++x) {
                for (var z = min.z; z <= max.z; ++z) {
                    var uncutDone = false;
                    for (var y = min.y; y <= max.y; ++y) {
                        Chunk chunk;
                        GetOrCreateChunk(new Vector3i(x, y, z), out chunk);
                        if (!uncutDone && action == ActionType.Reset) {
                            uncutDone = true;
                            chunk.UnCutAllVertically();
                        }

                        chunk.Modify(brush, action, intensity, operationTerrainPosition, radius, coneHeight, upsideDown, textureIndex);
                    }
                }
            }

            if (action == ActionType.Reset) {
                RemoveUselessChunks();
            }

            cutter.Apply(true);
            Utils.Profiler.EndSample();
        }

        private float2 GetMinMaxHeightWithin(Vector3i minVox, Vector3i maxVox)
        {
            var minMax = new float2(float.MaxValue, float.MinValue);
            for (var x = minVox.x; x <= maxVox.x; ++x) {
                for (var z = minVox.z; z <= maxVox.z; ++z) {
                    var h = heightFeeder.GetHeight(x, z);
                    if (h < minMax.x)
                        minMax.x = h;
                    if (h > minMax.y)
                        minMax.y = h;
                }
            }

            return minMax;
        }

        public void EnsureChunkExists(Vector3i chunkPosition)
        {
            if (chunkPosition.x < 0 || chunkPosition.z < 0 || chunkPosition.x >= TerrainChunkWidth || chunkPosition.z >= TerrainChunkHeight)
                return;

            Chunk chunk;
            if (!GetOrCreateChunk(chunkPosition, out chunk)) {
                chunk.CreateWithoutOperation();
            }
        }

        public void EnsureChunkWillBePersisted(VoxelChunk voxelChunk)
        {
            chunksToPersist.Add(voxelChunk);
        }

        private void RemoveUselessChunks()
        {
            var chunksToRemove = new List<Chunk>();
            foreach (var chunk in chunks) {
                if (IsUseless(chunk.Key)) {
                    Debug.Log("[Digger] Cleaning chunk at " + chunk.Key);
                    chunksToRemove.Add(chunk.Value);
                }
            }

            foreach (var chunk in chunksToRemove) {
                RemoveChunk(chunk);
            }
        }

        private void RemoveChunk(Chunk chunk)
        {
            chunks.Remove(chunk.ChunkPosition);
            var file = GetPathVoxelFile(chunk.ChunkPosition);
            if (File.Exists(file)) {
                File.Delete(file);
            }

            file = GetPathVoxelMetadataFile(chunk.ChunkPosition);
            if (File.Exists(file)) {
                File.Delete(file);
            }

            if (Application.isEditor) {
                DestroyImmediate(chunk.gameObject);
            } else {
                Destroy(chunk.gameObject);
            }
        }

        private bool IsUseless(Vector3i chunkPosition)
        {
            Chunk chunk;
            if (!chunks.TryGetValue(chunkPosition, out chunk))
                return false; // if it doesn't exist, it doesn't need to be removed
            if (chunk.HasVisualMesh)
                return false; // if it has a visual mesh, it must not be removed
            foreach (var direction in Vector3i.allDirections) {
                Chunk neighbour;
                if (!chunks.TryGetValue(chunkPosition + direction, out neighbour))
                    continue;
                if (neighbour.NeedsNeighbour(-direction))
                    return false; // if one of the chunk's neighbours need it, it must not be removed
            }

            return true;
        }

        private void LoadChunks(bool rebuildMeshes)
        {
            if (!Directory.Exists(InternalPathData))
                return;
            if (chunks == null) {
                Debug.LogError("Chunks dico should not be null in loading");
                return;
            }

            foreach (var chunk in chunks) {
                chunk.Value.Load(rebuildMeshes);
            }

            var dir = new DirectoryInfo(InternalPathData);
            foreach (var file in dir.EnumerateFiles($"*.{VoxelFileExtension}")) {
                var chunkPosition = Chunk.GetPositionFromName(file.Name);
                if (!chunks.ContainsKey(chunkPosition) && chunkPosition.x >= 0 && chunkPosition.z >= 0 && chunkPosition.x <= TerrainChunkWidth && chunkPosition.z <= TerrainChunkHeight) {
                    Chunk chunk;
                    GetOrCreateChunk(chunkPosition, out chunk);
                    chunk.Load(rebuildMeshes);
                }
            }
        }

        public void Clear()
        {
#if UNITY_EDITOR
            Utils.Profiler.BeginSample("[Dig] Clear");
            cutter.Clear();
            cutter = null;

            AssetDatabase.StartAssetEditing();
            if (Directory.Exists(BasePathData)) {
                Directory.Delete(BasePathData, true);
                AssetDatabase.DeleteAsset(BasePathData);
            }

            if (chunks != null) {
                foreach (var chunk in chunks) {
                    if (Application.isEditor) {
                        DestroyImmediate(chunk.Value.gameObject);
                    } else {
                        Destroy(chunk.Value.gameObject);
                    }
                }

                chunks = null;
            }

            chunksToPersist = null;
            alphamaps = null;
            Material = null;

            version = 1;
            PersistVersion();
            AssetDatabase.StopAssetEditing();
            Undo.ClearAll();
            Utils.Profiler.EndSample();
#endif
        }

        public void CreateDirs()
        {
#if UNITY_EDITOR
            master.CreateDirs();

            if (!Directory.Exists(BasePathData)) {
                AssetDatabase.CreateFolder(master.SceneDataPath, BaseFolder);
            }

            if (!Directory.Exists(InternalPathData)) {
                Directory.CreateDirectory(InternalPathData);
            }
#endif
        }
    }
}