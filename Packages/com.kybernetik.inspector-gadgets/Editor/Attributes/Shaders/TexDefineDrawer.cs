// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace InspectorGadgets.Editor.PropertyDrawers
{
    /// <summary>[Pro-Only]
    /// Allows [TexDefine(KEYWORD)] to be used on texture properties in shaders.
    /// </summary>
    internal sealed class TexDefineDrawer : MaterialPropertyDrawer
    {
        /************************************************************************************************************************/

        private readonly string Keyword;
        private readonly bool Log;

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="TexDefineDrawer"/>.</summary>
        public TexDefineDrawer(string keyword)
        {
            Keyword = keyword;
        }

        /// <summary>Creates a new <see cref="TexDefineDrawer"/>.</summary>
        public TexDefineDrawer(string keyword, string log)
        {
            Keyword = keyword;
            Log = bool.Parse(log);
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
            => MaterialEditor.GetDefaultPropertyHeight(prop);

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void OnGUI(Rect area, MaterialProperty prop, string label, MaterialEditor editor)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMixedValue;

            var texture = editor.TextureProperty(area, prop, label);

            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                prop.textureValue = texture;

                var material = editor.target as Material;
                if (texture != null)
                    material.EnableKeyword(Keyword);
                else
                    material.DisableKeyword(Keyword);

                if (Log)
                    Debug.Log($"{Keyword} = {texture != null}");
            }
        }

        /************************************************************************************************************************/
    }
}

#endif
