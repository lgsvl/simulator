/**
 * Copyright (c) 2019-2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Sensors
{
    using UnityEngine;
    using UnityEngine.Experimental.Rendering;
    using UnityEngine.Rendering;

    /// <summary>
    /// <para>Class wrapping render target management for camera-based sensors.</para>
    /// <para>Can be backed by either a default <see cref="RenderTexture"/> or a set of separate color and depth
    /// <see cref="RTHandle"/> if compatibility with HDRP or depth sampling is required.</para>
    /// </summary>
    public class SensorRenderTarget
    {
        private RTHandle colorRt;
        private RTHandle depthRt;

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

        private SensorRenderTarget(int width, int height, bool cube, GraphicsFormat colorFormat = GraphicsFormat.R8G8B8A8_UNorm)
        {
            currentWidth = width;
            currentHeight = height;
            IsCube = cube;

            if (cube)
                AllocCube();
            else
                Alloc2D(colorFormat);
        }

        public static implicit operator RenderTexture(SensorRenderTarget target) => target.ColorTexture;

        /// <summary>
        /// Creates new instance of <see cref="SensorRenderTarget"/> and allocates required resources. Uses 2D texture.
        /// </summary>
        /// <param name="width">Width (in pixels) of the texture.</param>
        /// <param name="height">Height (in pixels) of the texture.</param>
        public static SensorRenderTarget Create2D(int width, int height)
        {
            var instance = new SensorRenderTarget(width, height, false);
            return instance;
        }

        /// <summary>
        /// Creates new instance of <see cref="SensorRenderTarget"/> and allocates required resources. Uses 2D texture.
        /// </summary>
        /// <param name="width">Width (in pixels) of the texture.</param>
        /// <param name="height">Height (in pixels) of the texture.</param>
        /// /// <param name="colorFormat">GraphicsFormat of the color texture.</param>
        public static SensorRenderTarget Create2D(int width, int height, GraphicsFormat colorFormat)
        {
            var instance = new SensorRenderTarget(width, height, false, colorFormat);
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
        /// Allocates color and depth textures for 2D render target.
        /// </summary>
        private void Alloc2D(GraphicsFormat colorFormat)
        {
            colorRt = RTHandles.Alloc(
                currentWidth,
                currentHeight,
                1,
                DepthBits.None,
                colorFormat,
                dimension: TextureDimension.Tex2D,
                useDynamicScale: true,
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
        }
    }
}