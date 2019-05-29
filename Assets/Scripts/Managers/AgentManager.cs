/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public class AgentManager : MonoBehaviour
{
    public GameObject currentActiveAgent { get; private set; } = null;
    private List<GameObject> activeAgents = new List<GameObject>();

    public event Action<GameObject> AgentChanged;

    public void SpawnAgents()
    {
        GameObject[] prefabs = SimulatorManager.Instance.Config?.AgentPrefabs;
        if (prefabs != null)
        {
            foreach (var prefab in prefabs) // config agents
            {
                var go = Instantiate(prefab);
                activeAgents.Add(go);
            }
        }
        else
        {
            activeAgents.AddRange(GameObject.FindGameObjectsWithTag("Player"));
        }
        if (activeAgents.Count > 0)
            SetCurrentActiveAgent(0);
    }
    
    public void SetCurrentActiveAgent(GameObject agent)
    {
        Debug.Assert(agent != null);
        currentActiveAgent = agent;
        ActiveAgentChanged(currentActiveAgent);
    }

    public void SetCurrentActiveAgent(int index)
    {
        if (activeAgents.Count == 0) return;
        if (index < 0 || index > activeAgents.Count - 1) return;
        currentActiveAgent = activeAgents[index];
        foreach (var agent in activeAgents)
        {
            if (agent == currentActiveAgent)
                agent.GetComponent<AgentController>().isActive = true;
            else
                agent.GetComponent<AgentController>().isActive = false;
        }
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
        currentActiveAgent?.GetComponent<AgentController>()?.ResetPosition();
    }
}
