/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;
using HD = apollo.hdmap;

namespace Simulator.Map
{
    public class MapLane : MapDataPoints
    {
        public bool displayLane = false;
        public bool isSelfReverseLane = false;
        public GameObject selfReverseLane = null;

        public float displayLaneWidth = 3.7f; // apollo default lane width

        [System.NonSerialized]
        public List<MapLane> befores = new List<MapLane>();
        [System.NonSerialized]
        public List<MapLane> afters = new List<MapLane>();

        [System.NonSerialized]
        public string id = null; // TODO move to mapdata?

        [System.NonSerialized]
        public MapLane leftLaneForward = null;
        [System.NonSerialized]
        public MapLane rightLaneForward = null;
        [System.NonSerialized]
        public MapLane leftLaneReverse = null;
        [System.NonSerialized]
        public MapLane rightLaneReverse = null;
        [System.NonSerialized]
        public int laneCount;
        [System.NonSerialized]
        public int laneNumber;

        public MapLine leftLineBoundry;
        public MapLine rightLineBoundry;
        public MapLine stopLine;

        // TODO uncomment for vectorMap
        //[System.NonSerialized]
        //public List<Map.Autoware.LaneInfo> laneInfos = new List<Map.Autoware.LaneInfo>();

        public List<MapLane> yieldToLanes = new List<MapLane>(); // TODO calc
        [System.NonSerialized]
        public List<MapLane> nextConnectedLanes = new List<MapLane>();
        [System.NonSerialized]
        public bool Spawnable = false;
        public bool isTrafficLane { get; set; } = false;
        public bool isStopSignIntersetionLane { get; set; } = false;

        public LaneTurnType laneTurnType = LaneTurnType.NO_TURN;
        public LaneBoundaryType leftBoundType;
        public LaneBoundaryType rightBoundType;
        public float speedLimit = 20.0f;

        public override void Draw()
        {
            if (mapLocalPositions.Count < 2) return;

            AnnotationGizmos.DrawWaypoints(transform, mapLocalPositions, MapAnnotationTool.PROXIMITY * 0.5f, laneColor + selectedColor);
            AnnotationGizmos.DrawLines(transform, mapLocalPositions, laneColor + selectedColor);
            AnnotationGizmos.DrawArrowHeads(transform, mapLocalPositions, laneColor + selectedColor);
            if (MapAnnotationTool.SHOW_HELP)
            {
#if UNITY_EDITOR
                UnityEditor.Handles.Label(transform.position, "    LANE " + laneTurnType);
#endif
            }
        }

        public void ReversePoints()
        {
            if (mapLocalPositions.Count < 2) return;

            mapLocalPositions.Reverse();

            // For parking, self-reverse lane should not have same waypoint coordinates.
            for (int i=0; i<mapLocalPositions.Count; i++)
                mapLocalPositions[i] = new Vector3((float)(mapLocalPositions[i].x + 0.1), (float)(mapLocalPositions[i].y + 0.1), (float)(mapLocalPositions[i].z + 0.1));
        }
    }
}