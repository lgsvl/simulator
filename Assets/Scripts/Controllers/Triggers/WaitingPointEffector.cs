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

public class WaitingPointEffector : TriggerEffector
{
    public override string TypeName { get; } = "WaitingPoint";

    public Vector3 ActivatorPoint;

    public float PointRadius = 2.0f;

    public override object Clone()
    {
        var clone = new WaitingPointEffector {ActivatorPoint = ActivatorPoint, PointRadius = PointRadius};
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

    public override void SerializeProperties(JSONNode jsonData)
    {
        var activatorNode = new JSONObject().WriteVector3(ActivatorPoint);
        jsonData.Add("activatorPoint", activatorNode);
        jsonData.Add("pointRadius", new JSONNumber(PointRadius));
    }

    public override void DeserializeProperties(JSONNode jsonData)
    {
        var activatorPoint = jsonData["activatorPoint"];
        if (activatorPoint == null)
            activatorPoint = jsonData["activator_point"];
        ActivatorPoint = activatorPoint.ReadVector3();

        var pointRadius = jsonData["pointRadius"];
        if (pointRadius == null)
            pointRadius = jsonData["point_radius"];
        PointRadius = pointRadius;
    }
}