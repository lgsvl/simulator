/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using PointCloud.Trees;
    using Simulator.PointCloud;
    using Simulator.PointCloud.Trees;
    using Simulator.Utilities;
    using Unity.Collections.LowLevel.Unsafe;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public class PointCloudImporterWindow : EditorWindow
    {
        private static class Styles
        {
            static Styles()
            {
                var baseCol = EditorGUIUtility.isProSkin
                    ? new Color(0.22f, 0.22f, 0.22f, 1.0f)
                    : new Color(0.76f, 0.76f, 0.76f, 1.0f);
                var colA = baseCol;
                var colB = baseCol * 1.1f;
                colA.a = colB.a = 1.0f;

                var textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

                var bgTexA = new Texture2D(1, 1);
                bgTexA.SetPixel(0, 0, colA);
                bgTexA.Apply();

                var bgTexB = new Texture2D(1, 1);
                bgTexB.SetPixel(0, 0, colB);
                bgTexB.Apply();

                var padding = new RectOffset(4, 4, 0, 0);

                ListStyleA = new GUIStyle
                {
                    normal = {background = bgTexA, textColor = textColor},
                    padding = padding,
                    clipping = TextClipping.Clip
                };
                ListStyleB = new GUIStyle
                {
                    normal = {background = bgTexB, textColor = textColor},
                    padding = padding,
                    clipping = TextClipping.Clip
                };
            }

            public static readonly GUIStyle TitleLabelStyle = new GUIStyle(GUI.skin.label)
                {alignment = TextAnchor.MiddleCenter, fontSize = 14};

            public static readonly GUIStyle ListStyleA;
            public static readonly GUIStyle ListStyleB;
        }

        private const string SettingsKey = "Simulator/TreeImportSettings";
        private const string AllowedExtensions = "laz,las,pcd,ply";

        private readonly List<string> allowedExtensionsList = new List<string>
        {
            ".laz", ".las", ".pcd", ".ply"
        };

        private readonly List<string> tmpList = new List<string>();

        private TreeImportSettings settings;
        private SerializedObject serializedSettings;

        private Vector2 scrollPos;
        private Vector2 inputFilesScrollPos;

        private SerializedProperty inputFiles;
        private SerializedProperty outputPath;
        private SerializedProperty treeType;
        private SerializedProperty sampling;
        private SerializedProperty rootNodeSubdivision;
        private SerializedProperty nodeBranchThreshold;
        private SerializedProperty maxTreeDepth;
        private SerializedProperty minPointDistance;
        private SerializedProperty generateMesh;
        private SerializedProperty roadOnlyMesh;
        private SerializedProperty meshDetailLevel;
        private SerializedProperty threadCount;
        private SerializedProperty chunkSize;
        private SerializedProperty center;
        private SerializedProperty normalize;
        private SerializedProperty lasRGB8BitWorkaround;
        private SerializedProperty axes;

        private SerializedObject SerializedSettings
        {
            get
            {
                if (serializedSettings == null || settings == null)
                    LoadSettings();
                return serializedSettings;
            }
        }

        [MenuItem("Simulator/Import Point Cloud", false, 140)]
        public static void Open()
        {
            var window = GetWindow<PointCloudImporterWindow>();
            window.titleContent = new GUIContent("Point Cloud Import");
            window.Show();
        }

        public void OnEnable()
        {
            LoadSettings();
            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        private void OnDisable()
        {
            SaveSettings();

            // ReSharper disable once DelegateSubtraction
            Undo.undoRedoPerformed -= UndoRedoPerformed;
        }

        private void UndoRedoPerformed()
        {
            if (serializedSettings == null)
                return;

            SerializedSettings.Update();
            Repaint();
        }

        private void LoadSettings()
        {
            var data = EditorPrefs.GetString(SettingsKey, null);
            settings = CreateInstance<TreeImportSettings>();
            settings.threadCount = SystemInfo.processorCount;

            try
            {
                JsonUtility.FromJsonOverwrite(data, settings);
            }
            catch
            {
                // Deserialization failed, but default values are already present
            }

            serializedSettings = new SerializedObject(settings);
            FindProperties();
        }

        private void LoadDefaultSettings()
        {
            settings = CreateInstance<TreeImportSettings>();
            settings.threadCount = SystemInfo.processorCount;
            serializedSettings = new SerializedObject(settings);
            FindProperties();
            SaveSettings();
        }

        private void FindProperties()
        {
            inputFiles = serializedSettings.FindProperty(nameof(TreeImportSettings.inputFiles));
            outputPath = serializedSettings.FindProperty(nameof(TreeImportSettings.outputPath));
            treeType = serializedSettings.FindProperty(nameof(TreeImportSettings.treeType));
            sampling = serializedSettings.FindProperty(nameof(TreeImportSettings.sampling));
            rootNodeSubdivision = serializedSettings.FindProperty(nameof(TreeImportSettings.rootNodeSubdivision));
            nodeBranchThreshold = serializedSettings.FindProperty(nameof(TreeImportSettings.nodeBranchThreshold));
            maxTreeDepth = serializedSettings.FindProperty(nameof(TreeImportSettings.maxTreeDepth));
            minPointDistance = serializedSettings.FindProperty(nameof(TreeImportSettings.minPointDistance));
            generateMesh = serializedSettings.FindProperty(nameof(TreeImportSettings.generateMesh));
            roadOnlyMesh = serializedSettings.FindProperty(nameof(TreeImportSettings.roadOnlyMesh));
            meshDetailLevel = serializedSettings.FindProperty(nameof(TreeImportSettings.meshDetailLevel));
            threadCount = serializedSettings.FindProperty(nameof(TreeImportSettings.threadCount));
            chunkSize = serializedSettings.FindProperty(nameof(TreeImportSettings.chunkSize));
            center = serializedSettings.FindProperty(nameof(TreeImportSettings.center));
            normalize = serializedSettings.FindProperty(nameof(TreeImportSettings.normalize));
            lasRGB8BitWorkaround = serializedSettings.FindProperty(nameof(TreeImportSettings.lasRGB8BitWorkaround));
            axes = serializedSettings.FindProperty(nameof(TreeImportSettings.axes));
        }

        private void SaveSettings()
        {
            if (serializedSettings == null)
                return;

            SerializedSettings.ApplyModifiedProperties();
            var data = JsonUtility.ToJson(settings);
            EditorPrefs.SetString(SettingsKey, data);
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            {
                DrawHeader();
                DrawSettingsSection();
                EditorGUILayout.Space();
                DrawDefaultSettingsButton();
                EditorGUILayout.Space();
                DrawImportButton();
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Point Cloud Import", Styles.TitleLabelStyle);
            GUILayout.Space(5);
            EditorGUILayout.LabelField(string.Empty, GUI.skin.horizontalSlider);
            GUILayout.Space(10);
        }

        private void DrawSettingsSection()
        {
            DrawInputFilesInspector();

            EditorGUILayout.Space();
            DrawOutputFilesInspector();

            EditorGUILayout.Space();
            treeType.isExpanded = EditorGUILayout.Foldout(treeType.isExpanded, "Tree Settings", EditorStyles.foldoutHeader);
            if (treeType.isExpanded)
            {
                EditorGUILayout.PropertyField(treeType);
                EditorGUILayout.PropertyField(sampling);
                EditorGUILayout.PropertyField(rootNodeSubdivision);
                EditorGUILayout.PropertyField(nodeBranchThreshold);
                EditorGUILayout.PropertyField(maxTreeDepth);
                EditorGUILayout.PropertyField(minPointDistance);
            }
            
            EditorGUILayout.Space();
            generateMesh.isExpanded = EditorGUILayout.Foldout(generateMesh.isExpanded, "Mesh Settings", EditorStyles.foldoutHeader);
            if (generateMesh.isExpanded)
            {
                EditorGUILayout.PropertyField(generateMesh);
                EditorGUI.BeginDisabledGroup(!generateMesh.boolValue);
                {
                    EditorGUILayout.PropertyField(roadOnlyMesh);
                    if (roadOnlyMesh.boolValue)
                        EditorGUILayout.HelpBox("Road detection is not suitable for all data sets. If you experience problems, try again with this option off.", MessageType.Info);
                    
                    EditorGUILayout.PropertyField(meshDetailLevel);
                }
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.Space();
            threadCount.isExpanded = EditorGUILayout.Foldout(threadCount.isExpanded, "Build Settings", EditorStyles.foldoutHeader);
            if (threadCount.isExpanded)
            {
                EditorGUILayout.PropertyField(threadCount);
                EditorGUILayout.PropertyField(chunkSize);
                EditorGUILayout.PropertyField(center);
                EditorGUILayout.PropertyField(normalize);
                EditorGUILayout.PropertyField(lasRGB8BitWorkaround);
                EditorGUILayout.PropertyField(axes);
            }
        }

        private void DrawInputFilesInspector()
        {
            const float addFileButtonWidth = 65;
            const float addFolderButtonWidth = 80;
            const float clearButtonWidth = 50;
            const float removeButtonWidth = 20;
            const float scrollWidth = 15;
            var lineJump = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            var defaultGuiColor = GUI.color;

            //
            // Auto-detect last used directory
            //
            var elementCount = inputFiles.arraySize;
            var startingPath = Application.dataPath;
            if (elementCount > 0)
            {
                var lastElement = inputFiles.GetArrayElementAtIndex(elementCount - 1);
                var lastPath = lastElement.stringValue;
                if (!string.IsNullOrEmpty(lastPath))
                {
                    var dir = Path.GetDirectoryName(lastPath);
                    if (dir != null)
                        lastPath = dir;

                    if (Directory.Exists(lastPath))
                        startingPath = lastPath;
                }
            }

            //
            // Header - label
            //
            var headerRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            headerRect.height = EditorGUIUtility.singleLineHeight;
            var labelWidth = headerRect.width - clearButtonWidth - addFileButtonWidth - addFolderButtonWidth - 15;
            headerRect.width = labelWidth;
            EditorGUI.LabelField(headerRect, "Input Files", EditorStyles.boldLabel);

            //
            // Header - add file button
            //
            headerRect.x += labelWidth + 5;
            headerRect.width = addFileButtonWidth;
            GUI.color = Color.green;
            if (GUI.Button(headerRect, "Add File"))
            {
                var newFile = EditorUtility.OpenFilePanel("Select file", startingPath, AllowedExtensions);
                if (!string.IsNullOrEmpty(newFile))
                {
                    newFile = Utility.GetRelativePathIfApplies(newFile);
                    CacheArrayProperty(inputFiles, tmpList);
                    if (!tmpList.Contains(newFile))
                    {
                        var index = inputFiles.arraySize;
                        inputFiles.InsertArrayElementAtIndex(index);
                        inputFiles.GetArrayElementAtIndex(index).stringValue = newFile;
                    }
                }
            }

            //
            // Header - add directory button
            //
            headerRect.x += addFileButtonWidth + 5;
            headerRect.width = addFolderButtonWidth;
            if (GUI.Button(headerRect, "Add Folder"))
            {
                var newFolder = EditorUtility.OpenFolderPanel("Select directory", startingPath, string.Empty);
                if (!string.IsNullOrEmpty(newFolder))
                {
                    var matchingFiles = Directory
                                        .EnumerateFiles(newFolder, "*.*", SearchOption.TopDirectoryOnly)
                                        .Where(
                                            x => Path.GetExtension(x) != null &&
                                                 allowedExtensionsList.Contains(Path.GetExtension(x).ToLowerInvariant()))
                                        .ToList();

                    if (matchingFiles.Count > 0)
                    {
                        CacheArrayProperty(inputFiles, tmpList);

                        var index = inputFiles.arraySize;
                        foreach (var file in matchingFiles.Where(file => !tmpList.Contains(file)))
                        {
                            var relativePath = Utility.GetRelativePathIfApplies(file);
                            inputFiles.InsertArrayElementAtIndex(index);
                            inputFiles.GetArrayElementAtIndex(index).stringValue = relativePath;
                            index++;
                        }
                    }
                }
            }

            //
            // Header - clear files button
            //
            headerRect.x += addFolderButtonWidth + 5;
            headerRect.width = clearButtonWidth;
            GUI.color = Color.red;
            if (GUI.Button(headerRect, "Clear"))
                inputFiles.ClearArray();
            GUI.color = defaultGuiColor;

            //
            // View for currently selected files
            //
            var rect = CreateBox(4);

            // Refresh element count, to include additions/removals
            elementCount = inputFiles.arraySize;
            if (elementCount == 0)
            {
                var labelRect = rect;
                labelRect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.LabelField(labelRect, "(No files selected)");
            }
            else
            {
                var elementsHeight = elementCount * lineJump + EditorGUIUtility.standardVerticalSpacing;
                var scrollVisible = elementsHeight > rect.height;
                var interiorWidth = scrollVisible ? rect.width - scrollWidth : rect.width;
                inputFilesScrollPos = GUI.BeginScrollView(
                    rect,
                    inputFilesScrollPos,
                    new Rect(0, 0, interiorWidth, elementsHeight));
                {
                    var elementRect = new Rect(
                        0,
                        0,
                        interiorWidth - removeButtonWidth - 5,
                        EditorGUIUtility.singleLineHeight);
                    var removeRect = elementRect;
                    removeRect.x = interiorWidth - removeButtonWidth;
                    removeRect.width = removeButtonWidth;
                    for (var i = 0; i < elementCount; ++i)
                    {
                        var element = inputFiles.GetArrayElementAtIndex(i);
                        var currentPath = element.stringValue;
                        var pathValid = !string.IsNullOrEmpty(currentPath) && File.Exists(Utility.GetFullPath(currentPath));

                        if (!pathValid)
                            GUI.color = Color.red;

                        EditorGUI.LabelField(
                            elementRect,
                            currentPath,
                            i % 2 == 0 ? Styles.ListStyleA : Styles.ListStyleB);

                        GUI.color = Color.red;
                        if (GUI.Button(removeRect, "X"))
                        {
                            inputFiles.DeleteArrayElementAtIndex(i--);
                            elementCount--;
                        }
                        else
                        {
                            elementRect.y += lineJump;
                            removeRect.y += lineJump;
                        }

                        GUI.color = defaultGuiColor;
                    }
                }
                GUI.EndScrollView();
            }
        }

        private void DrawOutputFilesInspector()
        {
            EditorGUILayout.LabelField("Output Files", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(outputPath);
            if (WillOverwrite())
            {
                EditorGUILayout.HelpBox(
                    "Tree data is present in selected directory. It will be overwritten.",
                    MessageType.Warning);
            }

            var path = settings.outputPath;
            if (path.StartsWith(".../") && !path.Contains('~'))
            {
                EditorGUILayout.HelpBox(
                    "Unity will generate .meta files in this directory, which may take a long time. " +
                    "Consider using folder name ending with `~` to avoid this.",
                    MessageType.Warning);
            }
        }

        private bool WillOverwrite()
        {
            var fullPath = Utility.GetFullPath(settings.outputPath);

            return !string.IsNullOrEmpty(fullPath) &&
                   File.Exists(Path.Combine(fullPath, "index" + TreeUtility.IndexFileExtension));
        }

        private void CacheArrayProperty(SerializedProperty serializedProperty, List<string> outputList)
        {
            outputList.Clear();
            var itemCount = serializedProperty.arraySize;
            for (var i = 0; i < itemCount; ++i)
                outputList.Add(serializedProperty.GetArrayElementAtIndex(i).stringValue);
        }

        private void DrawDefaultSettingsButton()
        {
            if (GUILayout.Button("Reset Settings"))
                LoadDefaultSettings();
        }

        private void DrawImportButton()
        {
            SerializedSettings.ApplyModifiedProperties();

            var valid = VerifySettings(out var message);

            EditorGUI.BeginDisabledGroup(!valid);
            {
                if (GUILayout.Button("Import"))
                {
                    if (WillOverwrite())
                    {
                        if (EditorUtility.DisplayDialog(
                            "Overwrite files?",
                            "This operation will overwrite all tree files in selected output folder. Continue?",
                            "Yes",
                            "No"))
                        {
                            if (NodeTreeBuilder.BuildNodeTree(settings))
                                ShowAutoAddPopup();
                        }
                    }
                    else
                    {
                        if (NodeTreeBuilder.BuildNodeTree(settings))
                            ShowAutoAddPopup();
                    }
                }
            }
            EditorGUI.EndDisabledGroup();

            if (!string.IsNullOrEmpty(message))
                EditorGUILayout.HelpBox(message, MessageType.Warning);
        }

        private void ShowAutoAddPopup()
        {
            var scene = SceneManager.GetActiveScene();

            // Never add renderer to loader scene
            if (scene.name == "LoaderScene")
                return;

            var activeLoader = FindObjectOfType<NodeTreeLoader>();
            if (activeLoader == null)
            {
                var agreed = EditorUtility.DisplayDialog(
                    "Success",
                    "Import succeeded.\n" +
                    "Do you want to add renderer to currently open scene?",
                    "Yes",
                    "No");

                if (agreed)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PointCloudRenderer.prefab");
                    var instance = Instantiate(prefab);
                    instance.name = "PointCloudRenderer";
                    var loader = instance.GetComponent<NodeTreeLoader>();
                    loader.UpdateData(outputPath.stringValue);

                    Undo.RegisterCreatedObjectUndo(instance, "Add point cloud renderer");
                }
            }
            else
            {
                var agreed = EditorUtility.DisplayDialog(
                    "Success",
                    "Import succeeded.\n" +
                    "Point cloud renderer is already present on the scene." +
                    "Do you want to replace its data with newly imported point cloud?",
                    "Yes",
                    "No");

                if (agreed)
                {
                    Undo.RecordObject(activeLoader, "Update point cloud renderer data");
                    activeLoader.UpdateData(outputPath.stringValue);
                }
            }
        }

        private bool VerifySettings(out string message)
        {
            if (string.IsNullOrEmpty(settings.outputPath) || !Directory.Exists(Utility.GetFullPath(settings.outputPath)))
            {
                message = "Target directory does not exist.";
                return false;
            }

            if (settings.inputFiles.Count == 0)
            {
                message = "At least one input file is required.";
                return false;
            }

            foreach (var file in settings.inputFiles)
            {
                if (!File.Exists(Utility.GetFullPath(file)))
                {
                    message = "Invalid input files.";
                    return false;
                }
            }

            var systemMemoryMb = SystemInfo.systemMemorySize;
            var nodeCount = treeType.enumValueIndex == (int)TreeType.Octree ? 8 : 4;
            var itemSize = UnsafeUtility.SizeOf<PointCloudPoint>();
            var estimatedMemoryUsageMb =
                (nodeCount + 1) * settings.threadCount * (long)settings.chunkSize * itemSize / 1000000 + 2000;
            const int reserveMb = 6000; /* Assume 6GB is needed for system and other processes */

            if (estimatedMemoryUsageMb > systemMemoryMb)
            {
                message = $"Insufficient system memory for current settings.\n({estimatedMemoryUsageMb.ToString()}/{systemMemoryMb.ToString()} MB)";
                return false;
            }

            if (estimatedMemoryUsageMb + reserveMb > systemMemoryMb)
            {
                message = $"High estimated memory usage with current settings.\n({estimatedMemoryUsageMb.ToString()}/{systemMemoryMb.ToString()} MB)";
                return true;
            }

            if (settings.chunkSize < settings.nodeBranchThreshold)
            {
                message = "Chunk size cannot be smaller than node branch threshold.";
                return true;
            }

            message = string.Empty;
            return true;
        }

        private Rect CreateBox(int lineCount)
        {
            var rect = EditorGUILayout.GetControlRect(
                false,
                lineCount * EditorGUIUtility.singleLineHeight +
                (lineCount + 3) * EditorGUIUtility.standardVerticalSpacing);
            rect = EditorGUI.IndentedRect(rect);

            rect.height -= 2 * EditorGUIUtility.standardVerticalSpacing;
            rect.y += EditorGUIUtility.standardVerticalSpacing;
            GUI.Box(rect, GUIContent.none);
            rect.width -= 8;
            rect.x += 4;
            rect.y += EditorGUIUtility.standardVerticalSpacing;

            return rect;
        }
    }
}
