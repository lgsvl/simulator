/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator.Api;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Simulator.Network.Core.Identification;
using Simulator.Network.Shared;
using Simulator.Network.Core.Messaging;
using System.Net;
using Simulator.Network.Core;
using Simulator.Network.Core.Connection;
using Simulator.Network.Core.Messaging.Data;

namespace Simulator.Controllable
{
    using SimpleJSON;

    public class ControllableManager : MonoBehaviour, IMessageSender, IMessageReceiver
    {
        private class ControllableController : IIdentifiedObject
        {
            private string key;

            public string Key => key ?? (key = string.IsNullOrEmpty(Controllable.UID) ?
                    $"{HierarchyUtilities.GetPath(Controllable.transform)}{Controllable.GetType().Name}" : Controllable.UID
                );

            public IControllable Controllable { get; }

            public ControllableController(IControllable iControllable)
            {
                Controllable = iControllable;
            }
        }
        
        public string Key => "ControllableManager"; //Network IMessageSender key
        
        public readonly List<IControllable> Controllables = new List<IControllable>();

        private readonly Dictionary<IControllable, ControllableController> Controllers = new Dictionary<IControllable, ControllableController>();

        private readonly Dictionary<string, ControllableController> ControllersByUID = new Dictionary<string, ControllableController>();

        private IdsRegister idsRegister;

        private void Awake()
        {
            var messagesManager = Loader.Instance.Network.MessagesManager;
            if (messagesManager != null)
            {
                idsRegister = new IdsRegister(Loader.Instance.Network.MessagesManager,
                    new SimpleIdManager(), Loader.Instance.Network.IsMaster, "ControllablesIdsRegister");
                messagesManager.RegisterObject(this);
                messagesManager.RegisterObject(idsRegister);
                idsRegister.SelfRegister();
            }
        }

        private void Start()
        {
            var allControllables = FindObjectsOfType<MonoBehaviour>().OfType<IControllable>();
            foreach (var controllable in allControllables)
                RegisterControllable(controllable);
        }

        private void OnDestroy()
        {
            var messagesManager = Loader.Instance.Network.MessagesManager;
            if (messagesManager != null)
            {
                for (var i = Controllables.Count - 1; i >= 0; i--)
                {
                    var controllable = Controllables[i];
                    UnregisterControllable(controllable);
                }

                messagesManager.UnregisterObject(this);
                messagesManager.UnregisterObject(idsRegister);
                idsRegister = null;
            }
        }

        public void RegisterControllable(IControllable iControllable)
        {
            if (Controllables.Contains(iControllable))
                return;
            var controllable = new ControllableController(iControllable);
            if (iControllable.UID == null)
                iControllable.UID = System.Guid.NewGuid().ToString();
            Controllables.Add(iControllable);
            Controllers.Add(iControllable, controllable);
            ControllersByUID.Add(iControllable.UID, controllable);
            idsRegister?.RegisterObject(controllable);
        }

        public void UnregisterControllable(IControllable controllable)
        {
            if (Controllables.Contains(controllable))
                Controllables.Remove(controllable);
            if (ControllersByUID.ContainsKey(controllable.UID))
                ControllersByUID.Remove(controllable.UID);
            if (Controllers.TryGetValue(controllable, out var controller))
            {
                Controllers.Remove(controllable);
                idsRegister?.UnregisterObject(controller);
            }
        }

        public IControllable SpawnControllable(GameObject prefab, string uid, Vector3 pos, Quaternion rot, Vector3 velocity, Vector3 angVelocity)
        {
            var api = ApiManager.Instance;
            var obj = Instantiate(prefab, pos, rot, transform);

            var rb = obj.gameObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = velocity;
                rb.angularVelocity = angVelocity;
            }

            IControllable controllable = obj.GetComponent<IControllable>();
            if (controllable == null)
            {
                Destroy(obj);
                Debug.LogWarning($"Prefab missing IControllable component, spawning {prefab.name} aborted");
                return null;
            }

            controllable.UID = uid;
            controllable.Spawned = true;

