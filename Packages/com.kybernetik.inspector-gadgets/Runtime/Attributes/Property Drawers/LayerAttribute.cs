// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

using UnityEngine;

namespace InspectorGadgets.Attributes
{
    /// <summary>Displays the attributed <see cref="int"/> field as a layer.</summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public sealed class LayerAttribute : PropertyAttribute { }
}

