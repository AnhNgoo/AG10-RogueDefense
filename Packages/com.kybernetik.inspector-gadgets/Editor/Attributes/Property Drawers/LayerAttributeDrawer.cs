// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using InspectorGadgets.Attributes;
using UnityEditor;
using UnityEngine;

namespace InspectorGadgets.Editor.PropertyDrawers
{
    /// <summary>[Editor-Only] [Pro-Only] A custom drawer for fields with a <see cref="LabelledCollectionAttribute"/>.</summary>
    [CustomPropertyDrawer(typeof(LayerAttribute))]
    public sealed class LayerAttributeDrawer : PropertyDrawer
    {
        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void OnGUI(Rect area, SerializedProperty property, GUIContent label)
        {
            property.intValue = EditorGUI.LayerField(area, label, property.intValue);
        }

        /************************************************************************************************************************/
    }
}

#endif

