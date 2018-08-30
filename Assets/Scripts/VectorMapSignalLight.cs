/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;

public class VectorMapSignalLight : MonoBehaviour
{
    public Vector3 offsets = new Vector3();
    public Vector3 boundScale = new Vector3();

    [System.Serializable]
    public class Data
    {
        public enum Type
        {
            Red = 1,
            Yellow = 2,
            Green = 3,
        }

        public Vector3 localPosition;

        public Type type = Type.Yellow;
    }

    public List<Data> signalDatas;

    public MapStopLineSegmentBuilder hintStopline;

    public static Color GetTypeColor(VectorMapSignalLight.Data data)
    {
        switch (data.type)
        {
            case Data.Type.Red:
                return Color.red;
            case Data.Type.Yellow:
                return Color.yellow;
            case Data.Type.Green:
                return Color.green;
        }

        return Color.black;
    }

    public System.ValueTuple<Vector3, Vector3, Vector3, Vector3> Get2DBounds()
    {
        var matrix = transform.parent == null ? Matrix4x4.identity : transform.parent.localToWorldMatrix * Matrix4x4.TRS(transform.localPosition + offsets, transform.localRotation, Vector3.Scale(transform.localScale, boundScale));

        float min = boundScale[0];
        int index = 0;
        for (int i = 0; i < 3; i++)
        {
            if (boundScale[i] < min)
            {
                min = boundScale[i];
                index = i;
            }
        }

        if (index == 0)
        {
            return new System.ValueTuple<Vector3, Vector3, Vector3, Vector3>(
                matrix.MultiplyPoint(new Vector3(0, 0.5f, 0.5f)),
                matrix.MultiplyPoint(new Vector3(0, -0.5f, 0.5f)),
                matrix.MultiplyPoint(new Vector3(0, -0.5f, -0.5f)),
                matrix.MultiplyPoint(new Vector3(0, 0.5f, -0.5f))
                );
        }
        else if (index == 1)
        {
            return new System.ValueTuple<Vector3, Vector3, Vector3, Vector3>(
                matrix.MultiplyPoint(new Vector3(0.5f, 0, 0.5f)),
                matrix.MultiplyPoint(new Vector3(-0.5f, 0, 0.5f)),
                matrix.MultiplyPoint(new Vector3(-0.5f, 0, -0.5f)),
                matrix.MultiplyPoint(new Vector3(0.5f, 0, -0.5f))
                );
        }
        else
        {
            return new System.ValueTuple<Vector3, Vector3, Vector3, Vector3>(
                matrix.MultiplyPoint(new Vector3(0.5f, 0.5f, 0)),
                matrix.MultiplyPoint(new Vector3(-0.5f, 0.5f, 0)),
                matrix.MultiplyPoint(new Vector3(-0.5f, -0.5f, 0)),
                matrix.MultiplyPoint(new Vector3(0.5f, -0.5f, 0))
                );
        }        
    }
}
