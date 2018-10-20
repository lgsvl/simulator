/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapSignalLightBuilder : MonoBehaviour
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

    protected virtual void OnDrawGizmos()
    {
        var lightLocalPositions = signalDatas.Select(x => x.localPosition).ToList();

        var lightCount = lightLocalPositions.Count;

        if (lightCount < 1)
        {
            return;
        }        

        for (int i = 0; i < lightCount; i++)
        {
            var start = transform.TransformPoint(lightLocalPositions[i]);
            Map.Draw.Gizmos.DrawArrow(start, start + transform.forward * 2f, VectorMapSignalLight.GetTypeColor(signalDatas[i]), Map.Autoware.VectorMapTool.ARROWSIZE * 1f/* * 1.5f*/, arrowPositionRatio:1);
        }
    }
}
