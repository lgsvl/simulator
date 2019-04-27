/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MapSignal : MapData
{
    public Vector3 boundScale = new Vector3(0.61f, 1.5f, 0f);
    public List<SignalData> signalData = new List<SignalData>()
    {
        new SignalData() { localPosition = new Vector3(0f, 0.42f, 0f), signalColor = SignalColorType.Red },
        new SignalData() { localPosition = new Vector3(0f, 0f, 0f), signalColor = SignalColorType.Yellow },
        new SignalData() { localPosition = new Vector3(0f, -0.42f, 0f), signalColor = SignalColorType.Green }
    };
    public MapLine stopLine;

    public Renderer signalLightMesh;

    public void SetSignalMeshData()
    {
        List<GameObject> allSignalMeshes = new List<GameObject>();
        allSignalMeshes.AddRange(GameObject.FindGameObjectsWithTag("SignalMesh"));

        foreach (var mesh in allSignalMeshes)
        {
            if (Vector3.Distance(transform.position, mesh.transform.position) < 1f)
                signalLightMesh = mesh.GetComponent<Renderer>();
        }
    }

    public Vector3 GetSignalDirection()
    {
        var lightLocalPositions = signalData.Select(x => x.localPosition).ToList();
        return transform.TransformPoint(lightLocalPositions[0] + transform.forward);
    }

    private Color GetTypeColor(SignalData data)
    {
        Color currentColor = Color.black;
        switch (data.signalColor)
        {
            case SignalColorType.Red:
                currentColor = Color.red;
                break;
            case SignalColorType.Yellow:
                currentColor = Color.yellow;
                break;
            case SignalColorType.Green:
                currentColor = Color.green;
                break;
            default:
                break;
        }
        return currentColor;
    }

    public override void Draw()
    {
        if (signalData == null || signalData.Count < 1) return;
        
        var lightLocalPositions = signalData.Select(x => x.localPosition).ToList();
        var lightCount = lightLocalPositions.Count;

        // lights
        if (MapAnnotationTool.SHOW_HELP)
            UnityEditor.Handles.Label(transform.position, "    SIGNAL");
        for (int i = 0; i < lightCount; i++)
        {
            var start = transform.TransformPoint(lightLocalPositions[i]);
            var end = start + transform.forward * 2f * (1 / MapAnnotationTool.EXPORT_SCALE_FACTOR); // TODO why is this 1/export scale?
            
            var signalColor = GetTypeColor(signalData[i]);

            AnnotationGizmos.DrawWaypoint(start, MapAnnotationTool.PROXIMITY * 0.15f, signalColor);
            Gizmos.color = signalColor;
            Gizmos.DrawLine(start, end);
            AnnotationGizmos.DrawArrowHead(start, end, signalColor, arrowHeadScale: MapAnnotationTool.ARROWSIZE, arrowPositionRatio: 1f);
        }

        // stopline
        if (stopLine != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, stopLine.transform.position);
            AnnotationGizmos.DrawArrowHead(transform.position, stopLine.transform.position, Color.magenta, arrowHeadScale: MapAnnotationTool.ARROWSIZE, arrowPositionRatio: 1f);
            if (MapAnnotationTool.SHOW_HELP)
                UnityEditor.Handles.Label(stopLine.transform.position, "    STOPLINE");
        }

        // bounds
        Gizmos.matrix = transform.localToWorldMatrix * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.Scale(Vector3.one, boundScale));
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        if (MapAnnotationTool.SHOW_HELP)
            UnityEditor.Handles.Label(transform.position + Vector3.up, "    SIGNAL BOUNDS");
    }
}
