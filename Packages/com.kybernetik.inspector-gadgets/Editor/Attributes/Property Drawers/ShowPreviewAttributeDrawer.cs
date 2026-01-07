// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using InspectorGadgets.Attributes;
using UnityEditor;
using UnityEngine;

namespace InspectorGadgets.Editor.PropertyDrawers
{
    /// <summary>[Editor-Only] [Pro-Only] A custom drawer for fields with a <see cref="ShowPreviewAttribute"/>.</summary>
    [CustomPropertyDrawer(typeof(ShowPreviewAttribute))]
    public sealed class ShowPreviewAttributeDrawer : ObjectDrawer
    {
        /************************************************************************************************************************/

        private const float Padding = 1;

        private float _BaseHeight;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = _BaseHeight = EditorGUI.GetPropertyHeight(property, label);

            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                if (property.objectReferenceValue != null)
                {
                    var previewTexture = AssetPreview.GetAssetPreview(property.objectReferenceValue);
                    if (previewTexture != null)
                    {
                        var attribute = (ShowPreviewAttribute)this.attribute;

                        height += Mathf.Min(attribute.MaxHeight, previewTexture.height) + Padding;
                    }
                }
            }

            return height;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void OnGUI(Rect area, SerializedProperty property, GUIContent label)
        {
            area.height = _BaseHeight;
            base.OnGUI(area, property, label);

            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                DoWarningBoxGUI($"{property.name} is not an object reference type", property);
                return;
            }

            if (property.objectReferenceValue == null)
                return;

            var preview = AssetPreview.GetAssetPreview(property.objectReferenceValue);
            if (preview == null)
            {
                DoWarningBoxGUI($"{property.name} doesn't have an asset preview", property);
                return;
            }

            var attribute = (ShowPreviewAttribute)this.attribute;

            var xMax = area.xMax;

            area.y += area.height + Padding;

            area.height = Mathf.Min(attribute.MaxHeight, preview.height);
            area.width = area.height * preview.height / preview.width;
            area.x = xMax - area.width;

            if (area.width <= 0 || area.height <= 0)
                return;

            GUI.DrawTexture(area, preview);
        }

        /************************************************************************************************************************/

        private void DoWarningBoxGUI(string warningText, SerializedProperty property)
        {
            EditorGUILayout.HelpBox(warningText, MessageType.Warning);
            //Debug.LogWarning(warningText, property.serializedObject.targetObject);
        }

        /************************************************************************************************************************/
    }
}

#endif

