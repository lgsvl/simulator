/**
* Copyright (c) 2018 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MapIntersectionBuilder : MapIntersection
{
    public float intersectionRange = 10f;
    [System.NonSerialized]
    public IntersectionComponent intersectionC;
    [System.NonSerialized]
    public List<MapLaneSegmentBuilder> mapIntersectionLanes = new List<MapLaneSegmentBuilder>();

    // stop sign
    public bool isStopSign { get; set; }
    [System.NonSerialized]
    public List<NPCControllerComponent> stopQueue = new List<NPCControllerComponent>();

    public void GetIntersection()
    {
        var allIntersections = FindObjectsOfType<IntersectionComponent>();

        foreach (var item in allIntersections)
        {
            if (!item.gameObject.activeInHierarchy)   //skip inactive ones         
                continue;

            if (Vector3.Distance(this.transform.position, item.transform.position) < intersectionRange)
            {
                intersectionC = item;
                break;
            }
        }

        SetIntersectionLaneData();

        if (intersectionC == null)
            Debug.LogError("Error finding intersection, check range");
    }

    public void SetIntersectionLaneData()
    {
        mapIntersectionLanes = new List<MapLaneSegmentBuilder>();
        mapIntersectionLanes.AddRange(transform.GetComponentsInChildren<MapLaneSegmentBuilder>());

        foreach (var lane in mapIntersectionLanes)
        {
            lane.laneCount = 1;
            lane.laneNumber = 1;
            lane.leftForward = lane.rightForward = lane.leftReverse = lane.rightReverse = null;
        }
    }

    public void EnterStopSignQueue(NPCControllerComponent npcController)
    {
        stopQueue.Add(npcController);
    }

    public bool CheckStopSignQueue(NPCControllerComponent npcController)
    {
            if (stopQueue.Count == 0 || npcController == stopQueue[0])
                return true;
            else
                return false;
    }

    public void ExitStopSignQueue(NPCControllerComponent npcController)
    {
        if (stopQueue.Count == 0) return;
        stopQueue.Remove(npcController);
    }

    private void RemoveFirstElement()
    {
        if (stopQueue.Count == 0) return;
        if (Vector3.Distance(stopQueue[0].transform.position, transform.position) > intersectionC.yieldTrigger.radius * 2f)
        {
            NPCControllerComponent npcC = stopQueue[0].GetComponent<NPCControllerComponent>();
            if (npcC != null)
            {
                ExitStopSignQueue(npcC);
                npcC.currentIntersectionComponent = null;
            }
        }
    }
}
