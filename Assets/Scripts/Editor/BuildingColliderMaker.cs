/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using UnityEditor;

public class BuildingColliderMaker : EditorWindow
{

    [MenuItem("Window/Simon/AttachBoxColliders")]
    static void Go()
    {
        var rootGo = Selection.activeGameObject;
        int count = 0;
        foreach(Transform child in rootGo.transform)
        {
            AttachCollider(child);
            EditorUtility.DisplayProgressBar("Creating colliders", count  + "/" + rootGo.transform.childCount, count++ / (float)rootGo.transform.childCount);
        }
        EditorUtility.ClearProgressBar();
    }

    [MenuItem("Window/Simon/AttachBoxColliderTest")]
    static void GoSingle()
    {
       AttachCollider(Selection.activeGameObject.transform); 
    }

    [MenuItem("Window/Simon/ClearBar")]
    static void ClearBar()
    {
        EditorUtility.ClearProgressBar();
    }

    static void AttachCollider(Transform child)
    {
        if(!child.gameObject.activeInHierarchy)
            return;

        if(child.GetComponentInChildren<BoxCollider>() == null)
        {
            GameObject collider = new GameObject("Collider");
            collider.transform.parent = child;
            collider.transform.localPosition = Vector3.zero;
            collider.transform.localRotation = Quaternion.identity;
            collider.AddComponent<BoxCollider>();
        }

        var box = child.GetComponentInChildren<BoxCollider>();
        box.center = Vector3.zero;
        box.size = Vector3.one * 0.01f;
        box.transform.localRotation = Quaternion.identity;
        box.transform.localPosition = Vector3.zero;

        float minSize = 999999999f;
        float minRot = 0f;
        Vector3 realSize = Vector3.one;
        Vector3 realCenter = Vector3.zero;
        for(float yRot = 0f; yRot < 90f; yRot += 1f)
        {
            box.transform.localRotation = Quaternion.Euler(0f, yRot, 0f);
            float maxX = 0f;
            float minX = 99999f;
            float minY = 99999f;
            float minZ = 99999f;
            float maxZ = 0f;
            float maxY = 0f;

            bool bad = true;
            foreach(var mesh in child.GetComponentsInChildren<MeshFilter>())
            {
                Vector3[] verts = null;
                if(mesh.sharedMesh != null)
                    verts = mesh.sharedMesh.vertices;
                
                if(verts == null)
                    Debug.Log("null mesh on " + child.name);
                else
                {
                    bad = false;
                    foreach(var vert in mesh.sharedMesh.vertices)
                    {
                        Vector3 worldPos = mesh.transform.TransformPoint(vert);
                        Vector3 boxPos = box.transform.InverseTransformPoint(worldPos);
                        if(boxPos.x > maxX)
                            maxX = boxPos.x;
                        if(boxPos.x < minX)
                            minX = boxPos.x;

                        if(boxPos.z > maxZ)
                            maxZ = boxPos.z;
                        if(boxPos.z < minZ)
                            minZ = boxPos.z;

                        if(boxPos.y > maxY)
                            maxY = boxPos.y;
                        if(boxPos.y < minY)
                            minY = boxPos.y;
                    }
                }
            }
            if(!bad)
            {
                float xSize = (maxX - minX);
                float ySize = (maxY - minY);
                float zSize = (maxZ - minZ);

                Vector3 center = box.transform.TransformPoint(new Vector3(minX + (maxX - minX) / 2, minY + (maxY - minY) / 2, minZ + (maxZ - minZ) / 2));
                if(xSize * ySize * zSize < minSize)
                {
                    minSize = xSize * ySize * zSize;
                    minRot = yRot;
                    realSize = new Vector3(xSize, ySize, zSize);
                    realCenter = center;
                }
            }
        }

        box.transform.localRotation = Quaternion.Euler(0f, minRot, 0f);
        box.size = realSize;
        box.transform.position = realCenter;

    }

}
