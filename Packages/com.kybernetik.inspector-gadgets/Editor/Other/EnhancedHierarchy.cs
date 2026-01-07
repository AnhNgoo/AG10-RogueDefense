// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace InspectorGadgets.Editor
{
    /// <summary>[Editor-Only] [Pro-Only] Improved GUI for the Hierarchy window.</summary>
    public class EnhancedHierarchy
    {
        /************************************************************************************************************************/

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemGUI;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        /************************************************************************************************************************/

        /// <summary>Called for each object shown in the Hierarchy window.</summary>
        private static void OnHierarchyWindowItemGUI(int instanceID, Rect area)
        {
            if (!IsEnabled)
                return;

            var item = EditorUtility.InstanceIDToObject(instanceID);
            if (item is GameObject gameObject)
                OnHierarchyWindowItemGUI(gameObject, area);
        }

        /************************************************************************************************************************/

        private static readonly List<Component> Components = new List<Component>();

        private static void OnHierarchyWindowItemGUI(GameObject gameObject, Rect area)
        {
            Components.Clear();
            gameObject.GetComponents(Components);

            if (MaxComponentCount >= 1)
                DoTransformGUI(Components[0], ref area);

            var count = Mathf.Min(Components.Count, MaxComponentCount);
            for (int i = 1; i < count; i++)
                DoComponentGUI(Components[i], ref area);

            DoChildCountGUI(gameObject, ref area);

            DoLayerAndTagGUI(gameObject, ref area);
        }

        /************************************************************************************************************************/

        private static void DoTransformGUI(Component component, ref Rect area)
        {
            var isActive = component.gameObject.activeSelf;

            var color = SetDisabledColor(!isActive);

            var clicked = DoComponentButtonGUI(component, ref area, component.GetType().Name, out var iconArea);

            GUI.color = color;

            if (clicked && Modifiers.AreKeysCurrentlyDown)
            {
                Undo.RegisterCompleteObjectUndo(component.gameObject, "Set Active");
                component.gameObject.SetActive(!isActive);
            }

            CheckDragAndDropOntoTransform(iconArea, component);
        }

        /************************************************************************************************************************/

        private static void CheckDragAndDropOntoTransform(Rect area, Component dropOnto)
        {
            switch (Event.current.type)
            {
                case EventType.DragUpdated:
                    if (TryGetDragAndDropTransform(area, dropOnto, out _))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                        Event.current.Use();
                    }
                    break;

                case EventType.DragPerform:
                    if (TryGetDragAndDropTransform(area, dropOnto, out var transform))
                        DropOntoTransform(transform, dropOnto as Transform);
                    break;
            }
        }

        /************************************************************************************************************************/

        private static bool TryGetDragAndDropTransform(Rect area, Component dropOnto, out Transform transform)
        {
            transform = null;

            if (!area.Contains(Event.current.mousePosition))
                return false;

            var dragging = DragAndDrop.objectReferences;
            if (dragging.Length != 1)
                return false;

            transform = IGUtils.GetTransform(dragging[0]);
            return transform != null && transform != dropOnto;
        }

        /************************************************************************************************************************/

        private static void DropOntoTransform(Transform drop, Transform onto)
        {
            DragAndDrop.AcceptDrag();
            Event.current.Use();

            var menu = new GenericMenu();

            menu.AddDisabledItem(new GUIContent("Drop: " + drop.name));
            menu.AddDisabledItem(new GUIContent("Target: " + onto.name));

            menu.AddItem(new GUIContent("Reparent Drop under Target"), false, () =>
            {
                Undo.SetTransformParent(drop, onto, "Reparent");
            });

            menu.AddItem(new GUIContent("Reparent Drop under Target and Reset Local Transform"), false, () =>
            {
                Undo.SetTransformParent(drop, onto, "Reparent");
                drop.localPosition = Vector3.zero;
                drop.localRotation = Quaternion.identity;
                drop.localScale = Vector3.one;
            });

            menu.AddItem(new GUIContent("Move Drop to Target"), false, () =>
            {
                Undo.RecordObject(drop, "Move");
                drop.SetPositionAndRotation(onto.position, onto.rotation);
            });

            menu.AddItem(new GUIContent("Move Target to Drop"), false, () =>
            {
                Undo.RecordObject(onto, "Move");
                onto.SetPositionAndRotation(drop.position, drop.rotation);
            });

            menu.ShowAsContext();
        }

        /************************************************************************************************************************/

        private static void DoComponentGUI(Component component, ref Rect area)
        {
            if (component == null)
                return;

            var isEnabled = EditorUtility.GetObjectEnabled(component);
            var color = SetDisabledColor(isEnabled == 0);

            var controlID = GUIUtility.GetControlID(FocusType.Passive);

            var clicked = DoComponentButtonGUI(component, ref area, component.GetType().Name, out _);

            GUI.color = color;

            if (clicked && isEnabled >= 0 && Modifiers.AreKeysCurrentlyDown)
            {
                Undo.RecordObject(component, "Enable Component");
                EditorUtility.SetObjectEnabled(component, isEnabled == 0);
            }
        }

        /************************************************************************************************************************/

        private static GUIStyle _IconStyle;

        private static bool DoComponentButtonGUI(Component component, ref Rect area, string tooltip, out Rect iconArea)
        {
            if (_IconStyle == null)
                _IconStyle = new GUIStyle();

            iconArea = IGEditorUtils.StealFromRight(ref area, area.height);

            var content = IGEditorUtils.TempContent(null, tooltip);
            content.image = AssetPreview.GetMiniThumbnail(component);

            var clicked = GUI.Button(iconArea, content, _IconStyle);

            content.image = null;

            var currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseDrag &&
                currentEvent.button == 0 &&
                iconArea.Contains(currentEvent.mousePosition))
            {
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new Object[] { component };
                DragAndDrop.StartDrag(component.GetType().Name);
                currentEvent.Use();
            }

            return clicked;
        }

        /************************************************************************************************************************/

        private static Color SetDisabledColor(bool isDisabled)
        {
            var color = GUI.color;
            if (isDisabled)
            {
                var newColor = color;
                newColor.a *= 0.5f;
                GUI.color = newColor;
            }
            return color;
        }

        /************************************************************************************************************************/

        private static void DrawLabelBackground(Rect area)
        {
            var color = EditorGUIUtility.isProSkin
                ? new Color(0.3f, 0.3f, 0.3f, 1)
                : new Color(0.7f, 0.7f, 0.7f, 1);

            EditorGUI.DrawRect(area, color);
        }

        /************************************************************************************************************************/
        #region Layer and Tag
        /************************************************************************************************************************/

        private static void DoLayerAndTagGUI(GameObject gameObject, ref Rect area)
        {
            DoTagGUI(gameObject, area, true, false);
            var layerWidth = DoLayerGUI(gameObject, area);
            var tagWidth = DoTagGUI(gameObject, area, false, true);

            area.width -= Mathf.Max(layerWidth, tagWidth);
        }

        /************************************************************************************************************************/

        private static float DoLayerGUI(GameObject gameObject, Rect area)
        {
            var layer = gameObject.layer;
            if (((1 << layer) & ShowLayers) == 0)
                return 0;

            var layerName = LayerMask.LayerToName(layer);
            return DrawTinyLabel(ref area, layerName, "Layer", true).width;
        }

        /************************************************************************************************************************/

        private static float DoTagGUI(GameObject gameObject, Rect area, bool drawBackground, bool drawText)
        {
            if (!ShowTags ||
                gameObject.CompareTag("Untagged"))
                return 0;

            return DrawTinyLabel(ref area, gameObject.tag, "Tag", false, drawBackground, drawText).width;
        }

        /************************************************************************************************************************/

        private static GUIStyle _LayerTagStyle;

        private static Rect DrawTinyLabel(
            ref Rect area,
            string text,
            string tooltip,
            bool top,
            bool drawBackground = true,
            bool drawText = true)
        {
            if (_LayerTagStyle == null)
                _LayerTagStyle = new GUIStyle(GUI.skin.label)
                {
                    padding = new RectOffset(),
                    fontSize = Mathf.CeilToInt(EditorStyles.label.fontSize * 0.5f) + 3,
                    alignment = TextAnchor.MiddleLeft,
                };

            var label = IGEditorUtils.TempContent(text, tooltip);

            _LayerTagStyle.CalcMinMaxWidth(label, out _, out var width);
            width += 1;

            var labelArea = IGEditorUtils.StealFromRight(ref area, width);

            labelArea.height *= 0.5f;

            if (top)
            {
                labelArea.height += 1;
            }
            else
            {
                labelArea.y += labelArea.height;
                labelArea.y -= 1;

                labelArea.height += 1;
            }

            if (drawBackground)
                DrawLabelBackground(labelArea);

            if (drawText)
                GUI.Label(labelArea, label, _LayerTagStyle);

            return labelArea;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Child Count
        /************************************************************************************************************************/

        private static GUIStyle _ChildCountStyle;

        private static void DoChildCountGUI(GameObject gameObject, ref Rect area)
        {
            if (!ShowChildCount)
                return;

            var childCount = GetChildCount(gameObject);
            if (childCount == null)
                return;

            if (_ChildCountStyle == null)
                _ChildCountStyle = new GUIStyle(GUI.skin.label)
                {
                    padding = new RectOffset(2, 2, 0, 0),
                    alignment = TextAnchor.MiddleRight,
                };

            var label = IGEditorUtils.TempContent(childCount, "Child Count");

            _ChildCountStyle.CalcMinMaxWidth(label, out _, out var width);

            var labelArea = IGEditorUtils.StealFromRight(ref area, width + 1);

            labelArea.xMin += 1;

            DrawLabelBackground(labelArea);
            GUI.Button(labelArea, label, _ChildCountStyle);
        }

        /************************************************************************************************************************/

        private static readonly Dictionary<GameObject, string>
            GameObjectToChildCount = new Dictionary<GameObject, string>();
        private static readonly List<Transform>
            Transforms = new List<Transform>();

        private static string GetChildCount(GameObject gameObject)
        {
            if (!GameObjectToChildCount.TryGetValue(gameObject, out var childCount))
            {
                Transforms.Clear();
                gameObject.GetComponentsInChildren(true, Transforms);

                var count = Transforms.Count - 1;
                if (count == 0)
                    childCount = null;
                else if (count < 1000)
                    childCount = $"{count}";
                else if (count < 1000000)
                    childCount = $"{count / 1000}.{count % 1000 / 100}K";
                else
                    childCount = $"{count / 1000000}.{count % 1000000 / 100000}M";

                Transforms.Clear();
                GameObjectToChildCount.Add(gameObject, childCount);
            }

            return childCount;
        }

        /************************************************************************************************************************/

        private static void OnHierarchyChanged()
        {
            GameObjectToChildCount.Clear();
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Preferences
        /************************************************************************************************************************/

        /// <summary>Should the <see cref="EnhancedHierarchy"/> be displayed?</summary>
        public static readonly AutoPrefs.EditorBool
            IsEnabled = new AutoPrefs.EditorBool(EditorStrings.PrefsKeyPrefix + nameof(IsEnabled), true);

        /// <summary>Should the <see cref="GameObject.tag"/> be shown?</summary>
        public static readonly AutoPrefs.EditorBool
            ShowTags = new AutoPrefs.EditorBool(EditorStrings.PrefsKeyPrefix + nameof(ShowTags), true);

        /// <summary>Should the <see cref="Transform.childCount"/> (recursive) be shown?</summary>
        public static readonly AutoPrefs.EditorBool
            ShowChildCount = new AutoPrefs.EditorBool(EditorStrings.PrefsKeyPrefix + nameof(ShowChildCount), true);

        /// <summary>Should the <see cref="GameObject.layer"/> be shown?</summary>
        public static readonly AutoPrefs.EditorInt
            ShowLayers = new AutoPrefs.EditorInt(EditorStrings.PrefsKeyPrefix + nameof(ShowLayers), ~(1 << 0));

        /// <summary>The maximum number of components that should be shown.</summary>
        public static readonly AutoPrefs.EditorInt
            MaxComponentCount = new AutoPrefs.EditorInt(EditorStrings.PrefsKeyPrefix + nameof(MaxComponentCount), 5);

        /// <summary>Which keys should be held when clicking a component to enable/disable it?</summary>
        private static readonly ModifierKeysPref
            Modifiers = new ModifierKeysPref(EditorStrings.PrefsKeyPrefix + nameof(Modifiers), ModifierKey.Alt);

        /************************************************************************************************************************/

        /// <summary>Editor GUI to display the <see cref="EnhancedHierarchy"/> preferences.</summary>
        public static class Preferences
        {
            /************************************************************************************************************************/

            /// <summary>The headding of this section.</summary>
            public const string Headding = "Enhanced Hierarchy";

            private static readonly GUIContent
                IsEnabledContent = new GUIContent("Enabled",
                    "Should the following enhancements be displayed in the Hierarchy?"),
                ShowLayerContent = new GUIContent("Show Layers",
                    "Which layers should be shown in the Hierarchy?"),
                ShowTagsContent = new GUIContent("Show Tag",
                    "Should the Hierarchy show the Tag of each object?"),
                ShowChildCountContent = new GUIContent("Show Child Count",
                    "Should the Hierarchy show the number of children each object has?"),
                MaxComponentCountContent = new GUIContent("Max Component Count",
                    "How many Components should the Hierarchy show for each object?"),
                ModifiersContent = new GUIContent("Click Modifiers",
                    "Clicking a Component Icon in the Hierarchy while holding these keys will Enable/Disable it." +
                    " Clicking the Transform Icon will Activate/Deactivate the GameObject.");

            /************************************************************************************************************************/

            /// <summary>Draws the preferences GUI.</summary>
            public static void DoGUI()
            {
                Editor.Preferences.DoSectionHeader(Headding);
                Editor.Preferences.DoProOnlyGroupEnabledPref(IsEnabled, IsEnabledContent);

                ShowLayers.DoGUI(ShowLayerContent,
                    (position, label, value, style) => IGEditorUtils.DoLayerMaskField(position, label, value));

                ShowTags.DoGUI(ShowTagsContent);
                ShowChildCount.DoGUI(ShowChildCountContent);

                MaxComponentCount.DoGUI(MaxComponentCountContent);
                if (MaxComponentCount < 0)
                    MaxComponentCount.Value = 0;

                if (MaxComponentCount == 0)
                    GUI.enabled = false;

                Modifiers.DoGUI(ModifiersContent);
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

