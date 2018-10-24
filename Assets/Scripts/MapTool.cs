/**
* Copyright (c) 2018 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/


using Apollo;
using Autoware;
using System.Collections.Generic;
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
    }
}