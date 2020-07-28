/**
 * Copyright (c) 2019-2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Simulator;
using Simulator.Sensors;
using Simulator.Utilities;
using Simulator.Components;
using Simulator.Network.Core;
using Simulator.Network.Core.Components;
using Simulator.Network.Core.Messaging;
using Simulator.Network.Shared;
using UnityEngine.Rendering.HighDefinition;
using Simulator.Bridge;

public class AgentManager : MonoBehaviour
{
    private MessagesManager networkMessagesManager;
    public string Key { get; } = "AgentManager";
    
    public GameObject CurrentActiveAgent { get; private set; } = null;
    public AgentController CurrentActiveAgentController { get; private set; } = null;
    public List<AgentConfig> ActiveAgents { get; private set; } = new List<AgentConfig>();

    public MessagesManager NetworkMessagesManager
    {
        get
        {
            if (networkMessagesManager == null) 
                networkMessagesManager = Loader.Instance.Network.MessagesManager;
            return networkMessagesManager;
        }
    }

    public event Action<GameObject> AgentChanged;

    public GameObject SpawnAgent(AgentConfig config)
    {
        var go = Instantiate(config.Prefab, transform);
        go.name = config.Name;
        // set it inactive until we can be sure setting up sensors etc worked without exceptions and it AgentController was initialized
        go.SetActive(false);
        var agentController = go.GetComponent<AgentController>();
        agentController.SensorsChanged += AgentControllerOnSensorsChanged;
        agentController.Config = config;
        agentController.Config.AgentGO = go;

        var lane = go.AddComponent<VehicleLane>();

        var baseLink = go.GetComponentInChildren<BaseLink>();
        if (baseLink == null)
        {
            baseLink = new GameObject("BaseLink").AddComponent<BaseLink>();
            baseLink.transform.SetParent(go.transform, false);
        }

        SIM.LogSimulation(SIM.Simulation.VehicleStart, config.Name);
        
        ActiveAgents.Add(agentController.Config);
        agentController.GTID = ++SimulatorManager.Instance.GTIDs;
        agentController.Config.GTID = agentController.GTID;

        BridgeClient bridgeClient = null;
        if (config.Bridge != null)
        {
            bridgeClient = go.AddComponent<BridgeClient>();
            bridgeClient.Init(config.Bridge);

            if (config.Connection != null)
            {
                bridgeClient.Connect(config.Connection);
            }
        }
        SIM.LogSimulation(SIM.Simulation.BridgeTypeStart, config.Bridge != null ? config.Bridge.Name : "None");
        var sensorsController = go.AddComponent<SensorsController>();
        agentController.AgentSensorsController = sensorsController;
        sensorsController.SetupSensors(config.Sensors);

        //Add required components for distributing rigidbody from master to clients
        var network = Loader.Instance.Network;
        if (network.IsClusterSimulation)
        {
            HierarchyUtilities.ChangeToUniqueName(go);
            if (network.IsClient)
            {
                //Disable controller and dynamics on clients so it will not interfere mocked components
                agentController.enabled = false;
                var vehicleDynamics = agentController.GetComponent<IVehicleDynamics>() as MonoBehaviour;
                if (vehicleDynamics != null)
                    vehicleDynamics.enabled = false;
            }
            
            //Change the simulation type only if it's not set in the prefab
            var distributedRigidbody = go.GetComponent<DistributedRigidbody>();
            if (distributedRigidbody == null)
            {
                distributedRigidbody = go.AddComponent<DistributedRigidbody>();
                distributedRigidbody.SimulationType = DistributedRigidbody.MockingSimulationType.ExtrapolateVelocities;
            }

            //Add the rest required components for cluster simulation
            ClusterSimulationUtilities.AddDistributedComponents(go);
        }

        go.transform.position = config.Position;
        go.transform.rotation = config.Rotation;
        agentController.Init();

        go.SetActive(true);
        return go;
    }

    public void SpawnAgents(AgentConfig[] agentConfigs)
    {
        CreateAgentsFromConfigs(agentConfigs);

        if (ActiveAgents.Count > 0)
        {
            SetCurrentActiveAgent(0);
        }
    }

    public void SetCurrentActiveAgent(GameObject agent)
    {
        Debug.Assert(agent != null);
        for (int i = 0; i < ActiveAgents.Count; i++)
        {
            if (ActiveAgents[i].AgentGO == agent)
            {
                SetCurrentActiveAgent(i);
                break;
            }
        }
    }

    public void SetCurrentActiveAgent(int index)
    {
        if (ActiveAgents.Count == 0) return;
        if (index < 0 || index > ActiveAgents.Count - 1) return;
        if (ActiveAgents[index] == null) return;

        CurrentActiveAgent = ActiveAgents[index].AgentGO;
        CurrentActiveAgentController = CurrentActiveAgent.GetComponent<AgentController>();

        foreach (var config in ActiveAgents)
        {
            config.AgentGO.GetComponent<AgentController>().Active = (config.AgentGO == CurrentActiveAgent);
        }
        ActiveAgentChanged(CurrentActiveAgent);
    }

    public void SetNextCurrentActiveAgent()
    {
        var index = GetCurrentActiveAgentIndex();
        index = index < ActiveAgents.Count - 1 ? index + 1 : 0;
        SetCurrentActiveAgent(index);
    }

    public bool GetIsCurrentActiveAgent(GameObject agent)
    {
        return agent == CurrentActiveAgent;
    }

    public int GetCurrentActiveAgentIndex()
    {
        int index = 0;
        for (int i = 0; i < ActiveAgents.Count; i++)
        {
            if (ActiveAgents[i].AgentGO == CurrentActiveAgent)
                index = i;
        }
        return index;
    }

    public float GetDistanceToActiveAgent(Vector3 pos)
    {
        return Vector3.Distance(CurrentActiveAgent.transform.position, pos);
    }

    private void ActiveAgentChanged(GameObject agent)
    {
        AgentChanged?.Invoke(agent);
    }

    private void AgentControllerOnSensorsChanged(AgentController agentController)
    {
        if (agentController == CurrentActiveAgentController)
            ActiveAgentChanged(CurrentActiveAgent);
    }

    public void ResetAgent()
    {
        CurrentActiveAgent?.GetComponent<AgentController>()?.ResetPosition();
    }

    public void DestroyAgent(GameObject go)
    {
        ActiveAgents.RemoveAll(config => config.AgentGO == go);
        var agentController = go.GetComponent<AgentController>();
        if (agentController!= null)
            agentController.SensorsChanged -= AgentControllerOnSensorsChanged;
        Destroy(go);

        if (ActiveAgents.Count == 0)
        {
            SimulatorManager.Instance.CameraManager.SetFreeCameraState();
        }
        else
        {
            SetCurrentActiveAgent(0);
        }
    }

    public void Reset()
    {
        List<AgentConfig> configs = new List<AgentConfig>(ActiveAgents);
        foreach (var config in configs)
        {
            DestroyAgent(config.AgentGO);
        }

        ActiveAgents.Clear();
    }

    private void CreateAgentsFromConfigs(AgentConfig[] agentConfigs)
    {
        var spawns = FindObjectsOfType<SpawnInfo>();
        var positions = spawns.OrderBy(spawn => spawn.name).Select(s => s.transform.position).ToArray();
        var rotations = spawns.OrderBy(spawn => spawn.name).Select(s => s.transform.rotation).ToArray();

        // TODO: In case of spawn point absense on the map
        // we have to do educated guess about default spawn point.
        //
        // The best would be to take meshes tagged as Road and
        // find any point on the surface regarless of the altitude.
        // But for now we use zero.
        int count = positions.Length;
        if (count == 0)
        {
            count = 1;
            positions = new [] { Vector3.zero };
            rotations = new [] { Quaternion.identity };
        }

        var renderers = new List<Renderer>();

        for (int current = 0; current < agentConfigs.Length; current++)
        {
            var config = agentConfigs[current];
            config.Position = positions[current % count];
            config.Rotation = rotations[current % count];

            var agent = SpawnAgent(config);

            // offset current spawn point by agent boundaries
            // in order to place next agent on top of current one
            agent.GetComponentsInChildren(renderers);
            var bounds = new Bounds(config.Position, Vector3.zero);
            renderers.ForEach(renderer => bounds.Encapsulate(renderer.bounds));

            positions[current % count] += Vector3.up * bounds.size.y;
        }
    }
}
