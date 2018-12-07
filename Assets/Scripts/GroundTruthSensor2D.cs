/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GroundTruthSensor2D : MonoBehaviour, Ros.IRosClient {
    public string objects2DTopicName = "/simulator/ground_truth/2d_detections";
	public float frequency = 10.0f;
	
	public ROSTargetEnvironment targetEnv;
	public Transform lidarLocalspaceTransform;
    // public GameObject boundingBox;

    public Camera cam;

    public float maxDistance = 300f;
    private float fieldOfView;
    
	private uint seqId;
	private uint objId;
	private float nextSend;
	private Ros.Bridge Bridge;
	private List<Ros.Detection2D> detectedObjects;
    private Dictionary<Collider, Ros.Detection2D> cameraDetectedColliders;
    private bool isEnabled = false;

    public RadarRangeTrigger cameraRangeTrigger;

    private float radVFOV;
    private float radHFOV;
    private float degVFOV;
    private float degHFOV;

    private void Start () {
        radVFOV = cam.fieldOfView * Mathf.Deg2Rad;
        radHFOV = 2 * Mathf.Atan(Mathf.Tan(radVFOV / 2) * cam.aspect);
        degVFOV = cam.fieldOfView;
        degHFOV = Mathf.Rad2Deg * radHFOV;

        float width = 2 * Mathf.Tan(radHFOV / 2) * maxDistance;
        float height = 3f;
        float depth = maxDistance;

        BoxCollider camBoxCollider = cameraRangeTrigger.GetComponent<BoxCollider>();

        camBoxCollider.center = new Vector3(0, height / 2f, depth / 2f);
        camBoxCollider.size = new Vector3(width, height, depth);

        cameraRangeTrigger.SetCallback(OnCameraObjectDetected);

        // Vector3[] frustumCorners = new Vector3[4];
        // camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), maxDistance, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);
        // for (int i = 0; i < 4; i++) {
        //     var worldSpaceCorner = camera.transform.TransformVector(frustumCorners[i]);
        //     Debug.Log("A: " + frustumCorners[i]);
        //     Debug.Log("B: " + worldSpaceCorner);
        // }

		detectedObjects = new List<Ros.Detection2D>();
		cameraDetectedColliders = new Dictionary<Collider, Ros.Detection2D>();
		nextSend = Time.time + 1.0f / frequency;
	}
	
	private void Update () {
    //     // if (!isEnabled) {
    //     //     return;
    //     // }

        detectedObjects = cameraDetectedColliders.Values.ToList();
    //     // Visualize(detectedObjects);
        
        cameraDetectedColliders.Clear();
		objId = 0;

		// if (Bridge == null || Bridge.Status != Ros.Status.Connected) {
        //     return;
        // }

		if (Time.time < nextSend) {
			return;
		}
		nextSend = Time.time + 1.0f / frequency;

		if (targetEnv == ROSTargetEnvironment.AUTOWARE || targetEnv == ROSTargetEnvironment.APOLLO) {
			PublishGroundTruth(detectedObjects);
		}
	}

    // public void Enable(bool enabled)
    // {lidarLocalspaceTransform
    //     isEnabled = enabllidarLocalspaceTransformed;
    //     detectedObjects.ClidarLocalspaceTransformlear();
    //     cameraDetectedCollidarLocalspaceTransformliders.Clear();
    //     objId = 0;
    // }

	public void OnRosBridgeAvailable(Ros.Bridge bridge) {
        Bridge = bridge;
        Bridge.AddPublisher(this);
    }

    public void OnRosConnected() {
        if (targetEnv == ROSTargetEnvironment.AUTOWARE || targetEnv == ROSTargetEnvironment.APOLLO) {
            Bridge.AddPublisher<Ros.Detection2DArray>(objects2DTopicName);
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

	private void OnCameraObjectDetected(Collider detect) {
        // Vector from camera to collider
        Vector3 vectorFromCamToCol = detect.transform.position - cam.transform.position;
        // Vector projected onto camera plane
        Vector3 vectorProjToCamPlane = Vector3.ProjectOnPlane(vectorFromCamToCol, cam.transform.up);
        // Angle in degree between collider and camera forward direction
        var angleHorizon = Vector3.Angle(vectorProjToCamPlane, cam.transform.forward);
        
        // Check if collider is out of field of view
        if (angleHorizon > degHFOV / 2) {
            return;
        }

        if (!cameraDetectedColliders.ContainsKey(detect)) {
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
            Vector3 relPos = lidarLocalspaceTransform.InverseTransformPoint(detect.transform.position);
            // Convert from (Right/Up/Forward) to (Forward/Left/Up)
            relPos.Set(relPos.z, -relPos.x, relPos.y);

            // Relative rotation of objects wrt Lidar frame
            Quaternion relRot = Quaternion.Inverse(lidarLocalspaceTransform.rotation) * detect.transform.rotation;
            // Convert from (Right/Up/Forward) to (Forward/Left/Up)
            relRot.Set(relRot.z, -relRot.x, relRot.y, relRot.w);

            // Linear velocity in forward direction of objects, in meters/sec
            float linear_vel = Vector3.Dot(GetLinVel(detect), detect.transform.forward);
            // Angular velocity around up axis of objects, in radians/sec
            float angular_vel = -(GetAngVel(detect)).y;

            cameraDetectedColliders.Add(detect, new Ros.Detection2D() {
                header = new Ros.Header() {
                    stamp = Ros.Time.Now(),
                    seq = seqId++,
                    frame_id = "velodyne",
                },
                id = objId++,
                label = label,
                score = 1.0f,
                bbox = new Ros.BoundingBox2D() {
                    x = 0,
                    y = 0,
                    width = 10,
                    height = 10,
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
                }
            });
        }
    }

	private void PublishGroundTruth(List<Ros.Detection2D> detectedObjects) {
		if (Bridge == null || Bridge.Status != Ros.Status.Connected) {
            return;
        }

        var detectedObjectArrayMsg = new Ros.Detection2DArray() {
            detections = detectedObjects,
        };

        // if (targetEnv == ROSTargetEnvironment.AUTOWARE || targetEnv == ROSTargetEnvironment.APOLLO) {
        //     Bridge.Publish(objects2DTopicName, detectedObjectArrayMsg);
        // }

        Debug.Log("Publish");
	}

    // private void Visualize(List<Ros.Detection3D> detectedObjects) {
    //     if (boundingBox == null) {
    //         return;
    //     }
        
    //     foreach (Ros.Detection3D obj in detectedObjects) {
    //         GameObject bbox = Instantiate(boundingBox);
    //         bbox.transform.parent = transform;

    //         Vector3 relPos = new Vector3(
    //             (float) obj.bbox.position.position.x,
    //             (float) obj.bbox.position.position.y,
    //             (float) obj.bbox.position.position.z
    //         );

    //         relPos.Set(-relPos.y, relPos.z, relPos.x);
    //         Vector3 worldPos = lidarLocalspaceTransform.TransformPoint(relPos);
    //         worldPos.y += (float) obj.bbox.size.z / 2.0f;  // Lift bbox up to ground
    //         bbox.transform.position = worldPos;

    //         Quaternion relRot = new Quaternion(
    //             (float) obj.bbox.position.orientation.x,
    //             (float) obj.bbox.position.orientation.y,
    //             (float) obj.bbox.position.orientation.z,
    //             (float) obj.bbox.position.orientation.w
    //         );

    //         relRot.Set(-relRot.y, relRot.z, relRot.x, relRot.w);
    //         Quaternion worldRot = lidarLocalspaceTransform.rotation * relRot;
    //         bbox.transform.rotation = worldRot;
            
    //         bbox.transform.localScale = new Vector3(
    //             (float) obj.bbox.size.y * 1.1f,
    //             (float) obj.bbox.size.z * 1.1f,
    //             (float) obj.bbox.size.x * 1.1f
    //         );

    //         Renderer rend = bbox.GetComponent<Renderer>();
    //         if (obj.label == "car") {
    //             rend.material.SetColor("_Color", new Color(0, 1, 0, 0.3f));  // Color.green
    //         } else if (obj.label == "pedestrian") {
    //             rend.material.SetColor("_Color", new Color(1, 0.92f, 0.016f, 0.3f));  // Color.yellow
    //         } else if (obj.label == "bicycle") {
    //             rend.material.SetColor("_Color", new Color(0, 1, 1, 0.3f));  // Color.cyan
    //         } else {
    //             rend.material.SetColor("_Color", new Color(1, 0, 1, 0.3f));  // Color.magenta
    //         }

    //         bbox.SetActive(true);
    //         Destroy(bbox, Time.deltaTime);
    //     }
    // }
}
