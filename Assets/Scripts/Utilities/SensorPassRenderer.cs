/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Utilities
{
    using System;
    using Sensors;
    using Sensors.Postprocessing;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.HighDefinition;

    public static class SensorPassRenderer
    {
        private static readonly int[] CubemapFaceOrder =
        {
            (int) CubemapFace.PositiveZ, (int) CubemapFace.NegativeZ, (int) CubemapFace.PositiveX,
            (int) CubemapFace.NegativeX, (int) CubemapFace.PositiveY, (int) CubemapFace.NegativeY
        };

        /// <summary>
        /// Renders objects with specified pass to given <see cref="SensorRenderTarget"/>.
        /// </summary>
        /// <param name="context">Current rendering context.</param>
        /// <param name="cmd">Command buffer for queueing commands. Will be executed and cleared.</param>
        /// <param name="hd">HD camera to use for rendering.</param>
        /// <param name="target">Render target to which image will be rendered.</param>
        /// <param name="pass">Pass to use for rendering.</param>
        /// <param name="clearColor">Color that will be used for clearing color buffer.</param>
        public static void Render(ScriptableRenderContext context, CommandBuffer cmd, HDCamera hd, SensorRenderTarget target, ShaderTagId pass, Color clearColor)
        {
            Render(context, cmd, hd, target, pass, clearColor, null);
        }

        /// <summary>
        /// Renders objects with specified pass to given <see cref="SensorRenderTarget"/>.
        /// </summary>
        /// <param name="context">Current rendering context.</param>
        /// <param name="cmd">Command buffer for queueing commands. Will be executed and cleared.</param>
        /// <param name="hd">HD camera to use for rendering.</param>
        /// <param name="target">Render target to which image will be rendered.</param>
        /// <param name="pass">Pass to use for rendering.</param>
        /// <param name="clearColor">Color that will be used for clearing color buffer.</param>
        /// <param name="postRender">Delegate that will be called after pass was rendered. Called for each face if <see cref="target"/> is a cubemap.</param>
        public static void Render(ScriptableRenderContext context, CommandBuffer cmd, HDCamera hd, SensorRenderTarget target, ShaderTagId pass, Color clearColor, Action<CubemapFace> postRender)
        {
            if (target.IsCube)
                RenderToCubemap(context, cmd, hd, target, pass, clearColor, postRender);
            else
                RenderToTexture(context, cmd, hd, target, pass, clearColor, postRender);
        }

        private static void RenderToCubemap(ScriptableRenderContext context, CommandBuffer cmd, HDCamera hd, SensorRenderTarget target, ShaderTagId pass, Color clearColor, Action<CubemapFace> postRender)
        {
            var hdrp = (HDRenderPipeline) RenderPipelineManager.currentPipeline;
            hdrp.UpdateShaderVariablesForCamera(cmd, hd);
            context.SetupCameraProperties(hd.camera);

            var originalProj = hd.camera.projectionMatrix;

            var transform = hd.camera.transform;
            var rot = transform.rotation;
            var localRot = transform.localRotation;
            var sensor = hd.camera.GetComponent<CameraSensorBase>();
            var renderPostprocess = sensor != null && sensor.Postprocessing != null && sensor.Postprocessing.Count > 0;

            cmd.SetInvertCulling(true); // Cubemap uses RHS standard, face culling has to be inverted

            for (var i = 0; i < 6; ++i)
            {
                // Custom face order is used for dynamic exposure - this way it will be based on forward cube face
                var faceIndex = CubemapFaceOrder[i]; 

                if ((target.CubeFaceMask & (1 << faceIndex)) == 0)
                    continue;

                transform.localRotation = localRot * Quaternion.LookRotation(CoreUtils.lookAtList[faceIndex], CoreUtils.upVectorList[faceIndex]);
                hdrp.SetupGlobalParamsForCubemap(cmd, hd, target.ColorHandle.rt.width, out var proj);
                hd.camera.projectionMatrix = proj;

                CoreUtils.SetRenderTarget(cmd, target.ColorHandle, target.DepthHandle, ClearFlag.None, 0, (CubemapFace) faceIndex);
                cmd.ClearRenderTarget(true, true, clearColor);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                if (hd.camera.TryGetCullingParameters(out var culling))
                {
                    var cull = context.Cull(ref culling);

                    var sorting = new SortingSettings(hd.camera);
                    var drawing = new DrawingSettings(pass, sorting);
                    var filter = new FilteringSettings(RenderQueueRange.all);

                    context.DrawRenderers(cull, ref drawing, ref filter);

                    if (renderPostprocess)
                    {
                        var ctx = new PostProcessPassContext(cmd, hd, target);
                        SimulatorManager.Instance.Sensors.PostProcessSystem.RenderForSensor(ctx, sensor, (CubemapFace) i);
                    }
                }

                if (postRender != null)
                {
                    cmd.SetInvertCulling(false);
                    postRender.Invoke((CubemapFace) faceIndex);
                    cmd.SetInvertCulling(true);
                }
            }

            cmd.SetInvertCulling(false);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            hd.camera.projectionMatrix = originalProj;
            transform.rotation = rot;
        }

        private static void RenderToTexture(ScriptableRenderContext context, CommandBuffer cmd, HDCamera hd, SensorRenderTarget target, ShaderTagId pass, Color clearColor, Action<CubemapFace> postRender)
        {
            var hdrp = (HDRenderPipeline) RenderPipelineManager.currentPipeline;
            hdrp.UpdateShaderVariablesForCamera(cmd, hd);
            context.SetupCameraProperties(hd.camera);

            CoreUtils.SetRenderTarget(cmd, target.ColorHandle, target.DepthHandle);
            cmd.ClearRenderTarget(true, true, clearColor);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            if (hd.camera.TryGetCullingParameters(out var culling))
            {
                var cull = context.Cull(ref culling);

                var sorting = new SortingSettings(hd.camera);
                var drawing = new DrawingSettings(pass, sorting);
                var filter = new FilteringSettings(RenderQueueRange.all);

                context.DrawRenderers(cull, ref drawing, ref filter);
            }

            var sensor = hd.camera.GetComponent<CameraSensorBase>();
            if (sensor != null && sensor.Postprocessing != null && sensor.Postprocessing.Count > 0)
            {
                var ctx = new PostProcessPassContext(cmd, hd, target);
                SimulatorManager.Instance.Sensors.PostProcessSystem.RenderForSensor(ctx, sensor);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }

            postRender?.Invoke(CubemapFace.Unknown);
        }
    }
}