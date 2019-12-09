namespace Simulator.Editor
{
    using PointCloud.Trees;
    using Simulator.PointCloud;
    using Unity.Collections.LowLevel.Unsafe;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Windows;

    public class PointCloudConverterWindow : EditorWindow
    {
        private static class Styles
        {
            public static readonly GUIStyle TitleLabelStyle = new GUIStyle(GUI.skin.label)
                {alignment = TextAnchor.MiddleCenter, fontSize = 14};
        }

        private const string SettingsKey = "Simulator/TreeImportSettings";

        private TreeImportSettings settings;
        private SerializedObject serializedSettings;

        private Vector2 scrollPos;
        
        [MenuItem("Simulator/Convert Point Cloud", false, 140)]
        public static void Open()
        {
            var window = GetWindow<PointCloudConverterWindow>();
            window.titleContent = new GUIContent("Point Cloud Converter");
            window.Show();
        }

        public void OnEnable()
        {
            LoadSettings();
            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        void OnDisable()
        {
            SaveSettings();
            Undo.undoRedoPerformed -= UndoRedoPerformed;
        }

        private void UndoRedoPerformed()
        {
            serializedSettings?.Update();
            Repaint();
        }

        private void LoadSettings()
        {
            var data = EditorPrefs.GetString(SettingsKey, null);
            settings = CreateInstance<TreeImportSettings>();
            
            try
            {
                JsonUtility.FromJsonOverwrite(data, settings);
            }
            catch
            {
                // Deserialization failed, but default values are already present
            }

            serializedSettings = new SerializedObject(settings);
        }

        private void LoadDefaultSettings()
        {
            settings = CreateInstance<TreeImportSettings>();
            serializedSettings = new SerializedObject(settings);
            SaveSettings();
        }

        private void SaveSettings()
        {
            serializedSettings.ApplyModifiedProperties();
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
            EditorGUILayout.LabelField("Convert Point Cloud", Styles.TitleLabelStyle);
            GUILayout.Space(5);
            EditorGUILayout.LabelField(string.Empty, GUI.skin.horizontalSlider);
            GUILayout.Space(10);
        }

        private void DrawSettingsSection()
        {
            var iterator = serializedSettings.GetIterator();
            var enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                // Settings class type doesn't have to be displayed
                if (iterator.name == "m_Script")
                    continue;
                
                enterChildren = false;
                EditorGUILayout.PropertyField(iterator, true);
            }
        }

        private void DrawDefaultSettingsButton()
        {
            if (GUILayout.Button("Reset Settings"))
                LoadDefaultSettings();
        }
        
        private void DrawImportButton()
        {
            serializedSettings.ApplyModifiedProperties();

            var valid = VerifySettings(out var message);
            
            EditorGUI.BeginDisabledGroup(!valid);
            {
                if (GUILayout.Button("Convert"))
                    NodeTreeBuilder.BuildNodeTree(settings);
            }
            EditorGUI.EndDisabledGroup();
            
            if (!string.IsNullOrEmpty(message))
                EditorGUILayout.HelpBox(message, MessageType.Warning);
        }

        private bool VerifySettings(out string message)
        {
            if (string.IsNullOrEmpty(settings.outputPath) || !Directory.Exists(settings.outputPath))
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
                if (!File.Exists(file))
                {
                    message = "Invalid input files.";
                    return false;
                }
            }

            var systemMemoryMb = SystemInfo.systemMemorySize;
            // [v] this is valid for octree only, change if other data structures are added 
            const int nodeCount = 8;
            var itemSize = UnsafeUtility.SizeOf<PointCloudPoint>();
            var estimatedMemoryUsageMb =
                (nodeCount + 1) * settings.threadCount * (long) settings.chunkSize * itemSize / 1000000 + 2000;
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

            message = string.Empty;
            return true;
        }
    }
}