/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using SimpleJSON;
using Simulator;
using UnityEngine;

public class WaitForDistanceEffector : TriggerEffector
{
    public override string TypeName { get; } = "WaitForDistance";

    public float MaxDistance = 5.0f;

    public override object Clone()
    {
        var clone = new WaitForDistanceEffector {MaxDistance = MaxDistance};
        return clone;
    }

    public override IEnumerator Apply(ITriggerAgent agent)
    {
        if (Loader.IsInScenarioEditor)
        {
            Debug.LogWarning(
                $"Visual Scenario Editor does not support the {GetType().Name}.");
            yield break;
        }
        
        //Make parent npc wait until any ego is closer than the max distance
        float lowestDistance;
        do
        {
            yield return null;
            lowestDistance = float.PositiveInfinity;
            var egos = SimulatorManager.Instance.AgentManager.ActiveAgents;
            foreach (var ego in egos)
            {
                var distance = Vector3.Distance(agent.AgentTransform.position, ego.AgentGO.transform.position);
                if (distance < lowestDistance)
                    lowestDistance = distance;
            }

            yield return null;
        } while (lowestDistance > MaxDistance);
    }

    public override void SerializeProperties(JSONNode jsonData)
    {
        jsonData.Add("maxDistance", new JSONNumber(MaxDistance));
    }

    public override void DeserializeProperties(JSONNode jsonData)
    {
        var maxDistance = jsonData["maxDistance"];
        if (maxDistance == null)
            maxDistance = jsonData["max_distance"];
        MaxDistance = maxDistance;
    }
}