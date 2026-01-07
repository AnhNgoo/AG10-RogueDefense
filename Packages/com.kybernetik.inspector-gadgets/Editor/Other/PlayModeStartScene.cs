// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace InspectorGadgets.Editor
{
    /// <summary>[Editor-Only] [Pro-Only]
    /// A utility for making Unity automatically start in a particular scene when entering Play Mode
    /// instead of starting in the scene you currently have open.
    /// </summary>
    internal class PlayModeStartScene : EditorWindow
    {
        /************************************************************************************************************************/
        #region Menu Functions
        /************************************************************************************************************************/

        private const string MenuName = "Play Mode Start Scene";

        private const string SceneContextMenu = "CONTEXT/" + nameof(SceneAsset) + "/" + MenuName + "/";

        /************************************************************************************************************************/

        [MenuItem("Window/General/" + MenuName)]
        [MenuItem(SceneContextMenu + "Open Window")]
        private static void OpenWindow()
            => GetWindow<PlayModeStartScene>();

        /************************************************************************************************************************/

        [MenuItem(SceneContextMenu + "Use This Scene")]
        private static void SetAsPlayModeStartScene(MenuCommand command)
        {
            Scene = (SceneAsset)command.context;
            Enabled.Value = true;
            Debug.Log($"Set {Scene.name} as the {MenuName}");
        }

        [MenuItem(SceneContextMenu + "Clear")]
        private static void ClearPlayModeStartScene()
        {
            Scene = null;
            Debug.Log($"Cleared the {MenuName}");
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Editor Window
        /************************************************************************************************************************/

        protected virtual void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            var scene = EditorGUILayout.ObjectField("Start Scene", Scene, typeof(SceneAsset), false);
            if (EditorGUI.EndChangeCheck())
                Scene = scene as SceneAsset;

            EditorGUI.BeginChangeCheck();
            Enabled.Value = EditorGUILayout.Toggle("Enabled", Enabled);
            if (EditorGUI.EndChangeCheck())
                Initialize();
        }

        /************************************************************************************************************************/

        [InitializeOnLoadMethod]
        [InitializeOnEnterPlayMode]
        private static void Initialize()
        {
            EditorSceneManager.playModeStartScene = Enabled
                ? Scene
                : default;
        }

        /************************************************************************************************************************/

        private static readonly AutoPrefs.EditorString GUID = new(
            EditorStrings.PrefsKeyPrefix + nameof(PlayModeStartScene) + "." + nameof(GUID));

        private static readonly AutoPrefs.EditorBool Enabled = new(
            EditorStrings.PrefsKeyPrefix + nameof(PlayModeStartScene) + "." + nameof(Enabled));

        /************************************************************************************************************************/

        private static SceneAsset Scene
        {
            get
            {
                var path = AssetDatabase.GUIDToAssetPath(GUID);
                return path == null
                    ? null
                    : AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            }
            set
            {
                EditorSceneManager.playModeStartScene = Enabled
                    ? value
                    : default;

                GUID.Value = value != null
                    && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out var guid, out long _)
                    ? guid
                    : null;
            }
        }

        /************************************************************************************************************************/

        static PlayModeStartScene()
        {
            EditorApplication.playModeStateChanged += change =>
            {
                if (change == PlayModeStateChange.ExitingEditMode)
                {
#if UNITY_TEST_FRAMEWORK
                    // Don't use this system while running tests.
                    if (HasOpenInstances<UnityEditor.TestTools.TestRunner.TestRunnerWindow>())
                    {
                        EditorSceneManager.playModeStartScene = null;
                    }
                    else
#endif
                    {
                        // On the first load when opening the editor it might not be ready to load the scene.
                        // So try initializing before entering Play Mode as well.
                        Initialize();
                    }
                }
            };
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

