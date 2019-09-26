/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;
using Simulator.Map;
using System.Linq;

public class NPCManager : MonoBehaviour
{
    [System.Serializable]
    public struct NPCS
    {
        public GameObject Prefab;
        public int Weight;
    }
    public List<NPCS> npcVehicles = new List<NPCS>();

    private LayerMask NPCSpawnCheckBitmask;
    private float checkRadius = 6f;
    private Camera activeCamera;

    public bool isDespawnTimer = false;
    public bool isRightSideDriving = true;
    public bool isSpawnAreaVisible = false;
    public bool isSpawnAreaLimited = true;
    public Vector3 spawnArea = Vector3.zero;
    public float despawnDistance = 300f;
    private Bounds spawnBounds = new Bounds();
    private Color spawnColor = Color.magenta;
    private Vector3 spawnPos;
    private Transform spawnT;
    public bool NPCActive { get; set; } = false;

    public enum NPCCountType
    {
        Low = 150,
        Medium = 125,
        High = 50
    };
    public NPCCountType npcCountType = NPCCountType.Low;

    private int npcCount = 0;
    private int activeNPCCount = 0;
    [HideInInspector]
    public List<NPCController> currentPooledNPCs = new List<NPCController>();
    private System.Random RandomGenerator;
    private System.Random NPCSeedGenerator;  // Only use this for initializing a new NPC
    private int Seed = new System.Random().Next();
    private List<NPCController> APINPCs = new List<NPCController>();

    private void Awake()
    {
        if (spawnT == null)
            spawnT = transform;

        if (activeCamera == null)
            activeCamera = Camera.main;
    }

    public void InitRandomGenerator(int seed)
    {
        Seed = seed;
        RandomGenerator = new System.Random(Seed);
        NPCSeedGenerator = new System.Random(Seed);
    }

    private void Start()
    {
        NPCSpawnCheckBitmask = 1 << LayerMask.NameToLayer("NPC") | 1 << LayerMask.NameToLayer("Agent");
        npcCount = Mathf.CeilToInt(SimulatorManager.Instance.MapManager.totalLaneDist / (int)npcCountType);
        if (!SimulatorManager.Instance.IsAPI)
        {
            SpawnNPCPool();
        }
    }

    public void PhysicsUpdate()
    {
        if (SimulatorManager.Instance.IsAPI)
        {
            foreach (var npc in APINPCs)
            {
                if (npc.gameObject.activeInHierarchy)
                {
                    npc.PhysicsUpdate();
                }
            }
        }
        else
        {
            if (NPCActive)
            {
                if (activeNPCCount < npcCount)
                    SetNPCOnMap();
            }
            else
            {
                DespawnAllNPC();
            }

            foreach (var npc in currentPooledNPCs)
            {
                if (npc.gameObject.activeInHierarchy)
                {
                    npc.PhysicsUpdate();
                }
            }
        }
    }

    #region api
    public void DespawnVehicle(NPCController obj)
    {
        if (obj == null)
        {
            return;
        }

        obj.StopNPCCoroutines();
        obj.currentIntersection?.npcsInIntersection.Remove(obj.transform);
        APINPCs.Remove(obj);
        Destroy(obj.gameObject);
    }

    public GameObject SpawnVehicle(string name, Vector3 position, Quaternion rotation)
    {
        var template = npcVehicles.Find(obj => obj.Prefab.name == name);
        if (template.Prefab == null)
        {
            return null;
        }

        var genId = System.Guid.NewGuid().ToString();
        var go = new GameObject("NPC " + genId);
        go.transform.SetParent(transform);
        go.layer = LayerMask.NameToLayer("NPC");
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 2000;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        var npcC = go.AddComponent<NPCController>();
        var npc_name = Instantiate(template.Prefab, go.transform).name;
        go.name = npc_name + genId;
        var NPCController = go.GetComponent<NPCController>();
        NPCController.NPCType = GetNPCType(npc_name);
        APINPCs.Add(NPCController);
        NPCController.id = genId;
        NPCController.GTID = ++SimulatorManager.Instance.GTIDs;
        var s = NPCSeedGenerator.Next();
        NPCController.Init(s);
        SimulatorManager.Instance.UpdateSemanticTags(go);
        go.transform.SetPositionAndRotation(position, rotation); // TODO check for incorrect calc speed
        npcC.SetLastPosRot(position, rotation);

        return go;
    }

