/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

namespace Simulator.Api
{
    public struct DriveWaypoint
    {
        public Vector3 Position;
        public float Speed;
        public Vector3 Angle;
        public float Idle;
        public bool Deactivate;
        public float TriggerDistance;
        public float TimeStamp;
    }

    public struct WalkWaypoint
    {
        public Vector3 Position;
        public float Idle;
        public float TriggerDistance;
    }
}
