/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Simulator.Bridge;
using Simulator.Bridge.Data;
using Simulator.Map;
using Simulator.Utilities;
using Simulator.Sensors.UI;
using Unity.Mathematics;

namespace Simulator.Sensors
{
    [Serializable]
    public class JSONGroundTruth3D
    {
        public uint Id;
        public string Label;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Dimension;
        public Vector3 Velocity;
        public double Time;
        public Vector3 GpsPosition;
        public Quaternion GpsRotation;
        public Vector3 GlobalPosition;
        public Quaternion GlobalRotation;
    }

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
        private double nextSend;

        private IBridge Bridge;
        private IWriter<Detected3DObjectData> Writer;

        private Dictionary<uint, Detected3DObject> Detected = new Dictionary<uint, Detected3DObject>();
        private Dictionary<uint, Collider> GTID2Collider = new Dictionary<uint, Collider>();
        private Collider[] Visualized = Array.Empty<Collider>();
        
        public override SensorDistributionType DistributionType => SensorDistributionType.HighLoad;
        MapOrigin MapOrigin;

        // Export NPC data into JSON. Set true if you enable it.
        [NonSerialized]
        public bool isToRecord;
        public bool doneLog = false;
        private Dictionary<string, List<JSONGroundTruth3D>> LogJsonDetected = new Dictionary<string, List<JSONGroundTruth3D>>();
        private double startTime;
        private Dictionary<string, double> prevTimes;

        void Start()
        {
            WireframeBoxes = SimulatorManager.Instance.WireframeBoxes;
            rangeTrigger.SetCallbacks(WhileInRange);
            rangeTrigger.transform.localScale = MaxDistance * Vector3.one;
            nextSend = SimulatorManager.Instance.CurrentTime + 1.0f / Frequency;
            MapOrigin = MapOrigin.Find();
            startTime = SimulatorManager.Instance.CurrentTime;
            prevTimes = new Dictionary<string,double>();
            isToRecord = false;
        }

        void Update()
        {
            if (Bridge != null && Bridge.Status == Status.Connected)
            {
                if (SimulatorManager.Instance.CurrentTime < nextSend)
                {
                    return;
                }
                nextSend = SimulatorManager.Instance.CurrentTime + 1.0f / Frequency;

                Writer.Write(new Detected3DObjectData()
                {
                    Name = Name,
                    Frame = Frame,
                    Time = SimulatorManager.Instance.CurrentTime,
                    Sequence = seqId++,
                    Data = Detected.Values.ToArray(),
                });
            }

            Visualized = GTID2Collider.Values.ToArray();
            Detected.Clear();
            GTID2Collider.Clear();
        }

