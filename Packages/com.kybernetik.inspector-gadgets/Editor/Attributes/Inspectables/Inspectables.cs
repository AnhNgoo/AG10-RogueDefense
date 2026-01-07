// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using InspectorGadgets.Editor.PropertyDrawers;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace InspectorGadgets.Attributes
{
    /// <summary>[Pro-Only]
    /// <see cref="Editor.Editor{T}"/> uses these attributes to add extra elements to the inspector.
    /// </summary>
    internal static class Inspectables
    {
        /************************************************************************************************************************/

        private static readonly Dictionary<Type, List<BaseInspectableAttributeDrawer>>
            AllDrawers = new();

        /************************************************************************************************************************/

        internal static List<BaseInspectableAttributeDrawer> Gather(Type type)
        {
            if (!AllDrawers.TryGetValue(type, out var inspectables))
            {
                inspectables = new();

                if (type.BaseType != null)
                    inspectables.AddRange(Gather(type.BaseType));

                Gather(type, inspectables, Editor.IGEditorUtils.AnyAccessBindings | BindingFlags.DeclaredOnly);

                AllDrawers.Add(type, inspectables);
            }

            return inspectables;
        }

        /************************************************************************************************************************/

        private static readonly List<BaseInspectableAttribute> Attributes = new();
        private static readonly List<FieldInfo> Fields = new();
        private static readonly List<PropertyInfo> Properties = new();
        private static readonly List<MethodInfo> Methods = new();

        private static void Gather(
            Type type,
            List<BaseInspectableAttributeDrawer> drawers,
            BindingFlags bindings)
        {
            Attributes.Clear();
            Fields.Clear();
            Properties.Clear();
            Methods.Clear();

            var first = Attributes.Count;
            var index = first;

            // Fields.
            IGUtils.GetAttributedFields(type, bindings, Attributes, Fields, false);
            for (int i = 0; i < Fields.Count; i++)
            {
                Initialize(drawers, Attributes, first, ref index, Fields[i]);
            }

            // Properties.
            IGUtils.GetAttributedProperties(type, bindings, Attributes, Properties, false);
            for (int i = 0; i < Properties.Count; i++)
            {
                Initialize(drawers, Attributes, first, ref index, Properties[i]);
            }

            // Methods.
            IGUtils.GetAttributedMethods(type, bindings, Attributes, Methods, false);
            for (int i = 0; i < Methods.Count; i++)
            {
                Initialize(drawers, Attributes, first, ref index, Methods[i]);
            }

            // Sort by DisplayIndex.
            IGUtils.StableInsertionSort(drawers);
        }

        /************************************************************************************************************************/

        private static void Initialize(
            List<BaseInspectableAttributeDrawer> drawers,
            List<BaseInspectableAttribute> attributes,
            int first,
            ref int index,
            MemberInfo member)
        {
            for (int i = 0; i < first; i++)
            {
                if (drawers[i].Member.MetadataToken == member.MetadataToken)
                {
                    attributes.RemoveAt(index);
                    return;
                }
            }

            var attribute = attributes[index];
            var drawer = CreateDrawer(attribute);
            if (drawer == null)
            {
                attributes.RemoveAt(index);
                Debug.LogError($"Unable to determine Inspectable drawer type for {attribute.GetType().FullName}");
                return;
            }

            var error = drawer.Initialize(attribute, member);
            if (error != null)
            {
                attributes.RemoveAt(index);
                drawer.LogInvalidMember(error);
                return;
            }

            drawers.Add(drawer);
            index++;
        }

        /************************************************************************************************************************/

        private static BaseInspectableAttributeDrawer CreateDrawer(BaseInspectableAttribute attribute)
        {
            if (!TryGetDrawerType(attribute.GetType(), out var drawerType))
                return null;

            return Activator.CreateInstance(drawerType) as BaseInspectableAttributeDrawer;
        }

        /************************************************************************************************************************/

        private static Dictionary<Type, Type> _InspectableTypeToDrawerType;

        private static bool TryGetDrawerType(Type inspectableType, out Type drawerType)
        {
            if (_InspectableTypeToDrawerType == null)
            {
                _InspectableTypeToDrawerType = new();

                var types = TypeCache.GetTypesDerivedFrom(typeof(BaseInspectableAttributeDrawer));
                foreach (var type in types)
                {
                    if (type.IsAbstract)
                        continue;

                    var arguments = type.BaseType.GetGenericArguments();
                    if (arguments.Length != 1)
                        continue;

                    _InspectableTypeToDrawerType.Add(arguments[0], type);
                }
            }

            return _InspectableTypeToDrawerType.TryGetValue(inspectableType, out drawerType);
        }

        /************************************************************************************************************************/
    }
}

#endif

