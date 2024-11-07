using System.Globalization;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Digger
{
    public class Chunk : MonoBehaviour
    {
        public const int Size = 33;
        public const int SizeOfMesh = Size - 1;
        public const int SizeVox = Size + 2;
        public const int SizeVox2 = SizeVox * SizeVox;

        [SerializeField] private DiggerSystem digger;
        [SerializeField] private ChunkLODGroup chunkLodGroup;

        [SerializeField] private VoxelChunk voxelChunk;

        [SerializeField] private Vector3i chunkPosition;
        [SerializeField] private Vector3i voxelPosition;
        [SerializeField] private Vector3 worldPosition;
        [SerializeField] private Vector3 sizeInWorld;
        [SerializeField] private bool hasVisualMesh;

        public Vector3i ChunkPosition => chunkPosition;
        public Vector3i VoxelPosition => voxelPosition;
        public Vector3 WorldPosition => worldPosition;
        public DiggerSystem Digger => digger;
        public bool HasVisualMesh => hasVisualMesh;

        public static string GetName(Vector3i chunkPosition)
        {
            return $"Chunk_{chunkPosition.x}_{chunkPosition.y}_{chunkPosition.z}";
        }

        public static Vector3i GetPositionFromName(string chunkName)
        {
            var coords = chunkName.Replace("Chunk_", "").Replace($".{DiggerSystem.VoxelFileExtension}", "").Split('_');
            return new Vector3i(int.Parse(coords[0], CultureInfo.InvariantCulture),
                                int.Parse(coords[1], CultureInfo.InvariantCulture),
                                int.Parse(coords[2], CultureInfo.InvariantCulture));
        }

        internal static Chunk CreateChunk(Vector3i chunkPosition,
                                          DiggerSystem digger,
                                          Terrain terrain,
                                          Material material,
                                          LayerMask layer)
        {
            var voxelPosition = chunkPosition * SizeOfMesh;
            var worldPosition = (Vector3) voxelPosition;
            worldPosition.x *= digger.HeightmapScale.x;
            worldPosition.z *= digger.HeightmapScale.z;

            var go = new GameObject(GetName(chunkPosition));
            go.layer = layer;
            go.hideFlags = digger.ShowDebug ? HideFlags.None : HideFlags.HideInHierarchy | HideFlags.HideInInspector;

#if UNITY_EDITOR
            // Set object static except for lightmap. We should be able to use lightmap in Unity 2019.2 but not before.
            GameObjectUtility.SetStaticEditorFlags(go,
                                                   StaticEditorFlags.BatchingStatic |
                                                   StaticEditorFlags.NavigationStatic |
                                                   StaticEditorFlags.OccludeeStatic |
                                                   StaticEditorFlags.OccluderStatic |
                                                   StaticEditorFlags.ReflectionProbeStatic |
                                                   StaticEditorFlags.OffMeshLinkGeneration);
#endif

            go.transform.parent = digger.transform;
            go.transform.localPosition = worldPosition + Vector3.down * 0.001f;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var chunk = go.AddComponent<Chunk>();
            chunk.digger = digger;
            chunk.chunkPosition = chunkPosition;
            chunk.voxelPosition = voxelPosition;
            chunk.worldPosition = worldPosition;
            chunk.sizeInWorld = SizeOfMesh * digger.HeightmapScale;
            chunk.chunkPosition = chunkPosition;

            chunk.voxelChunk = VoxelChunk.Create(digger, chunk);
            chunk.chunkLodGroup = ChunkLODGroup.Create(chunkPosition, chunk, digger, terrain, material, layer);

            return chunk;
        }

        private static void SetupMeshRenderer(Terrain terrain, MeshRenderer meshRenderer)
        {
#if UNITY_EDITOR
            var terrainSerializedObject = new SerializedObject(terrain);
            var serializedObject = new SerializedObject(meshRenderer);
            var terrainLightmapParameters = terrainSerializedObject.FindProperty("m_LightmapParameters");
            var lightmapParameters = serializedObject.FindProperty("m_LightmapParameters");
            lightmapParameters.objectReferenceValue = terrainLightmapParameters.objectReferenceValue;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
#endif
        }


        public void CreateWithoutOperation()
        {
            for (var lodIndex = 0; lodIndex < ChunkLODGroup.LODCount; ++lodIndex) {
                var collisionMesh = lodIndex == digger.ColliderLodIndex ? voxelChunk.BuildCollisionMesh(ChunkLODGroup.IndexToLod(lodIndex)) : null;
                var res = chunkLodGroup.PostBuild(lodIndex, null, collisionMesh, new ChunkTriggerBounds(digger.HeightmapScale));
                if (lodIndex == 0)
                    hasVisualMesh = res;
            }
        }

        public void Modify(BrushType brush, ActionType action, float intensity, Vector3 operationTerrainPosition,
                           float radius, float coneHeight, bool upsideDown, int textureIndex)
        {
            Utils.Profiler.BeginSample("[Dig] Chunk.Modify");
            var center = operationTerrainPosition - worldPosition;

            voxelChunk.DoOperation(brush, action, intensity, center, radius, coneHeight, upsideDown, textureIndex);

            for (var lodIndex = 0; lodIndex < ChunkLODGroup.LODCount; ++lodIndex) {
                var lod = ChunkLODGroup.IndexToLod(lodIndex);
                var visualMesh = voxelChunk.BuildVisualMesh(lod, chunkLodGroup.GetControlMaps(lodIndex));
                var collisionMesh = lodIndex == digger.ColliderLodIndex ? voxelChunk.BuildCollisionMesh(lod) : null;

                var res = chunkLodGroup.PostBuild(lodIndex, visualMesh, collisionMesh, voxelChunk.TriggerBounds);
                chunkLodGroup.ApplyControlMaps(lodIndex);
                if (lodIndex == 0)
                    hasVisualMesh = res;
            }

            EnsureNeededNeighboursExist();
            Utils.Profiler.EndSample();
        }

        private void EnsureNeededNeighboursExist()
        {
            Utils.Profiler.BeginSample("[Dig] Chunk.EnsureNeededNeighboursExist");
            foreach (var direction in Vector3i.allDirections) {
                if (NeedsNeighbour(direction)) {
                    digger.EnsureChunkExists(chunkPosition + direction);
                }
            }

            Utils.Profiler.EndSample();
        }

        public bool NeedsNeighbour(Vector3i direction)
        {
            if (!HasVisualMesh)
                return false;

            var alteredBounds = voxelChunk.TriggerBounds;
            if (alteredBounds.IsVirgin)
                return false;

            const int margin = 4;
            var maxMargin = sizeInWorld - margin * Vector3.one;

            if (direction.x < 0 && alteredBounds.Min.x > margin)
                return false;
            if (direction.x > 0 && alteredBounds.Max.x < maxMargin.x)
                return false;
            if (direction.y < 0 && alteredBounds.Min.y > margin)
                return false;
            if (direction.y > 0 && alteredBounds.Max.y < maxMargin.y)
                return false;
            if (direction.z < 0 && alteredBounds.Min.z > margin)
                return false;
            if (direction.z > 0 && alteredBounds.Max.z < maxMargin.z)
                return false;

            return true;
        }

        public void UnCutAllVertically()
        {
            Utils.Profiler.BeginSample("[Dig] Chunk.UnCutAllVertically");
            voxelChunk.UnCutAllVertically();
            Utils.Profiler.EndSample();
        }


        public void Load(bool rebuildMeshes)
        {
            voxelChunk.Load();

            if (rebuildMeshes) {
                for (var lodIndex = 0; lodIndex < ChunkLODGroup.LODCount; ++lodIndex) {
                    var lod = ChunkLODGroup.IndexToLod(lodIndex);
                    var visualMesh = voxelChunk.BuildVisualMesh(lod, chunkLodGroup.GetControlMaps(lodIndex));
                    var collisionMesh = lodIndex == digger.ColliderLodIndex ? voxelChunk.BuildCollisionMesh(lod) : null;

                    var res = chunkLodGroup.PostBuild(lodIndex, visualMesh, collisionMesh, voxelChunk.TriggerBounds);
                    chunkLodGroup.ApplyControlMaps(lodIndex);
                    if (lodIndex == 0)
                        hasVisualMesh = res;
                }
            }
        }
    }
}