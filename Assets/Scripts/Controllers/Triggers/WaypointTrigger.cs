/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaypointTrigger
{
    public List<TriggerEffector> Effectors = new List<TriggerEffector>();

    public IEnumerator Apply(ITriggerAgent triggerAgent, Action callback = null)
    {
        //Run effectors parallel and wait for all of them to finish 
        var coroutines = new Coroutine[Effectors.Count];
        for (int i = 0; i < Effectors.Count; i++)
            coroutines[i] = triggerAgent.StartCoroutine(Effectors[i].Apply(triggerAgent));

        for (int i = 0; i < coroutines.Length; i++)
            yield return coroutines[i];

        callback?.Invoke();
    }
}
