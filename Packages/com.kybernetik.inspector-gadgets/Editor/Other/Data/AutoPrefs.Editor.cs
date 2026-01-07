// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using System;
using UnityEngine;
using UnityEditor;

namespace InspectorGadgets.Editor
{
    /// <summary>[Editor-Only]
    /// A collection of wrappers for EditorPrefs which simplify the way you can store and retrieve values.
    /// </summary>
    /// <remarks>PlayerPrefs versions can be found in <see cref="InspectorGadgets.AutoPrefs"/></remarks>
    public static class AutoPrefs
    {
        /************************************************************************************************************************/

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            InspectorGadgets.AutoPrefs.OnAnyValueChanged +=
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews;
        }

        /************************************************************************************************************************/
        #region GUI
        /************************************************************************************************************************/

        /// <summary>[Editor-Only] A delegate used to draw a GUI field and return its value.</summary>
        public delegate T GUIFieldMethod<T>(Rect area, GUIContent label, T value, GUIStyle style);

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Draws GUI controls for this pref and returns true if the value was changed.</summary>
        public static bool DoGUI<T>(
            this InspectorGadgets.AutoPrefs.AutoPref<T> pref,
            Rect area,
            GUIContent label,
            GUIStyle style,
            GUIFieldMethod<T> doGUIField)
        {
            var resetStyle = InternalGUI.MiniSquareButtonStyle;

            var isDefault = Equals(pref.Value, pref.DefaultValue);
            if (!isDefault)
                area.width -= resetStyle.fixedWidth;

            EditorGUI.BeginChangeCheck();
            var value = doGUIField(area, label, pref.Value, style);
            if (EditorGUI.EndChangeCheck())
            {
                pref.Value = value;
                return true;
            }

            if (!isDefault)
            {
                var resetPosition = new Rect(area.xMax, area.y, resetStyle.fixedWidth, resetStyle.fixedHeight);

                if (GUI.Button(resetPosition, EditorStrings.GUI.Reset, resetStyle))
                {
                    pref.Value = pref.DefaultValue;
                    return true;
                }
            }

            return false;
        }

        /// <summary>[Editor-Only] Draws GUI controls for this pref and returns true if the value was changed.</summary>
        public static bool DoGUI<T>(
            this InspectorGadgets.AutoPrefs.AutoPref<T> pref,
            GUIContent label,
            GUIStyle style,
            GUIFieldMethod<T> doGUIField)
            => pref.DoGUI(GetControlRect(), label, style, doGUIField);

        /// <summary>[Editor-Only] Draws GUI controls for this pref and returns true if the value was changed.</summary>
        public static bool DoGUI<T>(
            this InspectorGadgets.AutoPrefs.AutoPref<T> pref,
            GUIContent label,
            GUIFieldMethod<T> doGUIField)
            => pref.DoGUI(GetControlRect(), label, null, doGUIField);

        /************************************************************************************************************************/

