/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Shared.Messages
{
    using System.Collections.Generic;
    using System.Net;
    using Controllable;
    using Core.Connection;
    using Core.Messaging;
    using Core.Messaging.Data;
    using UnityEngine;

    /// <summary>
    /// <see cref="IControllable"/> implementation which distributes control actions from master to clients
    /// </summary>
    public abstract class DistributedControllable : MonoBehaviour, IControllable, IMessageSender, IMessageReceiver
    {
        /// <summary>
        /// Cached messages manager from the <see cref="SimulatorManager"/> instance
        /// </summary>
        private MessagesManager messagesManager;

        /// <inheritdoc/>
        public virtual bool Spawned { get; set; }

        /// <inheritdoc/>
        public virtual string UID { get; set; }

        /// <inheritdoc/>
        public virtual string Key { get; }

        /// <inheritdoc/>
        public virtual string ControlType { get; set; }
        
        /// <inheritdoc/>
        public virtual string CurrentState { get; set; }
        
        /// <inheritdoc/>
        public virtual string[] ValidStates { get; set; }
        
        /// <inheritdoc/>
        public virtual string[] ValidActions { get; set; }
        
        /// <inheritdoc/>
        public virtual string DefaultControlPolicy { get; set; }
        
        /// <inheritdoc/>
        public virtual string CurrentControlPolicy { get; set; }

        /// <summary>
        /// Unity Start method
        /// </summary>
        protected virtual void Start()
        {
            messagesManager = SimulatorManager.Instance.Network.MessagesManager;
            messagesManager?.RegisterObject(this);
        }

        /// <summary>
        /// Unity OnDestroy method
        /// </summary>
        protected virtual void OnDestroy()
        {
            messagesManager?.UnregisterObject(this);
        }

        /// <inheritdoc/>
        public void Control(List<ControlAction> controlActions)
        {
            HandleControlActions(controlActions);
            if (SimulatorManager.Instance.Network.IsMaster && controlActions.Count>0)
            {
                //Forward control actions to clients
                var serializedControlActions = new BytesStack();
                for (var i = 0; i < controlActions.Count; i++)
                {
                    var controlAction = controlActions[i];
                    serializedControlActions.PushString(controlAction.Action);
                    serializedControlActions.PushString(controlAction.Value);
                }
                BroadcastMessage(new Message(Key, serializedControlActions, MessageType.ReliableOrdered));
            }
        }

        /// <summary>Control a controllable object with a new control policy</summary>
        /// <param name="controlActions">A new control policy to control this object</param>
        protected abstract void HandleControlActions(List<ControlAction> controlActions);
        
        /// <inheritdoc/>
        public void ReceiveMessage(IPeerManager sender, Message message)
        {
            var controlActions = new List<ControlAction>();
            while (message.Content.Count > 0)
            {
                var action = message.Content.PopString();
                var value = message.Content.PopString();
                controlActions.Add(new ControlAction()
                {
                    Action = action,
                    Value = value
                });
            }
            if (controlActions.Count>0)
                HandleControlActions(controlActions);
        }

        /// <inheritdoc/>
        public void UnicastMessage(IPEndPoint endPoint, Message message)
        {
            if (Key != null)
                messagesManager?.UnicastMessage(endPoint, message);
        }

        /// <inheritdoc/>
        public void BroadcastMessage(Message message)
        {
            if (Key != null)
                messagesManager?.BroadcastMessage(message);
        }

        /// <inheritdoc/>
        void IMessageSender.UnicastInitialMessages(IPEndPoint endPoint)
        {
            //TODO support reconnection - send instantiation messages to the peer
        }
    }
}