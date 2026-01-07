// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InspectorGadgets.Editor
{
    /// <summary>[Pro-Only] [Editor-Only] Manages values that need to persist after leaving Play Mode.</summary>
    public static class PersistentValues
    {
        /************************************************************************************************************************/

        public enum Operation
        {
            Toggle,
            Add,
            Remove,
        }

        /************************************************************************************************************************/

        private static readonly Dictionary<Object, Serialization.ObjectReference>
            Objects = new Dictionary<Object, Serialization.ObjectReference>();
        private static readonly List<Serialization.PropertyReference>
            Properties = new List<Serialization.PropertyReference>();

        private static bool _IsInitialized;

        /************************************************************************************************************************/

        private static List<Object> _ObjectsToPersist;

        [MenuItem(EditorStrings.PersistAfterPlayModeComponent, priority = 510)]// Group just after Unity's Paste commands.
        private static void PersistComponent(MenuCommand command)
        {
            IGEditorUtils.GroupedInvoke(command, (context) =>
            {
                PersistObjects(Operation.Toggle, context.ToArray());
            });
        }

        [MenuItem(EditorStrings.PersistAfterPlayModeComponent, validate = true)]
        private static bool ValidatePersistMethod()
        {
            return EditorApplication.isPlayingOrWillChangePlaymode;
        }

        /************************************************************************************************************************/

        public static bool WillPersist(Object obj)
        {
            if (!_IsInitialized)
                return false;

            return Objects.ContainsKey(obj);
        }

        /************************************************************************************************************************/

        public static void PersistObjects(Operation operation, params Object[] objects)
        {
            Initialize();

            bool isWatching = false;

            for (int i = 0; i < objects.Length; i++)
            {
                var obj = objects[i];

                if (operation == Operation.Add ||
                    (operation == Operation.Toggle && !Objects.ContainsKey(obj)))
                {
                    Objects.Add(obj, obj);

                    if (!isWatching)
                    {
                        isWatching = true;
                        WatcherWindow.Watch(objects);
                    }
                }
                else
                {
                    Objects.Remove(obj);
                }
            }
        }

        /************************************************************************************************************************/

        internal static void AddMenuItem(GenericMenu menu, SerializedProperty property)
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorUtility.IsPersistent(property.serializedObject.targetObject) ||
                !GUI.enabled)
                return;

            var index = IndexOfProperty(property);

            menu.AddItem(new GUIContent(EditorStrings.PersistAfterPlayMode), index >= 0,
                () => PersistProperty(Operation.Toggle, property));
        }

        /************************************************************************************************************************/

        public static bool WillPersist(SerializedProperty property)
        {
            if (!_IsInitialized)
                return false;

            var index = IndexOfProperty(property);
            return index >= 0;
        }

        /************************************************************************************************************************/

        public static void PersistProperty(Operation operation, SerializedProperty property)
        {
            Initialize();

            var index = IndexOfProperty(property);

            if (operation == Operation.Add ||
                (operation == Operation.Toggle && index < 0))
            {
                Properties.Add(property);
                WatcherWindow.Watch(property);
            }
            else if (index >= 0)
            {
                Properties.RemoveAt(index);
            }
        }

        /************************************************************************************************************************/

        private static int IndexOfProperty(SerializedProperty property)
        {
            for (int i = 0; i < Properties.Count; i++)
            {
                if (Serialization.AreSameProperty(Properties[i].Property, property))
                    return i;
            }

            return -1;
        }

        /************************************************************************************************************************/

        private static void Initialize()
        {
            if (_IsInitialized)
                return;

            _IsInitialized = true;

            Serialization.ObjectReference[] objects = null;
            object[][] objectValues = null;
            Serialization.PropertyReference[] properties = null;
            object[][] propertyValues = null;

            EditorApplication.playModeStateChanged += (change) =>
            {
                switch (change)
                {
                    case PlayModeStateChange.ExitingPlayMode:
                        // Objects.
                        objects = new Serialization.ObjectReference[Objects.Count];
                        Objects.Values.CopyTo(objects, 0);

                        objectValues = new object[objects.Length][];
                        for (int i = 0; i < objects.Length; i++)
                        {
                            objectValues[i] = GetValues(objects[i]);
                        }

                        // Properties.
                        properties = Properties.ToArray();
                        Properties.Clear();

                        propertyValues = new object[properties.Length][];
                        for (int i = 0; i < properties.Length; i++)
                        {
                            var property = properties[i];
                            var targetValues = new object[property.TargetObjects.Length];
                            var j = 0;
                            property.Property.serializedObject.Update();
                            property.Property.ForEachTarget((prop) =>
                            {
                                targetValues[j++] = prop.GetValue();
                            }, null);
                            propertyValues[i] = targetValues;
                        }

                        break;

                    case PlayModeStateChange.EnteredEditMode:
                        // Objects.
                        for (int i = 0; i < objects.Length; i++)
                        {
                            SetValues(objects[i], objectValues[i]);
                        }

                        // Properties.
                        Debug.Assert(properties != null, "EnteredEditMode without ExitingPlayMode. This should never happen.");

                        for (int i = 0; i < properties.Length; i++)
                        {
                            var property = properties[i].Property;
                            if (property == null)
                                continue;

                            property.serializedObject.Update();
                            var targetValues = propertyValues[i];
                            var j = 0;
                            property.ForEachTarget((prop) =>
                            {
                                var value = targetValues[j++];
                                prop.SetValue(value);
                            });
                        }

                        objects = null;
                        objectValues = null;
                        properties = null;
                        propertyValues = null;
                        break;
                }
            };
        }

        /************************************************************************************************************************/

        private static object[] GetValues(Object obj)
        {
            var component = obj as Component;
            if (!(component is null))
                return GetValues(component);

            Debug.Log("Unable to persist " + obj, obj);
            return null;
        }

        private static void SetValues(Object obj, object[] values)
        {
            var component = obj as Component;
            if (!(component is null))
            {
                SetValues(component, values);
                return;
            }

            Debug.Log("Unable to persist " + obj, obj);
        }

        /************************************************************************************************************************/

        private static object[] GetValues(Component component)
        {
            var values = new List<object>();

            using (var serializedObject = new SerializedObject(component))
            {
                IGEditorUtils.ForEachProperty(serializedObject, false, (property) =>
                {
                    values.Add(property.GetValue());
                });
            }

            return values.ToArray();
        }

        private static void SetValues(Component component, object[] values)
        {
            var i = 0;

            using (var serializedObject = new SerializedObject(component))
            {
                IGEditorUtils.ForEachProperty(serializedObject, false, (property) =>
                {
                    property.SetValue(values[i++]);
                });
            }
        }

        /************************************************************************************************************************/
    }
}

#endif
