/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Simulator.Sensors.Postprocessing
{
    [PostProcessOrder(100)]
    public sealed class GreyScaleFX : PostProcessPass<GreyScale>
    {
        private const string ShaderName = "Hidden/Shader/GreyScale";
        private Material material;
        protected override bool IsActive => material != null;

        protected override void DoSetup()
        {
            if (Shader.Find(ShaderName) != null)
            {
                material = new Material(Shader.Find(ShaderName));
            }
            else
            {
                Debug.LogWarning($"Unable to find shader {ShaderName}. Post Process Volume {nameof(GreyScaleFX)} is unable to load.");
            }
        }

        protected override void DoCleanup()
        {
            CoreUtils.Destroy(material);
        }

        protected override void Render(PostProcessPassContext ctx, RTHandle source, RTHandle destination, GreyScale data)
        {
            if (Mathf.Approximately(data.intensity, 0f))
            {
                HDUtils.BlitCameraTexture(ctx.cmd, source, destination);
                return;
            }

            ctx.cmd.SetGlobalFloat("_Intensity", data.intensity);
            ctx.cmd.SetGlobalTexture("_InputTexture", source);
            HDUtils.DrawFullScreen(ctx.cmd, material, destination);
        }
    }
}

