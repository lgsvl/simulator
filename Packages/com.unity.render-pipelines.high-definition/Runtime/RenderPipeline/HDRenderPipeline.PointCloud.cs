namespace UnityEngine.Rendering.HighDefinition
{
    using System;

    public partial class HDRenderPipeline
    {
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
                // const uint mantissa = ((uint)LightFeatureFlags.Punctual | (uint)LightFeatureFlags.Directional | (uint)LightFeatureFlags.Area) & 0x007FFFFFu;
                const uint mantissa = (uint)LightFeatureFlags.Directional & 0x007FFFFFu;
                const uint exponent = 0b10000000u;
                return HDShadowUtils.Asfloat((exponent << 23) | mantissa);
            }
        }

        /// <summary>
        /// Marks current copy of depth buffer as invalid. Should be called after original depth buffer is modified.
        /// </summary>
        public void InvalidateDepthBufferCopy()
        {
            m_IsDepthBufferCopyValid = false;
        }
        
        /// <summary>
        /// Returns array of currently used GBuffer render targets for given camera.
        /// </summary>
        public RenderTargetIdentifier[] GetGBuffersRTI(HDCamera hdCamera)
        {
            return m_GbufferManager.GetBuffersRTI(hdCamera.frameSettings);
        }

        public void ForceRenderSky(HDCamera camera, CommandBuffer cmd)
        {
            RenderSky(camera, cmd);
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
    }
}