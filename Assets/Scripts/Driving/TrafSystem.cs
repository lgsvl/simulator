/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class SplineNode {
    public Vector3 position;
    public Vector3 tangent;
}

[System.Serializable]
public class TrafEntry
{
    public int identifier;
    public int subIdentifier;

    public TrafIntersection intersection;
    public TrafIntersectionPath path;
    public TrafRoad road;
    public TrafficLightContainer light;
    public List<Vector3> waypoints;
    public List<SplineNode> spline; //seems light this one is empty for all entries

    //TODO: remove road ScriptableObject dependency
	[System.NonSerialized]
    private List<TrafEntry> giveWayEntries;

    private int registered = 0;

    private static Queue<TrafAIMotor> utilQueue = new Queue<TrafAIMotor>();

    public Vector3[] GetPoints()
    {
        return waypoints.ToArray();
        //return road.waypoints.Select(wp => wp.position).ToArray();
    }

    public List<SplineNode> GetSpline() {
        return spline;
    }

    public InterpolatedPosition GetInterpolatedPosition(float t)
    {
        float totalDist = 0f;
        for(int i = 1; i < waypoints.Count; i++)
        {
            totalDist += Vector3.Distance(waypoints[i], waypoints[i - 1]);
        }

        float workingDist = 0f;
        for(int i = 1; i < waypoints.Count; i++)
        {
            float thisDist = Vector3.Distance(waypoints[i], waypoints[i - 1]);
            if((workingDist + thisDist) / totalDist >= t)
            {
                return new InterpolatedPosition() { targetIndex = i, position = Vector3.Lerp(waypoints[i - 1], waypoints[i], t - workingDist / totalDist) };
            }
            workingDist += thisDist;
        }
        return new InterpolatedPosition() { targetIndex = waypoints.Count - 1, position = waypoints[waypoints.Count - 1] };
    }

   
    public InterpolatedPosition GetInterpolatedPositionInfractions(float t)
    {
        float totalDist = 0f;
        for (int i = 1; i < waypoints.Count; i++)
        {
            totalDist += Vector3.Distance(waypoints[i], waypoints[i - 1]);
        }

        float workingDist = 0f;
        for (int i = 1; i < waypoints.Count; i++)
        {
            float thisDist = Vector3.Distance(waypoints[i], waypoints[i - 1]);
            if ((workingDist + thisDist) / totalDist >= t)
            {
                return new InterpolatedPosition() { targetIndex = i, position = Vector3.Lerp(waypoints[i - 1], waypoints[i], (t - workingDist / totalDist) / ((workingDist + thisDist)/totalDist - workingDist / totalDist))};
            }
            workingDist += thisDist;
        }
        return new InterpolatedPosition() { targetIndex = waypoints.Count - 1, position = waypoints[waypoints.Count - 1] };
    }

    public float GetTotalDistance()
    {
        float totalDist = 0f;
        for (int i = 1; i < waypoints.Count; i++)
        {
            totalDist += Vector3.Distance(waypoints[i], waypoints[i - 1]);
        }

        return totalDist;
    }

    public bool isIntersection()
    {
        return intersection != null;
    }

    public bool IsClear()
    {
        return registered <= 0;
    }

    public void RegisterInterest(TrafAIMotor tm)
    {
        registered++;
        if(isIntersection() && intersection.stopSign)
        {
            intersection.stopQueue.Enqueue(tm);
        }
    }

    //deregister a specific one from queue
    public void DeregisterInterest(TrafAIMotor tm)
    {
        registered--;
        if (isIntersection() && intersection.stopSign && intersection.stopQueue.Count > 0)
        {
            if (tm == intersection.stopQueue.Peek())
            {
                intersection.stopQueue.Dequeue();
            }
            else
            {
                utilQueue.Clear();
                var count = intersection.stopQueue.Count;
                for (int i = 0; i < count; i++)
                {
                    var item = intersection.stopQueue.Dequeue();
                    if (item == tm)
                    {
                        continue;
                    }
                    utilQueue.Enqueue(item);
                }

                //swap
                var temp = intersection.stopQueue;
                intersection.stopQueue = utilQueue;
                utilQueue = temp;
            }
        }
    }

