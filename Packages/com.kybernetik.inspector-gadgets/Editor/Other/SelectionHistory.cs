// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InspectorGadgets.Editor
{
    /// <summary>[Editor-Only] [Pro-Only] An <see cref="EditorWindow"/> which keeps track of your selection history.</summary>
    internal sealed class SelectionHistory : EditorWindow
    {
        /************************************************************************************************************************/

        private static class Styles
        {
            public static readonly GUIStyle Button = new GUIStyle(EditorStyles.miniButton)
            {
                margin = new RectOffset(1, 0, 1, 1),
                padding = new RectOffset(0, 0, 0, 0),
            };

            public static readonly GUIStyle Popup = new GUIStyle(EditorStyles.popup)
            {
                fixedHeight = 0,
            };
        }

        /************************************************************************************************************************/

        private const string
            Controls =
                "• Left Click = Select and revert Camera" +
                "\n• Right Click = Select and keep Camera",
            PrefKey = nameof(InspectorGadgets) + "." + nameof(SelectionHistory) + ".";

        public static readonly AutoPrefs.EditorInt Capacity = new AutoPrefs.EditorInt(PrefKey + nameof(Capacity), 25);
        public static readonly AutoPrefs.EditorBool EnableDropdown = new AutoPrefs.EditorBool(PrefKey + nameof(Capacity), true);

        [SerializeField] private List<SelectionCommand> _History;
        [SerializeField] private Vector2 _Scroll;

        /************************************************************************************************************************/

        public static SelectionHistory Instance { get; private set; }

        private void OnEnable()
        {
            Instance = this;
            minSize = new Vector2(50, EditorGUIUtility.singleLineHeight);
            titleContent = new GUIContent("Selection History");

            if (_History == null)
                _History = new List<SelectionCommand>(Capacity);
            else
                RemoveMissingReferences();

            Selection.selectionChanged += OnSelectionChanged;
        }

        private void OnDisable()
        {
            Instance = null;
            if (_History.Count > 0)
                _History[_History.Count - 1].OnSelectionChanged();

            Selection.selectionChanged -= OnSelectionChanged;
        }

        /************************************************************************************************************************/

        private void RemoveMissingReferences()
        {
            for (int i = _History.Count - 1; i >= 0; i--)
            {
                if (_History[i].SelectedObject == null)
                    _History.RemoveAt(i);
            }
        }

        /************************************************************************************************************************/

        private void OnGUI()
        {
            RemoveMissingReferences();

            if (_History.Count == 0)
            {
                DoIntroductionGUI();
                return;
            }

            if (EnableDropdown && position.height <= minSize.y * 1.5f)
            {
                DoCompactGUI();
                return;
            }

            DoExpandedGUI();
        }

        /************************************************************************************************************************/

        private void DoIntroductionGUI()
        {
            EditorGUI.BeginChangeCheck();
            var content = IGEditorUtils.TempContent("Capacity",
                "The maximum number of selections that will be remembered");
            var capacity = EditorGUILayout.DelayedIntField(content, Capacity);
            if (EditorGUI.EndChangeCheck())
                Capacity.Value = Mathf.Clamp(capacity, 1, 1024);

            EditorGUI.BeginChangeCheck();
            content = IGEditorUtils.TempContent("Enable Dropdown",
                "Should a dropdown menu be used instead of buttons while this window is too short for multiple lines?");
            var enableDropdown = EditorGUILayout.Toggle(content, EnableDropdown);
            if (EditorGUI.EndChangeCheck())
                EnableDropdown.Value = enableDropdown;

            EditorGUILayout.HelpBox(
                "After you select something, buttons will appear here:\n" + Controls,
                MessageType.Info, true);
        }

        /************************************************************************************************************************/

        private void DoCompactGUI()
        {
            var area = position;
            area.x = 0;
            area.y = 0;
            area.height = EditorGUIUtility.singleLineHeight;

            var current = Selection.activeObject;
            var content = current != null
                ? EditorGUIUtility.ObjectContent(current, current.GetType())
                : IGEditorUtils.TempContent("Nothing Selected");

            if (GUI.Button(area, content, Styles.Popup))
            {
                var setCamera = Event.current.button == 0;
                var menu = new GenericMenu();

                menu.AddItem(new GUIContent("Clear"), false, _History.Clear);
                menu.AddSeparator("");

                for (int i = _History.Count - 1; i >= 0; i--)
                {
                    var command = _History[i];
                    var obj = command.SelectedObject.Object;
                    if (obj != null)
                        menu.AddItem(new GUIContent(obj.name), obj == current, () => command.SelectTarget(setCamera));
                }

                menu.ShowAsContext();
            }
        }

        /************************************************************************************************************************/

        private void DoExpandedGUI()
        {
            _Scroll = GUILayout.BeginScrollView(_Scroll);

            Rect area = default;

            var i = _History.Count - 1;
            var isCurrent = Selection.activeObject == _History[i].SelectedObject;

            for (; i >= 0; i--)
            {
                _History[i].DoGUI(ref area, isCurrent, Styles.Button);
                isCurrent = false;
            }

            GUILayout.EndScrollView();
        }

        /************************************************************************************************************************/

        private void OnSelectionChanged()
        {
            EditorApplication.delayCall += Repaint;

            var selectedObject = Selection.activeObject;
            if (selectedObject == null)
                return;

            RemoveMissingReferences();
            if (_History.Count > 0)
            {
                var lastCommand = _History[_History.Count - 1];
                lastCommand.OnSelectionChanged();
                _History[_History.Count - 1] = lastCommand;
            }

            // Don't add it immediately in case we're in the middle of drawing this window.
            EditorApplication.delayCall += () =>
            {
                for (int i = _History.Count - 1; i >= 0; i--)
                {
                    if (_History[i].SelectedObject == selectedObject)
                    {
                        _History.RemoveAt(i);
                        break;
                    }
                }

                if (_History.Count >= Capacity)
                    _History.RemoveRange(0, _History.Count - Capacity + 1);

                _History.Add(new SelectionCommand(selectedObject));
            };
        }

        /************************************************************************************************************************/
        #region Hotkeys
        /************************************************************************************************************************/

        [MenuItem("Window/General/Selection History")]
        private static void OpenWindow() => GetWindow<SelectionHistory>();

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Commands
        /************************************************************************************************************************/

        [Serializable]
        private struct SelectionCommand
        {
            /************************************************************************************************************************/

            [SerializeField]
            private Serialization.ObjectReference _SelectedObject;
            public Serialization.ObjectReference SelectedObject => _SelectedObject;

            [SerializeField] private Vector3 _CameraPivot;
            [SerializeField] private Quaternion _CameraRotation;
            [SerializeField] private float _CameraSize;
            [SerializeField] private bool _HasCameraDetails, _CameraIn2dMode, _CameraOrthographic;

            /************************************************************************************************************************/

            [NonSerialized]
            private float _Width;

            public float Width
            {
                get
                {
                    if (_Width <= 0)
                        CalculateWidth(Styles.Button);
                    return _Width;
                }
            }

            /************************************************************************************************************************/

            private void CalculateWidth(GUIStyle style)
            {
                if (_SelectedObject == null)
                    return;

                var selectedObject = _SelectedObject.Object;
                if (selectedObject == null)
                    return;

                _Width = style.CalcSize(new GUIContent(selectedObject.name)).x + style.border.right;
                _Width += EditorGUIUtility.singleLineHeight;
                Instance.Repaint();
            }

            /************************************************************************************************************************/

            public SelectionCommand(Object selectedObject)
            {
                _SelectedObject = selectedObject;
                _HasCameraDetails = false;
                _CameraPivot = default;
                _CameraRotation = default;
                _CameraSize = default;
                _CameraIn2dMode = default;
                _CameraOrthographic = default;
                _Width = default;
                OnSelectionChanged();
            }

            /************************************************************************************************************************/

            public void OnSelectionChanged()
            {
                var scene = SceneView.lastActiveSceneView;
                if (scene == null)
                    return;

                _HasCameraDetails = true;
                _CameraPivot = scene.pivot;
                _CameraRotation = scene.rotation;
                _CameraSize = scene.size;
                _CameraIn2dMode = scene.in2DMode;
                _CameraOrthographic = scene.orthographic;
            }

            /************************************************************************************************************************/

            public void SelectTarget(bool setCamera)
            {
                Selection.activeObject = _SelectedObject;

                if (!setCamera ||
                    !_HasCameraDetails)
                    return;

                var scene = SceneView.lastActiveSceneView;
                if (scene == null)
                    return;

                var copy = this;
                EditorApplication.delayCall += () =>
                {
                    scene.pivot = copy._CameraPivot;
                    scene.rotation = copy._CameraRotation;
                    scene.size = copy._CameraSize;
                    scene.in2DMode = copy._CameraIn2dMode;
                    scene.orthographic = copy._CameraOrthographic;
                };
            }

            /************************************************************************************************************************/

            public void DoGUI(ref Rect area, bool isSelected, GUIStyle style)
            {
                var width = Width;
                if (area.width < width)
                    area = GUILayoutUtility.GetRect(width, EditorGUIUtility.singleLineHeight, style);

                var toggleArea = IGEditorUtils.StealFromLeft(ref area, width, style.margin);

                var selectedObject = _SelectedObject?.Object;
                var content = EditorGUIUtility.ObjectContent(selectedObject, selectedObject?.GetType());
                content.tooltip = Controls;

                if (GUI.Toggle(toggleArea, isSelected, content, style) && !isSelected)
                {
                    var setCamera = Event.current.button == 0;
                    var copy = this;
                    EditorApplication.delayCall += () => copy.SelectTarget(setCamera);
                }
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif
