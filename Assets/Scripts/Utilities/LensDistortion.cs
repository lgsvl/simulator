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
    public class LensDistortion 
    {
        private ComputeShader lensDistortionShader;
        private int plumBobDistortionKernel, unifiedProjectionDistortionKernel;
        private float distortionFactor;

        public LensDistortion()
        {
            lensDistortionShader = GameObject.Instantiate(RuntimeSettings.Instance.LensDistortion);
            plumBobDistortionKernel = lensDistortionShader.FindKernel("PlumBobDistortion");
            unifiedProjectionDistortionKernel = lensDistortionShader.FindKernel("UnifiedProjectionDistortion");
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

        public void CalculateRenderTextureSize(int width, int height, out int newWidth, out int newHeight)
        {
            newWidth = (int)(width / (1 + distortionFactor));
            newHeight = (int)(height / (1 + distortionFactor));
        }

        public void PlumbBobDistort(RenderTexture inputTexture, RenderTexture distortedTexture)
        {
            lensDistortionShader.SetTexture(plumBobDistortionKernel, "_InputTexture", inputTexture);
            lensDistortionShader.SetTexture(plumBobDistortionKernel, "_DistortedTexture", distortedTexture);

            lensDistortionShader.Dispatch(plumBobDistortionKernel, (distortedTexture.width +7) / 8, (distortedTexture.height + 7) / 8, 1);
        }

        public void UnifiedProjectionDistort(RenderTexture inputCubemapTexture, RenderTexture distortedTexture)
        {
            lensDistortionShader.SetTexture(unifiedProjectionDistortionKernel, "_InputCubemapTexture", inputCubemapTexture);
            lensDistortionShader.SetTexture(unifiedProjectionDistortionKernel, "_DistortedTexture", distortedTexture);

            lensDistortionShader.Dispatch(unifiedProjectionDistortionKernel, (distortedTexture.width + 7) / 8, (distortedTexture.height + 7) / 8, 1);
        }
    }
}