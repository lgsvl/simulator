/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapData : MonoBehaviour
{
    public Color laneColor { get; private set; } = Color.cyan;
    public Color whiteLineColor { get; private set; } = Color.white;
    public Color yellowLineColor { get; private set; } = Color.yellow;
    public Color stopLineColor { get; private set; } = Color.red;
    public Color stopSignColor { get; private set; } = Color.red;
    public Color junctionColor { get; private set; } = Color.gray;
    public Color poleColor { get; private set; } = Color.white;
    public Color speedBumpColor { get; private set; } = Color.yellow;
    public Color curbColor { get; private set; } = Color.blue;
    public Color pedestrianColor { get; private set; } = Color.green;
    public Color intersectionColor { get; private set; } = new Color(1f, 0.5f, 0.0f);

    public enum LaneTurnType
    {
        NO_TURN = 1,
        LEFT_TURN = 2,
        RIGHT_TURN = 3,
        U_TURN = 4
    };

    public enum LaneBoundaryType
    {
        UNKNOWN = 0,
        DOTTED_YELLOW = 1,
        DOTTED_WHITE = 2,
        SOLID_YELLOW = 3,
        SOLID_WHITE = 4,
        DOUBLE_YELLOW = 5,
        CURB = 6
    };

    public enum LineType
    {
        UNKNOWN = -1, // TODO why is this -1 not 0?
        SOLID_WHITE = 0,
        SOLID_YELLOW = 1,
        DOTTED_WHITE = 2,
        DOTTED_YELLOW = 3,
        DOUBLE_WHITE = 4,
        DOUBLE_YELLOW = 5,
        CURB = 6,
        STOP = 7
    };

    [System.Serializable]
    public class SignalData
    {
        public Vector3 localPosition = Vector3.zero;
        public SignalColorType signalColor = SignalColorType.Yellow;
    }

    public enum SignalColorType
    {
        Red = 1,
        Yellow = 2,
        Green = 3
    };

    public enum TrafficLightSetState
    {
        Red,
        Green,
        Yellow
    };

    public enum SignType
    {
        STOP = 0,
        YIELD = 1,
        // TODO all the signs!
    }

    public virtual void Draw()
    {
        //
    }

    protected virtual void OnDrawGizmos()
    {
        if (MapAnnotationTool.SHOW_MAP_ALL)
            Draw();
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (MapAnnotationTool.SHOW_MAP_SELECTED)
            Draw();
    }

    public static class AnnotationGizmos
    {
        public static void DrawArrowHead(Vector3 start, Vector3 end, Color color, float arrowHeadScale = 1.0f, float arrowHeadLength = 0.02f, float arrowHeadAngle = 13.0f, float arrowPositionRatio = 0.5f)
        {
            var originColor = Gizmos.color;
            Gizmos.color = color;

            var lineVector = end - start;
            var arrowFwdVec = lineVector.normalized * arrowPositionRatio * lineVector.magnitude;
            if (arrowFwdVec == Vector3.zero) return;

            //Draw arrow head
            Vector3 right = (Quaternion.LookRotation(arrowFwdVec) * Quaternion.Euler(arrowHeadAngle, 0, 0) * Vector3.back) * arrowHeadLength;
            Vector3 left = (Quaternion.LookRotation(arrowFwdVec) * Quaternion.Euler(-arrowHeadAngle, 0, 0) * Vector3.back) * arrowHeadLength;
            Vector3 up = (Quaternion.LookRotation(arrowFwdVec) * Quaternion.Euler(0, arrowHeadAngle, 0) * Vector3.back) * arrowHeadLength;
            Vector3 down = (Quaternion.LookRotation(arrowFwdVec) * Quaternion.Euler(0, -arrowHeadAngle, 0) * Vector3.back) * arrowHeadLength;

            Vector3 arrowTip = start + (arrowFwdVec);

            Gizmos.DrawLine(arrowTip, arrowTip + right * arrowHeadScale);
            Gizmos.DrawLine(arrowTip, arrowTip + left * arrowHeadScale);
            Gizmos.DrawLine(arrowTip, arrowTip + up * arrowHeadScale);
            Gizmos.DrawLine(arrowTip, arrowTip + down * arrowHeadScale);

            Gizmos.color = originColor;
        }

        public static void DrawWaypoint(Vector3 point, float pointRadius, Color color)
        {
            Gizmos.color = color;
            Gizmos.DrawSphere(point, pointRadius);
            Gizmos.color = color;
            Gizmos.DrawWireSphere(point, pointRadius);
        }

        public static void DrawCubeWaypoint(Vector3 point, Vector3 size, Color color)
        {
            Gizmos.color = color;
            Gizmos.DrawCube(point, size);
            Gizmos.color = color;
            Gizmos.DrawWireCube(point, size);
        }

        public static void DrawArrowHeads(Transform mainTrans, List<Vector3> localPoints, Color lineColor)
        {
            for (int i = 0; i < localPoints.Count - 1; i++)
            {
                var start = mainTrans.TransformPoint(localPoints[i]);
                var end = mainTrans.TransformPoint(localPoints[i + 1]);
                DrawArrowHead(start, end, lineColor, arrowHeadScale: MapAnnotationTool.ARROWSIZE * 1f, arrowPositionRatio: 0.5f); // TODO why reference map annotation tool?
            }
        }

        public static void DrawLines(Transform mainTrans, List<Vector3> localPoints, Color lineColor)
        {
            var pointCount = localPoints.Count;
            for (int i = 0; i < pointCount - 1; i++)
            {
                var start = mainTrans.TransformPoint(localPoints[i]);
                var end = mainTrans.TransformPoint(localPoints[i + 1]);
                Gizmos.color = lineColor;
                Gizmos.DrawLine(start, end);
            }
        }

        public static void DrawWaypoints(Transform mainTrans, List<Vector3> localPoints, float pointRadius, Color color)
        {
            var pointCount = localPoints.Count;
            for (int i = 0; i < pointCount - 1; i++)
            {
                var start = mainTrans.TransformPoint(localPoints[i]);
                DrawWaypoint(start, pointRadius, color);
            }

            var last = mainTrans.TransformPoint(localPoints[pointCount - 1]);
            DrawWaypoint(last, pointRadius, color);
        }
    }
}
