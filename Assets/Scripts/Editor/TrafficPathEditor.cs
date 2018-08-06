/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(TrafficPath), true)]
public class TrafficPathEditor : Editor {

    Tool LastTool = Tool.None;

    void OnEnable()
    {
        LastTool = Tools.current;
        Tools.current = Tool.None;
    }

    void OnDisable()
    {
        Tools.current = LastTool;
    }

    public override void OnInspectorGUI() {

        var t = target as TrafficPath;

		DrawDefaultInspector ();

        GUILayout.BeginHorizontal();

        if(GUILayout.Button("New WP at start"))
        {
            AddWP(0);
        }

        if(GUILayout.Button("New WP at end"))
        {
            AddWP(t.waypoints.Count);
        }

        if(GUILayout.Button("reset"))
        {
            foreach(var wp in t.waypoints)
            {
                wp.position = wp.position - t.transform.position;
            }
        }

        GUILayout.EndHorizontal();

	}

    void AddWP(int index)
    {
        var t = target as TrafficPath;
        if(t.waypoints == null)
            t.waypoints = new List<WayPoint>();

        var newWp = new WayPoint();
        newWp.position = t.transform.position;

        if( index != 0)
        {
            newWp.position = t.waypoints[index - 1].position;
        }
        else
        {
            newWp.position = Vector3.zero;
        }

        t.waypoints.Insert(index, newWp);

    }

    void OnSceneGUI()
    {
        DrawPath(target as TrafficPath, true);
    }

    public static void DrawPath(TrafficPath t, bool allowEdit)
    {
        if(t.waypoints != null && t.waypoints.Count > 0)
        {
            var wps = t.waypoints.Where(w => w != null).ToList();

            for(int wp = 0; wp < wps.Count; wp++)
            {
                if(allowEdit)
                    wps[wp].position = t.transform.InverseTransformPoint(Handles.PositionHandle(t.transform.TransformPoint(wps[wp].position), Quaternion.identity));
                Handles.color = Color.red;
                Handles.SphereHandleCap(HandleUtility.nearestControl, t.transform.TransformPoint(wps[wp].position), Quaternion.identity, 2f, EventType.Repaint);
            }
            Handles.DrawPolyLine(wps.Select(w => t.transform.TransformPoint(w.position)).ToArray());
        }
    }
}