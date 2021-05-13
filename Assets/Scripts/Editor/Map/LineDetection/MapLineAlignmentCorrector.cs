/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.MapLineDetection
{
    using System.Collections.Generic;
    using System.Linq;
    using Simulator.Map;
    using Simulator.Map.LineDetection;
    using UnityEditor;
    using UnityEngine;

    public class MapLineAlignmentCorrector
    {
        private class LaneLineSegments
        {
            public SegmentedLine3D leftLine;
            public SegmentedLine3D rightLine;
        }

        private readonly LineDetectionSettings settings;
        private readonly List<SegmentedLine3D> segments = new List<SegmentedLine3D>();
        private List<SegmentedLine3D> connectedSegments;

        public MapLineAlignmentCorrector(LineDetectionSettings settings)
        {
            this.settings = settings;
        }

        public void AddSegments(List<SegmentedLine3D> newSegments)
        {
            segments.AddRange(newSegments);
        }

        public Dictionary<MapTrafficLane, LaneLineOverrideData> Process()
        {
            var mapManagerData = new MapManagerData();
            var lanes = mapManagerData.GetTrafficLanes();

            var lineDict = new Dictionary<MapLine, SegmentedLine3D>();
            var laneDict = new Dictionary<MapTrafficLane, LaneLineSegments>();

            foreach (var lane in lanes)
            {
                var laneData = new LaneLineSegments();

                if (lineDict.ContainsKey(lane.rightLineBoundry))
                    laneData.rightLine = lineDict[lane.rightLineBoundry];
                else
                {
                    var segment = LaneLineToSegmentedLine(lane.rightLineBoundry);
                    laneData.rightLine = segment;
                    lineDict[lane.rightLineBoundry] = segment;
                }

                if (lineDict.ContainsKey(lane.leftLineBoundry))
                    laneData.leftLine = lineDict[lane.leftLineBoundry];
                else
                {
                    var segment = LaneLineToSegmentedLine(lane.leftLineBoundry);
                    laneData.leftLine = segment;
                    lineDict[lane.leftLineBoundry] = segment;
                }

                laneDict.Add(lane, laneData);
            }

            connectedSegments = ConnectSegments(segments, settings);

            const float correctionThreshold = 1f;
            SegmentedLine3D prevMatch = null;

            var index = 0;
            try
            {
                foreach (var mapLine in lineDict.Values)
                {
                    var progress = (float) index++ / lineDict.Count;
                    EditorUtility.DisplayProgressBar("Correcting HD map lines", $"Processing lane {index} of {lineDict.Count}", progress);

                    for (var i = 0; i < mapLine.lines.Count; ++i)
                    {
                        Vector3 start, end;

                        if (i == 0)
                        {
                            var startVec = mapLine.lines[i].Vector;
                            var startnVec = new Vector2(-startVec.z, startVec.x).normalized * correctionThreshold;
                            start = FindBestSegmentMatch(mapLine.lines[i].Start, startnVec, prevMatch, out prevMatch);
                        }
                        else
                            start = mapLine.lines[i - 1].End;

                        var vec = mapLine.lines[i].Vector;
                        if (i != mapLine.lines.Count - 1)
                            vec += mapLine.lines[i + 1].Vector;
                        var nVec = new Vector2(-vec.z, vec.x).normalized * correctionThreshold;
                        end = FindBestSegmentMatch(mapLine.lines[i].End, nVec, prevMatch, out prevMatch);
                        var width = mapLine.lines[i].width;
                        var color = mapLine.lines[i].color;
                        mapLine.lines[i] = new Line3D(start, end, width) {color = color};
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            var result = new Dictionary<MapTrafficLane, LaneLineOverrideData>();

            foreach (var kvp in laneDict)
            {
                var entry = new LaneLineOverrideData();

                var rightPoints = new List<Vector3>();
                rightPoints.Add(kvp.Value.rightLine.lines[0].Start);
                for (var i = 0; i < kvp.Value.rightLine.lines.Count; ++i)
                    rightPoints.Add(kvp.Value.rightLine.lines[i].End);
                entry.rightLineWorldPositions = rightPoints;

                var leftPoints = new List<Vector3>();
                leftPoints.Add(kvp.Value.leftLine.lines[0].Start);
                for (var i = 0; i < kvp.Value.leftLine.lines.Count; ++i)
                    leftPoints.Add(kvp.Value.leftLine.lines[i].End);
                entry.leftLineWorldPositions = leftPoints;

                result.Add(kvp.Key, entry);
            }

            return result;
        }

        private static float Angle(Vector2 vecA, Vector2 vecB)
        {
            return Mathf.Min(Vector2.Angle(vecA, vecB), Vector2.Angle(vecA, -vecB));
        }

        private Vector3 FindBestSegmentMatch(Vector3 position, Vector2 nVector, SegmentedLine3D previousMatchedSegment, out SegmentedLine3D matchedSegment)
        {
            const float angleThreshold = 30f;

            var pos2D = new Vector2(position.x, position.z);
            var nVec0 = pos2D - nVector;
            var nVec1 = pos2D + nVector;
            var nVec = nVec1 - nVec0;

            var bestSqrMag = float.MaxValue;
            var bestPos = Vector3.zero;
            var matchFound = false;

            SegmentedLine3D currentMatchedSegment = null;

            void CheckSegment(SegmentedLine3D segment)
            {
                // foreach (var line in segment.lines)
                for (var i = 0; i < segment.lines.Count; ++i)
                {
                    var lnStart = segment.lines[i].Start;
                    var lnEnd = segment.lines[i].End;

                    if (i == 0)
                        lnStart -= segment.lines[i].Vector.normalized;
                    if (i == segment.lines.Count - 1)
                        lnEnd += segment.lines[i].Vector.normalized;

                    var lnStart2D = new Vector2(lnStart.x, lnStart.z);
                    var lnEnd2D = new Vector2(lnEnd.x, lnEnd.z);

                    if (Angle(lnEnd2D - lnStart2D, nVec) < 90 - angleThreshold)
                        continue;

                    LineUtils.LineLineIntersection(nVec0, nVec1, lnStart2D, lnEnd2D, out var _, out var segmentsIntersect, out var intersection);
                    if (!segmentsIntersect)
                        continue;

                    var sqrMag = (intersection - pos2D).sqrMagnitude;
                    if (sqrMag < bestSqrMag)
                    {
                        matchFound = true;
                        bestSqrMag = sqrMag;
                        currentMatchedSegment = segment;
                        bestPos = Vector3.Lerp(lnStart, lnEnd, (intersection.x - lnStart.x) / (lnEnd.x - lnStart.x));
                    }
                }
            }

            if (previousMatchedSegment != null)
                CheckSegment(previousMatchedSegment);

            if (!matchFound)
            {
                foreach (var segment in connectedSegments)
                    CheckSegment(segment);
            }

            matchedSegment = currentMatchedSegment;

            return !matchFound ? position : bestPos;
        }

        private static SegmentedLine3D LaneLineToSegmentedLine(MapLine laneLine)
        {
            var lines = new List<Line3D>();
            var wPos = laneLine.mapWorldPositions;

            for (var i = 1; i < wPos.Count; ++i)
                lines.Add(new Line3D(wPos[i - 1], wPos[i]));

            return new SegmentedLine3D(lines);
        }

        private static List<SegmentedLine3D> ConnectSegments(List<SegmentedLine3D> originalSegments, LineDetectionSettings settings)
        {
            var result = originalSegments.Select(x => x.Clone()).ToList();
            var distanceThreshold = settings.worldDottedLineDistanceThreshold;
            var angleThreshold = settings.worldSpaceSnapAngle;

            for (var i = 0; i < result.Count; ++i)
            {
                var bestMatchIndex = -1;
                var bestMatchDist = float.MaxValue;
                var bestMatchUseStartA = false;
                var bestMatchUseStartB = false;

                for (var j = i + 1; j < result.Count; ++j)
                {
                    FindClosestEnds(result[i], result[j], out var useStartA, out var useStartB);
                    var iPos = useStartA ? result[i].Start : result[i].End;
                    var jPos = useStartB ? result[j].Start : result[j].End;
                    var iVec = useStartA ? -result[i].StartVectorXZ : result[i].EndVectorXZ;
                    var jVec = useStartB ? result[j].StartVectorXZ : -result[j].EndVectorXZ;
                    var ijDist = Vector3.Distance(iPos, jPos);

                    if (Mathf.Abs(iPos.y - jPos.y) > 2f)
                        continue;

                    if (ijDist > distanceThreshold || Angle(iVec, jVec) > angleThreshold)
                        continue;

                    var joinVec = iPos - jPos;
                    var joinVecXZ = new Vector2(joinVec.x, joinVec.z);

                    if (Angle(joinVecXZ, jVec) > 0.5f * angleThreshold || Angle(joinVecXZ, iVec) > 0.5f * angleThreshold)
                        continue;

                    if (ijDist < bestMatchDist)
                    {
                        bestMatchDist = ijDist;
                        bestMatchIndex = j;
                        bestMatchUseStartA = useStartA;
                        bestMatchUseStartB = useStartB;
                    }
                }

                if (bestMatchIndex == -1)
                    continue;

                if (bestMatchUseStartA)
                    result[i].Invert();

                if (!bestMatchUseStartB)
                    result[bestMatchIndex].Invert();

                result[i].lines.Add(new Line3D(result[i].End, result[bestMatchIndex].Start));
                result[i].lines.AddRange(result[bestMatchIndex].lines);
                result.RemoveAt(bestMatchIndex);

                i = -1;
            }

            return result;
        }

        private static void FindClosestEnds(SegmentedLine3D segA, SegmentedLine3D segB, out bool useStartA, out bool useStartB)
        {
            var min = (segA.Start - segB.Start).sqrMagnitude;
            useStartA = true;
            useStartB = true;

            var sqrMag = (segA.Start - segB.End).sqrMagnitude;
            if (sqrMag < min)
            {
                min = sqrMag;
                useStartA = true;
                useStartB = false;
            }

            sqrMag = (segA.End - segB.Start).sqrMagnitude;
            if (sqrMag < min)
            {
                min = sqrMag;
                useStartA = false;
                useStartB = true;
            }

            sqrMag = (segA.End - segB.End).sqrMagnitude;
            if (sqrMag < min)
            {
                useStartA = false;
                useStartB = false;
            }
        }
    }
}