// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

using UnityEngine;

namespace InspectorGadgets.Attributes
{
    /// <summary>Copied from <see cref="UnityEditor.MessageType"/>.</summary>
    public enum InfoBoxType
    {
        None,
        Info,
        Warning,
        Error
    }

    /// <summary>[Pro-Only]
    /// Causes the attributed field to be drawn in a specific color.
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public sealed class InfoBoxAttribute : PropertyAttribute
    {
        /************************************************************************************************************************/

        /// <summary>The text to display to use.</summary>
        public readonly string Text;

        /// <summary>The icon type to use when displaying the box.</summary>
        public InfoBoxType Type { get; set; }

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="InfoBoxAttribute"/>.</summary>
        public InfoBoxAttribute(string text)
        {
            Text = text;
        }

        /************************************************************************************************************************/
    }
}

