// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

using System;
using UnityEngine;

namespace InspectorGadgets.Attributes
{
    /// <summary>[Pro-Only]
    /// Provides labels for the elements of a collection field to use instead of just calling them "Element X".
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public sealed class LabelledCollectionAttribute : PropertyAttribute
    {
        /************************************************************************************************************************/

        internal string _MemberName;
        internal Func<int, string> _GetLabel;

        /************************************************************************************************************************/

        /// <summary>Get the label to use for the element at the specified `index` in the collection.</summary>
        public string GetLabel(int index)
            => _GetLabel != null ? _GetLabel(index) : null;

        /************************************************************************************************************************/

        /// <summary>Uses the specified `labels` for the collection elements.</summary>
        public LabelledCollectionAttribute(params string[] labels)
        {
            _GetLabel = index => labels[index % labels.Length];
        }

        /************************************************************************************************************************/

        /// <summary>Uses the value names of the specified `enumType` for the collection elements.</summary>
        public LabelledCollectionAttribute(Type enumType)
        {
            var names = Enum.GetNames(enumType);
            _GetLabel = index =>
            {
                return index >= 0 && index < names.Length
                    ? names[index]
                    : index.ToString();
            };
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Uses the member with the specified name to determine the labels of the collection elements.
        /// <para></para>
        /// If the member is a collection field, the values in that collection will be used as the element labels.
        /// <para></para>
        /// If the member is a method with a single int parameter and a non-void return type, it will be called with
        /// each element index to determine the label.
        /// </summary>
        public LabelledCollectionAttribute(string memberName)
        {
            _MemberName = memberName;
        }

        /************************************************************************************************************************/
    }
}

