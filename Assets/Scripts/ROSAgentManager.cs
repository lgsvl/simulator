/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        LoadAgents();
    }
    #endregion

    public List<AgentSetup> agentPrefabs = new List<AgentSetup>();
    public List<RosBridgeConnector> activeAgents = new List<RosBridgeConnector>();



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

    public void LoadAgents()
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

    public RosBridgeConnector Add()
    {
        var robot = new RosBridgeConnector();
        activeAgents.Add(robot);
        return robot;
    }

    public void Remove(GameObject target)
    {
        activeAgents.RemoveAll(x => x.MenuObject == target);
        MenuManager.Instance.RunButtonInteractiveCheck();
    }

    public void Disconnect()
    {
        foreach (var robot in activeAgents)
        {
            robot.Disconnect();
        }
    }

    void Update()
    {
        foreach (var robot in activeAgents)
        {
            robot.Update();
        }
    }

    void OnApplicationQuit()
    {
        Disconnect();
    }
}
