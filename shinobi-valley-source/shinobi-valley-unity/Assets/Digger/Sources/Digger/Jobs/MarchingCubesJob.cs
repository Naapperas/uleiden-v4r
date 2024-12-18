using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Digger
{
    [BurstCompile(CompileSynchronously = false)]
    internal struct MarchingCubesJob : IJobParallelFor
    {
        public struct Out
        {
            public NativeArray<Vector3> outVertices;
            public NativeArray<Vector3> outNormals;
            public NativeArray<int2> outInfos;
            public NativeArray<int> outTriangles;
            public NativeArray<Vector2> outUV1s;
            public NativeArray<Color> outColors;
            public NativeArray<Vector4> outUV2s;
            public NativeArray<Vector4> outUV3s;
            public NativeArray<Vector4> outUV4s;

            public void Dispose()
            {
                outVertices.Dispose();
                outNormals.Dispose();
                outInfos.Dispose();
                outTriangles.Dispose();
                outUV1s.Dispose();
                outColors.Dispose();
                outUV2s.Dispose();
                outUV3s.Dispose();
                outUV4s.Dispose();
            }

            public static Out New(bool fullOutput)
            {
                var o = new Out
                {
                    outVertices = new NativeArray<Vector3>(32767, Allocator.TempJob),
                    outNormals = new NativeArray<Vector3>(32767, Allocator.TempJob),
                    outTriangles = new NativeArray<int>(32767, Allocator.TempJob),
                    outInfos = new NativeArray<int2>(fullOutput ? 32767 : 1, Allocator.TempJob),
                    outUV1s = new NativeArray<Vector2>(fullOutput ? 32767 : 1, Allocator.TempJob),
                    outColors = new NativeArray<Color>(fullOutput ? 32767 : 1, Allocator.TempJob),
                    outUV2s = new NativeArray<Vector4>(fullOutput ? 32767 : 1, Allocator.TempJob),
                    outUV3s = new NativeArray<Vector4>(fullOutput ? 32767 : 1, Allocator.TempJob),
                    outUV4s = new NativeArray<Vector4>(fullOutput ? 32767 : 1, Allocator.TempJob)
                };

                return o;
            }
        }

        private struct WorkNorm
        {
            public Vector3 N0;
            public Vector3 N1;
            public Vector3 N2;
            public Vector3 N3;
            public Vector3 N4;
            public Vector3 N5;
            public Vector3 N6;
            public Vector3 N7;

            public Vector3 this[int i] {
                get {
                    switch (i) {
                        case 0: return N0;
                        case 1: return N1;
                        case 2: return N2;
                        case 3: return N3;
                        case 4: return N4;
                        case 5: return N5;
                        case 6: return N6;
                        case 7: return N7;
                    }

                    return Vector3.zero; // don't throw an exception to allow Burst compilation
                }
            }
        }

        private struct WorkVert
        {
            public Vector3 V0;
            public Vector3 V1;
            public Vector3 V2;
            public Vector3 V3;
            public Vector3 V4;
            public Vector3 V5;
            public Vector3 V6;
            public Vector3 V7;
            public Vector3 V8;
            public Vector3 V9;
            public Vector3 V10;
            public Vector3 V11;

            public Vector3 this[int i] {
                get {
                    switch (i) {
                        case 0:  return V0;
                        case 1:  return V1;
                        case 2:  return V2;
                        case 3:  return V3;
                        case 4:  return V4;
                        case 5:  return V5;
                        case 6:  return V6;
                        case 7:  return V7;
                        case 8:  return V8;
                        case 9:  return V9;
                        case 10: return V10;
                        case 11: return V11;
                    }

                    return Vector3.zero; // don't throw an exception to allow Burst compilation
                }
            }
        }

        private struct WorkVertIndices
        {
            public int Vi0;
            public int Vi1;
            public int Vi2;
            public int Vi3;
            public int Vi4;
            public int Vi5;
            public int Vi6;
            public int Vi7;
            public int Vi8;
            public int Vi9;
            public int Vi10;
            public int Vi11;

            public int this[int i] {
                get {
                    switch (i) {
                        case 0:  return Vi0;
                        case 1:  return Vi1;
                        case 2:  return Vi2;
                        case 3:  return Vi3;
                        case 4:  return Vi4;
                        case 5:  return Vi5;
                        case 6:  return Vi6;
                        case 7:  return Vi7;
                        case 8:  return Vi8;
                        case 9:  return Vi9;
                        case 10: return Vi10;
                        case 11: return Vi11;
                    }

                    return -1; // don't throw an exception to allow Burst compilation
                }
            }
        }

        private const int SizeVox = Chunk.SizeVox;
        private const int SizeVox2 = Chunk.SizeVox2;

        [ReadOnly] private NativeArray<int> edgeTable;
        [ReadOnly] private NativeArray<int> triTable;
        [ReadOnly] private NativeArray<Vector3> corners;
        [ReadOnly] private NativeArray<Voxel> voxels;
        [ReadOnly] private NativeArray<float> alphamaps;

        private int2 alphamapsSize;
        private int mapCount;

        private NativeCounter.Concurrent vertexCounter;
        private NativeCounter.Concurrent triangleCounter;

        [WriteOnly] [NativeDisableParallelForRestriction]
        private NativeArray<Vector3> outVertices;

        [WriteOnly] [NativeDisableParallelForRestriction]
        private NativeArray<Vector3> outNormals;

        [WriteOnly] [NativeDisableParallelForRestriction]
        private NativeArray<int> outTriangles;

        [WriteOnly] [NativeDisableParallelForRestriction]
        private NativeArray<int2> outInfos;

        [WriteOnly] [NativeDisableParallelForRestriction]
        private NativeArray<Vector2> outUV1s;

        [WriteOnly] [NativeDisableParallelForRestriction]
        private NativeArray<Color> outColors;

        [WriteOnly] [NativeDisableParallelForRestriction]
        private NativeArray<Vector4> outUV2s;

        [WriteOnly] [NativeDisableParallelForRestriction]
        private NativeArray<Vector4> outUV3s;

        [WriteOnly] [NativeDisableParallelForRestriction]
        private NativeArray<Vector4> outUV4s;


        private Vector3 chunkWorldPosition;
        private Vector3 scale;
        private Vector2 uvScale;
        private int lod;

        public float Isovalue;
        public byte AlteredOnly;
        public byte FullOutput;


        public MarchingCubesJob(NativeArray<int> edgeTable,
                                NativeArray<int> triTable,
                                NativeArray<Vector3> corners,
                                NativeCounter.Concurrent vertexCounter,
                                NativeCounter.Concurrent triangleCounter,
                                NativeArray<Voxel> voxels,
                                NativeArray<float> alphamaps,
                                Out o,
                                Vector3 scale,
                                Vector2 uvScale,
                                Vector3 chunkWorldPosition,
                                int lod,
                                int2 alphamapsSize,
                                int mapCount)
        {
            this.edgeTable = edgeTable;
            this.triTable = triTable;
            this.corners = corners;
            this.vertexCounter = vertexCounter;
            this.triangleCounter = triangleCounter;
            this.voxels = voxels;
            this.alphamaps = alphamaps;

            this.outVertices = o.outVertices;
            this.outNormals = o.outNormals;
            this.outInfos = o.outInfos;
            this.outTriangles = o.outTriangles;
            this.outUV1s = o.outUV1s;
            this.outColors = o.outColors;
            this.outUV2s = o.outUV2s;
            this.outUV3s = o.outUV3s;
            this.outUV4s = o.outUV4s;

            this.Isovalue = 0;
            this.AlteredOnly = 1;
            this.FullOutput = 1;
            this.scale = scale;
            this.lod = lod;
            this.alphamapsSize = alphamapsSize;
            this.mapCount = mapCount;
            this.uvScale = uvScale;
            this.chunkWorldPosition = chunkWorldPosition;
        }

        private int2 VertexInfo(Voxel vA, Voxel vB)
        {
            if (Math.Abs(vA.Value) < Math.Abs(vB.Value)) {
                return new int2(Convert.ToInt32(vA.Altered), Convert.ToInt32(vA.Altered));
            }

            return new int2(Convert.ToInt32(vB.Altered), Convert.ToInt32(vB.Altered));
        }

        private Vector3 VertexInterp(Vector3 p1, Vector3 p2, Voxel vA, Voxel vB)
        {
            if (Utils.Approximately(vA.Value, 0))
                return p1;
            if (Utils.Approximately(vB.Value, 0))
                return p2;

            var mu = (Isovalue - vA.Value) / (vB.Value - vA.Value);

            Vector3 p;
            p.x = p1.x + mu * (p2.x - p1.x);
            p.y = p1.y + mu * (p2.y - p1.y);
            p.z = p1.z + mu * (p2.z - p1.z);

            return p;
        }

        private Vector3 ComputeNormalAt(int xi, int yi, int zi)
        {
            return Utils.Normalize(new Vector3(
                                       voxels[(xi + 1) * SizeVox2 + yi * SizeVox + zi].Value - voxels[(xi - 1) * SizeVox2 + yi * SizeVox + zi].Value,
                                       voxels[xi * SizeVox2 + (yi + 1) * SizeVox + zi].Value - voxels[xi * SizeVox2 + (yi - 1) * SizeVox + zi].Value,
                                       voxels[xi * SizeVox2 + yi * SizeVox + (zi + 1)].Value - voxels[xi * SizeVox2 + yi * SizeVox + (zi - 1)].Value
                                   ));
        }


        private void ComputeUVsAndColor(int vertIndex, Vector3 vertex, int texInfo)
        {
            var uv = new Vector2((chunkWorldPosition.x + vertex.x) * uvScale.x, (chunkWorldPosition.z + vertex.z) * uvScale.y);
            outUV1s[vertIndex] = uv;

            if (texInfo == 0 || texInfo == 1 || texInfo == -1) {
                // near the terrain surface -> set same texture
                outColors[vertIndex] = GetControlAt(uv, 0);
                outUV2s[vertIndex] = GetControlAt(uv, 1);
                outUV3s[vertIndex] = GetControlAt(uv, 2);
                outUV4s[vertIndex] = GetControlAt(uv, 3);
            } else {
                var textureIndex = Voxel.GetTextureIndex(texInfo);
                if (textureIndex < 0) {
                    //error: Debug.LogError($"Texture index is negative: {textureIndex}, {texInfo}");
                    return;
                }

                outColors[vertIndex] = GetControlFor(textureIndex, 0);
                outUV2s[vertIndex] = GetControlFor(textureIndex, 1);
                outUV3s[vertIndex] = GetControlFor(textureIndex, 2);
                outUV4s[vertIndex] = GetControlFor(textureIndex, 3);
            }
        }

        private Vector4 GetControlAt(Vector2 uv, int index)
        {
            // adjust splatUVs so the edges of the terrain tile lie on pixel centers
            var splatUV = new Vector2(uv.x * (alphamapsSize.x - 1), uv.y * (alphamapsSize.y - 1));

            var x = Math.Min(Math.Max(Convert.ToInt32(Math.Floor(splatUV.x)), 0), alphamapsSize.x - 2);
            var z = Math.Min(Math.Max(Convert.ToInt32(Math.Floor(splatUV.y)), 0), alphamapsSize.y - 2);
            var relPos = splatUV - new Vector2(x, z);

            index *= 4;

            var ctrl = Vector4.zero;
            for (var i = 0; i < 4; ++i) {
                if (index + i < mapCount) {
                    var a00 = alphamaps[x * alphamapsSize.y * mapCount + z * mapCount + index + i];
                    var a01 = alphamaps[(x + 1) * alphamapsSize.y * mapCount + z * mapCount + index + i];
                    var a10 = alphamaps[x * alphamapsSize.y * mapCount + (z + 1) * mapCount + index + i];
                    var a11 = alphamaps[(x + 1) * alphamapsSize.y * mapCount + (z + 1) * mapCount + index + i];
                    ctrl[i] = Utils.BilinearInterpolate(a00, a01, a10, a11, relPos.y, relPos.x);
                }
            }

            return ctrl;
        }

        private static Vector4 GetControlFor(int textureIndex, int index)
        {
            var ctrl = Vector4.zero;
            if (index * 4 == textureIndex)
                ctrl.x = 1f;
            else if (index * 4 + 1 == textureIndex)
                ctrl.y = 1f;
            else if (index * 4 + 2 == textureIndex)
                ctrl.z = 1f;
            else if (index * 4 + 3 == textureIndex)
                ctrl.w = 1f;
            return ctrl;
        }


        public void Execute(int index)
        {
            var xi = index / SizeVox2;
            var yi = (index - xi * SizeVox2) / SizeVox;
            var zi = index - xi * SizeVox2 - yi * SizeVox;

            if (xi == 0 || xi >= SizeVox - lod - 1 || yi == 0 || yi >= SizeVox - lod - 1 || zi == 0 || zi >= SizeVox - lod - 1
                || (xi - 1) % lod != 0 || (yi - 1) % lod != 0 || (zi - 1) % lod != 0)
                return;

            var v0 = voxels[xi * SizeVox * SizeVox + yi * SizeVox + zi];
            var v1 = voxels[(xi + lod) * SizeVox * SizeVox + yi * SizeVox + zi];
            var v2 = voxels[(xi + lod) * SizeVox * SizeVox + yi * SizeVox + (zi + lod)];
            var v3 = voxels[xi * SizeVox * SizeVox + yi * SizeVox + (zi + lod)];
            var v4 = voxels[xi * SizeVox * SizeVox + (yi + lod) * SizeVox + zi];
            var v5 = voxels[(xi + lod) * SizeVox * SizeVox + (yi + lod) * SizeVox + zi];
            var v6 = voxels[(xi + lod) * SizeVox * SizeVox + (yi + lod) * SizeVox + (zi + lod)];
            var v7 = voxels[xi * SizeVox * SizeVox + (yi + lod) * SizeVox + (zi + lod)];

            if (AlteredOnly == 1 &&
                v0.Altered == 0 &&
                v1.Altered == 0 &&
                v2.Altered == 0 &&
                v3.Altered == 0 &&
                v4.Altered == 0 &&
                v5.Altered == 0 &&
                v6.Altered == 0 &&
                v7.Altered == 0)
                return;

            var cubeindex = 0;
            if (v0.IsInside) cubeindex |= 1;
            if (v1.IsInside) cubeindex |= 2;
            if (v2.IsInside) cubeindex |= 4;
            if (v3.IsInside) cubeindex |= 8;
            if (v4.IsInside) cubeindex |= 16;
            if (v5.IsInside) cubeindex |= 32;
            if (v6.IsInside) cubeindex |= 64;
            if (v7.IsInside) cubeindex |= 128;

            /* Cube is entirely in/out of the surface */
            if (cubeindex == 0 || cubeindex == 255)
                return;

            var position = new Vector3
            {
                x = xi - 1,
                y = yi - 1,
                z = zi - 1
            };

            var voxelNorm = new WorkNorm
            {
                N0 = ComputeNormalAt(xi, yi, zi),
                N1 = ComputeNormalAt((xi + lod), yi, zi),
                N2 = ComputeNormalAt((xi + lod), yi, (zi + lod)),
                N3 = ComputeNormalAt(xi, yi, (zi + lod)),
                N4 = ComputeNormalAt(xi, (yi + lod), zi),
                N5 = ComputeNormalAt((xi + lod), (yi + lod), zi),
                N6 = ComputeNormalAt((xi + lod), (yi + lod), (zi + lod)),
                N7 = ComputeNormalAt(xi, (yi + lod), (zi + lod))
            };

            var wVert = new WorkVert();
            var wVertIndices = new WorkVertIndices();

            /* Find the vertices where the surface intersects the cube */
            if ((edgeTable[cubeindex] & 1) != 0) {
                var norm = VertexInterp(voxelNorm.N0, voxelNorm.N1, v0, v1);
                wVert.V0 = Vector3.Scale(position + VertexInterp(corners[0], corners[1], v0, v1) * lod, scale);
                var vertIndex = vertexCounter.Increment() - 1;
                outVertices[vertIndex] = wVert.V0;
                outNormals[vertIndex] = Utils.Normalize(norm);
                wVertIndices.Vi0 = vertIndex;
                if (FullOutput == 1) {
                    var texInfo = VertexInfo(v0, v1);
                    outInfos[vertIndex] = texInfo;
                    ComputeUVsAndColor(vertIndex, wVert.V0, texInfo.x);
                }
            }

            if ((edgeTable[cubeindex] & 2) != 0) {
                var norm = VertexInterp(voxelNorm.N1, voxelNorm.N2, v1, v2);
                wVert.V1 = Vector3.Scale(position + VertexInterp(corners[1], corners[2], v1, v2) * lod, scale);
                var vertIndex = vertexCounter.Increment() - 1;
                outVertices[vertIndex] = wVert.V1;
                outNormals[vertIndex] = Utils.Normalize(norm);
                wVertIndices.Vi1 = vertIndex;
                if (FullOutput == 1) {
                    var texInfo = VertexInfo(v1, v2);
                    outInfos[vertIndex] = texInfo;
                    ComputeUVsAndColor(vertIndex, wVert.V1, texInfo.x);
                }
            }

            if ((edgeTable[cubeindex] & 4) != 0) {
                var norm = VertexInterp(voxelNorm.N2, voxelNorm.N3, v2, v3);
                wVert.V2 = Vector3.Scale(position + VertexInterp(corners[2], corners[3], v2, v3) * lod, scale);
                var vertIndex = vertexCounter.Increment() - 1;
                outVertices[vertIndex] = wVert.V2;
                outNormals[vertIndex] = Utils.Normalize(norm);
                wVertIndices.Vi2 = vertIndex;
                if (FullOutput == 1) {
                    var texInfo = VertexInfo(v2, v3);
                    outInfos[vertIndex] = texInfo;
                    ComputeUVsAndColor(vertIndex, wVert.V2, texInfo.x);
                }
            }

            if ((edgeTable[cubeindex] & 8) != 0) {
                var norm = VertexInterp(voxelNorm.N3, voxelNorm.N0, v3, v0);
                wVert.V3 = Vector3.Scale(position + VertexInterp(corners[3], corners[0], v3, v0) * lod, scale);
                var vertIndex = vertexCounter.Increment() - 1;
                outVertices[vertIndex] = wVert.V3;
                outNormals[vertIndex] = Utils.Normalize(norm);
                wVertIndices.Vi3 = vertIndex;
                if (FullOutput == 1) {
                    var texInfo = VertexInfo(v3, v0);
                    outInfos[vertIndex] = texInfo;
                    ComputeUVsAndColor(vertIndex, wVert.V3, texInfo.x);
                }
            }

            if ((edgeTable[cubeindex] & 16) != 0) {
                var norm = VertexInterp(voxelNorm.N4, voxelNorm.N5, v4, v5);
                wVert.V4 = Vector3.Scale(position + VertexInterp(corners[4], corners[5], v4, v5) * lod, scale);
                var vertIndex = vertexCounter.Increment() - 1;
                outVertices[vertIndex] = wVert.V4;
                outNormals[vertIndex] = Utils.Normalize(norm);
                wVertIndices.Vi4 = vertIndex;
                if (FullOutput == 1) {
                    var texInfo = VertexInfo(v4, v5);
                    outInfos[vertIndex] = texInfo;
                    ComputeUVsAndColor(vertIndex, wVert.V4, texInfo.x);
                }
            }

            if ((edgeTable[cubeindex] & 32) != 0) {
                var norm = VertexInterp(voxelNorm.N5, voxelNorm.N6, v5, v6);
                wVert.V5 = Vector3.Scale(position + VertexInterp(corners[5], corners[6], v5, v6) * lod, scale);
                var vertIndex = vertexCounter.Increment() - 1;
                outVertices[vertIndex] = wVert.V5;
                outNormals[vertIndex] = Utils.Normalize(norm);
                wVertIndices.Vi5 = vertIndex;
                if (FullOutput == 1) {
                    var texInfo = VertexInfo(v5, v6);
                    outInfos[vertIndex] = texInfo;
                    ComputeUVsAndColor(vertIndex, wVert.V5, texInfo.x);
                }
            }

            if ((edgeTable[cubeindex] & 64) != 0) {
                var norm = VertexInterp(voxelNorm.N6, voxelNorm.N7, v6, v7);
                wVert.V6 = Vector3.Scale(position + VertexInterp(corners[6], corners[7], v6, v7) * lod, scale);
                var vertIndex = vertexCounter.Increment() - 1;
                outVertices[vertIndex] = wVert.V6;
                outNormals[vertIndex] = Utils.Normalize(norm);
                wVertIndices.Vi6 = vertIndex;
                if (FullOutput == 1) {
                    var texInfo = VertexInfo(v6, v7);
                    outInfos[vertIndex] = texInfo;
                    ComputeUVsAndColor(vertIndex, wVert.V6, texInfo.x);
                }
            }

            if ((edgeTable[cubeindex] & 128) != 0) {
                var norm = VertexInterp(voxelNorm.N7, voxelNorm.N4, v7, v4);
                wVert.V7 = Vector3.Scale(position + VertexInterp(corners[7], corners[4], v7, v4) * lod, scale);
                var vertIndex = vertexCounter.Increment() - 1;
                outVertices[vertIndex] = wVert.V7;
                outNormals[vertIndex] = Utils.Normalize(norm);
                wVertIndices.Vi7 = vertIndex;
                if (FullOutput == 1) {
                    var texInfo = VertexInfo(v7, v4);
                    outInfos[vertIndex] = texInfo;
                    ComputeUVsAndColor(vertIndex, wVert.V7, texInfo.x);
                }
            }

            if ((edgeTable[cubeindex] & 256) != 0) {
                var norm = VertexInterp(voxelNorm.N0, voxelNorm.N4, v0, v4);
                wVert.V8 = Vector3.Scale(position + VertexInterp(corners[0], corners[4], v0, v4) * lod, scale);
                var vertIndex = vertexCounter.Increment() - 1;
                outVertices[vertIndex] = wVert.V8;
                outNormals[vertIndex] = Utils.Normalize(norm);
                wVertIndices.Vi8 = vertIndex;
                if (FullOutput == 1) {
                    var texInfo = VertexInfo(v0, v4);
                    outInfos[vertIndex] = texInfo;
                    ComputeUVsAndColor(vertIndex, wVert.V8, texInfo.x);
                }
            }

            if ((edgeTable[cubeindex] & 512) != 0) {
                var norm = VertexInterp(voxelNorm.N1, voxelNorm.N5, v1, v5);
                wVert.V9 = Vector3.Scale(position + VertexInterp(corners[1], corners[5], v1, v5) * lod, scale);
                var vertIndex = vertexCounter.Increment() - 1;
                outVertices[vertIndex] = wVert.V9;
                outNormals[vertIndex] = Utils.Normalize(norm);
                wVertIndices.Vi9 = vertIndex;
                if (FullOutput == 1) {
                    var texInfo = VertexInfo(v1, v5);
                    outInfos[vertIndex] = texInfo;
                    ComputeUVsAndColor(vertIndex, wVert.V9, texInfo.x);
                }
            }

            if ((edgeTable[cubeindex] & 1024) != 0) {
                var norm = VertexInterp(voxelNorm.N2, voxelNorm.N6, v2, v6);
                wVert.V10 = Vector3.Scale(position + VertexInterp(corners[2], corners[6], v2, v6) * lod, scale);
                var vertIndex = vertexCounter.Increment() - 1;
                outVertices[vertIndex] = wVert.V10;
                outNormals[vertIndex] = Utils.Normalize(norm);
                wVertIndices.Vi10 = vertIndex;
                if (FullOutput == 1) {
                    var texInfo = VertexInfo(v2, v6);
                    outInfos[vertIndex] = texInfo;
                    ComputeUVsAndColor(vertIndex, wVert.V10, texInfo.x);
                }
            }

            if ((edgeTable[cubeindex] & 2048) != 0) {
                var norm = VertexInterp(voxelNorm.N3, voxelNorm.N7, v3, v7);
                wVert.V11 = Vector3.Scale(position + VertexInterp(corners[3], corners[7], v3, v7) * lod, scale);
                var vertIndex = vertexCounter.Increment() - 1;
                outVertices[vertIndex] = wVert.V11;
                outNormals[vertIndex] = Utils.Normalize(norm);
                wVertIndices.Vi11 = vertIndex;
                if (FullOutput == 1) {
                    var texInfo = VertexInfo(v3, v7);
                    outInfos[vertIndex] = texInfo;
                    ComputeUVsAndColor(vertIndex, wVert.V11, texInfo.x);
                }
            }

            /* Create the triangle */
            for (var i = 0; triTable[cubeindex * 16 + i] != -1; i += 3) {
                var i1 = triTable[cubeindex * 16 + (i + 0)];
                var i2 = triTable[cubeindex * 16 + (i + 1)];
                var i3 = triTable[cubeindex * 16 + (i + 2)];
                var vert1 = wVert[i1];
                var vert2 = wVert[i2];
                var vert3 = wVert[i3];
                if (!Utils.Approximately(vert1, vert2) &&
                    !Utils.Approximately(vert2, vert3) &&
                    !Utils.Approximately(vert1, vert3) &&
                    !Utils.AreColinear(vert1, vert2, vert3)) {
                    var triIndex = triangleCounter.Increment() - 3;
                    outTriangles[triIndex + 0] = wVertIndices[i1];
                    outTriangles[triIndex + 1] = wVertIndices[i2];
                    outTriangles[triIndex + 2] = wVertIndices[i3];
                }
            }
        }
    }
}