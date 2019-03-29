/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


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
    private List<MapPedestrianSegmentBuilder> segInView = new List<MapPedestrianSegmentBuilder>();
    private float pedRendDistanceThreshold = 250.0f;
    private int performanceUpdateRate = 60;
    private int frameCount = 0;

    //private Transform activeCamera;
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
        if (pedSegments == null || pedSegments.Count == 0) return;

        frameCount++;

        if (frameCount < performanceUpdateRate) return;
        
        frameCount = 0;

        // check if ped needs returned to pool
        for (int i = 0; i < pedActive.Count; i++)
        {
            if (!CheckPositionInView(pedActive[i].transform.position))
            {
                ReturnPedestrianToPool(pedActive[i]);
            }
        }

        // get ped per seg count
        segInView.Clear();
        for (int i = 0; i < pedSegments.Count; i++)
        {
            for (var j = 0; j < pedSegments[i].segment.targetWorldPositions.Count; j++)
            {
                if (CheckPositionInView(pedSegments[i].segment.targetWorldPositions[j]))
                {
                    segInView.Add(pedSegments[i]);
                    break; // found a seg within threshold so add to segInView list
                }
            }
        }
        if (segInView.Count != 0)
            pedPerSegmentCount = Mathf.FloorToInt(pedTotalCount / segInView.Count);

        // check if ped can be made active
        for (int i = 0; i < segInView.Count; i++)
        {
            for (var j = 0; j < segInView[i].segment.targetWorldPositions.Count; j++)
            {
                if (CheckPositionInView(segInView[i].segment.targetWorldPositions[j]))
                {
                    int addCount = pedPerSegmentCount - segInView[i].transform.childCount;
                    if (addCount > 0)
                    {
                        for (int k = 0; k < addCount; k++)
                            SpawnPedestrian(segInView[i]);
                    }
                    break; // found a waypoint within threshold so spawn at this seg
                }
            }
        }   
    }

    public bool CheckPositionInView(Vector3 pos)
    {
        return ROSAgentManager.Instance.GetDistanceToActiveAgent(pos) < pedRendDistanceThreshold;
    }

    public void SpawnPedestrians()
    {
        isPedActive = true;
    }
    
    public void KillPedestrians()
    {
        if (pedSegments == null || pedSegments.Count == 0) return;

        isPedActive = false;
        List<PedestrianComponent> peds = new List<PedestrianComponent>(FindObjectsOfType<PedestrianComponent>());
        for (int i = 0; i < peds.Count; i++)
        {
            ReturnPedestrianToPool(peds[i].gameObject);
        }
        pedActive.Clear();
    }

    private void InitPedestrians()
    {
        pedSegments.Clear();
        pedSegments = new List<MapPedestrianSegmentBuilder>(FindObjectsOfType<MapPedestrianSegmentBuilder>());
        for (int i = 0; i < pedSegments.Count; i++)
        {
            foreach (var localPos in pedSegments[i].segment.targetLocalPositions)
                pedSegments[i].segment.targetWorldPositions.Add(pedSegments[i].transform.TransformPoint(localPos)); //Convert ped segment local to world position 
        }

        pedPool.Clear();
        for (int i = 0; i < pedTotalCount; i++)
        {
            GameObject ped = Instantiate(pedPrefab, Vector3.zero, Quaternion.identity, transform);
            pedPool.Add(ped);
            Instantiate(pedestrians[(int)Random.Range(0, pedestrians.Count)], ped.transform);
            ped.SetActive(false);
        }
        SegmentationManager.Instance.OverrideMaterialsPedestriansSpawned(pedPool);
    }

    public void SpawnPedestrian(MapPedestrianSegmentBuilder seg)
    {
        if (pedPool.Count == 0) return;

        GameObject ped = pedPool[0];
        ped.transform.SetParent(seg.transform);
        pedPool.RemoveAt(0);
        pedActive.Add(ped);
        ped.SetActive(true);
        PedestrianComponent pedC = ped.GetComponent<PedestrianComponent>();
        if (pedC != null)
            pedC.InitPed(seg.segment.targetWorldPositions);
    }

    public GameObject SpawnPedestrianApi(string name, Vector3 position, Quaternion rotation)
    {
        var prefab = pedestrians.Find(obj => obj.name == name);
        if (prefab == null)
        {
            return null;
        }

        GameObject ped = Instantiate(pedPrefab, Vector3.zero, Quaternion.identity, transform);
        Instantiate(prefab, ped.transform);
        ped.GetComponent<PedestrianComponent>().InitManual(position, rotation);
        return ped;
    }

    public void DespawnPedestrianApi(PedestrianComponent ped)
    {
        Destroy(ped.gameObject);
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
