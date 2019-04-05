using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class WaypointQueue
{
    private Queue<Vector3> Q;
    private Queue<Vector3> StopQ;
    public MapLaneSegmentBuilder currentLane {get; private set;}

    public WaypointQueue()
    {
        this.Q = new Queue<Vector3>();
    }

    public void setStartLane(MapLaneSegmentBuilder lane)
    {
        this.Q.Clear(); // ensure there is nothing left over in the queue (this is the FIRST lane)
        this.currentLane = lane;
        fetchWaypointsFromLane();
    }

    private void fetchWaypointsFromLane()
    {
        foreach (Vector3 waypoint in this.currentLane.segment.targetWorldPositions)
        {
            this.Q.Enqueue(waypoint);
        }
    }

    public Vector3 Dequeue()
    {
        if (this.Q.Count == 0)
        {
            if (this.currentLane)
            {
                // enqueue waypoints from next lane, then dequeue
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