            // Add components for auto light layers change
            var triggerCollider = obj.AddComponent<SphereCollider>();
            if (triggerCollider != null)
            {
                triggerCollider.radius = 0.3f;
                triggerCollider.isTrigger = true;
            }
            obj.AddComponent<AgentZoneController>();

            RegisterControllable(controllable);

            if (Loader.Instance.Network.IsClusterSimulation)
            {
                //Add required components for cluster simulation
                ClusterSimulationUtilities.AddDistributedComponents(obj);
            }

            return controllable;
        }

        public void RemoveControllable(string id, IControllable controllable)
        {
            UnregisterControllable(controllable);
            if (controllable.Spawned)
                Destroy(controllable.gameObject);
        }

        public void Reset()
        {
            for (var i = Controllables.Count - 1; i >= 0; i--)
                UnregisterControllable(Controllables[i]);

            var allControllables = FindObjectsOfType<MonoBehaviour>().OfType<IControllable>();
            foreach (var controllable in allControllables)
            {
                if (controllable.Spawned)
                    Destroy(controllable.gameObject);
                else
                    RegisterControllable(controllable);
            }
        }

        public bool TryGetControllable(string uid, out IControllable iControllable)
        {
            var result = ControllersByUID.TryGetValue(uid, out var controllable);
            iControllable = controllable?.Controllable;
            return result;
        }

        #region network

        public void DistributeCommand(IControllable controllable, ControlAction action)
        {
            if (!Loader.Instance.Network.IsClusterSimulation || Loader.Instance.Network.IsClient)
                return;
            if (!Controllers.ContainsKey(controllable))
                return;
            var message = MessagesPool.Instance.GetMessage();
            message.AddressKey = Key;
            message.Content.PushString(action.Value);
            message.Content.PushString(action.Action);
            idsRegister.PushId(message, Controllers[controllable]);
            message.Type = DistributedMessageType.ReliableOrdered;
            BroadcastMessage(message);
        }

        public void ReceiveMessage(IPeerManager sender, DistributedMessage distributedMessage)
        {
            if (!Loader.Instance.Network.IsClusterSimulation || !Loader.Instance.Network.IsClient)
                return;
            //Currently single control actions are supported
            var controllableId = idsRegister.PopId(distributedMessage.Content);
            var controller = idsRegister.ResolveObject(controllableId) as ControllableController;
            if (controller == null)
                return;
            var action = new ControlAction
            {
                Action = distributedMessage.Content.PopString(), Value = distributedMessage.Content.PopString()
            };
            controller.Controllable.Control(new List<ControlAction> {action});
        }

        public void UnicastMessage(IPEndPoint endPoint, DistributedMessage distributedMessage)
        {
            Loader.Instance.Network.MessagesManager?.UnicastMessage(endPoint, distributedMessage);
        }

        public void BroadcastMessage(DistributedMessage distributedMessage)
        {
            Loader.Instance.Network.MessagesManager?.BroadcastMessage(distributedMessage);
        }

        void IMessageSender.UnicastInitialMessages(IPEndPoint endPoint)
        {
            //TODO support reconnection - send instantiation messages to the peer
        }

        #endregion
    }

    public struct ControlAction
    {
        public string Action;
        public JSONNode Value;
    }

    public interface IControllable : IGloballyUniquelyIdentified
    {
        Transform transform { get; }
        GameObject gameObject { get; }

        bool Spawned { get; set; }
        string UID { get; set; }
        string ControlType { get; set; } // Control type of a controllable object (i.e., signal)
        string CurrentState { get; set; } // Current state of a controllable object (i.e., green)
        string[] ValidStates { get; } // Valid states (i.e., green, yellow, red)
        string[] ValidActions { get; } // Valid actions (i.e., trigger, wait)

        // Control policy defines rules for control actions
        List<ControlAction> DefaultControlPolicy { get; set; } // Default control policy
        List<ControlAction> CurrentControlPolicy { get; set; } // Control policy that's currently active

        /// <summary>Control a controllable object with a new control policy</summary>
        /// <param name="controlActions">A new control policy to control this object</param>
        void Control(List<ControlAction> controlActions);
    }
}