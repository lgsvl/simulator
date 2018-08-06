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

[CustomEditor(typeof(TrafficRoad), true)]
public class TrafficRoadEditor : Editor
{
    void OnSceneGUI()
    {
        var t = target as TrafficRoad;
        
        if(t.left != null)
            TrafficPathEditor.DrawPath(t.left, false);

        if(t.right != null)
            TrafficPathEditor.DrawPath(t.right, false);
    }
}
