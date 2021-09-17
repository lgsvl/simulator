/**
 * Copyright (c) 2019-2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

namespace Simulator.Api
{
    using Utilities;

    public struct DriveWaypoint : IWaypoint
    {
        public Vector3 Position;
        public float Speed;
        public float Acceleration;
        public Vector3 Angle;
        public float Idle;
        public bool Deactivate;
        public float TriggerDistance;
        public float TimeStamp;
        public WaypointTrigger Trigger;
        Vector3 IWaypoint.Position
        {
            get => Position;
            set => Position = value;
        }
        Vector3 IWaypoint.Angle
        {
            get => Angle;
            set => Angle = value;
        }

        IWaypoint IWaypoint.Clone()
        {
            return new DriveWaypoint()
            {
                Position = Position,
                Speed = Speed,
                Acceleration = Acceleration,
                Angle = Angle,
                Idle = Idle,
                Deactivate = Deactivate,
                TimeStamp = TimeStamp,
                Trigger = Trigger,
                TriggerDistance = TriggerDistance
            };
        }

        IWaypoint IWaypoint.GetControlPoint()
        {
            return new DriveWaypoint()
            {
                Position = Position,
                Speed = Speed,
                Acceleration = Acceleration,
                Angle = Angle,
                Idle = 0.0f,
                Deactivate = false,
                TimeStamp = -1.0f,
                Trigger = null,
                TriggerDistance = 0.0f
            };
        }
    }

    public struct WalkWaypoint : IWaypoint
    {
        public Vector3 Position;
        public float Speed;
        public float Acceleration;
        public float Idle;
        public float TriggerDistance;
        public WaypointTrigger Trigger;
        
        Vector3 IWaypoint.Position
        {
            get => Position;
            set => Position = value;
        }
        
        public Vector3 Angle { get; set; }

        IWaypoint IWaypoint.Clone()
        {
            var clone = new WalkWaypoint()
            {
                Position = Position,
                Speed = Speed,
                Acceleration = Acceleration,
                Idle = Idle,
                Trigger = Trigger,
                TriggerDistance = TriggerDistance
            };
            ((IWaypoint) clone).Angle = ((IWaypoint) this).Angle;
            return clone;
        }

        IWaypoint IWaypoint.GetControlPoint()
        {
            var clone = new WalkWaypoint()
            {
                Position = Position,
                Speed = Speed,
                Acceleration = Acceleration,
                Idle = Idle,
                Trigger = null,
                TriggerDistance = 0.0f
            };
            ((IWaypoint) clone).Angle = ((IWaypoint) this).Angle;
            return clone;
        }
    }
}
