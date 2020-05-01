/**
 * Copyright (c) 2019-2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Simulator.Bridge.Data;
using Simulator.Utilities;

namespace Simulator.Sensors
{
    using Simulator.PointCloud;

    [SensorType("Depth Camera", new[] {typeof(ImageData)})]
    public class DepthCameraSensor : CameraSensorBase
    {
        public override void Start()
        {
            base.Start();
            CameraTargetTextureReadWriteType = RenderTextureReadWrite.Linear;
            SensorCamera.GetComponent<HDAdditionalCameraData>().customRender += CustomRender;
        }

        void CustomRender(ScriptableRenderContext context, HDCamera hd)
        {
            var camera = hd.camera;

            var cmd = CommandBufferPool.Get();
            hd.SetupGlobalParams(cmd, 0);

            if (!Fisheye)
                CoreUtils.SetRenderTarget(cmd, renderTarget.ColorHandle, renderTarget.DepthHandle);
            
            CoreUtils.ClearRenderTarget(cmd, ClearFlag.All, Color.clear);

            ScriptableCullingParameters culling;
            if (camera.TryGetCullingParameters(out culling))
            {
                var cull = context.Cull(ref culling);

                context.SetupCameraProperties(camera);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var sorting = new SortingSettings(camera);
                var drawing = new DrawingSettings(new ShaderTagId("SimulatorDepthPass"), sorting);
                var filter = new FilteringSettings(RenderQueueRange.all);

                context.DrawRenderers(cull, ref drawing, ref filter);
            }

            if (!Fisheye)
                PointCloudManager.RenderDepth(context, cmd, hd, renderTarget.ColorHandle, renderTarget.DepthHandle);
            
            CommandBufferPool.Release(cmd);
        }

        public override bool Save(string path, int quality, int compression)
        {
            // Hide base.Save() since DepthCameraSensor does not support Save function for now.
            return false;
        }
    }
}
