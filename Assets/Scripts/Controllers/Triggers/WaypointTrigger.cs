/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

public class WaypointTrigger
{
    protected List<TriggerEffector> effectors = new List<TriggerEffector>();

    public event Action<TriggerEffector> EffectorAdded;

    public event Action<TriggerEffector> EffectorRemoved;

    public List<TriggerEffector> Effectors => effectors;

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

    public void AddEffector(TriggerEffector effector)
    {
        Effectors.Add(effector);
        EffectorAdded?.Invoke(effector);
    }

    public void RemoveEffector(TriggerEffector effector)
    {
        Effectors.Remove(effector);
        EffectorRemoved?.Invoke(effector);
    }

    public JSONNode SerializeTrigger()
    {
        var triggerNode = new JSONObject();
        var effectorsArray = new JSONArray();
        ;
        triggerNode.Add("effectors", effectorsArray);
        foreach (var effector in Effectors)
        {
            var effectorNode = new JSONObject();
            effectorNode.Add("typeName", new JSONString(effector.TypeName));
            var parameters = new JSONObject();
            effectorNode.Add("parameters", parameters);
            effector.SerializeProperties(parameters);
            effectorsArray.Add(effectorNode);
        }

        return triggerNode;
    }

    public static WaypointTrigger DeserializeTrigger(JSONNode data)
    {
        if (data == null)
            return null;
        var effectorsNode = data["effectors"].AsArray;
        var trigger = new WaypointTrigger();
        for (var i = 0; i < effectorsNode.Count; i++)
        {
            var typeName = effectorsNode[i]["typeName"];
            if (typeName == null)
                typeName = effectorsNode[i]["type_name"];
            var newEffector = TriggersManager.GetEffectorOfType(typeName);
            if (newEffector == null)
            {
                Debug.LogWarning($"Could not find a trigger effector with type name: {typeName}.");
                continue;
            }

            newEffector.DeserializeProperties(effectorsNode[i]["parameters"]);
            trigger.AddEffector(newEffector);
        }

        return trigger;
    }
}