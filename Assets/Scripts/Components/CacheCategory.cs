/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator;
using Simulator.Web;
using Simulator.Database;
using Simulator.Database.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class CacheCategory : MonoBehaviour
{
    public Text CategoryText;
    private Dropdown CategoryDropdown;
    private Button CategoryDeleteButton;

    private BundleConfig.BundleTypes CategoryType;
    private List<AssetModel> CategoryAssets;
    private List<SimulationData> SimulationsData;

    private AssetService Service = new AssetService();
    private SimulationService SimulationService = new SimulationService();

    public void InitBundles(BundleConfig.BundleTypes type)
    {
        CategoryDropdown = GetComponentInChildren<Dropdown>();
        CategoryDeleteButton = GetComponentInChildren<Button>();
        CategoryDeleteButton.onClick.AddListener(DeleteOption);

        CategoryType = type;
        CategoryText.text = CategoryType.ToString();
        RefreshOptions();
    }

    public void InitSimulations()
    {
        CategoryDropdown = GetComponentInChildren<Dropdown>();
        CategoryDeleteButton = GetComponentInChildren<Button>();
        CategoryDeleteButton.onClick.AddListener(DeleteOption);
        CategoryText.text = "Simulations";
        RefreshSimulationsOptions();
    }

    public void RefreshOptions()
    {
        CategoryAssets = Service.List(CategoryType).ToList();
        CategoryDropdown.ClearOptions();
        CategoryDropdown.AddOptions(CategoryAssets.Select(e => e.Name + " " + e.DateAdded).ToList());
        CategoryDropdown.value = 0;
        CategoryDropdown.transform.parent.gameObject.SetActive(CategoryAssets.Count != 0);
    }

    public void RefreshSimulationsOptions()
    {
        SimulationsData = SimulationService.List().ToList();
        CategoryDropdown.ClearOptions();
        CategoryDropdown.AddOptions(SimulationsData.Select(s => s.Name).ToList());
        CategoryDropdown.value = 0;
        CategoryDropdown.transform.parent.gameObject.SetActive(SimulationsData.Count != 0);
    }

    public void DeleteOption()
    {
        if (SimulationsData != null)
        {
            SimulationService.Delete(SimulationsData[CategoryDropdown.value]);
            RefreshSimulationsOptions();
        }
        else
        {
            string assetGuid = CategoryAssets[CategoryDropdown.value].AssetGuid;
            string assetPath = Path.Combine(Config.PersistentDataPath, CategoryType.ToString() + "s", assetGuid); // TODO align types with path
            Service.Delete(assetGuid);
            if (File.Exists(assetPath))
            {
                File.Delete(assetPath);
            }
            RefreshOptions();
        }

        ConnectionUI.instance.UpdateDropdown();
    }

    public void DeleteCategory()
    {
        if (SimulationsData != null)
        {
            foreach (var sim in SimulationsData)
            {
                SimulationService.Delete(sim);
            }
            RefreshSimulationsOptions();
        }
        else
        {
            Service.DeleteCategory(CategoryType);
            if (Directory.Exists(Path.Combine(Config.PersistentDataPath, CategoryType.ToString() + "s")))
            {
                DirectoryInfo di = new DirectoryInfo(Path.Combine(Config.PersistentDataPath, CategoryType.ToString() + "s"));
                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
            }
            RefreshOptions();
        }

        ConnectionUI.instance.UpdateDropdown();
    }
}
