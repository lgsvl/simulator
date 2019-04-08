/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class WaypointQueue
{
    private Queue<Vector3> Q;
    public stopTarget_t StopTarget;
    public MapLaneSegmentBuilder currentLane {get; private set;}
    public MapLaneSegmentBuilder previousLane {get; private set;}

    public struct stopTarget_t
    {
        public bool isStopAhead;
        public Vector3 waypoint;
        public stopTarget_t(Vector3 point)
        {
            this.isStopAhead = true;
            this.waypoint = point;
        }
    }
    public WaypointQueue()
    {
        this.Q = new Queue<Vector3>();
    }

    public void setStartLane(MapLaneSegmentBuilder lane)
    {
        this.Q.Clear(); // ensure there is nothing left over in the queue (this is the FIRST lane)
        this.currentLane = lane;
        this.previousLane = lane; // to avoid having a null previousLane at the start.
        firstLaneSegmentInit();
        fetchStopTargetFromLane();
    }

    private void firstLaneSegmentInit()
    {
        Vector3 start_pt = this.currentLane.segment.targetWorldPositions[0];
        Vector3 end_pt = this.currentLane.segment.targetWorldPositions[1];
        Vector3 mid_pt = Vector3.Lerp(start_pt, end_pt, 0.1f);

        this.Q.Enqueue(start_pt);
        this.Q.Enqueue(mid_pt);

        for (int i = 1; i < this.currentLane.segment.targetWorldPositions.Count; i++)
        {
            this.Q.Enqueue(this.currentLane.segment.targetWorldPositions[i]);
        } 
    }

    private void fetchWaypointsFromLane()
    {
        foreach (Vector3 waypoint in this.currentLane.segment.targetWorldPositions)
        {
            this.Q.Enqueue(waypoint);
        }
    }

    private void fetchStopTargetFromLane()
    {
        if (this.previousLane?.stopLine == null) // using previous lane because current lane will have switched off by the end.
        {
            this.StopTarget.isStopAhead = false;
            this.StopTarget.waypoint = new Vector3();
        }
        else
        {
            this.StopTarget.isStopAhead = true;
            this.StopTarget.waypoint = this.previousLane.segment.targetWorldPositions[this.previousLane.segment.targetWorldPositions.Count - 1];
        }
    }

    public Vector3 Dequeue()
    {
        if (this.Q.Count == 0)
        {
            if (this.currentLane)
            {
                // enqueue waypoints from next lane, then dequeue
                this.previousLane = this.currentLane;
                // fetch next lane
                this.currentLane =  this.currentLane.nextConnectedLanes[(int)UnityEngine.Random.Range(0, this.currentLane.nextConnectedLanes.Count)];
                fetchWaypointsFromLane();
            }
            else
            {
                Debug.LogError("currentLane is not set for the waypoint queue.");
            }
        }
        
        return this.Q.Dequeue();
    }

    


}

