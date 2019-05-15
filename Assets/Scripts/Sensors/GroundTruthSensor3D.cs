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

namespace Simulator.Sensors
{
    public class GroundTruthSensor3D : SensorBase
    {
        public float frequency = 10.0f;

        public RangeTrigger rangeTrigger;
        public GameObject boundingBox;

        private uint seqId;
        private uint objId;
        private float nextSend;

        private IBridge Bridge;
        private IWriter<Detected3DObjectData> Writer;

        private Dictionary<Collider, Detected3DObject> Detected = new Dictionary<Collider, Detected3DObject>();
        private Dictionary<Collider, GameObject> Visualized = new Dictionary<Collider, GameObject>();

        private void Start()
        {
            rangeTrigger.SetCallbacks(OnEnterRange, WhileInRange, OnExitRange);
            nextSend = Time.time + 1.0f / frequency;
        }

        private void Update()
        {
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
                Sequence = seqId++,
                Frame = Frame,
                Data = Detected.Values.ToArray(),
            });
        }

        public override void OnBridgeSetup(IBridge bridge)
        {
            Bridge = bridge;
            Writer = Bridge.AddWriter<Detected3DObjectData>(Topic);
        }

        private void OnEnterRange(Collider other)
        {
            if (other.isTrigger)
            {
                return;
            }

            if (!Visualized.ContainsKey(other))
            {
                GameObject bbox = Instantiate(boundingBox, other.transform.position, other.transform.rotation, transform);
                if (other is BoxCollider)
                {
                    var box = other as BoxCollider;
                    bbox.transform.localScale = box.size * 1.1f;
                }
                else if (other is CapsuleCollider)
                {
                    var capsule = other as CapsuleCollider;
                    bbox.transform.localScale = new Vector3(capsule.radius, capsule.height, capsule.radius) * 1.1f;
                }

                Renderer rend = bbox.GetComponent<Renderer>();
                switch (LayerMask.LayerToName(other.gameObject.layer))
                {
                    case "NPC":
                        rend.material.SetColor("_UnlitColor", new Color(0, 1, 0, 0.3f));  // Color.green
                        break;
                    case "Pedestrian":
                        rend.material.SetColor("_UnlitColor", new Color(1, 0.92f, 0.016f, 0.3f));  // Color.yellow
                        break;
                    case "Bicycle":
                        rend.material.SetColor("_UnlitColor", new Color(0, 1, 1, 0.3f));  // Color.cyan
                        break;
                    default:
                        rend.material.SetColor("_UnlitColor", new Color(1, 0, 1, 0.3f));  // Color.magenta
                        break;
                }

                bbox.SetActive(true);
                Visualized.Add(other, bbox);
            }

            if (!Detected.ContainsKey(other))
            {
                string label = "";
                Vector3 size = Vector3.zero;
                float y_offset = 0.0f;
                if (other.gameObject.layer == LayerMask.NameToLayer("NPC"))
                {
                    // if GroundTruth (Player), NPC, or NPC Static layer
                    label = "Car";
                    if (other.GetType() == typeof(BoxCollider))
                    {
                        size.x = ((BoxCollider)other).size.z;
                        size.y = ((BoxCollider)other).size.x;
                        size.z = ((BoxCollider)other).size.y;
                        y_offset = ((BoxCollider)other).center.y;
                    }
                }
                else if (other.gameObject.layer == LayerMask.NameToLayer("Pedestrian"))
                {
                    // if Pedestrian layer
                    label = "Pedestrian";
                    if (other.GetType() == typeof(CapsuleCollider))
                    {
                        size.x = ((CapsuleCollider)other).radius;
                        size.y = ((CapsuleCollider)other).radius;
                        size.z = ((CapsuleCollider)other).height;
                        y_offset = ((CapsuleCollider)other).center.y;
                    }
                }

                if (label == "" || size.magnitude == 0)
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

        private void WhileInRange(Collider other)
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

            if (Visualized.ContainsKey(other))
            {
                Visualized[other].transform.position = other.transform.position;
                Visualized[other].transform.rotation = other.transform.rotation;
            }
        }

        private void OnExitRange(Collider other)
        {
            if (Detected.ContainsKey(other))
            {
                Detected.Remove(other);
            }

            if (Visualized.ContainsKey(other))
            {
                Destroy(Visualized[other]);
                Visualized.Remove(other);
            }
        }

    }
}