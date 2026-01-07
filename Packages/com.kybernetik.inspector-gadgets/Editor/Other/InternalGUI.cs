// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace InspectorGadgets.Editor
{
    internal static class InternalGUI
    {
        /************************************************************************************************************************/

        public static readonly float
            NameLabelWidth;

        public static readonly GUIStyle
            FieldLabelStyle,
            FloatFieldStyle,
            MiniSquareButtonStyle,
            SmallButtonStyle,
            UniformScaleButtonStyle,
            ModeButtonStyle,
            ModeLabelStyle;

        /************************************************************************************************************************/

        static InternalGUI()
        {
            FieldLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(0, 0, 2, 2),
            };

            NameLabelWidth = FieldLabelStyle.CalculateWidth("Rotation") + EditorGUIUtility.standardVerticalSpacing;

            FloatFieldStyle = new GUIStyle(EditorStyles.numberField);

            MiniSquareButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                margin = new RectOffset(0, 0, 2, 0),
                padding = new RectOffset(3, 2, 2, 3),
                alignment = TextAnchor.MiddleCenter,
                fixedWidth = EditorGUIUtility.singleLineHeight - 1,
                fixedHeight = EditorGUIUtility.singleLineHeight,
            };

            SmallButtonStyle = new GUIStyle(MiniSquareButtonStyle)
            {
                padding = new RectOffset(3, 3, 2, 2),
                fixedWidth = 0,
            };

            UniformScaleButtonStyle = new GUIStyle(MiniSquareButtonStyle)
            {
                margin = new RectOffset(2, 0, 2, 0),
                padding = new RectOffset(1, -1, -3, 0),
                fontSize = (int)EditorGUIUtility.singleLineHeight,
            };

            ModeLabelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
            };

            ModeButtonStyle = new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(),
            };
        }

        /************************************************************************************************************************/

        public static readonly AutoPrefs.EditorVector4 SceneLabelBackgroundColor = new AutoPrefs.EditorVector4(
            EditorStrings.PrefsKeyPrefix + nameof(SceneLabelBackgroundColor), new Vector4(0.15f, 0.15f, 0.15f, 0.5f),
            onValueChanged: (value) => _SceneLabelBackgroundColorChanged = true);

        private static bool _SceneLabelBackgroundColorChanged;

        private static Texture2D _SceneLabelBackground;
        public static Texture2D SceneLabelBackground
        {
            get
            {
                if (SceneLabelBackgroundColor.Value.w <= 0)
                    return null;

                if (_SceneLabelBackground == null)
                {
                    _SceneLabelBackground = new Texture2D(1, 1);
                    _SceneLabelBackgroundColorChanged = true;

                    AssemblyReloadEvents.beforeAssemblyReload +=
                        () => Object.DestroyImmediate(_SceneLabelBackground);
                }

                if (_SceneLabelBackgroundColorChanged)
                {
                    _SceneLabelBackgroundColorChanged = false;
                    _SceneLabelBackground.SetPixel(0, 0, SceneLabelBackgroundColor);
                    _SceneLabelBackground.Apply();
                }

                return _SceneLabelBackground;
            }
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Draws a <see cref="EditorGUI.FloatField(Rect, GUIContent, float)"/> with an alternate string when it is not
        /// selected (for example, "1" might become "1s" to indicate "seconds").
        /// </summary>
        /// <remarks>
        /// This method treats most <see cref="EventType"/>s normally, but for <see cref="EventType.Repaint"/> it
        /// instead draws a text field with the converted string.
        /// </remarks>
        public static float DoSpecialFloatField(
            Rect area,
            GUIContent label,
            float value,
            CompactUnitConversionCache toString,
            GUIStyle style)
        {
            if (label != null && !string.IsNullOrEmpty(label.text))
            {
                if (Event.current.type != EventType.Repaint)
                    return EditorGUI.FloatField(area, label, value, style);

                var dragArea = new Rect(area.x, area.y, EditorGUIUtility.labelWidth, area.height);
                EditorGUIUtility.AddCursorRect(dragArea, MouseCursor.SlideArrow);

                var text = toString.Convert(value, area.width - EditorGUIUtility.labelWidth);
                EditorGUI.TextField(area, label, text, style);
            }
            else
            {
                var indentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;

                if (Event.current.type != EventType.Repaint)
                    value = EditorGUI.FloatField(area, value, style);
                else
                    EditorGUI.TextField(area, toString.Convert(value, area.width), style);

                EditorGUI.indentLevel = indentLevel;
            }

            return value;
        }

        /************************************************************************************************************************/
    }
}

#endif
