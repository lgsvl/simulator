/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;

public class WindshieldMaterialQualitySwitcher : RenderQualitySwitchListener
{
    public Material transparentMat;
    public Material opaqueMat;
    public List<Renderer> inspectRenderers;

    protected override void Start() => base.Start();

    protected override void QualitySwitch(bool highQuality)
    {
        Material matA, matB;
        if (highQuality)
        {
            matA = opaqueMat;
            matB = transparentMat;
        }
        else
        {
            matA = transparentMat;
            matB = opaqueMat;
        }

        foreach (var rend in inspectRenderers)
        {
            for (int i = 0; i < rend.sharedMaterials.Length; i++)
            {
                var sharedMat = rend.sharedMaterials[i];
                if (sharedMat == matA)
                {
                    var newMats = new Material[rend.materials.Length];
                    for (int j = 0; j < newMats.Length; j++)
                    {
                        if (j == i)
                        {
                            newMats[j] = matB;
                        }
                        else
                        {
                            newMats[j] = rend.sharedMaterials[j];
                        }
                    }
                    rend.materials = newMats;
                }
            }
        }
    }
}
