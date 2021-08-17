/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Linq;
using UnityEngine;
using UnityEditor;
using Simulator.Sensors;
using Simulator.Utilities;
using Simulator.Map;

namespace Simulator.Editor
{
    using System;
    using Components;

    public class PointCloudExport : EditorWindow
    {
        [SerializeField] int TemplateIndex = 3;
        [SerializeField] int GeneratorTypeIndex = 0;

        [SerializeField] int LidarLaserCount;
        [SerializeField] float LidarMinDistance;
        [SerializeField] float LidarMaxDistance;
        [SerializeField] float LidarRotationFrequency;
        [SerializeField] int LidarMeasurementsPerRotation;
        [SerializeField] float LidarFieldOfView;
        [SerializeField] float LidarCenterAngle;

        [SerializeField] float Height = 2.0f;
        [SerializeField] float Distance = 1.0f;
        [SerializeField] float Ratio = 0.1f;
        [SerializeField] string FileName;

        private LidarTemplate currentTemplate;

        private Type[] availableGeneratorTypes;

        [MenuItem("Simulator/Export Point Cloud", false, 130)]
        public static void Open()
        {
            var window = GetWindow<PointCloudExport>();
            var data = EditorPrefs.GetString("Simulator/PointCloudExport", JsonUtility.ToJson(window, false));
            JsonUtility.FromJsonOverwrite(data, window);
            window.titleContent = new GUIContent("Point Cloud Export");
            window.Show();
        }

        private void OnDisable()
        {
            var data = JsonUtility.ToJson(this, false);
            EditorPrefs.SetString("Simulator/PointCloudExport", data);
        }

        public void OnEnable()
        {
            var template = LidarTemplate.Templates.First(t => t.Name == "Lidar32");
            Apply(template);
        }

        private void Apply(LidarTemplate template)
        {
            currentTemplate.LaserCount = template.LaserCount;
            currentTemplate.MinDistance = template.MinDistance;
            currentTemplate.MaxDistance = template.MaxDistance;
            currentTemplate.RotationFrequency = template.RotationFrequency;
            currentTemplate.MeasurementsPerRotation = template.MeasurementsPerRotation;
            currentTemplate.FieldOfView = template.FieldOfView;
            currentTemplate.CenterAngle = template.CenterAngle;
        }

