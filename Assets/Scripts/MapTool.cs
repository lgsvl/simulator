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

        //joint and convert a set of singlely-connected segments and also setup world positions for all segments
        public static void ConvertAndJointSingleConnectedSegments<T>(MapSegment curSeg, HashSet<MapSegment> allSegs, ref HashSet<MapSegment> newAllSegs, HashSet<MapSegment> visitedSegs) where T : MapTool
        {
            var combSegs = new List<MapSegment>();
            visitedSegs.Add(curSeg);
            combSegs.Add(curSeg);

            while (curSeg.afters.Count == 1 && curSeg.afters[0].befores.Count == 1 && curSeg.afters[0].befores[0] == curSeg && !visitedSegs.Contains((MapSegment)(curSeg.afters[0])))
            {
                if (curSeg.builder as MapLaneSegmentBuilder != null &&
                    (((MapLaneSegmentBuilder)curSeg.builder).leftNeighborForward != ((MapLaneSegmentBuilder)(curSeg.afters[0].builder)).leftNeighborForward ||
                    ((MapLaneSegmentBuilder)curSeg.builder).rightNeighborForward != ((MapLaneSegmentBuilder)(curSeg.afters[0].builder)).rightNeighborForward ||
                    ((MapLaneSegmentBuilder)curSeg.builder).leftNeighborReverse != ((MapLaneSegmentBuilder)(curSeg.afters[0].builder)).leftNeighborReverse ||
                    ((MapLaneSegmentBuilder)curSeg.builder).rightNeighborReverse != ((MapLaneSegmentBuilder)(curSeg.afters[0].builder)).rightNeighborReverse)
                    )
                {
                    break;
                }
                visitedSegs.Add(curSeg.afters[0]);
                combSegs.Add(curSeg.afters[0]);
                curSeg = curSeg.afters[0];
            }

            //determine special type
            System.Type segType = typeof(MapSegment);
            if ((curSeg.builder as MapLaneSegmentBuilder) != null)
            {
                segType = typeof(MapLaneSegment);
            }

            //construct new lane segments in with its wappoints in world positions and update related dependencies to relate to new segments
            MapSegment combinedSeg = new MapSegment();

            if (segType == typeof(MapLaneSegment))
            {
                combinedSeg = new MapLaneSegment();

                var combinedLnSeg = (MapLaneSegment)combinedSeg;

                combinedLnSeg.leftNeighborSegmentForward = ((MapLaneSegmentBuilder)combSegs[0].builder).leftNeighborForward.segment as MapLaneSegment;
                combinedLnSeg.rightNeighborSegmentForward = ((MapLaneSegmentBuilder)combSegs[0].builder).rightNeighborForward.segment as MapLaneSegment;
                combinedLnSeg.leftNeighborSegmentReverse = ((MapLaneSegmentBuilder)combSegs[0].builder).leftNeighborReverse.segment as MapLaneSegment;
                combinedLnSeg.rightNeighborSegmentReverse = ((MapLaneSegmentBuilder)combSegs[0].builder).rightNeighborReverse.segment as MapLaneSegment;

                combinedLnSeg.leftNeighborSegmentForward.rightNeighborSegmentForward = combinedLnSeg;
                combinedLnSeg.rightNeighborSegmentForward.leftNeighborSegmentForward = combinedLnSeg;
                combinedLnSeg.leftNeighborSegmentReverse.rightNeighborSegmentReverse = combinedLnSeg;
                combinedLnSeg.rightNeighborSegmentReverse.leftNeighborSegmentReverse = combinedLnSeg;
            }

            combinedSeg.befores = combSegs[0].befores;
            combinedSeg.afters = combSegs[combSegs.Count - 1].afters;

            foreach (var beforeSeg in combinedSeg.befores)
            {
                for (int i = 0; i < beforeSeg.afters.Count; i++)
                {
                    if (beforeSeg.afters[i] == combSegs[0])
                    {
                        beforeSeg.afters[i] = combinedSeg;
                    }
                }
            }
            foreach (var afterSeg in combinedSeg.afters)
            {
                for (int i = 0; i < afterSeg.befores.Count; i++)
                {
                    if (afterSeg.befores[i] == combSegs[combSegs.Count - 1])
                    {
                        afterSeg.befores[i] = combinedSeg;
                    }
                }
            }

            foreach (var seg in combSegs)
            {
                foreach (var localPos in seg.targetLocalPositions)
                {
                    combinedSeg.targetWorldPositions.Add(seg.builder.transform.TransformPoint(localPos)); //Convert to world position
                }
            }

            if (segType == typeof(MapLaneSegment))
            {
                var combinedLnSeg = combinedSeg as MapLaneSegment;
                if (typeof(T) == typeof(Apollo.HDMapTool))
                {

                }
                if (typeof(T) == typeof(Autoware.VectorMapTool))
                {
                    foreach (var seg in combSegs)
                    {
                        var lnSegBldr = seg.builder as MapLaneSegmentBuilder;

                        int laneCount = lnSegBldr.laneInfo.laneCount;
                        int laneNumber = lnSegBldr.laneInfo.laneNumber;
                        foreach (var localPos in seg.targetLocalPositions)
                        {
                            combinedLnSeg.laneInfos.Add(new Autoware.LaneInfo() { laneCount = laneCount, laneNumber = laneNumber });
                        }
                    }
                }               
            }

            foreach (var seg in combSegs)
            {
                allSegs.Remove(seg);
            }

            newAllSegs.Add(combinedSeg);
        }
    }
}