/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class PedestrianSpawnerComponent : MonoBehaviour
{
    public List<Transform> targetTransforms = new List<Transform>();
    public List<Vector3> targets = new List<Vector3>();
    private float targetRange = 2f;

    private void Awake()
    {
        foreach (Transform child in transform)
        {
            targetTransforms.Add(child);
        }
        targetTransforms = targetTransforms.OrderBy(x => x.name).ToList();
        foreach (var item in targetTransforms)
        {
            targets.Add(item.position);
        }
    }
}
