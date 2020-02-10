/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using System.Net;
using Simulator;
using Simulator.Map;
using UnityEngine;
using Simulator.Network.Core;
using Simulator.Network.Core.Components;
using Simulator.Network.Core.Connection;
using Simulator.Network.Core.Messaging;
using Simulator.Network.Core.Messaging.Data;
using Simulator.Network.Shared.Messages;
using Simulator.Utilities;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class PedestrianManager : MonoBehaviour, IMessageSender, IMessageReceiver
{
    public GameObject pedPrefab;
    public List<GameObject> pedModels = new List<GameObject>();
    public bool PedestriansActive { get; set; } = false;

    private List<PedestrianController> currentPedPool = new List<PedestrianController>();
    private Vector3 SpawnBoundsSize;
    private bool DebugSpawnArea = false;
    private LayerMask PedSpawnCheckBitmask;
    public string Key => "PedestrianManager"; //Network IMessageSender key

    private int PedMaxCount = 0;
    private int ActivePedCount = 0;

    private System.Random RandomGenerator;
    private System.Random PEDSeedGenerator; // Only use this for initializing a new pedestrian
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
        Loader.Instance.Network.MessagesManager?.RegisterObject(this);
        var pt = Vector3.zero;
        if (spawnInfos.Length > 0)
        {
            pt = spawnInfos[0].transform.position;
        }
        if (Loader.Instance.Network.IsClient)
            return;

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

    private void OnDestroy()
    {
        Loader.Instance.Network.MessagesManager?.UnregisterObject(this);
    }

    public void PhysicsUpdate()
    {
        if (Loader.Instance.Network.IsClient)
            return;
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
        //Ensure sure all pedestrians get unique names
        HierarchyUtility.ChangeToUniqueName(ped);
        var pedController = ped.GetComponent<PedestrianController>();
        pedController.SetGroundTruthBox();
        var modelIndex = RandomGenerator.Next(pedModels.Count);
        var model = pedModels[modelIndex];
        Instantiate(model, ped.transform);
        ped.SetActive(false);
        SimulatorManager.Instance.UpdateSemanticTags(ped);
        currentPedPool.Add(pedController);

        //Add required components for distributing rigidbody from master to clients
        if (Loader.Instance.Network.IsMaster)
        {
            if (ped.GetComponent<DistributedObject>() == null)
                ped.AddComponent<DistributedObject>();
            if (ped.GetComponent<DistributedRigidbody>() == null)
                ped.AddComponent<DistributedRigidbody>();
            BroadcastMessage(new Message(Key,
                GetSpawnMessage(ped.name, modelIndex, ped.transform.position, ped.transform.rotation),
                MessageType.ReliableUnordered));
        }

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
    public GameObject SpawnPedestrianApi(string name, string uid, Vector3 position, Quaternion rotation)
    {
        var prefab = pedModels.Find(obj => obj.name == name);
        if (prefab == null)
        {
            return null;
        }

        GameObject ped = Instantiate(pedPrefab, Vector3.zero, Quaternion.identity, transform);
        ped.name = $"{ped.name}{uid}";
        var pedC = ped.GetComponent<PedestrianController>();
        var rb = ped.GetComponent<Rigidbody>();
        Instantiate(prefab, ped.transform);
        SimulatorManager.Instance.UpdateSemanticTags(ped);
        currentPedPool.Add(pedC);

        pedC.InitManual(position, rotation, PEDSeedGenerator.Next());
        pedC.GTID = ++SimulatorManager.Instance.GTIDs;
        pedC.SetGroundTruthBox();
        
        if (ped.GetComponent<DistributedObject>() == null)
            ped.AddComponent<DistributedObject>();
        if (rb !=null && rb.gameObject.GetComponent<DistributedRigidbody>() == null)
            rb.gameObject.AddComponent<DistributedRigidbody>();

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

    #region network

    private BytesStack GetSpawnMessage(string pedestrianName, int modelIndex, Vector3 position, Quaternion rotation)
    {
        var bytesStack = new BytesStack();
        bytesStack.PushCompressedRotation(rotation);
        bytesStack.PushCompressedPosition(position);
        bytesStack.PushInt(modelIndex, 2);
        bytesStack.PushString(pedestrianName);
        bytesStack.PushEnum<PedestrianManagerCommandType>((int) PedestrianManagerCommandType.SpawnPedestrian);
        return bytesStack;
    }

    private void SpawnPedestrianMock(string pedestrianName, int modelIndex, Vector3 position, Quaternion rotation)
    {
        GameObject ped = Instantiate(pedPrefab, position, rotation, transform);
        ped.name = pedestrianName;
        ped.SetActive(false);
        if (ped.GetComponent<DistributedObject>() == null)
            ped.AddComponent<DistributedObject>().Initialize();
        if (ped.GetComponent<DistributedRigidbody>() == null)
            ped.AddComponent<DistributedRigidbody>();
        var pedController = ped.GetComponent<PedestrianController>();
        pedController.SetGroundTruthBox();
        var model = pedModels[modelIndex];
        Instantiate(model, ped.transform);
        pedController.Control = PedestrianController.ControlType.Manual;
        pedController.enabled = false;
        currentPedPool.Add(pedController);
    }

    private BytesStack GetDespawnMessage(int orderNumber)
    {
        var bytesStack = new BytesStack();
        bytesStack.PushInt(orderNumber, 2);
        bytesStack.PushEnum<PedestrianManagerCommandType>((int) PedestrianManagerCommandType.DespawnPedestrian);
        return bytesStack;
    }

    public void ReceiveMessage(IPeerManager sender, Message message)
    {
        var commandType = message.Content.PopEnum<PedestrianManagerCommandType>();
        switch (commandType)
        {
            case PedestrianManagerCommandType.SpawnPedestrian:
                var pedestrianName = message.Content.PopString();
                var modelIndex = message.Content.PopInt(2);
                SpawnPedestrianMock(pedestrianName, modelIndex, message.Content.PopDecompressedPosition(),
                    message.Content.PopDecompressedRotation());
                break;
            case PedestrianManagerCommandType.DespawnPedestrian:
                var pedestrianId = message.Content.PopInt(2);
                //TODO Preserve despawn command if it arrives before spawn command
                if (pedestrianId >= 0 && pedestrianId < currentPedPool.Count)
                    Destroy(currentPedPool[pedestrianId].gameObject);
                break;
        }
    }

    public void UnicastMessage(IPEndPoint endPoint, Message message)
    {
        Loader.Instance.Network.MessagesManager?.UnicastMessage(endPoint, message);
    }

    public void BroadcastMessage(Message message)
    {
        Loader.Instance.Network.MessagesManager?.BroadcastMessage(message);
    }

    void IMessageSender.UnicastInitialMessages(IPEndPoint endPoint)
    {
        //TODO support reconnection - send instantiation messages to the peer
    }

    #endregion
}