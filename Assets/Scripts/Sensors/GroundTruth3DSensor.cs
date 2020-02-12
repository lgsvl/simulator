/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Simulator.Bridge;
using Simulator.Bridge.Data;
using Simulator.Utilities;
using Simulator.Sensors.UI;
using Simulator.Map;

namespace Simulator.Sensors
{
    [SensorType("3D Ground Truth", new[] { typeof(Detected3DObjectData), typeof(TrafficLightData) })]
    public class GroundTruth3DSensor : SensorBase
    {
        [SensorParameter]
        [Range(1f, 100f)]
        public float Frequency = 10.0f;

        [SensorParameter]
        [Range(1f, 1000f)]
        public float MaxDistance = 100.0f;

        public RangeTrigger rangeTrigger;

        WireframeBoxes WireframeBoxes;

        private uint seqId;
        private uint objId;
        private float nextSend;

        private IBridge Bridge;
        private IWriter<Detected3DObjectData> obstacleWriter;

        private Dictionary<Collider, Detected3DObject> Detected = new Dictionary<Collider, Detected3DObject>();
        private Collider[] Visualized = Array.Empty<Collider>();

        public override bool CanBeDelegatedToClient => true;

        // Traffic Light
        private uint tlSeqId;
        private string trafficLightTopic = "/apollo/perception/traffic_light";
        private IWriter<TrafficLightData> trafficLightWriter;
        private MapLane closestLane;

        void Start()
        {
            WireframeBoxes = SimulatorManager.Instance.WireframeBoxes;
            rangeTrigger.SetCallbacks(WhileInRange);
            rangeTrigger.transform.localScale = MaxDistance * Vector3.one;
            nextSend = Time.time + 1.0f / Frequency;
        }

        void Update()
        {
            if (Bridge != null && Bridge.Status == Status.Connected)
            {
                if (Time.time < nextSend)
                {
                    return;
                }
                nextSend = Time.time + 1.0f / Frequency;

                obstacleWriter.Write(new Detected3DObjectData()
                {
                    Name = Name,
                    Frame = Frame,
                    Time = SimulatorManager.Instance.CurrentTime,
                    Sequence = seqId++,
                    Data = Detected.Values.ToArray(),
                });

                closestLane = SimulatorManager.Instance.MapManager.GetEgoCurrentLane(transform.parent.transform.position);

                if (closestLane != null)
                {
                    trafficLightWriter.Write(new TrafficLightData()
                    {
                        Time = SimulatorManager.Instance.CurrentTime,
                        Sequence = tlSeqId++,
                        blink = false,
                        confidence = 1.0f,
                        color = closestLane.stopLine?.currentState.ToString()
                });
                }
            }

            Visualized = Detected.Keys.ToArray();
            Detected.Clear();
        }

        public override void OnBridgeSetup(IBridge bridge)
        {
            Bridge = bridge;
            obstacleWriter = Bridge.AddWriter<Detected3DObjectData>(Topic);
            trafficLightWriter = Bridge.AddWriter<TrafficLightData>(trafficLightTopic);
        }

        void WhileInRange(Collider other)
        {
            GameObject egoGO = transform.parent.gameObject;
            GameObject parent = other.transform.parent.gameObject;
            if (parent == egoGO)
            {
                return;
            }

            if (!(other.gameObject.layer == LayerMask.NameToLayer("GroundTruth")) || !parent.activeInHierarchy)
            {
                return;
            }

            if (!Detected.ContainsKey(other))
            {
                uint id;
                string label;
                float linear_vel;
                float angular_vel;
                float egoPosY = egoGO.GetComponent<VehicleActions>().Bounds.center.y;
                if (parent.layer == LayerMask.NameToLayer("Agent"))
                {
                    var egoC = parent.GetComponent<VehicleController>();
                    var egoA = parent.GetComponent<VehicleActions>();
                    var rb = parent.GetComponent<Rigidbody>();
                    id = egoC.GTID;
                    label = "Sedan";
                    linear_vel = Vector3.Dot(rb.velocity, parent.transform.forward);
                    angular_vel = -rb.angularVelocity.y;
                }
                else if (parent.layer == LayerMask.NameToLayer("NPC"))
                {
                    var npcC = parent.GetComponent<NPCController>();
                    id = npcC.GTID;
                    label = npcC.NPCLabel;
                    linear_vel = Vector3.Dot(npcC.GetVelocity(), parent.transform.forward);
                    angular_vel = -npcC.GetAngularVelocity().y;
                }
                else if (parent.layer == LayerMask.NameToLayer("Pedestrian"))
                {
                    var pedC = parent.GetComponent<PedestrianController>();
                    id = pedC.GTID;
                    label = "Pedestrian";
                    linear_vel = Vector3.Dot(pedC.CurrentVelocity, parent.transform.forward);
                    angular_vel = -pedC.CurrentAngularVelocity.y;
                }
                else
                {
                    return;
                }

                Vector3 size = ((BoxCollider)other).size;
                // Convert from (Right/Up/Forward) to (Forward/Left/Up)
                size.Set(size.z, size.x, size.y);

                if (size.magnitude == 0)
                {
                    return;
                }

                // Local position of object in ego local space
                Vector3 relPos = transform.InverseTransformPoint(parent.transform.position);
                // Convert from (Right/Up/Forward) to (Forward/Left/Up)
                relPos.Set(relPos.z, -relPos.x, relPos.y);

                // Relative rotation of objects wrt ego frame
                var euler = parent.transform.rotation.eulerAngles - transform.parent.rotation.eulerAngles;
                // Convert from (Right/Up/Forward) to (Forward/Left/Up)
                euler.Set(-euler.z, euler.x, -euler.y);
                var relRot = Quaternion.Euler(euler);

                Detected.Add(other, new Detected3DObject()
                {
                    Id = id,
                    Label = label,
                    Score = 1.0f,
                    Position = relPos,
                    Rotation = relRot,
                    Scale = size,
                    LinearVelocity = new Vector3(linear_vel, 0, 0),  // Linear velocity in forward direction of objects, in meters/sec
                    AngularVelocity = new Vector3(0, 0, angular_vel),  // Angular velocity around up axis of objects, in radians/sec
                });
            }
        }

        public override void OnVisualize(Visualizer visualizer)
        {
            foreach (var other in Visualized)
            {
                if (other.gameObject.activeInHierarchy)
                {
                    GameObject parent = other.gameObject.transform.parent.gameObject;
                    Color color = Color.green;
                    if (parent.layer == LayerMask.NameToLayer("Pedestrian"))
                    {
                        color = Color.yellow;
                    }

                    BoxCollider box = other as BoxCollider;
                    WireframeBoxes.Draw(box.transform.localToWorldMatrix, new Vector3(0f, box.bounds.extents.y, 0f), box.size, color);
                }
            }
        }

        public override void OnVisualizeToggle(bool state)
        {
            //
        }

        public override bool CheckVisible(Bounds bounds)
        {
            return Vector3.Distance(transform.position, bounds.center) < MaxDistance;
        }
    }
}
