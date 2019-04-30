using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Web;

public class BundleManager : MonoBehaviour {
    public static BundleManager instance { get; private set; }

    private List<string> bundlesToLoad = new List<string>();

    private void Start()
    {
        instance = this;
        StartCoroutine(WaitToLoad());
    }

    public void Load(string bundle)
    {
        bundlesToLoad.Add(bundle);
    }

    IEnumerator WaitToLoad()
    {
        WaitForEndOfFrame wait = new WaitForEndOfFrame();
        while (true) {
            // TODO: create a logic of loading different maps here
            if (bundlesToLoad.Count > 0)
            {
                string assetPath = Path.Combine(bundlesToLoad[bundlesToLoad.Count - 1]);
                bundlesToLoad.RemoveAt(bundlesToLoad.Count -1);
                AssetBundle currentBundle = AssetBundle.LoadFromFile(assetPath); // will take long with many scenes so change to async later
                if (currentBundle != null)
                {
                    string[] scenes = currentBundle.GetAllScenePaths(); // assume each bundle has at most one scene TODO unload scene async
                    if (scenes.Length > 0)
                    {
                        // NOTE: According to our wiki page there is only one scene to load: MapName.scene
                        // https://wiki.lgsvl.com/display/AUT/Unity+Environments+Content+Pipeline+and+Directory+Structure
                        string sceneName = Path.GetFileNameWithoutExtension(scenes[0]);
                        WebClient.SendNotification(new ClientMessage("DownloadUpdate", $"Initiating load of {sceneName}"));
                        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                        WebClient.SendNotification(new ClientMessage("SimulationUpdate", $"Running"));
                    }
                    else
                    {
                        foreach(string s in currentBundle.GetAllAssetNames())
                        {
                            Debug.Log($"Loading asset: {s}");
                            WebClient.SendNotification(new ClientMessage("DownloadUpdate", $"Instantiating {s}"));
                            GameObject.Instantiate(currentBundle.LoadAsset(s));
                        }

                        // ??? throw RUNTINE ERROR
                    }
                }
            }

            yield return wait;
        }
    }
}
