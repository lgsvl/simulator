/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using Autoware;

namespace Map
{
    namespace Autoware
    {
        public class VectorMapTool : MapTool
        {
            public List<Transform> targets;

            public float proximity = PROXIMITY;
            public float arrowSize = ARROWSIZE;

            [VectorMapCSV("point.csv")]
            private List<Point> points = new List<Point>();
            [VectorMapCSV("line.csv")]
            private List<Line> lines = new List<Line>();
            [VectorMapCSV("lane.csv")]
            private List<Lane> lanes = new List<Lane>();
            [VectorMapCSV("dtlane.csv")]
            private List<DtLane> dtLanes = new List<DtLane>();
            [VectorMapCSV("stopline.csv")]
            private List<StopLine> stoplines = new List<StopLine>();
            [VectorMapCSV("whiteline.csv")]
            private List<WhiteLine> whiteLines = new List<WhiteLine>();
            [VectorMapCSV("vector.csv")]
            private List<Vector> vectors = new List<Vector>();
            [VectorMapCSV("pole.csv")]
            private List<Pole> poles = new List<Pole>();
            [VectorMapCSV("signaldata.csv")]
            private List<SignalData> signalDatas = new List<SignalData>();

            struct ExportData
            {
                public IList List;
                public System.Type Type;
                public string FileName;
            }

            List<ExportData> exportLists;

            public string foldername = "vector_map";

            public List<Lane> Lanes
            {
                get
                {
                    return lanes;
                }

                set
                {
                    lanes = value;
                }
            }

            public List<DtLane> DtLanes
            {
                get
                {
                    return dtLanes;
                }

                set
                {
                    dtLanes = value;
                }
            }

            public List<StopLine> Stoplines
            {
                get
                {
                    return stoplines;
                }

                set
                {
                    stoplines = value;
                }
            }

            public List<WhiteLine> WhiteLines
            {
                get
                {
                    return whiteLines;
                }

                set
                {
                    whiteLines = value;
                }
            }

            public List<Vector> Vectors
            {
                get
                {
                    return vectors;
                }

                set
                {
                    vectors = value;
                }
            }

            public List<Pole> Poles
            {
                get
                {
                    return poles;
                }

                set
                {
                    poles = value;
                }
            }

            enum TerminalType
            {
                START = 0,
                END,
            }

            enum LineType
            {
                NONE = -1,
                STOP = 0,
                WHITE,
                YELLOW,
            }

            VectorMapTool()
            {
                exportLists = new List<ExportData>();
                var categoryList = GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(e => System.Attribute.IsDefined(e, typeof(VectorMapCSVAttribute)));
                foreach (var c in categoryList)
                {
                    exportLists.Add(new ExportData()
                    {
                        List = (IList)c.GetValue(this),
                        Type = c.FieldType.GetGenericArguments()[0],
                        FileName = c.GetCustomAttribute<VectorMapCSVAttribute>().FileName,
                    });
                }

                PROXIMITY = proximity;
                ARROWSIZE = arrowSize;
            }

            void OnEnable()
            {
                if (proximity != PROXIMITY)
                {
                    PROXIMITY = proximity;
                }
                if (arrowSize != ARROWSIZE)
                {
                    ARROWSIZE = arrowSize;
                }
            }

            void OnValidate()
            {
                if (proximity != PROXIMITY)
                {
                    PROXIMITY = proximity;
                }
                if (arrowSize != ARROWSIZE)
                {
                    ARROWSIZE = arrowSize;
                }
            }

            public void ExportVectorMap()
            {
                if (Calculate())
                {
                    Export();
                }
            }

