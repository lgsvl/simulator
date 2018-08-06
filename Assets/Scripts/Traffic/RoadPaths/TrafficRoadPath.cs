/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TrafficRoadPath : MonoBehaviour {

    public List<TrafficPathNode> paths;

    public TrafficPathNode GetNext(int i) {
        if(i < paths.Count)
            return paths[i];
        else
            return paths[i % paths.Count];
    }
}
