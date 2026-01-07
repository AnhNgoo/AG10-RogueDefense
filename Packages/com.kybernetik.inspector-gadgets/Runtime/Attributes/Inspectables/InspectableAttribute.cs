// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

using System;

namespace InspectorGadgets.Attributes
{
    /// <summary>[Pro-Only]
    /// Adds the attributed field or property to the inspector as if it were serialized.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class InspectableAttribute : BaseInspectableAttribute
    {
        /************************************************************************************************************************/

        /// <summary>If true, the displayed field will be greyed out so the user can't modify it.</summary>
        public bool Readonly { get; set; }

        /// <summary>If true, the inspector will be constantly repainted while this label is shown to keep it updated.</summary>
        public bool ConstantlyRepaint { get; set; }

        /************************************************************************************************************************/
    }
}

