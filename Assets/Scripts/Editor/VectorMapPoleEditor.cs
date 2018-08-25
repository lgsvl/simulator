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

        ////temp fix function
        //if (GUILayout.Button("Fix")) 
        //{
        //    var poles = FindObjectsOfType<VectorMapPole>();
        //    foreach (var pole in poles)
        //    {
        //        var polePar = pole.transform.parent;
        //        var lights = polePar.GetComponentsInChildren<VectorMapSignalLight>(true);
        //        foreach (var light in lights)
        //        {
        //            if (!pole.signalLights.Contains(light))
        //            {
        //                pole.signalLights.Add(light);
        //            }
        //        }
        //    }
        //}
    }

    protected void OnSceneGUI()
    {
        VectorMapPole pole = (VectorMapPole)target;
        Vector3 pos = pole.transform.position;
        Vector3 dir = pole.transform.forward;
        float length = pole.length;

        Map.Draw.DrawArrowForDebug(pos, pos + dir * length, Color.white, Map.Autoware.VectorMapTool.ARROWSIZE);
    }
}
