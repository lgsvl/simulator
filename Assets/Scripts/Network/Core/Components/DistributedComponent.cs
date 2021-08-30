/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Components
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Net;
    using Connection;
    using Messaging;
    using Messaging.Data;
    using Threading;
    using UnityEngine;

    /// <summary>
    /// Distributed component that is synchronized with the mocked components on connected clients
    /// </summary>
    public abstract class DistributedComponent : MonoBehaviour, IMessageSender, IMessageReceiver
    {
        /// <summary>
        /// Current state of the coroutines
        /// </summary>
        protected enum CoroutinesState
        {
            /// <summary>
            /// Coroutines are stopped
            /// </summary>
            Stopped,

            /// <summary>
            /// Coroutines are running on this game object
            /// </summary>
            RunningOnObject,

            /// <summary>
            /// Coroutines are running on the thread dispatcher
            /// </summary>
            RunningOnDispatcher
        }

        /// <summary>
        /// Current initialization state of this component
        /// </summary>
        public enum InitializationState
        {
            /// <summary>
            /// Component is currently deinitialized
            /// </summary>
            Deinitialized,
            
            /// <summary>
            /// Component is initializing asynchronously
            /// </summary>
            Initializing,
            
            /// <summary>
            /// Component is initialized
            /// </summary>
            Initialized
        }

        /// <summary>
        /// Cached IMessageReceiver key
        /// </summary>
        private string key;

        /// <summary>
        /// Parent distributed object of this component
        /// </summary>
        private DistributedObject parentObject;
        
        /// <summary>
        /// Current initialization state of this component
        /// </summary>
        public InitializationState State { get; protected set; }

        /// <summary>
        /// Is this distributed component initialized
        /// </summary>
        public bool IsInitialized => State == InitializationState.Initialized;

        /// <summary>
        /// Component key required to bind with corresponding mocked component
        /// </summary>
        protected abstract string ComponentKey { get; }

        /// <summary>
        /// Should the component be destroyed when the parent object is not available
        /// </summary>
        protected virtual bool DestroyWithoutParent { get; } = false;

        /// <summary>
        /// Is this distributed component authoritative (sends data to other components)
        /// </summary>
        protected bool IsAuthoritative => ParentObject.IsAuthoritative;

        /// <summary>
        /// Expected maximum size of the snapshots
        /// </summary>
        protected virtual int SnapshotMaxSize { get; } = 0;

        /// <inheritdoc/>
        public string Key =>
            key ??=
                $"{ParentObject.Key}/{HierarchyUtilities.GetRelativePath(ParentObject.transform, transform)}{ComponentKey}";

        /// <summary>
        /// Parent distributed object of this component
        /// </summary>
        public DistributedObject ParentObject => parentObject ? parentObject : parentObject = LocateParentObject();

        /// <summary>
        /// Required coroutines of this component
        /// </summary>
        protected List<IEnumerator> requiredCoroutines = new List<IEnumerator>();

        /// <summary>
        /// Current coroutines state
        /// </summary>
        protected CoroutinesState coroutinesState;

        /// <summary>
        /// Unity Start method
        /// </summary>
        protected virtual void Start()
        {
            CallInitialize();
        }

        /// <summary>
        /// Unity OnDestroy method
        /// </summary>
        protected virtual void OnDestroy()
        {
            Deinitialize();
        }

        /// <summary>
        /// Calls the initialize method, can be delayed if parent object is not initialized
        /// </summary>
        public virtual void CallInitialize()
        {
            if (State != InitializationState.Deinitialized)
                return;
            
            if (ParentObject == null)
            {
                if (DestroyWithoutParent)
                    SelfDestroy();
            }
            else
            {
                if (ParentObject.IsInitialized)
                    Initialize();
                else if (ParentObject.WillBeDestroyed)
                {
                    if (DestroyWithoutParent)
                        SelfDestroy();
                }
                else
                {
                    State = InitializationState.Initializing;
                    ParentObject.Initialized += Initialize;
                    ParentObject.DestroyCalled += SelfDestroy;
                }
            }
        }

        /// <summary>
        /// Unity OnEnable method
        /// </summary>
        protected virtual void OnEnable()
        {
            if (IsInitialized && ParentObject != null)
            {
                if (coroutinesState != CoroutinesState.Stopped)
                    return;

                StartRequiredCoroutines();
            }
        }

        /// <summary>
        /// Unity OnDisable method
        /// </summary>
        protected virtual void OnDisable()
        {
            if (coroutinesState == CoroutinesState.RunningOnObject)
            {
                requiredCoroutines = null;
                coroutinesState = CoroutinesState.Stopped;
            }
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
            if (State == InitializationState.Initializing)
            {
                ParentObject.Initialized -= Initialize;
                ParentObject.DestroyCalled -= SelfDestroy;
            }

            ParentObject.RegisterComponent(this);
            State = InitializationState.Initialized;
            BroadcastSnapshot(true);
            ParentObject.IsAuthoritativeChanged += ParentObjectOnIsAuthoritativeChanged;
            ParentObjectOnIsAuthoritativeChanged(ParentObject.IsAuthoritative);
        }

        /// <summary>
        /// Deinitialization method
        /// </summary>
        public virtual void Deinitialize()
        {
            switch (State)
            {
                case InitializationState.Deinitialized:
                    return;
                
                case InitializationState.Initializing:
                    ParentObject.Initialized -= Initialize;
                    ParentObject.DestroyCalled -= SelfDestroy;
                    State = InitializationState.Deinitialized;
                    return;
                
                case InitializationState.Initialized:
                    //Check if this object is currently being destroyed
                    if (this != null)
                    {
                        ParentObject.UnregisterComponent(this);
                        ParentObject.Initialized -= Initialize;
                        ParentObject.DestroyCalled -= SelfDestroy;
                        ParentObject.IsAuthoritativeChanged -= ParentObjectOnIsAuthoritativeChanged;
                    }

                    StopCoroutines();
                    State = InitializationState.Deinitialized;
                    break;
            }
        }

        /// <summary>
        /// Method that starts or stops required coroutines
        /// </summary>
        /// <param name="isAuthoritative">Is the parent <see cref="DistributedObject"/> authoritative</param>
        private void ParentObjectOnIsAuthoritativeChanged(bool isAuthoritative)
        {
            StopCoroutines();
            StartRequiredCoroutines();
        }

        /// <summary>
        /// Gets the required coroutines and starts them
        /// </summary>
        protected virtual void StartRequiredCoroutines()
        {
            if (coroutinesState != CoroutinesState.Stopped)
            {
                Log.Warning("Cannot start required coroutines. Stop coroutines before starting them again.");
                return;
            }

            requiredCoroutines = GetRequiredCoroutines();
            if (isActiveAndEnabled)
            {
                if (requiredCoroutines != null)
                    foreach (var requiredCoroutine in requiredCoroutines)
                    {
                        StartCoroutine(requiredCoroutine);
                    }

                coroutinesState = CoroutinesState.RunningOnObject;
            }
            else
            {
                if (requiredCoroutines != null)
                    foreach (var requiredCoroutine in requiredCoroutines)
                    {
                        ThreadingUtilities.Dispatcher.StartCoroutine(requiredCoroutine);
                    }

                coroutinesState = CoroutinesState.RunningOnDispatcher;
            }
        }

        /// <summary>
        /// Stops coroutines added to the required coroutines list and clears the list
        /// </summary>
        protected virtual void StopCoroutines()
        {
            switch (coroutinesState)
            {
                case CoroutinesState.Stopped:
                    break;
                case CoroutinesState.RunningOnObject:
                    if (requiredCoroutines != null)
                        foreach (var requiredCoroutine in requiredCoroutines)
                        {
                            if (requiredCoroutine != null)
                                StopCoroutine(requiredCoroutine);
                        }

                    requiredCoroutines = null;
                    coroutinesState = CoroutinesState.Stopped;
                    break;
                case CoroutinesState.RunningOnDispatcher:
                    if (requiredCoroutines != null && ThreadingUtilities.Dispatcher != null)
                        foreach (var requiredCoroutine in requiredCoroutines)
                        {
                            if (requiredCoroutine != null)
                                ThreadingUtilities.Dispatcher.StopCoroutine(requiredCoroutine);
                        }

                    requiredCoroutines = null;
                    coroutinesState = CoroutinesState.Stopped;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Gets the list of required coroutines to be running in the current component state
        /// </summary>
        /// <returns>List of required coroutines to be running</returns>
        protected virtual List<IEnumerator> GetRequiredCoroutines()
        {
            return null;
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
            if (IsInitialized && distributedMessage != null)
                ParentObject.UnicastMessage(endPoint, distributedMessage);
        }

        /// <inheritdoc/>
        public void BroadcastMessage(DistributedMessage distributedMessage)
        {
            if (IsInitialized && distributedMessage != null)
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
        /// <returns>True if snapshot was pushed, false otherwise</returns>
        protected abstract bool PushSnapshot(BytesStack messageContent);

        /// <summary>
        /// Gets snapshot message to be send
        /// </summary>
        /// <param name="reliableSnapshot">Should the snapshot be reliable</param>
        protected DistributedMessage GetSnapshotMessage(bool reliableSnapshot = false)
        {
            var message = MessagesPool.Instance.GetMessage(SnapshotMaxSize);
            if (!PushSnapshot(message.Content))
                return null;
            message.AddressKey = Key;
            message.Type = reliableSnapshot
                ? DistributedMessageType.ReliableOrdered
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
            if (ParentObject.IsAuthoritative)
                return;

            DistributedMessage messageCopy = null;
            if (ParentObject.ForwardMessages)
            {
                messageCopy = new DistributedMessage(distributedMessage);
                if (string.IsNullOrEmpty(messageCopy.AddressKey))
                    Log.Error("Null before");
            }

            ParseMessage(distributedMessage);
            if (ParentObject.ForwardMessages && messageCopy != null)
            {
                if (!string.IsNullOrEmpty(messageCopy.AddressKey))
                    BroadcastMessage(messageCopy);
                else
                    Log.Error("Null after");
            }
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