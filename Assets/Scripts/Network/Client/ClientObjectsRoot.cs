/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Client
{
    using Core.Components;
    using Core.Configs;
    using Core.Messaging;
    
    /// <summary>
    /// Root object for the mocked objects in the client simulation
    /// </summary>
    public class ClientObjectsRoot : DistributedObjectsRoot
    {
        /// <summary>
        /// Messages manager for incoming messages via connection manager
        /// </summary>
        private MessagesManager messagesManager;
        
        /// <summary>
        /// Network settings for this simulation
        /// </summary>
        private NetworkSettings settings;

        /// <summary>
        /// Messages manager for incoming messages via connection manager
        /// </summary>
        public override MessagesManager MessagesManager => messagesManager;

        /// <summary>
        /// Network settings for this simulation
        /// </summary>
        public override NetworkSettings Settings => settings;

        /// <inheritdoc/>
        public override bool AuthoritativeDistributionAsDefault => false;

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
