using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public class Bootstrap : MonoBehaviour {

    private GameObject camera = null;

	// Use this for initialization
	void Start () {

        // TODO: create a logic of loading different maps here

        string assetPath = Application.dataPath + "/AssetBundles/map_cubetown";       
        //string assetPath = "D:/EricSterner/Repos/SampleHDRP/Unity/HDRPTestProject/Assets/AssetBundles/map_cubetown";

        AssetBundle currentBundle = AssetBundle.LoadFromFile(assetPath); //will take long with many scenes so change to async later
        if (currentBundle != null)
        {
            string[] scenes = currentBundle.GetAllScenePaths(); //assume each bundle has at most one scene TODO unload scene async
            if (scenes.Length > 0)
            {
                string sceneName = Path.GetFileNameWithoutExtension(scenes[0]);
                SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            } else
            {
                // ??? throw RUNTINE ERROR 
            }


        }
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
