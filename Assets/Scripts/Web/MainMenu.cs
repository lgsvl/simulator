using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

using Database;
using Nancy.Hosting.Self;

namespace Web
{
    public class MainMenu : MonoBehaviour
    {
        private int port = 8080;
        private string url;
        private string path = "/";
        private NancyHost Host;

        // NOTE: It's better to hold reference to Simulation object, not index
        //       Wait till Eric will start loading the scene and replace this ID
        //       with real simulation object. ID could be saved in this object during start.
        //       When simulation is not running this reference will be null.
        public static Simulation currentSimulation;

        // TODO: create a separate class with static configuration
        public static string ApplicationRoot;

        public Button button;

        void Start()
        {
            ApplicationRoot = Path.Combine(Application.dataPath, "..");
            var path = Path.Combine(Application.persistentDataPath, "data.db");
            DatabaseManager.Init($"Data Source = {path};version=3;");

            // Bind to all interfaces instead of localhost
            var config = new HostConfiguration { RewriteLocalhost = true };
            url = $"http://localhost:{port}";

            Host = new NancyHost(new MyBootstrapper(), config, new Uri(url));
            Host.Start();
            DownloadManager.Init();
          
            DontDestroyOnLoad(this);
            DontDestroyOnLoad(FindObjectOfType<Camera>());

            // Add button click listener
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
}