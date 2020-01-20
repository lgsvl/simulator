/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Server.Components
{
    using System;
    using Client.Components;
    using Shared.Messaging.Data;
    using UnityEngine;

    /// <summary>
    /// Distributed rigidbody component
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class DistributedRigidbody : DistributedComponent
    {
//Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Type of the simulation applied in corresponding mocked rigidbodies
        /// </summary>
        [SerializeField]
        private MockedRigidbody.MockingSimulationType simulationType;
#pragma warning restore 0649
        
        /// <summary>
        /// Is distributed rigidbody currently sleeping
        /// </summary>
        private bool isSleeping;

        /// <summary>
        /// Maximum snapshots sent per second
        /// </summary>
        public int SnapshotsPerSecondLimit { get; set; } = 60;

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

        /// <summary>
        /// Is distributed rigidbody currently sleeping
        /// </summary>
        public bool IsSleeping => isSleeping;

        /// <summary>
        /// Type of the simulation applied in corresponding mocked rigidbodies
        /// </summary>
        public MockedRigidbody.MockingSimulationType SimulationType
        {
            get => simulationType;
            set => simulationType = value;
        }

        /// <summary>
        /// Unity LateUpdate method
        /// </summary>
        protected void LateUpdate()
        {
            if (Time.time >= lastSnapshotTime + 1.0f / SnapshotsPerSecondLimit)
            {
                //Check if rigidbody is sleeping
                if (CachedRigidbody.IsSleeping() && Math.Abs(CachedRigidbody.velocity.magnitude) < 0.1f &&
                    Mathf.Abs(CachedRigidbody.angularVelocity.magnitude) < 0.1f)
                {
                    if (IsSleeping)
                        return;
                    BroadcastSnapshot(true);
                    isSleeping = true;
                }
                else
                {
                    isSleeping = false;
                    BroadcastSnapshot();
                }
                lastSnapshotTime = Time.time;
            }
        }

        /// <inheritdoc/>
        protected override BytesStack GetSnapshot()
        {
            //Reverse order when writing to the stack
            var localPosition = CachedRigidbody.position - transform.parent.position;
            BytesStack bytesStack;
            switch (SimulationType)
            {
                case MockedRigidbody.MockingSimulationType.ExtrapolateVelocities:
                    bytesStack = new BytesStack(ByteCompression.PositionRequiredBytes +
                                                ByteCompression.RotationMaxRequiredBytes+2*3*3);
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
        protected override void AddCorrespondingMock()
        {
            var mockedRigidbody = gameObject.AddComponent<MockedRigidbody>();
            mockedRigidbody.SimulationType = SimulationType;
        }
    }
}