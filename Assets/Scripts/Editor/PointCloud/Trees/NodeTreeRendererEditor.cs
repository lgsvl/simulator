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
        private SerializedProperty RenderCamera;

        protected override void OnEnable()
        {
            FindSharedProperties();
            RenderCamera = serializedObject.FindProperty(nameof(NodeTreeRenderer.RenderCamera));
        }

        protected override void DrawInspector(PointCloudRenderer obj)
        {
            EditorGUILayout.PropertyField(RenderCamera);
            DrawProtectedProperties(obj);
        }
    }
}