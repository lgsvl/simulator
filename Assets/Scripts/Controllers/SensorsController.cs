/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using SimpleJSON;
using Simulator.Components;
using Simulator.Network.Core.Shared.Connection;
using Simulator.Network.Core.Shared.Messaging;
using Simulator.Network.Core.Shared.Messaging.Data;
using Simulator.Sensors;
using Simulator.Utilities;
using UnityEngine;

public class SensorsController : MonoBehaviour, IMessageSender, IMessageReceiver
{
    private struct SensorInstanceController
    {
        private readonly JSONNode configuration;
        private readonly SensorBase instance;
        private bool enabled;
        
        public SensorBase Instance => instance;
        public JSONNode Configuration => configuration;
        public bool Enabled => enabled;

        public SensorInstanceController(JSONNode configuration, SensorBase instance)
        {
            this.configuration = configuration;
            this.instance = instance;
            
            //Negate current value so proper method can be called
            enabled = !instance.isActiveAndEnabled;
            if (enabled)
                Disable();
            else
                Enable();
        }

        public void Enable()
        {
            if (enabled)
                return;

            if (Instance != null)
                Instance.gameObject.SetActive(true);

            enabled = true;
            
        }

        public void Disable()
        {
            if (!enabled)
                return;

            if (Instance != null)
                Instance.gameObject.SetActive(false);
            enabled = false;
        }
    }

    private MessagesManager messagesManager;

    private BridgeClient bridgeClient;

    private Dictionary<string, SensorInstanceController> sensorsInstances = new Dictionary<string, SensorInstanceController>();

    public string Key => "AgentManager";

    public MessagesManager MessagesManager =>
        messagesManager ?? (messagesManager = SimulatorManager.Instance.Network.MessagesManager);

    private BridgeClient AgentBridgeClient
    {
        get
        {
            if (bridgeClient == null)
                bridgeClient = GetComponent<BridgeClient>();
            return bridgeClient;
        }
    }

    public event Action SensorsChanged;

    public void Start()
    {
        MessagesManager?.RegisterObject(this);
    }

    public void OnDestroy()
    {
        MessagesManager?.UnregisterObject(this);
    }

    static string GetSensorType(SensorBase sensor)
    {
        var type = sensor.GetType().GetCustomAttributes(typeof(SensorType), false)[0] as SensorType;
        return type.Name;
    }

    public void SetupSensors(string sensors)
    {
        var network = SimulatorManager.Instance.Network;
        if (!string.IsNullOrEmpty(sensors) && !network.IsClient)
        {
            InstantiateSensors(sensors);
            if (network.IsMaster && network.Master.Clients.Count > 0)
                DistributeSensors();
        }
    }

    private void InstantiateSensors(string sensors)
    {
        var available = Simulator.Web.Config.Sensors.ToDictionary(sensor => sensor.Name);
        var prefabs = Simulator.Web.Config.SensorPrefabs.ToDictionary(sensor => GetSensorType(sensor));

        var parents = new Dictionary<string, GameObject>()
        {
            {string.Empty, gameObject},
        };

        var agentController = GetComponent<AgentController>();
        var requested = JSONNode.Parse(sensors).Children.ToList();
        while (requested.Count != 0)
        {
            int requestedCount = requested.Count;

            foreach (var parent in parents.Keys.ToArray())
            {
                var parentObject = parents[parent];

                for (int i = 0; i < requested.Count; i++)
                {
                    var item = requested[i];
                    if (item["parent"].Value == parent)
                    {
                        var name = item["name"].Value;
                        var type = item["type"].Value;

                        SensorConfig config;
                        if (!available.TryGetValue(type, out config))
                        {
                            throw new Exception($"Unknown sensor type {type} for {gameObject.name} vehicle");
                        }

                        var sensor = CreateSensor(gameObject, parentObject, prefabs[type].gameObject, item);
                        var sensorBase = sensor.GetComponent<SensorBase>();
                        sensorBase.Name = name;
                        sensor.name = name;
                        SIM.LogSimulation(SIM.Simulation.SensorStart, name);
                        if (AgentBridgeClient != null)
                        {
                            sensor.GetComponent<SensorBase>().OnBridgeSetup(AgentBridgeClient.Bridge);
                        }

                        parents.Add(name, sensor);
                        requested.RemoveAt(i);
                        i--;
                        var sensorInstanceController = new SensorInstanceController(item, sensorBase);
                        sensorInstanceController.Enable();
                        agentController.AgentSensors.Add(sensorBase);
                        sensorsInstances.Add(name, sensorInstanceController);
                    }
                }
            }

            if (requestedCount == requested.Count)
            {
                throw new Exception(
                    $"Failed to create {requested.Count} sensor(s), cannot determine parent-child relationship");
            }
            SensorsChanged?.Invoke();
        }
    }

