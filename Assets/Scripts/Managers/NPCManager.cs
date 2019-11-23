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

    public enum NPCCountType
    {
        Low = 150,
        Medium = 125,
        High = 50
    };
    public NPCCountType npcCountType = NPCCountType.Low;

    public bool NPCActive { get; set; } = false;
    [HideInInspector]
    public List<NPCController> currentPooledNPCs = new List<NPCController>();
    private LayerMask NPCSpawnCheckBitmask;
    private Vector3 SpawnBoundsSize = new Vector3(500f, 50f, 500f);
    private bool DebugSpawnArea = false;
    private int NPCCount = 0;
    private int ActiveNPCCount = 0;
    private System.Random RandomGenerator;
    private System.Random NPCSeedGenerator;  // Only use this for initializing a new NPC
    private int Seed = new System.Random().Next();
    private List<NPCController> APINPCs = new List<NPCController>();

    public void InitRandomGenerator(int seed)
    {
        Seed = seed;
        RandomGenerator = new System.Random(Seed);
        NPCSeedGenerator = new System.Random(Seed);
    }

    private void Start()
    {
        NPCSpawnCheckBitmask = LayerMask.GetMask("NPC", "Agent");
        NPCCount = Mathf.CeilToInt(SimulatorManager.Instance.MapManager.totalLaneDist / (int)npcCountType);
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
                if (ActiveNPCCount < NPCCount)
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
        go.tag = "Car";
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
    public void ToggleNPC()
    {
        NPCActive = !NPCActive;
    }

    private void SpawnNPCPool()
    {
        for (int i = 0; i < currentPooledNPCs.Count; i++)
        {
            Destroy(currentPooledNPCs[i]);
        }
        currentPooledNPCs.Clear();
        ActiveNPCCount = 0;

        int poolCount = Mathf.FloorToInt(NPCCount + (NPCCount * 0.1f));
        poolCount = Mathf.Clamp(poolCount, 1, 100);
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

            var spawnPos = lane.mapWorldPositions[0];
            currentPooledNPCs[i].transform.position = spawnPos;

            if (!WithinSpawnArea(spawnPos))
            {
                continue;
            }

            if (IsVisible(currentPooledNPCs[i].gameObject))
            {
                continue;
            }

            if (Physics.CheckSphere(spawnPos, 6f, NPCSpawnCheckBitmask))
            {
                continue;
            }

            currentPooledNPCs[i].transform.LookAt(lane.mapWorldPositions[1]);
            currentPooledNPCs[i].InitLaneData(lane);
            currentPooledNPCs[i].GTID = ++SimulatorManager.Instance.GTIDs;
            currentPooledNPCs[i].gameObject.SetActive(true);
            currentPooledNPCs[i].enabled = true;
            ActiveNPCCount++;
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
        ActiveNPCCount--;
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
        if (ActiveNPCCount == 0) return;

        for (int i = 0; i < currentPooledNPCs.Count; i++)
        {
            DespawnNPC(currentPooledNPCs[i].gameObject);
        }
        foreach (var item in FindObjectsOfType<MapIntersection>())
        {
            item.stopQueue.Clear();
        }

        ActiveNPCCount = 0;
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

    public bool WithinSpawnArea(Vector3 pos)
    {
        var spawnT = SimulatorManager.Instance.AgentManager.CurrentActiveAgent?.transform;
        spawnT = spawnT ?? transform;
        var spawnBounds = new Bounds(spawnT.position, SpawnBoundsSize);
        return spawnBounds.Contains(pos);
    }

    public bool IsVisible(GameObject npc)
    {
        var activeCamera = SimulatorManager.Instance.CameraManager.SimulatorCamera;
        var npcColliderBounds = npc.GetComponent<NPCController>().MainCollider.bounds;
        var activeCameraPlanes = GeometryUtility.CalculateFrustumPlanes(activeCamera);
        return GeometryUtility.TestPlanesAABB(activeCameraPlanes, npcColliderBounds);
    }

    private void DrawSpawnArea()
    {
        var spawnT = SimulatorManager.Instance.AgentManager.CurrentActiveAgent?.transform;
        spawnT = spawnT ?? transform;
        Gizmos.matrix = spawnT.localToWorldMatrix;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, SpawnBoundsSize);
    }

    private void OnDrawGizmosSelected()
    {
        if (!DebugSpawnArea) return;
        DrawSpawnArea();
    }
    #endregion
}
