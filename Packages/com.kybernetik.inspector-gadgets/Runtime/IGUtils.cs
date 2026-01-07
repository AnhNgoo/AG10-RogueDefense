// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InspectorGadgets
{
    /// <summary>Various utility methods used by <see cref="InspectorGadgets"/>.</summary>
    public static partial class IGUtils
    {
        /************************************************************************************************************************/
        #region Strings
        /************************************************************************************************************************/

        /// <summary>Adds spaces to `camelCase` before each uppercase letter.</summary>
        public static string ConvertCamelCaseToFriendly(string camelCase, bool uppercaseFirst = false)
        {
            var friendly = new StringBuilder();
            ConvertCamelCaseToFriendly(friendly, camelCase, 0, camelCase.Length, uppercaseFirst);
            return friendly.ToString();
        }

        /// <summary>Adds spaces to `camelCase` before each uppercase letter.</summary>
        public static void ConvertCamelCaseToFriendly(
            StringBuilder friendly,
            string camelCase,
            int start,
            int end,
            bool uppercaseFirst = false)
        {
            friendly.Append(uppercaseFirst ?
                char.ToUpper(camelCase[start]) :
                camelCase[start]);

            start++;

            for (; start < end; start++)
            {
                var character = camelCase[start];
                if (char.IsUpper(character))// Space before upper case.
                {
                    friendly.Append(' ');
                    friendly.Append(character);

                    // No spaces between consecutive upper case, unless followed by a non-upper case.
                    start++;
                    if (start >= camelCase.Length) return;
                    else
                    {
                        character = camelCase[start];
                        if (!char.IsUpper(character))
                        {
                            start--;
                        }
                        else
                        {
                            char nextCharacter;
                            while (true)
                            {
                                start++;
                                if (start >= camelCase.Length)
                                {
                                    friendly.Append(character);
                                    return;
                                }
                                else
                                {
                                    nextCharacter = camelCase[start];

                                    if (char.IsUpper(nextCharacter))
                                    {
                                        friendly.Append(character);
                                    }
                                    else
                                    {
                                        friendly.Append(' ');
                                        friendly.Append(character);
                                        friendly.Append(nextCharacter);
                                        break;
                                    }

                                    character = nextCharacter;
                                }
                            }
                        }
                    }
                }
                else if (char.IsNumber(character))// Space before number.
                {
                    friendly.Append(' ');
                    friendly.Append(character);
                    while (true)
                    {
                        start++;
                        if (start >= camelCase.Length) return;
                        else
                        {
                            character = camelCase[start];

                            if (char.IsNumber(character))
                                friendly.Append(character);
                            else
                            {
                                start--;
                                break;
                            }
                        }
                    }
                }
                else friendly.Append(character);// Otherwise just append the character.
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Adds spaces to `fieldName` before each uppercase letter and removes any underscores from the start.
        /// </summary>
        public static string ConvertFieldNameToFriendly(string fieldName, bool uppercaseFirst = false)
        {
            if (string.IsNullOrEmpty(fieldName))
                return "";

            var friendly = new StringBuilder();

            var start = 0;
            while (fieldName[start] == '_')
            {
                start++;
                if (start >= fieldName.Length)
                    return fieldName;
            }

            ConvertCamelCaseToFriendly(friendly, fieldName, start, fieldName.Length, uppercaseFirst);
            return friendly.ToString();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Truncate the given string so it can be used in a GUI.Label. MaxLength = 16,382.
        /// </summary>
        public static string TruncateForLabel(string text)
        {
            const int MaxLength = 16382;// 16384 is a power of 2.
            if (text.Length > MaxLength)
                return text[..MaxLength];
            else
                return text;
        }

        /************************************************************************************************************************/

        /// <summary>Returns a string containing the value of each element in `collection`.</summary>
        public static string DeepToString(IEnumerable collection, string separator)
        {
            if (collection == null)
                return "null";
            else
                return DeepToString(collection.GetEnumerator(), separator);
        }

        /// <summary>Returns a string containing the value of each element in `collection` (each on a new line).</summary>
        public static string DeepToString(IEnumerable collection)
            => DeepToString(collection, Environment.NewLine);

        /************************************************************************************************************************/

        /// <summary>Each element returned by `enumerator` is appended to `text`.</summary>
        public static void AppendDeepToString(StringBuilder text, IEnumerator enumerator, string separator)
        {
            text.Append("[]");
            var countIndex = text.Length - 1;
            var count = 0;

            while (enumerator.MoveNext())
            {
                text.Append(separator);
                text.Append('[');
                text.Append(count);
                text.Append("] = ");
                text.Append(enumerator.Current);

                count++;
            }

            text.Insert(countIndex, count);
        }

        /// <summary>Returns a string containing the value of each element in `enumerator`.</summary>
        public static string DeepToString(IEnumerator enumerator, string separator)
        {
            var text = new StringBuilder();
            AppendDeepToString(text, enumerator, separator);
            return text.ToString();
        }

        /// <summary>Returns a string containing the value of each element in `enumerator` (each on a new line).</summary>
        public static string DeepToString(IEnumerator enumerator)
            => DeepToString(enumerator, Environment.NewLine);

        /************************************************************************************************************************/

        /// <summary>
        /// Appends the full transform path to the target with slashes between the names of each of its parents, much like a file path.
        /// </summary>
        public static string GetTransformPath(Transform target)
        {
            var text = new StringBuilder();
            AppendTransformPath(text, target);
            return text.ToString();
        }

        /// <summary>
        /// Appends the full transform path to the target with slashes between the names of each of its parents, much like a file path.
        /// </summary>
        public static void AppendTransformPath(StringBuilder text, Transform target)
        {
            if (target.parent != null)
            {
                AppendTransformPath(text, target.parent);
                text.Append('/');
            }

            text.Append(target.name);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// If `source` is not null, `target` is given its value.
        /// </summary>
        public static void Set(this float? source, ref float target)
        {
            if (source != null)
                target = source.Value;
        }

        /// <summary>
        /// If `source` is not null, return its value, otherwise return `target`.
        /// </summary>
        public static float Set(this float? source, float target)
            => source != null ? source.Value : target;

        /************************************************************************************************************************/

        /// <summary>Returns the specified `value` as a string, or "-" if it is null.</summary>
        public static string ToDisplayString(this float? value)
            => value != null ? value.Value.ToDisplayString() : "-";

        /// <summary>Returns the specified `value` as a string using the <see cref="CultureInfo.InvariantCulture"/>.</summary>
        public static string ToDisplayString(this float value)
            => value.ToString(CultureInfo.InvariantCulture);

        /************************************************************************************************************************/

        /// <summary>
        /// Calculate the number of removals, inserts, and replacements needed to turn `a` into `b`.
        /// </summary>
        public static int CalculateLevenshteinDistance(string a, string b)
        {
            if (string.IsNullOrEmpty(a))
            {
                return string.IsNullOrEmpty(b) ? 0 : b.Length;
            }

            if (string.IsNullOrEmpty(b))
                return a.Length;

            var n = a.Length;
            var m = b.Length;
            var d = new int[n + 1, m + 1];

            // initialize the top and right of the table to 0, 1, 2, ...
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
                // Execution is contained in the For statement.
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
                // Execution is contained in the For statement.
            }

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    var cost = (b[j - 1] == a[i - 1]) ? 0 : 1;

                    d[i, j] = Mathf.Min(
                        Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Type Names
        /************************************************************************************************************************/

        private static readonly Dictionary<Type, string>
            TypeNames = new Dictionary<Type, string>
            {
                { typeof(object), "object" },
                { typeof(void), "void" },
                { typeof(bool), "bool" },
                { typeof(byte), "byte" },
                { typeof(sbyte), "sbyte" },
                { typeof(char), "char" },
                { typeof(string), "string" },
                { typeof(short), "short" },
                { typeof(int), "int" },
                { typeof(long), "long" },
                { typeof(ushort), "ushort" },
                { typeof(uint), "uint" },
                { typeof(ulong), "ulong" },
                { typeof(float), "float" },
                { typeof(double), "double" },
                { typeof(decimal), "decimal" },
            };

        private static readonly Dictionary<Type, string>
            FullTypeNames = new Dictionary<Type, string>(TypeNames);

        /************************************************************************************************************************/

        /// <summary>
        /// Returns the name of a `type` as it would appear in C# code.
        /// <para></para>
        /// For example, typeof(List&lt;float&gt;).FullName would give you:
        /// System.Collections.Generic.List`1[[System.Single, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]
        /// <para></para>
        /// This method would instead return System.Collections.Generic.List&lt;float&gt; if `fullName` is true, or
        /// just List&lt;float&gt; if it is false.
        /// <para></para>
        /// Note that all returned values are stored in a dictionary to speed up repeated use.
        /// </summary>
        public static string GetNameCS(this Type type, bool fullName = true)
        {
            if (type == null)
                return "";

            // Check if we have already got the name for that type.
            var names = fullName ? FullTypeNames : TypeNames;

            if (names.TryGetValue(type, out var name))
                return name;

            var text = new StringBuilder();

            if (type.IsArray)// Array = TypeName[].
            {
                text.Append(type.GetElementType().GetNameCS(fullName));

                text.Append('[');
                var dimensions = type.GetArrayRank();
                while (dimensions-- > 1)
                    text.Append(',');
                text.Append(']');

                goto Return;
            }

            if (type.IsPointer)// Pointer = TypeName*.
            {
                text.Append(type.GetElementType().GetNameCS(fullName));
                text.Append('*');

                goto Return;
            }

            if (type.IsGenericParameter)// Generic Parameter = TypeName (for unspecified generic parameters).
            {
                text.Append(type.Name);
                goto Return;
            }

            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)// Nullable = TypeName != null ?
            {
                text.Append(underlyingType.GetNameCS(fullName));
                text.Append('?');

                goto Return;
            }

            // Other Type = Namespace.NestedTypes.TypeName<GenericArguments>.

            if (fullName && type.Namespace != null)// Namespace.
            {
                text.Append(type.Namespace);
                text.Append('.');
            }

            var genericArguments = 0;

            if (type.DeclaringType != null)// Account for Nested Types.
            {
                // Count the nesting level.
                var nesting = 1;
                var declaringType = type.DeclaringType;
                while (declaringType.DeclaringType != null)
                {
                    declaringType = declaringType.DeclaringType;
                    nesting++;
                }

                // Append the name of each outer type, starting from the outside.
                while (nesting-- > 0)
                {
                    // Walk out to the current nesting level.
                    // This avoids the need to make a list of types in the nest or to insert type names instead of appending them.
                    declaringType = type;
                    for (int i = nesting; i >= 0; i--)
                        declaringType = declaringType.DeclaringType;

                    // Nested Type Name.
                    genericArguments = AppendNameAndGenericArguments(text, declaringType, fullName, genericArguments);
                    text.Append('.');
                }
            }

            // Type Name.
            AppendNameAndGenericArguments(text, type, fullName, genericArguments);

            Return:// Remember and return the name.
            name = text.ToString();
            names.Add(type, name);
            return name;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Appends the generic arguments of `type` (after skipping the specified number).
        /// </summary>
        public static int AppendNameAndGenericArguments(StringBuilder text, Type type, bool fullName = true, int skipGenericArguments = 0)
        {
            text.Append(type.Name);

            if (type.IsGenericType)
            {
                var backQuote = type.Name.IndexOf('`');
                if (backQuote >= 0)
                {
                    text.Length -= type.Name.Length - backQuote;

                    var genericArguments = type.GetGenericArguments();
                    if (skipGenericArguments < genericArguments.Length)
                    {
                        text.Append('<');

                        var firstArgument = genericArguments[skipGenericArguments];
                        skipGenericArguments++;

                        if (firstArgument.IsGenericParameter)
                        {
                            while (skipGenericArguments < genericArguments.Length)
                            {
                                text.Append(',');
                                skipGenericArguments++;
                            }
                        }
                        else
                        {
                            text.Append(firstArgument.GetNameCS(fullName));

                            while (skipGenericArguments < genericArguments.Length)
                            {
                                text.Append(", ");
                                text.Append(genericArguments[skipGenericArguments].GetNameCS(fullName));
                                skipGenericArguments++;
                            }
                        }

                        text.Append('>');
                    }
                }
            }

            return skipGenericArguments;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Member Names
        /************************************************************************************************************************/

        /// <summary>
        /// Returns the full name of a `member` as it would appear in C# code.
        /// <para></para>
        /// For example, passing this method info in as its own parameter would return "<see cref="IGUtils"/>.GetNameCS".
        /// <para></para>
        /// Note that when `member` is a <see cref="Type"/>, this method calls <see cref="GetNameCS(Type, bool)"/> instead.
        /// </summary>
        public static string GetNameCS(this MemberInfo member, bool fullName = true)
        {
            if (member == null)
                return "null";

            var type = member as Type;
            if (type != null)
                return type.GetNameCS(fullName);

            var text = new StringBuilder();

            if (member.DeclaringType != null)
            {
                text.Append(member.DeclaringType.GetNameCS(fullName));
                text.Append('.');
            }

            text.Append(member.Name);

            return text.ToString();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Appends the full name of a `member` as it would appear in C# code.
        /// <para></para>
        /// For example, passing this method info in as its own parameter would append "<see cref="IGUtils"/>.AppendName".
        /// <para></para>
        /// Note that when `member` is a <see cref="Type"/>, this method calls <see cref="GetNameCS(Type, bool)"/> instead.
        /// </summary>
        public static StringBuilder AppendNameCS(this StringBuilder text, MemberInfo member, bool fullName = true)
        {
            if (member == null)
            {
                text.Append("null");
                return text;
            }

            var type = member as Type;
            if (type != null)
            {
                text.Append(type.GetNameCS(fullName));
                return text;
            }

            if (member.DeclaringType != null)
            {
                text.Append(member.DeclaringType.GetNameCS(fullName));
                text.Append('.');
            }

            text.Append(member.Name);

            return text;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Attributes
        /************************************************************************************************************************/

        /// <summary>Gets a single custom attribute of type T and casts it.</summary>
        public static T GetCustomAttribute<T>(this ICustomAttributeProvider target, bool inherit = false)
            where T : Attribute
        {
            var attributeType = typeof(T);
            if (!target.IsDefined(attributeType, inherit))
                return null;
            else
                return (T)target.GetCustomAttributes(attributeType, inherit)[0];
        }

        /************************************************************************************************************************/

        /// <summary>Get all fields with the specified attribute in `type`.</summary>
        public static void GetAttributedFields<TAttribute>(
            Type type,
            BindingFlags bindingFlags,
            List<TAttribute> attributes,
            List<FieldInfo> fields,
            bool inherit = true)
            where TAttribute : Attribute
        {
            var attributeType = typeof(TAttribute);

            var allFields = type.GetFields(bindingFlags);

            for (int iField = 0; iField < allFields.Length; iField++)
            {
                var field = allFields[iField];
                if (!field.IsDefined(attributeType, inherit))
                    continue;

                var fieldAttributes = field.GetCustomAttributes(attributeType, inherit);
                for (int iAttribute = 0; iAttribute < fieldAttributes.Length; iAttribute++)
                {
                    attributes.Add((TAttribute)fieldAttributes[iAttribute]);
                    fields.Add(field);
                }
            }
        }

        /************************************************************************************************************************/

        /// <summary>Get all properties with the specified attribute in `type`.</summary>
        public static void GetAttributedProperties<TAttribute>(
            Type type,
            BindingFlags bindingFlags,
            List<TAttribute> attributes,
            List<PropertyInfo> properties,
            bool inherit = true)
            where TAttribute : Attribute
        {
            var attributeType = typeof(TAttribute);

            var allProperties = type.GetProperties(bindingFlags);

            for (int iProperty = 0; iProperty < allProperties.Length; iProperty++)
            {
                var property = allProperties[iProperty];
                if (!property.IsDefined(attributeType, inherit))
                    continue;

                var propertyAttributes = property.GetCustomAttributes(attributeType, inherit);
                for (int iAttribute = 0; iAttribute < propertyAttributes.Length; iAttribute++)
                {
                    attributes.Add((TAttribute)propertyAttributes[iAttribute]);
                    properties.Add(property);
                }
            }
        }

        /************************************************************************************************************************/

        /// <summary>Get all methods with the specified attribute in `type`.</summary>
        public static void GetAttributedMethods<TAttribute>(
            Type type,
            BindingFlags bindingFlags,
            List<TAttribute> attributes,
            List<MethodInfo> methods,
            bool inherit = true)
            where TAttribute : Attribute
        {
            var attributeType = typeof(TAttribute);

            var allMethods = type.GetMethods(bindingFlags);

            for (int iMethod = 0; iMethod < allMethods.Length; iMethod++)
            {
                var method = allMethods[iMethod];

                if (!method.IsDefined(attributeType, inherit))
                    continue;

                var methodAttributes = method.GetCustomAttributes(attributeType, inherit);
                for (int iAttribute = 0; iAttribute < methodAttributes.Length; iAttribute++)
                {
                    attributes.Add((TAttribute)methodAttributes[iAttribute]);
                    methods.Add(method);
                }
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Animation Curves
        /************************************************************************************************************************/

        /// <summary>
        /// Re-scales `value` from the old range (`oldMin` to `oldMax`) to the new range (0 to 1).
        /// </summary>
        public static float LinearRescaleTo01(this float value, float oldMin, float oldMax)
        {
            if (oldMin != oldMax)
                return (value - oldMin) / (oldMax - oldMin);
            else
                return 0.5f;
        }

        /// <summary>
        /// Re-scales `value` from the old range (`oldMin` to `oldMax`) to the new range (`newMin` to `newMax`).
        /// </summary>
        public static float LinearRescale(this float value, float oldMin, float oldmax, float newMin, float newmax)
        {
            return value.LinearRescaleTo01(oldMin, oldmax) * (newmax - newMin) + newMin;
        }

        /************************************************************************************************************************/

        /// <summary>Creates a duplicate of the given `curve`.</summary>
        public static AnimationCurve CopyCurve(AnimationCurve curve)
        {
            return new AnimationCurve(curve.keys)
            {
                postWrapMode = curve.postWrapMode,
                preWrapMode = curve.preWrapMode
            };
        }

        /************************************************************************************************************************/

        /// <summary>Gets the time of the first and last keyframes.</summary>
        public static void GetStartEndTime(this AnimationCurve curve, out float start, out float end)
            => curve.keys.GetStartEndTime(out start, out end);

        /// <summary>Gets the time of the first and last keyframes.</summary>
        public static void GetStartEndTime(this Keyframe[] keys, out float start, out float end)
        {
            if (keys.Length == 0)
            {
                start = end = 0;
                return;
            }

            start = keys[0].time;
            end = keys[^1].time;
        }

        /************************************************************************************************************************/

        /// <summary>Gets the value of the first and last keyframes.</summary>
        public static void GetStartEndValue(this AnimationCurve curve, out float start, out float end)
            => curve.keys.GetStartEndValue(out start, out end);

        /// <summary>Gets the value of the first and last keyframes.</summary>
        public static void GetStartEndValue(this Keyframe[] keys, out float start, out float end)
        {
            if (keys.Length == 0)
            {
                start = end = 0;
                return;
            }

            start = keys[0].value;
            end = keys[^1].value;
        }

        /************************************************************************************************************************/

        /// <summary>Gets the higher and lower value of the first and last keyframes.</summary>
        public static void GetStartEndValueSorted(this AnimationCurve curve, out float min, out float max)
            => curve.keys.GetStartEndValueSorted(out min, out max);

        /// <summary>Gets the higher and lower value of the first and last keyframes.</summary>
        public static void GetStartEndValueSorted(this Keyframe[] keys, out float min, out float max)
        {
            keys.GetStartEndValue(out min, out max);

            if (min > max)
                (max, min) = (min, max);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Gets the values of the lowest and highest keyframes.
        /// <para></para>
        /// Note that these will not necessarily be the exact bounds of the curve if the tangents cause it to continue
        /// further than the keyframes.
        /// </summary>
        public static void GetMinMaxKeyValue(this AnimationCurve curve, out float min, out float max)
            => curve.keys.GetMinMaxKeyValue(out min, out max);

        /// <summary>
        /// Gets the values of the lowest and highest keyframes.
        /// <para></para>
        /// Note that these will not necessarily be the exact bounds of the curve if the tangents cause it to continue
        /// further than the keyframes.
        /// </summary>
        public static void GetMinMaxKeyValue(this Keyframe[] keys, out float min, out float max)
        {
            if (keys.Length == 0)
            {
                min = max = 0;
                return;
            }

            min = max = keys[0].value;

            for (int i = 1; i < keys.Length; i++)
            {
                var value = keys[i].value;

                if (min > value)
                    min = value;
                else if (max < value)
                    max = value;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Estimates the highest and lowest values in the `curve`.</summary>
        public static void GetMinMaxValueEstimate(this AnimationCurve curve, out float min, out float max, int sampleResolution = 10)
        {
            var count = curve.length;
            if (count == 0)
            {
                min = max = 0;
                return;
            }

            var keys = curve.keys;

            keys.GetStartEndValue(out var start, out max);
            if (start > max)
            {
                min = max;
                max = start;
            }
            else
            {
                min = start;
            }

            var previousTime = start;
            for (int i = 1; i < count; i++)
            {
                var next = keys[i];
                if (min > next.value) min = next.value;
                else if (max < next.value) max = next.value;

                var samples = Mathf.CeilToInt(sampleResolution * (next.time - previousTime));
                var step = 1f / (samples + 1);
                for (int j = 0; j < samples; j++)
                {
                    var value = curve.Evaluate(Mathf.Lerp(previousTime, next.time, (j + 1) * step));
                    if (min > value) min = value;
                    else if (max < value) max = value;
                }

                previousTime = next.time;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Indicates whether the time and values both start at 0 and end at 1.</summary>
        public static bool IsNormalized(this AnimationCurve curve)
            => curve.keys.IsNormalized();

        /// <summary>Indicates whether the time and values both start at 0 and end at 1.</summary>
        public static bool IsNormalized(this Keyframe[] keys)
        {
            keys.GetStartEndTime(out var startTime, out var endTime);
            keys.GetStartEndValue(out var startValue, out var endValue);

            return startTime == 0 && startValue == 0 && endTime == 1 && endValue == 1;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Rescales the curve so the time and values both start at 0 and end at 1.
        /// Note that this does not modify any of the tangents so the shape of the curve may change.
        /// Returns the input curve so calls can be chained.
        /// </summary>
        public static AnimationCurve Normalize(this AnimationCurve curve)
        {
            curve.keys = curve.keys.Normalize();
            return curve;
        }

        /// <summary>
        /// Rescales the curve so the time and values both start at 0 and end at 1.
        /// Returns the input array so calls can be chained.
        /// </summary>
        public static Keyframe[] Normalize(this Keyframe[] keys)
        {
            keys.GetStartEndTime(out var startTime, out var endTime);
            keys.GetStartEndValue(out var startValue, out var endValue);

            if (startTime == 0 && startValue == 0 && endTime == 1 && endValue == 1)
                return keys;

            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                key.time = key.time.LinearRescaleTo01(startTime, endTime);
                key.value = key.value.LinearRescaleTo01(startValue, endValue);
                keys[i] = key;
            }

            return keys;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Smooths the tangents of all keyframes.
        /// Returns the input curve so calls can be chained.
        /// </summary>
        public static AnimationCurve SmoothTangents(this AnimationCurve curve)
        {
            for (int i = 0; i < curve.length; i++)
                curve.SmoothTangents(i, 0);

            return curve;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Reverses the curve so that it runs backwards over the same time range.
        /// Returns the input curve so calls can be chained.
        /// </summary>
        public static AnimationCurve FlipHorizontal(this AnimationCurve curve)
        {
            var keys = curve.keys;
            if (keys.Length <= 1)
                return curve;

            (curve.postWrapMode, curve.preWrapMode) = (curve.preWrapMode, curve.postWrapMode);

            keys.GetStartEndTime(out var start, out var end);

            for (int i = 0; i < curve.length; i++)
            {
                var key = keys[i];
                key.time = start + end - key.time;

                var temp = key.inTangent;
                key.inTangent = -key.outTangent;
                key.outTangent = -temp;

                temp = key.inWeight;
                key.inWeight = key.outWeight;
                key.outWeight = temp;

                key.weightedMode = key.weightedMode.FlipWeightedMode();

                keys[i] = key;
            }

            curve.keys = keys;
            return curve;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Reverses the curve so that its values are upside down within range.
        /// Returns the input curve so calls can be chained.
        /// </summary>
        public static AnimationCurve FlipVertical(this AnimationCurve curve)
        {
            var keys = curve.keys;
            if (keys.Length <= 1)
                return curve;

            (curve.postWrapMode, curve.preWrapMode) = (curve.preWrapMode, curve.postWrapMode);

            curve.GetMinMaxKeyValue(out var min, out var max);

            for (int i = 0; i < curve.length; i++)
            {
                var key = keys[i];
                key.value = min + max - key.value;

                key.inTangent *= -1;
                key.outTangent *= -1;

                keys[i] = key;
            }

            curve.keys = keys;
            return curve;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Duplicates the curve past the end and mirrors the duplicated section.
        /// Returns the input curve so calls can be chained.
        /// </summary>
        public static AnimationCurve ExtendMirrorred(this AnimationCurve curve)
        {
            var keys = curve.keys;
            if (keys.Length <= 1)
                return curve;

            curve.postWrapMode = curve.preWrapMode;

            keys.GetStartEndTime(out var startTime, out var endTime);
            keys.GetMinMaxKeyValue(out var minValue, out var maxValue);

            var newKeys = new Keyframe[keys.Length * 2 - 1];

            for (int i = 0; i < curve.length; i++)
            {
                var key = keys[i];

                var opposite = newKeys.Length - 1 - i;
                if (opposite == i)
                {
                    key.outTangent = key.inTangent;
                    key.outWeight = key.inWeight;

                    switch (key.weightedMode)
                    {
                        case WeightedMode.In:
                        case WeightedMode.Out:
                            key.weightedMode = WeightedMode.Both;
                            break;
                        default:
                            break;
                    }

                    newKeys[i] = key;
                    continue;
                }

                newKeys[i] = key;

                key.time = endTime * 2 - key.time;
                key.value = maxValue * 2 - key.value;

                (key.outTangent, key.inTangent) = (key.inTangent, key.outTangent);

                (key.outWeight, key.inWeight) = (key.inWeight, key.outWeight);

                key.weightedMode = key.weightedMode.FlipWeightedMode();

                newKeys[newKeys.Length - 1 - i] = key;
            }

            curve.keys = newKeys;
            return curve;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Ensures that the curve is horizontally symmetrical in relation to its existing start and end points.
        /// </summary>
        public static AnimationCurve EnforceHorizontalSymmetry(this AnimationCurve curve)
        {
            var keys = curve.keys;
            var count = keys.Length;

            if (count <= 1)
                return curve;

            keys.GetStartEndTime(out var startTime, out var endTime);
            keys.GetStartEndValue(out var startValue, out var endValue);

            var halfCount = Mathf.CeilToInt(count * 0.5f);
            for (int i = 0; i < halfCount; i++)
            {
                var leftKey = keys[i];
                var rightKey = keys[count - 1 - i];

                var time = (leftKey.time + endTime - rightKey.time) * 0.5f;
                leftKey.time = time;
                rightKey.time = endTime - time;

                var value = (leftKey.value + endValue - rightKey.value) * 0.5f;
                leftKey.value = value;
                rightKey.value = endValue - value;

                var tangent = (leftKey.outTangent + rightKey.inTangent) * 0.5f;
                leftKey.outTangent = tangent;
                rightKey.inTangent = tangent;

                tangent = (leftKey.inTangent + rightKey.outTangent) * 0.5f;
                leftKey.inTangent = tangent;
                rightKey.outTangent = tangent;

                keys[i] = leftKey;
                keys[count - 1 - i] = rightKey;
            }

            curve.keys = keys;
            return curve;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// <see cref="WeightedMode.Out"/> becomes <see cref="WeightedMode.In"/> and vice versa.
        /// </summary>
        public static WeightedMode FlipWeightedMode(this WeightedMode mode)
        {
            switch (mode)
            {
                case WeightedMode.None:
                case WeightedMode.Both:
                default:
                    return mode;
                case WeightedMode.In: return WeightedMode.Out;
                case WeightedMode.Out: return WeightedMode.In;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns a string describing the start and end time and value of the `curve`.
        /// </summary>
        public static string GetDescription(this AnimationCurve curve)
        {
            if (curve == null)
                return null;

            var keys = curve.keys;

            keys.GetStartEndTime(out var startTime, out var endTime);
            keys.GetStartEndValue(out var startValue, out var endValue);

            return $"{nameof(AnimationCurve)}" +
                $" (Keys: {keys.Length}," +
                $" Time: {startTime} to {endTime}," +
                $" Value: {startValue} to {endValue})";
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Reflection
        /************************************************************************************************************************/

        /// <summary>
        /// Calls the specified `method` once for each type in each loaded assembly that references the specified `assembly`.
        /// </summary>
        public static void ForEachTypeInDependantAssemblies(Assembly assembly, Action<Type> method)
        {
            ForEachType(assembly, method);

            var name = assembly.GetName().Name;

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                var otherAssembly = assemblies[i];
                if (otherAssembly == assembly)
                    goto NextAssembly;

                var references = otherAssembly.GetReferencedAssemblies();
                for (int j = 0; j < references.Length; j++)
                {
                    if (references[j].Name == name)
                    {
                        ForEachType(otherAssembly, method);
                        goto NextAssembly;
                    }
                }

                NextAssembly: continue;
            }
        }

        /// <summary>
        /// Calls the specified `method` once for each type in the specified `assembly`.
        /// </summary>
        public static void ForEachType(Assembly assembly, Action<Type> method)
        {
            var types = assembly.GetTypes();
            for (int i = 0; i < types.Length; i++)
                method(types[i]);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Gets all non-abstract types in the currently loaded assemblies which derive from the specified base type
        /// (including the base type itself if it is not abstract).
        /// </summary>
        public static List<Type> GetDerivedTypes(this Type baseType, bool includeBase = true)
        {
            var derivedTypes = new List<Type>();
            baseType.GetDerivedTypes(derivedTypes, includeBase);
            return derivedTypes;
        }

        /// <summary>
        /// Gets all non-abstract types in the currently loaded assemblies which derive from the specified base type
        /// (including the base type itself if it is not abstract).
        /// </summary>
        public static void GetDerivedTypes(this Type baseType, ICollection<Type> derivedTypes, bool includeBase = true)
        {
            if (includeBase && !baseType.IsAbstract)
                derivedTypes.Add(baseType);

            if (!baseType.ContainsGenericParameters)
            {
                ForEachTypeInDependantAssemblies(baseType.Assembly, (type) =>
                {
                    if (type == baseType ||
                        type.IsAbstract ||
                        !baseType.IsAssignableFrom(type))
                        return;

                    derivedTypes.Add(type);
                });
            }
            else// If the type has unspecified generic parameters, we need to compare its entire heirarchy individually.
            {
                ForEachTypeInDependantAssemblies(baseType.Assembly, (type) =>
                {
                    if (type == baseType ||
                        type.IsAbstract)
                        return;

                    var originalType = type;

                    while (true)
                    {
                        if (type == null)
                            break;

                        if (type.IsGenericType)
                            type = type.GetGenericTypeDefinition();

                        if (type == baseType)
                        {
                            derivedTypes.Add(originalType);
                            break;
                        }
                        else
                        {
                            type = type.BaseType;
                        }
                    }
                });
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Calls <see cref="Assembly.GetType(string, bool, bool)"/> on each currently loaded assembly until it finds a
        /// match then returns it.
        /// </summary>
        public static Type FindType(string name, bool ignoreCase)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                var type = assemblies[i].GetType(name, false, ignoreCase);
                if (type != null)
                    return type;
            }

            return null;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Conversion
        /************************************************************************************************************************/

        /// <summary>Converts a <see cref="Vector3"/> to a <see cref="Color"/>.</summary>
        public static Color ToColor(this Vector3 vector)
            => new Color(vector.x, vector.y, vector.z);

        /// <summary>Converts a <see cref="Vector4"/> to a <see cref="Color"/>.</summary>
        public static Color ToColor(this Vector4 vector)
            => new Color(vector.x, vector.y, vector.z, vector.w);

        /// <summary>Converts a <see cref="Color"/> to a <see cref="Vector3"/>.</summary>
        public static Vector3 ToVector3(this Color color)
            => new Vector3(color.r, color.g, color.b);

        /// <summary>Converts a <see cref="Color"/> to a <see cref="Vector4"/>.</summary>
        public static Vector4 ToVector4(this Color color)
            => new Vector4(color.r, color.g, color.b, color.a);

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Components
        /************************************************************************************************************************/

        /// <summary>
        /// Tries various search methods in the following order until it finds something:
        /// <see cref="GameObject.GetComponent(Type)"/>, 
        /// <see cref="GameObject.GetComponentsInParent(Type, bool)"/>,
        /// <see cref="GameObject.GetComponentsInChildren(Type, bool)"/>,
        /// <see cref="Resources.FindObjectsOfTypeAll(Type)"/>.
        /// <para></para>
        /// In the first group where a component of the correct type is found, if multiple components were found the
        /// one with a name closest to the `nameHint` is chosen.
        /// </summary>
        public static Component ProgressiveSearch(GameObject gameObject, Type componentType, string nameHint)
        {
            Component component;

            component = gameObject.GetComponent(componentType);
            if (component != null)
                return component;

            component = GetBestMatch(gameObject.GetComponentsInParent(componentType, true), nameHint);
            if (component != null)
                return component;

            component = GetBestMatch(gameObject.GetComponentsInChildren(componentType, true), nameHint);
            if (component != null)
                return component;

            component = GetBestMatch(Resources.FindObjectsOfTypeAll(componentType), nameHint) as Component;
            if (component != null)
                return component;

            return null;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Finds a component of the specified `componentType` on the `gameObject` or any of its parents or children.
        /// If multiple components are found the one with a name closest to the `nameHint` is chosen.
        /// </summary>
        public static Component GetComponentInHierarchy(GameObject gameObject, Type componentType, string nameHint)
        {
            var bestScore = int.MaxValue;

            var bestComponent = gameObject.GetComponent(componentType);
            if (bestComponent != null)
                bestScore = CalculateLevenshteinDistance(nameHint, bestComponent.name);

            var component = GetBestMatch(gameObject.GetComponentsInParent(componentType), nameHint, out var score);
            if (bestScore > score)
            {
                bestComponent = component;
                bestScore = score;
            }

            component = GetBestMatch(gameObject.GetComponentsInChildren(componentType), nameHint, out score);
            if (bestScore > score)
            {
                bestComponent = component;
            }

            return bestComponent;
        }

        /************************************************************************************************************************/

        /// <summary>Returns the <see cref="GameObject.transform"/> or <see cref="Component.transform"/>.</summary>
        public static Transform GetTransform(Object obj)
        {
            if (obj is GameObject gameObject)
                return gameObject.transform;

            if (obj is Component component)
                return component.transform;

            return null;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Collections
        /************************************************************************************************************************/

        /// <summary>
        /// Compares the name of each of the `objects` and returns the one that is closest to the `nameHint`.
        /// <para></para>
        /// See also: <see cref="CalculateLevenshteinDistance"/>.
        /// </summary>
        public static T GetBestMatch<T>(T[] objects, string nameHint, out int score) where T : Object
        {
            score = int.MaxValue;

            if (objects.Length == 0)
                return null;

            T bestObject = null;

            for (int i = 0; i < objects.Length; i++)
            {
                var obj = objects[i];
                if (obj == null)
                    continue;

                var objScore = CalculateLevenshteinDistance(nameHint, obj.name);
                if (score > objScore)
                {
                    bestObject = obj;
                    score = objScore;
                }
            }

            return bestObject;
        }

        /// <summary>
        /// Compares the name of each of the `objects` and returns the one that is closest to the `nameHint`.
        /// <para></para>
        /// See also: <see cref="CalculateLevenshteinDistance"/>.
        /// </summary>
        public static T GetBestMatch<T>(T[] objects, string nameHint) where T : Object
            => GetBestMatch(objects, nameHint, out var score);

        /************************************************************************************************************************/

        /// <summary>
        /// Sorts `list`, maintaining the order of any elements with an identical comparison
        /// (unlike the standard <see cref="List{T}.Sort(Comparison{T})"/> method).
        /// </summary>
        public static void StableInsertionSort<T>(IList<T> list, Comparison<T> comparison)
        {
            var count = list.Count;
            for (int j = 1; j < count; j++)
            {
                var key = list[j];

                var i = j - 1;
                for (; i >= 0 && comparison(list[i], key) > 0; i--)
                {
                    list[i + 1] = list[i];
                }
                list[i + 1] = key;
            }
        }

        /// <summary>
        /// Sorts `list`, maintaining the order of any elements with an identical comparison
        /// (unlike the standard <see cref="List{T}.Sort()"/> method).
        /// </summary>
        public static void StableInsertionSort<T>(IList<T> list) where T : IComparable<T>
            => StableInsertionSort(list, (a, b) => a.CompareTo(b));

        /************************************************************************************************************************/

        /// <summary>
        /// If the specified `key` is present in the `dictionary`, its value is returned.
        /// Otherwise a new value is added to the `dictionary` and returned.
        /// </summary>
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) where TValue : new()
        {
            if (!dictionary.TryGetValue(key, out var value))
            {
                value = new TValue();
                dictionary.Add(key, value);
            }

            return value;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/

    }
}

