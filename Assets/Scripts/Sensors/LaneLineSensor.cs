/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Sensors
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Simulator.Bridge;
    using Simulator.Bridge.Data;
    using Simulator.Utilities;
    using Simulator.Sensors.UI;
    using Simulator.Map;
    using Simulator.Sensors.Postprocessing;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.HighDefinition;

    [SensorType("LaneLineSensor", new[] {typeof(LaneLineData)})]
    [RequireComponent(typeof(Camera))]
    [RequireCustomPass(typeof(LaneLineSensorPass))]
    public class LaneLineSensor : SensorBase
    {
        // Sample JSON
        //{
        //  "type": "LaneLineSensor",
        //  "name": "Lane Line",
        //  "params": {
        //    "Width": 1920,
        //    "Height": 1080,
        //    "Frequency": 10,
        //    "FieldOfView": 60,
        //    "MinDistance": 0.1,
        //    "MaxDistance": 2000,
        //    "DetectionRange": 50,
        //    "SampleDelta": 0.5,
        //    "Topic": "/simulator/lane_line",
        //  },
        //  "transform": {
        //    "x": 0,
        //    "y": 1.7,
        //    "z": -0.2,
        //    "pitch": 0,
        //    "yaw": 0,
        //    "roll": 0
        //  }
        //}

        private static CustomPassVolume volume;

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
        public float MaxDistance = 2000.0f;

        [SensorParameter]
        [Range(0.01f, 2000f)]
        public float DetectionRange = 50f;

        [SensorParameter]
        [Range(0.1f, 2f)]
        public float SampleDelta = 0.5f;

        public readonly List<LaneLineSensorPass.Line> linesToRender = new List<LaneLineSensorPass.Line>();

        private uint SeqId;
        private uint ObjId;
        private float NextCaptureTime;

        RenderTexture ActiveRT;

        private List<MeshCollider> cachedRoadColliders;

        private List<Vector2> LeftLanePoints = new List<Vector2>();
        private List<Vector2> RightLanePoints = new List<Vector2>();

        private LaneLineCubicCurve LeftCurve;
        private LaneLineCubicCurve RightCurve;

        private bool previewEnabled;

        private readonly float[,] coeffMatrix = new float[4, 5];
        private readonly Plane[] frustumPlanes = new Plane[6];

        private Camera sensorCamera;
        private BridgeInstance Bridge;
        private Publisher<LaneLineData> Publish;

        private Camera SensorCamera
        {
            get
            {
                if (sensorCamera == null)
                    sensorCamera = GetComponent<Camera>();

                return sensorCamera;
            }
        }

        public override SensorDistributionType DistributionType => SensorDistributionType.HighLoad;

        private void Start()
        {
            ActiveRT = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
            {
                dimension = TextureDimension.Tex2D,
                antiAliasing = 1,
                useMipMap = false,
                useDynamicScale = false,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            ActiveRT.Create();

            SensorCamera.targetTexture = ActiveRT;
            SensorCamera.fieldOfView = FieldOfView;
            SensorCamera.nearClipPlane = MinDistance;
            SensorCamera.farClipPlane = MaxDistance;

            var hd = SensorCamera.GetComponent<HDAdditionalCameraData>();
            hd.hasPersistentHistory = true;

            cachedRoadColliders = FindObjectsOfType<MeshCollider>().Where(x => x.name.Contains("Road")).ToList();

            NextCaptureTime = Time.time + 1.0f / Frequency;
        }

        public override void OnBridgeSetup(BridgeInstance bridge)
        {
            Bridge = bridge;
            Publish = Bridge.AddPublisher<LaneLineData>(Topic);
        }

        void OnDestroy()
        {
            if (ActiveRT != null)
            {
                ActiveRT.Release();
            }
        }

        private void SamplePointsNew(MapLane egoLane, bool isLeft)
        {
            MapLane currentLane = egoLane;
            var worldPoints = ListPool<Vector3>.Get();
            var distanceTrimmedWorldPoints = ListPool<Vector3>.Get();
            var frustumTrimmedWorldPoints = ListPool<Vector3>.Get();
            var localPoints = ListPool<Vector2>.Get();

            void ReleasePoolItems()
            {
                ListPool<Vector3>.Release(worldPoints);
                ListPool<Vector3>.Release(distanceTrimmedWorldPoints);
                ListPool<Vector3>.Release(frustumTrimmedWorldPoints);
                ListPool<Vector2>.Release(localPoints);
            }

            worldPoints.AddRange(isLeft ? currentLane.leftLineBoundry.mapWorldPositions : currentLane.rightLineBoundry.mapWorldPositions);

            var min = float.MaxValue;
            var minIndex = 0;
            for (var i = 0; i < worldPoints.Count; ++i)
            {
                var sqrMag = (worldPoints[i] - transform.position).sqrMagnitude;
                if (sqrMag < min)
                {
                    min = sqrMag;
                    minIndex = i;
                }
            }

            var dirVec = minIndex < worldPoints.Count - 1
                ? worldPoints[minIndex + 1] - worldPoints[minIndex]
                : worldPoints[minIndex] - worldPoints[minIndex - 1];

            if (Vector3.Dot(dirVec, transform.forward) < 0)
                worldPoints.Reverse();

            int pointIndex = 0;
            var safety = 0;
            var prevZ = -1f;

            while (safety++ < 5000)
            {
                var cameraPos = SensorCamera.transform.InverseTransformPoint(worldPoints[pointIndex]);
                if (prevZ > DetectionRange && cameraPos.z > DetectionRange)
                    break;

                prevZ = cameraPos.z;

                // Point is behind the camera
                if (cameraPos.z < 0 && distanceTrimmedWorldPoints.Count > 0)
                {
                    // Previous is also behind - remove it, only one point behind is needed for interpolation
                    if (prevZ < 0)
                        distanceTrimmedWorldPoints.RemoveAt(distanceTrimmedWorldPoints.Count - 1);
                    // Road is probably curving further away and goes behind the camera - terminate
                    else
                        break;
                }

                distanceTrimmedWorldPoints.Add(worldPoints[pointIndex]);

                pointIndex++;
                if (pointIndex >= worldPoints.Count)
                {
                    if (pointIndex >= worldPoints.Count)
                    {
                        if (currentLane.nextConnectedLanes.Count != 1)
                            break;
                        currentLane = currentLane.nextConnectedLanes[0];
                        if (!currentLane.isTrafficLane)
                            break;

                        var prevEnd = worldPoints[worldPoints.Count - 1];
                        worldPoints.Clear();
                        worldPoints.AddRange(isLeft ? currentLane.leftLineBoundry.mapWorldPositions : currentLane.rightLineBoundry.mapWorldPositions);
                        if ((prevEnd - worldPoints[0]).sqrMagnitude > (prevEnd - worldPoints[worldPoints.Count - 1]).sqrMagnitude)
                            worldPoints.Reverse();

                        pointIndex = 1;
                    }
                }
            }

            if (distanceTrimmedWorldPoints.Count < 2)
            {
                ReleasePoolItems();
                return;
            }

            // Trim detected lines to camera frustum
            ClipLinesToFrustum(distanceTrimmedWorldPoints, frustumTrimmedWorldPoints);

            if (frustumTrimmedWorldPoints.Count < 2)
            {
                ReleasePoolItems();
                return;
            }

            // Move points to local space
            foreach (var point in frustumTrimmedWorldPoints)
            {
                var viewPos = SensorCamera.transform.InverseTransformPoint(point);
                localPoints.Add(new Vector2(viewPos.z, viewPos.x));
            }

            var totalLength = 0f;
            for (var i = 1; i < localPoints.Count; ++i)
                totalLength += Vector2.Distance(localPoints[i - 1], localPoints[i]);

            // Too short to approximate anything usable
            if (totalLength < 0.5f)
            {
                ReleasePoolItems();
                return;
            }

            var step = SampleDelta;
            var targetList = isLeft ? LeftLanePoints : RightLanePoints;
            var startIndex = 0;
            var startDist = 0f;

            // Hard limit for max samples, but will almost always terminate sooner when reaching max distance
            for (var i = 0; i <= 8192; ++i)
            {
                if (startIndex == localPoints.Count - 1)
                {
                    targetList.Add(localPoints[localPoints.Count - 1]);
                    if (targetList.Count >= 2)
                        break;
                }

                var floatVal = i * step;
                var nextDist = startDist + Vector2.Distance(localPoints[startIndex], localPoints[startIndex + 1]);

                while (nextDist < floatVal && ++startIndex < localPoints.Count - 1)
                {
                    startDist = nextDist;
                    nextDist = startDist + Vector2.Distance(localPoints[startIndex], localPoints[startIndex + 1]);
                }

                if (startIndex == localPoints.Count - 1)
                {
                    targetList.Add(localPoints[localPoints.Count - 1]);
                    break;
                }

                var diff = (nextDist - startDist);
                Vector3 pos;
                if (Mathf.Approximately(diff, 0f))
                    pos = localPoints[startIndex];
                else
                {
                    var lVal = (floatVal - startDist) / (nextDist - startDist);
                    pos = Vector2.Lerp(localPoints[startIndex], localPoints[startIndex + 1], lVal);
                }

                targetList.Add(pos);

                if (pos.x >= DetectionRange && targetList.Count >= 2)
                    break;
            }

            ReleasePoolItems();
        }

        private static bool IsInFrustum(Vector3 viewPoint)
        {
            return viewPoint.z > 0 && viewPoint.x >= 0f && viewPoint.y >= 0f && viewPoint.x <= 1f && viewPoint.y <= 1f;
        }

        private void ClipLinesToFrustum(List<Vector3> worldSpaceLines, List<Vector3> clippingResult)
        {
            var cam = SensorCamera;
            GeometryUtility.CalculateFrustumPlanes(cam, frustumPlanes);

            clippingResult.Clear();

            var prev = worldSpaceLines[0];
            var prevInFrustum = IsInFrustum(cam.WorldToViewportPoint(prev));

            if (prevInFrustum)
                clippingResult.Add(prev);

            for (var i = 1; i < worldSpaceLines.Count; ++i)
            {
                var current = worldSpaceLines[i];
                var vector = (current - prev).normalized;
                var dist = Vector3.Distance(prev, current);
                var viewSpace = cam.WorldToViewportPoint(current);
                var inFrustum = IsInFrustum(viewSpace);

                // Both ends out of frustum - skip
                // This line can still pass through frustum, but it's very rare and complex to solve
                if (!prevInFrustum && !inFrustum)
                {
                    prev = current;
                    continue;
                }

                if (!prevInFrustum)
                {
                    var ray = new Ray(current, prev - current);
                    var bestHit = dist + 1f;

                    for (var j = 0; j < frustumPlanes.Length; ++j)
                    {
                        if (frustumPlanes[j].Raycast(ray, out var enter) && enter < dist)
                            bestHit = Mathf.Min(bestHit, enter);
                    }

                    if (bestHit <= dist)
                    {
                        var clippedPrev = current - vector * bestHit;
                        prev = clippedPrev;
                        clippingResult.Add(clippedPrev);
                        clippingResult.Add(current);
                        prevInFrustum = true;
                    }

                    continue;
                }

                if (!inFrustum)
                {
                    var ray = new Ray(prev, current - prev);
                    var bestHit = dist + 1f;

                    for (var j = 0; j < frustumPlanes.Length; ++j)
                    {
                        if (frustumPlanes[j].Raycast(ray, out var enter) && enter < dist)
                            bestHit = Mathf.Min(bestHit, enter);
                    }

                    if (bestHit <= dist)
                    {
                        var clippedCurrent = prev + vector * bestHit;
                        clippingResult.Add(clippedCurrent);
                        // Point is going out of clip space, terminate further search
                        break;
                    }
                }

                clippingResult.Add(current);
                prev = current;
            }
        }

        private Vector4 FitToCubicPolynomial(List<Vector2> points, out float maxX, out float minX)
        {
            maxX = float.MinValue;
            minX = float.MaxValue;

            foreach (var point in points)
            {
                maxX = Mathf.Max(maxX, point.x);
                minX = Mathf.Min(minX, point.x);
            }

            for (var i = 0; i < 4; ++i)
            {
                coeffMatrix[i, 4] = 0f;

                foreach (var point in points)
                    coeffMatrix[i, 4] -= point.y * Mathf.Pow(point.x, i);

                for (var j = 0; j < 4; ++j)
                {
                    coeffMatrix[i, j] = 0f;

                    foreach (var point in points)
                        coeffMatrix[i, j] -= Mathf.Pow(point.x, j + i);
                }
            }

            for (var i = 0; i < 4; ++i)
            {
                if (Mathf.Approximately(coeffMatrix[i, i], 0f))
                {
                    for (var j = i + 1; j < 4; ++j)
                    {
                        if (Mathf.Approximately(coeffMatrix[j, i], 0f))
                            continue;

                        for (var k = i; k < 5; ++k)
                        {
                            var tmp = coeffMatrix[i, k];
                            coeffMatrix[i, k] = coeffMatrix[j, k];
                            coeffMatrix[j, k] = tmp;
                        }

                        break;
                    }
                }

                var ii = coeffMatrix[i, i];
                if (Mathf.Approximately(ii, 0f))
                    throw new Exception("No polynomial solution found.");

                for (var j = i; j < 5; ++j)
                    coeffMatrix[i, j] /= ii;

                for (var j = 0; j < 4; ++j)
                {
                    if (j == i)
                        continue;

                    var ji = coeffMatrix[j, i];

                    for (var k = 0; k < 5; ++k)
                        coeffMatrix[j, k] -= ji * coeffMatrix[i, k];
                }
            }

            var coefficients = new Vector4();

            for (var i = 0; i < 4; ++i)
                coefficients[i] = coeffMatrix[i, 4];

            return coefficients;
        }

        private void Update()
        {
            if (Time.time >= NextCaptureTime)
            {
                NextCaptureTime = Time.time + 1.0f / Frequency;

                LeftLanePoints.Clear();
                RightLanePoints.Clear();
                // TODO: Since this is done in every frame, we may need to optimize GetClosestLane.
                // It currently use brute force. It would be better to use QuadTree or similar data
                // structure to accelerate the search.
                MapLane egoLane = null;
                // Peek in front of car if car is not on valid lane
                for (var i = 0; i < 4; ++i)
                {
                    var trans = transform;
                    egoLane = SimulatorManager.Instance.MapManager.GetClosestLaneAll(trans.position + trans.forward * (i * 2f));
                    if (egoLane.isTrafficLane)
                        break;
                }

                if (egoLane != null && egoLane.isTrafficLane)
                {
                    SamplePointsNew(egoLane, true);
                    SamplePointsNew(egoLane, false);
                }

                var isDataValid = true;

                // Not enough points to calculate the curve - skip sending message
                if (LeftLanePoints.Count < 2 || RightLanePoints.Count < 2)
                {
                    LeftCurve = new LaneLineCubicCurve();
                    RightCurve = new LaneLineCubicCurve();
                    isDataValid = false;
                }
                else
                {
                    var leftCoefficients = FitToCubicPolynomial(LeftLanePoints, out var leftMaxX, out var leftMinX);
                    var rightCoefficients = FitToCubicPolynomial(RightLanePoints, out var rightMaxX, out var rightMinX);

                    LeftCurve = new LaneLineCubicCurve(leftMinX, leftMaxX, leftCoefficients);
                    RightCurve = new LaneLineCubicCurve(rightMinX, rightMaxX, rightCoefficients);
                }

                linesToRender.Clear();

                if (previewEnabled)
                {
                    if (isDataValid)
                    {
                        VisulzeLanePoints(LeftCurve, Color.green);
                        VisulzeLanePoints(RightCurve, Color.blue);
                    }

                    SensorCamera.Render();
                }

                if (Bridge != null && Bridge.Status == Status.Connected && isDataValid)
                {
                    Publish(new LaneLineData
                    {
                        Frame = Frame,
                        Sequence = SeqId,
                        Time = SimulatorManager.Instance.CurrentTime,
                        Type = LaneLineType.WHITE_SOLID,
                        CurveCameraCoord = LeftCurve
                    });

                    Publish(new LaneLineData
                    {
                        Frame = Frame,
                        Sequence = SeqId++,
                        Time = SimulatorManager.Instance.CurrentTime,
                        Type = LaneLineType.WHITE_SOLID,
                        CurveCameraCoord = RightCurve
                    });
                }
            }
        }

        private void VisulzeLanePoints(LaneLineCubicCurve curve, Color color)
        {
            var plane = GetRoadPlane();
            var x = curve.MinX;
            var localPoint = new Vector2(x, curve.Sample(x));
            var pt = SensorCamera.transform.TransformPoint(new Vector3(localPoint.y, 0f, localPoint.x));
            pt = SnapPointToPlane(pt, plane);

            for (var i = curve.MinX + SampleDelta; i <= curve.MaxX + SampleDelta; i += SampleDelta)
            {
                var xPos = Mathf.Min(i, curve.MaxX);
                localPoint = new Vector2(xPos, curve.Sample(xPos));
                var newPt = SensorCamera.transform.TransformPoint(new Vector3(localPoint.y, 0f, localPoint.x));
                newPt = SnapPointToPlane(newPt, plane);
                linesToRender.Add(new LaneLineSensorPass.Line
                {
                    transform = Matrix4x4.identity,
                    start = pt,
                    end = newPt,
                    color = (Vector4) color
                });
                pt = newPt;
            }
        }

        private Plane GetRoadPlane()
        {
            var camTransform = SensorCamera.transform;
            var position = camTransform.position;

            foreach (var col in cachedRoadColliders)
            {
                var bounds = col.bounds;
                if (position.x < bounds.min.x || position.z < bounds.min.z || position.x > bounds.max.x || position.z > bounds.max.z)
                    continue;

                var ray = new Ray(position, Vector3.down);
                if (col.Raycast(ray, out var hitInfo, 100f))
                    return new Plane(hitInfo.normal, hitInfo.point);
            }

            return new Plane(camTransform.up, camTransform.parent.position);
        }

        private Vector3 SnapPointToPlane(Vector3 point, Plane plane)
        {
            var ray = new Ray(point, Vector3.down);
            if (plane.Raycast(ray, out var dist))
                return point + Vector3.down * (dist - 0.1f);

            return point + Vector3.up * 0.1f;
        }

        public override void OnVisualize(Visualizer visualizer)
        {
            if (previewEnabled)
                visualizer.UpdateRenderTexture(SensorCamera.activeTexture, SensorCamera.aspect);
        }

        public override void OnVisualizeToggle(bool state)
        {
            previewEnabled = state;
        }
    }
}