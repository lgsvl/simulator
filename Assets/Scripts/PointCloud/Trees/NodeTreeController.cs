namespace PointCloud.Trees
{
    using System.Collections.Generic;
    using Simulator.PointCloud.Trees;
    using UnityEngine;

    /// <summary>
    /// Class controlling node tree rendering process for a single camera.
    /// </summary>
    [RequireComponent(typeof(NodeTreeRenderer))]
    public class NodeTreeController : MonoBehaviour
    {
        /// <summary>
        /// Describes culling mode used to determine node visibility.
        /// </summary>
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

        [SerializeField]
        [Tooltip("Loader responsible for loading octree that should be rendered.")]
        private NodeTreeLoader nodeTreeLoader;

        [SerializeField]
        [Tooltip("Camera that will be used for determining visibility of octree nodes.")]
        private Camera cullCamera;

        [SerializeField]
        [Tooltip("Defines node culling mode used by this controller.")]
        private CullMode cullMode;

        [Space]
        [SerializeField]
        [Tooltip("Maximum amount of points that can be rendered at once.")]
        private int pointLimit = 2000000;

        [SerializeField]
        [Tooltip("Minimum screen projection size (in pixels) of the node.")]
        private float minProjection = 100;

        [SerializeField]
        [Tooltip("Delay in frames between rebuilding visible nodes list.")]
        private int rebuildSteps = 10;

#pragma warning restore 0649

        private readonly Plane[] planes = new Plane[6];

        private readonly List<VisibleNode> visibleNodes = new List<VisibleNode>();

        private readonly List<string> usedNodes = new List<string>();
        
        private readonly VisibleNodeComparer comparer = new VisibleNodeComparer();
        
        private BufferBuilder bufferBuilder;
        
        private NodeTreeRenderer targetRenderer;

        private float viewMultiplier;
        
        private Vector3 rendererSpaceCameraPosition;

        private int pointCount;

        private int nodeCount;

        private int framesSinceLastRebuild;

        private void OnEnable()
        {
            targetRenderer = GetComponent<NodeTreeRenderer>();

            if (nodeTreeLoader == null || targetRenderer == null)
                return;
            
            bufferBuilder = new BufferBuilder(nodeTreeLoader.Tree.NodeLoader, pointLimit, rebuildSteps);
        }

        private void OnDisable()
        {
            bufferBuilder?.Dispose();
            bufferBuilder = null;
        }

        private void Update()
        {
            if (cullCamera == null)
            {
                cullCamera = SimulatorManager.Instance?.CameraManager?.SimulatorCamera ?? Camera.main;
            }

            if (nodeTreeLoader.Tree == null || cullCamera == null || targetRenderer == null)
                return;
            
            CullAndRender();
        }
        
        /// <summary>
        /// Calculates node visibility, requests memory and buffer updates and triggers render for associated camera.
        /// </summary>
        private void CullAndRender()
        {
            var tree = nodeTreeLoader.Tree;
            var root = tree.NodeRecords[TreeUtility.RootNodeIdentifier];
            visibleNodes.Clear();
            usedNodes.Clear();
            pointCount = 0;

            viewMultiplier = cullCamera.orthographic
                ? 0.5f * Screen.height / cullCamera.orthographicSize
                : 0.5f * Screen.height / Mathf.Tan(0.5f * cullCamera.fieldOfView * Mathf.Deg2Rad);

            var rendererTransform = targetRenderer.transform;
            rendererSpaceCameraPosition = targetRenderer.transform.InverseTransformPoint(cullCamera.transform.position);
            
            var m = rendererTransform.localToWorldMatrix;
            var v = cullCamera.worldToCameraMatrix;
            var p = cullCamera.projectionMatrix;
            
            GeometryUtility.CalculateFrustumPlanes(p * v * m, planes);

            CheckNodeRecursive(root);

            if (visibleNodes.Count == 0)
            {
                targetRenderer.ClearBuffer();
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
            
            if (pointCount == 0)
            {
                targetRenderer.ClearBuffer();
                return;
            }

            tree.NodeLoader.RequestLoad(usedNodes);
            var buffer = bufferBuilder.GetPopulatedBuffer(usedNodes, out var validPointCount);
            if (buffer != null)
                targetRenderer.Refresh(tree, buffer, validPointCount);
        }

        /// <summary>
        /// Checks node's visibility and importance. If node is viable for rendering, repeats the process for its children.
        /// </summary>
        /// <param name="node">Node to check.</param>
        private void CheckNodeRecursive(NodeRecord node)
        {
            if (cullMode == CullMode.CameraFrustum)
            {
                var inFrustum = GeometryUtility.TestPlanesAABB(planes, node.Bounds);
                if (!inFrustum)
                    return;
            }

            var projectedSize = cullCamera.orthographic
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
                
                CheckNodeRecursive(node.Children[i]);
            }
        }
    }
}