/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Simulator.Bridge;
using Simulator.Bridge.Data;
using Simulator.Utilities;

namespace Simulator.Sensors
{
    [SensorType("3D Ground Truth", new[] {typeof(Detected3DObjectData) })]
    public class GroundTruth3DSensor : SensorBase
    {
        [SensorParameter]
        [Range(1f, 100f)]
        public float Frequency = 10.0f;

        public RangeTrigger rangeTrigger;

        WireframeBoxes WireframeBoxes;

        private uint seqId;
        private uint objId;
        private float nextSend;

        private IBridge Bridge;
        private IWriter<Detected3DObjectData> Writer;

        private Dictionary<Collider, Detected3DObject> Detected = new Dictionary<Collider, Detected3DObject>();
        private Dictionary<Collider, Box> Visualized = new Dictionary<Collider, Box>();

        struct Box
        {
            public Vector3 Size;
            public Color Color;
        }

        void Start()
        {
            WireframeBoxes = SimulatorManager.Instance.WireframeBoxes;
            rangeTrigger.SetCallbacks(OnEnterRange, WhileInRange, OnExitRange);
            nextSend = Time.time + 1.0f / Frequency;
        }

        void Update()
        {
            foreach (var v in Visualized)
            {
                var collider = v.Key;
                var box = v.Value;

                WireframeBoxes.Draw(collider.gameObject.transform.localToWorldMatrix, Vector3.zero, box.Size, box.Color);
            }

            if (Bridge == null || Bridge.Status != Status.Connected)
            {
                return;
            }

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

        public override void OnBridgeSetup(IBridge bridge)
        {
            Bridge = bridge;
            Writer = Bridge.AddWriter<Detected3DObjectData>(Topic);
        }

        void OnEnterRange(Collider other)
        {
            if (other.isTrigger)
            {
                return;
            }

            if (!Detected.ContainsKey(other))
            {
                var bbox = new Box();

                string label = null;
                Vector3 size;
                float y_offset = 0.0f;

                if (other is BoxCollider)
                {
                    var box = other as BoxCollider;
                    bbox.Size = box.size;
                    size.x = box.size.z;
                    size.y = box.size.x;
                    size.z = box.size.y;
                    y_offset = box.center.y;
                }
                else if (other is CapsuleCollider)
                {
                    var capsule = other as CapsuleCollider;
                    bbox.Size = new Vector3(capsule.radius * 2, capsule.height, capsule.radius * 2);
                    size.x = capsule.radius * 2;
                    size.y = capsule.radius * 2;
                    size.z = capsule.height;
                    y_offset = capsule.center.y;
                }
                else
                {
                    return;
                }

                if (other.gameObject.layer == LayerMask.NameToLayer("NPC"))
                {
                    label = "Car";
                    bbox.Color = Color.green;
                }
                else if (other.gameObject.layer == LayerMask.NameToLayer("Pedestrian"))
                {
                    label = "Pedestrian";
                    bbox.Color = Color.yellow;
                }
                else if (other.gameObject.layer == LayerMask.NameToLayer("Bicycle"))
                {
                    label = "bicycle";
                    bbox.Color = Color.cyan;
                }
                else
                {
                    bbox.Color = Color.magenta;
                }

                Visualized.Add(other, bbox);

                if (string.IsNullOrEmpty(label))
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

                // Linear velocity in forward direction of objects, in meters/sec
                float linear_vel = Vector3.Dot(other.attachedRigidbody == null ? Vector3.zero : other.attachedRigidbody.velocity, other.transform.forward);
                // Angular velocity around up axis of objects, in radians/sec
                float angular_vel = -(other.attachedRigidbody == null ? Vector3.zero : other.attachedRigidbody.angularVelocity).y;

                Detected.Add(other, new Detected3DObject()
                {
                    Id = objId++,
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

        void WhileInRange(Collider other)
        {
            if (Detected.ContainsKey(other))
            {
                // Local position of object in Lidar local space
                Vector3 relPos = transform.InverseTransformPoint(other.transform.position);
                // Lift up position to the ground
                relPos.y += ((BoxCollider)other).center.y;
                // Convert from (Right/Up/Forward) to (Forward/Left/Up)
                relPos.Set(relPos.z, -relPos.x, relPos.y);

                // Relative rotation of objects wrt Lidar frame
                Quaternion relRot = Quaternion.Inverse(transform.rotation) * other.transform.rotation;
                // Convert from (Right/Up/Forward) to (Forward/Left/Up)
                relRot.Set(relRot.z, -relRot.x, relRot.y, relRot.w);

                Detected[other].Position = relPos;
                Detected[other].Rotation = relRot;
                Detected[other].LinearVelocity = Vector3.right * Vector3.Dot(other.attachedRigidbody == null ? Vector3.zero : other.attachedRigidbody.velocity, other.transform.forward);
                Detected[other].AngularVelocity = Vector3.left * (other.attachedRigidbody == null ? Vector3.zero : other.attachedRigidbody.angularVelocity).y;
            }
        }

        void OnExitRange(Collider other)
        {
            if (Detected.ContainsKey(other))
            {
                Detected.Remove(other);
            }

            if (Visualized.ContainsKey(other))
            {
                Visualized.Remove(other);
            }
        }
    }
}