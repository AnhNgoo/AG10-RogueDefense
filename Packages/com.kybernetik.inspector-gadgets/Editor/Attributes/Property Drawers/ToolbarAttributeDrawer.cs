// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using InspectorGadgets.Attributes;
using System;
using UnityEditor;
using UnityEngine;

namespace InspectorGadgets.Editor.PropertyDrawers
{
    /// <summary>[Editor-Only] [Pro-Only] A custom drawer for fields with a <see cref="ToolbarAttribute"/>.</summary>
    [CustomPropertyDrawer(typeof(ToolbarAttribute))]
    public sealed class ToolbarAttributeDrawer : PropertyDrawer
    {
        /************************************************************************************************************************/

        private static readonly GUIStyle
            LeftButtonStyle,
            MidButtonStyle,
            RightButtonStyle;

        static ToolbarAttributeDrawer()
        {
            LeftButtonStyle = new GUIStyle(EditorStyles.miniButtonLeft);
            MidButtonStyle = new GUIStyle(EditorStyles.miniButtonMid);
            RightButtonStyle = new GUIStyle(EditorStyles.miniButtonRight);

            LeftButtonStyle.padding = MidButtonStyle.padding = RightButtonStyle.padding = new RectOffset(4, 4, 2, 2);
        }

        /************************************************************************************************************************/

        private static readonly int
            DragControlHint = "ToolbarDragControl".GetHashCode();

        private ToolbarAttribute _Attribute;
        private Type _FieldType;
        private bool _IsFlags;
        private int _Count;
        private GUIContent[] _Labels;
        private long[] _Values;
        private float[] _Widths;
        private float _TotalWidth;
        private float _FieldWidth;
        private float _Scrollarea;

        /************************************************************************************************************************/

