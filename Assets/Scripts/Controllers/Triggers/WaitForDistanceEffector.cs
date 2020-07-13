/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using UnityEngine;

public class WaitForDistanceEffector : TriggerEffector
{
    public override string TypeName { get; } = "WaitForDistance";
    
    public override IEnumerator Apply(NPCController parentNPC)
    {
        //Make parent npc wait until any ego is closer than the distance "Value"
        float lowestDistance;
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
        } while (lowestDistance > Value);
    }
}
