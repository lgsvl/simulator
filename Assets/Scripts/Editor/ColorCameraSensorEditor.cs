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
    [CustomEditor(typeof(ColorCameraSensor))]
    class ColorCameraSensorEditor : UnityEditor.Editor
    {
        ColorCameraSensor colorCamera;

        SerializedProperty ColorCameraName;
        SerializedProperty ColorCameraTopic;
        SerializedProperty ColorCameraFrame;

        SerializedProperty Width;
        SerializedProperty Height;
        SerializedProperty Frequency;
        SerializedProperty JpegQuality;
        SerializedProperty FieldOfView;
        SerializedProperty MinDistance;
        SerializedProperty MaxDistance;
        SerializedProperty Distorted;
        SerializedProperty DistortionParameters;

        void OnEnable()
        {
            ColorCameraName = serializedObject.FindProperty(nameof(ColorCameraSensor.Name));
            ColorCameraTopic = serializedObject.FindProperty(nameof(ColorCameraSensor.Topic));
            ColorCameraFrame = serializedObject.FindProperty(nameof(ColorCameraSensor.Frame));

            Width = serializedObject.FindProperty(nameof(ColorCameraSensor.Width));
            Height = serializedObject.FindProperty(nameof(ColorCameraSensor.Height));
            Frequency = serializedObject.FindProperty(nameof(ColorCameraSensor.Frequency));
            JpegQuality = serializedObject.FindProperty(nameof(ColorCameraSensor.JpegQuality));
            FieldOfView = serializedObject.FindProperty(nameof(ColorCameraSensor.FieldOfView));
            MinDistance = serializedObject.FindProperty(nameof(ColorCameraSensor.MinDistance));
            MaxDistance = serializedObject.FindProperty(nameof(ColorCameraSensor.MaxDistance));
            Distorted = serializedObject.FindProperty(nameof(ColorCameraSensor.Distorted));
            DistortionParameters = serializedObject.FindProperty(nameof(ColorCameraSensor.DistortionParameters));
        }

        public override void OnInspectorGUI()
        {
            colorCamera = (ColorCameraSensor)target;

            serializedObject.Update();

            EditorGUILayout.PropertyField(ColorCameraName);
            EditorGUILayout.PropertyField(ColorCameraTopic);
            EditorGUILayout.PropertyField(ColorCameraFrame);

            EditorGUILayout.PropertyField(Width);
            EditorGUILayout.PropertyField(Height);
            EditorGUILayout.PropertyField(Frequency);
            EditorGUILayout.PropertyField(JpegQuality);
            EditorGUILayout.PropertyField(FieldOfView);
            EditorGUILayout.PropertyField(MinDistance);
            EditorGUILayout.PropertyField(MaxDistance);
            EditorGUILayout.PropertyField(Distorted);
            if (colorCamera.Distorted)
            {
                EditorGUILayout.PropertyField(DistortionParameters, true);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
