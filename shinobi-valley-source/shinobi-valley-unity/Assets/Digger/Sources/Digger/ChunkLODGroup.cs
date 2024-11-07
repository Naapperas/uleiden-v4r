using System;
using UnityEngine;

#if UNITY_EDITOR

#endif

namespace Digger
{
    public class ChunkLODGroup : MonoBehaviour
    {
        public const int LODCount = 3;
        [SerializeField] private LODGroup lodGroup;
        [SerializeField] private ChunkObject[] chunks;

        internal static ChunkLODGroup Create(Vector3i chunkPosition,
                                             Chunk chunk,
                                             DiggerSystem digger,
                                             Terrain terrain,
                                             Material material,
                                             LayerMask layer)
        {
            var go = new GameObject(GetName(chunkPosition));
            go.transform.parent = chunk.transform;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var chunkLodGroup = go.AddComponent<ChunkLODGroup>();
            var lodGroup = go.AddComponent<LODGroup>();
            chunkLodGroup.chunks = new[]
            {
                ChunkObject.Create(1, chunkPosition, chunkLodGroup, digger.ColliderLodIndex == 0, digger, terrain, material, layer),
                ChunkObject.Create(2, chunkPosition, chunkLodGroup, digger.ColliderLodIndex == 1, digger, terrain, material, layer),
                ChunkObject.Create(4, chunkPosition, chunkLodGroup, digger.ColliderLodIndex == 2, digger, terrain, material, layer)
            };
            var renderers = new Renderer[chunkLodGroup.chunks.Length];
            for (var i = 0; i < renderers.Length; ++i) {
                renderers[i] = chunkLodGroup.chunks[i].GetComponent<MeshRenderer>();
            }

            var lods = new[]
            {
                new LOD(digger.ScreenRelativeTransitionHeightLod0, new[] {renderers[0]}),
                new LOD(digger.ScreenRelativeTransitionHeightLod1, new[] {renderers[1]}),
                new LOD(0f, new[] {renderers[2]})
            };
            lodGroup.SetLODs(lods);
            chunkLodGroup.lodGroup = lodGroup;
            return chunkLodGroup;
        }

        public static string GetName(Vector3i chunkPosition)
        {
            return $"ChunkLODGroup_{chunkPosition.x}_{chunkPosition.y}_{chunkPosition.z}";
        }

        public bool PostBuild(int lodIndex, Mesh visualMesh, Mesh collisionMesh, ChunkTriggerBounds bounds)
        {
            var hasVisualMesh = chunks[lodIndex].PostBuild(visualMesh, collisionMesh, bounds);
            lodGroup.RecalculateBounds();
            return hasVisualMesh;
        }

        public static int IndexToLod(int lod)
        {
            return 1 << lod;
        }

        public static int LodToIndex(int lod)
        {
            switch (lod) {
                case 1:
                    return 0;
                case 2:
                    return 1;
                case 4:
                    return 2;
                case 8:
                    return 3;
                default:
                    throw new IndexOutOfRangeException($"Invalid LOD: {lod}");
            }
        }

        public Texture2D[] GetControlMaps(int lodIndex)
        {
            return chunks[lodIndex].ControlMaps;
        }

        public void ApplyControlMaps(int lodIndex)
        {
            var m = chunks[lodIndex].Material;
            var cm = chunks[lodIndex].ControlMaps;
            for (var i = 0; cm != null && i < cm.Length; ++i) {
                m.SetTexture("_Control" + i, cm[i]);
            }
        }
    }
}