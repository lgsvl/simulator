/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using Simulator.Editor.Autoware;
using Simulator.Map;

namespace Simulator.Editor
{
    using Vector = Simulator.Editor.Autoware.Vector;
    public class AutowareMapTool
    {
        class MapVectorLane
        {
            public List<Vector3> MapWorldPositions = new List<Vector3>();
            public List<MapVectorLane> Befores = new List<MapVectorLane>();
            public List<MapVectorLane> Afters = new List<MapVectorLane>();
            public int LaneCount;
            public int LaneNumber;
            public float SpeedLimit;


            public void copyFromMapLane(MapLane lane)
            {
                this.MapWorldPositions.Clear();
                this.MapWorldPositions.AddRange(lane.mapWorldPositions);
                this.LaneCount = lane.laneCount;
                this.LaneNumber = lane.laneNumber;
                this.SpeedLimit = lane.speedLimit;
            }
        }


        [VectorMapCSV("point.csv")]
        private List<Point> Points = new List<Point>();
        [VectorMapCSV("line.csv")]
        private List<Line> Lines = new List<Line>();
        [VectorMapCSV("lane.csv")]
        private List<Lane> Lanes = new List<Lane>();
        [VectorMapCSV("dtlane.csv")]
        private List<DtLane> DtLanes = new List<DtLane>();
        [VectorMapCSV("stopline.csv")]
        private List<StopLine> StopLines = new List<StopLine>();
        [VectorMapCSV("whiteline.csv")]
        private List<WhiteLine> WhiteLines = new List<WhiteLine>();
        [VectorMapCSV("vector.csv")]
        private List<Vector> Vectors = new List<Vector>();
        [VectorMapCSV("pole.csv")]
        private List<Pole> Poles = new List<Pole>();
        [VectorMapCSV("signaldata.csv")]
        private List<SignalData> SignalDataList = new List<SignalData>();
        [VectorMapCSV("node.csv")]
        private List<Node> Nodes = new List<Node>();
        private MapManagerData MapAnnotationData;

        struct ExportData
        {
            public IList List;
            public System.Type Type;
            public string FileName;
        }

        List<ExportData> ExportLists;

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
            YELLOW
        }

        public AutowareMapTool()
        {
            ExportLists = new List<ExportData>();
            var categoryList = GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(e => System.Attribute.IsDefined(e, typeof(VectorMapCSVAttribute)));
            foreach (var c in categoryList)
            {
                ExportLists.Add(new ExportData()
                {
                    List = (IList)c.GetValue(this),
                    Type = c.FieldType.GetGenericArguments()[0],
                    FileName = c.GetCustomAttribute<VectorMapCSVAttribute>().FileName,
                });
            }
        }