            bool Calculate()
            {
                exportLists.ForEach(e => e.List.Clear()); //clear all vector map data before calculate

                //list of target transforms
                var targetList = new List<Transform>();
                var noTarget = true;
                foreach (var t in targets)
                {
                    if (t != null)
                    {
                        noTarget = false;
                        targetList.Add(t);
                    }
                }
                if (noTarget)
                {
                    targetList.Add(transform);
                }

                //initial collection
                var segBldrs = new List<MapSegmentBuilder>();
                var signalLightPoles = new List<VectorMapPoleBuilder>();
                foreach (var t in targetList)
                {
                    if (t == null)
                    {
                        continue;
                    }

                    var vmsb = t.GetComponentsInChildren<MapSegmentBuilder>();
                    var vmp = t.GetComponentsInChildren<VectorMapPoleBuilder>();

                    segBldrs.AddRange(vmsb);
                    signalLightPoles.AddRange(vmp);
                }

                bool missingPoints = false;

                var allSegs = new HashSet<MapSegment>(); //All segments regardless of segment actual type

                //connect builder reference for each segment
                foreach (var segBldr in segBldrs)
                {
                    segBldr.segment.builder = segBldr;
                    allSegs.Add(segBldr.segment);
                }

                //Link before and after segment for each segment
                foreach (var segment in allSegs)
                {
                    //Make sure to clear unwanted leftover data from previous generation
                    segment.Clear();
                    segment.vectormapInfo = new VectorMapSegmentInfo();

                    //this is to avoid accidentally connect two nearby stoplines
                    if ((segment.builder as MapStopLineSegmentBuilder) != null)
                    {
                        continue;
                    }

                    //each segment must have at least 2 waypoints for calculation so complement to 2 waypoints as needed
                    while (segment.targetLocalPositions.Count < 2)
                    {
                        segment.targetLocalPositions.Add(Vector3.zero);
                        missingPoints = true;
                    }

                    var firstPt = segment.builder.transform.TransformPoint(segment.targetLocalPositions[0]);
                    var lastPt = segment.builder.transform.TransformPoint(segment.targetLocalPositions[segment.targetLocalPositions.Count - 1]);

                    foreach (var segment_cmp in allSegs)
                    {
                        if (segment_cmp.builder.GetType() != segment.builder.GetType()) //only connect same actual type
                        {
                            continue;
                        }

                        var firstPt_cmp = segment_cmp.builder.transform.TransformPoint(segment_cmp.targetLocalPositions[0]);
                        var lastPt_cmp = segment_cmp.builder.transform.TransformPoint(segment_cmp.targetLocalPositions[segment_cmp.targetLocalPositions.Count - 1]);

                        if ((firstPt - lastPt_cmp).magnitude < PROXIMITY / exportScaleFactor)
                        {
                            segment.befores.Add(segment_cmp);
                        }

                        if ((lastPt - firstPt_cmp).magnitude < PROXIMITY / exportScaleFactor)
                        {
                            segment.afters.Add(segment_cmp);
                        }
                    }
                }

                if (missingPoints)
                {
                    Debug.Log("Some segment has less than 2 waypoints, complement it to 2");
                }

                var allLnSegs = new HashSet<MapSegment>();
                var allLinSegs = new HashSet<MapSegment>();

                foreach (var segment in allSegs)
                {
                    var type = segment.builder.GetType();
                    if (type == typeof(MapLaneSegmentBuilder))
                    {
                        allLnSegs.Add(segment);
                    }
                    else if (type == typeof(VectorMapStopLineSegmentBuilder) || type == typeof(MapStopLineSegmentBuilder))
                    {
                        allLinSegs.Add(segment);
                    }
                    else if (type == typeof(MapBoundaryLineSegmentBuilder))
                    {
                        allLinSegs.Add(segment);
                    }
                }

                //New sets for newly converted(to world space) segments
                var allConvertedLnSeg = new HashSet<MapSegment>(); //for lane segments
                var allConvertedLinSeg = new HashSet<MapSegment>(); //for line segments

                //Filter and convert all lane segments
                if (allLnSegs.Count > 0)
                {
                    var startLnSegs = new HashSet<MapSegment>(); //The lane segments that are at merging or forking or starting position
                    var visitedLnSegs = new HashSet<MapSegment>(); //tracking for record

                    foreach (var lnSeg in allLnSegs)
                    {
                        if (lnSeg.befores.Count != 1 || (lnSeg.befores.Count == 1 && lnSeg.befores[0].afters.Count > 1)) //no any before segments
                        {
                            startLnSegs.Add(lnSeg);
                        }
                    }

                    foreach (var startLnSeg in startLnSegs)
                    {
                        ConvertAndJointSingleConnectedSegments(startLnSeg, allLnSegs, ref allConvertedLnSeg, visitedLnSegs);
                    }

                    while (allLnSegs.Count > 0)//Remaining should be isolated loops
                    {
                        MapSegment pickedLnSeg = null;
                        foreach (var lnSeg in allLnSegs)
                        {
                            pickedLnSeg = lnSeg;
                            break;
                        }
                        if (pickedLnSeg != null)
                        {
                            ConvertAndJointSingleConnectedSegments(pickedLnSeg, allLnSegs, ref allConvertedLnSeg, visitedLnSegs);
                        }
                    }
                }

                //convert all line segments
                if (allLinSegs.Count > 0)
                {
                    //setup world positions and their proper befores and afters for all line segments
                    foreach (var linSeg in allLinSegs)
                    {
                        var convertedSeg = new MapSegment();
                        convertedSeg.builder = linSeg.builder;
                        foreach (var localPos in linSeg.targetLocalPositions)
                        {
                            convertedSeg.targetWorldPositions.Add(linSeg.builder.transform.TransformPoint(localPos)); //Convert to world position
                        }

                        convertedSeg.befores = linSeg.befores;
                        convertedSeg.afters = linSeg.afters;

                        foreach (var beforeSeg in convertedSeg.befores)
                        {
                            for (int i = 0; i < beforeSeg.afters.Count; i++)
                            {
                                if (beforeSeg.afters[i] == linSeg)
                                {
                                    beforeSeg.afters[i] = convertedSeg;
                                }
                            }
                        }

                        foreach (var afterSeg in convertedSeg.afters)
                        {
                            for (int i = 0; i < afterSeg.befores.Count; i++)
                            {
                                if (afterSeg.befores[i] == linSeg)
                                {
                                    afterSeg.befores[i] = convertedSeg;
                                }
                            }
                        }

                        allConvertedLinSeg.Add(convertedSeg);
                    }
                }

                //set up all lanes vector map data
                var lnSegTerminalIDsMapping = new Dictionary<MapSegment, int[]>(); //tracking for record
                if (allConvertedLnSeg.Count > 0)
                {
                    foreach (var lnSeg in allConvertedLnSeg)
                    {
                        List<Vector3> positions;
                        List<Autoware.LaneInfo> laneInfos;
                        var waypointLaneInfos = lnSeg.vectormapInfo.laneInfos;

                        //Interpolate based on waypoint world positions
                        Interpolate(lnSeg.targetWorldPositions, waypointLaneInfos, out positions, out laneInfos, 1.0f / exportScaleFactor, true); //interpolate and divide to ensure 1 meter apart

                        lnSegTerminalIDsMapping.Add(lnSeg, new int[2]);
                        for (int i = 0; i < positions.Count; i++)
                        {
                            //Point
                            var pos = positions[i];
                            var vectorMapPos = VectorMapUtility.GetVectorMapPosition(pos, exportScaleFactor);
                            var vmPoint = Point.MakePoint(points.Count + 1, vectorMapPos.Bx, vectorMapPos.Ly, vectorMapPos.H);
                            points.Add(vmPoint);

                            //DtLane
                            var deltaDir = Vector3.zero;
                            if (i < positions.Count - 1)
                            {
                                deltaDir = positions[i + 1] - pos;
                            }
                            else
                            {
                                if (lnSeg.afters.Count > 0)
                                {
                                    deltaDir = lnSeg.afters[0].targetWorldPositions[0] - pos;
                                }
                                else
                                {
                                    deltaDir = pos - positions[i - 1];
                                }
                            }
                            Vector3 eulerAngles = Quaternion.FromToRotation(Vector3.right, deltaDir).eulerAngles;
                            var convertedEulerAngles = VectorMapUtility.GetRvizCoordinates(eulerAngles);
                            var vmDTLane = DtLane.MakeDtLane(DtLanes.Count + 1, i, vmPoint.PID, -convertedEulerAngles.z * Mathf.Deg2Rad, .0, .1, .1);
                            DtLanes.Add(vmDTLane);

                            //Lane
                            int beforeLaneID = 0;
                            int laneId = Lanes.Count + 1;
                            if (i > 0)
                            {
                                beforeLaneID = Lanes.Count;
                                var beforeLane = Lanes[beforeLaneID - 1];
                                beforeLane.FLID = laneId;
                                Lanes[beforeLaneID - 1] = beforeLane; //This is needed for struct copy to be applied back
                            }
                            var vmLane = Lane.MakeLane(laneId, vmDTLane.DID, beforeLaneID, 0, laneInfos[i].laneCount, laneInfos[i].laneNumber);
                            Lanes.Add(vmLane);

                            if (i == 0)
                            {
                                lnSegTerminalIDsMapping[lnSeg][(int)TerminalType.START] = vmLane.LnID;
                            }
                            if (i == positions.Count - 1)
                            {
                                lnSegTerminalIDsMapping[lnSeg][(int)TerminalType.END] = vmLane.LnID;
                            }
                        }
                    }

                    //Assuming merging and diversing won't happen at the same spot
                    //correcting start and end lanes's BLID and FLID to the lane segment's before and after lane IDs in other lane segments
                    //each lane will have up to 4 before lane Ids and after lane Ids to set, later executed operation will be ignore if number exceeds 4
                    //correcting DtLanes last lane's dir value
                    foreach (var lnSeg in allConvertedLnSeg)
                    {
                        var terminalIdPair = lnSegTerminalIDsMapping[lnSeg];
                        if (lnSeg.befores.Count > 0)
                        {
                            var segStartId = terminalIdPair[(int)TerminalType.START];
                            var ln = Lanes[segStartId - 1];

                            for (int i = 0; i < lnSeg.befores.Count; i++)
                            {
                                int beforeSegAfterLn = lnSegTerminalIDsMapping[lnSeg.befores[i]][(int)TerminalType.END];
                                //chose the BLID that has not been set to meaningful lane id
                                if (ln.BLID == 0)
                                {
                                    ln.BLID = beforeSegAfterLn;
                                }
                                else if (ln.BLID2 == 0)
                                {
                                    ln.BLID2 = beforeSegAfterLn;
                                    //ln.JCT = 3; //means merging
                                }
                                else if (ln.BLID3 == 0)
                                {
                                    ln.BLID3 = beforeSegAfterLn;
                                }
                                else if (ln.BLID4 == 0)
                                {
                                    ln.BLID4 = beforeSegAfterLn;
                                }
                            }

                            Lanes[segStartId - 1] = ln;
                        }

                        if (lnSeg.afters.Count > 0)
                        {
                            var segEndId = terminalIdPair[(int)TerminalType.END];
                            var ln = Lanes[segEndId - 1];

                            int afterSegStartLn = lnSegTerminalIDsMapping[lnSeg.afters[0]][(int)TerminalType.START];
                            for (int i = 0; i < lnSeg.afters.Count; i++)
                            {
                                afterSegStartLn = lnSegTerminalIDsMapping[lnSeg.afters[i]][(int)TerminalType.START];
                                //chose the FLID that has not been set to meaningful lane id
                                if (ln.FLID == 0)
                                {
                                    ln.FLID = afterSegStartLn;
                                }
                                else if (ln.FLID2 == 0)
                                {
                                    ln.FLID2 = afterSegStartLn;
                                    ln.JCT = 1; //means branching
                                }
                                else if (ln.FLID3 == 0)
                                {
                                    ln.FLID3 = afterSegStartLn;
                                }
                                else if (ln.FLID4 == 0)
                                {
                                    ln.FLID4 = afterSegStartLn;
                                }
                            }

                            Lanes[segEndId - 1] = ln;

                            //Adjust last dtlane of each lane segment
                            var endDtLn = DtLanes[Lanes[segEndId - 1].DID - 1];
                            var DtLnAfter = DtLanes[Lanes[afterSegStartLn - 1].DID - 1];

                            var pointPos = VectorMapUtility.GetUnityPosition(new VectorMapPosition() { Bx = points[endDtLn.PID - 1].Bx, Ly = points[endDtLn.PID - 1].Ly, H = points[endDtLn.PID - 1].H }, exportScaleFactor);
                            var pointAfterPos = VectorMapUtility.GetUnityPosition(new VectorMapPosition() { Bx = points[DtLnAfter.PID - 1].Bx, Ly = points[DtLnAfter.PID - 1].Ly, H = points[DtLnAfter.PID - 1].H }, exportScaleFactor);
                            var deltaDir = pointAfterPos - pointPos;
                            Vector3 eulerAngles = Quaternion.FromToRotation(Vector3.right, deltaDir).eulerAngles;
                            var convertedEulerAngles = VectorMapUtility.GetRvizCoordinates(eulerAngles);
                            endDtLn.Dir = -convertedEulerAngles.z * Mathf.Deg2Rad;
                            DtLanes[endDtLn.DID - 1] = endDtLn;
                        }
                    }
                }

                //set up all lines vector map data
                var linSegTerminalIDsMapping = new Dictionary<MapSegment, KeyValuePair<int, LineType>[]>(); //tracking for record
                var stopLinkIDMapping = new Dictionary<MapStopLineSegmentBuilder, List<int>>();
                if (allConvertedLinSeg.Count > 0)
                {
                    foreach (var linSeg in allConvertedLinSeg)
                    {
                        var linType = LineType.NONE;
                        if (linSeg.builder.GetType() == typeof(MapStopLineSegmentBuilder)) //If it is stopline
                        {
                            linType = LineType.STOP;
                        }
                        else if (linSeg.builder.GetType() == typeof(MapBoundaryLineSegmentBuilder)) //if it is whiteline
                        {
                            var whiteLineBuilder = linSeg.builder as MapBoundaryLineSegmentBuilder;
                            if (whiteLineBuilder.lineType == Map.BoundLineType.SOLID_WHITE || whiteLineBuilder.lineType == Map.BoundLineType.DOUBLE_WHITE || whiteLineBuilder.lineType == Map.BoundLineType.DOTTED_WHITE)
                            {
                                linType = LineType.WHITE;
                            }
                            else if (whiteLineBuilder.lineType == Map.BoundLineType.SOLID_YELLOW || whiteLineBuilder.lineType == Map.BoundLineType.DOUBLE_YELLOW || whiteLineBuilder.lineType == Map.BoundLineType.DOTTED_YELLOW)
                            {
                                linType = LineType.YELLOW;
                            }
                        }

                        var positions = linSeg.targetWorldPositions;

                        linSegTerminalIDsMapping.Add(linSeg, new KeyValuePair<int, LineType>[2] { new KeyValuePair<int, LineType>(-1, LineType.NONE), new KeyValuePair<int, LineType>(-1, LineType.NONE) });
                        for (int i = 0; i < positions.Count - 1; i++)
                        {
                            //Point
                            var startPos = positions[i];
                            var endPos = positions[i + 1];

                            var vectorMapPos = VectorMapUtility.GetVectorMapPosition(startPos, exportScaleFactor);
                            var vmStartPoint = Point.MakePoint(points.Count + 1, vectorMapPos.Bx, vectorMapPos.Ly, vectorMapPos.H);
                            points.Add(vmStartPoint);

                            vectorMapPos = VectorMapUtility.GetVectorMapPosition(endPos, exportScaleFactor);
                            var vmEndPoint = Point.MakePoint(points.Count + 1, vectorMapPos.Bx, vectorMapPos.Ly, vectorMapPos.H);
                            points.Add(vmEndPoint);

                            //Line
                            int beforeLineID = 0;
                            if (i > 0)
                            {
                                beforeLineID = lines.Count;
                                var beforeLine = lines[beforeLineID - 1];
                                beforeLine.FLID = lines.Count + 1;
                                lines[beforeLineID - 1] = beforeLine; //This is needed for struct copy to be applied back
                            }
                            var vmLine = Line.MakeLine(lines.Count + 1, vmStartPoint.PID, vmEndPoint.PID, beforeLineID, 0);
                            lines.Add(vmLine);

                            if (i == 0)
                            {
                                linSegTerminalIDsMapping[linSeg][(int)TerminalType.START] = new KeyValuePair<int, LineType>(vmLine.LID, linType);
                            }
                            if (i == positions.Count - 2)
                            {
                                linSegTerminalIDsMapping[linSeg][(int)TerminalType.END] = new KeyValuePair<int, LineType>(vmLine.LID, linType);
                            }

                            int LinkID;
                            MakeVisualLine(vmLine, linType, out LinkID);
                            if (linType == LineType.STOP)
                            {
                                var builder = (MapStopLineSegmentBuilder)linSeg.builder;
                                if (stopLinkIDMapping.ContainsKey(builder))
                                {
                                    stopLinkIDMapping[builder].Add(LinkID);
                                    //Debug.Log("Extra IDs for same builder: " + stopLinkIDMapping[builder].Count);
                                }
                                else
                                {
                                    stopLinkIDMapping.Add((MapStopLineSegmentBuilder)linSeg.builder, new List<int>() { LinkID });
                                }
                            }
                        }
                    }

                    //correcting each segment's start and end line segments BLID and FLID to new line segments that is between each segment and their adjacent segment
                    //only make BLID/FLID set to only one of the new line segments' last/first line ID because in the vector map format one line only has one BLID and one FLID
                    //Later executed operation will overrite previously configured BLID and/or FLID
                    foreach (var linSeg in allConvertedLinSeg)
                    {
                        var terminalIdPairs = linSegTerminalIDsMapping[linSeg];
                        if (linSeg.befores.Count > 0 && linSegTerminalIDsMapping.ContainsKey(linSeg.befores[0])) //if before line doesn't exist in the record set then no operation to avoid extra in between line
                        {
                            var tPairs = terminalIdPairs[(int)TerminalType.START];
                            var segStartId = tPairs.Key;
                            var segStartLin = lines[segStartId - 1];
                            var beforeSegEndLinId = linSegTerminalIDsMapping[linSeg.befores[0]][(int)TerminalType.END].Key; //only make BLID set to only one of the line segments' last line ID
                            var newLin = Line.MakeLine(lines.Count + 1, lines[beforeSegEndLinId - 1].FPID, segStartLin.BPID, beforeSegEndLinId, segStartLin.LID);
                            lines.Add(newLin);
                            MakeVisualLine(newLin, tPairs.Value);
                            segStartLin.BLID = beforeSegEndLinId;
                            lines[segStartId - 1] = segStartLin;
                        }

                        if (linSeg.afters.Count > 0 && linSegTerminalIDsMapping.ContainsKey(linSeg.afters[0])) //if before line doesn't exist in the record set then no operation to avoid extra in between line
                        {
                            var tPairs = terminalIdPairs[(int)TerminalType.END];
                            var segEndId = tPairs.Key;
                            var segEndLin = lines[segEndId - 1];
                            var afterSegStartLinId = linSegTerminalIDsMapping[linSeg.afters[0]][(int)TerminalType.START].Key; //only make FLID set to only one of the line segments' first line ID
                            var newLin = Line.MakeLine(lines.Count + 1, segEndLin.FPID, lines[afterSegStartLinId - 1].BPID, segEndLin.LID, afterSegStartLinId);
                            lines.Add(newLin);
                            MakeVisualLine(newLin, tPairs.Value);
                            segEndLin.FLID = newLin.LID;
                            lines[segEndId - 1] = segEndLin;
                        }
                    }
                }

                //Setup all traffic light poles and their corrsponding traffic lights
                var tempMapping = new Dictionary<VectorMapPoleBuilder, int>();
                foreach (var pole in signalLightPoles)
                {
                    //Vector
                    var pos = pole.transform.position;
                    var vectorMapPos = VectorMapUtility.GetVectorMapPosition(pos, exportScaleFactor);
                    var PID = points.Count + 1;
                    var vmPoint = Point.MakePoint(PID, vectorMapPos.Bx, vectorMapPos.Ly, vectorMapPos.H);
                    points.Add(vmPoint);

                    var VID = Vectors.Count + 1;
                    float Vang = Vector3.Angle(pole.transform.forward, Vector3.up);
                    float Hang = .0f;
                    if (Vang != .0f)
                    {
                        var projectedHorizonVec = Vector3.ProjectOnPlane(pole.transform.forward, Vector3.up);
                        Hang = Vector3.Angle(projectedHorizonVec, Vector3.forward) * (Vector3.Cross(Vector3.forward, projectedHorizonVec).y > 0 ? 1 : -1);
                    }
                    var vmVector = Vector.MakeVector(VID, PID, Hang, Vang);
                    Vectors.Add(vmVector);

                    float Length = pole.length;
                    float Dim = .4f;
                    var PLID = Poles.Count + 1;
                    var vmPole = Pole.MakePole(PLID, VID, Length, Dim);
                    Poles.Add(vmPole);
                    tempMapping.Add(pole, PLID);
                }


                foreach (var pole in signalLightPoles)
                {
                    var PLID = tempMapping[pole];
                    foreach (var signalLight in pole.signalLights)
                    {
                        if (signalLight == null || !signalLight.gameObject.activeInHierarchy)
                        {
                            continue;
                        }

                        var trafficLightAim = signalLight.transform.forward;
                        foreach (var lightData in signalLight.signalDatas)
                        {
                            //Vector
                            var trafficLightPos = signalLight.transform.TransformPoint(lightData.localPosition);
                            var vectorMapPos = VectorMapUtility.GetVectorMapPosition(trafficLightPos, exportScaleFactor);
                            var PID = points.Count + 1;
                            var vmPoint = Point.MakePoint(PID, vectorMapPos.Bx, vectorMapPos.Ly, vectorMapPos.H);
                            points.Add(vmPoint);

                            var VID = Vectors.Count + 1;
                            float Vang = Vector3.Angle(trafficLightAim, Vector3.up);
                            float Hang = .0f;
                            if (Vang != .0f)
                            {
                                var projectedHorizonVec = Vector3.ProjectOnPlane(trafficLightAim, Vector3.up);
                                Hang = Vector3.Angle(projectedHorizonVec, Vector3.forward) * (Vector3.Cross(Vector3.forward, projectedHorizonVec).y > 0 ? 1 : -1);
                            }
                            var vmVector = Vector.MakeVector(VID, PID, Hang, Vang);
                            Vectors.Add(vmVector);

                            //Signaldata
                            int ID = signalDatas.Count + 1;
                            int Type = (int)lightData.type;
                            int LinkID = -1;

                            if (signalLight.hintStopline != null)
                            {
#if UNITY_EDITOR
                                if (!stopLinkIDMapping.ContainsKey(signalLight.hintStopline))
                                {
                                    Debug.Log("Selected the hint stopline that is not in the mapping");
                                    UnityEditor.Selection.activeGameObject = signalLight.hintStopline.gameObject;
                                    return false;
                                }
#endif
                                LinkID = PickAimingLinkID(signalLight.transform, stopLinkIDMapping[signalLight.hintStopline]);

#if UNITY_EDITOR
                                if (LinkID < 0)
                                {
                                    Debug.Log("Selected the hint stopline that is related to the missing LinkID");
                                    UnityEditor.Selection.activeGameObject = signalLight.hintStopline.gameObject;
                                    return false;
                                }
#endif
                            }
                            else
                            {
                                LinkID = FindProperStoplineLinkID(trafficLightPos, trafficLightAim);
                            }

                            var vmSignalData = SignalData.MakeSignalData(ID, VID, PLID, Type, LinkID);
                            signalDatas.Add(vmSignalData);
                        }
                    }
                }

                return true;
            }

