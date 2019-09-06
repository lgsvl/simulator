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

        public RangeTrigger rangeTrigger;

        WireframeBoxes WireframeBoxes;

        private uint seqId;
        private uint objId;
        private float nextSend;

        private IBridge Bridge;
        private IWriter<Detected3DObjectData> Writer;

        private Dictionary<Collider, Detected3DObject> Detected = new Dictionary<Collider, Detected3DObject>();
        private Dictionary<int, uint> IDByInstanceID = new Dictionary<int, uint>();
        private Collider[] Visualized = Array.Empty<Collider>();

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

                Writer.Write(new Detected3DObjectData()
                {
                    Name = Name,
                    Frame = Frame,
                    Time = SimulatorManager.Instance.CurrentTime,
                    Sequence = seqId++,
                    Data = Detected.Values.ToArray(),
                });
            }

            Visualized = Detected.Keys.ToArray();
            Detected.Clear();
        }

        public override void OnBridgeSetup(IBridge bridge)
        {
            Bridge = bridge;
            Writer = Bridge.AddWriter<Detected3DObjectData>(Topic);
        }

        void WhileInRange(Collider other)
        {
            if (other.isTrigger || !other.gameObject.activeInHierarchy)
            {
                return;
            }

            if (!Detected.ContainsKey(other))
            {
                Vector3 size;
                float y_offset;
                float linear_vel;  // Linear velocity in forward direction of objects, in meters/sec
                float angular_vel;  // Angular velocity around up axis of objects, in radians/sec
                if (other is MeshCollider)
                {
                    var mesh = other as MeshCollider;
                    var npcC = mesh.gameObject.GetComponentInParent<NPCController>();
                    if (npcC != null)
                    {
                        size.x = npcC.bounds.size.x;
                        size.y = npcC.bounds.size.y;
                        size.z = npcC.bounds.size.z;
                        y_offset = 0f;
                        linear_vel = Vector3.Dot(npcC.GetVelocity(), other.transform.forward);
                        angular_vel = -npcC.GetAngularVelocity().y;
                    }
                    else
                    {
                        var egoA = mesh.GetComponent<VehicleActions>();
                        size.x = egoA.bounds.size.z;
                        size.y = egoA.bounds.size.x;
                        size.z = egoA.bounds.size.y;
                        y_offset = 0f;
                        linear_vel = Vector3.Dot(other.attachedRigidbody == null ? Vector3.zero : other.attachedRigidbody.velocity, other.transform.forward);
                        angular_vel = -(other.attachedRigidbody == null ? Vector3.zero : other.attachedRigidbody.angularVelocity).y;
                    }
                }
                else if (other is CapsuleCollider)
                {
                    var capsule = other as CapsuleCollider;
                    var pedC = other.GetComponent<PedestrianController>();
                    size.x = capsule.radius * 2;
                    size.y = capsule.radius * 2;
                    size.z = capsule.height;
                    y_offset = capsule.center.y;
                    linear_vel = Vector3.Dot(pedC.CurrentVelocity, other.transform.forward);
                    angular_vel = -pedC.CurrentAngularVelocity.y;
                }
                else
                {
                    return;
                }

                if (size.magnitude == 0)
                {
                    return;
                }

                string label;
                if (other.gameObject.layer == LayerMask.NameToLayer("NPC"))
                {
                    label = "Car";
                }
                else if (other.gameObject.layer == LayerMask.NameToLayer("Pedestrian"))
                {
                    label = "Pedestrian";
                }
                else if (other.gameObject.layer == LayerMask.NameToLayer("Bicycle"))
                {
                    label = "bicycle";
                }
                else
                {
                    return;
                }

                // Local position of object in Lidar local space
                Vector3 relPos = transform.InverseTransformPoint(other.transform.position);
                // Lift up position to the ground
                relPos.y += y_offset;
                // Convert from (Right/Up/Forward) to (Forward/Left/Up)
                relPos.Set(relPos.z, -relPos.x, relPos.y);

                // Relative rotation of objects wrt Lidar frame
                Quaternion relRot = Quaternion.Inverse(transform.rotation) * other.transform.rotation;
                // Convert from (Right/Up/Forward) to (Forward/Left/Up)
                relRot.Set(relRot.z, -relRot.x, relRot.y, relRot.w);

                Detected.Add(other, new Detected3DObject()
                {
                    Id = GetNextID(other),
                    Label = label,
                    Score = 1.0f,
                    Position = relPos,
                    Rotation = relRot,
                    Scale = size,
                    LinearVelocity = new Vector3(linear_vel, 0, 0),
                    AngularVelocity = new Vector3(0, 0, angular_vel),
                });
            }
        }

        private uint GetNextID(Collider other)
        {
            int instanceID = other.gameObject.GetInstanceID();
            if (!IDByInstanceID.ContainsKey(instanceID))
            {
                IDByInstanceID.Add(instanceID, (uint)IDByInstanceID.Count);
            }

            return IDByInstanceID[instanceID];
        }

        public override void OnVisualize(Visualizer visualizer)
        {
            foreach (var other in Visualized)
            {
                if (!other.gameObject.activeInHierarchy)
                {
                    return;
                }

                Vector3 size = Vector3.zero;
                if (other is MeshCollider)
                {
                    var mesh = other as MeshCollider;
                    var npcC = mesh.gameObject.GetComponentInParent<NPCController>();
                    if (npcC != null)
                    {
                        size = npcC.bounds.size;
                    }
                    else
                    {
                        var egoA = mesh.GetComponent<VehicleActions>();
                        size = egoA.bounds.size;
                    }
                }
                else if (other is CapsuleCollider)
                {
                    var capsule = other as CapsuleCollider;
                    size = new Vector3(capsule.radius * 2, capsule.height, capsule.radius * 2);
                }

                Color color = Color.magenta;
                if (other.gameObject.layer == LayerMask.NameToLayer("NPC"))
                {
                    color = Color.green;
                }
                else if (other.gameObject.layer == LayerMask.NameToLayer("Pedestrian"))
                {
                    color = Color.yellow;
                }
                else if (other.gameObject.layer == LayerMask.NameToLayer("Bicycle"))
                {
                    color = Color.cyan;
                }

                WireframeBoxes.Draw(other.gameObject.transform.localToWorldMatrix, other is MeshCollider ? Vector3.zero : new Vector3(0f, other.bounds.extents.y, 0f), size, color);
            }
        }

        public override void OnVisualizeToggle(bool state)
        {
            //
        }
    }
}
