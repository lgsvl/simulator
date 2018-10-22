/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadSegmentDataComponent : MonoBehaviour
{
    public int lane { get; set; }
    public float bounds { get; set; }

    private void Start()
    {
        bounds = GetComponent<MeshRenderer>().bounds.size.x;
    }
}
