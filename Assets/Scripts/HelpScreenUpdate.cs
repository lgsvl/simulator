/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HelpScreenUpdate : MonoBehaviour
{
    public Text RobotsText = null;
    public RosRobots Robots = null;

    public RectTransform Help;

    void Update()
    {
        if (Help.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            Hide();
        }

        if (Input.GetKeyDown(KeyCode.F1))
        {
            if (Help.gameObject.activeSelf)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }
    }

    void Show()
    {
        Help.gameObject.SetActive(true);

        var sb = new StringBuilder();
        sb.Append("Available ROS Bridges:\n\n");

        if (Robots == null)
        {
            var single = FindObjectOfType<SingleRosConnection>();
            FormatRobot(sb, single.Connector);
        }
        else
        {
            foreach (var ros in Robots.Robots)
            {
                FormatRobot(sb, ros);
                sb.Append("\n");
            }
        }

        RobotsText.text = sb.ToString();
    }

    void Hide()
    {
        Help.gameObject.SetActive(false);
    }

    static void FormatRobot(StringBuilder sb, RosBridgeConnector ros)
    {
        if (ros.Bridge.Status == Ros.Status.Connected)
        {
            sb.AppendLine($"{ros.PrettyAddress}");
            foreach (var topic in ros.Bridge.TopicPublishers)
            {
                sb.AppendLine($"PUB: {topic.Name} ({topic.Type})");
            }
            foreach (var topic in ros.Bridge.TopicSubscriptions)
            {
                sb.AppendLine($"SUB: {topic.Name} ({topic.Type})");
            }
        }
        else
        {
            sb.AppendLine($"{ros.PrettyAddress} ({ros.Bridge.Status})");
        }
    }
}
