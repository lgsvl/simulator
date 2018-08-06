/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;

[System.Serializable]
public class InfractionNode {
    public Vector3 position;
    public Vector3 tangent;
    public bool doubleYellow = true;
    public Vector3 obstaclePosition;
    public Vector3 obstacleTangent;
}

public class TrafficPathRoad : MonoBehaviour {

    public InfractionNode[] infractionNodesLane1;
    public InfractionNode[] infractionNodesLane2;

    // Use this for initialization
    void Start () {
    
    }
    
    // Update is called once per frame
    void Update () {
    
    }
}
