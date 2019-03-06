/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class ActiveAgentMissive : Missive
{
    public RosBridgeConnector agent;
}

public enum StartModeTypes
{
    None,
    Menu,
    Dev,
    StaticConfig,
    API
};

public class ROSAgentManager : MonoBehaviour
{
    #region Singleton
    private static ROSAgentManager _instance = null;
    public static ROSAgentManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<ROSAgentManager>();
                if (_instance == null)
                    Debug.LogError("<color=red>ROSAgentManager" + " Not Found!</color>");
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
            _instance = this;

        if (_instance != this)
            DestroyImmediate(gameObject);
        else
            DontDestroyOnLoad(gameObject);
    }
    #endregion
    
    public StartModeTypes currentMode = StartModeTypes.None;

    public List<AgentSetup> agentPrefabs = new List<AgentSetup>();
    public List<RosBridgeConnector> activeAgents = new List<RosBridgeConnector>();
    public UserInterfaceSetup uiPrefab;
    private RosBridgeConnector currentActiveAgent = null;
    
    public bool isAgentsLoaded { get; set; } = false;
    
    private void Start()
    {
        LoadAgents();
    }

    public void LoadAgents()
    {
        switch (currentMode)
        {
            case StartModeTypes.None:
                break;
            case StartModeTypes.Menu:
                AddSavedAgents();
                break;
            case StartModeTypes.Dev:
                AddDevModeAgents();
                break;
            case StartModeTypes.StaticConfig:
                break;
            case StartModeTypes.API:
                break;
            default:
                break;
        }
        isAgentsLoaded = true;
    }

    private void AddSavedAgents()
    {
        int count = PlayerPrefs.GetInt("ROS_AGENT_COUNT");
        for (int i = 0; i < count; i++)
        {
            var address = PlayerPrefs.GetString($"ROS_AGENT_{i}_ADDRESS", "localhost");
            var port = PlayerPrefs.GetInt($"ROS_AGENT_{i}_PORT", 9090);
            var type = PlayerPrefs.GetInt($"ROS_AGENT_{i}_TYPE", 0);

            activeAgents.Add(new RosBridgeConnector(address, port, type > agentPrefabs.Count - 1 ? agentPrefabs[0] : agentPrefabs[type]));
        }
    }

    public void SaveAgents()
    {
        PlayerPrefs.SetInt("ROS_AGENT_COUNT", activeAgents.Count);
        for (int i = 0; i < activeAgents.Count; i++)
        {
            var agent = activeAgents[i];
            PlayerPrefs.SetString($"ROS_AGENT_{i}_ADDRESS", agent.Address);
            PlayerPrefs.SetInt($"ROS_AGENT_{i}_PORT", agent.Port);
            PlayerPrefs.SetInt($"ROS_AGENT_{i}_TYPE", agentPrefabs.IndexOf(agent.agentType));
        }
        PlayerPrefs.Save();
    }
    
    public RosBridgeConnector Add()
    {
        var connector = new RosBridgeConnector();
        activeAgents.Add(connector);
        return connector;
    }

    public void Add(RosBridgeConnector connector)
    {
        activeAgents.Add(connector);
    }

    public void Remove(GameObject target)
    {
        activeAgents.RemoveAll(x => x.MenuObject == target);
        MenuManager.Instance?.RunButtonInteractiveCheck();
    }

    public void Clear()
    {
        activeAgents.Clear();
    }

    public void RemoveDevModeAgents()
    {
        var agents = FindObjectsOfType<AgentSetup>();
        foreach (var item in agents)
        {
            item.RemoveTweakables();
            Destroy(item.gameObject);
        }
    }

    private void AddDevModeAgents()
    {
        activeAgents.Clear();
        string overrideAddress = System.Environment.GetEnvironmentVariable("ROS_BRIDGE_HOST");
        List<AgentSetup> tempAgents = FindObjectsOfType<AgentSetup>().ToList();
        foreach (var agent in tempAgents)
        {
            RosBridgeConnector connector = new RosBridgeConnector(overrideAddress == null ? agent.address : overrideAddress, agent.port, agent);
            UserInterfaceSetup uiSetup = Instantiate(uiPrefab);
            uiSetup.agent = agent.gameObject;
            connector.Agent = agent.gameObject;
            connector.BridgeStatus = uiSetup.BridgeStatus;
            activeAgents.Add(connector);
            agent.Setup(uiSetup, connector, null);
        }
        Ros.Bridge.canConnect = true;
        SetCurrentActiveAgent(0);
    }

    public void Disconnect()
    {
        foreach (var agent in activeAgents)
        {
            agent.Disconnect();
        }
    }

    public void DisconnectAgents()
    {
        foreach (var agent in activeAgents)
        {
            agent.Disconnect();
        }
        currentMode = StartModeTypes.None;
    }

    void Update()
    {
        foreach (var agent in activeAgents)
        {
            agent.Update();
        }
    }

    #region active agents
    public void SetCurrentActiveAgent(RosBridgeConnector agent)
    {
        if (agent == null) return;
        currentActiveAgent = agent;
        ActiveAgentChanged(currentActiveAgent);
        currentActiveAgent.Agent.GetComponent<VehicleController>()?.SetDashUIState();
    }

    public void SetCurrentActiveAgent(int index)
    {
        if (activeAgents.Count == 0) return;
        currentActiveAgent = activeAgents[index];
        ActiveAgentChanged(currentActiveAgent);
        currentActiveAgent.Agent?.GetComponent<VehicleController>()?.SetDashUIState();
    }

    public bool GetIsCurrentActiveAgent(GameObject agent)
    {
        if (currentActiveAgent == null) return false; // TODO why null from VehicleController sometimes?
        return agent == currentActiveAgent.Agent;
    }

    public float GetDistanceToActiveAgent(Vector3 pos)
    {
        return Vector3.Distance(currentActiveAgent.Agent.transform.position, pos);
    }

    public GameObject GetCurrentActiveAgent()
    {
        return currentActiveAgent != null ? currentActiveAgent.Agent : null;
    }
    #endregion

    void OnApplicationQuit()
    {
        Disconnect();
    }

    #region missive
    private void ActiveAgentChanged(RosBridgeConnector agent)
    {
        var missive = new ActiveAgentMissive
        {
            agent = agent
        };
        Missive.Send(missive);
    }
    #endregion
}
