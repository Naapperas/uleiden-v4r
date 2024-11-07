using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Digger
{
    public class DiggerMaster : MonoBehaviour
    {
        public const string ParentFolder = "DiggerData";
        public const string ScenesBaseFolder = "Scenes";

        [SerializeField] private string sceneDataFolder;
        [SerializeField] private float screenRelativeTransitionHeightLod0 = 0.2f;
        [SerializeField] private float screenRelativeTransitionHeightLod1 = 0.1f;
        [SerializeField] private int colliderLodIndex = 0;

        [SerializeField] private int resolutionMult = 1;

        private static string ParentPath => Path.Combine("Assets", ParentFolder);
        private static string ScenesBasePath => Path.Combine(ParentPath, ScenesBaseFolder);
        public string SceneDataPath => Path.Combine(ScenesBasePath, sceneDataFolder);

        public string SceneDataFolder {
            get { return sceneDataFolder; }
            set { sceneDataFolder = value; }
        }

        public float ScreenRelativeTransitionHeightLod0 {
            get { return screenRelativeTransitionHeightLod0; }
            set { screenRelativeTransitionHeightLod0 = value; }
        }

        public float ScreenRelativeTransitionHeightLod1 {
            get { return screenRelativeTransitionHeightLod1; }
            set { screenRelativeTransitionHeightLod1 = value; }
        }

        public int ColliderLodIndex {
            get { return colliderLodIndex; }
            set { colliderLodIndex = value; }
        }

        public int ResolutionMult {
            get { return resolutionMult; }
            set { resolutionMult = value; }
        }

        public void CreateDirs()
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(sceneDataFolder)) {
                sceneDataFolder = SceneManager.GetActiveScene().name;
            }

            if (!Directory.Exists(ParentPath)) {
                AssetDatabase.CreateFolder("Assets", ParentFolder);
            }

            if (!Directory.Exists(ScenesBasePath)) {
                AssetDatabase.CreateFolder(ParentPath, ScenesBaseFolder);
            }

            if (!Directory.Exists(SceneDataPath)) {
                AssetDatabase.CreateFolder(ScenesBasePath, sceneDataFolder);
            }
#endif
        }
    }
}