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

namespace VectorMap
{
    public enum LineColor
    {
        WHITE,
        YELLOW,
    }

    public static class Draw
    {
        public static void DrawArrowForDebug(Vector3 pos, Vector3 end, Color color, float scaler = 1.0f, float arrowHeadLength = 0.02f, float arrowHeadAngle = 20.0f, float arrowPositionRatio = 0.5f)
        {
            var forwardVec = (end - pos).normalized * arrowPositionRatio * (pos - end).magnitude;

            //Draw line
            Debug.DrawRay(pos, forwardVec, color);

            //Draw arrow head
            Vector3 right = (Quaternion.LookRotation(forwardVec) * Quaternion.Euler(arrowHeadAngle, 0, 0) * Vector3.back) * arrowHeadLength;
            Vector3 left = (Quaternion.LookRotation(forwardVec) * Quaternion.Euler(-arrowHeadAngle, 0, 0) * Vector3.back) * arrowHeadLength;
            Vector3 up = (Quaternion.LookRotation(forwardVec) * Quaternion.Euler(0, arrowHeadAngle, 0) * Vector3.back) * arrowHeadLength;
            Vector3 down = (Quaternion.LookRotation(forwardVec) * Quaternion.Euler(0, -arrowHeadAngle, 0) * Vector3.back) * arrowHeadLength;

            right *= scaler;
            left *= scaler;
            up *= scaler;
            down *= scaler;

            Vector3 arrowTip = pos + (forwardVec);

            Debug.DrawRay(arrowTip, right, color);
            Debug.DrawRay(arrowTip, left, color);
            Debug.DrawRay(arrowTip, up, color);
            Debug.DrawRay(arrowTip, down, color);
        }
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

        public static List<Vector3> GetWorldCoordinates(List<Vector3> waypointsLocal, Transform refTrans)
        {
            List<Vector3> worldCoordinates = new List<Vector3>();
            foreach (var pointLocal in waypointsLocal)
            {
                worldCoordinates.Add(refTrans.TransformPoint(pointLocal));
            }
            return worldCoordinates;
        }

        public static void Interpolate(List<Vector3> waypoints, List<LaneInfo> laneInfos, out List<Vector3> interpolatedWaypoints, out List<LaneInfo> interpolatedLaneInfos, float fixedDistance = 1.0f, bool addLastPoint = true)
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

                    if (accumulatedDist + forwardVec.magnitude < fixedDistance)
                    {
                        accumulatedDist += forwardVec.magnitude;
                        startPoint += forwardVec;
                        ++curIndex; //Still accumulating so keep looping
                    }
                    else
                    {
                        newPoint = startPoint + forwardVec.normalized * (fixedDistance - accumulatedDist);
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
    }

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
}
