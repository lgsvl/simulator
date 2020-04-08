/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.PointCloud.Trees
{
    using System.Collections.Generic;
    using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    /// <summary>
    /// Point cloud renderer version operating on <see cref="NodeTree"/>.
    /// </summary>
    [ExecuteInEditMode]
    public class NodeTreeRenderer : PointCloudRenderer
    {
        public enum CullMode
        {
            /// Nodes will be culled using camera frustum.
            CameraFrustum,
            /// Nodes will be culled using only distance from camera.
            Distance
        }
        
        private struct VisibleNode
        {
            public readonly string Identifier;
            public readonly float Weight;
            public readonly int PointCount;

            public VisibleNode(string identifier, float weight, int pointCount)
            {
                Identifier = identifier;
                Weight = weight;
                PointCount = pointCount;
            }
        }

        private class VisibleNodeComparer : IComparer<VisibleNode>
        {
            public int Compare(VisibleNode x, VisibleNode y)
            {
                return y.Weight.CompareTo(x.Weight);
            }
        }
        
#pragma warning disable 0649
        
        [Tooltip("Loader responsible for loading octree that should be rendered.")]
        public NodeTreeLoader nodeTreeLoader;
        
        [Tooltip("Camera that will be used for determining visibility of octree nodes.")]
        public Camera cullCamera;
        
        [Tooltip("Defines node culling mode used by this controller.")]
        public CullMode cullMode;
        
        [Tooltip("Maximum amount of points that can be rendered at once.")]
        public int pointLimit = 2000000;
        
        [Tooltip("Minimum screen projection size (in pixels) of the node.")]
        public float minProjection = 100;
        
        [Tooltip("Delay in frames between rebuilding visible nodes list.")]
        public int rebuildSteps = 10;
        
        [Tooltip("Amount of tree levels to load as preview in editor.")]
        public int previewDepth = 2;

#pragma warning restore 0649
        
        private int pointCount;

        private readonly Plane[] planes = new Plane[6];

        private readonly List<VisibleNode> visibleNodes = new List<VisibleNode>();

        private readonly List<string> usedNodes = new List<string>();
        
        private readonly VisibleNodeComparer comparer = new VisibleNodeComparer();
        
        private BufferBuilder bufferBuilder;

        private float viewMultiplier;
        
        private Vector3 rendererSpaceCameraPosition;
        
        private int nodeCount;

        private int framesSinceLastRebuild;

        public override Bounds Bounds
        {
            get
            {
                if (nodeTreeLoader != null)
                    return nodeTreeLoader.Tree?.Bounds ?? default;
                
                return default;
            }
        }

        private Camera CullCamera
        {
            get
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                    return cullCamera;

                if (EditorWindow.focusedWindow != SceneView.lastActiveSceneView)
                    return cullCamera;
                
                var cam = SceneView.lastActiveSceneView.camera;
                return cam != null ? cam : cullCamera;
#else
                return cullCamera;
#endif
            }
        }

        public override int PointCount => pointCount;

        protected void OnEnable()
        {
            if (nodeTreeLoader == null || nodeTreeLoader.Tree == null)
                return;
            
            RecreateBuilder();
        }

        protected override void OnDisable()
        {
            bufferBuilder?.Dispose();
            bufferBuilder = null;
        }
        
        private void Update()
        {
            if (CullCamera == null && Application.isPlaying)
            {
                // Don't use '.?' operator here - it ignores Unity's lifetime check for UnityEngine.Object
                if (SimulatorManager.InstanceAvailable &&
                    SimulatorManager.Instance.CameraManager != null &&
                    SimulatorManager.Instance.CameraManager.SimulatorCamera != null)
                {
                    cullCamera = SimulatorManager.Instance.CameraManager.SimulatorCamera;
                }
                else
                {
                    cullCamera = Camera.main;
                }
            }

            if (nodeTreeLoader == null || nodeTreeLoader.Tree == null)
            {
                bufferBuilder?.Dispose();
                bufferBuilder = null;
                ClearBuffer();
                return;
            }

            if (bufferBuilder == null || !bufferBuilder.Valid)
                RecreateBuilder();

            visibleNodes.Clear();
            usedNodes.Clear();
            pointCount = 0;
            
            if (CullCamera != null)
                CullAndRefresh();
            else
                RefreshPreview();
        }
        
        private void OnValidate()
        {
            if (bufferBuilder != null && (bufferBuilder.MaxBufferElements != pointLimit ||
                                          bufferBuilder.RebuildSteps != rebuildSteps))
            {
                RecreateBuilder();
            }
        }

        public void Refresh(NodeTree tree, ComputeBuffer buffer, int validPointCount)
        {
            Buffer = buffer;
        }

        public void ClearBuffer()
        {
            pointCount = 0;
            Buffer = null;
        }
        
        private void RecreateBuilder()
        {
            bufferBuilder?.Dispose();
            bufferBuilder = new BufferBuilder(nodeTreeLoader.Tree.NodeLoader, pointLimit, rebuildSteps);
        }
        
        /// <summary>
        /// Calculates node visibility, requests memory and buffer updates and triggers render for associated camera.
        /// </summary>
        private void CullAndRefresh()
        {
            var tree = nodeTreeLoader.Tree;
            var root = tree.NodeRecords[TreeUtility.RootNodeIdentifier];

            viewMultiplier = CullCamera.orthographic
                ? 0.5f * Screen.height / CullCamera.orthographicSize
                : 0.5f * Screen.height / Mathf.Tan(0.5f * CullCamera.fieldOfView * Mathf.Deg2Rad);

            var rendererTransform = transform;
            rendererSpaceCameraPosition = transform.InverseTransformPoint(CullCamera.transform.position);
            
            var m = rendererTransform.localToWorldMatrix;
            var v = CullCamera.worldToCameraMatrix;
            var p = CullCamera.projectionMatrix;
            
            GeometryUtility.CalculateFrustumPlanes(p * v * m, planes);

            CullNodeRecursive(root);

            if (visibleNodes.Count == 0)
            {
                ClearBuffer();
                return;
            }

            visibleNodes.Sort(comparer);
            var index = 0;
            
            while (pointCount < pointLimit && index < visibleNodes.Count)
            {
                var node = visibleNodes[index++];
                
                // Adding this node would exceed point limit, terminate
                if (pointCount + node.PointCount > pointLimit)
                    break;
                
                usedNodes.Add(node.Identifier);
                pointCount += node.PointCount;
            }
            
            RefreshTree();
        }

        private void RefreshPreview()
        {
            var tree = nodeTreeLoader.Tree;
            var root = tree.NodeRecords[TreeUtility.RootNodeIdentifier];
            
            if (previewDepth > 0)
                PreparePreviewRecursive(root, 1);

            foreach (var node in visibleNodes)
            {
                usedNodes.Add(node.Identifier);
                pointCount += node.PointCount;
            }

            RefreshTree();
        }

        private void RefreshTree()
        {
            var tree = nodeTreeLoader.Tree;
            if (pointCount == 0)
            {
                ClearBuffer();
                return;
            }

            tree.NodeLoader.RequestLoad(usedNodes);
            var buffer = bufferBuilder.GetPopulatedBuffer(usedNodes, out var validPointCount);
            if (buffer != null)
                Refresh(tree, buffer, validPointCount);
        }

        /// <summary>
        /// Checks node's visibility and importance. If node is viable for rendering, repeats the process for its children.
        /// </summary>
        /// <param name="node">Node to check.</param>
        private void CullNodeRecursive(NodeRecord node)
        {
            if (cullMode == CullMode.CameraFrustum)
            {
                var inFrustum = GeometryUtility.TestPlanesAABB(planes, node.Bounds);
                if (!inFrustum)
                    return;
            }

            var projectedSize = CullCamera.orthographic
                ? viewMultiplier * node.BoundingSphereRadius
                : viewMultiplier * node.BoundingSphereRadius /
                  node.CalculateDistanceTo(rendererSpaceCameraPosition);
            
            if (projectedSize < minProjection)
                return;
            
            visibleNodes.Add(new VisibleNode(node.Identifier, projectedSize, node.PointCount));

            if (!node.HasChildren)
                return;

            for (var i = 0; i < node.Children.Length; ++i)
            {
                if (node.Children[i] == null)
                    continue;
                
                CullNodeRecursive(node.Children[i]);
            }
        }

        private void PreparePreviewRecursive(NodeRecord node, int currentDepth)
        {
            visibleNodes.Add(new VisibleNode(node.Identifier, 1f, node.PointCount));

            if (!node.HasChildren || currentDepth >= previewDepth)
                return;

            for (var i = 0; i < node.Children.Length; ++i)
            {
                if (node.Children[i] == null)
                    continue;
                
                PreparePreviewRecursive(node.Children[i], currentDepth + 1);
            }
        }
    }
}