            void Export()
            {
                var sb1 = new StringBuilder();
                var sb2 = new StringBuilder();

                foreach (var list in exportLists)
                {
                    var csvFilename = list.FileName;
                    var csvHeader = VectorMapUtility.GetCSVHeader(list.Type);

                    sb1.Clear();
                    sb1.Append(foldername);
                    sb1.Append(Path.DirectorySeparatorChar);
                    sb1.Append(csvFilename);

                    var filepath = sb1.ToString();

                    if (!System.IO.Directory.Exists(foldername))
                    {
                        System.IO.Directory.CreateDirectory(foldername);
                    }

                    if (System.IO.File.Exists(filepath))
                    {
                        System.IO.File.Delete(filepath);
                    }

                    sb2.Clear();
                    using (StreamWriter sw = File.CreateText(filepath))
                    {
                        sb2.Append(csvHeader);
                        sb2.Append("\n");

                        foreach (var e in list.List)
                        {
                            foreach (var field in list.Type.GetFields(BindingFlags.Instance | BindingFlags.Public))
                            {
                                sb2.Append(field.GetValue(e).ToString());
                                sb2.Append(",");
                            }
                            sb2.Remove(sb2.Length - 1, 1);
                            sb2.Append("\n");
                        }
                        sw.Write(sb2);
                    }
                }
            }


