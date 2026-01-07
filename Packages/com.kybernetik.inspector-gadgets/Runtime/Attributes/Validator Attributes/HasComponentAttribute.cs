// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InspectorGadgets.Attributes
{
    /// <summary>[Pro-Only]
    /// When applied to a <see cref="GameObject"/> field, any object assigned to that field must have a component of
    /// the specified type.
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public sealed class HasComponentAttribute : ValidatorAttribute
    {
        /************************************************************************************************************************/

        /// <summary>The types of components that are required.</summary>
        public readonly Type[] ComponentTypes;

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="HasComponentAttribute"/> to require a component of the specified type.</summary>
        public HasComponentAttribute(params Type[] componentTypes)
        {
            ComponentTypes = componentTypes;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override bool TryValidate(ref Object value)
        {
            if (value is GameObject gameObject)
            {
                for (int i = 0; i < ComponentTypes.Length; i++)
                {
                    var componentType = ComponentTypes[i];
                    if (gameObject.GetComponent(componentType) == null)
                    {
                        Debug.LogWarning($"Unable to assign {gameObject}" +
                            $" because it doesn't have a {componentType.GetNameCS()} component", gameObject);

                        return false;
                    }
                }
            }

            return true;
        }

        /************************************************************************************************************************/
    }
}

