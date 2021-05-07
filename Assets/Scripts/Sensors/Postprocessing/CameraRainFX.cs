/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Sensors.Postprocessing
{
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.HighDefinition;

    [PostProcessOrder(10)]
    public class CameraRainFX : PostProcessPass<Rain>
    {
        private const string ShaderName = "Hidden/Shader/CameraRainFX";
        private Material material;
        protected override bool IsActive => material != null;
        private EnvironmentEffectsManager EnvironmentEffectsManager => SimulatorManager.Instance.EnvironmentEffectsManager;

        protected override void DoSetup()
        {
            if (Shader.Find(ShaderName) != null)
            {
                material = new Material(Shader.Find(ShaderName));
            }
            else
            {
                Debug.LogWarning($"Unable to find shader {ShaderName}. Post Process Volume {nameof(CameraRainFX)} is unable to load.");
            }
        }

        protected override void DoCleanup()
        {
            CoreUtils.Destroy(material);
        }

        protected override void Render(PostProcessPassContext ctx, RTHandle source, RTHandle destination, Rain data)
        {
            var cmd = ctx.cmd;

            if (Mathf.Approximately(EnvironmentEffectsManager.Rain, 0f))
            {
                HDUtils.BlitCameraTexture(cmd, source, destination);
                return;
            }

            if (EnvironmentEffectsManager.Rain < .35)
            {
                cmd.EnableShaderKeyword("LOW");
                cmd.DisableShaderKeyword("MED");
                cmd.DisableShaderKeyword("HGH");
            }
            else if (EnvironmentEffectsManager.Rain >= .35 && EnvironmentEffectsManager.Rain < .7)
            {
                cmd.EnableShaderKeyword("MED");
                cmd.DisableShaderKeyword("LOW");
                cmd.DisableShaderKeyword("HGH");
            }
            else
            {
                cmd.EnableShaderKeyword("HGH");
                cmd.DisableShaderKeyword("LOW");
                cmd.DisableShaderKeyword("MED");
            }

            cmd.SetGlobalFloat("_Intensity", EnvironmentEffectsManager.Rain);
            cmd.SetGlobalTexture("_InputTexture", source);
            cmd.SetGlobalFloat("_Size", data.size);
            HDUtils.DrawFullScreen(cmd, material, destination);
        }
    }
}