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
public class WayPoint
{
    public Vector3 position;

    [HideInInspector]
    public IntersectionPath intersection;

    [HideInInspector]
    public TrafficPath parent;

    //in world space
    [HideInInspector]
    public Vector3 forwardVector;

    public bool IsClear()
    {
        return intersection == null || intersection.IsClear();
    }

    public Vector3 GetPosition()
    {
        return parent.transform.TransformPoint(position);
    }
 }

public class TrafficPath : MonoBehaviour, IEnumerable<WayPoint> {

    public List<WayPoint> waypoints;

    public int loopIndex = 0;

    protected virtual void Awake()
    {
        foreach(var wp in waypoints)
        {
            wp.parent = this;
        }
    }

    public virtual IEnumerator<WayPoint> GetEnumerator()
    {
        foreach(var wp in waypoints)
        {
            yield return wp;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}
