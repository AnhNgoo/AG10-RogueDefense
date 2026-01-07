// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using UnityEngine;
using Object = UnityEngine.Object;

namespace InspectorGadgets.Editor
{
    /// <summary>[Editor-Only] String constants used throughout <see cref="InspectorGadgets"/>.</summary>
    public static class EditorStrings
    {
        /************************************************************************************************************************/

        public const string PrefsKeyPrefix = nameof(InspectorGadgets) + ".";

        public const string Context = "CONTEXT/";
        public const string Alt = "&";
        public const string Ctrl = "%";
        public const string Shift = "#";

        /// <summary>
        /// Menu items where the last word begins with an underscore or certain other characters are interpreted as
        /// having a keyboard shortcut. So we use the '\b' (backspace) character to prevent it from doing that.
        /// </summary>
        public const string NegateShortcut = "\b";

        // Some of the following constants aren't using nameof statements
        // due to referencing issues in InspectorGadgets.Lite.dll.

        public const string CommentAssetIncludeInBuild = Context + "CommentAsset/Include in Build";
        public const string CommentComponentIncludeInBuild = Context + "CommentComponent/Include in Build";

        public const string RectTransformToggleAutoHideUI = Context + nameof(RectTransform) + "/Toggle Auto Hide UI";
        public const string UIBehaviourToggleAutoHideUI = Context + "UIBehaviour/Toggle Auto Hide UI";
#if UNITY_UGUI
        public const string CanvasToggleAutoHideUI = Context + nameof(Canvas) + "/Toggle Auto Hide UI";
#endif

        public const string PingScriptAsset = Context + nameof(MonoBehaviour) + "/Ping Script Asset";
        public const string ShowOrHideScriptProperty = Context + nameof(MonoBehaviour) + "/Show or Hide Script Property";

        public const string CreateEditorScript = Context + nameof(Component) + "/Create Editor Script";

        public const string CopyTransformPath = Context + nameof(Transform) + "/Copy Transform Path";
        public const string OpenDocumentation = Context + nameof(Transform) + "/Inspector Gadgets Documentation";
        public const string CollapseAllComponents = Context + nameof(Transform) + "/Collapse All Components";
        public const string RemoveAllComponents = Context + nameof(Transform) + "/Remove All Components";
        public const string PingObject = Context + nameof(Transform) + "/Ping Object in Hierarchy";

        public const string NewLockedInspector = "/New Locked Inspector " + Ctrl + Alt + "I";
        public const string MainNewLockedInspector = "Edit/Selection" + NewLockedInspector;
        public const string ObjectNewLockedInspector = Context + nameof(Object) + NewLockedInspector;

        public const string GameObjectResetSelectedTransforms = nameof(GameObject) + "/Reset Selected Transforms " + Ctrl + Alt + "Z";
        public const string SnapToGrid = nameof(GameObject) + "/Snap to Grid " + Ctrl + Alt + "X";

        public const string Watch = Context + nameof(Object) + "/Watch";

        /************************************************************************************************************************/

        public const string PersistAfterPlayMode = "Persist After Play Mode";
        public const string PersistAfterPlayModeComponent = Context + nameof(Component) + "/" + PersistAfterPlayMode;

        /************************************************************************************************************************/

        public static class GUI
        {
            /************************************************************************************************************************/

            public const string
                LocalWorldTooltip = "[L] Local position/rotation/scale\n[W] World position/rotation/scale",
                ScaleModeTooltip = "[=] Uniform scale\n[≠] Non-uniform scale",
                ScaleSkewWarning = "An arbitrarily rotated object cannot have its World Scale properly represented by a Vector3" +
                    " because it may be skewed, so the value may not be accurate.",
                PrecisionWarning = "Due to floating-point precision limitations, it is recommended to bring the world coordinates" +
                    " of the GameObject within a smaller range.";

            /************************************************************************************************************************/

            public static readonly GUIContent
                X = new GUIContent("X"),
                Y = new GUIContent("Y"),
                Z = new GUIContent("Z"),
                LocalMode = new GUIContent("L", LocalWorldTooltip),
                WorldMode = new GUIContent("W", LocalWorldTooltip),
                FreezeChildTransforms = new GUIContent("F", "Freeze child transforms?"),
                DrawAllGizmos = new GUIContent("G", "Draw gizmos for all selected objects?"),
                Copy = new GUIContent("C", "Left Click = Copy to clipboard\nRight Click = Log current value"),
                Reset = new GUIContent("R", "Reset to Default"),
                UniformScale = new GUIContent("≠", ScaleModeTooltip),
                NonUniformScale = new GUIContent("=", ScaleModeTooltip);

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
    }
}

#endif
