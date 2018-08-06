/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class TrafficPathNode : MonoBehaviour {

    public bool isIntersection = false;
    public bool isIntersectionStart = false;
    public bool isIntersectionFinish = false;
    public TrafficPathNode intersectionStartNode;
    public bool positive = true;
    public int laneNum;
    public TrafficIntersection intersection;
    public GameObject intersectionObject;
    public TrafficPathRoad road;
    public bool isTrafficLights;

    public bool infractionsReversed;
    public TrafficPathNode infractionsprevious;
    public Transform tangent;

    public LightCorner corner;

    private int interestedParties = 0;

    public Vector3 obstaclePos;
    public Vector3 obstacleSpawn;

    public bool isClear()
    {
        return interestedParties == 0;
    }

    public void RegisterInterest()
    {
        interestedParties++;
    }

    public void RemoveInterest()
    {
        interestedParties--;
    }

    public List<TrafficPathNode> giveWayNodes;

    public bool MustGiveWay()
    {
        foreach(var node in giveWayNodes)
        {
            if(!node.isClear())
                return true;
        }
        return false;
    }

    public GameObject next;
    public GameObject nextInIntersection;
}
