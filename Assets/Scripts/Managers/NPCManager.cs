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
        public NPCSizeType NPCType;
    }
    public List<NPCS> NPCVehicles = new List<NPCS>();

    [System.Serializable]
    public struct NPCColors
    {
        public NPCSizeType Type;
        public List<NPCTypeColors> TypeColors;
    }
    [System.Serializable]
    public struct NPCTypeColors
    {
        public Color Color;
        public int Weight;
    }
    // loosely based on ppg 2017 trends https://news.ppg.com/automotive-color-trends/
    public List<NPCColors> NPCColorData = new List<NPCColors>();

    private MapOrigin MapOrigin;
    private bool InitSpawn = true;

    public bool NPCActive { get; set; } = false;
    [HideInInspector]
    public List<NPCController> CurrentPooledNPCs = new List<NPCController>();
    private LayerMask NPCSpawnCheckBitmask;
    private Vector3 SpawnBoundsSize;
    private bool DebugSpawnArea = false;
    private int NPCMaxCount = 0;
    private int ActiveNPCCount = 0;
    private System.Random RandomGenerator;
    private System.Random NPCSeedGenerator;  // Only use this for initializing a new NPC
    private int Seed = new System.Random().Next();
    private List<NPCController> APINPCs = new List<NPCController>();

    private Camera SimulatorCamera;

    public void InitRandomGenerator(int seed)
    {
        Seed = seed;
        RandomGenerator = new System.Random(Seed);
        NPCSeedGenerator = new System.Random(Seed);
    }

    private void Start()
    {
        MapOrigin = MapOrigin.Find();
        NPCSpawnCheckBitmask = LayerMask.GetMask("NPC", "Agent");
        SpawnBoundsSize = new Vector3(MapOrigin.NPCSpawnBoundSize, 50f, MapOrigin.NPCSpawnBoundSize);
        NPCMaxCount = MapOrigin.NPCMaxCount;
        SimulatorCamera = SimulatorManager.Instance.CameraManager.SimulatorCamera;

        if (!SimulatorManager.Instance.IsAPI)
        {
            SpawnNPCPool();
            if (NPCActive)
                SetNPCOnMap();
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
                if (ActiveNPCCount < NPCMaxCount)
                    SetNPCOnMap();
            }
            else
            {
                DespawnAllNPC();
            }

            foreach (var npc in CurrentPooledNPCs)
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
        var template = NPCVehicles.Find(obj => obj.Prefab.name == name);
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
        NPCController.NPCLabel = GetNPCLabel(npc_name);
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
        for (int i = 0; i < CurrentPooledNPCs.Count; i++)
        {
            Destroy(CurrentPooledNPCs[i]);
        }
        CurrentPooledNPCs.Clear();
        ActiveNPCCount = 0;

        int poolCount = Mathf.FloorToInt(NPCMaxCount + (NPCMaxCount * 0.1f));
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
            var npcData = GetWeightedRandomNPC();
            var npc_name = Instantiate(npcData.Prefab, go.transform).name;
            go.name = npc_name + genId;
            var NPCController = go.GetComponent<NPCController>();
            NPCController.Size = npcData.NPCType;
            NPCController.NPCColor = GetWeightedRandomColor(npcData.NPCType);
            NPCController.NPCLabel = GetNPCLabel(npc_name);
            NPCController.id = genId;
            NPCController.Init(NPCSeedGenerator.Next());
            CurrentPooledNPCs.Add(NPCController);

            SimulatorManager.Instance.UpdateSemanticTags(go);
        }
    }

    private void SetNPCOnMap()
    {
        for (int i = 0; i < CurrentPooledNPCs.Count; i++)
        {
            if (CurrentPooledNPCs[i].gameObject.activeInHierarchy)
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
            CurrentPooledNPCs[i].transform.position = spawnPos;

            if (!WithinSpawnArea(spawnPos))
            {
                continue;
            }

            if (!InitSpawn)
            {
                if (IsVisible(CurrentPooledNPCs[i].gameObject))
                {
                    continue;
                }
            }

            if (Physics.CheckSphere(spawnPos, 6f, NPCSpawnCheckBitmask))
            {
                continue;
            }

            CurrentPooledNPCs[i].transform.LookAt(lane.mapWorldPositions[1]);
            CurrentPooledNPCs[i].InitLaneData(lane);
            CurrentPooledNPCs[i].GTID = ++SimulatorManager.Instance.GTIDs;
            CurrentPooledNPCs[i].gameObject.SetActive(true);
            CurrentPooledNPCs[i].enabled = true;
            ActiveNPCCount++;
        }
        InitSpawn = false;
    }

    public Transform GetRandomActiveNPC()
    {
        if (CurrentPooledNPCs.Count == 0) return transform;

        int index = RandomGenerator.Next(CurrentPooledNPCs.Count);
        while (!CurrentPooledNPCs[index].gameObject.activeInHierarchy)
        {
            index = RandomGenerator.Next(CurrentPooledNPCs.Count);
        }
        return CurrentPooledNPCs[index].transform;
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

        for (int i = 0; i < CurrentPooledNPCs.Count; i++)
        {
            DespawnNPC(CurrentPooledNPCs[i].gameObject);
        }
        foreach (var item in FindObjectsOfType<MapIntersection>())
        {
            item.stopQueue.Clear();
        }

        ActiveNPCCount = 0;
    }

    private string GetNPCLabel(string npc_name)
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

    private NPCS GetWeightedRandomNPC()
    {
        int totalWeight = NPCVehicles.Where(npcs => HasSizeFlag(npcs.NPCType)).Sum(npcs => npcs.Weight);
        int rnd = RandomGenerator.Next(totalWeight);

        for (int i = 0; i < NPCVehicles.Count; i++)
        {
            if (HasSizeFlag(NPCVehicles[i].NPCType))
            {
                if (rnd < NPCVehicles[i].Weight)
                {
                    return NPCVehicles[i];
                }
                rnd -= NPCVehicles[i].Weight;
            }
        }

        throw new System.Exception("NPC size weights are incorrectly set!");
    }

    private Color GetWeightedRandomColor(NPCSizeType type)
    {
        var colors = NPCColorData.Find(colorData => colorData.Type == type).TypeColors;
        int totalWeight = colors.Sum(c => c.Weight);
        int rnd = RandomGenerator.Next(totalWeight);

        for (int i = 0; i < colors.Count; i++)
        {
            if (rnd < colors[i].Weight)
            {
                return colors[i].Color;
            }
            rnd -= colors[i].Weight;
        }

        throw new System.Exception("NPC color weights are incorrectly set!");
    }

    private bool HasSizeFlag(NPCSizeType sizeType)
    {
        if ((MapOrigin.NPCSizeMask & (int)sizeType) != 0)
        {
            return true;
        }
        return false;
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
        bool visible = false;
        var activeAgentController = SimulatorManager.Instance.AgentManager.CurrentActiveAgentController;
        var npcColliderBounds = npc.GetComponent<NPCController>().MainCollider.bounds;

        var activeCameraPlanes = GeometryUtility.CalculateFrustumPlanes(SimulatorCamera);
        visible = GeometryUtility.TestPlanesAABB(activeCameraPlanes, npcColliderBounds);

        foreach (var sensor in activeAgentController.AgentSensors)
        {
            visible = sensor.CheckVisible(npcColliderBounds);
            if (visible) break;
        }

        return visible;
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
