/**
 * Copyright (c) 2019 LG Electronics, Inc.
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
    public class PointCloudExport : EditorWindow
    {
        [SerializeField] int LidarTemplate = 3;

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

        [MenuItem("Simulator/Export Point Cloud", false, 130)]
        public static void Open()
        {
            var window = GetWindow<PointCloudExport>();
            var data = EditorPrefs.GetString("Simulator/PointCloudExport", JsonUtility.ToJson(window, false));
            JsonUtility.FromJsonOverwrite(data, window);
            window.titleContent = new GUIContent("Point Cloud Export");
            window.Show();
        }

        void OnDisable()
        {
            var data = JsonUtility.ToJson(this, false);
            EditorPrefs.SetString("Simulator/PointCloudExport", data);
        }

        public void OnEnable()
        {
            var template = LidarSensor.Template.Templates.First(t => t.Name == "Lidar32");
            Apply(template);
        }

        void Apply(LidarSensor.Template template)
        {
            LidarLaserCount = template.LaserCount;
            LidarMinDistance = template.MinDistance;
            LidarMaxDistance = template.MaxDistance;
            LidarRotationFrequency = template.RotationFrequency;
            LidarMeasurementsPerRotation = template.MeasurementsPerRotation;
            LidarFieldOfView = template.FieldOfView;
            LidarCenterAngle = template.CenterAngle;
        }

        public void OnGUI()
        {
            var titleLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };
            var subtitleLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10 };

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Export Point Cloud", titleLabelStyle, GUILayout.ExpandWidth(true));
            GUILayout.Space(5);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox("Settings", UnityEditor.MessageType.Info);
            LidarTemplate = EditorGUILayout.Popup(LidarTemplate, LidarSensor.Template.Templates.Select(t => t.Name).ToArray());
            if (LidarTemplate == 0)
            {
                LidarLaserCount = EditorGUILayout.IntSlider(new GUIContent("Laser Count"), LidarLaserCount, 1, 128);
                LidarMinDistance = EditorGUILayout.Slider(new GUIContent("Min Distance"), LidarMinDistance, 0.0f, 10.0f);
                LidarMaxDistance = EditorGUILayout.Slider(new GUIContent("Max Distance"), LidarMaxDistance, LidarMinDistance, 100.0f);
                LidarRotationFrequency = EditorGUILayout.Slider(new GUIContent("Rotation Frequency"), LidarRotationFrequency, 1.0f, 30.0f);
                LidarMeasurementsPerRotation = EditorGUILayout.IntSlider(new GUIContent("Measurements Per Rotation"), LidarMeasurementsPerRotation, 18, 6000);
                LidarFieldOfView = EditorGUILayout.Slider(new GUIContent("Field of View"), LidarFieldOfView, 1.0f, 45.0f);
                LidarCenterAngle = EditorGUILayout.Slider(new GUIContent("Center Angle"), LidarCenterAngle, -45.0f, 45.0f);
            }
            else
            {
                Apply(LidarSensor.Template.Templates[LidarTemplate]);
            }

            if (LidarLaserCount > 32)
            {
                EditorGUILayout.HelpBox("Using lidar with more than 32 laser count will significantly increase generation time", UnityEditor.MessageType.Warning);
            }

            Height = EditorGUILayout.FloatField(new GUIContent("Height", "Height of lidar from ground"), Height);
            Distance = EditorGUILayout.FloatField(new GUIContent("Distance", "Distance between capture positions, higher value increases generation perforamnce"), Distance);
            Ratio = EditorGUILayout.Slider(new GUIContent("Ratio", "Fraction of points to keep, hight vealue increases saved point count"), Ratio, 0.0f, 1.0f);

            EditorGUILayout.HelpBox("Save As...", UnityEditor.MessageType.Info);
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

            if (GUILayout.Button("Generate"))
            {
                Export();
            }
        }

        void Export()
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

            var lanes = mapHolder.trafficLanesHolder.transform.parent.GetComponentsInChildren<MapLane>();
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

        void Generate(MapLane[] lanes)
        {
            var settings = EditorSettings.Load();

            var lidar = Instantiate(settings.LidarSensor);
            lidar.Init();
            lidar.TemplateIndex = LidarTemplate;
            lidar.LaserCount = LidarLaserCount;
            lidar.MinDistance = LidarMinDistance;
            lidar.MaxDistance = LidarMaxDistance;
            lidar.RotationFrequency = LidarRotationFrequency;
            lidar.RotationFrequency = LidarRotationFrequency;
            lidar.MeasurementsPerRotation = LidarMeasurementsPerRotation;
            lidar.RotationFrequency = LidarRotationFrequency;
            lidar.FieldOfView = LidarFieldOfView;
            lidar.CenterAngle = LidarCenterAngle;
            lidar.ApplyTemplate();
            lidar.Reset();

            int mapLayerMask = LayerMask.GetMask("Default");

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
                                        var position = p0 + delta * c + hit.normal * Height;

                                        lidar.transform.position = position;

                                        var points = lidar.Capture();

                                        for (int p = 0; p < points.Length; p++)
                                        {
                                            var point = points[p];
                                            if (point != Vector4.zero && Random.value < Ratio)
                                            {
                                                var pt = point;
                                                writer.Write(new Vector3(pt.x, pt.z, pt.y), point.w);
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
                lidar.OnDestroy();
                DestroyImmediate(lidar.gameObject);
            }
        }
    }
}