/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using SimpleJSON;
using UnityEngine;

public class WaitingPointEffector : TriggerEffector
{
    public override string TypeName { get; } = "WaitingPoint";
    public Vector3 ActivatorPoint;
    public float PointRadius = 2.0f;
    
    public override IEnumerator Apply(ITriggerAgent agent)
    {
        //Make parent npc wait until any ego is closer than the max distance
        var lowestDistance = float.PositiveInfinity;
        do
        {
            var egos = SimulatorManager.Instance.AgentManager.ActiveAgents;
            foreach (var ego in egos)
            {
                var distance = Vector3.Distance(ActivatorPoint, ego.AgentGO.transform.position);
                if (distance < lowestDistance)
                    lowestDistance = distance;
            }

            yield return null;
        } while (lowestDistance > PointRadius);
    }

    public override void DeserializeProperties(JSONNode jsonData)
    {
        ActivatorPoint = jsonData["activator_point"].ReadVector3();
        PointRadius = jsonData["point_radius"];
    }

    public override void SerializeProperties(JSONNode jsonData)
    {
        var activatorNode = new JSONObject().WriteVector3(ActivatorPoint);
        jsonData.Add("activator_point", activatorNode);
        jsonData.Add("point_radius", new JSONNumber(PointRadius));
    }
}
