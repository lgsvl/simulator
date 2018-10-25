/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;

public class FilterShape : MonoBehaviour
{
    public Color shapeColor = Color.red;

    public enum DrawMethod
    {
        OnGizmosSelected,
        OnGizmosAlways,
    }
    public DrawMethod drawMethod = DrawMethod.OnGizmosSelected;

    public enum Shape
    {
        Cube,
        Sphere,
    }
    public Shape shape;

    public bool Contains(Vector3 pos)
    {
        Transform refT = transform;
        Vector3 posLcl = new Vector3(
            refT.InverseTransformPoint(pos).x,
            refT.InverseTransformPoint(pos).y,
            refT.InverseTransformPoint(pos).z);

        if (shape == FilterShape.Shape.Cube)
        {
            if (Mathf.Abs(posLcl.x) < 0.5f &&
                Mathf.Abs(posLcl.y) < 0.5f &&
                Mathf.Abs(posLcl.z) < 0.5f)
            {
                return true;
            }
        }
        else if (shape == FilterShape.Shape.Sphere)
        {
            if ((posLcl - refT.localPosition).magnitude < 1.0f)
            {
                return true;
            }
        }
        return false;
    }

    protected void Draw()
    {
        if (shape == Shape.Cube)
        {
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = rotationMatrix;
            Gizmos.color = shapeColor;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
        else if (shape == Shape.Sphere)
        {
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = rotationMatrix;
            Gizmos.color = shapeColor;
            Gizmos.DrawWireSphere(Vector3.zero, 1.0f);
        }
    }

    protected virtual void OnDrawGizmos()
    {
        if (drawMethod != DrawMethod.OnGizmosAlways) return;
        Draw();
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (drawMethod != DrawMethod.OnGizmosSelected) return;
        Draw();
    }
}
