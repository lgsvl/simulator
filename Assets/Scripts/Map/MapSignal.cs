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
    public Vector3 boundScale; // apollo signal bounds
    public List<SignalData> signalData = new List<SignalData>();
    public MapLine stopLine;
    public Renderer signalLightMesh;
    public SignalLightStateType currentState = SignalLightStateType.Yellow;

    public void SetSignalMeshData()
    {
        var signalMeshes = new List<GameObject>();
        signalMeshes.AddRange(GameObject.FindGameObjectsWithTag("SignalMesh"));
        foreach (var mesh in signalMeshes)
        {
            if (Vector3.Distance(transform.position, mesh.transform.position) < 1f)
            {
                signalLightMesh = mesh.GetComponent<Renderer>();
                break;
            }
        }
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

    public void SetSignalState(SignalLightStateType state)
    {
        stopLine.currentState = state;
        currentState = state;
        switch (state)
        {
            case SignalLightStateType.Red:
                signalLightMesh.material.SetColor("_EmissiveColor", Color.red);
                break;
            case SignalLightStateType.Green:
                signalLightMesh.material.SetColor("_EmissiveColor", Color.green);
                break;
            case SignalLightStateType.Yellow:
                signalLightMesh.material.SetColor("_EmissiveColor", Color.yellow);
                break;
            default:
                break;
        }
    }

    public override void Draw()
    {
        if (signalData == null || signalData.Count < 1) return;
        
        var lightLocalPositions = signalData.Select(x => x.localPosition).ToList();
        var lightCount = lightLocalPositions.Count;

        // lights
        if (MapAnnotationTool.SHOW_HELP)
        {
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position, "    SIGNAL");
#endif
        }
        for (int i = 0; i < lightCount; i++)
        {
            var start = transform.TransformPoint(lightLocalPositions[i]);
            var end = start + transform.forward * 2f * (1 / MapAnnotationTool.EXPORT_SCALE_FACTOR); // TODO why is this 1/export scale?
            
            var signalColor = GetTypeColor(signalData[i]) + selectedColor;

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
            {
#if UNITY_EDITOR
                UnityEditor.Handles.Label(stopLine.transform.position, "    STOPLINE");
#endif
            }
        }

        // bounds
        Gizmos.matrix = transform.localToWorldMatrix * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.Scale(Vector3.one, boundScale));
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
