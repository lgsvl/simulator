/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Sensors.Postprocessing
{
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.HighDefinition;
    using Random = UnityEngine.Random;

    [PostProcessOrder(1100)]
    public sealed class VideoArtifactsFX : PostProcessPass<VideoArtifacts>
    {
        static class ShaderIDs
        {
            internal static readonly int BlockSeed1 = Shader.PropertyToID("_BlockSeed1");
            internal static readonly int BlockSeed2 = Shader.PropertyToID("_BlockSeed2");
            internal static readonly int BlockStrength = Shader.PropertyToID("_BlockStrength");
            internal static readonly int BlockStride = Shader.PropertyToID("_BlockStride");
            internal static readonly int InputTexture = Shader.PropertyToID("_InputTexture");
            internal static readonly int Seed = Shader.PropertyToID("_Seed");
            internal static readonly int BlockSize = Shader.PropertyToID("_BlockSize");
        }

        private Material material;

        private float prevTime;
        private float jumpTime;
        private float blockTime;

        private int blockSeed1 = 71;
        private int blockSeed2 = 113;
        private int blockStride = 1;

        protected override bool IsActive => material != null;

        protected override void DoSetup()
        {
            if (Shader.Find("Hidden/Shader/VideoArtifacts") != null)
                material = CoreUtils.CreateEngineMaterial("Hidden/Shader/VideoArtifacts");
        }

        protected override void Render(PostProcessPassContext ctx, RTHandle source, RTHandle destination, VideoArtifacts data)
        {
            var cmd = ctx.cmd;

            if (Mathf.Approximately(data.intensity, 0f))
            {
                HDUtils.BlitCameraTexture(cmd, source, destination);
                return;
            }

            // Update the time parameters.
            var time = Time.time;
            var delta = time - prevTime;
            prevTime = time;

            // Block parameters
            var block = data.intensity;
            var block3 = block * block * block;

            // Shuffle block parameters every 1/30 seconds.
            blockTime += delta * 60;
            if (blockTime > 1)
            {
                if (Random.value < 0.09f) blockSeed1 += 251;
                if (Random.value < 0.29f) blockSeed2 += 373;
                if (Random.value < 0.25f) blockStride = Random.Range(1, 32);
                blockTime = 0;
            }

            // Invoke the shader.
            cmd.SetGlobalInt(ShaderIDs.Seed, (int) (time * 10000));
            cmd.SetGlobalFloat(ShaderIDs.BlockStrength, block3);
            cmd.SetGlobalInt(ShaderIDs.BlockStride, blockStride);
            cmd.SetGlobalInt(ShaderIDs.BlockSeed1, blockSeed1);
            cmd.SetGlobalInt(ShaderIDs.BlockSeed2, blockSeed2);
            cmd.SetGlobalTexture(ShaderIDs.InputTexture, source);
            cmd.SetGlobalInt(ShaderIDs.BlockSize, data.blockSize);
            HDUtils.DrawFullScreen(cmd, material, destination);
        }

        protected override void DoCleanup()
        {
            CoreUtils.Destroy(material);
        }
    }
}