            //joint and convert a set of singlely-connected segments and also setup world positions for all segments
            public static void ConvertAndJointSingleConnectedSegments(MapSegment curSeg, HashSet<MapSegment> allSegs, ref HashSet<MapSegment> newAllSegs, HashSet<MapSegment> visitedSegs)
            {
                var combSegs = new List<MapSegment>();
                visitedSegs.Add(curSeg);
                combSegs.Add(curSeg);

                //determine special type
                bool isLane = false;
                if ((curSeg.builder as MapLaneSegmentBuilder) != null)
                {
                    isLane = true;
                }

                while (curSeg.afters.Count == 1 && curSeg.afters[0].befores.Count == 1 && curSeg.afters[0].befores[0] == curSeg && !visitedSegs.Contains((MapSegment)(curSeg.afters[0])))
                {
                    visitedSegs.Add(curSeg.afters[0]);
                    combSegs.Add(curSeg.afters[0]);
                    curSeg = curSeg.afters[0];
                }

                //construct new lane segments in with its wappoints in world positions and update related dependencies to relate to new segments
                MapSegment combinedSeg = new MapSegment();

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

                if (isLane)
                {
                    foreach (var seg in combSegs)
                    {
                        var lnSegBldr = seg.builder as MapLaneSegmentBuilder;

                        int laneCount = lnSegBldr.laneInfo.laneCount;
                        int laneNumber = lnSegBldr.laneInfo.laneNumber;
                        foreach (var localPos in seg.targetLocalPositions)
                        {
                            combinedSeg.vectormapInfo.laneInfos.Add(new Autoware.LaneInfo() { laneCount = laneCount, laneNumber = laneNumber });
                        }
                    }                    
                }

                foreach (var seg in combSegs)
                {
                    allSegs.Remove(seg);
                }

                newAllSegs.Add(combinedSeg);
            }

