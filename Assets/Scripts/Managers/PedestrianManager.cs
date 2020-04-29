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
using Simulator.Network.Shared;
using Simulator.Network.Shared.Messages;
using Simulator.Utilities;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class PedestrianManager : MonoBehaviour, IMessageSender, IMessageReceiver
{
    public struct PedSpawnData
    {
        public bool Active;
        public bool API;
        public string GenId;
        public int ModelIndex;
        public GameObject Model;
        public Vector3 Position;
        public Quaternion Rotation;
        public int Seed;
    };

    public GameObject pedPrefab;
    public List<GameObject> pedModels = new List<GameObject>();
    public bool PedestriansActive { get; set; } = false;
    [HideInInspector]
    public List<PedestrianController> CurrentPooledPeds = new List<PedestrianController>();
    private Vector3 SpawnBoundsSize;
    private bool DebugSpawnArea = false;
    private LayerMask PedSpawnCheckBitmask;
    public string Key => "PedestrianManager"; //Network IMessageSender key

    private int PedMaxCount = 0;
    private int ActivePedCount = 0;

    private System.Random RandomGenerator;
    public System.Random PEDSeedGenerator { get; private set; } // Only use this for initializing a new pedestrian
    private int Seed = new System.Random().Next();

    private MapOrigin MapOrigin;
    private bool InitSpawn = true;

    private Camera SimulatorCamera;
    private MapManager MapManager;

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
        MapManager = SimulatorManager.Instance.MapManager;

        SpawnInfo[] spawnInfos = FindObjectsOfType<SpawnInfo>();
        Loader.Instance.Network.MessagesManager?.RegisterObject(this);
        var pt = Vector3.zero;
        if (spawnInfos.Length > 0)
        {
            pt = spawnInfos[0].transform.position;
        }

        if (Loader.Instance.Network.IsClient)
        {
            return;
        }

        if (NavMesh.SamplePosition(pt, out NavMeshHit hit, 1f, NavMesh.AllAreas))
        {
            if (!SimulatorManager.Instance.IsAPI && !Loader.Instance.Network.IsClient)
            {
                SpawnPedPool();
                if (PedestriansActive)
                    SetPedOnMap();
            }
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
        {
            return;
        }

        if (PedestriansActive)
        {
            if (ActivePedCount < PedMaxCount)
            {
                SetPedOnMap();
            }
        }
        else
        {
            DespawnAllPeds();
        }

        foreach (var ped in CurrentPooledPeds)
        {
            if (ped.gameObject.activeInHierarchy)
            {
                ped.PhysicsUpdate();
            }
        }
    }

    public List<PedestrianController> SpawnPedPool()
    {
        Debug.Assert(pedPrefab != null && pedModels != null && pedModels.Count != 0);

        for (int i = 0; i < CurrentPooledPeds.Count; i++)
        {
            Destroy(CurrentPooledPeds[i]);
        }
        CurrentPooledPeds.Clear();
        ActivePedCount = 0;

        var pooledPeds = new List<PedestrianController>();
        
        int poolCount = Mathf.FloorToInt(PedMaxCount + (PedMaxCount * 0.1f));
        for (int i = 0; i < poolCount; i++)
        {
            var modelIndex = RandomGenerator.Next(pedModels.Count);
            var model = pedModels[modelIndex];
            var spawnData = new PedSpawnData
            {
                Active = false,
                API = false,
                GenId = System.Guid.NewGuid().ToString(),
                Model = model,
                ModelIndex = modelIndex,
                Position = Vector3.zero,
                Rotation = Quaternion.identity,
                Seed = PEDSeedGenerator.Next(),
            };
            pooledPeds.Add(SpawnPedestrian(spawnData));
            if (Loader.Instance.Network.IsMaster)
                BroadcastMessage(GetSpawnMessage(spawnData));
        }
        return pooledPeds;
    }

    public PedestrianController SpawnPedestrian(PedSpawnData spawnData)
    {
        GameObject ped = Instantiate(pedPrefab, Vector3.zero, Quaternion.identity, transform);
        var pedController = ped.GetComponent<PedestrianController>();
        pedController.SetGroundTruthBox();
        Instantiate(spawnData.Model, ped.transform);
        ped.SetActive(spawnData.Active);

        pedController.GUID = spawnData.GenId;
        pedController.GTID = ++SimulatorManager.Instance.GTIDs;
        CurrentPooledPeds.Add(pedController);

        SimulatorManager.Instance.UpdateSegmentationColors(ped);

        if (spawnData.API)
        {
            pedController.InitManual(spawnData);
        }

        //Add required components for distributing rigidbody from master to clients
        if (Loader.Instance.Network.IsClusterSimulation)
            ClusterSimulationUtilities.AddDistributedComponents(ped);

        return pedController;
    }

    public void SetPedOnMap()
    {
        for (int i = 0; i < CurrentPooledPeds.Count; i++)
        {
            if (CurrentPooledPeds[i].gameObject.activeInHierarchy)
                continue;

            var path = MapManager.GetPedPath(RandomIndex(MapManager.pedestrianLanes.Count));
            if (path == null) continue;

            if (path.mapWorldPositions == null || path.mapWorldPositions.Count == 0)
                continue;

            if (path.mapWorldPositions.Count < 2)
                continue;

            var spawnPos = path.mapWorldPositions[RandomIndex(path.mapWorldPositions.Count)];
            CurrentPooledPeds[i].transform.position = spawnPos;

            if (!WithinSpawnArea(spawnPos))
                continue;

            if (!InitSpawn)
            {
                if (IsVisible(CurrentPooledPeds[i].gameObject))
                    continue;
            }

            if (Physics.CheckSphere(spawnPos, 3f, PedSpawnCheckBitmask))
                continue;

            CurrentPooledPeds[i].InitPed(spawnPos, path.mapWorldPositions, PEDSeedGenerator.Next());
            CurrentPooledPeds[i].gameObject.SetActive(true);
            ActivePedCount++;
        }
        InitSpawn = false;
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

        for (int i = 0; i < CurrentPooledPeds.Count; i++)
        {
            DespawnPed(CurrentPooledPeds[i]);
        }
        ActivePedCount = 0;
    }

    #region api
    public void DespawnPedestrianApi(PedestrianController ped)
    {
        ped.StopPEDCoroutines();
        CurrentPooledPeds.Remove(ped);
        Destroy(ped.gameObject);
    }

    public void Reset()
    {
        RandomGenerator = new System.Random(Seed);
        PEDSeedGenerator = new System.Random(Seed);

        List<PedestrianController> peds = new List<PedestrianController>(CurrentPooledPeds);
        peds.ForEach(x => DespawnPedestrianApi(x));
        CurrentPooledPeds.Clear();
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
        if (!DebugSpawnArea)
        {
            return;
        }

        DrawSpawnArea();
    }
    #endregion

    #region network

    private DistributedMessage GetSpawnMessage(PedSpawnData data)
    {
        var message = MessagesPool.Instance.GetMessage(
            ByteCompression.RotationMaxRequiredBytes +
            ByteCompression.PositionRequiredBytes +
            2 +
            BytesStack.GetMaxByteCount(data.GenId) +
            4);
        message.AddressKey = Key;
        message.Content.PushCompressedRotation(data.Rotation);
        message.Content.PushCompressedPosition(data.Position);
        message.Content.PushInt(data.ModelIndex, 2);
        message.Content.PushString(data.GenId);
        message.Content.PushEnum<PedestrianManagerCommandType>((int) PedestrianManagerCommandType.SpawnPedestrian);
        message.Type = DistributedMessageType.ReliableOrdered;
        return message;
    }

    private void SpawnPedestrianMock(string GUID, int modelIndex, Vector3 position, Quaternion rotation) // TODO mock might be random control now, use spawnData
    {
        GameObject ped = Instantiate(pedPrefab, position, rotation, transform);
        ped.SetActive(false);
        var pedController = ped.GetComponent<PedestrianController>();
        pedController.GUID = GUID;
        //Add required components for cluster simulation
        ClusterSimulationUtilities.AddDistributedComponents(ped);
        pedController.SetGroundTruthBox();
        var model = pedModels[modelIndex];
        Instantiate(model, ped.transform);
        pedController.Control = PedestrianController.ControlType.Manual;
        pedController.enabled = false;
        //Force distributed component initialization, as gameobject will stay disabled
        pedController.InitPed(position, new List<Vector3>(), 0);
        pedController.Initialize();
        CurrentPooledPeds.Add(pedController);
    }

    private BytesStack GetDespawnMessage(int orderNumber)
    {
        var bytesStack = new BytesStack();
        bytesStack.PushInt(orderNumber, 2);
        bytesStack.PushEnum<PedestrianManagerCommandType>((int) PedestrianManagerCommandType.DespawnPedestrian);
        return bytesStack;
    }

    public void ReceiveMessage(IPeerManager sender, DistributedMessage distributedMessage)
    {
        var commandType = distributedMessage.Content.PopEnum<PedestrianManagerCommandType>();
        switch (commandType)
        {
            case PedestrianManagerCommandType.SpawnPedestrian:
                var pedestrianGUID = distributedMessage.Content.PopString();
                var modelIndex = distributedMessage.Content.PopInt(2);
                SpawnPedestrianMock(pedestrianGUID, modelIndex, 
                    distributedMessage.Content.PopDecompressedPosition(),
                    distributedMessage.Content.PopDecompressedRotation());
                break;
            case PedestrianManagerCommandType.DespawnPedestrian:
                var pedestrianId = distributedMessage.Content.PopInt(2);
                //TODO Preserve despawn command if it arrives before spawn command
                if (pedestrianId >= 0 && pedestrianId < CurrentPooledPeds.Count)
                    Destroy(CurrentPooledPeds[pedestrianId].gameObject);
                break;
        }
    }

    public void UnicastMessage(IPEndPoint endPoint, DistributedMessage distributedMessage)
    {
        Loader.Instance.Network.MessagesManager?.UnicastMessage(endPoint, distributedMessage);
    }

    public void BroadcastMessage(DistributedMessage distributedMessage)
    {
        Loader.Instance.Network.MessagesManager?.BroadcastMessage(distributedMessage);
    }

    void IMessageSender.UnicastInitialMessages(IPEndPoint endPoint)
    {
        //TODO support reconnection - send instantiation messages to the peer
    }

    #endregion
}