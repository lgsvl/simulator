/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class TrafRoadPoint
{
    public Vector3 position;
}

public class InterpolatedPosition
{
    public int targetIndex;
    public Vector3 position;
}

[System.Serializable]
public class TrafRoad : ScriptableObject {

    public List<TrafRoadPoint> waypoints;

    public InterpolatedPosition GetInterpolatedPosition(float t)
    {
        float totalDist = 0f;
        for(int i = 1; i < waypoints.Count; i++)
        {
            totalDist += Vector3.Distance(waypoints[i].position, waypoints[i-1].position);
        }

        float workingDist = 0f;
        for(int i = 1; i < waypoints.Count; i++)
        {
            float thisDist = Vector3.Distance(waypoints[i].position, waypoints[i - 1].position);
            if((workingDist + thisDist) / totalDist >= t)
            {
                return new InterpolatedPosition() { targetIndex = i, position = Vector3.Lerp(waypoints[i - 1].position, waypoints[i].position, t - workingDist / totalDist) };
            }
            workingDist += thisDist;
        }
        return new InterpolatedPosition() { targetIndex = waypoints.Count - 1, position = waypoints[waypoints.Count - 1].position};
    }


}
