/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using SimpleJSON;
using UnityEngine;

public class TimeToCollisionEffector : TriggerEffector
{
    private const float TimeToCollisionLimit = float.PositiveInfinity;
    
    public override string TypeName { get; } = "TimeToCollision";

    public override AgentType[] UnsupportedAgentTypes { get; } = { AgentType.Unknown, AgentType.Ego};

    public override IEnumerator Apply(ITriggerAgent agent)
    {
        var lowestTTC = TimeToCollisionLimit;
        var egos = SimulatorManager.Instance.AgentManager.ActiveAgents;
        AgentController collisionEgo = null;
        foreach (var ego in egos)
        {
            var agentController = ego.AgentGO.GetComponentInChildren<AgentController>();
            var ttc = CalculateTTC(agentController, agent);
            if (ttc >= lowestTTC || ttc < 0.0f) continue;
            
            lowestTTC = ttc;
            collisionEgo = agentController;
        }

        //If there is no collision detected don't wait
        if (lowestTTC >= TimeToCollisionLimit || collisionEgo == null)
            yield break;

        //Agent will adjust waiting time while waiting
        do
        {
            yield return null;
            lowestTTC = CalculateTTC(collisionEgo, agent);
            //Check if TTC is valid, for example ego did not change the direction
            if (lowestTTC >= TimeToCollisionLimit)
                yield break;
        } while (lowestTTC > 0.0f);
    }

    public override void DeserializeProperties(JSONNode jsonData)
    {
        
    }

    public override void SerializeProperties(JSONNode jsonData)
    {
        
    }

    private float CalculateTTC(AgentController ego, ITriggerAgent agent)
    {
        //Calculate intersection point, return infinity if vehicles won't intersect
        if (!GetLineIntersection(ego.transform.position, ego.transform.forward, agent.AgentTransform.position, agent.AgentTransform.forward,
            out var intersection))
            return float.PositiveInfinity;

        var egoDistance = Distance2D(ego.transform.position, intersection);
        var npcDistance = Distance2D(agent.AgentTransform.position, intersection);
        var egoTimeToIntersection = CalculateTimeForAccelerated(ego.Velocity.magnitude, ego.Acceleration.magnitude, egoDistance);
        var npcTimeToIntersection = CalculateTimeForAccelerated(agent.MovementSpeed, agent.Acceleration.magnitude, npcDistance);
        
        //If npc will reach intersection quicker, ttc will be positive
        //If agent cannot reach collision point before ego, ttc will be negative
        return egoTimeToIntersection - npcTimeToIntersection;
    }

    private float Distance2D(Vector3 position, Vector3 intersection)
    {
        //Calculated distance omitting the Y axis
        var x = position.x;
        var z = position.z;
        return Mathf.Sqrt((x - intersection.x) * (x - intersection.x) + (z - intersection.z) * (z - intersection.z));
    }

    private float CalculateTimeForAccelerated(float startingSpeed, float acceleration, float distance)
    {
        if (acceleration <= 0.0f)
            return distance / startingSpeed;
        var deltaSqrt = Mathf.Sqrt(4 * startingSpeed * startingSpeed + 8 * acceleration * distance);
        var t1 = (-2 * startingSpeed - deltaSqrt) / (2 * acceleration);
        var t2 = (-2 * startingSpeed + deltaSqrt) / (2 * acceleration);
        //Chose positive time value and discard negative time value
        var t = Mathf.Max(t1, t2);
        return t;
    }

    private bool GetLineIntersection(Vector3 position0, Vector3 direction0,
        Vector3 position1, Vector3 direction1, out Vector3 intersection)
    {
        var result = GetLineIntersection(new Vector2(position0.x, position0.z),
            new Vector2(direction0.x, direction0.z),
            new Vector2(position1.x, position1.z), new Vector2(direction1.x, direction1.z), out var intersection2d);
        intersection = new Vector3(intersection2d.x, 0.0f, intersection2d.y);
        return result;
    }

    private bool GetLineIntersection(Vector2 position0, Vector2 direction0,
        Vector2 position1, Vector2 direction1, out Vector2 intersection)
    {
        //Can't divide by 0, swap vectors if needed
        if (Mathf.Approximately(direction1.x, 0.0f))
        {
            //Both lines 
            if (Mathf.Approximately(direction0.x, 0.0f))
            {
                intersection = Vector2.zero;
                return false;
            }

            var tempV = position0;
            position0 = position1;
            position1 = tempV;
            tempV = direction0;
            direction0 = direction1;
            direction1 = tempV;
        }

        //Calculate intersection point
        var u = (position0.y * direction1.x + direction1.y * position1.x - position1.y * direction1.x -
                 direction1.y * position0.x) / (direction0.x * direction1.y - direction0.y * direction1.x);
        var v = (position0.x + direction0.x * u - position1.x) / direction1.x;
        intersection = position0 + direction0 * u;
        //Check if intersection is in front of both starting positions
        return u >= 0.0f && v >= 0.0f;
    }
}
