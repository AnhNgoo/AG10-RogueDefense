// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InspectorGadgets.Editor
{
    /************************************************************************************************************************/
    #region Base Visualiser
    /************************************************************************************************************************/

    /// <summary>[Editor-Only]
    /// Base class for editor windows that help visualise the value of a <see cref="SerializedProperty"/>.
    /// </summary>
    public abstract class PropertyVisualiserWindow : EditorWindow
    {
        /************************************************************************************************************************/

        [SerializeField] private Serialization.PropertyReference _Target;
        [SerializeField] private bool _RelativeToSelection = true;

        /************************************************************************************************************************/

        /// <summary>A serializable reference to the <see cref="SerializedProperty"/> being visualised.</summary>
        public Serialization.PropertyReference Target => _Target;

        /************************************************************************************************************************/

        /// <summary>The name of the window to use as its title.</summary>
        protected abstract string WindowName { get; }

        /// <summary>Called at the start of <see cref="OnGUI"/>. If this method returns false, the window will be closed.</summary>
        protected virtual bool ValidateTarget() => true;

        /// <summary>Override this method to draw gizmos in the scene to visualise the target property.</summary>
        protected abstract void OnSceneGUI(SceneView sceneView);

        /************************************************************************************************************************/

        /// <summary>
        /// Called when this window is loaded.
        /// Sets the window title and registers the <see cref="DoSceneGUI"/> callback.
        /// </summary>
        protected virtual void OnEnable()
        {
            titleContent = new GUIContent(WindowName);
            autoRepaintOnSceneChange = true;
            SceneView.duringSceneGui += DoSceneGUI;
        }

        /// <summary>
        /// Called when this window is unloaded.
        /// Unregisters the <see cref="DoSceneGUI"/> callback.
        /// </summary>
        protected virtual void OnDisable()
        {
            SceneView.duringSceneGui -= DoSceneGUI;
        }

        private void DoSceneGUI(SceneView sceneView)
        {
            if (!_Target.IsValid())
                return;

            OnSceneGUI(sceneView);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Draws this window's GUI by calling <see cref="DoOptionsGUI"/> then drawing the target property.
        /// </summary>
        protected void OnGUI()
        {
            if (!_Target.IsValid() || !ValidateTarget())
            {
                Close();
                return;
            }

            EditorGUIUtility.wideMode = true;
            EditorGUI.BeginChangeCheck();

            // Visualiser Options.

            GUILayout.BeginVertical(GUI.skin.box);
            DoOptionsGUI();
            GUILayout.EndVertical();

            // Target Details.

            GUILayout.BeginVertical(GUI.skin.box);
            GUI.enabled = false;

            EditorGUILayout.ObjectField("Target", _Target.TargetObject, typeof(Object), true);

            EditorGUILayout.LabelField("Property Path", _Target.Property.propertyPath);

            GUI.enabled = (_Target.TargetObject.Object.hideFlags & HideFlags.NotEditable) != HideFlags.NotEditable;

            _Target.Property.serializedObject.Update();
            EditorGUILayout.PropertyField(_Target.Property, _Target.Property.isExpanded);
            _Target.Property.serializedObject.ApplyModifiedProperties();

            GUI.enabled = true;
            GUILayout.EndVertical();

            Repaint();

            if (EditorGUI.EndChangeCheck())
                SceneView.RepaintAll();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Draws a toggle to control whether the visualisations should be relative to the selected object or not.
        /// Override this method to add additional options to the window.
        /// </summary>
        protected virtual void DoOptionsGUI()
        {
            _RelativeToSelection = EditorGUILayout.Toggle("Relative to Selection", _RelativeToSelection);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// If the "Relative to Selection" toggle is enabled, this property returns the selected object's <see cref="Transform"/>.
        /// </summary>
        protected Transform SelectedTransform
        {
            get
            {
                if (_RelativeToSelection)
                    return Selection.activeTransform;
                else
                    return null;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Add a "Visualise" menu item which calls <see cref="Visualise{T}"/>.</summary>
        public static void AddVisualiseItem<T>(GenericMenu menu, SerializedProperty property) where T : PropertyVisualiserWindow
        {
            menu.AddItem(new GUIContent("Visualise"), false, () => Visualise<T>(property));
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Opens a <see cref="PropertyVisualiserWindow"/> of the specified type and assigns the specified `property`
        /// as its target.
        /// </summary>
        public static void Visualise<T>(SerializedProperty property) where T : PropertyVisualiserWindow
        {
            var window = CreateInstance<T>();
            window._Target = new Serialization.PropertyReference(property);
            window.Show();
        }

        /************************************************************************************************************************/
    }

    /************************************************************************************************************************/
    #endregion
    /************************************************************************************************************************/
    #region Vector3 Visualiser
    /************************************************************************************************************************/

    /// <summary>[Editor-Only]
    /// The "Visualise" command in the context menu of a <see cref="Vector3"/> property can be used to open this window
    /// which visualises the property's value in the scene.
    /// </summary>
    public class Vector3VisualiserWindow : PropertyVisualiserWindow
    {
        /************************************************************************************************************************/

        /// <summary>The name of the window to use as its title.</summary>
        protected override string WindowName => "Vector3 Visualiser";

        /************************************************************************************************************************/

        /// <summary>Returns true if the target property is a <see cref="SerializedPropertyType.Vector3"/>.</summary>
        protected override bool ValidateTarget()
            => Target.Property.propertyType == SerializedPropertyType.Vector3;

        /************************************************************************************************************************/

        /// <summary>Draws gizmos in the scene to visualise the target property.</summary>
        protected override void OnSceneGUI(SceneView sceneView)
        {
            Target.Property.serializedObject.Update();

            var value = Target.Property.vector3Value;
            var origin = default(Vector3);
            var rotation = Quaternion.identity;

            var selection = SelectedTransform;
            if (selection != null)
            {
                value = selection.TransformPoint(value);
                origin = selection.position;
                rotation = selection.rotation;
            }

            Handles.DrawLine(origin, value);

            EditorGUI.BeginChangeCheck();

            value = Handles.PositionHandle(value, rotation);

            if (EditorGUI.EndChangeCheck())
            {
                if (selection != null)
                {
                    value = selection.InverseTransformPoint(value);
                }

                Target.Property.vector3Value = value;
                Target.Property.serializedObject.ApplyModifiedProperties();
            }
        }

        /************************************************************************************************************************/
    }

    /************************************************************************************************************************/
    #endregion
    /************************************************************************************************************************/
    #region Vector2 Visualiser
    /************************************************************************************************************************/

    /// <summary>[Editor-Only]
    /// The "Visualise" command in the context menu of a <see cref="Vector2"/> property can be used to open this window
    /// which visualises the property's value in the scene.
    /// </summary>
    public class Vector2VisualiserWindow : PropertyVisualiserWindow
    {
        /************************************************************************************************************************/

        private static readonly string[] AxisOptions = { "XY", "XZ" };

        [SerializeField] private int _XAxis = 0;
        [SerializeField] private int _YAxis = 1;

        /************************************************************************************************************************/

        /// <summary>The name of the window to use as its title.</summary>
        protected override string WindowName => "Vector2 Visualiser";

        /************************************************************************************************************************/

        /// <summary>Returns true if the target property is a <see cref="SerializedPropertyType.Vector2"/>.</summary>
        protected override bool ValidateTarget()
            => Target.Property.propertyType == SerializedPropertyType.Vector2;

        /************************************************************************************************************************/

        /// <summary>Draws the extra options of this <see cref="PropertyVisualiserWindow"/>.</summary>
        protected override void DoOptionsGUI()
        {
            base.DoOptionsGUI();

            GUILayout.BeginHorizontal();

            EditorGUILayout.PrefixLabel("Axes");

            var axisMode = (_YAxis == 1) ? 0 : 1;
            var newAxisMode = GUILayout.Toolbar(axisMode, AxisOptions);
            if (axisMode != newAxisMode)
            {
                switch (newAxisMode)
                {
                    case 0:
                        _XAxis = 0;
                        _YAxis = 1;
                        break;
                    case 1:
                        _XAxis = 0;
                        _YAxis = 2;
                        break;
                    default:
                        throw new Exception("Unexpected Case");
                }
            }

            GUILayout.EndHorizontal();
        }

        /************************************************************************************************************************/

        /// <summary>Draws gizmos in the scene to visualise the target property.</summary>
        protected override void OnSceneGUI(SceneView sceneView)
        {
            Target.Property.serializedObject.Update();

            var value = Target.Property.vector2Value;

            var position = new Vector3();
            position[_XAxis] = value.x;
            position[_YAxis] = value.y;

            var origin = default(Vector3);
            var rotation = Quaternion.identity;

            var selection = SelectedTransform;
            if (selection != null)
            {
                position = selection.TransformPoint(position);
                origin = selection.position;
                rotation = selection.rotation;
            }

            Handles.DrawLine(origin, position);

            EditorGUI.BeginChangeCheck();

            position = Handles.PositionHandle(position, rotation);

            if (EditorGUI.EndChangeCheck())
            {
                if (selection != null)
                {
                    position = selection.InverseTransformPoint(position);
                }

                value.x = position[_XAxis];
                value.y = position[_YAxis];

                Target.Property.vector2Value = value;
                Target.Property.serializedObject.ApplyModifiedProperties();
            }
        }

        /************************************************************************************************************************/
    }

    /************************************************************************************************************************/
    #endregion
    /************************************************************************************************************************/
    #region Float Visualiser
    /************************************************************************************************************************/

    /// <summary>[Editor-Only]
    /// The "Visualise" command in the context menu of a <see cref="float"/> property can be used to open this window
    /// which visualises the property's value in the scene.
    /// </summary>
    public class FloatVisualiserWindow : PropertyVisualiserWindow
    {
        /************************************************************************************************************************/

        /// <summary>The name of the window to use as its title.</summary>
        protected override string WindowName => "Float Visualiser";

        /************************************************************************************************************************/

        /// <summary>Returns true if the target property is a <see cref="SerializedPropertyType.Float"/>.</summary>
        protected override bool ValidateTarget()
            => Target.Property.propertyType == SerializedPropertyType.Float;

        /************************************************************************************************************************/

        /// <summary>Draws gizmos in the scene to visualise the target property.</summary>
        protected override void OnSceneGUI(SceneView sceneView)
        {
            Target.Property.serializedObject.Update();

            var value = Target.Property.floatValue;

            var position = default(Vector3);
            var rotation = Quaternion.identity;

            var selection = SelectedTransform;
            if (selection != null)
            {
                position = selection.position;
                rotation = selection.rotation;
            }

            EditorGUI.BeginChangeCheck();

            value = Handles.RadiusHandle(rotation, position, value);

            if (EditorGUI.EndChangeCheck())
            {
                Target.Property.floatValue = value;
                Target.Property.serializedObject.ApplyModifiedProperties();
            }
        }

        /************************************************************************************************************************/
    }

    /************************************************************************************************************************/
    #endregion
    /************************************************************************************************************************/
}

#endif
