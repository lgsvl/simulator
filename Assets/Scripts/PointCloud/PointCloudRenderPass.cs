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

        private bool SkyPreRenderRequired()
        {
            // Unlit injection point has sky already rendered - skip
            if (injectionPoint != PointCloudManager.LitInjectionPoint)
                return false;

            foreach (var pointCloudRenderer in pointCloudRenderers)
            {
                if ((pointCloudRenderer.Mask & PointCloudRenderer.RenderMask.Camera) != 0 &&
                    pointCloudRenderer.SupportsLighting &&
                    pointCloudRenderer.DebugBlendSky)
                {
                    return true;
                }
            }

            return false;
        }

        protected override void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera hdCamera,
            CullingResults cullingResult)
        {
            if (pointCloudRenderers == null || pointCloudRenderers.Length == 0)
                return;

            var isLit = injectionPoint == PointCloudManager.LitInjectionPoint;

            RenderTargetIdentifier[] rtIds;
            
            GetCameraBuffers(out var colorBuffer, out var depthBuffer);

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
            
            if (SkyPreRenderRequired())
                RenderPipeline.ForceRenderSky(hdCamera, cmd);

            foreach (var pcr in pointCloudRenderers)
            {
                if (!(pcr.SupportsLighting ^ isLit))
                {
                    if (isLit)
                    {
                        // Update SH coefficients on compose material - Unity will not push this data for custom pass
                        PointCloudManager.Resources.UpdateSHCoefficients(cmd, pcr.transform.position);
                    }
                    
                    pcr.Render(cmd, hdCamera, rtIds, depthBuffer, colorBuffer);
                }
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