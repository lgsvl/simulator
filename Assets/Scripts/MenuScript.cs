/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuScript : MonoBehaviour
{
    public GameObject MainPanel;
    public GameObject FreeRoamingPanel;
    public Image MapImage;
    private Sprite defaultMapSprite;

    public AssetBundleManager assetBundleManager;
    private List<string> loadableSceneNames = new List<string>();
    private List<AssetBundle> allLoadedBundles = new List<AssetBundle>();
    private string selectedSceneName = "";

    public Dropdown MapDropdown;
    private List<Sprite> MapSprites = new List<Sprite>();

    public GameObject DuckieBot;
    public GameObject Sedan;

    public MenuAddRobot MenuAddRobot;

    public Text RosRunError;

    public RosRobots Robots;

    public GameObject DuckiebotRobot;
    public Canvas UserInterface;
    public Canvas UserInterfaceRobotList;

    public Button RunButton;

    public GameObject aboutPanel;
    public Text buildVersionText;

    GameObject CurrentPanel;

    static internal bool IsTrainingMode = false;

    public void Start()
    {
        if (defaultMapSprite == null)
        {
            defaultMapSprite = MapImage.sprite;
        }

        CurrentPanel = MainPanel;

        foreach (var robot in Robots.Robots)
        {
            MenuAddRobot.Add(robot);
        }

        if (Robots.Robots.Count == 0)
        {
            MenuAddRobot.Add(Robots.Add());
        }

        UpdateMapsAndMenu();
        InitGlobalSettings();
    }

    public static void InitGlobalSettings()
    {
        QualitySettings.shadowDistance = 20f;
        QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
    }

    public void ShowFreeRoaming()
    {
        Activate(FreeRoamingPanel);
        var title = GameObject.Find("MapChooseTitleText").GetComponent<Text>();
        title.text = "Free Roaming";
        IsTrainingMode = false;
    }

    public void ShowTraining()
    {
        Activate(FreeRoamingPanel);
        var title = GameObject.Find("MapChooseTitleText").GetComponent<Text>();
        title.text = "Training";
        IsTrainingMode = true;
    }

    public void ShowAbout()
    {
        Activate(aboutPanel);
        MainPanel.SetActive(true);
        buildVersionText.text = $"Build Version: {BuildInfo.buildVersion}";
        IsTrainingMode = false;
    }

    public void ShowMainmenu()
    {
        Activate(MainPanel);
    }

    IEnumerator HideErrorAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        var color = RosRunError.color;
        for (int i = 0; i < 10; i++)
        {
            color.a = 1.0f - i / 10.0f;
            RosRunError.color = color;
            yield return new WaitForSeconds(0.1f);
        }
        color.a = 0.0f;
        RosRunError.color = color;
    }

    public void UpdateMapsAndMenu()
    {
        if (!Application.isEditor)
        {
            allLoadedBundles.ForEach(b => b.Unload(false));
            allLoadedBundles.Clear();
        }

        MapDropdown.ClearOptions();
        loadableSceneNames.Clear();
        MapSprites.Clear();

#if UNITY_EDITOR
        if (assetBundleManager != null)
        {
            foreach (var map in assetBundleManager.assetBundleSettings.maps)
            {
                var scn = map.sceneAsset as UnityEditor.SceneAsset;
                if (scn != null)
                {
                    var sceneName = scn.name;
                    if (Application.CanStreamedLevelBeLoaded(sceneName) && !loadableSceneNames.Contains(sceneName))
                    {
                        loadableSceneNames.Add(sceneName);
                        MapSprites.Add(map.spriteImg);
                    }
                }
            }
            MapDropdown.AddOptions(loadableSceneNames);
        }
#endif

        if (!Application.isEditor)
        {
            var bundleRoot = Path.Combine(Application.dataPath, "..", "AssetBundles");
            var files = Directory.GetFiles(bundleRoot);
            foreach (var f in files)
            {
                if (Path.HasExtension(f))
                {
                    continue;
                }
                var filename = Path.GetFileName(f);
                if (filename.StartsWith("map_"))
                {
                    var mapName = filename.Substring("map_".Length);
                    var bundle = AssetBundle.LoadFromFile(f); //will take long with many scenes so change to async later
                    if (bundle != null)
                    {
                        allLoadedBundles.Add(bundle);
                    }
                    string[] scenes = bundle.GetAllScenePaths(); //assume each bundle has at most one scene
                    if (scenes.Length > 0)
                    {
                        string sceneName = Path.GetFileNameWithoutExtension(scenes[0]);
                        loadableSceneNames.Add(sceneName);
                        Sprite spriteImg = null;
                        var spriteBundleFile = f.Replace($"map_{mapName}", $"mapimage_{mapName}");
                        if (File.Exists(spriteBundleFile))
                        {
                            var spriteBundle = AssetBundle.LoadFromFile(spriteBundleFile);
                            if (spriteBundle != null)
                            {
                                allLoadedBundles.Add(spriteBundle);
                                spriteImg = spriteBundle.LoadAsset<Sprite>($"mapimage_{mapName}");
                            }
                        } 
                        MapSprites.Add(spriteImg);
                    }
                }
            }
            MapDropdown.AddOptions(loadableSceneNames);
        }

        MapDropdown.value = 0;
        ChangeMapImage();
    }

    public void OnRunClick()
    {
        bool allConnected = true;
        foreach (var robot in Robots.Robots)
        {
            if (robot.Bridge.Status != Ros.Status.Connected)
            {
                allConnected = false;
                break;
            }
        }

        if (!allConnected)
        {
            if (Input.GetKey(KeyCode.LeftShift) == false)
            {
                var color = RosRunError.color;
                color.a = 1.0f;
                RosRunError.color = color;
                StartCoroutine(HideErrorAfter(3.0f));
                return;
            }
        }

        Robots.Save();

        selectedSceneName = loadableSceneNames[MapDropdown.value];

        // TODO: add nice loading progress to both async operations (bundle and scene loading)
        var loader = SceneManager.LoadSceneAsync(selectedSceneName);
        loader.completed += SceneLoadFinished;

        RunButton.interactable = false;
    }

    void SceneLoadFinished(AsyncOperation op)
    {
        if (!Application.isEditor)
        {
            allLoadedBundles.ForEach(b => b.Unload(false));
            allLoadedBundles.Clear();
        }

        var robotListCanvas = Instantiate(UserInterfaceRobotList);
        var robotList = robotListCanvas.transform.FindDeepChild("Content");

        float height = robotListCanvas.transform.FindDeepChild("RobotList").GetComponent<RectTransform>().rect.height;

        Vector3 spawnPosition = new Vector3(1.0f, 0.018f, 0.7f);
        Quaternion spawnRotation = Quaternion.identity;
        GameObject spawnTemplate = DuckieBot;

        var spawnInfo = FindObjectOfType<SpawnInfo>();
        if (spawnInfo != null)
        {
            spawnPosition = spawnInfo.transform.position;
            spawnRotation = spawnInfo.transform.rotation;
            if (spawnInfo.type == SpawnInfo.Type.Duckiebot)
            {
                spawnTemplate = DuckieBot;
            }
            else if (spawnInfo.type == SpawnInfo.Type.Sedan)
            {
                spawnTemplate = Sedan;
            }
            spawnInfo.ChangeGlobalSettings();
            Destroy(spawnInfo.gameObject);
        }

        for (int i = 0; i < Robots.Robots.Count; i++)
        {
            var robotImage = Instantiate(DuckiebotRobot, robotList);
            robotImage.transform.FindDeepChild("Address").GetComponent<Text>().text = Robots.Robots[i].PrettyAddress;
            var ilocal = i;
            var button = robotImage.GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                for (int k = 0; k < Robots.Robots.Count; k++)
                {
                    Robots.Robots[k].UiObject.enabled = k == ilocal;
                    var b = Robots.Robots[k].UiButton.GetComponent<Button>();
                    var c = b.colors;
                    c.normalColor = k == ilocal ? new Color(1, 1, 1) : new Color(0.8f, 0.8f, 0.8f);
                    b.colors = c;
                    Robots.Robots[k].Robot.GetComponent<RobotSetup>().FollowCamera.gameObject.SetActive(k == ilocal);
                }
            });

            var bot = Instantiate(spawnTemplate, spawnPosition - new Vector3(0.25f * i, 0, 0), spawnRotation);
            var bridge = Robots.Robots[i];

            var uiObject = Instantiate(UserInterface);
            uiObject.GetComponent<RfbClient>().Address = Robots.Robots[i].Address;
            var ui = uiObject.transform;
            uiObject.GetComponent<UserInterfaceSetup>().MainPanel.transform.Translate(new Vector3(0, -height, 0));
            bridge.UiObject = uiObject;
            bridge.UiButton = robotImage;
            bridge.BridgeStatus = uiObject.GetComponent<UserInterfaceSetup>().BridgeStatus;
            ui.GetComponent<HelpScreenUpdate>().Robots = Robots;

            bot.GetComponent<RobotSetup>().Setup(ui.GetComponent<UserInterfaceSetup>(), bridge.Bridge);

            bot.GetComponent<RobotSetup>().FollowCamera.gameObject.SetActive(i == 0);
            uiObject.enabled = i == 0;
            var colors = button.colors;
            colors.normalColor = i == 0 ? new Color(1, 1, 1) : new Color(0.8f, 0.8f, 0.8f);
            button.colors = colors;

            var name = new GameObject($"duckiebot_{i}_name");
            name.transform.parent = robotListCanvas.transform.FindDeepChild("Panel").transform;
            bridge.UiName = name.AddComponent<Text>();
            bridge.UiName.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            bridge.UiName.text = Robots.Robots[i].PrettyAddress;
            bridge.UiName.fontSize = 16;
            bridge.UiName.fontStyle = FontStyle.Bold;
            bridge.UiName.horizontalOverflow = HorizontalWrapMode.Overflow;
            bridge.UiName.verticalOverflow = VerticalWrapMode.Overflow;

            bridge.Robot = bot;
        }
    }

    public static void AssignBridge(GameObject robot, Ros.Bridge bridge)
    {
        var components = robot.GetComponentsInChildren(typeof(Component));
        foreach (var component in components)
        {
            var ros = component as Ros.IRosClient;
            if (ros != null)
            {
                ros.OnRosBridgeAvailable(bridge);
            }
        }
    }

    public void ChangeMapImage()
    {
        if (MapDropdown.value >= MapSprites.Count)
        {
            return;
        }

        var s = MapSprites[MapDropdown.value];
        if (s == null)
        {
            s = defaultMapSprite;
        }
        MapImage.sprite = s;
    }

    void Activate(GameObject target)
    {
        target.SetActive(true);
        CurrentPanel.SetActive(false);
        CurrentPanel = target;
    }
}