    public void Reset()
    {
        RandomGenerator = new System.Random(Seed);
        NPCSeedGenerator = new System.Random(Seed);

        List<NPCController> npcs = new List<NPCController>(APINPCs);
        foreach (var npc in npcs)
        {
            DespawnVehicle(npc);
        }

        APINPCs.Clear();
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

        int poolCount = Mathf.FloorToInt(npcCount + (npcCount * 0.1f));
        for (int i = 0; i < poolCount; i++)
        {
            var genId = System.Guid.NewGuid().ToString();
            var go = new GameObject("NPC " + genId);
            go.SetActive(false);
            go.transform.SetParent(transform);
            go.layer = LayerMask.NameToLayer("NPC");
            go.tag = "Car";
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 2000;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            go.AddComponent<NPCController>();
            var npc_name = Instantiate(GetWeightedRandom(), go.transform).name;
            go.name = npc_name + genId;
            var NPCController = go.GetComponent<NPCController>();
            NPCController.NPCType = GetNPCType(npc_name);
            NPCController.id = genId;
            NPCController.Init(NPCSeedGenerator.Next());
            currentPooledNPCs.Add(NPCController);

            SimulatorManager.Instance.UpdateSemanticTags(go);
        }
    }

    private void SetNPCOnMap()
    {
        for (int i = 0; i < currentPooledNPCs.Count; i++)
        {
            if (currentPooledNPCs[i].gameObject.activeInHierarchy)
            {
                continue;
            }
            var mapManager = SimulatorManager.Instance.MapManager;
            var lane = mapManager.GetLane(RandomGenerator.Next(mapManager.trafficLanes.Count));
            if (lane == null) return;

            if (lane.mapWorldPositions == null || lane.mapWorldPositions.Count == 0)
                continue;

            if (lane.mapWorldPositions.Count < 2)
                continue;

            if (!lane.Spawnable)
                continue;

            var start = lane.mapWorldPositions[0];

            if (isSpawnAreaLimited)
            {
                if (IsPositionWithinSpawnArea(start))
                {
                    if (!Physics.CheckSphere(lane.mapWorldPositions[0], checkRadius, NPCSpawnCheckBitmask))
                    {
                        spawnPos = lane.mapWorldPositions[0];
                        currentPooledNPCs[i].transform.position = spawnPos;
                        if (!IsVisible(currentPooledNPCs[i].gameObject))
                        {
                            currentPooledNPCs[i].transform.LookAt(lane.mapWorldPositions[1]); // TODO check if index 1 is valid
                            currentPooledNPCs[i].InitLaneData(lane);
                            currentPooledNPCs[i].GTID = ++SimulatorManager.Instance.GTIDs;
                            currentPooledNPCs[i].gameObject.SetActive(true);
                            currentPooledNPCs[i].enabled = true;
                            activeNPCCount++;
                        }
                        else
                        {
                            currentPooledNPCs[i].gameObject.SetActive(false);
                            currentPooledNPCs[i].enabled = false;
                            currentPooledNPCs[i].transform.position = transform.position;
                            currentPooledNPCs[i].transform.rotation = Quaternion.identity;
                        }
                    }
                }
            }
            else
            {
                if (!Physics.CheckSphere(lane.mapWorldPositions[0], checkRadius, NPCSpawnCheckBitmask))
                {
                    spawnPos = lane.mapWorldPositions[0];
                    currentPooledNPCs[i].transform.position = spawnPos;
                    currentPooledNPCs[i].transform.LookAt(lane.mapWorldPositions[1]); // TODO check if index 1 is valid
                    currentPooledNPCs[i].InitLaneData(lane);
                    currentPooledNPCs[i].GTID = ++SimulatorManager.Instance.GTIDs;
                    currentPooledNPCs[i].gameObject.SetActive(true);
                    currentPooledNPCs[i].enabled = true;
                    activeNPCCount++;
                }
            }
        }
    }

