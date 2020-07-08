/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor
{
    using Simulator.Editor.MapMeshes;
    using Simulator.Map;
    using UnityEditor;
    using UnityEngine;

    public class MapMeshWindow : EditorWindow
    {
        private static class Styles
        {
            public static readonly GUIStyle TitleLabelStyle = new GUIStyle(GUI.skin.label)
                {alignment = TextAnchor.MiddleCenter, fontSize = 14};
        }

        private const string SettingsKey = "Simulator/MapMeshSettings";
        private const string ParentObjectName = "MapMesh_generated";

        private MapMeshSettings settings;
        private SerializedObject serializedSettings;

        private SerializedProperty createCollider;
        private SerializedProperty createRenderers;
        private SerializedProperty snapLaneEnds;
        private SerializedProperty pushOuterVerts;
        private SerializedProperty pushDistance;
        private SerializedProperty snapThreshold;
        private SerializedProperty roadUvUnit;
        private SerializedProperty lineUvUnit;
        private SerializedProperty lineBump;
        private SerializedProperty lineWidth;

        private SerializedObject SerializedSettings
        {
            get
            {
                if (serializedSettings == null || settings == null)
                    LoadSettings();
                return serializedSettings;
            }
        }

        [MenuItem("Simulator/Build HD Map Mesh", false, 125)]
        private static void ShowWindow()
        {
            var window = GetWindow<MapMeshWindow>();
            window.titleContent = new GUIContent("HD Map Mesh Builder");
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
            settings = CreateInstance<MapMeshSettings>();

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
            settings = CreateInstance<MapMeshSettings>();
            serializedSettings = new SerializedObject(settings);
            FindProperties();
            SaveSettings();
        }

        private void FindProperties()
        {
            createCollider = serializedSettings.FindProperty(nameof(MapMeshSettings.createCollider));
            createRenderers = serializedSettings.FindProperty(nameof(MapMeshSettings.createRenderers));
            snapLaneEnds = serializedSettings.FindProperty(nameof(MapMeshSettings.snapLaneEnds));
            pushOuterVerts = serializedSettings.FindProperty(nameof(MapMeshSettings.pushOuterVerts));
            pushDistance = serializedSettings.FindProperty(nameof(MapMeshSettings.pushDistance));
            snapThreshold = serializedSettings.FindProperty(nameof(MapMeshSettings.snapThreshold));
            roadUvUnit = serializedSettings.FindProperty(nameof(MapMeshSettings.roadUvUnit));
            lineUvUnit = serializedSettings.FindProperty(nameof(MapMeshSettings.lineUvUnit));
            lineBump = serializedSettings.FindProperty(nameof(MapMeshSettings.lineBump));
            lineWidth = serializedSettings.FindProperty(nameof(MapMeshSettings.lineWidth));
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
            DrawHeader();

            var mapAvailable = FindObjectOfType<MapHolder>() != null;
            if (!mapAvailable)
            {
                EditorGUILayout.HelpBox("To build mesh, import HD Map first.", MessageType.Info);
                if (GUILayout.Button("Open Importer"))
                    MapImport.Open();
            }
            else
            {
                DrawSettingsSection();
                EditorGUILayout.Space();
                DrawDefaultSettingsButton();
                EditorGUILayout.Space();
                DrawImportButton();
            }
        }

        private void DrawHeader()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("HD Map Mesh Builder", Styles.TitleLabelStyle);
            GUILayout.Space(5);
            EditorGUILayout.LabelField(string.Empty, GUI.skin.horizontalSlider);
            GUILayout.Space(10);
        }

        private void DrawSettingsSection()
        {
            EditorGUILayout.PropertyField(createCollider);
            EditorGUILayout.PropertyField(createRenderers);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Mesh Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(snapLaneEnds);

            EditorGUI.BeginDisabledGroup(!snapLaneEnds.boolValue);
            EditorGUILayout.PropertyField(snapThreshold);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField(pushOuterVerts);

            EditorGUI.BeginDisabledGroup(!pushOuterVerts.boolValue);
            EditorGUILayout.PropertyField(pushDistance);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Rendering Settings", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(!createRenderers.boolValue);
            {
                EditorGUILayout.PropertyField(roadUvUnit);
                EditorGUILayout.PropertyField(lineUvUnit);
                EditorGUILayout.PropertyField(lineWidth);
                EditorGUILayout.PropertyField(lineBump);
            }
            EditorGUI.EndDisabledGroup();
        }

        private void DrawDefaultSettingsButton()
        {
            if (GUILayout.Button("Reset Settings"))
                LoadDefaultSettings();
        }

        private void DrawImportButton()
        {
            SerializedSettings.ApplyModifiedProperties();

            if (GUILayout.Button("Build Mesh"))
            {
                if (!settings.createCollider && !settings.createRenderers)
                {
                    Debug.LogError("Both renderers and colliders are disabled - nothing to generate.");
                    return;
                }

                var parent = GameObject.Find(ParentObjectName);
                if (parent != null)
                    DestroyImmediate(parent);

                parent = new GameObject(ParentObjectName);
                var materials = AssetDatabase.LoadAssetAtPath<MapMeshMaterials>("Assets/Resources/Editor/HDMapMaterials.asset");

                var builder = new MapMeshBuilder(settings);
                builder.BuildMesh(parent, materials);
            }
        }
    }
}