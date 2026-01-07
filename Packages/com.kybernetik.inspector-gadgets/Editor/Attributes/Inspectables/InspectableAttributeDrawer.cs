// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using InspectorGadgets.Attributes;
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InspectorGadgets.Editor.PropertyDrawers
{
    internal sealed class InspectableAttributeDrawer : BaseInspectableAttributeDrawer<InspectableAttribute>
    {
        /************************************************************************************************************************/

        private FieldInfo _Field;
        private PropertyInfo _Property;
        private Type _MemberType;
        private SerializedPropertyType _PropertyType;
        private Exception _Exception;
        private bool _IsExpanded;
        private Action _OnInspectorGUI;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override string Initialize()
        {
            // Validate the member.
            var field = Member as FieldInfo;
            if (field != null)
            {
                _Field = field;
                _MemberType = field.FieldType;

                if (field.IsLiteral || field.IsInitOnly)
                    Attribute.Readonly = true;
            }
            else
            {
                var property = Member as PropertyInfo;
                if (property != null)
                {
                    if (property.GetGetMethod(true) == null)
                    {
                        return "it must have both a getter and setter";
                    }

                    if (property.GetSetMethod(true) == null)
                        Attribute.Readonly = true;

                    _Property = property;
                    _MemberType = property.PropertyType;
                }
                else
                {
                    return "it is not a field or property";
                }
            }

            if (Attribute.Label == null)
                Attribute.Label = IGUtils.ConvertFieldNameToFriendly(Member.Name, true);

            if (Attribute.Tooltip == null)
                Attribute.Tooltip = "[Inspectable] " + Member.GetNameCS();

            _PropertyType = Serialization.GetPropertyType(_MemberType);

            return null;
        }

        /************************************************************************************************************************/

        private object _Value;

        /// <inheritdoc/>
        public override void OnGUI(Object[] targets)
        {
            var showMixedValue = EditorGUI.showMixedValue;

            if (Event.current.type == EventType.Layout)
            {
                try
                {
                    _Value = GetValue(targets);
                    _Exception = null;
                }
                catch (Exception exception)
                {
                    _Exception = exception;
                }
            }

            GUILayout.BeginVertical();

            var area = PrefixLabel(Attribute.LabelContent);

            if (_Exception != null)
            {
                if (GUI.Button(area, _Exception.GetType().Name, EditorStyles.miniButton))
                {
                    Debug.LogException(_Exception, targets[0]);
                }
            }
            else
            {
                EditorGUI.BeginChangeCheck();

                var guiEnabled = GUI.enabled;
                if (Attribute.Readonly)
                    GUI.enabled = false;

                switch (_PropertyType)
                {
                    // Primitives.

                    case SerializedPropertyType.Boolean: _Value = EditorGUI.Toggle(area, (bool)_Value); break;
                    case SerializedPropertyType.Integer: _Value = EditorGUI.IntField(area, (int)_Value); break;
                    case SerializedPropertyType.Float: _Value = EditorGUI.FloatField(area, (float)_Value); break;
                    case SerializedPropertyType.String: _Value = EditorGUI.TextField(area, (string)_Value); break;

                    // Vectors.

                    case SerializedPropertyType.Vector2: _Value = EditorGUI.Vector2Field(area, GUIContent.none, (Vector2)_Value); break;
                    case SerializedPropertyType.Vector3: _Value = EditorGUI.Vector3Field(area, GUIContent.none, (Vector3)_Value); break;
                    case SerializedPropertyType.Vector4: _Value = EditorGUI.Vector4Field(area, GUIContent.none, (Vector4)_Value); break;

                    case SerializedPropertyType.Quaternion:
                        _Value = Quaternion.Euler(EditorGUI.Vector3Field(area, GUIContent.none, ((Quaternion)_Value).eulerAngles));
                        break;

                    // Other.

                    case SerializedPropertyType.Color: _Value = EditorGUI.ColorField(area, (Color)_Value); break;

                    case SerializedPropertyType.Gradient:
                        break;

                    case SerializedPropertyType.Rect: _Value = EditorGUI.RectField(area, (Rect)_Value); break;
                    case SerializedPropertyType.Bounds: _Value = EditorGUI.BoundsField(area, (Bounds)_Value); break;

                    case SerializedPropertyType.Vector2Int: _Value = EditorGUI.Vector2IntField(area, GUIContent.none, (Vector2Int)_Value); break;
                    case SerializedPropertyType.Vector3Int: _Value = EditorGUI.Vector3IntField(area, GUIContent.none, (Vector3Int)_Value); break;
                    case SerializedPropertyType.RectInt: _Value = EditorGUI.RectIntField(area, (RectInt)_Value); break;
                    case SerializedPropertyType.BoundsInt: _Value = EditorGUI.BoundsIntField(area, (BoundsInt)_Value); break;

                    case SerializedPropertyType.AnimationCurve: _Value = EditorGUI.CurveField(area, (AnimationCurve)_Value); break;

                    // Special.

                    case SerializedPropertyType.ObjectReference: _Value = EditorGUI.ObjectField(area, (Object)_Value, _MemberType, true); break;
                    case SerializedPropertyType.Enum: _Value = EditorGUI.EnumPopup(area, (Enum)_Value); break;

                    default:
                        if (_Value == null)
                        {
                            GUI.Label(area, "Null");
                        }
                        else
                        {
                            if (_OnInspectorGUI == null ||
                                _OnInspectorGUI.Target != _Value)
                                _OnInspectorGUI = MethodCache.OnInspectorGUI.GetDelegate(_Value);

                            if (_OnInspectorGUI != null)
                            {
                                GUI.enabled = true;
                                area = GUILayoutUtility.GetLastRect();
                                _IsExpanded = EditorGUI.Foldout(area, _IsExpanded, GUIContent.none, true);

                                if (_IsExpanded)
                                {
                                    EditorGUI.indentLevel++;
                                    GUI.enabled = Attribute.Readonly;
                                    _OnInspectorGUI();
                                    EditorGUI.indentLevel--;
                                }
                            }
                            else
                            {
                                string label = null;
                                try
                                {
                                    label = _Value.ToString();
                                    GUI.Label(area, label);
                                }
                                catch (Exception exception)
                                {
                                    if (GUI.Button(area, exception.GetType().Name, EditorStyles.miniButton))
                                    {
                                        Debug.LogException(exception, targets[0]);
                                    }
                                }
                            }
                        }
                        break;
                }

                GUI.enabled = guiEnabled;

                if (EditorGUI.EndChangeCheck() && !Attribute.Readonly)
                    SetValue(targets, _Value);
            }

            GUILayout.EndVertical();

            CheckContextMenu(GUILayoutUtility.GetLastRect(), targets);

            if (Attribute.ConstantlyRepaint)
                HandleUtility.Repaint();

            EditorGUI.showMixedValue = showMixedValue;
        }

        /************************************************************************************************************************/

        private object GetValue(Object[] targets)
        {
            if (targets == null)
                return GetSingleValue(null);

            var firstValue = GetSingleValue(targets[0]);

            var i = 1;
            for (; i < targets.Length; i++)
            {
                var value = GetSingleValue(targets[i]);
                if (!Equals(firstValue, value))
                {
                    EditorGUI.showMixedValue = true;
                    break;
                }
            }

            return firstValue;
        }

        private object GetSingleValue(Object target)
        {
            if (_Field != null)
                return _Field.GetValue(target);
            else
                return _Property.GetValue(target, null);
        }

        /************************************************************************************************************************/

        private void SetValue(Object[] targets, object value)
        {
            if (targets == null)
                SetSingleValue(null, value);
            else
                for (int i = 0; i < targets.Length; i++)
                    SetSingleValue(targets[i], value);
        }

        private void SetSingleValue(Object target, object value)
        {
            if (_Field != null)
                _Field.SetValue(target, value);
            else
                _Property.SetValue(target, value, null);
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void PopulateContextMenu(GenericMenu menu, Object[] targets)
        {
            menu.AddItem(new GUIContent("Copy to Clipboard"), false, () =>
            {
                var value = GetValue(targets);
                EditorGUIUtility.systemCopyBuffer = value != null ? value.ToString() : "null";
            });

            menu.AddItem(new GUIContent("Log Value"), false, () =>
            {
                MemberInfo member = _Field;
                if (member == null)
                    member = _Property;

                var value = GetValue(targets);

                Debug.Log(member.GetNameCS() + ": " + value, targets[0]);
            });
        }

        /************************************************************************************************************************/
    }
}

#endif

