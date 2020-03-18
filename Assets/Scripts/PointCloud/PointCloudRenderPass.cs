/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.PointCloud
{
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.HighDefinition;

    public class PointCloudRenderPass : CustomPass
    {
        private PointCloudRenderer[] pointCloudRenderers;

        private RenderTargetIdentifier[] cachedTargetIdentifiers;

        private HDRenderPipeline RenderPipeline => RenderPipelineManager.currentPipeline as HDRenderPipeline;

        public void UpdateRenderers(PointCloudRenderer[] renderers)
        {
            pointCloudRenderers = renderers;
        }

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            RenderPipeline.OnRenderShadowMap += RenderShadows;
            base.Setup(renderContext, cmd);
        }

        protected override void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera hdCamera,
            CullingResults cullingResult)
        {
            if (pointCloudRenderers == null || pointCloudRenderers.Length == 0)
                return;

            var isLit = injectionPoint == PointCloudManager.LitInjectionPoint;
            var forceRender = false;
            
            if (hdCamera.camera.cameraType == CameraType.SceneView)
            {
                // Scene preview camera is always rendered as unlit points - for lit pass, just skip it altogether...
                if (isLit)
                    return;
                // ...and for unlit, render if even if renderer wants lit version
                forceRender = true;
            }
            
            RTHandle depthBuffer;
            RenderTargetIdentifier[] rtIds;
            
            GetCameraBuffers(out var colorBuffer, out depthBuffer);

            if (isLit)
            {
                // This cannot be cached - GBuffer RTIs can change between frames
                rtIds = RenderPipeline.GetGBuffersRTI(hdCamera);
            }
            else
            {
                if (cachedTargetIdentifiers == null)
                    cachedTargetIdentifiers = new[] {colorBuffer.nameID};
                else
                    cachedTargetIdentifiers[0] = colorBuffer.nameID;
                
                rtIds = cachedTargetIdentifiers;
            }

            foreach (var pcr in pointCloudRenderers)
            {
                if (!(pcr.SupportsLighting ^ isLit) || forceRender)
                    pcr.Render(cmd, hdCamera, rtIds, depthBuffer, colorBuffer);
            }

            // Decals rendering triggers early depth buffer copy and marks it as valid for later usage.
            // Mark the copy as invalid after point cloud rendering, as depth buffer was changed.
            // Point cloud render should probably take part in depth prepass and be included in copy, but it can be
            // done at a later time.
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals))
                RenderPipeline.InvalidateDepthBufferCopy();
        }

        private void RenderShadows(CommandBuffer cmd, float worldTexelSize)
        {
            if (pointCloudRenderers == null || pointCloudRenderers.Length == 0)
                return;

            foreach (var pcr in pointCloudRenderers)
                pcr.RenderShadows(cmd, worldTexelSize);
        }

        protected override void Cleanup()
        {
            RenderPipeline.OnRenderShadowMap -= RenderShadows;
            pointCloudRenderers = null;
            
            base.Cleanup();
        }
    }
}