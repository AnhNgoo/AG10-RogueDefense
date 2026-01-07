// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using InspectorGadgets.Attributes;
using UnityEngine;

namespace InspectorGadgets.Editor.PropertyDrawers
{
    /// <summary>[Editor-Only] [Pro-Only] A custom drawer for fields with a <see cref="ColorAttribute"/>.</summary>
    [UnityEditor.CustomPropertyDrawer(typeof(ColorAttribute))]
    public sealed class ColorAttributeDrawer : ObjectDrawer
    {
        /************************************************************************************************************************/

        public override void OnGUI(Rect area, UnityEditor.SerializedProperty property, GUIContent label)
        {
            var oldColor = GUI.color;
            GUI.color = (attribute as ColorAttribute).Color;
            {
                base.OnGUI(area, property, label);
            }
            GUI.color = oldColor;
        }

        /************************************************************************************************************************/

        public override float GetPropertyHeight(UnityEditor.SerializedProperty property, GUIContent label)
        {
            return UnityEditor.EditorGUI.GetPropertyHeight(property, label, true);
        }

        /************************************************************************************************************************/
    }
}

#endif

