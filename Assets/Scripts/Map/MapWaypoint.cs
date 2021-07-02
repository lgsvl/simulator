/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;

namespace Simulator.Map
{
    public class MapWaypoint : MapData
    {
        public LayerMask layerMask;
        public bool snapping = true;

        private Vector3 lastMovePos;

        private void Update()
        {
            if (snapping)
            {
                Ray ray = new Ray(transform.position + Vector3.up * MapAnnotationTool.PROXIMITY * 2, Vector3.down);
                RaycastHit hit = new RaycastHit();
                while (Physics.Raycast(ray, out hit, 1000.0f, layerMask.value))
                {
                    if (hit.collider.transform == transform)
                    {
                        ray = new Ray(hit.point - Vector3.up * MapAnnotationTool.PROXIMITY * 0.001f, Vector3.down);
                        continue;
                    }

                    if ((hit.point - lastMovePos).magnitude > 0.001f) //prevent self drifting
                    {
                        transform.position = hit.point;
                        lastMovePos = hit.point;
                    }

                    break;
                }
            }
        }

        public override void Draw()
        {
            AnnotationGizmos.DrawWaypoints(transform, new List<Vector3>() { Vector3.zero }, MapAnnotationTool.WAYPOINT_SIZE, tempWaypointColor + selectedColor);
            if (MapAnnotationTool.SHOW_HELP)
            {
#if UNITY_EDITOR
                UnityEditor.Handles.Label(transform.position, "    TEMP WAYPOINT");
#endif
            }
        }

        protected override void OnDrawGizmos()
        {
            Draw();
        }
    }
}