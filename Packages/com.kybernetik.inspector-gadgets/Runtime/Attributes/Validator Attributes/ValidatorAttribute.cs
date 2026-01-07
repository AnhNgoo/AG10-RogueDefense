// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

using UnityEngine;

namespace InspectorGadgets.Attributes
{
    /// <summary>[Pro-Only]
    /// Base class for attributes that apply some sort of validation to a field.
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public abstract class ValidatorAttribute : PropertyAttribute
    {
        /************************************************************************************************************************/

        /// <summary>Validates a <see cref="double"/> value or returns false to revert to the previous value.</summary>
        public virtual bool TryValidate(ref double value) => true;

        /// <summary>Validates a <see cref="long"/> value or returns false to revert to the previous value.</summary>
        public virtual bool TryValidate(ref long value) => true;

        /// <summary>Validates an <see cref="Object"/> value or returns false to revert to the previous value.</summary>
        public virtual bool TryValidate(ref Object value) => true;

        /************************************************************************************************************************/
    }
}

