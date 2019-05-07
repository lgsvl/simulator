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
    
    public SphereCollider yieldTrigger { get; set; }
    public float yieldTriggerRadius = 10f; // match to size of intersection so all stop sign queue goes in and out TODO choose box or sphere?

    [System.NonSerialized]
    public List<Transform> npcsInIntersection = new List<Transform>();

    public bool isStopSign { get; set; }
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

                if (dot < -0.7f) // facing
                {
                    if (!facingGroup.Contains(item) && !oppFacingGroup.Contains(item))
                        facingGroup.Add(item);
                    if (!facingGroup.Contains(group) && !oppFacingGroup.Contains(group))
                        facingGroup.Add(group);
                }
                else if (dot > -0.5f && dot < 0.5f) // perpendicular
                {
                    if (!facingGroup.Contains(item) && !oppFacingGroup.Contains(item))
                        facingGroup.Add(item);
                    if (!oppFacingGroup.Contains(group) && !facingGroup.Contains(group))
                        oppFacingGroup.Add(group);
                }
                else if (signalGroup.Count == 1) // same direction
                {
                    if (!facingGroup.Contains(item))
                        facingGroup.Add(item);
                }
            }
        }
        if (signalGroup.Count != facingGroup.Count + oppFacingGroup.Count)
            Debug.LogError("Error finding facing light sets, please check light annotation");

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
        List<SphereCollider> oldTriggers = new List<SphereCollider>();
        oldTriggers.AddRange(GetComponents<SphereCollider>());
        for (int i = 0; i < oldTriggers.Count; i++)
            Destroy(oldTriggers[i]);

        yieldTrigger = this.gameObject.AddComponent<SphereCollider>();
        yieldTrigger.isTrigger = true;
        yieldTrigger.radius = yieldTriggerRadius;

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
        if (Vector3.Distance(stopQueue[0].transform.position, transform.position) > yieldTrigger.radius * 2f) // needs a distance
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
            if (Vector3.Distance(npcsInIntersection[i].position, transform.position) > yieldTrigger.radius * 2f)
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
        //if (npcControllerComponent != null && npcControllerComponent.currentIntersectionComponent == null)
        //    npcControllerComponent.currentIntersectionComponent = this;
    }

    private void OnTriggerExit(Collider other)
    {
        npcsInIntersection.Remove(other.transform);
        NPCControllerComponent npcControllerComponent = other.GetComponent<NPCControllerComponent>();
        if (npcControllerComponent != null)
        {
            //npcControllerComponent.RemoveFromStopSignQueue();
            //npcControllerComponent.currentIntersectionComponent = null;
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
        if (MapAnnotationTool.SHOW_HELP)
            UnityEditor.Handles.Label(transform.position, "    INTERSECTION");
    }
}
