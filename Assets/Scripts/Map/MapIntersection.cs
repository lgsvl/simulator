/**
* Copyright (c) 2018 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapIntersection : MonoBehaviour
{
    public Vector3 boundScale = new Vector3(4f, 18f, 16f);

    private float length = 10f;
    
    //UI related
    private static Color gizmoSurfaceColor = Color.white * new Color(1f, 0.5f, 0.0f, 0.3f);
    private static Color gizmoLineColor = Color.white * new Color(1f, 0.5f, 0.0f, 0.45f);
    private static Color gizmoSurfaceColor_highlight = Color.white * new Color(1f, 0.5f, 0.0f, 0.8f);
    private static Color gizmoLineColor_highlight = Color.white * new Color(1f, 0.5f, 0.0f, 1f);

    protected virtual Color GizmoSurfaceColor { get { return gizmoSurfaceColor; } }
    protected virtual Color GizmoLineColor { get { return gizmoLineColor; } }
    protected virtual Color GizmoSurfaceColor_highlight { get { return gizmoSurfaceColor_highlight; } }
    protected virtual Color GizmoLineColor_highlight { get { return gizmoLineColor_highlight; } }

    private void Draw(bool highlight = false)
    {
        var start = transform.position;
        var end = start + transform.up * length;

        var surfaceColor = highlight ? GizmoSurfaceColor_highlight : GizmoSurfaceColor;
        var lineColor = highlight ? GizmoLineColor_highlight : GizmoLineColor;

        Map.Draw.Gizmos.DrawWaypoint(transform.position, Map.MapTool.PROXIMITY * 0.35f, surfaceColor, lineColor);
        Gizmos.color = lineColor;
        Gizmos.DrawLine(start, end);
        Map.Draw.Gizmos.DrawArrowHead(start, end, lineColor, arrowHeadScale: Map.Autoware.VectorMapTool.ARROWSIZE, arrowPositionRatio: 1f);
    }

    private void DrawContainBox()
    {
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.Scale(boundScale, transform.lossyScale));
        Gizmos.color = GizmoLineColor;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    }

    //private bool Contains(Vector3 pos)
    //{
    //    Vector3 posLcl = new Vector3(
    //        transform.InverseTransformPoint(pos).x,
    //        transform.InverseTransformPoint(pos).y,
    //        transform.InverseTransformPoint(pos).z);

    //    if (Mathf.Abs(posLcl.x) < 0.5f * boundScale.x &&
    //        Mathf.Abs(posLcl.y) < 0.5f * boundScale.y &&
    //        Mathf.Abs(posLcl.z) < 0.5f * boundScale.z)
    //    {
    //        return true;
    //    }

    //    return false;
    //}

    //public void LinkContainedSignalLights()
    //{
    //    var allIntersections = FindObjectsOfType<IntersectionComponent>();
    //    foreach (var sl in allIntersections)
    //    {
    //        if (!sl.gameObject.activeInHierarchy)   //skip inactive ones         
    //            continue;

    //        if (Contains(sl.transform.position) && !intersectionTrafficLights.Contains(sl))
    //            intersectionTrafficLights.Add(sl);
    //    }

    //    intersectionTrafficLights.RemoveAll(sl => sl == null);
    //}

    protected virtual void OnDrawGizmos()
    {
        if (!Map.MapTool.showMap) return;
        Draw();
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (!Map.MapTool.showMap) return;
        Draw(highlight: true);
        DrawContainBox();
    }
}
