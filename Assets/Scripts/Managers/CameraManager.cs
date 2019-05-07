/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public GameObject simulatorCameraPrefab;
    public GameObject simulatorCamera { get; private set; }

    private void Start()
    {
        Debug.Log("Init Camera Manager");
        simulatorCamera = Instantiate(simulatorCameraPrefab, transform);
    }
}
