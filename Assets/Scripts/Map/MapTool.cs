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
        public static bool showMapSelected;

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

        public static bool JointTwoMapLaneSegments(List<MapLaneSegmentBuilder> laneBuilders, bool mergeConnectionPoint)
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

            System.Action<MapLaneSegmentBuilder, MapLaneSegmentBuilder> JoinLaneBuilders;
            if (mergeConnectionPoint)
            {
                JoinLaneBuilders = (A, B) => {
                    var lastPt = A.transform.TransformPoint(A.segment.targetLocalPositions[A.segment.targetLocalPositions.Count - 1]);
                    var firstPt_cmp = B.transform.TransformPoint(B.segment.targetLocalPositions[0]);
                    var mergePt = (lastPt + firstPt_cmp) * .5f;
                    A.segment.targetLocalPositions[A.segment.targetLocalPositions.Count - 1] = A.transform.InverseTransformPoint(mergePt);
                    for (int i = 1; i < B.segment.targetLocalPositions.Count; i++)
                    {
                        var newPt = B.transform.TransformPoint(B.segment.targetLocalPositions[i]);
                        A.segment.targetLocalPositions.Add(A.transform.InverseTransformPoint(newPt));
                    }
                    DestroyImmediate(B.gameObject);
                };
            }
            else
            {
                JoinLaneBuilders = (A, B) => {
                    for (int i = 0; i < B.segment.targetLocalPositions.Count; i++)
                    {
                        var newPt = B.transform.TransformPoint(B.segment.targetLocalPositions[i]);
                        A.segment.targetLocalPositions.Add(A.transform.InverseTransformPoint(newPt));
                    }
                    DestroyImmediate(B.gameObject);
                };
            }

            var lane_A = laneBuilders[0];
            var lane_B = laneBuilders[1];

            var A_first = lane_A.transform.TransformPoint(lane_A.segment.targetLocalPositions[0]);
            var A_last = lane_A.transform.TransformPoint(lane_A.segment.targetLocalPositions[lane_A.segment.targetLocalPositions.Count - 1]);
            var B_first = lane_B.transform.TransformPoint(lane_B.segment.targetLocalPositions[0]);
            var B_last = lane_B.transform.TransformPoint(lane_B.segment.targetLocalPositions[lane_B.segment.targetLocalPositions.Count - 1]);

            if (Vector3.Distance(A_last, B_first) < Vector3.Distance(B_last, A_first))
            {
                JoinLaneBuilders(lane_A, lane_B);
            }
            else
            {
                JoinLaneBuilders(lane_B, lane_A);
            }

            return true;
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

        public static void AutoGenerateNewLane(Vector3 startPoint, Vector3 startAimVector, float startTangent, Vector3 endPoint, Vector3 endAimVector, float endTangent, int count, out List<Vector3> retPoints)
        {
            retPoints = new List<Vector3>();
            retPoints.Add(startPoint);
            var interval = 1f / (float)count;
            for (float f = interval; f < 0.999f; f += interval)
            {
                var mid = GetBezierPosition(startPoint, startAimVector, startTangent, endPoint, endAimVector, endTangent, f);
                retPoints.Add(mid);
            }            
            retPoints.Add(endPoint);
        }
        
        private static Vector3 GetBezierPosition(Vector3 startPoint, Vector3 startAimVector, float startTangent, Vector3 endPoint, Vector3 endAimVector, float endTangent, float f)
        {
            Vector3 p0 = startPoint;
            Vector3 p1 = p0 + startAimVector * startTangent;
            Vector3 p3 = endPoint;
            Vector3 p2 = p3 - endAimVector * endTangent;
            
            return Mathf.Pow(1f - f, 3f) * p0 + 3f * Mathf.Pow(1f - f, 2f) * f * p1 + 3f * (1f - f) * Mathf.Pow(f, 2f) * p2 + Mathf.Pow(f, 3f) * p3;
        }
    }
}