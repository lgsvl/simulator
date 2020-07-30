/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Managers
{
    using System;
    using System.Collections.Generic;
    using Map;
    using UnityEngine;

    /// <summary>
    /// Handler caches the map lane lines data and snaps passed transforms positions to lanes
    /// </summary>
    public class LaneSnappingHandler
    {
        /// <summary>
        /// Information how point can be snapped to the line
        /// </summary>
        private enum SnappedPointType
        {
            /// <summary>
            /// Point can not be snapped to the line with passed parameters
            /// </summary>
            Invalid,
            
            /// <summary>
            /// Point will be snapped between start and end of the line
            /// </summary>
            InBetween,
            
            /// <summary>
            /// Point will be snapped to the start of the line
            /// </summary>
            Start,
            
            /// <summary>
            /// Point will be snapped to the end of the line
            /// </summary>
            End
        }

        /// <summary>
        /// What lane type should be analyzed in the snapping
        /// </summary>
        public enum LaneType
        {
            /// <summary>
            /// Traffic lanes for vehicles
            /// </summary>
            Traffic,
            
            /// <summary>
            /// Pedestrian lanes for pedestrians
            /// </summary>
            Pedestrian
        }

        /// <summary>
        /// Data of a single line in the map lane
        /// </summary>
        private struct LaneLineData
        {
            /// <summary>
            /// Length of this line
            /// </summary>
            private float length;
            
            /// <summary>
            /// Parameter a of the line
            /// </summary>
            private float a;
            
            /// <summary>
            /// Parameter b of the line
            /// </summary>
            private float b;
            
            /// <summary>
            /// Parameter c of the line
            /// </summary>
            private float c;
            
            /// <summary>
            /// Precached calculation of the a*a+b*b
            /// </summary>
            private float a2b2;
            
            /// <summary>
            /// Precached calculation of the sqrt(a*a+b*b)
            /// </summary>
            private float sqrta2b2;

            /// <summary>
            /// Line start position
            /// </summary>
            public Vector3 start;
            
            /// <summary>
            /// Line end position
            /// </summary>
            public Vector3 end;
            
            /// <summary>
            /// Precached direction of this line, used to apply rotation
            /// </summary>
            public Quaternion direction;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="start">Line start position</param>
            /// <param name="end">Line end position</param>
            public LaneLineData(Vector3 start, Vector3 end)
            {
                this.start = start;
                this.end = end;
                direction = Quaternion.LookRotation((end - start).normalized);
                length = Distance(start, end);
                a = end.z - start.z;
                b = -(end.x - start.x);
                c = end.x * start.z - end.z * start.x;
                a2b2 = a * a + b * b;
                sqrta2b2 = Mathf.Sqrt(a2b2);
            }

            /// <summary>
            /// Calculates distance between two points
            /// </summary>
            /// <param name="firstPoint">First point</param>
            /// <param name="secondPoint">Second point</param>
            /// <returns>Distance between two points</returns>
            private static float Distance(Vector3 firstPoint, Vector3 secondPoint)
            {
                return Mathf.Abs(Mathf.Pow(firstPoint.x - secondPoint.x, 2) + Mathf.Pow(firstPoint.z - secondPoint.z, 2));
            }

            /// <summary>
            /// Calculates distance to the line, returns float.MaxValue is greater than passed max distance
            /// </summary>
            /// <param name="point">Point that will be snapped to line</param>
            /// <param name="maxDistance">Maximal distance that can be applied</param>
            /// <param name="snappedPointType">Returns type how point will be snapped to the line</param>
            /// <returns>Distance between the point and the line, returns float.MaxValue is greater than passed max distance</returns>
            public float Distance(Vector3 point, float maxDistance, out SnappedPointType snappedPointType)
            {
                var distanceToStraightLine = Mathf.Abs((a * point.x + b * point.z + c) / sqrta2b2);
                if (distanceToStraightLine > maxDistance)
                {
                    snappedPointType = SnappedPointType.Invalid;
                    return float.MaxValue;
                }

                var pointStart = Distance(point, start);
                var pointEnd = Distance(point, end);
                var lengthPow = Mathf.Pow(length, 2);
                var pointStartPow = Mathf.Pow(pointStart, 2);
                var pointEndPow = Mathf.Pow(pointEnd, 2);

                //Check if point will be snapped to the start, end or in between
                //Point is snapped to point between start and end only when the formed triangle is obtuse
                if (pointStartPow > lengthPow + pointEndPow)
                {
                    if (pointEnd > maxDistance)
                    {
                        snappedPointType = SnappedPointType.Invalid;
                        return float.MaxValue;
                    }
                    snappedPointType = SnappedPointType.End;
                    return pointEnd;
                }

                if (pointEndPow > lengthPow + pointStartPow)
                {
                    if (pointStart > maxDistance)
                    {
                        snappedPointType = SnappedPointType.Invalid;
                        return float.MaxValue;
                    }
                    snappedPointType = SnappedPointType.Start;
                    return pointStart;
                }

                snappedPointType = SnappedPointType.InBetween;
                return distanceToStraightLine;
            }

            /// <summary>
            /// Calculates position of the closest point on the line to the passed point
            /// </summary>
            /// <param name="point">Passed point that will be snapped</param>
            /// <param name="snappedPointType">Precalculated snapping type (from the distance method)</param>
            /// <returns>Position of closest point</returns>
            /// <exception cref="ArgumentOutOfRangeException">Invalid snapped point type</exception>
            public Vector3 ClosestPoint(Vector3 point, SnappedPointType snappedPointType)
            {
                switch (snappedPointType)
                {
                    case SnappedPointType.InBetween:
                        var x = (b * (b * point.x - a * point.z) - a * c) / a2b2;
                        var z = (a * (-b * point.x + a * point.z) - b * c) / a2b2;
                        return new Vector3(x, point.y, z);
                    case SnappedPointType.Start:
                        return new Vector3(start.x, point.y, start.z);
                    case SnappedPointType.End:
                        return new Vector3(end.x, point.y, end.z);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(snappedPointType), snappedPointType, null);
                }
            }
        }

        /// <summary>
        /// Persistence data key for snapping lock value
        /// </summary>
        private static string SnappingLockKey = "Simulator/ScenarioEditor/LaneSnappingHandler/SnappingLock";

        /// <summary>
        /// Available traffic lane lines on the current map
        /// </summary>
        private readonly List<LaneLineData> trafficLines = new List<LaneLineData>();
        
        /// <summary>
        /// Available pedestrian lane lines on the current map
        /// </summary>
        private readonly List<LaneLineData> pedestrianLines = new List<LaneLineData>();

        /// <summary>
        /// Snapping lock value. 0 - snapping enabled, 1 - snapping locked
        /// </summary>
        private int snappingLock = -1;

        /// <summary>
        /// Is the snapping enabled
        /// </summary>
        public bool SnappingEnabled
        {
            get
            {
                // 0 - snapping enabled, 1 - snapping locked
                if (snappingLock == -1)
                    snappingLock = PlayerPrefs.GetInt(SnappingLockKey, 0);
                return snappingLock == 0;
            }
            set
            {
                // 0 - snapping enabled, 1 - snapping locked
                var intValue = value ? 0 : 1;
                if (intValue == snappingLock) return;
                snappingLock = intValue;
                PlayerPrefs.SetInt(SnappingLockKey, intValue);
            }
        }

        /// <summary>
        /// Initialization method, has to be called every time map is loaded
        /// </summary>
        public void Initialize()
        {
            var mapManagerData = new MapManagerData();
            var trafficLanes = mapManagerData.GetTrafficLanes();
            foreach (var mapLane in trafficLanes)
            {
                if (mapLane.mapLocalPositions.Count==0)
                    continue;
                var previousPosition = mapLane.transform.TransformPoint(mapLane.mapLocalPositions[0]);
                for (var i = 1; i < mapLane.mapLocalPositions.Count; i++)
                {
                    var localPos = mapLane.mapLocalPositions[i];
                    var position = mapLane.transform.TransformPoint(localPos);
                    trafficLines.Add(new LaneLineData(previousPosition, position));
                    previousPosition = position;
                }
            }
            var pedestrianLanes = mapManagerData.GetPedestrianLanes();
            foreach (var mapLane in pedestrianLanes)
            {
                if (mapLane.mapLocalPositions.Count==0)
                    continue;
                var previousPosition = mapLane.transform.TransformPoint(mapLane.mapLocalPositions[0]);
                for (var i = 1; i < mapLane.mapLocalPositions.Count; i++)
                {
                    var localPos = mapLane.mapLocalPositions[i];
                    var position = mapLane.transform.TransformPoint(localPos);
                    pedestrianLines.Add(new LaneLineData(previousPosition, position));
                    previousPosition = position;
                }
            }
        }

        /// <summary>
        /// Deinitialization method, has to be called every time map is unloaded
        /// </summary>
        public void Deinitialize()
        {
            trafficLines.Clear();
            pedestrianLines.Clear();
        }

        /// <summary>
        /// Snapping method, tries to snap the transform to closest line of selected lane type, but not further than max distance
        /// </summary>
        /// <param name="laneType">Lane type to which transform should be snapped</param>
        /// <param name="transformToMove">Transform that will be moved if valid snapping point is found</param>
        /// <param name="transformToRotate">Transform that will be rotated to lane direction if valid snapping point is found</param>
        /// <param name="maxDistance">Max distance from the line for snapping to be valid</param>
        public void SnapToLane(LaneType laneType, Transform transformToMove, Transform transformToRotate = null, float maxDistance = 5.0f)
        {
            //Check if snapping is allowed
            if (!SnappingEnabled)
                return;
            
            //Select lane lines for this agent type
            List<LaneLineData> lines;
            switch (laneType)
            {
                case LaneType.Traffic:
                    lines = trafficLines;
                    break;
                case LaneType.Pedestrian:
                    lines = pedestrianLines;
                    break;
                default:
                    return;
            }
            
            //Find closest line
            var shortestDistance = maxDistance;
            var lineIndex = -1;
            var snappedPointType = SnappedPointType.Invalid;
            for (var i = 0; i < lines.Count; i++)
            {
                var distance = lines[i].Distance(transformToMove.position, maxDistance, out var snapType);
                if (distance >= shortestDistance) continue;
                snappedPointType = snapType;
                shortestDistance = distance;
                lineIndex = i;
            }

            //Check if position can be snapped to any lane
            if (lineIndex == -1)
                return;

            //Snap to the closest point on the line and apply it's direction
            var closestPoint = lines[lineIndex].ClosestPoint(transformToMove.position, snappedPointType);
            transformToMove.position = closestPoint;
            if (transformToRotate!=null)
                transformToRotate.rotation = lines[lineIndex].direction;
        }
    }
}