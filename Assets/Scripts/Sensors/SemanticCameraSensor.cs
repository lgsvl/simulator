/**
 * Copyright (c) 2019 LG Electronics, Inc.
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
    [SensorType("Semantic Camera", new[] {typeof(ImageData)})]
    [RequireComponent(typeof(Camera))]
    public class SemanticCameraSensor : CameraSensorBase
    {
        public void Start()
        {
            base.Start();
            // SemanticCameraSensor always use JpegQuality = 100
            JpegQuality = 100;
            SensorCamera.GetComponent<HDAdditionalCameraData>().customRender += CustomRender;
        }

        void CustomRender(ScriptableRenderContext context, HDCamera hd)
        {
            var camera = hd.camera;

            ScriptableCullingParameters culling;
            if (camera.TryGetCullingParameters(out culling))
            {
                var cull = context.Cull(ref culling);

                context.SetupCameraProperties(camera);

                var cmd = CommandBufferPool.Get();
                hd.SetupGlobalParams(cmd, 0);
                cmd.ClearRenderTarget(true, true, SimulatorManager.Instance.SemanticSkyColor);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);

                var sorting = new SortingSettings(camera);
                var drawing = new DrawingSettings(new ShaderTagId("SimulatorSemanticPass"), sorting);
                var filter = new FilteringSettings(RenderQueueRange.all);

                context.DrawRenderers(cull, ref drawing, ref filter);
            }
        }
    }
}
