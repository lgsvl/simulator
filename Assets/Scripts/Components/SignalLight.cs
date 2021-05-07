/**
 * Copyright (c) 2019-2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SignalLight : MonoBehaviour
{
    [System.Serializable]
    public class SignalLightData
    {
        public Color SignalColor = Color.red;
        public string SignalColorName = "red";
    }
    public List<SignalLightData> SignalLightDatas = new List<SignalLightData>();

    [HideInInspector]
    public Bounds Bounds;
    private List<Renderer> SignalLightRenderers = new List<Renderer>();
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissiveColorId = Shader.PropertyToID("_EmissiveColor");
    private static readonly int EmissiveExposureWeightId = Shader.PropertyToID("_EmissiveExposureWeight");

    private void Awake()
    {
        SetSignalLightRenderers();
    }

    private void SetSignalLightRenderers()
    {
        Bounds = new Bounds(transform.position, Vector3.zero);
        var renderers = GetComponentsInChildren<Renderer>().ToList();
        foreach (var r in renderers)
        {
            if (r.name.Contains("SignalLight"))
            {
                SignalLightRenderers.Add(r);
            }
            Bounds.Encapsulate(r.bounds);
        }

        if (SignalLightRenderers.Count == 0)
        {
            Debug.LogWarning("SignalLight cannot find child mesh renderers named 'SignalLight'", gameObject);
            return;
        }

        SignalLightRenderers = SignalLightRenderers.OrderBy(x => x.name).ToList();
        for (int i = 0; i < SignalLightRenderers.Count; i++)
        {
            SignalLightRenderers[i].material.SetColor(BaseColorId, SignalLightDatas[i].SignalColor);
            SignalLightRenderers[i].material.SetFloat(EmissiveExposureWeightId, 0.6f); // latest unity update requires this change from 1f
        }
    }

    public void SetSignalLightState(string state)
    {
        int index = -1;
        for (int i = 0; i < SignalLightDatas.Count; i++)
        {
            if (SignalLightDatas[i].SignalColorName == state)
            {
                index = i;
            }
        }
        if (index == -1)
        {
            Debug.LogWarning($"No signal color '{state}' found", gameObject);
            return;
        }

        if (SignalLightRenderers.Count == 0)
        {
            Debug.LogWarning("SignalLight has no mesh renderers to set light emission", gameObject);
            return;
        }

        foreach (var sig in SignalLightRenderers)
        {
            sig.material.SetVector(EmissiveColorId, Color.black);
        }
        SignalLightRenderers[index].material.SetVector(EmissiveColorId, Color.white * 250f);
    }
}
