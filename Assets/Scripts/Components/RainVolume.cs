/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class RainVolume : MonoBehaviour
{
    public ParticleSystem Init(ParticleSystem pfx, int seed)
    {
        var particleSystem = Instantiate(pfx, transform);
        particleSystem.randomSeed = (uint)seed;
        particleSystem.Play();
        var shape = particleSystem.shape;
        shape.scale = new Vector3(100f, 100f, 1f);
        var emission = particleSystem.emission;
        emission.rateOverTime = 0f;
        return particleSystem;
    }

    // TODO mem issue
    public VisualEffect Init(VisualEffect vfx, int seed)
    {
        var effect = Instantiate(vfx, transform);
        return effect;
    }
}
