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
    Action<Collider> triggerEnter;
    Action<Collider> triggerStay;
    Action<Collider> triggerExit;
    LayerMask mask;

    public void SetCallbacks(Action<Collider> enter, Action<Collider> stay, Action<Collider> exit)
    {
        triggerEnter = enter;
        triggerStay = stay;
        triggerExit = exit;
        mask = LayerMask.GetMask("NPC", "Pedestrian", "Bicycle");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerEnter == null || ((mask.value >> other.gameObject.layer) & 1) == 0)
        {
            return;
        }

        triggerEnter(other);
    }

    void OnTriggerStay(Collider other)
    {
        if (triggerStay == null || ((mask.value >> other.gameObject.layer) & 1) == 0)
        {
            return;
        }

        triggerStay(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (triggerExit == null || ((mask.value >> other.gameObject.layer) & 1) == 0)
        {
            return;
        }

        triggerExit(other);
    }
}
