/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;
using Simulator.Map;
using Simulator.Utilities;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class PedestrianManager : MonoBehaviour
{
    public GameObject pedPrefab;
    public List<GameObject> pedModels = new List<GameObject>();
    public bool PedestriansActive { get; set; } = false;

    private List<PedestrianController> currentPedPool = new List<PedestrianController>();
    private Vector3 SpawnBoundsSize;
    private bool DebugSpawnArea = false;
    private LayerMask PedSpawnCheckBitmask;

    private int PedMaxCount = 0;
    private int ActivePedCount = 0;

    private System.Random RandomGenerator;
    private System.Random PEDSeedGenerator;  // Only use this for initializing a new pedestrian
    private int Seed = new System.Random().Next();

    private MapOrigin MapOrigin;
    private bool InitSpawn = true;

    private Camera SimulatorCamera;

    public void InitRandomGenerator(int seed)
    {
        Seed = seed;
        RandomGenerator = new System.Random(Seed);
        PEDSeedGenerator = new System.Random(Seed);
    }

    private void Start()
    {
        MapOrigin = MapOrigin.Find();
        PedSpawnCheckBitmask = LayerMask.GetMask("Pedestrian", "Agent", "NPC");
        SpawnBoundsSize = new Vector3(MapOrigin.PedSpawnBoundSize, 50f, MapOrigin.PedSpawnBoundSize);
        PedMaxCount = MapOrigin.PedMaxCount;
        SimulatorCamera = SimulatorManager.Instance.CameraManager.SimulatorCamera;

        SpawnInfo[] spawnInfos = FindObjectsOfType<SpawnInfo>();
        var pt = Vector3.zero;
        if (spawnInfos.Length > 0)
        {
            pt = spawnInfos[0].transform.position;
        }
        NavMeshHit hit;
        if (NavMesh.SamplePosition(pt, out hit, 1f, NavMesh.AllAreas))
        {
            InitPedestrians();
            if (PedestriansActive)
                SetPedOnMap();
        }
        else
        {
            var sceneName = SceneManager.GetActiveScene().name;
            Debug.LogError($"{sceneName} is missing Pedestrian NavMesh");
            gameObject.SetActive(false);
        }
    }

    public void PhysicsUpdate()
    {
        foreach (var ped in currentPedPool)
        {
            if (ped.gameObject.activeInHierarchy)
            {
                ped.PhysicsUpdate();
            }
        }
        if (!SimulatorManager.Instance.IsAPI)
        {
            if (PedestriansActive)
            {
                if (ActivePedCount < PedMaxCount)
                    SetPedOnMap();
            }
            else
            {
                DespawnAllPeds();
            }
        }
    }

    private void InitPedestrians()
    {
        Debug.Assert(pedPrefab != null && pedModels != null && pedModels.Count != 0);

        currentPedPool.Clear();
        int poolCount = Mathf.FloorToInt(PedMaxCount + (PedMaxCount * 0.1f));
        for (int i = 0; i < poolCount; i++)
        {
            SpawnPedestrian();
        }
    }

    private void SetPedOnMap()
    {
        var mapManager = SimulatorManager.Instance.MapManager;
        for (int i = 0; i < currentPedPool.Count; i++)
        {
            if (currentPedPool[i].gameObject.activeInHierarchy)
                continue;

            var path = mapManager.GetPedPath(RandomIndex(mapManager.pedestrianLanes.Count));
            if (path == null) continue;

            if (path.mapWorldPositions == null || path.mapWorldPositions.Count == 0)
                continue;

            if (path.mapWorldPositions.Count < 2)
                continue;

            var spawnPos = path.mapWorldPositions[RandomIndex(path.mapWorldPositions.Count)];
            currentPedPool[i].transform.position = spawnPos;

            if (!WithinSpawnArea(spawnPos))
                continue;

            if (!InitSpawn)
            {
                if (IsVisible(currentPedPool[i].gameObject))
                    continue;
            }

            if (Physics.CheckSphere(spawnPos, 3f, PedSpawnCheckBitmask))
                continue;

            currentPedPool[i].InitPed(spawnPos, path.mapWorldPositions, PEDSeedGenerator.Next());
            currentPedPool[i].GTID = ++SimulatorManager.Instance.GTIDs;
            currentPedPool[i].gameObject.SetActive(true);
            ActivePedCount++;
        }
        InitSpawn = false;
    }

    private GameObject SpawnPedestrian()
    {
        GameObject ped = Instantiate(pedPrefab, Vector3.zero, Quaternion.identity, transform);
        var pedController = ped.GetComponent<PedestrianController>();
        pedController.SetGroundTruthBox();
        Instantiate(pedModels[RandomGenerator.Next(pedModels.Count)], ped.transform);
        ped.SetActive(false);
        SimulatorManager.Instance.UpdateSemanticTags(ped);
        currentPedPool.Add(pedController);
        return ped;
    }

    public void DespawnPed(PedestrianController ped)
    {
        ped.gameObject.SetActive(false);
        ActivePedCount--;
        ped.transform.position = transform.position;
        ped.transform.rotation = Quaternion.identity;
    }

    public void DespawnAllPeds()
    {
        if (ActivePedCount == 0) return;

        for (int i = 0; i < currentPedPool.Count; i++)
        {
            DespawnPed(currentPedPool[i]);
        }
        ActivePedCount = 0;
    }

    #region api
    public GameObject SpawnPedestrianApi(string name, Vector3 position, Quaternion rotation)
    {
        var prefab = pedModels.Find(obj => obj.name == name);
        if (prefab == null)
        {
            return null;
        }

        GameObject ped = Instantiate(pedPrefab, Vector3.zero, Quaternion.identity, transform);
        var pedC = ped.GetComponent<PedestrianController>();
        Instantiate(prefab, ped.transform);
        SimulatorManager.Instance.UpdateSemanticTags(ped);
        currentPedPool.Add(pedC);

        pedC.InitManual(position, rotation, PEDSeedGenerator.Next());
        pedC.GTID = ++SimulatorManager.Instance.GTIDs;
        pedC.SetGroundTruthBox();

        return ped;
    }

    public void DespawnPedestrianApi(PedestrianController ped)
    {
        ped.StopPEDCoroutines();
        currentPedPool.Remove(ped);
        Destroy(ped.gameObject);
    }

    public void Reset()
    {
        RandomGenerator = new System.Random(Seed);
        PEDSeedGenerator = new System.Random(Seed);

        List<PedestrianController> peds = new List<PedestrianController>(currentPedPool);
        peds.ForEach(x => DespawnPedestrianApi(x));
        currentPedPool.Clear();
    }
    #endregion

    #region utilities
    private int RandomIndex(int max = 1)
    {
        return RandomGenerator.Next(max);
    }

    public bool WithinSpawnArea(Vector3 pos)
    {
        var spawnT = SimulatorManager.Instance.AgentManager.CurrentActiveAgent?.transform;
        spawnT = spawnT ?? transform;
        var spawnBounds = new Bounds(spawnT.position, SpawnBoundsSize);
        return spawnBounds.Contains(pos);
    }

    public bool IsVisible(GameObject ped)
    {
        bool visible = false;
        var activeAgentController = SimulatorManager.Instance.AgentManager.CurrentActiveAgentController;
        var pedColliderBounds = ped.GetComponent<Collider>().bounds;

        var activeCameraPlanes = GeometryUtility.CalculateFrustumPlanes(SimulatorCamera);
        visible = GeometryUtility.TestPlanesAABB(activeCameraPlanes, pedColliderBounds);

        foreach (var sensor in activeAgentController.AgentSensors)
        {
            visible = sensor.CheckVisible(pedColliderBounds);
            if (visible) break;
        }

        return visible;
    }

    private void DrawSpawnArea()
    {
        if (SimulatorManager.Instance == null) // prefab editor issue
            return;

        var spawnT = SimulatorManager.Instance.AgentManager.CurrentActiveAgent?.transform;
        spawnT = spawnT ?? transform;
        Gizmos.matrix = spawnT.localToWorldMatrix;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, SpawnBoundsSize);
    }

    private void OnDrawGizmosSelected()
    {
        if (!DebugSpawnArea) return;
        DrawSpawnArea();
    }
    #endregion
}
