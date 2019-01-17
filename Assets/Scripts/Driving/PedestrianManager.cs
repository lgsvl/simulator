/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianManager : MonoBehaviour
{
    #region Singleton
    private static PedestrianManager _instance = null;
    public static PedestrianManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<PedestrianManager>();
                if (_instance == null)
                    Debug.LogError("<color=red>PedestrianManager Not Found!</color>");
            }
            return _instance;
        }
    }
    #endregion

    #region vars
    public bool isOptimizing = true;
    private bool isPedActive = false;
    public int pedTotalCount = 100;
    private int pedPerSegmentCount = 0;
    public GameObject pedPrefab;
    public List<GameObject> pedestrians; //source prefabs

    private List<MapPedestrianSegmentBuilder> pedSegments = new List<MapPedestrianSegmentBuilder>();
    
    private List<GameObject> pedPool = new List<GameObject>();
    private List<GameObject> pedActive = new List<GameObject>();

    private float pedRendDistanceThreshold = 225.0f;
    private int performanceUpdateRate = 60;
    private int frameCount = 0;
    #endregion

    #region mono
    private void Start()
    {
        InitPedestrians();
    }

    private void Update()
    {
        OptimizePedestrians();
    }

    private void OnApplicationQuit()
    {
        _instance = null;
        DestroyImmediate(gameObject);
    }
    #endregion

    #region pedestrians
    public void OptimizePedestrians()
    {
        if (!isOptimizing) return;
        if (!isPedActive) return;
        
        frameCount++;

        if (frameCount < performanceUpdateRate) return;
        
        frameCount = 0;

        // check if ped needs returned to pool
        for (int i = 0; i < pedActive.Count; i++)
        {
            if (!CheckPedestrianInView(pedActive[i].transform.position))
            {
                ReturnPedestrianToPool(pedActive[i]);
            }
        }

        // get ped per seg count
        pedPerSegmentCount = 0;
        foreach (var seg in pedSegments)
        {
            if (CheckPedestrianInView(seg.transform.position))
                pedPerSegmentCount++;
        }

        // check if ped can be made active
        foreach (var seg in pedSegments)
        {
            for (var i = 0; i < seg.segment.targetWorldPositions.Count; i++)
            {
                if (CheckPedestrianInView(seg.segment.targetWorldPositions[i]))
                {
                    int addCount = pedPerSegmentCount - seg.transform.childCount;
                    if (addCount > 0)
                    {
                        for (int j = 0; j < addCount; j++)
                            SpawnPedestrian(seg);
                    }
                    break; // found a waypoint within threshold so spawn at this seg
                }
            }
        }   
    }

    public bool CheckPedestrianInView(Vector3 pos)
    {
        return (TrafPerformanceManager.Instance.DistanceToNearestPlayerCamera(pos) < pedRendDistanceThreshold); // TODO change
    }

    public void SpawnPedestrians()
    {
        foreach (var seg in pedSegments)
        {
            for (var i = 0; i < seg.segment.targetWorldPositions.Count; i++)
            {
                if (CheckPedestrianInView(seg.segment.targetWorldPositions[i]))
                {
                    for (int j = 0; j < pedPerSegmentCount; j++)
                    {
                        SpawnPedestrian(seg);
                    }
                    break; // found a waypoint within threshold so spawn at this seg
                }
            }
        }
        isPedActive = true;
    }
    
    public void KillPedestrians()
    {
        isPedActive = false;
        for (int i = 0; i < pedActive.Count; i++)
        {
            ReturnPedestrianToPool(pedActive[i]);
        }
        pedActive.Clear();
    }

    private void InitPedestrians()
    {
        pedSegments.Clear();
        pedSegments = new List<MapPedestrianSegmentBuilder>(FindObjectsOfType<MapPedestrianSegmentBuilder>());
        pedPerSegmentCount = Mathf.FloorToInt(pedTotalCount / pedSegments.Count);

        pedPool.Clear();
        pedActive.Clear();
        for (int i = 0; i < pedSegments.Count; i++)
        {
            for (int j = 0; j < pedPerSegmentCount; j++)
            {
                foreach (var localPos in pedSegments[i].segment.targetLocalPositions)
                    pedSegments[i].segment.targetWorldPositions.Add(pedSegments[i].transform.TransformPoint(localPos)); //Convert ped segment local to world position

                GameObject ped = Instantiate(pedPrefab, Vector3.zero, Quaternion.identity, transform);
                pedPool.Add(ped);
                Instantiate(pedestrians[(int)Random.Range(0, pedestrians.Count)], ped.transform);
                ped.SetActive(false);
            }
        }
    }

    public void SpawnPedestrian(MapPedestrianSegmentBuilder seg)
    {
        GameObject ped = pedPool[0];
        ped.transform.SetParent(seg.transform);
        pedPool.RemoveAt(0);
        pedActive.Add(ped);
        ped.SetActive(true);
        PedestrianComponent pedC = ped.GetComponent<PedestrianComponent>();
        if (pedC != null)
            pedC.InitPed(seg.segment.targetWorldPositions);
    }

    public void ReturnPedestrianToPool(GameObject go)
    {
        go.transform.SetParent(transform);
        go.SetActive(false);
        pedActive.Remove(go);
        pedPool.Add(go);
    }
    #endregion
}
