/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Utilities
{
    using Sensors;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.HighDefinition;

    public static class SensorPassRenderer
    {
        private static readonly int ViewMatrix = Shader.PropertyToID("_ViewMatrix");
        private static readonly int InvViewMatrix = Shader.PropertyToID("_InvViewMatrix");
        private static readonly int ProjMatrix = Shader.PropertyToID("_ProjMatrix");
        private static readonly int InvProjMatrix = Shader.PropertyToID("_InvProjMatrix");
        private static readonly int ViewProjMatrix = Shader.PropertyToID("_ViewProjMatrix");
        private static readonly int CameraViewProjMatrix = Shader.PropertyToID("_CameraViewProjMatrix");
        private static readonly int InvViewProjMatrix = Shader.PropertyToID("_InvViewProjMatrix");
        private static readonly int ScreenSize = Shader.PropertyToID("_ScreenSize");

        private static readonly Matrix4x4 CubeProj = Matrix4x4.Perspective(90.0f, 1.0f, 0.1f, 1000.0f);

        private static readonly int[] CubemapFaceOrder =
        {
            (int) CubemapFace.PositiveZ, (int) CubemapFace.NegativeZ, (int) CubemapFace.PositiveX,
            (int) CubemapFace.NegativeX, (int) CubemapFace.PositiveY, (int) CubemapFace.NegativeY
        };

        private static void SetupGlobalParamsForCubemap(CommandBuffer cmd, Matrix4x4 view, int cubemapSize)
        {
            var gpuView = view;
            if (ShaderConfig.s_CameraRelativeRendering != 0)
                gpuView.SetColumn(3, new Vector4(0, 0, 0, 1));
            var gpuProj = GL.GetGPUProjectionMatrix(CubeProj, false);
            var vp = gpuProj * gpuView;

            cmd.SetGlobalMatrix(ViewMatrix, gpuView);
            cmd.SetGlobalMatrix(InvViewMatrix, gpuView.inverse);
            cmd.SetGlobalMatrix(ProjMatrix, gpuProj);
            cmd.SetGlobalMatrix(InvProjMatrix, gpuProj.inverse);
            cmd.SetGlobalMatrix(ViewProjMatrix, vp);
            cmd.SetGlobalMatrix(InvViewProjMatrix, vp.inverse);
            cmd.SetGlobalMatrix(CameraViewProjMatrix, vp);
            cmd.SetGlobalVector(ScreenSize, new Vector4(cubemapSize, cubemapSize, 1f / cubemapSize, 1f / cubemapSize));
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
        public static void Render(ScriptableRenderContext context, CommandBuffer cmd, HDCamera hd, SensorRenderTarget target, ShaderTagId pass, Color clearColor)
        {
            if (target.IsCube)
                RenderToCubemap(context, cmd, hd, target, pass, clearColor);
            else
                RenderToTexture(context, cmd, hd, target, pass, clearColor);
        }

        private static void RenderToCubemap(ScriptableRenderContext context, CommandBuffer cmd, HDCamera hd, SensorRenderTarget target, ShaderTagId pass, Color clearColor)
        {
            hd.SetupGlobalParams(cmd, 0);
            context.SetupCameraProperties(hd.camera);

            var transform = hd.camera.transform;
            var r = transform.rotation;

            var originalProj = hd.camera.projectionMatrix;
            hd.camera.projectionMatrix = CubeProj;

            var sensor = hd.camera.GetComponent<CameraSensorBase>();
            var renderPostprocess = sensor != null && sensor.Postprocessing != null && sensor.Postprocessing.Count > 0;

            for (var i = 0; i < 6; ++i)
            {
                // Custom face order is used for dynamic exposure - this way it will be based on forward cube face
                var faceIndex = CubemapFaceOrder[i]; 

                if ((target.CubeFaceMask & (1 << faceIndex)) == 0)
                    continue;

                transform.localRotation = Quaternion.LookRotation(CoreUtils.lookAtList[faceIndex], CoreUtils.upVectorList[faceIndex]);
                var view = hd.camera.worldToCameraMatrix;
                SetupGlobalParamsForCubemap(cmd, view, target.ColorHandle.rt.width);

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
                    // NOTE: This should flip culling, not hard-set it to front. SRP API does not provide this option
                    //       currently. Expected issues with front-culled geometry.
                    // TODO: investigate HDAdditionalCameraData.FlipYMode.ForceFlipY, it might be a way to solve this
                    var stateBlock = new RenderStateBlock(RenderStateMask.Raster)
                    {
                        rasterState = new RasterState
                        {
                            cullingMode = CullMode.Front
                        }
                    };
                    context.DrawRenderers(cull, ref drawing, ref filter, ref stateBlock);

                    if (renderPostprocess)
                        SimulatorManager.Instance.Sensors.PostProcessSystem.RenderForSensor(cmd, hd, sensor, target.ColorHandle, (CubemapFace) i);
                }
            }

            if (renderPostprocess)
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }

            transform.rotation = r;
            hd.camera.projectionMatrix = originalProj;
        }

        private static void RenderToTexture(ScriptableRenderContext context, CommandBuffer cmd, HDCamera hd, SensorRenderTarget target, ShaderTagId pass, Color clearColor)
        {
            hd.SetupGlobalParams(cmd, 0);
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
                SimulatorManager.Instance.Sensors.PostProcessSystem.RenderForSensor(cmd, hd, sensor, target.ColorHandle);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }
        }
    }
}