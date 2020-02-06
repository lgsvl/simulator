/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Components
{
    using System;
    using Messaging.Data;
    using UnityEngine;

    /// <summary>
    /// Distributed transform component
    /// </summary>
    public class DistributedTransform : DistributedComponent
    {
        /// <summary>
        /// Snapshot data being sent over the network
        /// </summary>
        private struct SnapshotData
        {
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
        }

        /// <summary>
        /// Maximum snapshots sent per second
        /// </summary>
        public int SnapshotsPerSecondLimit { get; set; } = 60;

        /// <summary>
        /// Time when the last snapshot has been sent
        /// </summary>
        private float lastSentSnapshotTime = float.MinValue;

        /// <summary>
        /// Timestamp of the last received snapshot
        /// </summary>
        private DateTime lastReceivedSnapshotTimestamp;

        /// <inheritdoc/>
        protected override string ComponentKey { get; } = "DistributedTransform";

        /// <summary>
        /// Last snapshot data which has been sent
        /// </summary>
        private SnapshotData lastSentSnapshot;

        /// <summary>
        /// Unity LateUpdate method
        /// </summary>
        protected void LateUpdate()
        {
            if (ParentObject.IsAuthoritative && Time.time >= lastSentSnapshotTime + 1.0f / SnapshotsPerSecondLimit &&
                AnySnapshotElementChanged(ByteCompression.PositionPrecision))
            {
                BroadcastSnapshot();
                lastSentSnapshotTime = Time.time;
            }
        }

        /// <summary>
        /// Checks if any element of the snapshot has changed by more than given precision
        /// </summary>
        /// <param name="precision">Precision when comparing current value with previous</param>
        /// <returns>Has any of the snapshot elements changed</returns>
        private bool AnySnapshotElementChanged(float precision)
        {
            var thisTransform = transform;
            var localRotation = thisTransform.localRotation;
            var localPosition = thisTransform.localPosition;
            //TODO improve checking rotation changes
            return Mathf.Abs(lastSentSnapshot.LocalPosition.x - localPosition.x) > precision ||
                   Mathf.Abs(lastSentSnapshot.LocalPosition.y - localPosition.y) > precision ||
                   Mathf.Abs(lastSentSnapshot.LocalPosition.z - localPosition.z) > precision ||
                   Mathf.Abs(lastSentSnapshot.LocalRotation.x - localRotation.x) > precision ||
                   Mathf.Abs(lastSentSnapshot.LocalRotation.y - localRotation.y) > precision ||
                   Mathf.Abs(lastSentSnapshot.LocalRotation.z - localRotation.z) > precision ||
                   Mathf.Abs(lastSentSnapshot.LocalRotation.w - localRotation.w) > precision;
        }

        /// <inheritdoc/>
        protected override BytesStack GetSnapshot()
        {
            //Reverse order when writing to the stack
            var bytesStack = new BytesStack(ByteCompression.PositionRequiredBytes +
                                            ByteCompression.RotationMaxRequiredBytes);
            var thisTransform = transform;
            var localRotation = thisTransform.localRotation;
            var localPosition = thisTransform.localPosition;
            bytesStack.PushCompressedRotation(localRotation);
            bytesStack.PushCompressedPosition(localPosition);
            lastSentSnapshot.LocalRotation = localRotation;
            lastSentSnapshot.LocalPosition = localPosition;
            return bytesStack;
        }

        /// <inheritdoc/>
        protected override void ApplySnapshot(Message message)
        {
            if (message.Timestamp <= lastReceivedSnapshotTimestamp)
                return;
            lastReceivedSnapshotTimestamp = message.Timestamp;

            //Parse incoming snapshot
            transform.localPosition = message.Content.PopDecompressedPosition();
            transform.localRotation = message.Content.PopDecompressedRotation();
        }
    }
}