/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Sensors.Postprocessing
{
    using UnityEngine;
    using UnityEngine.Experimental.Rendering;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.HighDefinition;

    [PostProcessOrder(20)]
    public sealed class SunFlareFX : PostProcessPass<SunFlare>
    {
        private const string ShaderName = "Hidden/Shader/SunFlare";

        // Changing these constants will require shader modifications; occlusion res will require additional reduction
        // pass above 64
        private const int AngleSamples = 128;
        private const int OcclusionRes = 64;

        private static readonly int SunViewPos = Shader.PropertyToID("_SunViewPos");
        private static readonly int SunSettings = Shader.PropertyToID("_SunSettings");
        private static readonly int InputTexture = Shader.PropertyToID("_InputTexture");
        private static readonly int DepthTexture = Shader.PropertyToID("_DepthTex");
        private static readonly int OcclusionTextureOut = Shader.PropertyToID("_OcclusionTexOut");
        private static readonly int OcclusionTextureIn = Shader.PropertyToID("_OcclusionTexIn");
        private static readonly int DepthTextureRes = Shader.PropertyToID("_DepthTexSize");
        private static readonly int OcclusionTextureRes = Shader.PropertyToID("_OcclusionTexSize");
        private static readonly int AngleOcclusion = Shader.PropertyToID("_AngleOcclusion");

        private static LayerMask LayerMask = LayerMask.GetMask("Obstacle");

        private int textureOcclusionKernel;
        private int blurTextureOcclusionKernel;
        private int reduceTextureOcclusionKernel;
        private int angleOcclusionKernel;

        private Material material;
        private ComputeShader computeShader;
        private RTHandle occlusionTextureA;
        private RTHandle occlusionTextureB;
        private ComputeBuffer angleOcclusion;

        private Transform sunTransform;

        protected override bool IsActive => material != null && SunTransform != null && computeShader != null;

        private Transform SunTransform
        {
            get
            {
                if (sunTransform == null)
                    sunTransform = SimulatorManager.Instance.EnvironmentEffectsManager.Sun.transform;
                return sunTransform;
            }
        }

        protected override void DoSetup()
        {
            if (Shader.Find(ShaderName) != null)
            {
                material = new Material(Shader.Find(ShaderName));
            }
            else
            {
                Debug.LogWarning($"Unable to find shader {ShaderName}. Post Process Volume {nameof(SunFlareFX)} is unable to load.");
            }

            computeShader = Resources.Load("PostProcessShaders/SunFlareOcclusion") as ComputeShader;
            if (computeShader == null)
            {
                Debug.LogWarning($"Unable to find shader SunFlareOcclusion in Resources. Post Process Volume {nameof(SunFlareFX)} is unable to load.");
            }
            else
            {
                textureOcclusionKernel = computeShader.FindKernel("TextureOcclusion");
                blurTextureOcclusionKernel = computeShader.FindKernel("BlurTextureOcclusion");
                reduceTextureOcclusionKernel = computeShader.FindKernel("ReduceTextureOcclusion");
                angleOcclusionKernel = computeShader.FindKernel("AngleOcclusion");
            }

            occlusionTextureA = RTHandles.Alloc(
                OcclusionRes, OcclusionRes,
                colorFormat: GraphicsFormat.R32_SFloat,
                name: "SunFlareOcclusionA",
                enableRandomWrite: true,
                useMipMap: false,
                autoGenerateMips: false,
                wrapMode: TextureWrapMode.Clamp);

            occlusionTextureB = RTHandles.Alloc(
                OcclusionRes, OcclusionRes,
                colorFormat: GraphicsFormat.R32_SFloat,
                name: "SunFlareOcclusionB",
                enableRandomWrite: true,
                useMipMap: false,
                autoGenerateMips: false,
                wrapMode: TextureWrapMode.Clamp);

            angleOcclusion = new ComputeBuffer(AngleSamples, sizeof(float));
        }

        protected override void DoCleanup()
        {
            CoreUtils.Destroy(material);
            occlusionTextureA.Release();
            occlusionTextureA = null;

            occlusionTextureB.Release();
            occlusionTextureB = null;

            angleOcclusion.Release();
            angleOcclusion = null;
        }

        protected override void Render(PostProcessPassContext ctx, RTHandle source, RTHandle destination, SunFlare data)
        {
            var cmd = ctx.cmd;
            var camera = ctx.hdCamera;
            var cam = camera.camera;
            var sunForward = sunTransform.forward;
            var sunWorldPos = cam.transform.position - sunForward * 1000f;
            var sunViewPos = cam.WorldToViewportPoint(sunWorldPos);

            var intensity = Mathf.Clamp01(Vector3.Dot(cam.transform.forward, -sunForward));
            var sunVisible = sunViewPos.z > 0 && sunViewPos.x >= -0.1f && sunViewPos.x < 1.1f &&
                             sunViewPos.y >= -0.1f && sunViewPos.y < 1.1f;

            if (!sunVisible)
            {
                if (Physics.Raycast(cam.transform.position, -sunForward, 1000f, LayerMask))
                    intensity = 0f;
            }

            if (intensity > 0f)
            {
                var depthTexRes = ctx.cameraDepthBuffer.referenceSize;
                var actualCameraSize = new Vector2Int(camera.actualWidth, camera.actualHeight);
                var occlTexRes = new Vector2Int(OcclusionRes, OcclusionRes);

                var scaleRatio = new Vector2((float) actualCameraSize.x / depthTexRes.x, (float) actualCameraSize.y / depthTexRes.y);
                var aspectRatio = (float) actualCameraSize.y / actualCameraSize.x;
                var scaledSun = new Vector4(sunViewPos.x * scaleRatio.x, sunViewPos.y * scaleRatio.y,
                    0.1f * aspectRatio * scaleRatio.x, 0.1f * scaleRatio.y);

                cmd.SetComputeVectorParam(computeShader, DepthTextureRes,
                    new Vector4(depthTexRes.x, depthTexRes.y, 1f / depthTexRes.x, 1f / depthTexRes.y));
                cmd.SetComputeVectorParam(computeShader, OcclusionTextureRes,
                    new Vector4(occlTexRes.x, occlTexRes.y, 1f / occlTexRes.x, 1f / occlTexRes.y));
                cmd.SetComputeVectorParam(computeShader, SunViewPos, scaledSun);

                var kernel = textureOcclusionKernel;
                cmd.SetComputeTextureParam(computeShader, kernel, DepthTexture, ctx.cameraDepthBuffer);
                cmd.SetComputeTextureParam(computeShader, kernel, OcclusionTextureOut, occlusionTextureA);
                cmd.DispatchCompute(computeShader, kernel, OcclusionRes / 8, OcclusionRes / 8, 1);

                kernel = blurTextureOcclusionKernel;
                cmd.SetComputeTextureParam(computeShader, kernel, OcclusionTextureIn, occlusionTextureA);
                cmd.SetComputeTextureParam(computeShader, kernel, OcclusionTextureOut, occlusionTextureB);
                cmd.DispatchCompute(computeShader, kernel, OcclusionRes / 8, OcclusionRes / 8, 1);

                kernel = reduceTextureOcclusionKernel;
                cmd.SetComputeTextureParam(computeShader, kernel, OcclusionTextureIn, occlusionTextureB);
                cmd.SetComputeTextureParam(computeShader, kernel, OcclusionTextureOut, occlusionTextureA);
                cmd.DispatchCompute(computeShader, kernel, 1, 1, 1);

                kernel = angleOcclusionKernel;
                cmd.SetComputeTextureParam(computeShader, kernel, OcclusionTextureIn, occlusionTextureB);
                cmd.SetComputeBufferParam(computeShader, kernel, AngleOcclusion, angleOcclusion);
                cmd.DispatchCompute(computeShader, kernel, AngleSamples / 64, 1, 1);

                cmd.SetGlobalVector(SunViewPos, sunViewPos);
                cmd.SetGlobalVector(SunSettings,
                    new Vector4(data.sunIntensity, data.haloIntensity, data.ghostingIntensity, intensity));
                cmd.SetGlobalTexture(InputTexture, source);
                cmd.SetGlobalTexture(OcclusionTextureIn, occlusionTextureA);
                cmd.SetGlobalTexture(OcclusionTextureOut, occlusionTextureB);
                cmd.SetGlobalBuffer(AngleOcclusion, angleOcclusion);
                HDUtils.DrawFullScreen(cmd, material, destination);
            }
            else
            {
                HDUtils.BlitCameraTexture(cmd, source, destination);
            }
        }
    }
}