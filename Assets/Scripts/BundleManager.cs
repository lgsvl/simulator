using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class BundleManager : MonoBehaviour {
    private static BundleManager _instance;
    public static BundleManager instance { get { return _instance; } set { _instance = value; } }
    string bundleToLoad = "";
    public List<string> bundles;

    private void Start()
    {
        _instance = this;
        GetBundles();
        StartCoroutine(WaitToLoad());
    }

    void GetBundles()
    {
        bundles = new List<string>();
        string assetPath = Path.Combine(Application.dataPath, "..", "AssetBundles/Environments/");
        var files = Directory.GetFiles(assetPath);
        foreach (var f in files)
        {
            if (Path.HasExtension(f))
            {
                continue;
            }
            var filename = Path.GetFileName(f);
            if (filename.StartsWith("map_"))
            {
                bundles.Add(filename);
            }
        }
    }

    // Use this for initialization
    public void Load (string bundle) {
        bundleToLoad = bundle;
    }

    IEnumerator WaitToLoad()
    {
        WaitForEndOfFrame wait = new WaitForEndOfFrame();
        while(bundleToLoad == string.Empty)
        {
            yield return wait;
        }
        
        // TODO: create a logic of loading different maps here

        string assetPath = Path.Combine(Application.dataPath, "..", "AssetBundles/Environments/" + bundleToLoad);
        AssetBundle currentBundle = AssetBundle.LoadFromFile(assetPath); //will take long with many scenes so change to async later
        if (currentBundle != null)
        {
            string[] scenes = currentBundle.GetAllScenePaths(); //assume each bundle has at most one scene TODO unload scene async
            if (scenes.Length > 0)
            {
                string sceneName = Path.GetFileNameWithoutExtension(scenes[0]);
                SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            }
            else
            {
                // ??? throw RUNTINE ERROR 
            }

        }

        yield return null;
    }
}
