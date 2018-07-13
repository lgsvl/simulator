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

    public VectorMapStopLineSegmentBuilder hintStopline;

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
}
