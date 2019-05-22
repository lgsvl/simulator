/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SegmentationManager : MonoBehaviour
{
    #region Singleton
    private static SegmentationManager _instance = null;
    public static SegmentationManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<SegmentationManager>();
                if (_instance == null)
                    Debug.LogError("<color=red>SegmentationManager Not Found!</color>");
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
            _instance = this;

        if (_instance != this)
            DestroyImmediate(gameObject);
    }
    #endregion

    // needs to match sub shader and tag
    public enum SegmentationTypes
    {
        Car = 0,
        Road = 1,
        Sidewalk = 2,
        Vegetation = 3,
        Obstacle = 4,
        TrafficLight = 5,
        Building = 6,
        Sign = 7,
        Shoulder = 8,
        Pedestrian = 9
    };

    public Shader segmentationShader;
    public Color skyColor;

    public List<Material> trafficLightMats = new List<Material>();
    private MapManager mapManager;
    private void Start()
    {
        mapManager = FindObjectOfType<MapManager>();
        OverrideSegmentationMaterials(true);
        SetSegmentationCameras();
    }

    private void OnDisable()
    {
        OverrideSegmentationMaterials(false);
    }

    public void OverrideMaterialsNPCsSpawned(GameObject obj)
    {
        foreach (var renderer in obj.GetComponentsInChildren<Renderer>())
        {
            foreach (var mat in renderer.sharedMaterials)
            {
                mat?.SetOverrideTag("SegmentColor", "Car");
            }
        }
    }

    public void OverrideMaterialsNPCsSpawned(List<GameObject> objs)
    {
        // TODO hack needs better way
        objs.ForEach(OverrideMaterialsNPCsSpawned);
    }

    public void OverrideMaterialsPedestriansSpawned(List<GameObject> objs)
    {
        // TODO hack needs better way
        foreach (var obj in objs)
        {
            foreach (var renderer in obj.GetComponentsInChildren<Renderer>())
            {
                foreach (var mat in renderer.sharedMaterials)
                {
                    mat?.SetOverrideTag("SegmentColor", "Pedestrian");
                }
            }
        }
    }

    public void SetupVehicle(GameObject obj)
    {
        foreach (var renderer in obj.GetComponentsInChildren<Renderer>())
        {
            foreach (var mat in renderer.sharedMaterials)
            {
                mat?.SetOverrideTag("SegmentColor", "Car");
            }
        }
        SetSegmentationCameras();
    }

    private void OverrideSegmentationMaterials(bool isSet)
    {
        foreach (SegmentationTypes segType in System.Enum.GetValues(typeof(SegmentationTypes)))
        {
            var segObjs = GameObject.FindGameObjectsWithTag(segType.ToString()).ToList();
            if (segType == SegmentationTypes.Car)
            {
                segObjs.AddRange(GameObject.FindGameObjectsWithTag("Player"));
                if (NPCManager.Instance != null) // TODO hack needs better way
                {
                    foreach (var item in NPCManager.Instance.npcVehicles)
                    {
                        foreach (var element in item.GetComponentInChildren<VehicleMaterialComponent>().vehicleMaterialData)
                        {
                            foreach (var mat in element.mats)
                            {
                                mat?.SetOverrideTag("SegmentColor", isSet ? segType.ToString() : "");
                            }
                        }
                    }
                }
            }
            
            if (segType == SegmentationTypes.TrafficLight)
            {
                if (mapManager != null)
                {
                    mapManager.red?.SetOverrideTag("SegmentColor", isSet ? segType.ToString() : "");
                    mapManager.yellow?.SetOverrideTag("SegmentColor", isSet ? segType.ToString() : "");
                    mapManager.green?.SetOverrideTag("SegmentColor", isSet ? segType.ToString() : "");
                }
                foreach (var mat in trafficLightMats)
                {
                    mat?.SetOverrideTag("SegmentColor", isSet ? segType.ToString() : "");
                }
            }
            else
            {
                foreach (var obj in segObjs)
                {
                    foreach (var renderer in obj.GetComponentsInChildren<Renderer>())
                    {
                        foreach (var mat in renderer.sharedMaterials)
                        {
                            mat?.SetOverrideTag("SegmentColor", isSet ? segType.ToString() : "");
                        }
                    }
                }
            }
        }
    }

    private void SetSegmentationCameras()
    {
        VideoToROS[] videoToROS = FindObjectsOfType<VideoToROS>();
        foreach (var item in videoToROS)
        {
            item.InitSegmentation(segmentationShader, skyColor);
        }
    }
}
