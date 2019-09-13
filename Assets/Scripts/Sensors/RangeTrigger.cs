/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using UnityEngine;

public class RangeTrigger : MonoBehaviour
{
    Action<Collider> triggerStay;
    LayerMask mask;

    public void SetCallbacks(Action<Collider> stay)
    {
        triggerStay = stay;
        mask = LayerMask.GetMask("GroundTruth");
    }

    void OnTriggerStay(Collider other)
    {
        if (triggerStay == null || ((mask.value >> other.gameObject.layer) & 1) == 0)
        {
            return;
        }

        triggerStay(other);
    }
}
