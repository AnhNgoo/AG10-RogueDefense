// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using InspectorGadgets.Attributes;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InspectorGadgets.Editor.PropertyDrawers
{
    internal sealed class ButtonAttributeDrawer : BaseInspectableAttributeDrawer<ButtonAttribute>
    {
        /************************************************************************************************************************/

        private MethodInfo _Method;

        /// <summary>Initialize this button with a method.</summary>
        protected override string Initialize()
        {
            _Method = Member as MethodInfo;
            if (_Method == null)
                return "it is not a method";

            if (Attribute.Label == null)
                Attribute.Label = IGUtils.ConvertCamelCaseToFriendly(_Method.Name);

            if (Attribute.Tooltip == null)
                Attribute.Tooltip = "Calls " + _Method.GetNameCS();

            return null;
        }

        /************************************************************************************************************************/

        /// <summary>Draw this button using <see cref="GUILayout"/>.</summary>
        public override void OnGUI(Object[] targets)
        {
            if (GUILayout.Button(Attribute.Label, UnityEditor.EditorStyles.miniButton))
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (Attribute.SetDirty)
                        UnityEditor.Undo.RecordObjects(targets, "Inspector");

                    if (_Method.IsStatic)// Static Method.
                    {
                        var result = _Method.Invoke(null, null);

                        if (_Method.ReturnType != typeof(void))
                            Debug.Log($"{Attribute.Label}: {result}");
                    }
                    else// Instance Method.
                    {
                        foreach (var target in targets)
                        {
                            var result = _Method.Invoke(target, null);

                            if (_Method.ReturnType != typeof(void))
                                Debug.Log($"{Attribute.Label}: {result}", target);
                        }
                    }
                };
            }
        }

        /************************************************************************************************************************/
    }
}

#endif

