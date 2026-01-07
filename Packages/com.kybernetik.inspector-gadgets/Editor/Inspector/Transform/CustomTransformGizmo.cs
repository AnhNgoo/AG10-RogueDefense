// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace InspectorGadgets.Editor
{
    /// <summary>[Editor-Only] Settings which control the custom Transform Gizmo.</summary>
    public static class CustomTransformGizmo
    {
        /************************************************************************************************************************/

        private static readonly AutoPrefs.EditorString
            StandardGizmoComponents;

        private static readonly HashSet<string>
            StandardGizmoComponentsSet = new();

        static CustomTransformGizmo()
        {
            StandardGizmoComponents = new(
                EditorStrings.PrefsKeyPrefix + nameof(StandardGizmoComponents),
                "UnityEngine.ProBuilder.ProBuilderMesh," +
                "UnityEngine.Splines.SplineContainer",
                value => ParseTypes());

            ParseTypes();
        }

        /************************************************************************************************************************/

        private static readonly List<Component>
            Components = new();

        /// <summary>Should the gizmo currently be shown?</summary>
        public static bool ShouldShow()
        {
            if (StandardGizmoComponentsSet.Count == 0)
                return true;

            foreach (var selected in Selection.gameObjects)
            {
                Components.Clear();
                selected.GetComponents(Components);

                foreach (var component in Components)
                    if (component != null &&
                        StandardGizmoComponentsSet.Contains(component.GetType().FullName))
                        return false;
            }

            return true;
        }

        /************************************************************************************************************************/

        private static readonly char[] SplitChars = { ' ', ',', ';' };

        private static void ParseTypes()
        {
            StandardGizmoComponentsSet.Clear();

            var types = StandardGizmoComponents.Value;
            var start = 0;

            while (start < types.Length)
            {
                var end = types.IndexOfAny(SplitChars, start);
                if (end < 0)
                    break;

                if (end > start + 1)
                    StandardGizmoComponentsSet.Add(types[start..end]);

                start = end + 1;
            }

            StandardGizmoComponentsSet.Add(types[start..]);
        }

        /************************************************************************************************************************/

        private static readonly GUIContent
            ComponentTypesToDisableGizmoLabel = new("Standard Gizmo Components",
                "A comma separated list containing the full name of any component types" +
                " that should disable the custom gizmo."),
            LogLabel = new("Log",
                "Log the full name of all Component types on the currently selected objects.");

        /// <summary>Draws the GUI for this system's preferences.</summary>
        public static void DoPreferencesGUI()
        {
            GUILayout.BeginHorizontal();

            StandardGizmoComponents.DoGUI(
                ComponentTypesToDisableGizmoLabel,
                GUI.skin.textField,
                (area, label, value, style) =>
                {
                    return EditorGUI.DelayedTextField(area, label, value, style);
                });

            if (GUILayout.Button(LogLabel, EditorStyles.miniButton, IGEditorUtils.DontExpandWidth))
                LogSelectedComponents();

            GUILayout.EndHorizontal();
        }

        /************************************************************************************************************************/

        private static void LogSelectedComponents()
        {
            var types = new HashSet<Type>();

            foreach (var selected in Selection.gameObjects)
            {
                Components.Clear();
                selected.GetComponents(Components);

                foreach (var component in Components)
                    if (component != null)
                        types.Add(component.GetType());
            }

            if (types.Count == 0)
            {
                Debug.Log("No GameObjects are currently selected.");
            }
            else
            {
                var text = new StringBuilder();
                text.Append("The selected GameObjects have Components of the following types: ");

                foreach (var type in types)
                    text.Append(type.ToString()).Append(", ");

                text.Length -= 2;

                Debug.Log(text);
            }
        }

        /************************************************************************************************************************/
    }
}

#endif
