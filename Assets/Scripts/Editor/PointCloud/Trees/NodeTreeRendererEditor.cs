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
    using Simulator.PointCloud.Trees;

    [CanEditMultipleObjects]
    [CustomEditor(typeof(NodeTreeRenderer))]
    public class NodeTreeRendererEditor : UnityEditor.Editor
    {
        SerializedProperty RenderCamera;
        SerializedProperty Colorize;
        SerializedProperty Render;
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
            RenderCamera = serializedObject.FindProperty(nameof(NodeTreeRenderer.RenderCamera));
            Colorize = serializedObject.FindProperty(nameof(NodeTreeRenderer.Colorize));
            Render = serializedObject.FindProperty(nameof(NodeTreeRenderer.Render));
            ConstantSize = serializedObject.FindProperty(nameof(NodeTreeRenderer.ConstantSize));
            PixelSize = serializedObject.FindProperty(nameof(NodeTreeRenderer.PixelSize));
            AbsoluteSize = serializedObject.FindProperty(nameof(NodeTreeRenderer.AbsoluteSize));
            MinPixelSize = serializedObject.FindProperty(nameof(NodeTreeRenderer.MinPixelSize));
            DebugSolidBlitLevel = serializedObject.FindProperty(nameof(NodeTreeRenderer.DebugSolidBlitLevel));
            SolidRemoveHidden = serializedObject.FindProperty(nameof(NodeTreeRenderer.SolidRemoveHidden));
            DebugSolidPullPush = serializedObject.FindProperty(nameof(NodeTreeRenderer.DebugSolidPullPush));
            DebugSolidFixedLevel = serializedObject.FindProperty(nameof(NodeTreeRenderer.DebugSolidFixedLevel));
            DebugSolidMetric = serializedObject.FindProperty(nameof(NodeTreeRenderer.DebugSolidMetric));
            DebugSolidMetric2 = serializedObject.FindProperty(nameof(PointCloudRenderer.DebugSolidMetric2));
            DebugSolidPullParam = serializedObject.FindProperty(nameof(PointCloudRenderer.DebugSolidPullParam));
            DebugSolidAlwaysFillDistance =
                serializedObject.FindProperty(nameof(NodeTreeRenderer.DebugSolidAlwaysFillDistance));
        }

        public override void OnInspectorGUI()
        {
            var obj = target as PointCloudRenderer;
            if (obj == null)
                return;

            serializedObject.Update();

            EditorGUILayout.PropertyField(RenderCamera);
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