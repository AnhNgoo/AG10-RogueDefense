// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

using System.Globalization;
using System.Text;
using UnityEngine;

namespace InspectorGadgets
{
    /// <summary>A <see cref="Vector4"/> which uses nullable floats.</summary>
    public sealed class NullableVector4
    {
        /************************************************************************************************************************/

        /// <summary>The X component of this vector.</summary>
        public float? x;

        /// <summary>The Y component of this vector.</summary>
        public float? y;

        /// <summary>The Z component of this vector.</summary>
        public float? z;

        /// <summary>The W component of this vector.</summary>
        public float? w;

        /************************************************************************************************************************/

        /// <summary>The component of this vector at the specified index: 0 = x, 1 = y, 2 = z, 3 = w.</summary>
        public float? this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0: return x;
                    case 1: return y;
                    case 2: return z;
                    case 3: return w;
                    default: return null;
                }
            }
            set
            {
                switch (i)
                {
                    case 0: x = value; break;
                    case 1: y = value; break;
                    case 2: z = value; break;
                    case 3: w = value; break;
                    default: throw new System.ArgumentOutOfRangeException(nameof(i));
                }
            }
        }

        /************************************************************************************************************************/

        /// <summary>Returns true if any of the components of this vector are null.</summary>
        public bool AnyNull()
        {
            return x == null || y == null || z == null || w == null;
        }

        /// <summary>Returns true if any of the components of this vector are null, ignoring components after `componentCount`.</summary>
        public bool AnyNull(int componentCount)
        {
            if (componentCount >= 1)
            {
                if (x == null) return true;
                if (componentCount >= 2)
                {
                    if (y == null) return true;
                    if (componentCount >= 3)
                    {
                        if (z == null) return true;
                        if (componentCount >= 4)
                        {
                            if (w == null) return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>Returns true if all of the components of this vector are null.</summary>
        public bool AllNull()
        {
            return x == null && y == null && z == null && w == null;
        }

        /// <summary>Returns true if all of the components of this vector are null, ignoring components after `componentCount`.</summary>
        public bool AllNull(int componentCount)
        {
            if (componentCount >= 1)
            {
                if (x != null) return false;
                if (componentCount >= 2)
                {
                    if (y != null) return false;
                    if (componentCount >= 3)
                    {
                        if (z != null) return false;
                        if (componentCount >= 4)
                        {
                            if (w != null) return false;
                        }
                    }
                }
            }

            return true;
        }

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="NullableVector4"/> with all components null.</summary>
        public NullableVector4() { }

        /// <summary>Creates a new <see cref="NullableVector4"/> with the specified components.</summary>
        public NullableVector4(float? x = null, float? y = null, float? z = null, float? w = null)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        /// <summary>Creates a new <see cref="NullableVector4"/> with each of its components set the same as the specified `value`.</summary>
        public NullableVector4(Vector2 value)
        {
            x = value.x;
            y = value.y;
        }

        /// <summary>Creates a new <see cref="NullableVector4"/> with each of its components set the same as the specified `value`.</summary>
        public NullableVector4(Vector3 value)
        {
            x = value.x;
            y = value.y;
            z = value.z;
        }

        /// <summary>Creates a new <see cref="NullableVector4"/> with each of its components set the same as the specified `value`.</summary>
        public NullableVector4(Vector4 value)
        {
            x = value.x;
            y = value.y;
            z = value.z;
            w = value.w;
        }

        /// <summary>Creates a new <see cref="NullableVector4"/> with each of its components set the same as the specified `value`.</summary>
        public NullableVector4(NullableVector4 value)
        {
            x = value.x;
            y = value.y;
            z = value.z;
            w = value.w;
        }

        /// <summary>Creates a new <see cref="NullableVector4"/> using the first 4 elements of the specified array as its components.</summary>
        public NullableVector4(float?[] values)
        {
            if (values == null)
                return;

            if (values.Length >= 1)
            {
                x = values[0];
                if (values.Length >= 2)
                {
                    y = values[1];
                    if (values.Length >= 3)
                    {
                        z = values[2];
                        if (values.Length >= 4)
                        {
                            w = values[3];
                        }
                    }
                }
            }
        }

        /************************************************************************************************************************/

        /// <summary>Sets all components of this vector to null.</summary>
        public void SetAllNull()
        {
            x = y = z = w = null;
        }

        /************************************************************************************************************************/

        /// <summary>Sets all components of this vector which are null to 0.</summary>
        public void ZeroAllNulls()
        {
            if (x == null) x = 0;
            if (y == null) y = 0;
            if (z == null) z = 0;
            if (w == null) w = 0;
        }

        /************************************************************************************************************************/

        /// <summary>Sets each of the components of this vector to be the same as the specified `value`.</summary>
        public void CopyFrom(NullableVector4 value)
        {
            x = value.x;
            y = value.y;
            z = value.z;
            w = value.w;
        }

        /// <summary>Sets each of the components of this vector to be the same as the specified `value`.</summary>
        public void CopyFrom(Vector2 value)
        {
            x = value.x;
            y = value.y;
        }

        /// <summary>Sets each of the components of this vector to be the same as the specified `value`.</summary>
        public void CopyFrom(Vector3 value)
        {
            x = value.x;
            y = value.y;
            z = value.z;
        }

        /// <summary>Sets each of the components of this vector to be the same as the specified `value`.</summary>
        public void CopyFrom(Vector4 value)
        {
            x = value.x;
            y = value.y;
            z = value.z;
            w = value.w;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns a new <see cref="Vector2"/> using components of this vector.
        /// Any components which are null will be set to 0.
        /// </summary>
        public Vector2 ToVector2()
        {
            return new Vector2(
                x != null ? x.Value : 0,
                y != null ? y.Value : 0);
        }

        /// <summary>
        /// Returns a new <see cref="Vector3"/> using components of this vector.
        /// Any components which are null will be set to 0.
        /// </summary>
        public Vector3 ToVector3()
        {
            return new Vector3(
                x != null ? x.Value : 0,
                y != null ? y.Value : 0,
                z != null ? z.Value : 0);
        }

        /// <summary>
        /// Returns a new <see cref="Vector3"/> using components of this vector.
        /// Any components which are null will be set to 0.
        /// </summary>
        public Vector4 ToVector4()
        {
            return new Vector4(
                x != null ? x.Value : 0,
                y != null ? y.Value : 0,
                z != null ? z.Value : 0,
                w != null ? w.Value : 0);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns a new <see cref="Vector3"/> using components of this vector.
        /// Any components which are null will instead use the corresponding component of the specified `fallback`.
        /// </summary>
        public Vector2 ToVector3(Vector2 fallback)
        {
            if (x != null) fallback.x = x.Value;
            if (y != null) fallback.y = y.Value;
            return fallback;
        }

        /// <summary>
        /// Returns a new <see cref="Vector3"/> using components of this vector.
        /// Any components which are null will instead use the corresponding component of the specified `fallback`.
        /// </summary>
        public Vector3 ToVector3(Vector3 fallback)
        {
            if (x != null) fallback.x = x.Value;
            if (y != null) fallback.y = y.Value;
            if (z != null) fallback.z = z.Value;
            return fallback;
        }

        /// <summary>
        /// Returns a new <see cref="Vector4"/> using components of this vector.
        /// Any components which are null will instead use the corresponding component of the specified `fallback`.
        /// </summary>
        public Vector4 ToVector4(Vector4 fallback)
        {
            if (x != null) fallback.x = x.Value;
            if (y != null) fallback.y = y.Value;
            if (z != null) fallback.z = z.Value;
            if (w != null) fallback.w = w.Value;
            return fallback;
        }

        /************************************************************************************************************************/

        /// <summary>Returns a nicely formatted string for this vector using '-' to denote nulls.</summary>
        public string ToString(int componentCount)
        {
            if (componentCount <= 0)
                throw new System.ArgumentException(nameof(componentCount));

            var str = new StringBuilder();
            str.Append('(');
            str.Append(x.ToDisplayString());
            if (componentCount >= 2)
            {
                str.Append(", ");
                str.Append(y.ToDisplayString());
                if (componentCount >= 3)
                {
                    str.Append(", ");
                    str.Append(z.ToDisplayString());
                    if (componentCount >= 4)
                    {
                        str.Append(", ");
                        str.Append(w.ToDisplayString());
                    }
                }
            }
            str.Append(')');
            return str.ToString();
        }

        /// <summary>Returns a nicely formatted string for this vector using '-' to denote nulls.</summary>
        public override string ToString()
            => ToString(4);

        /************************************************************************************************************************/

        /// <summary>
        /// Attempts to parse a series of floats from the given string and returns the index of the last successfully parsed value.
        /// </summary>
        public static int TryParse(string value, int componentCount, out NullableVector4 results)
        {
            results = new NullableVector4();

            if (value == null)
                return 0;

            var start = value.IndexOf('(') + 1;

            int end;
            var componentsParsed = -1;

            for (int i = 0; i < componentCount; i++)
            {
                end = value.IndexOf(',', start);
                if (end < 0)
                {
                    end = value.IndexOf(')', start);
                    if (end < 0)
                        end = value.Length;
                }

                var digitStart = start;
                var digitEnd = end;

                // Trim any non-digit prefix as long as it doesn't start with a '.'.
                var separator = CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator;
                while (digitStart < digitEnd)
                {
                    if (digitStart + separator.Length < digitEnd &&
                        string.Compare(value, digitStart, separator, 0, separator.Length) == 0)
                        break;

                    var c = value[digitStart];
                    if (c == '-' || char.IsDigit(c))
                        break;
                    else
                        digitStart++;
                }

                // Trim any non-digit suffix.
                while (digitEnd > digitStart)
                {
                    var c = value[digitEnd - 1];
                    if (char.IsDigit(c))
                        break;
                    else
                        digitEnd--;
                }

                var substring = value.Substring(digitStart, digitEnd - digitStart);

                if (float.TryParse(substring, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
                {
                    results[i] = floatValue;
                    componentsParsed = i;
                }

                start = end + 1;

                if (start >= value.Length)
                {
                    i++;
                    break;
                }
            }

            return componentsParsed;
        }

        /************************************************************************************************************************/
        #region Operators
        /************************************************************************************************************************/

        /// <summary>Returns true if all of the components of `a` and `b` are equal.</summary>
        public static bool operator ==(NullableVector4 a, NullableVector4 b)
        {
            if (a is null) return b is null;

            if (b is null) return false;

            if (a.x != b.x) return false;
            if (a.y != b.y) return false;
            if (a.z != b.z) return false;
            if (a.w != b.w) return false;

            return true;
        }

        /// <summary>Returns true if any of the components of `a` and `b` are not equal.</summary>
        public static bool operator !=(NullableVector4 a, NullableVector4 b)
            => !(a == b);

        /************************************************************************************************************************/

        /// <summary>Returns true if all of the components of `this` and `other` are equal, ignoring components after `componentCount`.</summary>
        public bool Equals(NullableVector4 other, int componentCount)
        {
            if (other == null)
                return false;

            if (componentCount >= 1)
            {
                if (x != other.x)
                    return false;

                if (componentCount >= 2)
                {
                    if (y != other.y)
                        return false;

                    if (componentCount >= 3)
                    {
                        if (z != other.z)
                            return false;

                        if (componentCount >= 4)
                        {
                            if (w != other.w)
                                return false;
                        }
                    }
                }
            }

            return true;
        }

        /************************************************************************************************************************/

        /// <summary>Returns true if all of the components of `a` and `b` are equal.</summary>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            var vector = obj as NullableVector4;
            if (vector != null)
                return this == vector;

            return false;
        }

        /// <summary>Uses the base method.</summary>
        public override int GetHashCode() => base.GetHashCode();

        /************************************************************************************************************************/

        /// <summary>Returns a new <see cref="NullableVector4"/> with each of its components set the same as the specified `vector`.</summary>
        public static implicit operator NullableVector4(Vector2 vector) => new NullableVector4(vector);

        /// <summary>Returns a new <see cref="NullableVector4"/> with each of its components set the same as the specified `vector`.</summary>
        public static implicit operator NullableVector4(Vector3 vector) => new NullableVector4(vector);

        /// <summary>Returns a new <see cref="NullableVector4"/> with each of its components set the same as the specified `vector`.</summary>
        public static implicit operator NullableVector4(Vector4 vector) => new NullableVector4(vector);

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

