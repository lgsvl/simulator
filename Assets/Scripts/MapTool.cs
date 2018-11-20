/**
* Copyright (c) 2018 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/


using Apollo;
using Autoware;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Map
{
    public class MapTool : MonoBehaviour
    {
        public static bool showMap;

        public static float PROXIMITY = 1.0f; //0.02f
        public static float ARROWSIZE = 50f; //1.0f

        public float exportScaleFactor = 1.0f;

        public static bool DoubleSubsegmentResolution(MapSegment seg)
        {
            if (seg.targetLocalPositions.Count < 2)
            {
                Debug.Log($"A {nameof(MapSegment)} contains less than 2 waypoints, can not double subsegment resolution");
                return false;
            }

            for (int j = seg.targetLocalPositions.Count - 1; j > 0; --j)
            {
                var mid = (seg.targetLocalPositions[j] + seg.targetLocalPositions[j - 1]) / 2f;
                seg.targetLocalPositions.Insert(j, mid);
            }

            return true;
        }

        public static bool HalfSubsegmentResolution(MapSegment seg)
        {
            if (seg.targetLocalPositions.Count < 3)
            {
                Debug.Log($"A {nameof(MapSegment)} contains less than 3 waypoints, can not half subsegment resolution");
                return false;
            }

            for (int j = seg.targetLocalPositions.Count - 2; j > 0; j -= 2)
            {
                seg.targetLocalPositions.RemoveAt(j);
            }

            return true;
        }

        public static bool JointTwoMapLaneSegments(List<MapLaneSegmentBuilder> laneBuilders)
        {
            if (laneBuilders.Count != 2)
            {
                Debug.Log($"Exactly two {nameof(MapLaneSegmentBuilder)} instances required");
                return false;
            }

            if (laneBuilders.Any(l => l.segment.targetLocalPositions.Count < 2))
            {
                Debug.Log($"Some {nameof(MapLaneSegmentBuilder)} contains less than 2 segment waypoints, can not finish operation");
                return false;
            }

            {
                var A = laneBuilders[0];
                var B = laneBuilders[1];
                var lastPt = A.transform.TransformPoint(A.segment.targetLocalPositions[A.segment.targetLocalPositions.Count - 1]);
                var firstPt_cmp = B.transform.TransformPoint(B.segment.targetLocalPositions[0]);
                if ((lastPt - firstPt_cmp).magnitude < Map.MapTool.PROXIMITY / FindObjectOfType<Map.MapTool>().exportScaleFactor)
                {
                    var mergePt = (lastPt + firstPt_cmp) * .5f;
                    A.segment.targetLocalPositions[A.segment.targetLocalPositions.Count - 1] = A.transform.InverseTransformPoint(mergePt);
                    for (int i = 1; i < B.segment.targetLocalPositions.Count; i++)
                    {
                        var newPt = B.transform.TransformPoint(B.segment.targetLocalPositions[i]);
                        A.segment.targetLocalPositions.Add(A.transform.InverseTransformPoint(newPt));
                    }
                    DestroyImmediate(B.gameObject);
                    return true;
                }
            }

            {
                var A = laneBuilders[1];
                var B = laneBuilders[0];
                var lastPt = A.transform.TransformPoint(A.segment.targetLocalPositions[A.segment.targetLocalPositions.Count - 1]);
                var firstPt_cmp = B.transform.TransformPoint(B.segment.targetLocalPositions[0]);
                if ((lastPt - firstPt_cmp).magnitude < Map.MapTool.PROXIMITY / FindObjectOfType<Map.MapTool>().exportScaleFactor)
                {
                    var mergePt = (lastPt + firstPt_cmp) * .5f;
                    A.segment.targetLocalPositions[A.segment.targetLocalPositions.Count - 1] = A.transform.InverseTransformPoint(mergePt);
                    for (int i = 1; i < B.segment.targetLocalPositions.Count; i++)
                    {
                        var newPt = B.transform.TransformPoint(B.segment.targetLocalPositions[i]);
                        A.segment.targetLocalPositions.Add(A.transform.InverseTransformPoint(newPt));
                    }
                    DestroyImmediate(B.gameObject);
                    return true;
                }
            }

            return false;
        }

        //assume leftmost lane to rightmost lane order in list
        public static void LinkLanes(List<MapLaneSegmentBuilder> mapLaneBuilders, bool reverseLink = false)
        {
            mapLaneBuilders.RemoveAll(b => b == null);
            if (reverseLink)
            {
                for (int i = 0; i < 1; i++)
                {
                    var A = mapLaneBuilders[i];
                    var B = mapLaneBuilders[i + 1];
                    A.leftNeighborReverse = B;
                    B.leftNeighborReverse = A;
                }
            }
            else
            {
                for (int i = 0; i < mapLaneBuilders.Count - 1; i++)
                {
                    var A = mapLaneBuilders[i];
                    var B = mapLaneBuilders[i + 1];
                    A.rightNeighborForward = B;
                    B.leftNeighborForward = A;
                }
            }
        }
    }
}