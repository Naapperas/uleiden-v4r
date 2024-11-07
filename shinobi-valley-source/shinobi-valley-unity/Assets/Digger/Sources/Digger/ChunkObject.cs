using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Digger
{
    public class ChunkObject : MonoBehaviour
    {
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private MeshFilter filter;
        [SerializeField] private MeshCollider meshCollider;
        [SerializeField] private BoxCollider holeCollider;
        [SerializeField] private bool hasCollider;
        [SerializeField] private Texture2D[] controlMaps;
        [SerializeField] private Material material;

        public Texture2D[] ControlMaps => controlMaps;
        public Material Material => material;

        internal static ChunkObject Create(int lod,
                                           Vector3i chunkPosition,
                                           ChunkLODGroup chunkLodGroup,
                                           bool hasCollider,
                                           DiggerSystem digger,
                                           Terrain terrain,
                                           Material material,
                                           LayerMask layer)
        {
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

            go.transform.parent = chunkLodGroup.transform;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var chunkObject = go.AddComponent<ChunkObject>();
            chunkObject.enabled = false;
            chunkObject.hasCollider = hasCollider;
            chunkObject.meshRenderer = go.AddComponent<MeshRenderer>();
            chunkObject.meshRenderer.lightmapScaleOffset = digger.Terrain.lightmapScaleOffset;
            chunkObject.meshRenderer.realtimeLightmapScaleOffset = digger.Terrain.realtimeLightmapScaleOffset;
            chunkObject.meshRenderer.sharedMaterial = material;
            chunkObject.material = material;
            SetupMeshRenderer(digger.Terrain, chunkObject.meshRenderer);

            go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
            go.GetComponent<Renderer>().receiveShadows = true;
            chunkObject.filter = go.AddComponent<MeshFilter>();
            chunkObject.meshRenderer.enabled = false;

            if (hasCollider) {
                chunkObject.meshCollider = go.AddComponent<MeshCollider>();

                var goCollider = new GameObject("ChunkTrigger");
                goCollider.transform.parent = go.transform;
                goCollider.transform.localPosition = Vector3.zero;
                var colliderHole = goCollider.AddComponent<TerrainColliderHoleComponent>();
                colliderHole.Digger = digger;
                colliderHole.TerrainCollider = terrain.GetComponent<TerrainCollider>();

                chunkObject.holeCollider = goCollider.AddComponent<BoxCollider>();
                chunkObject.holeCollider.isTrigger = true;
                chunkObject.holeCollider.enabled = false;
            }

            if (digger.MaterialType == TerrainMaterialType.MicroSplat) {
#if (__MICROSPLAT__ && __MICROSPLAT_MESH__)
                Utils.Profiler.BeginSample("ChunkObject.Create MicroSplat assets");
                AssetDatabase.StartAssetEditing();
                material = new Material(material);
                AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath(Path.Combine(digger.BasePathData, $"microsplat_{chunkPosition.x}_{chunkPosition.y}_{chunkPosition.z}_{lod}.mat")));

                chunkObject.material = material;
                chunkObject.controlMaps = new Texture2D[4];
                for (var i = 0; i < 4; ++i) {
                    var tex = new Texture2D(512 / lod, 512 / lod, TextureFormat.RGBA32, true, true);
                    AssetDatabase.CreateAsset(tex, AssetDatabase.GenerateUniqueAssetPath(Path.Combine(digger.BasePathData, $"control_{chunkPosition.x}_{chunkPosition.y}_{chunkPosition.z}_{lod}.asset")));
                    material.SetTexture("_Control" + i, tex);
                    chunkObject.controlMaps[i] = tex;
                }

                var microSplatMesh = go.AddComponent<MicroSplatMesh>();
                microSplatMesh.templateMaterial = material;
                microSplatMesh.controlTextures = chunkObject.ControlMaps;
                microSplatMesh.Sync();
                AssetDatabase.StopAssetEditing();
                Utils.Profiler.EndSample();
#else
                // affect null explicitly to avoid warning
                chunkObject.controlMaps = null;
#endif
            }

            return chunkObject;
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

        public static string GetName(Vector3i chunkPosition)
        {
            return $"ChunkObject_{chunkPosition.x}_{chunkPosition.y}_{chunkPosition.z}";
        }

        public bool PostBuild(Mesh visualMesh, Mesh collisionMesh, ChunkTriggerBounds bounds)
        {
            Utils.Profiler.BeginSample("[Dig] Chunk.PostBuild");
            var hasVisualMesh = false;
            if (visualMesh != null && visualMesh.vertexCount > 0 && visualMesh.triangles.Length > 0) {
                filter.sharedMesh = visualMesh;
                meshRenderer.enabled = true;
                hasVisualMesh = true;
            } else {
                if (filter.sharedMesh) {
                    if (Application.isEditor) {
                        DestroyImmediate(filter.sharedMesh);
                    } else {
                        Destroy(filter.sharedMesh);
                    }

                    filter.sharedMesh = null;
                }

                meshRenderer.enabled = false;
            }

            if (hasCollider) {
                if (!bounds.IsVirgin) {
                    var b = bounds.ToBounds();
                    holeCollider.center = b.center;
                    holeCollider.size = b.size + Vector3.one * 2;
                    holeCollider.enabled = true;
                } else {
                    holeCollider.enabled = false;
                }

                if (collisionMesh != null && collisionMesh.vertexCount > 0 && collisionMesh.triangles.Length > 0) {
                    meshCollider.sharedMesh = collisionMesh;
                    meshCollider.enabled = true;
                } else {
                    if (meshCollider.sharedMesh) {
                        if (Application.isEditor) {
                            DestroyImmediate(meshCollider.sharedMesh);
                        } else {
                            Destroy(meshCollider.sharedMesh);
                        }

                        meshCollider.sharedMesh = null;
                    }

                    meshCollider.enabled = false;
                }
            }

            Utils.Profiler.EndSample();
            return hasVisualMesh;
        }
    }
}