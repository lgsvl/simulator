/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using SimpleJSON;
using UnityEngine;

public class WaitForDistanceEffector : TriggerEffector
{
    public override string TypeName { get; } = "WaitForDistance";
    public float MaxDistance = 5.0f;
    
    public override IEnumerator Apply(NPCController parentNPC)
    {
        //Make parent npc wait until any ego is closer than the max distance
        var lowestDistance = float.PositiveInfinity;
        do
        {
            yield return null;
            lowestDistance = float.PositiveInfinity;
            var egos = SimulatorManager.Instance.AgentManager.ActiveAgents;
            foreach (var ego in egos)
            {
                var distance = Vector3.Distance(parentNPC.transform.position, ego.AgentGO.transform.position);
                if (distance < lowestDistance)
                    lowestDistance = distance;
            }

            yield return null;
        } while (lowestDistance > MaxDistance);
    }

    public override void DeserializeProperties(JSONNode jsonData)
    {
        MaxDistance = jsonData["max_distance"];
    }

    public override void SerializeProperties(JSONNode jsonData)
    {
        jsonData.Add("max_distance", new JSONNumber(MaxDistance));
    }
}
