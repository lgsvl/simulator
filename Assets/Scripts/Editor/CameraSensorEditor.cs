/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEditor;
using Simulator.Sensors;
using System;

namespace Simulator.Editor
{
    [CustomEditor(typeof(CameraSensorBase), true)]
    class CameraSensorEditor : UnityEditor.Editor
    {
        CameraSensorBase camera;

        SerializedProperty CameraName;
        SerializedProperty CameraTopic;
        SerializedProperty CameraFrame;

        SerializedProperty Width;
        SerializedProperty Height;
        SerializedProperty Frequency;
        SerializedProperty JpegQuality;
        SerializedProperty FieldOfView;
        SerializedProperty MinDistance;
        SerializedProperty MaxDistance;
        SerializedProperty Distorted;
        SerializedProperty DistortionParameters;
        SerializedProperty Fisheye;
        SerializedProperty Xi;
        SerializedProperty CubemapSize;
        SerializedProperty InstanceSegmentationTags;
        string[] CubemapSizeOptions = new string[] { "512", "1024", "2048" };
        int CubemapSizeIndex = 1;

        void OnEnable()
        {
            CameraName = serializedObject.FindProperty(nameof(CameraSensorBase.Name));
            CameraTopic = serializedObject.FindProperty(nameof(CameraSensorBase.Topic));
            CameraFrame = serializedObject.FindProperty(nameof(CameraSensorBase.Frame));

            Width = serializedObject.FindProperty(nameof(CameraSensorBase.Width));
            Height = serializedObject.FindProperty(nameof(CameraSensorBase.Height));
            Frequency = serializedObject.FindProperty(nameof(CameraSensorBase.Frequency));
            JpegQuality = serializedObject.FindProperty(nameof(CameraSensorBase.JpegQuality));
            FieldOfView = serializedObject.FindProperty(nameof(CameraSensorBase.FieldOfView));
            MinDistance = serializedObject.FindProperty(nameof(CameraSensorBase.MinDistance));
            MaxDistance = serializedObject.FindProperty(nameof(CameraSensorBase.MaxDistance));
            Distorted = serializedObject.FindProperty(nameof(CameraSensorBase.Distorted));
            DistortionParameters = serializedObject.FindProperty(nameof(CameraSensorBase.DistortionParameters));
            Fisheye = serializedObject.FindProperty(nameof(CameraSensorBase.Fisheye));
            Xi = serializedObject.FindProperty(nameof(CameraSensorBase.Xi));
            CubemapSize = serializedObject.FindProperty(nameof(CameraSensorBase.CubemapSize));
            InstanceSegmentationTags = serializedObject.FindProperty(nameof(SegmentationCameraSensor.InstanceSegmentationTags));
        }

        public override void OnInspectorGUI()
        {
            camera = (CameraSensorBase)target;

            serializedObject.Update();

            EditorGUILayout.PropertyField(CameraName);
            EditorGUILayout.PropertyField(CameraTopic);
            EditorGUILayout.PropertyField(CameraFrame);

            EditorGUILayout.PropertyField(Width);
            EditorGUILayout.PropertyField(Height);
            EditorGUILayout.PropertyField(Frequency);
            EditorGUILayout.PropertyField(JpegQuality);
            EditorGUILayout.PropertyField(FieldOfView);
            EditorGUILayout.PropertyField(MinDistance);
            EditorGUILayout.PropertyField(MaxDistance);
            switch (camera.CubemapSize)
            {
                case 512:
                    {
                        CubemapSizeIndex = 0;
                        break;
                    }
                case 1024:
                    {
                        CubemapSizeIndex = 1;
                        break;
                    }
                case 2048:
                    {
                        CubemapSizeIndex = 2;
                        break;
                    }
                default:
                    {
                        throw new Exception("Unsupported Cubemap Size: " + camera.CubemapSize);
                    }
            }
            CubemapSizeIndex = EditorGUILayout.Popup("Cubemap Size:", CubemapSizeIndex, CubemapSizeOptions);
            Int32.TryParse(CubemapSizeOptions[CubemapSizeIndex], out camera.CubemapSize);
            EditorGUILayout.PropertyField(Distorted);
            if (camera.Distorted)
            {
                EditorGUILayout.PropertyField(DistortionParameters, true);
                EditorGUILayout.PropertyField(Fisheye);
                if (camera.Fisheye)
                {
                    EditorGUILayout.PropertyField(Xi);
                }
            }

            if (InstanceSegmentationTags != null)
            {
                EditorGUILayout.PropertyField(InstanceSegmentationTags, true);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
