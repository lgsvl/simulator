using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Web;
using Web.Modules;

public class BundleManager : MonoBehaviour
{
    public static BundleManager instance { get; private set; }

    private Queue<ConfigModel> configsToLoad = new Queue<ConfigModel>();

    private void Start()
    {
        instance = this;
        StartCoroutine(WaitToLoad());
    }

    public void Load(ConfigModel bundle)
    {
        configsToLoad.Enqueue(bundle);
    }

    IEnumerator WaitToLoad()
    {
        WaitForEndOfFrame wait = new WaitForEndOfFrame();
        while (true)
        {
            // TODO: create a logic of loading different maps here
            if (configsToLoad.Count > 0)
            {
                ConfigModel config = configsToLoad.Dequeue();
                string mapAssetPath = config.Map;
                string[] agentAssetPaths = config.Vehicles;

                // load map
                string sceneName = "";
                AssetBundle currentBundle = AssetBundle.LoadFromFile(mapAssetPath); // will take long with many scenes so change to async later
                if (currentBundle != null)
                {
                    string[] scenes = currentBundle.GetAllScenePaths(); // assume each bundle has at most one scene TODO unload scene async
                    if (scenes.Length > 0)
                    {
                        //NOTE: According to our wiki page there is only one scene to load: MapName.scene
                        //https://wiki.lgsvl.com/display/AUT/Unity+Environments+Content+Pipeline+and+Directory+Structure
                        sceneName = Path.GetFileNameWithoutExtension(scenes[0]);
                        NotificationManager.SendNotification(new ClientNotification("DownloadUpdate", $"Initiating load of {sceneName}"));
                        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                        MainMenu.currentSimulation.Status = "Running";
                        NotificationManager.SendNotification(new ClientNotification("SimulationUpdate", SimulationModule.ConvertSimToResponse(MainMenu.currentSimulation)));
                        
                        GameObject simObj = Resources.Load<GameObject>("SimulatorManager");
                        if (simObj == null)
                        {
                            Debug.LogError("Missing SimulatorManager.prefab in Resources folder!");
                            yield break;
                        }
                        GameObject clone = Instantiate(simObj);
                        clone.name = "SimulatorManager";
                    }
                }

                // load agents
                List<GameObject> agents = new List<GameObject>();
                for (int i = 0; i < agentAssetPaths.Length; i++)
                {
                    AssetBundle currentAgentBundle = AssetBundle.LoadFromFile(agentAssetPaths[i]);
                    if (currentAgentBundle != null)
                    {
                        var assetNames = currentAgentBundle.GetAllAssetNames();
                        foreach (var name in assetNames)
                        {
                            if (name.Contains(".prefab"))
                            {
                                var agentPrefab = currentAgentBundle.LoadAsset<GameObject>(name);
                                agents.Add(agentPrefab);
                            }
                        }
                    }
                }

                // finished loading
                ConfigData data = new ConfigData {
                    Name = config.Name,
                    Status = config.Status,
                    Id = config.Id,
                    Cluster = config.Cluster,
                    Map = config.Map,
                    Vehicles = config.Vehicles,
                    ApiOnly = config.ApiOnly,
                    Interactive = config.Interactive,
                    OffScreen = config.OffScreen,
                    TimeOfDay = config.TimeOfDay,
                    Rain = config.Rain,
                    Fog = config.Fog,
                    Wetness = config.Wetness,
                    Cloudiness = config.Cloudiness,
                    MapName = sceneName,
                    Agents = agents
                };
                SimulatorManager.Instance.LoadData(data);
                yield break;
            }

            yield return wait;
        }
    }
}
