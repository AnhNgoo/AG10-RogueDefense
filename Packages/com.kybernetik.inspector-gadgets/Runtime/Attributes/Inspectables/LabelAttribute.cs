// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

using System;

namespace InspectorGadgets.Attributes
{
    /// <summary>[Pro-Only]
    /// Adds a label at the bottom of the default inspector to display the value of the marked property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property)]
    public sealed class LabelAttribute : BaseInspectableAttribute
    {
        /************************************************************************************************************************/

        /// <summary>If true, the label will be hidden when the value is null.</summary>
        public bool HideWhenNull { get; set; }

        /// <summary>If true, the inspector will be constantly repainted while this label is shown to keep it updated.</summary>
        public bool ConstantlyRepaint { get; set; }

        /// <summary>
        /// If true, the label or attributed member name will be drawn on one line with the actual value drawn below it
        /// and able to take as many lines as you want.
        /// </summary>
        public bool LargeMode { get; set; }

        /************************************************************************************************************************/
    }
}

