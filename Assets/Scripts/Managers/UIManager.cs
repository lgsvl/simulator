/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator.Utilities;
using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameObject menuHolder;
    public GameObject controlsPanel;
    public GameObject infoPanel;

    private void Start()
    {
        menuHolder.SetActive(false);
        controlsPanel.SetActive(false);
        infoPanel.SetActive(false);

        var info = Resources.Load<BuildInfo>("BuildInfo");
        if (info != null)
        {
            var timestamp = DateTime.ParseExact(info.Timestamp, "o", CultureInfo.InvariantCulture);
            Debug.Log($"Timestamp = {timestamp}");
            Debug.Log($"Version = {info.Version}");
            Debug.Log($"GitCommit = {info.GitCommit}");
            Debug.Log($"GitBranch = {info.GitBranch}");

            var infoText = infoPanel.transform.GetChild(infoPanel.transform.childCount - 1).GetComponent<Text>();
            if (infoText != null)
            {
                infoText.text = $@"
Timestamp = {timestamp}
Version = {info.Version}
GitCommit = {info.GitCommit}
GitBranch = {info.GitBranch}
";
            }
        }
    }

    public void ToggleControlsUI()
    {
        if (controlsPanel.activeInHierarchy)
        {
            menuHolder.SetActive(false);
            controlsPanel.SetActive(false);
        }
        else
        {
            menuHolder.SetActive(true);
            controlsPanel.SetActive(true);
        }
    }
}
