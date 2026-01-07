// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using InspectorGadgets.Attributes;
using InspectorGadgets.Editor.PropertyDrawers;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace InspectorGadgets.Editor
{
#if ! DISABLE_IG_COMPONENT_EDITOR
    [CustomEditor(typeof(Component), true, isFallback = true), CanEditMultipleObjects]
#endif
    internal sealed class ComponentEditor : Editor<Component>
    {
        // This is here because having it in the generic base class would make a new instance for each generic subtype.
        public static readonly AutoPrefs.EditorBool
            HideScriptProperty = new AutoPrefs.EditorBool(EditorStrings.PrefsKeyPrefix + "HideScriptProperty", true);
    }

#if ! DISABLE_IG_SCRIPTABLE_OBJECT_EDITOR
    [CustomEditor(typeof(ScriptableObject), true, isFallback = true), CanEditMultipleObjects]
#endif
    internal sealed class ScriptableObjectEditor : Editor<ScriptableObject>
    {
        /************************************************************************************************************************/

        /// <summary>
        /// Called by the Unity editor to draw the custom inspector GUI elements.
        /// <para></para>
        /// Draws the regular inspector then adds a message explaining that changes in Play Mode will persist.
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (target != null &&
                EditorApplication.isPlayingOrWillChangePlaymode &&
                EditorUtility.IsPersistent(target))
            {
                EditorGUILayout.HelpBox("This is an asset, not a scene object," +
                    " which means that any changes you make to it are permanent" +
                    " and will NOT be undone when you exit Play Mode.", MessageType.Warning);
            }
        }

        /************************************************************************************************************************/
    }

#if UNITY_ANIMATION && ! DISABLE_IG_STATE_MACHINE_BEHAVIOUR_EDITOR
    [CustomEditor(typeof(StateMachineBehaviour), true, isFallback = true), CanEditMultipleObjects]
    internal sealed class StateMachineBehaviourEditor : Editor<StateMachineBehaviour> { }