    private GameObject CreateSensor(GameObject agent, GameObject parent, GameObject prefab, JSONNode item)
    {
        Vector3 position;
        Quaternion rotation;

        var transform = item["transform"];
        if (transform == null)
        {
            position = parent.transform.position;
            rotation = parent.transform.rotation;
        }
        else
        {
            position = parent.transform.TransformPoint(transform.ReadVector3());
            rotation = parent.transform.rotation * Quaternion.Euler(transform.ReadVector3("pitch", "yaw", "roll"));
        }

        var sensor = Instantiate(prefab, position, rotation, agent.transform);

        var sb = sensor.GetComponent<SensorBase>();
        var sbType = sb.GetType();

        foreach (var param in item["params"])
        {
            var key = param.Key;
            var value = param.Value;

            var field = sbType.GetField(key);
            if (field == null)
            {
                throw new Exception(
                    $"Unknown {key} parameter for {item["name"].Value} sensor on {gameObject.name} vehicle");
            }

            if (field.FieldType.IsEnum)
            {
                try
                {
                    var obj = Enum.Parse(field.FieldType, value.Value);
                    field.SetValue(sb, obj);
                }
                catch (ArgumentException ex)
                {
                    throw new Exception(
                        $"Failed to set {key} field to {value.Value} enum value for {gameObject.name} vehicle, {sb.Name} sensor",
                        ex);
                }
            }
            else if (field.FieldType == typeof(Color))
            {
                if (ColorUtility.TryParseHtmlString(value.Value, out var color))
                {
                    field.SetValue(sb, color);
                }
                else
                {
                    throw new Exception(
                        $"Failed to set {key} field to {value.Value} color for {gameObject.name} vehicle, {sb.Name} sensor");
                }
            }
            else if (field.FieldType == typeof(bool))
            {
                field.SetValue(sb, value.AsBool);
            }
            else if (field.FieldType == typeof(int))
            {
                field.SetValue(sb, value.AsInt);
            }
            else if (field.FieldType == typeof(float))
            {
                field.SetValue(sb, value.AsFloat);
            }
            else if (field.FieldType == typeof(string))
            {
                field.SetValue(sb, value.Value);
            }
            else if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var type = field.FieldType.GetGenericArguments()[0];
                Type listType = typeof(List<>).MakeGenericType(new[] {type});
                System.Collections.IList list = (System.Collections.IList) Activator.CreateInstance(listType);

                if (type == typeof(float))
                {
                    foreach (var elemValue in value)
                    {
                        float elem = elemValue.Value.AsFloat;
                        list.Add(elem);
                    }
                }
                else
                {
                    foreach (var elemValue in value)
                    {
                        var elem = Activator.CreateInstance(type);

                        foreach (var elemField in type.GetFields())
                        {
                            var name = elemField.Name;

                            if (elemValue.Value[name].IsNumber)
                            {
                                elemField.SetValue(elem, elemValue.Value[name].AsFloat);
                            }
                            else if (elemValue.Value[name].IsString)
                            {
                                elemField.SetValue(elem, elemValue.Value[name].Value);
                            }
                        }

                        list.Add(elem);
                    }
                }

                field.SetValue(sb, list);
            }
            else
            {
                throw new Exception(
                    $"Unknown {field.FieldType} type for {key} field for {gameObject.name} vehicle, {sb.Name} sensor");
            }
        }

        return sensor;
    }

    public void RemoveSensor(string name)
    {
        if (!sensorsInstances.TryGetValue(name, out var sensorInstance)) return;
        GetComponent<AgentController>().AgentSensors.Remove(sensorInstance.Instance);
        Destroy(sensorInstance.Instance.gameObject);
        sensorsInstances.Remove(name);
    }

    private void DistributeSensors()
    {
        var network = SimulatorManager.Instance.Network;
        if (!network.IsMaster || network.Master.Clients.Count <= 0)
            return;

        var sensorsToDistribute = new List<SensorInstanceController>();
        foreach (var sensorData in sensorsInstances)
        {
            if (sensorData.Value.Instance.CanBeDelegatedToClient)
                sensorsToDistribute.Add(sensorData.Value);
        }

        var peerSensors = new JSONArray();

        //Distribute sensors between clients and master simulation
        var sensorsPerPeer = sensorsToDistribute.Count / network.Master.Clients.Count;
        var currentSensorI = 0;

        //Clients
        for (var i = 0; i < network.Master.Clients.Count; i++)
        {
            var client = network.Master.Clients[i];
            peerSensors = new JSONArray();
            for (; currentSensorI < sensorsPerPeer * (i + 1); currentSensorI++)
            {
                peerSensors.Add(sensorsToDistribute[currentSensorI].Configuration);
                sensorsToDistribute[currentSensorI].Disable();
            }

            var content = new BytesStack();
            content.PushString(peerSensors.ToString());
            var message = new Message(Key, content, MessageType.ReliableUnordered);
            UnicastMessage(client.Peer.PeerEndPoint, message);
        }
        SensorsChanged?.Invoke();
    }

    public void ReceiveMessage(IPeerManager sender, Message message)
    {
        var sensors = message.Content.PopString();
        InstantiateSensors(sensors);
    }

    /// <inheritdoc/>
    public void UnicastMessage(IPEndPoint endPoint, Message message)
    {
        if (Key != null)
            MessagesManager?.UnicastMessage(endPoint, message);
    }

    /// <inheritdoc/>
    public void BroadcastMessage(Message message)
    {
        if (Key != null)
            MessagesManager?.BroadcastMessage(message);
    }

    /// <inheritdoc/>
    void IMessageSender.UnicastInitialMessages(IPEndPoint endPoint)
    {
        //TODO support reconnection - send instantiation messages to the peer
    }
}