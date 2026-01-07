// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

namespace InspectorGadgets.Editor
{
    /// <summary>[Editor-Only] Flags for a combination of Alt, Ctrl, and Shift.</summary>
    [Flags]
    public enum ModifierKey
    {
        None = 0,
        Alt = 1 << 0,
        Ctrl = 1 << 1,
        Shift = 1 << 2,
        Disabled = 1 << 3,
    }

    /// <summary>[Editor-Only] An <see cref="AutoPrefs.EditorInt"/> which wraps <see cref="ModifierKey"/> flags.</summary>
    public class ModifierKeysPref : AutoPrefs.EditorInt
    {
        /************************************************************************************************************************/

        /// <inheritdoc/>
        public ModifierKeysPref(
            string key,
            ModifierKey defaultValue = ModifierKey.None,
            Action<int> onValueChanged = null)
            : base(key, (int)defaultValue, onValueChanged)
        { }

        /************************************************************************************************************************/

        /// <summary>The current value of this pref.</summary>
        public new ModifierKey Value
        {
            get => (ModifierKey)base.Value;
            set => base.Value = (int)value;
        }

        /************************************************************************************************************************/

        /// <summary>Does the <see cref="Value"/> contain the `modifier`?</summary>
        public bool ValueContains(ModifierKey modifier)
            => (Value & modifier) == modifier;

        /************************************************************************************************************************/

        /// <summary>Are all modifiers in the <see cref="Value"/> currently being held down?</summary>
        public bool AreKeysCurrentlyDown
            => AreKeysDown(Event.current);

        /// <summary>Are all modifiers in the <see cref="Value"/> being held down in the `currentEvent`?</summary>
        public bool AreKeysDown(Event currentEvent)
            => !ValueContains(ModifierKey.Disabled)
            && (!ValueContains(ModifierKey.Alt) || currentEvent.alt)
            && (!ValueContains(ModifierKey.Ctrl) || currentEvent.control)
            && (!ValueContains(ModifierKey.Shift) || currentEvent.shift);

        /************************************************************************************************************************/

        public bool DoGUI(GUIContent label)
            => this.DoGUI(label, (area, content, value, style)
                => (int)(ModifierKey)EditorGUI.EnumFlagsField(area, content, (ModifierKey)value));

        /************************************************************************************************************************/

        /// <summary>Returns the current value of this pref.</summary>
        public static implicit operator ModifierKey(ModifierKeysPref pref)
            => pref.Value;

        /************************************************************************************************************************/
    }
}

#endif
