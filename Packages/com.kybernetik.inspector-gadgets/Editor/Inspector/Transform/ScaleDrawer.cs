// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace InspectorGadgets.Editor.PropertyDrawers
{
    internal sealed class ScaleDrawer : TransformPropertyDrawer
    {
        /************************************************************************************************************************/

        public ScaleDrawer(TransformEditor parentEditor)
            : base(parentEditor,
                  "Scale",
                  "The local scaling of this Game Object relative to the parent.",
                  "The world scale of this Game Object.")
        { }

        /************************************************************************************************************************/

        public override void OnEnable(SerializedObject transform)
        {
            base.OnEnable(transform);
            _MainSerializedProperty = transform.FindProperty("m_LocalScale");
            _XSerializedProperty = _MainSerializedProperty.FindPropertyRelative("x");
            _YSerializedProperty = _MainSerializedProperty.FindPropertyRelative("y");
            _ZSerializedProperty = _MainSerializedProperty.FindPropertyRelative("z");
        }

        /************************************************************************************************************************/

        public override Vector3 GetWorldValue(Transform target)
        {
            var value = target.lossyScale;

            // If the value is very close to (1,1,1), just show that value.
            // The Vector3 equality operator is actually an approximation.
            if (value == Vector3.one)
                return Vector3.one;

            return value;
        }

        public override Vector3 GetLocalValue(Transform target) => target.localScale;

        public override void SetLocalValue(Transform target, Vector3 localScale)
        {
            if (ParentEditor.CurrentFreezeChildTransforms)
                PositionDrawer.CacheChildPositions(target);

            target.localScale = localScale;

            if (ParentEditor.CurrentFreezeChildTransforms)
                PositionDrawer.RevertChildPositions(target);
        }

        public override void SetWorldValue(Transform target, Vector3 worldScale)
        {
            if (ParentEditor.CurrentFreezeChildTransforms)
                PositionDrawer.CacheChildPositions(target);

            if (target.parent != null)
            {
                var parentWorldScale = target.parent.lossyScale;
                worldScale.x /= parentWorldScale.x;
                worldScale.y /= parentWorldScale.y;
                worldScale.z /= parentWorldScale.z;
            }

            target.localScale = worldScale;

            if (ParentEditor.CurrentFreezeChildTransforms)
                PositionDrawer.RevertChildPositions(target);
        }

        public override string UndoName => "Scale";
        public override Vector3 DefaultValue => Vector3.one;

        public override Vector3 SnapValues => IGEditorUtils.ScaleSnapVector;
        public override Vector3 SnapValue(Vector3 value) => IGEditorUtils.SnapScale(value);

        /************************************************************************************************************************/

        protected override Vector3 GetCurrentDragValue(Transform target)
        {
            var value = GetLocalValue(target);
            return value != default
                ? value
                : Vector3.one.normalized;
        }

        protected override void HandleDragValue(
            Transform target, Vector3 startingValue, Vector3 dragDelta, Vector3 dragOffset)
        {
            dragOffset.z = 0;
            if (dragOffset == default)
                return;

            dragOffset.y = -dragOffset.y;

            var abs = new Vector2(
                Mathf.Abs(dragOffset.x),
                Mathf.Abs(dragOffset.y));

            var axis = abs.x < abs.y ? 1 : 0;

            startingValue *= 1 + Mathf.Sign(dragOffset[axis]) * dragOffset.magnitude / Screen.dpi;

            SetLocalValue(target, startingValue);
        }

        /************************************************************************************************************************/

        private static readonly GUIContent PasteContentValue = new GUIContent("P");
        public override GUIContent PasteContent => PasteContentValue;

        private static readonly GUIContent SnapContentValue = new GUIContent("S");
        public override GUIContent SnapContent => SnapContentValue;

        protected override string GetSnapTooltip() => $"({IGEditorUtils.ScaleSnap})";

        /************************************************************************************************************************/

        private static readonly NullableVector4 ClipboardValue = new NullableVector4(Vector3.one);
        public override NullableVector4 PrivateClipboard => ClipboardValue;

        /************************************************************************************************************************/

        protected override void DoVectorField(Rect area, float labelRight)
        {
            DoUniformScaleToggleGUI(ref area);

            if (!ParentEditor.UseUniformScale || !HasUniformScale())
            {
                ParentEditor.UseUniformScale = false;
                base.DoVectorField(area, labelRight);
            }
            else
            {
                DoUniformScaleGUI(area);
            }
        }

        /************************************************************************************************************************/

        private void DoUniformScaleToggleGUI(ref Rect area)
        {
            var enabled = GUI.enabled;

            if (!ParentEditor.UseUniformScale &&
                !ParentEditor.CurrentIsLocalMode &&
                ParentEditor.Rotation.IsArbitrarilyRotated)
                GUI.enabled = false;

            var padding = IGEditorUtils.Spacing;
            var buttonArea = IGEditorUtils.StealFromLeft(ref area, InternalGUI.UniformScaleButtonStyle.fixedWidth + padding);
            buttonArea.x += padding;

            var content = ParentEditor.UseUniformScale ? EditorStrings.GUI.NonUniformScale : EditorStrings.GUI.UniformScale;
            if (GUI.Button(buttonArea, content, InternalGUI.UniformScaleButtonStyle))
            {
                ParentEditor.UseUniformScale = !ParentEditor.UseUniformScale;
                GUIUtility.hotControl = 0;
                GUIUtility.keyboardControl = 0;

                if (ParentEditor.UseUniformScale)
                    ApplyUniformScale();
            }

            GUI.enabled = enabled;
        }

        /************************************************************************************************************************/

        private bool HasUniformScale()
        {
            return
                DisplayValues.x == DisplayValues.y &&
                DisplayValues.x == DisplayValues.z;
        }

        /************************************************************************************************************************/

        private void ApplyUniformScale()
        {
            RecordTargetsForUndo(UndoName);

            for (int i = 0; i < Targets.Length; i++)
            {
                var target = Targets[i];
                var scale = GetCurrentValue(target);
                scale.x = scale.y = scale.z = (scale.x + scale.y + scale.z) / 3f;
                SetCurrentValue(target, scale);
            }

            UpdateDisplayValues();
        }

        /************************************************************************************************************************/

        private void DoUniformScaleGUI(Rect area)
        {
            area.xMin -= 2;
            area.width += 0.5f;// Not sure why, but this makes it line up properly.

            EditorGUI.BeginChangeCheck();

            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = InternalGUI.NameLabelWidth - 5;

            DisplayValues.x = MultiFloatField(
                area, _MainSerializedProperty, Color.white, CurrentLabel, DisplayValues.x, IGEditorUtils.ScaleSnap, 1);

            EditorGUIUtility.labelWidth = labelWidth;

            if (EditorGUI.EndChangeCheck() && DisplayValues.x != null)
            {
                RecordTargetsForUndo(UndoName);

                var x = DisplayValues.x.Value;
                var scale = new Vector3(x, x, x);
                for (int i = 0; i < Targets.Length; i++)
                    SetCurrentValue(i, scale);
            }
        }

        /************************************************************************************************************************/

        public override void OnPropertyContextMenu(GenericMenu menu, SerializedProperty property)
        {
            AddPropertyNameItem(menu, property);

            var axis = CurrentVectorAxis;
            if (axis < 0)// Vector Label.
            {
                AddVectorClipboardFunctions(menu);

                menu.AddSeparator("");

                menu.AddPropertyModifierFunction(property,
                    $"{SerializedPropertyContextMenu.Set}Negate (*= -1)",
                    (targetProperty) => targetProperty.vector3Value *= -1);

                menu.AddPropertyModifierFunction(property,
                    $"{SerializedPropertyContextMenu.Set}Average",
                    (targetProperty) =>
                    {
                        var value = targetProperty.vector3Value;
                        value.x = value.y = value.z = (value.x + value.y + value.z) / 3;
                        targetProperty.vector3Value = value;
                    });

                var currentEditor = ParentEditor;
                menu.AddPropertyModifierFunction(property,
                    $"{SerializedPropertyContextMenu.Randomize}0.5-1.5 (Uniform)",
                    (targetProperty) =>
                    {
                        var value = Random.value;
                        targetProperty.vector3Value = new Vector3(value, value, value);
                        currentEditor.UseUniformScale = true;
                    });
                menu.AddPropertyModifierFunction(property,
                    $"{SerializedPropertyContextMenu.Randomize}0.5-1.5 (Non-Uniform)",
                    (targetProperty) =>
                    {
                        targetProperty.vector3Value = new Vector3(Random.value, Random.value, Random.value);
                    });

                AddSnapVectorToGridItem(menu, property);

                SerializedPropertyContextMenu.Vector3MenuHandler.AddLogFunction(menu, property);
            }
            else// X, Y, Z.
            {
                AddFloatClipboardFunctions(menu, axis);

                menu.AddSeparator("");

                menu.AddPropertyModifierFunction(property,
                    $"{SerializedPropertyContextMenu.Set}Negate (*= -1)",
                    (targetProperty) => targetProperty.floatValue *= -1);
                AddSnapFloatToGridItem(menu, property);
                menu.AddPropertyModifierFunction(property,
                    $"{SerializedPropertyContextMenu.Randomize}0.5-1.5",
                    (targetProperty) => targetProperty.floatValue = Random.value);

                menu.AddSeparator("");

                SerializedPropertyContextMenu.AddLogValueFunction(menu, property, (targetProperty) => targetProperty.floatValue);
            }
        }

        /************************************************************************************************************************/

        public static void AddSnapVectorToGridItem(GenericMenu menu, SerializedProperty property)
        {
            menu.AddPropertyModifierFunction(property,
                $"{SerializedPropertyContextMenu.Set}Snap to Grid ({IGEditorUtils.ScaleSnap})",
                (targetProperty) => targetProperty.vector3Value = IGEditorUtils.SnapScale(targetProperty.vector3Value));
        }

        public static void AddSnapFloatToGridItem(GenericMenu menu, SerializedProperty property)
        {
            menu.AddPropertyModifierFunction(property,
                $"{SerializedPropertyContextMenu.Set}Snap to Grid ({IGEditorUtils.ScaleSnap})",
                (targetProperty) => targetProperty.floatValue = IGEditorUtils.Snap(targetProperty.floatValue, IGEditorUtils.ScaleSnap));
        }

        /************************************************************************************************************************/

        protected override string GetCurrentModePropertyPrefix()
        {
            return ParentEditor.CurrentIsLocalMode ? "localScale" : "lossyScale";
        }

        /************************************************************************************************************************/
        #region Custom Handles
        /************************************************************************************************************************/

        public override void DoToolGUI(Transform target, Vector3 handlePosition)
        {
            EditorGUI.BeginChangeCheck();
            var size = HandleUtility.GetHandleSize(handlePosition);
            var newScale = Handles.ScaleHandle(target.localScale, handlePosition, target.rotation, size);
            if (EditorGUI.EndChangeCheck() && GUI.enabled)
            {
                RecordTransformForUndo(target, UndoName);

                var scaleOffset = target.localScale;
                scaleOffset.x = newScale.x / scaleOffset.x;
                scaleOffset.y = newScale.y / scaleOffset.y;
                scaleOffset.z = newScale.z / scaleOffset.z;

                if (TransformEditor.DrawAllGizmos)
                {
                    ApplyScale(target, handlePosition, scaleOffset);
                }
                else
                {
                    for (int i = 0; i < Targets.Length; i++)
                    {
                        ApplyScale(Targets[i], handlePosition, scaleOffset);
                    }
                }
            }
        }

        /************************************************************************************************************************/

        private void ApplyScale(Transform target, Vector3 pivot, Vector3 scaleOffset)
        {
            if (ParentEditor.CurrentFreezeChildTransforms)
            {
                PositionDrawer.CacheChildPositions(target);
            }

            if (pivot == target.position)
            {
                target.localScale = Vector3.Scale(target.localScale, scaleOffset);
            }
            else
            {
                var positionOffset = target.position - pivot;
                target.localScale = Vector3.Scale(target.localScale, scaleOffset);
                target.position = pivot + Vector3.Scale(positionOffset, scaleOffset);
            }

            if (ParentEditor.CurrentFreezeChildTransforms)
            {
                PositionDrawer.RevertChildPositions(target);
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif
