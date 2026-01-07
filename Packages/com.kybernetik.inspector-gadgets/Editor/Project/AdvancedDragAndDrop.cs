// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace InspectorGadgets.Editor
{
    /// <summary>[Editor-Only] [Pro-Only]
    /// Shows a context menu with more advanced options when dropping things into the Project Window.
    /// </summary>
    public class AdvancedDragAndDrop : EditorWindow
    {
        /************************************************************************************************************************/

        internal static readonly ModifierKeysPref
            Modifiers = new ModifierKeysPref(
                EditorStrings.PrefsKeyPrefix + nameof(AdvancedDragAndDrop) + "." + nameof(Modifiers),
                ModifierKey.Ctrl | ModifierKey.Alt);

        /************************************************************************************************************************/
        #region GUI
        /************************************************************************************************************************/

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
        }

        /************************************************************************************************************************/

        /// <summary>Called for each asset shown in the Project window.</summary>
        private static void OnProjectWindowItemGUI(string guid, Rect area)
        {
            var currentEvent = Event.current;
            if (!Modifiers.AreKeysDown(currentEvent) ||
                !area.Contains(currentEvent.mousePosition))
                return;

            bool execute;
            switch (currentEvent.type)
            {
                case EventType.DragUpdated:
                    execute = false;
                    break;

                case EventType.DragPerform:
                    execute = true;
                    break;

                case EventType.Repaint:
                    if (DragAndDrop.paths.Length > 0)
                        EditorGUI.DrawRect(area, new Color(0.5f, 0.5f, 0.5f, 0.25f));
                    return;

                default:// Ignore all other event types.
                    return;
            }

            if (!execute)
            {
                if (DragAndDrop.paths.Length > 0)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                    currentEvent.Use();
                }
            }
            else
            {
                var sourcePaths = DragAndDrop.paths;
                if (sourcePaths.Length > 0)
                {
                    var destinationDirectory = AssetDatabase.GUIDToAssetPath(guid);
                    if (File.Exists(destinationDirectory))
                        destinationDirectory = Path.GetDirectoryName(destinationDirectory);

                    OpenWindow(sourcePaths, destinationDirectory);
                    currentEvent.Use();
                }
            }
        }

        /************************************************************************************************************************/

        public static void OpenWindow(string[] sourcePaths, string destinationDirectory)
        {
            var window = GetWindow<AdvancedDragAndDrop>(true);
            window.SetPaths(sourcePaths, destinationDirectory);
        }

        /************************************************************************************************************************/

        private const string
            PrefsPrefix = nameof(AdvancedDragAndDrop) + ".",
            MetadataExtension = ".meta";

        private static readonly AutoPrefs.EditorBool
            Copy = new AutoPrefs.EditorBool(PrefsPrefix + nameof(Copy)),
            Overwrite = new AutoPrefs.EditorBool(PrefsPrefix + nameof(Overwrite)),
            SkipMetadata = new AutoPrefs.EditorBool(PrefsPrefix + nameof(SkipMetadata)),
            ClearDestinationFolder = new AutoPrefs.EditorBool(PrefsPrefix + nameof(ClearDestinationFolder));

        private static readonly GUIContent[]
            MoveCopy =
            {
                new GUIContent("Move",
                    "Remove each file from the source and put at the destination instead."),
                new GUIContent("Copy",
                    "Create a copy of each file at the destination and leave the source files as they were."),
            },
            CreateOverwrite =
            {
                new GUIContent("Create",
                    "If the destination file already exists, ensure the new file has a unique name."),
                new GUIContent("Overwrite",
                    "If the destination file already exists, replace it with the new one."),
            };
        private static readonly GUIContent
            SkipMetadataLabel = new GUIContent("Skip Metadata",
                "Should '.meta' files be copied? Otherwise they will be ignored."),
            ClearDestinationFolderLabel = new GUIContent("Clear Destination Folder",
                "When copying a folder, should the destination folder be fully cleared first?"),
            CancelLabel = new GUIContent("Cancel", "Esc"),
            ExecuteLabel = new GUIContent("Execute", "Enter");

        [SerializeField] private Vector2 _Scroll;
        [SerializeField] private string[] _SourcePaths;
        [SerializeField] private string[] _DestinationPaths;

        private int PathCount
            => Math.Min(_SourcePaths.Length, _DestinationPaths.Length);

        /************************************************************************************************************************/

        private void SetPaths(string[] sourcePaths, string destinationDirectory)
        {
            var count = sourcePaths.Length;
            _SourcePaths = sourcePaths;
            _DestinationPaths = new string[count];

            for (int i = 0; i < count; i++)
            {
                var sourcePath = sourcePaths[i];
                var isDirectory = Directory.Exists(sourcePath);
                var sourceFileName = Path.GetFileName(sourcePath);
                var destinationPath = Path.Combine(destinationDirectory, sourceFileName);

                _DestinationPaths[i] = destinationPath.Replace('\\', '/');
            }
        }

        /************************************************************************************************************************/

        protected virtual void OnEnable()
        {
            titleContent = new GUIContent("Advanced Drag and Drop");
        }

        /************************************************************************************************************************/

        protected virtual void OnGUI()
        {
            DoFileListGUI();
            DoFooterGUI();
        }

        /************************************************************************************************************************/

        private void DoFileListGUI()
        {
            _Scroll = GUILayout.BeginScrollView(_Scroll);

            var count = PathCount;

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();

            GUILayout.Label("Source");

            for (int i = 0; i < count; i++)
                _SourcePaths[i] = EditorGUILayout.TextField(_SourcePaths[i]);

            GUILayout.EndVertical();
            GUILayout.BeginVertical();

            GUILayout.Label("Destination");

            for (int i = 0; i < count; i++)
            {
                if (Overwrite)
                {
                    _DestinationPaths[i] = EditorGUILayout.TextField(_DestinationPaths[i]);
                }
                else
                {
                    EditorGUI.BeginChangeCheck();

                    var destination = _DestinationPaths[i];
                    destination = AssetDatabase.GenerateUniqueAssetPath(destination);
                    destination = EditorGUILayout.TextField(destination);

                    if (EditorGUI.EndChangeCheck())
                        _DestinationPaths[i] = destination;
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.EndScrollView();
        }

        /************************************************************************************************************************/

        private static float _BlockWidth;

        private void DoFooterGUI()
        {
            if (_BlockWidth == 0)
            {
                var toggle = GUI.skin.toggle;
                _BlockWidth = toggle.CalculateWidth(ClearDestinationFolderLabel);
                _BlockWidth += toggle.margin.horizontal;

                var minSize = this.minSize;
                minSize.x = _BlockWidth;
                this.minSize = minSize;
            }

            var width = position.width;
            if (width < _BlockWidth * 2)
            {
                DoMoveCopyGUI();
                DoCreateOverwriteGUI();
                DoSkipMetadataGUI();
                DoDeleteDestinationFolderGUI();
                DoCancelExecuteGUI();
            }
            else if (width < _BlockWidth * 4)
            {
                GUILayout.BeginHorizontal();
                {
                    DoMoveCopyGUI();
                    DoCreateOverwriteGUI();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    DoSkipMetadataGUI();
                    DoDeleteDestinationFolderGUI();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    DoCancelExecuteGUI();
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.BeginHorizontal();
                {
                    DoMoveCopyGUI();
                    DoCreateOverwriteGUI();
                    DoSkipMetadataGUI();
                    DoDeleteDestinationFolderGUI();
                    DoCancelExecuteGUI();
                }
                GUILayout.EndHorizontal();
            }
        }

        /************************************************************************************************************************/

        private void DoMoveCopyGUI()
            => DoPrefToolbarGUI(Copy, MoveCopy);

        private void DoCreateOverwriteGUI()
            => DoPrefToolbarGUI(Overwrite, CreateOverwrite);

        private void DoPrefToolbarGUI(AutoPrefs.EditorBool pref, GUIContent[] labels)
        {
            EditorGUI.BeginChangeCheck();

            var selected = pref ? 1 : 0;
            selected = GUILayout.Toolbar(selected, labels);

            if (EditorGUI.EndChangeCheck())
                pref.Value = selected != 0;
        }

        /************************************************************************************************************************/

        private void DoSkipMetadataGUI()
        {
            var enabled = GUI.enabled;
            GUI.enabled = Overwrite;
            SkipMetadata.Value = GUILayout.Toggle(SkipMetadata, SkipMetadataLabel);
            GUI.enabled = enabled;
        }

        /************************************************************************************************************************/

        private void DoDeleteDestinationFolderGUI()
        {
            var enabled = GUI.enabled;
            GUI.enabled = Overwrite;
            ClearDestinationFolder.Value = GUILayout.Toggle(ClearDestinationFolder, ClearDestinationFolderLabel);
            GUI.enabled = enabled;
        }

        /************************************************************************************************************************/

        private void DoCancelExecuteGUI()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(CancelLabel))
                Close();
            if (GUILayout.Button(ExecuteLabel) || TryUseEnter())
                Execute();
            GUILayout.EndHorizontal();
        }

        /************************************************************************************************************************/

        private bool TryUseEnter()
        {
            var currentEvent = Event.current;
            if (currentEvent.type != EventType.KeyUp)
                return false;

            switch (currentEvent.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    currentEvent.Use();
                    return true;
            }

            return false;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Execution
        /************************************************************************************************************************/

        private void Execute()
        {
            AssetDatabase.SaveAssets();

            try
            {
                AssetDatabase.StartAssetEditing();

                var count = PathCount;
                for (int i = 0; i < count; i++)
                {
                    var source = _SourcePaths[i];
                    var destination = _DestinationPaths[i];

                    if (string.IsNullOrWhiteSpace(source) ||
                        string.IsNullOrWhiteSpace(destination))
                        continue;

                    if (!Overwrite)
                        destination = AssetDatabase.GenerateUniqueAssetPath(destination);

                    if (Directory.Exists(source))
                    {
                        if (ClearDestinationFolder)
                            Directory.Delete(destination, true);

                        CopyFilesRecursively(source, destination, Copy, Overwrite, SkipMetadata);
                    }
                    else
                    {
                        if (Copy)
                        {
                            File.Copy(source, destination, Overwrite);
                            if (!SkipMetadata && Overwrite)
                            {
                                var metadataSourcePath = source + MetadataExtension;
                                if (File.Exists(metadataSourcePath))
                                    File.Copy(metadataSourcePath, destination + MetadataExtension, true);
                            }
                        }
                        else
                        {
                            MoveFile(source, destination, Overwrite);
                            if (!SkipMetadata && Overwrite)
                                MoveFile(source + MetadataExtension, destination + MetadataExtension, true);
                        }
                    }

                    AssetDatabase.ImportAsset(destination);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();

                Close();
            }
        }

        /************************************************************************************************************************/

        private static void CopyFilesRecursively(
            string sourceDirectory, string destinationDirectory, bool copy, bool overwrite, bool skipMeta)
        {
            TryCreateDirectory(destinationDirectory);

            foreach (var directory in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                var newDirectory = ChangePathRoot(directory, sourceDirectory, destinationDirectory);
                TryCreateDirectory(newDirectory);
            }

            foreach (var file in Directory.GetFiles(sourceDirectory, "*.*", SearchOption.AllDirectories))
            {
                if (skipMeta && file.EndsWith(MetadataExtension, true, CultureInfo.CurrentCulture))
                    continue;

                var newFile = ChangePathRoot(file, sourceDirectory, destinationDirectory);
                if (copy)
                    File.Copy(file, newFile, overwrite);
                else
                    MoveFile(file, newFile, overwrite);
            }

            if (!copy)
            {
                Directory.Delete(sourceDirectory, true);
                File.Delete(sourceDirectory + MetadataExtension);
            }
        }

        /************************************************************************************************************************/

        private static string ChangePathRoot(string path, string sourceRoot, string destinationRoot)
        {
            var rootLength = sourceRoot.Length + 1;
            path = path.Substring(rootLength, path.Length - rootLength);
            path = Path.Combine(destinationRoot, path);
            return path;
        }

        /************************************************************************************************************************/

        private static DirectoryInfo TryCreateDirectory(string path)
        {
            if (Directory.Exists(path))
                return default;

            return Directory.CreateDirectory(path);
        }

        /************************************************************************************************************************/

        private static void MoveFile(string source, string destination, bool overwrite)
        {
            if (!File.Exists(source))
                return;

            if (overwrite)
                File.Delete(destination);

            File.Move(source, destination);
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

