/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Components
{
    using System.Net;
    using Connection;
    using Messaging;
    using Messaging.Data;
    using UnityEngine;

    /// <summary>
    /// Distributed component that is synchronized with the mocked components on connected clients
    /// </summary>
    public abstract class DistributedComponent : MonoBehaviour, IMessageSender, IMessageReceiver
    {
        /// <summary>
        /// Cached IMessageReceiver key
        /// </summary>
        private string key;

        /// <summary>
        /// Parent distributed object of this component
        /// </summary>
        private DistributedObject parentObject;

        /// <summary>
        /// Is this distributed component initialized
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Component key required to bind with corresponding mocked component
        /// </summary>
        protected abstract string ComponentKey { get; }

        /// <summary>
        /// Should the component be destroyed when the parent object is not available
        /// </summary>
        protected virtual bool DestroyWithoutParent { get; } = false;

        /// <summary>
        /// Expected maximum size of the snapshots
        /// </summary>
        protected virtual int SnapshotMaxSize { get; } = 0;

        /// <inheritdoc/>
        public string Key => key ?? (key =
                                 $"{ParentObject.Key}/{HierarchyUtilities.GetRelativePath(ParentObject.transform, transform)}{ComponentKey}"
                             );

        /// <summary>
        /// Parent distributed object of this component
        /// </summary>
        public DistributedObject ParentObject => parentObject ? parentObject : parentObject = LocateParentObject();

        /// <summary>
        /// Unity Start method
        /// </summary>
        protected virtual void Start()
        {
            if (ParentObject == null)
            {
                if (DestroyWithoutParent)
                    SelfDestroy();
            }
            else
            {
                if (ParentObject.IsInitialized)
                    Initialize();
                else if (ParentObject.WillBeDestroyed){
                    if (DestroyWithoutParent)
                        SelfDestroy();
                } else
                {
                    ParentObject.Initialized += Initialize;
                    ParentObject.DestroyCalled += SelfDestroy;
                }
            }
        }

        /// <summary>
        /// Unity OnDestroy method
        /// </summary>
        protected virtual void OnDestroy()
        {
            Deinitialize();
        }

        protected virtual void SelfDestroy()
        {
            Destroy(this);
        }

        /// <summary>
        /// Initialization method
        /// </summary>
        public virtual void Initialize()
        {
            if (IsInitialized)
                return;
            ParentObject.Initialized -= Initialize;
            ParentObject.DestroyCalled -= SelfDestroy;
            ParentObject.RegisterComponent(this);
            IsInitialized = true;
            BroadcastSnapshot(true);
        }

        /// <summary>
        /// Deinitialization method
        /// </summary>
        public virtual void Deinitialize()
        {
            if (!IsInitialized)
                return;
            ParentObject.UnregisterComponent(this);
            IsInitialized = false;
        }

        /// <summary>
        /// Locate distributed object in the hierarchy parent objects
        /// </summary>
        private DistributedObject LocateParentObject()
        {
            var node = transform;
            while (node != null && parentObject == null)
            {
                parentObject = node.GetComponent<DistributedObject>();
                node = node.parent;
            }

            return parentObject;
        }

        /// <inheritdoc/>
        public void UnicastMessage(IPEndPoint endPoint, DistributedMessage distributedMessage)
        {
            if (IsInitialized)
                ParentObject.UnicastMessage(endPoint, distributedMessage);
        }

        /// <inheritdoc/>
        public void BroadcastMessage(DistributedMessage distributedMessage)
        {
            if (IsInitialized)
                ParentObject.BroadcastMessage(distributedMessage);
        }

        /// <inheritdoc/>
        public void UnicastInitialMessages(IPEndPoint endPoint)
        {
            if (!ParentObject.IsAuthoritative)
                return;
            if (IsInitialized)
                UnicastSnapshot(endPoint);
        }

        /// <summary>
        /// Get current component snapshot
        /// </summary>
        /// <returns>Current component snapshot</returns>
        protected abstract void PushSnapshot(BytesStack messageContent);

        /// <summary>
        /// Gets snapshot message to be send
        /// </summary>
        /// <param name="reliableSnapshot">Should the snapshot be reliable</param>
        protected DistributedMessage GetSnapshotMessage(bool reliableSnapshot = false)
        {
            var message = MessagesPool.Instance.GetMessage(SnapshotMaxSize);
            PushSnapshot(message.Content);
            message.AddressKey = Key;
            message.Type = reliableSnapshot
                ? DistributedMessageType.ReliableUnordered
                : DistributedMessageType.Unreliable;
            return message;
        }

        /// <summary>
        /// Method broadcasting recently current snapshot
        /// </summary>
        /// <param name="reliableSnapshot">Should the snapshot be reliable</param>
        public virtual void BroadcastSnapshot(bool reliableSnapshot = false)
        {
            BroadcastMessage(GetSnapshotMessage(reliableSnapshot));
        }

        /// <summary>
        /// Method unicasting recently current snapshot
        /// </summary>
        /// <param name="endPoint">Endpoint of the target client</param>
        /// <param name="reliableSnapshot">Should the snapshot be reliable</param>
        protected virtual void UnicastSnapshot(IPEndPoint endPoint, bool reliableSnapshot = false)
        {
            UnicastMessage(endPoint, GetSnapshotMessage(reliableSnapshot));
        }

        /// <inheritdoc/>
        public void ReceiveMessage(IPeerManager sender, DistributedMessage distributedMessage)
        {
            if (!ParentObject.IsAuthoritative)
                ParseMessage(distributedMessage);
        }

        /// <summary>
        /// Parses the received message and calls proper abstract method
        /// </summary>
        /// <param name="distributedMessage">Received snapshot in the message</param>
        protected virtual void ParseMessage(DistributedMessage distributedMessage)
        {
            ApplySnapshot(distributedMessage);
        }

        /// <summary>
        /// Parsing received snapshot
        /// </summary>
        /// <param name="distributedMessage">Received snapshot in the message</param>
        protected abstract void ApplySnapshot(DistributedMessage distributedMessage);
    }
}