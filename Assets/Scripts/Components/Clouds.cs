using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clouds : MonoBehaviour
{
    private Renderer cloudsRenderer;

    private void Awake()
    {
        cloudsRenderer = GetComponent<Renderer>();
    }

    private void Update()
    {
        cloudsRenderer.material.SetFloat("_ScaledTime", Time.timeSinceLevelLoad);
    }
}
