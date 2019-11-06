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
    public class MapSign : MapData, IMapType
    {
        public SignType signType;
        public MapLine stopLine;

        public Vector3 boundOffsets = new Vector3(); // TODO
        public Vector3 boundScale = new Vector3();
        [System.NonSerialized]
        public Renderer signMesh;
        public string id
        {
            get;
            set;
        }

        public override void Draw()
        {
            var start = transform.position;
            var end = start + transform.up * 2f;

            AnnotationGizmos.DrawWaypoint(transform.position, MapAnnotationTool.PROXIMITY * 0.35f, stopSignColor + selectedColor);
            Gizmos.color = stopSignColor + selectedColor;
            Gizmos.DrawLine(start, end);
            AnnotationGizmos.DrawArrowHead(start, end, stopSignColor + selectedColor, arrowHeadScale: MapAnnotationTool.ARROWSIZE, arrowPositionRatio: 1f);
            if (MapAnnotationTool.SHOW_HELP)
            {
#if UNITY_EDITOR
                UnityEditor.Handles.Label(transform.position, "    " + signType + " SIGN");
#endif
            }

            if (stopLine != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, stopLine.transform.position);
                AnnotationGizmos.DrawArrowHead(transform.position, stopLine.transform.position, Color.magenta, arrowHeadScale: MapAnnotationTool.ARROWSIZE, arrowPositionRatio: 1f);
                if (MapAnnotationTool.SHOW_HELP)
                {
#if UNITY_EDITOR
                    UnityEditor.Handles.Label(stopLine.transform.position, "    STOPLINE");
#endif
                }
            }

            // bounds need offset
            Gizmos.matrix = transform.localToWorldMatrix * Matrix4x4.TRS(boundOffsets, Quaternion.identity, Vector3.Scale(Vector3.one, boundScale));
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            if (MapAnnotationTool.SHOW_HELP)
            {
#if UNITY_EDITOR
                UnityEditor.Handles.Label(transform.position + Vector3.up, "    SIGNAL BOUNDS");
#endif
            }
        }
    }
}