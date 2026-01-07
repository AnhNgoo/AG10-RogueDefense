// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace InspectorGadgets.Editor
{
    /// <summary>[Pro-Only] [Editor-Only]
    /// <see cref="MethodCache{TDelegate}"/>s for custom event messages added by Inspector Gadgets.
    /// </summary>
    public static class MethodCache
    {
        /************************************************************************************************************************/

        public static readonly MethodCache<Action> AfterInspectorGUI = nameof(AfterInspectorGUI);
        public static readonly MethodCache<Action> OnInspectorGUI = nameof(OnInspectorGUI);

        public delegate bool OverridePropertyGUIMethod(UnityEditor.SerializedProperty property, GUIContent label);
        public static readonly MethodCache<OverridePropertyGUIMethod> OverridePropertyGUI = nameof(OverridePropertyGUI);

        public delegate void OnPropertyContextMenuMethod(UnityEditor.GenericMenu menu, UnityEditor.SerializedProperty property);
        public static readonly MethodCache<OnPropertyContextMenuMethod> OnPropertyContextMenu = nameof(OnPropertyContextMenu);

        /************************************************************************************************************************/
    }

    /// <summary>[Pro-Only] [Editor-Only]
    /// Retrieves and caches methods that match a specified signature.
    /// </summary>
    public sealed class MethodCache<TDelegate> where TDelegate : class
    {
        /************************************************************************************************************************/

        public readonly string Name;
        public readonly Type ReturnType;
        public readonly Type[] ParameterTypes;

        private readonly Dictionary<Type, MethodInfo>
            TypeToMethod = new Dictionary<Type, MethodInfo>();

        /************************************************************************************************************************/

        /// <summary>
        /// Creates a new <see cref="MethodCache{TDelegate}"/> targeting the specified method name.
        /// </summary>
        public MethodCache(string name)
        {
            Name = name;

            var invoke = typeof(TDelegate).GetMethod("Invoke");

            ReturnType = invoke.ReturnType;

            var parameters = invoke.GetParameters();
            ParameterTypes = new Type[parameters.Length];
            for (int i = 0; i < ParameterTypes.Length; i++)
            {
                ParameterTypes[i] = parameters[i].ParameterType;
            }
        }

        /// <summary>
        /// Returns a new <see cref="MethodCache{TDelegate}"/> targeting the specified method name.
        /// </summary>
        public static implicit operator MethodCache<TDelegate>(string name)
            => new MethodCache<TDelegate>(name);

        /************************************************************************************************************************/

        /// <summary>Returns a delegate which calls the cached method on the `target`.</summary>
        public TDelegate GetDelegate(object target)
        {
            if (target == null)
                return null;

            var method = GetMethod(target.GetType());
            if (method == null)
                return null;

            return Delegate.CreateDelegate(typeof(TDelegate), target, method) as TDelegate;
        }

        /************************************************************************************************************************/

        private MethodInfo GetMethod(Type type)
        {
            if (!TypeToMethod.TryGetValue(type, out var method))
            {
                method = type.GetMethod(Name, IGEditorUtils.AnyAccessBindings, null, ParameterTypes, null);

                if (method != null && method.ReturnType != ReturnType)
                    method = null;

                if (method == null && type.BaseType != null)
                    method = GetMethod(type.BaseType);

                TypeToMethod.Add(type, method);
            }

            return method;
        }

        /************************************************************************************************************************/
    }
}

#endif
