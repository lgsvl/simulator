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
        public static float PROXIMITY = 1.0f; //0.02f
        public static float ARROWSIZE = 50f; //1.0f

        public float exportScaleFactor = 1.0f;

        public static void DoubleSegmentResolution(MapSegment seg)
        {
            for (int j = 0; j < seg.targetLocalPositions.Count - 1; j++)
            {
                var mid = (seg.targetLocalPositions[j] + seg.targetLocalPositions[j + 1]) / 2f;
                seg.targetLocalPositions.Insert(j + 1, mid);
                ++j;
            }
        }
    }
}