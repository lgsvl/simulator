/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ActiveAgentMissive : Missive
{
    public GameObject agent;
}

public class AgentManager : MonoBehaviour
{
    private GameObject currentActiveAgent = null;
    private List<GameObject> activeAgents = new List<GameObject>();

    private void Start()
    {
        Debug.Log("Init Agent Manager");
        if (SimulatorManager.Instance.Config == null) return;
        
        SpawnAgents(SimulatorManager.Instance.Config.AgentPrefabs);
    }

    public void SpawnAgents(GameObject[] prefabs)
    {
        foreach (var prefab in prefabs)
        {
            var go = Instantiate(prefab);
            activeAgents.Add(go);
        }
    }

    public void SetCurrentActiveAgent(GameObject agent)
    {
        if (agent == null) return;
        currentActiveAgent = agent;
        ActiveAgentChanged(currentActiveAgent);
    }

    public void SetCurrentActiveAgent(int index)
    {
        if (activeAgents.Count == 0) return;
        currentActiveAgent = activeAgents[index];
        ActiveAgentChanged(currentActiveAgent);
    }

    public bool GetIsCurrentActiveAgent(GameObject agent)
    {
        return agent == currentActiveAgent;
    }

    public float GetDistanceToActiveAgent(Vector3 pos)
    {
        return Vector3.Distance(currentActiveAgent.transform.position, pos);
    }

    public GameObject GetCurrentActiveAgent()
    {
        return currentActiveAgent;
    }

    private void ActiveAgentChanged(GameObject agent)
    {
        var missive = new ActiveAgentMissive
        {
            agent = agent
        };
        Missive.Send(missive);
    }
}
