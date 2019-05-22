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
using UnityEngine.UI;

public class GroundTruthSensor2D : MonoBehaviour, Comm.BridgeClient
{
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
    private Comm.Bridge Bridge;
    Comm.Writer<Ros.Detection2DArray> DectectedObjectArrayWriter;
    Comm.Writer<apollo.common.Detection2DArray> Apollo35DetectedObjectArrayWriter;
    private List<Ros.Detection2D> detectedObjects;
    private Dictionary<Collider, Ros.Detection2D> cameraDetectedColliders;
    private bool isEnabled = false;

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

    public GameObject boundingBox;
    private List<GameObject> boundingBoxes = new List<GameObject>();
    private List<GameObject> cameraBoundingBoxes = new List<GameObject>();
    private float previewWidth = -1;
    private float previewHeight = -1;

    private void Awake()
    {
        var videoWidth = 1920;
        var videoHeight = 1080;
        var rtDepth = 24;
        var rtFormat = RenderTextureFormat.ARGB32;
        var rtReadWrite = RenderTextureReadWrite.Linear;

        RenderTexture activeRT = new RenderTexture(videoWidth, videoHeight, rtDepth, rtFormat, rtReadWrite)
        {
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
        textureStyle = new GUIStyle
        {
            normal = new GUIStyleState
            {
                background = backgroundTexture
            }
        };

        if (targetCamera != null)
        {
            targetCameraPreview = targetCamera.GetComponent<VideoToROS>().cameraPreview;
        }
    }

    void OnDestroy()
    {
        groundTruthCamera.targetTexture.Release();
    }

    private void Start()
    {
        nextSend = Time.time + 1.0f / frequency;
    }

    private void Update()
    {
        if (isEnabled && cameraDetectedColliders != null)
        {
            detectedObjects = cameraDetectedColliders.Values.ToList();
            cameraDetectedColliders.Clear();
            objId = 0;

            Visualize(detectedObjects, groundTruthCamera, cameraPreview, boundingBoxes);
            PublishGroundTruth(detectedObjects);
        }

        if (isCameraPredictionEnabled)
        {
            Visualize(cameraPredictedVisuals, targetCamera, targetCameraPreview, cameraBoundingBoxes);
        }
    }

    public void Enable(bool enabled)
    {
        isEnabled = enabled;
        objId = 0;

        groundTruthCamera.enabled = enabled;
        cameraPreview.gameObject.SetActive(enabled);

        detectedObjects?.Clear();
        cameraDetectedColliders?.Clear();
    }

    public void EnableCameraPrediction(bool enabled)
    {
        isCameraPredictionEnabled = enabled;

        cameraPredictedVisuals?.Clear();
        cameraPredictedObjects?.Clear();
    }

    public void GetSensors(List<Component> sensors)
    {
        sensors.Add(this);
    }

    public void OnBridgeAvailable(Comm.Bridge bridge)
    {
        Bridge = bridge;
        Bridge.OnConnected += () =>
        {
            if (targetEnv == ROSTargetEnvironment.AUTOWARE || targetEnv == ROSTargetEnvironment.APOLLO || targetEnv == ROSTargetEnvironment.LGSVL)
            {
                DectectedObjectArrayWriter = Bridge.AddWriter<Ros.Detection2DArray>(objects2DTopicName);
            }

            if (targetEnv == ROSTargetEnvironment.APOLLO35)
            {
                Apollo35DetectedObjectArrayWriter = Bridge.AddWriter<apollo.common.Detection2DArray>(objects2DTopicName);
            }

            if (targetEnv == ROSTargetEnvironment.AUTOWARE)
            {
                Bridge.AddReader<Ros.DetectedObjectArray>(autowareCameraDetectionTopicName, msg =>
                {
                    if (!isCameraPredictionEnabled || cameraPredictedObjects == null)
                    {
                        return;
                    }
                    foreach (Ros.DetectedObject obj in msg.objects)
                    {
                        var label = obj.label;
                        if (label == "person")
                        {
                            label = "pedestrian";  // Autoware label as person
                        }
                        Ros.Detection2D obj_converted = new Ros.Detection2D()
                        {
                            header = new Ros.Header()
                            {
                                stamp = new Ros.Time()
                                {
                                    secs = obj.header.stamp.secs,
                                    nsecs = obj.header.stamp.nsecs,
                                },
                                seq = obj.header.seq,
                                frame_id = obj.header.frame_id,
                            },
                            id = obj.id,
                            label = label,
                            score = obj.score,
                            bbox = new Ros.BoundingBox2D()
                            {
                                x = obj.x + obj.width / 2,  // Autoware (x, y) point at top-left corner
                                y = obj.y + obj.height / 2,
                                width = obj.width,
                                height = obj.height,
                            },
                            velocity = new Ros.Twist()
                            {
                                linear = new Ros.Vector3()
                                {
                                    x = obj.velocity.linear.x,
                                    y = 0,
                                    z = 0,
                                },
                                angular = new Ros.Vector3()
                                {
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
        };
    }

    System.Func<Collider, Vector3> GetLinVel = ((col) =>
    {
        var trafAiMtr = col.GetComponentInParent<TrafAIMotor>();
        if (trafAiMtr != null)
        {
            return trafAiMtr.currentVelocity;
        }
        else
        {
            return col.attachedRigidbody == null ? Vector3.zero : col.attachedRigidbody.velocity;
        }
    });

    System.Func<Collider, Vector3> GetAngVel = ((col) =>
    {
        return col.attachedRigidbody == null ? Vector3.zero : col.attachedRigidbody.angularVelocity;
    });

    private void OnCameraObjectDetected(Collider detect)
    {
        if (!isEnabled)
        {
            return;
        }

        // Vector from camera to collider
        Vector3 vectorFromCamToCol = detect.transform.position - groundTruthCamera.transform.position;
        // Vector projected onto camera plane
        Vector3 vectorProjToCamPlane = Vector3.ProjectOnPlane(vectorFromCamToCol, groundTruthCamera.transform.up);
        // Angle in degree between collider and camera forward direction
        var angleHorizon = Vector3.Angle(vectorProjToCamPlane, groundTruthCamera.transform.forward);

        // Check if collider is out of field of view
        if (angleHorizon > degHFOV / 2)
        {
            return;
        }

        string label = "";
        Vector3 size = Vector3.zero;
        if (detect.gameObject.layer == 28 || detect.gameObject.layer == 14 || detect.gameObject.layer == 19)
        {
            // if GroundTruth (Player), NPC, or NPC Static layer
            label = "car";
            if (detect.GetType() == typeof(BoxCollider))
            {
                size.x = ((BoxCollider)detect).size.z;
                size.y = ((BoxCollider)detect).size.x;
                size.z = ((BoxCollider)detect).size.y;
            }
        }
        else if (detect.gameObject.layer == 18)
        {
            // if Pedestrian layer
            label = "pedestrian";
            if (detect.GetType() == typeof(CapsuleCollider))
            {
                size.x = ((CapsuleCollider)detect).radius;
                size.y = ((CapsuleCollider)detect).radius;
                size.z = ((CapsuleCollider)detect).height;
            }
        }

        if (label == "" || size.magnitude == 0)
        {
            return;
        }

        RaycastHit hit;
        var start = groundTruthCamera.transform.position;
        var end = detect.bounds.center;
        var direction = (end - start).normalized;
        var distance = (end - start).magnitude;
        Ray cameraRay = new Ray(start, direction);

        int layerMask = 1 << 8;
        layerMask = ~layerMask;  // Except duckiebot layer
        if (Physics.Raycast(cameraRay, out hit, distance, layerMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider == detect)
            {
#if VISUALIZE_RAYCAST
                Debug.DrawRay(start, direction * distance, Color.green);
#endif
                if (!cameraDetectedColliders.ContainsKey(detect))
                {
                    Vector3 cen = detect.bounds.center;
                    Vector3 ext = size * 0.5f;
                    ext.Set(ext.y, ext.z, ext.x);

                    Vector3[] pts = new Vector3[8];
                    pts[0] = new Vector3(cen.x + ext.x, cen.y + ext.y, cen.z + ext.z);  // Back top right corner
                    pts[1] = new Vector3(cen.x + ext.x, cen.y + ext.y, cen.z - ext.z);  // Front top right corner
                    pts[2] = new Vector3(cen.x + ext.x, cen.y - ext.y, cen.z + ext.z);  // Back bottom right corner
                    pts[3] = new Vector3(cen.x + ext.x, cen.y - ext.y, cen.z - ext.z);  // Front bottom right corner
                    pts[4] = new Vector3(cen.x - ext.x, cen.y + ext.y, cen.z + ext.z);  // Back top left corner
                    pts[5] = new Vector3(cen.x - ext.x, cen.y + ext.y, cen.z - ext.z);  // Front top left corner
                    pts[6] = new Vector3(cen.x - ext.x, cen.y - ext.y, cen.z + ext.z);  // Back bottom left corner
                    pts[7] = new Vector3(cen.x - ext.x, cen.y - ext.y, cen.z - ext.z);  // Front bottom left corner

                    for (int i = 0; i < 8; i++)
                    {
                        pts[i] = detect.transform.rotation * (pts[i] - cen) + cen;  // Rotate bounds around center in local space
                        pts[i] = groundTruthCamera.WorldToViewportPoint(pts[i]);  // Convert world space to camera viewport
                    }

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

                    if (x - width / 2 < 0)
                    {
                        var offset = Mathf.Abs(x - width / 2);
                        x = x + offset / 2;
                        width = width - offset;
                    }
                    if (x + width / 2 > groundTruthCamera.pixelWidth)
                    {
                        var offset = Mathf.Abs(x + width / 2 - groundTruthCamera.pixelWidth);
                        x = x - offset / 2;
                        width = width - offset;
                    }
                    if (y - height / 2 < 0)
                    {
                        var offset = Mathf.Abs(y - height / 2);
                        y = y + offset / 2;
                        height = height - offset;
                    }
                    if (y + height / 2 > groundTruthCamera.pixelHeight)
                    {
                        var offset = Mathf.Abs(y + height / 2 - groundTruthCamera.pixelHeight);
                        y = y - offset / 2;
                        height = height - offset;
                    }

                    if (width < 0 || height < 0)
                    {
                        return;
                    }

                    // Linear velocity in forward direction of objects, in meters/sec
                    float linear_vel = Vector3.Dot(GetLinVel(detect), detect.transform.forward);
                    // Angular velocity around up axis of objects, in radians/sec
                    float angular_vel = -(GetAngVel(detect)).y;

                    cameraDetectedColliders.Add(detect, new Ros.Detection2D()
                    {
                        header = new Ros.Header()
                        {
                            stamp = Ros.Time.Now(),
                            seq = seqId++,
                            frame_id = targetCamera.name,
                        },
                        id = objId++,
                        label = label,
                        score = 1.0f,
                        bbox = new Ros.BoundingBox2D()
                        {
                            x = (float)x,
                            y = (float)y,
                            width = (float)width,
                            height = (float)height,
                        },
                        velocity = new Ros.Twist()
                        {
                            linear = new Ros.Vector3()
                            {
                                x = linear_vel,
                                y = 0,
                                z = 0,
                            },
                            angular = new Ros.Vector3()
                            {
                                x = 0,
                                y = 0,
                                z = angular_vel,
                            },
                        }
                    });
                }
#if VISUALIZE_RAYCAST
                else
                {
                    Vector3 cen = detect.bounds.center;
                    Vector3 ext = size * 0.5f;
                    ext.Set(ext.y, ext.z, ext.x);

                    Vector3[] pts = new Vector3[8];
                    pts[0] = new Vector3(cen.x + ext.x, cen.y + ext.y, cen.z + ext.z);  // Back top right corner
                    pts[1] = new Vector3(cen.x + ext.x, cen.y + ext.y, cen.z - ext.z);  // Front top right corner
                    pts[2] = new Vector3(cen.x + ext.x, cen.y - ext.y, cen.z + ext.z);  // Back bottom right corner
                    pts[3] = new Vector3(cen.x + ext.x, cen.y - ext.y, cen.z - ext.z);  // Front bottom right corner
                    pts[4] = new Vector3(cen.x - ext.x, cen.y + ext.y, cen.z + ext.z);  // Back top left corner
                    pts[5] = new Vector3(cen.x - ext.x, cen.y + ext.y, cen.z - ext.z);  // Front top left corner
                    pts[6] = new Vector3(cen.x - ext.x, cen.y - ext.y, cen.z + ext.z);  // Back bottom left corner
                    pts[7] = new Vector3(cen.x - ext.x, cen.y - ext.y, cen.z - ext.z);  // Front bottom left corner

                    for (int i = 0; i < 8; i++)
                    {
                        pts[i] = detect.transform.rotation * (pts[i] - cen) + cen;  // Rotate bounds around center in local space
                    }

                    Debug.DrawLine(pts[5], pts[1], Color.green);
                    Debug.DrawLine(pts[1], pts[3], Color.green);
                    Debug.DrawLine(pts[3], pts[7], Color.green);
                    Debug.DrawLine(pts[7], pts[5], Color.green);

                    Debug.DrawLine(pts[4], pts[0], Color.green);
                    Debug.DrawLine(pts[0], pts[2], Color.green);
                    Debug.DrawLine(pts[2], pts[6], Color.green);
                    Debug.DrawLine(pts[6], pts[4], Color.green);

                    Debug.DrawLine(pts[5], pts[4], Color.green);
                    Debug.DrawLine(pts[1], pts[0], Color.green);
                    Debug.DrawLine(pts[3], pts[2], Color.green);
                    Debug.DrawLine(pts[7], pts[6], Color.green);
                }
#endif
            }
#if VISUALIZE_RAYCAST
            else Debug.DrawRay(start, direction * distance, Color.red);
#endif
        }
    }

    private void PublishGroundTruth(List<Ros.Detection2D> detectedObjects)
    {
        if (Bridge == null || Bridge.Status != Comm.BridgeStatus.Connected)
        {
            return;
        }

        if (Time.time < nextSend)
        {
            return;
        }

        if (detectedObjects == null)
        {
            return;
        }

        if (targetEnv == ROSTargetEnvironment.AUTOWARE || targetEnv == ROSTargetEnvironment.APOLLO || targetEnv == ROSTargetEnvironment.LGSVL)
        {
            var detectedObjectArrayMsg = new Ros.Detection2DArray()
            {
                header = new Ros.Header()
                {
                    stamp = Ros.Time.Now(),
                },
                detections = detectedObjects,
            };
            DectectedObjectArrayWriter.Publish(detectedObjectArrayMsg);
            nextSend = Time.time + 1.0f / frequency;
        }

        if (targetEnv == ROSTargetEnvironment.APOLLO35)
        {
            apollo.common.Detection2DArray cyberDetectionObjectArray = new apollo.common.Detection2DArray();
            foreach (Ros.Detection2D rosDetection2D in detectedObjects)
            {
                apollo.common.Detection2D cyberDetection2D = new apollo.common.Detection2D()
                {
                    header = new apollo.common.Header()
                    {
                        sequence_num = rosDetection2D.header.seq,
                        frame_id = rosDetection2D.header.frame_id,
                        timestamp_sec = (double)rosDetection2D.header.stamp.secs,
                    },
                    id = rosDetection2D.id,
                    label = rosDetection2D.label,
                    score = rosDetection2D.score,
                    bbox = new apollo.common.BoundingBox2D()
                    {
                        x = rosDetection2D.bbox.x,
                        y = rosDetection2D.bbox.y,
                        height = rosDetection2D.bbox.height,
                        width = rosDetection2D.bbox.width,
                    },
                    velocity = new apollo.common.Twist()
                    {
                        linear = new apollo.common.Vector3()
                        {
                            x = rosDetection2D.velocity.linear.x,
                            y = rosDetection2D.velocity.linear.y,
                            z = rosDetection2D.velocity.linear.z,
                        },
                        angular = new apollo.common.Vector3()
                        {
                            x = rosDetection2D.velocity.angular.x,
                            y = rosDetection2D.velocity.angular.y,
                            z = rosDetection2D.velocity.angular.z,
                        },
                    },
                };
                cyberDetectionObjectArray.detections.Add(cyberDetection2D);
            }
            System.DateTime Unixepoch = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
            double measurement_time = (double)(System.DateTime.UtcNow - Unixepoch).TotalSeconds;
            cyberDetectionObjectArray.header = new apollo.common.Header()
            {
                timestamp_sec = measurement_time,
            };

            Apollo35DetectedObjectArrayWriter.Publish(cyberDetectionObjectArray);
            nextSend = Time.time + 1.0f / frequency;
        }
    }

    void Visualize(List<Ros.Detection2D> objects, Camera cam, RenderTextureDisplayer camPreview, List<GameObject> boundingBoxes)
    {
        if (objects == null || cam == null || camPreview == null)
        {
            return;
        }

        if (!cam.enabled || !camPreview.gameObject.activeInHierarchy)
        {
            return;
        }

        foreach (GameObject bbox in boundingBoxes)
        {
            Destroy(bbox);
        }
        boundingBoxes.Clear();

        foreach (Ros.Detection2D obj in objects)
        {
            float x = (float)obj.bbox.x;
            float y = (float)obj.bbox.y;
            float width = (float)obj.bbox.width;
            float height = (float)obj.bbox.height;

            if (previewWidth == -1 || previewHeight == -1)
            {
                previewWidth = camPreview.GetComponent<RectTransform>().sizeDelta.x;
                previewHeight = camPreview.GetComponent<RectTransform>().sizeDelta.y;
            }

            x = obj.bbox.x / cam.pixelWidth * previewWidth;
            y = obj.bbox.y / cam.pixelHeight * previewHeight;
            width = obj.bbox.width / cam.pixelWidth * previewWidth;
            height = obj.bbox.height / cam.pixelHeight * previewHeight;

            GameObject bbox = Instantiate(boundingBox, camPreview.transform);

            RectTransform rt = bbox.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector3(x, -y, 0);
            rt.sizeDelta = new Vector3(width, height);
            rt.localScale = Vector3.one;

            Image image = bbox.GetComponent<Image>();
            switch (obj.label)
            {
                case "car":
                    image.color = new Color(0, 1, 0, 0.1f);  // Color.green
                    break;
                case "pedestrian":
                    image.color = new Color(1, 0.92f, 0.016f, 0.1f);  // Color.yellow
                    break;
                case "bicycle":
                    image.color = new Color(0, 1, 1, 0.1f);  // Color.cyan
                    break;
                default:
                    image.color = new Color(1, 0, 1, 0.1f);  // Color.magenta
                    break;
            }

            bbox.SetActive(true);
            Destroy(bbox, Time.deltaTime);
            boundingBoxes.Add(bbox);
        }
    }

    public void EnableVisualize(bool enable)
    {
        isVisualize = enable;
    }

    private void AddUIElement(Camera cam)
    {
        if (targetEnv == ROSTargetEnvironment.AUTOWARE || targetEnv == ROSTargetEnvironment.APOLLO || targetEnv == ROSTargetEnvironment.LGSVL || targetEnv == ROSTargetEnvironment.APOLLO35)
        {
            var groundTruth2DCheckbox = GetComponentInParent<UserInterfaceTweakables>().AddCheckbox("ToggleGroundTruth2D", "Enable Ground Truth 2D:", isEnabled);
            groundTruth2DCheckbox.onValueChanged.AddListener(x => Enable(x));
            cameraPreview = GetComponentInParent<UserInterfaceTweakables>().AddCameraPreview("Ground Truth 2D Camera", "", cam);
        }

        if (targetEnv == ROSTargetEnvironment.AUTOWARE)
        {
            var cameraPredictionCheckbox = transform.parent.gameObject.GetComponent<UserInterfaceTweakables>().AddCheckbox("ToggleCameraPrediction", "Enable Camera Prediction:", isCameraPredictionEnabled);
            cameraPredictionCheckbox.onValueChanged.AddListener(x => EnableCameraPrediction(x));
        }
    }
}
