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
using System.Collections.Generic;
using System;

namespace Simulator.Sensors
{
    [SensorType("Segmentation Camera", new[] {typeof(ImageData)})]
    [RequireComponent(typeof(Camera))]
    public class SegmentationCameraSensor : CameraSensorBase
    {
        public enum InstanceCandidateTags
        {
            Car,
            Road,
            Sidewalk,
            Vegetation,
            Obstacle,
            TrafficLight,
            Building,
            Sign,
            Shoulder,
            Pedestrian,
            Curb
        }

        // "InstanceSegmentationTags" indicates which tags should have instance segmention.
        // This setting should be a global one for whole simulator. But we need WebUI to allow users
        // able to set this global setting (not via Unity editor).
        // Before we get WebUI support for that, we temporarily put this setting as a local property
        // of SegmentationCameraSensor here, and use it to reset SegmentationColors in SimulatorManager.

        // TODO: Move this setting to SimulatorManager and use WebUI to set it.
        public List<InstanceCandidateTags> InstanceSegmentationTags = new List<InstanceCandidateTags>();

        public override void Start()
        {
            base.Start();
            // SegmentationCameraSensor always use JpegQuality = 100
            JpegQuality = 100;
            SensorCamera.GetComponent<HDAdditionalCameraData>().customRender += CustomRender;

            if (InstanceSegmentationTags.Count > 0)
            {
                // Check if instance segmentation has been set (either by Editor or by another SegmentationCamera).
                if (SimulatorManager.Instance.CheckInstanceSegmentationSetting())
                {
                    throw new Exception("Instance segmentation has been set for some tags. Reset is not allowed!");
                }

                foreach (InstanceCandidateTags tag in InstanceSegmentationTags)
                {
                    SimulatorManager.Instance.SetInstanceColor(tag.ToString());
                }
                SimulatorManager.Instance.ResetSegmentationColors();
            }
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
                cmd.ClearRenderTarget(true, true, SimulatorManager.Instance.SkySegmentationColor);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);

                var sorting = new SortingSettings(camera);
                var drawing = new DrawingSettings(new ShaderTagId("SimulatorSegmentationPass"), sorting);
                var filter = new FilteringSettings(RenderQueueRange.all);

                context.DrawRenderers(cull, ref drawing, ref filter);
            }
        }
    }
}
