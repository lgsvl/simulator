/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapLine : MapData
{
    public bool displayHandles = false;
    public List<Vector3> mapLocalPositions = new List<Vector3>();
    [System.NonSerialized]
    public List<Vector3> mapWorldPositions = new List<Vector3>();
    public LineType lineType;

    [System.NonSerialized]
    public MapIntersection mapIntersection;
    //[System.NonSerialized]
    //public IntersectionTrafficLightSetComponent intersectionTrafficLightSetC;

    // stopline
    public bool isStopSign = false;
    public TrafficLightSetState currentState = TrafficLightSetState.Yellow;

    //public void GetTrafficLightSet()
    //{
    //    foreach (var item in mapIntersection.intersectionC.lightGroups)
    //    {
    //        float dot = Vector3.Dot(this.transform.TransformDirection(Vector3.forward), item.transform.TransformDirection(Vector3.forward)); // TODO not vector right usually
    //        //if (debug) Debug.Log(dot);

    //        if (dot < -0.7f)
    //        {
    //            //if (debug) Debug.Log(dot);
    //            intersectionTrafficLightSetC = item;
    //            intersectionTrafficLightSetC.stopline = this;

    //        }
    //    }
    //}

    public override void Draw()
    {
        if (mapLocalPositions.Count < 2) return;

        Color typeColor = Color.clear;
        switch (lineType)
        {
            case LineType.UNKNOWN:
                typeColor = Color.black;
                break;
            case LineType.SOLID_WHITE:
            case LineType.DOTTED_WHITE:
            case LineType.DOUBLE_WHITE:
                typeColor = whiteLineColor;
                break;
            case LineType.SOLID_YELLOW:
            case LineType.DOTTED_YELLOW:
            case LineType.DOUBLE_YELLOW:
                typeColor = yellowLineColor;
                break;
            case LineType.CURB:
                typeColor = curbColor;
                break;
            case LineType.STOP:
                typeColor = stopLineColor;
                break;
            default:
                break;
        }

        if (MapAnnotationTool.SHOW_HELP)
            UnityEditor.Handles.Label(transform.position, "    " + lineType + " LINE");
        AnnotationGizmos.DrawWaypoints(transform, mapLocalPositions, MapAnnotationTool.PROXIMITY * 0.5f, typeColor);
        AnnotationGizmos.DrawLines(transform, mapLocalPositions, typeColor);
    }
}
