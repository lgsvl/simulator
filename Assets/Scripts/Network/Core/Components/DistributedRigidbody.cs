/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Components
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Messaging;
    using Messaging.Data;
    using UnityEngine;

    /// <summary>
    /// Distributed rigidbody component
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class DistributedRigidbody : DistributedComponent
    {
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

            public DateTime ServerTimestamp;

            public Vector3 LocalPosition;

            public Quaternion Rotation;

            public Vector3 Velocity;

            public Vector3 AngularVelocity;
        }

        /// <summary>
        /// Limit of the extrapolation in seconds, after this time rigidbody will be snapped to position from the last snapshot
        /// </summary>
        private const float ExtrapolationLimit = 0.3f;

        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Type of the simulation applied in corresponding mocked rigidbodies
        /// </summary>
        [SerializeField]
        private MockingSimulationType simulationType;
#pragma warning restore 0649

        /// <summary>
        /// Is distributed rigidbody currently sleeping
        /// </summary>
        private bool isSleeping;

        /// <summary>
        /// Newest received snapshot from the server
        /// </summary>
        private SnapshotData newestSnapshot;

        /// <summary>
        /// Previous (to the newest) received snapshot from the server
        /// </summary>
        private SnapshotData previousSnapshot;

        /// <summary>
        /// Time when the last snapshot has been sent
        /// </summary>
        private float lastSnapshotTime = float.MinValue;

        /// <summary>
        /// Cached rigidbody component reference
        /// </summary>
        private Rigidbody cachedRigidbody;

        /// <summary>
        /// Cached rigidbody component reference
        /// </summary>
        public Rigidbody CachedRigidbody =>
            cachedRigidbody ? cachedRigidbody : cachedRigidbody = GetComponent<Rigidbody>();

        /// <inheritdoc/>
        protected override string ComponentKey { get; } = "DistributedRigidbody";

        /// <inheritdoc/>
        protected override bool DestroyWithoutParent { get; } = true;

        /// <inheritdoc/>
        protected override int SnapshotMaxSize { get; } = ByteCompression.PositionRequiredBytes +
                                                          ByteCompression.RotationMaxRequiredBytes +
                                                          2 * 3 * 3;

        /// <summary>
        /// Maximum snapshots sent per second
        /// </summary>
        public int SnapshotsPerSecondLimit { get; set; } = 60;

        /// <summary>
        /// Is distributed rigidbody currently sleeping
        /// </summary>
        public bool IsSleeping => isSleeping;

        /// <summary>
        /// Type of the simulation applied in corresponding mocked rigidbodies
        /// </summary>
        public MockingSimulationType SimulationType
        {
            get => simulationType;
            set => simulationType = value;
        }

        /// <inheritdoc/>
        public override void Initialize()
        {
            base.Initialize();
            if (!IsInitialized) return;

            if (ParentObject.IsAuthoritative)
                return;

            CachedRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            CachedRigidbody.isKinematic = true;
            CachedRigidbody.interpolation = RigidbodyInterpolation.None;
        }

        /// <inheritdoc/>
        protected override List<IEnumerator> GetRequiredCoroutines()
        {
            var coroutines = new List<IEnumerator>();
            if (ParentObject.IsAuthoritative)
            {
                coroutines.Add(UpdateSnapshots());
            }
            else
            {
                if (SimulationType == MockingSimulationType.ApplySnapshotsOnly) return coroutines;
                coroutines.Add(ExtrapolateSnapshots());
            }

            return coroutines;
        }

        /// <summary>
        /// Coroutine that sends updated snapshots if snapshot changes
        /// </summary>
        private IEnumerator UpdateSnapshots()
        {
            var waitForEndOffFrame = new WaitForEndOfFrame();
            while (IsInitialized)
            {
                if (!(Time.unscaledTime >= lastSnapshotTime + 1.0f / SnapshotsPerSecondLimit))
                {
                    yield return waitForEndOffFrame;

                    continue;
                }

                //Check if rigidbody is sleeping
                if (CachedRigidbody.IsSleeping() &&
                    Math.Abs(CachedRigidbody.velocity.magnitude) < 0.1f &&
                    Mathf.Abs(CachedRigidbody.angularVelocity.magnitude) < 0.1f)
                {
                    if (IsSleeping)
                    {
                        yield return waitForEndOffFrame;

                        continue;
                    }

                    BroadcastSnapshot(true);
                    isSleeping = true;
                }
                else
                {
                    isSleeping = false;
                    BroadcastSnapshot();
                }

                lastSnapshotTime = Time.unscaledTime;
                yield return waitForEndOffFrame;
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
                if (previousSnapshot.Timestamp == DateTime.MinValue)
                {
                    yield return null;
                    continue;
                }

                var timeAfterNewestSnapshot =
                    (float) (DateTime.UtcNow - newestSnapshot.Timestamp).TotalSeconds;

                //Apply extrapolation if there are at least two snapshots and extrapolate no longer than 100ms
                if (previousSnapshot.Timestamp == DateTime.MinValue || timeAfterNewestSnapshot > ExtrapolationLimit)
                {
                    CachedRigidbody.MovePosition(newestSnapshot.LocalPosition + transform.parent.position);
                    CachedRigidbody.MoveRotation(newestSnapshot.Rotation);
                    yield return null;

                    continue;
                }

                //Convert time to seconds
                var timeBetweenSnapshots =
                    (float) (newestSnapshot.Timestamp - previousSnapshot.Timestamp).TotalSeconds;
                var t = (timeAfterNewestSnapshot + timeBetweenSnapshots) / timeBetweenSnapshots;
                // Limit the extrapolation of the rotation
                var angularVelocity = Estimations.SphericalInterpolation(
                    previousSnapshot.AngularVelocity,
                    newestSnapshot.AngularVelocity,
                    t);
                CachedRigidbody.MoveRotation(newestSnapshot.Rotation *
                                             Quaternion.Euler(
                                                 angularVelocity * (Mathf.Rad2Deg * timeAfterNewestSnapshot)));

                var velocity = Estimations.LinearInterpolation(
                    previousSnapshot.Velocity,
                    newestSnapshot.Velocity,
                    t);

                //Apply velocity only if it has the same direction as the previous one
                if (Vector3.Dot(velocity, previousAppliedVelocity) > 0)
                {
                    var velocityCorrection = velocity * timeAfterNewestSnapshot;
                    var extrapolatedPosition =
                        newestSnapshot.LocalPosition + velocityCorrection;
                    CachedRigidbody.MovePosition(extrapolatedPosition + transform.parent.position);
                }
                else
                    CachedRigidbody.MovePosition(
                        newestSnapshot.LocalPosition + transform.parent.position);

                previousAppliedVelocity = velocity;

                yield return null;
            }
        }

        /// <inheritdoc/>
        protected override bool PushSnapshot(BytesStack messageContent)
        {
            //Reverse order when writing to the stack
            var localPosition = CachedRigidbody.position - transform.parent.position;
            try
            {
                switch (SimulationType)
                {
                    case MockingSimulationType.ExtrapolateVelocities:
                        messageContent.PushCompressedVector3(CachedRigidbody.angularVelocity, -10.0f, 10.0f, 2);
                        messageContent.PushCompressedVector3(CachedRigidbody.velocity, -200.0f, 200.0f, 2);
                        messageContent.PushCompressedRotation(CachedRigidbody.rotation);
                        messageContent.PushCompressedPosition(localPosition);
                        break;
                    default:
                        messageContent.PushCompressedRotation(CachedRigidbody.rotation);
                        messageContent.PushCompressedPosition(localPosition);
                        break;
                }
            }
            catch (ArgumentException exception)
            {
                Debug.LogError($"{GetType().Name} could not push a snapshot. Exception: {exception.Message}.");
                return false;
            }

            return true;
        }

        /// <inheritdoc/>
        protected override void ApplySnapshot(DistributedMessage distributedMessage)
        {
            if (distributedMessage.ServerTimestamp <= newestSnapshot.ServerTimestamp) return;

            previousSnapshot = newestSnapshot;

            //Parse incoming snapshot
            newestSnapshot.LocalPosition = distributedMessage.Content.PopDecompressedPosition();
            var position = newestSnapshot.LocalPosition + transform.parent.position;
            newestSnapshot.Rotation = distributedMessage.Content.PopDecompressedRotation();
            newestSnapshot.Timestamp = distributedMessage.Timestamp;
            newestSnapshot.ServerTimestamp = distributedMessage.ServerTimestamp;
            switch (SimulationType)
            {
                case MockingSimulationType.ApplySnapshotsOnly:
                    CachedRigidbody.position = position;
                    CachedRigidbody.rotation = newestSnapshot.Rotation;
                    break;
                case MockingSimulationType.ExtrapolateVelocities:
                    if (distributedMessage.Content.Count > 0)
                    {
                        newestSnapshot.Velocity = distributedMessage.Content.PopDecompressedVector3(-200.0f, 200.0f, 2);
                        newestSnapshot.AngularVelocity =
                            distributedMessage.Content.PopDecompressedVector3(-10.0f, 10.0f, 2);
                    }

                    break;
                default: throw new ArgumentOutOfRangeException();
            }

            //Other than unreliable messages include keyframes and require instant apply
            if (distributedMessage.Type != DistributedMessageType.Unreliable)
            {
                var cachedTransform = transform;
                cachedTransform.localPosition = newestSnapshot.LocalPosition;
                cachedTransform.rotation = newestSnapshot.Rotation;
            }
        }
    }
}