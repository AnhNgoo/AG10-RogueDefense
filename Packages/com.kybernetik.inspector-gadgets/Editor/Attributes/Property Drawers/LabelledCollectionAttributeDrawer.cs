// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using InspectorGadgets.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace InspectorGadgets.Editor.PropertyDrawers
{
    /// <summary>[Editor-Only] [Pro-Only] A custom drawer for fields with a <see cref="LabelledCollectionAttribute"/>.</summary>
    [CustomPropertyDrawer(typeof(LabelledCollectionAttribute))]
    public sealed class LabelledCollectionAttributeDrawer : ObjectDrawer
    {
        /************************************************************************************************************************/

        private readonly Dictionary<string, int>
            PropertyIndices = new Dictionary<string, int>();

        /************************************************************************************************************************/

        public override void OnGUI(Rect area, SerializedProperty property, GUIContent label)
        {
            if (!string.IsNullOrEmpty(label.text) ||
                !string.IsNullOrEmpty(label.tooltip))
            {
                var path = property.propertyPath;

                if (!PropertyIndices.TryGetValue(path, out var index))
                {
                    index = path.LastIndexOf('[');
                    if (index >= 0)
                    {
                        index++;
                        var close = path.IndexOf(']', index);
                        if (index >= 0)
                        {
                            var textIndex = path.Substring(index, close - index);
                            if (!int.TryParse(textIndex, out index))
                                index = -1;
                        }
                    }

                    PropertyIndices.Add(path, index);
                }

                if (index >= 0)
                {
                    var attribute = (LabelledCollectionAttribute)this.attribute;
                    Initialize(attribute, fieldInfo, property);

                    var name = attribute.GetLabel(index);

                    if (!string.IsNullOrEmpty(name))
                        label = new GUIContent(name, $"{label.text}: {name}");
                }
            }

            base.OnGUI(area, property, label);
        }

        /************************************************************************************************************************/

        private static readonly Type[] MethodParameterTypes = { typeof(int) };

        private static void Initialize(
            LabelledCollectionAttribute attribute,
            FieldInfo attributedField,
            SerializedProperty property)
        {
            if (attribute._MemberName == null)
                return;

            var memberName = attribute._MemberName;
            attribute._MemberName = null;

            var accessor = property.GetAccessor();
            if (accessor != null)
            {
                var type = accessor.GetField(property).FieldType;
                if (type.IsArray)// Array Field.
                {
                    attribute._GetLabel = index =>
                    {
                        var array = accessor.GetValue(property.serializedObject.targetObject) as Array;
                        if (array != null && index >= 0 && index < array.Length)
                        {
                            var value = array.GetValue(index);
                            return value?.ToString();
                        }
                        else
                        {
                            return index.ToString();
                        }
                    };
                }
                else if (typeof(IList).IsAssignableFrom(type))// List Field.
                {
                    attribute._GetLabel = index =>
                    {
                        var list = accessor.GetValue(property.serializedObject.targetObject) as IList;
                        if (list != null && index >= 0 && index < list.Count)
                        {
                            var value = list[index];
                            return value?.ToString();
                        }
                        else
                        {
                            return index.ToString();
                        }
                    };
                }

                return;
            }

            var method = attributedField.DeclaringType.GetMethod(
                memberName,
                IGEditorUtils.AnyAccessBindings,
                null,
                MethodParameterTypes,
                null);
            if (method != null)// Named Method returns labels.
            {
                if (method.ReturnType != typeof(void))
                {
                    accessor = accessor.Parent;
                    var parameters = new object[1];

                    attribute._GetLabel = index =>
                    {
                        parameters[0] = index;
                        var name = method.Invoke(accessor.GetValue(property.serializedObject.targetObject), parameters);
                        return name?.ToString();
                    };
                }

                return;
            }
        }

        /************************************************************************************************************************/
    }
}

#endif

