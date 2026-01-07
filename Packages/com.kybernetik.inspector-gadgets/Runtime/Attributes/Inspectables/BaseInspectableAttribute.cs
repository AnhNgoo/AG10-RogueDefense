// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

using System;
using UnityEngine;

namespace InspectorGadgets.Attributes
{
    /// <summary>[Pro-Only]
    /// <see cref="Editor.Editor{T}"/> uses these attributes to add extra elements to the inspector.
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public abstract class BaseInspectableAttribute : Attribute,
        IComparable<BaseInspectableAttribute>
    {
        /************************************************************************************************************************/

        /// <summary>The label to use as a prefix before the value. If not set, it will use the name of the attributed member.</summary>
        public string Label { get; set; }

        /// <summary>The tooltip to use as for the label. If not set, it will use the full name of the attributed member.</summary>
        public string Tooltip { get; set; }

        private GUIContent _LabelContent;

        /// <summary>
        /// The <see cref="GUIContent"/> used for this inspectable's label, creates from the <see cref="Label"/> and
        /// <see cref="Tooltip"/>.
        /// </summary>
        public GUIContent LabelContent
        {
            get
            {
                if (_LabelContent == null)
                    _LabelContent = new GUIContent(Label, Tooltip);
                return _LabelContent;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// If set, this inspectable will be drawn at the specified index amongst the regular serialized fields instead
        /// of after them.
        /// </summary>
        public int DisplayIndex { get; set; } = int.MaxValue;

        /************************************************************************************************************************/

        private EditorState? _When;

        /// <summary>Determines when this attribute should be active.</summary>
        public EditorState When
        {
            get => _When.HasValue ? _When.Value : default;
            set => _When = value;
        }

        /// <summary>Determines when this attribute should be active.</summary>
        public ref EditorState? WhenNullable => ref _When;

        /************************************************************************************************************************/

        /// <summary>Compares the <see cref="DisplayIndex"/> of this inspectable to the specified `other`.</summary>
        public int CompareTo(BaseInspectableAttribute other)
            => DisplayIndex.CompareTo(other.DisplayIndex);

        /************************************************************************************************************************/
    }
}

