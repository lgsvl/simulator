using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionUI : MonoBehaviour
{
    public Text statusText;
    public Button statusButton;
    public Text statusButtonText;
    public Image statusButtonIcon;
    public Button linkButton;
    public Text linkButtonText;
    public static ConnectionUI instance;
    public Color offlineColor;
    public Color onlineColor;
    public void Start()
    {
        if(instance != null)
        {
            Destroy(gameObject);
            return;
        }

        ColorUtility.TryParseHtmlString("#1F2940", out offlineColor);
        ColorUtility.TryParseHtmlString("#FFFFFF", out onlineColor);
        statusButtonIcon.material.color = Color.white;
        instance = this;
        statusButton.onClick.AddListener(OnStatusButtonClicked);
        linkButton.onClick.AddListener(OnLinkButtonClicked);
        UpdateStatus();
    }

    public void UpdateDownloadProgress(string name, float percentage)
    {
        statusText.text = $"Downloading {name}... {percentage}%";
    }

    public void UpdateStatus()
    {
        switch (ConnectionManager.Status)
        {
            case ConnectionManager.ConnectionStatus.Connecting:
                statusText.text = $"Connecting to the cloud...";
                linkButton.gameObject.SetActive(false);
                statusButtonIcon.color = offlineColor;
                break;
            case ConnectionManager.ConnectionStatus.Connected:
                statusText.text = "";
                statusButtonText.text = "Online";
                statusButtonIcon.color = offlineColor;
                linkButtonText.text = "LINK TO CLOUD";
                linkButton.gameObject.SetActive(true);
                statusButton.interactable = true;
                break;
            case ConnectionManager.ConnectionStatus.Offline:
                statusButtonText.text = "Offline";
                statusText.text = "Go online to start using simulator.";
                statusButtonIcon.color = offlineColor;
                linkButton.gameObject.SetActive(false);
                statusButton.interactable = true;
                break;
            case ConnectionManager.ConnectionStatus.Online:
                statusButtonText.text = "Online";
                statusButtonIcon.color = onlineColor;
                statusText.text = "";
                linkButtonText.text = "OPEN BROWSER";
                linkButton.gameObject.SetActive(true);
                statusButton.interactable = true;
                break;
        }
    }

    public void SetLinkingButtonActive(bool active)
    {
        linkButton.gameObject.SetActive(active);
    }

    public void OnStatusButtonClicked()
    {
        ConnectionManager.instance.ConnectionStatusEvent();
        statusButton.interactable = false;
    }

    public void OnLinkButtonClicked()
    {
        if (ConnectionManager.Status == ConnectionManager.ConnectionStatus.Connected)
        {
            Application.OpenURL(Simulator.Web.Config.CloudUrl + "/clusters/link?token=" + ConnectionManager.instance.simInfo.linkToken);
        }
        else if (ConnectionManager.Status == ConnectionManager.ConnectionStatus.Online)
        {
            Application.OpenURL(Simulator.Web.Config.CloudUrl);
            SIM.LogSimulation(SIM.Simulation.ApplicationClick, "Open Browser");
        }
    }
}
