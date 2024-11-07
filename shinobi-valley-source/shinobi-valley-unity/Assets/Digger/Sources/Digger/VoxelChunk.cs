using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Digger
{
    public class VoxelChunk : MonoBehaviour
    {
        private const int SizeVox = Chunk.SizeVox;

        [NonSerialized] private Voxel[] voxelArray;

        [SerializeField] private DiggerSystem digger;
        [SerializeField] private float[] heightArray;
        [SerializeField] private float[] verticalNormalArray;

        [SerializeField] private Vector3i chunkPosition;
        [SerializeField] private Vector3i voxelPosition;
        [SerializeField] private Vector3 worldPosition;

        [SerializeField] private ChunkTriggerBounds bounds;

        public ChunkTriggerBounds TriggerBounds => bounds;

        internal static VoxelChunk Create(DiggerSystem digger, Chunk chunk)
        {
            var go = new GameObject("VoxelChunk")
            {
                hideFlags = HideFlags.DontSaveInBuild
            };
            go.transform.parent = chunk.transform;
            go.transform.position = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            var voxelChunk = go.AddComponent<VoxelChunk>();
            voxelChunk.digger = digger;
            voxelChunk.chunkPosition = chunk.ChunkPosition;
            voxelChunk.voxelPosition = chunk.VoxelPosition;
            voxelChunk.worldPosition = chunk.WorldPosition;
            voxelChunk.Load();

            return voxelChunk;
        }

        private void FeedHeights()
        {
            if (heightArray == null)
                heightArray = new float[SizeVox * SizeVox];

            if (verticalNormalArray == null)
                verticalNormalArray = new float[SizeVox * SizeVox];

            for (var xi = 0; xi < SizeVox; ++xi) {
                for (var zi = 0; zi < SizeVox; ++zi) {
                    heightArray[xi * SizeVox + zi] = digger.HeightFeeder.GetHeight(voxelPosition.x + (xi - 1), voxelPosition.z + (zi - 1));
                    verticalNormalArray[xi * SizeVox + zi] = digger.HeightFeeder.GetVerticalNormal(voxelPosition.x + (xi - 1), voxelPosition.z + (zi - 1));
                }
            }
        }

        private void GenerateVoxels()
        {
            Utils.Profiler.BeginSample("[Dig] VoxelChunk.GenerateVoxels");
            if (voxelArray == null)
                voxelArray = new Voxel[SizeVox * SizeVox * SizeVox];

            var heights = new NativeArray<float>(heightArray, Allocator.TempJob);
            var voxels = new NativeArray<Voxel>(SizeVox * SizeVox * SizeVox, Allocator.TempJob);

            // Set up the job data
            var jobData = new VoxelGenerationJob();
            jobData.ChunkAltitude = voxelPosition.y;
            jobData.Heights = heights;
            jobData.Voxels = voxels;

            // Schedule the job
            var handle = jobData.Schedule(voxels.Length, 64);

            // Wait for the job to complete
            handle.Complete();

            voxels.CopyTo(voxelArray);
            heights.Dispose();
            voxels.Dispose();

            digger.EnsureChunkWillBePersisted(this);

            Utils.Profiler.EndSample();
        }

        public void DoOperation(BrushType brush, ActionType action, float intensity, Vector3 position,
                                float radius, float coneHeight, bool upsideDown, int textureIndex)
        {
            Utils.Profiler.BeginSample("[Dig] VoxelChunk.DoOperation");
            var heights = new NativeArray<float>(heightArray, Allocator.TempJob);
            var normals = new NativeArray<float>(verticalNormalArray, Allocator.TempJob);
            var voxels = new NativeArray<Voxel>(voxelArray, Allocator.TempJob);

            var jobData = new VoxelModificationJob
            {
                Brush = brush,
                Action = action,
                HeightmapScale = digger.HeightmapScale,
                ChunkAltitude = voxelPosition.y,
                Voxels = voxels,
                Heights = heights,
                VerticalNormals = normals,
                Intensity = intensity,
                Center = position,
                Radius = radius,
                ConeHeight = coneHeight,
                UpsideDown = upsideDown,
                RadiusWithMargin = radius + Math.Max(Math.Max(digger.CutMargin.x, digger.CutMargin.y), digger.CutMargin.z),
                TextureIndex = (sbyte) textureIndex
            };
            jobData.PostConstruct();

            // Schedule the job
            var handle = jobData.Schedule(voxels.Length, 64);

            // Wait for the job to complete
            handle.Complete();

            voxels.CopyTo(voxelArray);
            voxels.Dispose();
            normals.Dispose();
            heights.Dispose();

            CutAndComputeTriggerBounds();
            RecordUndoIfNeeded();
            digger.EnsureChunkWillBePersisted(this);

            Utils.Profiler.EndSample();
        }

        public void UnCutAllVertically()
        {
            var hScale = digger.HeightmapScale;
            var tData = digger.Terrain.terrainData;
            for (var xi = 0; xi < SizeVox; ++xi) {
                for (var zi = 0; zi < SizeVox; ++zi) {
                    var pos = new Vector3((xi - 1) * hScale.x, 0, (zi - 1) * hScale.z);
                    var wpos = pos + worldPosition;
                    var p = TerrainUtils.TerrainRelativePositionToAlphamapPosition(tData, wpos);
                    digger.Cutter.UnCut(p.x, p.z);
                }
            }
        }

        private void CutAndComputeTriggerBounds()
        {
            Utils.Profiler.BeginSample("[Dig] VoxelChunk.CutAndComputeTriggerBounds");
            var tData = digger.Terrain.terrainData;
            var hScale = digger.HeightmapScale;
            var cScale = digger.ControlMapScale;
            var cutSizeX = Math.Max(1, (int) (hScale.x / cScale.x));
            var cutSizeZ = Math.Max(1, (int) (hScale.z / cScale.z));
            bounds = new ChunkTriggerBounds(digger.HeightmapScale);

            for (var index = 0; index < voxelArray.Length; index++) {
                var voxel = voxelArray[index];
                if (voxel.IsAlteredNearBelowSurface || voxel.IsAlteredNearAboveSurface) {
                    var xi = index / Chunk.SizeVox2;
                    var yi = (index - xi * Chunk.SizeVox2) / Chunk.SizeVox;
                    var zi = index - xi * Chunk.SizeVox2 - yi * Chunk.SizeVox;
                    var pos = new Vector3((xi - 1) * hScale.x, (yi - 1), (zi - 1) * hScale.z);
                    var wpos = pos + worldPosition;
                    var p = TerrainUtils.TerrainRelativePositionToAlphamapPosition(tData, wpos);
                    bounds.ExtendIfNeeded(pos);
                    for (var x = -cutSizeX; x < cutSizeX; ++x) {
                        for (var z = -cutSizeZ; z < cutSizeZ; ++z) {
                            digger.Cutter.Cut(p.x + x, p.z + z, voxel.IsAlteredNearAboveSurface);
                        }
                    }
                }
            }

            Utils.Profiler.EndSample();
        }

        public Mesh BuildVisualMesh(int lod, Texture2D[] controlMaps)
        {
            return BuildMesh(lod, 0f, true, false, controlMaps);
        }

        public Mesh BuildCollisionMesh(int lod)
        {
            return BuildMesh(lod, 0f, false, true, null);
        }

        private Mesh BuildMesh(int lod, float isovalue, bool alteredOnly, bool colliderMesh, Texture2D[] controlMaps)
        {
            Utils.Profiler.BeginSample("[Dig] VoxelChunk.BuildMesh");
            var edgeTable = MarchingCubesTables.NewEdgeTable();
            var triTable = MarchingCubesTables.NewTriTable();
            var corners = MarchingCubesTables.NewCorners();
            var voxels = new NativeArray<Voxel>(voxelArray, Allocator.TempJob);
            var alphamaps = new NativeArray<float>(digger.Alphamaps, Allocator.TempJob);
            var o = MarchingCubesJob.Out.New(!colliderMesh);
            var vertexCounter = new NativeCounter(Allocator.TempJob);
            var triangleCounter = new NativeCounter(Allocator.TempJob, 3);

            var scale = new float3(digger.HeightmapScale) {y = 1f};

            // for retro-compatibility
            if (lod <= 0) lod = 1;

            var tData = digger.Terrain.terrainData;
            var uvScale = new Vector2(1f / tData.size.x,
                                      1f / tData.size.z);
            var alphamapsSize = new int2(tData.alphamapWidth, tData.alphamapHeight);
            var mapCount = digger.AlphamapCount;

            // Set up the job data
            var jobData = new MarchingCubesJob(edgeTable,
                                               triTable,
                                               corners,
                                               vertexCounter.ToConcurrent(),
                                               triangleCounter.ToConcurrent(),
                                               voxels,
                                               alphamaps,

                                               // out params
                                               o,

                                               // misc
                                               scale,
                                               uvScale,
                                               worldPosition,
                                               lod,
                                               alphamapsSize,
                                               mapCount);

            jobData.Isovalue = isovalue;
            jobData.AlteredOnly = (byte) (alteredOnly ? 1 : 0);
            jobData.FullOutput = (byte) (colliderMesh ? 0 : 1);

            // Schedule the job
            var handle = jobData.Schedule(voxels.Length, 4);

            // Wait for the job to complete
            handle.Complete();

            var vertexCount = vertexCounter.Count;
            var triangleCount = triangleCounter.Count;

            edgeTable.Dispose();
            triTable.Dispose();
            corners.Dispose();
            voxels.Dispose();
            alphamaps.Dispose();
            vertexCounter.Dispose();
            triangleCounter.Dispose();

            Mesh mesh;
            if (colliderMesh) {
                mesh = ToMeshSimple(o, vertexCount, triangleCount);
            } else {
                mesh = ToMesh(o, vertexCount, triangleCount, controlMaps);
            }

            o.Dispose();

            Utils.Profiler.EndSample();
            return mesh;
        }

        private static Mesh ToMeshSimple(MarchingCubesJob.Out o, int vertexCount, int triangleCount)
        {
            if (vertexCount < 3 || triangleCount < 3)
                return null;
            var mesh = new Mesh
            {
                vertices = Utils.ToArray(o.outVertices, vertexCount),
                normals = Utils.ToArray(o.outNormals, vertexCount)
            };
            mesh.SetTriangles(Utils.ToArray(o.outTriangles, triangleCount), 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private Mesh ToMesh(MarchingCubesJob.Out o, int vertexCount, int triangleCount, Texture2D[] controlMaps)
        {
            if (vertexCount < 3 || triangleCount < 1)
                return null;

            Utils.Profiler.BeginSample("[Dig] VoxelChunk.ToMesh");
            var tData = digger.Terrain.terrainData;

            var mesh = new Mesh();
            if (digger.MaterialType == TerrainMaterialType.MicroSplat) {
                AddVertexDataForMicroSplat(mesh, o, vertexCount, triangleCount, tData, controlMaps);
            } else {
                AddVertexData(mesh, o, vertexCount, triangleCount, tData);
            }

            mesh.bounds = GetBounds();
            mesh.RecalculateTangents();

            Utils.Profiler.EndSample();
            return mesh;
        }

        private Bounds GetBounds()
        {
            var worldSize = Vector3.one * Chunk.SizeOfMesh;
            worldSize.x *= digger.HeightmapScale.x;
            worldSize.z *= digger.HeightmapScale.z;
            return new Bounds(worldSize * 0.5f, worldSize);
        }

        private void AddVertexData(Mesh mesh, MarchingCubesJob.Out o, int vertexCount, int triangleCount, TerrainData tData)
        {
            mesh.vertices = Utils.ToArray(o.outVertices, vertexCount);
            mesh.SetTriangles(Utils.ToArray(o.outTriangles, triangleCount), 0);
            mesh.colors = Utils.ToArray(o.outColors, vertexCount);
            mesh.SetUVs(1, Utils.ToList(o.outUV2s, vertexCount));
            mesh.SetUVs(2, Utils.ToList(o.outUV3s, vertexCount));
            mesh.SetUVs(3, Utils.ToList(o.outUV4s, vertexCount));

            var uvs = Utils.ToArray(o.outUV1s, vertexCount);
            var normals = Utils.ToArray(o.outNormals, vertexCount);

            for (var i = 0; i < vertexCount; ++i) {
                var texInfo = o.outInfos[i].x;
                if (texInfo == 0 || texInfo == 1 || texInfo == -1) {
                    // near the terrain surface -> set same normal
                    var uv = uvs[i];
                    normals[i] = tData.GetInterpolatedNormal(uv.x, uv.y);
                }
            }

            mesh.normals = normals;
            mesh.uv = uvs;
        }

        private void AddVertexDataForMicroSplat(Mesh mesh, MarchingCubesJob.Out o, int vertexCount, int triangleCount, TerrainData tData, Texture2D[] controlMaps)
        {
            var vertices = Utils.ToArray(o.outVertices, vertexCount);
            var uvs = Utils.ToArray(o.outUV1s, vertexCount);
            var normals = Utils.ToArray(o.outNormals, vertexCount);

            for (var i = 0; i < vertexCount; ++i) {
                var texInfo = o.outInfos[i].x;
                if (texInfo == 0 || texInfo == 1 || texInfo == -1) {
                    // near the terrain surface -> set same normal
                    var uv = uvs[i];
                    normals[i] = tData.GetInterpolatedNormal(uv.x, uv.y);
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.SetTriangles(Utils.ToArray(o.outTriangles, triangleCount), 0);

#if UNITY_EDITOR
            UnwrapParam.SetDefaults(out var unwrapParam);
            unwrapParam.areaError = 0.04f;
            unwrapParam.angleError = 0.04f;
            unwrapParam.packMargin = 0.006f;
            // CAUTION: GenerateSecondaryUVSet also changes the vertices and triangles!
            Unwrapping.GenerateSecondaryUVSet(mesh, unwrapParam);
#else
            Debug.LogError("Cannot generate unique UVs outside of Unity editor");
#endif
            var newUV2s = mesh.uv2;
            var newVerts = mesh.vertices;
            var newTris = mesh.triangles;

            var remap = RemapVertices(newVerts, vertices);

            // all control maps have the same size
            var width = controlMaps[0].width;
            var height = controlMaps[0].height;
            var pixels0 = NewClearSplatmap(width, height);
            var pixels1 = NewClearSplatmap(width, height);
            var pixels2 = NewClearSplatmap(width, height);
            var pixels3 = NewClearSplatmap(width, height);

            for (var i = 0; i < newTris.Length; i += 3) {
                var i1 = newTris[i];
                var i2 = newTris[i + 1];
                var i3 = newTris[i + 2];
                var ri1 = remap[i1];
                var ri2 = remap[i2];
                var ri3 = remap[i3];
                var uuv1 = newUV2s[i1];
                var uuv2 = newUV2s[i2];
                var uuv3 = newUV2s[i3];

                UpdateControlMaps(o.outColors[ri1], o.outColors[ri2], o.outColors[ri3],
                                  uuv1, uuv2, uuv3,
                                  pixels0, width, height);

                UpdateControlMaps(o.outUV2s[ri1], o.outUV2s[ri2], o.outUV2s[ri3],
                                  uuv1, uuv2, uuv3,
                                  pixels1, width, height);

                UpdateControlMaps(o.outUV3s[ri1], o.outUV3s[ri2], o.outUV3s[ri3],
                                  uuv1, uuv2, uuv3,
                                  pixels2, width, height);

                UpdateControlMaps(o.outUV4s[ri1], o.outUV4s[ri2], o.outUV4s[ri3],
                                  uuv1, uuv2, uuv3,
                                  pixels3, width, height);
            }

            controlMaps[0].SetPixels(pixels0);
            controlMaps[1].SetPixels(pixels1);
            controlMaps[2].SetPixels(pixels2);
            controlMaps[3].SetPixels(pixels3);
            for (var ctrlIndex = 0; ctrlIndex < 4; ++ctrlIndex) {
                controlMaps[ctrlIndex].Apply();
            }
        }

        private static Color[] NewClearSplatmap(int width, int height)
        {
            var pixels = new Color[width * height];
            for (var i = 0; i < pixels.Length; ++i) {
                pixels[i] = Color.clear;
            }

            return pixels;
        }

        private void UpdateControlMaps(Color col1, Color col2, Color col3,
                                       Vector2 uuv1, Vector2 uuv2, Vector2 uuv3,
                                       Color[] pixels, int width, int height)
        {
            if (Utils.Approximately(col1, Color.clear) &&
                Utils.Approximately(col2, Color.clear) &&
                Utils.Approximately(col3, Color.clear))
                return; // nothing to do -> quick win

            var a = new int2((int) (uuv1.x * width), (int) (uuv1.y * height));
            var b = new int2((int) (uuv2.x * width), (int) (uuv2.y * height));
            var c = new int2((int) (uuv3.x * width), (int) (uuv3.y * height));


            const float margin = 16;
            var centroid = new float2(a + b + c) * 0.33333333f;
            var d = a - centroid;
            var ma = a + new int2(new Vector2(d.x, d.y).normalized * margin);
            d = b - centroid;
            var mb = b + new int2(new Vector2(d.x, d.y).normalized * margin);
            d = c - centroid;
            var mc = c + new int2(new Vector2(d.x, d.y).normalized * margin);

            var min = Utils.Min(ma, mb, mc);
            var max = Utils.Max(ma, mb, mc);
            max.x = Math.Min(max.x, width - 1);
            max.y = Math.Min(max.y, height - 1);
            min.x = Math.Max(min.x, 0);
            min.y = Math.Max(min.y, 0);

            for (var y = min.y; y <= max.y; ++y) {
                for (var x = min.x; x <= max.x; ++x) {
                    var p = new int2(x, y);
                    var weights = Utils.TriangleInterpolate(a, b, c, p);
                    if (weights.x < -0.001f || weights.y < -0.001f || weights.z < -0.001f) {
                        if (Utils.Approximately(pixels[p.y * width + p.x], Color.clear)) {
                            weights = Utils.TriangleInterpolate(ma, mb, mc, p);
                            if (weights.x > -0.001f && weights.y > -0.001f && weights.z > -0.001f) {
                                pixels[p.y * width + p.x] = weights.x * col1 + weights.y * col2 + weights.z * col3;
                            }
                        }

                        continue; // outside of triangle
                    }

                    pixels[p.y * width + p.x] = weights.x * col1 + weights.y * col2 + weights.z * col3;
                }
            }
        }

        private static List<int> RemapVertices(Vector3[] verts1, Vector3[] verts2)
        {
            var remap = new List<int>();
            foreach (var v1 in verts1) {
                for (var j = 0; j < verts2.Length; ++j) {
                    if (Utils.Approximately(v1, verts2[j])) {
                        remap.Add(j);
                        break;
                    }
                }
            }

            if (remap.Count != verts1.Length) {
                Debug.LogError("Failed to remap vertices");
            }

            return remap;
        }

        private void RecordUndoIfNeeded()
        {
            if (voxelArray == null || voxelArray.Length == 0) {
                Debug.LogError("Voxel array should not be null when recording undo");
                return;
            }

            Utils.Profiler.BeginSample("[Dig] VoxelChunk.RecordUndoIfNeeded");
            var path = digger.GetPathVoxelFile(chunkPosition);

            var savePath = digger.GetPathVersionedVoxelFile(chunkPosition, digger.PreviousVersion);
            if (File.Exists(path) && !File.Exists(savePath)) {
                File.Copy(path, savePath);
            }

            var metadataPath = digger.GetPathVoxelMetadataFile(chunkPosition);

            var saveMetadataPath = digger.GetPathVersionedVoxelMetadataFile(chunkPosition, digger.PreviousVersion);
            if (File.Exists(metadataPath) && !File.Exists(saveMetadataPath)) {
                File.Copy(metadataPath, saveMetadataPath);
            }

            Utils.Profiler.EndSample();
        }

        public void Persist()
        {
            if (voxelArray == null || voxelArray.Length == 0) {
                Debug.LogError("Voxel array should not be null in saving");
                return;
            }

            Utils.Profiler.BeginSample("[Dig] VoxelChunk.Persist");
            var path = digger.GetPathVoxelFile(chunkPosition);

            var voxels = new NativeArray<Voxel>(voxelArray, Allocator.Temp);
            var bytes = new NativeSlice<Voxel>(voxels).SliceConvert<byte>();
            File.WriteAllBytes(path, bytes.ToArray());
            voxels.Dispose();

            var metadataPath = digger.GetPathVoxelMetadataFile(chunkPosition);
            using (var stream = new FileStream(metadataPath, FileMode.Create, FileAccess.Write, FileShare.Write, 4096, FileOptions.Asynchronous)) {
                using (var writer = new BinaryWriter(stream, Encoding.Default)) {
                    writer.Write(bounds.IsVirgin);
                    writer.Write(bounds.Min.x);
                    writer.Write(bounds.Min.y);
                    writer.Write(bounds.Min.z);
                    writer.Write(bounds.Max.x);
                    writer.Write(bounds.Max.y);
                    writer.Write(bounds.Max.z);
                }
            }

            var savePath = digger.GetPathVersionedVoxelFile(chunkPosition, digger.Version);
            File.Copy(path, savePath, true);
            var saveMetadataPath = digger.GetPathVersionedVoxelMetadataFile(chunkPosition, digger.Version);
            File.Copy(metadataPath, saveMetadataPath, true);
            Utils.Profiler.EndSample();
        }

        public void Load()
        {
            // Feed heights again in case they have been modified
            FeedHeights();

            var path = digger.GetPathVoxelFile(chunkPosition);
            if (!File.Exists(path)) {
                if (voxelArray == null) {
                    // If there is no persisted voxels but voxel array is null, then we fallback and (re)generate them.
                    GenerateVoxels();
                }

                return;
            }

            var voxelBytes = new NativeArray<byte>(File.ReadAllBytes(path), Allocator.Temp);
            var bytes = new NativeSlice<byte>(voxelBytes);
            var voxels = bytes.SliceConvert<Voxel>();
            voxelArray = voxels.ToArray();
            voxelBytes.Dispose();

            var hScale = digger.HeightmapScale;
            var metadataPath = digger.GetPathVoxelMetadataFile(chunkPosition);
            using (Stream stream = new FileStream(metadataPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                using (var reader = new BinaryReader(stream, Encoding.Default)) {
                    bounds = new ChunkTriggerBounds(
                        reader.ReadBoolean(),
                        new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                        new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                        hScale);
                }
            }

            Utils.Profiler.EndSample();
        }
    }
}