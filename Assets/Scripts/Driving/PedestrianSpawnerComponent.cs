/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianSpawnerComponent : MonoBehaviour
{
    public Transform target01 { get; set; }
    public Transform target02 { get; set; }

    private void Awake()
    {
        target01 = transform.GetChild(0);
        target02 = transform.GetChild(1);
    }

    public Vector3 GetPositionBetweenTargets()
    {
        return new Vector3(Random.Range(target01.position.x, target02.position.x), target01.position.y, Random.Range(target01.position.z, target02.position.z));
    }
}
