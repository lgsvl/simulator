/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Simulator.Controllable;
using Simulator.Utilities;

namespace Simulator.Map
{
    public class MapIntersection : MapData
    {
        [System.NonSerialized]
        public List<MapSignal> facingGroup = new List<MapSignal>();
        [System.NonSerialized]
        public List<MapSignal> oppFacingGroup = new List<MapSignal>();
        [System.NonSerialized]
        public List<MapSignal> currentSignalGroup = new List<MapSignal>();

        public Vector3 triggerBounds; // match to size of intersection so all stop sign queue goes in and out
        public BoxCollider yieldTrigger { get; set; }

        [System.NonSerialized]
        public List<Transform> npcsInIntersection = new List<Transform>();
        [System.NonSerialized]
        public List<NPCController> stopQueue = new List<NPCController>();

        [System.NonSerialized]
        List<MapSignal> signalGroup = new List<MapSignal>();
        private MonoBehaviour FixedUpdateManager;
        public bool isStopSignIntersection = false;

        public void SetIntersectionData()
        {
            var allMapLines = new List<MapLine>();
            var stopLines = new List<MapLine>();
            allMapLines.AddRange(transform.GetComponentsInChildren<MapLine>());
            foreach (var line in allMapLines)
            {
                if (line.lineType == LineType.STOP)
                {
                    stopLines.Add(line);
                    line.intersection = this;
                    if (line.isStopSign)
                    {
                        isStopSignIntersection = true;
                    }
                }
            }

            var intersectionLanes = new List<MapLane>();
            intersectionLanes.AddRange(transform.GetComponentsInChildren<MapLane>());
            foreach (var lane in intersectionLanes)
            {
                lane.laneCount = 1;
                lane.laneNumber = 1;
                lane.leftLaneForward = lane.rightLaneForward = lane.leftLaneReverse = lane.rightLaneReverse = null;

                lane.isStopSignIntersectionLane = isStopSignIntersection;
                lane.isIntersectionLane = true;
            }

            signalGroup.Clear();
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
                line.signals.Clear();
                foreach (var signal in signalGroup)
                {
                    float dot = Vector3.Dot(signal.transform.TransformDirection(Vector3.forward), line.transform.TransformDirection(Vector3.forward));
                    if (dot < -0.7f)
                    {
                        signal.stopLine = line;
                        line.signals.Add(signal);
                    }
                }
            }
        }

        public void SetTriggerAndState()
        {
            // trigger
            yieldTrigger = null;
            List<BoxCollider> oldTriggers = new List<BoxCollider>();
            oldTriggers.AddRange(GetComponents<BoxCollider>());
            for (int i = 0; i < oldTriggers.Count; i++)
                Destroy(oldTriggers[i]);

            yieldTrigger = this.gameObject.AddComponent<BoxCollider>();
            yieldTrigger.isTrigger = true;
            yieldTrigger.size = triggerBounds;

            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

            // set init signal state
            foreach (var signal in facingGroup)
            {
                var controlPolicy = "green=15;yellow=3;red=22;loop";
                signal.DefaultControlPolicy = controlPolicy;
                signal.SetSignalState("green");
                signal.ID = ++SimulatorManager.Instance.SignalIDs;
            }

            foreach (var signal in oppFacingGroup)
            {
                var controlPolicy = "red=20;green=15;yellow=3;red=2;loop";
                signal.DefaultControlPolicy = controlPolicy;
                signal.SetSignalState("red");
                signal.ID = ++SimulatorManager.Instance.SignalIDs;
            }
        }

        public void StartTrafficLightLoop()
        {
            foreach (var signal in signalGroup)
            {
                List<ControlAction> controlActions = signal.ParseControlPolicy(signal.DefaultControlPolicy, out string errorMsg);
                signal.CurrentControlPolicy = signal.DefaultControlPolicy;
                signal.Control(controlActions);
            }
        }

        public List<MapSignal> GetSignals()
        {
            return signalGroup;
        }

        public void EnterStopSignQueue(NPCController npcController)
        {
            stopQueue.Add(npcController);
        }

        public bool CheckStopSignQueue(NPCController npcController)
        {
            if (stopQueue.Count == 0 || npcController == stopQueue[0] && npcsInIntersection.Count == 0)
            {
                return true;
            }
            else
                return false;
        }

        public void ExitStopSignQueue(NPCController npcController)
        {
            if (stopQueue.Count == 0 || npcController == null) return;
            stopQueue.Remove(npcController);
        }

        public void ExitIntersectionList(NPCController npcController)
        {
            if (npcsInIntersection.Count == 0 || npcController == null) return;
            npcsInIntersection.Remove(npcController.transform);
        }

        private void RemoveFirstElement()
        {
            if (stopQueue.Count == 0) return;
            if (Vector3.Distance(stopQueue[0].transform.position, transform.position) > triggerBounds.x * 2f) // needs a distance
            {
                NPCController npcC = stopQueue[0];
                ExitStopSignQueue(npcC);
                npcC.currentIntersection = null;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer != LayerMask.NameToLayer("NPC")) // TODO include Agent
                return;

            NPCController npcController = other.GetComponentInParent<NPCController>();
            if (npcController == null) return;

            SimulatorManager.Instance?.MapManager?.RemoveNPCFromIntersections(npcController);
            npcsInIntersection.Add(npcController.transform);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.layer != LayerMask.NameToLayer("NPC"))
                return;

            NPCController npcController = other.GetComponentInParent<NPCController>();
            if (npcController == null) return;

            ExitIntersectionList(npcController);
            ExitStopSignQueue(npcController);
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
}