        public override void OnBridgeSetup(IBridge bridge)
        {
            Bridge = bridge;
            Writer = Bridge.AddWriter<Detected3DObjectData>(Topic);
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
            float linear_vel;
            float angular_vel;
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

            // Local position of object in ego local space
            Vector3 relPos = transform.InverseTransformPoint(parent.transform.position);
            // Convert from (Right/Up/Forward) to (Forward/Left/Up)
            relPos.Set(relPos.z, -relPos.x, relPos.y);

            // Relative rotation of objects wrt ego frame
            var relRot = Quaternion.Inverse(transform.rotation) * parent.transform.rotation;
            // Convert from (Right/Up/Forward) to (Forward/Left/Up)
            relRot.Set(-relRot.z, relRot.x, -relRot.y, relRot.w);

            if (!Detected.ContainsKey(id))
            {
                Vector3 size = ((BoxCollider)other).size;
                // Convert from (Right/Up/Forward) to (Forward/Left/Up)
                size.Set(size.z, size.x, size.y);

                if (size.magnitude == 0)
                {
                    return;
                }

                Detected.Add(id, new Detected3DObject()
                {
                    Id = id,
                    Label = label,
                    Score = 1.0f,
                    Position = (float3)relPos,
                    Rotation = relRot,
                    Scale = size,
                    LinearVelocity = new Vector3(linear_vel, 0, 0),  // Linear velocity in forward direction of objects, in meters/sec
                    AngularVelocity = new Vector3(0, 0, angular_vel),  // Angular velocity around up axis of objects, in radians/sec
                });
            }
            else
            {
                Detected[id].Position = (float3)relPos;
                Detected[id].Rotation = relRot;
                Detected[id].LinearVelocity = new Vector3(linear_vel, 0, 0);
                Detected[id].AngularVelocity = new Vector3(0, 0, angular_vel);
            }

            if (!GTID2Collider.ContainsKey(id))
            {
                GTID2Collider.Add(id, other);
            }

            if (isToRecord)
            {
                var labelId = label + Convert.ToString(id);
                RecordJSONLog(Detected[id], labelId, parent, 0.1f);
                ExportJSON(40.0f);

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

        public void RecordJSONLog(Detected3DObject detected3DObject, string labelId, GameObject parent, float cycleTime)
        {
            var sensorRotation = transform.rotation;
            var sensorPosition = transform.position;

            var globalPos = parent.transform.position;
            var globalRot = parent.transform.rotation;

            if (!prevTimes.ContainsKey(labelId))
            {
                prevTimes.Add(labelId, SimulatorManager.Instance.CurrentTime);
            }

            // Get GPS coordinates.
            bool IgnoreMapOrigin = false;
            GpsLocation location = new GpsLocation();
            Vector3 position = transform.position;
            location = MapOrigin.GetGpsLocation(position, IgnoreMapOrigin);

            if (SimulatorManager.Instance.CurrentTime - prevTimes[labelId] > cycleTime)
            {
                // string labelId = null;
                var groundTruth3D = new JSONGroundTruth3D()
                {
                    Id = detected3DObject.Id,
                    Label = detected3DObject.Label,
                    Position = (float3)detected3DObject.Position,
                    Rotation = detected3DObject.Rotation,
                    Velocity = detected3DObject.LinearVelocity,
                    Dimension = detected3DObject.Scale,
                    Time = SimulatorManager.Instance.CurrentTime,
                    GpsPosition = new Vector3((float)(location.Easting + (IgnoreMapOrigin ? -500000 : 0)),
                        (float)location.Northing, (float)location.Altitude),
                    GpsRotation = sensorRotation,
                    GlobalPosition = globalPos,
                    GlobalRotation = globalRot
                };

                if (!LogJsonDetected.ContainsKey(labelId))
                {
                    var jsonGroundTruth3Ds = new List<JSONGroundTruth3D>();
                    jsonGroundTruth3Ds.Add(groundTruth3D);
                    LogJsonDetected.Add(labelId, jsonGroundTruth3Ds);
                }
                else
                {
                    LogJsonDetected[labelId].Add(groundTruth3D);
                }

                prevTimes[labelId] = SimulatorManager.Instance.CurrentTime;
            }
        }

        public void ExportJSON(float timeElapsed)
        {
            if (!doneLog && SimulatorManager.Instance.CurrentTime - startTime > timeElapsed)
            {
                var jsonDetections = new JSONArray();

                foreach (var k in LogJsonDetected.Keys)
                {
                    var jsonBbox = new JSONArray();
                    var jsonDetection = new JSONObject();

                    uint id = LogJsonDetected[k][0].Id;
                    string label = LogJsonDetected[k][0].Label;

                    foreach (var msg in LogJsonDetected[k])
                    {
                        var jsonNPC = new JSONObject();
                        jsonNPC.Add("position", msg.Position);
                        jsonNPC.Add("rotation", msg.Rotation);
                        jsonNPC.Add("velocity", msg.Velocity);
                        jsonNPC.Add("time", msg.Time);
                        jsonNPC.Add("gps_position", msg.GpsPosition);
                        jsonNPC.Add("gps_rotation", msg.GpsRotation);
                        jsonNPC.Add("global_position", msg.GlobalPosition);
                        jsonNPC.Add("global_rotation", msg.GlobalRotation);

                        jsonBbox.Add(jsonNPC);
                    }

                    jsonDetection.Add("id", id);
                    jsonDetection.Add("label", label);
                    jsonDetection.Add("bbox", jsonBbox);

                    jsonDetections.Add(jsonDetection);
                }

                var path = Path.Combine(Application.dataPath, "..", "ground_truth_3d_log.json");
                File.WriteAllText(path, jsonDetections.ToString());
                doneLog = true;
                Debug.Log("Finished writing ground truth 3d log json file,");
            }
        }
    }
}