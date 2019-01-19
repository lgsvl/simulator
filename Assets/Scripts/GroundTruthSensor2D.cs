/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

// #define VISUALIZE_RAYCAST

using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GroundTruthSensor2D : MonoBehaviour, Ros.IRosClient {
    public string objects2DTopicName = "/simulator/ground_truth/2d_detections";
    public string autowareCameraDetectionTopicName = "/detection/vision_objects";
	public float frequency = 10.0f;
	
	public ROSTargetEnvironment targetEnv;
    public RadarRangeTrigger cameraRangeTrigger;
    public float maxDistance = 100f;
    public Camera groundTruthCamera;
    public Camera targetCamera;

	private uint seqId;
	private uint objId;
	private float nextSend;
	private Ros.Bridge Bridge;
	private List<Ros.Detection2D> detectedObjects;
    private Dictionary<Collider, Ros.Detection2D> cameraDetectedColliders;
    private bool isEnabled = false;
    private bool isFirstEnabled = true;

    private float radVFOV;  // Vertical Field of View, in radian
    private float radHFOV;  // Horizontal Field of Voew, in radian
    private float degVFOV;  // Vertical Field of View, in degree
    private float degHFOV;  // Horizontal Field of View, in degree

    private RenderTextureDisplayer cameraPreview;
    private RenderTextureDisplayer targetCameraPreview;

    private static Texture2D backgroundTexture;
    private static GUIStyle textureStyle;

    private bool isCameraPredictionEnabled = false;
    private List<Ros.Detection2D> cameraPredictedObjects;
    private List<Ros.Detection2D> cameraPredictedVisuals;
    private bool isVisualize = true;

    private void Awake() {
        var videoWidth = 1920;
        var videoHeight = 1080;
        var rtDepth = 24;
        var rtFormat = RenderTextureFormat.ARGB32;
        var rtReadWrite = RenderTextureReadWrite.Linear;

        RenderTexture activeRT = new RenderTexture(videoWidth, videoHeight, rtDepth, rtFormat, rtReadWrite) {
            dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
            antiAliasing = 1,
            useMipMap = false,
            useDynamicScale = false,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        activeRT.name = "GroundTruthHD";
        activeRT.Create();
        groundTruthCamera.targetTexture = activeRT;

        GetComponentInParent<CameraSettingsManager>().AddCamera(groundTruthCamera);
        AddUIElement(groundTruthCamera);

        radVFOV = groundTruthCamera.fieldOfView * Mathf.Deg2Rad;
        radHFOV = 2 * Mathf.Atan(Mathf.Tan(radVFOV / 2) * groundTruthCamera.aspect);
        degVFOV = groundTruthCamera.fieldOfView;
        degHFOV = Mathf.Rad2Deg * radHFOV;

        float width = 2 * Mathf.Tan(radHFOV / 2) * maxDistance;
        float height = 3f;
        float depth = maxDistance;

        BoxCollider camBoxCollider = cameraRangeTrigger.GetComponent<BoxCollider>();
        camBoxCollider.center = new Vector3(0, 0, depth / 2f);
        camBoxCollider.size = new Vector3(width, height, depth);

        detectedObjects = new List<Ros.Detection2D>();
		cameraDetectedColliders = new Dictionary<Collider, Ros.Detection2D>();
        cameraPredictedObjects = new List<Ros.Detection2D>();
        cameraPredictedVisuals = new List<Ros.Detection2D>();
        cameraRangeTrigger.SetCallback(OnCameraObjectDetected);

        backgroundTexture = Texture2D.whiteTexture;
        textureStyle = new GUIStyle {
            normal = new GUIStyleState {
                background = backgroundTexture
            }
        };

        if (targetCamera != null) {
            targetCameraPreview = targetCamera.GetComponent<VideoToROS>().cameraPreview;
        }
    }

    void OnDestroy() {
        groundTruthCamera.targetTexture.Release();
    }

    private void Start() {
		nextSend = Time.time + 1.0f / frequency;
	}
	
	private void Update() {
        if (isEnabled && cameraDetectedColliders != null) {
            detectedObjects = cameraDetectedColliders.Values.ToList();
            cameraDetectedColliders.Clear();
		    objId = 0;

            if (targetEnv == ROSTargetEnvironment.AUTOWARE || targetEnv == ROSTargetEnvironment.APOLLO) {
                PublishGroundTruth(detectedObjects);
            }
        }
	}

    public void Enable(bool enabled) {
        isEnabled = enabled;
        objId = 0;

        if (isEnabled && isFirstEnabled) {
            isFirstEnabled = false;
            RobotSetup robotSetup = GetComponentInParent<RobotSetup>();
            if (robotSetup != null && robotSetup.NeedsBridge != null) {
                robotSetup.AddToNeedsBridge(this);
            }
        }

        groundTruthCamera.enabled = enabled;
        cameraPreview.gameObject.SetActive(enabled);

        if (detectedObjects != null) {
            detectedObjects.Clear();
        }

        if (cameraDetectedColliders != null) {
            cameraDetectedColliders.Clear();
        }
    }

    public void EnableCameraPrediction(bool enabled) {
        isCameraPredictionEnabled = enabled;

        if (isCameraPredictionEnabled && isFirstEnabled) {
            isFirstEnabled = false;
            RobotSetup robotSetup = GetComponentInParent<RobotSetup>();
            if (robotSetup != null && robotSetup.NeedsBridge != null) {
                robotSetup.AddToNeedsBridge(this);
            }
        }

        if (cameraPredictedVisuals != null) {
            cameraPredictedVisuals.Clear();
        }

        if (cameraPredictedObjects != null) {
            cameraPredictedObjects.Clear();
        }
    }

    public void OnRosBridgeAvailable(Ros.Bridge bridge) {
        Bridge = bridge;
        Bridge.AddPublisher(this);
    }

    public void OnRosConnected() {
        if (targetEnv == ROSTargetEnvironment.AUTOWARE || targetEnv == ROSTargetEnvironment.APOLLO) {
            Bridge.AddPublisher<Ros.Detection2DArray>(objects2DTopicName);
        }

        if (targetEnv == ROSTargetEnvironment.AUTOWARE) {
            Bridge.Subscribe<Ros.DetectedObjectArray>(autowareCameraDetectionTopicName, msg => {
                if (!isCameraPredictionEnabled || cameraPredictedObjects == null) {
                    return;
                }

                foreach (Ros.DetectedObject obj in msg.objects) {
                    var label = obj.label;
                    if (label == "person") {
                        label = "pedestrian";  // Autoware label as person
                    }
                    Ros.Detection2D obj_converted = new Ros.Detection2D() {
                        header = new Ros.Header() {
                            stamp = new Ros.Time() {
                                secs = obj.header.stamp.secs,
                                nsecs = obj.header.stamp.nsecs,
                            },
                            seq = obj.header.seq,
                            frame_id = obj.header.frame_id,
                        },
                        id = obj.id,
                        label = label,
                        score = obj.score,
                        bbox = new Ros.BoundingBox2D() {
                            x = obj.x + obj.width / 2,  // Autoware (x, y) point at top-left corner
                            y = obj.y + obj.height / 2,
                            width = obj.width,
                            height = obj.height,
                        },
                        velocity = new Ros.Twist() {
                            linear = new Ros.Vector3() {
                                x = obj.velocity.linear.x,
                                y = 0,
                                z = 0,
                            },
                            angular = new Ros.Vector3() {
                                x = 0,
                                y = 0,
                                z = obj.velocity.angular.z,
                            },
                        },
                    };
                    cameraPredictedObjects.Add(obj_converted);
                }
                cameraPredictedVisuals = cameraPredictedObjects.ToList();
                cameraPredictedObjects.Clear();
            });
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
        if (!isEnabled) {
            return;
        }

        // Vector from camera to collider
        Vector3 vectorFromCamToCol = detect.transform.position - groundTruthCamera.transform.position;
        // Vector projected onto camera plane
        Vector3 vectorProjToCamPlane = Vector3.ProjectOnPlane(vectorFromCamToCol, groundTruthCamera.transform.up);
        // Angle in degree between collider and camera forward direction
        var angleHorizon = Vector3.Angle(vectorProjToCamPlane, groundTruthCamera.transform.forward);
        
        // Check if collider is out of field of view
        if (angleHorizon > degHFOV / 2) {
            return;
        }

        string label = "";
        Vector3 size = Vector3.zero;
        if (detect.gameObject.layer == 8 || detect.gameObject.layer == 14 || detect.gameObject.layer == 19) {
            // if Duckiebot, NPC, or NPC Static layer
            label = "car";
            if (detect.GetType() == typeof(BoxCollider)) {
                size.x = ((BoxCollider) detect).size.z;
                size.y = ((BoxCollider) detect).size.x;
                size.z = ((BoxCollider) detect).size.y;
            }
        } else if (detect.gameObject.layer == 18) {
            // if Pedestrian layer
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

        RaycastHit hit;
        var start = groundTruthCamera.transform.position;
        var end = detect.bounds.center;
        var direction = (end - start).normalized;
        var distance = (end - start).magnitude;
        Ray cameraRay = new Ray(start, direction);

        if (Physics.Raycast(cameraRay, out hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) {
            if (hit.collider == detect) {
#if VISUALIZE_RAYCAST
                Debug.DrawRay(start, direction * distance, Color.green);
#endif
                if (!cameraDetectedColliders.ContainsKey(detect)) {
                    Vector3 cen = detect.bounds.center;
                    Vector3 ext = detect.bounds.extents;
                    Vector3[] pts = new Vector3[8] {
                        groundTruthCamera.WorldToViewportPoint(new Vector3(cen.x + ext.x, cen.y + ext.y, cen.z + ext.z)),
                        groundTruthCamera.WorldToViewportPoint(new Vector3(cen.x + ext.x, cen.y + ext.y, cen.z - ext.z)),
                        groundTruthCamera.WorldToViewportPoint(new Vector3(cen.x + ext.x, cen.y - ext.y, cen.z + ext.z)),
                        groundTruthCamera.WorldToViewportPoint(new Vector3(cen.x + ext.x, cen.y - ext.y, cen.z - ext.z)),
                        groundTruthCamera.WorldToViewportPoint(new Vector3(cen.x - ext.x, cen.y + ext.y, cen.z + ext.z)),
                        groundTruthCamera.WorldToViewportPoint(new Vector3(cen.x - ext.x, cen.y + ext.y, cen.z - ext.z)),
                        groundTruthCamera.WorldToViewportPoint(new Vector3(cen.x - ext.x, cen.y - ext.y, cen.z + ext.z)),
                        groundTruthCamera.WorldToViewportPoint(new Vector3(cen.x - ext.x, cen.y - ext.y, cen.z - ext.z))
                    };

                    Vector3 min = pts[0];
                    Vector3 max = pts[0];
                    foreach (Vector3 v in pts)
                    {
                        min = Vector3.Min(min, v);
                        max = Vector3.Max(max, v);
                    }

                    float width = groundTruthCamera.pixelWidth * (max.x - min.x);
                    float height = groundTruthCamera.pixelHeight * (max.y - min.y);
                    float x = (groundTruthCamera.pixelWidth * min.x) + (width / 2f);
                    float y = groundTruthCamera.pixelHeight - ((groundTruthCamera.pixelHeight * min.y) + (height / 2f));

                    if (x - width / 2 < 0) {
                        var offset = Mathf.Abs(x - width / 2);
                        x = x + offset / 2;
                        width = width - offset;
                    }
                    if (x + width / 2 > groundTruthCamera.pixelWidth) {
                        var offset = Mathf.Abs(x + width / 2 - groundTruthCamera.pixelWidth);
                        x = x - offset / 2;
                        width = width - offset;
                    }
                    if (y - height / 2 < 0) {
                        var offset = Mathf.Abs(y - height / 2);
                        y = y + offset / 2;
                        height = height - offset;
                    }
                    if (y + height / 2 > groundTruthCamera.pixelHeight) {
                        var offset = Mathf.Abs(y + height / 2 - groundTruthCamera.pixelHeight);
                        y = y - offset / 2;
                        height = height - offset;
                    }

                    if (width < 0 || height < 0) {
                        return;
                    }

                    // Linear velocity in forward direction of objects, in meters/sec
                    float linear_vel = Vector3.Dot(GetLinVel(detect), detect.transform.forward);
                    // Angular velocity around up axis of objects, in radians/sec
                    float angular_vel = -(GetAngVel(detect)).y;

                    cameraDetectedColliders.Add(detect, new Ros.Detection2D() {
                        header = new Ros.Header() {
                            stamp = Ros.Time.Now(),
                            seq = seqId++,
                            frame_id = groundTruthCamera.name,
                        },
                        id = objId++,
                        label = label,
                        score = 1.0f,
                        bbox = new Ros.BoundingBox2D() {
                            x = (float) x,
                            y = (float) y,
                            width = (float) width,
                            height = (float) height,
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
#if VISUALIZE_RAYCAST
            else Debug.DrawRay(start, direction * distance, Color.red);
#endif
        }
    }

	private void PublishGroundTruth(List<Ros.Detection2D> detectedObjects) {
		if (Bridge == null || Bridge.Status != Ros.Status.Connected) {
            return;
        }

		if (Time.time < nextSend) {
			return;
		}

        if (detectedObjects == null) {
            return;
        }

        if (targetEnv == ROSTargetEnvironment.AUTOWARE || targetEnv == ROSTargetEnvironment.APOLLO) {
            var detectedObjectArrayMsg = new Ros.Detection2DArray() {
                detections = detectedObjects,
            };
            Bridge.Publish(objects2DTopicName, detectedObjectArrayMsg);
            nextSend = Time.time + 1.0f / frequency;
        }
	}

    void Visualize(List<Ros.Detection2D> objects, Camera cam, RenderTextureDisplayer camPreview) {
        if (objects == null || cam == null || camPreview == null) {
            return;
        }

        if (!cam.enabled || !camPreview.gameObject.activeSelf) {
            return;
        }

        foreach (Ros.Detection2D obj in objects) {
            float x = (float) obj.bbox.x;
            float y = (float) obj.bbox.y;
            float width = (float) obj.bbox.width;
            float height = (float) obj.bbox.height;

            Vector3[] corners = new Vector3[4];
            ((RectTransform) camPreview.transform).GetWorldCorners(corners);
            var previewWidth = corners[3].x - corners[0].x;
            var previewHeight = corners[1].y - corners[0].y;

            x = obj.bbox.x / cam.pixelWidth * previewWidth;
            y = obj.bbox.y / cam.pixelHeight * previewHeight;
            width = obj.bbox.width / cam.pixelWidth * previewWidth;
            height = obj.bbox.height / cam.pixelHeight * previewHeight;

            // Top-left corner is (0, 0)
            var x_left = x - width / 2;
            var x_right = x + width / 2;
            var y_up = y - height / 2;
            var y_down = y + height / 2;

            // Crop if box is out of preview
            if (x_left < 0) x_left = 0;
            if (x_right > previewWidth) x_right = previewWidth;
            if (y_up < 0) y_up = 0;
            if (y_down > previewHeight) y_down = previewHeight;

            Vector2 min = new Vector2(corners[0].x + x_left, (Screen.height - corners[1].y) + y_up);
            Vector2 max = new Vector2(corners[0].x + x_right, (Screen.height - corners[1].y) + y_down);

            Rect rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            if (obj.label == "car") {
                GUI.backgroundColor = new Color(0, 1, 0, 0.3f);  // Color.green
            } else if (obj.label == "pedestrian") {
                GUI.backgroundColor = new Color(1, 0.92f, 0.016f, 0.3f);  // Color.yellow
            } else if (obj.label == "bicycle") {
                GUI.backgroundColor = new Color(0, 1, 1, 0.3f);  // Color.cyan
            } else {
                GUI.backgroundColor = new Color(1, 0, 1, 0.3f);  // Color.magenta
            }

            GUI.Box(rect, "", textureStyle);
        }
    }

    public void EnableVisualize(bool enable) {
        isVisualize = enable;
    }

    void OnGUI() {
        if (!isVisualize) return;
        if (isEnabled) {
            Visualize(detectedObjects, groundTruthCamera, cameraPreview);
        }

        if (isCameraPredictionEnabled) {
            Visualize(cameraPredictedVisuals, targetCamera, targetCameraPreview);
        }
    }

    private void AddUIElement(Camera cam) {
        if (targetEnv == ROSTargetEnvironment.AUTOWARE || targetEnv == ROSTargetEnvironment.APOLLO) {
            var groundTruth2DCheckbox = GetComponentInParent<UserInterfaceTweakables>().AddCheckbox("ToggleGroundTruth2D", "Enable Ground Truth 2D:", isEnabled);
            groundTruth2DCheckbox.onValueChanged.AddListener(x => Enable(x));
            cameraPreview = GetComponentInParent<UserInterfaceTweakables>().AddCameraPreview("Ground Truth 2D Camera", "", cam);
        }

        if (targetEnv == ROSTargetEnvironment.AUTOWARE) {
            var cameraPredictionCheckbox = transform.parent.gameObject.GetComponent<UserInterfaceTweakables>().AddCheckbox("ToggleCameraPrediction", "Enable Camera Prediction:", isCameraPredictionEnabled);
            cameraPredictionCheckbox.onValueChanged.AddListener(x => EnableCameraPrediction(x));
        }
    }
}
