// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using InspectorGadgets.Editor.PropertyDrawers;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace InspectorGadgets.Editor
{
    /// <summary>[Editor-Only] GUI for Inspector Gadgets settings.</summary>
    /// <remarks>These settings are accessible via <c>Edit/Preferences/Inspector Gadgets</c>.</remarks>
    public static class Preferences
    {
        /************************************************************************************************************************/

        private const string
            TransformInspector = "Transform Inspector",
            SceneTools = "Scene Tools",
            ScriptInspector = "Script Inspector",
            AssetDragAndDrop = "Asset Drag and Drop";

        /************************************************************************************************************************/

        private static SettingsProvider _SettingsProvider;

        /// <summary>Creates this preferences page.</summary>
        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            if (_SettingsProvider == null)
            {
                var keywords = new HashSet<string>
                {
                    TransformInspector,
                    SceneTools,
                    ScriptInspector,
                    AssetDragAndDrop,
                    AutoHideUI.Preferences.Headding,
                    EnhancedHierarchy.Preferences.Headding,
                };
                GatherGUIContentText(typeof(Preferences), keywords);
                GatherGUIContentText(typeof(AutoHideUI.Preferences), keywords);
                GatherGUIContentText(typeof(EnhancedHierarchy.Preferences), keywords);

                _SettingsProvider = new SettingsProvider("Preferences/Inspector Gadgets", SettingsScope.User)
                {
                    label = "Inspector Gadgets",
                    guiHandler = searchContext => DoGUI(),
                    keywords = keywords,
                };
            }

            return _SettingsProvider;
        }

        /************************************************************************************************************************/

        private static void GatherGUIContentText(Type type, HashSet<string> set)
        {
            var fields = type.GetFields(IGEditorUtils.StaticBindings);
            for (int i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                if (field.FieldType != typeof(GUIContent))
                    continue;

                var value = (GUIContent)field.GetValue(null);
                if (value != null)
                    set.Add(value.text);
            }
        }

        /************************************************************************************************************************/

        /// <summary>Callback for drawing the <c>SceneViewObjectPickerWindow</c> preferences.</summary>
        /// <remarks>This allows that class to be defined outside the Inspector Gadgets Lite DLL.</remarks>
        public static event Action DoSceneViewObjectPickerWindowGUI;

        private static void DoGUI()
        {
            DoHeaderGUI();

            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = GUILayoutUtility.GetLastRect().width * 0.5f;

            DoTransformInspectorGUI();
            DoSceneToolsGUI();
            DoScriptInspectorGUI();
            AutoHideUI.Preferences.DoGUI();
            DoSceneViewObjectPickerWindowGUI?.Invoke();
            EnhancedHierarchy.Preferences.DoGUI();
            DoAssetDragAndDropGUI();

            GUI.enabled = true;// Still let the scroll bar catch scroll wheel events.

            EditorGUIUtility.labelWidth = labelWidth;
        }

        /************************************************************************************************************************/

        private static void DoHeaderGUI()
        {
            var headding = "Inspector Gadgets Pro v" + Strings.InspectorGadgetsVersion;
            GUILayout.Label(headding, EditorStyles.boldLabel);

            if (GUILayout.Button($"Open Documentation:  {Strings.DocumentationURL}"))
                IGEditorUtils.OpenDocumentation();

            if (GUILayout.Button($"Contact:  {Strings.DeveloperEmail}"))
            {
                GUIUtility.systemCopyBuffer = Strings.DeveloperEmail;
                Debug.Log($"Copied '{Strings.DeveloperEmail}' to the clipboard");
                EditorUtility.OpenWithDefaultApp($"mailto:{Strings.DeveloperEmail}?subject=InspectorGadgets");
            }

            if (GUILayout.Button($"Change Log"))
                IGEditorUtils.OpenDocumentation("/docs/changes");
        }

        /************************************************************************************************************************/

        private static readonly GUIContent
            ShowCopyButton = new GUIContent("Show Copy Button",
                "Should the Transform Inspector show the [C] button to copy a transform property to an internal clipboard?"),
            ShowPasteButton = new GUIContent("Show Paste Button",
                "Should the Transform Inspector show the [P] button to paste a transform property from the internal clipboard?"),
            ShowSnapButton = new GUIContent("Show Snap Button",
                "Should the Transform Inspector show the [S] button to snap a transform property to the nearest snap increment specified in Edit/Snap Setings?"),
            ShowResetButton = new GUIContent("Show Reset Button",
                "Should the Transform Inspector show the [R] button to reset a transform property to its default value?"),
            DisableUselessButtons = new GUIContent("Disable Useless Buttons",
                "Should the above buttons be greyed out when they would do nothing?"),
            UseFieldColors = new GUIContent("Use Field Colors",
                "Should the X/Y/Z fields be colored Red/Green/Blue respectively?"),
            FieldPrimaryColor = new GUIContent("Field Primary Color",
                "The strength of the main color for each axis."),
            FieldSecondaryColor = new GUIContent("Field Secondary Color",
                "The strength of the other colors."),
            ShowApproximationsLabel = new GUIContent("Show Approximations",
                "Should the fields show approximations if the value is too long for the GUI?"),
            ItaliciseNonSnappedFields = new GUIContent("Italicise Non-Snapped Fields",
                "Should Transform fields which aren't a multiple of the snap increment specified in Edit/Snap Setings use italic text?"),
            ShrinkScientificFields = new GUIContent("Shrink Scientific Fields",
                "Should the font size be decreased for values that are displayed using scientific notation?"),
            DefaultToUniformScale = new GUIContent("Default to Uniform Scale",
                "Should Transform scale be shown as a single float field by default when the selected object has the same scale on all axes?"),
            SnapToGroundDistance = new GUIContent("Snap to Ground Distance",
                "The distance within which to check for the ground when using the Snap to Ground function in the Transform Position context menu."),
            SnapToGroundLayers = new GUIContent("Snap to Ground Layers",
                "This layer mask determines which physics layers are treated as ground for the Transform Position context menu.");

        private static void DoTransformInspectorGUI()
        {
            DoSectionHeader(TransformInspector);

            var enabled = GUI.enabled;

            TransformPropertyDrawer.ShowCopyButton.DoGUI(ShowCopyButton);
            TransformPropertyDrawer.ShowPasteButton.DoGUI(ShowPasteButton);
            TransformPropertyDrawer.ShowSnapButton.DoGUI(ShowSnapButton);
            TransformPropertyDrawer.ShowResetButton.DoGUI(ShowResetButton);
            TransformPropertyDrawer.DisableUselessButtons.DoGUI(DisableUselessButtons);

            TransformPropertyDrawer.UseFieldColors.DoGUI(UseFieldColors);
            if (!TransformPropertyDrawer.UseFieldColors)
                GUI.enabled = false;

            TransformPropertyDrawer.FieldPrimaryColor.DoGUI(FieldPrimaryColor, GUI.skin.horizontalSlider,
            (area, label, value, style) =>
            {
                return EditorGUI.Slider(area, label, value, 0, 1);
            });

            TransformPropertyDrawer.FieldSecondaryColor.DoGUI(FieldSecondaryColor, GUI.skin.horizontalSlider,
            (area, label, value, style) =>
            {
                return EditorGUI.Slider(area, label, value, 0, 1);
            });

            TransformPropertyDrawer.ShowApproximations.DoGUI(ShowApproximationsLabel);
            TransformPropertyDrawer.ItaliciseNonSnappedFields.DoGUI(ItaliciseNonSnappedFields);

            GUI.enabled = enabled;

            TransformEditor.DefaultToUniformScale.DoGUI(DefaultToUniformScale);

            if (PositionDrawer.SnapToGroundDistance.DoGUI(SnapToGroundDistance))
                PositionDrawer.SnapToGroundDistance.Value = Mathf.Max(PositionDrawer.SnapToGroundDistance, 0);
            PositionDrawer.SnapToGroundLayers.DoGUI(SnapToGroundLayers,
                (position, label, value, style) => IGEditorUtils.DoLayerMaskField(position, label, value));
        }

        /************************************************************************************************************************/

        private static readonly GUIContent
            OverrideTransformGizmos = new GUIContent("Override Transform Gizmos",
                "Should the default scene gizmos be overwritten in order to implement various features like \"Freeze child transforms\" and \"Draw gizmos for all selected objects\"?"),
            ShowMovementGuides = new GUIContent("Show Movement Guides",
                "Should the scene view movement tool show some extra lines while you are moving an object to indicate where you are moving it from?"),
            ShowMovementDistance = new GUIContent("Show Movement Distance",
                "Should moving an object display the distance from the old position?"),
            ShowMovementDistancePerAxis = new GUIContent("Show Movement Distance Per Axis",
                "Should the distance moved on each individual axis also be displayed?"),
            SceneLabelBackgroundColor = new GUIContent("Scene Label Background Color",
                "The color to use behind scene view labels to make them easier to read."),
            ShowPositionLabels = new GUIContent("Show Position Labels",
                "Should the scene view show the selected object's position around the Move tool?");

        private static void DoSceneToolsGUI()
        {
            DoSectionHeader(SceneTools);

            TransformEditor.OverrideTransformGizmos.DoGUI(OverrideTransformGizmos);
            if (!TransformEditor.OverrideTransformGizmos)
            {
                EditorGUILayout.HelpBox(
                    "With this disabled, features like 'Freeze child transforms' and" +
                    " 'Draw gizmos for all selected objects' won't work.",
                    MessageType.Warning);
                Tools.hidden = false;
                GUI.enabled = false;
            }

            CustomTransformGizmo.DoPreferencesGUI();

            PositionDrawer.ShowMovementGuides.DoGUI(ShowMovementGuides);
            PositionDrawer.ShowMovementDistance.DoGUI(ShowMovementDistance);

            var enabled = GUI.enabled;

            if (!PositionDrawer.ShowMovementDistance)
                GUI.enabled = false;

            PositionDrawer.ShowMovementDistancePerAxis.DoGUI(ShowMovementDistancePerAxis);

            GUI.enabled = enabled;

            InternalGUI.SceneLabelBackgroundColor.DoColorGUI(SceneLabelBackgroundColor);

            PositionDrawer.ShowPositionLabels.DoGUI(ShowPositionLabels);
        }

        /************************************************************************************************************************/

        private static readonly GUIContent
            HideScriptProperty = new GUIContent("Hide Script Property",
                "Should the \"Script\" property at the top of each MonoBehaviour inspector be hidden to save space?"),
            AutoGatherRequiredComponents = new GUIContent("Auto Gather Required Components",
                "Should selecting an object in the editor automatically gather references for any of its component fields with a [RequireAssignment] attribute?" +
                "\n\nGathering is conducted using InspectorGadgetsUtils.GetComponentInHierarchy which finds the most appropriately named component in the selected object's children or parents."),
            AutoGatherSerializedComponents = new GUIContent("Auto Gather Serialized Components",
                "Should selecting an object in the editor automatically gather references for any of its component fields which are public or have [SerializeField] attribute?" +
                "\n\nGathering is conducted using InspectorGadgetsUtils.GetComponentInHierarchy which finds the most appropriately named component in the selected object's children or parents."),
            ItaliciseSelfReferences = new GUIContent("Italicise Self References",
                "Should Object reference fields be drawn in italics when referencing another component on the same GameObject?"),
            AlwaysShowGet = new GUIContent("Always Show Get",
                "� If enabled, the Get button will be shown on Object reference fields at all times." +
                "\n� If disabled, it will only be shown when the field is empty or you're holding Alt."),
            DefaultEditorState = new GUIContent("Default Editor State",
                "When to show Inspectables if not specified in their constructor"),
            ObjectEditorNestLimit = new GUIContent("Object Editor Nest Limit",
                "If higher than 0, Object fields will be drawn with a foldout arrow to draw the target object's inspector nested inside the current one.");

        private static void DoScriptInspectorGUI()
        {
            DoSectionHeader(ScriptInspector);

            ComponentEditor.HideScriptProperty.DoGUI(HideScriptProperty);
            ObjectDrawer.ItaliciseSelfReferences.DoGUI(ItaliciseSelfReferences);
            ObjectDrawer.AlwaysShowGet.DoGUI(AlwaysShowGet);

            ObjectDrawer.ObjectEditorNestLimit.DoGUI(ObjectEditorNestLimit,
                (position, label, value, style) => EditorGUI.IntSlider(position, label, value, 0, 10));

            EditorGUI.BeginChangeCheck();
            var defaultEditorState = EditorGUILayout.EnumPopup(DefaultEditorState, IGEditorUtils.DefaultEditorState);
            if (EditorGUI.EndChangeCheck())
                IGEditorUtils.DefaultEditorState = (EditorState)defaultEditorState;

            if (GUILayout.Button("Find and Fix Missing Scripts"))
                EditorWindow.GetWindow<MissingScriptWindow>();
        }

        /************************************************************************************************************************/

        private static readonly GUIContent
            DragAndDropSubAssetsLabel = new GUIContent("Drag and Drop Sub-Assets",
                "Holding these keys while you Drag and Drop one Asset onto another will move the dragged asset" +
                " into the target as a Sub-Asset."),
            AdvancedDragAndDropLabel = new GUIContent("Advanced Drag and Drop Assets",
                "Holding these keys while you Drag and Drop Assets or External Files will open a window with options" +
                " to choose what you want to do with the dropped files." +
                "\n\nNote that you must drop onto an Asset. Dropping onto empty space will not work.");

        private static void DoAssetDragAndDropGUI()
        {
            DoSectionHeader(AssetDragAndDrop);

            DragAndDropSubAssets.Modifiers.DoGUI(DragAndDropSubAssetsLabel);
            AdvancedDragAndDrop.Modifiers.DoGUI(AdvancedDragAndDropLabel);
        }

        /************************************************************************************************************************/

        /// <summary>Enables the GUI and draws a space and label.</summary>
        public static void DoSectionHeader(string text)
        {
            GUI.enabled = true;
            EditorGUILayout.Space();
            GUILayout.Label(text, EditorStyles.boldLabel);
        }

        /************************************************************************************************************************/

        /// <summary>Draws a pref which enables the following GUI group and which is Pro-Only.</summary>
        public static void DoProOnlyGroupEnabledPref(InspectorGadgets.AutoPrefs.Bool pref, GUIContent label)
        {
            pref.DoGUI(label);
            GUI.enabled = pref;
        }

        /************************************************************************************************************************/
    }
}

#endif
