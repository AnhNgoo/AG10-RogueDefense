// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

namespace InspectorGadgets
{
    /// <summary>Represents a Unity Editor state which can be used as a condition.</summary>
    public enum EditorState
    {
        /// <summary>All the time, regardless of the current state of the Unity Editor.</summary>
        Always,

        /// <summary>When the Unity Editor is in Play Mode or in a Runtime Build.</summary>
        Playing,

        /// <summary>When the Unity Editor is not in Play Mode.</summary>
        Editing,
    }
}

