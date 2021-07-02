/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

namespace Simulator.Map
{
    public class MapHolder : MonoBehaviour
    {
        public Transform trafficLanesHolder;
        public Transform intersectionsHolder;
        [HideInInspector]
        public float MapWaypointSize = 0.5f;
    }
}