            int PickAimingLinkID(Transform t, List<int> LinkIDs)
            {
                var srcDir = t.forward;
                var srcPos = t.position;
                float dotMax = 0;
                int theIdx = -1;
                for (int i = 0; i < LinkIDs.Count; i++)
                {
                    var lane = Lanes[LinkIDs[i] - 1];
                    var dtLane = DtLanes[lane.DID - 1];
                    var point = points[dtLane.PID - 1];
                    var linkIDPos = VectorMapUtility.GetUnityPosition(new VectorMapPosition() { Bx = point.Bx, Ly = point.Ly, H = point.H }, exportScaleFactor);
                    float dot = Vector3.Dot(srcDir, (linkIDPos - srcPos).normalized);
                    if (dot > dotMax)
                    {
                        theIdx = i;
                        dotMax = dot;
                    }
                }

                if (theIdx < 0)
                {
                    return -1;
                }

                return LinkIDs[theIdx];
            }

            int FindProperStoplineLinkID(Vector3 trafficLightPos, Vector3 trafficLightAim, float radius = 60f)
            {
                throw new System.Exception("do not this function, it needs to be modified to adapt to segmented stoplines");
#if THIS_IS_NOT_USED
        var stoplineCandids = new Dictionary<int, KeyValuePair<Vector3, Vector3>>();
        trafficLightPos.Set(trafficLightPos.x, 0, trafficLightPos.z);
        trafficLightAim.Set(trafficLightAim.x, 0, trafficLightAim.z);
        for (int i = 0; i < stoplines.Count; i++)
        {
            var line = lines[stoplines[i].LID - 1];
            var startPos = VectorMapUtility.GetUnityPosition(points[line.BPID - 1]);
            var endPos = VectorMapUtility.GetUnityPosition(points[line.FPID - 1]);
            startPos.Set(startPos.x, 0, startPos.z);
            endPos.Set(endPos.x, 0, endPos.z);
            var pos = (startPos + endPos) * 0.5f;
            if ((pos - trafficLightPos).magnitude < radius)
            {
                if (Vector3.Cross(trafficLightAim, startPos - trafficLightPos).y * Vector3.Cross(trafficLightAim, endPos - trafficLightPos).y < 0) // traffic light aims inbetween stopline's two ends
                {
                    stoplineCandids.Add(stoplines[i].ID, new KeyValuePair<Vector3, Vector3>(startPos, endPos));
                }
            }
        }

        float forbiddenThreshold = 4.5f;
        float minValidDist = 10000f;
        int nearestStopID = -1;
        foreach (var stopId in stoplineCandids)
        {
            var start = stopId.Value.Key;
            var end = stopId.Value.Value;
            var distToLine = (trafficLightPos - NearestPointOnFiniteLine(start, end, trafficLightPos)).magnitude;
            if (distToLine > forbiddenThreshold)
            {
                if (distToLine < minValidDist)
                {
                    nearestStopID = stopId.Key;
                    minValidDist = distToLine;
                }
            }
        }

        return stoplines[nearestStopID - 1].LinkID;
#endif
            }

