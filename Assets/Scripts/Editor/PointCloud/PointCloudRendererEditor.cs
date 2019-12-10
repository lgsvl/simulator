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
        SerializedProperty Blit;
        SerializedProperty ConstantSize;
        SerializedProperty PixelSize;
        SerializedProperty AbsoluteSize;
        SerializedProperty MinPixelSize;
        SerializedProperty DebugSolidBlitLevel;
        SerializedProperty SolidRemoveHidden;
        SerializedProperty DebugSolidPullPush;
        SerializedProperty DebugSolidFixedLevel;
        SerializedProperty DebugSolidMetric;
        SerializedProperty DebugSolidMetric2;
        SerializedProperty DebugSolidPullParam;
        SerializedProperty DebugSolidAlwaysFillDistance;

        void OnEnable()
        {
            var obj = target as PointCloudRenderer;

            PointCloudData = serializedObject.FindProperty(nameof(PointCloudRenderer.Data));
            Colorize = serializedObject.FindProperty(nameof(PointCloudRenderer.Colorize));
            Render = serializedObject.FindProperty(nameof(PointCloudRenderer.Render));
            Blit = serializedObject.FindProperty(nameof(PointCloudRenderer.Blit));
            ConstantSize = serializedObject.FindProperty(nameof(PointCloudRenderer.ConstantSize));
            PixelSize = serializedObject.FindProperty(nameof(PointCloudRenderer.PixelSize));
            AbsoluteSize = serializedObject.FindProperty(nameof(PointCloudRenderer.AbsoluteSize));
            MinPixelSize = serializedObject.FindProperty(nameof(PointCloudRenderer.MinPixelSize));
            DebugSolidBlitLevel = serializedObject.FindProperty(nameof(PointCloudRenderer.DebugSolidBlitLevel));
            SolidRemoveHidden = serializedObject.FindProperty(nameof(PointCloudRenderer.SolidRemoveHidden));
            DebugSolidPullPush = serializedObject.FindProperty(nameof(PointCloudRenderer.DebugSolidPullPush));
            DebugSolidFixedLevel = serializedObject.FindProperty(nameof(PointCloudRenderer.DebugSolidFixedLevel));
            DebugSolidMetric = serializedObject.FindProperty(nameof(PointCloudRenderer.DebugSolidMetric));
            DebugSolidMetric2 = serializedObject.FindProperty(nameof(PointCloudRenderer.DebugSolidMetric2));
            DebugSolidPullParam = serializedObject.FindProperty(nameof(PointCloudRenderer.DebugSolidPullParam));
            DebugSolidAlwaysFillDistance = serializedObject.FindProperty(nameof(PointCloudRenderer.DebugSolidAlwaysFillDistance));
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
            else if (obj.Render == PointCloudRenderer.RenderType.Solid)
            {
                EditorGUILayout.PropertyField(Blit);
                EditorGUILayout.PropertyField(DebugSolidBlitLevel);
                EditorGUILayout.PropertyField(SolidRemoveHidden);
                EditorGUILayout.PropertyField(DebugSolidPullPush);
                EditorGUILayout.PropertyField(DebugSolidFixedLevel);
                EditorGUILayout.PropertyField(DebugSolidMetric);
                EditorGUILayout.PropertyField(DebugSolidMetric2);
                EditorGUILayout.PropertyField(DebugSolidPullParam);
                EditorGUILayout.PropertyField(DebugSolidAlwaysFillDistance);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
