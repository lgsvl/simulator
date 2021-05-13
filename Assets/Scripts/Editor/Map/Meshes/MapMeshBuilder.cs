/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.MapMeshes
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Map;
    using Map.LineDetection;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.Rendering;

    public class MapMeshBuilder
    {
        private class LaneBoundEnumerator : IEnumerable<LineVert>, IEnumerator<LineVert>
        {
            private readonly Dictionary<MapLine, LineData> linesData;
            private int cIndex;
            private MapTrafficLane cLane;

            public CornerMask CurrentMask
            {
                get
                {
                    var isLeft = cIndex == 0 || cIndex == 1;
                    var current = Current;
                    if (current == null)
                        throw new Exception("Invalid lane corner enumeration.");

                    var laneStart = cLane.transform.TransformPoint(cLane.mapLocalPositions[0]);
                    var laneEnd = cLane.transform.TransformPoint(cLane.mapLocalPositions[cLane.mapLocalPositions.Count - 1]);

                    var isStart = Vector3.Distance(current.position, laneStart) < Vector3.Distance(current.position, laneEnd);
                    return new CornerMask(!isLeft, isStart);
                }
            }

            public LaneBoundEnumerator(Dictionary<MapLine, LineData> linesData)
            {
                this.linesData = linesData;
            }

            public IEnumerable<LineVert> Enumerate(MapTrafficLane lane)
            {
                cIndex = -1;
                cLane = lane;
                return this;
            }

            public IEnumerator<LineVert> GetEnumerator()
            {
                return this;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public bool MoveNext()
            {
                if (cIndex == 3)
                    return false;

                cIndex++;
                return true;
            }

            public void Reset()
            {
                cIndex = -1;
            }

            public LineVert Current
            {
                get
                {
                    switch (cIndex)
                    {
                        case 0:
                            return linesData[cLane.leftLineBoundry].worldPoints[0];
                        case 1:
                        {
                            var points = linesData[cLane.leftLineBoundry].worldPoints;
                            return points[points.Count - 1];
                        }
                        case 2:
                            return linesData[cLane.rightLineBoundry].worldPoints[0];
                        case 3:
                        {
                            var points = linesData[cLane.rightLineBoundry].worldPoints;
                            return points[points.Count - 1];
                        }
                        default:
                            return null;
                    }
                }
            }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }

        private readonly struct CornerMask
        {
            private readonly bool isRight;
            private readonly bool isStart;

            public CornerMask(bool right, bool start)
            {
                isRight = right;
                isStart = start;
            }

            public bool Connects(CornerMask cm, bool direct)
            {
                return direct ? ConnectsDirect(cm) : ConnectsParallel(cm);
            }

            private bool ConnectsDirect(CornerMask cm)
            {
                return isRight == cm.isRight && isStart != cm.isStart;
            }

            private bool ConnectsParallel(CornerMask cm)
            {
                return isRight == cm.isRight && isStart == cm.isStart;
            }
        }

        private class LineVert
        {
            public Vector3 position;
            public Vector3 outVector;

            private List<LineVert> linked = new List<LineVert>();

            public IReadOnlyList<LineVert> Linked => linked;

            public LineVert(Vector3 position)
            {
                this.position = position;
            }

            public void AddLink(LineVert linkedVec)
            {
                if (linkedVec == this)
                    return;

                if (!linked.Contains(linkedVec))
                    linked.Add(linkedVec);
            }

            public void AddLinks(List<LineVert> linkedVecs)
            {
                foreach (var linkedVec in linkedVecs)
                    AddLink(linkedVec);
            }
        }

        private class LineData
        {
            public enum LineShape
            {
                None,
                Solid,
                Dotted,
                Double
            }

            public int usageCount;
            public readonly Color color;
            public readonly LineShape shape;
            public readonly List<LineVert> worldPoints = new List<LineVert>();
            public readonly List<(Mesh mesh, Matrix4x4 matrix)> meshData = new List<(Mesh mesh, Matrix4x4 matrix)>();

            public LineData(MapLine line, List<Vector3> positionsOverride = null)
            {
                if (positionsOverride != null)
                {
                    foreach (var pos in positionsOverride)
                        worldPoints.Add(new LineVert(pos));
                }
                else
                {
                    foreach (var pos in line.mapLocalPositions)
                        worldPoints.Add(new LineVert(line.transform.TransformPoint(pos)));
                }

                switch (line.lineType)
                {
                    case MapData.LineType.UNKNOWN:
                    case MapData.LineType.VIRTUAL:
                        shape = LineShape.None;
                        break;
                    case MapData.LineType.CURB:
                    case MapData.LineType.STOP:
                        color = Color.white;
                        shape = LineShape.Solid;
                        break;
                    case MapData.LineType.SOLID_WHITE:
                        color = Color.white;
                        shape = LineShape.Solid;
                        break;
                    case MapData.LineType.SOLID_YELLOW:
                        color = Color.yellow;
                        shape = LineShape.Solid;
                        break;
                    case MapData.LineType.DOTTED_WHITE:
                        color = Color.white;
                        shape = LineShape.Dotted;
                        break;
                    case MapData.LineType.DOTTED_YELLOW:
                        color = Color.yellow;
                        shape = LineShape.Dotted;
                        break;
                    case MapData.LineType.DOUBLE_WHITE:
                        color = Color.white;
                        shape = LineShape.Double;
                        break;
                    case MapData.LineType.DOUBLE_YELLOW:
                        color = Color.yellow;
                        shape = LineShape.Double;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private readonly MapMeshSettings settings;
        private readonly Dictionary<MapLine, LineData> linesData = new Dictionary<MapLine, LineData>();

        public MapMeshBuilder(MapMeshSettings settings)
        {
            this.settings = settings;
        }

        public void BuildMesh(GameObject parentObject, MapMeshMaterials materials)
        {
            BuildMesh(parentObject, materials, false);
        }

        public void BuildLinesMesh(GameObject parentObject, MapMeshMaterials materials, LaneLineOverride lineOverride = null)
        {
            BuildMesh(parentObject, materials, true, lineOverride);
        }

        private void BuildMesh(GameObject parentObject, MapMeshMaterials materials, bool linesOnly, LaneLineOverride lineOverride = null)
        {
            try
            {
                EditorUtility.DisplayProgressBar("Building mesh", "Preprocessing HD map data...", 0f);

                var mapManagerData = new MapManagerData();
                var lanes = mapManagerData.GetTrafficLanes();
                var allLanes = new List<MapTrafficLane>();
                allLanes.AddRange(lanes);
                var intersections = mapManagerData.GetIntersections();

                foreach (var intersection in intersections)
                {
                    var intLanes = intersection.GetComponentsInChildren<MapTrafficLane>();
                    foreach (var intLane in intLanes)
                    {
                        allLanes.Add(intLane);
                        if (!linesData.ContainsKey(intLane.leftLineBoundry))
                            linesData[intLane.leftLineBoundry] = new LineData(intLane.leftLineBoundry);

                        if (!linesData.ContainsKey(intLane.rightLineBoundry))
                            linesData[intLane.rightLineBoundry] = new LineData(intLane.rightLineBoundry);
                    }

                    var intLines = intersection.GetComponentsInChildren<MapLine>();
                    foreach (var intLine in intLines)
                    {
                        if (!linesData.ContainsKey(intLine))
                            linesData[intLine] = new LineData(intLine) {usageCount = 1};
                    }
                }

                foreach (var lane in allLanes)
                {
                    // Count boundary usage
                    if (linesData.ContainsKey(lane.leftLineBoundry))
                        linesData[lane.leftLineBoundry].usageCount++;
                    else
                    {
                        var leftOverride = lineOverride == null ? null : lineOverride.GetLaneLineData(lane, true);
                        linesData[lane.leftLineBoundry] = new LineData(lane.leftLineBoundry, leftOverride) {usageCount = 1};
                    }

                    if (linesData.ContainsKey(lane.rightLineBoundry))
                        linesData[lane.rightLineBoundry].usageCount++;
                    else
                    {
                        var rightOverride = lineOverride == null ? null : lineOverride.GetLaneLineData(lane, false);
                        linesData[lane.rightLineBoundry] = new LineData(lane.rightLineBoundry, rightOverride) {usageCount = 1};
                    }
                }

                if (settings.snapLaneEnds)
                    SnapLanes(allLanes);
                
                if (linesOnly)
                {
                    var roadsData =
                        UnityEngine.Object.FindObjectsOfType<MeshCollider>()
                            .Where(x => x.name.Contains("Road"))
                            .Select(road => (road.sharedMesh, road.transform.localToWorldMatrix))
                            .ToList();

                    foreach (var kvp in linesData)
                        kvp.Value.meshData.AddRange(roadsData);

                    var doneLineCount = 0;
                    foreach (var lineData in linesData)
                    {
                        EditorUtility.DisplayProgressBar("Building mesh", $"Creating lane lines ({doneLineCount}/{linesData.Count})", (float) doneLineCount++ / linesData.Count);
                        CreateLineMesh(lineData, parentObject, materials);
                    }
                }
                else
                {
                    if (settings.pushOuterVerts)
                        CalculateOutVectors(allLanes);

                    for (var i = 0; i < lanes.Count; ++i)
                    {
                        EditorUtility.DisplayProgressBar("Building mesh", $"Creating lanes ({i}/{lanes.Count})", (float) i / lanes.Count);
                        CreateLaneMesh(lanes[i], parentObject, materials);
                    }

                    for (var i = 0; i < intersections.Count; ++i)
                    {
                        EditorUtility.DisplayProgressBar("Building mesh", $"Creating intersections ({i}/{intersections.Count})", (float) i / intersections.Count);
                        CreateIntersectionMesh(intersections[i], parentObject, materials);
                    }

                    if (settings.createRenderers)
                    {
                        var doneLineCount = 0;
                        foreach (var lineData in linesData)
                        {
                            EditorUtility.DisplayProgressBar("Building mesh", $"Creating lane lines ({doneLineCount}/{linesData.Count})", (float) doneLineCount++ / linesData.Count);
                            CreateLineMesh(lineData, parentObject, materials);
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                EditorSceneManager.MarkAllScenesDirty();
            }
        }

        private List<List<Vertex>> CreateLanePoly(MapTrafficLane lane, out List<List<Vertex>> pushedMesh)
        {
            var name = lane.gameObject.name;
            var poly = BuildLanePoly(lane, out var roadsideMesh, true);
            pushedMesh = roadsideMesh;
            return MeshUtils.OptimizePoly(poly, name);
        }

        private void CreateLaneMesh(MapTrafficLane lane, GameObject parentObject, MapMeshMaterials materials)
        {
            var name = lane.gameObject.name;
            var poly = CreateLanePoly(lane, out var pushedMesh);
            var mesh = poly == null ? null : Triangulation.TiangulateMultiPolygon(poly, name);

            if (mesh == null)
            {
                Debug.LogWarning($"Zero surface mesh detected, skipping. ({name})");
                return;
            }

            AddUv(mesh);
            var laneTransform = lane.transform;
            MoveMeshVericesToLocalSpace(mesh, laneTransform);

            if (mesh == null)
                return;

            var go = new GameObject(name + "_mesh");
            go.transform.SetParent(parentObject.transform);
            go.transform.rotation = laneTransform.rotation;
            go.transform.position = laneTransform.position;

            var localToWorldMatrix = go.transform.localToWorldMatrix;
            linesData[lane.leftLineBoundry].meshData.Add((mesh, localToWorldMatrix));
            linesData[lane.rightLineBoundry].meshData.Add((mesh, localToWorldMatrix));

            if (settings.createRenderers)
            {
                var mf = go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                mf.sharedMesh = mesh;
                mr.sharedMaterial = materials.road;
            }

            if (settings.createCollider)
            {
                var mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = mesh;
            }

            if (settings.separateOuterMesh && pushedMesh != null)
            {
                var roadsideMesh = Triangulation.TiangulateMultiPolygon(pushedMesh, $"{name} - roadside");
                AddUv(roadsideMesh);
                MoveMeshVericesToLocalSpace(roadsideMesh, laneTransform);

                var roadsideGo = new GameObject(name + "_mesh_roadside");
                roadsideGo.transform.SetParent(parentObject.transform);
                roadsideGo.transform.rotation = laneTransform.rotation;
                roadsideGo.transform.position = laneTransform.position;

                var roadsideLocalToWorldMatrix = roadsideGo.transform.localToWorldMatrix;
                linesData[lane.leftLineBoundry].meshData.Add((roadsideMesh, roadsideLocalToWorldMatrix));
                linesData[lane.rightLineBoundry].meshData.Add((roadsideMesh, roadsideLocalToWorldMatrix));
                
                if (settings.createRenderers)
                {
                    var mf = roadsideGo.AddComponent<MeshFilter>();
                    var mr = roadsideGo.AddComponent<MeshRenderer>();
                    mf.sharedMesh = roadsideMesh;
                    mr.sharedMaterial = materials.road;
                }

                if (settings.createCollider)
                {
                    var mc = roadsideGo.AddComponent<MeshCollider>();
                    mc.sharedMesh = roadsideMesh;
                }
            }
        }

        private List<List<Vertex>> CreateIntersectionPoly(MapIntersection intersection)
        {
            var name = intersection.gameObject.name;
            var intersectionLanes = intersection.GetComponentsInChildren<MapTrafficLane>();
            if (intersectionLanes.Length == 0)
                return null;

            var optimizedLanePolys = new List<List<Vertex>>();

            foreach (var lane in intersectionLanes)
            {
                var lanePoly = BuildLanePoly(lane, out _, true, true);
                var optimizedLanePoly = MeshUtils.OptimizePoly(lanePoly, $"{lane.gameObject.name}, part of {name}");
                if (optimizedLanePoly != null)
                    optimizedLanePolys.AddRange(optimizedLanePoly);
            }

            var merged = MeshUtils.ClipVatti(optimizedLanePolys);
            return MeshUtils.OptimizePoly(merged, name);
        }

        private void CreateIntersectionMesh(MapIntersection intersection, GameObject parentObject, MapMeshMaterials materials)
        {
            var name = intersection.gameObject.name;
            var optimized = CreateIntersectionPoly(intersection);
            var mesh = optimized == null ? null : Triangulation.TiangulateMultiPolygon(optimized, name);

            if (mesh == null)
            {
                Debug.LogWarning($"Zero surface mesh detected, skipping. ({name})");
                return;
            }

            AddUv(mesh);
            var intersectionTransform = intersection.transform;
            MoveMeshVericesToLocalSpace(mesh, intersectionTransform);

            var go = new GameObject(name + "_mesh");
            go.transform.SetParent(parentObject.transform);
            go.transform.rotation = intersectionTransform.rotation;
            go.transform.position = intersectionTransform.position;

            var localToWorldMatrix = go.transform.localToWorldMatrix;
            var intersectionLanes = intersection.GetComponentsInChildren<MapTrafficLane>();
            foreach (var lane in intersectionLanes)
            {
                linesData[lane.leftLineBoundry].meshData.Add((mesh, localToWorldMatrix));
                linesData[lane.rightLineBoundry].meshData.Add((mesh, localToWorldMatrix));
            }

            if (settings.createRenderers)
            {
                var mf = go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                mf.sharedMesh = mesh;
                mr.sharedMaterial = materials.road;
            }

            if (settings.createCollider)
            {
                var mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = mesh;
            }
        }

        private void CreateLineMesh(KeyValuePair<MapLine, LineData> lineData, GameObject parentObject, MapMeshMaterials materials)
        {
            if (lineData.Value.shape == LineData.LineShape.None)
                return;

            var mesh = BuildLineMesh(lineData.Key, lineData.Value.shape == LineData.LineShape.Double ? settings.lineWidth * 3 : settings.lineWidth);

            var go = new GameObject(lineData.Key.gameObject.name + "_mesh");
            go.transform.tag = "LaneLine";
            go.transform.SetParent(parentObject.transform);
            var lineTransform = lineData.Key.transform;
            go.transform.rotation = lineTransform.rotation;
            go.transform.position = lineTransform.position;
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = mesh;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            switch (lineData.Value.shape)
            {
                case LineData.LineShape.Solid:
                    mr.sharedMaterial = materials.GetSolidLineMaterial(lineData.Value.color);
                    break;
                case LineData.LineShape.Dotted:
                    mr.sharedMaterial = materials.GetDottedLineMaterial(lineData.Value.color);
                    break;
                case LineData.LineShape.Double:
                    mr.sharedMaterial = materials.GetDoubleLineMaterial(lineData.Value.color);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private List<List<Vertex>> BuildLanePoly(MapTrafficLane lane, out List<List<Vertex>> pushedMesh, bool worldSpace = false, bool alwaysMerge = false)
        {
            var leftPoints = ListPool<LineVert>.Get();
            var rightPoints = ListPool<LineVert>.Get();

            var leftBound = lane.leftLineBoundry;
            var rightBound = lane.rightLineBoundry;

            leftPoints.AddRange(linesData[leftBound].worldPoints);
            rightPoints.AddRange(linesData[rightBound].worldPoints);

            GetLinesOrientation(lane, out var leftReversed, out var rightReversed);

            if (rightReversed)
                rightPoints.Reverse();

            if (!leftReversed)
                leftPoints.Reverse();

            RemoveWeldedPoints(rightPoints, leftPoints);

            var polygon = rightPoints.Select(point => new Vertex(point.position)).ToList();
            polygon.AddRange(leftPoints.Select(point => new Vertex(point.position)));

            MeshUtils.RemoveDuplicates(polygon);

            var poly = new List<List<Vertex>>() {polygon};
            pushedMesh = null;

            // Add roadside
            if (settings.pushOuterVerts)
            {
                var useSeparateMesh = settings.separateOuterMesh && !alwaysMerge;
                var targetPoly = useSeparateMesh ? new List<List<Vertex>>() : poly;
                
                if (linesData[lane.rightLineBoundry].usageCount == 1)
                {
                    AddRoadsidePolygons(ref targetPoly, rightPoints);
                    targetPoly = MeshUtils.ClipVatti(targetPoly);
                }

                if (linesData[lane.leftLineBoundry].usageCount == 1)
                {
                    AddRoadsidePolygons(ref targetPoly, leftPoints);
                    targetPoly = MeshUtils.ClipVatti(targetPoly);
                }

                if (useSeparateMesh)
                    pushedMesh = targetPoly.Count > 0 ? targetPoly : null;
                else
                    poly = targetPoly;
            }

            if (!worldSpace)
            {
                for (var i = 0; i < poly.Count; ++i)
                {
                    for (var j = 0; j < poly[i].Count; ++j)
                        poly[i][j].Position = lane.transform.InverseTransformPoint(polygon[i].Position);
                }
            }

            ListPool<LineVert>.Release(leftPoints);
            ListPool<LineVert>.Release(rightPoints);

            if (settings.fixInvalidPolygons)
            {
                var changed = false;
                foreach (var subPoly in poly)
                {
                    if (TryFixSubPoly(subPoly, lane.name))
                        changed = true;
                }

                if (changed)
                    Debug.Log($"Mesh changed vertices ordering to fix intersecting edges ({lane.name})");
            }

            return poly;
        }

        private bool TryFixSubPoly(List<Vertex> poly, string debugName)
        {
            if (poly.Count < 4)
                return false;

            var safety = 32;
            var changed = true;
            var atLeastOneChanged = false;

            List<Vertex> original = null;

            while (safety-- > 0 && changed)
            {
                changed = false;

                for (var i0 = 0; i0 < poly.Count; ++i0)
                {
                    var i1 = MeshUtils.LoopIndex(i0 + 1, poly.Count);
                    var i2 = MeshUtils.LoopIndex(i0 + 2, poly.Count);
                    var i3 = MeshUtils.LoopIndex(i0 + 3, poly.Count);

                    if (MeshUtils.AreLinesIntersecting(poly[i0], poly[i1], poly[i2], poly[i3]))
                    {
                        original ??= new List<Vertex>(poly);
                        var tmp = poly[i1];
                        poly[i1] = poly[i2];
                        poly[i2] = tmp;
                        changed = true;
                        atLeastOneChanged = true;
                    }
                }
            }

            if (safety == 0)
            {
                Debug.LogWarning($"Unable to fix intersecting edges - polygon might fall back to convex hull ({debugName})");
                if (original != null)
                {
                    poly.Clear();
                    poly.AddRange(original);
                }

                return false;
            }

            return atLeastOneChanged;
        }

        private void AddRoadsidePolygons(ref List<List<Vertex>> poly, List<LineVert> linePoints)
        {
            for (var i = 1; i < linePoints.Count; ++i)
            {
                var v0 = new Vertex(linePoints[i].position);
                var v1 = new Vertex(linePoints[i - 1].position);
                var v2 = new Vertex(linePoints[i - 1].position + linePoints[i - 1].outVector * settings.pushDistance);
                var v3 = new Vertex(linePoints[i].position + linePoints[i].outVector * settings.pushDistance);

                if (MeshUtils.AreLinesIntersecting(v0, v3, v1, v2))
                {
                    var vInt = MeshUtils.GetLineLineIntersectionPoint(v0, v3, v1, v2);
                    poly.Add(new List<Vertex> {v0, v1, vInt});
                }
                else
                {
                    poly.Add(new List<Vertex> {v0, v1, v2, v3});
                }
            }
        }

        private void GetLinesOrientation(MapTrafficLane lane, out bool leftReversed, out bool rightReversed)
        {
            var leftPoints = linesData[lane.leftLineBoundry].worldPoints;
            var rightPoints = linesData[lane.rightLineBoundry].worldPoints;

            var laneStart = lane.transform.TransformPoint(lane.mapLocalPositions[0]);
            var leftStart = leftPoints[0].position;
            var leftEnd = leftPoints[leftPoints.Count - 1].position;
            var rightStart = rightPoints[0].position;
            var rightEnd = rightPoints[rightPoints.Count - 1].position;

            leftReversed = Vector3.Distance(laneStart, leftEnd) < Vector3.Distance(laneStart, leftStart);
            rightReversed = Vector3.Distance(laneStart, rightEnd) < Vector3.Distance(laneStart, rightStart);
        }

        private void RemoveWeldedPoints(List<LineVert> forward, List<LineVert> reverse)
        {
            RemoveWeldedPointsForwardReverse(forward, reverse);
            RemoveWeldedPointsForwardReverse(reverse, forward);
        }

        private void RemoveWeldedPointsForwardReverse(List<LineVert> forward, List<LineVert> reverse)
        {
            const float threshold = 0.05f;

            if (Vector3.Distance(forward[0].position, reverse[reverse.Count - 1].position) < threshold)
            {
                var forwardVecList = forward.Select(x => x.position).ToList();
                var forwardIndex = 0;
                var reverseIndex = reverse.Count - 1;
                for (var i = 1; i < reverse.Count - 1; ++i)
                {
                    var reversePoint = reverse[reverse.Count - 1 - i];
                    var dist = MeshUtils.PointLaneDistance(reversePoint.position, forwardVecList, out var crIndex);

                    if (dist < threshold)
                    {
                        forwardIndex = crIndex;
                        reverseIndex = reverse.Count - 1 - i;
                    }
                    else
                        break;
                }

                var point = new LineVert(forward[forwardIndex].position);

                forward.RemoveRange(0, forwardIndex);
                reverse.RemoveRange(reverseIndex, reverse.Count - reverseIndex);
                reverse.Add(point);
            }
        }

        private void CalculateOutVectors(List<MapTrafficLane> lanes)
        {
            foreach (var lane in lanes)
            {
                var lb = lane.leftLineBoundry;
                var rb = lane.rightLineBoundry;

                if (linesData[lb].usageCount != 1 && linesData[rb].usageCount != 1)
                    continue;

                GetLinesOrientation(lane, out var leftReversed, out var rightReversed);

                if (linesData[lb].usageCount == 1)
                    CalculateOutVectorsForLine(linesData[lb].worldPoints, !leftReversed);

                if (linesData[rb].usageCount == 1)
                    CalculateOutVectorsForLine(linesData[rb].worldPoints, rightReversed);
            }

            var e = new LaneBoundEnumerator(linesData);

            foreach (var lane in lanes)
            {
                foreach (var vert in e.Enumerate(lane))
                {
                    var vertHasOutVec = vert.outVector.sqrMagnitude > 0.5f;
                    var sum = vertHasOutVec ? vert.outVector : Vector3.zero;
                    var count = vertHasOutVec ? 1 : 0;
                    foreach (var linkedVert in vert.Linked)
                    {
                        if (linkedVert.outVector.sqrMagnitude < 0.5f)
                            continue;

                        sum += linkedVert.outVector;
                        count++;
                    }

                    if (count == 0)
                        continue;

                    var outVec = (sum / count).normalized;

                    vert.outVector = outVec;
                    foreach (var linkedVert in vert.Linked)
                        linkedVert.outVector = outVec;
                }
            }
        }

        private void CalculateOutVectorsForLine(List<LineVert> line, bool reversed)
        {
            int GetIndex(int rawIndex)
            {
                return reversed ? line.Count - rawIndex - 1 : rawIndex;
            }

            for (var i = 0; i < line.Count; ++i)
            {
                var c = GetIndex(i);
                var p = GetIndex(i - 1);
                var n = GetIndex(i + 1);

                var fwd = Vector3.zero;
                if (i > 0)
                    fwd += line[c].position - line[p].position;
                if (i < line.Count - 1)
                    fwd += line[n].position - line[c].position;

                line[c].outVector = new Vector3(fwd.z, 0f, -fwd.x).normalized;
            }
        }

        private Vector3 CorrectPointToMesh(MapLine line, Vector3 point)
        {
            var topY = float.MinValue;

            foreach (var (laneMesh, matrix) in linesData[line].meshData)
            {
                if (!MeshUtils.IntersectRayMesh(new Ray(point + Vector3.up, Vector3.down), laneMesh, matrix, out var hit))
                    continue;

                if (hit.point.y > topY)
                    topY = hit.point.y;
            }

            if (topY > float.MinValue)
                point.y = topY;

            return point;
        }

        private Mesh BuildLineMesh(MapLine line, float width)
        {
            width *= 0.5f;
            var verts = new List<Vector3>();
            var indices = new List<int>();
            var uvs = new List<Vector2>();

            var points = ListPool<Vector3>.Get();
            points.AddRange(linesData[line].worldPoints.Select(x => x.position));

            if (points.Count < 2)
                return null;

            for (var i = 0; i < points.Count; ++i)
                points[i] = CorrectPointToMesh(line, points[i]);

            var fVec = (points[1] - points[0]).normalized * width;
            verts.Add(points[0] + new Vector3(-fVec.z, fVec.y, fVec.x));
            verts.Add(points[0] + new Vector3(fVec.z, fVec.y, -fVec.x));
            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(1, 0));

            var lastUv = 0f;

            for (var i = 1; i < points.Count; ++i)
            {
                if (i == points.Count - 1)
                {
                    var vec = (points[i] - points[i - 1]).normalized * width;
                    verts.Add(points[i] + new Vector3(-vec.z, vec.y, vec.x));
                    verts.Add(points[i] + new Vector3(vec.z, vec.y, -vec.x));
                }
                else
                {
                    var a = points[i - 1];
                    var b = points[i];
                    var c = points[i + 1];
                    var ab = (b - a).normalized * width;
                    var bc = (c - b).normalized * width;
                    var abL = new Vector3(-ab.z, ab.y, ab.x);
                    var bcL = new Vector3(-bc.z, bc.y, bc.x);
                    var abR = new Vector3(ab.z, ab.y, -ab.x);
                    var bcR = new Vector3(bc.z, bc.y, -bc.x);

                    var pointLeft = FindIntersection(a + abL, b + abL, b + bcL, c + bcL);
                    var pointRight = FindIntersection(a + abR, b + abR, b + bcR, c + bcR);

                    verts.Add(pointLeft);
                    verts.Add(pointRight);
                }

                var raise = 0f;

                for (var j = 1; j <= 2; ++j)
                {
                    var pointA = verts[verts.Count - j - 2];
                    var pointB = verts[verts.Count - j];

                    for (var k = 0; k <= 8; ++k)
                    {
                        var point = Vector3.Lerp(pointA, pointB, k / 8.0f);

                        foreach (var (laneMesh, matrix) in linesData[line].meshData)
                        {
                            if (!MeshUtils.IntersectRayMesh(new Ray(point + Vector3.up, Vector3.down), laneMesh, matrix, out var hit))
                                continue;

                            var diff = hit.point.y - point.y;
                            if (diff > raise)
                                raise = diff;
                        }
                    }

                    pointA.y += raise;
                    pointB.y += raise;

                    verts[verts.Count - j - 2] = pointA;
                    verts[verts.Count - j] = pointB;
                }

                var uvIncr = Vector3.Distance(points[i], points[i - 1]) / settings.lineUvUnit;
                uvs.Add(new Vector2(0, lastUv + uvIncr));
                uvs.Add(new Vector2(1, lastUv + uvIncr));
                lastUv += uvIncr;

                indices.Add(2 * i - 2);
                indices.Add(2 * i);
                indices.Add(2 * i + 1);
                indices.Add(2 * i - 2);
                indices.Add(2 * i + 1);
                indices.Add(2 * i - 1);
            }

            for (var i = 0; i < verts.Count; ++i)
            {
                var vert = verts[i];
                vert.y += settings.lineBump;
                verts[i] = line.transform.InverseTransformPoint(vert);
            }

            ListPool<Vector3>.Release(points);

            var mesh = new Mesh {name = $"{line.gameObject.name}_mesh"};
            mesh.SetVertices(verts);
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.Optimize();
            return mesh;
        }

        private void SnapLanes(List<MapTrafficLane> lanes)
        {
            var validPoints = new List<LineVert>();
            var linkedPoints = new List<LineVert>();
            var connectedLanes = new List<MapTrafficLane>();
            var pointsToProcess = new List<LineVert>();
            var ptpMasks = new List<CornerMask>();
            var directConnections = new List<bool>();
            var e = new LaneBoundEnumerator(linesData);

            foreach (var lane in lanes)
            {
                pointsToProcess.Clear();
                ptpMasks.Clear();

                foreach (var vert in e.Enumerate(lane))
                {
                    pointsToProcess.Add(vert);
                    ptpMasks.Add(e.CurrentMask);
                }

                connectedLanes.Clear();
                directConnections.Clear();

                foreach (var prevLane in lane.prevConnectedLanes)
                {
                    connectedLanes.Add(prevLane);
                    directConnections.Add(true);
                    foreach (var nestedLane in prevLane.nextConnectedLanes)
                    {
                        if (!connectedLanes.Contains(nestedLane))
                        {
                            directConnections.Add(false);
                            connectedLanes.Add(nestedLane);
                        }
                    }
                }

                foreach (var nextLane in lane.nextConnectedLanes)
                {
                    connectedLanes.Add(nextLane);
                    directConnections.Add(true);
                    foreach (var nestedLane in nextLane.prevConnectedLanes)
                    {
                        if (!connectedLanes.Contains(nestedLane))
                        {
                            directConnections.Add(false);
                            connectedLanes.Add(nestedLane);
                        }
                    }
                }

                for (var i = 0; i < pointsToProcess.Count; ++i)
                {
                    var point = pointsToProcess[i];

                    linkedPoints.Clear();
                    validPoints.Clear();
                    validPoints.Add(point);

                    for (var j = 0; j < connectedLanes.Count; ++j)
                    {
                        var bestVert = (LineVert) null;
                        var bestDistance = float.MaxValue;

                        foreach (var cPoint in e.Enumerate(connectedLanes[j]))
                        {
                            if (!ptpMasks[i].Connects(e.CurrentMask, directConnections[j]))
                                continue;

                            var dist = Vector3.Distance(point.position, cPoint.position);
                            if (dist < settings.snapThreshold && dist < bestDistance)
                            {
                                bestVert = cPoint;
                                bestDistance = dist;
                            }
                        }

                        if (bestVert != null)
                            validPoints.Add(bestVert);
                    }

                    if (validPoints.Count > 1)
                    {
                        linkedPoints.AddRange(validPoints);

                        foreach (var validPoint in validPoints)
                        {
                            foreach (var linked in validPoint.Linked)
                            {
                                if (!linkedPoints.Contains(linked))
                                    linkedPoints.Add(linked);
                            }
                        }

                        var avg = linkedPoints.Aggregate(Vector3.zero, (current, validPoint) => current + validPoint.position);
                        avg /= linkedPoints.Count;

                        foreach (var linkedPoint in linkedPoints)
                        {
                            linkedPoint.position = avg;
                            linkedPoint.AddLinks(linkedPoints);
                        }
                    }
                }
            }
        }

        private void AddUv(Mesh worldSpaceMesh)
        {
            var verts = worldSpaceMesh.vertices;
            var uv = new Vector2[verts.Length];

            for (var i = 0; i < verts.Length; ++i)
                uv[i] = MapWorldUv(verts[i]);

            worldSpaceMesh.SetUVs(0, uv);
            worldSpaceMesh.RecalculateTangents();
        }

        private void MoveMeshVericesToLocalSpace(Mesh worldSpaceMesh, Transform transform)
        {
            var verts = worldSpaceMesh.vertices;

            for (var i = 0; i < verts.Length; ++i)
                verts[i] = transform.InverseTransformPoint(verts[i]);

            worldSpaceMesh.SetVertices(verts);
            worldSpaceMesh.RecalculateBounds();
            worldSpaceMesh.RecalculateNormals();
            worldSpaceMesh.RecalculateTangents();
            worldSpaceMesh.Optimize();
        }

        private Vector2 MapWorldUv(Vector3 worldPosition)
        {
            return new Vector2(worldPosition.x / settings.roadUvUnit, worldPosition.z / settings.roadUvUnit);
        }

        private Vector3 FindIntersection(Vector3 a0, Vector3 a1, Vector3 b0, Vector3 b1)
        {
            return MeshUtils.AreLinesIntersecting(a0, a1, b0, b1)
                ? MeshUtils.GetLineLineIntersectionPoint(a0, a1, b0, b1)
                : a1;
        }
    }
}