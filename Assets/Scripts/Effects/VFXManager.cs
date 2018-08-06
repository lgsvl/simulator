using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct VFXProperty
{
    public GameObject sourceGo;
}

[System.Serializable]
public struct VFXSettings
{
    public VFXProperty carCollisionParticleVFX;
}

[System.Serializable]
public struct VFXSnapshot
{
    public float time;
    public Vector3 pos;
}

public class VFXManager : UnitySingleton<VFXManager>
{
    public enum VFXType
    {
        CarCollisionParticles,
    }
    public VFXSettings settings;

    private Dictionary<VFXType, VFXSnapshot> lastVFXSnapshots;
    private const float smallestSameSpotVFXTimeInterval = 0.05f;

    private void Start()
    {
        lastVFXSnapshots = new Dictionary<VFXType, VFXSnapshot>();
    }

    public void SpawnVFX(GameObject VFXSourceGo, Vector3 pos, VFXType vfxType, Vector3 extraVel, float delayTime, float lifetime)
    {
        if (delayTime == 0)
        {
            //If same type of vfx happen too fast and multiple times in the same spot then only spawn one of them at this moment
            if (lastVFXSnapshots.ContainsKey(vfxType) && (lastVFXSnapshots[vfxType].pos - pos).magnitude < 0.05f && Time.time - lastVFXSnapshots[vfxType].time < smallestSameSpotVFXTimeInterval)
            {
                return;
            }

            //Record a specific vfx's snapshot
            if (lastVFXSnapshots.ContainsKey(vfxType))
            {
                lastVFXSnapshots[vfxType] = new VFXSnapshot() { time = Time.time, pos = pos };                
            }
            else
            {
                lastVFXSnapshots.Add(vfxType, new VFXSnapshot() { time = Time.time, pos = pos });
            }

            var go = GameObject.Instantiate(VFXSourceGo, pos, Quaternion.identity);
            var partSyses = go.GetComponentsInChildren<ParticleSystem>();
            foreach (var pSys in partSyses)
            {
                var emisModule = pSys.emission;
                var bursts = new ParticleSystem.Burst[emisModule.burstCount];
                emisModule.GetBursts(bursts);
                for (int i = 0; i < bursts.Length; i++)
                {
                    bursts[i].maxCount = (short)((float)bursts[i].maxCount * (1f + extraVel.magnitude * 0.5f));
                    bursts[i].minCount = (short)((float)bursts[i].minCount * (1f + extraVel.magnitude * 0.5f));
                }
                var main = pSys.main;
                main.startSpeedMultiplier = 1f + extraVel.magnitude;
            }     
            
            Destroy(go, lifetime);
        }
        else if (delayTime > 0)
        {
            StartCoroutine(SpawnVFX_Delay(VFXSourceGo, pos, vfxType, extraVel, delayTime, lifetime));
        }        
    }

    IEnumerator SpawnVFX_Delay(GameObject VFXSourceGo, Vector3 pos, VFXType vfxType, Vector3 extraVel, float delayTime, float lifetime)
    {
        yield return new WaitForSeconds(delayTime);
        SpawnVFX(VFXSourceGo, pos, vfxType, extraVel, 0, lifetime);
    }
}
