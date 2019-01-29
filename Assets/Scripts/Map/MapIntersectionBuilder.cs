/**
* Copyright (c) 2018 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class MapIntersectionBuilder : MapIntersection
{
    //public bool debug = false;
    public IntersectionComponent intersectionC;
    private float intersectionRange = 10f;

    // stop sign
    public Queue<NPCControllerComponent> stopQueue = new Queue<NPCControllerComponent>();
    public bool isStopSign { get; set; }


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

    public void EnterQueue(NPCControllerComponent npcController)
    {
        stopQueue.Enqueue(npcController);
    }

    public bool CheckQueue(NPCControllerComponent npcController)
    {
            if (stopQueue.Count == 0 || npcController == stopQueue.Peek())
                return true;
            else
                return false;
    }

    public void ExitQueue(NPCControllerComponent npcController)
    {
        if (stopQueue.Count == 0) return;

        if (npcController == stopQueue.Peek())
        {
            stopQueue.Dequeue();
        }
    }



    public void Test()
    {
        
    }
}
