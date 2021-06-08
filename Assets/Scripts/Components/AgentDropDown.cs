/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using UnityEngine.EventSystems;

public class AgentDropDown : MonoBehaviour ,IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        SimulatorManager.Instance.UIManager.SetAgentsDropdown();
    }

    private void OnEnable()
    {
        SimulatorManager.Instance.UIManager.SetAgentsDropdown();
    }
}