        private void ClampScrollarea()
        {
            _Scrollarea = Mathf.Clamp(_Scrollarea, 0, _TotalWidth - _FieldWidth);
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void OnGUI(Rect area, SerializedProperty property, GUIContent label)
        {
            var currentEvent = Event.current;
            if (currentEvent.type == EventType.Layout)
                return;

            Initialize(property, label);

            if (_Labels == null || _Labels.Length == 0)
            {
                EditorGUI.PropertyField(area, property, label);
                return;
            }

            DoToolbar(area, property, label);

            // Handle scroll events if you are too wide.
            if (currentEvent.type == EventType.ScrollWheel &&
                _TotalWidth > _FieldWidth &&
                area.Contains(currentEvent.mousePosition))
            {
                _Scrollarea += (currentEvent.delta.y + currentEvent.delta.x) * 10;
                ClampScrollarea();
                currentEvent.Use();
            }
        }

        /************************************************************************************************************************/

        private void Initialize(SerializedProperty property, GUIContent label)
        {
            // Already initialized.
            if (_Labels != null)
                return;

            DetermineFieldType();

            _Attribute = attribute as ToolbarAttribute;

            if (_FieldType == typeof(bool))// Bool.
            {
                _Labels = _Attribute.Labels;

                if (_Labels == null || _Labels.Length != 2)
                {
                    if (_Labels == null)
                    {
                        _Labels = new GUIContent[]
                        {
                            new GUIContent("False"),
                            new GUIContent("True"),
                        };
                    }
                    else if (_Labels.Length <= 1)
                    {
                        Debug.LogWarning(GetImproperAttributeWarningPrefix() +
                            " must specify exactly two labels to be used on a bool field.");

                        _Labels = Array.Empty<GUIContent>();
                        return;
                    }
                    else
                    {
                        Array.Resize(ref _Labels, 2);
                    }
                }

                _Count = 2;
                return;
            }
            else if (_FieldType == typeof(string))// String.
            {
                _Labels = _Attribute.Labels;

                if (_Labels == null)
                {
                    Debug.LogWarning(GetImproperAttributeWarningPrefix() +
                        " must specify the labels you with to display.");

                    _Labels = Array.Empty<GUIContent>();
                    return;
                }

                _Count = _Labels.Length;
            }
            else if (_FieldType.IsEnum)// Enum.
            {
                if (_Attribute.Labels != null)
                    Debug.LogWarning(GetImproperAttributeWarningPrefix() +
                        " shound not specify any labels since it is an enum field.");

                _IsFlags = _FieldType.IsDefined(typeof(FlagsAttribute), true);

                var enumValues = Enum.GetValues(_FieldType);

                var displayNames = property.enumDisplayNames;
                var trueNames = property.enumNames;

                _Labels = new GUIContent[enumValues.Length];
                _Values = new long[enumValues.Length];

                _Count = 0;

                for (int i = 0; i < enumValues.Length; i++)
                {
                    var value = Convert.ToInt64(enumValues.GetValue(i));

                    // Skip zero values in flags enums.
                    if (_IsFlags && value == 0)
                        continue;

                    var tooltip = trueNames[i];

                    var members = _FieldType.GetMember(tooltip);
                    for (int iMember = 0; iMember < members.Length; iMember++)
                    {
                        var member = members[iMember];
                        var tooltipAttribute = member.GetCustomAttribute<TooltipAttribute>(true);
                        if (tooltipAttribute != null)
                        {
                            tooltip = tooltipAttribute.tooltip;
                            break;
                        }
                    }

                    _Labels[_Count] = new GUIContent(displayNames[i], tooltip);

                    _Values[_Count] = value;

                    _Count++;
                }
            }
            else// Invalid field type.
            {
                _Labels = Array.Empty<GUIContent>();
                Debug.LogWarning(GetImproperAttributeWarningPrefix() +
                    " will do nothing because it is only valid on bool, string, and enum fields.");
                return;
            }

            // Initialize the button widths.
            _Widths = new float[_Count];

            _TotalWidth = 0;

            for (int i = 0; i < _Count; i++)
            {
                var width = MidButtonStyle.CalcSize(_Labels[i]).x;
                _Widths[i] = width;
                _TotalWidth += width;
            }
        }

        /************************************************************************************************************************/

        private string GetImproperAttributeWarningPrefix()
            => $"The [Toolbar] attribute applied to {fieldInfo.DeclaringType.FullName}.{fieldInfo.Name}";

        /************************************************************************************************************************/

        private void DetermineFieldType()
        {
            _FieldType = fieldInfo.FieldType;

            while (_FieldType.IsGenericType)
            {
                var genericArguments = _FieldType.GetGenericArguments();
                if (genericArguments.Length == 1)
                    _FieldType = genericArguments[0];
                else
                    break;
            }

            while (_FieldType.IsArray)
            {
                _FieldType = _FieldType.GetElementType();
            }
        }

        /************************************************************************************************************************/

        private void DoToolbar(Rect area, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(area, label, property);

            _FieldWidth = area.width - EditorGUIUtility.labelWidth;

            var right = area.xMax;

            area.width = EditorGUIUtility.labelWidth;

            // Drag.

            var controlID = GUIUtility.GetControlID(DragControlHint, FocusType.Passive, area);

            float spareSpace;
            if (_TotalWidth > _FieldWidth)
            {
                DragNumberValue(area, controlID, ref _Scrollarea);
                ClampScrollarea();

                _FieldWidth -= 2;
                //right -= 1;
                spareSpace = 0;
            }
            else
            {
                spareSpace = (_FieldWidth - _TotalWidth) / _Count;
            }

            // Label.

            if (_Attribute.Label != null)
                label.text = _Attribute.Label;

            EditorGUI.PrefixLabel(area, label);

            if (_Labels.Length == 0)
                return;

            // Buttons.

            area.x += area.width;
            area.xMax = right;
            GUI.BeginGroup(area);

            area.x = -_Scrollarea;
            area.y = 0;

            if (_FieldType == typeof(bool))
                DoBoolButtons(area, property, spareSpace);
            else if (_FieldType == typeof(string))
                DoStringButtons(area, property, spareSpace);
            if (_FieldType.IsEnum)
                DoEnumButtons(area, property, spareSpace);

            GUI.EndGroup();

            EditorGUI.EndProperty();
        }

        /************************************************************************************************************************/

        private void DoBoolButtons(Rect area, SerializedProperty property, float spareSpace)
        {
            const float ToggleWidth = 16;
            area.width -= ToggleWidth;

            var width = area.width;

            area.width = ToggleWidth;
            var value = property.boolValue;
            if (value != GUI.Toggle(area, value, GUIContent.none))
                property.boolValue = !value;

            area.x += area.width;
            area.width = Mathf.Floor(width * 0.5f);
            if (GUI.Toggle(area, !property.boolValue, _Labels[0], LeftButtonStyle))
                property.boolValue = false;

            area.x += area.width;
            area.width = width - area.width;
            if (GUI.Toggle(area, property.boolValue, _Labels[1], RightButtonStyle))
                property.boolValue = true;
        }

        /************************************************************************************************************************/

        private void DoEnumButtons(Rect area, SerializedProperty property, float spareSpace)
        {
            var right = area.width;

            var propertyValue = property.longValue;

            for (int i = 0; i < _Count; i++)
            {
                var value = _Values[i];
                area.width = _Widths[i] + spareSpace;

                // Make sure the last button goes all the way to the end.
                if (i == _Count - 1 && area.xMax < right)
                    area.xMax = right;

                if (_IsFlags)// Flags Enum.
                {
                    var wasPressed = (property.intValue & value) == value;
                    var isPressed = GUI.Toggle(area, wasPressed, _Labels[i], GetStyle(i));

                    if (isPressed != wasPressed)
                    {
                        if (isPressed)
                            propertyValue |= value;
                        else
                            propertyValue &= ~value;
                    }
                }
                else// Standard Enum.
                {
                    var pressed = propertyValue == value;

                    pressed = GUI.Toggle(area, pressed, _Labels[i], GetStyle(i));

                    if (pressed) propertyValue = value;
                }

                area.x += area.width;
            }

            if (property.longValue != propertyValue)
                property.longValue = propertyValue;
        }

        /************************************************************************************************************************/

        private void DoStringButtons(Rect area, SerializedProperty property, float spareSpace)
        {
            var right = area.width;

            var propertyValue = property.stringValue;

            for (int i = 0; i < _Count; i++)
            {
                var label = _Labels[i];
                area.width = _Widths[i] + spareSpace;

                // Make sure the last button goes all the way to the end.
                if (i == _Count - 1 && area.xMax < right)
                    area.xMax = right;

                var pressed = propertyValue == label.text;

                pressed = GUI.Toggle(area, pressed, label, GetStyle(i));

                if (pressed) propertyValue = label.text;

                area.x += area.width;
            }

            if (property.stringValue != propertyValue)
                property.stringValue = propertyValue;
        }

        /************************************************************************************************************************/

        private static float _DragStartValue;

        private static void DragNumberValue(Rect area, int id, ref float value)
        {
            var current = Event.current;
            switch (current.GetTypeForControl(id))
            {
                case EventType.MouseDown:// Begin.
                    if (current.button == 0 && area.Contains(current.mousePosition))
                    {
                        EditorGUIUtility.editingTextField = false;
                        GUIUtility.hotControl = id;
                        GUIUtility.keyboardControl = id;
                        Undo.IncrementCurrentGroup();
                        current.Use();
                        _DragStartValue = value;
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }
                    break;

                case EventType.MouseUp:// End.
                    if (GUIUtility.hotControl == id)// && EditorGUI.s_DragCandidateState != 0)
                    {
                        GUIUtility.hotControl = 0;
                        current.Use();
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }
                    break;

                case EventType.MouseDrag:// Move.
                    if (GUIUtility.hotControl == id)
                    {
                        value -= HandleUtility.niceMouseDelta;
                        GUI.changed = true;
                        current.Use();
                    }
                    break;

                case EventType.KeyDown:// Cancel.
                    if (GUIUtility.hotControl == id && current.keyCode == KeyCode.Escape)
                    {
                        value = _DragStartValue;
                        GUI.changed = true;
                        GUIUtility.hotControl = 0;
                        current.Use();
                    }
                    break;

                case EventType.Repaint:// Repaint.
                    EditorGUIUtility.AddCursorRect(area, MouseCursor.SlideArrow);
                    break;

                default:
                    break;
            }
        }

        /************************************************************************************************************************/

        private GUIStyle GetStyle(int index)
        {
            if (index <= 0)
                return LeftButtonStyle;

            if (index >= _Count - 1)
                return RightButtonStyle;

            return MidButtonStyle;
        }

        /************************************************************************************************************************/
    }
}

#endif

