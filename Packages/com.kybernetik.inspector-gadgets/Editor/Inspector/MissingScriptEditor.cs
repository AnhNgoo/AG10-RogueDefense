// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InspectorGadgets.Editor
{
    /// <summary>[Editor-Only]
    /// A custom Inspector for <see cref="MonoBehaviour"/> but not its children, so it should only ever get used on
    /// missing scripts.
    /// </summary>
#if !DISABLE_IG_MONOBEHAVIOUR_INSPECTOR
    [CustomEditor(typeof(MonoBehaviour), isFallback = true)]
#endif
    public class MissingScriptEditor : UnityEditor.Editor
    {
        /************************************************************************************************************************/

        [NonSerialized]
        private SerializedProperty _ScriptProperty;

        /// <summary>The target's "m_Script" property.</summary>
        protected SerializedProperty ScriptProperty
        {
            get
            {
                GatherSerializedProperties();
                return _ScriptProperty;
            }
        }

        /************************************************************************************************************************/

        [NonSerialized]
        private List<SerializedProperty> _OtherProperties;

        /// <summary>All of the target's properties other than "m_Script".</summary>
        protected List<SerializedProperty> OtherProperties
        {
            get
            {
                GatherSerializedProperties();
                return _OtherProperties;
            }
        }

        /************************************************************************************************************************/
        #region Property Gathering and Analysis
        /************************************************************************************************************************/

        /// <summary>
        /// Gathers the target's properties. If its script is missing, this method tries to find other similar scripts.
        /// </summary>
        protected virtual void OnEnable()
        {
            if (target != null ||
                MissingScriptWindow.Instance == null ||
                ScriptProperty == null)
                return;

            if (MissingScriptWindow.Instance.AutoDestroy)
            {
                DestroyProperly(targets, false);
                return;
            }

            if (MissingScriptWindow.Instance.HasTarget)
                return;

            // Get the referenced script's name (if possible).
            var scriptReference = _ScriptProperty.objectReferenceValue;
            var scriptName = scriptReference != null ? scriptReference.name : null;

            var fields = new List<FieldInfo>();

            var scripts = MissingScriptWindow.AllScripts;
            var similarityRatings = new int[scripts.Length];

            // Go through each script and calculate its similarity to the missing script.
            for (int i = 0; i < scripts.Length; i++)
            {
                var script = scripts[i];
                var scriptType = script.GetClass();
                if (scriptType == null)
                    continue;

                // Scripts with the same name have max similarity.
                if (script.name == scriptName)
                {
                    similarityRatings[i] = int.MaxValue;
                    continue;
                }

                // Otherwise count the number of fields with names and types matching the serialized properties.
                fields.Clear();
                GatherAllFields(scriptType, fields);
                similarityRatings[i] = CountSimilarFields(fields);
            }

            var hasSerializedProperties = scriptName != null || _OtherProperties.Count > 0;
            MissingScriptWindow.Instance.SetScripts(target, _ScriptProperty, hasSerializedProperties, similarityRatings);
        }

        /************************************************************************************************************************/

        private Object[] _GatheredPropertiesFrom;

        private void GatherSerializedProperties()
        {
            var targets = this.targets;

            if (_OtherProperties != null)
            {
                if (_GatheredPropertiesFrom == null ||
                    _GatheredPropertiesFrom.Length != targets.Length)
                    goto Gather;

                for (int i = 0; i < _GatheredPropertiesFrom.Length; i++)
                    if (!ReferenceEquals(_GatheredPropertiesFrom[i], targets[i]))
                        goto Gather;

                return;
            }

            Gather:

            if (_GatheredPropertiesFrom == null || _GatheredPropertiesFrom.Length != targets.Length)
                _GatheredPropertiesFrom = new Object[targets.Length];

            for (int i = 0; i < _GatheredPropertiesFrom.Length; i++)
            {
                _GatheredPropertiesFrom[i] = targets[i];
            }

            _OtherProperties = new List<SerializedProperty>();

            try
            {
                var iterator = serializedObject.GetIterator();

                if (!iterator.NextVisible(true))
                    return;

                do
                {
                    var property = iterator.Copy();
                    if (property.propertyPath == "m_Script")
                        _ScriptProperty = property;
                    else
                        _OtherProperties.Add(property);
                }
                while (iterator.NextVisible(false));
            }
            catch { }

            if (_ScriptProperty == null && target is MonoBehaviour)
                Debug.Log("Script property was not found on " + target, target);
        }

        /************************************************************************************************************************/

        private static void GatherAllFields(Type type, List<FieldInfo> fields)
        {
            var typeFields = type.GetFields(IGEditorUtils.InstanceBindings);
            for (int i = 0; i < typeFields.Length; i++)
            {
                var field = typeFields[i];
                if (field.IsDefined(typeof(NonSerializedAttribute), true) ||
                    field.IsDefined(typeof(HideInInspector), true))
                    continue;

                if (!field.IsPublic && !field.IsDefined(typeof(SerializeField), true))
                    continue;

                fields.Add(field);
            }
        }

        /************************************************************************************************************************/

        private int CountSimilarFields(List<FieldInfo> fields)
        {
            var count = 0;

            for (int i = 0; i < _OtherProperties.Count; i++)
            {
                var property = _OtherProperties[i];

                for (int j = 0; j < fields.Count; j++)
                {
                    var field = fields[j];

                    if (IsMatch(property, field))
                    {
                        count++;
                        break;
                    }
                }
            }

            return count;
        }

        /************************************************************************************************************************/

        private static bool IsMatch(SerializedProperty property, FieldInfo field)
        {
            if (field.Name != property.propertyPath)
                return false;

            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer: return typeof(int).IsAssignableFrom(field.FieldType) || typeof(uint).IsAssignableFrom(field.FieldType);
                case SerializedPropertyType.Boolean: return field.FieldType == typeof(bool);
                case SerializedPropertyType.Float: return typeof(float).IsAssignableFrom(field.FieldType);
                case SerializedPropertyType.String: return field.FieldType == typeof(string);
                case SerializedPropertyType.Color: return field.FieldType == typeof(Color) || field.FieldType == typeof(Color32);
                case SerializedPropertyType.ObjectReference: return typeof(Object).IsAssignableFrom(field.FieldType);
                case SerializedPropertyType.LayerMask: return field.FieldType == typeof(LayerMask);
                case SerializedPropertyType.Enum: return field.FieldType.IsEnum;// This doesn't confirm that the enum type actually matches the field. Would need to check property.enumDisplayNames.
                case SerializedPropertyType.Vector2: return field.FieldType == typeof(Vector2);
                case SerializedPropertyType.Vector3: return field.FieldType == typeof(Vector3);
                case SerializedPropertyType.Vector4: return field.FieldType == typeof(Vector4);
                case SerializedPropertyType.Rect: return field.FieldType == typeof(Rect);
                case SerializedPropertyType.Generic: return field.FieldType.HasElementType || typeof(IEnumerable).IsAssignableFrom(field.FieldType);
                case SerializedPropertyType.Character: return field.FieldType == typeof(char);
                case SerializedPropertyType.AnimationCurve: return field.FieldType == typeof(AnimationCurve);
                case SerializedPropertyType.Bounds: return field.FieldType == typeof(Bounds);
                case SerializedPropertyType.Gradient: return field.FieldType == typeof(Gradient);
                case SerializedPropertyType.Quaternion: return field.FieldType == typeof(Quaternion);
                case SerializedPropertyType.Vector2Int: return field.FieldType == typeof(Vector2Int);
                case SerializedPropertyType.Vector3Int: return field.FieldType == typeof(Vector3Int);
                case SerializedPropertyType.RectInt: return field.FieldType == typeof(RectInt);
                case SerializedPropertyType.BoundsInt: return field.FieldType == typeof(BoundsInt);

                default:
                case SerializedPropertyType.ArraySize:
                case SerializedPropertyType.ExposedReference:
                case SerializedPropertyType.FixedBufferSize:
                    break;
            }

            return false;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/

        /// <summary>
        /// Draws the target's inspector with a message indicating that the script is missing.
        /// </summary>
        public override void OnInspectorGUI()
        {
            if (target != null)
            {
                base.OnInspectorGUI();
                return;
            }

            serializedObject.Update();

            // Draw the script field and a help box.

            GUI.enabled = true;

            EditorGUILayout.PropertyField(ScriptProperty, true);

            EditorGUILayout.HelpBox(EditorUtility.scriptCompilationFailed ?
                "The associated script can not be loaded. Please fix any compile errors and assign a valid script." :
                "The associated script can not be loaded. Please assign a valid script.", MessageType.Warning);

            // Draw all other serialized properties disabled.
            GUI.enabled = false;
            for (int i = 0; i < _OtherProperties.Count; i++)
            {
                EditorGUILayout.PropertyField(_OtherProperties[i], true);
            }
            GUI.enabled = true;

            // Apply modifications in case the user dragged a new script into the script field.
            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button(target is Component ? "Remove Component" : "Destroy This"))
            {
                EditorApplication.delayCall += () => DestroyProperly(targets, true);
            }

            if (GUILayout.Button("Find and Fix Missing Scripts"))
            {
                EditorWindow.GetWindow<MissingScriptWindow>();
                OnEnable();
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Indicates to the <see cref="MissingScriptWindow"/> that the target has been deselected.
        /// </summary>
        protected virtual void OnDisable()
        {
            if (MissingScriptWindow.Instance != null)
                MissingScriptWindow.Instance.ClearTarget();
        }

        /************************************************************************************************************************/

        /// <summary>Destroys the `targets`. Shows a confirmation dialog before destroying assets.</summary>
        public static void DestroyProperly(Object[] targets, bool confirmAssetDeletion)
        {
            for (int i = 0; i < targets.Length; i++)
                DestroyProperly(targets[i], confirmAssetDeletion);
        }

        /// <summary>Destroys the `target`. Shows a confirmation dialog before destroying assets.</summary>
        public static void DestroyProperly(Object target, bool confirmAssetDeletion)
        {
            var assetPath = AssetDatabase.GetAssetPath(target);
            if (!string.IsNullOrEmpty(assetPath))
            {
                var isMainAsset = AssetDatabase.IsMainAsset(target);
                var message = isMainAsset ?
                    assetPath :
                    $"{target.name} (sub asset of '{assetPath}')";

                message += "\n\nYou cannot undo this action.";

                if (!confirmAssetDeletion || EditorUtility.DisplayDialog("Delete selected asset?", message, "Delete", "Cancel"))
                {
                    if (isMainAsset)
                    {
                        AssetDatabase.DeleteAsset(assetPath);
                    }
                    else
                    {
                        DestroyImmediate(target, true);
                        AssetDatabase.ImportAsset(assetPath);
                    }
                }
            }
            else
            {
                Undo.DestroyObjectImmediate(target);
            }

            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        /************************************************************************************************************************/
    }
}

#endif
