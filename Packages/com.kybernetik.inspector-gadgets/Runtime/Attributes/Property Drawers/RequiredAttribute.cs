// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

using System;
using UnityEngine;

namespace InspectorGadgets.Attributes
{
    /// <summary>[Pro-Only]
    /// When the attributed member is drawn in the inspector, it will be highlighted in red when it has the default
    /// value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public sealed class RequiredAttribute : PropertyAttribute
    {
    }
}

