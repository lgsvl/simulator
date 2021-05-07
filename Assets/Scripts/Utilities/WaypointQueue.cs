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
using Simulator.Map;


namespace Simulator.Utilities
{
    public class WaypointQueue
    {
        private Queue<Vector3> Q;
        public stopTarget_t StopTarget;
        public MapTrafficLane currentLane { get; private set; }
        public MapTrafficLane previousLane { get; private set; }
        private System.Random RandomGenerator;

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
        public WaypointQueue(int seed)
        {
            this.Q = new Queue<Vector3>();
            RandomGenerator = new System.Random(seed);
        }

        public void setStartLane(MapTrafficLane lane)
        {
            this.Q.Clear(); // ensure there is nothing left over in the queue (this is the FIRST lane)
            this.currentLane = lane;
            this.previousLane = lane; // to avoid having a null previousLane at the start.
            firstLaneSegmentInit();
            fetchStopTargetFromLane();
        }

        private void firstLaneSegmentInit()
        {
            Vector3 start_pt = this.currentLane.mapWorldPositions[0];
            Vector3 end_pt = this.currentLane.mapWorldPositions[1];
            Vector3 mid_pt = Vector3.Lerp(start_pt, end_pt, 0.1f);

            this.Q.Enqueue(start_pt);
            this.Q.Enqueue(mid_pt);

            for (int i = 1; i < this.currentLane.mapWorldPositions.Count; i++)
            {
                this.Q.Enqueue(this.currentLane.mapWorldPositions[i]);
            }
        }

        private void fetchWaypointsFromLane()
        {
            foreach (Vector3 waypoint in this.currentLane.mapWorldPositions)
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
                this.StopTarget.waypoint = this.previousLane.mapWorldPositions[this.previousLane.mapWorldPositions.Count - 1];
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
                    this.currentLane = this.currentLane.nextConnectedLanes[RandomGenerator.Next(this.currentLane.nextConnectedLanes.Count)];
                    fetchWaypointsFromLane();
                }
                else
                {
                    Debug.LogWarning("currentLane is not set for the waypoint queue.");
                }
            }
            return this.Q.Dequeue();
        }
    }
}
