/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Simulator;
using Simulator.Components;
using Simulator.Network.Core;
using Simulator.Network.Core.Connection;
using Simulator.Network.Core.Messaging;
using Simulator.Network.Core.Messaging.Data;
using Simulator.Network.Shared;
using Simulator.Sensors;
using Simulator.Utilities;
using Simulator.Web;
using UnityEngine;
using VirtualFileSystem;

public class SensorsController : MonoBehaviour, IMessageSender, IMessageReceiver
{
    private struct SensorInstanceController
    {
        private readonly SensorData configuration;
        private readonly SensorBase instance;
        private bool enabled;

        public SensorBase Instance => instance;
        public SensorData Configuration => configuration;
        public bool Enabled => enabled;

        public SensorInstanceController(SensorData configuration, SensorBase instance)
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
            {
                bridgeClient = GetComponent<BridgeClient>();
            }
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
        {
            sensorsManager.UnregisterSensor(sensorInstanceController.Value.Instance);
        }

        MessagesManager?.UnregisterObject(this);
    }

    static string GetSensorType(SensorBase sensor)
    {
        var type = sensor.GetType().GetCustomAttributes(typeof(SensorType), false)[0] as SensorType;
        return type.Name;
    }

    public void SetupSensors(SensorData[] sensors)
    {
        var network = Loader.Instance.Network;
        if (sensors != null && !network.IsClient)
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

    private void InstantiateSensors(SensorData[] sensors)
    {
        var parents = new Dictionary<string, GameObject>()
        {
            {string.Empty, gameObject},
        };

        var agentController = GetComponent<AgentController>();
        var requested = sensors.ToList();
        var baseLink = transform.GetComponentInChildren<BaseLink>();

        while (requested.Count > 0)
        {
            // remember how many are still looking to find their parent
            int requestedCount = requested.Count;
            for (int i = 0; i < requested.Count(); i++)
            {
                var item = requested[i];
                string parentName = item.Parent != null ? item.Parent : string.Empty;

                if (parents.ContainsKey(parentName))
                {
                    var parentObject = parents[parentName];
                    var name = item.Name;
                    var type = item.Type;
                    GameObject prefab = null;
                    if (item.Plugin.AssetGuid == null)
                    {
                        prefab = Config.SensorPrefabs.FirstOrDefault(s => GetSensorType(s) == type).gameObject;
                    }
                    else if (Config.SensorTypeLookup.ContainsKey(item.Plugin.AssetGuid))
                    {
                        prefab = Config.SensorTypeLookup[item.Plugin.AssetGuid]?.gameObject;
                    }
                    else
                    {
                        var dir = Path.Combine(Application.persistentDataPath, "Sensors");
                        var vfs = VfsEntry.makeRoot(dir);
                        Config.CheckDir(vfs.GetChild(item.Plugin.AssetGuid), Config.LoadSensorPlugin);
                        if (Config.SensorTypeLookup.ContainsKey(item.Plugin.AssetGuid))
                        {
                            prefab = Config.SensorTypeLookup[item.Plugin.AssetGuid]?.gameObject;
                        }
                    }

                    if (prefab == null)
                    {
                       throw new Exception($"Unknown sensor type {type} for {gameObject.name} vehicle");
                    }

                    var sensor = CreateSensor(gameObject, parentObject, prefab, item, baseLink);
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
                    {
                        SimulatorManager.Instance.Sensors.RegisterSensor(sensorBase);
                    }

                    sensorInstanceController.Enable();
                    agentController.AgentSensors.Add(sensorBase);
                    sensorsInstances.Add(name, sensorInstanceController);
                }
            }

            // no sensors found their parent this round, they also won't find it next round
            if (requestedCount == requested.Count)
            {
                throw new Exception($"Failed to create {requested.Count} sensor(s), cannot determine parent-child relationship");
            }

            SensorsChanged?.Invoke();
        }
    }

    private GameObject CreateSensor(GameObject agent, GameObject parent, GameObject prefab, SensorData item, BaseLink baseLink)
    {
        if (baseLink != null)
        {
            parent = parent == gameObject ? baseLink.gameObject : parent; // replace baselink with the default gameObject parent
        }

        var position = parent.transform.position;
        var rotation = parent.transform.rotation;
        var transform = item.Transform;
        if (transform != null)
        {
            position = parent.transform.TransformPoint(transform.x, transform.y, transform.z);
            rotation = parent.transform.rotation * Quaternion.Euler(transform.pitch, transform.yaw, transform.roll);
        }

        var sensor = Instantiate(prefab, position, rotation, agent.transform);
        var sb = sensor.GetComponent<SensorBase>();
        sb.ParentTransform = parent.transform;

        if (item.@params == null)
        {
            return sensor;
        }

        var sbType = sb.GetType();
        foreach (var param in item.@params)
        {
            // these keys do not go through camelCase modifier so we do it manually
            var key = param.Key.First().ToString().ToUpper() + param.Key.Substring(1);
            var value = param.Value;

            var field = sbType.GetField(key);
            if (field == null)
            {
                throw new Exception($"Unknown {key} parameter for {item.Name} sensor on {gameObject.name} vehicle");
            }

            if (field.FieldType == typeof(System.Int64)
            || field.FieldType == typeof(System.Int32)
            || field.FieldType == typeof(System.Double)
            || field.FieldType == typeof(System.Single)
            || field.FieldType == typeof(System.String)
            || field.FieldType == typeof(System.Boolean))
            {
                field.SetValue(sb, Convert.ChangeType(value, field.FieldType));
            }
            else if (field.FieldType.IsEnum)
            {
                try
                {
                    var obj = Enum.Parse(field.FieldType, (string) value);
                    field.SetValue(sb, obj);
                }
                catch (ArgumentException ex)
                {
                    throw new Exception($"Failed to set {key} field to {value} enum value for {gameObject.name} vehicle, {sb.Name} sensor", ex);
                }
            }
            else if (field.FieldType == typeof(Color))
            {
                if (ColorUtility.TryParseHtmlString((string)value, out var color))
                {
                    field.SetValue(sb, color);
                }
                else
                {
                    throw new Exception($"Failed to set {key} field to {value} color for {gameObject.name} vehicle, {sb.Name} sensor");
                }
            }
            else if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var type = field.FieldType.GetGenericArguments()[0];
                Type listType = typeof(List<>).MakeGenericType(new[] {type});
                System.Collections.IList list = (System.Collections.IList) Activator.CreateInstance(listType);
                var jarray = (Newtonsoft.Json.Linq.JArray) value;

                if (type.IsEnum)
                {
                    // TODO test this branch - remove this comment when you found a sensor + config that exercises this branch
                    foreach (var elemValue in jarray.ToObject<List<string>>())
                    {
                        object elem;
                        try
                        {
                            elem = Enum.Parse(type, elemValue);
                        }
                        catch (ArgumentException ex)
                        {
                            throw new Exception($"Failed to set {key} field to {value} enum value for {gameObject.name} vehicle, {sb.Name} sensor", ex);
                        }

                        list.Add(elem);
                    }
                }
                else if (type == typeof(System.Single))
                {
                    list = jarray.ToObject<List<System.Single>>();
                }
                else
                {
                    List<Type> assignableTypes = null;
                    if (type.IsAbstract)
                    {
                        assignableTypes = type.Assembly.GetTypes().Where(t => t != type && type.IsAssignableFrom(t)).ToList();
                    }

                    foreach (var jtoken in jarray)
                    {
                        object elem;
                        if (!type.IsAbstract)
                        {
                            elem = jtoken.ToObject(type);
                        }
                        else
                        {
                            var typeName = jtoken.Value<string>("type");
                            if (typeName == null)
                            {
                                throw new Exception($"Type {type.Name} is abstract and does not define `type` field ({gameObject.name} vehicle, {sb.Name} sensor, {key} field)");
                            }

                            var elementType = assignableTypes.FirstOrDefault(t => t.Name == typeName);
                            if (elementType == null)
                            {
                                throw new Exception($"No {typeName} type derived from {type.Name} for {key} field for {gameObject.name} vehicle, {sb.Name} sensor");
                            }

                            elem = jtoken.ToObject(elementType);
                        }

                        list.Add(elem);
                    }
                }

                field.SetValue(sb, list);
            }
            else
            {
                throw new Exception($"Unknown {field.FieldType} type for {key} field for {gameObject.name} vehicle, {sb.Name} sensor");
            }
        }

        return sensor;
    }

    public void RemoveSensor(string name)
    {
        if (!sensorsInstances.TryGetValue(name, out var sensorInstance))
        {
            return;
        }
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
        {
            return;
        }

        var clientsCount = clients.Count;
        var clientsSensors = new List<SensorData>[clientsCount];
        for (var i = 0; i < clientsSensors.Length; i++)
        {
            clientsSensors[i] = new List<SensorData>();
        }

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
                    {
                        if (clientsLoad[i] < clientsLoad[lowestLoadIndex])
                        {
                            lowestLoadIndex = i;
                        }
                    }
                    if (masterLoad >= clientsLoad[lowestLoadIndex])
                    {
                        //Sensor will be distributed to lowest load client
                        clientsLoad[lowestLoadIndex] += lowLoadValue;
                        clientsSensors[lowestLoadIndex].Add(sensorData.Configuration);
                        sensorData.Disable();
                        SimulatorManager.Instance.Sensors.AppendEndPoint(sensorData.Instance, clients[lowestLoadIndex].Peer.PeerEndPoint);
                    }
                    else
                    {
                        //Sensor won't be distributed, instance on master is not disabled
                        masterLoad += lowLoadValue;
                    }
                    break;
                case SensorBase.SensorDistributionType.HighLoad:
                    var highLoadValue = 0.1f;
                    for (var i = 1; i < clientsCount; i++)
                    {
                        if (clientsLoad[i] < clientsLoad[lowestLoadIndex])
                        {
                            lowestLoadIndex = i;
                        }
                    }
                    if (masterLoad >= clientsLoad[lowestLoadIndex])
                    {
                        //Sensor will be distributed to lowest load client
                        clientsLoad[lowestLoadIndex] += highLoadValue;
                        clientsSensors[lowestLoadIndex].Add(sensorData.Configuration);
                        sensorData.Disable();
                        SimulatorManager.Instance.Sensors.AppendEndPoint(sensorData.Instance, clients[lowestLoadIndex].Peer.PeerEndPoint);
                    }
                    else
                    {
                        //Sensor won't be distributed, instance on master is not disabled
                        masterLoad += highLoadValue;
                    }
                    break;
                case SensorBase.SensorDistributionType.UltraHighLoad:
                    var ultraHighLoadValue = 1.0f;
                    for (var i = 1; i < clientsCount; i++)
                    {
                        if (clientsLoad[i] < clientsLoad[lowestLoadIndex])
                        {
                            lowestLoadIndex = i;
                        }
                    }

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
            {
                overloadedClients++;
            }

            Debug.LogWarning($"Running cluster simulation with {overloadedClients} overloaded instances. Decrease sensors count or extend the cluster for best performance.");
        }
        else if (masterLoad > 1.0f)
        {
            Debug.LogWarning($"Running cluster simulation with overloaded master simulation. Used sensors cannot be distributed to the clients.");
        }

        //Send sensors data to clients
        for (var i = 0; i < clientsCount; i++)
        {
            var client = network.Master.Clients[i];
            var sensorString = JsonConvert.SerializeObject(clientsSensors[i], JsonSettings.camelCase);
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
        {
            return;
        }
        var sensors = distributedMessage.Content.PopString();
        var parsed = JsonConvert.DeserializeObject<SensorData[]>(sensors);
        InstantiateSensors(parsed);
    }

    /// <inheritdoc/>
    public void UnicastMessage(IPEndPoint endPoint, DistributedMessage distributedMessage)
    {
        if (!string.IsNullOrEmpty(Key))
        {
            MessagesManager?.UnicastMessage(endPoint, distributedMessage);
        }
    }

    /// <inheritdoc/>
    public void BroadcastMessage(DistributedMessage distributedMessage)
    {
        if (!string.IsNullOrEmpty(Key))
        {
            MessagesManager?.BroadcastMessage(distributedMessage);
        }
    }

    /// <inheritdoc/>
    void IMessageSender.UnicastInitialMessages(IPEndPoint endPoint)
    {
        //TODO support reconnection - send instantiation messages to the peer
    }
}
