// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using InspectorGadgets.Attributes;
using UnityEditor;
using UnityEngine;

namespace InspectorGadgets.Editor.PropertyDrawers
{
    /// <summary>[Editor-Only] [Pro-Only] A custom drawer for fields with an <see cref="InfoBoxAttribute"/>.</summary>
    [CustomPropertyDrawer(typeof(InfoBoxAttribute))]
    public sealed class InfoBoxAttributeDrawer : DecoratorDrawer
    {
        /************************************************************************************************************************/

        public InfoBoxAttribute Attribute
            => (InfoBoxAttribute)attribute;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override float GetHeight()
        {
            var width = EditorGUIUtility.currentViewWidth -
                EditorStyles.inspectorDefaultMargins.padding.horizontal -
                EditorGUI.IndentedRect(default).x -
                20;// Extra padding in case there's a scroll bar.

            var attribute = Attribute;
            var content = EditorGUIUtility.TrTextContentWithIcon(attribute.Text, (MessageType)attribute.Type);
            return
                EditorStyles.helpBox.CalcHeight(content, width) +
                IGEditorUtils.Spacing;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void OnGUI(Rect area)
        {
            area = EditorGUI.IndentedRect(area);
            area.height -= IGEditorUtils.Spacing;
            var attribute = Attribute;
            EditorGUI.HelpBox(area, attribute.Text, (MessageType)attribute.Type);
        }

        /************************************************************************************************************************/
    }
}

#endif

