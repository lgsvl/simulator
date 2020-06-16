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
using Simulator;
using Simulator.Components;
using Simulator.Network.Core;
using Simulator.Network.Core.Connection;
using Simulator.Network.Core.Messaging;
using Simulator.Network.Core.Messaging.Data;
using Simulator.Network.Shared;
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

    private string key;

    private MessagesManager messagesManager;

    private BridgeClient bridgeClient;

    private Dictionary<string, SensorInstanceController> sensorsInstances =
        new Dictionary<string, SensorInstanceController>();

    public string Key =>
        key ?? (key =
            $"{HierarchyUtilities.GetPath(transform)}SensorsController"
        );

    public MessagesManager MessagesManager =>
        messagesManager ?? (messagesManager = Loader.Instance.Network.MessagesManager);

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
        var sensorsManager = SimulatorManager.Instance.Sensors;
        foreach (var sensorInstanceController in sensorsInstances)
            sensorsManager.UnregisterSensor(sensorInstanceController.Value.Instance);
        MessagesManager?.UnregisterObject(this);
    }

    static string GetSensorType(SensorBase sensor)
    {
        var type = sensor.GetType().GetCustomAttributes(typeof(SensorType), false)[0] as SensorType;
        return type.Name;
    }

    public void SetupSensors(string sensors)
    {
        var network = Loader.Instance.Network;
        if (!string.IsNullOrEmpty(sensors) && !network.IsClient)
        {
            InstantiateSensors(sensors);
            if (network.IsMaster)
            {
                if (network.Master.State == SimulationState.Running)
                    DistributeSensors();
                else
                    network.Master.StateChanged += MasterOnStateChanged;
            }
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
        var baseLink = transform.GetComponentInChildren<BaseLink>();
        Debug.Log("looking for BaseLink");
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

                        var sensor = CreateSensor(gameObject, parentObject, prefabs[type].gameObject, item, baseLink);
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
                        if (SimulatorManager.InstanceAvailable)
                            SimulatorManager.Instance.Sensors.RegisterSensor(sensorBase);
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

    private GameObject CreateSensor(GameObject agent, GameObject parent, GameObject prefab, JSONNode item, BaseLink baseLink)
    {
        if (baseLink != null)
        {
            parent = parent == gameObject ? baseLink.gameObject : parent; // replace baselink with the default gameObject parent
        }
        var position = parent.transform.position;
        var rotation = parent.transform.rotation;

        var transform = item["transform"];
        if (transform != null)
        {
            position = parent.transform.TransformPoint(transform.ReadVector3());
            rotation = parent.transform.rotation * Quaternion.Euler(transform.ReadVector3("pitch", "yaw", "roll"));
        }

        var sensor = Instantiate(prefab, position, rotation, agent.transform);

        var sb = sensor.GetComponent<SensorBase>();
        sb.ParentTransform = parent.transform;

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

                if (type.IsEnum)
                {
                    foreach (var elemValue in value)
                    {
                        object elem;
                        try
                        {
                            elem = Enum.Parse(type, elemValue.Value);
                        }
                        catch (ArgumentException ex)
                        {
                            throw new Exception(
                                $"Failed to set {key} field to {value.Value} enum value for {gameObject.name} vehicle, {sb.Name} sensor",
                                ex);
                        }
                        list.Add(elem);
                    }
                }
                else if (type == typeof(float))
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
        var network = Loader.Instance.Network;
        var master = network.Master;
        var clients = master.Clients;
        if (!network.IsMaster || clients.Count <= 0)
            return;

        var clientsCount = clients.Count;
        var clientsSensors = new JSONArray[clientsCount];
        for (var i=0; i<clientsSensors.Length; i++)
            clientsSensors[i] = new JSONArray();

        //Order sensors by distribution type, so ultra high loads will be handled first
        var sensorsByDistributionType =
            sensorsInstances.Values
                .Where(controller =>
                    controller.Instance.DistributionType != SensorBase.SensorDistributionType.DoNotDistribute)
                .OrderByDescending(controller => controller.Instance.DistributionType);
        //Track the load of simulations
        var clientsLoad = new float[clientsCount];
        //Decrease master simulation sensors load
        var masterLoad = 0.15f;
        foreach (var sensorData in sensorsByDistributionType)
        {
            var lowestLoadIndex = 0;
            switch (sensorData.Instance.DistributionType)
            {
                case SensorBase.SensorDistributionType.LowLoad:
                    var lowLoadValue = 0.05f;
                    for (var i = 1; i < clientsCount; i++)
                        if (clientsLoad[i] < clientsLoad[lowestLoadIndex])
                            lowestLoadIndex = i;
                    if (masterLoad >= clientsLoad[lowestLoadIndex])
                    {
                        //Sensor will be distributed to lowest load client
                        clientsLoad[lowestLoadIndex] += lowLoadValue;
                        clientsSensors[lowestLoadIndex].Add(sensorData.Configuration);
                        sensorData.Disable();
                        
                        SimulatorManager.Instance.Sensors.AppendEndPoint(sensorData.Instance, clients[lowestLoadIndex].Peer.PeerEndPoint);
                    }
                    else 
                        //Sensor won't be distributed, instance on master is not disabled
                        masterLoad += lowLoadValue;
                    break;
                case SensorBase.SensorDistributionType.HighLoad:
                    var highLoadValue = 0.1f;
                    for (var i = 1; i < clientsCount; i++)
                        if (clientsLoad[i] < clientsLoad[lowestLoadIndex])
                            lowestLoadIndex = i;
                    if (masterLoad >= clientsLoad[lowestLoadIndex])
                    {
                        //Sensor will be distributed to lowest load client
                        clientsLoad[lowestLoadIndex] += highLoadValue;
                        clientsSensors[lowestLoadIndex].Add(sensorData.Configuration);
                        sensorData.Disable();
                        SimulatorManager.Instance.Sensors.AppendEndPoint(sensorData.Instance, clients[lowestLoadIndex].Peer.PeerEndPoint);
                    }
                    else 
                        //Sensor won't be distributed, instance on master is not disabled
                        masterLoad += highLoadValue;
                    break;
                case SensorBase.SensorDistributionType.UltraHighLoad:
                    var ultraHighLoadValue = 1.0f;
                    for (var i = 1; i < clientsCount; i++)
                        if (clientsLoad[i] < clientsLoad[lowestLoadIndex])
                            lowestLoadIndex = i;
                    //Sensor will be distributed to lowest load client
                    clientsLoad[lowestLoadIndex] += ultraHighLoadValue;
                    clientsSensors[lowestLoadIndex].Add(sensorData.Configuration);
                    
                    SimulatorManager.Instance.Sensors.AppendEndPoint(sensorData.Instance, clients[lowestLoadIndex].Peer.PeerEndPoint);
                    sensorData.Disable();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        //Check if any client is overloaded
        var overloadedClients = clientsLoad.Count(load => load > 1.0f);
        if (overloadedClients > 0)
        {
            if (masterLoad > 1.0f)
                overloadedClients++;
            Debug.LogWarning($"Running cluster simulation with {overloadedClients} overloaded instances. Decrease sensors count or extend the cluster for best performance.");
        }
        else if (masterLoad > 1.0f)
            Debug.LogWarning($"Running cluster simulation with overloaded master simulation. Used sensors cannot be distributed to the clients.");

        //Send sensors data to clients
        for (var i = 0; i < clientsCount; i++)
        {
            var client = network.Master.Clients[i];
            var sensorString = clientsSensors[i].ToString();
            var message = MessagesPool.Instance.GetMessage(BytesStack.GetMaxByteCount(sensorString));
            message.AddressKey = Key;
            message.Content.PushString(sensorString);
            message.Type = DistributedMessageType.ReliableOrdered;
            UnicastMessage(client.Peer.PeerEndPoint, message);
        }

        SensorsChanged?.Invoke();
    }

    private void MasterOnStateChanged(SimulationState state)
    {
        if (state == SimulationState.Running)
        {
            Loader.Instance.Network.Master.StateChanged -= MasterOnStateChanged;
            DistributeSensors();
        }
    }

    public void ReceiveMessage(IPeerManager sender, DistributedMessage distributedMessage)
    {
        if (sensorsInstances.Any() || Loader.Instance.Network.IsMaster)
            return;
        var sensors = distributedMessage.Content.PopString();
        InstantiateSensors(sensors);
    }

    /// <inheritdoc/>
    public void UnicastMessage(IPEndPoint endPoint, DistributedMessage distributedMessage)
    {
        if (!string.IsNullOrEmpty(Key))
            MessagesManager?.UnicastMessage(endPoint, distributedMessage);
    }

    /// <inheritdoc/>
    public void BroadcastMessage(DistributedMessage distributedMessage)
    {
        if (!string.IsNullOrEmpty(Key))
            MessagesManager?.BroadcastMessage(distributedMessage);
    }

    /// <inheritdoc/>
    void IMessageSender.UnicastInitialMessages(IPEndPoint endPoint)
    {
        //TODO support reconnection - send instantiation messages to the peer
    }
}