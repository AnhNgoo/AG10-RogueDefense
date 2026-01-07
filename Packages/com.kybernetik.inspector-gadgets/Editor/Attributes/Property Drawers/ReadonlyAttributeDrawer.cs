// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using InspectorGadgets.Attributes;
using UnityEditor;
using UnityEngine;

namespace InspectorGadgets.Editor.PropertyDrawers
{
    /// <summary>[Editor-Only] [Pro-Only] A custom drawer for fields with a <see cref="ReadonlyAttribute"/>.</summary>
    [CustomPropertyDrawer(typeof(ReadonlyAttribute))]
    public sealed class ReadonlyAttributeDrawer : ObjectDrawer
    {
        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void OnGUI(Rect area, SerializedProperty property, GUIContent label)
        {
            var attribute = (ReadonlyAttribute)this.attribute;

            var enabled = GUI.enabled;
            GUI.enabled = !attribute.When.ValueOrDefault().IsNow();
            base.OnGUI(area, property, label);

            var currentEvent = Event.current;
            if (currentEvent.rawType == EventType.ContextClick &&
                area.Contains(currentEvent.mousePosition))
                SerializedPropertyContextMenu.OpenMenu(property);

            GUI.enabled = enabled;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        /************************************************************************************************************************/
    }
}

#endif

