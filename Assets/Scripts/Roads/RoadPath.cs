/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class RoadPathNode
{
    public Vector3 position;
    public Vector3 tangent;

    public Vector3 obstacleSpawnPosition;
    public Vector3 obstacleSpawnTangent;

    public bool isInintersection = false;
}

[System.Serializable]
public class RoadPathNodeInfractions
{
    public Vector3 position;
    public Vector3 tangent;

    public bool doubleYellow = true;

}

public class RoadPath : MonoBehaviour {

    public RoadPathNode[] pathNodesLane1;
    public RoadPathNode[] pathNodesLane2;
    public RoadPathNodeInfractions[] infractionPathNodesLane1;
    public RoadPathNodeInfractions[] infractionPathNodesLane2;


    public List<RoadPathNode> tempPnodes1;
    public List<RoadPathNode> tempPnodes2;
    public List<RoadPathNodeInfractions> tempInodes1;
    public List<RoadPathNodeInfractions> tempInodes2;
    // Use this for initialization
    void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
