// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

// The only way to currently determine the name and serialized fields of a missing script is via a custom inspector
// for MonoBehaviour scripts (but not other classes that inherit from it) which needs each object to be selected in
// turn. If anyone knows of a better way to do it, please email mail@kybernetik.com.au.

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InspectorGadgets.Editor
{
    /// <summary>[Editor-Only]
    /// A utility for tracking down and fixing missing script references.
    /// </summary>
    internal sealed class MissingScriptWindow : EditorWindow, IHasCustomMenu
    {
        /************************************************************************************************************************/
        #region Fields and Properties
        /************************************************************************************************************************/

        public static MissingScriptWindow Instance { get; private set; }

        public bool AutoDestroy { get; private set; }

        // Gathered Assets.
        [NonSerialized] private SceneSetup[] _OriginalSceneSetup;
        [NonSerialized] private readonly List<string> MissingAssetPaths = new List<string>();
        [NonSerialized] private readonly List<GameObject> MissingPrefabs = new List<GameObject>();
        [NonSerialized] private readonly List<string> ScenePaths = new List<string>();
        [NonSerialized] private readonly List<GameObject> SceneObjects = new List<GameObject>();
        [NonSerialized] private int _SceneObjectIndex;

        // Selected Target Details.
        [NonSerialized] private Object _Target;
        [NonSerialized] private SerializedProperty _ScriptProperty;
        [NonSerialized] private bool _HasSerializedProperties;
        [NonSerialized] private int[] _SimilarityRatings;

        [NonSerialized] private Vector2 _ScrollPosition;
        [NonSerialized] private bool _HasLoadedAnotherScene;
        [NonSerialized] private bool _ConfirmResetScene;

        /************************************************************************************************************************/

        public bool HasTarget => _ScriptProperty != null;

        /************************************************************************************************************************/

        private static MonoScript[] _AllScripts;

        public static MonoScript[] AllScripts
        {
            get
            {
                if (_AllScripts == null)
                    _AllScripts = MonoImporter.GetAllRuntimeMonoScripts();

                return _AllScripts;
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region EditorWindow Methods
        /************************************************************************************************************************/

        private void OnEnable()
        {
            titleContent = new GUIContent("Missing Script Window");
            Instance = this;
            Selection.selectionChanged += OnSelectionChanged;
        }

        /************************************************************************************************************************/

        private void OnGUI()
        {
            const float MinWidth = 300;
            var fieldWidth = position.width < MinWidth ?
                position.width * 0.5f :
                MinWidth * 0.5f + Mathf.Pow(position.width - MinWidth, 0.65f);
            EditorGUIUtility.labelWidth = position.width - fieldWidth;

            if (_OriginalSceneSetup == null)
                GatherScenesAndMissingAsssets();

            if (EditorUtility.scriptCompilationFailed)
                EditorGUILayout.HelpBox("The project currently has compile errors which should be fixed" +
                    " before trying to fix missing script references.", MessageType.Warning);

            if (DoSelectNextButtonGUI())
                return;

            if (_ScriptProperty == null)
            {
                DoGatheredAssetsGUI();
                return;
            }

            _ScrollPosition = GUILayout.BeginScrollView(_ScrollPosition);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_ScriptProperty, true);
            if (EditorGUI.EndChangeCheck())
            {
                var script = _ScriptProperty.objectReferenceValue as MonoScript;
                if (script != null && script.GetClass() != null)
                    _ScriptProperty.serializedObject.ApplyModifiedProperties();
                else
                    _ScriptProperty.serializedObject.Update();
            }

            if (!(_Target is null) && GUILayout.Button("Destroy Component"))
                DestroyTargetAndSelectNext();

            DoMissingScriptButtons();

            GUILayout.EndScrollView();
        }

        /************************************************************************************************************************/

        private void Update()
        {
            if (AutoDestroy)
            {
                if (_Target is null)
                {
                    AutoDestroy = false;
                    return;
                }

                DestroyTargetAndSelectNext();
            }
        }

        /************************************************************************************************************************/

        private void OnDestroy()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            ResetSceneSetup();
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Context Menu
        /************************************************************************************************************************/

        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Destroy All Missing Scripts"), false, DestroyAllMissingScripts);
        }

        /************************************************************************************************************************/

        private void DestroyAllMissingScripts()
        {
            const string Message = "Are you sure you want to destroy all missing scripts" +
                " in scenes and assets everywhere in your project?" +
                "\n\nYou cannot undo this action.";
            if (!EditorUtility.DisplayDialog("Destroy All Missing Scripts?", Message, "Destroy", "Cancel"))
                return;

            AutoDestroy = true;
            DeleteAllMissingAssets();
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/

        private void OnSelectionChanged()
        {
            if (_HasLoadedAnotherScene)
                _ConfirmResetScene = true;

            Repaint();
        }

        /************************************************************************************************************************/

        private void GatherScenesAndMissingAsssets()
        {
            _OriginalSceneSetup = EditorSceneManager.GetSceneManagerSetup();
            _HasLoadedAnotherScene = false;
            _ConfirmResetScene = false;

            MissingAssetPaths.Clear();
            MissingPrefabs.Clear();
            ScenePaths.Clear();

            var guids = AssetDatabase.FindAssets($"t:{nameof(Object)}");
            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (asset == null)
                {
                    MissingAssetPaths.Add(path);
                }
            }

            guids = AssetDatabase.FindAssets($"t:{nameof(GameObject)}");
            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (HasAnyMissingComponents(prefab))
                {
                    MissingPrefabs.Add(prefab);
                }
            }

            guids = AssetDatabase.FindAssets($"t:{nameof(SceneAsset)}");
            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                ScenePaths.Add(path);
            }
        }

        /************************************************************************************************************************/

        private void DoGatheredAssetsGUI()
        {
            _ScrollPosition = GUILayout.BeginScrollView(_ScrollPosition);

            DoMissingAssetsGUI();

            GUILayout.Label("Prefabs with missing scripts: " + MissingPrefabs.Count, EditorStyles.boldLabel);
            for (int i = 0; i < MissingPrefabs.Count; i++)
            {
                var prefab = MissingPrefabs[i];
                EditorGUILayout.ObjectField(AssetDatabase.GetAssetPath(prefab), prefab, typeof(GameObject), true);
            }

            GUILayout.Label("Scenes: " + ScenePaths.Count, EditorStyles.boldLabel);
            GUILayout.Label("We won't know if they contain any missing scripts until we open them.", EditorStyles.wordWrappedLabel);
            for (int i = 0; i < ScenePaths.Count; i++)
            {
                var scene = ScenePaths[i];
                EditorGUILayout.ObjectField(scene, AssetDatabase.LoadAssetAtPath<SceneAsset>(scene), typeof(SceneAsset), true);
            }

            GUILayout.EndScrollView();

            GUILayout.Label(
                "Assets are removed from these lists as they are selected." +
                "\nTo begin again, simply click this button:",
                EditorStyles.wordWrappedLabel);

            if (GUILayout.Button("Re-Gather Assets"))
            {
                GatherScenesAndMissingAsssets();
            }
        }

        /************************************************************************************************************************/

        private void DoMissingAssetsGUI()
        {
            GUILayout.Label("Assets with missing scripts: " + MissingAssetPaths.Count, EditorStyles.boldLabel);
            if (MissingAssetPaths.Count == 0)
                return;

            GUILayout.BeginHorizontal();

            GUILayout.Label("These assets can't be automatically selected" +
                " because Unity doesn't give us any way to reference them.", EditorStyles.wordWrappedLabel);

            if (GUILayout.Button("Delete All", EditorStyles.miniButton, Editor.IGEditorUtils.DontExpandWidth))
            {
                const string Message = "Are you sure you want to delete all assets with missing scripts?" +
                    "\n\nYou cannot undo this action.";

                if (EditorUtility.DisplayDialog("Delete all assets with missing scripts?", Message, "Delete", "Cancel"))
                {
                    DeleteAllMissingAssets();
                    GUIUtility.ExitGUI();
                }
            }

            GUILayout.EndHorizontal();

            for (int i = 0; i < MissingAssetPaths.Count; i++)
            {
                GUILayout.BeginHorizontal();

                var path = MissingAssetPaths[i];
                GUILayout.Label(MissingAssetPaths[i], GUI.skin.textArea);
                var area = GUILayoutUtility.GetLastRect();
                var currentEvent = Event.current;
                if (currentEvent.type == EventType.MouseUp &&
                    area.Contains(currentEvent.mousePosition))
                {
                    path = Path.GetDirectoryName(path);
                    var asset = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
                    SelectObject(asset);
                }

                if (GUILayout.Button("Delete", EditorStyles.miniButton, Editor.IGEditorUtils.DontExpandWidth))
                {
                    var message = path + "\n\nYou cannot undo this action.";

                    if (EditorUtility.DisplayDialog("Delete asset?", message, "Delete", "Cancel"))
                    {
                        AssetDatabase.DeleteAsset(path);
                        MissingAssetPaths.RemoveAt(i);
                        GUIUtility.ExitGUI();
                    }
                }

                GUILayout.EndHorizontal();
            }
        }

        /************************************************************************************************************************/

        private void DeleteAllMissingAssets()
        {
            for (int i = 0; i < MissingAssetPaths.Count; i++)
            {
                AssetDatabase.DeleteAsset(MissingAssetPaths[i]);
            }
            MissingAssetPaths.Clear();
        }

        /************************************************************************************************************************/

        private void DoMissingScriptButtons()
        {
            if (_SimilarityRatings == null)
                return;

            var scripts = AllScripts;
            for (int i = scripts.Length - 1; i >= 0; i--)
            {
                var similarity = _SimilarityRatings[i];
                if (similarity <= 0)
                {
                    if (i == scripts.Length - 1)
                    {
                        GUILayout.Label(_HasSerializedProperties
                            ? "No similar scripts found."
                            : "Unable to determine the missing script's name or find any serialized properties to compare other scripts against.",
                            EditorStyles.wordWrappedLabel);
                    }

                    continue;
                }

                var script = scripts[i];
                var prefix = similarity == int.MaxValue ?
                    "[Name Match] " :
                    $"[{similarity}] ";

                if (GUILayout.Button(prefix + script.name))
                {
                    _ScriptProperty.objectReferenceValue = script;
                    _ScriptProperty.serializedObject.ApplyModifiedProperties();
                    SelectNextMissingScript();
                    return;
                }
            }
        }

        /************************************************************************************************************************/

        private void SelectNextMissingScript()
        {
            _ScriptProperty = null;

            // Next Prefab.

            for (int i = MissingPrefabs.Count - 1; i >= 0; i--)
            {
                var prefab = MissingPrefabs[i];
                MissingPrefabs.RemoveAt(i);
                if (HasAnyMissingComponents(prefab))
                {
                    SelectObject(prefab);
                    return;
                }
            }

            NextScene:

            if (_SceneObjectIndex >= SceneObjects.Count)
            {
                if (ScenePaths.Count > 0)
                {
                    if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        return;

                    var scene = ScenePaths[ScenePaths.Count - 1];
                    ScenePaths.RemoveAt(ScenePaths.Count - 1);
                    EditorSceneManager.OpenScene(scene);
                    _HasLoadedAnotherScene = true;

                    GatherSceneObjects();
                    _SceneObjectIndex = -1;

                    // Continue on to pick a scene object.
                }
                else
                {
                    // No scenes left, so we're done.
                    ResetSceneSetup();
                    return;
                }
            }

            NextSceneObject:

            _SceneObjectIndex++;
            if (_SceneObjectIndex < SceneObjects.Count)
            {
                var sceneObject = SceneObjects[_SceneObjectIndex];
                if (HasAnyMissingComponents(sceneObject))
                {
                    SelectObject(sceneObject);
                    return;
                }
                else
                {
                    goto NextSceneObject;
                }
            }
            else
            {
                SceneObjects.Clear();
                goto NextScene;
            }
        }

        private void GatherSceneObjects()
        {
            SceneObjects.Clear();

            foreach (var gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if ((gameObject.hideFlags & HideFlags.HideAndDontSave) != 0)
                    continue;

                if (EditorUtility.IsPersistent(gameObject.transform.root.gameObject))
                    continue;

                SceneObjects.Add(gameObject);
            }
        }

        /************************************************************************************************************************/

        private void DestroyTargetAndSelectNext()
        {
            MissingScriptEditor.DestroyProperly(_Target, true);
            ClearTarget();
            SelectNextMissingScript();
        }

        /************************************************************************************************************************/

        private static void SelectObject(Object target)
        {
            Selection.activeObject = null;
            EditorApplication.delayCall += () =>
                Selection.activeObject = target;
        }

        /************************************************************************************************************************/

        public void SetScripts(Object target, SerializedProperty scriptProperty, bool hasSerializedProperties, int[] similarityRatings)
        {
            _Target = target;

            _ScriptProperty = scriptProperty;
            _HasSerializedProperties = hasSerializedProperties;
            _SimilarityRatings = similarityRatings;
            Array.Sort(similarityRatings, AllScripts);
            GetWindow<MissingScriptWindow>();
        }

        /************************************************************************************************************************/

        public void ClearTarget()
        {
            _Target = null;
            _ScriptProperty = null;
            _SimilarityRatings = null;
        }

        /************************************************************************************************************************/

        private bool DoSelectNextButtonGUI()
        {
            if (MissingPrefabs.Count > 0 || ScenePaths.Count > 0 || _SceneObjectIndex < SceneObjects.Count)
            {
                if (GUILayout.Button("Select Next Missing Script"))
                {
                    SelectNextMissingScript();
                    return true;
                }
            }
            else
            {
                if (GUILayout.Button("Close"))
                {
                    Close();
                    return true;
                }
            }

            return false;
        }

        /************************************************************************************************************************/

        private static readonly List<MonoBehaviour> Components = new List<MonoBehaviour>();

        private static bool HasAnyMissingComponents(GameObject gameObject)
        {
            gameObject.GetComponents(Components);
            for (int i = 0; i < Components.Count; i++)
            {
                if (Components[i] == null)
                    return true;
            }

            return false;
        }

        /************************************************************************************************************************/

        private void ResetSceneSetup()
        {
            _HasLoadedAnotherScene = false;

            // If the current scene setup has changed, ask if the user wants to return to what they started with.
            var sceneSetup = EditorSceneManager.GetSceneManagerSetup();
            if (sceneSetup.Length != _OriginalSceneSetup.Length)
                goto ReturnToOriginalSetup;

            for (int i = 0; i < sceneSetup.Length; i++)
            {
                if (sceneSetup[i].path != _OriginalSceneSetup[i].path)
                    goto ReturnToOriginalSetup;
            }

            return;

            ReturnToOriginalSetup:

            if (_ConfirmResetScene && !EditorUtility.DisplayDialog("Restore Original Scene Setup",
                "Would you like to return to the scene you had open when you opened the Scripty Hunter?", "Restore", "Do Nothing"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.RestoreSceneManagerSetup(_OriginalSceneSetup);
        }

        /************************************************************************************************************************/
    }
}

#endif
