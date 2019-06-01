/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Simulator.Map;

namespace Simulator.Map
{
    public class MapLine : MapData
    {
        public bool displayHandles = false;
        public List<Vector3> mapLocalPositions = new List<Vector3>();
        [System.NonSerialized]
        public List<Vector3> mapWorldPositions = new List<Vector3>();
        [System.NonSerialized]
        public List<MapLine> befores = new List<MapLine>();
        [System.NonSerialized]
        public List<MapLine> afters = new List<MapLine>();
        [System.NonSerialized]
        public MapSignal signal; // TODO multiple signals?
        [System.NonSerialized]
        public MapIntersection intersection;

        public LineType lineType;
        public bool isStopSign = false;
        public SignalLightStateType currentState = SignalLightStateType.Yellow;

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
            {
#if UNITY_EDITOR
                UnityEditor.Handles.Label(transform.position, "    " + lineType + " LINE");
#endif
            }
            AnnotationGizmos.DrawWaypoints(transform, mapLocalPositions, MapAnnotationTool.PROXIMITY * 0.5f, typeColor + selectedColor);
            AnnotationGizmos.DrawLines(transform, mapLocalPositions, typeColor + selectedColor);
        }
    }
}