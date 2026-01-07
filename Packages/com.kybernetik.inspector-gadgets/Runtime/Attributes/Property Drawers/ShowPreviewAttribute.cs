// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

using UnityEngine;

namespace InspectorGadgets.Attributes
{
    /// <summary>[Pro-Only]
    /// Causes the attributed <see cref="Object"/> reference field to draw a preview of the target object.
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public sealed class ShowPreviewAttribute : PropertyAttribute
    {
        /************************************************************************************************************************/

        /// <summary>The maximum height (in pixels) at which to draw the preview.</summary>
        /// <remarks>Width will be determined using the aspect ratio of the preview.</remarks>
        public readonly int MaxHeight;

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="ShowPreviewAttribute"/> with the specified size.</summary>
        public ShowPreviewAttribute(int maxHeight = 64)
        {
            MaxHeight = maxHeight;
        }

        /************************************************************************************************************************/
    }
}