        bool Calculate()
        {
            // Initial collection
            var laneSegments = new List<MapLane>();
            var lineSegments = new List<MapLine>();
            var signalLightPoles = new List<MapPole>();

            laneSegments.AddRange(MapAnnotationData.GetData<MapLane>());
            lineSegments.AddRange(MapAnnotationData.GetData<MapLine>());
            signalLightPoles.AddRange(MapAnnotationData.GetData<MapPole>());

            var allVectorLaneSegments = new HashSet<MapVectorLane>();
            var allLineSegments = new HashSet<MapLine>();

            foreach (var laneSegment in laneSegments)
            {
                var vectorLaneSegment = new MapVectorLane();
                vectorLaneSegment.copyFromMapLane(laneSegment);
                allVectorLaneSegments.Add(vectorLaneSegment);
            }

            // Link before and after segment for each lane segment
            foreach (var laneSegment in allVectorLaneSegments)
            {
                // Each segment must have at least 2 waypoints for calculation, otherwise exit
                while (laneSegment.MapWorldPositions.Count < 2)
                {
                    Debug.LogError("Some segment has less than 2 waypoints. Cancelling map generation.");
                    return false;
                }

                // Link lanes
                var firstPt = laneSegment.MapWorldPositions[0];
                var lastPt = laneSegment.MapWorldPositions[laneSegment.MapWorldPositions.Count - 1];

                foreach (var laneSegmentCmp in allVectorLaneSegments)
                {
                    if (laneSegment == laneSegmentCmp)
                    {
                        continue;
                    }

                    var firstPt_cmp = laneSegmentCmp.MapWorldPositions[0];
                    var lastPt_cmp = laneSegmentCmp.MapWorldPositions[laneSegmentCmp.MapWorldPositions.Count - 1];

                    if ((firstPt - lastPt_cmp).magnitude < MapAnnotationTool.PROXIMITY / MapAnnotationTool.EXPORT_SCALE_FACTOR)
                    {
                        laneSegment.Befores.Add(laneSegmentCmp);
                    }

                    if ((lastPt - firstPt_cmp).magnitude < MapAnnotationTool.PROXIMITY / MapAnnotationTool.EXPORT_SCALE_FACTOR)
                    {
                        laneSegment.Afters.Add(laneSegmentCmp);
                    }
                }
            }

            foreach (var lineSegment in lineSegments)
            {
                if (lineSegment.lineType != MapData.LineType.VIRTUAL)
                {
                    allLineSegments.Add(lineSegment);
                }
            }
            // Link before and after segment for each line segment
            foreach (var lineSegment in allLineSegments)
            {
                if (lineSegment.lineType != MapData.LineType.STOP) continue; // Skip stop lines

                // Each segment must have at least 2 waypoints for calculation, otherwise exit
                while (lineSegment.mapLocalPositions.Count < 2)
                {
                    Debug.LogError("Some segment has less than 2 waypoints. Cancelling map generation.");
                    return false;
                }

                // Link lanes
                var firstPt = lineSegment.transform.TransformPoint(lineSegment.mapLocalPositions[0]);
                var lastPt = lineSegment.transform.TransformPoint(lineSegment.mapLocalPositions[lineSegment.mapLocalPositions.Count - 1]);

                foreach (var lineSegmentCmp in allLineSegments)
                {
                    if (lineSegment == lineSegmentCmp)
                    {
                        continue;
                    }
                    if (lineSegmentCmp.lineType != MapData.LineType.STOP) continue; // Skip stop lines

                    var firstPt_cmp = lineSegmentCmp.transform.TransformPoint(lineSegmentCmp.mapLocalPositions[0]);
                    var lastPt_cmp = lineSegmentCmp.transform.TransformPoint(lineSegmentCmp.mapLocalPositions[lineSegmentCmp.mapLocalPositions.Count - 1]);

                    if ((firstPt - lastPt_cmp).magnitude < MapAnnotationTool.PROXIMITY / MapAnnotationTool.EXPORT_SCALE_FACTOR)
                    {
                        lineSegmentCmp.mapLocalPositions[lineSegmentCmp.mapLocalPositions.Count - 1] = lineSegmentCmp.transform.InverseTransformPoint(firstPt);
                        lineSegmentCmp.mapWorldPositions[lineSegmentCmp.mapWorldPositions.Count - 1] = firstPt;
                    }

                    if ((lastPt - firstPt_cmp).magnitude < MapAnnotationTool.PROXIMITY / MapAnnotationTool.EXPORT_SCALE_FACTOR)
                    {
                        lineSegmentCmp.mapLocalPositions[0] = lineSegmentCmp.transform.InverseTransformPoint(lastPt);
                        lineSegmentCmp.mapWorldPositions[0] = lastPt;
                    }
                }
            }

            // New sets for newly converted(to world space) segments
            var allConvertedLaneSegments = new HashSet<MapVectorLane>(); //for lane segments

            // Filter and convert all lane segments
            if (allVectorLaneSegments.Count > 0)
            {
                var startLaneSegs = new HashSet<MapVectorLane>(); // The lane segments that are at merging or forking or starting position
                var visitedLaneSegs = new HashSet<MapVectorLane>(); // tracking for record

                foreach (var laneSegment in allVectorLaneSegments)
                {
                    if (laneSegment.Befores.Count != 1 || (laneSegment.Befores.Count == 1 && laneSegment.Befores[0].Afters.Count > 1)) //no any before segments
                    {
                        startLaneSegs.Add(laneSegment);
                    }
                }

                foreach (var startLaneSeg in startLaneSegs)
                {
                    ConvertAndJointSingleConnectedSegments(startLaneSeg, allVectorLaneSegments, ref allConvertedLaneSegments, visitedLaneSegs);
                }

                while (allVectorLaneSegments.Count > 0) // Remaining should be isolated loops
                {
                    MapVectorLane pickedLaneSeg = null;
                    foreach (var laneSegment in allVectorLaneSegments)
                    {
                        pickedLaneSeg = laneSegment;
                        break;
                    }
                    if (pickedLaneSeg != null)
                    {
                        ConvertAndJointSingleConnectedSegments(pickedLaneSeg, allVectorLaneSegments, ref allConvertedLaneSegments, visitedLaneSegs);
                    }
                }
            }

            // Set up all lanes vector map data
            var laneSegTerminalIDsMapping = new Dictionary<MapVectorLane, int[]>(); //tracking for record
            if (allConvertedLaneSegments.Count > 0)
            {
                foreach (var laneSegment in allConvertedLaneSegments)
                {
                    List<Vector3> positions;
                    var laneCount = laneSegment.LaneCount;
                    var laneNumber = laneSegment.LaneNumber;

                    //Interpolate based on waypoint world positions
                    Interpolate(laneSegment.MapWorldPositions, out positions, 1.0f / MapAnnotationTool.EXPORT_SCALE_FACTOR, true); //interpolate and divide to ensure 1 meter apart

                    laneSegTerminalIDsMapping.Add(laneSegment, new int[2]);
                    for (int i = 0; i < positions.Count; i++)
                    {
                        //Point
                        var pos = positions[i];
                        var vectorMapPos = VectorMapUtility.GetVectorMapPosition(pos, MapAnnotationTool.EXPORT_SCALE_FACTOR);
                        var vmPoint = Point.MakePoint(Points.Count + 1, vectorMapPos.Bx, vectorMapPos.Ly, vectorMapPos.H);
                        Points.Add(vmPoint);

                        //Node
                        var vmNode = Node.MakeNode(Nodes.Count, Points.Count + 1);
                        Nodes.Add(vmNode);

                        //DtLane
                        var deltaDir = Vector3.zero;
                        if (i < positions.Count - 1)
                        {
                            deltaDir = positions[i + 1] - pos;
                        }
                        else
                        {
                            if (laneSegment.Afters.Count > 0)
                            {
                                deltaDir = laneSegment.Afters[0].MapWorldPositions[0] - pos;
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
                            beforeLaneID = Lanes.Count; // beforeLaneID won't be 0 since we add one vmLane to Lanes when i == 0
                            var beforeLane = Lanes[beforeLaneID - 1];
                            beforeLane.FLID = laneId;
                            Lanes[beforeLaneID - 1] = beforeLane; //This is needed for struct copy to be applied back
                        }
                        var speedLimit = (int)laneSegment.SpeedLimit;
                        var vmLane = Lane.MakeLane(laneId, vmDTLane.DID, beforeLaneID, 0, (Nodes.Count - 1), Nodes.Count, laneCount, laneNumber, speedLimit);
                        Lanes.Add(vmLane); // if positions.Count is n, then Lanes.Count is n-1.

                        if (i == 0)
                        {
                            laneSegTerminalIDsMapping[laneSegment][(int)TerminalType.START] = vmLane.LnID;
                        }
                        if (i == positions.Count - 1)
                        {
                            laneSegTerminalIDsMapping[laneSegment][(int)TerminalType.END] = vmLane.LnID;
                        }
                    }
                }

                //Assuming merging and diversing won't happen at the same spot
                //correcting start and end lanes's BLID and FLID to the lane segment's before and after lane IDs in other lane segments
                //each lane will have up to 4 before lane Ids and after lane Ids to set, later executed operation will be ignore if number exceeds 4
                //correcting DtLanes last lane's dir value
                foreach (var laneSegment in allConvertedLaneSegments)
                {
                    var terminalIdPair = laneSegTerminalIDsMapping[laneSegment];
                    if (laneSegment.Befores.Count > 0)
                    {
                        var segStartId = terminalIdPair[(int)TerminalType.START];
                        var ln = Lanes[segStartId - 1];

                        for (int i = 0; i < laneSegment.Befores.Count; i++)
                        {
                            int beforeSegAfterLn = laneSegTerminalIDsMapping[laneSegment.Befores[i]][(int)TerminalType.END];
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

                    if (laneSegment.Afters.Count > 0)
                    {
                        var segEndId = terminalIdPair[(int)TerminalType.END];
                        var ln = Lanes[segEndId - 1];

                        int afterSegStartLn = laneSegTerminalIDsMapping[laneSegment.Afters[0]][(int)TerminalType.START];
                        for (int i = 0; i < laneSegment.Afters.Count; i++)
                        {
                            afterSegStartLn = laneSegTerminalIDsMapping[laneSegment.Afters[i]][(int)TerminalType.START];
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

                        // Adjust last dtlane of each lane segment
                        var endDtLn = DtLanes[Lanes[segEndId - 1].DID - 1];
                        var DtLnAfter = DtLanes[Lanes[afterSegStartLn - 1].DID - 1];

                        var pointPos = VectorMapUtility.GetUnityPosition(new VectorMapPosition() { Bx = Points[endDtLn.PID - 1].Bx, Ly = Points[endDtLn.PID - 1].Ly, H = Points[endDtLn.PID - 1].H }, MapAnnotationTool.EXPORT_SCALE_FACTOR);
                        var pointAfterPos = VectorMapUtility.GetUnityPosition(new VectorMapPosition() { Bx = Points[DtLnAfter.PID - 1].Bx, Ly = Points[DtLnAfter.PID - 1].Ly, H = Points[DtLnAfter.PID - 1].H }, MapAnnotationTool.EXPORT_SCALE_FACTOR);
                        var deltaDir = pointAfterPos - pointPos;
                        Vector3 eulerAngles = Quaternion.FromToRotation(Vector3.right, deltaDir).eulerAngles;
                        var convertedEulerAngles = VectorMapUtility.GetRvizCoordinates(eulerAngles);
                        endDtLn.Dir = -convertedEulerAngles.z * Mathf.Deg2Rad;
                        DtLanes[endDtLn.DID - 1] = endDtLn;
                    }
                }
            }

            // Set up all lines vector map data
            var lineSegTerminalIDsMapping = new Dictionary<MapLine, KeyValuePair<int, LineType>[]>(); //tracking for record
            var stoplineLinkIDMapping = new Dictionary<MapLine, List<int>>();
            if (allLineSegments.Count > 0)
            {
                foreach (var lineSegment in allLineSegments)
                {
                    var lineType = LineType.NONE;
                    if (lineSegment.lineType == MapData.LineType.STOP) //If it is stopline
                    {
                        lineType = LineType.STOP;
                    }
                    else if (lineSegment.lineType == MapData.LineType.SOLID_WHITE || lineSegment.lineType == MapData.LineType.DOUBLE_WHITE || lineSegment.lineType == MapData.LineType.DOTTED_WHITE)
                    {
                        lineType = LineType.WHITE;
                    }
                    else if (lineSegment.lineType == MapData.LineType.SOLID_YELLOW || lineSegment.lineType == MapData.LineType.DOUBLE_YELLOW || lineSegment.lineType == MapData.LineType.DOTTED_YELLOW)
                    {
                        lineType = LineType.YELLOW;
                    }

                    var positions = lineSegment.mapWorldPositions;

                    lineSegTerminalIDsMapping.Add(lineSegment, new KeyValuePair<int, LineType>[2] { new KeyValuePair<int, LineType>(-1, LineType.NONE), new KeyValuePair<int, LineType>(-1, LineType.NONE) });
                    for (int i = 0; i < positions.Count - 1; i++)
                    {
                        //Point
                        var startPos = positions[i];
                        var endPos = positions[i + 1];

                        var vectorMapPos = VectorMapUtility.GetVectorMapPosition(startPos, MapAnnotationTool.EXPORT_SCALE_FACTOR);
                        var vmStartPoint = Point.MakePoint(Points.Count + 1, vectorMapPos.Bx, vectorMapPos.Ly, vectorMapPos.H);
                        Points.Add(vmStartPoint);

                        vectorMapPos = VectorMapUtility.GetVectorMapPosition(endPos, MapAnnotationTool.EXPORT_SCALE_FACTOR);
                        var vmEndPoint = Point.MakePoint(Points.Count + 1, vectorMapPos.Bx, vectorMapPos.Ly, vectorMapPos.H);
                        Points.Add(vmEndPoint);

                        //Line
                        int beforeLineID = 0;
                        if (i > 0)
                        {
                            beforeLineID = Lines.Count;
                            var beforeLine = Lines[beforeLineID - 1];
                            beforeLine.FLID = Lines.Count + 1;
                            Lines[beforeLineID - 1] = beforeLine; //This is needed for struct copy to be applied back
                        }
                        var vmLine = Line.MakeLine(Lines.Count + 1, vmStartPoint.PID, vmEndPoint.PID, beforeLineID, 0);
                        Lines.Add(vmLine);

                        if (i == 0)
                        {
                            lineSegTerminalIDsMapping[lineSegment][(int)TerminalType.START] = new KeyValuePair<int, LineType>(vmLine.LID, lineType);
                        }
                        if (i == positions.Count - 2)
                        {
                            lineSegTerminalIDsMapping[lineSegment][(int)TerminalType.END] = new KeyValuePair<int, LineType>(vmLine.LID, lineType);
                        }

                        int LinkID;
                        MakeVisualLine(vmLine, lineType, out LinkID);
                        if (lineType == LineType.STOP)
                        {
                            if (LinkID < 1)
                            {
                                Debug.LogError("Some stopline can not find have a correct linkID that equals to a nearby lane's id", lineSegment.gameObject);
                                UnityEditor.Selection.activeGameObject = lineSegment.gameObject;
                                Debug.LogError($"Selected the problematic {nameof(lineSegment)}");
                                return false;
                            }
                            if (stoplineLinkIDMapping.ContainsKey(lineSegment))
                            {
                                stoplineLinkIDMapping[lineSegment].Add(LinkID);
                                //Debug.Log("Extra IDs for same builder: " + stopLinkIDMapping[builder].Count);
                            }
                            else
                            {
                                stoplineLinkIDMapping.Add(lineSegment, new List<int>() { LinkID });
                            }
                        }
                    }
                }

                //correcting each segment's start and end line segments BLID and FLID to new line segments that is between each segment and their adjacent segment
                //only make BLID/FLID set to only one of the new line segments' last/first line ID because in the vector map format one line only has one BLID and one FLID
                //Later executed operation will overrite previously configured BLID and/or FLID
                foreach (var lineSegment in allLineSegments)
                {
                    var terminalIdPairs = lineSegTerminalIDsMapping[lineSegment];
                    if (lineSegment.befores.Count > 0 && lineSegTerminalIDsMapping.ContainsKey(lineSegment.befores[0])) //if before line doesn't exist in the record set then no operation to avoid extra in between line
                    {
                        var tPairs = terminalIdPairs[(int)TerminalType.START];
                        var segStartId = tPairs.Key;
                        var segStartLin = Lines[segStartId - 1];
                        var beforeSegEndLinId = lineSegTerminalIDsMapping[lineSegment.befores[0]][(int)TerminalType.END].Key; //only make BLID set to only one of the line segments' last line ID
                        var newLin = Line.MakeLine(Lines.Count + 1, Lines[beforeSegEndLinId - 1].FPID, segStartLin.BPID, beforeSegEndLinId, segStartLin.LID);
                        Lines.Add(newLin);
                        MakeVisualLine(newLin, tPairs.Value);
                        segStartLin.BLID = beforeSegEndLinId;
                        Lines[segStartId - 1] = segStartLin;
                    }

                    if (lineSegment.afters.Count > 0 && lineSegTerminalIDsMapping.ContainsKey(lineSegment.afters[0])) //if before line doesn't exist in the record set then no operation to avoid extra in between line
                    {
                        var tPairs = terminalIdPairs[(int)TerminalType.END];
                        var segEndId = tPairs.Key;
                        var segEndLin = Lines[segEndId - 1];
                        var afterSegStartLinId = lineSegTerminalIDsMapping[lineSegment.afters[0]][(int)TerminalType.START].Key; //only make FLID set to only one of the line segments' first line ID
                        var newLin = Line.MakeLine(Lines.Count + 1, segEndLin.FPID, Lines[afterSegStartLinId - 1].BPID, segEndLin.LID, afterSegStartLinId);
                        Lines.Add(newLin);
                        MakeVisualLine(newLin, tPairs.Value);
                        segEndLin.FLID = newLin.LID;
                        Lines[segEndId - 1] = segEndLin;
                    }
                }
            }

            //Setup all traffic light poles and their corrsponding traffic lights
            var tempIDMapping = new Dictionary<MapPole, int>(); //builder pole id mapping
            foreach (var pole in signalLightPoles)
            {
                //Vector
                var pos = pole.transform.position;
                var vectorMapPos = VectorMapUtility.GetVectorMapPosition(pos, MapAnnotationTool.EXPORT_SCALE_FACTOR);
                var PID = Points.Count + 1;
                var vmPoint = Point.MakePoint(PID, vectorMapPos.Bx, vectorMapPos.Ly, vectorMapPos.H);
                Points.Add(vmPoint);

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
                tempIDMapping.Add(pole, PLID);
            }


            foreach (var pole in signalLightPoles)
            {
                var PLID = tempIDMapping[pole];
                foreach (var signalLight in pole.signalLights)
                {
                    if (signalLight == null || !signalLight.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    var trafficLightAim = signalLight.transform.forward;
                    foreach (var lightData in signalLight.signalData)
                    {
                        //Vector
                        var trafficLightPos = signalLight.transform.TransformPoint(lightData.localPosition);
                        var vectorMapPos = VectorMapUtility.GetVectorMapPosition(trafficLightPos, MapAnnotationTool.EXPORT_SCALE_FACTOR);
                        var PID = Points.Count + 1;
                        var vmPoint = Point.MakePoint(PID, vectorMapPos.Bx, vectorMapPos.Ly, vectorMapPos.H);
                        Points.Add(vmPoint);

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
                        int ID = SignalDataList.Count + 1;
                        int Type = (int)lightData.signalColor;
                        int LinkID = -1;

                        if (signalLight.stopLine != null)
                        {
                            if (!stoplineLinkIDMapping.ContainsKey(signalLight.stopLine))
                            {
                                Debug.LogError("Selected the related stopline that is not in the mapping");
                                UnityEditor.Selection.activeGameObject = signalLight.stopLine.gameObject;
                                return false;
                            }

                            LinkID = PickAimingLinkID(signalLight.transform, stoplineLinkIDMapping[signalLight.stopLine]);

                            if (LinkID < 0)
                            {
                                Debug.LogError("Selected the related stopline that is related to the missing LinkID");
                                UnityEditor.Selection.activeGameObject = signalLight.stopLine.gameObject;
                                return false;
                            }
                        }
                        else
                        {
                            Debug.LogError($"some signal light({nameof(MapSignal)}) have null stopline");
                            UnityEditor.Selection.activeGameObject = signalLight.gameObject;
                            Debug.LogError($"selected the problematic {nameof(MapSignal)}");
                            return false;
#if THIS_IS_NOT_USED
                            LinkID = FindProperStoplineLinkID(trafficLightPos, trafficLightAim);
#endif
                        }

                        if (Type == 2)
                        {
                            Type = 3;
                        }
                        else if (Type == 3)
                        {
                            Type = 2;
                        }
                        var vmSignalData = SignalData.MakeSignalData(ID, VID, PLID, Type, LinkID);
                        SignalDataList.Add(vmSignalData);
                    }
                }
            }

            return true;
        }

        public void Export(string foldername)
        {
            MapAnnotationData = new MapManagerData();

            // Process lanes, intersections.
            MapAnnotationData.GetIntersections();
            MapAnnotationData.GetTrafficLanes();

            if (Calculate())
            {
                var sb = new StringBuilder();

                foreach (var list in ExportLists)
                {
                    var csvFilename = list.FileName;
                    var csvHeader = VectorMapUtility.GetCSVHeader(list.Type);

                    var filepath = Path.Combine(foldername, csvFilename);

                    if (!System.IO.Directory.Exists(foldername))
                    {
                        System.IO.Directory.CreateDirectory(foldername);
                    }

                    if (System.IO.File.Exists(filepath))
                    {
                        System.IO.File.Delete(filepath);
                    }

                    sb.Clear();
                    using (StreamWriter sw = File.CreateText(filepath))
                    {
                        sb.Append(csvHeader);
                        sb.Append("\n");

                        foreach (var e in list.List)
                        {
                            foreach (var field in list.Type.GetFields(BindingFlags.Instance | BindingFlags.Public))
                            {
                                sb.Append(field.GetValue(e).ToString());
                                sb.Append(",");
                            }
                            sb.Remove(sb.Length - 1, 1);
                            sb.Append("\n");
                        }
                        sw.Write(sb);
                    }
                }

                Debug.Log("Successfully generated and exported Autoware Vector Map!");
            }
        }

        // Join and convert a set of singlely-connected segments and also setup world positions for all segments
        static void ConvertAndJointSingleConnectedSegments(MapVectorLane curSeg, HashSet<MapVectorLane> laneSegmentsSet, ref HashSet<MapVectorLane> newLaneSegmentsSet, HashSet<MapVectorLane> visitedSegs)
        {
            var combSegs = new List<MapVectorLane>();
            visitedSegs.Add(curSeg);
            combSegs.Add(curSeg);

            while (curSeg.Afters.Count == 1 && curSeg.Afters[0].Befores.Count == 1 && curSeg.Afters[0].Befores[0] == curSeg && !visitedSegs.Contains((MapVectorLane)(curSeg.Afters[0])))
            {
                visitedSegs.Add(curSeg.Afters[0]);
                combSegs.Add(curSeg.Afters[0]);
                curSeg = curSeg.Afters[0];
            }

            //construct new lane segments in with its wappoints in world positions and update related dependencies to relate to new segments
            MapVectorLane combinedSeg = new MapVectorLane();
            combinedSeg.LaneCount = curSeg.LaneCount;
            combinedSeg.LaneNumber = curSeg.LaneNumber;
            combinedSeg.SpeedLimit = curSeg.SpeedLimit;

            combinedSeg.Befores = combSegs[0].Befores;
            combinedSeg.Afters = combSegs[combSegs.Count - 1].Afters;

            foreach (var beforeSeg in combinedSeg.Befores)
            {
                for (int i = 0; i < beforeSeg.Afters.Count; i++)
                {
                    if (beforeSeg.Afters[i] == combSegs[0])
                    {
                        beforeSeg.Afters[i] = combinedSeg;
                    }
                }
            }
            foreach (var afterSeg in combinedSeg.Afters)
            {
                for (int i = 0; i < afterSeg.Befores.Count; i++)
                {
                    if (afterSeg.Befores[i] == combSegs[combSegs.Count - 1])
                    {
                        afterSeg.Befores[i] = combinedSeg;
                    }
                }
            }

            // Note: no local positions for combinedSeg
            foreach (var seg in combSegs)
            {
                foreach (var worldPos in seg.MapWorldPositions)
                {
                    combinedSeg.MapWorldPositions.Add(worldPos); //Convert to world position
                }
            }

            foreach (var seg in combSegs)
            {
                laneSegmentsSet.Remove(seg);
            }
            newLaneSegmentsSet.Add(combinedSeg);
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
                var point = Points[dtLane.PID - 1];
                var linkIDPos = VectorMapUtility.GetUnityPosition(new VectorMapPosition() { Bx = point.Bx, Ly = point.Ly, H = point.H }, MapAnnotationTool.EXPORT_SCALE_FACTOR);
                float dot = Vector3.Dot(srcDir, (linkIDPos - srcPos).normalized);
                if (dot > dotMax)
                {
                    theIdx = i;
                    dotMax = dot;
                }
            }

            // Find the aiming linkId for traffic signals that are located behind stopline
            // Temp solution: virtually move srcPos in -srcDir direction by 20 meter.
            srcPos -= srcDir * (20);
            for (int i = 0; i < LinkIDs.Count; i++)
            {
                var lane = Lanes[LinkIDs[i] - 1];
                var dtLane = DtLanes[lane.DID - 1];
                var point = Points[dtLane.PID - 1];
                var linkIDPos = VectorMapUtility.GetUnityPosition(new VectorMapPosition() { Bx = point.Bx, Ly = point.Ly, H = point.H }, MapAnnotationTool.EXPORT_SCALE_FACTOR);
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
    for (int i = 0; i < StopLines.Count; i++)
    {
        var line = lines[StopLines[i].LID - 1];
        var startPos = VectorMapUtility.GetUnityPosition(Points[line.BPID - 1]);
        var endPos = VectorMapUtility.GetUnityPosition(Points[line.FPID - 1]);
        startPos.Set(startPos.x, 0, startPos.z);
        endPos.Set(endPos.x, 0, endPos.z);
        var pos = (startPos + endPos) * 0.5f;
        if ((pos - trafficLightPos).magnitude < radius)
        {
            if (Vector3.Cross(trafficLightAim, startPos - trafficLightPos).y * Vector3.Cross(trafficLightAim, endPos - trafficLightPos).y < 0) // traffic light aims inbetween stopline's two ends
            {
                stoplineCandids.Add(StopLines[i].ID, new KeyValuePair<Vector3, Vector3>(startPos, endPos));
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

    return StopLines[nearestStopID - 1].LinkID;
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

        static void Interpolate(List<Vector3> waypoints, out List<Vector3> interpolatedWaypoints, float interpoInterval = 1.0f, bool addLastPoint = true)
        {
            interpolatedWaypoints = new List<Vector3>();

            interpolatedWaypoints.Add(waypoints[0]); //add the first point

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
            retLinkID = 0;

            var startPos = VectorMapUtility.GetUnityPosition(new VectorMapPosition() { Bx = Points[line.BPID - 1].Bx, Ly = Points[line.BPID - 1].Ly, H = Points[line.BPID - 1].H }, MapAnnotationTool.EXPORT_SCALE_FACTOR);
            var endPos = VectorMapUtility.GetUnityPosition(new VectorMapPosition() { Bx = Points[line.FPID - 1].Bx, Ly = Points[line.FPID - 1].Ly, H = Points[line.FPID - 1].H }, MapAnnotationTool.EXPORT_SCALE_FACTOR);
            if (type == LineType.STOP) //If it is stopline
            {
                int LinkID = FindNearestLaneId(VectorMapUtility.GetVectorMapPosition((startPos + endPos) / 2, MapAnnotationTool.EXPORT_SCALE_FACTOR), true);
                retLinkID = LinkID;
                var vmStopline = StopLine.MakeStopLine(StopLines.Count + 1, line.LID, 0, 0, LinkID);
                StopLines.Add(vmStopline);
            }
            else if (type == LineType.WHITE || type == LineType.YELLOW) //if it is whiteline
            {
                int LinkID = FindNearestLaneId(VectorMapUtility.GetVectorMapPosition((startPos + endPos) / 2, MapAnnotationTool.EXPORT_SCALE_FACTOR));
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
        int FindNearestLaneId(VectorMapPosition refPos, bool preferJunction = false)
        {
            //find nearest lane set first
            float radius = 6.0f / MapAnnotationTool.EXPORT_SCALE_FACTOR; // reduce 6.0 to increase speed if every map annotation is on the ground.
            var lnIDs = new List<int>();
            for (int i = 0; i < Lanes.Count; i++)
            {
                var lane = Lanes[i];
                var p = Points[DtLanes[lane.DID - 1].PID - 1];
                var dist = Mathf.Sqrt(Mathf.Pow((float)(refPos.Bx - p.Bx), 2.0f) + Mathf.Pow((float)(refPos.Ly - p.Ly), 2.0f) + Mathf.Pow((float)(refPos.H - p.H), 2.0f));
                if (dist < radius)
                {
                    lnIDs.Add(lane.LnID);
                }
            }

            float min = float.MaxValue;
            float jctMin = float.MaxValue;
            int retLnId = 0;
            int nearestJctLnId = 0;
            for (int i = 0; i < lnIDs.Count; i++)
            {
                bool jct = false;
                var lane = Lanes[lnIDs[i] - 1];

                if (preferJunction && lane.FLID2 > 0)
                {
                    jct = true;
                }

                var p = Points[DtLanes[lane.DID - 1].PID - 1];
                var dist = Mathf.Sqrt(Mathf.Pow((float)(refPos.Bx - p.Bx), 2.0f) + Mathf.Pow((float)(refPos.Ly - p.Ly), 2.0f) + Mathf.Pow((float)(refPos.H - p.H), 2.0f));
                if (dist < min)
                {
                    min = dist;
                    retLnId = lnIDs[i];
                }
                if (jct && dist < jctMin)
                {
                    jctMin = dist;
                    nearestJctLnId = lnIDs[i];
                }
            }

            if (nearestJctLnId != 0)
            {
                retLnId = nearestJctLnId;
            }

            return retLnId;
        }
    }
}
