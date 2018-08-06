using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrakeLightController : MonoBehaviour {
    public List<Renderer> brakeLightRenderers;

    private void Start()
    {
        if (brakeLightRenderers.Count < 1)
        {
            brakeLightRenderers = new List<Renderer>();
            var rends = GetComponentsInChildren<Renderer>();
            foreach (var rend in rends)
            {
                brakeLightRenderers.Add(rend);
            }
        }
    }

    public void SetBrakeLight(bool state)
    {
        if (state)
        {
            foreach (var rend in brakeLightRenderers)
            {
                foreach (var mat in rend.materials)
                {
                    mat.EnableKeyword("_EMISSION");
                }
            }
        }
        else
        {
            foreach (var rend in brakeLightRenderers)
            {
                foreach (var mat in rend.materials)
                {
                    mat.DisableKeyword("_EMISSION");
                }
            }
        }
    }
}
