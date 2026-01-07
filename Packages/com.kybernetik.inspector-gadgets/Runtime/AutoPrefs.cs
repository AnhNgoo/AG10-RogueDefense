// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

// The missing methods these warnings complain about are implemented by the child types, so they aren't actually missing.
#pragma warning disable CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)

using System;
using UnityEngine;

namespace InspectorGadgets
{
    /// <summary>
    /// A collection of wrappers for PlayerPrefs which simplify the way you can store and retrieve values.
    /// </summary>
    /// <remarks>PlayerPrefs versions can be found in <c>InspectorGadgets.Editor.AutoPrefs</c>.</remarks>
    public static class AutoPrefs
    {
        /************************************************************************************************************************/

        /// <summary>An object which encapsulates a pref value stored with a specific key.</summary>
        public interface IAutoPref
        {
            /// <summary>The key used to identify this pref.</summary>
            string Key { get; }

            /// <summary>The current value of this pref.</summary>
            object Value { get; }
        }

        /************************************************************************************************************************/

        public static event Action OnAnyValueChanged;

        /************************************************************************************************************************/

        /// <summary>An object which encapsulates a pref value stored with a specific key.</summary>
        public abstract class AutoPref<T> : IAutoPref
        {
            /************************************************************************************************************************/
            #region Fields and Properties
            /************************************************************************************************************************/

            /// <summary>The key used to identify this pref.</summary>
            public readonly string Key;

            /// <summary>The default value to use if this pref has no existing value.</summary>
            public readonly T DefaultValue;

            /// <summary>Called when the <see cref="Value"/> is changed.</summary>
            public readonly Action<T> OnValueChanged;

            /************************************************************************************************************************/

            private bool _IsLoaded;
            private T _Value;

            /// <summary>The current value of this pref.</summary>
            public T Value
            {
                get
                {
                    if (!_IsLoaded)
                        Reload();

                    return _Value;
                }
                set
                {
                    if (!_IsLoaded)
                    {
                        if (!IsSaved())
                        {
                            // If there is no saved value, set the value and make sure it's saved.
                            _Value = value;
                            _IsLoaded = true;
                            Save();

                            OnValueChanged?.Invoke(value);

                            return;
                        }
                        else Reload();
                    }

                    // Otherwise store and save the new value if it's different.
                    if (!Equals(_Value, value))
                    {
                        _Value = value;
                        Save();

                        OnValueChanged?.Invoke(value);
                    }
                }
            }

            /************************************************************************************************************************/

            string IAutoPref.Key => Key;
            object IAutoPref.Value => Value;

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
            #region Methods
            /************************************************************************************************************************/

            /// <summary>Constructs an <see cref="AutoPref{T}"/> with the specified `key` and `defaultValue`.</summary>
            protected AutoPref(string key, T defaultValue, Action<T> onValueChanged)
            {
                Key = key;
                DefaultValue = defaultValue;
                OnValueChanged = onValueChanged;
                OnValueChanged += value => OnAnyValueChanged?.Invoke();
            }

            /// <summary>Loads the value of this pref from the system.</summary>
            protected abstract T Load();

            /// <summary>Saves the value of this pref to the system.</summary>
            protected abstract void Save();

            /************************************************************************************************************************/

            /// <summary>Returns the current value of this pref.</summary>
            public static implicit operator T(AutoPref<T> pref)
                => pref.Value;

            /************************************************************************************************************************/

            /// <summary>Checks if the value of this pref is equal to the specified `value`.</summary>
            public static bool operator ==(AutoPref<T> pref, T value)
                => Equals(pref.Value, value);

            /// <summary>Checks if the value of this pref is not equal to the specified `value`.</summary>
            public static bool operator !=(AutoPref<T> pref, T value)
                => !(pref == value);

            /************************************************************************************************************************/

