using System.IO;
using UnityEditor;
using UnityEngine;

namespace Digger
{
    public static class MicroSplatMaterialManager
    {
        private const int ControlSplatCount = 4;
        private const int ControlSplatSize = 256;

        public static Material CreateMicroSplatMaterial(string path, string filename, Material srcMat, Shader shader)
        {
            CreateDirs(path);

            var mat = new Material(shader) {shaderKeywords = MicroSplatShaderManager.GetShaderMeshKeywordsFor(srcMat)};
            AssetDatabase.CreateAsset(mat, Path.Combine(path, filename));
            mat.CopyPropertiesFromMaterial(srcMat);
            mat.shaderKeywords = MicroSplatShaderManager.GetShaderMeshKeywordsFor(srcMat);

            var controlTexturesPath = Path.Combine(path, "controls");
            for (var i = 0; i < ControlSplatCount; ++i) {
                var tex = CreateControlTexture(Path.Combine(controlTexturesPath, $"Control{i}.png"));
                mat.SetTexture("_Control" + i, tex);
            }

            return mat;
        }

        private static Texture2D CreateControlTexture(string path)
        {
            var tex = new Texture2D(ControlSplatSize, ControlSplatSize, TextureFormat.ARGB32, true, true);
            var bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            ti.sRGBTexture = false;
            ti.isReadable = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.filterMode = FilterMode.Bilinear;
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ti.assetPath);
        }

        private static void CreateDirs(string path)
        {
            Utils.Profiler.BeginSample("[Dig] MicroSplatMaterialManager.CreateDirs");
            if (!Directory.Exists(Path.Combine(path, "controls"))) {
                AssetDatabase.CreateFolder(path, "controls");
            }

            Utils.Profiler.EndSample();
        }
    }
}