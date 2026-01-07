// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

using UnityEngine;

namespace InspectorGadgets.Attributes
{
    /// <summary>[Pro-Only]
    /// Causes a bool, string, or enum field to be drawn in the inspector as a series of toggle buttons rather than the
    /// usual dropdown list or text field.
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public sealed class ToolbarAttribute : PropertyAttribute
    {
        /************************************************************************************************************************/

        /// <summary>If set, this string will replace the field's default label.</summary>
        public string Label { get; set; }

        /// <summary>The labels for each button in the toolbar. Enums will use their own names.</summary>
        public readonly GUIContent[] Labels;

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="ToolbarAttribute"/>.</summary>
        /// <remarks>You must specify the <see cref="Labels"/> unless the attributed field is an enum.</remarks>
        public ToolbarAttribute() { }

        /// <summary>Creates a new <see cref="ToolbarAttribute"/> using the specified labels (not required for enums).</summary>
        /// <remarks>For bool fields, the first label is used for false and the second for true.</remarks>
        public ToolbarAttribute(params string[] labels)
        {
            Labels = new GUIContent[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                Labels[i] = new GUIContent(labels[i]);
            }
        }

        /************************************************************************************************************************/
    }
}

