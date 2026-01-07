// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using InspectorGadgets.Attributes;
using System;
using System.Reflection;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InspectorGadgets.Editor.PropertyDrawers
{
    /// <summary>[Editor-Only] [Pro-Only] A custom drawer for <see cref="BaseInspectableAttribute"/>.</summary>
    public abstract class BaseInspectableAttributeDrawer<T> : BaseInspectableAttributeDrawer
        where T : BaseInspectableAttribute
    {
        /************************************************************************************************************************/

        /// <summary>The attribute being drawn.</summary>
        public T Attribute { get; private set; }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        internal override string Initialize(BaseInspectableAttribute attribute, MemberInfo member)
        {
            Attribute = attribute as T;
            return base.Initialize(attribute, member);
        }

        /************************************************************************************************************************/
    }

    /// <summary>[Editor-Only] [Pro-Only] A custom drawer for <see cref="BaseInspectableAttribute"/>.</summary>
    public abstract class BaseInspectableAttributeDrawer : IComparable<BaseInspectableAttributeDrawer>
    {
        /************************************************************************************************************************/

        /// <summary>The attribute being drawn.</summary>
        public BaseInspectableAttribute BaseAttribute { get; private set; }

        /// <summary>The attributed member.</summary>
        public MemberInfo Member { get; private set; }

        /************************************************************************************************************************/

        /// <summary><see cref="BaseInspectableAttribute.DisplayIndex"/></summary>
        public int DisplayIndex
            => BaseAttribute.DisplayIndex;

        /// <summary><see cref="BaseInspectableAttribute.When"/></summary>
        public EditorState When
            => BaseAttribute.WhenNullable.ValueOrDefault();

        /************************************************************************************************************************/

        /// <summary>Compares the <see cref="BaseAttribute"/>s.</summary>
        public int CompareTo(BaseInspectableAttributeDrawer other)
            => BaseAttribute.CompareTo(other.BaseAttribute);

        /************************************************************************************************************************/

        /// <summary>Initializes this drawer.</summary>
        internal virtual string Initialize(BaseInspectableAttribute attribute, MemberInfo member)
        {
            BaseAttribute = attribute;
            Member = member;
            return Initialize();
        }

        /// <summary>Initializes this drawer.</summary>
        protected abstract string Initialize();

        /************************************************************************************************************************/

        /// <summary>
        /// Logs a warning that the specified `member` can't have this kind of attribute for the given `reason`.
        /// </summary>
        internal void LogInvalidMember(string reason)
        {
            var text = new StringBuilder();
            text.Append("The member: ");
            text.Append(Member.DeclaringType.FullName);
            text.Append('.');
            text.Append(Member.Name);
            text.Append(" cannot have a [");
            text.Append(GetType().FullName);
            text.Append("] attribute because ");
            text.Append(reason);
            text.Append('.');
            Debug.LogWarning(text.ToString());
        }

        /************************************************************************************************************************/

        /// <summary>Draws this inspectable using <see cref="GUILayout"/>.</summary>
        public abstract void OnGUI(Object[] targets);

        /************************************************************************************************************************/

        /// <summary>
        /// If <see cref="Event.current"/> is a Context Click within the `area`, this method creates a menu, calls
        /// <see cref="PopulateContextMenu"/>, and shows it as a context menu.
        /// </summary>
        protected void CheckContextMenu(Rect area, Object[] targets)
        {
            var currentEvent = Event.current;
            if (currentEvent.type != EventType.ContextClick ||
                !area.Contains(currentEvent.mousePosition))
                return;

            var menu = new UnityEditor.GenericMenu();
            PopulateContextMenu(menu, targets);
            menu.ShowAsContext();
        }

        /// <summary>Adds various items to the `menu` relating to the `targets`.</summary>
        protected virtual void PopulateContextMenu(UnityEditor.GenericMenu menu, Object[] targets) { }

        /************************************************************************************************************************/

        /// <summary>
        /// Draws a label like <see cref="UnityEditor.EditorGUILayout.PrefixLabel(string)"/> but doesn't get greyed out
        /// if the GUI is disabled for the following control.
        /// </summary>
        public static Rect PrefixLabel(GUIContent label)
        {
            var area = GUILayoutUtility.GetRect(0, UnityEditor.EditorGUIUtility.singleLineHeight, LabelStyle);

            var labelWidth = UnityEditor.EditorGUIUtility.labelWidth + LabelStyle.padding.horizontal;
            labelWidth -= UnityEditor.EditorGUI.indentLevel * Editor.IGEditorUtils.IndentSize;
            var labelArea = Editor.IGEditorUtils.StealFromLeft(ref area, labelWidth);
            labelArea = UnityEditor.EditorGUI.IndentedRect(labelArea);

            GUI.Label(labelArea, label, LabelStyle);
            return area;
        }

        /************************************************************************************************************************/

        private static GUIStyle _LabelStyle;

        /// <summary>A style based on the default label with the font set to italic.</summary>
        public static GUIStyle LabelStyle
        {
            get
            {
                if (_LabelStyle == null)
                    _LabelStyle = new GUIStyle(UnityEditor.EditorStyles.label)
                    {
                        fontStyle = FontStyle.Italic
                    };

                return _LabelStyle;
            }
        }

        /************************************************************************************************************************/
    }
}

#endif

