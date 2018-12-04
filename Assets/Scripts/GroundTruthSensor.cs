/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GroundTruthSensor : MonoBehaviour, Ros.IRosClient {
	public string objects3DTopicName = "/simulator/ground_truth/3d_detections";
	public float frequency = 10.0f;
	
	public ROSTargetEnvironment targetEnv;
	public Transform lidarLocalspaceTransform;
	public RadarRangeTrigger lidarRangeTrigger;
    public GameObject boundingBox;
    
	private uint seqId;
	private uint objId;
	private float nextSend;
	private Ros.Bridge Bridge;
	private List<Ros.Detection3D> detectedObjects;
    private Dictionary<Collider, Ros.Detection3D> lidarDetectedColliders;
    private bool isEnabled = false;

    private void Start () {
		detectedObjects = new List<Ros.Detection3D>();
		lidarDetectedColliders = new Dictionary<Collider, Ros.Detection3D>();
        lidarRangeTrigger.SetCallback(OnLidarObjectDetected);
		nextSend = Time.time + 1.0f / frequency;
	}
	
	private void Update () {
		if (targetEnv != ROSTargetEnvironment.AUTOWARE && targetEnv != ROSTargetEnvironment.APOLLO) {
            return;
        }

		if (Bridge == null || Bridge.Status != Ros.Status.Connected || !isEnabled) {
            return;
        }

        detectedObjects = lidarDetectedColliders.Values.ToList();
        Visualize(detectedObjects);

		if (Time.time < nextSend) {
			return;
		}
		nextSend = Time.time + 1.0f / frequency;

		if (targetEnv == ROSTargetEnvironment.AUTOWARE || targetEnv == ROSTargetEnvironment.APOLLO) {
            detectedObjects = lidarDetectedColliders.Values.ToList();
			PublishGroundTruth(detectedObjects);
			lidarDetectedColliders.Clear();
			objId = 0;
		}
	}

    public void Enable(bool enabled)
    {
        isEnabled = enabled;
        detectedObjects.Clear();
        lidarDetectedColliders.Clear();
        objId = 0;
    }

	public void OnRosBridgeAvailable(Ros.Bridge bridge) {
        Bridge = bridge;
        Bridge.AddPublisher(this);
    }

    public void OnRosConnected() {
        if (targetEnv == ROSTargetEnvironment.AUTOWARE || targetEnv == ROSTargetEnvironment.APOLLO) {
            Bridge.AddPublisher<Ros.Detection3DArray>(objects3DTopicName);
        }
    }

	private void OnLidarObjectDetected(Collider detect) {
        if (isEnabled && !lidarDetectedColliders.ContainsKey(detect)) {
            // Local position of object in Lidar local space
            Vector3 relPos = lidarLocalspaceTransform.InverseTransformPoint(detect.transform.position);
            // Convert from (Right/Up/Forward) to (Forward/Left/Up)
            relPos.Set(relPos.z, -relPos.x, relPos.y);

            // Relative rotation of objects wrt Lidar frame
            Quaternion relRot = Quaternion.Inverse(lidarLocalspaceTransform.rotation) * detect.transform.rotation;
            relRot.Set(relRot.z, -relRot.x, relRot.y, relRot.w);

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

            string label;
            Ros.Vector3 size;
            if (detect.gameObject.layer == 14) {
                label = "car";
                size = new Ros.Vector3() {
                    x = detect.GetComponent<BoxCollider>().size.z,
                    y = detect.GetComponent<BoxCollider>().size.x,
                    z = detect.GetComponent<BoxCollider>().size.y
                };
            } else if (detect.gameObject.layer == 18) {
                label = "pedestrian";
                size = new Ros.Vector3() {
                    x = detect.GetComponent<CapsuleCollider>().radius,
                    y = detect.GetComponent<CapsuleCollider>().radius,
                    z = detect.GetComponent<CapsuleCollider>().height
                };
            } else {
                label = "";
                size = new Ros.Vector3() {
                    x = 1.0f,
                    y = 1.0f,
                    z = 1.0f
                };
            }
            
            lidarDetectedColliders.Add(detect, new Ros.Detection3D() {
                header = new Ros.Header() {
                    stamp = Ros.Time.Now(),
                    seq = seqId++,
                    frame_id = "velodyne",
                },
                id = objId++,
                label = label,
                score = 1.0f,
                bbox = new Ros.BoundingBox3D() {
                    position = new Ros.Pose() {
                        position = new Ros.Point() {
                            x = relPos.x,
                            y = relPos.y,
                            z = relPos.z,
                        },
                        orientation = new Ros.Quaternion() {
                            x = relRot.x,
                            y = relRot.y,
                            z = relRot.z,
                            w = relRot.w,
                        },
                    },
                    size = size,
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

	private void PublishGroundTruth(List<Ros.Detection3D> detectedObjects) {
		if (Bridge == null || Bridge.Status != Ros.Status.Connected) {
            return;
        }

        var detectedObjectArrayMsg = new Ros.Detection3DArray() {
            detections = detectedObjects,
        };

        if (targetEnv == ROSTargetEnvironment.AUTOWARE || targetEnv == ROSTargetEnvironment.APOLLO) {
            Bridge.Publish(objects3DTopicName, detectedObjectArrayMsg);
        }
	}

    private void Visualize(List<Ros.Detection3D> detectedObjects) {
        if (boundingBox == null) {
            return;
        }
        
        foreach (Ros.Detection3D obj in detectedObjects) {
            GameObject bbox = Instantiate(boundingBox);
            bbox.transform.parent = transform;

            Vector3 relPos = new Vector3(
                (float)obj.bbox.position.position.x,
                (float)obj.bbox.position.position.y,
                (float)obj.bbox.position.position.z
            );

            relPos.Set(-relPos.y, relPos.z, relPos.x);
            Vector3 worldPos = lidarLocalspaceTransform.TransformPoint(relPos);
            bbox.transform.position = worldPos;

            Quaternion relRot = new Quaternion(
                (float)obj.bbox.position.orientation.x,
                (float)obj.bbox.position.orientation.y,
                (float)obj.bbox.position.orientation.z,
                (float)obj.bbox.position.orientation.w
            );

            relRot.Set(-relRot.y, relRot.z, relRot.x, relRot.w);
            Quaternion worldRot = lidarLocalspaceTransform.rotation * relRot;
            bbox.transform.rotation = worldRot;
            
            bbox.transform.localScale = new Vector3(
                (float)obj.bbox.size.y + 0.4f,
                (float)obj.bbox.size.z + 2.0f,
                (float)obj.bbox.size.x + 0.4f
            );

            Renderer rend = bbox.GetComponent<Renderer>();

            if (obj.label == "car") {
                rend.material.SetColor("_Color", new Color(0, 1, 0, 0.3f));  // Color.green
            } else if (obj.label == "pedestrian") {
                rend.material.SetColor("_Color", new Color(1, 0.92f, 0.016f, 0.3f));  // Color.yellow
            } else if (obj.label == "bicycle") {
                rend.material.SetColor("_Color", new Color(0, 1, 1, 0.3f));  // Color.cyan
            } else {
                rend.material.SetColor("_Color", new Color(1, 0, 1, 0.3f));  // Color.magenta
            }

            bbox.SetActive(true);
            Destroy(bbox, Time.deltaTime);
        }
    }
}
