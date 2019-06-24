/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEditor;
using Simulator.PointCloud;

namespace Simulator.Editor.PointCloud
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(PointCloudRenderer))]
    public class PointCloudRendererEditor : UnityEditor.Editor
    {
        SerializedProperty PointCloudData;
        SerializedProperty Colorize;
        SerializedProperty Render;
        SerializedProperty ConstantSize;
        SerializedProperty PixelSize;
        SerializedProperty AbsoluteSize;
        SerializedProperty MinPixelSize;

        void OnEnable()
        {
            var obj = target as PointCloudRenderer;

            PointCloudData = serializedObject.FindProperty(nameof(PointCloudRenderer.Data));
            Colorize = serializedObject.FindProperty(nameof(PointCloudRenderer.Colorize));
            Render = serializedObject.FindProperty(nameof(PointCloudRenderer.Render));
            ConstantSize = serializedObject.FindProperty(nameof(PointCloudRenderer.ConstantSize));
            PixelSize = serializedObject.FindProperty(nameof(PointCloudRenderer.PixelSize));
            AbsoluteSize = serializedObject.FindProperty(nameof(PointCloudRenderer.AbsoluteSize));
            MinPixelSize = serializedObject.FindProperty(nameof(PointCloudRenderer.MinPixelSize));
        }

        public override void OnInspectorGUI()
        {
            var obj = target as PointCloudRenderer;

            serializedObject.Update();

            EditorGUILayout.PropertyField(PointCloudData);
            EditorGUILayout.PropertyField(Colorize);
            EditorGUILayout.PropertyField(Render);

            if (obj.Render == PointCloudRenderer.RenderType.Points)
            {
                EditorGUILayout.PropertyField(ConstantSize);
                if (obj.ConstantSize)
                {
                    EditorGUILayout.PropertyField(PixelSize);
                }
                else
                {
                    EditorGUILayout.PropertyField(AbsoluteSize);
                    EditorGUILayout.PropertyField(MinPixelSize);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
