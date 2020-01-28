/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Master
{
    using Core.Components;
    using Core.Configs;
    using Core.Messaging;

    /// <summary>
    /// Root object for the distributed objects in the master simulation
    /// </summary>
    public class MasterObjectsRoot : DistributedObjectsRoot
    {
        /// <summary>
        /// Messages manager for outgoing messages via connection manager
        /// </summary>
        private MessagesManager messagesManager;
        
        /// <summary>
        /// Network settings for this simulation
        /// </summary>
        private NetworkSettings settings;

        /// <summary>
        /// Messages manager for outgoing messages via connection manager
        /// </summary>
        public override MessagesManager MessagesManager => messagesManager;

        /// <summary>
        /// Network settings for this simulation
        /// </summary>
        public override NetworkSettings Settings => settings;

        /// <inheritdoc/>
        public override bool AuthoritativeDistributionAsDefault => true;

        /// <summary>
        /// Set the messages manager for this root
        /// </summary>
        /// <param name="messagesManager">MessagesManager to be set</param>
        public void SetMessagesManager(MessagesManager messagesManager)
        {
            this.messagesManager = messagesManager;
        }

        /// <summary>
        /// Set the network settings for this root
        /// </summary>
        /// <param name="networkSettings">NetworkSettings to be set</param>
        public void SetSettings(NetworkSettings networkSettings)
        {
            settings = networkSettings;
        }
    }
}
