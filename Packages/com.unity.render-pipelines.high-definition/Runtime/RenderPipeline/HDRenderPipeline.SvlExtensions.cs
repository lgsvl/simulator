/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace UnityEngine.Rendering.HighDefinition
{
    using System;
    using Experimental.Rendering.RenderGraphModule;

    public partial class HDRenderPipeline
    {
        public struct GBufferRenderData
        {
            public RenderGraphContext context;
            public HDCamera camera;
            public RenderTargetIdentifier[] gBuffer;
            public RTHandle depthBuffer;
            public RTHandle customPassColorBuffer;
            public RTHandle customPassDepthBuffer;
        }

        /// <summary>
        /// Stencil ref that should be used when rendering geometry to GBuffer.
        /// </summary>
        // This assumes no split lighting and no SSR
        public static int StencilRefGBuffer => (int)StencilUsage.RequiresDeferredLighting;

        /// <summary>
        /// Stencil write mask that should be used when rendering geometry to GBuffer.
        /// </summary>
        public static int StencilWriteMaskGBuffer =>
            (int) StencilUsage.RequiresDeferredLighting | (int) StencilUsage.SubsurfaceScattering |
            (int) StencilUsage.TraceReflectionRay;

        public static float UnlitShadowsFilter
        {
            get
            {
                // Temporarily disable punctual and area light shadows due to artifacts (probably shadow bias)
                const uint exponent = 0b10000000;
                const uint mantissa = 0x007FFFFF;

                uint finalFlag = 0x00000000;
                finalFlag |= (uint) LightFeatureFlags.Directional;
                finalFlag &= mantissa;
                finalFlag |= exponent;

                return HDShadowUtils.Asfloat(finalFlag);
            }
        }

        /// <summary>
        /// <para>
        /// Event called after each shadow request rendering is done. State is set up to render to proper viewport
        /// in shadow map. See <see cref="HDShadowAtlas.RenderShadows"/> for available GPU variables.
        /// </para>
        /// <para>
        /// arg0 [CommandBuffer] - Buffer used for shadows rendering.<br/>
        /// arg1 [float] - World texel size for current shadow request.
        /// </para>
        /// </summary>
        public event Action<CommandBuffer, float> OnRenderShadowMap;

        /// <summary>
        /// <para>
        /// Event called after GBuffer pass in Shader Graph is done. Use this to modify GBuffer contents.
        /// </para>
        /// <para>
        /// arg0 [GBufferRenderData] - Struct containing current context and available resources.<br/>
        /// </para>
        /// </summary>
        public event Action<GBufferRenderData> OnRenderGBuffer;

        /// <summary>
        /// MSAA samples currently used by this render pipeline.
        /// </summary>
        public MSAASamples MSAASamples => m_MSAASamples;

        /// <summary>
        /// Marks current copy of depth buffer as invalid. Should be called after original depth buffer is modified.
        /// </summary>
        public void InvalidateDepthBufferCopy()
        {
            m_IsDepthBufferCopyValid = false;
        }

        /// <summary>
        /// Queues sky render for given camera. Results will be written to passed color and depth buffers.
        /// </summary>
        /// <param name="camera">Camera for which sky should be rendered.</param>
        /// <param name="cmd">Buffer used to queue commands.</param>
        /// <param name="colorBuffer">Target color buffer.</param>
        /// <param name="depthBuffer">Target depth buffer.</param>
        public void ForceRenderSky(HDCamera camera, CommandBuffer cmd, RTHandle colorBuffer, RTHandle depthBuffer)
        {
            if (m_EnableRenderGraph)
                m_SkyManager.RenderSky(camera, GetCurrentSunLight(), colorBuffer, depthBuffer, m_CurrentDebugDisplaySettings, cmd);
            else
                RenderSky(camera, cmd);
        }

        /// <summary>
        /// Updates parameters in global shader variables CBuffer for subsequent rendering of cubemap face.
        /// </summary>
        /// <param name="cmd">Buffer used to queue commands.</param>
        /// <param name="hdCamera">HD Camera that will be used to render the cubemap.</param>
        /// <param name="cubemapSize">Size (in pixels) of the cubemap face.</param>
        /// <param name="proj">Projection matrix calculated for current settings.</param>
        public void SetupGlobalParamsForCubemap(CommandBuffer cmd, HDCamera hdCamera, int cubemapSize, out Matrix4x4 proj)
        {
            SetupGlobalParamsForCubemapInternal(cmd, hdCamera, cubemapSize, ref m_ShaderVariablesGlobalCB, out proj);
        }

        private void SetupGlobalParamsForCubemapInternal(CommandBuffer cmd, HDCamera hdCamera, int cubemapSize, ref ShaderVariablesGlobal cb, out Matrix4x4 proj)
        {
            var gpuView = hdCamera.camera.worldToCameraMatrix;
            if (ShaderConfig.s_CameraRelativeRendering != 0)
                gpuView.SetColumn(3, new Vector4(0, 0, 0, 1));
            var cubeProj =  Matrix4x4.Perspective(90.0f, 1.0f, hdCamera.camera.nearClipPlane, hdCamera.camera.farClipPlane);
            proj = cubeProj;
            var gpuProj = GL.GetGPUProjectionMatrix(cubeProj, false);
            var vp = gpuProj * gpuView;

            cb._ViewMatrix = gpuView;
            cb._InvViewMatrix = gpuView.inverse;
            cb._ProjMatrix = gpuProj;
            cb._InvProjMatrix = gpuProj.inverse;
            cb._ViewProjMatrix = vp;
            cb._InvViewProjMatrix = vp.inverse;
            cb._CameraViewProjMatrix = vp;
            cb._ScreenSize = new Vector4(cubemapSize, cubemapSize, 1f / cubemapSize, 1f / cubemapSize);

            ConstantBuffer.PushGlobal(cmd, m_ShaderVariablesGlobalCB, HDShaderIDs._ShaderVariablesGlobal);
        }

        /// <summary>
        ///  Updates shader variables in ShaderVariablesGlobal CBuffer with given camera properties.
        /// </summary>
        /// <param name="cmd">CommandBuffer used for queueing commands.</param>
        /// <param name="camera">Camera from which values will be taken.</param>
        public void UpdateShaderVariablesForCamera(CommandBuffer cmd, HDCamera camera)
        {
            camera.UpdateShaderVariablesGlobalCB(ref m_ShaderVariablesGlobalCB);
            ConstantBuffer.PushGlobal(cmd, m_ShaderVariablesGlobalCB, HDShaderIDs._ShaderVariablesGlobal);
        }

        /// <summary>
        /// Returns view and projection matrices currently set in global CBuffer.
        /// </summary>
        /// <param name="view"></param>
        /// <param name="proj"></param>
        public void GetGlobalShaderMatrices(out Matrix4x4 view, out Matrix4x4 proj)
        {
            view = m_ShaderVariablesGlobalCB._ViewMatrix;
            proj = m_ShaderVariablesGlobalCB._ProjMatrix;
        }

        internal void InvokeShadowMapRender(CommandBuffer cmd, float worldTexelSize)
        {
            // Note: This method will never get triggered if there are no default HDRP shadow requests generated for
            // other renderers
            // CullResults.GetShadowCasterBounds can be faked and ScriptableRenderContext.DrawShadows can be skipped
            // to force shadow casting, but CullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives
            // does most of the shadow math in unmanaged code. HDRP notes suggest that this might be moved to C# at
            // some point - point cloud shadow casting should be able to override culling results then
            // For now adding a sub-pixel sized mesh in front of the camera can force shadows, as silly as this sounds
            // TODO: override culling results for point cloud shadow casting
            OnRenderShadowMap?.Invoke(cmd, worldTexelSize);
        }

        internal void InvokeGBufferRender(RenderGraphContext context, HDCamera camera, RenderTargetIdentifier[] gBuffer, RTHandle depthBuffer)
        {
            if (OnRenderGBuffer == null)
                return;

            var data = new GBufferRenderData
            {
                context = context,
                camera = camera,
                gBuffer = gBuffer,
                depthBuffer = depthBuffer,
                customPassColorBuffer = m_CustomPassColorBuffer.Value,
                customPassDepthBuffer = m_CustomPassDepthBuffer.Value
            };

            OnRenderGBuffer.Invoke(data);
        }
    }
}