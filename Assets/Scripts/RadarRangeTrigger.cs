/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

public class RadarRangeTrigger : MonoBehaviour
{
    public delegate void Callback(Collider other);
    Callback callback;

    public void SetCallback(Callback callback)
    {
        this.callback = callback;
    }

    void OnTriggerStay(Collider other)
    {
        callback(other);
    }
}
