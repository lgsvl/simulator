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
    using Identification;
    using Messaging;
    using Messaging.Data;
    using UnityEngine;

    /// <summary>
    /// Distributed object that is synchronized with the mocked objects on connected clients
    /// </summary>
    public class DistributedObject : MonoBehaviour, IMessageSender, IMessageReceiver
    {
//Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Determines if this object is distributed only to selected endpoints
        /// </summary>
        [SerializeField]
        private bool selectiveDistribution;
#pragma warning restore 0649

        /// <summary>
        /// Root component for this distributed object
        /// </summary>
        private DistributedObjectsRoot root;

        /// <summary>
        /// Sibling component that has GUID assigned, if available used as the Key base
        /// </summary>
        private IGloballyUniquelyIdentified guidSource;

        /// <summary>
        /// Cached IMessageSender key
        /// </summary>
        private string key;

        /// <summary>
        /// Currently registered distributed components
        /// </summary>
        private readonly List<DistributedComponent> registeredComponents = new List<DistributedComponent>();

        /// <summary>
        /// End points which will be addressed while broadcasting, requires selective distribution
        /// </summary>
        public List<IPEndPoint> AddressedEndPoints { get; } = new List<IPEndPoint>();

        /// <summary>
        /// Is this distributed object initialized
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Is this distributed component authoritative (sends data to other components)
        /// </summary>
        public bool IsAuthoritative { get; protected set; }

        /// <summary>
        /// Will this object be destroyed
        /// </summary>
        public bool WillBeDestroyed { get; private set; }

        /// <inheritdoc/>
        public string Key => string.IsNullOrEmpty(key)
            ? GuidSource == null
                ? key = $"{HierarchyUtilities.GetRelativePath(Root.transform, transform)}DistributedObject"
                : GuidSource.GUID == null
                    ? null
                    : key = $"{GuidSource.GUID}/DistributedObject"
            : key;

        /// <summary>
        /// Root component for this distributed object
        /// </summary>
        public DistributedObjectsRoot Root => root != null ? root : (root = LocateRoot());

        /// <summary>
        /// Determines if this object is distributed only to selected endpoints
        /// </summary>
        public bool SelectiveDistribution
        {
            get => selectiveDistribution;
            set => selectiveDistribution = value;
        }

        /// <summary>
        /// Sibling component that has GUID assigned, if available used as the Key base
        /// </summary>
        private IGloballyUniquelyIdentified GuidSource =>
            guidSource ?? (guidSource = GetComponent<IGloballyUniquelyIdentified>());

        /// <summary>
        /// Event invoked when distributed object is initialized
        /// </summary>
        public event Action Initialized;

        /// <summary>
        /// Event called when distributed object will be destroyed
        /// </summary>
        public event Action DestroyCalled;

        /// <summary>
        /// Event invoked when new distributed component is registered to this object
        /// </summary>
        public event Action<DistributedComponent> NewComponentRegistered;

        /// <summary>
        /// Event invoked when distributed component is being unregistered from this object
        /// </summary>
        public event Action<DistributedComponent> ComponentUnregistered;

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
        /// Unity OnDestroy method
        /// </summary>
        protected virtual void OnEnable()
        {
            if (!IsInitialized)
                return;

            var message = MessagesPool.Instance.GetMessage(4);
            message.Content.PushEnum<DistributedObjectCommandType>((int) DistributedObjectCommandType.Enable);
            message.AddressKey = Key;
            message.Type = DistributedMessageType.ReliableOrdered;
            BroadcastMessage(message);
        }

        /// <summary>
        /// Unity OnDestroy method
        /// </summary>
        protected virtual void OnDisable()
        {
            if (!IsInitialized)
                return;

            var message = MessagesPool.Instance.GetMessage(4);
            message.Content.PushEnum<DistributedObjectCommandType>((int) DistributedObjectCommandType.Disable);
            message.AddressKey = Key;
            message.Type = DistributedMessageType.ReliableOrdered;
            BroadcastMessage(message);
        }

        /// <summary>
        /// Calls initialize method, may be delayed if the GUID in the guidSource is not set yet
        /// </summary>
        public void CallInitialize()
        {
            if (Root == null)
            {
                WillBeDestroyed = true;
                DestroyCalled?.Invoke();
                Destroy(this);
            }
            else
            {
                if (GuidSource == null)
                    Initialize();
                else
                {
                    if (string.IsNullOrEmpty(GuidSource.GUID))
                        StartCoroutine(WaitForGUID(Initialize));
                    else
                        Initialize();
                }
            }
        }

        /// <summary>
        /// Method waiting while the GUID in the guidSource is not set
        /// </summary>
        /// <param name="callback">Callback called after GUID is set</param>
        /// <returns>IEnumerator</returns>
        private IEnumerator WaitForGUID(Action callback)
        {
            while (GuidSource != null && string.IsNullOrEmpty(GuidSource.GUID))
                yield return new WaitForEndOfFrame();
            callback();
        }

        /// <summary>
        /// Initialization method
        /// </summary>
        protected virtual void Initialize()
        {
            if (IsInitialized)
                return;
            IsAuthoritative = Root.AuthoritativeDistributionAsDefault;
            Root.RegisterObject(this);
            IsInitialized = true;
            Initialized?.Invoke();
            BroadcastMessage(GetCurrentStateMessage());
        }

        /// <summary>
        /// Construct message with current state
        /// </summary>
        /// <returns>Message with current state</returns>
        protected virtual DistributedMessage GetCurrentStateMessage()
        {
            var message = MessagesPool.Instance.GetMessage(4);
            if (gameObject.activeInHierarchy)
                message.Content.PushEnum<DistributedObjectCommandType>((int) DistributedObjectCommandType.Enable);
            else
                message.Content.PushEnum<DistributedObjectCommandType>((int) DistributedObjectCommandType.Disable);
            message.AddressKey = Key;
            message.Type = DistributedMessageType.ReliableOrdered;
            return message;
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
        /// Register new distributed component to this object
        /// </summary>
        /// <param name="component">New distributed component to be registered</param>
        public void RegisterComponent(DistributedComponent component)
        {
            if (registeredComponents.Contains(component))
                return;
            Root.MessagesManager.RegisterObject(component);
            registeredComponents.Add(component);
            NewComponentRegistered?.Invoke(component);
        }

        /// <summary>
        /// Unregister distributed component from this object
        /// </summary>
        /// <param name="component">Distributed component to be unregistered</param>
        public void UnregisterComponent(DistributedComponent component)
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
        private DistributedObjectsRoot LocateRoot()
        {
            var node = transform;
            while (node != null && root == null)
            {
                root = node.GetComponent<DistributedObjectsRoot>();
                node = node.parent;
            }

            return root;
        }

        /// <summary>
        /// Adds end point address to the selective distribution list
        /// </summary>
        /// <param name="endPoint">End point to be added to the selective distribution</param>
        /// <exception cref="ArgumentException">Selective distribution is disabled on the object.</exception>
        public void AddEndPointToSelectiveDistribution(IPEndPoint endPoint)
        {
            if (!SelectiveDistribution)
                throw new ArgumentException("Selective distribution is disabled on the object.");
            if (AddressedEndPoints.Contains(endPoint))
                return;
            AddressedEndPoints.Add(endPoint);
            if (!IsInitialized) return;
            UnicastInitialMessages(endPoint);
        }

        /// <summary>
        /// Removed end point address from the selective distribution list
        /// </summary>
        /// <param name="endPoint">End point from be added to the selective distribution</param>
        /// <exception cref="ArgumentException">Selective distribution is disabled on the object.</exception>
        public void RemoveEndPointFromSelectiveDistribution(IPEndPoint endPoint)
        {
            if (!SelectiveDistribution)
                throw new ArgumentException("Selective distribution is disabled on the object.");
            AddressedEndPoints.Remove(endPoint);
        }

        /// <inheritdoc/>
        public void UnicastMessage(IPEndPoint endPoint, DistributedMessage distributedMessage)
        {
            if (IsAuthoritative && (!SelectiveDistribution || AddressedEndPoints.Contains(endPoint)))
                Root.UnicastMessage(endPoint, distributedMessage);
        }

        /// <inheritdoc/>
        public void BroadcastMessage(DistributedMessage distributedMessage)
        {
            if (!IsAuthoritative)
                return;
            if (SelectiveDistribution)
                foreach (var addressedEndPoint in AddressedEndPoints)
                    Root.UnicastMessage(addressedEndPoint, distributedMessage);
            else
                Root.BroadcastMessage(distributedMessage);
        }

        /// <inheritdoc/>
        public void UnicastInitialMessages(IPEndPoint endPoint)
        {
            if (!IsAuthoritative)
                return;
            UnicastMessage(endPoint, GetCurrentStateMessage());
        }

        /// <inheritdoc/>
        public void ReceiveMessage(IPeerManager sender, DistributedMessage distributedMessage)
        {
            //Check if game object is not destroyed
            if (this ==null || gameObject == null)
                return;
            var commandType = distributedMessage.Content.PopEnum<DistributedObjectCommandType>();
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