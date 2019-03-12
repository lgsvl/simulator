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
    //public bool debug = false;
    public IntersectionComponent intersectionC;
    public float intersectionRange = 10f;

    // stop sign
    public bool isStopSign { get; set; }
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

        if (intersectionC == null)
            Debug.LogError("Error finding intersection, check range");
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

    private void Update()
    {
        //for (int i = 0; i < stopQueue.Count; i++)
        //{
        //    if (Vector3.Distance(stopQueue[i].transform.position, transform.position) > intersectionC.yieldTrigger.radius * 2f)
        //    {
        //        NPCControllerComponent npcC = stopQueue[i].GetComponent<NPCControllerComponent>();
        //        if (npcC != null )
        //        {
        //            ExitStopSignQueue(npcC);
        //            npcC.currentIntersectionComponent = null;
        //        }
        //    }
        //}
    }
}
