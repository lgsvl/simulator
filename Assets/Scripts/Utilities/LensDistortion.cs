/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;

namespace Simulator.Utilities
{
    using System;
    using UnityEngine.Rendering;

    public class LensDistortion
    {
        private static readonly int InputTextureProperty = Shader.PropertyToID("_InputTexture");
        private static readonly int InputCubemapTextureProperty = Shader.PropertyToID("_InputCubemapTexture");
        private static readonly int DistortedTextureProperty = Shader.PropertyToID("_DistortedTexture");
        private static readonly int TexSizeProperty = Shader.PropertyToID("_TexSize");

        public int ActualWidth { get; private set; }
        public int ActualHeight { get; private set; }

        private readonly ComputeShader lensDistortionShader;
        private readonly int plumBobDistortionKernel;
        private readonly int unifiedProjectionDistortionKernel;
        private float distortionFactor;

        private Vector4 initDistortionVector;
        private float initFov;
        private float initXi;
        private int initWidth;
        private int initHeight;

        public LensDistortion()
        {
            lensDistortionShader = UnityEngine.Object.Instantiate(RuntimeSettings.Instance.LensDistortion);
            plumBobDistortionKernel = lensDistortionShader.FindKernel("PlumBobDistortion");
            unifiedProjectionDistortionKernel = lensDistortionShader.FindKernel("UnifiedProjectionDistortion");
        }

        public bool IsValid(List<float> distortionParameters, float fieldOfView, float xi, int width, int height)
        {
            var distortionVector = new Vector4(distortionParameters[0], distortionParameters[1],
                distortionParameters[2], distortionParameters[3]);

            return initDistortionVector == distortionVector && Mathf.Approximately(fieldOfView, initFov) &&
                   Mathf.Approximately(xi, initXi) && initWidth == width && initHeight == height;
        }

        // Refer https://wiki.lgsvl.com/display/AUT/Lens+Distortion for more details.
        public void InitDistortion(List<float> distortionParameters, float fieldOfView, float xi, int width, int height)
        { 
            initDistortionVector = new Vector4(distortionParameters[0], distortionParameters[1],
                distortionParameters[2], distortionParameters[3]);

            initFov = fieldOfView;
            initXi = xi;
            initWidth = width;
            initHeight = height;

            var frustumHeight = 2 * Mathf.Tan(fieldOfView / 2 * Mathf.Deg2Rad);
            var frustumWidth = frustumHeight * width / height;

            double a1 = distortionParameters[0];
            double a2 = distortionParameters[1];
            double a3 = distortionParameters[2];
            double a4 = distortionParameters[3];

            var center = new Vector2(frustumWidth / 2, frustumHeight / 2);
            var r2 = Vector2.Dot(center, center);
            var r4 = r2 * r2;
            distortionFactor = (float)(a1 * r2 + a2 * r4 + a3 * r2 * r4 + a4 * r4 * r4);

            var a12 = a1 * a1;

            var b1 = (float)(-a1);
            var b2 = (float)(3 * a12 - a2);
            var b3 = (float)(8 * a1 * a2 - 12 * a12 * a1 - a3);
            var b4 = (float)(55 * a12 * a12 + 10 * a1 * a3 - 55 * a12 * a2 + 5 * a2 * a2 - a4);

            ActualWidth = (int) (width / (1 + distortionFactor));
            ActualHeight = (int) (height / (1 + distortionFactor));

            if (ActualWidth <= 0 || ActualHeight <= 0)
                throw new Exception("Distortion parameters cause texture size invalid (<= 0).");

            lensDistortionShader.SetFloat("enlargeFactorA", 1 + distortionFactor);
            lensDistortionShader.SetFloat("enlargeFactorB", -distortionFactor / 2);
            lensDistortionShader.SetFloat("frustumWidth", frustumWidth);
            lensDistortionShader.SetFloat("frustumHeight", frustumHeight);

            lensDistortionShader.SetFloat("b1", b1);
            lensDistortionShader.SetFloat("b2", b2);
            lensDistortionShader.SetFloat("b3", b3);
            lensDistortionShader.SetFloat("b4", b4);

            lensDistortionShader.SetFloat("xi", xi);
        }

        public void PlumbBobDistort(CommandBuffer cmd, RenderTexture inputTexture, RenderTexture distortedTexture)
        {
            cmd.SetComputeTextureParam(lensDistortionShader, plumBobDistortionKernel, InputTextureProperty, inputTexture);
            cmd.SetComputeTextureParam(lensDistortionShader, plumBobDistortionKernel, DistortedTextureProperty, distortedTexture);
            cmd.SetComputeVectorParam(lensDistortionShader, TexSizeProperty, new Vector4(distortedTexture.width, distortedTexture.height, 1f / distortedTexture.width, 1f / distortedTexture.height));

            cmd.DispatchCompute(lensDistortionShader, plumBobDistortionKernel, (distortedTexture.width +7) / 8, (distortedTexture.height + 7) / 8, 1);
        }

        public void UnifiedProjectionDistort(CommandBuffer cmd, RenderTexture inputCubemapTexture, RenderTexture distortedTexture)
        {
            cmd.SetComputeTextureParam(lensDistortionShader, unifiedProjectionDistortionKernel, InputCubemapTextureProperty, inputCubemapTexture);
            cmd.SetComputeTextureParam(lensDistortionShader, unifiedProjectionDistortionKernel, DistortedTextureProperty, distortedTexture);
            cmd.SetComputeVectorParam(lensDistortionShader, TexSizeProperty, new Vector4(distortedTexture.width, distortedTexture.height, 1f / distortedTexture.width, 1f / distortedTexture.height));

            cmd.DispatchCompute(lensDistortionShader, unifiedProjectionDistortionKernel, (distortedTexture.width + 7) / 8, (distortedTexture.height + 7) / 8, 1);
        }
    }
}