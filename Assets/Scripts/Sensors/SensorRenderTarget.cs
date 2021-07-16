/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Sensors
{
    using UnityEngine;
    using UnityEngine.Experimental.Rendering;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.HighDefinition;

    /// <summary>
    /// <para>Class wrapping render target management for camera-based sensors.</para>
    /// <para>Can be backed by either a default <see cref="RenderTexture"/> or a set of separate color and depth
    /// <see cref="RTHandle"/> if compatibility with HDRP or depth sampling is required.</para>
    /// </summary>
    public class SensorRenderTarget
    {
        /// <summary>
        /// Struct used to store parameters for final blit from TextureArray to Texture2D.
        /// </summary>
        struct BlitParams
        {
            public int srcTexArraySlice;
            public int dstTexArraySlice;
            public Rect viewport;
            public Material blitMaterial;
        }

        private static readonly int BlitTextureProperty = Shader.PropertyToID("_BlitTexture");
        private static readonly int BlitScaleBiasProperty = Shader.PropertyToID("_BlitScaleBias");
        private static readonly int BlitMipLevelProperty = Shader.PropertyToID("_BlitMipLevel");
        private static readonly int BlitTexArraySliceProperty = Shader.PropertyToID("_BlitTexArraySlice");

        private static readonly MaterialPropertyBlock BlitPropertyBlock = new MaterialPropertyBlock();

        private RTHandle colorRt;
        private RTHandle depthRt;

        // This only exists to avoid assertion error when trying to display Tex2DArray on RawImage
        private RTHandle uiRt;

        /// <summary>
        /// Width (in pixels) currently used by allocated resources.
        /// </summary>
        private readonly int currentWidth;

        /// <summary>
        /// Height (in pixels) currently used by allocated resources.
        /// </summary>
        private readonly int currentHeight;

        /// <summary>
        /// Dimension currently used by allocated resources.
        /// </summary>
        private readonly TextureDimension currentDimension;

        /// <summary>
        /// Texture used for rendering color data.
        /// </summary>
        public RenderTexture ColorTexture => colorRt.rt;

        /// <summary>
        /// Texture used for rendering depth data.
        /// </summary>
        public RenderTexture DepthTexture => depthRt.rt;

        /// <summary>
        /// Texture used for rendering UI.
        /// </summary>
        public RenderTexture UiTexture => uiRt.rt;

        /// <summary>
        /// <see cref="RTHandle"/> pointing to color texture.
        /// </summary>
        public RTHandle ColorHandle => colorRt;

        /// <summary>
        /// <see cref="RTHandle"/> pointing to depth texture.
        /// </summary>
        public RTHandle DepthHandle => depthRt;
        
        /// <summary>
        /// Is this target a cube map?
        /// </summary>
        public bool IsCube { get; private set; }
        
        /// <summary>
        /// Mask for cubemap faces used on this target.
        /// </summary>
        public int CubeFaceMask { get; private set; }

        private SensorRenderTarget(int width, int height, bool cube, GraphicsFormat colorFormat = GraphicsFormat.R8G8B8A8_UNorm, bool uiVisible = false)
        {
            currentWidth = width;
            currentHeight = height;
            IsCube = cube;

            if (cube)
                AllocCube();
            else
                Alloc2D(colorFormat, uiVisible);
        }

        public static implicit operator RenderTexture(SensorRenderTarget target) => target.ColorTexture;

        /// <summary>
        /// Creates new instance of <see cref="SensorRenderTarget"/> and allocates required resources. Uses 2D texture.
        /// </summary>
        /// <param name="width">Width (in pixels) of the texture.</param>
        /// <param name="height">Height (in pixels) of the texture.</param>
        /// <param name="uiVisible">Will this texture be displayed on UI elements?</param>
        public static SensorRenderTarget Create2D(int width, int height, bool uiVisible = false)
        {
            var instance = new SensorRenderTarget(width, height, false, uiVisible: uiVisible);
            return instance;
        }

        /// <summary>
        /// Creates new instance of <see cref="SensorRenderTarget"/> and allocates required resources. Uses 2D texture.
        /// </summary>
        /// <param name="width">Width (in pixels) of the texture.</param>
        /// <param name="height">Height (in pixels) of the texture.</param>
        /// <param name="colorFormat">GraphicsFormat of the color texture.</param>
        /// <param name="uiVisible">Will this texture be displayed on UI elements?</param>
        public static SensorRenderTarget Create2D(int width, int height, GraphicsFormat colorFormat, bool uiVisible = false)
        {
            var instance = new SensorRenderTarget(width, height, false, colorFormat, uiVisible);
            return instance;
        }

        /// <summary>
        /// Creates new instance of <see cref="SensorRenderTarget"/> and allocates required resources. Uses cube map.
        /// </summary>
        /// <param name="width">Width (in pixels) of the texture.</param>
        /// <param name="height">Height (in pixels) of the texture.</param>
        public static SensorRenderTarget CreateCube(int width, int height)
        {
            var faceMask = 0;
            faceMask |= 1 << (int)(CubemapFace.PositiveX);
            faceMask |= 1 << (int)(CubemapFace.NegativeX);
            faceMask |= 1 << (int)(CubemapFace.PositiveY);
            faceMask |= 1 << (int)(CubemapFace.NegativeY);
            faceMask |= 1 << (int)(CubemapFace.PositiveZ);
            faceMask |= 1 << (int)(CubemapFace.NegativeZ);
            return CreateCube(width, height, faceMask);
        }

        /// <summary>
        /// Creates new instance of <see cref="SensorRenderTarget"/> and allocates required resources. Uses cube map.
        /// </summary>
        /// <param name="width">Width (in pixels) of the texture.</param>
        /// <param name="height">Height (in pixels) of the texture.</param>
        /// <param name="faceMask">Mask for cubemap faces that should be rendered.</param>
        public static SensorRenderTarget CreateCube(int width, int height, int faceMask)
        {
            var instance = new SensorRenderTarget(width, height, true) {CubeFaceMask = faceMask};
            return instance;
        }

        /// <summary>
        /// Performs blit from TextureArray RT to Texture2D RT that can be used for UI or GPU readback.
        /// </summary>
        /// <param name="parameters">Blit parameters.</param>
        /// <param name="source">Blit source (TextureArray <see cref="RTHandle"/>)</param>
        /// <param name="destination">Blit source (Texture2D <see cref="RTHandle"/>)</param>
        /// <param name="cmd">Used command buffer.</param>
        private static void BlitFinalCameraTexture(BlitParams parameters, RTHandle source, RTHandle destination, CommandBuffer cmd)
        {
            var scaleBias = new Vector4(parameters.viewport.width / source.rt.width, parameters.viewport.height / source.rt.height, 0.0f, 0.0f);

            BlitPropertyBlock.SetTexture(BlitTextureProperty, source);
            BlitPropertyBlock.SetVector(BlitScaleBiasProperty, scaleBias);
            BlitPropertyBlock.SetFloat(BlitMipLevelProperty, 0);
            BlitPropertyBlock.SetInt(BlitTexArraySliceProperty, parameters.srcTexArraySlice);
            HDUtils.DrawFullScreen(cmd, parameters.viewport, parameters.blitMaterial, destination, BlitPropertyBlock, 0, parameters.dstTexArraySlice);
        }

        /// <summary>
        /// Allocates color and depth textures for 2D render target.
        /// </summary>
        private void Alloc2D(GraphicsFormat colorFormat, bool uiVisible)
        {
            colorRt = RTHandles.Alloc(
                currentWidth,
                currentHeight,
                1,
                DepthBits.None,
                colorFormat,
                dimension: TextureXR.dimension,
                useDynamicScale: true,
                enableRandomWrite: true,
                name: "SRT_Color",
                wrapMode: TextureWrapMode.Clamp);

            depthRt = RTHandles.Alloc(
                currentWidth,
                currentHeight,
                TextureXR.slices,
                DepthBits.Depth32,
                GraphicsFormat.R32_UInt,
                dimension: TextureXR.dimension,
                useDynamicScale: true,
                name: "SRT_Depth",
                wrapMode: TextureWrapMode.Clamp);

            if (uiVisible)
            {
                uiRt = RTHandles.Alloc(
                    currentWidth,
                    currentHeight,
                    1,
                    DepthBits.None,
                    colorFormat,
                    dimension: TextureDimension.Tex2D,
                    useDynamicScale: true,
                    enableRandomWrite: true,
                    name: "SRT_UI",
                    wrapMode: TextureWrapMode.Clamp);
            }
        }

        /// <summary>
        /// Allocates color and depth textures for cube render target.
        /// </summary>
        private void AllocCube()
        {
            colorRt = RTHandles.Alloc(
                currentWidth,
                currentHeight,
                1,
                DepthBits.None,
                GraphicsFormat.R8G8B8A8_UNorm,
                dimension: TextureDimension.Cube,
                useDynamicScale: true,
                name: "SRT_Color",
                wrapMode: TextureWrapMode.Clamp);

            depthRt = RTHandles.Alloc(
                currentWidth,
                currentHeight,
                1,
                DepthBits.Depth32,
                GraphicsFormat.R32_UInt,
                dimension: TextureDimension.Cube,
                useDynamicScale: true,
                name: "SRT_Depth",
                wrapMode: TextureWrapMode.Clamp);
        }

        /// <summary>
        /// Checks if this instance is still valid for usage.
        /// </summary>
        /// <param name="width">Expected width (in pixels) of the texture.</param>
        /// <param name="height">Expected height (in pixels) of the texture.</param>
        /// <returns>True if instance is valid, false otherwise.</returns>
        public bool IsValid(int width, int height)
        {
            var sizeMatches = currentWidth == width && currentHeight == height;
            var created = colorRt != null && colorRt.rt.IsCreated() && depthRt != null && depthRt.rt.IsCreated();

            return sizeMatches && created;
        }

        /// <summary>
        /// Releases all native resources allocated by this instance.
        /// </summary>
        public void Release()
        {
            if (colorRt != null)
            {
                RTHandles.Release(colorRt);
                colorRt = null;
            }

            if (depthRt != null)
            {
                RTHandles.Release(depthRt);
                depthRt = null;
            }

            if (uiRt != null)
            {
                RTHandles.Release(uiRt);
                uiRt = null;
            }
        }

        /// <summary>
        /// Performs blit from TextureArray RT to Texture2D RT that can be used for UI or GPU readback.
        /// </summary>
        /// <param name="cmd">Used command buffer.</param>
        /// <param name="camera">Camera for which blit is performed.</param>
        public void BlitTo2D(CommandBuffer cmd, HDCamera camera)
        {
            var parameters = new BlitParams
            {
                viewport = new Rect(0, 0, currentWidth, currentHeight),
                srcTexArraySlice = -1,
                dstTexArraySlice = -1,
            };

            parameters.blitMaterial = HDUtils.GetBlitMaterial(TextureXR.useTexArray ? TextureDimension.Tex2DArray : TextureDimension.Tex2D, parameters.srcTexArraySlice >= 0);
            BlitFinalCameraTexture(parameters, colorRt, uiRt, cmd);
        }
    }
}