            public static Vector3 NearestPointOnFiniteLine(Vector3 start, Vector3 end, Vector3 pnt)
            {
                var line = (end - start);
                var len = line.magnitude;
                line.Normalize();

                var v = pnt - start;
                var d = Vector3.Dot(v, line);
                d = Mathf.Clamp(d, 0f, len);
                return start + line * d;
            }

            public static void Interpolate(List<Vector3> waypoints, List<LaneInfo> laneInfos, out List<Vector3> interpolatedWaypoints, out List<LaneInfo> interpolatedLaneInfos, float interpoInterval = 1.0f, bool addLastPoint = true)
            {
                interpolatedWaypoints = new List<Vector3>();
                interpolatedLaneInfos = new List<LaneInfo>();

                interpolatedWaypoints.Add(waypoints[0]); //add the first point
                interpolatedLaneInfos.Add(laneInfos[0]); //add the first point

                Vector3 startPoint = waypoints[0];
                int curIndex = 0;
                var newPoint = waypoints[1];
                float accumulatedDist = 0;
                bool finish = false;

                while (true)
                {
                    while (true)
                    {
                        if (curIndex >= waypoints.Count - 1)
                        {
                            if (accumulatedDist > 0)
                            {
                                if (addLastPoint)
                                {
                                    interpolatedWaypoints.Add(waypoints[waypoints.Count - 1]);
                                    interpolatedLaneInfos.Add(laneInfos[laneInfos.Count - 1]);
                                }
                            }
                            finish = true;
                            break;
                        }

                        Vector3 forwardVec = waypoints[curIndex + 1] - startPoint;

                        if (accumulatedDist + forwardVec.magnitude < interpoInterval)
                        {
                            accumulatedDist += forwardVec.magnitude;
                            startPoint += forwardVec;
                            ++curIndex; //Still accumulating so keep looping
                        }
                        else
                        {
                            newPoint = startPoint + forwardVec.normalized * (interpoInterval - accumulatedDist);
                            interpolatedWaypoints.Add(newPoint);
                            interpolatedLaneInfos.Add(laneInfos[curIndex]);
                            startPoint = newPoint;
                            accumulatedDist = 0;
                            break; //break here after find a new point
                        }
                    }
                    if (finish) //reached the end of the original point list
                    {
                        break;
                    }
                }
            }

