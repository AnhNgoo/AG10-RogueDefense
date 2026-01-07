// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using InspectorGadgets.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InspectorGadgets.Editor
{
    /// <summary>[Editor-Only] Various utility methods used by <see cref="InspectorGadgets"/>.</summary>
    public static partial class IGEditorUtils
    {
        /************************************************************************************************************************/

        /// <summary>Commonly used <see cref="BindingFlags"/>.</summary>
        public const BindingFlags
            AnyAccessBindings = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static,
            InstanceBindings = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            StaticBindings = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        /************************************************************************************************************************/
        #region Menu Functions
        /************************************************************************************************************************/

        /// <summary>Replaces any forward slashes with backslashes: <c>/</c> -> <c>\</c>.</summary>
        public static string AllBackslashes(this string str)
            => str.Replace('/', '\\');

        /************************************************************************************************************************/

        [MenuItem(EditorStrings.OpenDocumentation)]
        internal static void OpenDocumentation()
            => OpenDocumentation(null);

        internal static void OpenDocumentation(string suffix)
            => EditorUtility.OpenWithDefaultApp(Strings.DocumentationURL + suffix);

        /************************************************************************************************************************/

        [MenuItem(EditorStrings.CollapseAllComponents)]
        private static void CollapseAllComponents(MenuCommand command)
        {
            var components = ((Transform)command.context).GetComponents<Component>();

            var expand = true;

            for (int i = 0; i < components.Length; i++)
            {
                if (UnityEditorInternal.InternalEditorUtility.GetIsInspectorExpanded(components[i]))
                {
                    expand = false;
                    break;
                }
            }

            for (int i = 0; i < components.Length; i++)
            {
                UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(components[i], expand);
            }

            var selection = Selection.objects;

            Selection.activeObject = null;

            EditorApplication.delayCall += () =>
                EditorApplication.delayCall += () =>
                    Selection.objects = selection;
        }

        /************************************************************************************************************************/

        [MenuItem(EditorStrings.RemoveAllComponents)]
        private static void RemoveAllComponents(MenuCommand menuCommand)
        {
            var components = ((Component)menuCommand.context).GetComponents<Component>();
            for (int i = 1; i < components.Length; i++)// Skip the Transform.
            {
                Undo.DestroyObjectImmediate(components[i]);
            }
        }

        /************************************************************************************************************************/

        [MenuItem(EditorStrings.PingObject)]
        private static void PingObject(MenuCommand menuCommand)
            => EditorGUIUtility.PingObject(menuCommand.context);

        /************************************************************************************************************************/
        // Open Locked Editor Windows.

        [MenuItem(EditorStrings.MainNewLockedInspector)]
        internal static void NewLockedInspector()
        {
            var window = CreateEditorWindow("UnityEditor.InspectorWindow", out var type);

            var isLocked = type.GetProperty("isLocked", InstanceBindings);
            isLocked.GetSetMethod().Invoke(window, new object[] { true });
        }

        /************************************************************************************************************************/

        internal static void NewLockedInspector(Object target)
        {
            var selection = Selection.objects;
            Selection.activeObject = target;
            NewLockedInspector();
            Selection.objects = selection;
        }

        /************************************************************************************************************************/

        [MenuItem(EditorStrings.ObjectNewLockedInspector)]
        private static void NewLockedInspector(MenuCommand command) => NewLockedInspector(command.context);

        /************************************************************************************************************************/

        // The window throws exceptions because it fails to initialize properly.
        //[MenuItem("Assets/New Locked Project Browser")]
        //internal static void NewLockedProjectBrowser()
        //{
        //    var window = CreateEditorWindow("UnityEditor.ProjectBrowser", out type);

        //    var isLocked = type.GetField("m_IsLocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        //    isLocked.SetValue(window, true);
        //}

        /************************************************************************************************************************/

        private static EditorWindow CreateEditorWindow(string typeName, out Type type)
        {
            type = typeof(EditorWindow).Assembly.GetType(typeName);
            if (type == null)
            {
                throw new Exception($"Unable to find {typeName} class in {typeof(EditorWindow).Assembly.Location}");
            }

            var window = ScriptableObject.CreateInstance(type) as EditorWindow;
            window.Show();
            return window;
        }

        /************************************************************************************************************************/

        [MenuItem(EditorStrings.GameObjectResetSelectedTransforms)]
        private static void ResetSelectedTransforms()
        {
            var gameObjects = Selection.gameObjects;
            if (gameObjects.Length == 0)
                return;

            Undo.RecordObjects(gameObjects, "Reset Transforms");
            for (int i = 0; i < gameObjects.Length; i++)
            {
                var transform = gameObjects[i].transform;
                transform.SetLocalPositionAndRotation(default, Quaternion.identity);
                transform.localScale = Vector3.one;
            }
        }

        /************************************************************************************************************************/

        [MenuItem(EditorStrings.PingScriptAsset)]
        private static void PingScriptAsset(MenuCommand menuCommand)
            => PingScriptAsset(menuCommand.context);

        internal static void PingScriptAsset(Object obj)
        {
            MonoScript script;

            var behaviour = obj as MonoBehaviour;
            if (behaviour != null)
            {
                script = MonoScript.FromMonoBehaviour(behaviour);
            }
            else
            {
                var scriptable = obj as ScriptableObject;
                if (scriptable != null)
                    script = MonoScript.FromScriptableObject(scriptable);
                else
                    return;
            }

            if (script != null)
                EditorGUIUtility.PingObject(script);
        }

        /************************************************************************************************************************/

        [MenuItem(EditorStrings.ShowOrHideScriptProperty)]
        private static void HideScriptProperty()
        {
            ComponentEditor.HideScriptProperty.Invert();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        /************************************************************************************************************************/

        [MenuItem(EditorStrings.CopyTransformPath)]
        private static void CopyTransformPath(MenuCommand menuCommand)
        {
            EditorGUIUtility.systemCopyBuffer = IGUtils.GetTransformPath(menuCommand.context as Transform);
        }

        /************************************************************************************************************************/

        private static List<Object> _GroupedContext;

        /// <summary>
        /// When a context menu function is executed with multiple objects selected, it calls the method once for each
        /// object. Passing each `command` into this method will group them all into a list and invoke the specified
        /// `method` once they have all been gathered.
        /// </summary>
        public static void GroupedInvoke(MenuCommand command, Action<List<Object>> method)
        {
            if (_GroupedContext == null)
                _GroupedContext = new List<Object>();

            if (_GroupedContext.Count == 0)
            {
                EditorApplication.delayCall += () =>
                {
                    method(_GroupedContext);
                    _GroupedContext.Clear();
                };
            }

            _GroupedContext.Add(command.context);
        }

        /************************************************************************************************************************/

        #region Create Editor Script

        [MenuItem(EditorStrings.CreateEditorScript)]
        private static void CreateEditorScript(MenuCommand command)
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                CreateEditorScript(command.context);
        }

        internal static void CreateEditorScript(Object target)
        {
            var path = AskForEditorScriptSavePath(target);
            if (path == null)
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            var editorName = Path.GetFileNameWithoutExtension(path);

            File.WriteAllText(path, BuildEditorScript(target, editorName));

            Debug.Log($"{editorName} script created at {path}");

            AssetDatabase.ImportAsset(path);
            AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<MonoScript>(path));
        }

        /************************************************************************************************************************/

        private static string FindComponentDirectory(Object component)
        {
            var behaviour = component as MonoBehaviour;
            if (behaviour != null)
            {
                var script = MonoScript.FromMonoBehaviour(behaviour);
                if (script != null)
                {
                    return Path.GetDirectoryName(AssetDatabase.GetAssetPath(script));
                }
            }

            return "Assets";
        }

        /************************************************************************************************************************/

        private static string AskForEditorScriptSavePath(Object target)
        {
            var path = FindComponentDirectory(target);
            var name = target.GetType().Name;
            var fileName = name + "Editor.cs";

            var dialogResult = EditorUtility.DisplayDialogComplex(
                "Create Editor Script",
                $"Create Editor Script for '{name}' at '{path}/{fileName}'",
                "Create", "Browse", "Cancel");

            switch (dialogResult)
            {
                case 0:// Create.
                    return path + "/" + fileName;

                case 1:// Browse.
                    return EditorUtility.SaveFilePanelInProject("Create Editor Script", fileName, "cs",
                        $"Where do you want to save the Editor Script for {name}?", path);

                default:// Cancel.
                    return null;
            }
        }

        /************************************************************************************************************************/

        private static string BuildEditorScript(Object target, string editorName)
        {
            const string Indent = "    ";

            var type = target.GetType();

            var text = new StringBuilder();
            text.AppendLine("#if UNITY_EDITOR");
            text.AppendLine();
            text.AppendLine("using UnityEditor;");
            text.AppendLine("using UnityEngine;");
            text.AppendLine();

            var indent = false;
            if (type.Namespace != null)
            {
                text.Append("namespace ").AppendLine(type.Namespace);
                text.AppendLine("{");
                indent = true;

                text.Append(Indent);
            }

            text.Append("[CustomEditor(typeof(");
            text.Append(type.Name);
            text.AppendLine("), true)]");

            if (indent) text.Append(Indent);
            text.Append("sealed class ");
            text.Append(editorName);
            text.Append(" : InspectorGadgets.Editor<");
            text.Append(type.Name);
            text.AppendLine(">");

            if (indent) text.Append(Indent);
            text.AppendLine("{");

            if (indent) text.Append(Indent);
            text.AppendLine("}");

            if (indent) text.AppendLine("}");

            text.AppendLine();
            text.Append("#endif");

            return text.ToString();
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Snapping
        /************************************************************************************************************************/

        /// <summary>Wraps <see cref="EditorSnapSettings.move"/>.</summary>
        public static Vector3 MoveSnapVector
        {
            get => EditorSnapSettings.move;
            set => EditorSnapSettings.move = value;
        }

        /************************************************************************************************************************/

        /// <summary>Wraps <see cref="EditorSnapSettings.rotate"/>.</summary>
        public static float RotationSnap
        {
            get => EditorSnapSettings.rotate;
            set => EditorSnapSettings.rotate = value;
        }

        /// <summary>(<see cref="RotationSnap"/>, <see cref="RotationSnap"/>, <see cref="RotationSnap"/>).</summary>
        public static Vector3 RotationSnapVector
        {
            get
            {
                var snap = RotationSnap;
                return new Vector3(snap, snap, snap);
            }
        }

        /************************************************************************************************************************/

        /// <summary>Wraps <see cref="EditorSnapSettings.scale"/>.</summary>
        public static float ScaleSnap
        {
            get => EditorSnapSettings.scale;
            set => EditorSnapSettings.scale = value;
        }

        /// <summary>(<see cref="ScaleSnap"/>, <see cref="ScaleSnap"/>, <see cref="ScaleSnap"/>).</summary>
        public static Vector3 ScaleSnapVector
        {
            get
            {
                var snap = ScaleSnap;
                return new Vector3(snap, snap, snap);
            }
        }

        /************************************************************************************************************************/

        /// <summary>Snaps the given `value` to a grid with the specified `snap` size.</summary>
        public static float Snap(float value, float snap)
            => Mathf.Round(value / snap) * snap;

        /************************************************************************************************************************/

        /// <summary>Snaps the given `position` to the grid (as specified in Edit/Snap Settings).</summary>
        public static Vector3 SnapPosition(Vector3 position)
        {
            var snap = MoveSnapVector;
            position.x = Snap(position.x, snap.x);
            position.y = Snap(position.y, snap.y);
            position.z = Snap(position.z, snap.z);
            return position;
        }

        /// <summary>Snaps the given `position` to the grid on the specified axis (as specified in Edit/Snap Settings).</summary>
        public static Vector3 SnapPosition(Vector3 position, int axisIndex)
        {
            position[axisIndex] = Snap(position[axisIndex], MoveSnapVector[axisIndex]);
            return position;
        }

        /************************************************************************************************************************/

        /// <summary>Snaps the given `rotationEuler` to the nearest snap increment on all axes (as specified in Edit/Snap Settings).</summary>
        public static Vector3 SnapRotation(Vector3 rotationEuler)
        {
            var snap = RotationSnap;
            rotationEuler.x = Snap(rotationEuler.x, snap);
            rotationEuler.y = Snap(rotationEuler.y, snap);
            rotationEuler.z = Snap(rotationEuler.z, snap);
            return rotationEuler;
        }

        /// <summary>Snaps the given `rotationEuler` to the nearest snap increment on the specified axis (as specified in Edit/Snap Settings).</summary>
        public static Vector3 SnapRotation(Vector3 rotationEuler, int axisIndex)
        {
            rotationEuler[axisIndex] = Snap(rotationEuler[axisIndex], RotationSnap);
            return rotationEuler;
        }

        /// <summary>Snaps the given `rotation` to the nearest snap increment on all axes (as specified in Edit/Snap Settings).</summary>
        public static Quaternion SnapRotation(Quaternion rotation)
            => Quaternion.Euler(SnapRotation(rotation.eulerAngles));

        /// <summary>Snaps the given `rotation` to the nearest snap increment on the specified axis (as specified in Edit/Snap Settings).</summary>
        public static Quaternion SnapRotation(Quaternion rotation, int axisIndex)
            => Quaternion.Euler(SnapRotation(rotation.eulerAngles, axisIndex));

        /************************************************************************************************************************/

        /// <summary>Snaps the given `scale` to the nearest snap increment on all axes (as specified in Edit/Snap Settings).</summary>
        public static Vector3 SnapScale(Vector3 scale)
        {
            var snap = ScaleSnap;
            scale.x = Snap(scale.x, snap);
            scale.y = Snap(scale.y, snap);
            scale.z = Snap(scale.z, snap);
            return scale;
        }

        /// <summary>Snaps the given `scale` to the nearest snap increment on the specified axis (as specified in Edit/Snap Settings).</summary>
        public static Vector3 SnapScale(Vector3 scale, int axisIndex)
        {
            scale[axisIndex] = Snap(scale[axisIndex], ScaleSnap);
            return scale;
        }

        /************************************************************************************************************************/

        /// <summary>Returns true if `value` is approximately equal to a multiple of `snap`.</summary>
        public static bool IsSnapped(float value, float snap)
            => Mathf.Approximately(value, Mathf.Round(value / snap) * snap);

        /************************************************************************************************************************/

        [MenuItem(EditorStrings.SnapToGrid)]
        private static void SnapSelectionToGrid()
        {
            var transforms = Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.Editable);

            Undo.RecordObjects(transforms, "Snap to Grid");

            for (int i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                transform.SetLocalPositionAndRotation(
                    SnapPosition(transform.localPosition),
                    SnapRotation(transform.localRotation));
                transform.localScale = SnapScale(transform.localScale);
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region GUI
        /************************************************************************************************************************/

        /// <summary><see cref="EditorGUIUtility.standardVerticalSpacing"/>. This value is 2 in Unity 2018.</summary>
        public static float Spacing => EditorGUIUtility.standardVerticalSpacing;

        /************************************************************************************************************************/

        /// <summary>Returns a value of 1 multiplied if `fast` and divided if `slow`.</summary>
        public static float GetSensitivity(bool fast, bool slow, float multiplier = 4)
            => fast
            ? slow ? 1f : multiplier
            : slow ? 1f / multiplier : 1f;

        /// <summary>
        /// Returns a value of 1 multiplied if <see cref="Event.shift"/> and divided if <see cref="Event.control"/>.
        /// </summary>
        public static float GetSensitivity(Event currentEvent, float multiplier = 4)
            => GetSensitivity(currentEvent.shift, currentEvent.alt, multiplier);

        /// <summary>
        /// Returns a value of 1 multiplied if <see cref="Event.shift"/> and divided if <see cref="Event.control"/>.
        /// </summary>
        public static float GetSensitivity(float multiplier = 4)
            => GetSensitivity(Event.current, multiplier);

        /************************************************************************************************************************/

        /// <summary>[Pro-Only] 
        /// Draws the GUI for all <see cref="Attributes.BaseInspectableAttribute"/>s of the `targets`.
        /// </summary>
        public static void DoInspectableGUI(Object[] targets)
        {
            var target = targets[0];
            if (target == null)
                return;

            var inspectables = Inspectables.Gather(target.GetType());
            for (int i = 0; i < inspectables.Count; i++)
            {
                var inspectable = inspectables[i];
                if (inspectable.When.IsNow())
                    inspectable.OnGUI(targets);
            }
        }

        /************************************************************************************************************************/

        /// <summary>Draw the target and name of the specified <see cref="Delegate"/>.</summary>
        public static void DoDelegateGUI(Rect area, Delegate del)
        {
            var width = area.width;

            area.xMax = EditorGUIUtility.labelWidth + IndentSize;

            var obj = del.Target as Object;
            if (obj != null)
            {
                // If the target is a Unity Object, draw it in an Object Field so the user can click to ping the object.

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.ObjectField(area, obj, typeof(Object), true);
                }
            }
            else if (del.Method.DeclaringType.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), true))
            {
                // Anonymous Methods draw only their method name.

                area.width = width;

                GUI.Label(area, del.Method.GetNameCS());

                return;
            }
            else if (del.Target == null)
            {
                GUI.Label(area, del.Method.DeclaringType.GetNameCS());
            }
            else
            {
                GUI.Label(area, del.Target.ToString());
            }

            area.x += area.width;
            area.width = width - area.width;

            GUI.Label(area, del.Method.GetNameCS(false));
        }

        /************************************************************************************************************************/

        /// <summary>Used by <see cref="TempContent"/>.</summary>
        private static GUIContent _TempContent;

        /// <summary>
        /// Creates and returns a <see cref="GUIContent"/> with the specified parameters on the first call and then
        /// simply returns the same one with new parameters on each subsequent call.
        /// </summary>
        public static GUIContent TempContent(string text = null, string tooltip = null)
        {
            if (_TempContent == null)
                _TempContent = new GUIContent();

            _TempContent.text = text;
            _TempContent.tooltip = tooltip;
            return _TempContent;
        }

        /************************************************************************************************************************/

        private static Dictionary<Func<GUIStyle>, GUIStyle> _GUIStyles;

        /// <summary>
        /// Creates a <see cref="GUIStyle"/> using the provided delegate and caches it so the same style can be
        /// returned when this method is called again for the same delegate.
        /// </summary>
        /// <remarks>
        /// This method allows you to create custom styles without needing to make a new field to store them in.
        /// </remarks>
        public static GUIStyle GetCachedStyle(Func<GUIStyle> createStyle)
        {
            if (_GUIStyles == null)
                _GUIStyles = new Dictionary<Func<GUIStyle>, GUIStyle>();

            if (!_GUIStyles.TryGetValue(createStyle, out var style))
            {
                style = createStyle();
                _GUIStyles.Add(createStyle, style);

                var currentEvent = Event.current;
                if (currentEvent != null && currentEvent.type == EventType.Repaint)
                    Debug.LogWarning($"GetCachedStyle created {createStyle} during a Repaint event." +
                        " This likely means that a new delegate is being passed into every call" +
                        " so it can't actually return the same cached object.");
            }

            return style;
        }

        /************************************************************************************************************************/

        private static GUILayoutOption[] _DontExpandWidth;

        /// <summary>
        /// A single <see cref="GUILayoutOption"/> created by passing <c>false</c> into <see cref="GUILayout.ExpandWidth"/>.
        /// </summary>
        public static GUILayoutOption[] DontExpandWidth
        {
            get
            {
                if (_DontExpandWidth == null)
                    _DontExpandWidth = new GUILayoutOption[] { GUILayout.ExpandWidth(false) };
                return _DontExpandWidth;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Calls <see cref="GUIStyle.CalcMinMaxWidth"/> and returns the max width.</summary>
        public static float CalculateWidth(this GUIStyle style, GUIContent content)
        {
            style.CalcMinMaxWidth(content, out _, out var width);
            return width;
        }

        /// <summary>Calls <see cref="GUIStyle.CalcMinMaxWidth"/> and returns the max width.</summary>
        /// <remarks>This method uses the <see cref="TempContent"/>.</remarks>
        public static float CalculateWidth(this GUIStyle style, string text)
            => style.CalculateWidth(TempContent(text));

        /************************************************************************************************************************/

        /// <summary>
        /// Subtracts the `width` from the left side of the `area` and returns a new <see cref="Rect"/> occupying the
        /// removed section.
        /// </summary>
        public static Rect StealFromLeft(ref Rect area, float width)
        {
            var newRect = new Rect(area.x, area.y, width, area.height);
            area.x += width;
            area.width -= width;
            return newRect;
        }

        /// <summary>
        /// Subtracts the `width` from the left side of the `area` and returns a new <see cref="Rect"/> occupying the
        /// removed section.
        /// </summary>
        public static Rect StealFromLeft(ref Rect area, float width, RectOffset padding)
        {
            width += padding.left + padding.right;
            var newArea = StealFromLeft(ref area, width);
            newArea.x += padding.left;
            newArea.width -= padding.left + padding.right;
            newArea.y += padding.top;
            return newArea;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Subtracts the `width` from the right side of the `area` and returns a new <see cref="Rect"/> occupying the
        /// removed section.
        /// </summary>
        public static Rect StealFromRight(ref Rect area, float width)
        {
            area.width -= width;
            return new Rect(area.xMax, area.y, width, area.height);
        }

        /// <summary>
        /// Subtracts the `width` from the right side of the `area` and returns a new <see cref="Rect"/> occupying the
        /// removed section.
        /// </summary>
        public static Rect StealFromRight(ref Rect area, float width, RectOffset padding)
        {
            width += padding.left + padding.right;
            var newArea = StealFromRight(ref area, width);
            newArea.x += padding.left;
            newArea.width -= padding.left + padding.right;
            newArea.y += padding.top;
            return newArea;
        }

        /************************************************************************************************************************/

        private static float _IndentSize = -1;

        /// <summary>The number of pixels of indentation for each <see cref="EditorGUI.indentLevel"/> increment.</summary>
        public static float IndentSize
        {
            get
            {
                if (_IndentSize < 0)
                {
                    var indentLevel = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 1;
                    _IndentSize = EditorGUI.IndentedRect(new Rect()).x;
                    EditorGUI.indentLevel = indentLevel;
                }

                return _IndentSize;
            }
        }

        /************************************************************************************************************************/

        private static List<string> _LayerNames;
        private static List<int> _LayerNumbers;

        /// <summary>
        /// Make a field for layer masks.
        /// </summary>
        public static int DoLayerMaskField(Rect area, GUIContent label, int layerMask)
        {
            if (_LayerNames == null)
            {
                _LayerNames = new List<string>();
                _LayerNumbers = new List<int>();
            }
            else
            {
                _LayerNames.Clear();
                _LayerNumbers.Clear();
            }

            for (int i = 0; i < 32; i++)
            {
                var layerName = LayerMask.LayerToName(i);
                if (layerName != "")
                {
                    _LayerNames.Add(layerName);
                    _LayerNumbers.Add(i);
                }
            }

            var maskWithoutEmpty = 0;
            for (int i = 0; i < _LayerNumbers.Count; i++)
            {
                if (((1 << _LayerNumbers[i]) & layerMask) > 0)
                    maskWithoutEmpty |= 1 << i;
            }

            maskWithoutEmpty = EditorGUI.MaskField(area, label, maskWithoutEmpty, _LayerNames.ToArray());
            var mask = 0;
            for (int i = 0; i < _LayerNumbers.Count; i++)
            {
                if ((maskWithoutEmpty & (1 << i)) > 0)
                    mask |= 1 << _LayerNumbers[i];
            }

            return mask;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Scene GUI
        /************************************************************************************************************************/

        /// <summary>Registers the specified method to be called while drawing the <see cref="SceneView"/> GUI.</summary>
        /// 
        /// <remarks>
        /// This allows you to use the <see cref="Handles"/> class which isn't available during a
        /// <see cref="MonoBehaviour"/> <c>OnDrawGizmos</c> message.
        /// <para></para>
        /// If the <see cref="Delegate.Target"/> is an <see cref="Object"/>, the method will be de-registered when that
        /// object is destroyed.
        /// <para></para>
        /// Otherwise it will be de-registered when the delegate itself is garbage collected.
        /// </remarks>
        /// 
        /// <example>
        /// You can prevent the delegate from being garbage collected by storing it as you pass it into this method.
        /// <para></para><code>
        /// #if UNITY_EDITOR
        ///     private Action _OnSceneGUI;
        /// 
        ///     public ThisClassName()// Constructor.
        ///     {
        ///         InspectorGadgets.Editor.IGEditorUtils.OnSceneGUI(_OnSceneGUI = () =>
        ///         {
        ///             UnityEditor.Handles.CubeHandleCap(0, default, Quaternion.identity, 1, Event.current.type);
        ///         });
        ///     }
        /// #endif
        /// </code></example>
        public static Action OnSceneGUI(Action doGUI, bool log = false)
        {
            // If the target is an Object, stop drawing when it is destroyed.
            if (doGUI.Target is Object obj)
                return OnSceneGUI(doGUI, obj, log);

            // Otherwise stop drawing when doGUI is garbage collected.
            var weakReference = new WeakReference(doGUI);

            OnSceneGUI(() =>
            {
                var draw = weakReference.Target;
                if (draw != null)
                    ((Action)draw)();
            },
            () => weakReference.IsAlive, log);

            return doGUI;
        }

        /************************************************************************************************************************/

        /// <summary>Registers the specified method to be called while drawing the <see cref="SceneView"/> GUI.</summary>
        /// <remarks>
        /// The method will be de-registered when the `target` is destroyed.
        /// <para></para>
        /// This method does nothing if the `target` is not a scene object.
        /// </remarks>
        /// <example>See <see cref="OnSceneGUI(Action, bool)"/>.</example>
        public static Action OnSceneGUI(Action doGUI, Object target, bool log = false)
        {
            OnSceneGUI(doGUI, () => target != null && !EditorUtility.IsPersistent(target), log);
            return doGUI;
        }

        /************************************************************************************************************************/

        /// <summary>Registers the specified method to be called while drawing the <see cref="SceneView"/> GUI.</summary>
        /// <remarks>The method will be de-registered when it throws any exception or `keepDrawing` returns false.</remarks>
        /// <example>See <see cref="OnSceneGUI(Action, bool)"/>.</example>
        public static void OnSceneGUI(Action doGUI, Func<bool> keepDrawing, bool log = false)
        {
            Action<SceneView> duringSceneGui = null;
            duringSceneGui = (sceneView) =>
            {
                try
                {
                    if (keepDrawing())
                    {
                        doGUI();
                        return;
                    }
                }
                catch (Exception exception)
                {
                    if (log)
                        Debug.LogException(exception);
                }

                if (log)
                    Debug.Log("Removing " + doGUI.Method);

                SceneView.duringSceneGui -= duringSceneGui;
            };

            if (log)
                Debug.Log("Adding " + doGUI.Method);

            SceneView.duringSceneGui += duringSceneGui;
        }

        /************************************************************************************************************************/

        /// <summary>Gets the transform of an available camera from a scene view or the scene.</summary>
        public static void GetCurrentCameraTransform(out Vector3 position, out Quaternion rotation)
        {
            var camera = GetCurrentCameraTransform();
            if (camera != null)
            {
                position = camera.position;
                rotation = camera.rotation;
            }
            else
            {
                position = default;
                rotation = Quaternion.identity;
            }
        }

        /// <summary>Gets the transform of an available camera from a scene view or the scene.</summary>
        public static Transform GetCurrentCameraTransform()
        {
            if (EditorApplication.isPlaying)
            {
                var camera = GetSceneCameraTransform();
                if (camera != null)
                    return camera;

                return GetSceneViewCameraTransform();
            }
            else
            {
                var camera = GetSceneViewCameraTransform();
                if (camera != null)
                    return camera;

                return GetSceneCameraTransform();
            }
        }

        /// <summary>Gets the transform of an available camera from a scene view.</summary>
        public static Transform GetSceneViewCameraTransform()
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
                return sceneView.camera.transform;

            foreach (SceneView otherSceneView in SceneView.sceneViews)
                return otherSceneView.camera.transform;

            return null;
        }

        /// <summary>Gets the transform of an available camera from a scene view.</summary>
        public static Transform GetSceneCameraTransform()
        {
            var camera = Camera.main;
            if (camera != null)
                return camera.transform;

            camera = FindObject<Camera>();
            if (camera != null)
                return camera.transform;

            return null;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Editor State
        /************************************************************************************************************************/

        private const string DefaultEditorStatePrefKey = nameof(InspectorGadgets) + "." + nameof(DefaultEditorState);

        /// <summary>Determines when to show Inspectable attributes if not specified in their constructor.</summary>
        /// <remarks>This value is stored in <see cref="PlayerPrefs"/>.</remarks>
        public static EditorState DefaultEditorState
        {
            get => (EditorState)PlayerPrefs.GetInt(DefaultEditorStatePrefKey);
            set => PlayerPrefs.SetInt(DefaultEditorStatePrefKey, (int)value);
        }

        /// <summary>
        /// Returns the `state` as long as it isn't <c>null</c>.
        /// Otherwise returns the <see cref="DefaultEditorState"/>.
        /// </summary>
        public static EditorState ValueOrDefault(this EditorState? state)
            => state != null ? state.Value : DefaultEditorState;

        /// <summary>Returns true if the Unity Editor is currently in the specified `state`.</summary>
        public static bool IsNow(this EditorState state)
        {
            switch (state)
            {
#if UNITY_EDITOR
                case EditorState.Playing:
                    return UnityEditor.EditorApplication.isPlaying;
                case EditorState.Editing:
                    return !UnityEditor.EditorApplication.isPlaying;
#else
                case EditorState.Playing:
                    return true;
                case EditorState.Editing:
                    return false;
#endif
                case EditorState.Always:
                default:
                    return true;
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Unity Compatibility
        /************************************************************************************************************************/

        internal static Object[] FindObjects(Type type)
#if UNITY_6000_0_OR_NEWER
            => Object.FindObjectsByType(type, FindObjectsSortMode.None);
#else
            => Object.FindObjectsOfType(type);
#endif

        /************************************************************************************************************************/

        internal static T FindObject<T>()
            where T : Object
#if UNITY_6000_0_OR_NEWER
            => Object.FindFirstObjectByType<T>();
#else
            => Object.FindObjectOfType<T>();
#endif

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Other
        /************************************************************************************************************************/

        private static Dictionary<Type, string> _TypeToDefaultExtension;

        /// <summary>Returns the default file extension for a type derived from <see cref="Object"/>.</summary>
        public static string GetDefaultFileExtension(Type type)
        {
            if (_TypeToDefaultExtension == null)
            {
                _TypeToDefaultExtension = new Dictionary<Type, string>
                {
                    { typeof(GameObject), "prefab" },

                    { typeof(Texture), "png" },
                    { typeof(Texture2D), "png" },
                    { typeof(Sprite), "png" },
                    
#if UNITY_ANIMATION
                    { typeof(AnimationClip), "anim" },
                    { typeof(RuntimeAnimatorController), "controller" },
                    { typeof(AnimatorOverrideController), "overrideController" },
                    { typeof(AvatarMask), "mask" },
                    { typeof(Avatar), null },
#endif
                    
#if UNITY_AUDIO
                    { typeof(UnityEngine.Audio.AudioMixer), "mixer" },
                    { typeof(AudioClip), null },
#endif
                    
#if UNITY_PHYSICS_2D
                    { typeof(PhysicsMaterial2D), "physicsMaterial2D" },
#endif

#if UNITY_PHYSICS_3D
#if UNITY_6000_0_OR_NEWER
                    { typeof(PhysicsMaterial), "physicMaterial" },
#else
                    { typeof(PhysicMaterial), "physicMaterial" },
#endif
#endif

                    { typeof(GUISkin), "guiSkin" },
                    { typeof(Material), "mat" },
                    { typeof(Shader), "shader" },
                    { typeof(ShaderVariantCollection), "shaderVariants" },
                    { typeof(ComputeShader), "compute" },
                    { typeof(Cubemap), "cubeMap" },
                    { typeof(Flare), "flare" },
                    { typeof(LightmapParameters), "giParams" },
                    { typeof(MonoScript), "cs" },
                    { typeof(UnityEngine.U2D.SpriteAtlas), "spriteAtlas" },

                    { typeof(Texture2DArray), null },
                    { typeof(Texture3D), null },
                };
            }

            if (_TypeToDefaultExtension.TryGetValue(type, out var extension))
                return extension;

            if (typeof(Component).IsAssignableFrom(type)) return "prefab";
            if (typeof(ScriptableObject).IsAssignableFrom(type)) return "asset";
            if (typeof(TextAsset).IsAssignableFrom(type)) return "txt";

            return "asset";
        }

        /************************************************************************************************************************/

        private static StringBuilder _ColorBuilder;

        /// <summary>Returns a string containing the hexadecimal representation of `color`.</summary>
        public static string ColorToHex(Color32 color)
        {
            if (_ColorBuilder == null)
                _ColorBuilder = new StringBuilder();
            else
                _ColorBuilder.Length = 0;

            AppendColorToHex(_ColorBuilder, color);
            return _ColorBuilder.ToString();
        }

        /// <summary>Appends the hexadecimal representation of `color`.</summary>
        public static void AppendColorToHex(StringBuilder text, Color32 color)
        {
            text.Append(color.r.ToString("X2"));
            text.Append(color.g.ToString("X2"));
            text.Append(color.b.ToString("X2"));
            text.Append(color.a.ToString("X2"));
        }

        /// <summary>Appends the a rich text color tag around the `message`.</summary>
        public static void AppendColorTag(StringBuilder text, Color32 color, string message)
        {
            text.Append("<color=#");
            AppendColorToHex(text, color);
            text.Append('>');
            text.Append(message);
            text.Append("</color>");
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Calls the specified `method` for each <see cref="SerializedProperty"/> in the `serializedObject` then
        /// applies any modified properties.
        /// </summary>
        public static void ForEachProperty(SerializedObject serializedObject, bool enterChildren, Action<SerializedProperty> method)
        {
            var property = serializedObject.GetIterator();
            if (!property.Next(true))
                return;

            do
            {
                method(property);
            }
            while (property.Next(enterChildren));

            serializedObject.ApplyModifiedProperties();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Calls <see cref="AssetDatabase.FindAssets(string, string[])"/> using the specified `type` as the filter.
        /// <para></para>
        /// If the `type` inherits from <see cref="Component"/> then it will instead use <see cref="GameObject"/> as
        /// the filter to find all Prefabs (since Unity won't find <see cref="Component"/> types directly).
        /// </summary>
        public static string[] FindAssetGuidsOfType(Type type)
        {
            var isComponent = typeof(Component).IsAssignableFrom(type);
            var filter = isComponent ? $"t:{nameof(GameObject)}" : $"t:{type.Name}";
            return AssetDatabase.FindAssets(filter);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Searches through all assets of the specified `type` and returns the one with a name closest to the
        /// `nameHint`.
        /// </summary>
        public static Object FindAssetOfType(Type type, string nameHint = null)
        {
            try
            {
                var title = $"Find {type} Asset";

                if (EditorUtility.DisplayCancelableProgressBar(title, "Finding GUIDs", 0))
                    return null;

                var guids = FindAssetGuidsOfType(type);
                if (guids.Length == 0)
                    return null;

                var loadedAssetCount = 0;

                var assets = new Object[guids.Length];
                for (int i = 0; i < guids.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[i]);

                    var progress = Mathf.Lerp(0.1f, 1, i / (float)guids.Length);
                    if (EditorUtility.DisplayCancelableProgressBar(title, $"Loading {path}", progress))
                        return null;

                    var asset = AssetDatabase.LoadAssetAtPath(path, type);
                    if (asset != null)
                    {
                        assets[i] = asset;
                        loadedAssetCount++;
                    }
                }

                if (EditorUtility.DisplayCancelableProgressBar(title,
                    $"Finding best name match of {loadedAssetCount} assets", 1))
                    return null;

                return IGUtils.GetBestMatch(assets, nameHint);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif
