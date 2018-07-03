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
using VectorMap;

[System.AttributeUsage(System.AttributeTargets.Field)]
public class VectorMapCategoryAttribute : System.Attribute
{
    public string FileName { get; private set; }

    public VectorMapCategoryAttribute(string filename)
    {
        FileName = filename;
    }
}

public class VectorMapTool : MonoBehaviour
{
    public Transform target;

    public static float PROXIMITY = 0.02f;
    public static float ARROWSIZE = 1.0f;
    public float proximity = PROXIMITY;
    public float arrowSize = ARROWSIZE;

    [VectorMapCategory("point.csv")]
    private List<Point> points = new List<Point>();
    [VectorMapCategory("line.csv")]
    private List<Line> lines = new List<Line>();
    [VectorMapCategory("lane.csv")]
    private List<Lane> lanes = new List<Lane>();
    [VectorMapCategory("dtlane.csv")]
    private List<DtLane> dtLanes = new List<DtLane>();
    [VectorMapCategory("stopline.csv")]
    private List<StopLine> stoplines = new List<StopLine>();
    [VectorMapCategory("whiteline.csv")]
    private List<WhiteLine> whiteLines = new List<WhiteLine>();

    struct ExportData
    {
        public IList List;
        public System.Type Type;
        public string FileName;
    }

    List<ExportData> exportLists;

    public string foldername = "vector_map";
    public float exportScaleFactor = 1.0f;

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
        var categoryList = GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(e => System.Attribute.IsDefined(e, typeof(VectorMapCategoryAttribute)));
        foreach (var c in categoryList)
        {
            exportLists.Add(new ExportData()
            {
                List = (IList)c.GetValue(this),
                Type = c.FieldType.GetGenericArguments()[0],
                FileName = c.GetCustomAttribute<VectorMapCategoryAttribute>().FileName,
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
        Calculate();
        Export();
    }

    void Calculate()
    {
        if (target == null)
        {
            target = transform;
        }

        exportLists.ForEach(e => e.List.Clear()); //clear all vector map data before calculate

        var segBldrs = target.GetComponentsInChildren<VectorMapSegmentBuilder>();

        bool missingPoints = false;

        var allSegs = new HashSet<VectorMapSegment>(); //All segments regardless of segment actual type

        //connect builder reference for each segment
        foreach (var segBldr in segBldrs)
        {
            segBldr.segment.builder = segBldr;
            allSegs.Add(segBldr.segment);
        }

        //Link before and after segment for each segment
        foreach (var segment in allSegs)
        {
            //Make sure clear everything that might have data left over by previous generation
            segment.befores.Clear();
            segment.afters.Clear();
            segment.targetWorldPositions.Clear();

            //this is to avoid accidentally connect two nearby stoplines
            if ((segment.builder as VectorMapStopLineSegmentBuilder) != null)
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
                if (segment_cmp.builder.GetType() != segment.builder.GetType())
                {
                    continue;
                }

                var firstPt_cmp = segment_cmp.builder.transform.TransformPoint(segment_cmp.targetLocalPositions[0]);
                var lastPt_cmp = segment_cmp.builder.transform.TransformPoint(segment_cmp.targetLocalPositions[segment_cmp.targetLocalPositions.Count - 1]);

                if ((firstPt - lastPt_cmp).magnitude < PROXIMITY)
                {
                    segment.befores.Add(segment_cmp);
                }

                if ((lastPt - firstPt_cmp).magnitude < PROXIMITY)
                {
                    segment.afters.Add(segment_cmp);
                }
            }
        }

        if (missingPoints)
        {
            Debug.Log("Some segment has less than 2 waypoints, complement it to 2");
        }

        var allLnSegs = new HashSet<VectorMapSegment>();
        var allLinSegs = new HashSet<VectorMapSegment>();

        foreach (var segment in allSegs)
        {
            if (segment.builder.GetType() == typeof(VectorMapLaneSegmentBuilder))
            {
                allLnSegs.Add(segment);
            }
            if (segment.builder.GetType() == typeof(VectorMapStopLineSegmentBuilder))
            {
                allLinSegs.Add(segment);
            }
            if (segment.builder.GetType() == typeof(VectorMapWhiteLineSegmentBuilder))
            {
                allLinSegs.Add(segment);
            }
        }

        //New sets for newly converted(to world space) segments
        var allConvertedLnSeg = new HashSet<VectorMapSegment>(); //for lane segments
        var allConvertedLinSeg = new HashSet<VectorMapSegment>(); //for line segments

        //Filter and convert all lane segments
        if (allLnSegs.Count > 0)
        {
            var startLnSegs = new HashSet<VectorMapSegment>(); //The lane segments that are at merging or forking or starting position
            var visitedLnSegs = new HashSet<VectorMapSegment>(); //tracking for record

            foreach (var lnSeg in allLnSegs)
            {
                if (lnSeg.befores.Count != 1 || (lnSeg.befores.Count == 1 && lnSeg.befores[0].afters.Count > 1)) //no any before segments
                {
                    startLnSegs.Add(lnSeg);
                }
            }

            foreach (var startLnSeg in startLnSegs)
            {
                ConvertAndJointSegmentSet(startLnSeg, allLnSegs, allConvertedLnSeg, visitedLnSegs);
            }

            while (allLnSegs.Count > 0)//Remaining should be isolated loops
            {
                VectorMapSegment pickedSeg = null;
                foreach (var lnSeg in allLnSegs)
                {
                    pickedSeg = lnSeg;
                    break;
                }
                if (pickedSeg != null)
                {
                    ConvertAndJointSegmentSet(pickedSeg, allLnSegs, allConvertedLnSeg, visitedLnSegs);
                }
            }
        }

        //convert all line segments
        if (allLinSegs.Count > 0)
        {
            //setup world positions and their proper befores and afters for all line segments
            foreach (var linSeg in allLinSegs)
            {
                var convertedSeg = new VectorMapSegment();
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
        var lnSegTerminalIDsMapping = new Dictionary<VectorMapSegment, int[]>(); //tracking for record
        if (allConvertedLnSeg.Count > 0)
        {
            foreach (var lnSeg in allConvertedLnSeg)
            {
                List<Vector3> positions;
                List<LaneInfo> laneInfos;
                var cast = (lnSeg as VectorMapLaneSegment);
                List<LaneInfo> waypointLaneInfos = (cast == null ? new List<LaneInfo>(new LaneInfo[lnSeg.targetWorldPositions.Count]) : cast.laneInfos);

                //Interpolate based on waypoint world positions
                VectorMapUtility.Interpolate(lnSeg.targetWorldPositions, waypointLaneInfos, out positions, out laneInfos, 1.0f / exportScaleFactor, true); //interpolate and divide to ensure 1 meter apart

                lnSegTerminalIDsMapping.Add(lnSeg, new int[2]);
                for (int i = 0; i < positions.Count; i++)
                {
                    //Point
                    var pos = positions[i];
                    var vectorMapPos = GetVectorMapPosition(pos);
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
                    var vmDTLane = DtLane.MakeDtLane(dtLanes.Count + 1, i, vmPoint.PID, -convertedEulerAngles.z * Mathf.Deg2Rad, .0, .1, .1);
                    dtLanes.Add(vmDTLane);

                    //Lane
                    int beforeLaneID = 0;
                    int laneId = lanes.Count + 1;
                    if (i > 0)
                    {
                        beforeLaneID = lanes.Count;
                        var beforeLane = lanes[beforeLaneID - 1];
                        beforeLane.FLID = laneId;
                        lanes[beforeLaneID - 1] = beforeLane; //This is needed for struct copy to be applied back
                    }
                    var vmLane = Lane.MakeLane(laneId, vmDTLane.DID, beforeLaneID, 0, laneInfos[i].laneCount, laneInfos[i].laneNumber);
                    lanes.Add(vmLane);

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
                    var ln = lanes[segStartId - 1];

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

                    lanes[segStartId - 1] = ln;
                }

                if (lnSeg.afters.Count > 0)
                {
                    var segEndId = terminalIdPair[(int)TerminalType.END];
                    var ln = lanes[segEndId - 1];

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

                    lanes[segEndId - 1] = ln;

                    //Adjust last dtlane of each lane segment
                    var endDtLn = dtLanes[lanes[segEndId - 1].DID - 1];
                    var DtLnAfter = dtLanes[lanes[afterSegStartLn - 1].DID - 1];

                    var pointPos = GetUnityPosition(new VectorMapPosition() { Bx = points[endDtLn.PID - 1].Bx, Ly = points[endDtLn.PID - 1].Ly, H = points[endDtLn.PID - 1].H });
                    var pointAfterPos = GetUnityPosition(new VectorMapPosition() { Bx = points[DtLnAfter.PID - 1].Bx, Ly = points[DtLnAfter.PID - 1].Ly, H = points[DtLnAfter.PID - 1].H });
                    var deltaDir = pointAfterPos - pointPos;
                    Vector3 eulerAngles = Quaternion.FromToRotation(Vector3.right, deltaDir).eulerAngles;
                    var convertedEulerAngles = VectorMapUtility.GetRvizCoordinates(eulerAngles);
                    endDtLn.Dir = -convertedEulerAngles.z * Mathf.Deg2Rad;
                    dtLanes[endDtLn.DID - 1] = endDtLn;
                }
            }
        }

        //set up all lines vector map data
        var linSegTerminalIDsMapping = new Dictionary<VectorMapSegment, KeyValuePair<int, LineType>[]>(); //tracking for record
        if (allConvertedLinSeg.Count > 0)
        {
            foreach (var linSeg in allConvertedLinSeg)
            {
                var linType = LineType.NONE;
                if (linSeg.builder.GetType() == typeof(VectorMapStopLineSegmentBuilder)) //If it is stopline
                {
                    linType = LineType.STOP;
                }
                else if (linSeg.builder.GetType() == typeof(VectorMapWhiteLineSegmentBuilder)) //if it is whiteline
                {
                    var whiteLineBuilder = linSeg.builder as VectorMapWhiteLineSegmentBuilder;
                    if (whiteLineBuilder.lineColor == LineColor.WHITE)
                    {
                        linType = LineType.WHITE;
                    }
                    else if (whiteLineBuilder.lineColor == LineColor.YELLOW)
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

                    var vectorMapPos = GetVectorMapPosition(startPos);
                    var vmStartPoint = Point.MakePoint(points.Count + 1, vectorMapPos.Bx, vectorMapPos.Ly, vectorMapPos.H);
                    points.Add(vmStartPoint);

                    vectorMapPos = GetVectorMapPosition(endPos);
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

                    MakeVisualLine(vmLine, linType);
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
    }

    void MakeVisualLine(Line line, LineType type)
    {
        var startPos = GetUnityPosition(new VectorMapPosition() { Bx = points[line.BPID - 1].Bx, Ly = points[line.BPID - 1].Ly, H = points[line.BPID - 1].H });
        var endPos = GetUnityPosition(new VectorMapPosition() { Bx = points[line.FPID - 1].Bx, Ly = points[line.FPID - 1].Ly, H = points[line.FPID - 1].H });
        if (type == LineType.STOP) //If it is stopline
        {
            int LinkID = FindNearestLaneId(GetVectorMapPosition((startPos + endPos) / 2));
            var vmStopline = StopLine.MakeStopLine(stoplines.Count + 1, line.LID, 0, 0, LinkID);
            stoplines.Add(vmStopline);
        }
        else if (type == LineType.WHITE || type == LineType.YELLOW) //if it is whiteline
        {
            int LinkID = FindNearestLaneId(GetVectorMapPosition((startPos + endPos) / 2));
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
            var vmWhiteline = WhiteLine.MakeWhiteLine(whiteLines.Count + 1, line.LID, .15, color, 0, LinkID);
            whiteLines.Add(vmWhiteline);
        }
    }

    //Needs optimization
    public int FindNearestLaneId(VectorMapPosition refPos)
    {
        float min = float.MaxValue;
        int retLnIdx = 0;
        for (int i = 0; i < lanes.Count; i++)
        {
            var p = points[lanes[i].DID];
            var dist = Mathf.Sqrt(Mathf.Pow((float)(refPos.Bx - p.Bx), 2.0f) + Mathf.Pow((float)(refPos.Ly - p.Ly), 2.0f) + Mathf.Pow((float)(refPos.H - p.H), 2.0f));
            if (dist < min)
            {
                min = dist;
                retLnIdx = i + 1;
            }
        }
        return retLnIdx;
    }

    public VectorMapPosition GetVectorMapPosition(Vector3 unityPos)
    {
        var convertedPos = VectorMapUtility.GetRvizCoordinates(unityPos);
        convertedPos *= exportScaleFactor;
        return new VectorMapPosition() { Bx = convertedPos.y, Ly = convertedPos.x, H = convertedPos.z };
    }

    public Vector3 GetUnityPosition(VectorMapPosition vmPos)
    {
        var inverseConvertedPos = new Vector3((float)vmPos.Ly, (float)vmPos.Bx, (float)vmPos.H);
        inverseConvertedPos /= exportScaleFactor;
        return VectorMapUtility.GetUnityCoordinate(inverseConvertedPos);
    }

    //joint and convert a set of singlely-connected segments and also setup world positions for all segments
    private static void ConvertAndJointSegmentSet(VectorMapSegment curLnSeg, HashSet<VectorMapSegment> allLnSegs, HashSet<VectorMapSegment> newAllLnSegs, HashSet<VectorMapSegment> visitedLnSegs)
    {
        var combLnSegs = new List<VectorMapSegment>();
        visitedLnSegs.Add(curLnSeg);
        combLnSegs.Add(curLnSeg);  
        
        while (curLnSeg.afters.Count == 1 && curLnSeg.afters[0].befores.Count == 1 && curLnSeg.afters[0].befores[0] == curLnSeg && !visitedLnSegs.Contains((VectorMapSegment)curLnSeg.afters[0]))
        {
            visitedLnSegs.Add(curLnSeg.afters[0]);
            combLnSegs.Add(curLnSeg.afters[0]);
            curLnSeg = curLnSeg.afters[0];
        }

        //construct new lane segments in with its wappoints in world positions and update related dependencies to relate to new segments
        var combinedLnSeg = new VectorMapLaneSegment();
        combinedLnSeg.befores = combLnSegs[0].befores;
        combinedLnSeg.afters = combLnSegs[combLnSegs.Count - 1].afters;

        foreach (var beforeSeg in combinedLnSeg.befores)
        {
            for (int i = 0; i < beforeSeg.afters.Count; i++)
            {
                if (beforeSeg.afters[i] == combLnSegs[0])
                {
                    beforeSeg.afters[i] = combinedLnSeg;
                }
            }
        }
        foreach (var afterSeg in combinedLnSeg.afters)
        {
            for (int i = 0; i < afterSeg.befores.Count; i++)
            {
                if (afterSeg.befores[i] == combLnSegs[combLnSegs.Count - 1])
                {
                    afterSeg.befores[i] = combinedLnSeg;
                }
            }
        }

        foreach (var lnSeg in combLnSegs)
        {
            int laneCount = 0;
            int laneNumber = 0;
            var segBlder = lnSeg.builder as VectorMapLaneSegmentBuilder;
            if (segBlder != null)
            {
                laneCount = segBlder.laneInfo.laneCount;
                laneNumber = segBlder.laneInfo.laneNumber;
            }

            foreach (var localPos in lnSeg.targetLocalPositions)
            {
                combinedLnSeg.targetWorldPositions.Add(lnSeg.builder.transform.TransformPoint(localPos)); //Convert to world position
                combinedLnSeg.laneInfos.Add(new LaneInfo() { laneCount = laneCount, laneNumber = laneNumber });
            }
        }

        foreach (var lnSeg in combLnSegs)
        {
            allLnSegs.Remove(lnSeg);
        }

        newAllLnSegs.Add(combinedLnSeg);
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
}
