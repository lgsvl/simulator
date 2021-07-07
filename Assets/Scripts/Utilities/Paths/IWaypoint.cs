/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Utilities
{
    using UnityEngine;

    public interface IWaypoint
    {
        public Vector3 Position { get; set; }
        
        public Vector3 Angle { get; set; }

        public IWaypoint Clone();

        public IWaypoint GetControlPoint();
    }
}