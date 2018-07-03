/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static VectorMap.VectorMapUtility;

#pragma warning disable 0219

public struct PointCloudVertex
{
    public Vector3 position;
    public Vector3 normal;
    public Vector2 uv;
    public Color color;
    public Material material;
}

public class PointCloudTool : MonoBehaviour
{
    static System.Random rand = new System.Random();
    public List<GameObject> gameobjects;
    [Header("Generation Settings")]
    public float density = 1.0f;
    public FilterShape boundShape;
    public Material pointCloudMaterial;
    [System.NonSerialized]
    Dictionary<GameObject, List<PointCloudVertex>> pointCloudPool = new Dictionary<GameObject, List<PointCloudVertex>>();
    const int vertexLimitPerMesh = 65000;

    [Header("Export Settings")]
    public bool filenamePerObject = true;
    public bool exportAscii = false;
    public string foldername = "pointcloud_map";
    public string filename = "Test.pcd";
    public float exportScaleFactor = 1.0f;

    [Header("Realtime Export")]
    [System.NonSerialized]
    public int batchSize = 500000;

    static string pointCloudHeader = "VERSION 0.7\n" +
                                    "FIELDS x y z rgb\n" +
                                    "SIZE 4 4 4 4\n" +
                                    "TYPE F F F F\n" +
                                    "COUNT 1 1 1 1\n" +
                                    "WIDTH $WIDTH\n" +
                                    "HEIGHT 1\n" +
                                    "VIEWPOINT 0 0 0 1 0 0 0\n" +
                                    "POINTS $POINT\n" +
                                    "DATA $DATA\n";

