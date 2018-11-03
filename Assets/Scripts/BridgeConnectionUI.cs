/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BridgeConnectionUI : MonoBehaviour
{
    public InputField bridgeAddress;
    public Dropdown robotOptions;
    public RosRobots rosRobots;

    void Awake()
    {
        rosRobots = GameObject.FindObjectOfType<RosRobots>();

        if (rosRobots == null) return;

        robotOptions.ClearOptions();
        var optionList = new List<string>();
        foreach (var robot in rosRobots.robotCandidates)
        {
            optionList.Add(robot.name);
        }
        robotOptions.AddOptions(optionList);
    }
}
