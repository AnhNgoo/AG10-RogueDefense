// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using InspectorGadgets.Attributes;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace InspectorGadgets.Editor.PropertyDrawers
{
    /// <summary>[Editor-Only] [Pro-Only] A custom drawer for fields with a <see cref="UniqueCollectionAttribute"/>.</summary>
    [CustomPropertyDrawer(typeof(UniqueCollectionAttribute))]
    public sealed class UniqueCollectionAttributeDrawer : ObjectDrawer
    {
        /************************************************************************************************************************/

        private static readonly Color
            WarningColor = new Color(1, 0.65f, 0.65f);
        private const string
            NonUniqueMessage = "This element is not unique";

        private readonly List<object> Elements = new List<object>();
        private string _FirstProperty;
        private int _CurrentIndex;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => base.GetPropertyHeight(property, label);

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void OnGUI(Rect area, SerializedProperty property, GUIContent label)
        {
            var color = GUI.color;
            var tooltip = label.tooltip;

            switch (Event.current.type)
            {
                case EventType.Layout:
                    if (_FirstProperty == null)
                    {
                        _FirstProperty = property.propertyPath;
                    }
                    else if (property.propertyPath == _FirstProperty)
                    {
                        Elements.Clear();
                        _CurrentIndex = 0;
                    }

                    Elements.Add(property.GetValue());
                    break;

                case EventType.Repaint:
                    var value = Elements[_CurrentIndex];
                    for (int i = 0; i < Elements.Count; i++)
                    {
                        if (i != _CurrentIndex && Equals(value, Elements[i]))
                        {
                            GUI.color = WarningColor;
                            if (!string.IsNullOrEmpty(label.tooltip))
                                label.tooltip = $"{NonUniqueMessage}\n{label.tooltip}";
                            else
                                label.tooltip = NonUniqueMessage;
                            break;
                        }
                    }

                    _CurrentIndex++;
                    break;

                default:
                    break;
            }

            base.OnGUI(area, property, label);

            label.tooltip = tooltip;
            GUI.color = color;
        }

        /************************************************************************************************************************/
    }
}

#endif

