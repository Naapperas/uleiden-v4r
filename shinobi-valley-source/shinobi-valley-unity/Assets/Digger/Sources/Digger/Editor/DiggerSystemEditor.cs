using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Digger
{
    [CustomEditor(typeof(DiggerSystem))]
    public class DiggerSystemEditor : Editor
    {
        private DiggerSystem diggerSystem;
        private static readonly int TerrainWidthInvProperty = Shader.PropertyToID("_TerrainWidthInv");
        private static readonly int TerrainHeightInvProperty = Shader.PropertyToID("_TerrainHeightInv");
        private static readonly int SplatArrayProperty = Shader.PropertyToID("_SplatArray");
        private static readonly int NormalArrayProperty = Shader.PropertyToID("_NormalArray");
        private const string SplatPrefixProperty = "_Splat";
        private const string NormalPrefixProperty = "_Normal";

        public void OnEnable()
        {
            diggerSystem = (DiggerSystem) target;
            Init(diggerSystem, false);
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox($"Digger data for this terrain can be found in {diggerSystem.BasePathData}", MessageType.Info);
            EditorGUILayout.HelpBox($"Raw voxel data can be found in {diggerSystem.BasePathData}/.internal", MessageType.Info);
            EditorGUILayout.HelpBox("DO NOT CHANGE / RENAME / MOVE this folder.", MessageType.Warning);
            EditorGUILayout.HelpBox("Don\'t forget to backup this folder as well when you backup your project.", MessageType.Warning);

            EditorGUILayout.LabelField("Use Digger Master to start digging.");

            var showDebug = EditorGUILayout.Toggle("Show debug data", diggerSystem.ShowDebug);
            if (showDebug != diggerSystem.ShowDebug) {
                foreach (Transform child in diggerSystem.transform) {
                    child.gameObject.hideFlags = showDebug ? HideFlags.None : HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                }
            }

            diggerSystem.ShowDebug = showDebug;
            if (showDebug) {
                EditorGUILayout.LabelField($"GUID: {diggerSystem.Guid}");
                EditorGUILayout.LabelField($"Undo/redo stack version: {diggerSystem.Version}");

                DrawDefaultInspector();
            }
        }

        public static void Init(DiggerSystem diggerSystem, bool forceRefresh)
        {
            if (!forceRefresh && diggerSystem.IsInitialized)
                return;

            diggerSystem.PreInit();

            if (!diggerSystem.Material || forceRefresh)
                SetupMaterial(diggerSystem, forceRefresh);

            diggerSystem.Init(forceRefresh);
        }

        private static void SetupMaterial(DiggerSystem diggerSystem, bool forceRefresh)
        {
            Utils.Profiler.BeginSample("[Dig] SetupMaterial");

            if (EditorUtils.CTSExists(diggerSystem.Terrain)) {
                diggerSystem.MaterialType = TerrainMaterialType.CTS;
                SetupCTSMaterial(diggerSystem);
            } else if (EditorUtils.MicroSplatExists(diggerSystem.Terrain)) {
                diggerSystem.MaterialType = TerrainMaterialType.MicroSplat;
                SetupMicroSplatMaterial(diggerSystem, diggerSystem.Terrain.materialTemplate);
            } else {
                diggerSystem.MaterialType = TerrainMaterialType.Standard;
                SetupDefaultMaterial(diggerSystem, forceRefresh);
            }

            Utils.Profiler.EndSample();
        }

        private static void SetupDefaultMaterial(DiggerSystem diggerSystem, bool forceRefresh)
        {
            var use2DArrayTextures = SystemInfo.supports2DArrayTextures;

            var expectedShaderName = use2DArrayTextures ? "Nature/Terrain/Digger/Mesh-Standard" : "Nature/Terrain/Digger/Mesh-Standard-NoTxtArr";
            if (!diggerSystem.Material || diggerSystem.Material.shader.name != expectedShaderName) {
                diggerSystem.Material = new Material(Shader.Find(expectedShaderName));
            }

            if (diggerSystem.Terrain.materialType != Terrain.MaterialType.Custom || !diggerSystem.Terrain.materialTemplate || forceRefresh) {
                diggerSystem.Terrain.materialType = Terrain.MaterialType.Custom;
                var terrainMaterial = new Material(Shader.Find("Nature/Terrain/Digger/Cuttable-Triplanar"));
                terrainMaterial = EditorUtils.CreateOrReplaceAsset(terrainMaterial, Path.Combine(diggerSystem.BasePathData, "terrainMaterial.mat"));
                terrainMaterial.SetFloat(TerrainWidthInvProperty, 1f / diggerSystem.Terrain.terrainData.size.x);
                terrainMaterial.SetFloat(TerrainHeightInvProperty, 1f / diggerSystem.Terrain.terrainData.size.z);
                diggerSystem.Terrain.materialTemplate = terrainMaterial;
            }

            if (diggerSystem.Terrain.materialTemplate.shader.name != "Nature/Terrain/Digger/Cuttable-Triplanar")
                Debug.LogWarning("Looks like terrain material doesn't match cave meshes material.");

            var showNMWarning = false;
            var tData = diggerSystem.Terrain.terrainData;
            var textures = new List<Texture2D>();
            var normals = new List<Texture2D>();
            for (var i = 0; i < tData.terrainLayers.Length && i < DiggerSystem.MaxTextureCountSupported; i++) {
                var terrainLayer = tData.terrainLayers[i];
                if (terrainLayer == null || terrainLayer.diffuseTexture == null)
                    continue;
                if (terrainLayer.normalMapTexture == null) {
                    Debug.LogWarning($"All terrain layers should have a normal map, but '{terrainLayer.name}' layer doesn't.");
                    showNMWarning = true;
                    continue;
                }

                textures.Add(terrainLayer.diffuseTexture);
                normals.Add(terrainLayer.normalMapTexture);

                diggerSystem.Material.SetFloat($"_tiles{i}x", 1.0f / terrainLayer.tileSize.x);
                diggerSystem.Material.SetFloat($"_tiles{i}y", 1.0f / terrainLayer.tileSize.y);
                diggerSystem.Material.SetFloat($"_offset{i}x", terrainLayer.tileOffset.x);
                diggerSystem.Material.SetFloat($"_offset{i}y", terrainLayer.tileOffset.y);
                diggerSystem.Material.SetFloat($"_normalScale{i}", terrainLayer.normalScale);
            }

            diggerSystem.TerrainTextures = textures.ToArray();

            if (use2DArrayTextures) {
                SetupDiggerMeshMaterial(diggerSystem, forceRefresh, textures, normals);
            } else {
                SetupDiggerMeshNoTextureArrayMaterial(diggerSystem, textures, normals);
            }

            var matPath = Path.Combine(diggerSystem.BasePathData, "meshMaterial.mat");
            diggerSystem.Material = EditorUtils.CreateOrReplaceAsset(diggerSystem.Material, matPath);
            AssetDatabase.ImportAsset(matPath, ImportAssetOptions.ForceUpdate);

            if (textures.Count == 0) {
                if (!EditorUtility.DisplayDialog("Warning!",
                                                 "Digger did not find any terrain layer with both a texture and a normal " +
                                                 "map. Digger meshes will be rendered completely white.\n\nPlease add some layers " +
                                                 "to your terrain and make sure they contain both a texture and a normal map. " +
                                                 "Once this is done, click on 'Sync & Refresh' button of DiggerMaster.",
                                                 "Ok", "What is a terrain layer?")) {
                    Application.OpenURL("https://docs.unity3d.com/Manual/class-TerrainLayer.html");
                }
            } else if (showNMWarning) {
                if (!EditorUtility.DisplayDialog("Warning!",
                                                 "Some terrain layers don't have a normal map. " +
                                                 "Digger will ignore them and won't be able to render them.\n\n" +
                                                 "It is recommended to set a normal map to all terrain layers.",
                                                 "Ok", "What is a terrain layer?")) {
                    Application.OpenURL("https://docs.unity3d.com/Manual/class-TerrainLayer.html");
                }
            }
        }

        private static void SetupDiggerMeshMaterial(DiggerSystem diggerSystem, bool forceRefresh, List<Texture2D> textures, List<Texture2D> normals)
        {
            if (!forceRefresh &&
                diggerSystem.Material.GetTexture(SplatArrayProperty) &&
                diggerSystem.Material.GetTexture(NormalArrayProperty))
                return;

            var texture2DArray = TextureArrayManager.GetCreateTexture2DArray(textures, false);
            var normal2DArray = TextureArrayManager.GetCreateTexture2DArray(normals, true);

            if (texture2DArray == null || normal2DArray == null)
                return;

            // Set the texture to material
            diggerSystem.Material.SetTexture(SplatArrayProperty, texture2DArray);
            diggerSystem.Material.SetTexture(NormalArrayProperty, normal2DArray);
        }

        private static void SetupDiggerMeshNoTextureArrayMaterial(DiggerSystem diggerSystem, List<Texture2D> textures, List<Texture2D> normals)
        {
            for (var i = 0; i < textures.Count; ++i) {
                diggerSystem.Material.SetTexture(SplatPrefixProperty + i, textures[i]);
            }

            for (var i = 0; i < normals.Count; ++i) {
                diggerSystem.Material.SetTexture(NormalPrefixProperty + i, normals[i]);
            }
        }

        private static void SetupCTSMaterial(DiggerSystem diggerSystem)
        {
            if (!diggerSystem.Terrain.materialTemplate) {
                Debug.LogError("Could not setup CTS material for Digger because terrain.materialTemplate is null.");
                return;
            }

            if (diggerSystem.Terrain.materialTemplate.shader.name.StartsWith("CTS/CTS Terrain Shader Basic")) {
                SetupCTSBasicMaterial(diggerSystem);
            } else if (diggerSystem.Terrain.materialTemplate.shader.name.StartsWith("CTS/CTS Terrain Shader Advanced Tess")) {
                SetupCTSAdvancedTessMaterial(diggerSystem);
            } else if (diggerSystem.Terrain.materialTemplate.shader.name.StartsWith("CTS/CTS Terrain Shader Advanced")) {
                SetupCTSAdvancedMaterial(diggerSystem);
            } else {
                Debug.LogError($"Could not setup CTS material for Digger because terrain shader was not a known CTS shader. Was {diggerSystem.Terrain.materialTemplate.shader.name}");
            }
        }

        private static void SetupCTSBasicMaterial(DiggerSystem diggerSystem)
        {
            if (!diggerSystem.Material || diggerSystem.Material.shader.name != "CTS/CTS Terrain Shader Basic Mesh") {
                diggerSystem.Material = new Material(Shader.Find("CTS/CTS Terrain Shader Basic Mesh"));
            }

            if (!diggerSystem.Terrain.materialTemplate || !diggerSystem.Terrain.materialTemplate.shader.name.StartsWith("CTS/CTS Terrain Shader Basic")) {
                Debug.LogWarning($"Looks like terrain material doesn\'t match cave meshes material. " +
                                 $"Expected \'CTS/CTS Terrain Shader Basic CutOut\', was {diggerSystem.Terrain.materialTemplate.shader.name}. " +
                                 $"Please fix this by assigning the right material to the terrain.");
                return;
            }

            diggerSystem.Material.CopyPropertiesFromMaterial(diggerSystem.Terrain.materialTemplate);

            var matPath = Path.Combine(diggerSystem.BasePathData, "meshMaterial.mat");
            diggerSystem.Material = EditorUtils.CreateOrReplaceAsset(diggerSystem.Material, matPath);
            AssetDatabase.ImportAsset(matPath, ImportAssetOptions.ForceUpdate);
        }

        private static void SetupCTSAdvancedMaterial(DiggerSystem diggerSystem)
        {
            if (!diggerSystem.Material || diggerSystem.Material.shader.name != "CTS/CTS Terrain Shader Advanced Mesh") {
                diggerSystem.Material = new Material(Shader.Find("CTS/CTS Terrain Shader Advanced Mesh"));
            }

            if (!diggerSystem.Terrain.materialTemplate || !diggerSystem.Terrain.materialTemplate.shader.name.StartsWith("CTS/CTS Terrain Shader Advanced")) {
                Debug.LogWarning($"Looks like terrain material doesn\'t match cave meshes material. " +
                                 $"Expected \'CTS/CTS Terrain Shader Advanced CutOut\', was {diggerSystem.Terrain.materialTemplate.shader.name}. " +
                                 $"Please fix this by assigning the right material to the terrain.");
                return;
            }

            diggerSystem.Material.CopyPropertiesFromMaterial(diggerSystem.Terrain.materialTemplate);

            var matPath = Path.Combine(diggerSystem.BasePathData, "meshMaterial.mat");
            diggerSystem.Material = EditorUtils.CreateOrReplaceAsset(diggerSystem.Material, matPath);
            AssetDatabase.ImportAsset(matPath, ImportAssetOptions.ForceUpdate);
        }

        private static void SetupCTSAdvancedTessMaterial(DiggerSystem diggerSystem)
        {
            if (!diggerSystem.Material || diggerSystem.Material.shader.name != "CTS/CTS Terrain Shader Advanced Tess Mesh") {
                diggerSystem.Material = new Material(Shader.Find("CTS/CTS Terrain Shader Advanced Tess Mesh"));
            }

            if (!diggerSystem.Terrain.materialTemplate || !diggerSystem.Terrain.materialTemplate.shader.name.StartsWith("CTS/CTS Terrain Shader Advanced Tess")) {
                Debug.LogWarning($"Looks like terrain material doesn\'t match cave meshes material. " +
                                 $"Expected \'CTS/CTS Terrain Shader Advanced Tess CutOut\', was {diggerSystem.Terrain.materialTemplate.shader.name}. " +
                                 $"Please fix this by assigning the right material to the terrain.");
                return;
            }

            diggerSystem.Material.CopyPropertiesFromMaterial(diggerSystem.Terrain.materialTemplate);

            var matPath = Path.Combine(diggerSystem.BasePathData, "meshMaterial.mat");
            diggerSystem.Material = EditorUtils.CreateOrReplaceAsset(diggerSystem.Material, matPath);
            AssetDatabase.ImportAsset(matPath, ImportAssetOptions.ForceUpdate);
        }

        private static void SetupMicroSplatMaterial(DiggerSystem diggerSystem, Material m)
        {
#if (__MICROSPLAT__ && __MICROSPLAT_MESH__)
            if (!m || !m.shader.name.StartsWith("MicroSplat")) {
                Debug.LogWarning($"Looks like terrain material doesn\'t match MicroSplat. " +
                                 $"Expected \'MicroSplat*\', was {m.shader.name}. " +
                                 $"Please fix this by assigning the right material to the terrain.");
                return;
            }

            var microSplatTerrain = diggerSystem.Terrain.GetComponent<MicroSplatTerrain>();
            if (!microSplatTerrain) {
                Debug.LogError($"Could not find MicroSplatTerrain on terrain {diggerSystem.Terrain.name}");
                return;
            }

            var templateMaterial = microSplatTerrain.templateMaterial;

            var microSplatMeshShader = MicroSplatShaderManager.GetCreateMicroSplatShader(templateMaterial);

            var materialsPath = Path.Combine(diggerSystem.BasePathData, "MicroSplatMaterials");
            if (!Directory.Exists(materialsPath)) {
                AssetDatabase.CreateFolder(diggerSystem.BasePathData, "MicroSplatMaterials");
            }

            diggerSystem.Material = MicroSplatMaterialManager.CreateMicroSplatMaterial(
                materialsPath,
                "microSplatMesh.mat",
                templateMaterial,
                microSplatMeshShader);


            var tData = diggerSystem.Terrain.terrainData;
            var textures = new List<Texture2D>();
            for (var i = 0; i < tData.terrainLayers.Length && i < DiggerSystem.MaxTextureCountSupported; i++) {
                var terrainLayer = tData.terrainLayers[i];
                if (terrainLayer == null || terrainLayer.diffuseTexture == null)
                    continue;

                textures.Add(terrainLayer.diffuseTexture);
            }

            diggerSystem.TerrainTextures = textures.ToArray();
#endif
        }
    }
}