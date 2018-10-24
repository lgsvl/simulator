/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapSignalLightBuilder : MonoBehaviour
{    
    //UI related
    private static Color gizmoSurfaceColor = Color.white * new Color(1.0f, 1.0f, 1.0f, 0.1f);
    private static Color gizmoLineColor = Color.white * new Color(1.0f, 1.0f, 1.0f, 0.15f);
    private static Color gizmoSurfaceColor_highlight = Color.white * new Color(1.0f, 1.0f, 1.0f, 0.8f);
    private static Color gizmoLineColor_highlight = Color.white * new Color(1.0f, 1.0f, 1.0f, 1f);

    protected virtual Color GizmoSurfaceColor { get { return gizmoSurfaceColor; } }
    protected virtual Color GizmoLineColor { get { return gizmoLineColor; } }
    protected virtual Color GizmoSurfaceColor_highlight { get { return gizmoSurfaceColor_highlight; } }
    protected virtual Color GizmoLineColor_highlight { get { return gizmoLineColor_highlight; } }

    [System.Serializable]
    public class Data
    {
        public enum Type
        {
            Red = 1,
            Yellow = 2,
            Green = 3,
        }

        public Vector3 localPosition;

        public Type type = Type.Yellow;
    }

    public List<Data> signalDatas;

    public MapStopLineSegmentBuilder hintStopline;

    public static Color GetTypeColor(VectorMapSignalLight.Data data)
    {
        switch (data.type)
        {
            case Data.Type.Red:
                return Color.red;
            case Data.Type.Yellow:
                return Color.yellow;
            case Data.Type.Green:
                return Color.green;
        }

        return Color.black;
    }

    private void Draw(bool highlight = false)
    {
        var lightLocalPositions = signalDatas.Select(x => x.localPosition).ToList();

        var lightCount = lightLocalPositions.Count;

        if (lightCount < 1)
        {
            return;
        }

        for (int i = 0; i < lightCount; i++)
        {
            var start = transform.TransformPoint(lightLocalPositions[i]);
            var end = start + transform.forward * 2f;

            var surfaceColor = highlight ? GizmoSurfaceColor_highlight : GizmoSurfaceColor;
            var lineColor = highlight ? GizmoLineColor_highlight : GizmoLineColor;

            var color = VectorMapSignalLight.GetTypeColor(signalDatas[i]);
            surfaceColor *= color;
            lineColor *= color;

            Map.Draw.Gizmos.DrawWaypoint(start, Map.Autoware.VectorMapTool.PROXIMITY * 0.225f, surfaceColor, lineColor);
            Gizmos.color = lineColor;
            Gizmos.DrawLine(start, end);
            Map.Draw.Gizmos.DrawArrowHead(start, end, lineColor, arrowHeadScale: Map.Autoware.VectorMapTool.ARROWSIZE, arrowPositionRatio: 1f);
        }
    }

    protected virtual void OnDrawGizmos()
    {
        Draw();
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Draw(highlight: true);
    }
}
