/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Client.Components
{
    using System;
    using System.Collections;
    using Shared;
    using Shared.Messaging.Data;
    using UnityEngine;

    /// <summary>
    /// Mocked rigidbody component
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class MockedRigidbody : MockedComponent
    {
        /// <summary>
        /// Limit of the extrapolation in seconds, after this time rigidbody will be snapped to position from the last snapshot
        /// </summary>
        private const float ExtrapolationLimit = 0.3f;

        /// <summary>
        /// Type of the simulation applied in this mocked rigidbody
        /// </summary>
        public enum MockingSimulationType
        {
            ApplySnapshotsOnly,
            ExtrapolateVelocities
        }

        /// <summary>
        /// Data included in the snapshot
        /// </summary>
        private struct SnapshotData
        {
            public DateTime Timestamp;
            public Vector3 LocalPosition;
            public Quaternion Rotation;
            public Vector3 Velocity;
            public Vector3 AngularVelocity;
        }

        /// <summary>
        /// Cached rigidbody component reference
        /// </summary>
        private Rigidbody cachedRigidbody;

        /// <summary>
        /// Cached rigidbody component reference
        /// </summary>
        public Rigidbody CachedRigidbody =>
            cachedRigidbody ? cachedRigidbody : cachedRigidbody = GetComponent<Rigidbody>();

        /// <summary>
        /// Type of the simulation applied in this mocked rigidbody
        /// </summary>
        public MockingSimulationType SimulationType { get; set; }

        /// <summary>
        /// Newest received snapshot from the server
        /// </summary>
        private SnapshotData newestSnapshot;

        /// <summary>
        /// Previous (to the newest) received snapshot from the server
        /// </summary>
        private SnapshotData previousSnapshot;

        /// <inheritdoc/>
        protected override string ComponentKey { get; } = "DistributedRigidbody";

        /// <inheritdoc/>
        public override void Initialize()
        {
            base.Initialize();
            if (!IsInitialized)
                return;
            CachedRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            CachedRigidbody.isKinematic = true;
            CachedRigidbody.interpolation = RigidbodyInterpolation.None;
            if (SimulationType != MockingSimulationType.ApplySnapshotsOnly)
            {
                StartCoroutine(ExtrapolateSnapshots());
            }
        }

        /// <summary>
        /// Method extrapolating the position and rotation every fixed update based on rigidbody state from latest snapshots
        /// </summary>
        protected IEnumerator ExtrapolateSnapshots()
        {
            var previousAppliedVelocity = Vector3.zero;
            while (IsInitialized)
            {
                if (newestSnapshot.Timestamp == DateTime.MinValue)
                {
                    yield return new WaitForEndOfFrame();
                    continue;
                }

                var timeAfterNewestSnapshot =
                    (float) (DateTime.UtcNow - newestSnapshot.Timestamp).TotalMilliseconds / 1000.0f;
                //Apply extrapolation if there are at least two snapshots and extrapolate no longer than 100ms
                if (previousSnapshot.Timestamp == DateTime.MinValue || timeAfterNewestSnapshot > ExtrapolationLimit)
                {
                    CachedRigidbody.position = newestSnapshot.LocalPosition + transform.parent.position;
                    CachedRigidbody.rotation = newestSnapshot.Rotation;
                    yield return new WaitForEndOfFrame();
                    continue;
                }

                //Convert time to seconds
                var timeBetweenSnapshots =
                    (float) (newestSnapshot.Timestamp - previousSnapshot.Timestamp).TotalMilliseconds / 1000.0f;
                var t = (timeAfterNewestSnapshot + timeBetweenSnapshots) / timeBetweenSnapshots;
                switch (SimulationType)
                {
                    case MockingSimulationType.ExtrapolateVelocities:
                        // Limit the extrapolation of the rotation
                        var angularVelocity = Estimations.SphericalInterpolation(previousSnapshot.AngularVelocity,
                            newestSnapshot.AngularVelocity, t);
                        CachedRigidbody.rotation = newestSnapshot.Rotation *
                                                   Quaternion.Euler(
                                                       angularVelocity * (Mathf.Rad2Deg * timeAfterNewestSnapshot));

                        var velocity = Estimations.LinearInterpolation(previousSnapshot.Velocity,
                            newestSnapshot.Velocity, t);
                        //Apply velocity only if it has the same direction as the previous one
                        if (Vector3.Dot(velocity, previousAppliedVelocity) > 0)
                        {
                            var extrapolatedPosition =
                                newestSnapshot.LocalPosition + velocity * timeAfterNewestSnapshot;
                            CachedRigidbody.position = extrapolatedPosition + transform.parent.position;
                        }
                        else
                            CachedRigidbody.position =
                                newestSnapshot.LocalPosition + transform.parent.position;

                        previousAppliedVelocity = velocity;
                        break;
                }

                yield return new WaitForEndOfFrame();
            }
        }

        /// <inheritdoc/>
        protected override void ApplySnapshot(Message message)
        {
            if (message.Timestamp <= newestSnapshot.Timestamp)
                return;
            previousSnapshot = newestSnapshot;

            //Parse incoming snapshot
            newestSnapshot.LocalPosition = message.Content.PopDecompressedPosition();
            var position = newestSnapshot.LocalPosition + transform.parent.position;
            newestSnapshot.Rotation = message.Content.PopDecompressedRotation();
            newestSnapshot.Timestamp = message.Timestamp;
            switch (SimulationType)
            {
                case MockingSimulationType.ApplySnapshotsOnly:
                    CachedRigidbody.position = position;
                    CachedRigidbody.rotation = newestSnapshot.Rotation;
                    break;
                case MockingSimulationType.ExtrapolateVelocities:
                    if (message.Content.Count > 0)
                    {
                        newestSnapshot.Velocity = message.Content.PopDecompressedVector3(-200.0f, 200.0f, 2);
                        newestSnapshot.AngularVelocity = message.Content.PopDecompressedVector3(-10.0f, 10.0f, 2);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}