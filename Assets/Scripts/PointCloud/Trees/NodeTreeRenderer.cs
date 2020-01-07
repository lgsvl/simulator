/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.PointCloud.Trees
{
    using UnityEngine;

    /// <summary>
    /// Point cloud renderer version operating on <see cref="NodeTree"/>.
    /// </summary>
    public class NodeTreeRenderer : PointCloudRenderer
    {
#pragma warning disable 0649
        
        public Camera RenderCamera;
        
#pragma warning restore 0649

        private int pointCount;
        
        private NodeTree renderedTree;

        protected override Bounds Bounds => renderedTree?.Bounds ?? default;

        protected override int PointCount => pointCount;

        protected override Camera TargetCamera => RenderCamera;

        protected override void OnEnable()
        {
            // Do nothing, buffer is managed through Refresh()
        }

        protected override void OnDisable()
        {
            // Do nothing, buffer is managed through Refresh()
        }

        public void Refresh(NodeTree tree, ComputeBuffer buffer, int validPointCount)
        {
            pointCount = validPointCount;
            renderedTree = tree;
            Buffer = buffer;
        }

        public void ClearBuffer()
        {
            Buffer = null;
        }
    }
}