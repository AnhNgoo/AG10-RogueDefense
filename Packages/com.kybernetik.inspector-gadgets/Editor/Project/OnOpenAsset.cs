// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InspectorGadgets.Editor
{
    internal static class OnOpenAsset
    {
        /************************************************************************************************************************/

        [OnOpenAsset]
        private static bool HandleOpenEvent(int instanceID, int line)
        {
            if (Event.current == null)
                return false;

            var asset = EditorUtility.InstanceIDToObject(instanceID);

            if (asset is GameObject prefab)
                return OnOpenPrefab(prefab);

            return OnUnhandledAsset(asset);
        }

        /************************************************************************************************************************/

        private static bool OnOpenPrefab(GameObject prefab)
        {
            var current = Event.current;

            if (!current.shift && current.alt)// Alt to Instantiate.
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Undo.RegisterCreatedObjectUndo(instance, "Alt Instantiate");
                instance.transform.SetAsLastSibling();
                Selection.activeObject = instance;

                if (current.control)// + Ctrl to frame the instance in the scene view.
                {
                    if (SceneView.lastActiveSceneView != null)
                    {
                        SceneView.lastActiveSceneView.FrameSelected();
                    }
                }

                return true;
            }

            return false;
        }

        /************************************************************************************************************************/

        private static bool OnUnhandledAsset(Object asset)
        {
            var current = Event.current;

            if (current.control && current.shift && current.alt)// Ctrl + Shift + Alt to open in explorer.
            {
                var assetPath = AssetDatabase.GetAssetPath(asset);
                EditorUtility.RevealInFinder(assetPath);
                return true;
            }

            return false;
        }

        /************************************************************************************************************************/
    }
}

#endif
