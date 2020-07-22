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

#pragma warning restore 0649

        private int usedNodesPointCount;

        private int pointCount;

        private readonly Plane[] planes = new Plane[6];

        private readonly List<VisibleNode> visibleNodes = new List<VisibleNode>();

        private readonly List<string> usedNodes = new List<string>();

        private readonly VisibleNodeComparer comparer = new VisibleNodeComparer();

        private BufferBuilder bufferBuilder;

        private float viewMultiplier;

        private int nodeCount;

        private int framesSinceLastRebuild;

#if UNITY_EDITOR
        private SceneViewBufferBuilder sceneViewBufferBuilder;

        private int sceneViewPointCount;
#endif

        public override Bounds Bounds
        {
            get
            {
                if (nodeTreeLoader != null)
                    return nodeTreeLoader.Tree?.Bounds ?? default;

                return default;
            }
        }

        private Camera CullCamera => cullCamera;

        public override int PointCount => pointCount;

#if UNITY_EDITOR
        public override int SceneViewPointCount => sceneViewPointCount;
#endif

        protected void OnEnable()
        {
            if (nodeTreeLoader == null || nodeTreeLoader.Tree == null)
                return;

            RecreateBuilder();
        }

        protected override void OnDisable()
        {
            DisposeBuilder();
        }

        private void Update()
        {
            UpdateNodes(false);
        }

        /// <summary>
        /// Performs immediate update of buffers, ignoring all multi-frame and multi-thread settings.
        /// </summary>
        /// <param name="overrideCamera">Camera to use for culling instead of default one (optional).</param>
        public void UpdateImmediate(Camera overrideCamera = null)
        {
            UpdateNodes(true, overrideCamera);
        }

        private void UpdateNodes(bool immediate, Camera overrideCamera = null)
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
                DisposeBuilder();
                ClearBuffer();
                return;
            }

            if (bufferBuilder == null || !bufferBuilder.Valid)
                RecreateBuilder();

            var tree = nodeTreeLoader.Tree;

            visibleNodes.Clear();
            usedNodes.Clear();
            usedNodesPointCount = 0;

            var usedCamera = overrideCamera == null ? CullCamera : overrideCamera;

            if (usedCamera != null)
            {
                Cull(tree, usedCamera);

                if (usedNodesPointCount == 0)
                {
                    pointCount = 0;
                    Buffer = null;
                }
                else
                {
                    ComputeBuffer buffer;
                    int validPointCount;
                    if (immediate)
                    {
                        tree.NodeLoader.LoadImmediate(usedNodes);
                        buffer = bufferBuilder.GetPopulatedBufferImmediate(usedNodes, out validPointCount);
                    }
                    else
                    {
                        tree.NodeLoader.RequestLoad(usedNodes);
                        buffer = bufferBuilder.GetPopulatedBuffer(usedNodes, out validPointCount);
                    }

                    if (buffer != null)
                    {
                        Buffer = buffer;
                        pointCount = validPointCount;
                    }
                }
            }

#if UNITY_EDITOR
            visibleNodes.Clear();
            usedNodes.Clear();
            usedNodesPointCount = 0;
            var sceneCamera = SceneView.lastActiveSceneView.camera;

            if (sceneCamera != null)
            {
                Cull(nodeTreeLoader.Tree, sceneCamera);

                if (usedNodesPointCount == 0)
                {
                    sceneViewPointCount = 0;
                    sceneViewBuffer = null;
                }
                else
                {
                    ComputeBuffer buffer;
                    int validPointCount;
                    if (immediate)
                    {
                        tree.NodeLoader.LoadImmediate(usedNodes);
                        buffer = bufferBuilder.GetPopulatedBufferImmediate(usedNodes, out validPointCount);
                    }
                    else
                    {
                        tree.NodeLoader.RequestLoad(usedNodes);
                        buffer = sceneViewBufferBuilder.GetPopulatedBuffer(usedNodes, out validPointCount);
                    }
                    
                    if (buffer != null)
                    {
                        sceneViewBuffer = buffer;
                        sceneViewPointCount = validPointCount;
                    }
                }
            }
#endif
        }
        
        private void OnValidate()
        {
            if (bufferBuilder != null && (bufferBuilder.MaxBufferElements != pointLimit ||
                                          bufferBuilder.RebuildSteps != rebuildSteps))
            {
                RecreateBuilder();
            }
        }

        public void ClearBuffer()
        {
            pointCount = 0;
            Buffer = null;

#if UNITY_EDITOR
            sceneViewPointCount = 0;
            sceneViewBuffer = null;
#endif
        }

        private void RecreateBuilder()
        {
            DisposeBuilder();

            bufferBuilder = new BufferBuilder(nodeTreeLoader.Tree.NodeLoader, pointLimit, rebuildSteps);

#if UNITY_EDITOR
            sceneViewBufferBuilder = new SceneViewBufferBuilder(nodeTreeLoader.Tree.NodeLoader, pointLimit, rebuildSteps);
#endif
        }

        private void DisposeBuilder()
        {
            bufferBuilder?.Dispose();
            bufferBuilder = null;

#if UNITY_EDITOR
            sceneViewBufferBuilder?.Dispose();
            sceneViewBufferBuilder = null;
#endif
        }

        /// <summary>
        /// Calculates node visibility, requests memory and buffer updates and triggers render for associated camera.
        /// </summary>
        private void Cull(NodeTree tree, Camera usedCamera)
        {
            visibleNodes.Clear();
            var root = tree.NodeRecords[TreeUtility.RootNodeIdentifier];

            viewMultiplier = usedCamera.orthographic
                ? 0.5f * Screen.height / usedCamera.orthographicSize
                : 0.5f * Screen.height / Mathf.Tan(0.5f * usedCamera.fieldOfView * Mathf.Deg2Rad);

            var rendererTransform = transform;
            var rendererSpaceCameraPosition = transform.InverseTransformPoint(usedCamera.transform.position);

            var m = rendererTransform.localToWorldMatrix;
            var v = usedCamera.worldToCameraMatrix;
            var p = usedCamera.projectionMatrix;

            GeometryUtility.CalculateFrustumPlanes(p * v * m, planes);

            CullNodeRecursive(root, usedCamera, rendererSpaceCameraPosition);

            if (visibleNodes.Count == 0)
            {
                if (usedCamera.cameraType != CameraType.SceneView)
                    ClearBuffer();

                return;
            }

            visibleNodes.Sort(comparer);
            var index = 0;

            while (usedNodesPointCount < pointLimit && index < visibleNodes.Count)
            {
                var node = visibleNodes[index++];

                // Adding this node would exceed point limit, terminate
                if (usedNodesPointCount + node.PointCount > pointLimit)
                    break;

                usedNodes.Add(node.Identifier);
                usedNodesPointCount += node.PointCount;
            }
        }

        /// <summary>
        /// Checks node's visibility and importance. If node is viable for rendering, repeats the process for its children.
        /// </summary>
        /// <param name="node">Node to check.</param>
        /// <param name="usedCamera">Camera used for culling.</param>
        /// <param name="rendererSpaceCameraPosition">Renderer position in camera-relative space.</param>
        private void CullNodeRecursive(NodeRecord node, Camera usedCamera, Vector3 rendererSpaceCameraPosition)
        {
            if (cullMode == CullMode.CameraFrustum)
            {
                var inFrustum = GeometryUtility.TestPlanesAABB(planes, node.Bounds);
                if (!inFrustum)
                    return;
            }

            var projectedSize = usedCamera.orthographic
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

                CullNodeRecursive(node.Children[i], usedCamera, rendererSpaceCameraPosition);
            }
        }
    }
}