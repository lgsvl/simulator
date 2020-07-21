/**
 * Copyright (c) 2019-2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Simulator.Bridge;
using Simulator.Bridge.Data;
using Simulator.Map;
using Simulator.Utilities;
using Simulator.Sensors.UI;

namespace Simulator.Sensors
{
    [SensorType("3D Ground Truth", new[] { typeof(Detected3DObjectData) })]
    public class GroundTruth3DSensor : SensorBase
    {
        [SensorParameter]
        [Range(1f, 100f)]
        public float Frequency = 10.0f;

        [SensorParameter]
        [Range(1f, 1000f)]
        public float MaxDistance = 100.0f;

        public RangeTrigger RangeTrigger;
        WireframeBoxes WireframeBoxes;

        private BridgeInstance Bridge;
        private Publisher<Detected3DObjectData> Publish;

        private Dictionary<uint, Tuple<Detected3DObject, Collider>> Detected;
        private HashSet<uint> CurrentIDs;

        public override SensorDistributionType DistributionType => SensorDistributionType.HighLoad;
        MapOrigin MapOrigin;

        public override void OnBridgeSetup(BridgeInstance bridge)
        {
            Bridge = bridge;
            Publish = Bridge.AddPublisher<Detected3DObjectData>(Topic);
        }

        void Start()
        {
            WireframeBoxes = SimulatorManager.Instance.WireframeBoxes;

            if (RangeTrigger == null)
            {
                RangeTrigger = GetComponentInChildren<RangeTrigger>();
            }

            RangeTrigger.SetCallbacks(WhileInRange);
            RangeTrigger.transform.localScale = MaxDistance * Vector3.one;

            MapOrigin = MapOrigin.Find();

            Detected = new Dictionary<uint, Tuple<Detected3DObject, Collider>>();
            CurrentIDs = new HashSet<uint>();

            StartCoroutine(OnPublish());
        }

        private void FixedUpdate()
        {
            CurrentIDs.Clear();
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

            uint id;
            string label;
            Vector3 velocity;
            float angular_speed;  // Angular speed around up axis of objects, in radians/sec
            if (parent.layer == LayerMask.NameToLayer("Agent"))
            {
                var egoC = parent.GetComponent<VehicleController>();
                var rb = parent.GetComponent<Rigidbody>();
                id = egoC.GTID;
                label = "Sedan";
                velocity = rb.velocity;
                angular_speed = rb.angularVelocity.y;
            }
            else if (parent.layer == LayerMask.NameToLayer("NPC"))
            {
                var npcC = parent.GetComponent<NPCController>();
                id = npcC.GTID;
                label = npcC.NPCLabel;
                velocity = npcC.GetVelocity();
                angular_speed = npcC.GetAngularVelocity().y;
            }
            else if (parent.layer == LayerMask.NameToLayer("Pedestrian"))
            {
                var pedC = parent.GetComponent<PedestrianController>();
                id = pedC.GTID;
                label = "Pedestrian";
                velocity = pedC.CurrentVelocity;
                angular_speed = pedC.CurrentAngularVelocity.y;
            }
            else
            {
                return;
            }

            Vector3 size = ((BoxCollider)other).size;
            if (size.magnitude == 0)
            {
                return;
            }

            // Linear speed in forward direction of objects, in meters/sec
            float speed = Vector3.Dot(velocity, parent.transform.forward);
            // Local position of object in ego local space
            Vector3 relPos = transform.InverseTransformPoint(parent.transform.position);
            // Relative rotation of objects wrt ego frame
            Quaternion relRot = Quaternion.Inverse(transform.rotation) * parent.transform.rotation;

            var mapRotation = MapOrigin.transform.localRotation;
            velocity = Quaternion.Inverse(mapRotation) * velocity;
            var heading = parent.transform.localEulerAngles.y - mapRotation.eulerAngles.y;

            // Center of bounding box
            GpsLocation location = MapOrigin.GetGpsLocation(((BoxCollider)other).bounds.center);
            GpsData gps = new GpsData()
            {
                Easting = location.Easting,
                Northing = location.Northing,
                Altitude = location.Altitude,
            };

            if (!Detected.ContainsKey(id))
            {
                var det = new Detected3DObject()
                {
                    Id = id,
                    Label = label,
                    Score = 1.0f,
                    Position = relPos,
                    Rotation = relRot,
                    Scale = size,
                    LinearVelocity = new Vector3(speed, 0, 0),
                    AngularVelocity = new Vector3(0, 0, angular_speed),
                    Velocity = velocity,
                    Gps = gps,
                    Heading = heading,
                    TrackingTime = 0f,
                };

                Detected.Add(id, new Tuple<Detected3DObject, Collider>(det, other));
            }
            else
            {
                var det = Detected[id].Item1;
                det.Position = relPos;
                det.Rotation = relRot;
                det.LinearVelocity = new Vector3(speed, 0, 0);
                det.AngularVelocity = new Vector3(0, 0, angular_speed);
                det.Acceleration = (velocity - det.Velocity) / Time.fixedDeltaTime;
                det.Velocity = velocity;
                det.Gps = gps;
                det.Heading = heading;
                det.TrackingTime += Time.fixedDeltaTime;
            }

            CurrentIDs.Add(id);
        }

        private IEnumerator OnPublish()
        {
            uint seqId = 0;
            double nextSend = SimulatorManager.Instance.CurrentTime + 1.0f / Frequency;

            while (true)
            {
                yield return new WaitForFixedUpdate();

                var IDs = new HashSet<uint>(Detected.Keys);
                IDs.ExceptWith(CurrentIDs);
                foreach(uint id in IDs)
                {
                    Detected.Remove(id);
                }

                if (Bridge != null && Bridge.Status == Status.Connected)
                {
                    if (SimulatorManager.Instance.CurrentTime < nextSend)
                    {
                        continue;
                    }
                    nextSend = SimulatorManager.Instance.CurrentTime + 1.0f / Frequency;

                    var currentObjects = new List<Detected3DObject>();
                    foreach (uint id in CurrentIDs)
                    {
                        currentObjects.Add(Detected[id].Item1);
                    }

                    var data = new Detected3DObjectData()
                    {
                        Name = Name,
                        Frame = Frame,
                        Time = SimulatorManager.Instance.CurrentTime,
                        Sequence = seqId++,
                        Data = currentObjects.ToArray(),
                    };

                    Publish(data);
                }
            }
        }

        public override void OnVisualize(Visualizer visualizer)
        {
            foreach (uint id in CurrentIDs)
            {
                var col = Detected[id].Item2;
                if (col.gameObject.activeInHierarchy)
                {
                    GameObject parent = col.gameObject.transform.parent.gameObject;
                    Color color = Color.green;
                    if (parent.layer == LayerMask.NameToLayer("Pedestrian"))
                    {
                        color = Color.yellow;
                    }

                    BoxCollider box = col as BoxCollider;
                    WireframeBoxes.Draw
                    (
                        box.transform.localToWorldMatrix,
                        new Vector3(0f, box.bounds.extents.y, 0f),
                        box.size,
                        color
                    );
                }
            }
        }

        public override void OnVisualizeToggle(bool state) {}

        public override bool CheckVisible(Bounds bounds)
        {
            return Vector3.Distance(transform.position, bounds.center) < MaxDistance;
        }

        void OnDestroy()
        {
            StopAllCoroutines();

            Detected.Clear();
            CurrentIDs.Clear();
        }
    }
}
