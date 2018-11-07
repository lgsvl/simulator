/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using System.Collections.Generic;
using UnityEngine;

public class RosRobots : MonoBehaviour
{
    public List<RobotSetup> robotCandidates;
    public List<RosBridgeConnector> Robots = new List<RosBridgeConnector>();

    void Awake()
    {
        int count = PlayerPrefs.GetInt("ROS_ROBOT_COUNT");
        for (int i = 0; i < count; i++)
        {
            var address = PlayerPrefs.GetString($"ROS_ROBOT_{i}_ADDRESS", "localhost");
            var port = PlayerPrefs.GetInt($"ROS_ROBOT_{i}_PORT", 9090);
            var type = PlayerPrefs.GetInt($"ROS_ROBOT_{i}_TYPE", 0);

            Robots.Add(new RosBridgeConnector(address, port, type > robotCandidates.Count - 1 ? robotCandidates[0] : robotCandidates[type]));
        }

        DontDestroyOnLoad(this);
    }

    public void Save()
    {
        PlayerPrefs.SetInt("ROS_ROBOT_COUNT", Robots.Count);
        for (int i = 0; i < Robots.Count; i++)
        {
            var robot = Robots[i];
            PlayerPrefs.SetString($"ROS_ROBOT_{i}_ADDRESS", robot.Address);
            PlayerPrefs.SetInt($"ROS_ROBOT_{i}_PORT", robot.Port);
            PlayerPrefs.SetInt($"ROS_ROBOT_{i}_TYPE", robotCandidates.IndexOf(robot.robotType));
        }
        PlayerPrefs.Save();
    }

    public RosBridgeConnector Add()
    {
        var robot = new RosBridgeConnector();
        Robots.Add(robot);
        return robot;
    }

    public void Remove(GameObject target)
    {
        Robots.RemoveAll(x => x.MenuObject == target);
        MenuScript.Instance.RunButtonInteractiveCheck();
    }

    public void Disconnect()
    {
        foreach (var robot in Robots)
        {
            robot.Disconnect();
        }
    }

    void Update()
    {
        foreach (var robot in Robots)
        {
            robot.Update();
        }
    }

    void OnApplicationQuit()
    {
        Disconnect();
    }
}
