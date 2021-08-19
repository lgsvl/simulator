/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using Simulator;
using Simulator.Controllable;
using Simulator.Utilities;
using UnityEngine;

public class ControlTriggerEffector : TriggerEffector
{
    public override string TypeName { get; } = "ControlTrigger";

    public override AgentType[] UnsupportedAgentTypes { get; } = {AgentType.Unknown};

    public readonly List<string> ControllablesUIDs = new List<string>();

    public List<ControlAction> ControlPolicy;

    public override object Clone()
    {
        var clone = new ControlTriggerEffector {ControlPolicy = ControlPolicy};
        foreach (var uid in ControllablesUIDs)
            clone.ControllablesUIDs.Add(uid);
        return clone;
    }

    public override IEnumerator Apply(ITriggerAgent agent)
    {
        if (ControllablesUIDs == null || ControllablesUIDs.Count == 0)
            yield break;
        if (Loader.IsInScenarioEditor)
        {
            Debug.LogWarning(
                $"Visual Scenario Editor does not support the {GetType().Name}.");
            yield break;
        }
        foreach (var uid in ControllablesUIDs)
        {
            if (!SimulatorManager.Instance.ControllableManager.TryGetControllable(uid, out var controllable)) continue;
            controllable.Control(ControlPolicy);
        }
    }

    public override void DeserializeProperties(JSONNode jsonData)
    {
        ControllablesUIDs.Clear();
        var controllablesNode = jsonData["controllablesUIDs"] as JSONArray;
        if (controllablesNode != null)
            foreach (var nodeChild in controllablesNode.Children)
                ControllablesUIDs.Add(nodeChild);
        ControlPolicy = Utility.ParseControlPolicy(null, jsonData["controlPolicy"], out _);
    }

    public override void SerializeProperties(JSONNode jsonData)
    {
        var controllablesNode = new JSONArray();
        foreach (var uid in ControllablesUIDs)
            controllablesNode.Add(uid);
        jsonData.Add("controllablesUIDs", controllablesNode);
        jsonData.Add("controlPolicy", Utility.SerializeControlPolicy(ControlPolicy));
    }
}