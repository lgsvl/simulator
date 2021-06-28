/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.PointCloud
{
    using System.Linq;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.HighDefinition;

    public class PointCloudRenderPass : CustomPass
    {
        private PointCloudRenderer[] pointCloudRenderers;

        private RenderTargetIdentifier[] rtiCache1;
        private RenderTargetIdentifier[] rtiCache4;
        private RenderTargetIdentifier[] rtiCache5;

        private HDRenderPipeline RenderPipeline => RenderPipelineManager.currentPipeline as HDRenderPipeline;

        public void UpdateRenderers(PointCloudRenderer[] renderers)
        {
            pointCloudRenderers = renderers;
        }

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            RenderPipeline.OnRenderShadowMap += RenderShadows;
            RenderPipeline.OnRenderGBuffer += RenderLit;
            base.Setup(renderContext, cmd);
        }

        protected override void Execute(CustomPassContext ctx)
        {
            if (pointCloudRenderers == null || pointCloudRenderers.Length == 0)
                return;

            if (rtiCache1 == null)
                rtiCache1 = new[] {ctx.cameraColorBuffer.nameID};
            else
                rtiCache1[0] = ctx.cameraColorBuffer.nameID;

            var rendered = false;

            foreach (var pcr in pointCloudRenderers)
            {
                if (pcr.SupportsLighting)
                    continue;

                rendered = true;
                pcr.Render(ctx.cmd, ctx.hdCamera, rtiCache1, ctx.cameraDepthBuffer, ctx.cameraColorBuffer);
            }

            // Decals rendering triggers early depth buffer copy and marks it as valid for later usage.
            // Mark the copy as invalid after point cloud rendering, as depth buffer was changed.
            // Point cloud render should probably take part in depth prepass and be included in copy, but it can be
            // done at a later time.
            if (rendered && ctx.hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals))
                RenderPipeline.InvalidateDepthBufferCopy();
        }

        private void RenderShadows(CommandBuffer cmd, float worldTexelSize)
        {
            if (pointCloudRenderers == null || pointCloudRenderers.Length == 0)
                return;

            foreach (var pcr in pointCloudRenderers)
                pcr.RenderShadows(cmd, worldTexelSize);
        }

        private void RenderLit(HDRenderPipeline.GBufferRenderData data)
        {
            if (pointCloudRenderers == null || pointCloudRenderers.Length == 0)
                return;

            var doLitPass = pointCloudRenderers.Any(pcr => pcr.SupportsLighting);
            if (!doLitPass)
                return;

            var lightLayers = data.camera.frameSettings.IsEnabled(FrameSettingsField.LightLayers);
            RenderTargetIdentifier[] rtiCache;

            if (lightLayers)
            {
                if (rtiCache5 == null)
                    rtiCache5 = new RenderTargetIdentifier[5];

                for (var i = 0; i < rtiCache5.Length; ++i)
                    rtiCache5[i] = data.gBuffer[i];

                rtiCache = rtiCache5;

                // Note: GBuffer5 can still be present if shadowmasking is enabled, but we can't bake static shadows
                //       into point cloud data anyway, so it's safe to ignore it in this context.
            }
            else
            {
                if (rtiCache4 == null)
                    rtiCache4 = new RenderTargetIdentifier[4];

                for (var i = 0; i < rtiCache4.Length; ++i)
                    rtiCache4[i] = data.gBuffer[i];

                rtiCache = rtiCache4;
            }

            RenderPipeline.ForceRenderSky(data.camera, data.context.cmd, data.customPassColorBuffer, data.customPassDepthBuffer);

            foreach (var pcr in pointCloudRenderers)
            {
                if (!pcr.SupportsLighting)
                    continue;

                PointCloudManager.Resources.UpdateSHCoefficients(data.context.cmd, pcr.transform.position);
                pcr.Render(data.context.cmd, data.camera, rtiCache, data.depthBuffer, data.customPassColorBuffer);
            }
        }

        protected override void Cleanup()
        {
            if (RenderPipeline != null)
            {
                RenderPipeline.OnRenderShadowMap -= RenderShadows;
                RenderPipeline.OnRenderGBuffer -= RenderLit;
            }

            pointCloudRenderers = null;

            base.Cleanup();
        }
    }
}