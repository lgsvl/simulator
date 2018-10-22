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

public class EnvironmentManager : MonoBehaviour
{
    #region Singelton
    private static EnvironmentManager _instance = null;
    public static EnvironmentManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<EnvironmentManager>();
                if (_instance == null)
                    Debug.LogError("<color=red>UIManager Not Found!</color>");
            }
            return _instance;
        }
    }
    #endregion

    #region vars
    public Transform roadObjectsHolder;

    [Range(1,8)]
    public int laneCount = 1;
    public bool biDirectional = true;

    private int activeRoadCount = 100;

    public GameObject roadSegmentPrefab;
    private GameObject lastRoadSegment;
    private GameObject currentRoadSegment;
    private GameObject adjacentSegment;
    private List<List<GameObject>> roadSegmentsLanes;

    public Material[] roadMats;
    public Material[] roadInvMats;
    #endregion

    #region mono
    void Awake()
    {
        if (_instance == null)
            _instance = this;

        if (_instance != this)
            DestroyImmediate(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        Missive.AddListener<TestStateMissive>(OnTestStateChange);
    }

    private void OnDisable()
    {
        Missive.RemoveListener<TestStateMissive>(OnTestStateChange);
    }

    private void Update()
    {
        CheckUserVehicleDistance();
    }

    void OnApplicationQuit()
    {
        _instance = null;
        DestroyImmediate(gameObject);
    }
    #endregion

    #region missive
    private void OnTestStateChange(TestStateMissive missive)
    {
        switch (missive.state)
        {
            case TestState.None:
                ResetRoad();
                break;
            case TestState.Init:
                InitRoad();
                break;
            case TestState.Warmup:
                break;
            case TestState.Running:
                break;
            default:
                break;
        }
    }
    #endregion

    #region road
    private void InitRoad()
    {
        // clear old data
        if (roadSegmentsLanes != null)
        {
            for (int laneIndex = 0; laneIndex < roadSegmentsLanes.Count; laneIndex++)
            {
                for (int segmentIndex = 0; segmentIndex < activeRoadCount; segmentIndex++)
                {
                    Destroy(roadSegmentsLanes[laneIndex][segmentIndex]);
                }
                roadSegmentsLanes[laneIndex].Clear();
            }
            roadSegmentsLanes.Clear();
        }

        OpenScenarioData.RoadNetwork tempData = DataManager.Instance.GetRoadNetworkData();
        bool tempBool;
        int tempInt;
        if (bool.TryParse(tempData.biDirectional, out tempBool))
            biDirectional = tempBool;
        if (int.TryParse(tempData.laneCount, out tempInt))
            laneCount = tempInt;

        // create nested lists for lane road segments
        roadSegmentsLanes = new List<List<GameObject>>();
        for (int i = 0; i < laneCount * 2; i++)
            roadSegmentsLanes.Add(new List<GameObject>());

        for (int laneIndex = 0; laneIndex < roadSegmentsLanes.Count; laneIndex++)
        {
            for (int segmentIndex = 0; segmentIndex < activeRoadCount; segmentIndex++)
            {
                currentRoadSegment = Instantiate(roadSegmentPrefab, roadObjectsHolder);
                currentRoadSegment.GetComponent<RoadSegmentDataComponent>().lane = laneIndex;

                roadSegmentsLanes[laneIndex].Add(currentRoadSegment);

                // lane creation z
                if (segmentIndex != 0)
                {
                    lastRoadSegment = roadSegmentsLanes[laneIndex][segmentIndex - 1];
                    currentRoadSegment.transform.position = lastRoadSegment.transform.position + new Vector3(0f, 0f, lastRoadSegment.GetComponent<MeshRenderer>().bounds.size.z);
                }
                
                // lane creation x
                if (laneIndex != 0)
                {
                    adjacentSegment = roadSegmentsLanes[laneIndex - 1][segmentIndex];
                    currentRoadSegment.transform.position = new Vector3(adjacentSegment.GetComponent<MeshRenderer>().bounds.size.x * -laneIndex, 0f, currentRoadSegment.transform.position.z);
                }

                // lane rotate
                if (laneIndex >= laneCount)
                {
                    currentRoadSegment.transform.RotateAround(currentRoadSegment.transform.position, currentRoadSegment.transform.up, 180f);
                }

                currentRoadSegment.GetComponent<MeshRenderer>().sharedMaterial = GetRoadMaterial(laneIndex);
                AddRoadEdgeCollider(laneIndex, currentRoadSegment);
            }
        }
    }

    private void GenerateRoadRow()
    {
        lastRoadSegment = roadSegmentsLanes[0][roadSegmentsLanes[0].Count - 1];
        for (int i = 0; i < laneCount * 2; i++)
        {
            currentRoadSegment = Instantiate(roadSegmentPrefab, roadObjectsHolder);
            currentRoadSegment.GetComponent<RoadSegmentDataComponent>().lane = i;
            roadSegmentsLanes[i].Add(currentRoadSegment);

            // row creation z
            currentRoadSegment.transform.position = lastRoadSegment.transform.position + new Vector3(0f, 0f, lastRoadSegment.GetComponent<MeshRenderer>().bounds.size.z);

            // row creation x
            if (i != 0)
            {
                adjacentSegment = roadSegmentsLanes[i - 1][roadSegmentsLanes[0].Count - 1];
                currentRoadSegment.transform.position = new Vector3(adjacentSegment.GetComponent<MeshRenderer>().bounds.size.x * -i, 0f, currentRoadSegment.transform.position.z);
            }

            // row rotate
            if (i >= laneCount)
            {
                currentRoadSegment.transform.RotateAround(currentRoadSegment.transform.position, currentRoadSegment.transform.up, 180f);
            }

            currentRoadSegment.GetComponent<MeshRenderer>().sharedMaterial = GetRoadMaterial(i);
            AddRoadEdgeCollider(i, currentRoadSegment);
        }
    }

    private Material GetRoadMaterial(int laneIndex)
    {
        Material tempMat;

        if (biDirectional)
        {
            if (laneCount == 1) // single lane
            {
                tempMat = roadMats[2];
            }
            else if (laneCount == 2) // inner and outer
            {
                if (laneIndex < laneCount)
                {
                    if (laneIndex == 0)
                        tempMat = roadMats[0];
                    else
                        tempMat = roadMats[3];
                }
                else
                {
                    if (laneIndex == laneCount)
                        tempMat = roadInvMats[3];
                    else
                        tempMat = roadInvMats[0];
                }
            }
            else // inner middle and outer
            {
                if (laneIndex < laneCount)
                {
                    if (laneIndex == 0)
                        tempMat = roadMats[0];
                    else if (laneIndex == laneCount - 1)
                        tempMat = roadMats[3];
                    else
                        tempMat = roadMats[1];
                }
                else
                {
                    if (laneIndex == laneCount * 2 - 1)
                        tempMat = roadInvMats[0];
                    else if (laneIndex == laneCount)
                        tempMat = roadInvMats[3];
                    else
                        tempMat = roadInvMats[1];
                }
            }
        }
        else
        {
            if (laneCount == 1)
            {
                if (laneIndex < laneCount)
                    tempMat = roadMats[0];
                else
                    tempMat = roadInvMats[0];
            }
            else if (laneCount == 2)
            {
                if (laneIndex < laneCount)
                {
                    if (laneIndex == 0)
                        tempMat = roadMats[0];
                    else
                        tempMat = roadMats[1];
                }
                else
                {
                    if (laneIndex == laneCount)
                        tempMat = roadInvMats[1];
                    else
                        tempMat = roadInvMats[0];
                }
            }
            else
            {
                if (laneIndex < laneCount)
                {
                    if (laneIndex == 0)
                        tempMat = roadMats[0];
                    else
                        tempMat = roadMats[1];
                }
                else
                {
                    if (laneIndex == laneCount * 2 - 1)
                        tempMat = roadInvMats[0];
                    else
                        tempMat = roadInvMats[1];
                }
            }
        }
        return tempMat;
    }

    private void CheckUserVehicleDistance()
    {
        if (VehicleManager.Instance.currentTestVehicle == null) return;
        if (roadSegmentsLanes == null || roadSegmentsLanes.Count == 0) return;
        if (roadSegmentsLanes[0] == null || roadSegmentsLanes[0].Count == 0) return;

        float dist = Vector3.Distance(VehicleManager.Instance.currentTestVehicle.transform.position, roadSegmentsLanes[laneCount][0].transform.position);
        var dot = Vector3.Dot(VehicleManager.Instance.currentTestVehicle.transform.forward, VehicleManager.Instance.currentTestVehicle.transform.InverseTransformPoint(roadSegmentsLanes[laneCount][0].transform.position));
        if (dist > 75 && dot < 0)
        {
            for (int j = 0; j < roadSegmentsLanes.Count; j++)
            {
                Destroy(roadSegmentsLanes[j][0]);
                roadSegmentsLanes[j].RemoveAt(0);
            }
            GenerateRoadRow();
        }
    }

    private void AddRoadEdgeCollider(int laneIndex, GameObject go)
    {
        if (laneIndex == 0 || laneIndex == roadSegmentsLanes.Count - 1)
        {
            Vector3 bounds = go.GetComponent<MeshRenderer>().bounds.size;
            BoxCollider col = go.AddComponent<BoxCollider>();

            col.center = new Vector3(bounds.x / 2, bounds.x / 2, 0f);
            col.size = new Vector3(0.0125f, bounds.x, bounds.z);
        }
    }

    public void ResetRoad()
    {
        // clear old data
        if (roadSegmentsLanes != null)
        {
            for (int laneIndex = 0; laneIndex < roadSegmentsLanes.Count; laneIndex++)
            {
                for (int segmentIndex = 0; segmentIndex < activeRoadCount; segmentIndex++)
                {
                    Destroy(roadSegmentsLanes[laneIndex][segmentIndex]);
                }
                roadSegmentsLanes[laneIndex].Clear();
            }
            roadSegmentsLanes.Clear();
        }
    }
    #endregion

}