        /// <summary>
        /// Uses <see cref="EditorGUILayout.GetControlRect(bool, float, GUIStyle, GUILayoutOption[])"/>
        /// to allocate a <see cref="GUILayout"/> <see cref="Rect"/> for a control.
        /// </summary>
        public static Rect GetControlRect()
            => EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);

        /************************************************************************************************************************/

        /// <summary>Draws a GUI field for an <see cref="InspectorGadgets.AutoPrefs.Bool"/>.</summary>
        public static bool DoGUI(this InspectorGadgets.AutoPrefs.Bool pref, GUIContent label)
            => pref.DoGUI(label, EditorStyles.toggle, EditorGUI.Toggle);

        /// <summary>Draws a GUI field for an <see cref="InspectorGadgets.AutoPrefs.Float"/>.</summary>
        public static bool DoGUI(this InspectorGadgets.AutoPrefs.Float pref, GUIContent label)
            => pref.DoGUI(label, EditorStyles.numberField, EditorGUI.FloatField);

        /// <summary>Draws a GUI field for an <see cref="InspectorGadgets.AutoPrefs.Int"/>.</summary>
        public static bool DoGUI(this InspectorGadgets.AutoPrefs.Int pref, GUIContent label)
            => pref.DoGUI(label, EditorStyles.numberField, EditorGUI.IntField);

        /************************************************************************************************************************/

        /// <summary>Draws a GUI Color field for an <see cref="InspectorGadgets.AutoPrefs.Vector4"/>.</summary>
        public static bool DoColorGUI(this InspectorGadgets.AutoPrefs.Vector4 pref, GUIContent label)
            => pref.DoGUI(label,
                (area, content, value, style) => EditorGUI.ColorField(area, content, value.ToColor()).ToVector4());

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// An <see cref="InspectorGadgets.AutoPrefs.AutoPref{T}"/> which encapsulates a <see cref="bool"/> value
        /// stored in <see cref="EditorPrefs"/>.
        /// </summary>
        public class EditorBool : InspectorGadgets.AutoPrefs.Bool
        {
            /************************************************************************************************************************/

            /// <summary>Constructs an <see cref="EditorBool"/> pref with the specified `key` and `defaultValue`.</summary>
            public EditorBool(string key, bool defaultValue = default, Action<bool> onValueChanged = null)
                : base(key, defaultValue, onValueChanged)
            { }

            /// <summary>Loads the value of this pref from <see cref="EditorPrefs"/>.</summary>
            protected override bool Load()
                => EditorPrefs.GetBool(Key, DefaultValue);

            /// <summary>Saves the value of this pref to <see cref="EditorPrefs"/>.</summary>
            protected override void Save()
                => EditorPrefs.SetBool(Key, Value);

            /************************************************************************************************************************/

            /// <summary>Returns true if <see cref="EditorPrefs"/> currently contains a value for this pref.</summary>
            public override bool IsSaved()
                => EditorPrefs.HasKey(Key);

            /// <summary>Deletes the value of this pref from <see cref="EditorPrefs"/> and reverts to the default value.</summary>
            public override void DeletePref()
            {
                EditorPrefs.DeleteKey(Key);
                RevertToDefaultValue();
            }

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="EditorBool"/> pref using the specified string as the key.</summary>
            public static implicit operator EditorBool(string key)
                => new EditorBool(key);

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// An <see cref="InspectorGadgets.AutoPrefs.AutoPref{T}"/> which encapsulates a <see cref="float"/> value stored in
        /// <see cref="EditorPrefs"/>.
        /// </summary>
        public class EditorFloat : InspectorGadgets.AutoPrefs.Float
        {
            /************************************************************************************************************************/

            /// <summary>Constructs an <see cref="EditorFloat"/> pref with the specified `key` and `defaultValue`.</summary>
            public EditorFloat(string key, float defaultValue = default, Action<float> onValueChanged = null)
                : base(key, defaultValue, onValueChanged)
            { }

            /// <summary>Loads the value of this pref from <see cref="EditorPrefs"/>.</summary>
            protected override float Load()
                => EditorPrefs.GetFloat(Key, DefaultValue);

            /// <summary>Saves the value of this pref to <see cref="EditorPrefs"/>.</summary>
            protected override void Save()
                => EditorPrefs.SetFloat(Key, Value);

            /************************************************************************************************************************/

            /// <summary>Returns true if <see cref="EditorPrefs"/> currently contains a value for this pref.</summary>
            public override bool IsSaved()
                => EditorPrefs.HasKey(Key);

            /// <summary>Deletes the value of this pref from <see cref="EditorPrefs"/> and reverts to the default value.</summary>
            public override void DeletePref()
            {
                EditorPrefs.DeleteKey(Key);
                RevertToDefaultValue();
            }

            /************************************************************************************************************************/
            #region Operators
            /************************************************************************************************************************/

            /// <summary>Checks if the value of this pref is greater then the specified `value`.</summary>
            public static bool operator >(EditorFloat pref, float value)
                => pref.Value > value;

            /// <summary>Checks if the value of this pref is less then the specified `value`.</summary>
            public static bool operator <(EditorFloat pref, float value)
                => pref.Value < value;

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="EditorFloat"/> pref using the specified string as the key.</summary>
            public static implicit operator EditorFloat(string key)
                => new EditorFloat(key);

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// An <see cref="InspectorGadgets.AutoPrefs.AutoPref{T}"/> which encapsulates an <see cref="int"/> value stored in
        /// <see cref="EditorPrefs"/>.
        /// </summary>
        public class EditorInt : InspectorGadgets.AutoPrefs.Int
        {
            /************************************************************************************************************************/

            /// <summary>Constructs an <see cref="EditorInt"/> pref with the specified `key` and `defaultValue`.</summary>
            public EditorInt(string key, int defaultValue = default, Action<int> onValueChanged = null)
                : base(key, defaultValue, onValueChanged)
            { }

            /// <summary>Loads the value of this pref from <see cref="EditorPrefs"/>.</summary>
            protected override int Load()
                => EditorPrefs.GetInt(Key, DefaultValue);

            /// <summary>Saves the value of this pref to <see cref="EditorPrefs"/>.</summary>
            protected override void Save()
                => EditorPrefs.SetInt(Key, Value);

            /************************************************************************************************************************/

            /// <summary>Returns true if <see cref="EditorPrefs"/> currently contains a value for this pref.</summary>
            public override bool IsSaved()
                => EditorPrefs.HasKey(Key);

            /// <summary>Deletes the value of this pref from <see cref="EditorPrefs"/> and reverts to the default value.</summary>
            public override void DeletePref()
            {
                EditorPrefs.DeleteKey(Key);
                RevertToDefaultValue();
            }

            /************************************************************************************************************************/
            #region Operators
            /************************************************************************************************************************/

            /// <summary>Checks if the value of this pref is greater then the specified `value`.</summary>
            public static bool operator >(EditorInt pref, int value)
                => pref.Value > value;

            /// <summary>Checks if the value of this pref is less then the specified `value`.</summary>
            public static bool operator <(EditorInt pref, int value)
                => pref.Value < value;

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="EditorInt"/> pref using the specified string as the key.</summary>
            public static implicit operator EditorInt(string key)
                => new EditorInt(key);

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// An <see cref="InspectorGadgets.AutoPrefs.AutoPref{T}"/> which encapsulates a <see cref="string"/> value stored in
        /// <see cref="EditorPrefs"/>.
        /// </summary>
        public class EditorString : InspectorGadgets.AutoPrefs.String
        {
            /************************************************************************************************************************/

            /// <summary>Constructs an <see cref="EditorString"/> pref with the specified `key` and `defaultValue`.</summary>
            public EditorString(string key, string defaultValue = default, Action<string> onValueChanged = null)
                : base(key, defaultValue, onValueChanged)
            { }

            /// <summary>Loads the value of this pref from <see cref="EditorPrefs"/>.</summary>
            protected override string Load()
                => EditorPrefs.GetString(Key, DefaultValue);

            /// <summary>Saves the value of this pref to <see cref="EditorPrefs"/>.</summary>
            protected override void Save()
                => EditorPrefs.SetString(Key, Value);

            /************************************************************************************************************************/

            /// <summary>Returns true if <see cref="EditorPrefs"/> currently contains a value for this pref.</summary>
            public override bool IsSaved()
                => EditorPrefs.HasKey(Key);

            /// <summary>Deletes the value of this pref from <see cref="EditorPrefs"/> and reverts to the default value.</summary>
            public override void DeletePref()
            {
                EditorPrefs.DeleteKey(Key);
                RevertToDefaultValue();
            }

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="EditorString"/> pref using the specified string as the key.</summary>
            public static implicit operator EditorString(string key)
                => new EditorString(key);

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// An <see cref="InspectorGadgets.AutoPrefs.AutoPref{T}"/> which encapsulates a <see cref="Vector2"/> value stored in
        /// <see cref="EditorPrefs"/>.
        /// </summary>
        public class EditorVector2 : InspectorGadgets.AutoPrefs.Vector2
        {
            /************************************************************************************************************************/

            /// <summary>Constructs an <see cref="EditorString"/> pref with the specified `key` and `defaultValue`.</summary>
            public EditorVector2(string key,
                Vector2 defaultValue = default,
                Action<Vector2> onValueChanged = null)
                : base(key, defaultValue, onValueChanged)
            { }

            /// <summary>Loads the value of this pref from <see cref="EditorPrefs"/>.</summary>
            protected override Vector2 Load()
                => new Vector2(
                EditorPrefs.GetFloat(Key, DefaultValue.x),
                EditorPrefs.GetFloat(KeyY, DefaultValue.y));

            /// <summary>Saves the value of this pref to <see cref="EditorPrefs"/>.</summary>
            protected override void Save()
            {
                EditorPrefs.SetFloat(Key, Value.x);
                EditorPrefs.SetFloat(KeyY, Value.y);
            }

            /************************************************************************************************************************/

            /// <summary>Returns true if <see cref="EditorPrefs"/> currently contains a value for this pref.</summary>
            public override bool IsSaved()
                => EditorPrefs.HasKey(Key)
                && EditorPrefs.HasKey(KeyY);

            /// <summary>Deletes the value of this pref from <see cref="EditorPrefs"/> and reverts to the default value.</summary>
            public override void DeletePref()
            {
                EditorPrefs.DeleteKey(Key);
                EditorPrefs.DeleteKey(KeyY);
                RevertToDefaultValue();
            }

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="EditorVector2"/> pref using the specified string as the key.</summary>
            public static implicit operator EditorVector2(string key)
                => new EditorVector2(key);

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// An <see cref="InspectorGadgets.AutoPrefs.AutoPref{T}"/> which encapsulates a <see cref="Vector3"/> value stored in
        /// <see cref="EditorPrefs"/>.
        /// </summary>
        public class EditorVector3 : InspectorGadgets.AutoPrefs.Vector3
        {
            /************************************************************************************************************************/

            /// <summary>Constructs an <see cref="EditorVector3"/> pref.</summary>
            public EditorVector3(string key,
                Vector3 defaultValue = default,
                Action<Vector3> onValueChanged = null)
                : base(key, defaultValue, onValueChanged)
            { }

            /// <summary>Loads the value of this pref from <see cref="EditorPrefs"/>.</summary>
            protected override Vector3 Load()
                => new Vector3(
                EditorPrefs.GetFloat(Key, DefaultValue.x),
                EditorPrefs.GetFloat(KeyY, DefaultValue.y),
                EditorPrefs.GetFloat(KeyZ, DefaultValue.z));

            /// <summary>Saves the value of this pref to <see cref="EditorPrefs"/>.</summary>
            protected override void Save()
            {
                EditorPrefs.SetFloat(Key, Value.x);
                EditorPrefs.SetFloat(KeyY, Value.y);
                EditorPrefs.SetFloat(KeyZ, Value.z);
            }

            /************************************************************************************************************************/

            /// <summary>Returns true if <see cref="EditorPrefs"/> currently contains a value for this pref.</summary>
            public override bool IsSaved()
                => EditorPrefs.HasKey(Key)
                && EditorPrefs.HasKey(KeyY)
                && EditorPrefs.HasKey(KeyZ);

            /// <summary>Deletes the value of this pref from <see cref="EditorPrefs"/> and reverts to the default value.</summary>
            public override void DeletePref()
            {
                EditorPrefs.DeleteKey(Key);
                EditorPrefs.DeleteKey(KeyY);
                EditorPrefs.DeleteKey(KeyZ);
                RevertToDefaultValue();
            }

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="EditorVector3"/> pref using the specified string as the key.</summary>
            public static implicit operator EditorVector3(string key)
                => new EditorVector3(key);

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// An <see cref="InspectorGadgets.AutoPrefs.AutoPref{T}"/> which encapsulates a <see cref="Vector4"/> value stored in
        /// <see cref="EditorPrefs"/>.
        /// </summary>
        public class EditorVector4 : InspectorGadgets.AutoPrefs.Vector4
        {
            /************************************************************************************************************************/

            /// <summary>Constructs an <see cref="EditorVector4"/> pref.</summary>
            public EditorVector4(string key,
                Vector4 defaultValue = default,
                Action<Vector4> onValueChanged = null)
                : base(key, defaultValue, onValueChanged)
            { }

            /// <summary>Loads the value of this pref from <see cref="EditorPrefs"/>.</summary>
            protected override Vector4 Load()
                => new Vector4(
                EditorPrefs.GetFloat(Key, DefaultValue.x),
                EditorPrefs.GetFloat(KeyY, DefaultValue.y),
                EditorPrefs.GetFloat(KeyZ, DefaultValue.z),
                EditorPrefs.GetFloat(KeyW, DefaultValue.w));

            /// <summary>Saves the value of this pref to <see cref="EditorPrefs"/>.</summary>
            protected override void Save()
            {
                EditorPrefs.SetFloat(Key, Value.x);
                EditorPrefs.SetFloat(KeyY, Value.y);
                EditorPrefs.SetFloat(KeyZ, Value.z);
                EditorPrefs.SetFloat(KeyW, Value.w);
            }

            /************************************************************************************************************************/

            /// <summary>Returns true if <see cref="EditorPrefs"/> currently contains a value for this pref.</summary>
            public override bool IsSaved()
                => EditorPrefs.HasKey(Key)
                && EditorPrefs.HasKey(KeyY)
                && EditorPrefs.HasKey(KeyZ)
                && EditorPrefs.HasKey(KeyW);

            /// <summary>Deletes the value of this pref from <see cref="EditorPrefs"/> and reverts to the default value.</summary>
            public override void DeletePref()
            {
                EditorPrefs.DeleteKey(Key);
                EditorPrefs.DeleteKey(KeyY);
                EditorPrefs.DeleteKey(KeyZ);
                EditorPrefs.DeleteKey(KeyW);
                RevertToDefaultValue();
            }

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="EditorVector4"/> pref using the specified string as the key.</summary>
            public static implicit operator EditorVector4(string key)
                => new EditorVector4(key);

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// An <see cref="InspectorGadgets.AutoPrefs.AutoPref{T}"/> which encapsulates a <see cref="Quaternion"/> value stored in
        /// <see cref="EditorPrefs"/>.
        /// </summary>
        public class EditorQuaternion : InspectorGadgets.AutoPrefs.Quaternion
        {
            /************************************************************************************************************************/

            /// <summary>Constructs a <see cref="Quaternion"/> pref with the specified `key` and `defaultValue`.</summary>
            public EditorQuaternion(string key,
                Quaternion defaultValue = default,
                Action<Quaternion> onValueChanged = null)
                : base(key, defaultValue, onValueChanged)
            { }

            /// <summary>Constructs a <see cref="Quaternion"/> pref with the specified `key` and <see cref="Quaternion.identity"/> as the default value.</summary>
            public EditorQuaternion(string key)
                : base(key, Quaternion.identity)
            { }

            /// <summary>Loads the value of this pref from <see cref="EditorPrefs"/>.</summary>
            protected override Quaternion Load()
                => new Quaternion(
                EditorPrefs.GetFloat(Key, DefaultValue.x),
                EditorPrefs.GetFloat(KeyY, DefaultValue.y),
                EditorPrefs.GetFloat(KeyZ, DefaultValue.z),
                EditorPrefs.GetFloat(KeyW, DefaultValue.w));

            /// <summary>Saves the value of this pref to <see cref="EditorPrefs"/>.</summary>
            protected override void Save()
            {
                EditorPrefs.SetFloat(Key, Value.x);
                EditorPrefs.SetFloat(KeyY, Value.y);
                EditorPrefs.SetFloat(KeyZ, Value.z);
                EditorPrefs.SetFloat(KeyW, Value.w);
            }

            /************************************************************************************************************************/

            /// <summary>Returns true if <see cref="EditorPrefs"/> currently contains a value for this pref.</summary>
            public override bool IsSaved()
                => EditorPrefs.HasKey(Key)
                && EditorPrefs.HasKey(KeyY)
                && EditorPrefs.HasKey(KeyZ)
                && EditorPrefs.HasKey(KeyW);

            /// <summary>Deletes the value of this pref from <see cref="EditorPrefs"/> and reverts to the default value.</summary>
            public override void DeletePref()
            {
                EditorPrefs.DeleteKey(Key);
                EditorPrefs.DeleteKey(KeyY);
                EditorPrefs.DeleteKey(KeyZ);
                EditorPrefs.DeleteKey(KeyW);
                RevertToDefaultValue();
            }

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="EditorQuaternion"/> pref using the specified string as the key.</summary>
            public static implicit operator EditorQuaternion(string key)
                => new EditorQuaternion(key);

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
    }
}

#endif