    public Transform GetRandomActiveNPC()
    {
        if (currentPooledNPCs.Count == 0) return transform;

        int index = RandomGenerator.Next(currentPooledNPCs.Count);
        while (!currentPooledNPCs[index].gameObject.activeInHierarchy)
        {
            index = RandomGenerator.Next(currentPooledNPCs.Count);
        }
        return currentPooledNPCs[index].transform;
    }

    public void DespawnNPC(GameObject npc)
    {
        npc.SetActive(false);
        activeNPCCount--;
        npc.transform.position = transform.position;
        npc.transform.rotation = Quaternion.identity;
        var npcC = npc.GetComponent<NPCController>();
        if (npcC)
        {
            npcC.StopNPCCoroutines();
            npcC.enabled = false;
        }
    }

    public void DespawnAllNPC()
    {
        if (activeNPCCount == 0) return;

        for (int i = 0; i < currentPooledNPCs.Count; i++)
        {
            DespawnNPC(currentPooledNPCs[i].gameObject);
        }
        foreach (var item in FindObjectsOfType<MapIntersection>())
        {
            item.stopQueue.Clear();
        }

        activeNPCCount = 0;
    }

    private string GetNPCType(string npc_name)
    {
        var npc_type = npc_name;
        var end_index = npc_name.IndexOf("(");
        if (end_index != -1)
        {
            npc_type = npc_name.Substring(0, end_index);
        }

        return npc_type;
    }
    #endregion

    #region utilities
    private int RandomIndex(int max = 1)
    {
        return RandomGenerator.Next(max);
    }

    private GameObject GetWeightedRandom()
    {
        int totalWeight = npcVehicles.Sum(npcs => npcs.Weight);
        int rnd = RandomGenerator.Next(totalWeight);

        GameObject npcPrefab = npcVehicles[0].Prefab;
        for (int i = 0; i < npcVehicles.Count; i++)
        {
            if (rnd < npcVehicles[i].Weight)
            {
                npcPrefab = npcVehicles[i].Prefab;
                break;
            }
            rnd -= npcVehicles[i].Weight;
        }

        return npcPrefab;
    }

    public bool IsPositionWithinSpawnArea(Vector3 pos)
    {
        Transform tempT = SimulatorManager.Instance.AgentManager.CurrentActiveAgent?.transform;
        if (tempT != null)
            spawnT = tempT;

        spawnBounds = new Bounds(spawnT.position, spawnArea);
        if (spawnBounds.Contains(pos))
            return true;
        else
            return false;
    }

    public bool IsVisible(GameObject npc)
    {
        Camera tempCam = Camera.main;
        if (tempCam != null)
            activeCamera = tempCam;
        var npcColliderBounds = npc.GetComponent<Collider>().bounds;
        var activeCameraPlanes = GeometryUtility.CalculateFrustumPlanes(activeCamera);
        return GeometryUtility.TestPlanesAABB(activeCameraPlanes, npcColliderBounds);
    }

    private void DrawSpawnArea()
    {
        Transform tempT = SimulatorManager.Instance.AgentManager.CurrentActiveAgent?.transform;
        if (tempT != null)
            spawnT = tempT;
        Gizmos.matrix = spawnT.localToWorldMatrix;
        Gizmos.color = spawnColor;
        Gizmos.DrawWireCube(Vector3.zero, spawnArea);
    }

    private void OnDrawGizmosSelected()
    {
        if (!isSpawnAreaVisible) return;
        DrawSpawnArea();
    }
    #endregion
}
