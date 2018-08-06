using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmokeDecay : MonoBehaviour {
    public ParticleSystem smokeParticles;
    public float initialConstEmitRate = 200f;
    public float decaySpeedScaler = 1f;

    void Start()
    {
        var emissionModule = smokeParticles.emission;
        var curConst = smokeParticles.emission.rateOverTime.constant;
        emissionModule.rateOverTime = new ParticleSystem.MinMaxCurve(initialConstEmitRate);
    }

    void Update()
    {
        var emissionModule = smokeParticles.emission;
        var curConst =  smokeParticles.emission.rateOverTime.constant;
        if (curConst < 0.001f)
        {
            Destroy(gameObject);
        }

        if (curConst > 10f)
        {
            emissionModule.rateOverTime = new ParticleSystem.MinMaxCurve(curConst -= Time.deltaTime * decaySpeedScaler);
        }
        else
        {
            emissionModule.rateOverTime = new ParticleSystem.MinMaxCurve(curConst -= Time.deltaTime * decaySpeedScaler * 0.25f);
        }
        
    }
}
