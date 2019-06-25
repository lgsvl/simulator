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
    public class MapClearArea : MapData
    {
        public bool displayHandles = false;
        public List<Vector3> mapLocalPositions = new List<Vector3>();
        [System.NonSerialized]
        public List<Vector3> mapWorldPositions = new List<Vector3>();

        public override void Draw()
        {
            if (mapLocalPositions.Count < 3) return;

            AnnotationGizmos.DrawWaypoints(transform, mapLocalPositions, MapAnnotationTool.PROXIMITY * 0.5f, clearAreaColor);
            AnnotationGizmos.DrawLines(transform, mapLocalPositions, clearAreaColor);
            if (MapAnnotationTool.SHOW_HELP)
            {
#if UNITY_EDITOR
                UnityEditor.Handles.Label(transform.position, "    CLEARAREA");
#endif
            }
        }
    }
}

