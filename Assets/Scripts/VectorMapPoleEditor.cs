/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VectorMapPole)), CanEditMultipleObjects]
public class VectorMapPoleEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }

    protected void OnSceneGUI()
    {
        VectorMapPole pole = (VectorMapPole)target;
        Vector3 pos = pole.transform.position;
        Vector3 dir = pole.transform.forward;
        float length = pole.length;

        VectorMap.Draw.DrawArrowForDebug(pos, pos + dir * length, Color.white, VectorMapTool.ARROWSIZE);
    }
}