#endif

    /************************************************************************************************************************/

    /// <summary>[Pro-Only] [Editor-Only]
    /// Base class to derive custom editors from, with a bunch of additional features on top of Unity's base
    /// <see cref="UnityEditor.Editor"/> class.
    /// </summary>
    /// <remarks>
    /// Doesn't draw the target's "Script" field to save inspector space and reduce clutter.
    /// <para></para>
    /// You can Middle Click anywhere in the inspector area to open the script in your script editor
    /// or Ctrl + Middle Click to open its editor script (or create one if none exists already).
    /// <para></para>
    /// Provides type-casted versions of <see cref="UnityEditor.Editor.target"/> and
    /// <see cref="UnityEditor.Editor.targets"/> so you don't always have to do it yourself
    /// (<see cref="Target"/> and <see cref="Targets"/> respectively).
    /// </remarks>
    public abstract class Editor<T> : MissingScriptEditor where T : class
    {
        /************************************************************************************************************************/
        #region Fields and Properties
        /************************************************************************************************************************/

        [NonSerialized] private bool _HasReflectedTarget;
        [NonSerialized] private List<BaseInspectableAttributeDrawer> _Inspectables;
        [NonSerialized] private Action _OnInspectorGUI, _AfterInspectorGUI;
        [NonSerialized] private MethodCache.OverridePropertyGUIMethod _OverridePropertyGUI;

        /************************************************************************************************************************/

        [NonSerialized] private bool _HasTarget;
        [NonSerialized] private T _Target;
        [NonSerialized] private T[] _Targets;

        /// <summary>The object being inspected (<see cref="UnityEditor.Editor.target"/> casted to T).</summary>
        public T Target
        {
            get
            {
                if (!_HasTarget)
                {
                    _HasTarget = true;
                    _Target = target as T;
                }

                return _Target;
            }
        }

        /// <summary>An array of all the objects being inspected (<see cref="UnityEditor.Editor.targets"/> casted to T).</summary>
        public T[] Targets
        {
            get
            {
                if (_Targets == null)
                {
                    var count = targets.Length;

                    _Targets = new T[count];

                    _Targets[0] = Target;

                    var i = 1;
                    for (; i < count; i++)
                        _Targets[i] = targets[i] as T;
                }

                return _Targets;
            }
        }

        /************************************************************************************************************************/

        /// <summary>The editor currently being drawn.</summary>
        public static Editor<T> Current { get; private set; }

        /// <summary>
        /// The object being inspected (<see cref="UnityEditor.Editor.target"/> casted to T) by the editor currently
        /// being drawn.
        /// </summary>
        public static T CurrentTarget => Current.Target;

        /// <summary>
        /// An array of all the objects being inspected (<see cref="UnityEditor.Editor.targets"/> casted to T) by the
        /// editor currently being drawn.
        /// </summary>
        public static T[] CurrentTargets => Current.Targets;

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Initialization

        /// <summary>
        /// Gathers all of the target's inspectables (<see cref="ButtonAttribute"/> and <see cref="LabelAttribute"/>)
        /// and checks for inspector GUI events in the script.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            if (target != null && !_HasReflectedTarget)
            {
                _HasReflectedTarget = true;

                _Inspectables = Inspectables.Gather(target.GetType());

                _OnInspectorGUI = MethodCache.OnInspectorGUI.GetDelegate(target);
                if (_OnInspectorGUI == null)
                {
                    _AfterInspectorGUI = MethodCache.AfterInspectorGUI.GetDelegate(target);
                    _OverridePropertyGUI = MethodCache.OverridePropertyGUI.GetDelegate(target);
                }
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Execution
        /************************************************************************************************************************/

        /// <summary>
        /// Draws the target's regular inspector followed by any extra inspectables
        /// (<see cref="ButtonAttribute"/> and <see cref="LabelAttribute"/>), and responds to Middle
        /// Click events.
        /// <para></para>
        /// To modify or replace just the regular inspector and keep the extra features of <see cref="Editor{T}"/>,
        /// override <see cref="DoPropertiesAndInspectables()"/> instead of <see cref="OnInspectorGUI"/>.
        /// </summary>
        public override void OnInspectorGUI()
        {
            if (target == null || serializedObject.targetObject == null)
            {
                base.OnInspectorGUI();
                return;
            }

            Current = this;

            if (_OnInspectorGUI != null)
                _OnInspectorGUI();
            else
                DoInspectorGUI();
        }

        /************************************************************************************************************************/

        /// <summary>Draws the inspector GUI of the <see cref="Current"/> editor.</summary>
        public static void DoInspectorGUI()
        {
            var current = Current;

            var rect = EditorGUILayout.BeginVertical();

            current.serializedObject.Update();

            current.DoPropertiesAndInspectables();

            current._AfterInspectorGUI?.Invoke();

            if (current.serializedObject.ApplyModifiedProperties())
                current.OnPropertyModified();

            EditorGUILayout.EndVertical();
            current.CheckMiddleClick(rect);
        }

        /************************************************************************************************************************/

        /// <summary>Draws all of the target's serialized properties and inspectables.</summary>
        public virtual void DoPropertiesAndInspectables()
        {
            //DrawDefaultInspector(); return;

            if (!ComponentEditor.HideScriptProperty && ScriptProperty != null)
                DoPropertyGUI(ScriptProperty);

            var inspectableIndex = 0;
            var inspectableCount = _Inspectables != null ? _Inspectables.Count : 0;

            for (int i = 0; i < OtherProperties.Count; i++)
            {
                // Draw any inspectables with the current index.
                while (inspectableIndex < inspectableCount)
                {
                    var inspectable = _Inspectables[inspectableIndex];
                    if (inspectable.DisplayIndex <= i)
                    {
                        if (inspectable.When.IsNow())
                            inspectable.OnGUI(targets);

                        inspectableIndex++;
                    }
                    else break;
                }

                DoPropertyGUI(OtherProperties[i]);
            }

            // Draw any remaining inspectables.
            while (inspectableIndex < inspectableCount)
            {
                var inspectable = _Inspectables[inspectableIndex];
                if (inspectable.When.IsNow())
                    inspectable.OnGUI(targets);

                inspectableIndex++;
            }
        }

        /************************************************************************************************************************/

        private static readonly GUIContent PropertyLabel = new GUIContent();

        private void DoPropertyGUI(SerializedProperty property)
        {
            PropertyLabel.text = property.displayName;
            PropertyLabel.tooltip = property.tooltip;

            if (_OverridePropertyGUI != null)
            {
                if (_OverridePropertyGUI(property, PropertyLabel))
                    return;
            }

            EditorGUILayout.PropertyField(property, PropertyLabel, true);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// This method is called if any of the target's serialized members are modified during
        /// <see cref="DoInspectorGUI()"/>.
        /// </summary>
        /// <remarks>This method does nothing unless overridden.</remarks>
        public virtual void OnPropertyModified() { }

        /************************************************************************************************************************/

        /// <summary>
        /// Checks if the current event is a Middle Click to open the script in the user's script editor application,
        /// or Ctrl + Middle Click to open or create its custom inspector script.
        /// </summary>
        public virtual void CheckMiddleClick(Rect area)
        {
            area.yMin -= 16;// Move the top of the rect up to include the component's title bar.
            var currentEvent = Event.current;

            if (currentEvent.type == EventType.MouseUp &&
                currentEvent.button == 2 &&
                area.Contains(currentEvent.mousePosition))
            {
                currentEvent.Use();

                EditorApplication.delayCall += () =>
                {
                    if (currentEvent.control)// Ctrl + Middle click to open or create the editor script.
                    {
                        var script = MonoScript.FromScriptableObject(this);
                        if (script != null && GetType().Assembly != typeof(Editor<>).Assembly)
                            AssetDatabase.OpenAsset(script);
                        else
                            IGEditorUtils.CreateEditorScript(target);
                    }
                    else if (currentEvent.shift)// Shift + Middle click to ping the script asset.
                    {
                        IGEditorUtils.PingScriptAsset(target);
                    }
                    else// Middle click to open the script.
                    {
                        var behaviour = target as MonoBehaviour;
                        if (behaviour != null)
                        {
                            AssetDatabase.OpenAsset(MonoScript.FromMonoBehaviour(behaviour));
                        }
                        else
                        {
                            var scriptable = target as ScriptableObject;
                            if (scriptable != null)
                                AssetDatabase.OpenAsset(MonoScript.FromScriptableObject(scriptable));
                        }
                    }
                };
            }
        }

        /************************************************************************************************************************/

        /// <summary>[Pro-Only] Draw all <see cref="InspectableAttribute"/> members.</summary>
        public void DoAllInspectables()
        {
            for (int i = 0; i < _Inspectables.Count; i++)
            {
                var inspectable = _Inspectables[i];
                if (inspectable.When.IsNow())
                    inspectable.OnGUI(targets);
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Records the current state of the <see cref="Targets"/> so that any subsequent changes can be undone
        /// (reverted back to the recorded state).
        /// </summary>
        public void RecordTargetUndo(string undoName)
        {
            Undo.RecordObjects(targets, undoName);
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif
