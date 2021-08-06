/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator.Web;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadedAssetsWindow : MonoBehaviour
{
    public Transform Content;
    public GameObject LoadedAssetPrefab;

    public Button CloseButton;
    public Button BackgroundButton;

    private List<GameObject> LoadedAssetsText = new List<GameObject>();

    private void Start()
    {
        CloseButton.onClick.AddListener(() => gameObject.SetActive(false));
        BackgroundButton.onClick.AddListener(() => gameObject.SetActive(false));
    }

    private void OnEnable()
    {
        foreach (var asset in Config.LoadedAssets)
        {
            var go = Instantiate(LoadedAssetPrefab, Content);
            var assetText = go.GetComponentInChildren<Text>();
            if (assetText)
            {
                assetText.text = $"Name: {asset.assetName}\nFormat: {asset.assetFormat}";
            }
            LoadedAssetsText.Add(go);
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < LoadedAssetsText.Count; i++)
        {
            Destroy(LoadedAssetsText[i]);
        }
        LoadedAssetsText.Clear();
    }
}
