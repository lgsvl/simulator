/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCManager : MonoBehaviour
{
    #region Singleton
    private static NPCManager _instance = null;
    public static NPCManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<NPCManager>();
                if (_instance == null)
                    Debug.LogError("<color=red>NPCManager Not Found!</color>");
            }
            return _instance;
        }
    }
    #endregion

    #region util
    private int NPCSpawnCheckBitmask = -1;
    private float checkRadius = 6f;
    private Collider[] collidersBuffer = new Collider[1];
    #endregion

    #region vars
    // npc
    public GameObject npcPrefab;
    public List<GameObject> npcVehicles = new List<GameObject>();
    public int npcCount = 0;
    private int activeNPCCount = 0;
    [HideInInspector]
    public List<GameObject> currentPooledNPCs = new List<GameObject>();
    [HideInInspector]
    public bool isInit = false;

    //private int lastRandLane = -1;
    //public float SpawnDelay { get; set; } = 2.5f;
    #endregion

    #region mono
    private void Awake()
    {
        if (_instance == null)
            _instance = this;

        if (_instance != this)
            DestroyImmediate(gameObject);
    }

    private IEnumerator Start()
    {
        if (MapManager.Instance == null) yield break;

        while (!MapManager.Instance.isInit)
            yield return null;

        NPCSpawnCheckBitmask = 1 << LayerMask.NameToLayer("NPC") | 1 << LayerMask.NameToLayer("Duckiebot");

        SpawnNPCPool();
        //InitNPCOnMap();
        isInit = true;
    }

    private void Update()
    {
        if (activeNPCCount < currentPooledNPCs.Count / 1.5f)
        {
            SetNPCOnMap();
        }
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }
    #endregion

    #region npc
    private void SpawnNPCPool()
    {
        for (int i = 0; i < currentPooledNPCs.Count; i++)
        {
            Destroy(currentPooledNPCs[i]);
        }
        currentPooledNPCs.Clear();
        activeNPCCount = 0;

        for (int i = 0; i < npcCount; i++)
        {
            GameObject go = Instantiate(npcPrefab, transform);
            go.name = Instantiate(npcVehicles[RandomIndex(npcVehicles.Count)], go.transform).name;
            string genId = System.Guid.NewGuid().ToString();
            go.GetComponent<NPCControllerComponent>().id = genId;
            go.name = go.name + genId;
            currentPooledNPCs.Add(go);
            go.SetActive(false);
        }
    }

    private void InitNPCOnMap()
    {
        foreach (var npc in currentPooledNPCs)
        {
            MapLaneSegmentBuilder seg = MapManager.Instance.GetRandomLane();
            if (Physics.CheckSphere(seg.segment.targetWorldPositions[0], checkRadius, NPCSpawnCheckBitmask))
                continue;

            npc.GetComponent<NPCControllerComponent>().Init(seg);
            npc.transform.position = seg.segment.targetWorldPositions[0];
            npc.SetActive(true);
            npc.transform.LookAt(seg.segment.targetWorldPositions[1]); // TODO check if index 1 is valid
            activeNPCCount++;
        }
    }

    private void SetNPCOnMap()
    {
        foreach (var npc in currentPooledNPCs)
        {
            if (!npc.activeInHierarchy)
            {
                MapLaneSegmentBuilder seg = MapManager.Instance.GetRandomLane();
                if (!Physics.CheckSphere(seg.segment.targetWorldPositions[0], checkRadius, NPCSpawnCheckBitmask))
                {
                    npc.GetComponent<NPCControllerComponent>().Init(seg);
                    npc.transform.position = seg.segment.targetWorldPositions[0] + Vector3.up;
                    npc.SetActive(true);
                    npc.transform.LookAt(seg.segment.targetWorldPositions[1]); // TODO check if index 1 is valid
                    activeNPCCount++;
                }
                break;
            }
        }
    }

    public Transform GetRandomActiveNPC()
    {
        if (currentPooledNPCs.Count == 0) return transform;

        int index = (int)Random.Range(0, currentPooledNPCs.Count);
        while (!currentPooledNPCs[index].activeInHierarchy)
        {
            index = (int)Random.Range(0, currentPooledNPCs.Count);
        }
        return currentPooledNPCs[index].transform;
    }

    public void DespawnNPC(GameObject npc)
    {
        activeNPCCount--;
        npc.SetActive(false);
        npc.transform.position = transform.position;
        npc.transform.rotation = Quaternion.identity;
    }

    //private IEnumerator StartNPCSpawn()
    //{
    //    while (true)
    //    {
    //        yield return new WaitForSeconds(Random.Range(0f, SpawnDelay));

    //        if (currentBridgeNPCs.Count < npcCount)
    //        {
    //            // pick empty lane
    //            int randLane = (int)Random.Range(0, laneData.Count);

    //            // check if lane just spawned npc // if so just skip this spawn
    //            if (randLane != lastRandLane)
    //            {
    //                lastRandLane = randLane;

    //                // spawn npc
    //                Vector3 pos = laneData[randLane].data[0];
    //                Vector3 fwdVec = (laneData[randLane].data[1] - laneData[randLane].data[0]).normalized;
    //                int hitCnt = Physics.OverlapCapsuleNonAlloc(pos - fwdVec * 3f, pos + fwdVec * 3f, 3.5f, collidersBuffer, NPCSpawnCheckBitmask);
    //                if (hitCnt < 1)
    //                {
    //                    GameObject go = Instantiate(npcVehicles[(int)Random.Range(0, npcVehicles.Count)], pos, Quaternion.LookRotation(fwdVec), this.transform);
    //                    go.GetComponent<NPCControllerComponent>().SetLaneData(laneData[randLane].data);
    //                    currentBridgeNPCs.Add(go);
    //                }
    //            }
    //        }
    //    }
    //}
    #endregion

    #region utilities
    private int RandomIndex(int max = 1)
    {
        return (int)Random.Range(0, max);
    }
    #endregion
}