/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Linq;
using UnityEditor;
using Simulator.Sensors;

namespace Simulator.Editor
{
    [CustomEditor(typeof(LidarSensor))]
    class LidarSensorEditor : UnityEditor.Editor
    {
        SerializedProperty LidarSensorName;
        SerializedProperty LidarSensorTopic;
        SerializedProperty LidarSensorFrame;

        SerializedProperty VerticalRayAngles;
        SerializedProperty LaserCount;
        SerializedProperty FieldOfView;
        SerializedProperty CenterAngle;
        SerializedProperty MinDistance;
        SerializedProperty MaxDistance;
        SerializedProperty RotationFrequency;
        SerializedProperty MeasurementsPerRotation;
        SerializedProperty Compensated;
        SerializedProperty Top;
        SerializedProperty PointSize;
        SerializedProperty PointColor;

        void OnEnable()
        {
            LidarSensorName = serializedObject.FindProperty(nameof(LidarSensor.Name));
            LidarSensorTopic = serializedObject.FindProperty(nameof(LidarSensor.Topic));
            LidarSensorFrame = serializedObject.FindProperty(nameof(LidarSensor.Frame));

            VerticalRayAngles = serializedObject.FindProperty(nameof(LidarSensor.VerticalRayAngles));
            LaserCount = serializedObject.FindProperty(nameof(LidarSensor.LaserCount));
            FieldOfView = serializedObject.FindProperty(nameof(LidarSensor.FieldOfView));
            CenterAngle = serializedObject.FindProperty(nameof(LidarSensor.CenterAngle));
            MinDistance = serializedObject.FindProperty(nameof(LidarSensor.MinDistance));
            MaxDistance = serializedObject.FindProperty(nameof(LidarSensor.MaxDistance));
            RotationFrequency = serializedObject.FindProperty(nameof(LidarSensor.RotationFrequency));
            MeasurementsPerRotation = serializedObject.FindProperty(nameof(LidarSensor.MeasurementsPerRotation));
            Compensated = serializedObject.FindProperty(nameof(LidarSensor.Compensated));
            Top = serializedObject.FindProperty(nameof(LidarSensor.Top));
            PointSize = serializedObject.FindProperty(nameof(LidarSensor.PointSize));
            PointColor = serializedObject.FindProperty(nameof(LidarSensor.PointColor));
        }

        public override void OnInspectorGUI()
        {
            var lidar = (LidarSensor)target;

            serializedObject.Update();

            var choices = LidarSensor.Template.Templates.Select(x => x.Name).ToArray();

            var prevIndex = lidar.TemplateIndex;
            lidar.TemplateIndex = EditorGUILayout.Popup("Lidar Template: ", lidar.TemplateIndex, choices);
            if (lidar.TemplateIndex != prevIndex && lidar.TemplateIndex != 0)
            {
                lidar.ApplyTemplate();
                EditorUtility.SetDirty(lidar);
            }

            EditorGUILayout.PropertyField(LidarSensorName);
            EditorGUILayout.PropertyField(LidarSensorTopic);
            EditorGUILayout.PropertyField(LidarSensorFrame);

            EditorGUILayout.PropertyField(VerticalRayAngles, true);
            if (lidar.VerticalRayAngles.Count == 0)
            {
                EditorGUILayout.PropertyField(LaserCount);
                EditorGUILayout.PropertyField(FieldOfView);
                EditorGUILayout.PropertyField(CenterAngle);
            }
            else
            {
                // If VerticalRayAngles.Count is set, make LaserCount read-only.
                // The value of LaserCount will be set in LidarSensor.Reset().
                // FieldOfView and CenterAngle are hidden since they are no longer used.
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(LaserCount);
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.PropertyField(MinDistance);
            EditorGUILayout.PropertyField(MaxDistance);
            EditorGUILayout.PropertyField(RotationFrequency);
            EditorGUILayout.PropertyField(MeasurementsPerRotation);
            EditorGUILayout.PropertyField(Compensated);
            EditorGUILayout.PropertyField(Top);
            EditorGUILayout.PropertyField(PointSize);
            EditorGUILayout.PropertyField(PointColor);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
