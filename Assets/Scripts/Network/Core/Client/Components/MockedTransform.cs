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
    /// Mocked transform component
    /// </summary>
    public class MockedTransform : MockedComponent
    { 
        /// <inheritdoc/>
        protected override string ComponentKey { get; } = "DistributedTransform";

        /// <summary>
        /// Timestamp of the last received snapshot
        /// </summary>
        private DateTime lastSnapshotTimestamp;

        /// <inheritdoc/>
        protected override void ApplySnapshot(Message message)
        {
            if (message.Timestamp <= lastSnapshotTimestamp)
                return;
            lastSnapshotTimestamp = message.Timestamp;

            //Parse incoming snapshot
            transform.localPosition = message.Content.PopDecompressedPosition();
            transform.localRotation = message.Content.PopDecompressedRotation();
        }
    }
}