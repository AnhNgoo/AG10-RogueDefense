// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

using System;

namespace InspectorGadgets.Attributes
{
    /// <summary>[Pro-Only]
    /// <see cref="Editor.Editor{T}"/> uses this attribute to add a button at the bottom of the default inspector to
    /// run the marked method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ButtonAttribute : BaseInspectableAttribute
    {
        /************************************************************************************************************************/

        /// <summary>
        /// If true, clicking the button will automatically call <see cref="UnityEditor.EditorUtility.SetDirty"/> after
        /// invoking the method.
        /// </summary>
        public bool SetDirty { get; set; }

        /************************************************************************************************************************/
    }
}

