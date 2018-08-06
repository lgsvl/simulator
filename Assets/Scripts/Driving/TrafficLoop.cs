/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum SideOfRoad { LEFT, RIGHT }

[System.Serializable]
public class PathItem
{
    public TrafficRoad road;
    public SideOfRoad side;
}

public class TrafficLoop : MonoBehaviour, IEnumerable<WayPoint> {

    public List<PathItem> paths;
    public SideOfRoad direction;

    public IEnumerator<WayPoint> GetEnumerator()
    {
        while(true)
        {
            foreach(var r in paths)
            {
                foreach(var p in r.road.Get(r.side))
                {
                    yield return p;
                }

            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}
