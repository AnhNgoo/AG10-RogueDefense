// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

namespace InspectorGadgets.Attributes
{
    /// <summary>[Pro-Only]
    /// Specifies the maximum value allowed by the attributed int or float field.
    /// See also: <see cref="MinValueAttribute"/> and <see cref="ClampValueAttribute"/>.
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public sealed class MaxValueAttribute : ValidatorAttribute
    {
        /************************************************************************************************************************/

        /// <summary>The maximum allowed integer value.</summary>
        public readonly long MaxLong;

        /// <summary>The maximum allowed floating point value.</summary>
        public readonly double MaxDouble;

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="MaxValueAttribute"/> with the specified maximum value.</summary>
        public MaxValueAttribute(int max) : this((long)max) { }

        /// <summary>Creates a new <see cref="MaxValueAttribute"/> with the specified maximum value.</summary>
        public MaxValueAttribute(long max)
        {
            MaxLong = max;
            MaxDouble = max;
        }

        /// <summary>Creates a new <see cref="MaxValueAttribute"/> with the specified maximum value.</summary>
        public MaxValueAttribute(float max) : this((double)max) { }

        /// <summary>Creates a new <see cref="MaxValueAttribute"/> with the specified maximum value.</summary>
        public MaxValueAttribute(double max)
        {
            MaxLong = (long)max;
            MaxDouble = max;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override bool TryValidate(ref long value)
        {
            if (value > MaxLong)
                value = MaxLong;

            return true;
        }

        /// <inheritdoc/>
        public override bool TryValidate(ref double value)
        {
            if (value > MaxDouble)
                value = MaxDouble;

            return true;
        }

        /************************************************************************************************************************/
    }
}

