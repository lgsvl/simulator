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
public class TrafIntersectionPath
{
    public TrafIntersectionEditNode start;
    public GameObject end;
    public List<int> giveWayTo;
    public int subId = 0;
    public GameObject light;

}

public class TrafIntersection : MonoBehaviour {

    public int systemId = -1;

    public TrafEntry owningEntry;

    public GameObject[] startPoints;
    public GameObject[] endPoints;
    public GameObject[] lights;
    public TrafSystem trafSystem;
    public bool stopSign = false;

    public Queue<TrafAIMotor> stopQueue;

    public List<TrafIntersectionPath> paths;

    void Awake()
    {
        stopQueue = new Queue<TrafAIMotor>();
    }
}
