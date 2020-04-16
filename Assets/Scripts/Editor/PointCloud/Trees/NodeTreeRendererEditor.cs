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
    public class NodeTreeRendererEditor : PointCloudRendererEditor
    {
        private SerializedProperty nodeTreeLoader;
        private SerializedProperty cullCamera;
        private SerializedProperty cullMode;
        private SerializedProperty pointLimit;
        private SerializedProperty minProjection;
        private SerializedProperty rebuildSteps;

        private void FindPrivateProperties()
        {
            nodeTreeLoader = serializedObject.FindProperty(nameof(NodeTreeRenderer.nodeTreeLoader));
            cullCamera = serializedObject.FindProperty(nameof(NodeTreeRenderer.cullCamera));
            cullMode = serializedObject.FindProperty(nameof(NodeTreeRenderer.cullMode));
            pointLimit = serializedObject.FindProperty(nameof(NodeTreeRenderer.pointLimit));
            minProjection = serializedObject.FindProperty(nameof(NodeTreeRenderer.minProjection));
            rebuildSteps = serializedObject.FindProperty(nameof(NodeTreeRenderer.rebuildSteps));
        }
        
        protected override void OnEnable()
        {
            FindSharedProperties();
            FindPrivateProperties();
        }

        protected override void DrawInspector(PointCloudRenderer obj)
        {
            DrawProtectedProperties(obj);
            
            // Use existing SerializedProperty property to remember foldout state
            nodeTreeLoader.isExpanded = EditorGUILayout.Foldout(nodeTreeLoader.isExpanded, "Culling Settings");
            if (nodeTreeLoader.isExpanded)
            {
                EditorGUILayout.PropertyField(nodeTreeLoader);
                EditorGUILayout.PropertyField(cullCamera);
                EditorGUILayout.PropertyField(cullMode);
                EditorGUILayout.PropertyField(pointLimit);
                EditorGUILayout.PropertyField(minProjection);
                EditorGUILayout.PropertyField(rebuildSteps);
            }
        }
    }
}