    public TrafAIMotor GetLongestWaitCar()
    {
        float maxWait = -10000f;
        TrafAIMotor longestWaitCar = null;
        foreach (var car in intersection.stopQueue)
        {
            if (car.timeWaitingAtStopSign > maxWait)
            {
                maxWait = car.timeWaitingAtStopSign;
                longestWaitCar = car;
            }
        }

        return longestWaitCar;
    }

    public void PutInfrontQueue(TrafAIMotor tm)
    {
        DeregisterInterest(tm);
        utilQueue.Clear();
        utilQueue.Enqueue(tm);

        var count = intersection.stopQueue.Count;
        for (int i = 0; i < count; i++)
        {
            utilQueue.Enqueue(intersection.stopQueue.Dequeue());
        }

        //swap
        var temp = intersection.stopQueue;
        intersection.stopQueue = utilQueue;
        utilQueue = temp;
    }
    
    public void ResetAllInterests()
    {
        registered = 0;
        if(isIntersection() && intersection.stopSign)
        {
            intersection.stopQueue.Clear();
        }
    }

    public void Init(TrafSystem system)
    {
        giveWayEntries = new List<TrafEntry>();
        foreach(int i in path.giveWayTo) {
            giveWayEntries.Add(system.GetEntry(identifier, i));
        }
    }

    public bool MustGiveWay()
    {
        foreach(var e in giveWayEntries)
        {
            if(!e.IsClear())
            {
                return true;
            }
        }
        return false;
    }

}

[System.Serializable]
public class RoadGraphNode {
    public List<RoadGraphEdge> edges;

    public RoadGraphEdge SelectRandom() {
        if(edges != null && edges.Count > 0) {
            return edges[Random.Range(0, edges.Count)];
        } else {
            Debug.Log("no edges");
            return null;
        }
    }

    public bool HasEdges()
    {
        return (edges != null && edges.Count > 0);
    }

}

[System.Serializable]
public class RoadGraphEdge { //Essentially connected entries
    public int id;
    public int subId;
}

[System.Serializable]
public class TrafRoadGraph {
    public RoadGraphNode[] roadGraph = new RoadGraphNode[3000 * 50];

    public RoadGraphNode GetNode(int id, int subId)
    {
        return roadGraph[id * 50 + subId];
    }

}

public class TrafSystem : MonoBehaviour {
    //[HideInInspector]
    public List<TrafEntry> entries;
    //[HideInInspector]
    public List<TrafEntry> intersections;

    [HideInInspector]
    public TrafRoadGraph roadGraph;

    public RoadGraphEdge FindJoiningIntersection(RoadGraphNode start, RoadGraphEdge target)
    {
        return start.edges.Find(e =>
        {
            return roadGraph.GetNode(e.id, e.subId).edges.Exists(edge =>
            {
                return edge.id == target.id && edge.subId == target.subId;
            });
        });
    }

    public TrafEntry GetEntry(int id, int subId)
    {
        if(id >= 1000)
        {
            return intersections.Find(i => i.identifier == id && i.subIdentifier == subId);
        }
        else
        {
            return entries.Find(i => i.identifier == id && i.subIdentifier == subId);
        }
    }

    public void DeleteIntersectionIfExists(int id, int subId)
    {
        if(intersections.Any(i => i.identifier == id && i.subIdentifier == subId))
        {
            intersections.RemoveAt(intersections.FindIndex(i => i.identifier == id && i.subIdentifier == subId));
        }
    }

    public void Awake()
    {
        foreach(var e in intersections)
        {
            e.Init(this);
        }
    }

    public void ResetIntersections()
    {
        foreach(var e in intersections)
        {
            e.ResetAllInterests();
        }
    }

}