            void MakeVisualLine(Line line, LineType type)
            {
                int dummyInt;
                MakeVisualLine(line, type, out dummyInt);
            }

            void MakeVisualLine(Line line, LineType type, out int retLinkID)
            {
                retLinkID = -1;

                var startPos = VectorMapUtility.GetUnityPosition(new VectorMapPosition() { Bx = points[line.BPID - 1].Bx, Ly = points[line.BPID - 1].Ly, H = points[line.BPID - 1].H }, exportScaleFactor);
                var endPos = VectorMapUtility.GetUnityPosition(new VectorMapPosition() { Bx = points[line.FPID - 1].Bx, Ly = points[line.FPID - 1].Ly, H = points[line.FPID - 1].H }, exportScaleFactor);
                if (type == LineType.STOP) //If it is stopline
                {
                    int LinkID = FindNearestLaneId(VectorMapUtility.GetVectorMapPosition((startPos + endPos) / 2, exportScaleFactor), true);
                    retLinkID = LinkID;
                    var vmStopline = StopLine.MakeStopLine(Stoplines.Count + 1, line.LID, 0, 0, LinkID);
                    Stoplines.Add(vmStopline);
                }
                else if (type == LineType.WHITE || type == LineType.YELLOW) //if it is whiteline
                {
                    int LinkID = FindNearestLaneId(VectorMapUtility.GetVectorMapPosition((startPos + endPos) / 2, exportScaleFactor));
                    retLinkID = LinkID;
                    string color = "W";
                    switch (type)
                    {
                        case LineType.WHITE:
                            color = "W";
                            break;
                        case LineType.YELLOW:
                            color = "Y";
                            break;
                    }
                    var vmWhiteline = WhiteLine.MakeWhiteLine(WhiteLines.Count + 1, line.LID, .15, color, 0, LinkID);
                    WhiteLines.Add(vmWhiteline);
                }
            }

