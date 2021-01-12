/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator.Map;
using UnityEngine;

namespace Simulator
{
    public class VehicleLane : MonoBehaviour
    {
        public MapTrafficLane CurrentMapLane { get; private set; }
        private LayerMask LaneLayer;

        private void Start()
        {
            LaneLayer = LayerMask.NameToLayer("Lane");
            CurrentMapLane = null;
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.gameObject.layer == LaneLayer)
            {
                CurrentMapLane = other.GetComponentInParent<MapTrafficLane>();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.layer == LaneLayer)
            {
                CurrentMapLane = null;
            }
        }

        private void OnDisable()
        {
            CurrentMapLane = null;
        }
    }
}
