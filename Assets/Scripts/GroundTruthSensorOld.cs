/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GroundTruthSensorOld : MonoBehaviour, Ros.IRosClient {
	public bool publishMessage = false;
	public string objects3DTopicName = "/simulator/ground_truth/objects_3d";
	public float frequency = 10.0f;
	
	public ROSTargetEnvironment targetEnv;
	public Transform lidarLocalspaceTransform;
	public RadarRangeTrigger lidarRangeTrigger;
    
	private uint seqId;
	private uint objId;
	private float nextSend;
	private Ros.Bridge Bridge;
	private List<Ros.DetectedObject> detectedObjects;
    private Dictionary<Collider, Ros.DetectedObject> lidarDetectedColliders;

    private void Start () {
		detectedObjects = new List<Ros.DetectedObject>();
		lidarDetectedColliders = new Dictionary<Collider, Ros.DetectedObject>();
        lidarRangeTrigger.SetCallback(OnLidarObjectDetected);
		nextSend = Time.time + 1.0f / frequency;
	}
	
	private void Update () {
		if (targetEnv != ROSTargetEnvironment.APOLLO && targetEnv != ROSTargetEnvironment.AUTOWARE) {
            return;
        }

		if (Bridge == null || Bridge.Status != Ros.Status.Connected || !publishMessage) {
            return;
        }

		if (Time.time < nextSend) {
			return;
		}
		nextSend = Time.time + 1.0f / frequency;

		if (targetEnv == ROSTargetEnvironment.AUTOWARE) {
			PublishGroundTruth();
			lidarDetectedColliders.Clear();
			objId = 0;
		}
	}

	public void OnRosBridgeAvailable(Ros.Bridge bridge) {
        Bridge = bridge;
        Bridge.AddPublisher(this);
    }

    public void OnRosConnected() {
        if (targetEnv == ROSTargetEnvironment.AUTOWARE) {
            Bridge.AddPublisher<Ros.DetectedObjectArray>(objects3DTopicName);
        }
    }

	private void OnLidarObjectDetected(Collider detect) {
        if (publishMessage && !lidarDetectedColliders.ContainsKey(detect)) {
            // Relative position of objects wrt Lidar frame
            Vector3 relPos = lidarLocalspaceTransform.InverseTransformPoint(detect.transform.position);
            relPos.Set(relPos.z, -relPos.x, relPos.y);

            // Relative rotation of objects wrt Lidar frame
            Quaternion relRot = Quaternion.Inverse(transform.rotation) * detect.transform.rotation;
            Vector3 angles = relRot.eulerAngles;
            float roll = -angles.z;
            float pitch = -angles.x;
            float yaw = angles.y;
            Quaternion quat = Quaternion.Euler(pitch, roll, yaw);

            System.Func<Collider, Vector3> GetLinVel = ((col) => {
                var trafAiMtr = col.GetComponentInParent<TrafAIMotor>();
                if (trafAiMtr != null)
                    return trafAiMtr.currentVelocity;
                else            
                    return col.attachedRigidbody == null ? Vector3.zero : col.attachedRigidbody.velocity;            
            });

            System.Func<Collider, Vector3> GetAngVel = ((col) => {
                return col.attachedRigidbody == null ? Vector3.zero : col.attachedRigidbody.angularVelocity;
            });

            // Linear velocity in forward direction of objects, in meters/sec
            float linear_vel = Vector3.Dot(GetLinVel(detect), detect.transform.forward);

            // Angular velocity around up axis of objects, in radians/sec
            float angular_vel = -(GetAngVel(detect)).y;
            
            lidarDetectedColliders.Add(detect, new Ros.DetectedObject() {
                header = new Ros.Header() {
                    stamp = Ros.Time.Now(),
                    seq = seqId++,
                    frame_id = "velodyne",
                },
                id = objId++,
                pose = new Ros.Pose() {
                    position = new Ros.Point() {
                        x = relPos.x,
                        y = relPos.y,
                        z = relPos.z,
                    },
                    orientation = new Ros.Quaternion() {
                        x = quat.x,
                        y = quat.y,
                        z = quat.z,
                        w = quat.w,
                    },
                },
                dimensions = new Ros.Vector3() {
                    x = detect.bounds.size.x,
                    y = detect.bounds.size.z,
                    z = detect.bounds.size.y,
                },
                velocity = new Ros.Twist() {
                    linear = new Ros.Vector3() {
                        x = linear_vel,
                        y = 0,
                        z = 0,
                    },
                    angular = new Ros.Vector3() {
                        x = 0,
                        y = 0,
                        z = angular_vel,
                    },
                },
            });
        }
    }

	private void PublishGroundTruth() {
		if (Bridge == null || Bridge.Status != Ros.Status.Connected) {
            return;
        }

		detectedObjects.Clear();
        detectedObjects = lidarDetectedColliders.Values.ToList();

        var detectedObjectArrayMsg = new Ros.DetectedObjectArray() {
            objects = detectedObjects,
        };

        if (targetEnv == ROSTargetEnvironment.AUTOWARE) {
            Bridge.Publish(objects3DTopicName, detectedObjectArrayMsg);
        }
	}
}
