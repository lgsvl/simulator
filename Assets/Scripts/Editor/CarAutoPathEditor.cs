/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


[CustomEditor(typeof(CarAutoPath))]
public class CarAutoPathEditor : Editor {

    bool vis = false;
    int min = 0;

    int max = 2;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();


        min = EditorGUILayout.IntField(min);
        max = EditorGUILayout.IntField(max);

        if (GUILayout.Button("vis"))
        {
            vis = !vis;
            SceneView.RepaintAll();
        }
        
    }

    public void OnSceneGUI()
    {
       if(!vis)
           return;

  
        var path = target as CarAutoPath;
        List<Vector3> vecs = new List<Vector3>();
        int i = 0;
        foreach(var p in path.pathNodes) {
            Vector3 currentSpot = p.position;// HermiteMath.HermiteVal(prevWaypoint, currentWaypoint, prevTangent, currentTangent, currentPc);
            RaycastHit hit;
            Physics.Raycast(currentSpot + Vector3.up * 5, -Vector3.up, out hit, 100f, ~(1 << LayerMask.NameToLayer("NPC")));

            Vector3 hTarget = new Vector3(currentSpot.x, hit.point.y + 0.2f, currentSpot.z);

            vecs.Add(hTarget);
            Handles.Label(hTarget, "" + i++);

        }


        Handles.color = Color.green;
        Handles.DrawPolyLine(vecs.ToArray());     
    }

}
