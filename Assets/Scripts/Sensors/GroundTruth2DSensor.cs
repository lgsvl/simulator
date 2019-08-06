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
    [SensorType("2D Ground Truth", new[] { typeof(Detected2DObjectData) })]
    public class GroundTruth2DSensor : SensorBase
    {
        [SensorParameter]
        [Range(1f, 100f)]
        public float Frequency = 10.0f;

        [SensorParameter]
        [Range(1, 1920)]
        public int Width = 1920;

        [SensorParameter]
        [Range(1, 1080)]
        public int Height = 1080;

        [SensorParameter]
        [Range(1.0f, 90.0f)]
        public float FieldOfView = 60.0f;

        [SensorParameter]
        [Range(0.01f, 1000.0f)]
        public float MinDistance = 0.1f;

        [SensorParameter]
        [Range(0.01f, 2000.0f)]
        public float MaxDistance = 1000.0f;

        public RangeTrigger cameraRangeTrigger;

        [SensorParameter]
        [Range(0.01f, 2000f)]
        public float DetectionRange = 100f;

        public Camera Camera;

        private uint seqId;
        private uint objId;
        private float nextSend;

        RenderTexture activeRT;

        private Dictionary<Collider, Detected2DObject> Detected = new Dictionary<Collider, Detected2DObject>();

        AAWireBox AAWireBoxes;

        private float degHFOV;  // Horizontal Field of View, in degree

        private IBridge Bridge;
        private IWriter<Detected2DObjectData> Writer;

        private void Start()
        {
            activeRT = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
            {
                dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
                antiAliasing = 1,
                useMipMap = false,
                useDynamicScale = false,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            activeRT.Create();

            Camera = GetComponentInChildren<Camera>();
            Camera.targetTexture = activeRT;
            Camera.fieldOfView = FieldOfView;
            Camera.nearClipPlane = MinDistance;
            Camera.farClipPlane = MaxDistance;

            AAWireBoxes = gameObject.AddComponent<AAWireBox>();
            AAWireBoxes.Camera = Camera;

            nextSend = Time.time + 1.0f / Frequency;

            var radHFOV = 2 * Mathf.Atan(Mathf.Tan(Camera.fieldOfView * Mathf.Deg2Rad / 2) * Camera.aspect);
            degHFOV = Mathf.Rad2Deg * radHFOV;

            BoxCollider camBoxCollider = cameraRangeTrigger.GetComponent<BoxCollider>();
            camBoxCollider.center = new Vector3(0, 0, DetectionRange / 2f);
            camBoxCollider.size = new Vector3(2 * Mathf.Tan(radHFOV / 2) * DetectionRange, 3f, DetectionRange);

            cameraRangeTrigger.SetCallbacks(OnCollider, OnCollider, OnColliderExit);
        }

        public override void OnBridgeSetup(IBridge bridge)
        {
            Bridge = bridge;
            Writer = Bridge.AddWriter<Detected2DObjectData>(Topic);
        }

        void OnDestroy()
        {
            activeRT.Release();
        }

        private void Update()
        {
            foreach (var v in Detected)
            {
                var collider = v.Key;
                var box = v.Value;

                var min = box.Position - box.Scale / 2;
                var max = box.Position + box.Scale / 2;

                Color color = Color.magenta;
                if (v.Value.Label == "Car")
                {
                    color = Color.green;
                }
                else if (v.Value.Label == "Pedestrian")
                {
                    color = Color.yellow;
                }
                else if (v.Value.Label == "bicycle")
                {
                    color = Color.cyan;
                }

                AAWireBoxes.Draw(min, max, color);
            }

            if (Bridge == null || Bridge.Status != Status.Connected)
            {
                return;
            }

            if (Time.time < nextSend)
            {
                return;
            }

            Writer.Write(new Detected2DObjectData()
            {
                Sequence = seqId++,
                Frame = Frame,
                Data = Detected.Values.ToArray(),
            });
        }

        Vector4 CalculateDetectedRect(Vector3 cen, Vector3 ext, Quaternion rotation)
        {
            ext.Set(ext.y, ext.z, ext.x);

            var pts = new[]
            {
                new Vector3(cen.x + ext.x, cen.y + ext.y, cen.z + ext.z),  // Back top right corner
                new Vector3(cen.x + ext.x, cen.y + ext.y, cen.z - ext.z),  // Front top right corner
                new Vector3(cen.x + ext.x, cen.y - ext.y, cen.z + ext.z),  // Back bottom right corner
                new Vector3(cen.x + ext.x, cen.y - ext.y, cen.z - ext.z),  // Front bottom right corner
                new Vector3(cen.x - ext.x, cen.y + ext.y, cen.z + ext.z),  // Back top left corner
                new Vector3(cen.x - ext.x, cen.y + ext.y, cen.z - ext.z),  // Front top left corner
                new Vector3(cen.x - ext.x, cen.y - ext.y, cen.z + ext.z),  // Back bottom left corner
                new Vector3(cen.x - ext.x, cen.y - ext.y, cen.z - ext.z),  // Front bottom left corner
            };

            for (int i = 0; i < pts.Length; i++)
            {
                pts[i] = rotation * (pts[i] - cen) + cen;  // Rotate bounds around center in local space
                pts[i] = Camera.WorldToViewportPoint(pts[i]);  // Convert world space to camera viewport
            }

            var min = pts[0];
            var max = pts[0];
            foreach (Vector3 v in pts)
            {
                min = Vector3.Min(min, v);
                max = Vector3.Max(max, v);
            }

            float width = Camera.pixelWidth * (max.x - min.x);
            float height = Camera.pixelHeight * (max.y - min.y);
            float x = (Camera.pixelWidth * min.x) + (width / 2f);
            float y = Camera.pixelHeight - ((Camera.pixelHeight * min.y) + (height / 2f));

            if (x - width / 2 < 0)
            {
                var offset = Mathf.Abs(x - width / 2);
                x = x + offset / 2;
                width = width - offset;
            }

            if (x + width / 2 > Camera.pixelWidth)
            {
                var offset = Mathf.Abs(x + width / 2 - Camera.pixelWidth);
                x = x - offset / 2;
                width = width - offset;
            }

            if (y - height / 2 < 0)
            {
                var offset = Mathf.Abs(y - height / 2);
                y = y + offset / 2;
                height = height - offset;
            }

            if (y + height / 2 > Camera.pixelHeight)
            {
                var offset = Mathf.Abs(y + height / 2 - Camera.pixelHeight);
                y = y - offset / 2;
                height = height - offset;
            }

            return new Vector4(x, y, width, height);
        }

        void OnCollider(Collider other)
        {
            if (Detected.ContainsKey(other))
            {
                Detected.Remove(other);
            }

            if (other.isTrigger)
            {
                return;
            }

            // Vector from camera to collider
            Vector3 vectorFromCamToCol = other.transform.position - Camera.transform.position;
            // Vector projected onto camera plane
            Vector3 vectorProjToCamPlane = Vector3.ProjectOnPlane(vectorFromCamToCol, Camera.transform.up);
            // Angle in degree between collider and camera forward direction
            var angleHorizon = Vector3.Angle(vectorProjToCamPlane, Camera.transform.forward);

            // Check if collider is out of field of view
            if (angleHorizon > degHFOV / 2)
            {
                return;
            }

            Vector3 size = Vector3.zero;
            if (other is BoxCollider)
            {
                var box = other as BoxCollider;
                size.x = box.size.z;
                size.y = box.size.x;
                size.z = box.size.y;
            }
            else if (other is CapsuleCollider)
            {
                var capsule = other as CapsuleCollider;
                size.x = capsule.radius * 2;
                size.y = capsule.radius * 2;
                size.z = capsule.height;
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

            RaycastHit hit;
            var start = Camera.transform.position;
            var end = other.bounds.center;
            var direction = (end - start).normalized;
            var distance = (end - start).magnitude;
            Ray cameraRay = new Ray(start, direction);

            if (Physics.Raycast(cameraRay, out hit, distance, ~LayerMask.GetMask("Agent"), QueryTriggerInteraction.Ignore))
            {
                if (hit.collider == other)
                {
                    Vector4 detectedRect = CalculateDetectedRect(other.bounds.center, size * 0.5f, other.transform.rotation);

                    if (detectedRect.z < 0 || detectedRect.w < 0)
                    {
                        return;
                    }

                    // Linear velocity in forward direction of objects, in meters/sec
                    float linear_vel = Vector3.Dot(other.attachedRigidbody == null ? Vector3.zero : other.attachedRigidbody.velocity, other.transform.forward);
                    // Angular velocity around up axis of objects, in radians/sec
                    float angular_vel = -(other.attachedRigidbody == null ? Vector3.zero : other.attachedRigidbody.angularVelocity).y;

                    Detected.Add(other, new Detected2DObject()
                    {
                        Id = objId++,
                        Label = label,
                        Score = 1.0f,
                        Position = new Vector2(detectedRect.x, detectedRect.y),
                        Scale = new Vector2(detectedRect.z, detectedRect.w),
                        LinearVelocity = new Vector3(linear_vel, 0, 0),
                        AngularVelocity = new Vector3(0, 0, angular_vel),
                    });
                }
            }
        }

        void OnColliderExit(Collider other)
        {
            if (Detected.ContainsKey(other))
            {
                Detected.Remove(other);
            }
        }
    }
}