    public static Vector3 GetRandomPointFromTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        double r1 = rand.NextDouble();
        double r2 = rand.NextDouble();
        double r1_sqrt = System.Math.Sqrt(r1);
        return p1 * (float)(1.0 - r1_sqrt) + p2 * (float)(r1_sqrt * (1.0 - r2)) + p3 * (float)(r2 * r1_sqrt);
    }

    public static Vector3 GetInterpolatedPointFromTriangle(Vector3 p, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 v1, Vector3 v2, Vector3 v3)
    {
        //Calculate section vectors from p to p1, p2 and p3
        var s1 = p1 - p;
        var s2 = p2 - p;
        var s3 = p3 - p;

        //Calculate the areas and factors
        var totalArea = Vector3.Cross(p1 - p2, p1 - p3).magnitude; //It's double times the triangle area
        var area1 = Vector3.Cross(s2, s3).magnitude;
        var area2 = Vector3.Cross(s3, s1).magnitude;
        var area3 = Vector3.Cross(s1, s2).magnitude;

        //Multiply in corresponding area ratio factors for final interpolated result
        return v1 * area1 / totalArea + v2 * area2 / totalArea + v3 * area3 / totalArea;
    }

    public static Vector2 GetInterpolatedPointFromTriangle(Vector3 p, Vector3 p1, Vector3 p2, Vector3 p3, Vector2 v1, Vector2 v2, Vector2 v3)
    {
        return (Vector2)GetInterpolatedPointFromTriangle((Vector3)p, (Vector3)p1, (Vector3)p2, (Vector3)p3, (Vector3)v1, (Vector3)v2, (Vector3)v3);
    }

    public static float GetTriangleArea(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        return Vector3.Cross(p1 - p2, p1 - p3).magnitude / 2.0f;
    }

    public void GeneratePointCloud(bool realtimeExport)
    {
        HashSet<MeshFilter> meshFilterDatabase = new HashSet<MeshFilter>();
        var pointCloudAreas = GetComponentsInChildren<PointCloudArea>();
        int totalCount = 0;
        int batchExportIndex = 0;
        int lastBatchExportIndex = batchExportIndex;
        List<PointCloudVertex> pointCloudVertices = null;

        if (gameobjects.Count > 0)
        {
            GameObject lastGo = gameobjects[0];
            for (int ind = 0; ind < gameobjects.Count; ind++)
            {
                var go = gameobjects[ind];

                if (go == null)
                {
                    continue;
                }

                lastBatchExportIndex = batchExportIndex;
                batchExportIndex = 0;

                if (filenamePerObject && !realtimeExport)
                {
                    pointCloudPool.Add(go, new List<PointCloudVertex>());
                    pointCloudVertices = pointCloudPool[go];
                }
                else
                {
                    if (!pointCloudPool.ContainsKey(gameObject))
                    {
                        pointCloudPool.Add(gameObject, new List<PointCloudVertex>());
                    }
                    pointCloudVertices = pointCloudPool[gameObject];
                }

                var meshFilters = go.GetComponentsInChildren<MeshFilter>();

                foreach (var mFilter in meshFilters)
                {
                    var theMesh = mFilter.sharedMesh;
                    var renderer = mFilter.gameObject.GetComponent<Renderer>();
                    if (theMesh == null || renderer == null)
                    {
                        continue;
                    }

                    if (!meshFilterDatabase.Contains(mFilter))
                    {
                        mFilter.gameObject.isStatic = false;

                        for (int i = 0; i < theMesh.subMeshCount; i++)
                        {
                            var triangles = new List<int>();
                            theMesh.GetTriangles(triangles, i);
                            for (int j = 0; j < triangles.Count; j += 3)
                            {
                                var A = theMesh.vertices[triangles[j]];
                                var B = theMesh.vertices[triangles[j + 1]];
                                var C = theMesh.vertices[triangles[j + 2]];

                                var A_wld = mFilter.transform.TransformPoint(A);
                                var B_wld = mFilter.transform.TransformPoint(B);
                                var C_wld = mFilter.transform.TransformPoint(C);

                                var center_wld = (A_wld + B_wld + C_wld) / 3.0f;

                                float pointAmount = GetTriangleArea(A_wld, B_wld, C_wld) * density;
                                float extraAmount = .0f;

                                int totalAreas = 0;
                                float totalExtraScalers = .0f;
                                foreach (var pcArea in pointCloudAreas)
                                {
                                    if (pcArea.Contains(center_wld))
                                    {
                                        totalExtraScalers += pcArea.densityScaler;
                                        ++totalAreas;
                                    }
                                }
                                if (totalAreas > 0)
                                {
                                    extraAmount = pointAmount * (totalExtraScalers / totalAreas) - pointAmount;
                                }

                                List<Vector3> points = new List<Vector3>();
                                if (Random.value < (pointAmount % 1.0f))
                                {
                                    ++pointAmount;
                                }
                                for (int k = 0; k < System.Math.Round(pointAmount); k++)
                                {
                                    points.Add(GetRandomPointFromTriangle(A_wld, B_wld, C_wld));
                                }

                                if (Random.value < (extraAmount % 1.0f))
                                {
                                    ++extraAmount;
                                }
                                for (int k = 0; k < System.Math.Round(extraAmount); k++)
                                {
                                    var p = GetRandomPointFromTriangle(A_wld, B_wld, C_wld);
                                    bool keep = false;
                                    foreach (var pcArea in pointCloudAreas)
                                    {
                                        if (pcArea.Contains(p))
                                        {
                                            keep = true;
                                        }
                                    }
                                    if (keep)
                                    {
                                        points.Add(p);
                                    }
                                }

                                foreach (var p in points)
                                {
                                    if (boundShape == null || boundShape.Contains(p))
                                    {
                                        pointCloudVertices.Add(new PointCloudVertex
                                        {
                                            position = p,
                                            normal = GetInterpolatedPointFromTriangle(p, A_wld, B_wld, C_wld, mFilter.transform.TransformVector(theMesh.normals[triangles[j]]), mFilter.transform.TransformVector(theMesh.normals[triangles[j + 1]]), mFilter.transform.TransformVector(theMesh.normals[triangles[j + 2]])),
                                            uv = GetInterpolatedPointFromTriangle(p, A_wld, B_wld, C_wld, theMesh.uv[triangles[j]], theMesh.uv[triangles[j + 1]], theMesh.uv[triangles[j + 2]]),
                                            color = Color.white,
                                            material = mFilter.gameObject.GetComponent<Renderer>().sharedMaterials[i],
                                        });
                                        ++totalCount;

                                        if (realtimeExport)
                                        {
                                            if (filenamePerObject)
                                            {                                                    
                                                if (go != lastGo)
                                                {
                                                    RealtimeExport(pointCloudVertices, lastGo.name, lastBatchExportIndex);
                                                    lastGo = go;
                                                    pointCloudVertices.Clear();
                                                }
                                                else if (pointCloudVertices.Count >= batchSize)
                                                {
                                                    RealtimeExport(pointCloudVertices, go.name, batchExportIndex++);
                                                    pointCloudVertices.Clear();
                                                }                                            
                                            }
                                            else
                                            {
                                                if (pointCloudVertices.Count >= batchSize)
                                                {
                                                    RealtimeExport(pointCloudVertices, "", batchExportIndex++);
                                                    pointCloudVertices.Clear();
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        mFilter.gameObject.isStatic = true;

                        meshFilterDatabase.Add(mFilter);
                    }
                }
            }

            if (realtimeExport && pointCloudVertices.Count > 0)
            {
                if (filenamePerObject)
                {
                    RealtimeExport(pointCloudVertices, gameobjects[gameobjects.Count - 1].name, batchExportIndex++);
                }
                else
                {
                    RealtimeExport(pointCloudVertices, "", batchExportIndex++);
                }
            }
        }

        Debug.Log("Total Point Cloud Vertices Count: " + totalCount);
    }

    public void ClearPointCloud()
    {
        pointCloudPool.Clear();
        var pcMeshes = GetComponentsInChildren<PointCloudMesh>();
        for (int i = 0; i < pcMeshes.Length; i++)
        {
            DestroyImmediate(pcMeshes[i].gameObject);
        }
    }

    public void ExportPointCloud()
    {
        var filename = this.filename;
        foreach (var pointCloudVerticesKey in pointCloudPool.Keys)
        {
            var pointCloudVertices = pointCloudPool[pointCloudVerticesKey];
            string finalExportName;

            if (filenamePerObject)
            {
                finalExportName = $"{filename}_{pointCloudVerticesKey.name}";
            }
            else
            {
                finalExportName = filename;
            }

            if (System.IO.File.Exists($"{finalExportName}.pcd"))
            {
                int postIndex = 1;
                while (System.IO.File.Exists($"{finalExportName}_{postIndex}.pcd"))
                {
                    ++postIndex;
                }
                finalExportName = $"{finalExportName}_{postIndex}";
            }

            finalExportName += ".pcd";

            WriteVerticesToFile(pointCloudVertices, finalExportName);
        }
    }

    public void RealtimeGenerateExport()
    {
        ClearPointCloud();
        GeneratePointCloud(true);
    }

    public void BuildVisualizationMeshes()
    {
        foreach (var pointCloudVerticesKey in pointCloudPool.Keys)
        {
            BuildMeshes(pointCloudPool[pointCloudVerticesKey]);
        }
    }

    private void BuildMeshes(List<PointCloudVertex> pointCloudVertices)
    {
        int verticesLeft = pointCloudVertices.Count;
        while (verticesLeft > 0)
        {
            var vertCount = verticesLeft > vertexLimitPerMesh ? vertexLimitPerMesh : verticesLeft;

            //Generate point cloud mesh
            var pointCloudMesh = new Mesh();
            var pcVertices = new List<Vector3>(vertCount);
            var pcIndices = new int[vertCount];
            Color[] pcColors = new Color[vertCount];
            for (int i = 0; i < vertCount; i++)
            {
                var adjustedIndex = i + (pointCloudVertices.Count - verticesLeft);
                pcVertices.Add(pointCloudVertices[adjustedIndex].position);
                pcIndices[i] = i;
                pcColors[i] = pointCloudVertices[adjustedIndex].color;
            }
            pointCloudMesh.SetVertices(pcVertices);
            pointCloudMesh.SetIndices(pcIndices, MeshTopology.Points, 0);

            //Link point cloud mesh to gameobject
            var pcMeshGo = new GameObject("Point Cloud Mesh");
            var t = transform.Find("Point Cloud Meshes");
            if (t == null)
            {
                t = (new GameObject("Point Cloud Meshes")).transform;
                t.SetParent(transform);
            }
            pcMeshGo.transform.SetParent(t);
            var mf = pcMeshGo.AddComponent<MeshFilter>();
            var mr = pcMeshGo.AddComponent<MeshRenderer>();
            var ms = pcMeshGo.AddComponent<PointCloudMesh>();
            mf.sharedMesh = pointCloudMesh;
            mr.sharedMaterial = pointCloudMaterial;
            mf.sharedMesh.SetColors(new List<Color>(pcColors));

            verticesLeft -= vertexLimitPerMesh;
        }
    }

    private void RealtimeExport(List<PointCloudVertex> pointCloudVertices, string itemName, int batchExportIndex)
    {
        var filename = this.filename;
        var itemname = (itemName == "" ? "" : ("_" + itemName));
        string finalExportName = $"{filename}{itemname}_{batchExportIndex.ToString("D5")}";

        if (System.IO.File.Exists($"{finalExportName}.pcd"))
        {
            int postIndex = 1;
            while (System.IO.File.Exists($"{finalExportName}_{postIndex}.pcd"))
            {
                ++postIndex;
            }
            finalExportName = $"{finalExportName}_{postIndex}";
        }

        finalExportName += ".pcd";

        WriteVerticesToFile(pointCloudVertices, finalExportName);
    }

    private void WriteVerticesToFile(List<PointCloudVertex> pointCloudVertices, string finalExportName)
    {
        if (!System.IO.Directory.Exists(foldername))
        {
            System.IO.Directory.CreateDirectory(foldername);
        }

        finalExportName = $"{foldername}{Path.DirectorySeparatorChar}{finalExportName}";

        var header = pointCloudHeader.Replace("$POINT", pointCloudVertices.Count.ToString()).Replace("$WIDTH", pointCloudVertices.Count.ToString());
        header = header.Replace("$DATA", exportAscii ? "ascii" : "binary");

        using (StreamWriter sw = File.CreateText(finalExportName))
        {
            sw.Write(header);
        }

        if (exportAscii)
        {
            using (StreamWriter sw = new StreamWriter(File.Open(finalExportName, FileMode.Append)))
            {
                for (int i = 0; i < pointCloudVertices.Count; i++)
                {
                    Vector3 convertedPosition = GetRvizCoordinates(pointCloudVertices[i].position);
                    sw.Write(convertedPosition.x * exportScaleFactor);
                    sw.Write(" ");
                    sw.Write(convertedPosition.y * exportScaleFactor);
                    sw.Write(" ");
                    sw.Write(convertedPosition.z * exportScaleFactor);
                    sw.Write(" ");
                    //sw.Write(pointCloudVertices[i].color.maxColorComponent);
                    sw.Write(((int)pointCloudVertices[i].color.r) << 16 | ((int)pointCloudVertices[i].color.g) << 8 | ((int)pointCloudVertices[i].color.b));
                    sw.Write(" \n");
                }
            }
            return;
        }

        using (BinaryWriter bw = new BinaryWriter(File.Open(finalExportName, FileMode.Append)))
        {
            for (int i = 0; i < pointCloudVertices.Count; i++)
            {
                Vector3 convertedPosition = VectorMap.VectorMapUtility.GetRvizCoordinates(pointCloudVertices[i].position);
                bw.Write(convertedPosition.x * exportScaleFactor);
                bw.Write(convertedPosition.y * exportScaleFactor);
                bw.Write(convertedPosition.z * exportScaleFactor);
                int rgb = ((int)pointCloudVertices[i].color.r) << 16 | ((int)pointCloudVertices[i].color.g) << 8 | ((int)pointCloudVertices[i].color.b);
                bw.Write(rgb);
            }
        }
    }
}
