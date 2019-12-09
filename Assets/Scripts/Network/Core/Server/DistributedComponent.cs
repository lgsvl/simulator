/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Server
{
    using System.Collections;
    using System.Net;
    using Components;
    using Shared;
    using Shared.Messaging;
    using Shared.Messaging.Data;
    using UnityEngine;

    /// <summary>
    /// Distributed component that is synchronized with the mocked components on connected clients
    /// </summary>
    public abstract class DistributedComponent : MonoBehaviour, IMessageSender
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

        /// <inheritdoc/>
        public string Key => key ?? (key =
                                 $"{ParentObject.Key}{HierarchyUtility.GetRelativePath(ParentObject.transform, transform)}{ComponentKey}"
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
                ReplaceWithMock();
            }
            else
            {
                if (ParentObject.IsInitialized)
                    Initialize();
                else if (ParentObject.WillBeDestroyed)
                    ReplaceWithMock();
                else
                {
                    ParentObject.Initialized += Initialize;
                    ParentObject.DestroyCalled += ReplaceWithMock;
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

        /// <summary>
        /// Replaces distributed component with corresponding mock component
        /// </summary>
        private void ReplaceWithMock()
        {
            if (ParentObject != null)
            {
                ParentObject.Initialized -= Initialize;
                ParentObject.DestroyCalled -= ReplaceWithMock;
            }

            AddCorrespondingMock();
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
            ParentObject.DestroyCalled -= ReplaceWithMock;
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
        public void UnicastMessage(IPEndPoint endPoint, Message message)
        {
            if (IsInitialized)
                ParentObject.UnicastMessage(endPoint, message);
        }

        /// <inheritdoc/>
        public void BroadcastMessage(Message message)
        {
            if (IsInitialized)
                ParentObject.BroadcastMessage(message);
        }

        /// <inheritdoc/>
        public void UnicastInitialMessages(IPEndPoint endPoint)
        {
            UnicastSnapshot(endPoint);
        }

        /// <summary>
        /// Get current component snapshot
        /// </summary>
        /// <returns>Current component snapshot</returns>
        protected abstract BytesStack GetSnapshot();

        /// <summary>
        /// Method broadcasting recently current snapshot
        /// </summary>
        /// <param name="reliableSnapshot">Should the snapshot be reliable</param>
        protected virtual void BroadcastSnapshot(bool reliableSnapshot = false)
        {
            BroadcastMessage(new Message(Key, GetSnapshot(),
                reliableSnapshot ? MessageType.ReliableUnordered : MessageType.Unreliable));
        }

        /// <summary>
        /// Method unicasting recently current snapshot
        /// </summary>
        /// <param name="endPoint">Endpoint of the target client</param>
        /// <param name="reliableSnapshot">Should the snapshot be reliable</param>
        protected virtual void UnicastSnapshot(IPEndPoint endPoint, bool reliableSnapshot = false)
        {
            UnicastMessage(endPoint, new Message(Key, GetSnapshot(),
                reliableSnapshot ? MessageType.ReliableUnordered : MessageType.Unreliable));
        }


        /// <summary>
        /// Method adding the corresponding mocked component to the same game object
        /// </summary>
        protected abstract void AddCorrespondingMock();
    }
}