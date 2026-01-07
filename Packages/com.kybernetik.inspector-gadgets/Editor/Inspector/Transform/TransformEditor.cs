// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using InspectorGadgets.Editor.PropertyDrawers;
using System;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace InspectorGadgets.Editor
{
    /// <summary>[Editor-Only] A custom Inspector for <see cref="Transform"/> components.</summary>
#if !DISABLE_IG_TRANSFORM_INSPECTOR
    [CustomEditor(typeof(Transform))]
    [CanEditMultipleObjects]
#endif
    public class TransformEditor : UnityEditor.Editor
    {
        /************************************************************************************************************************/
        #region Fields
        /************************************************************************************************************************/

        internal PositionDrawer Position { get; private set; }
        internal RotationDrawer Rotation { get; private set; }
        internal ScaleDrawer Scale { get; private set; }

        internal Transform[] Targets { get; private set; }
        internal bool CurrentIsLocalMode { get; private set; }
        internal bool CurrentFreezeChildTransforms { get; private set; }
        internal bool UseUniformScale { get; set; }

        /************************************************************************************************************************/

        internal static readonly AutoPrefs.EditorBool
            DefaultToUniformScale = new(
                EditorStrings.PrefsKeyPrefix + nameof(DefaultToUniformScale), true),
            IsLocalMode = new(
                EditorStrings.PrefsKeyPrefix + nameof(IsLocalMode), true),
            OverrideTransformGizmos = new(
                EditorStrings.PrefsKeyPrefix + nameof(OverrideTransformGizmos), true),
            FreezeChildTransforms = new(
                EditorStrings.PrefsKeyPrefix + nameof(FreezeChildTransforms), false),
            DrawAllGizmos = new(
                EditorStrings.PrefsKeyPrefix + nameof(DrawAllGizmos), false);

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Inspector
        /************************************************************************************************************************/

        private void OnEnable()
        {
            var targetCount = targets.Length;
            if (Targets == null || Targets.Length != targetCount)
                Targets = new Transform[targetCount];
            for (int i = 0; i < targetCount; i++)
                Targets[i] = targets[i] as Transform;

            if (Position == null)
            {
                Position = new PositionDrawer(this);
                Rotation = new RotationDrawer(this);
                Scale = new ScaleDrawer(this);
            }

            Position.OnEnable(serializedObject);
            Rotation.OnEnable(serializedObject);
            Scale.OnEnable(serializedObject);

            CurrentIsLocalMode = IsLocalMode;
            CurrentFreezeChildTransforms = FreezeChildTransforms;
            UseUniformScale = DefaultToUniformScale;
        }

        private void OnDisable()
        {
            Tools.hidden = false;

            IsLocalMode.Value = CurrentIsLocalMode;
            FreezeChildTransforms.Value = CurrentFreezeChildTransforms;

            Position.OnDisable();
            Rotation.OnDisable();
            Scale.OnDisable();
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            if (Event.current.type == EventType.Layout)
            {
                Rotation.CheckIfArbitrarilyRotated();
                if (!CurrentIsLocalMode && Rotation.IsArbitrarilyRotated)
                    UseUniformScale = false;
            }

            var height = Position.Height + Rotation.Height + Scale.Height + IGEditorUtils.Spacing;
            var area = EditorGUILayout.GetControlRect(true, height);

            DoModeButtons(ref area);

            area.height = Position.Height;
            Position.DoInspectorGUI(area);

            area.y += area.height + IGEditorUtils.Spacing;
            area.height = Rotation.Height;
            Rotation.DoInspectorGUI(area);

            area.y += area.height + IGEditorUtils.Spacing;
            area.height = Scale.Height;
            Scale.DoInspectorGUI(area);

            DoWarnings();
        }

        /************************************************************************************************************************/
        #region Mode Buttons
        /************************************************************************************************************************/

        private void DoModeButtons(ref Rect area)
        {
            var style = InternalGUI.MiniSquareButtonStyle;

            var xMin = area.xMin;
            xMin -= IGEditorUtils.IndentSize - 1;
            if (xMin < IGEditorUtils.Spacing)
                xMin = IGEditorUtils.Spacing;
            area.xMin = xMin;

            var buttonArea = area;
            buttonArea.width = style.fixedWidth;

            area.xMin += buttonArea.width;

            var guiEnabled = GUI.enabled;

            buttonArea.height = style.fixedHeight;

            // Local / World.
            var label = CurrentIsLocalMode ? EditorStrings.GUI.LocalMode : EditorStrings.GUI.WorldMode;
            if (GUI.Button(buttonArea, label, style))
            {
                CurrentIsLocalMode = !CurrentIsLocalMode;
                Rotation.CacheEulerAngles();
                GUIUtility.hotControl = 0;
                GUIUtility.keyboardControl = 0;
            }

            var height = style.fixedHeight + IGEditorUtils.Spacing;
            buttonArea.y += height;

            if (!OverrideTransformGizmos)
                GUI.enabled = false;

            // Freeze Child Transforms.
            EditorGUI.BeginChangeCheck();
            CurrentFreezeChildTransforms = GUI.Toggle(buttonArea, CurrentFreezeChildTransforms,
                EditorStrings.GUI.FreezeChildTransforms, style);
            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }

            // Draw All Gizmos.

            buttonArea.y += height;

            if (GUI.Toggle(buttonArea, DrawAllGizmos, EditorStrings.GUI.DrawAllGizmos, style) != DrawAllGizmos)
            {
                DrawAllGizmos.Invert();
                SceneView.RepaintAll();
            }

            GUI.enabled = guiEnabled;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/

        private void DoWarnings()
        {
            if (!CurrentIsLocalMode && Rotation.IsArbitrarilyRotated)
            {
                GUILayout.Space(IGEditorUtils.Spacing);
                EditorGUILayout.HelpBox(EditorStrings.GUI.ScaleSkewWarning, MessageType.Warning);
                GUILayout.Space(-IGEditorUtils.Spacing);
            }

            const float PositionWarningThreshold = 100000f;

            var position = Targets[0].position;
            if (Math.Abs(position.x) > PositionWarningThreshold ||
                Math.Abs(position.y) > PositionWarningThreshold ||
                Math.Abs(position.z) > PositionWarningThreshold)
            {
                GUILayout.Space(IGEditorUtils.Spacing);
                EditorGUILayout.HelpBox(EditorStrings.GUI.PrecisionWarning, MessageType.Warning);
                GUILayout.Space(-IGEditorUtils.Spacing);
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Scene GUI
        /************************************************************************************************************************/

        /// <summary>Draws custom Scene View controls.</summary>
        protected virtual void OnSceneGUI()
        {
            if (!OverrideTransformGizmos)
                return;

            if (!CustomTransformGizmo.ShouldShow())
            {
                Tools.hidden = false;
                return;
            }

            TransformPropertyDrawer currentTool;
            switch (Tools.current)
            {
                case Tool.Move: currentTool = Position; break;
                case Tool.Rotate: currentTool = Rotation; break;
                case Tool.Scale: currentTool = Scale; break;
                default:
                    Tools.hidden = false;
                    return;
            }

            // Ignore controls while holding alt so it can control the camera without clicking on handles.
            if (Event.current.alt && Event.current.type != EventType.Repaint)
                return;

            Tools.hidden = true;

            if (DrawAllGizmos || target == Selection.activeTransform)
            {
                DoHandlesGUI(target as Transform, currentTool);
            }
        }

        /************************************************************************************************************************/

        private static readonly Color
            FrozenChildLineColor = new Color(1, 0, 0, 0.25f),
            FrozenChildDotColor = new Color(1, 0, 0, 0.5f);

        private void DoHandlesGUI(Transform target, TransformPropertyDrawer currentTool)
        {
            var handlePosition = GetHandlePosition(target);

            EditorGUI.BeginDisabledGroup(ShouldDisableSceneTools());
            {
                currentTool.DoToolGUI(target, handlePosition);

                if (CurrentFreezeChildTransforms && Event.current.type == EventType.Repaint)
                {
                    var color = Handles.color;

                    var parentPosition = target.position;

                    for (int i = 0; i < target.childCount; i++)
                    {
                        var child = target.GetChild(i);
                        var childPosition = child.position;

                        Handles.color = FrozenChildLineColor;
                        Handles.DrawLine(parentPosition, childPosition);
                        Handles.color = FrozenChildDotColor;

                        Handles.SphereHandleCap(0, childPosition, child.rotation,
                            HandleUtility.GetHandleSize(childPosition) * 0.1f, EventType.Repaint);
                    }

                    Handles.color = color;
                }

                if (EditorApplication.isPlaying && target.gameObject.isStatic)
                    ShowStaticLabel(handlePosition);
            }
            EditorGUI.EndDisabledGroup();
        }

        /************************************************************************************************************************/

        private static readonly FieldInfo
            HandleOffset = typeof(Tools).GetField("handleOffset", IGEditorUtils.StaticBindings),
            LocalHandleOffset = typeof(Tools).GetField("localHandleOffset", IGEditorUtils.StaticBindings);

        internal static Vector3 GetHandlePosition(Transform target)
        {

            if (target == null)
                return new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);

            Vector3 offset;
            if (HandleOffset != null)
                offset = (Vector3)HandleOffset.GetValue(null);
            else
                offset = default;

            if (LocalHandleOffset != null)
                offset += Tools.handleRotation * (Vector3)LocalHandleOffset.GetValue(null);

            switch (Tools.pivotMode)
            {
                case PivotMode.Center:
                    if (DrawAllGizmos)
                        return CalculateBounds(target).center + offset;
                    return InternalEditorUtility.CalculateSelectionBounds(true, false).center + offset;

                case PivotMode.Pivot:
                default:
                    return target.position + offset;
            }
        }

        /************************************************************************************************************************/

        private static Bounds CalculateBounds(Transform target)
        {
            var bounds = new Bounds();
            var hasOrigin = false;

#if UNITY_PHYSICS_3D
            var colliders = target.GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++)
                EncapsulateBounds(ref bounds, colliders[i].bounds, ref hasOrigin);
#endif

#if UNITY_PHYSICS_2D
            var colliders2D = target.GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < colliders2D.Length; i++)
                EncapsulateBounds(ref bounds, colliders2D[i].bounds, ref hasOrigin);
#endif

            var renderers = target.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
                EncapsulateBounds(ref bounds, renderers[i].bounds, ref hasOrigin);

            if (!hasOrigin)
                bounds.center = target.position;

            return bounds;
        }

        private static void EncapsulateBounds(ref Bounds bounds, Bounds encapsulate, ref bool hasOrigin)
        {
            if (!hasOrigin)
            {
                hasOrigin = true;
                bounds = encapsulate;
            }
            else
            {
                bounds.Encapsulate(encapsulate);
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns true in Play Mode if any of the selected objects are marked as static.
        /// </summary>
        private bool ShouldDisableSceneTools()
        {
            if (!EditorApplication.isPlaying)
                return false;

            for (int i = 0; i < Targets.Length; i++)
            {
                if (Targets[i].gameObject.isStatic)
                    return true;
            }

            return false;
        }

        /************************************************************************************************************************/

        private static GUIContent _StaticLabel;
        private static GUIStyle _StaticLabelStyle;

        // UnityEditor.Handles.ShowStaticLabel.
        private static void ShowStaticLabel(Vector3 position)
        {
            if (_StaticLabel == null)
            {
                _StaticLabel = new GUIContent("Static");
                _StaticLabelStyle = "SC ViewAxisLabel";
                _StaticLabelStyle.alignment = TextAnchor.MiddleLeft;
                _StaticLabelStyle.fixedWidth = 0f;
            }

            Handles.color = Color.white;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            Handles.BeginGUI();
            var rect = HandleUtility.WorldPointToSizedRect(position, _StaticLabel, _StaticLabelStyle);
            rect.x += 10f;
            rect.y += 10f;
            GUI.Label(rect, _StaticLabel, _StaticLabelStyle);
            Handles.EndGUI();
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif
