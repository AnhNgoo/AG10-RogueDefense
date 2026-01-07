// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

namespace InspectorGadgets
{
    /// <summary>A common interface for objects which display a comment in the Inspector.</summary>
    public interface IComment
    {
        /************************************************************************************************************************/

        /// <summary>The text of this comment.</summary>
        string Text { get; set; }

        /// <summary>[Editor-Only] The name of the serialized backing field of the <see cref="Text"/> property.</summary>
        string TextFieldName { get; }

        /// <summary>False if this script is set to <see cref="UnityEngine.HideFlags.DontSaveInBuild"/>.</summary>
        bool IncludeInBuild { get; set; }

        /************************************************************************************************************************/
    }
}

