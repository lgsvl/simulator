/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;

public class TrafficRoad : MonoBehaviour {

    public static bool swapPaths = false;

    public TrafficPath right;
    public TrafficPath left;

    public TrafficPath GetRight()
    {
        return swapPaths ? left : right;
    }

    public TrafficPath GetLeft()
    {
        return swapPaths ? right : left;
    }

    public TrafficPath Get(SideOfRoad side)
    {
        if(side == SideOfRoad.LEFT)
            return GetLeft();
        else
            return GetRight();
    }
}
