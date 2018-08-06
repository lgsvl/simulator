/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum LightCorner { LL, LR, RL, RR }

public class TrafficIntersection : MonoBehaviour {

    private List<Transform> cachedTransforms;

    public List<Transform> GetPaths()
    {
        if(cachedTransforms != null && cachedTransforms.Count > 0)
            return cachedTransforms;

        Transform baseNode = transform.Find("intersectionnodes");
        var ts = new List<Transform>();
        foreach(Transform t in baseNode.transform)
        {
            ts.Add(t);
        }
        cachedTransforms = ts;
        return ts;
    }


}
