/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using Simulator.Sensors;
using Simulator.Utilities;
using Simulator.Components;

public class AgentManager : MonoBehaviour
{
    public ManualControlSensor manualControlPrefab; // TODO remove when sensor config is finished

    public GameObject CurrentActiveAgent { get; private set; } = null;
    private List<GameObject> activeAgents = new List<GameObject>();

    public event Action<GameObject> AgentChanged;

    public void SpawnAgents()
    {
        var agents = SimulatorManager.Instance.Config?.Agents;
        if (agents != null)
        {
            foreach (var agent in agents)
            {
                var go = Instantiate(agent.Prefab);
                go.name = agent.Name;
                activeAgents.Add(go);

                BridgeClient bridgeClient = null;
                if (agent.Bridge != null)
                {
                    bridgeClient = go.AddComponent<BridgeClient>();
                    bridgeClient.Init(agent.Bridge);

                    var split = agent.Connection.Split(':');
                    bridgeClient.Connect(split[0], int.Parse(split[1]));
                }
                if (!string.IsNullOrEmpty(agent.Sensors))
                {
                    SetupSensors(go, agent.Sensors, bridgeClient);
                }
            }
        }
        else
        {
            activeAgents.AddRange(GameObject.FindGameObjectsWithTag("Player"));

            if (activeAgents.Count > 0)
            {
                var go = activeAgents[0];

                var bridgeClient = go.AddComponent<BridgeClient>();
                bridgeClient.Init(new Simulator.Bridge.Ros.RosApolloBridgeFactory());
                bridgeClient.Connect("localhost", 9090);

                SetupSensors(go, DefaultSensors.Apollo30, bridgeClient);
            }
        }

        if (activeAgents.Count > 0)
            SetCurrentActiveAgent(0);

        foreach (var agent in activeAgents)
            agent.GetComponent<AgentController>().Init();
    }

    public void SetCurrentActiveAgent(GameObject agent)
    {
        Debug.Assert(agent != null);
        CurrentActiveAgent = agent;
        ActiveAgentChanged(CurrentActiveAgent);
    }

    public void SetCurrentActiveAgent(int index)
    {
        if (activeAgents.Count == 0) return;
        if (index < 0 || index > activeAgents.Count - 1) return;
        CurrentActiveAgent = activeAgents[index];
        foreach (var agent in activeAgents)
        {
            if (agent == CurrentActiveAgent)
                agent.GetComponent<AgentController>().isActive = true;
            else
                agent.GetComponent<AgentController>().isActive = false;
        }
        ActiveAgentChanged(CurrentActiveAgent);
    }

    public bool GetIsCurrentActiveAgent(GameObject agent)
    {
        return agent == CurrentActiveAgent;
    }

    public float GetDistanceToActiveAgent(Vector3 pos)
    {
        return Vector3.Distance(CurrentActiveAgent.transform.position, pos);
    }

    private void ActiveAgentChanged(GameObject agent)
    {
        AgentChanged?.Invoke(agent);
    }

    public void ToggleAgent(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        if (int.TryParse(ctx.control.name, out int index))
            SetCurrentActiveAgent(index - 1);
    }

    public void ResetAgent()
    {
        CurrentActiveAgent?.GetComponent<AgentController>()?.ResetPosition();
    }

    static string GetSensorType(SensorBase sensor)
    {
        var type = sensor.GetType().GetCustomAttributes(typeof(SensorType), false)[0] as SensorType;
        return type.Name;
    }

    public void SetupSensors(GameObject agent, string sensors, BridgeClient bridgeClient)
    {
        var available = Simulator.Web.Config.Sensors.ToDictionary(sensor => sensor.Name);
        var prefabs = RuntimeSettings.Instance.SensorPrefabs.ToDictionary(sensor => GetSensorType(sensor));

        var parents = new Dictionary<string, GameObject>()
        {
            { string.Empty, agent },
        };

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

                        var sensor = CreateSensor(agent, parentObject, prefabs[type].gameObject, item);
                        sensor.name = name;
                        if (bridgeClient != null)
                        {
                            sensor.GetComponent<SensorBase>().OnBridgeSetup(bridgeClient.Bridge);
                        }

                        parents.Add(name, sensor);
                        requested.RemoveAt(i);
                        i--;
                    }
                }
            }

            if (requestedCount == requested.Count)
            {
                throw new Exception($"Failed to create {requested.Count} sensor(s), cannot determine parent-child relationship");
            }
        }
    }

    GameObject CreateSensor(GameObject agent, GameObject parent, GameObject prefab, JSONNode item)
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
                throw new Exception($"Unknown {key} parameter for {item["name"].Value} sensor on {gameObject.name} vehicle");
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
                    throw new Exception($"Failed to set {key} field to {value.Value} enum value for {gameObject.name} vehicle, {sb.Name} sensor", ex);
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
                    throw new Exception($"Failed to set {key} field to {value.Value} color for {gameObject.name} vehicle, {sb.Name} sensor");
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
            else
            {
                throw new Exception($"Unknown {field.FieldType} type for {key} field for {gameObject.name} vehicle, {sb.Name} sensor");
            }
        }

        return sensor;
    }
}
