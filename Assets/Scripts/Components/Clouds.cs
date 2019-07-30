/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

namespace Simulator.Components
{
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
}
