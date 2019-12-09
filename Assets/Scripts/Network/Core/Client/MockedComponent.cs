/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Client
{
    using Shared;
    using Shared.Connection;
    using Shared.Messaging;
    using Shared.Messaging.Data;
    using UnityEngine;

    /// <summary>
    /// Component that is mocking the real distributed component from the server
    /// </summary>
    public abstract class MockedComponent : MonoBehaviour, IMessageReceiver
    {
        /// <summary>
        /// Parent mocked object of this component
        /// </summary>
        private MockedObject parentObject;

        /// <summary>
        /// Cached IMessageReceiver key
        /// </summary>
        private string key;

        /// <summary>
        /// Is this distributed component initialized
        /// </summary>
        public bool IsInitialized { get; private set; }
        
        /// <summary>
        /// Component key required to bind with corresponding distributed component
        /// </summary>
        protected abstract string ComponentKey { get; }

        /// <inheritdoc/>
        public string Key => key ?? (key = $"{ParentObject.Key}{HierarchyUtility.GetRelativePath(ParentObject.transform, transform)}{ComponentKey}");

        /// <summary>
        /// Parent mocked object of this component
        /// </summary>
        public MockedObject ParentObject => parentObject ? parentObject : parentObject = LocateParentObject();

        /// <summary>
        /// Unity Start method
        /// </summary>
        protected virtual void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Unity OnDestroy method
        /// </summary>
        protected virtual void OnDestroy()
        {
            Deinitialize();
        }

        /// <summary>
        /// Initialization method
        /// </summary>
        public virtual void Initialize()
        {
            if (IsInitialized)
                return;
            if (ParentObject == null || !ParentObject.IsInitialized)
            {
                Destroy(this);
                return;
            }
            ParentObject.RegisterComponent(this);
            IsInitialized = true;
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
        /// Locate mocked object in the hierarchy parent objects
        /// </summary>
        private MockedObject LocateParentObject()
        {
            var node = transform;
            while (node != null && parentObject == null)
            {
                parentObject = node.GetComponent<MockedObject>();
                node = node.parent;
            }

            return parentObject;
        }

        /// <inheritdoc/>
        public void ReceiveMessage(IPeerManager sender, Message message)
        {
            ParseMessage(message);
        }

        /// <summary>
        /// Parses the received message and calls proper abstract method
        /// </summary>
        /// <param name="message">Received snapshot in the message</param>
        protected virtual void ParseMessage(Message message)
        {
            ApplySnapshot(message);
        }

        /// <summary>
        /// Parsing received snapshot
        /// </summary>
        /// <param name="message">Received snapshot in the message</param>
        protected abstract void ApplySnapshot(Message message);
    }
}