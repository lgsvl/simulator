/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Client
{
    using System;
    using System.Collections.Generic;
    using Shared;
    using Shared.Connection;
    using Shared.Messaging;
    using Shared.Messaging.Data;
    using UnityEngine;

    /// <summary>
    /// Object that is mocking the real distributed object from the server
    /// </summary>
    public class MockedObject : MonoBehaviour, IMessageReceiver
    {
        /// <summary>
        /// Root component for this mocked object
        /// </summary>
        private MockedObjectsRoot root;

        /// <summary>
        /// Cached IMessageReceiver key
        /// </summary>
        private string key;
        
        /// <summary>
        /// Currently registered mocked components
        /// </summary>
        private readonly List<MockedComponent> registeredComponents = new List<MockedComponent>();

        /// <summary>
        /// Is this mocked object initialized
        /// </summary>
        public bool IsInitialized { get; private set; }
        
        /// <summary>
        /// Receiver key for the inbox
        /// </summary>
        public string Key => key ?? (key = HierarchyUtility.GetRelativePath(Root.transform, transform));

        /// <summary>
        /// Root component for this mocked object
        /// </summary>
        public MockedObjectsRoot Root => root ? root : (root = LocateRoot());
        

        /// <summary>
        /// Event invoked when new mocked component is registered to this object
        /// </summary>
        public event Action<MockedComponent> NewComponentRegistered;

        /// <summary>
        /// Event invoked when mocked component is being unregistered from this object
        /// </summary>
        public event Action<MockedComponent> ComponentUnregistered;

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
            if (Root == null)
            {
                Destroy(this);
                return;
            }
            Root.RegisterObject(this);
            IsInitialized = true;
        }

        /// <summary>
        /// Deinitialization method
        /// </summary>
        public virtual void Deinitialize()
        {
            if (!IsInitialized)
                return;
            for (var i = registeredComponents.Count - 1; i >= 0; i--)
                registeredComponents[i].Deinitialize();
            Root.UnregisterObject(this);
            IsInitialized = false;
        }
        

        /// <summary>
        /// Register new mocked component to this object
        /// </summary>
        /// <param name="component">New mocked component to be registered</param>
        public void RegisterComponent(MockedComponent component)
        {
            if (registeredComponents.Contains(component))
                return;
            registeredComponents.Add(component);
            Root.MessagesManager.RegisterObject(component);
            NewComponentRegistered?.Invoke(component);
        }
        
        /// <summary>
        /// Unregister mocked component from this object
        /// </summary>
        /// <param name="component">Mocked component to be unregistered</param>
        public void UnregisterComponent(MockedComponent component)
        {
            if (!registeredComponents.Contains(component))
                return;
            Root.MessagesManager.UnregisterObject(component);
            registeredComponents.Remove(component);
            ComponentUnregistered?.Invoke(component);
        }

        /// <summary>
        /// Locate root component in the hierarchy parent objects
        /// </summary>
        private MockedObjectsRoot LocateRoot()
        {
            var node = transform;
            while (node !=null && root == null)
            {
                root = node.GetComponent<MockedObjectsRoot>();
                node = node.parent;
            }

            return root;
        }
        
        /// <inheritdoc/>
        public void ReceiveMessage(IPeerManager sender, Message message)
        {
            var commandType = message.Content.PopEnum<DistributedObjectCommandType>();
            switch (commandType)
            {
                case DistributedObjectCommandType.Enable:
                    gameObject.SetActive(true);
                    break;
                case DistributedObjectCommandType.Disable:
                    gameObject.SetActive(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}