/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator;
using Simulator.Web;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CacheControlWindow : MonoBehaviour
{
    public Transform Content;
    public GameObject CategoryHolderPrefab;

    public Button CloseButton;
    public Button BackgroundButton;

    public GameObject DeleteAllConfirmPanel;
    public Button DeleteAllCategoriesButton;
    public Button DeleteAllConfirmButton;
    public Button DeleteAllCancelButton;

    private List<GameObject> Categories = new List<GameObject>();

    private void Start()
    {
        CloseButton.onClick.AddListener(() => gameObject.SetActive(false));
        BackgroundButton.onClick.AddListener(() => gameObject.SetActive(false));
        DeleteAllCategoriesButton.onClick.AddListener(OpenDeleteAllConfirmPanel);
        DeleteAllConfirmButton.onClick.AddListener(DeleteAllConfirm);
        DeleteAllCancelButton.onClick.AddListener(DeleteAllCancel);
    }

    private void OnEnable()
    {
        PopulateCategories();
        DeleteAllConfirmPanel.SetActive(false);
    }

    private void OnDisable()
    {
        ClearCategories();
    }

    private void PopulateCategories()
    {
        foreach (BundleConfig.BundleTypes cat in Enum.GetValues(typeof(BundleConfig.BundleTypes)))
        {
            var go = Instantiate(CategoryHolderPrefab, Content);
            go.GetComponent<CacheCategory>().InitBundles(cat);
            Categories.Add(go);
        }

        var simGO = Instantiate(CategoryHolderPrefab, Content);
        simGO.GetComponent<CacheCategory>().InitSimulations();
        Categories.Add(simGO);
    }

    private void ClearCategories()
    {
        for (int i = 0; i < Categories.Count; i++)
        {
            Destroy(Categories[i]);
        }
        Categories.Clear();
    }

    private void OpenDeleteAllConfirmPanel()
    {
        DeleteAllConfirmPanel.SetActive(true);
    }

    private void DeleteAllConfirm()
    {
        foreach (var cat in Categories)
        {
            cat.GetComponent<CacheCategory>().DeleteCategory();
        }
        DeleteAllConfirmPanel.SetActive(false);
        ConnectionUI.instance.UpdateDropdown();
    }

    private void DeleteAllCancel()
    {
        DeleteAllConfirmPanel.SetActive(false);
    }
}
