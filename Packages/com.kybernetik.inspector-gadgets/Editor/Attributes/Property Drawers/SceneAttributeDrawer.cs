// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using InspectorGadgets.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace InspectorGadgets.Editor.PropertyDrawers
{
    /// <summary>[Editor-Only] [Pro-Only] A custom drawer for fields with a <see cref="SceneAttribute"/>.</summary>
    [CustomPropertyDrawer(typeof(SceneAttribute))]
    public sealed class SceneAttributeDrawer : PropertyDrawer
    {
        /************************************************************************************************************************/

        private static bool _IsListeningForSceneListChange;
        private static EditorBuildSettingsScene[] _AllScenes;
        private static readonly List<string> ActiveScenes = new List<string>();
        private static readonly GUIContent ButtonContent = new GUIContent();

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void OnGUI(Rect area, SerializedProperty property, GUIContent label)
        {
            GatherScenes();

            area = EditorGUI.IndentedRect(area);

            var width = area.width;

            var labelWidth = EditorGUIUtility.labelWidth + 20;

            area.xMax = labelWidth;
            GUI.Label(area, label);

            area.width = width;
            area.xMin = labelWidth;

            string buttonLabel;
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:

                    var value = property.intValue;

                    if (value >= 0 && value < ActiveScenes.Count)
                        buttonLabel = ActiveScenes[value];
                    else
                        buttonLabel = "None";

                    ButtonContent.text = value + ": " + buttonLabel;
                    ButtonContent.tooltip = buttonLabel;

                    if (DoButtonGUI(area))
                        ShowIndexPopup(buttonLabel, property);

                    break;

                case SerializedPropertyType.String:

                    buttonLabel = property.stringValue;
                    ButtonContent.text = buttonLabel;
                    ButtonContent.tooltip = null;

                    if (DoButtonGUI(area))
                        ShowPathPopup(buttonLabel, property);

                    break;

                default:
                    GUI.Label(area, "Invalid [Scene] attribute");
                    break;
            }
        }

        /************************************************************************************************************************/

        private void GatherScenes()
        {
            if (_AllScenes != null)
                return;

            if (!_IsListeningForSceneListChange)
            {
                _IsListeningForSceneListChange = true;

                EditorBuildSettings.sceneListChanged += () =>
                {
                    _AllScenes = null;
                    ActiveScenes.Clear();
                };
            }

            _AllScenes = EditorBuildSettings.scenes;

            for (int i = 0; i < _AllScenes.Length; i++)
            {
                var scene = _AllScenes[i];
                if (scene.enabled)
                    ActiveScenes.Add(scene.path);
            }
        }

        /************************************************************************************************************************/

        private bool DoButtonGUI(Rect area)
        {
            return GUI.Button(area, ButtonContent, EditorStyles.popup);
        }

        /************************************************************************************************************************/

        private void ShowIndexPopup(string selectedLabel, SerializedProperty property)
        {
            var menu = new GenericMenu();

            AddMenuItem(menu, "None", selectedLabel, () => property.intValue = -1, property);

            var sceneIndex = 0;
            for (int i = 0; i < _AllScenes.Length; i++)
            {
                var scene = _AllScenes[i];
                if (scene.enabled)
                {
                    var currentSceneIndex = sceneIndex;
                    AddMenuItem(menu, currentSceneIndex + ": " + scene.path.AllBackslashes(), selectedLabel,
                        () => property.intValue = currentSceneIndex, property);
                    sceneIndex++;
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent(scene.path.AllBackslashes()));
                }
            }

            menu.ShowAsContext();
        }

        /************************************************************************************************************************/

        private void ShowPathPopup(string selectedLabel, SerializedProperty property)
        {
            var menu = new GenericMenu();

            AddMenuItem(menu, "None", selectedLabel, () => property.stringValue = "", property);

            var sceneIndex = 0;
            for (int i = 0; i < _AllScenes.Length; i++)
            {
                var scene = _AllScenes[i];
                var name = Path.GetFileNameWithoutExtension(scene.path);

                if (scene.enabled)
                {
                    AddMenuItem(menu, name, selectedLabel, () => property.stringValue = name, property);
                    sceneIndex++;
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent(name));
                }
            }

            menu.ShowAsContext();
        }

        /************************************************************************************************************************/

        private static void AddMenuItem(GenericMenu menu, string label, string selectedLabel, Action method, SerializedProperty property)
        {
            menu.AddItem(new GUIContent(label), label == selectedLabel, () =>
            {
                method();
                property.serializedObject.ApplyModifiedProperties();
            });
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        /************************************************************************************************************************/
    }
}

#endif

