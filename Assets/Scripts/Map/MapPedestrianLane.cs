/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;

namespace Simulator.Map
{
    public class MapPedestrianLane : MapLane
    {
        public MapAnnotationTool.PedestrianPathType type = MapAnnotationTool.PedestrianPathType.SIDEWALK;
        public int PedVolume { get; set; } = 1;

        [System.NonSerialized]
        public List<MapSignal> Signals = new List<MapSignal>();

        public override void Draw()
        {
            if (mapLocalPositions.Count < 2) return;

            AnnotationGizmos.DrawWaypoints(transform, mapLocalPositions, MapAnnotationTool.WAYPOINT_SIZE, pedestrianColor + selectedColor);
            AnnotationGizmos.DrawLines(transform, mapLocalPositions, pedestrianColor + selectedColor);
            AnnotationGizmos.DrawArrowHeads(transform, mapLocalPositions, pedestrianColor + selectedColor);
            if (MapAnnotationTool.SHOW_HELP)
            {
#if UNITY_EDITOR
                UnityEditor.Handles.Label(transform.position, "    PEDESTRIAN " + type);
#endif
            }
        }
    }
}
