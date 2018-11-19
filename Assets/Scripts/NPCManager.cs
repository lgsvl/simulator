/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LaneData
{
    public List<Vector3> data = new List<Vector3>();
}

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
    private Collider[] collidersBuffer = new Collider[1];
    #endregion

    #region vars
    // npc
    public List<GameObject> npcVehicles = new List<GameObject>();
    public int npcCount = 0;
    private int lastRandLane = -1;
    private List<GameObject> currentBridgeNPCs = new List<GameObject>();
    public int NPCCount
    {
        get
        {
            return currentBridgeNPCs.Count;
        }
    }

    private float spawnDelay = 2.5f;
    public float SpawnDelay
    {
        get
        {
            return spawnDelay;
        }

        set
        {
            spawnDelay = value;
        }
    }

    // lane data
    public Transform laneDataHolder;
    [HideInInspector]
    public List<MapLaneSegmentBuilder> mapLaneSegData;
    [HideInInspector]
    public List<LaneData> laneData = new List<LaneData>();
    #endregion

    #region mono
    private void Awake()
    {
        if (_instance == null)
            _instance = this;

        if (_instance != this)
            DestroyImmediate(gameObject);

        PopulateData();
    }

    private void Start()
    {
        NPCSpawnCheckBitmask = 1 << LayerMask.NameToLayer("NPC");
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }
    #endregion

    #region data
    public void ClearData()
    {
        mapLaneSegData.Clear();
        laneData.Clear();
    }

    public void PopulateData()
    {
        laneData.Clear();
        mapLaneSegData.Clear();
        foreach (Transform child in laneDataHolder)
        {
            if (child == null) return;
            mapLaneSegData.AddRange(child.GetComponentsInChildren<MapLaneSegmentBuilder>());
        }

        foreach (var mapLaneData in mapLaneSegData)
        {
            LaneData tempData = new LaneData();
            foreach (var localPos in mapLaneData.segment.targetLocalPositions)
            {
                tempData.data.Add(mapLaneData.transform.TransformPoint(localPos));
            }
            laneData.Add(tempData);
        }
    }
    #endregion

    #region npc
    public void SpawnBridgeNPCs()
    {
        StartCoroutine(StartNPCSpawn());
    }

    public void DespawnBridgeNPC(GameObject npc)
    {
        currentBridgeNPCs.Remove(npc);
        Destroy(npc);
    }

    public void ResetBridgeNPC()
    {
        StopAllCoroutines();
        lastRandLane = -1;
        for (int i = 0; i < currentBridgeNPCs.Count; i++)
        {
            if (currentBridgeNPCs[i] != null)
                Destroy(currentBridgeNPCs[i]);
        }
        currentBridgeNPCs.Clear();
    }

    private IEnumerator StartNPCSpawn()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(0f, spawnDelay));

            if (currentBridgeNPCs.Count < npcCount)
            {
                // pick empty lane
                int randLane = (int)Random.Range(0, laneData.Count);

                // check if lane just spawned npc // if so just skip this spawn
                if (randLane != lastRandLane)
                {
                    lastRandLane = randLane;

                    // spawn npc
                    Vector3 pos = laneData[randLane].data[0];
                    Vector3 fwdVec = (laneData[randLane].data[1] - laneData[randLane].data[0]).normalized;
                    int hitCnt = Physics.OverlapCapsuleNonAlloc(pos - fwdVec * 3f, pos + fwdVec * 3f, 3.5f, collidersBuffer, NPCSpawnCheckBitmask);
                    if (hitCnt < 1)
                    {
                        GameObject go = Instantiate(npcVehicles[(int)Random.Range(0, npcVehicles.Count)], pos, Quaternion.LookRotation(fwdVec), this.transform);
                        go.GetComponent<NPCControllerComponent>().SetLaneData(laneData[randLane].data);
                        currentBridgeNPCs.Add(go);
                    }
                }
            }
        }
    }
    #endregion
}