            //Needs optimization
            public int FindNearestLaneId(VectorMapPosition refPos, bool preferJunction = false)
            {
                //find nearest lane set first
                float radius = 1.5f / exportScaleFactor;
                var lnIDs = new List<int>();
                for (int i = 0; i < Lanes.Count; i++)
                {
                    var lane = Lanes[i];
                    var p = points[DtLanes[lane.DID - 1].PID - 1];
                    var dist = Mathf.Sqrt(Mathf.Pow((float)(refPos.Bx - p.Bx), 2.0f) + Mathf.Pow((float)(refPos.Ly - p.Ly), 2.0f) + Mathf.Pow((float)(refPos.H - p.H), 2.0f));
                    if (dist < radius)
                    {
                        lnIDs.Add(lane.LnID);
                    }
                }

                float min = float.MaxValue;
                float jctMin = float.MaxValue;
                int retLnIdx = 0;
                int nearestJctLnIdx = 0;
                for (int i = 0; i < lnIDs.Count; i++)
                {
                    bool jct = false;
                    var lane = Lanes[lnIDs[i] - 1];

                    if (preferJunction && lane.FLID2 > 0)
                    {
                        jct = true;
                    }

                    var p = points[DtLanes[lane.DID - 1].PID - 1];
                    var dist = Mathf.Sqrt(Mathf.Pow((float)(refPos.Bx - p.Bx), 2.0f) + Mathf.Pow((float)(refPos.Ly - p.Ly), 2.0f) + Mathf.Pow((float)(refPos.H - p.H), 2.0f));
                    if (dist < min)
                    {
                        min = dist;
                        retLnIdx = lnIDs[i];
                    }
                    if (jct && dist < jctMin)
                    {
                        jctMin = dist;
                        nearestJctLnIdx = lnIDs[i];
                    }
                }

                if (nearestJctLnIdx != 0)
                {
                    retLnIdx = nearestJctLnIdx;
                }

                return retLnIdx;
            }
        }
    }
}
