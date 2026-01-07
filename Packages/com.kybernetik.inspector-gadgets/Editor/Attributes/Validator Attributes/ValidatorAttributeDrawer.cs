// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using InspectorGadgets.Attributes;
using UnityEditor;
using UnityEngine;

namespace InspectorGadgets.Editor.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(ValidatorAttribute), true)]
    internal sealed class ValidatorAttributeDrawer : ObjectDrawer
    {
        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => base.GetPropertyHeight(property, label);

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void OnGUI(Rect area, SerializedProperty property, GUIContent label)
        {
            double previousDoubleValue = 0;
            long previousLongValue = 0;
            Object previousObjectReference = null;
            switch (property.propertyType)
            {
                case SerializedPropertyType.Float:
                    previousDoubleValue = property.doubleValue;
                    break;
                case SerializedPropertyType.Integer:
                    previousLongValue = property.longValue;
                    break;
                case SerializedPropertyType.ObjectReference:
                    previousObjectReference = property.objectReferenceValue;
                    break;
            }

            EditorGUI.BeginChangeCheck();

            base.OnGUI(area, property, label);

            if (EditorGUI.EndChangeCheck())
            {
                var attribute = this.attribute as ValidatorAttribute;
                if (attribute == null)
                    return;

                switch (property.propertyType)
                {
                    case SerializedPropertyType.Float:
                        var doubleValue = property.doubleValue;
                        if (attribute.TryValidate(ref doubleValue))
                            property.doubleValue = doubleValue;
                        else
                            property.doubleValue = previousDoubleValue;
                        break;

                    case SerializedPropertyType.Integer:
                        var longValue = property.longValue;
                        if (attribute.TryValidate(ref longValue))
                            property.longValue = longValue;
                        else
                            property.longValue = previousLongValue;
                        break;

                    case SerializedPropertyType.ObjectReference:
                        var objectReference = property.objectReferenceValue;
                        if (attribute.TryValidate(ref objectReference))
                            property.objectReferenceValue = objectReference;
                        else
                            property.objectReferenceValue = previousObjectReference;
                        break;
                }
            }
        }

        /************************************************************************************************************************/
    }
}

#endif

