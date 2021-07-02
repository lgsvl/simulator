/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Map
{
    public class MapClearArea : MapDataPoints, IMapType
    {
        public string id
        {
            get;
            set;
        }
        public override void Draw()
        {
            if (mapLocalPositions.Count < 3) return;

            AnnotationGizmos.DrawWaypoints(transform, mapLocalPositions, MapAnnotationTool.WAYPOINT_SIZE, clearAreaColor + selectedColor);
            AnnotationGizmos.DrawLines(transform, mapLocalPositions, clearAreaColor + selectedColor);
            if (MapAnnotationTool.SHOW_HELP)
            {
#if UNITY_EDITOR
                UnityEditor.Handles.Label(transform.position, "    CLEARAREA");
#endif
            }
        }
    }
}

