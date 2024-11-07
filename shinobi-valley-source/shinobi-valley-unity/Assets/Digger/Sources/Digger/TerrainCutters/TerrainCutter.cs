using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Digger.TerrainCutters
{
    public class TerrainCutter : MonoBehaviour
    {
        [SerializeField] private DiggerSystem digger;
        [SerializeField] private Texture2D transparencyMap;

        private int[][,] detailMaps;
        private static readonly int TransparencyMapProperty = Shader.PropertyToID("_TransparencyMap");


        public static TerrainCutter Create(Terrain terrain, DiggerSystem digger)
        {
            var cutter = digger.gameObject.AddComponent<TerrainCutter>();
            cutter.digger = digger;

#if UNITY_EDITOR
            var transparencyMapPath = digger.GetTransparencyMapPath();
            cutter.transparencyMap = AssetDatabase.LoadAssetAtPath<Texture2D>(transparencyMapPath);
#endif
            if (cutter.transparencyMap == null)
                cutter.transparencyMap = cutter.CreateNewTransparencyMap();

            cutter.Refresh();
            cutter.Apply(true);
            return cutter;
        }

        private Texture2D CreateNewTransparencyMap()
        {
            var terrainData = digger.Terrain.terrainData;
            var texture = new Texture2D(terrainData.alphamapResolution,
                                        terrainData.alphamapResolution);

            // Init all pixels to Color.white
            var colors = new Color[texture.width * texture.height];
            for (var i = 0; i < colors.Length; ++i) {
                colors[i] = Color.white;
            }

            texture.SetPixels(colors);
            texture.Apply();

            if (digger.MaterialType == TerrainMaterialType.MicroSplat) {
                SetMicroSplatClipMap(digger.Terrain, texture);
            }

            return texture;
        }

        public void Refresh()
        {
            if (!transparencyMap)
                transparencyMap = CreateNewTransparencyMap();
            GrabDetailMaps();
        }

        private void GrabDetailMaps()
        {
            var tData = digger.Terrain.terrainData;
            detailMaps = new int[tData.detailPrototypes.Length][,];
            for (var layer = 0; layer < detailMaps.Length; ++layer) {
                detailMaps[layer] = tData.GetDetailLayer(0, 0, tData.detailWidth, tData.detailHeight, layer);
            }
        }

        private void CutDetailMaps(TerrainData tData, int x, int z)
        {
            if (detailMaps == null)
                GrabDetailMaps();

            var width = tData.detailWidth / tData.alphamapWidth;
            var height = tData.detailHeight / tData.alphamapHeight;
            foreach (var detailMap in detailMaps) {
                for (var w = 0; w < width; ++w) {
                    for (var h = 0; h < height; ++h) {
                        if (x + w >= 0 && x + w < tData.detailWidth && z + h >= 0 && z + h < tData.detailHeight) {
                            detailMap[x + w, z + h] = 0;
                        }
                    }
                }
            }
        }

        public void Cut(int x, int z, bool removeDetailsOnly)
        {
            if (!removeDetailsOnly)
                transparencyMap.SetPixel(x, z, Color.clear);

            var tData = digger.Terrain.terrainData;
            var detailMapPos = TerrainUtils.AlphamapPositionToDetailMapPosition(tData, x, z);
            CutDetailMaps(tData, detailMapPos.y, detailMapPos.x);
        }

        public void UnCut(int x, int z)
        {
            transparencyMap.SetPixel(x, z, Color.white);
        }

        public void Apply(bool persist)
        {
#if UNITY_EDITOR
            Utils.Profiler.BeginSample("[Dig] Cutter.Apply");
            transparencyMap.Apply();

            if (detailMaps != null) {
                var tData = digger.Terrain.terrainData;
                for (var layer = 0; layer < detailMaps.Length; ++layer) {
                    tData.SetDetailLayer(0, 0, layer, detailMaps[layer]);
                }
            }

            if (persist)
                Persist();

            switch (digger.MaterialType) {
                case TerrainMaterialType.CTS:
                    SetCTSCutoutMask(digger.Terrain, transparencyMap);
                    break;
                case TerrainMaterialType.MicroSplat:
                    // nothing to do
                    break;
                default:
                    digger.Terrain.materialTemplate.SetTexture(TransparencyMapProperty, transparencyMap);
                    break;
            }

            Utils.Profiler.EndSample();
#endif
        }

        public void LoadFrom(string path)
        {
            if (!transparencyMap)
                transparencyMap = CreateNewTransparencyMap();

            using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                using (var reader = new BinaryReader(stream, Encoding.Default)) {
                    var count = reader.ReadInt32();
                    var raw = reader.ReadBytes(count);
                    transparencyMap.LoadRawTextureData(raw);
                }
            }

            Apply(true);
        }

        public void SaveTo(string path)
        {
            if (!transparencyMap)
                return;

            var raw = transparencyMap.GetRawTextureData();
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write)) {
                using (var writer = new BinaryWriter(stream, Encoding.Default)) {
                    writer.Write(raw.Length);
                    writer.Write(raw);
                }
            }
        }

        public void Clear()
        {
#if UNITY_EDITOR
            Utils.Profiler.BeginSample("[Dig] Cutter.Clear");
            AssetDatabase.DeleteAsset(digger.GetTransparencyMapPath());
            transparencyMap = null;
            Utils.Profiler.EndSample();
#endif
        }

        private void Persist()
        {
#if UNITY_EDITOR
            Utils.Profiler.BeginSample("[Dig] Cutter.Persist");
            var transparencyMapPath = digger.GetTransparencyMapPath();
            transparencyMap = EditorUtils.CreateOrReplaceAsset(transparencyMap, transparencyMapPath);
            Utils.Profiler.EndSample();
#endif
        }


        #region CTS

        private void SetCTSCutoutMask(Terrain terrain, Texture2D cutoutMask)
        {
#if (UNITY_EDITOR && CTS_PRESENT)
            var cts = terrain.GetComponent<CTS.CompleteTerrainShader>();
            if (!cts) {
                Debug.LogError($"Could not find CompleteTerrainShader on terrain {terrain.name}");
                return;
            }

            var wasCutout = cts.UseCutout;
            cts.UseCutout = true;
            cts.CutoutMask = cutoutMask;
            if (!wasCutout) {
                cts.CutoutHeight = -1;
            }

            cts.ApplyMaterialAndUpdateShader();
#endif
        }

        #endregion

        #region MicroSplat

        private void SetMicroSplatClipMap(Terrain terrain, Texture2D clipMap)
        {
#if (UNITY_EDITOR && __MICROSPLAT__ && __MICROSPLAT_MESH__)
            var microSplat = terrain.GetComponent<MicroSplatTerrain>();
            if (!microSplat) {
                Debug.LogError($"Could not find MicroSplatTerrain on terrain {terrain.name}");
                return;
            }

            var keywords = microSplat.templateMaterial.shaderKeywords;
            if (!keywords.Contains("_ALPHAHOLETEXTURE")) {
                EditorUtility.DisplayDialog("MicroSplat setup",
                                            "Digger requires the MicroSplat terrain material to have Alpha Hole feature enabled to 'Clip map' mode.\n" +
                                            "Please enable this feature from the material's inspector.", "Ok");
            }

            if (!keywords.Contains("_TRIPLANAR")) {
                EditorUtility.DisplayDialog("MicroSplat setup",
                                            "Digger gives better results if the MicroSplat terrain material has Triplanar mode enabled.\n" +
                                            "You may enable this feature from the material's inspector.", "Ok");
            }

            microSplat.clipMap = clipMap;
            microSplat.Sync();
#endif
        }

        #endregion
    }
}