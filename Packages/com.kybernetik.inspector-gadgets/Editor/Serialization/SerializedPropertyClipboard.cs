// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace InspectorGadgets.Editor
{
    internal static partial class AssemblyLinker
    {
        /************************************************************************************************************************/

        private static Func<SerializedProperty, IDisposable> GetSerializedPropertyContextFunction()
            => IGEditorUtils.GetSerializedPropertyContext;

        /************************************************************************************************************************/

        /// <summary>
        /// Invokes InspectorGadgets.AssemblyLinker.GetSerializedPropertyContextFunction using reflection so that the
        /// caller doesn't need to reference InspectorGadgets.dll directly.
        /// <para></para>
        /// Requires Inspector Gadgets Pro v6.0+.
        /// </summary>
        private static Func<SerializedProperty, IDisposable> GetExternalSerializedPropertyContextFunction()
        {
            Assembly assembly;
            try
            {
                assembly = Assembly.Load(nameof(InspectorGadgets));
            }
            catch (FileNotFoundException)
            {
                return null;
            }

            var linker = assembly.GetType($"{nameof(InspectorGadgets)}.{nameof(AssemblyLinker)}");
            if (linker == null)
                return null;

            var getter = linker.GetMethod(nameof(GetSerializedPropertyContextFunction),
                IGEditorUtils.StaticBindings, null, Type.EmptyTypes, null);
            if (getter == null)
                return null;

            return getter.Invoke(null, null) as Func<SerializedProperty, IDisposable>;
        }

        /************************************************************************************************************************/
    }

    public static partial class IGEditorUtils
    {
        /************************************************************************************************************************/
        #region Copy and Paste GUI
        /************************************************************************************************************************/

        /// <summary>
        /// Returns a disposable context that will allow copy and paste commands to be executed on the `property`.
        /// </summary>
        public static IDisposable GetSerializedPropertyContext(SerializedProperty property)
            => SerializedPropertyContext.Get(property);

        /************************************************************************************************************************/

        private sealed class SerializedPropertyContext : DisposableStaticLazyStack<SerializedPropertyContext>
        {
            private SerializedProperty _Property;
            private EventType _EventType;
            private int _StartID;
            private bool _WasEditingTextField;

            /************************************************************************************************************************/

            public static SerializedPropertyContext Get(SerializedProperty property)
            {
                var context = Increment();

                context._Property = property;
                context._EventType = Event.current.type;
                context._StartID = GUIUtility.GetControlID(FocusType.Passive);
                context._WasEditingTextField = EditorGUIUtility.editingTextField;
                EditorGUILayout.BeginFadeGroup(1);

                return context;
            }

            /************************************************************************************************************************/

            public override void Dispose()
            {
                base.Dispose();

                EditorGUILayout.EndFadeGroup();

                var endID = GUIUtility.GetControlID(FocusType.Passive);

                var currentEvent = Event.current;
                switch (currentEvent.type)
                {
                    case EventType.ValidateCommand:
                    case EventType.ExecuteCommand:
                        if (GUIUtility.keyboardControl >= _StartID &&
                            GUIUtility.keyboardControl <= endID)
                            HandleCommand(currentEvent);
                        break;

                    case EventType.Used:
                        if (_EventType == EventType.ExecuteCommand)
                        {
                            currentEvent.type = _EventType;

                            HandleCommand(currentEvent);

                            if (currentEvent.type == EventType.Used)
                                EditorGUIUtility.editingTextField = false;
                            else
                                currentEvent.type = EventType.Used;
                        }
                        break;
                }
            }

            /************************************************************************************************************************/

            private bool IsReceivingEvent(Event e)
            {
                if (_EventType != e.type)
                    return true;

                var position = GUILayoutUtility.GetLastRect();
                position.x = 0;
                return position.Contains(e.mousePosition);
            }

            /************************************************************************************************************************/

            private static Dictionary<Type, object> _TypeToClipboardValue;

            private bool HandleCommand(Event currentEvent)
            {
                if (_WasEditingTextField)
                    return false;

                switch (currentEvent.commandName)
                {
                    case "Copy":
                        {
                            if (_TypeToClipboardValue == null)
                                _TypeToClipboardValue = new Dictionary<Type, object>();

                            var accessor = _Property.GetAccessor();
                            if (accessor == null)
                                return false;

                            if (currentEvent.type == EventType.ExecuteCommand)
                            {
                                var type = accessor.GetFieldElementType(_Property);
                                _TypeToClipboardValue[type] = accessor.GetValue(_Property);
                            }

                            currentEvent.Use();
                            MarkStackAsUsed();
                        }
                        return true;

                    case "Paste":
                        {
                            if (_TypeToClipboardValue == null)
                                return false;

                            var accessor = _Property.GetAccessor();
                            if (accessor == null)
                                return false;

                            var type = accessor.GetFieldElementType(_Property);
                            if (!_TypeToClipboardValue.TryGetValue(type, out var value))
                                return false;

                            if (currentEvent.type == EventType.ExecuteCommand)
                            {
                                EditorGUIUtility.editingTextField = false;
                                _Property.SetValue(value);
                            }

                            currentEvent.Use();
                            MarkStackAsUsed();
                        }
                        return true;
                }

                return false;
            }

            /************************************************************************************************************************/

            private static void MarkStackAsUsed()
            {
                for (int i = 0; i < Stack.Count; i++)
                    Stack[i]._EventType = EventType.Used;
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif
