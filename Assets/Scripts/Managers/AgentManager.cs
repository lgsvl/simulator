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
    public Simulator.Sensors.ManualControlSensor manualControlPrefab; // TODO remove when sensor config is finished

    public GameObject CurrentActiveAgent { get; private set; } = null;
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

        foreach (var agent in activeAgents)
        {
            Instantiate(manualControlPrefab, agent.transform); // TODO remove when sensor config is finished
            agent.GetComponent<AgentController>().Init();
        }
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
}
