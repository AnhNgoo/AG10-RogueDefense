// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using InspectorGadgets.Attributes;
using InspectorGadgets.Editor.PropertyDrawers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InspectorGadgets.Editor
{
    internal static class SerializedPropertyContextMenu
    {
        /************************************************************************************************************************/

        public static readonly FloatHandler FloatMenuHandler = new();
        public static readonly Vector2Handler Vector2MenuHandler = new();
        public static readonly Vector3Handler Vector3MenuHandler = new();
        public static readonly Vector4Handler Vector4MenuHandler = new();
        public static readonly QuaternionHandler QuaternionMenuHandler = new();

        private static readonly Dictionary<SerializedPropertyType, ContextMenuHandler>
            ContextMenuHandlers = new()
            {
                { SerializedPropertyType.Boolean, new BoolHandler() },
                { SerializedPropertyType.ArraySize, new BaseIntHandler() },
                { SerializedPropertyType.Integer, new IntHandler() },
                { SerializedPropertyType.String, new StringHandler() },
                { SerializedPropertyType.Float, FloatMenuHandler },
                { SerializedPropertyType.Vector2, Vector2MenuHandler },
                { SerializedPropertyType.Vector3, Vector3MenuHandler },
                { SerializedPropertyType.Vector4, Vector4MenuHandler },
                { SerializedPropertyType.Quaternion, QuaternionMenuHandler },
                { SerializedPropertyType.Rect, new RectHandler() },
                { SerializedPropertyType.Bounds, new BoundsHandler() },
                { SerializedPropertyType.Color, new ColorHandler() },
                { SerializedPropertyType.Enum, new EnumHandler() },
                { SerializedPropertyType.AnimationCurve, new AnimationCurveHandler() },
                { SerializedPropertyType.ObjectReference, new ObjectReferenceHandler() },
                { SerializedPropertyType.Generic, new GenericHandler() },
                { SerializedPropertyType.ManagedReference, new ManagedReferenceHandler() },
            };

        public const string
            Set = "Set/",
            Convert = Set + "Convert/",
            Randomize = Set + "Randomize/";

        /************************************************************************************************************************/

        [InitializeOnLoadMethod]
        private static void Initialize() => EditorApplication.contextualPropertyMenu += PopulateMenu;

        public static void PopulateMenu(GenericMenu menu, SerializedProperty property)
        {

            // Cache the serialized object and property path since the 'property' itself will be reused and no
            // longer point to the right place by the time any of the menu functions are called.
            property = property.Copy();

            if (TransformPropertyDrawer.CurrentlyDrawing != null)
            {
                TransformPropertyDrawer.CurrentlyDrawing.OnPropertyContextMenu(menu, property);
            }
            else
            {
                if (property.serializedObject.targetObject is RectTransform)
                {
                    if (AddRectTransformItems(menu, property))
                    {
                        AddWatchFunction(menu, property);
                        return;
                    }
                }

                if (ContextMenuHandlers.TryGetValue(property.propertyType, out var handler))
                {
                    handler.AddItems(menu, property);
                }
            }

            AddWatchFunction(menu, property);
            AddHelpFunctions(menu, property);
        }

        /************************************************************************************************************************/

        public static void OpenMenu(SerializedProperty property)
        {
            var menu = new GenericMenu();
            PopulateMenu(menu, property);
            menu.ShowAsContext();
        }

        /************************************************************************************************************************/

        private static void AddWatchFunction(GenericMenu menu, SerializedProperty property)
        {
            menu.AddItem(new GUIContent("Watch"), false, () => WatcherWindow.Watch(property));
        }

        /************************************************************************************************************************/

        private static void AddHelpFunctions(GenericMenu menu, SerializedProperty property)
        {
            const string Prefix = "Help ->/";
            menu.AddItem(new GUIContent(Prefix + "Inspector Gadgets Documentation"), false, IGEditorUtils.OpenDocumentation);

            var targetType = property.serializedObject.targetObject.GetType();

            var helpURL = targetType.GetCustomAttribute<HelpURLAttribute>();
            if (helpURL != null)
            {
                menu.AddItem(new GUIContent(Prefix + helpURL.URL.Replace('/', '\\')), false,
                    () => EditorUtility.OpenWithDefaultApp(helpURL.URL));
            }

            var name = "Unity " + targetType.GetNameCS(false);
            menu.AddItem(new GUIContent($"{Prefix}Google '{name}'"), false,
                () => EditorUtility.OpenWithDefaultApp("https://www.google.com/search?q=" + name));

            var fullName = targetType.GetNameCS();
            menu.AddItem(new GUIContent($"{Prefix}Google '{fullName}'"), false,
                () => EditorUtility.OpenWithDefaultApp("https://www.google.com/search?q=" + fullName));
        }

        /************************************************************************************************************************/

        // Unsupported: LayerMask, Character, Gradient, ExposedReference, FixedBufferSize.

        // Character uses (char)property.intValue

        // case SerializedPropertyType.LayerMask:
        //      SerializedProperty.layerMaskStringValue
        //      int[] GetLayerMaskSelectedIndex();
        //      string[] GetLayerMaskNames();
        //     break;

        // case SerializedPropertyType.Gradient:
        //      SerializedProperty.gradientValue
        //     break;

        /************************************************************************************************************************/
        #region Context Menu Handlers
        /************************************************************************************************************************/
        #region Rect Transform
        /************************************************************************************************************************/

        private static bool AddRectTransformItems(GenericMenu menu, SerializedProperty property)
        {
            switch (property.propertyPath)
            {
                case "m_AnchoredPosition.x": AddRectPositionItems(menu, property, 0, "RectTransform.anchoredPosition.x"); break;
                case "m_AnchoredPosition.y": AddRectPositionItems(menu, property, 1, "RectTransform.anchoredPosition.y"); break;

                case "m_SizeDelta.x": AddRectPositionItems(menu, property, 0, "RectTransform.sizeDelta.x"); break;
                case "m_SizeDelta.y": AddRectPositionItems(menu, property, 1, "RectTransform.sizeDelta.y"); break;

                case "m_LocalPosition.z": AddRectPositionItems(menu, property, 2, "Transform.localPosition.z"); break;

                case "m_AnchorMin": AddRectVector2Items(menu, property, "RectTransform.anchorMin"); break;
                case "m_AnchorMin.x": AddRectFloatItems(menu, property, "RectTransform.anchorMin.x"); break;
                case "m_AnchorMin.y": AddRectFloatItems(menu, property, "RectTransform.anchorMin.y"); break;

                case "m_AnchorMax": AddRectVector2Items(menu, property, "RectTransform.anchorMax"); break;
                case "m_AnchorMax.x": AddRectFloatItems(menu, property, "RectTransform.anchorMax.x"); break;
                case "m_AnchorMax.y": AddRectFloatItems(menu, property, "RectTransform.anchorMax.y"); break;

                case "m_Pivot": AddRectVector2Items(menu, property, "RectTransform.pivot"); break;
                case "m_Pivot.x": AddRectFloatItems(menu, property, "RectTransform.pivot.x"); break;
                case "m_Pivot.y": AddRectFloatItems(menu, property, "RectTransform.pivot.y"); break;

                case "m_LocalRotation": AddRectRotationItems(menu, property); break;

                case "m_LocalScale": AddRectScaleVectorItems(menu, property); break;
                case "m_LocalScale.x": AddRectScaleFloatItems(menu, property); break;
                case "m_LocalScale.y": AddRectScaleFloatItems(menu, property); break;
                case "m_LocalScale.z": AddRectScaleFloatItems(menu, property); break;

                default: return false;
            }

            return true;
        }

        /************************************************************************************************************************/

        private static void AddRectPositionItems(GenericMenu menu, SerializedProperty property, int axis, string name)
        {
            menu.AddDisabledItem(new GUIContent("float      " + name));

            FloatMenuHandler.AddClipboardFunctions(menu, property);

            menu.AddSeparator("");
            FloatHandler.AddSetFunction(menu, property, "Zero (0)", 0);
            FloatHandler.AddNegateFunction(menu, property);
            FloatHandler.AddRoundFunction(menu, property);

            PositionDrawer.AddSnapFloatToGridItem(menu, property, axis);

            AddRectSnapToSiblingsItems(menu, property, axis);

            FloatMenuHandler.AddLogFunction(menu, property);
        }

        /************************************************************************************************************************/

        private static void AddRectVector2Items(GenericMenu menu, SerializedProperty property, string name)
        {
            menu.AddDisabledItem(new GUIContent($"{nameof(Vector2)}      {name}"));
            Vector2MenuHandler.AddClipboardFunctions(menu, property);
            AddRectVectorAlignmentItems(menu, property);
            Vector2MenuHandler.AddLogFunction(menu, property);
        }

        /************************************************************************************************************************/

        private static void AddRectVectorAlignmentItems(GenericMenu menu, SerializedProperty property)
        {
            menu.AddPropertyModifierFunction(property,
                $"{Set}Bottom Left (0, 0)",
                (targetProperty) => targetProperty.vector2Value = new Vector2(0, 0));
            menu.AddPropertyModifierFunction(property,
                $"{Set}Bottom Center (0.5, 0)",
                (targetProperty) => targetProperty.vector2Value = new Vector2(0.5f, 0));
            menu.AddPropertyModifierFunction(property,
                $"{Set}Bottom Right (1, 0)",
                (targetProperty) => targetProperty.vector2Value = new Vector2(1, 0));

            menu.AddPropertyModifierFunction(property,
                $"{Set}Middle Left (0, 0.5)",
                (targetProperty) => targetProperty.vector2Value = new Vector2(0, 0.5f));
            menu.AddPropertyModifierFunction(property,
                $"{Set}Middle Center (0.5, 0.5)",
                (targetProperty) => targetProperty.vector2Value = new Vector2(0.5f, 0.5f));
            menu.AddPropertyModifierFunction(property,
                $"{Set}Middle Right (1, 0.5)",
                (targetProperty) => targetProperty.vector2Value = new Vector2(1, 0.5f));

            menu.AddPropertyModifierFunction(property,
                $"{Set}Top Left (0, 1)",
                (targetProperty) => targetProperty.vector2Value = new Vector2(0, 1));
            menu.AddPropertyModifierFunction(property,
                $"{Set}Top Center (0.5, 1)",
                (targetProperty) => targetProperty.vector2Value = new Vector2(0.5f, 1));
            menu.AddPropertyModifierFunction(property,
                $"{Set}Top Right (1, 1)",
                (targetProperty) => targetProperty.vector2Value = new Vector2(1, 1));

        }

        /************************************************************************************************************************/

        private static void AddRectFloatItems(GenericMenu menu, SerializedProperty property, string name)
        {
            menu.AddDisabledItem(new GUIContent($"float      {name}"));
            FloatMenuHandler.AddClipboardFunctions(menu, property);
            AddRectFloatAlignmentItems(menu, property);
            FloatMenuHandler.AddLogFunction(menu, property);
        }

        /************************************************************************************************************************/

        private static void AddRectFloatAlignmentItems(GenericMenu menu, SerializedProperty property)
        {
            menu.AddSeparator("");
            FloatHandler.AddSetFunction(menu, property, Set + "Zero (0)", 0);
            FloatHandler.AddSetFunction(menu, property, Set + "Half (0.5)", 0.5f);
            FloatHandler.AddSetFunction(menu, property, Set + "One (1)", 1);
        }

        /************************************************************************************************************************/

        private static void AddRectRotationItems(GenericMenu menu, SerializedProperty property)
        {
            menu.AddDisabledItem(new GUIContent($"{nameof(Quaternion)}      {nameof(RectTransform)}.{nameof(RectTransform.localRotation)}"));

            QuaternionMenuHandler.AddClipboardFunctions(menu, property);

            QuaternionMenuHandler.AddCustomItems(menu, property);
            menu.AddPropertyModifierFunction(property, Randomize + "Z", (targetProperty) =>
              {
                  var euler = targetProperty.quaternionValue.eulerAngles;
                  euler.z = UnityEngine.Random.Range(0, 360f);
                  targetProperty.quaternionValue = Quaternion.Euler(euler);
              });
            RotationDrawer.AddSnapQuaternionToGridFunction(menu, property);

            QuaternionMenuHandler.AddLogFunction(menu, property);
        }

        /************************************************************************************************************************/

        private static void AddRectScaleVectorItems(GenericMenu menu, SerializedProperty property)
        {
            menu.AddDisabledItem(new GUIContent($"{nameof(Vector3)}      {nameof(RectTransform)}.{nameof(RectTransform.localScale)}"));

            Vector3MenuHandler.AddClipboardFunctions(menu, property);

            menu.AddSeparator("");

            menu.AddPropertyModifierFunction(property,
                $"{Set}Zero (0, 0, 0)",
                (targetProperty) => targetProperty.vector3Value = default);

            menu.AddPropertyModifierFunction(property,
                $"{Set}One (1, 1, 1)",
                (targetProperty) => targetProperty.vector3Value = Vector3.one);

            menu.AddPropertyModifierFunction(property,
                $"{Set}Negate (*= -1)",
                (targetProperty) => targetProperty.vector3Value *= -1);

            menu.AddPropertyModifierFunction(property,
                $"{Set}Average",
                (targetProperty) =>
                {
                    var value = targetProperty.vector3Value;
                    value.x = value.y = value.z = (value.x + value.y + value.z) / 3;
                    targetProperty.vector3Value = value;
                });

            menu.AddPropertyModifierFunction(property,
                $"{Randomize}0.5-1.5 (Uniform)",
                (targetProperty) =>
                {
                    var value = UnityEngine.Random.value;
                    targetProperty.vector3Value = new Vector3(value, value, value);
                });
            menu.AddPropertyModifierFunction(property,
                $"{Randomize}0.5-1.5 (Non-Uniform)",
                (targetProperty) =>
                {
                    targetProperty.vector3Value = new Vector3(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
                });

            ScaleDrawer.AddSnapVectorToGridItem(menu, property);

            Vector3MenuHandler.AddLogFunction(menu, property);
        }

        /************************************************************************************************************************/

        private static void AddRectScaleFloatItems(GenericMenu menu, SerializedProperty property)
        {
            menu.AddDisabledItem(new GUIContent($"float      {nameof(RectTransform)}.{nameof(RectTransform.localScale)}.{property.name}"));

            FloatMenuHandler.AddClipboardFunctions(menu, property);

            menu.AddSeparator("");
            FloatHandler.AddSetFunction(menu, property, $"{Set}Zero (0)", 0);
            FloatHandler.AddSetFunction(menu, property, $"{Set}One (1)", 1);

            menu.AddPropertyModifierFunction(property,
                $"{Set}Negate (*= -1)",
                (targetProperty) => targetProperty.floatValue *= -1);

            menu.AddPropertyModifierFunction(property,
                $"{Randomize}0.5-1.5",
                (targetProperty) => targetProperty.floatValue = UnityEngine.Random.value);

            ScaleDrawer.AddSnapFloatToGridItem(menu, property);

            FloatMenuHandler.AddLogFunction(menu, property);
        }

        /************************************************************************************************************************/
        #region Snap to Siblings
        /************************************************************************************************************************/

        private static void AddRectSnapToSiblingsItems(GenericMenu menu, SerializedProperty property, int axis)
        {
            menu.AddSeparator("");

            AddSquarifyItem(menu, property, $"{Set}Width = Height", 0, (rect) => rect.height - rect.width);
            AddSquarifyItem(menu, property, $"{Set}Height = Width", 1, (rect) => rect.width - rect.height);

            menu.AddItem(new GUIContent("Snap to Siblings/Right"), false, () => SnapToSiblings(property, 0));
            menu.AddItem(new GUIContent("Snap to Siblings/Up"), false, () => SnapToSiblings(property, 1));
            menu.AddItem(new GUIContent("Snap to Siblings/Left"), false, () => SnapToSiblings(property, 2));
            menu.AddItem(new GUIContent("Snap to Siblings/Down"), false, () => SnapToSiblings(property, 3));
        }

        /************************************************************************************************************************/

        private static void AddSquarifyItem(GenericMenu menu, SerializedProperty property, string label, int setAxis, Func<Rect, float> calculateResize)
        {
            menu.AddPropertyModifierFunction(property, label, (targetProperty) =>
            {
                var transform = targetProperty.serializedObject.targetObject as RectTransform;
                var rect = transform.rect;
                if (rect.width != rect.height)
                {
                    var sizeDelta = transform.sizeDelta;
                    sizeDelta[setAxis] += calculateResize(rect);
                    transform.sizeDelta = sizeDelta;
                }
            });
        }

        /************************************************************************************************************************/

        private static Vector3[] _FourCorners;

        private static float GetEdge(RectTransform transform, int direction)
        {
            // Right, Up, Left, Down.

            const int Corners = 4;

            if (_FourCorners == null)
                _FourCorners = new Vector3[Corners];

            transform.GetLocalCorners(_FourCorners);

            int i;

            // Transform the corners into the parent's local space.
            for (i = 0; i < Corners; i++)
            {
                var corner = _FourCorners[i];
                corner = transform.TransformPoint(corner);
                if (transform.parent != null)
                    corner = transform.parent.InverseTransformPoint(corner);
                _FourCorners[i] = corner;
            }

            var axis = direction % 2;
            var isPositiveDirection = IsPositiveDirection(direction);

            // Find the edge furthest in the target direction.
            var edge = _FourCorners[0][axis];

            i = 1;
            for (; i < Corners; i++)
            {
                var corner = _FourCorners[i][axis];

                if (isPositiveDirection)
                {
                    if (edge < corner)
                        edge = corner;
                }
                else
                {
                    if (edge > corner)
                        edge = corner;
                }
            }

            return edge;
        }

        private static bool IsPositiveDirection(int direction) => (direction % 4) < 2;

        private static int CompareEdges(RectTransform a, RectTransform b, int direction)
        {
            var result = GetEdge(a, direction).CompareTo(GetEdge(b, direction));
            return IsPositiveDirection(direction) ? -result : result;
        }

        /************************************************************************************************************************/

        private static void SnapToSiblings(SerializedProperty property, int direction)
        {
            var selection = GetSortedTargetRects(property, (a, b) => CompareEdges(a, b, direction));

            var hasRecordedUndo = false;

            for (int i = 0; i < selection.Count; i++)
            {
                var transform = selection[i];
                var edge = GetEdge(transform, direction);
                var nextEdge = GetNextEdge(edge, transform, direction);

                if (nextEdge != edge)
                {
                    if (!hasRecordedUndo)
                    {
                        hasRecordedUndo = true;
                        Undo.RecordObjects(selection.ToArray(), "Snap to Siblings");
                    }

                    var anchoredPosition = transform.anchoredPosition;
                    anchoredPosition[direction % 2] += nextEdge - edge;
                    transform.anchoredPosition = anchoredPosition;
                }
            }
        }

        /************************************************************************************************************************/

        private static List<RectTransform> GetSortedTargetRects(SerializedProperty property, Comparison<RectTransform> comparison)
        {
            var targetObjects = property.serializedObject.targetObjects;

            var rects = new List<RectTransform>();
            for (int i = 0; i < targetObjects.Length; i++)
            {
                var rect = targetObjects[i] as RectTransform;
                if (rect != null && rect.parent != null)
                    rects.Add(rect);
            }

            rects.Sort(comparison);

            return rects;
        }

        /************************************************************************************************************************/

        private static float GetNextEdge(float current, RectTransform transform, int direction)
        {
            var positiveDirection = IsPositiveDirection(direction);

            float nextEdge;
            bool foundEdge;

            var parentRect = transform.parent as RectTransform;
            if (parentRect != null)
            {
                nextEdge = GetEdge(parentRect, direction) - parentRect.localPosition[direction % 2];
                foundEdge = true;
            }
            else
            {
                nextEdge = positiveDirection ? float.PositiveInfinity : float.NegativeInfinity;
                foundEdge = false;
            }

            var minSideDirection = direction + 1;
            if (IsPositiveDirection(minSideDirection))
                minSideDirection += 2;

            var maxSideDirection = minSideDirection + 2;

            var minSide = GetEdge(transform, minSideDirection);
            var maxSide = GetEdge(transform, maxSideDirection);

            direction += 2;
            foreach (RectTransform child in transform.parent)
            {
                // Ignore objects that aren't in line with the target.
                if (child == transform ||
                    GetEdge(child, minSideDirection) > maxSide ||
                    GetEdge(child, maxSideDirection) < minSide)
                    continue;

                var edge = GetEdge(child, direction);

                const float Leeway = 0.1f;

                if (positiveDirection)
                {
                    if (nextEdge > edge && edge > current - Leeway)
                    {
                        nextEdge = edge;
                        foundEdge = true;
                    }
                }
                else
                {
                    if (nextEdge < edge && edge < current + Leeway)
                    {
                        nextEdge = edge;
                        foundEdge = true;
                    }
                }
            }

            return foundEdge ? nextEdge : current;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/

        public abstract class ContextMenuHandler
        {
            public static SerializedProperty CurrentProperty { get; private set; }
            public static string CurrentTypeName { get; private set; }

            private MethodCache.OnPropertyContextMenuMethod _CustomEvent;

            public void AddItems(GenericMenu menu, SerializedProperty property)
            {
                CurrentProperty = property;
                CurrentTypeName = GetTypeName(property);

                AddNameItems(menu, property);
                AddClipboardFunctions(menu, property);
                AddCustomItems(menu, property);
                AddLogFunction(menu, property);

                if (_CustomEvent == null)
                    _CustomEvent = MethodCache.OnPropertyContextMenu.GetDelegate(property.serializedObject.targetObject);

                _CustomEvent?.Invoke(menu, property);
            }

            public abstract string GetTypeName(SerializedProperty property);

            public virtual void AddNameItems(GenericMenu menu, SerializedProperty property)
            {
                menu.AddDisabledItem(new GUIContent($"{CurrentTypeName}      {EditorStrings.NegateShortcut}{property.propertyPath}"));
            }

            public virtual void AddClipboardFunctions(GenericMenu menu, SerializedProperty property)
            {
                var isDefault = Serialization.IsDefaultValueByType(property);
                menu.AddPropertyModifierFunction(property, "Reset", !isDefault,
                    (targetProperty) => Serialization.ResetValue(targetProperty));

                // Copy the current events by creating a new SerializedObject and getting the same property.
                // It will continue holding the current values because we never call Update on it.
                var isCopied = SerializedProperty.DataEquals(property, _CopiedProperty);
                menu.AddFunction("Copy", !isCopied, () => Copy(property));

                AddPasteFunctions(menu, property);

                PersistentValues.AddMenuItem(menu, property);
            }

            public virtual bool UseSetGroup => false;

            /************************************************************************************************************************/

            private static SerializedProperty _CopiedProperty;
            private static object _CopiedValue;

            /************************************************************************************************************************/

            public void Copy(SerializedProperty property)
            {
                _CopiedProperty?.serializedObject.Dispose();

                _CopiedProperty = new SerializedObject(property.serializedObject.targetObjects).FindProperty(property.propertyPath);

                _CopiedValue = _CopiedProperty.GetValue();

                var clipboardString = GetDisplayString(_CopiedValue);
                if (clipboardString != null)
                    EditorGUIUtility.systemCopyBuffer = clipboardString;
            }

            /************************************************************************************************************************/

            public void Copy(object value, SerializedProperty property = null)
            {
                _CopiedProperty?.serializedObject.Dispose();

                _CopiedProperty = property?.Copy();

                _CopiedValue = value;

                var clipboardString = GetDisplayString(_CopiedValue);
                if (clipboardString != null)
                    EditorGUIUtility.systemCopyBuffer = clipboardString;
            }

            /************************************************************************************************************************/

            public void AddPasteFunctions(GenericMenu menu, SerializedProperty property)
            {
                var parsed = false;
                object parsedValue = null;
                try
                {
                    parsed = TryParse(EditorGUIUtility.systemCopyBuffer, out parsedValue);
                    if (parsed)
                    {
                        menu.AddPropertyModifierFunction(property,
                            $"Paste '{GetDisplayString(parsedValue)}'",
                            (targetProperty) => targetProperty.SetValue(parsedValue));
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }

                if (!parsed || _CopiedValue != parsedValue)
                {
                    var label = parsed ? "Paste (Direct)" : "Paste";
                    var isCopied = SerializedProperty.DataEquals(property, _CopiedProperty);
                    menu.AddPropertyModifierFunction(property, label, _CopiedProperty != null && !isCopied,
                        (targetProperty) => targetProperty.CopyValueFrom(_CopiedProperty));
                }
            }

            /************************************************************************************************************************/

            public float Paste(SerializedProperty property)
            {
                if (_CopiedProperty != null)
                    return property.CopyValueFrom(_CopiedProperty);
                else
                    return 0;
            }

            /************************************************************************************************************************/

            public virtual string GetDisplayString(object value) => value?.ToString();
            public virtual string GetDisplayString(SerializedProperty property) => GetDisplayString(property.GetValue());

            public virtual bool TryParse(string value, out object result)
            {
                result = null;
                return false;
            }

            public bool TryGetClipboardValue(out object result) => TryParse(EditorGUIUtility.systemCopyBuffer, out result);

            /************************************************************************************************************************/

            public abstract void AddCustomItems(GenericMenu menu, SerializedProperty property);

            /************************************************************************************************************************/

            public virtual void AddLogFunction(GenericMenu menu, SerializedProperty property)
            {
                menu.AddSeparator("");
                AddLogValueFunction(menu, property, GetDisplayString);
            }
        }

        /************************************************************************************************************************/

        public abstract class ContextMenuHandler<T> : ContextMenuHandler
        {
            public sealed override string GetDisplayString(object value) => GetDisplayString((T)value);
            public virtual string GetDisplayString(T value) => value?.ToString();

            public sealed override bool TryParse(string value, out object result)
            {
                if (TryParse(value, out T t))
                {
                    result = t;
                    return true;
                }
                else
                {
                    result = null;
                    return false;
                }
            }

            public virtual bool TryParse(string value, out T result)
            {
                result = default;
                return false;
            }

            public bool TryGetClipboardValue(out T result) => TryParse(EditorGUIUtility.systemCopyBuffer, out result);
        }

        /************************************************************************************************************************/

        public sealed class BoolHandler : ContextMenuHandler<bool>
        {
            public override string GetTypeName(SerializedProperty property) => "bool";

            public override void AddCustomItems(GenericMenu menu, SerializedProperty property)
            {
                menu.AddSeparator("");
                menu.AddPropertyModifierFunction(property, "Randomize",
                    (targetProperty) => targetProperty.boolValue = UnityEngine.Random.Range(0, 2) != 0);
            }

            /************************************************************************************************************************/

            public override bool TryParse(string value, out bool result) => bool.TryParse(value, out result);

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        public class BaseIntHandler : ContextMenuHandler<int>
        {
            public override string GetTypeName(SerializedProperty property) => "int";

            public override void AddCustomItems(GenericMenu menu, SerializedProperty property) { }

            /************************************************************************************************************************/

            public override bool TryParse(string value, out int result) => int.TryParse(value, out result);

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        public sealed class IntHandler : BaseIntHandler
        {
            public override void AddCustomItems(GenericMenu menu, SerializedProperty property)
            {
                menu.AddSeparator("");

                var accessor = property.GetAccessor();
                if (accessor != null)
                {
                    var field = accessor.GetField(property);
                    if (field != null)
                    {
                        var intRange = field.GetCustomAttribute<RangeAttribute>(true);
                        if (intRange != null)
                        {
                            menu.AddPropertyModifierFunction(property, $"{Randomize}{(int)intRange.min}-{(int)intRange.max}",
                                (targetProperty) => targetProperty.intValue = UnityEngine.Random.Range((int)intRange.min, (int)intRange.max));
                            return;
                        }
                    }
                }

                menu.AddPropertyModifierFunction(property,
                    $"{Set}Negate (*= -1)",
                    (targetProperty) => targetProperty.intValue *= -1);
                menu.AddPropertyModifierFunction(property,
                    $"{Randomize}0-1",
                    (targetProperty) => targetProperty.intValue = UnityEngine.Random.Range(0, 2));
                menu.AddPropertyModifierFunction(property,
                    $"{Randomize}0-99",
                    (targetProperty) => targetProperty.intValue = UnityEngine.Random.Range(0, 100));
            }
        }

        /************************************************************************************************************************/

        public sealed class StringHandler : ContextMenuHandler<string>
        {
            public override string GetTypeName(SerializedProperty property) => "string";

            public override void AddCustomItems(GenericMenu menu, SerializedProperty property)
            {
                menu.AddSeparator("");
                menu.AddPropertyModifierFunction(property,
                    $"{Set}To Lower",
                    (targetProperty) => targetProperty.stringValue = targetProperty.stringValue.ToLower());
                menu.AddPropertyModifierFunction(property,
                    $"{Set}To Upper",
                    (targetProperty) => targetProperty.stringValue = targetProperty.stringValue.ToUpper());
            }

            /************************************************************************************************************************/

            public override bool TryParse(string value, out string result)
            {
                result = value;
                return result != null;
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        public sealed class FloatHandler : ContextMenuHandler<float>
        {
            public override string GetTypeName(SerializedProperty property) => "float";

            public override void AddCustomItems(GenericMenu menu, SerializedProperty property)
            {
                menu.AddSeparator("");

                var accessor = property.GetAccessor();
                if (accessor != null)
                {
                    var field = accessor.GetField(property);
                    if (field != null)
                    {
                        var floatRange = field.GetCustomAttribute<RangeAttribute>(true);
                        if (floatRange != null)
                        {
                            menu.AddPropertyModifierFunction(property, $"{Randomize}{floatRange.min}-{floatRange.max}",
                                (targetProperty) => targetProperty.floatValue = UnityEngine.Random.Range(floatRange.min, floatRange.max));

                            if ((int)floatRange.min != (int)floatRange.max)
                            {
                                menu.AddPropertyModifierFunction(property, Convert + "Round to Integer",
                                    (targetProperty) => targetProperty.floatValue = Mathf.Clamp(Mathf.Round(targetProperty.floatValue), floatRange.min, floatRange.max));
                            }

                            return;
                        }
                    }
                }

                AddSetFunction(menu, property, Set + "Zero (0)", 0);
                menu.AddPropertyModifierFunction(property,
                    $"{Randomize}0-1",
                    (targetProperty) => targetProperty.floatValue = UnityEngine.Random.value);
                menu.AddPropertyModifierFunction(property,
                    $"{Randomize}0-100",
                    (targetProperty) => targetProperty.floatValue = UnityEngine.Random.Range(0f, 100f));
                menu.AddPropertyModifierFunction(property,
                    $"{Randomize}0-360",
                    (targetProperty) => targetProperty.floatValue = UnityEngine.Random.Range(0f, 360f));

                var currentValue = property.floatValue;
                if (currentValue != 0)
                    menu.AddPropertyModifierFunction(property, Randomize + "0-" + currentValue, (targetProperty) => targetProperty.floatValue = UnityEngine.Random.Range(0f, currentValue));

                AddNegateFunction(menu, property);
                AddRoundFunction(menu, property);
                menu.AddPropertyModifierFunction(property,
                    $"{Convert}Degrees to Radians",
                    (targetProperty) => targetProperty.floatValue *= Mathf.Deg2Rad);
                menu.AddPropertyModifierFunction(property,
                    $"{Convert}Radians to Degrees",
                    (targetProperty) => targetProperty.floatValue *= Mathf.Rad2Deg);

                menu.AddSeparator("");
                PropertyVisualiserWindow.AddVisualiseItem<FloatVisualiserWindow>(menu, property);
            }

            public static void AddSetFunction(GenericMenu menu, SerializedProperty property, string label, float value)
                => menu.AddPropertyModifierFunction(property,
                    label,
                    (targetProperty) => targetProperty.floatValue = value);

            public static void AddNegateFunction(GenericMenu menu, SerializedProperty property)
                => menu.AddPropertyModifierFunction(property,
                    $"{Set}Negate (*= -1)",
                    (targetProperty) => targetProperty.floatValue *= -1);

            public static void AddRoundFunction(GenericMenu menu, SerializedProperty property)
                => menu.AddPropertyModifierFunction(property,
                    $"{Convert}Round to Int",
                    (targetProperty) => targetProperty.floatValue = Mathf.Round(targetProperty.floatValue));

            /************************************************************************************************************************/

            public override bool TryParse(string value, out float result)
                => float.TryParse(value, out result);

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        public sealed class Vector2Handler : ContextMenuHandler<Vector2>
        {
            public override string GetTypeName(SerializedProperty property) => nameof(Vector2);

            public override void AddCustomItems(GenericMenu menu, SerializedProperty property)
            {
                menu.AddSeparator("");

                menu.AddPropertyModifierFunction(property,
                    $"{Set}Zero (0, 0)",
                    (targetProperty) => targetProperty.vector2Value = default);
                menu.AddPropertyModifierFunction(property,
                    $"{Set}Right (1, 0)",
                    (targetProperty) => targetProperty.vector2Value = Vector2.right);
                menu.AddPropertyModifierFunction(property,
                    $"{Set}Up (0, 1)",
                    (targetProperty) => targetProperty.vector2Value = Vector2.up);
                menu.AddPropertyModifierFunction(property,
                    $"{Set}One (1, 1)",
                    (targetProperty) => targetProperty.vector2Value = Vector2.one);

                menu.AddPropertyModifierFunction(property,
                    $"{Set}Negate (*= -1)",
                    (targetProperty) => targetProperty.vector2Value *= -1);

                menu.AddPropertyModifierFunction(property,
                    $"{Set}Normalize",
                    (targetProperty) => targetProperty.vector2Value = targetProperty.vector2Value.normalized);

                menu.AddPropertyModifierFunction(property,
                    $"{Randomize}Inside Unit Circle",
                    (targetProperty) => targetProperty.vector2Value = UnityEngine.Random.insideUnitCircle);

                menu.AddSeparator("");
                PropertyVisualiserWindow.AddVisualiseItem<Vector2VisualiserWindow>(menu, property);
            }

            /************************************************************************************************************************/

            public override bool TryParse(string value, out Vector2 result)
            {
                var success = NullableVector4.TryParse(value, 2, out var parsedValue);
                result = parsedValue.ToVector2();
                return success > 0;
            }

            public override string GetDisplayString(Vector2 value)
            {
                return $"({value.x.ToDisplayString()}, {value.y.ToDisplayString()})";
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        public sealed class Vector3Handler : ContextMenuHandler<Vector3>
        {
            public override string GetTypeName(SerializedProperty property) => nameof(Vector3);

            public override void AddCustomItems(GenericMenu menu, SerializedProperty property)
            {
                menu.AddSeparator("");

                var accessor = property.GetAccessor();
                if (accessor != null)
                {
                    var field = accessor.GetField(property);
                    if (field != null && !field.IsDefined(typeof(EulerAttribute), true))
                    {
                        menu.AddPropertyModifierFunction(property,
                            $"{Set}Zero (0, 0, 0)",
                            (targetProperty) => targetProperty.vector3Value = default);
                        menu.AddPropertyModifierFunction(property,
                            $"{Set}Right (1, 0, 0)",
                            (targetProperty) => targetProperty.vector3Value = Vector3.right);
                        menu.AddPropertyModifierFunction(property,
                            $"{Set}Up (0, 1, 0)",
                            (targetProperty) => targetProperty.vector3Value = Vector3.up);
                        menu.AddPropertyModifierFunction(property,
                            $"{Set}Forward (0, 0, 1)",
                            (targetProperty) => targetProperty.vector3Value = Vector3.forward);
                        menu.AddPropertyModifierFunction(property,
                            $"{Set}One (1, 1, 1)",
                            (targetProperty) => targetProperty.vector3Value = Vector3.one);

                        menu.AddPropertyModifierFunction(property,
                            $"{Set}Normalize",
                            (targetProperty) => targetProperty.vector3Value = targetProperty.vector3Value.normalized);
                    }
                }

                menu.AddPropertyModifierFunction(property,
                    $"{Set}Negate (*= -1)",
                    (targetProperty) => targetProperty.vector3Value *= -1);

                menu.AddPropertyModifierFunction(property,
                    $"{Randomize}Inside Unit Sphere",
                    (targetProperty) => targetProperty.vector3Value = UnityEngine.Random.insideUnitSphere);
                menu.AddPropertyModifierFunction(property,
                    $"{Randomize}On Unit Sphere",
                    (targetProperty) => targetProperty.vector3Value = UnityEngine.Random.onUnitSphere);
                menu.AddPropertyModifierFunction(property,
                    $"{Randomize}Euler Angles",
                    (targetProperty) => targetProperty.vector3Value = UnityEngine.Random.rotationUniform.eulerAngles);

                menu.AddSeparator("");
                PropertyVisualiserWindow.AddVisualiseItem<Vector3VisualiserWindow>(menu, property);
            }

            /************************************************************************************************************************/

            public override bool TryParse(string value, out Vector3 result)
            {
                var success = NullableVector4.TryParse(value, 3, out var parsedValue);
                result = parsedValue.ToVector3();
                return success > 0;
            }

            public override string GetDisplayString(Vector3 value)
                => $"({value.x.ToDisplayString()}, {value.y.ToDisplayString()}, {value.z.ToDisplayString()})";

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        public sealed class Vector4Handler : ContextMenuHandler<Vector4>
        {
            public override string GetTypeName(SerializedProperty property) => nameof(Vector4);

            public override void AddCustomItems(GenericMenu menu, SerializedProperty property)
            {
                menu.AddSeparator("");

                menu.AddPropertyModifierFunction(property,
                    $"{Set}Zero (0, 0, 0, 0)",
                    (targetProperty) => targetProperty.vector4Value = Vector4.zero);
                menu.AddPropertyModifierFunction(property,
                    $"{Set}One (1, 1, 1, 1)",
                    (targetProperty) => targetProperty.vector4Value = Vector4.one);

                menu.AddPropertyModifierFunction(property,
                    $"{Set}Negate (*= -1)",
                    (targetProperty) => targetProperty.vector4Value *= -1);

                menu.AddPropertyModifierFunction(property,
                    $"{Set}Normalize",
                    (targetProperty) => targetProperty.vector4Value = targetProperty.vector4Value.normalized);
            }

            /************************************************************************************************************************/

            public override bool TryParse(string value, out Vector4 result)
            {
                var success = NullableVector4.TryParse(value, 4, out var parsedValue);
                result = parsedValue.ToVector4();
                return success > 0;
            }

            public override string GetDisplayString(Vector4 value)
            {
                return $"(" +
                    $"{value.x.ToDisplayString()}, " +
                    $"{value.y.ToDisplayString()}, " +
                    $"{value.z.ToDisplayString()}, " +
                    $"{value.w.ToDisplayString()})";
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        public sealed class QuaternionHandler : ContextMenuHandler<Quaternion>
        {
            public override string GetTypeName(SerializedProperty property) => nameof(Quaternion);

            public override void AddCustomItems(GenericMenu menu, SerializedProperty property)
            {
                menu.AddSeparator("");
                menu.AddPropertyModifierFunction(property,
                    $"{Set}Identity (Euler 0, 0, 0)",
                    (targetProperty) => targetProperty.quaternionValue = Quaternion.identity);

                menu.AddPropertyModifierFunction(property,
                    $"{Set}Negate (*= -1)",
                    (targetProperty) =>
                    {
                        var euler = targetProperty.quaternionValue.eulerAngles;
                        euler *= -1;
                        targetProperty.quaternionValue = Quaternion.Euler(euler);
                    });

                menu.AddPropertyModifierFunction(property,
                    $"{Randomize}Rotation",
                    (targetProperty) => targetProperty.quaternionValue = UnityEngine.Random.rotationUniform);
            }

            public override void AddLogFunction(GenericMenu menu, SerializedProperty property)
            {
                menu.AddSeparator("");
                AddLogValueFunction(menu, property, GetDisplayString, "Log Euler Angles");
                AddLogValueFunction(menu, property,
                    (targetProperty) => GetFullString(targetProperty.quaternionValue), $"Log {nameof(Quaternion)}");
            }

            public static string GetFullString(Quaternion value)
            {
                return "(" +
                    $"{value.x.ToDisplayString()}, " +
                    $"{value.y.ToDisplayString()}, " +
                    $"{value.z.ToDisplayString()}, " +
                    $"{value.w.ToDisplayString()})";
            }

            public override string GetDisplayString(Quaternion value)
            {
                var euler = value.eulerAngles;
                return $"({euler.x.ToDisplayString()}, {euler.y.ToDisplayString()}, {euler.z.ToDisplayString()})";
            }

            /************************************************************************************************************************/

            public override bool TryParse(string value, out Quaternion result)
            {
                var success = NullableVector4.TryParse(value, 4, out var parsedValue);
                if (success == 4)
                {
                    result = new Quaternion(
                        parsedValue.x.Value,
                        parsedValue.y.Value,
                        parsedValue.z.Value,
                        parsedValue.w.Value);
                    return true;
                }
                else if (success > 0)
                {
                    var euler = parsedValue.ToVector3();
                    result = Quaternion.Euler(euler);
                    return true;
                }
                else
                {
                    result = default;
                    return false;
                }
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        public sealed class RectHandler : ContextMenuHandler<Rect>
        {
            public override string GetTypeName(SerializedProperty property) => nameof(Rect);

            public override void AddCustomItems(GenericMenu menu, SerializedProperty property)
            {
                menu.AddSeparator("");
                menu.AddPropertyModifierFunction(property,
                    $"{Set}Zero (0, 0, 0, 0)",
                    (targetProperty) => targetProperty.rectValue = new Rect());
                menu.AddPropertyModifierFunction(property,
                    $"{Set}Make Square",
                    (targetProperty) =>
                    {
                        var rect = targetProperty.rectValue;
                        var size = Mathf.Floor((rect.width + rect.height) * 0.5f);
                        targetProperty.rectValue = new Rect(rect.x, rect.y, size, size);
                    });
            }

            /************************************************************************************************************************/

            public override string GetDisplayString(Rect value)
            {
                return
                    $"({nameof(value.x)}: {value.x.ToDisplayString()}," +
                    $" {nameof(value.y)}: {value.y.ToDisplayString()}," +
                    $" {nameof(value.width)}: {value.width.ToDisplayString()}," +
                    $" {nameof(value.height)}: {value.height.ToDisplayString()})";
            }

            public override bool TryParse(string value, out Rect result)
            {
                result = default;
                var success = false;

                int start, end;
                string substring;

                // X.
                start = value.IndexOf("x:", StringComparison.CurrentCultureIgnoreCase);
                if (start >= 0)
                {
                    start += 2;
                    end = value.IndexOf(',', start + 1);
                    if (end < 0) end = value.Length;
                    substring = value[start..end];

                    if (float.TryParse(substring, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
                    {
                        result.x = floatValue;
                        success = true;
                    }
                }

                // Y.
                start = value.IndexOf("y:", StringComparison.CurrentCultureIgnoreCase);
                if (start >= 0)
                {
                    start += 2;
                    end = value.IndexOf(',', start + 1);
                    if (end < 0) end = value.Length;
                    substring = value[start..end];

                    if (float.TryParse(substring, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
                    {
                        result.y = floatValue;
                        success = true;
                    }
                }

                // Width.
                start = value.IndexOf("width:", StringComparison.CurrentCultureIgnoreCase);
                if (start >= 0)
                {
                    start += 6;
                    end = value.IndexOf(',', start + 1);
                    if (end < 0) end = value.Length;
                    substring = value[start..end];

                    if (float.TryParse(substring, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
                    {
                        result.width = floatValue;
                        success = true;
                    }
                }

                // Height.
                start = value.IndexOf("height:", StringComparison.CurrentCultureIgnoreCase);
                if (start >= 0)
                {
                    start += 7;
                    end = value.IndexOf(')', start + 1);
                    if (end < 0)
                    {
                        end = value.IndexOf(',', start + 1);
                        if (end < 0) end = value.Length;
                    }
                    substring = value[start..end];

                    if (float.TryParse(substring, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
                    {
                        result.height = floatValue;
                        success = true;
                    }
                }

                if (success)
                {
                    return true;
                }
                else if (Vector4MenuHandler.TryParse(value, out var vector))
                {
                    result = new Rect(vector.x, vector.y, vector.z, vector.w);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        public sealed class BoundsHandler : ContextMenuHandler<Bounds>
        {
            public override string GetTypeName(SerializedProperty property) => nameof(Bounds);

            public override void AddCustomItems(GenericMenu menu, SerializedProperty property) { }

            /************************************************************************************************************************/

            public override string GetDisplayString(Bounds value)
            {
                var text = new StringBuilder();

                text.Append("Center: (");
                text.Append(value.center.x.ToDisplayString());
                text.Append(", ");
                text.Append(value.center.y.ToDisplayString());
                text.Append(", ");
                text.Append(value.center.z.ToDisplayString());
                text.Append("), Extents: (");
                text.Append(value.extents.x.ToDisplayString());
                text.Append(", ");
                text.Append(value.extents.y.ToDisplayString());
                text.Append(", ");
                text.Append(value.extents.z.ToDisplayString());
                text.Append(")");

                return text.ToString();
            }

            public override bool TryParse(string value, out Bounds result)
            {
                result = default;

                var success = false;

                int start, end;
                string substring;

                // Center.
                start = value.IndexOf("Center: (", StringComparison.CurrentCultureIgnoreCase);
                if (start >= 0)
                {
                    start += 9;
                    end = value.IndexOf(')', start + 1);
                    if (end < 0) end = value.Length;
                    substring = value[start..end];

                    if (NullableVector4.TryParse(substring, 3, out var parser) >= 0)
                    {
                        result.center = parser.ToVector3();
                        success = true;
                    }
                }

                // Extents.
                start = value.IndexOf("Extents: (", StringComparison.CurrentCultureIgnoreCase);
                if (start >= 0)
                {
                    start += 10;
                    end = value.IndexOf(')', start + 1);
                    if (end < 0) end = value.Length;
                    substring = value[start..end];

                    if (NullableVector4.TryParse(substring, 3, out var parser) >= 0)
                    {
                        result.extents = parser.ToVector3();
                        success = true;
                    }
                }

                return success;
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        public sealed class ColorHandler : ContextMenuHandler<Color>
        {
            public override string GetTypeName(SerializedProperty property) => nameof(Color);

            public override void AddCustomItems(GenericMenu menu, SerializedProperty property)
            {
                menu.AddSeparator("");

                menu.AddPropertyModifierFunction(property, $"{Set}Invert", (targetProperty) =>
                {
                    var color = targetProperty.colorValue;
                    color.r = 1 - color.r;
                    color.g = 1 - color.g;
                    color.b = 1 - color.b;
                    color.a = 1 - color.a;
                    targetProperty.colorValue = color;
                });

                menu.AddPropertyModifierFunction(property, $"{Randomize}Hue", (targetProperty) =>
                {
                    var color = targetProperty.colorValue;
                    Color.RGBToHSV(color, out color.r, out color.g, out color.b);
                    color.r = UnityEngine.Random.Range(0f, 1f);
                    targetProperty.colorValue = Color.HSVToRGB(color.r, color.g, color.b);
                });
            }

            /************************************************************************************************************************/

            public override string GetDisplayString(Color value)
            {
                return $"RGBA(" +
                    $"{value.r.ToDisplayString()}, " +
                    $"{value.g.ToDisplayString()}, " +
                    $"{value.b.ToDisplayString()}, " +
                    $"{value.a.ToDisplayString()})";
            }

            public override bool TryParse(string value, out Color result)
            {
                var success = NullableVector4.TryParse(value, 4, out var parsedValue);
                if (success == 4)
                {
                    result = new Color(parsedValue.x.Value, parsedValue.y.Value, parsedValue.z.Value, parsedValue.w.Value);
                }
                else if (success == 3)
                {
                    result = new Color(parsedValue.x.Value, parsedValue.y.Value, parsedValue.z.Value, 1);
                }
                else
                {
                    result = default;
                    return false;
                }

                // Try to determine if the clipboard values are bytes or floats.
                // Unfortunately this will treat byte color (1, 1, 1, 1) which is near black as the float color white.
                // We could require float values to always show the decimal point and make a custom parser method.
                // But that should be rare enough to not be a significant issue.

                if (result.r > 1 || result.g > 1 || result.b > 1 || result.a > 1)// Byte values (0-255).
                {
                    const float Rescale = 1f / byte.MaxValue;
                    result.r *= Rescale;
                    result.g *= Rescale;
                    result.b *= Rescale;
                    result.a *= Rescale;
                }

                result.r = Mathf.Clamp01(result.r);
                result.g = Mathf.Clamp01(result.g);
                result.b = Mathf.Clamp01(result.b);
                result.a = Mathf.Clamp01(result.a);

                return true;
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        public sealed class EnumHandler : ContextMenuHandler
        {
            public override string GetTypeName(SerializedProperty property) => property.type;

            public override void AddCustomItems(GenericMenu menu, SerializedProperty property)
            {
                menu.AddSeparator("");
                menu.AddPropertyModifierFunction(property, "Randomize",
                    (targetProperty) => targetProperty.enumValueIndex = UnityEngine.Random.Range(0, targetProperty.enumNames.Length));
            }

            /************************************************************************************************************************/

            public override bool TryParse(string value, out object result)
            {
                try
                {
                    var accessor = CurrentProperty.GetAccessor();
                    var type = accessor.GetFieldElementType(CurrentProperty);
                    result = Enum.Parse(type, value, true);
                    return true;
                }
                catch
                {
                    result = null;
                    return false;
                }
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        public sealed class AnimationCurveHandler : ContextMenuHandler<AnimationCurve>
        {
            /************************************************************************************************************************/

            public override string GetTypeName(SerializedProperty property) => nameof(AnimationCurve);

            /************************************************************************************************************************/

            public override void AddClipboardFunctions(GenericMenu menu, SerializedProperty property)
            {
                var curve = property.animationCurveValue;

                curve.GetStartEndTime(out var start, out var end);
                menu.AddDisabledItem(new GUIContent($"Time: {start} -> {end}"));

                curve.GetStartEndValue(out start, out end);
                menu.AddDisabledItem(new GUIContent($"Value: {start} -> {end}"));

                base.AddClipboardFunctions(menu, property);
            }

            /************************************************************************************************************************/

            public override void AddCustomItems(GenericMenu menu, SerializedProperty property)
            {
                menu.AddSeparator("");

                AddCurveModifierFunction(menu, property,
                    $"{Set}Normalize",
                    (curve) => curve.Normalize());

                AddCurveModifierFunction(menu, property,
                    $"{Set}Smooth Tangents",
                    (curve) => curve.SmoothTangents());

                AddCurveModifierFunction(menu, property,
                    $"{Set}Flip/Horizontal",
                    (curve) => curve.FlipHorizontal());
                AddCurveModifierFunction(menu, property,
                    $"{Set}Flip/Vertical",
                    (curve) => curve.FlipVertical());
                AddCurveModifierFunction(menu, property,
                    $"{Set}Flip/Both Axes",
                    (curve) => curve.FlipHorizontal().FlipVertical());
                AddCurveModifierFunction(menu, property,
                    $"{Set}Flip/Extend Mirrorred",
                    (curve) => curve.ExtendMirrorred());
                AddCurveModifierFunction(menu, property,
                    $"{Set}Flip/Extend Mirrorred (Normalize)",
                    (curve) => curve.ExtendMirrorred().Normalize());
                AddCurveModifierFunction(menu, property,
                    $"{Set}Flip/Enforce Horizontal Symmetry",
                    (curve) => curve.EnforceHorizontalSymmetry());
            }

            /************************************************************************************************************************/

            private static void AddCurveModifierFunction(GenericMenu menu, SerializedProperty property,
                string label, Action<AnimationCurve> function)
            {
                menu.AddPropertyModifierFunction(property, label, (targetProperty) =>
                {
                    var curve = targetProperty.animationCurveValue;
                    function(curve);
                    targetProperty.animationCurveValue = curve;
                });
            }

            /************************************************************************************************************************/

            public override string GetDisplayString(AnimationCurve value) => IGUtils.GetDescription(value);

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        public sealed class ObjectReferenceHandler : ContextMenuHandler<Object>
        {
            /************************************************************************************************************************/

            public static string GetPropertyTypeName(SerializedProperty property)
            {
                var name = property.type;

                if (name.Length > 6 && name.StartsWith("PPtr<"))
                {
                    var start = 5;

                    if (name[start] == '$')
                        start++;

                    return name[start..^1];
                }

                return name;
            }

            public override string GetTypeName(SerializedProperty property) => GetPropertyTypeName(property);

            /************************************************************************************************************************/

            private static readonly Dictionary<string, Type>
                UnderlyingTypes = new Dictionary<string, Type>();

            public static Type GetUnderlyingType(SerializedProperty property)
            {
                var accessor = property.GetAccessor();
                if (accessor != null)
                {
                    return accessor.GetFieldElementType(property);
                }
                else
                {
                    if (!UnderlyingTypes.TryGetValue(property.type, out var type))
                    {
                        var typeName = $"{nameof(UnityEngine)}.{GetPropertyTypeName(property)}";
                        type = typeof(Application).Assembly.GetType(typeName);
                        UnderlyingTypes.Add(property.type, type);
                    }

                    return type;
                }
            }

            /************************************************************************************************************************/

            public override void AddNameItems(GenericMenu menu, SerializedProperty property)
            {
                base.AddNameItems(menu, property);
                AddPathFunction(menu, property);
            }

            /************************************************************************************************************************/

            private static void AddPathFunction(GenericMenu menu, SerializedProperty property)
            {
                var obj = property.objectReferenceValue;

                var path = AssetDatabase.GetAssetPath(obj);
                string pathType;

                if (!string.IsNullOrEmpty(path))
                {
                    path = path.Replace('/', '\\');
                    pathType = "Asset";
                    goto AddFunction;
                }

                if (!(property.serializedObject.targetObject is Component thisComponent))
                    return;

                if (!(obj is GameObject referencedGameObject))
                {
                    if (!(obj is Component referencedComponent))
                        return;

                    referencedGameObject = referencedComponent.gameObject;
                }

                var thisTransform = thisComponent.transform;
                var referencedTransform = referencedGameObject.transform;
                path = GetRelativePath(thisTransform, referencedTransform, '\\', out pathType);

                AddFunction:
                menu.AddItem(new GUIContent($"{pathType} Path/{path}"), false, () =>
                {
                    EditorGUIUtility.systemCopyBuffer = path;
                    Debug.Log($"Copied '{path}' to the clipboard.");
                });
            }

            /************************************************************************************************************************/

            public static string GetRelativePath(Transform from, Transform to, char separator, out string pathType)
            {
                if (from == to)
                {
                    pathType = "Relative";
                    return "this";
                }

                var path = new StringBuilder();

                if (from.root == to.root)
                {
                    pathType = "Relative";
                    path.Append("this").Append(separator);

                    // Find the first common parent.
                    while (!to.IsChildOf(from))
                    {
                        from = from.parent;
                        path.Append("..").Append(separator);
                    }
                }
                else
                {
                    pathType = "Hierarchy";
                    from = to.root;
                    path.Append(from.name);
                    if (from != to)
                        path.Append(separator);
                }

                // Now thisTransform is a parent of referencedTransform.
                if (to != from)
                {
                    var upEnd = path.Length;
                    while (true)
                    {
                        path.Insert(upEnd, to.name);
                        to = to.parent;

                        if (to == from)
                            break;

                        path.Insert(upEnd, separator);
                    }
                }

                return path.ToString();
            }

            /************************************************************************************************************************/

            public override void AddCustomItems(GenericMenu menu, SerializedProperty property)
            {
                menu.AddSeparator("");

                menu.AddPropertyModifierFunction(property, Set + "Null", (targetProperty) => targetProperty.objectReferenceValue = null);

                var reference = property.objectReferenceValue;
                var state = reference != null && !EditorUtility.IsPersistent(reference) ?
                    MenuFunctionState.Normal : MenuFunctionState.Disabled;
                menu.AddPropertyModifierFunction(property, "Destroy", state, (targetProperty) =>
                {
                    Undo.DestroyObjectImmediate(targetProperty.objectReferenceValue);
                    targetProperty.objectReferenceValue = null;
                });

                var value = property.objectReferenceValue;
                if (value != null && !property.hasMultipleDifferentValues)
                {
                    menu.AddItem(new GUIContent("Open Inspector"), false, () =>
                    {
                        var component = value as Component;
                        if (component != null)
                            value = component.gameObject;

                        IGEditorUtils.NewLockedInspector(value);
                    });
                }

                var type = GetUnderlyingType(property);
                if (type == null)
                {
                    menu.AddDisabledItem(new GUIContent("Unable to determine underlying property type"));
                }
                else
                {
                    menu.AddPropertyModifierFunction(property, Set + "Find Object of Type",
                        (targetProperty) => targetProperty.objectReferenceValue =
                            IGUtils.GetBestMatch(Resources.FindObjectsOfTypeAll(type), targetProperty.displayName));

                    menu.AddPropertyModifierFunction(property, Set + "Find Asset of Type",
                        (targetProperty) => targetProperty.objectReferenceValue =
                            IGEditorUtils.FindAssetOfType(type, targetProperty.displayName));

                    var area = new Rect(GUIUtility.GUIToScreenPoint(Event.current.mousePosition), default);
                    menu.AddItem(new GUIContent(Set + "Browse ..."), false,
                        () => EditorApplication.delayCall += () => ChooseComponentRelative(property, area, type));

                    if (typeof(Component).IsAssignableFrom(type))
                    {
                        AddComponentFunctions(menu, property, type);
                    }
                    else if (typeof(ScriptableObject).IsAssignableFrom(type))
                    {
                        AddScriptableObjectFunctions(menu, property, type);
                    }
                }

                AddSaveAsAssetFunction(menu, property);
            }

            /************************************************************************************************************************/

            private static void AddComponentFunctions(GenericMenu menu, SerializedProperty property, Type fieldType)
            {
                AddSetObjectReferenceFunction<Component>(menu, property, Set + "Find Component (Progressive Search)",
                    (target) => IGUtils.ProgressiveSearch(target.gameObject, fieldType, property.displayName));

                if (typeof(Transform).IsAssignableFrom(fieldType))
                    return;

                var derivedTypes = fieldType.GetDerivedTypes(true);
                if (derivedTypes.Count == 0)
                {
                    return;
                }
                else
                {
                    for (int i = 0; i < derivedTypes.Count; i++)
                    {
                        var type = derivedTypes[i];

                        var label = type.GetNameCS();
                        if (derivedTypes.Count > 1)
                            label = $"{Set}Add Component ->/{label}";
                        else
                            label = $"{Set}Add Component ({label})";

                        AddSetObjectReferenceFunction<Component>(menu, property, label,
                            (target) => Undo.AddComponent(target.gameObject, type));
                    }
                }
            }

            /************************************************************************************************************************/

            private static void ChooseComponentRelative(SerializedProperty property, Rect area, Type fieldType)
            {
                if (!typeof(Component).IsAssignableFrom(fieldType))
                {
                    ChooseAsset(property, area, fieldType);
                    return;
                }

                var menu = new GenericMenu();

                var self = ((Component)property.serializedObject.targetObject).transform;

                var objects = Resources.FindObjectsOfTypeAll(fieldType);
                var paths = new string[objects.Length];

                var skip = 0;
                for (int i = 0; i < objects.Length; i++)
                {
                    var component = (Component)objects[i];
                    if (EditorUtility.IsPersistent(component))
                    {
                        skip++;
                        continue;
                    }

                    var path = $"{GetRelativePath(self, component.transform, '/', out var pathType)}" +
                        $" ({component.GetType().GetNameCS(false)})";

                    var components = component.GetComponents(fieldType);
                    if (components.Length > 1)
                    {
                        var index = Array.IndexOf(component.GetComponents(fieldType), component);
                        path += $" [{index}]";
                    }

                    paths[i] = path;
                }

                if (skip < objects.Length)
                {
                    Array.Sort(paths, objects);

                    for (int i = skip; i < objects.Length; i++)
                    {
                        var path = paths[i];
                        if (path == null)
                            continue;

                        var component = objects[i];

                        menu.AddPropertyModifierFunction(property, paths[i], (targetProperty) =>
                        {
                            targetProperty.objectReferenceValue = component;
                        });
                    }

                    menu.AddItem(new GUIContent(Set + "Browse in Prefabs ..."), false,
                        () => EditorApplication.delayCall += () => ChooseAsset(property, area, fieldType));

                    menu.DropDown(area);
                }
                else
                {
                    ChooseAsset(property, area, fieldType);
                }
            }

            /************************************************************************************************************************/

            private static void ChooseAsset(SerializedProperty property, Rect area, Type type)
            {
                var menu = new GenericMenu();

                var guids = IGEditorUtils.FindAssetGuidsOfType(type);
                var assets = new Object[guids.Length];

                var paths = guids;
                var skip = 0;
                for (int i = 0; i < guids.Length; i++)
                {
                    var path = paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
                    var asset = assets[i] = AssetDatabase.LoadAssetAtPath(path, type);
                    if (asset == null)
                    {
                        paths[i] = null;
                        skip++;
                    }
                }

                if (skip < paths.Length)
                {
                    Array.Sort(paths, assets);

                    for (int i = skip; i < paths.Length; i++)
                    {
                        var component = assets[i];
                        menu.AddPropertyModifierFunction(property, paths[i], (targetProperty) =>
                        {
                            targetProperty.objectReferenceValue = component;
                        });
                    }
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent(
                        $"No {type.GetNameCS()} Assets Found"));
                }

                menu.DropDown(area);
            }

            /************************************************************************************************************************/

            private static void AddScriptableObjectFunctions(GenericMenu menu, SerializedProperty property, Type fieldType)
            {
                var derivedTypes = fieldType.GetDerivedTypes(true);

                for (int i = derivedTypes.Count - 1; i >= 0; i--)
                {
                    if (derivedTypes[i].IsGenericTypeDefinition)
                        derivedTypes.RemoveAt(i);
                }

                if (derivedTypes.Count == 0)
                {
                    return;
                }
                else if (derivedTypes.Count == 1 && derivedTypes[0] == fieldType)
                {
                    menu.AddPropertyModifierFunction(property, Set + "Create new Instance",
                        (targetProperty) => CreateScriptableObject(targetProperty, fieldType));
                }
                else
                {
                    for (int i = 0; i < derivedTypes.Count; i++)
                    {
                        var type = derivedTypes[i];
                        var label = Set + "Create new Instance ->/" + type.GetNameCS();
                        menu.AddPropertyModifierFunction(property, label,
                            (targetProperty) => CreateScriptableObject(targetProperty, type));
                    }
                }
            }

            private static void CreateScriptableObject(SerializedProperty property, Type type)
            {
                var obj = ScriptableObject.CreateInstance(type);
                obj.name = type.Name;

                // If the target object is an asset, we need to save the new object inside it.
                if (EditorUtility.IsPersistent(property.serializedObject.targetObject))
                {
                    AssetDatabase.AddObjectToAsset(obj, property.serializedObject.targetObject);
                }

                property.objectReferenceValue = obj;
            }

            /************************************************************************************************************************/

            public override string GetDisplayString(Object value) => value != null ? value.ToString() : "null";

            /************************************************************************************************************************/

            public override bool TryParse(string value, out Object result)
            {
                // Try interpreting the format "Name (Type)".
                var close = value.LastIndexOf(')');
                if (close >= 2)
                {
                    var open = value.LastIndexOf('(', close - 1);
                    if (open > 1)
                    {
                        var typeName = value[(open + 1)..close];
                        var type = IGUtils.FindType(typeName, true);
                        if (TryFind(value[..(open - 1)], type, out result))
                            return true;
                    }
                }

                // Otherwise try to find an object of the current property type using the full string.
                var accessor = CurrentProperty?.GetAccessor();
                if (accessor != null)
                {
                    var elementType = accessor.GetFieldElementType(CurrentProperty);
                    return TryFind(value, elementType, out result);
                }

                result = null;
                return false;
            }

            public static bool TryFind(string name, Type type, out Object result)
            {
                if (type == null)
                {
                    result = null;
                    return false;
                }

                var objects = IGEditorUtils.FindObjects(type);

                for (int i = 0; i < objects.Length; i++)
                {
                    result = objects[i];
                    if (string.Compare(name, result.name, true) == 0)
                        return true;
                }

                result = null;
                return false;
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        public class GenericHandler : ContextMenuHandler
        {
            /************************************************************************************************************************/

            public override string GetTypeName(SerializedProperty property)
            {
                var accessor = property.GetAccessor();
                if (accessor != null)
                {
                    var propertyType = accessor.GetFieldElementType(property);
                    if (propertyType != null)
                        return propertyType.GetNameCS();
                }

                return "Unknown Type";
            }

            /************************************************************************************************************************/

            public override void AddCustomItems(GenericMenu menu, SerializedProperty property)
            {
                if (!property.isArray)
                    return;

                var accessor = property.GetAccessor();
                if (accessor == null)
                    return;

                var fieldType = accessor.GetField(property).FieldType;
                var elementType = Serialization.CollectionPropertyAccessor.GetElementType(fieldType);

                if (typeof(Object).IsAssignableFrom(elementType))
                {
                    menu.AddSeparator("");

                    menu.AddPropertyModifierFunction(property, Set + "Clear Array", (targetProperty) => targetProperty.ClearArray());

                    AddSetObjectReferenceArrayFunction<Object>(menu, property,
                        $"{Set}Find Objects of Type (in the Scene)",
                        target => IGEditorUtils.FindObjects(elementType));
                    AddSetObjectReferenceArrayFunction<Object>(menu, property,
                        $"{Set}Find Objects of Type (including Assets)",
                        (target) =>
                        {
                            var guids = IGEditorUtils.FindAssetGuidsOfType(elementType);

                            var objects = new Object[guids.Length];
                            for (int i = 0; i < guids.Length; i++)
                            {
                                objects[i] = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[i]), elementType);
                            }

                            return objects;
                        });

                    if (typeof(Component).IsAssignableFrom(elementType))
                    {
                        AddSetObjectReferenceArrayFunction<Component>(menu, property,
                            $"{Set}Get Components",
                            (target) => target.GetComponents(elementType));
                        AddSetObjectReferenceArrayFunction<Component>(menu, property,
                            $"{Set}Get Components in Children",
                            (target) => target.GetComponentsInChildren(elementType));
                        AddSetObjectReferenceArrayFunction<Component>(menu, property,
                            $"{Set}Get Components in Parent",
                            (target) => target.GetComponentsInParent(elementType));
                    }
                }
            }

            /************************************************************************************************************************/

            public override string GetDisplayString(object value)
            {
                if (value is IEnumerable enumerable)
                    return IGUtils.DeepToString(enumerable);
                else
                    return base.GetDisplayString(value);
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        public sealed class ManagedReferenceHandler : GenericHandler
        {
            /************************************************************************************************************************/

            public override void AddCustomItems(GenericMenu menu, SerializedProperty property)
            {
                var accessor = property.GetAccessor();
                if (accessor == null)
                    return;

                var valueType = accessor.GetValue(property)?.GetType();

                AddTypeSelector(menu, property, valueType, null);

                var baseType = accessor.GetFieldElementType(property);
                var derivedTypes = baseType.GetDerivedTypes(true);

                for (int i = 0; i < derivedTypes.Count; i++)
                    AddTypeSelector(menu, property, valueType, derivedTypes[i]);
            }

            /************************************************************************************************************************/

            private static void AddTypeSelector(GenericMenu menu, SerializedProperty property, Type selectedType, Type newType)
            {
                if (newType != null && !IsViableType(newType))
                    return;

                var label = Set + (newType != null ? newType.GetNameCS() : "Null");
                var state = selectedType == newType ? MenuFunctionState.Selected : MenuFunctionState.Normal;
                menu.AddPropertyModifierFunction(property, label, state, (targetProperty) =>
                {
                    var value = newType == null ?
                        null :
                        Activator.CreateInstance(newType, true);

                    targetProperty.managedReferenceValue = value;
                    targetProperty.isExpanded = true;
                });
            }

            /************************************************************************************************************************/

            private static bool IsViableType(Type type) =>
                !type.IsAbstract &&
                !type.IsEnum &&
                !type.IsGenericTypeDefinition &&
                !type.IsInterface &&
                !type.IsPrimitive &&
                !type.IsSpecialName &&
                type.Name[0] != '<' &&
                type.IsDefined(typeof(SerializableAttribute), false) &&
                !type.IsDefined(typeof(ObsoleteAttribute), true) &&
                !typeof(Object).IsAssignableFrom(type) &&
                type.GetConstructor(IGEditorUtils.InstanceBindings, null, Type.EmptyTypes, null) != null;

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Utils
        /************************************************************************************************************************/

        private static void AddSetObjectReferenceFunction<T>(GenericMenu menu, SerializedProperty property, string label,
            Func<T, Object> getValue) where T : Object
        {
            menu.AddPropertyModifierFunction(property, label, (targetProperty) =>
            {
                targetProperty.objectReferenceValue = getValue(targetProperty.serializedObject.targetObject as T);
            });
        }

        /************************************************************************************************************************/

        private static void AddSetObjectReferenceArrayFunction<T>(GenericMenu menu, SerializedProperty property, string label, Func<T, Object[]> getValues) where T : Object
        {
            menu.AddItem(new GUIContent(label), false, () =>
            {
                property.ForEachTarget((targetProperty) =>
                {
                    var values = getValues(targetProperty.serializedObject.targetObject as T);

                    targetProperty.Next(true);
                    targetProperty.arraySize = values.Length;
                    targetProperty.Next(true);

                    for (int j = 0; j < values.Length; j++)
                    {
                        targetProperty.Next(false);
                        targetProperty.objectReferenceValue = values[j];
                    }
                });
            });
        }

        /************************************************************************************************************************/

        private static void AddSaveAsAssetFunction(GenericMenu menu, SerializedProperty property)
        {
            var saveThis = property.objectReferenceValue;

            if (saveThis == null ||
                property.hasMultipleDifferentValues ||
                saveThis is Component ||
                saveThis is UnityEditor.Editor ||
                saveThis is EditorWindow)
                return;

            menu.AddItem(new GUIContent("Save as Asset"), false, () =>
            {
                var path = EditorUtility.SaveFilePanelInProject("Save as Asset", saveThis.name, "asset", "Save as Asset");
                if (string.IsNullOrEmpty(path))
                    return;

                var existingAsset = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (existingAsset == null || existingAsset.GetType() == saveThis.GetType())
                {
                    AssetDatabase.CreateAsset(saveThis, path);
                }
                else
                {
                    AssetDatabase.AddObjectToAsset(saveThis, path);
                }
            });
        }

        /************************************************************************************************************************/

        public static void AddLogValueFunction(GenericMenu menu, SerializedProperty property, Func<SerializedProperty, object> getValue, string label = "Log Value")
        {
            menu.AddItem(new GUIContent(label), false, () =>
            {
                var path = $" -> {property.propertyPath} = ";

                var targets = property.serializedObject.targetObjects;

                if (!property.hasMultipleDifferentValues)
                {
                    var target = targets[0];
                    Debug.Log(target + path + getValue(property), property.serializedObject.targetObject);
                    return;
                }
                else
                {
                    property.ForEachTarget((targetProperty) => Debug.Log(targetProperty.serializedObject.targetObject + path + getValue(targetProperty), property.serializedObject.targetObject));
                }
            });
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif
