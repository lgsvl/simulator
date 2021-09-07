/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Simulator;
using Simulator.Components;
using Simulator.Network.Core;
using Simulator.Network.Core.Components;
using Simulator.Network.Core.Connection;
using Simulator.Network.Core.Messaging;
using Simulator.Network.Core.Messaging.Data;
using Simulator.Network.Shared;
using Simulator.Sensors;
using Simulator.Sensors.Postprocessing;
using Simulator.Utilities;
using Simulator.Web;
using UnityEngine;
using VirtualFileSystem;

public class SensorsController : MonoBehaviour, ISensorsController, IMessageSender, IMessageReceiver
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

            if (Loader.Instance.Network.IsClusterSimulation)
            {
                var distributedObject = instance.gameObject.AddComponent<DistributedObject>();
                distributedObject.ForwardMessages = Loader.Instance.Network.IsMaster;
                distributedObject.DistributeIsActive = false;
                distributedObject.CallInitialize();
                var distributedTransform = instance.gameObject.AddComponent<DistributedTransform>();
                distributedTransform.CallInitialize();
            }

            //Negate current value so proper method can be called
            //Without negating current status method will not be be executed
            enabled = !instance.isActiveAndEnabled;
            if (enabled)
            {
                Disable();
            }
            else
            {
                Enable();
            }
        }

        public void Enable()
        {
            if (enabled)
            {
                return;
            }

            if (Instance != null)
            {
                Instance.gameObject.SetActive(true);
            }

            if (Loader.Instance.Network.IsClusterSimulation)
            {
                var distributedObject = instance.gameObject.GetComponent<DistributedObject>();
                distributedObject.IsAuthoritative = true;
            }

            enabled = true;
        }

        public void Disable()
        {
            if (!enabled)
            {
                return;
            }

            if (Instance != null)
            {
                Instance.gameObject.SetActive(false);
            }

            if (Loader.Instance.Network.IsClusterSimulation)
            {
                var distributedObject = instance.gameObject.GetComponent<DistributedObject>();
                distributedObject.IsAuthoritative = false;
            }

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

    private MessagesManager MessagesManager =>
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

    private static string GetSensorType(SensorBase sensor)
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

        var Controller = GetComponent<IAgentController>();
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
                if (!parents.ContainsKey(parentName))
                    continue;

                var parentObject = parents[parentName];
                var name = item.Name;
                var type = item.Type;
                GameObject prefab = null;
                if (item.Plugin?.AssetGuid == null)
                {
                    Debug.LogWarning($"sensor without assetguid: {item.Name} {item.Type}");
                    prefab = Config.SensorPrefabs.FirstOrDefault(s => GetSensorType(s) == type)?.gameObject;
                    if (prefab == null)
                    {
                        Debug.LogError($"could not find alternative for {item.Name} {item.Type} choices were: {string.Join(",", Config.SensorPrefabs.Select(sp=> GetSensorType(sp)))}");
                    }
                }
                else if (Config.SensorTypeLookup.ContainsKey(item.Plugin.AssetGuid))
                {
                    prefab = Config.SensorTypeLookup[item.Plugin.AssetGuid]?.gameObject;
                }
                else
                {
                    var dir = Path.Combine(Config.PersistentDataPath, "Sensors");
                    var vfs = VfsEntry.makeRoot(dir);
                    Config.CheckDir(vfs.GetChild(item.Plugin.AssetGuid), Config.LoadSensorPlugin);
                    if (Config.SensorTypeLookup.ContainsKey(item.Plugin.AssetGuid))
                    {
                        prefab = Config.SensorTypeLookup[item.Plugin.AssetGuid]?.gameObject;
                    }
                }

                if (prefab == null)
                {
                    throw new Exception($"Issue loading sensor type {type} for gameobject {gameObject.name} check logs");
                }

                var sensor = CreateSensor(gameObject, parentObject, prefab, item, baseLink);
                var sensorBase = sensor.GetComponent<SensorBase>();
                sensorBase.Name = name;
                sensor.name = name;
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
                Controller?.AgentSensors.Add(sensorBase);
                sensorsInstances.Add(name, sensorInstanceController);
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
        var sbType = sb.GetType();
        sb.ParentTransform = parent.transform;

        if (item.@params != null)
        {
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
                        var obj = Enum.Parse(field.FieldType, (string)value);
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
                    Type listType = typeof(List<>).MakeGenericType(type);
                    IList list = (IList)Activator.CreateInstance(listType);
                    var jarray = (Newtonsoft.Json.Linq.JArray)value;

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
                        // Full qualified type name used, e.g. "$_type": "Simulator.Sensors.Postprocessing.SunFlare, Simulator"
                        var jArrStr = jarray.ToString();
                        if (jArrStr.Contains("$_type"))
                        {
                            // Revert change from SensorParameter attribute (more details there)
                            jArrStr = jArrStr.Replace("$_type", "$type");

                            // If sensor's assembly is named after its type, we're loading asset bundle with renamed assembly
                            // Type was serialized before renaming - update types from that assembly to reflect this change 
                            if (sbType.Assembly.GetName().Name == item.Type)
                                jArrStr = Regex.Replace(jArrStr, "(\"\\$type\"\\:[^,]*, )(Simulator.Sensors)", $"$1{item.Type}");

                            list = JsonConvert.DeserializeObject(jArrStr, typeof(List<>).MakeGenericType(type), new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto }) as IList;
                        }
                        // Shortened type name used, e.g. "type": "SunFlare"
                        else
                        {
                            List<Type> assignableTypes = null;
                            if (type.IsAbstract)
                            {
                                assignableTypes = type.Assembly.GetTypes().Where(t => t != type && type.IsAssignableFrom(t))
                                    .ToList();
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
                                        throw new Exception(
                                            $"Type {type.Name} is abstract and does not define `type` field ({gameObject.name} vehicle, {sb.Name} sensor, {key} field)");
                                    }

                                    var elementType = assignableTypes.FirstOrDefault(t => t.Name == typeName);
                                    if (elementType == null)
                                    {
                                        throw new Exception(
                                            $"No {typeName} type derived from {type.Name} for {key} field for {gameObject.name} vehicle, {sb.Name} sensor");
                                    }

                                    elem = jtoken.ToObject(elementType);
                                }

                                list.Add(elem);
                            }
                        }
                    }

                    field.SetValue(sb, list);
                }
                else
                {
                    var jObjStr = value.ToString();

                    // Revert change from SensorParameter attribute (more details there)
                    jObjStr = jObjStr.Replace("$_type", "$type");

                    // If sensor's assembly is named after its type, we're loading asset bundle with renamed assembly
                    // Type was serialized before renaming - update types from that assembly to reflect this change
                    if (sbType.Assembly.GetName().Name == item.Type)
                        jObjStr = Regex.Replace(jObjStr, "(\"\\$type\"\\:[^,]*, )(Simulator.Sensors)", $"$1{item.Type}");

                    var instance = JsonConvert.DeserializeObject(jObjStr, field.FieldType, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
                    field.SetValue(sb, instance);
                }
            }
        }

        // Populate all fields with their default instances, if their override was not set through config 
        foreach (var field in sbType.GetRuntimeFields().Where(field => field.IsDefined(typeof(SensorParameter), true)))
        {
            var attr = field.GetCustomAttribute<SensorParameter>();

            var current = field.GetValue(sb);
            if (current != null)
                continue;

            var defaultInstance = attr.GetDefaultInstance(sbType, field.FieldType);
            if (defaultInstance == null)
                continue;

            field.SetValue(sb, defaultInstance);
        }

        if (sb is CameraSensorBase cameraBase && cameraBase.Postprocessing == null && sbType.IsDefined(typeof(DefaultPostprocessingAttribute), true))
        {
            if (cameraBase.Postprocessing == null)
            {
                var attr = sbType.GetCustomAttribute<DefaultPostprocessingAttribute>();
                var ppInstances = attr.GetDefaultInstances;
                cameraBase.Postprocessing = ppInstances;
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
        GetComponent<IAgentController>().AgentSensors.Remove(sensorInstance.Instance);
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
        var clientsSensors = new Dictionary<IPeerManager, List<string>>();
        for (var i = 0; i < clientsCount; i++)
        {
            clientsSensors.Add(clients[i].Peer, new List<string>());
        }

        //Order sensors by distribution type, so ultra high loads will be handled first
        var sensorsByDistributionType =
            sensorsInstances.Values.Where(controller =>
                    controller.Instance.DistributionType != SensorBase.SensorDistributionType.MainOnly)
                .OrderByDescending(controller => controller.Instance.DistributionType);

        var loadBalancer = master.LoadBalancer;
        foreach (var sensorData in sensorsByDistributionType)
        {
            IPeerManager sensorPeer;
            switch (sensorData.Instance.DistributionType)
            {
                case SensorBase.SensorDistributionType.MainOnly:
                    loadBalancer.AppendMasterLoad(sensorData.Instance.PerformanceLoad);
                    break;
                case SensorBase.SensorDistributionType.MainOrClient:
                    sensorPeer = loadBalancer.AppendLoad(sensorData.Instance.PerformanceLoad, true);
                    if (sensorPeer != null)
                    {
                        //Sensor will be distributed to lowest load client
                        clientsSensors[sensorPeer].Add(sensorData.Configuration.Name);
                        sensorData.Disable();
                        SimulatorManager.Instance.Sensors.AppendEndPoint(sensorData.Instance, sensorPeer.PeerEndPoint);
                    }
                    break;
                case SensorBase.SensorDistributionType.ClientOnly:
                    sensorPeer = loadBalancer.AppendLoad(sensorData.Instance.PerformanceLoad, false);
                    //Sensor will be distributed to lowest load client
                    clientsSensors[sensorPeer].Add(sensorData.Configuration.Name);
                    SimulatorManager.Instance.Sensors.AppendEndPoint(sensorData.Instance, sensorPeer.PeerEndPoint);
                    sensorData.Disable();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        var sensorsData = sensorsInstances.Select(s => s.Value.Configuration);
        var allSensors = JsonConvert.SerializeObject(sensorsData, JsonSettings.camelCase);
        var sensorsLength = BytesStack.GetMaxByteCount(allSensors);
        //Send sensors data to clients
        for (var i = 0; i < clientsCount; i++)
        {
            var client = network.Master.Clients[i];
            var enabledSensors = JsonConvert.SerializeObject(clientsSensors[client.Peer], JsonSettings.camelCase);
            var message = MessagesPool.Instance.GetMessage(sensorsLength + BytesStack.GetMaxByteCount(enabledSensors));
            message.AddressKey = Key;
            message.Content.PushString(enabledSensors);
            message.Content.PushString(allSensors);
            message.Type = DistributedMessageType.ReliableOrdered;
            ((IMessageSender)this).UnicastMessage(client.Peer.PeerEndPoint, message);
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

    void IMessageReceiver.ReceiveMessage(IPeerManager sender, DistributedMessage distributedMessage)
    {
        if (sensorsInstances.Any() || Loader.Instance.Network.IsMaster)
        {
            return;
        }
        var allSensorsString = distributedMessage.Content.PopString();
        var allSensors = JsonConvert.DeserializeObject<SensorData[]>(allSensorsString);
        var enabledSensorsString = distributedMessage.Content.PopString();
        var enabledSensors = JsonConvert.DeserializeObject<List<string>>(enabledSensorsString);
        InstantiateSensors(allSensors);
        foreach (var instance in sensorsInstances)
        {
            if (enabledSensors.Contains(instance.Key))
                instance.Value.Enable();
            else
                instance.Value.Disable();
        }
        SensorsChanged?.Invoke();
    }

    /// <inheritdoc/>
    void IMessageSender.UnicastMessage(IPEndPoint endPoint, DistributedMessage distributedMessage)
    {
        if (!string.IsNullOrEmpty(Key))
        {
            MessagesManager?.UnicastMessage(endPoint, distributedMessage);
        }
    }

    /// <inheritdoc/>
    void IMessageSender.BroadcastMessage(DistributedMessage distributedMessage)
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
