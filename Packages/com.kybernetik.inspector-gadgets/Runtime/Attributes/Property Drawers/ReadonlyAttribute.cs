// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

using UnityEngine;

namespace InspectorGadgets.Attributes
{
    /// <summary>[Pro-Only]
    /// Causes the attributed field to be greyed out and un-editable in the inspector.
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public sealed class ReadonlyAttribute : PropertyAttribute
    {
        /************************************************************************************************************************/

        /// <summary>Indicates when the field should be greyed out.</summary>
        public EditorState? When { get; set; }

        /************************************************************************************************************************/

        /// <summary>
        /// Creates a new <see cref="ReadonlyAttribute"/> to apply its effects in the default <see cref="EditorState"/>
        /// (set in the <c>Edit/Preferences</c> menu.
        /// </summary>
        public ReadonlyAttribute() { }

        /// <summary>
        /// Creates a new <see cref="ReadonlyAttribute"/> to apply its effects in the specified <see cref="EditorState"/>.
        /// </summary>
        public ReadonlyAttribute(EditorState when = EditorState.Always)
        {
            When = when;
        }

        /************************************************************************************************************************/
    }
}

