/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentManager : MonoBehaviour
{
    List<GameObject> activeAgents = new List<GameObject>();

    public void Init()
    {
        Debug.Log("Init Agent Manager");
        SpawnAgents(SimulatorManager.Instance.currentConfigData);
    }

    public void SpawnAgents(ConfigData data)
    {
        if (data == null) return;
        foreach (var agent in data.Agents)
        {
            var go = Instantiate(agent);
            activeAgents.Add(go);
        }
    }
}
