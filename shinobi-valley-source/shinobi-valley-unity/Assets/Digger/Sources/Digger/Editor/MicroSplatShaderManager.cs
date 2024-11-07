using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Digger
{
    public static class MicroSplatShaderManager
    {
        private const string ParentFolder = DiggerMaster.ParentFolder;
        private const string MicroSplatShadersFolder = "MicroSplatShaders";

        private const string MicroSplatShaderAssetLabel = "ms";

        private static string ParentPath => Path.Combine("Assets", ParentFolder);
        private static string MicroSplatShadersPath => Path.Combine(ParentPath, MicroSplatShadersFolder);

        public static Shader GetCreateMicroSplatShader(Material srcMat)
        {
            Utils.Profiler.BeginSample("[Dig] MicroSplatShaderManager.GetCreateMicroSplatShader");
            CreateDirs();
            var shader = FindMicroSplatShader(srcMat);
            if (shader) {
                Debug.Log($"Found existing MicroSplat shader '{shader.name}'. Let's use it.");
                Utils.Profiler.EndSample();
                return shader;
            }

            Debug.Log("Didn't find existing MicroSplat shader. Let's create it.");

            var newShader = CreateMicroSplatShader(srcMat);
            AssetDatabase.SetLabels(newShader, new[] {LabelFor(srcMat)});

            Utils.Profiler.EndSample();
            return newShader;
        }

        public static string[] GetShaderMeshKeywordsFor(Material srcMat)
        {
            var keywords = new HashSet<string>(srcMat.shaderKeywords);
            keywords.Remove("_ALPHAHOLETEXTURE");
            keywords.Add("_MICROMESH");
            keywords.Add("_MESHUV2");
            keywords.Add("_TRIPLANAR");
            return keywords.ToArray();
        }

        private static Shader FindMicroSplatShader(Material srcMat)
        {
            Utils.Profiler.BeginSample("[Dig] MicroSplatShaderManager.FindMicroSplatShader>FindAssets");
            var label = LabelFor(srcMat);
            var guids = AssetDatabase.FindAssets($"l:{label}", new[] {MicroSplatShadersPath});
            Utils.Profiler.EndSample();
            if (guids == null || guids.Length == 0) {
                return null;
            }

            // we loop but there should be only one item in the list
            foreach (var guid in guids) {
                Utils.Profiler.BeginSample("[Dig] MicroSplatShaderManager.FindMicroSplatShader>LoadAssetAtPath");
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(guid));
                Utils.Profiler.EndSample();
                if (shader) {
                    return shader;
                }
            }

            return null;
        }

        private static string LabelFor(Material srcMat)
        {
            return srcMat.shaderKeywords.Aggregate(MicroSplatShaderAssetLabel, (current, keyword) => current + $"_{keyword}");
        }

        private static void CreateDirs()
        {
            Utils.Profiler.BeginSample("[Dig] MicroSplatShaderManager.CreateDirs");
            if (!Directory.Exists(ParentPath)) {
                AssetDatabase.CreateFolder("Assets", ParentFolder);
            }

            if (!Directory.Exists(MicroSplatShadersPath)) {
                AssetDatabase.CreateFolder(ParentPath, MicroSplatShadersFolder);
            }

            Utils.Profiler.EndSample();
        }

        private static Shader CreateMicroSplatShader(Material srcMat)
        {
#if (__MICROSPLAT__ && __MICROSPLAT_MESH__)
            var compiler = new MicroSplatShaderGUI.MicroSplatCompiler();
            compiler.Init();

            var keywords = GetShaderMeshKeywordsFor(srcMat);

            var newShaderPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(MicroSplatShadersPath, "microSplatMesh.shader"));
            var newBaseShaderPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(MicroSplatShadersPath, "microSplatMesh_Base.shader"));

            var name = GUID.Generate().ToString();
            var baseName = "Hidden/MicroSplat/ForDigger_" + name + "_Base";

            var baseShader = compiler.Compile(keywords, baseName);
            var regularShader = compiler.Compile(keywords, name, baseName);

            File.WriteAllText(newShaderPath, regularShader);
            File.WriteAllText(newBaseShaderPath, baseShader);

            AssetDatabase.Refresh();
            Debug.Log($"Created MicroSplat shader at {newShaderPath}");

            return AssetDatabase.LoadAssetAtPath<Shader>(newShaderPath);
#else
            return null;
#endif
        }
    }
}