/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

namespace Simulator.Map
{
    public class MapParkingSpace : MapDataPoints, IMapType, ISpawnable
    {
        public string id { get; set; }

        public override void Draw()
        {
            if (mapLocalPositions.Count < 2) return;

            AnnotationGizmos.DrawWaypoints(transform, mapLocalPositions, MapAnnotationTool.WAYPOINT_SIZE, parkingSpaceColor + selectedColor);
            AnnotationGizmos.DrawWaypoint(MiddleEnter, 0.2f, parkingSpaceColor);
            AnnotationGizmos.DrawLines(transform, mapLocalPositions, parkingSpaceColor + selectedColor,true);
            if (MapAnnotationTool.SHOW_HELP)
            {
#if UNITY_EDITOR
                UnityEditor.Handles.Label(transform.position, "    PARKINGSPACE");
#endif
            }
        }

        public bool Spawnable { get; set; } = true;

        //helpers
        public Vector3 MiddleEnter
        {
            get
            {
                if (mapWorldPositions.Count < 2)
                {
                    RefreshWorldPositions();
                }

                return 0.5f * (mapWorldPositions[0] + mapWorldPositions[1]);
            }
        }

        public Vector3 MiddleExit
        {
            get
            {
                if (mapWorldPositions.Count < 4)
                {
                    RefreshWorldPositions();
                }
                return 0.5f * (mapWorldPositions[2] + mapWorldPositions[3]);
            }
        }

        public Vector3 Center => 0.5f * (MiddleEnter + MiddleExit);
        public float Length => (MiddleEnter - MiddleExit).magnitude;

        public float Width
        {
            get
            {
                var left = (mapWorldPositions[2] - mapWorldPositions[1]).normalized;
                var enterWidth = (mapWorldPositions[1] + left * Vector3.Dot(mapWorldPositions[0] - mapWorldPositions[1], left) - mapWorldPositions[0]).magnitude;
                var exitWidth = (mapWorldPositions[2] + left * Vector3.Dot(mapWorldPositions[3] - mapWorldPositions[2], left) - mapWorldPositions[3]).magnitude;
                return 0.5f * (enterWidth + exitWidth);
            }
        }
    }
}
