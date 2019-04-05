using System;
using Nancy.Hosting.Self;

using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    private int port = 8079;
    private string url;
    private string path = "/";

    NancyHost Host;

    [SerializeField]
    private Button button;

    void Start()
    {
        // bind to all interfaces instead of localhost
        var config = new HostConfiguration { RewriteLocalhost = true };
        url = "http://localhost:" + port;
        Host = new NancyHost(new MyBootstrapper(), config, new Uri(url));
        Host.Start();
        button.onClick.AddListener(OnButtonClicked);
    }

    void OnButtonClicked()
    {
        Application.OpenURL(url + path);
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(OnButtonClicked);
        Host?.Stop();
    }
}