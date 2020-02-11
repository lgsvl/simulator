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
    using Server;
    using Shared;
    using Shared.Configs;
    using Shared.Connection;
    using Shared.Messaging;
    using Shared.Messaging.Data;
    using UnityEngine;

    /// <summary>
    /// The root component for the mocked objects
    /// </summary>
    public abstract class MockedObjectsRoot : MonoBehaviour, IMessageReceiver
    {
        /// <summary>
        /// Data of the instantiated object
        /// </summary>
        protected struct InstantiatedObjectData
        {
            /// <summary>
            /// Instantiated object prefab id in the NetworkingConfig
            /// </summary>
            public int PrefabId { get; }

            /// <summary>
            /// Reference to instantiated mocked object game object
            /// </summary>
            public GameObject InstantiatedGameObject { get; }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="prefabId">Instantiated object prefab id in the NetworkingConfig</param>
            /// <param name="instantiatedGameObject">Reference to instantiated mocked object game object</param>
            public InstantiatedObjectData(int prefabId, GameObject instantiatedGameObject)
            {
                PrefabId = prefabId;
                InstantiatedGameObject = instantiatedGameObject;
            }
        }

        /// <summary>
        /// Currently registered mocked objects
        /// </summary>
        private readonly List<MockedObject> registeredObjects = new List<MockedObject>();

        /// <summary>
        /// List of instantiated objects via this root
        /// </summary>
        protected readonly List<InstantiatedObjectData> instantiatedObjects = new List<InstantiatedObjectData>();

        /// <summary>
        /// Is this mocked objects roots initialized
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Receiver key to bind with server's distributed objects root
        /// </summary>
        public string Key { get; } = "DistributedObjectsRoot";

        /// <summary>
        /// Inbox key to bind with server's outbox
        /// </summary>
        public string InboxKey { get; } = "DistributedObjectsRootOutbox";

        /// <summary>
        /// Messages manager where this root is registered as receiver
        /// </summary>
        public abstract MessagesManager MessagesManager { get; }

        /// <summary>
        /// Networking config
        /// </summary>
        protected abstract NetworkSettings Settings { get; }

        /// <summary>
        /// Event invoked when new mocked object is registered to this root
        /// </summary>
        public event Action<MockedObject> NewMockedObjectRegistered;

        /// <summary>
        /// Event invoked when mocked object is being unregistered from this root
        /// </summary>
        public event Action<MockedObject> MockedObjectUnregistered;

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
        protected virtual void Initialize()
        {
            if (IsInitialized)
                return;
            MessagesManager.RegisterObject(this);
            IsInitialized = true;
        }

        /// <summary>
        /// Deinitialization method
        /// </summary>
        protected virtual void Deinitialize()
        {
            if (!IsInitialized)
                return;
            for (var i = registeredObjects.Count - 1; i >= 0; i--)
                registeredObjects[i].Deinitialize();
            MessagesManager.UnregisterObject(this);
            IsInitialized = false;
        }

        /// <summary>
        /// Register new mocked object to this root
        /// </summary>
        /// <param name="newObject">New mocked object to be registered</param>
        public void RegisterObject(MockedObject newObject)
        {
            if (registeredObjects.Contains(newObject))
                return;
            MessagesManager.RegisterObject(newObject);
            registeredObjects.Add(newObject);
            NewMockedObjectRegistered?.Invoke(newObject);
        }

        /// <summary>
        /// Unregister mocked object from this root
        /// </summary>
        /// <param name="objectToUnregister">Mocked object to be unregistered</param>
        public void UnregisterObject(MockedObject objectToUnregister)
        {
            if (!registeredObjects.Contains(objectToUnregister))
                return;
            MessagesManager.UnregisterObject(objectToUnregister);
            registeredObjects.Remove(objectToUnregister);
            MockedObjectUnregistered?.Invoke(objectToUnregister);
        }

        /// <inheritdoc/>
        public void ReceiveMessage(IPeerManager sender, Message message)
        {
            var commandType = (DistributedRootCommandType) message.Content.PopInt(
                ByteCompression.RequiredBytes<DistributedRootCommandType>());
            switch (commandType)
            {
                case DistributedRootCommandType.InstantiateDistributedObject:
                    InstantiatePrefab(message.Content.PopInt(),
                        message.Content.PopString(),
                        message.Content.PopString());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Instantiates new prefab 
        /// </summary>
        public void InstantiatePrefab(int prefabId, string relativePath, string objectName)
        {
            if (prefabId < 0 || prefabId >= Settings.DistributedObjectPrefabs.Length)
                throw new ArgumentException(
                    $"Prefab of distributed object with id {prefabId} is not defined in {typeof(NetworkSettings).Name}.");
            var rootTransform = transform;
            var newGameObject = Instantiate(Settings.DistributedObjectPrefabs[prefabId],
                rootTransform.position,
                rootTransform.rotation,
                HierarchyUtility.GetOrCreateChild(rootTransform, relativePath));
            newGameObject.name = objectName;
            var distributedObject = newGameObject.GetComponent<DistributedObject>();
            if (distributedObject == null)
                throw new ArgumentException(
                    $"Prefab of distributed object with id {prefabId} has no {typeof(DistributedObject).Name} component in the root game object.");
            instantiatedObjects.Add(new InstantiatedObjectData(prefabId, newGameObject));
        }
    }
}