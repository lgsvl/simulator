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
    public List<RosBridgeConnector> Robots = new List<RosBridgeConnector>();

    void Awake()
    {
        int count = PlayerPrefs.GetInt("ROS_ROBOT_COUNT");
        for (int i = 0; i < count; i++)
        {
            var address = PlayerPrefs.GetString(string.Format("ROS_ROBOT_{0}_ADDRESS", i), "localhost");
            var port = PlayerPrefs.GetInt(string.Format("ROS_ROBOT_{0}_PORT", i), 9090);
            var version = PlayerPrefs.GetInt(string.Format("ROS_ROBOT_{0}_VERSION", i), 1);

            Robots.Add(new RosBridgeConnector(address, port, version));
        }

        DontDestroyOnLoad(this);
    }

    public void Save()
    {
        PlayerPrefs.SetInt("ROS_ROBOT_COUNT", Robots.Count);
        for (int i = 0; i < Robots.Count; i++)
        {
            var robot = Robots[i];
            PlayerPrefs.SetString(string.Format("ROS_ROBOT_{0}_ADDRESS", i), robot.Address);
            PlayerPrefs.SetInt(string.Format("ROS_ROBOT_{0}_PORT", i), robot.Port);
            PlayerPrefs.SetInt(string.Format("ROS_ROBOT_{0}_VERSION", i), robot.Version);
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
