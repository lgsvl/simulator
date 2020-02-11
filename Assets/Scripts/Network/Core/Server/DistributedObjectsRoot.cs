/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Server
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using Shared;
    using Shared.Configs;
    using Shared.Messaging;
    using Shared.Messaging.Data;
    using UnityEngine;

    /// <summary>
    /// The root component for the distributed objects
    /// </summary>
    public abstract class DistributedObjectsRoot : MonoBehaviour, IMessageSender
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
            /// Reference to instantiated distributed object component
            /// </summary>
            public DistributedObject DistributedObject { get; }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="prefabId">Instantiated object prefab id in the NetworkingConfig</param>
            /// <param name="distributedObject">Reference to instantiated distributed object component</param>
            public InstantiatedObjectData(int prefabId, DistributedObject distributedObject)
            {
                PrefabId = prefabId;
                DistributedObject = distributedObject;
            }
        }

        /// <summary>
        /// Currently registered distributed objects
        /// </summary>
        private readonly List<DistributedObject> registeredObjects = new List<DistributedObject>();

        /// <summary>
        /// List of instantiated objects via this root
        /// </summary>
        protected readonly List<InstantiatedObjectData> instantiatedObjects = new List<InstantiatedObjectData>();

        /// <summary>
        /// Required bytes count for the distributed objects command type
        /// </summary>
        protected static readonly int CommandTypeRequiredBytes =
            ByteCompression.RequiredBytes<DistributedRootCommandType>();

        /// <summary>
        /// Is this distributed objects root initialized
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Sender key to bind with mocked objects roots on clients
        /// </summary>
        public virtual string Key { get; } = "DistributedObjectsRoot";

        /// <summary>
        /// Messages manager where this root is registered as sender
        /// </summary>
        public abstract MessagesManager MessagesManager { get; }

        /// <summary>
        /// Networking config
        /// </summary>
        protected abstract NetworkSettings Settings { get; }

        /// <summary>
        /// Event invoked when new distributed object is registered to this root
        /// </summary>
        public event Action<DistributedObject> NewDistributedObjectRegistered;

        /// <summary>
        /// Event invoked when distributed object is being unregistered from this root
        /// </summary>
        public event Action<DistributedObject> DistributedObjectUnregistered;

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
        /// Register new distributed object to this root
        /// </summary>
        /// <param name="newObject">New distributed object to be registered</param>
        public void RegisterObject(DistributedObject newObject)
        {
            if (registeredObjects.Contains(newObject))
                return;
            registeredObjects.Add(newObject);
            MessagesManager.RegisterObject(newObject);
            NewDistributedObjectRegistered?.Invoke(newObject);
        }

        /// <summary>
        /// Unregister distributed object from this root
        /// </summary>
        /// <param name="objectToUnregister">Distributed object to be unregistered</param>
        public void UnregisterObject(DistributedObject objectToUnregister)
        {
            if (!registeredObjects.Contains(objectToUnregister))
                return;
            MessagesManager.UnregisterObject(objectToUnregister);
            registeredObjects.Remove(objectToUnregister);
            DistributedObjectUnregistered?.Invoke(objectToUnregister);
        }

        /// <inheritdoc/>
        public void UnicastMessage(IPEndPoint endPoint, Message message)
        {
            MessagesManager.UnicastMessage(endPoint, message);
        }

        /// <inheritdoc/>
        public void BroadcastMessage(Message message)
        {
            MessagesManager.BroadcastMessage(message);
        }

        /// <inheritdoc/>
        public void UnicastInitialMessages(IPEndPoint endPoint)
        {
            for (var i = 0; i < instantiatedObjects.Count; i++)
            {
                var instantiatedObject = instantiatedObjects[i];
                if (instantiatedObject.DistributedObject == null)
                    continue;
                var bytesStack = GetInstantiationMessage(instantiatedObject.PrefabId, HierarchyUtility.GetRelativePath(
                    transform,
                    instantiatedObject.DistributedObject.transform), instantiatedObject.DistributedObject.name);
                UnicastMessage(endPoint, new Message(Key, bytesStack, MessageType.ReliableUnordered));
                instantiatedObject.DistributedObject.UnicastInitialMessages(endPoint);
            }
        }

        /// <summary>
        /// Instantiates selected prefab in the unique relative path
        /// </summary>
        /// <param name="prefabId">Id of instantiated prefab in <see cref="NetworkSettings.DistributedObjectPrefabs"/></param>
        /// <param name="relativePath">Relative path to the root where object will be created, can be changed if object name is not unique</param>
        /// <returns>Instantiated new distributed object</returns>
        /// <exception cref="ArgumentException">Invalid configuration of the instantiated prefab</exception>
        private DistributedObject InstantiatePrefab(int prefabId, string relativePath)
        {
            if (prefabId < 0 || prefabId >= Settings.DistributedObjectPrefabs.Length)
                throw new ArgumentException(
                    $"Prefab of distributed object with id {prefabId} is not defined in {typeof(NetworkSettings).Name}.");
            var distributedObjectParent = HierarchyUtility.GetOrCreateChild(transform, relativePath);
            var newGameObject = Instantiate(Settings.DistributedObjectPrefabs[prefabId], distributedObjectParent);
            HierarchyUtility.ChangeToUniqueName(newGameObject);
            var distributedObject = newGameObject.GetComponent<DistributedObject>();
            if (distributedObject == null)
                throw new ArgumentException(
                    $"Prefab of distributed object with id {prefabId} has no {typeof(DistributedObject).Name} component in the root game object.");
            instantiatedObjects.Add(new InstantiatedObjectData(prefabId, distributedObject));
            return distributedObject;
        }

        /// <summary>
        /// Constructs a 
        /// </summary>
        /// <param name="prefabId"></param>
        /// <param name="relativePath"></param>
        /// <param name="newObjectName"></param>
        /// <returns></returns>
        private BytesStack GetInstantiationMessage(int prefabId, string relativePath, string newObjectName)
        {
            var bytesStack = new BytesStack();
            bytesStack.PushString(newObjectName);
            bytesStack.PushString(relativePath);
            bytesStack.PushInt(prefabId, 4);
            bytesStack.PushInt((int) DistributedRootCommandType.InstantiateDistributedObject,
                CommandTypeRequiredBytes);
            return bytesStack;
        }

        /// <summary>
        /// Instantiates selected prefab in the unique relative path and broadcast it to connected mocked objects roots
        /// </summary>
        /// <param name="prefabId">Id of instantiated prefab in <see cref="NetworkSettings.DistributedObjectPrefabs"/></param>
        /// <param name="relativePath">Relative path to the root where object will be created, can be changed if object name is not unique</param>
        /// <returns>Instantiated new distributed object</returns>
        /// <exception cref="ArgumentException">Invalid configuration of the instantiated prefab</exception>
        public DistributedObject InstantiatePrefabAndBroadcast(int prefabId, string relativePath)
        {
            var distributedObject = InstantiatePrefab(prefabId, relativePath);
            BroadcastMessage(new Message(Key, GetInstantiationMessage(prefabId, relativePath, distributedObject.name),
                MessageType.ReliableUnordered));
            return distributedObject;
        }

        /// <summary>
        /// Instantiates selected prefab in the unique relative path and unicast it to selected mocked objects roots
        /// </summary>
        /// <param name="prefabId">Id of instantiated prefab in <see cref="NetworkSettings.DistributedObjectPrefabs"/></param>
        /// <param name="relativePath">Relative path to the root where object will be created, can be changed if object name is not unique</param>
        /// <param name="endPoints">End points where the instantiation message will be addressed</param>
        /// <returns>Instantiated new distributed object</returns>
        /// <exception cref="ArgumentException">Invalid configuration of the instantiated prefab</exception>
        public DistributedObject InstantiatePrefabSelectively(int prefabId, string relativePath,
            List<IPEndPoint> endPoints)
        {
            var distributedObject = InstantiatePrefab(prefabId, relativePath);
            distributedObject.SelectiveDistribution = true;
            foreach (var endPoint in endPoints)
            {
                distributedObject.AddEndPointToSelectiveDistribution(endPoint);
                UnicastMessage(endPoint, new Message(Key,
                    GetInstantiationMessage(prefabId, relativePath, distributedObject.name),
                    MessageType.ReliableUnordered));
            }

            return distributedObject;
        }
    }
}