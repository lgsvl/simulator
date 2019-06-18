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
    }

    public struct WalkWaypoint
    {
        public Vector3 Position;
        public float Idle;
    }
}
