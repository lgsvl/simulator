/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

public class AgentDropDown : MonoBehaviour
{
    private void OnEnable()
    {
        SimulatorManager.Instance.UIManager.SetAgentsDropdown();
    }
}
