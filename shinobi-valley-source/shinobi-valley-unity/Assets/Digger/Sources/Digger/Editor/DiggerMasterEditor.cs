using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Digger
{
    [CustomEditor(typeof(DiggerMaster))]
    public class DiggerMasterEditor : Editor
    {
        private DiggerMaster master;
        private DiggerSystem[] diggerSystems;

        private bool clicking;

        private GameObject reticleSphere;
        private GameObject reticleHalfSphere;
        private GameObject reticleCube;
        private GameObject reticleCone;
        private GameObject reticleCylinder;

        private BrushType brush {
            get => (BrushType) EditorPrefs.GetInt("diggerMaster_brush", 0);
            set => EditorPrefs.SetInt("diggerMaster_brush", (int) value);
        }

        private ActionType action {
            get => (ActionType) EditorPrefs.GetInt("diggerMaster_action", 0);
            set => EditorPrefs.SetInt("diggerMaster_action", (int) value);
        }

        private float opacity {
            get => EditorPrefs.GetFloat("diggerMaster_opacity", 0.3f);
            set => EditorPrefs.SetFloat("diggerMaster_opacity", value);
        }

        private float size {
            get => EditorPrefs.GetFloat("diggerMaster_size", 3f);
            set => EditorPrefs.SetFloat("diggerMaster_size", value);
        }

        private float depth {
            get => EditorPrefs.GetFloat("diggerMaster_depth", 0f);
            set => EditorPrefs.SetFloat("diggerMaster_depth", value);
        }

        private float coneHeight {
            get => EditorPrefs.GetFloat("diggerMaster_coneHeight", 6f);
            set => EditorPrefs.SetFloat("diggerMaster_coneHeight", value);
        }

        private bool upsideDown {
            get => EditorPrefs.GetBool("diggerMaster_upsideDown", false);
            set => EditorPrefs.SetBool("diggerMaster_upsideDown", value);
        }

        private int textureIndex {
            get => EditorPrefs.GetInt("diggerMaster_textureIndex", 0);
            set => EditorPrefs.SetInt("diggerMaster_textureIndex", value);
        }

        private GameObject ReticleSphere {
            get {
                if (!reticleSphere) {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Digger/Misc/SphereReticle.prefab");
                    reticleSphere = Instantiate(prefab);
                    reticleSphere.hideFlags = HideFlags.HideAndDontSave;
                }

                return reticleSphere;
            }
        }

        private GameObject ReticleCube {
            get {
                if (!reticleCube) {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Digger/Misc/CubeReticle.prefab");
                    reticleCube = Instantiate(prefab);
                    reticleCube.hideFlags = HideFlags.HideAndDontSave;
                }

                return reticleCube;
            }
        }

        private GameObject ReticleHalfSphere {
            get {
                if (!reticleHalfSphere) {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Digger/Misc/HalfSphereReticle.prefab");
                    reticleHalfSphere = Instantiate(prefab);
                    reticleHalfSphere.hideFlags = HideFlags.HideAndDontSave;
                }

                return reticleHalfSphere;
            }
        }

        private GameObject ReticleCone {
            get {
                if (!reticleCone) {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Digger/Misc/ConeReticle.prefab");
                    reticleCone = Instantiate(prefab);
                    reticleCone.hideFlags = HideFlags.HideAndDontSave;
                }

                return reticleCone;
            }
        }

        private GameObject ReticleCylinder {
            get {
                if (!reticleCylinder) {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Digger/Misc/CylinderReticle.prefab");
                    reticleCylinder = Instantiate(prefab);
                    reticleCylinder.hideFlags = HideFlags.HideAndDontSave;
                }

                return reticleCylinder;
            }
        }

        private GameObject Reticle {
            get {
                if (action == ActionType.Reset) {
                    if (reticleSphere)
                        DestroyImmediate(reticleSphere);
                    if (reticleCube)
                        DestroyImmediate(reticleCube);
                    if (reticleHalfSphere)
                        DestroyImmediate(reticleHalfSphere);
                    if (reticleCone)
                        DestroyImmediate(reticleCone);
                    return ReticleCylinder;
                }

                switch (brush) {
                    case BrushType.HalfSphere:
                        if (reticleSphere)
                            DestroyImmediate(reticleSphere);
                        if (reticleCube)
                            DestroyImmediate(reticleCube);
                        if (reticleCylinder)
                            DestroyImmediate(reticleCylinder);
                        if (reticleCone)
                            DestroyImmediate(reticleCone);
                        return ReticleHalfSphere;
                    case BrushType.RoundedCube:
                        if (reticleSphere)
                            DestroyImmediate(reticleSphere);
                        if (reticleHalfSphere)
                            DestroyImmediate(reticleHalfSphere);
                        if (reticleCylinder)
                            DestroyImmediate(reticleCylinder);
                        if (reticleCone)
                            DestroyImmediate(reticleCone);
                        return ReticleCube;
                    case BrushType.Stalagmite:
                        if (reticleSphere)
                            DestroyImmediate(reticleSphere);
                        if (reticleHalfSphere)
                            DestroyImmediate(reticleHalfSphere);
                        if (reticleCylinder)
                            DestroyImmediate(reticleCylinder);
                        if (reticleCube)
                            DestroyImmediate(reticleCube);
                        return ReticleCone;
                    case BrushType.Sphere:
                    default:
                        if (reticleHalfSphere)
                            DestroyImmediate(reticleHalfSphere);
                        if (reticleCube)
                            DestroyImmediate(reticleCube);
                        if (reticleCylinder)
                            DestroyImmediate(reticleCylinder);
                        if (reticleCone)
                            DestroyImmediate(reticleCone);
                        return ReticleSphere;
                }
            }
        }

        public void OnEnable()
        {
            master = (DiggerMaster) target;
            diggerSystems = FindObjectsOfType<DiggerSystem>();
            foreach (var diggerSystem in diggerSystems) {
                DiggerSystemEditor.Init(diggerSystem, false);
            }

            SceneView.onSceneGUIDelegate -= OnScene;
            SceneView.onSceneGUIDelegate += OnScene;
            Undo.undoRedoPerformed -= UndoCallback;
            Undo.undoRedoPerformed += UndoCallback;
        }

        public void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoCallback;
            SceneView.onSceneGUIDelegate -= OnScene;
            if (reticleSphere)
                DestroyImmediate(reticleSphere);
            if (reticleHalfSphere)
                DestroyImmediate(reticleHalfSphere);
            if (reticleCube)
                DestroyImmediate(reticleCube);
            if (reticleCone)
                DestroyImmediate(reticleCone);
            if (reticleCylinder)
                DestroyImmediate(reticleCylinder);
        }

        private static void UndoCallback()
        {
            var diggers = FindObjectsOfType<DiggerSystem>();
            foreach (var digger in diggers) {
                digger.DoUndo();
            }
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Global Settings", EditorStyles.boldLabel);
            master.SceneDataFolder = EditorGUILayout.TextField("Scene data folder", master.SceneDataFolder);
            EditorGUILayout.HelpBox($"Digger data for this scene can be found in {master.SceneDataPath}", MessageType.Info);
            EditorGUILayout.HelpBox("Don\'t forget to backup this folder as well when you backup your project.", MessageType.Warning);
            EditorGUILayout.Space();

            var newResolutionMult = EditorGUILayout.IntPopup("Resolution", master.ResolutionMult, new[] {"x1", "x2", "x4", "x8"}, new[] {1, 2, 4, 8});
            if (newResolutionMult != master.ResolutionMult && EditorUtility.DisplayDialog("Change resolution & clear everything",
                                                                                          "All modifications must be cleared for new resolution to take effect.\n\n" +
                                                                                          "THIS WILL CLEAR ALL MODIFICATIONS MADE WITH DIGGER.\n" +
                                                                                          "This operation CANNOT BE UNDONE.\n\n" +
                                                                                          "Are you sure you want to proceed?", "Yes, clear it", "Cancel")) {
                master.ResolutionMult = newResolutionMult;
                DoClear();
            }

            EditorGUILayout.HelpBox("You will have to clear all modifications for new resolution to take effect.\n\n" +
                                    "If your heightmaps have a low resolution, you might want to set this to x2, x4 or x8 to generate " +
                                    "meshes with higher resolution and finer details. " +
                                    "However, keep in mind that the higher the resolution is, the more performance will be impacted.", MessageType.Warning);
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("LOD Settings", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Screen Relative Transition Height of LODs:");
            master.ScreenRelativeTransitionHeightLod0 = EditorGUILayout.Slider("    LOD 0", master.ScreenRelativeTransitionHeightLod0, 0f, 1f);
            master.ScreenRelativeTransitionHeightLod1 = EditorGUILayout.Slider("    LOD 1", master.ScreenRelativeTransitionHeightLod1, 0f, master.ScreenRelativeTransitionHeightLod0);
            master.ColliderLodIndex = EditorGUILayout.IntSlider(
                new GUIContent("Collider LOD", "LOD that will hold the collider. Increasing it will produce mesh colliders with fewer vertices but also less accuracy."),
                master.ColliderLodIndex, 0, 2);

            var diggerSystem = FindObjectOfType<DiggerSystem>();
            if (diggerSystem) {
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Editing", EditorStyles.boldLabel);

                action = (ActionType) EditorGUILayout.EnumPopup("Action", action);

                if (action != ActionType.Reset) {
                    brush = (BrushType) EditorGUILayout.EnumPopup("Brush", brush);
                }

                size = EditorGUILayout.Slider("Brush Size", size, 0.5f, 20f);

                if (brush == BrushType.Stalagmite) {
                    coneHeight = EditorGUILayout.Slider("Stalagmite Height", coneHeight, 1f, 10f);
                    upsideDown = EditorGUILayout.Toggle("Upside Down", upsideDown);
                }

                if (action != ActionType.Reset) {
                    opacity = EditorGUILayout.Slider("Opacity", opacity, 0f, 1f);
                    depth = EditorGUILayout.Slider("Depth", depth, -size, size);


                    GUIStyle gridList = "GridList";
                    var errorMessage = new GUIContent("No texture to display.\n\n" +
                                                      "You have to add some layers to the terrain with " +
                                                      "BOTH a texture and a normal map. Then, click on 'Sync & Refresh'.");
                    textureIndex = EditorUtils.AspectSelectionGrid(textureIndex, diggerSystem.TerrainTextures, 64, gridList, errorMessage);

                    if (diggerSystem.Terrain.terrainData.terrainLayers.Length > DiggerSystem.MaxTextureCountSupported) {
                        EditorGUILayout.HelpBox("Digger shader supports a maximum of 8 textures. " +
                                                "Consequently, only the first 8 terrain layers can be used.", MessageType.Warning);
                    }
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Utils", EditorStyles.boldLabel);

            var doClear = GUILayout.Button("Clear") && EditorUtility.DisplayDialog("Clear",
                                                                                   "This will clear all modifications made with Digger.\n" +
                                                                                   "This operation CANNOT BE UNDONE.\n\n" +
                                                                                   "Are you sure you want to proceed?", "Yes, clear it", "Cancel");
            if (doClear) {
                DoClear();
            }

            var doReload = GUILayout.Button("Sync & Refresh") && EditorUtility.DisplayDialog("Sync & Refresh",
                                                                                             "This will recompute all modifications made with Digger. " +
                                                                                             "This operation is not destructive, but can be long.\n\n" +
                                                                                             "Are you sure you want to proceed?",
                                                                                             "Yes, go ahead", "Cancel");
            if (doReload) {
                var diggers = FindObjectsOfType<DiggerSystem>();
                foreach (var digger in diggers) {
                    DiggerSystemEditor.Init(digger, true);
                }

                GUIUtility.ExitGUI();
            }
        }

        private static void DoClear()
        {
            var diggers = FindObjectsOfType<DiggerSystem>();
            foreach (var digger in diggers) {
                digger.Clear();
                DiggerSystemEditor.Init(digger, true);
            }

            GUIUtility.ExitGUI();
        }

        private void OnScene(SceneView sceneview)
        {
            var controlId = GUIUtility.GetControlID(FocusType.Passive);
            var e = Event.current;
            if (e.type == EventType.Layout || e.type == EventType.Repaint) {
                HandleUtility.AddDefaultControl(controlId);
                return;
            }

            if (!clicking && !e.alt && e.type == EventType.MouseDown && e.button == 0) {
                clicking = true;
            } else if (clicking && (e.type == EventType.MouseUp || e.type == EventType.MouseLeaveWindow || e.isKey || e.alt)) {
                clicking = false;
                foreach (var diggerSystem in diggerSystems) {
                    diggerSystem.PersistAndRecordUndo();
                }
            }

            var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            var hit = GetIntersectionWithTerrain(ray);

            if (hit.HasValue) {
                var p = hit.Value.point + depth * ray.direction.normalized;
                Reticle.transform.position = p;
                Reticle.transform.localScale = 1.9f * Vector3.one * size;
                Reticle.transform.rotation = Quaternion.identity;
                if (action == ActionType.Reset) {
                    Reticle.transform.localScale += 1000f * Vector3.up;
                } else if (brush == BrushType.Stalagmite) {
                    Reticle.transform.localScale = new Vector3(2f * size, 1f * coneHeight, 2f * size);
                    if (upsideDown) {
                        Reticle.transform.rotation = Quaternion.AngleAxis(180f, Vector3.right);
                    }
                }

                if (clicking) {
                    foreach (var diggerSystem in diggerSystems) {
                        diggerSystem.Modify(brush, action, opacity, p, size, coneHeight, upsideDown, textureIndex);
                    }
                }

                HandleUtility.Repaint();
            }
        }

        private static RaycastHit? GetIntersectionWithTerrain(Ray ray)
        {
            if (DiggerPhysics.Raycast(ray, out var hit, 1000f)) {
                return hit;
            }

            return null;
        }

        [MenuItem("Tools/Digger/Setup terrains")]
        public static void SetupTerrains()
        {
            if (!FindObjectOfType<DiggerMaster>()) {
                var goMaster = new GameObject("Digger Master");
                goMaster.transform.localPosition = Vector3.zero;
                goMaster.transform.localRotation = Quaternion.identity;
                goMaster.transform.localScale = Vector3.one;
                var master = goMaster.AddComponent<DiggerMaster>();
                master.CreateDirs();
            }

            var isCTS = false;
            var lightmapStaticWarn = false;
            var terrains = FindObjectsOfType<Terrain>();
            foreach (var terrain in terrains) {
                if (!terrain.gameObject.GetComponentInChildren<DiggerSystem>()) {
                    var go = new GameObject("Digger");
                    go.transform.parent = terrain.transform;
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localRotation = Quaternion.identity;
                    go.transform.localScale = Vector3.one;
                    var digger = go.AddComponent<DiggerSystem>();
                    DiggerSystemEditor.Init(digger, true);
                    isCTS = isCTS || digger.MaterialType == TerrainMaterialType.CTS;
                    lightmapStaticWarn = lightmapStaticWarn || GameObjectUtility.GetStaticEditorFlags(terrain.gameObject).HasFlag(StaticEditorFlags.ContributeGI);
                }
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            if (lightmapStaticWarn) {
                if (!EditorUtility.DisplayDialog("Warning - Lightmapping", "It is recommended to disable lightmapping on terrains " +
                                                                           "when using Digger. Otherwise there might be a visual difference between " +
                                                                           "Digger meshes and the terrains.\n\n" +
                                                                           "To disable lightmapping on a terrain, go to terrain settings and disable " +
                                                                           "'Lightmap Static' toggle.", "Ok", "Terrain settings?")) {
                    Application.OpenURL("https://docs.unity3d.com/Manual/terrain-OtherSettings.html");
                }
            }

            if (isCTS) {
                EditorUtility.DisplayDialog("Warning - CTS", "Digger has detected CTS on your terrain(s) and has been setup accordingly.\n\n" +
                                                             "You may have to close the scene and open it again (or restart Unity) to " +
                                                             "force it to refresh before using Digger.", "Ok");
            }
        }

        [MenuItem("Tools/Digger/Remove Digger from the scene")]
        public static void RemoveDiggerFromTerrains()
        {
            var confirm = EditorUtility.DisplayDialog("Remove Digger from the scene",
                                                      "You are about to completely remove Digger from the scene and clear all related Digger data.\n\n" +
                                                      "This operation CANNOT BE UNDONE.\n\n" +
                                                      "Are you sure you want to proceed?", "Yes, remove Digger", "Cancel");
            if (!confirm)
                return;

            var terrains = FindObjectsOfType<Terrain>();
            foreach (var terrain in terrains) {
                var digger = terrain.gameObject.GetComponentInChildren<DiggerSystem>();
                if (digger) {
                    digger.Clear();
                    DestroyImmediate(digger.gameObject);
                }
            }

            var diggerMaster = FindObjectOfType<DiggerMaster>();
            if (diggerMaster) {
                DestroyImmediate(diggerMaster.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        public static void LoadAllChunks()
        {
            var diggers = FindObjectsOfType<DiggerSystem>();
            foreach (var digger in diggers) {
                digger.ReloadVersion();
                digger.Reload(true, true);
                Undo.ClearUndo(digger);
            }
        }
    }
}