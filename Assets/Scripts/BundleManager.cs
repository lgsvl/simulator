using Database;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Web;
using Web.Modules;

public class BundleManager : MonoBehaviour {
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
        while (true) {
            // TODO: create a logic of loading different maps here
            if (configsToLoad.Count > 0)
            {
                ConfigModel config = configsToLoad.Dequeue();
                string assetPath = Path.Combine(config.Map);
                AssetBundle currentBundle = AssetBundle.LoadFromFile(assetPath); // will take long with many scenes so change to async later
                if (currentBundle != null)
                {
                    string[] scenes = currentBundle.GetAllScenePaths(); // assume each bundle has at most one scene TODO unload scene async
                    if (scenes.Length > 0)
                    {
                    //NOTE: According to our wiki page there is only one scene to load: MapName.scene
                    //https://wiki.lgsvl.com/display/AUT/Unity+Environments+Content+Pipeline+and+Directory+Structure
                        string sceneName = Path.GetFileNameWithoutExtension(scenes[0]);
                        NotificationManager.SendNotification(new ClientNotification("DownloadUpdate", $"Initiating load of {sceneName}"));
                        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                        using (var db = DatabaseManager.Open())
                        {
                            var model = db.SingleOrDefault<Simulation>(MainMenu.currentRunningId);
                            model.Status = "Running";
                            db.Update(model);
                           NotificationManager.SendNotification(new ClientNotification("SimulationUpdate", SimulationModule.ConvertSimToResponse(model)));
                        }
                    }
                }
            }

            yield return wait;
        }
    }
}
