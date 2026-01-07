// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InspectorGadgets.Editor
{
    /// <summary>[Editor-Only] [Pro-Only]
    /// Allows you to drag and drop assets onto other assets in the Project window to turn them into sub-assets.
    /// </summary>
    internal static class DragAndDropSubAssets
    {
        /************************************************************************************************************************/

        internal static readonly ModifierKeysPref
            Modifiers = new ModifierKeysPref(
                EditorStrings.PrefsKeyPrefix + nameof(DragAndDropSubAssets) + "." + nameof(Modifiers),
                ModifierKey.Alt);

        /************************************************************************************************************************/

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
        }

        /************************************************************************************************************************/

        private static readonly List<Object> Sources = new List<Object>();

        /// <summary>Called for each asset shown in the Project window.</summary>
        private static void OnProjectWindowItemGUI(string guid, Rect area)
        {
            var currentEvent = Event.current;

            switch (currentEvent.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    break;

                default:// Ignore all other event types.
                    return;
            }

            if (!Modifiers.AreKeysDown(currentEvent) ||
                !area.Contains(currentEvent.mousePosition))
                return;

            var dragging = DragAndDrop.objectReferences;
            var targetPath = AssetDatabase.GUIDToAssetPath(guid);
            var isTargetFolder = AssetDatabase.IsValidFolder(targetPath);

            // Gather all the dropped objects and their sub-assets.
            Sources.Clear();
            for (int i = 0; i < dragging.Length; i++)
            {
                var source = dragging[i];
                Sources.Add(source);

                if (AssetDatabase.IsMainAsset(source))
                {
                    // If the destination is a folder and any of the dragged objects are the main asset, let Unity handle it.
                    if (isTargetFolder)
                        return;

                    var sourcePath = AssetDatabase.GetAssetPath(source);
                    var sourceAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(sourcePath);
                    Sources.AddRange(sourceAssets);
                }
            }

            var targetAsset = AssetDatabase.LoadAssetAtPath<Object>(targetPath);
            for (int i = 0; i < Sources.Count; i++)
            {
                var source = Sources[i];
                if (!ShouldAllowSubAsset(source, targetAsset, targetPath, currentEvent))
                {
                    Sources.Clear();
                    return;
                }
            }

            switch (currentEvent.type)
            {
                case EventType.DragUpdated:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                    currentEvent.Use();
                    break;

                case EventType.DragPerform:
                    Move(dragging, targetPath);
                    DragAndDrop.AcceptDrag();
                    currentEvent.Use();
                    break;
            }

            Sources.Clear();
        }

        /************************************************************************************************************************/

        private static Dictionary<Type, HashSet<Type>> _TypeToAllowedSubAssets;

        /// <summary>Determines if `sub` is allowed as a sub-asset of `main` and asks the user if unsure.</summary>
        private static bool ShouldAllowSubAsset(Object sub, Object main, string mainPath, Event currentEvent)
        {
            // Anything can be moved out to a folder.
            if (main is DefaultAsset &&
                AssetDatabase.IsValidFolder(mainPath))
                return true;

            // Known types that can't be sub-assets.
            if (sub is GameObject ||
                sub is Component
#if UNITY_AUDIO
                || sub is AudioClip
#endif
                )
                return false;

            if (main is GameObject)
            {
#if UNITY_ANIMATION
                // Animator Controllers can't be sub-assets of Prefabs.
                if (sub is RuntimeAnimatorController)
                    return false;
#endif

                // Models can't have sub-assets.
                var importer = AssetImporter.GetAtPath(mainPath);
                if (importer is ModelImporter)
                    return false;

                return true;
            }

            // Known types that can have sub-assets.
            if (main is ScriptableObject ||
                main is Material ||
#if UNITY_ANIMATION
                main is AnimationClip ||
                main is RuntimeAnimatorController ||
                main is AvatarMask ||
                main is Avatar ||
#endif
#if UNITY_PHYSICS_2D
                main is PhysicsMaterial2D ||
#endif
#if UNITY_PHYSICS_3D
#if UNITY_6000_0_OR_NEWER
                main is PhysicsMaterial ||
#else
                main is PhysicMaterial ||
#endif
#endif
                main is ShaderVariantCollection ||
                main is Flare ||
                main is LightmapParameters ||
                main is UnityEngine.U2D.SpriteAtlas ||
                main is Font ||
                main is GUISkin)
                return true;

            // Known types that can't have sub-assets.
            if (main is Texture ||
                main is Sprite ||
                main is TextAsset ||
#if UNITY_AUDIO
                main is AudioClip ||
#endif
                main is ComputeShader)
                return false;

            if (currentEvent.type != EventType.DragPerform)
                return true;

            if (sub is Texture && AssetDatabase.IsMainAsset(sub))
            {
                return EditorUtility.DisplayDialog("Are you sure?",
                    "Textures can be sub-assets, but cannot be extracted back out into separate assets afterwards." +
                    "\n\nThis operation cannot be undone.",
                    "Make Sub-Asset",
                    "Cancel");
            }

            // Unknown types might or might not work. Test on backups first.
            if (_TypeToAllowedSubAssets == null)
                _TypeToAllowedSubAssets = new Dictionary<Type, HashSet<Type>>();

            var mainType = main.GetType();
            var subType = sub.GetType();

            if (!_TypeToAllowedSubAssets.TryGetValue(mainType, out var allowedSubAssets))
                _TypeToAllowedSubAssets.Add(mainType, allowedSubAssets = new HashSet<Type>());

            if (allowedSubAssets.Contains(subType))
                return true;

            if (EditorUtility.DisplayDialog("Are you sure?",
                $"It is unknown whether a '{sub.GetType().FullName}' can be a Sub-Asset of a '{main.GetType().FullName}'." +
                $"\n\nAttempting to do so may irreversibly  corrupt the assets." +
                $"\n\nIf you wish to proceed, it is recommended that you use 'Ctrl + D' to Duplicate both assets" +
                $" so you can test this combination before applying it to your real assets.",
                "Make Sub-Asset",
                "Cancel"))
            {
                allowedSubAssets.Add(subType);
                return true;
            }
            else return false;
        }

        /************************************************************************************************************************/

        /// <summary>Move the `sources` to become sub-assets of the `destination`.</summary>
        private static void Move(Object[] sources, string destination)
        {
            // Dropping a sub-asset onto itself or its root extracts it into the folder.
            if (Array.IndexOf(DragAndDrop.paths, destination) >= 0)
                destination = Path.GetDirectoryName(destination);

            try
            {
                AssetDatabase.StartAssetEditing();

                for (int iSource = 0; iSource < sources.Length; iSource++)
                {
                    var source = sources[iSource];

                    if (AssetDatabase.IsMainAsset(source))
                    {
                        var sourcePath = AssetDatabase.GetAssetPath(source);
                        var sourceAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(sourcePath);

                        Move(source, destination);
                        for (int iAsset = 0; iAsset < sourceAssets.Length; iAsset++)
                            Move(sourceAssets[iAsset], destination);

                        AssetDatabase.DeleteAsset(sourcePath);
                    }
                    else
                    {
                        Move(source, destination);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        /************************************************************************************************************************/

        /// <summary>Move the `source` to become a sub-asset of the `destination`.</summary>
        private static void Move(Object source, string destination)
        {
            var hiddenReferences = GatherHiddenReferences(source);

            AssetDatabase.RemoveObjectFromAsset(source);

            if (AssetDatabase.IsValidFolder(destination))
            {
                destination = $"{destination}/{source.name}.{IGEditorUtils.GetDefaultFileExtension(source.GetType())}";
                destination = AssetDatabase.GenerateUniqueAssetPath(destination);

                AssetDatabase.CreateAsset(source, destination);
            }
            else
            {
                AssetDatabase.AddObjectToAsset(source, destination);
            }

            foreach (var reference in hiddenReferences)
            {
                AssetDatabase.RemoveObjectFromAsset(reference);
                AssetDatabase.AddObjectToAsset(reference, source);
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Recursively gathers any objects referenced by the `asset` which are using
        /// <see cref="HideFlags.HideInHierarchy"/> and are located at the same asset path.
        /// </summary>
        private static HashSet<Object> GatherHiddenReferences(Object asset)
        {
            var references = new HashSet<Object>();
            GatherHiddenReferences(asset, references, AssetDatabase.GetAssetPath(asset));
            return references;
        }

        /// <summary>
        /// Recursively gathers any objects referenced by the `asset` which are using
        /// <see cref="HideFlags.HideInHierarchy"/> and are located at the same asset path.
        /// </summary>
        private static void GatherHiddenReferences(Object asset, HashSet<Object> references, string path)
        {
            var property = new SerializedObject(asset).GetIterator();
            while (property.Next(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference)
                    continue;

                var obj = property.objectReferenceValue;
                if (obj == null ||
                    (obj.hideFlags & HideFlags.HideInHierarchy) == 0 ||
                    references.Contains(obj) ||
                    AssetDatabase.GetAssetPath(obj) != path)
                    continue;

                references.Add(obj);
                GatherHiddenReferences(obj, references, path);
            }
        }

        /************************************************************************************************************************/
    }
}

#endif