        public void OnGUI()
        {
            var titleLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Export Point Cloud", titleLabelStyle, GUILayout.ExpandWidth(true));
            GUILayout.Space(5);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Space(10);

            EditorGUILayout.LabelField("Generator Class", EditorStyles.boldLabel);
            if (availableGeneratorTypes == null)
                FindPointCloudGenerators();

            if (availableGeneratorTypes.Length > 0)
            {
                EditorGUILayout.Popup(GeneratorTypeIndex, availableGeneratorTypes.Select(x => x.Name).ToArray());
            }
            else
            {
                EditorGUILayout.HelpBox($"There are no types implementing {nameof(IPointCloudGenerator)}.\nClone LidarSensor into Assets/External/Sensors to use this tool.", MessageType.Error);
            }

            if (GUILayout.Button("Refresh"))
            {
                FindPointCloudGenerators();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            TemplateIndex = EditorGUILayout.Popup(TemplateIndex, LidarTemplate.Templates.Select(t => t.Name).ToArray());
            if (TemplateIndex == 0)
            {
                currentTemplate.LaserCount = EditorGUILayout.IntSlider(new GUIContent("Laser Count"), LidarLaserCount, 1, 128);
                currentTemplate.MinDistance = EditorGUILayout.Slider(new GUIContent("Min Distance"), LidarMinDistance, 0.0f, 10.0f);
                currentTemplate.MaxDistance = EditorGUILayout.Slider(new GUIContent("Max Distance"), LidarMaxDistance, LidarMinDistance, 100.0f);
                currentTemplate.RotationFrequency = EditorGUILayout.Slider(new GUIContent("Rotation Frequency"), LidarRotationFrequency, 1.0f, 30.0f);
                currentTemplate.MeasurementsPerRotation = EditorGUILayout.IntSlider(new GUIContent("Measurements Per Rotation"), LidarMeasurementsPerRotation, 18, 6000);
                currentTemplate.FieldOfView = EditorGUILayout.Slider(new GUIContent("Field of View"), LidarFieldOfView, 1.0f, 45.0f);
                currentTemplate.CenterAngle = EditorGUILayout.Slider(new GUIContent("Center Angle"), LidarCenterAngle, -45.0f, 45.0f);
            }
            else
            {
                Apply(LidarTemplate.Templates[TemplateIndex]);
            }

            if (LidarLaserCount > 32)
            {
                EditorGUILayout.HelpBox("Using lidar with more than 32 laser count will significantly increase generation time", UnityEditor.MessageType.Warning);
            }

            Height = EditorGUILayout.FloatField(new GUIContent("Height", "Height of lidar from ground in meters"), Height);
            Distance = EditorGUILayout.FloatField(new GUIContent("Distance", "Distance between capture positions, higher value increases generation perforamnce"), Distance);
            Ratio = EditorGUILayout.Slider(new GUIContent("Ratio", "Fraction of points to keep, higher value increases saved point count"), Ratio, 0.0f, 1.0f);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Save as...", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            FileName = EditorGUILayout.TextField(FileName);
            if (GUILayout.Button("...", GUILayout.ExpandWidth(false)))
            {
                var path = EditorUtility.SaveFilePanel("Save Point Cloud as PCD File", "", "simulator.pcd", "pcd");
                if (!string.IsNullOrEmpty(path))
                {
                    FileName = path;
                }
            }
            GUILayout.EndHorizontal();

            EditorGUI.BeginDisabledGroup(availableGeneratorTypes.Length == 0);
            if (GUILayout.Button("Generate"))
            {
                Export();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void FindPointCloudGenerators()
        {
            GeneratorTypeIndex = 0;
            var type = typeof(IPointCloudGenerator);
            availableGeneratorTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && !p.IsInterface)
                .ToArray();
        }

        private void Export()
        {
            if (string.IsNullOrEmpty(FileName))
            {
                EditorUtility.DisplayDialog("Error", "Please specify output filename!", "OK");
                return;
            }

            var mapHolder = FindObjectOfType<MapHolder>();
            if (mapHolder == null)
            {
                EditorUtility.DisplayDialog("Error", "Missing MapHolder, please add MapHolder component to map object and set holder transforms", "OK");
                return;
            }

            var lanes = mapHolder.trafficLanesHolder.transform.parent.GetComponentsInChildren<MapTrafficLane>();
            if (lanes.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", "No lane annotations found on map!", "OK");
                return;
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            Generate(lanes);
            var elapsed = sw.Elapsed;

            Debug.Log($"Point Cloud generated in {(int)elapsed.TotalMinutes} min {elapsed.Seconds} sec");
        }

        private void Generate(MapTrafficLane[] lanes)
        {
            var sensorName = availableGeneratorTypes[GeneratorTypeIndex].Name;
            var path = $"Assets/External/Sensors/{sensorName}/{sensorName}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var go = Instantiate(prefab);
            var generator = go.GetComponent<IPointCloudGenerator>();
            generator.ApplySettings(currentTemplate);

            int mapLayerMask = LayerMask.GetMask("Default", "Obstacle");

            try
            {
                using (var writer = new PcdWriter(FileName))
                {
                    for (int i = 0; i < lanes.Length; i++)
                    {
                        var lane = lanes[i];

                        float progress = (float)i / (lanes.Length - 1);
                        var size = EditorUtility.FormatBytes(writer.Size);
                        if (EditorUtility.DisplayCancelableProgressBar("Generating Point Cloud...", $"{writer.Count:N0} points ({size})", progress))
                        {
                            return;
                        }

                        var positions = lane.mapLocalPositions;
                        if (positions.Count > 1)
                        {
                            Vector3 p0 = lane.transform.TransformPoint(positions[0]);
                            Vector3 p1;

                            foreach (var next in positions.Skip(1))
                            {
                                p1 = lane.transform.TransformPoint(next);

                                RaycastHit hit;
                                if (Physics.Raycast(p0 + Vector3.up * LidarMaxDistance, Vector2.down, out hit, float.MaxValue, mapLayerMask))
                                {
                                    float length = Vector3.Distance(p0, p1);
                                    int capturesPerSegment = (int)(Mathf.Max(Distance, length) / Distance);

                                    var delta = (p1 - p0) / capturesPerSegment;
                                    for (int c = 0; c < capturesPerSegment; c++)
                                    {
                                        var pos = p0 + delta * c + hit.normal * Height;
                                        var points = generator.GeneratePoints(pos);

                                        for (int p = 0; p < points.Length; p++)
                                        {
                                            var point = points[p];
                                            if (point != Vector4.zero && UnityEngine.Random.Range(0f, 1f) < Ratio)
                                            {
                                                var pt = point;
                                                writer.Write(new Vector3(pt.z, -pt.x, pt.y), point.w);  // Converting to right-handed xyz
                                            }
                                        };
                                    }
                                }

                                p0 = p1;
                            }
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                generator.Cleanup();
                DestroyImmediate(go);
            }
        }
    }
}
