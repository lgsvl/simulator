/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Simulator.Utilities
{
    public class PostProcess
    {
        private ComputeShader postProcessShader;
        private int plumBobDistortionKernel;
        private int unifiedProjectionDistortionKernel;
        private int linearToGammaKernel;
        private float distortionFactor;

        public PostProcess()
        {
            postProcessShader = GameObject.Instantiate(RuntimeSettings.Instance.PostProcess);
            plumBobDistortionKernel = postProcessShader.FindKernel("PlumBobDistortion");
            unifiedProjectionDistortionKernel = postProcessShader.FindKernel("UnifiedProjectionDistortion");
            linearToGammaKernel = postProcessShader.FindKernel("LinearToGamma");
        }

        // Refer https://wiki.lgsvl.com/display/AUT/Lens+Distortion for more details.
        public void InitDistortion(List<float> distortionParameters, float frustumWidth, float frustumHeight, float xi)
        { 
            double a1, a2, a3, a4;
            a1 = distortionParameters[0];
            a2 = distortionParameters[1];
            a3 = distortionParameters[2];
            a4 = distortionParameters[3];

            Vector2 center = new Vector2(frustumWidth / 2, frustumHeight / 2);
            float r2 = Vector2.Dot(center, center);
            float r4 = r2 * r2;
            distortionFactor = (float)(a1 * r2 + a2 * r4 + a3 * r2 * r4 + a4 * r4 * r4);

            double a12 = a1 * a1;

            float b1, b2, b3, b4;
            b1 = (float)(-a1);
            b2 = (float)(3 * a12 - a2);
            b3 = (float)(8 * a1 * a2 - 12 * a12 * a1 - a3);
            b4 = (float)(55 * a12 * a12 + 10 * a1 * a3 - 55 * a12 * a2 + 5 * a2 * a2 - a4);

            postProcessShader.SetFloat("enlargeFactorA", 1 + distortionFactor);
            postProcessShader.SetFloat("enlargeFactorB", -distortionFactor / 2);
            postProcessShader.SetFloat("frustumWidth", frustumWidth);
            postProcessShader.SetFloat("frustumHeight", frustumHeight);

            postProcessShader.SetFloat("b1", b1);
            postProcessShader.SetFloat("b2", b2);
            postProcessShader.SetFloat("b3", b3);
            postProcessShader.SetFloat("b4", b4);

            postProcessShader.SetFloat("xi", xi);
        }

        public void CalculateRenderTextureSize(int width, int height, out int newWidth, out int newHeight)
        {
            newWidth = (int)(width / (1 + distortionFactor));
            newHeight = (int)(height / (1 + distortionFactor));
        }

        public void LinearToGamma(RenderTexture inputTexture, RenderTexture distortedTexture)
        {
            postProcessShader.SetTexture(linearToGammaKernel, "_InputTexture", inputTexture);
            postProcessShader.SetTexture(linearToGammaKernel, "_DistortedTexture", distortedTexture);

            postProcessShader.Dispatch(linearToGammaKernel, (distortedTexture.width + 7) / 8, (distortedTexture.height + 7) / 8, 1);
        }

        public void PlumbBobDistort(RenderTexture inputTexture, RenderTexture distortedTexture)
        {
            postProcessShader.SetTexture(plumBobDistortionKernel, "_InputTexture", inputTexture);
            postProcessShader.SetTexture(plumBobDistortionKernel, "_DistortedTexture", distortedTexture);

            postProcessShader.Dispatch(plumBobDistortionKernel, (distortedTexture.width +7) / 8, (distortedTexture.height + 7) / 8, 1);
        }

        public void UnifiedProjectionDistort(RenderTexture inputCubemapTexture, RenderTexture distortedTexture)
        {
            postProcessShader.SetTexture(unifiedProjectionDistortionKernel, "_InputCubemapTexture", inputCubemapTexture);
            postProcessShader.SetTexture(unifiedProjectionDistortionKernel, "_DistortedTexture", distortedTexture);

            postProcessShader.Dispatch(unifiedProjectionDistortionKernel, (distortedTexture.width + 7) / 8, (distortedTexture.height + 7) / 8, 1);
        }
    }
}