            /// <summary>Reloads the value of this pref from the system.</summary>
            public void Reload()
            {
                _Value = Load();
                _IsLoaded = true;
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
            #region Utils
            /************************************************************************************************************************/

            /// <summary>Returns a hash code for this pref.</summary>
            public override int GetHashCode()
                => base.GetHashCode();

            /************************************************************************************************************************/

            /// <summary>Returns true if the preferences currently contains a saved value for this pref.</summary>
            public virtual bool IsSaved()
                => PlayerPrefs.HasKey(Key);

            /************************************************************************************************************************/

            /// <summary>Deletes the value of this pref from the preferences and reverts to the default value.</summary>
            public virtual void DeletePref()
            {
                PlayerPrefs.DeleteKey(Key);
                RevertToDefaultValue();
            }

            /// <summary>Sets the <see cref="Value"/> = <see cref="DefaultValue"/>.</summary>
            protected void RevertToDefaultValue()
                => Value = DefaultValue;

            /************************************************************************************************************************/

            /// <summary>Returns <c>Value?.ToString()</c>.</summary>
            public override string ToString()
                => Value?.ToString();

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>
        /// An <see cref="AutoPref{T}"/> which encapsulates a <see cref="bool"/> value stored in
        /// <see cref="PlayerPrefs"/>.
        /// </summary>
        public class Bool : AutoPref<bool>
        {
            /************************************************************************************************************************/

            /// <summary>Constructs a <see cref="Bool"/> pref with the specified `key` and `defaultValue`.</summary>
            public Bool(string key, bool defaultValue = default, Action<bool> onValueChanged = null)
                : base(key, defaultValue, onValueChanged)
            { }

            /// <summary>Loads the value of this pref from <see cref="PlayerPrefs"/>.</summary>
            protected override bool Load()
                => PlayerPrefs.GetInt(Key, DefaultValue ? 1 : 0) > 0;

            /// <summary>Saves the value of this pref to <see cref="PlayerPrefs"/>.</summary>
            protected override void Save()
                => PlayerPrefs.SetInt(Key, Value ? 1 : 0);

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="Bool"/> pref using the specified string as the key.</summary>
            public static implicit operator Bool(string key)
                => new Bool(key);

            /************************************************************************************************************************/

            /// <summary>Toggles the value of this pref from false to true or vice versa.</summary>
            public void Invert()
                => Value = !Value;

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>
        /// An <see cref="AutoPref{T}"/> which encapsulates a <see cref="float"/> value stored in
        /// <see cref="PlayerPrefs"/>.</summary>
        public class Float : AutoPref<float>
        {
            /************************************************************************************************************************/

            /// <summary>Constructs a <see cref="Float"/> pref with the specified `key` and `defaultValue`.</summary>
            public Float(string key, float defaultValue = default, Action<float> onValueChanged = null)
                : base(key, defaultValue, onValueChanged)
            { }

            /// <summary>Loads the value of this pref from <see cref="PlayerPrefs"/>.</summary>
            protected override float Load()
                => PlayerPrefs.GetFloat(Key, DefaultValue);

            /// <summary>Saves the value of this pref to <see cref="PlayerPrefs"/>.</summary>
            protected override void Save()
                => PlayerPrefs.SetFloat(Key, Value);

            /************************************************************************************************************************/

            /// <summary>Checks if the value of this pref is greater then the specified `value`.</summary>
            public static bool operator >(Float pref, float value)
                => pref.Value > value;

            /// <summary>Checks if the value of this pref is less then the specified `value`.</summary>
            public static bool operator <(Float pref, float value)
                => pref.Value < value;

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="Float"/> pref using the specified string as the key.</summary>
            public static implicit operator Float(string key)
                => new Float(key);

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>
        /// An <see cref="AutoPref{T}"/> which encapsulates an <see cref="int"/> value stored in
        /// <see cref="PlayerPrefs"/>.
        /// </summary>
        public class Int : AutoPref<int>
        {
            /************************************************************************************************************************/

            /// <summary>Constructs an <see cref="Int"/> pref with the specified `key` and `defaultValue`.</summary>
            public Int(string key, int defaultValue = default, Action<int> onValueChanged = null)
                : base(key, defaultValue, onValueChanged)
            { }

            /// <summary>Loads the value of this pref from <see cref="PlayerPrefs"/>.</summary>
            protected override int Load()
                => PlayerPrefs.GetInt(Key, DefaultValue);

            /// <summary>Saves the value of this pref to <see cref="PlayerPrefs"/>.</summary>
            protected override void Save()
                => PlayerPrefs.SetInt(Key, Value);

            /************************************************************************************************************************/

            /// <summary>Checks if the value of this pref is greater then the specified `value`.</summary>
            public static bool operator >(Int pref, int value)
                => pref.Value > value;

            /// <summary>Checks if the value of this pref is less then the specified `value`.</summary>
            public static bool operator <(Int pref, int value)
                => pref.Value < value;

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="Int"/> pref using the specified string as the key.</summary>
            public static implicit operator Int(string key)
                => new Int(key);

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>
        /// An <see cref="AutoPref{T}"/> which encapsulates a <see cref="string"/> value stored in
        /// <see cref="PlayerPrefs"/>.
        /// </summary>
        public class String : AutoPref<string>
        {
            /************************************************************************************************************************/

            /// <summary>Constructs a <see cref="String"/> pref with the specified `key` and `defaultValue`.</summary>
            public String(string key, string defaultValue = default, Action<string> onValueChanged = null)
                : base(key, defaultValue, onValueChanged)
            { }

            /// <summary>Loads the value of this pref from <see cref="PlayerPrefs"/>.</summary>
            protected override string Load()
                => PlayerPrefs.GetString(Key, DefaultValue);

            /// <summary>Saves the value of this pref to <see cref="PlayerPrefs"/>.</summary>
            protected override void Save()
                => PlayerPrefs.SetString(Key, Value);

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="String"/> pref using the specified string as the key.</summary>
            public static implicit operator String(string key)
                => new String(key);

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>
        /// An <see cref="AutoPref{T}"/> which encapsulates a <see cref="UnityEngine.Vector2"/> value stored in
        /// <see cref="PlayerPrefs"/>.
        /// </summary>
        public class Vector2 : AutoPref<UnityEngine.Vector2>
        {
            /************************************************************************************************************************/

            /// <summary>The key used to identify the x value of this pref.</summary>
            public string KeyX => Key;

            /// <summary>The key used to identify the y value of this pref.</summary>
            public readonly string KeyY;

            /************************************************************************************************************************/

            /// <summary>Constructs a <see cref="Vector2"/> pref with the specified `key` and `defaultValue`.</summary>
            public Vector2(string key,
                UnityEngine.Vector2 defaultValue = default,
                Action<UnityEngine.Vector2> onValueChanged = null)
                : base(key + "X", defaultValue, onValueChanged)
            {
                KeyY = key + "Y";
            }

            /// <summary>Loads the value of this pref from <see cref="PlayerPrefs"/>.</summary>
            protected override UnityEngine.Vector2 Load()
                => new UnityEngine.Vector2(
                PlayerPrefs.GetFloat(Key, DefaultValue.x),
                PlayerPrefs.GetFloat(KeyY, DefaultValue.y));

            /// <summary>Saves the value of this pref to <see cref="PlayerPrefs"/>.</summary>
            protected override void Save()
            {
                PlayerPrefs.SetFloat(Key, Value.x);
                PlayerPrefs.SetFloat(KeyY, Value.y);
            }

            /************************************************************************************************************************/

            /// <summary>Returns true if <see cref="PlayerPrefs"/> currently contains a value for this pref.</summary>
            public override bool IsSaved()
                => PlayerPrefs.HasKey(Key)
                && PlayerPrefs.HasKey(KeyY);

            /// <summary>Deletes the value of this pref from <see cref="PlayerPrefs"/> and reverts to the default value.</summary>
            public override void DeletePref()
            {
                PlayerPrefs.DeleteKey(Key);
                PlayerPrefs.DeleteKey(KeyY);
                RevertToDefaultValue();
            }

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="Vector2"/> pref using the specified string as the key.</summary>
            public static implicit operator Vector2(string key)
                => new Vector2(key);

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>
        /// An <see cref="AutoPref{T}"/> which encapsulates a <see cref="UnityEngine.Vector3"/> value stored in
        /// <see cref="PlayerPrefs"/>.
        /// </summary>
        public class Vector3 : AutoPref<UnityEngine.Vector3>
        {
            /************************************************************************************************************************/

            /// <summary>The key used to identify the x value of this pref.</summary>
            public string KeyX => Key;

            /// <summary>The key used to identify the y value of this pref.</summary>
            public readonly string KeyY;

            /// <summary>The key used to identify the z value of this pref.</summary>
            public readonly string KeyZ;

            /************************************************************************************************************************/

            /// <summary>Constructs a <see cref="Vector3"/> pref with the specified `key` and `defaultValue`.</summary>
            public Vector3(string key,
                UnityEngine.Vector3 defaultValue = default,
                Action<UnityEngine.Vector3> onValueChanged = null)
                : base(key + "X", defaultValue, onValueChanged)
            {
                KeyY = key + "Y";
                KeyZ = key + "Z";
            }

            /// <summary>Loads the value of this pref from <see cref="PlayerPrefs"/>.</summary>
            protected override UnityEngine.Vector3 Load()
                => new UnityEngine.Vector3(
                PlayerPrefs.GetFloat(Key, DefaultValue.x),
                PlayerPrefs.GetFloat(KeyY, DefaultValue.y),
                PlayerPrefs.GetFloat(KeyZ, DefaultValue.z));

            /// <summary>Saves the value of this pref to <see cref="PlayerPrefs"/>.</summary>
            protected override void Save()
            {
                PlayerPrefs.SetFloat(Key, Value.x);
                PlayerPrefs.SetFloat(KeyY, Value.y);
                PlayerPrefs.SetFloat(KeyZ, Value.z);
            }

            /************************************************************************************************************************/

            /// <summary>Returns true if <see cref="PlayerPrefs"/> currently contains a value for this pref.</summary>
            public override bool IsSaved()
                => PlayerPrefs.HasKey(Key)
                && PlayerPrefs.HasKey(KeyY)
                && PlayerPrefs.HasKey(KeyZ);

            /// <summary>Deletes the value of this pref from <see cref="PlayerPrefs"/> and reverts to the default value.</summary>
            public override void DeletePref()
            {
                PlayerPrefs.DeleteKey(Key);
                PlayerPrefs.DeleteKey(KeyY);
                PlayerPrefs.DeleteKey(KeyZ);
                RevertToDefaultValue();
            }

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="Vector3"/> pref using the specified string as the key.</summary>
            public static implicit operator Vector3(string key)
                => new Vector3(key);

            /// <summary>Returns a <see cref="Color"/> using the (x, y, z) of the pref as (r, g, b, a = 1).</summary>
            public static implicit operator Color(Vector3 pref)
            {
                var value = pref.Value;
                return new Color(value.x, value.y, value.z, 1);
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>
        /// An <see cref="AutoPref{T}"/> which encapsulates a <see cref="UnityEngine.Vector4"/> value stored in
        /// <see cref="PlayerPrefs"/>.
        /// </summary>
        public class Vector4 : AutoPref<UnityEngine.Vector4>
        {
            /************************************************************************************************************************/

            /// <summary>The key used to identify the x value of this pref.</summary>
            public string KeyX => Key;

            /// <summary>The key used to identify the y value of this pref.</summary>
            public readonly string KeyY;

            /// <summary>The key used to identify the z value of this pref.</summary>
            public readonly string KeyZ;

            /// <summary>The key used to identify the w value of this pref.</summary>
            public readonly string KeyW;

            /************************************************************************************************************************/

            /// <summary>Constructs a <see cref="Vector4"/> pref with the specified `key` and `defaultValue`.</summary>
            public Vector4(string key,
                UnityEngine.Vector4 defaultValue = default,
                Action<UnityEngine.Vector4> onValueChanged = null)
                : base(key + "X", defaultValue, onValueChanged)
            {
                KeyY = key + "Y";
                KeyZ = key + "Z";
                KeyW = key + "W";
            }

            /// <summary>Loads the value of this pref from <see cref="PlayerPrefs"/>.</summary>
            protected override UnityEngine.Vector4 Load()
                => new UnityEngine.Vector4(
                PlayerPrefs.GetFloat(Key, DefaultValue.x),
                PlayerPrefs.GetFloat(KeyY, DefaultValue.y),
                PlayerPrefs.GetFloat(KeyZ, DefaultValue.z),
                PlayerPrefs.GetFloat(KeyW, DefaultValue.w));

            /// <summary>Saves the value of this pref to <see cref="PlayerPrefs"/>.</summary>
            protected override void Save()
            {
                PlayerPrefs.SetFloat(Key, Value.x);
                PlayerPrefs.SetFloat(KeyY, Value.y);
                PlayerPrefs.SetFloat(KeyZ, Value.z);
                PlayerPrefs.SetFloat(KeyW, Value.w);
            }

            /************************************************************************************************************************/

            /// <summary>Returns true if <see cref="PlayerPrefs"/> currently contains a value for this pref.</summary>
            public override bool IsSaved()
                => PlayerPrefs.HasKey(Key)
                && PlayerPrefs.HasKey(KeyY)
                && PlayerPrefs.HasKey(KeyZ)
                && PlayerPrefs.HasKey(KeyW);

            /// <summary>Deletes the value of this pref from <see cref="PlayerPrefs"/> and reverts to the default value.</summary>
            public override void DeletePref()
            {
                PlayerPrefs.DeleteKey(Key);
                PlayerPrefs.DeleteKey(KeyY);
                PlayerPrefs.DeleteKey(KeyZ);
                PlayerPrefs.DeleteKey(KeyW);
                RevertToDefaultValue();
            }

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="Vector4"/> pref using the specified string as the key.</summary>
            public static implicit operator Vector4(string key)
                => new Vector4(key);

            /// <summary>Returns a <see cref="Color"/> using the (x, y, z, w) of the pref as (r, g, b, a).</summary>
            public static implicit operator Color(Vector4 pref)
            {
                var value = pref.Value;
                return new Color(value.x, value.y, value.z, value.w);
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>
        /// An <see cref="AutoPref{T}"/> which <see cref="UnityEngine.Quaternion"/> value stored in
        /// <see cref="PlayerPrefs"/>.
        /// </summary>
        public class Quaternion : AutoPref<UnityEngine.Quaternion>
        {
            /************************************************************************************************************************/

            /// <summary>The key used to identify the x value of this pref.</summary>
            public string KeyX => Key;

            /// <summary>The key used to identify the y value of this pref.</summary>
            public readonly string KeyY;

            /// <summary>The key used to identify the z value of this pref.</summary>
            public readonly string KeyZ;

            /// <summary>The key used to identify the w value of this pref.</summary>
            public readonly string KeyW;

            /************************************************************************************************************************/

            /// <summary>Constructs a <see cref="Quaternion"/> pref with the specified `key` and `defaultValue`.</summary>
            public Quaternion(string key,
                UnityEngine.Quaternion defaultValue = default,
                Action<UnityEngine.Quaternion> onValueChanged = null)
                : base(key + "X", defaultValue, onValueChanged)
            {
                KeyY = key + "Y";
                KeyZ = key + "Z";
                KeyW = key + "W";
            }

            /// <summary>Loads the value of this pref from <see cref="PlayerPrefs"/>.</summary>
            protected override UnityEngine.Quaternion Load()
                => new UnityEngine.Quaternion(
                PlayerPrefs.GetFloat(Key, DefaultValue.x),
                PlayerPrefs.GetFloat(KeyY, DefaultValue.y),
                PlayerPrefs.GetFloat(KeyZ, DefaultValue.z),
                PlayerPrefs.GetFloat(KeyW, DefaultValue.w));

            /// <summary>Saves the value of this pref to <see cref="PlayerPrefs"/>.</summary>
            protected override void Save()
            {
                PlayerPrefs.SetFloat(Key, Value.x);
                PlayerPrefs.SetFloat(KeyY, Value.y);
                PlayerPrefs.SetFloat(KeyZ, Value.z);
                PlayerPrefs.SetFloat(KeyW, Value.w);
            }

            /************************************************************************************************************************/

            /// <summary>Returns true if <see cref="PlayerPrefs"/> currently contains a value for this pref.</summary>
            public override bool IsSaved()
                => PlayerPrefs.HasKey(Key)
                && PlayerPrefs.HasKey(KeyY)
                && PlayerPrefs.HasKey(KeyZ)
                && PlayerPrefs.HasKey(KeyW);

            /// <summary>Deletes the value of this pref from <see cref="PlayerPrefs"/> and reverts to the default value.</summary>
            public override void DeletePref()
            {
                PlayerPrefs.DeleteKey(Key);
                PlayerPrefs.DeleteKey(KeyY);
                PlayerPrefs.DeleteKey(KeyZ);
                PlayerPrefs.DeleteKey(KeyW);
                RevertToDefaultValue();
            }

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="Quaternion"/> pref using the specified string as the key.</summary>
            public static implicit operator Quaternion(string key)
                => new Quaternion(key);

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
    }
}

