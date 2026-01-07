// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using Object = UnityEngine.Object;

namespace InspectorGadgets.Editor
{
    /// <summary>[Pro-Only] [Editor-Only]
    /// A cache for manually created <see cref="UnityEditor.Editor"/>s which handles their cleanup automatically.
    /// </summary>
    internal static class Editors
    {
        /************************************************************************************************************************/

        private static readonly HashSet<UnityEditor.Editor>
            AllEditors = new HashSet<UnityEditor.Editor>();

        /************************************************************************************************************************/

        public static UnityEditor.Editor Create(Object target)
        {
            var editor = UnityEditor.Editor.CreateEditor(target);
            AllEditors.Add(editor);
            return editor;
        }

        public static UnityEditor.Editor Create(Object[] targets)
        {
            var editor = UnityEditor.Editor.CreateEditor(targets);
            AllEditors.Add(editor);
            return editor;
        }

        /************************************************************************************************************************/

        public static void Destroy(UnityEditor.Editor editor)
        {
            if (!(editor is null))
                AllEditors.Remove(editor);

            Object.DestroyImmediate(editor);
        }

        /************************************************************************************************************************/

        static Editors()
        {
            Selection.selectionChanged += DestroyOldEditors;
            AssemblyReloadEvents.beforeAssemblyReload += DestroyOldEditors;
        }

        /************************************************************************************************************************/

        private static void DestroyOldEditors()
        {
            foreach (var editor in AllEditors)
            {
                Object.DestroyImmediate(editor);
            }

            AllEditors.Clear();
        }

        /************************************************************************************************************************/
    }
}

#endif
