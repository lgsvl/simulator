/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using UnityEngine;
using System;
using static Apollo.Utils;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Map
{
    namespace Apollo
    {
        //Highest level entity in HD map hierarchy
        public class HDMap
        {
            public Header? header;
            public List<CrossWalk> crosswalk;
            public List<Junction> junction;
            public List<Lane> lane;//Basic
            public List<StopSign> stop_sign;
            public List<Signal> signal;
            public List<YieldSign> yield;
            public List<Overlap> overlap; //Basic
            public List<ClearArea> clear_area;
            public List<SpeedBump> speed_bump;
            public List<Road> road;
        }

        //Top level fields
        public struct Header
        {
            public string version;
            public string date;
            public Projection? projection;
            public string district;
            public string generation;
            public string rev_major;
            public string rev_minor;
            public double? left;
            public double? top;
            public double? right;
            public double? bottom;
            public string vendor;
        }

        public struct CrossWalk
        {
            //To be finished
        }

        public struct Junction
        {
            //To be finished
        }

        public struct LineSegment
        {
            public List<Ros.PointENU> point; //Basic
        }

        public struct CurveSegment
        {
            public struct CurveType_OneOf : IOneOf<CurveType_OneOf>
            {
                public LineSegment? line_segment;

                public KeyValuePair<string, object> GetOne()
                {
                    if (line_segment != null)
                    {
                        return new KeyValuePair<string, object>(nameof(line_segment), line_segment);
                    }
                    return new KeyValuePair<string, object>("", null);
                }
            }
            public IOneOf<CurveType_OneOf> curve_type;

            public double? s;
            public Ros.PointENU? start_position;
            public double? heading;//Basic // start orientation
            public double? length;//Basic
        }

        public struct Curve
        {
            public List<CurveSegment> segment;
        }

        //point count of left boundary, right boundary, central curve and left samples right samples should all be the same
        //length of central curve length can be different from boundaries' lengths
        public struct Lane
        {
            public Id? id;

            public Curve? central_curve;

            public LaneBoundary? left_boundary;
            public LaneBoundary? right_boundary;

            public double? length;//Basic

            public double? speed_limit;//Basic

            public List<Id> overlap_id;//Basic

            public List<Id> predecessor_id;
            public List<Id> successor_id;

            public List<Id> left_neighbor_forward_lane_id;
            public List<Id> right_neighbor_forward_lane_id;

            public enum LaneType
            {
                NONE = 1,
                CITY_DRIVING = 2,
                BIKING = 3,
                SIDEWALK = 4,
                PARKING = 5,
                SHOULDER = 6,
            }
            public LaneType? type;//Basic

            public enum LaneTurn
            {
                NO_TURN = 1,
                LEFT_TURN = 2,
                RIGHT_TURN = 3,
                U_TURN = 4,
            }
            public LaneTurn? turn;//Basic, meaning?

            public List<Id> left_neighbor_reverse_lane_id;
            public List<Id> right_neighbor_reverse_lane_id;

            public Id? junction_id;

            public List<LaneSampleAssociation> left_sample;//Basic, last sample's s is same as central curve length
            public List<LaneSampleAssociation> right_sample;//Basic, last sample's s is same as central curve length

            public enum LaneDirection
            {
                FORWARD = 1,
                BACKWARD = 2,
                BIDIRECTION = 3,
            }
            public LaneDirection? direction;

            public List<LaneSampleAssociation> left_road_sample;
            public List<LaneSampleAssociation> right_road_sample;
        }

        public struct LaneBoundaryType
        {
            public enum Type
            {
                UNKNOWN = 0,
                DOTTED_YELLOW = 1,
                DOTTED_WHITE = 2,
                SOLID_YELLOW = 3,
                SOLID_WHITE = 4,
                DOUBLE_YELLOW = 5,
                CURB = 6,
            }

            public double? s;//Basic

            public List<LaneBoundaryType.Type> types;//Basic
        }

        public struct LaneBoundary
        {
            public Curve? curve;
            public double? length;//Basic, this length should be the same as the length in Curve.CurveSegment
            public bool? @virtual;
            public List<LaneBoundaryType> boundary_type;//Basic
        }

        public struct LaneSampleAssociation
        {
            public double? s;
            public double? width;
        }

        public struct StopSign
        {
            public Id? id;

            public List<Curve> stop_line;

            public List<Id> overlap_id;
        }

        public struct Signal
        {
            public enum Type
            {
                UNKNOWN = 1,
                MIX_2_HORIZONTAL = 2,
                MIX_2_VERTICAL = 3,
                MIX_3_HORIZONTAL = 4,
                MIX_3_VERTICAL = 5,
                SINGLE = 6,
            }

            public Id? id;
            public Polygon? boundary;
            public List<Subsignal> subsignal;
            public List<Id> overlap_id;
            public Type? type;
            public List<Curve> stop_line;
        }

        public struct YieldSign
        {
            //To be finished
        }

        public struct Overlap
        {
            public Id? id;

            // Information about one overlap, include all overlapped objects.
            public List<ObjectOverlapInfo> @object;
        }

        public struct ClearArea
        {
            //To be finished
        }

        public struct SpeedBump
        {
            //To be finished
        }

        public struct Road
        {
            public Id? id;
            public List<RoadSection> section;
            public Id? junction_id;
        }

        //Other component fields
        public struct Projection
        {
            public string proj;
        }

        public struct Id
        {
            public string id;

            public Id(string id)
            {
                this.id = id;
            }

            public static implicit operator Id(string s)
            {
                return new Id(s.ToString());
            }

            public static implicit operator Id(int i)
            {
                return new Id(i.ToString());
            }

            public static implicit operator Id(float f)
            {
                return new Id(f.ToString());
            }

            public static implicit operator Id(double d)
            {
                return new Id(d.ToString());
            }
        }

        public struct LaneOverlapInfo
        {
            public double? start_s;
            public double? end_s;
            public bool? is_merge;
        }

        public struct SignalOverlapInfo
        {
        }

        public struct StopSignOverlapInfo
        {
        }

        public struct CrosswalkOverlapInfo
        {
        }

        public struct JunctionOverlapInfo
        {
        }

        public struct YieldOverlapInfo
        {
        }

        public struct ClearAreaOverlapInfo
        {
        }

        public struct SpeedBumpOverlapInfo
        {
        }

        public struct ParkingSpaceOverlapInfo
        {
        }

        public struct ObjectOverlapInfo
        {
            public Id? id;

            public struct OverlapInfo_OneOf : IOneOf<OverlapInfo_OneOf>
            {
                public LaneOverlapInfo? lane_overlap_info;
                public SignalOverlapInfo? signal_overlap_info;
                public StopSignOverlapInfo? stop_sign_overlap_info;
                public CrosswalkOverlapInfo? crosswalk_overlap_info;
                public JunctionOverlapInfo? junction_overlap_info;
                public YieldOverlapInfo? yield_sign_overlap_info;
                public ClearAreaOverlapInfo? clear_area_overlap_info;
                public SpeedBumpOverlapInfo? speed_bump_overlap_info;
                public ParkingSpaceOverlapInfo? parking_space_overlap_info;

                public KeyValuePair<string, object> GetOne()
                {
                    if (lane_overlap_info != null)
                    {
                        return new KeyValuePair<string, object>(nameof(lane_overlap_info), lane_overlap_info);
                    }
                    else if (signal_overlap_info != null)
                    {
                        return new KeyValuePair<string, object>(nameof(signal_overlap_info), signal_overlap_info);
                    }
                    else if (stop_sign_overlap_info != null)
                    {
                        return new KeyValuePair<string, object>(nameof(stop_sign_overlap_info), stop_sign_overlap_info);
                    }
                    else if (crosswalk_overlap_info != null)
                    {
                        return new KeyValuePair<string, object>(nameof(crosswalk_overlap_info), crosswalk_overlap_info);
                    }
                    else if (junction_overlap_info != null)
                    {
                        return new KeyValuePair<string, object>(nameof(junction_overlap_info), junction_overlap_info);
                    }
                    else if (yield_sign_overlap_info != null)
                    {
                        return new KeyValuePair<string, object>(nameof(yield_sign_overlap_info), yield_sign_overlap_info);
                    }
                    else if (clear_area_overlap_info != null)
                    {
                        return new KeyValuePair<string, object>(nameof(clear_area_overlap_info), clear_area_overlap_info);
                    }
                    else if (speed_bump_overlap_info != null)
                    {
                        return new KeyValuePair<string, object>(nameof(speed_bump_overlap_info), speed_bump_overlap_info);
                    }
                    else if (parking_space_overlap_info != null)
                    {
                        return new KeyValuePair<string, object>(nameof(parking_space_overlap_info), parking_space_overlap_info);
                    }
                    return new KeyValuePair<string, object>("", null);
                }
            }
            public IOneOf<OverlapInfo_OneOf> overlap_info;
        }

        public struct Polygon
        {
            public List<Ros.PointENU> point;
        }

        public struct Subsignal
        {
            public enum Type
            {
                UNKNOWN = 1,
                CIRCLE = 2,
                ARROW_LEFT = 3,
                ARROW_FORWARD = 4,
                ARROW_RIGHT = 5,
                ARROW_LEFT_AND_FORWARD = 6,
                ARROW_RIGHT_AND_FORWARD = 7,
                ARROW_U_TURN = 8,
            }

            public Id? id;
            public Type? type;

            public Ros.PointENU? location;
        }

        public struct RoadSection
        {
            public Id? id;
            public List<Id> lane_id;
            public RoadBoundary? boundary;
        }

        public struct RoadBoundary
        {
            public BoundaryPolygon? outer_polygon;
            public List<BoundaryPolygon> hole;
        }

        public struct RoadROIBoundary
        {
            public Id? id;
            public List<RoadBoundary> road_boundaries;
        }

        public struct BoundaryPolygon
        {
            public List<BoundaryEdge> edge;
        }

        public struct BoundaryEdge
        {
            public enum Type
            {
                UNKNOWN = 0,
                NORMAL = 1,
                LEFT_BOUNDARY = 2,
                RIGHT_BOUNDARY = 3,
            }

            public Curve? curve;
            public Type? type;
        }

        public static class HDMapUtil
        {
            public static float Angle = -45.3f;
            //Convert coordinate to Autoware/Rviz coordinate
            public static Ros.PointENU GetApolloCoordinates(Vector3 unityPos, bool dim3D = true)
            {
                return new Ros.PointENU() { x = unityPos.x, y = unityPos.z, z = dim3D ? unityPos.y : (double?)null };
            }

            public static Ros.PointENU GetApolloCoordinates(Vector3 unityPos, float scale, bool dim3D = true)
            {
                return new Ros.PointENU() { x = unityPos.x * scale, y = unityPos.z * scale, z = dim3D ? unityPos.y * scale : (double?)null };
            }

            public static Ros.PointENU GetApolloCoordinates(Vector3 unityPos, float originEasting, float originNorthing, bool dim3D = true)
            {
                return GetApolloCoordinates(unityPos, originEasting, originNorthing, 0, dim3D);
            }

            public static Ros.PointENU GetApolloCoordinates(Vector3 unityPos, float originEasting, float originNorthing, float altitudeOffset, bool dim3D = true)
            {
                unityPos = Quaternion.Euler(0f, Angle, 0f) * unityPos;
                return new Ros.PointENU() { x = unityPos.x + originEasting, y = unityPos.z + originNorthing, z = dim3D ? unityPos.y + altitudeOffset : (double?)null };
            }

            public static Ros.PointENU GetApolloCoordinates(Vector3 unityPos, float scale, float originEasting, float originNorthing, float altitudeOffset, bool dim3D = true)
            {
                unityPos = Quaternion.Euler(0f, Angle, 0f) * unityPos;
                return new Ros.PointENU() { x = unityPos.x * scale + originEasting, y = unityPos.z * scale + originNorthing, z = dim3D ? unityPos.y * scale * altitudeOffset : (double?)null };
            }

            public static Vector3 GetUnityPosition(Ros.PointENU point)
            {
                return new Vector3((float)point.x, (float)point.z, (float)point.y);
            }

            public static void SerializeHDMap(HDMap map, out StringBuilder sb)
            {
                sb = new StringBuilder();
                Ros.Bridge.SerializeInternal(1, sb, map.GetType(), map, sType: Ros.SerialType.HDMap);
                sb.Trim();
                if (sb[0] == '{')
                {
                    sb.Remove(0, 1);
                }
                if (sb[sb.Length - 1] == '}')
                {
                    sb.Remove(sb.Length - 1, 1);
                }
            }
        }
    }

    namespace Autoware
    {
        //Required CSV Entries

        public class Point
        {
            public int PID;
            public double B;
            public double L;
            public double H;
            public double Bx;
            public double Ly;
            public int ReF;
            public int MCODE1;
            public int MCODE2;
            public int MCODE3;

            public static Point GetDefaultPoint()
            {
                return new Point()
                {
                    PID = 1,
                    B = .0,
                    L = .0,
                    H = .0,
                    Bx = .0,
                    Ly = .0,
                    ReF = 7,
                    MCODE1 = 0,
                    MCODE2 = 0,
                    MCODE3 = 0,
                };
            }

            public static Point MakePoint(int PID, double Bx, double Ly, double H)
            {
                return new Point()
                {
                    PID = PID,
                    B = .0,
                    L = .0,
                    H = H,
                    Bx = Bx,
                    Ly = Ly,
                    ReF = 7,
                    MCODE1 = 0,
                    MCODE2 = 0,
                    MCODE3 = 0,
                };
            }
        }

        public struct Line
        {
            public int LID;
            public int BPID;
            public int FPID;
            public int BLID;
            public int FLID;

            public static Line GetDefaultLine()
            {
                return new Line()
                {
                    LID = 1,
                    BPID = 1,
                    FPID = 2,
                    BLID = 0,
                    FLID = 2,
                };
            }

            public static Line MakeLine(int LID, int BPID, int FPID, int BLID, int FLID)
            {
                return new Line()
                {
                    LID = LID,
                    BPID = BPID,
                    FPID = FPID,
                    BLID = BLID, //this is before line id
                    FLID = FLID, //this is after line id
                };
            }
        }

        public struct Lane
        {
            public int LnID;
            public int DID;
            public int BLID;
            public int FLID;
            public int BNID;
            public int FNID;
            public int JCT;
            public int BLID2;
            public int BLID3;
            public int BLID4;
            public int FLID2;
            public int FLID3;
            public int FLID4;
            public int ClossID;
            public double Span;
            public int LCnt;
            public int Lno;
            public int LaneType;
            public int LimitVel;
            public int RefVel;
            public int RoadSecID;
            public int LaneChgFG;

            public static Lane GetDefaultLane()
            {
                return new Lane()
                {
                    LnID = 1,
                    DID = 1,
                    BLID = 0,
                    FLID = 1,
                    BNID = 1,
                    FNID = 2,
                    JCT = 0,
                    BLID2 = 0,
                    BLID3 = 0,
                    BLID4 = 0,
                    FLID2 = 0,
                    FLID3 = 0,
                    FLID4 = 0,
                    ClossID = 0,
                    Span = 1.0,
                    LCnt = 1,
                    Lno = 1,
                    LaneType = 0,
                    LimitVel = 60,
                    RefVel = 60,
                    RoadSecID = 0,
                    LaneChgFG = 0,
                };
            }

            public static Lane MakeLane(int LnID, int DID, int BLID, int FLID, int LCnt, int Lno)
            {
                return new Lane()
                {
                    LnID = LnID,
                    DID = DID,
                    BLID = BLID, //this is before lane id
                    FLID = FLID, //this is after lane id
                    BNID = 1,
                    FNID = 2,
                    JCT = 0,
                    BLID2 = 0,
                    BLID3 = 0,
                    BLID4 = 0,
                    FLID2 = 0,
                    FLID3 = 0,
                    FLID4 = 0,
                    ClossID = 0,
                    Span = 1.0,
                    LCnt = LCnt,
                    Lno = Lno,
                    LaneType = 0,
                    LimitVel = 60,
                    RefVel = 60,
                    RoadSecID = 0,
                    LaneChgFG = 0,
                };
            }
        }

        public struct DtLane
        {
            public int DID;
            public double Dist; //int or double?
            public int PID;
            public double Dir;
            public double Apara;
            public double r;
            public double slope;
            public double cant;
            public double LW;
            public double RW;

            public static DtLane GetDefaultDtLane()
            {
                return new DtLane()
                {
                    DID = 1,
                    Dist = .0,
                    PID = 1,
                    Dir = .0,
                    Apara = .0,
                    r = .0,
                    slope = .0,
                    cant = .0,
                    LW = .065,
                    RW = .065,
                };
            }

            public static DtLane MakeDtLane(int DID, int Dist, int PID, double Dir, double slope, double LW, double RW)
            {
                return new DtLane()
                {
                    DID = DID,
                    Dist = Dist,
                    PID = PID,
                    Dir = Dir,
                    Apara = .0,
                    r = .0,
                    slope = slope,
                    cant = .0,
                    LW = LW,
                    RW = RW,
                };
            }
        }

        public struct StopLine
        {
            public int ID;
            public int LID;
            public int TLID;
            public int SignID;
            public int LinkID;

            public static StopLine GetDefaultStopLine()
            {
                return new StopLine()
                {
                    ID = 1,
                    LID = 1,
                    TLID = 0,
                    SignID = 0,
                    LinkID = 0,
                };
            }

            public static StopLine MakeStopLine(int ID, int LID, int TLID, int SignID, int LinkID)
            {
                return new StopLine()
                {
                    ID = ID,
                    LID = LID,
                    TLID = TLID,
                    SignID = SignID,
                    LinkID = LinkID,
                };
            }
        }

        public struct WhiteLine
        {
            public int ID;
            public int LID;
            public double Width;
            public string Color;
            public int type;
            public int LinkID;

            public static WhiteLine GetDefaultWhiteLine()
            {
                return new WhiteLine()
                {
                    ID = 1,
                    LID = 1,
                    Width = .15,
                    Color = "W",
                    type = 0,
                    LinkID = 0,
                };
            }

            public static WhiteLine MakeWhiteLine(int ID, int LID, double Width, string Color, int type, int LinkID)
            {
                return new WhiteLine()
                {
                    ID = ID,
                    LID = LID,
                    Width = Width,
                    Color = Color,
                    type = type,
                    LinkID = LinkID,
                };
            }
        }

        public struct Vector
        {
            public int VID;
            public int PID;
            public double Hang;
            public double Vang;

            public static Vector GetDefaultVector()
            {
                return new Vector()
                {
                    VID = 1,
                    PID = 1,
                    Hang = .0,
                    Vang = .0,
                };
            }

            public static Vector MakeVector(int VID, int PID, double Hang, double Vang)
            {
                return new Vector()
                {
                    VID = VID,
                    PID = PID,
                    Hang = Hang,
                    Vang = Vang,
                };
            }
        }

        public struct Pole
        {
            public int PLID;
            public int VID;
            public double Length;
            public double Dim;

            public static Pole GetDefaultPole()
            {
                return new Pole()
                {
                    PLID = 1,
                    VID = 1,
                    Length = 13.5,
                    Dim = 0.4,
                };
            }

            public static Pole MakePole(int PLID, int VID, double Length, double Dim)
            {
                return new Pole()
                {
                    PLID = PLID,
                    VID = VID,
                    Length = Length,
                    Dim = Dim,
                };
            }
        }

        public struct SignalData
        {
            public int ID;
            public int VID;
            public int PLID;
            public int Type;
            public int LinkID;

            public static SignalData GetDefaultSignalData()
            {
                return new SignalData()
                {
                    ID = 1,
                    VID = 1,
                    PLID = 1,
                    Type = 1,
                    LinkID = 1,
                };
            }

            public static SignalData MakeSignalData(int ID, int VID, int PLID, int Type, int LinkID)
            {
                return new SignalData()
                {
                    ID = ID,
                    VID = VID,
                    PLID = PLID,
                    Type = Type,
                    LinkID = LinkID,
                };
            }
        }

        //Other

        [System.Serializable]
        public struct LaneInfo
        {
            public int laneCount;
            public int laneNumber;
        }

        public struct VectorMapPosition
        {
            public double Bx;
            public double Ly;
            public double H;
        }

        public class VectorMapUtility
        {
            public static string GetCSVHeader(System.Type type)
            {
                var fieldInfos = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
                return string.Join(",", fieldInfos.Select(x => x.Name));
            }

            //Convert coordinate to Autoware/Rviz coordinate
            public static Vector3 GetRvizCoordinates(Vector3 unityPos)
            {
                return new Vector3(unityPos.x, unityPos.z, unityPos.y);
            }

            //Convert coordinate to Autoware/Rviz coordinate
            public static Vector3 GetUnityCoordinate(Vector3 rvizPos)
            {
                return new Vector3(rvizPos.x, rvizPos.z, rvizPos.y);
            }

            public static VectorMapPosition GetVectorMapPosition(Vector3 unityPos, float exportScale = 1)
            {
                var convertedPos = VectorMapUtility.GetRvizCoordinates(unityPos);
                convertedPos *= exportScale;
                return new VectorMapPosition() { Bx = convertedPos.y, Ly = convertedPos.x, H = convertedPos.z };
            }

            public static Vector3 GetUnityPosition(Point vmPoint)
            {
                return GetUnityPosition(new VectorMapPosition() { Bx = vmPoint.Bx, Ly = vmPoint.Ly, H = vmPoint.H });
            }

            public static Vector3 GetUnityPosition(VectorMapPosition vmPos, float exportScale = 1)
            {
                var inverseConvertedPos = new Vector3((float)vmPos.Ly, (float)vmPos.Bx, (float)vmPos.H);
                inverseConvertedPos /= exportScale;
                return VectorMapUtility.GetUnityCoordinate(inverseConvertedPos);
            }

            public static List<Vector3> GetWorldCoordinates(List<Vector3> waypointsLocal, Transform refTrans)
            {
                List<Vector3> worldCoordinates = new List<Vector3>();
                foreach (var pointLocal in waypointsLocal)
                {
                    worldCoordinates.Add(refTrans.TransformPoint(pointLocal));
                }
                return worldCoordinates;
            }
        }
    }

    public enum BoundLineType
    {
        UNKNOWN = -1,
        SOLID_WHITE = 0,
        SOLID_YELLOW,
        DOTTED_WHITE,
        DOTTED_YELLOW,
        DOUBLE_WHITE,
        DOUBLE_YELLOW,
        CURB,
    }

    namespace Draw
    {
        public static class Gizmos
        {
            public static void DrawArrow(Vector3 start, Vector3 end, Color color, float arrowHeadScale = 1.0f, float arrowHeadLength = 0.02f, float arrowHeadAngle = 20.0f, float arrowPositionRatio = 0.5f)
            {
                var originColor = UnityEngine.Gizmos.color;
                UnityEngine.Gizmos.color = color;

                //Draw base line
                UnityEngine.Gizmos.DrawLine(start, end);

                var lineVector = end - start;
                var arrowFwdVec = lineVector.normalized * arrowPositionRatio * lineVector.magnitude;

                //Draw arrow head
                Vector3 right = (Quaternion.LookRotation(arrowFwdVec) * Quaternion.Euler(arrowHeadAngle, 0, 0) * Vector3.back) * arrowHeadLength;
                Vector3 left = (Quaternion.LookRotation(arrowFwdVec) * Quaternion.Euler(-arrowHeadAngle, 0, 0) * Vector3.back) * arrowHeadLength;
                Vector3 up = (Quaternion.LookRotation(arrowFwdVec) * Quaternion.Euler(0, arrowHeadAngle, 0) * Vector3.back) * arrowHeadLength;
                Vector3 down = (Quaternion.LookRotation(arrowFwdVec) * Quaternion.Euler(0, -arrowHeadAngle, 0) * Vector3.back) * arrowHeadLength;

                Vector3 arrowTip = start + (arrowFwdVec);

                UnityEngine.Gizmos.DrawLine(arrowTip, arrowTip + right * arrowHeadScale);
                UnityEngine.Gizmos.DrawLine(arrowTip, arrowTip + left * arrowHeadScale);
                UnityEngine.Gizmos.DrawLine(arrowTip, arrowTip + up * arrowHeadScale);
                UnityEngine.Gizmos.DrawLine(arrowTip, arrowTip + down * arrowHeadScale);

                UnityEngine.Gizmos.color = originColor;
            }
        }
#if UNITY_EDITOR
        public static class Handles
        {
            public static void DrawArrow(Vector3 start, Vector3 end, Color color, float arrowHeadScale = 1.0f, float arrowHeadLength = 0.02f, float arrowHeadAngle = 20.0f, float arrowPositionRatio = 0.5f)
            {
                var originColor = UnityEditor.Handles.color;
                UnityEditor.Handles.color = color;

                //Draw base line
                UnityEditor.Handles.DrawLine(start, end);

                var lineVector = end - start;
                var arrowFwdVec = lineVector.normalized * arrowPositionRatio * lineVector.magnitude;

                //Draw arrow head
                Vector3 right = (Quaternion.LookRotation(arrowFwdVec) * Quaternion.Euler(arrowHeadAngle, 0, 0) * Vector3.back) * arrowHeadLength;
                Vector3 left = (Quaternion.LookRotation(arrowFwdVec) * Quaternion.Euler(-arrowHeadAngle, 0, 0) * Vector3.back) * arrowHeadLength;
                Vector3 up = (Quaternion.LookRotation(arrowFwdVec) * Quaternion.Euler(0, arrowHeadAngle, 0) * Vector3.back) * arrowHeadLength;
                Vector3 down = (Quaternion.LookRotation(arrowFwdVec) * Quaternion.Euler(0, -arrowHeadAngle, 0) * Vector3.back) * arrowHeadLength;

                Vector3 arrowTip = start + (arrowFwdVec);

                UnityEditor.Handles.DrawLine(arrowTip, arrowTip + right * arrowHeadScale);
                UnityEditor.Handles.DrawLine(arrowTip, arrowTip + left * arrowHeadScale);
                UnityEditor.Handles.DrawLine(arrowTip, arrowTip + up * arrowHeadScale);
                UnityEditor.Handles.DrawLine(arrowTip, arrowTip + down * arrowHeadScale);

                UnityEditor.Handles.color = originColor;
            }
        }
#endif
    }
}
