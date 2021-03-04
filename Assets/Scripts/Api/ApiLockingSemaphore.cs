/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Net;
using System.Collections.Generic;
using Simulator.Network.Core.Connection;
using Simulator.Network.Core.Messaging.Data;
using Simulator.Network.Core.Threading;
using Simulator.Network.Core.Messaging;

namespace Simulator.Api
{
    using UnityEngine;

    public class ApiLockingSemaphore : IMessageSender, IMessageReceiver
    {
        private enum MessageType
        {
            LockingCommandExecuted = 0,
        }

        public LockingSemaphore ActionsSemaphore { get; } = new LockingSemaphore();

        public string Key => "ApiLockingSemaphore";
        
        public bool IsLocked => ActionsSemaphore.IsLocked;

        public bool IsUnlocked => ActionsSemaphore.IsUnlocked;

        private readonly List<ILockingCommand> lockingCommands = new List<ILockingCommand>();

        public void Initialize()
        {
            Loader.Instance.Network.MessagesManager?.RegisterObject(this);
        }

        public void Deinitialize()
        {
            Loader.Instance.Network.MessagesManager?.UnregisterObject(this);
            foreach (var lockingCommand in lockingCommands)
                lockingCommand.Executed -= LockingCommandOnExecuted;
            lockingCommands.Clear();
            ForceUnlock();
        }

        public void RegisterNewCommand(ILockingCommand lockingCommand)
        {
            if (lockingCommands.Contains(lockingCommand))
                return;
            lockingCommands.Add(lockingCommand);
            lockingCommand.Executed += LockingCommandOnExecuted;
            ActionsSemaphore.Lock();
            
            //Lock master simulation foreach client
            var masterManager = Loader.Instance.Network.Master;
            if (masterManager != null)
            {
                foreach (var client in masterManager.Clients)
                    ActionsSemaphore.Lock();
            }
        }

        private void LockingCommandOnExecuted(ILockingCommand executedCommand)
        {
            executedCommand.Executed -= LockingCommandOnExecuted;
            lockingCommands.Remove(executedCommand);
            ActionsSemaphore.Unlock();
            
            var isClient = Loader.Instance.Network.IsClient;
            if (!isClient) return;
            var message = MessagesPool.Instance.GetMessage(
                ByteCompression.RequiredBytes<MessageType>());
            message.AddressKey = Key;
            message.Content.PushEnum<MessageType>((int) MessageType.LockingCommandExecuted);
            message.Type = DistributedMessageType.ReliableOrdered;
            BroadcastMessage(message);
        }

        public void ForceUnlock()
        {
            // If an exception was thrown from an async api handler, make sure
            // we unlock the semaphore, if not done so already
            while (ActionsSemaphore.IsLocked) ActionsSemaphore.Unlock();
        }

        public void ReceiveMessage(IPeerManager sender, DistributedMessage distributedMessage)
        {
            var isMaster = Loader.Instance.Network.IsMaster;
            if (!isMaster)
                return;
            
            var messageType = distributedMessage.Content.PopEnum<MessageType>();
            switch (messageType)
            {
                case MessageType.LockingCommandExecuted:
                    ActionsSemaphore.Unlock();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void UnicastMessage(IPEndPoint endPoint, DistributedMessage distributedMessage)
        {
            Loader.Instance.Network.MessagesManager?.UnicastMessage(endPoint, distributedMessage);
        }

        public void BroadcastMessage(DistributedMessage distributedMessage)
        {
            Loader.Instance.Network.MessagesManager?.BroadcastMessage(distributedMessage);
        }

        public void UnicastInitialMessages(IPEndPoint endPoint)
        {
        }
    }
}