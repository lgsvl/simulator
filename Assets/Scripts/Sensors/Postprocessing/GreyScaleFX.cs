/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Simulator.Sensors.Postprocessing
{
    [PostProcessOrder(2)]
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
                Debug.LogError($"Unable to find shader {ShaderName}. Post Process Volume {nameof(GreyScaleFX)} is unable to load.");
            }
        }

        protected override void DoCleanup()
        {
            CoreUtils.Destroy(material);
        }

        protected override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination, GreyScale data)
        {
            if (material == null)
                return;

            material.SetFloat("_Intensity", data.intensity);
            material.SetTexture("_InputTexture", source);
            HDUtils.DrawFullScreen(cmd, material, destination);
        }
    }
}

