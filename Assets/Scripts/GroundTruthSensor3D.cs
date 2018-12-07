/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GroundTruthSensor3D : MonoBehaviour, Ros.IRosClient {
	public string objects3DTopicName = "/simulator/ground_truth/3d_detections";
	public float frequency = 10.0f;
	
	public ROSTargetEnvironment targetEnv;
    public GameObject lidarSensor;
    public RadarRangeTrigger lidarRangeTrigger;
    public GameObject boundingBox;
    
	private uint seqId;
	private uint objId;
	private float nextSend;
	private Ros.Bridge Bridge;
	private List<Ros.Detection3D> detectedObjects;
    private Dictionary<Collider, Ros.Detection3D> lidarDetectedColliders;
    private bool isEnabled = false;

    private void Awake()
    {
        AddUIElement();
    }

    private void Start () {
		detectedObjects = new List<Ros.Detection3D>();
		lidarDetectedColliders = new Dictionary<Collider, Ros.Detection3D>();
        lidarRangeTrigger.SetCallback(OnLidarObjectDetected);
		nextSend = Time.time + 1.0f / frequency;

        lidarRangeTrigger.transform.localScale = new Vector3(
            lidarSensor.GetComponent<LidarSensor>().MaxDistance * 2,
            lidarSensor.GetComponent<LidarSensor>().MaxDistance * 2,
            lidarSensor.GetComponent<LidarSensor>().MaxDistance * 2
        );
	}
	
	private void Update () {
        if (!isEnabled) {
            return;
        }

        detectedObjects = lidarDetectedColliders.Values.ToList();
        Visualize(detectedObjects);
        
        lidarDetectedColliders.Clear();
		objId = 0;

		if (Bridge == null || Bridge.Status != Ros.Status.Connected) {
            return;
        }

		if (Time.time < nextSend) {
			return;
		}
		nextSend = Time.time + 1.0f / frequency;

		if (targetEnv == ROSTargetEnvironment.AUTOWARE || targetEnv == ROSTargetEnvironment.APOLLO) {
			PublishGroundTruth(detectedObjects);
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

    System.Func<Collider, Vector3> GetLinVel = ((col) => {
        var trafAiMtr = col.GetComponentInParent<TrafAIMotor>();
        if (trafAiMtr != null) {
            return trafAiMtr.currentVelocity;
        } else {
            return col.attachedRigidbody == null ? Vector3.zero : col.attachedRigidbody.velocity;
        }
    });

    System.Func<Collider, Vector3> GetAngVel = ((col) => {
        return col.attachedRigidbody == null ? Vector3.zero : col.attachedRigidbody.angularVelocity;
    });

	private void OnLidarObjectDetected(Collider detect) {
        if (isEnabled && !lidarDetectedColliders.ContainsKey(detect)) {
            string label = "";
            Vector3 size = Vector3.zero;
            if (detect.gameObject.layer == 14 || detect.gameObject.layer == 19) {
                // if NPC or NPC Static
                label = "car";
                if (detect.GetType() == typeof(BoxCollider)) {
                    size.x = ((BoxCollider) detect).size.z;
                    size.y = ((BoxCollider) detect).size.x;
                    size.z = ((BoxCollider) detect).size.y;
                }
            } else if (detect.gameObject.layer == 18) {
                // if Pedestrian
                label = "pedestrian";
                if (detect.GetType() == typeof(CapsuleCollider)) {
                    size.x = ((CapsuleCollider) detect).radius;
                    size.y = ((CapsuleCollider) detect).radius;
                    size.z = ((CapsuleCollider) detect).height;
                }
            }

            if (label == "" || size.magnitude == 0) {
                return;
            }

            // Local position of object in Lidar local space
            Vector3 relPos = lidarSensor.transform.InverseTransformPoint(detect.transform.position);
            // Convert from (Right/Up/Forward) to (Forward/Left/Up)
            relPos.Set(relPos.z, -relPos.x, relPos.y);

            // Relative rotation of objects wrt Lidar frame
            Quaternion relRot = Quaternion.Inverse(lidarSensor.transform.rotation) * detect.transform.rotation;
            // Convert from (Right/Up/Forward) to (Forward/Left/Up)
            relRot.Set(relRot.z, -relRot.x, relRot.y, relRot.w);

            // Linear velocity in forward direction of objects, in meters/sec
            float linear_vel = Vector3.Dot(GetLinVel(detect), detect.transform.forward);
            // Angular velocity around up axis of objects, in radians/sec
            float angular_vel = -(GetAngVel(detect)).y;
            
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
                    size = new Ros.Vector3() {
                        x = size.x,
                        y = size.y,
                        z = size.z,
                    },
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
                (float) obj.bbox.position.position.x,
                (float) obj.bbox.position.position.y,
                (float) obj.bbox.position.position.z
            );

            relPos.Set(-relPos.y, relPos.z, relPos.x);
            Vector3 worldPos = lidarSensor.transform.TransformPoint(relPos);
            worldPos.y += (float) obj.bbox.size.z / 2.0f;  // Lift bbox up to ground
            bbox.transform.position = worldPos;

            Quaternion relRot = new Quaternion(
                (float) obj.bbox.position.orientation.x,
                (float) obj.bbox.position.orientation.y,
                (float) obj.bbox.position.orientation.z,
                (float) obj.bbox.position.orientation.w
            );

            relRot.Set(-relRot.y, relRot.z, relRot.x, relRot.w);
            Quaternion worldRot = lidarSensor.transform.rotation * relRot;
            bbox.transform.rotation = worldRot;
            
            bbox.transform.localScale = new Vector3(
                (float) obj.bbox.size.y * 1.1f,
                (float) obj.bbox.size.z * 1.1f,
                (float) obj.bbox.size.x * 1.1f
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

    private void AddUIElement() {
        var groundTruth3DCheckbox = transform.parent.gameObject.GetComponent<UserInterfaceTweakables>().AddCheckbox("ToggleGroundTruth3D", "Enable Ground Truth 3D:", isEnabled);
        groundTruth3DCheckbox.onValueChanged.AddListener(x => Enable(x));
    }
}
