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
    using UnityEngine;
    using Messaging;
    using Messaging.Data;


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
        /// Coroutine of the UpdateSnapshots method
        /// </summary>
        private IEnumerator updateSnapshotsCoroutine;

        /// <summary>
        /// Coroutine of the ExtrapolateVelocities method
        /// </summary>
        private IEnumerator extrapolateCoroutine;

        /// <summary>
        /// Cached rigidbody component reference
        /// </summary>
        public Rigidbody CachedRigidbody =>
            cachedRigidbody ? cachedRigidbody : cachedRigidbody = GetComponent<Rigidbody>();

        /// <inheritdoc/>
        protected override string ComponentKey { get; } = "DistributedRigidbody";

        /// <inheritdoc/>
        protected override bool DestroyWithoutParent { get; } = true;

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
            if (!IsInitialized)
                return;
            if (ParentObject.IsAuthoritative)
            {
                if (updateSnapshotsCoroutine != null)
                    return;
                updateSnapshotsCoroutine = UpdateSnapshots();
                StartCoroutine(updateSnapshotsCoroutine);
                return;
            }
            CachedRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            CachedRigidbody.isKinematic = true;
            CachedRigidbody.interpolation = RigidbodyInterpolation.None;
            var colliders = CachedRigidbody.GetComponentsInChildren<Collider>();
            for (var i = 0; i < colliders.Length; i++)
            {
                var childCollider = colliders[i];
                childCollider.isTrigger = true;
            }
                
            if (SimulationType != MockingSimulationType.ApplySnapshotsOnly &&
                extrapolateCoroutine == null)
            {
                extrapolateCoroutine = ExtrapolateSnapshots();
                StartCoroutine(extrapolateCoroutine);
            }
        }
        
        /// <summary>
        /// Unity OnEnable method
        /// </summary>
        protected void OnEnable()
        {
            if (ParentObject.IsAuthoritative)
            {
                if (updateSnapshotsCoroutine != null)
                    return;
                updateSnapshotsCoroutine = UpdateSnapshots();
                StartCoroutine(updateSnapshotsCoroutine);
            }
            else
            {
                if (SimulationType == MockingSimulationType.ApplySnapshotsOnly ||
                    extrapolateCoroutine != null) return;
                extrapolateCoroutine = ExtrapolateSnapshots();
                StartCoroutine(extrapolateCoroutine);
            }
        }

        /// <summary>
        /// Unity OnDisable method
        /// </summary>
        protected void OnDisable()
        {
            updateSnapshotsCoroutine = null;
            extrapolateCoroutine = null;
        }

        /// <summary>
        /// Unity LateUpdate method
        /// </summary>
        protected IEnumerator UpdateSnapshots()
        {
            while (IsInitialized)
            {
                if (!(Time.time >= lastSnapshotTime + 1.0f / SnapshotsPerSecondLimit))
                {
                    yield return new WaitForEndOfFrame();
                    continue;
                }
                //Check if rigidbody is sleeping
                if (CachedRigidbody.IsSleeping() && Math.Abs(CachedRigidbody.velocity.magnitude) < 0.1f &&
                    Mathf.Abs(CachedRigidbody.angularVelocity.magnitude) < 0.1f)
                {
                    if (IsSleeping)
                    {
                        yield return new WaitForEndOfFrame();
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

                lastSnapshotTime = Time.time;
                yield return new WaitForEndOfFrame();
            }

            updateSnapshotsCoroutine = null;
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
                    yield return null;
                    continue;
                }

                var timeAfterNewestSnapshot =
                    (float) (DateTime.UtcNow - newestSnapshot.Timestamp).TotalMilliseconds / 1000.0f;
                //Apply extrapolation if there are at least two snapshots and extrapolate no longer than 100ms
                if (previousSnapshot.Timestamp == DateTime.MinValue || timeAfterNewestSnapshot > ExtrapolationLimit)
                {
                    CachedRigidbody.position = newestSnapshot.LocalPosition + transform.parent.position;
                    CachedRigidbody.rotation = newestSnapshot.Rotation;
                    yield return null;
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

                yield return null;
            }

            extrapolateCoroutine = null;
        }

        /// <inheritdoc/>
        protected override BytesStack GetSnapshot()
        {
            //Reverse order when writing to the stack
            var localPosition = CachedRigidbody.position - transform.parent.position;
            BytesStack bytesStack;
            switch (SimulationType)
            {
                case MockingSimulationType.ExtrapolateVelocities:
                    bytesStack = new BytesStack(ByteCompression.PositionRequiredBytes +
                                                ByteCompression.RotationMaxRequiredBytes + 2 * 3 * 3);
                    bytesStack.PushCompressedVector3(CachedRigidbody.angularVelocity, -10.0f, 10.0f, 2);
                    bytesStack.PushCompressedVector3(CachedRigidbody.velocity, -200.0f, 200.0f, 2);
                    bytesStack.PushCompressedRotation(CachedRigidbody.rotation);
                    bytesStack.PushCompressedPosition(localPosition);
                    return bytesStack;
                default:
                    bytesStack = new BytesStack(ByteCompression.PositionRequiredBytes +
                                                ByteCompression.RotationMaxRequiredBytes);
                    bytesStack.PushCompressedRotation(CachedRigidbody.rotation);
                    bytesStack.PushCompressedPosition(localPosition);
                    return bytesStack;
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

            //Other than unreliable messages include keyframes and require instant apply
            if (message.Type != MessageType.Unreliable)
            {
                var cachedTransform = transform;
                cachedTransform.localPosition = newestSnapshot.LocalPosition;
                cachedTransform.rotation = newestSnapshot.Rotation;
            }
        }
    }
}