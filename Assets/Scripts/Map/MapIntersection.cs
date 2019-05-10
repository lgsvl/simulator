/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapIntersection : MapData
{
    private bool isFacing = false;
    [System.NonSerialized]
    public List<MapSignal> facingGroup = new List<MapSignal>();
    [System.NonSerialized]
    public List<MapSignal> oppFacingGroup = new List<MapSignal>();
    [System.NonSerialized]
    public List<MapSignal> currentSignalGroup = new List<MapSignal>();

    public Vector3 triggerBounds; // match to size of intersection so all stop sign queue goes in and out
    public BoxCollider yieldTrigger { get; set; }
    public float yieldTriggerRadius = 10f; 

    [System.NonSerialized]
    public List<Transform> npcsInIntersection = new List<Transform>();
    [System.NonSerialized]
    public List<NPCControllerComponent> stopQueue = new List<NPCControllerComponent>();
    
    public void SetIntersectionData()
    {
        var intersectionLanes = new List<MapLane>();
        intersectionLanes.AddRange(transform.GetComponentsInChildren<MapLane>());
        foreach (var lane in intersectionLanes)
        {
            lane.laneCount = 1;
            lane.laneNumber = 1;
            lane.leftLaneForward = lane.rightLaneForward = lane.leftLaneReverse = lane.rightLaneReverse = null;
        }

        var allMapLines = new List<MapLine>();
        var stopLines = new List<MapLine>();
        allMapLines.AddRange(transform.GetComponentsInChildren<MapLine>());
        foreach (var line in allMapLines)
        {
            if (line.lineType == LineType.STOP)
            {
                stopLines.Add(line);
                line.intersection = this;
            }
        }

        var signalGroup = new List<MapSignal>();
        signalGroup.AddRange(transform.GetComponentsInChildren<MapSignal>());

        foreach (var item in signalGroup)
        {
            foreach (var group in signalGroup)
            {
                float dot = Vector3.Dot(group.transform.TransformDirection(Vector3.forward), item.transform.TransformDirection(Vector3.forward));
                if (dot < -0.7f || dot > 0.7f) // facing
                {
                    if (!facingGroup.Contains(item))
                    {
                        if (facingGroup.Count == 0)
                            facingGroup.Add(item);
                        else
                        {
                            float groupDot = Vector3.Dot(facingGroup[0].transform.TransformDirection(Vector3.forward), item.transform.TransformDirection(Vector3.forward));
                            if (groupDot > 0.7f || groupDot < -0.7f)
                                facingGroup.Add(item);
                        }
                    }
                }
                else
                {
                    if (!facingGroup.Contains(group) && !oppFacingGroup.Contains(group))
                    {
                        if (oppFacingGroup.Count == 0)
                            oppFacingGroup.Add(group);
                        else
                        {
                            float groupDot = Vector3.Dot(oppFacingGroup[0].transform.TransformDirection(Vector3.forward), group.transform.TransformDirection(Vector3.forward));
                            if (groupDot > 0.7f || groupDot < -0.7f)
                                oppFacingGroup.Add(group);
                        }
                    }
                }

            }
        }
        
        foreach (var group in signalGroup)
            group.SetSignalMeshData();
        
        foreach (var line in stopLines)
        {
            foreach (var signal in signalGroup)
            {
                float dot = Vector3.Dot(signal.transform.TransformDirection(Vector3.forward), line.transform.TransformDirection(Vector3.forward));
                if (dot < -0.7f)
                {
                    signal.stopLine = line;
                    line.signal = signal;
                }
            }
        }

        // trigger
        yieldTrigger = null;
        List<BoxCollider> oldTriggers = new List<BoxCollider>();
        oldTriggers.AddRange(GetComponents<BoxCollider>());
        for (int i = 0; i < oldTriggers.Count; i++)
            Destroy(oldTriggers[i]);

        yieldTrigger = this.gameObject.AddComponent<BoxCollider>();
        yieldTrigger.isTrigger = true;
        yieldTrigger.size = triggerBounds;

        isFacing = false;

        // set init signal state
        foreach (var signal in signalGroup)
        {
            signal.SetSignalState(SignalLightStateType.Red);
            signal.currentState = SignalLightStateType.Red;
        }
    }

    public void StartTrafficLightLoop()
    {
        StartCoroutine(TrafficLightLoop());
    }

    private IEnumerator TrafficLightLoop()
    {
        yield return new WaitForSeconds(Random.Range(0, 5f));
        while (true)
        {
            yield return null;

            currentSignalGroup = isFacing ? facingGroup : oppFacingGroup;

            foreach (var signal in currentSignalGroup)
            {
                signal.SetSignalState(SignalLightStateType.Green);
            }

            yield return new WaitForSeconds(SimulatorManager.Instance.mapManager.activeTime);

            foreach (var signal in currentSignalGroup)
            {
                signal.SetSignalState(SignalLightStateType.Yellow);
            }

            yield return new WaitForSeconds(SimulatorManager.Instance.mapManager.yellowTime);

            foreach (var signal in currentSignalGroup)
            {
                signal.SetSignalState(SignalLightStateType.Red);
            }

            yield return new WaitForSeconds(SimulatorManager.Instance.mapManager.allRedTime);

            isFacing = !isFacing;
        }
    }

    public void EnterStopSignQueue(NPCControllerComponent npcController)
    {
        stopQueue.Add(npcController);
    }

    public bool CheckStopSignQueue(NPCControllerComponent npcController)
    {
        if (stopQueue.Count == 0 || npcController == stopQueue[0])
            return true;
        else
            return false;
    }

    public void ExitStopSignQueue(NPCControllerComponent npcController)
    {
        if (stopQueue.Count == 0) return;
        stopQueue.Remove(npcController);
    }

    private void RemoveFirstElement()
    {
        if (stopQueue.Count == 0) return;
        if (Vector3.Distance(stopQueue[0].transform.position, transform.position) > triggerBounds.x * 2f) // needs a distance
        {
            NPCControllerComponent npcC = stopQueue[0].GetComponent<NPCControllerComponent>();
            if (npcC != null)
            {
                ExitStopSignQueue(npcC);
                npcC.currentIntersection = null;
            }
        }
    }

    private void Update()
    {
        for (int i = 0; i < npcsInIntersection.Count; i++)
        {
            if (Vector3.Distance(npcsInIntersection[i].position, transform.position) > triggerBounds.x * 2f)
            {
                if (npcsInIntersection.Contains(npcsInIntersection[i]))
                    npcsInIntersection.Remove(npcsInIntersection[i]);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        npcsInIntersection.Add(other.transform);
        NPCControllerComponent npcControllerComponent = other.GetComponent<NPCControllerComponent>();
        if (npcControllerComponent != null && npcControllerComponent.currentIntersection == null)
            npcControllerComponent.currentIntersection = this;
    }

    private void OnTriggerExit(Collider other)
    {
        npcsInIntersection.Remove(other.transform);
        NPCControllerComponent npcControllerComponent = other.GetComponent<NPCControllerComponent>();
        if (npcControllerComponent != null)
        {
            npcControllerComponent.RemoveFromStopSignQueue();
            npcControllerComponent.currentIntersection = null;
        }
    }

    public override void Draw()
    {
        var start = transform.position;
        var end = start + transform.up * 6f;

        AnnotationGizmos.DrawWaypoint(transform.position, MapAnnotationTool.PROXIMITY * 0.5f, intersectionColor + selectedColor);
        Gizmos.color = intersectionColor + selectedColor;
        Gizmos.DrawLine(start, end);
        AnnotationGizmos.DrawArrowHead(start, end, intersectionColor + selectedColor, arrowHeadScale: MapAnnotationTool.ARROWSIZE, arrowPositionRatio: 1f);

        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.Scale(triggerBounds, transform.lossyScale));
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

        if (MapAnnotationTool.SHOW_HELP)
        {
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position, "    INTERSECTION");
#endif
        }
    }
}
