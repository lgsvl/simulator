/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using System.Net;
using Simulator;
using Simulator.Map;
using UnityEngine;
using Simulator.Network.Core.Connection;
using Simulator.Network.Core.Messaging;
using Simulator.Network.Core.Messaging.Data;
using Simulator.Network.Shared;
using Simulator.Network.Shared.Messages;
using Simulator.Utilities;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using static Simulator.Web.Config;
using System.Linq;

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
    public List<PedAssetData> PedestrianData = new List<PedAssetData>();

    public bool PedestriansActive { get; set; } = false;
    [HideInInspector]
    public List<PedestrianController> CurrentPooledPeds = new List<PedestrianController>();
    private List<PedestrianController> ActiveAutomaticPeds = new List<PedestrianController>();
    private LayerMask PedSpawnCheckBitmask;
    public SpawnsManager spawnsManager;

    public string Key => "PedestrianManager"; //Network IMessageSender key

    private int PedMaxCount = 0;

    private System.Random RandomGenerator;
    public System.Random PEDSeedGenerator { get; private set; } // Only use this for initializing a new pedestrian
    private int Seed = new System.Random().Next();

    private MapOrigin MapOrigin;

    public delegate void SpawnCallbackType(PedestrianController controller);
    List<SpawnCallbackType> SpawnCallbacks = new List<SpawnCallbackType>();

    public delegate void DespawnCallbackType(PedestrianController controller);
    List<DespawnCallbackType> DespawnCallbacks = new List<DespawnCallbackType>();

    public void RegisterSpawnCallback(SpawnCallbackType callback)
    {
        SpawnCallbacks.Add(callback);
    }

    public void DeregisterSpawnCallback(SpawnCallbackType callback)
    {
        if (!SpawnCallbacks.Remove(callback))
        {
            Debug.LogWarning("Error in DeregisterDespawnCallback. " + callback + " is not registered before.");
        }
    }

    public void ClearSpawnCallbacks()
    {
        SpawnCallbacks.Clear();
    }

    public void RegisterDespawnCallback(DespawnCallbackType callback)
    {
        DespawnCallbacks.Add(callback);
    }

    public void DeregisterDespawnCallback(DespawnCallbackType callback)
    {
        if (!DespawnCallbacks.Remove(callback))
        {
            Debug.LogWarning("Error in DeregisterDespawnCallback. " + callback + " is not registered before.");
        }
    }

    public void ClearDespawnCallbacks()
    {
        DespawnCallbacks.Clear();
    }

    public void InitRandomGenerator(int seed)
    {
        Seed = seed;
        RandomGenerator = new System.Random(Seed);
        PEDSeedGenerator = new System.Random(Seed);
    }

    private void Start()
    {
        MapOrigin = MapOrigin.Find();
        spawnsManager = GetComponent<SpawnsManager>();
        PedSpawnCheckBitmask = LayerMask.GetMask("Pedestrian", "Agent", "NPC");
        PedMaxCount = MapOrigin.PedMaxCount;

        PedestrianData.Clear();

        if (Loader.Instance.CurrentSimulation == null)
        {
            return;
        }

        if (Loader.Instance.CurrentSimulation.Peds == null)
        {
            Loader.Instance.CurrentSimulation.Peds = Simulator.Web.Config.Pedestrians.Values.ToArray();
        }

        foreach (var data in Loader.Instance.CurrentSimulation.Peds)
        {
            if (data.Enabled)
            {
                GameObject obj = null;
                foreach (var item in Simulator.Web.Config.Pedestrians.Values)
                {
                    if (item.Name == data.Name)
                    {
                        obj = item.Prefab;
                    }
                }
                PedestrianData.Add(new PedAssetData
                {
                    Prefab = obj,
                    Name = obj.name,
                    AssetGuid = data.AssetGuid
                });
            }
        }

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
                var pool = SpawnPedPool();
                if (pool != null)
                {
                    if (PedestriansActive)
                    {
                        SetPedOnMap(true);
                    }
                }
                else
                {
                    Debug.Log("No pedestrian pool, disabled pedestrian manager ");
                    gameObject.SetActive(false);
                }
            }
        }
        else
        {
            var sceneName = SceneManager.GetActiveScene().name;
            Debug.LogWarning($"{sceneName} missing NavMesh at {pt} please create navmesh at this point. Pedestrian manager disabled");
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
            if (ActiveAutomaticPeds.Count < PedMaxCount)
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
        if (pedPrefab == null)
        {
            Debug.LogWarning("Pedestrian prefab is null, please check Pedestrian manager public reference");
            return null;
        }

        if (PedestrianData.Count == 0)
        {
            Debug.LogWarning("Pedestrian count is 0, please clone and build pedestrians");
            return null;
        }

        var pooledPeds = new List<PedestrianController>();
        var pedCount = CurrentPooledPeds.Count;
        int poolCount = Mathf.FloorToInt(PedMaxCount + (PedMaxCount * 0.1f));
        for (int i = pedCount; i < poolCount; i++)
        {
            var modelIndex = RandomGenerator.Next(PedestrianData.Count);
            var model = PedestrianData[modelIndex].Prefab;
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
        Instantiate(spawnData.Model, ped.transform);
        pedController.SetGroundTruthBox();
        ped.SetActive(spawnData.Active);

        pedController.GUID = spawnData.GenId;
        CurrentPooledPeds.Add(pedController);

        if (spawnData.API)
        {
            pedController.InitAPIPed(spawnData);
        }

        //Add required components for distributing rigidbody from master to clients
        if (Loader.Instance.Network.IsClusterSimulation)
        {
            ClusterSimulationUtilities.AddDistributedComponents(ped);
        }

        // Add components for auto light layers change
        var triggerCollider = ped.AddComponent<SphereCollider>();
        if (triggerCollider != null)
        {
            triggerCollider.radius = 0.3f;
            triggerCollider.isTrigger = true;
        }
        ped.AddComponent<AgentZoneController>();

        foreach (var callback in SpawnCallbacks)
        {
            callback(pedController);
        }

        return pedController;
    }

    public void SetPedOnMap(bool isInitialSpawn = false)
    {
        for (int i = 0; i < CurrentPooledPeds.Count; i++)
        {
            if (CurrentPooledPeds[i].gameObject.activeInHierarchy)
                continue;

            var spawnPoint = spawnsManager.GetValidSpawnPoint(CurrentPooledPeds[i].Bounds, !isInitialSpawn);
            if (spawnPoint == null)
                return;

            var pedLane = spawnPoint.lane as MapPedestrianLane;
            if (pedLane==null)
                continue;
            
            CurrentPooledPeds[i].InitPed(spawnPoint.position, spawnPoint.spawnIndex, pedLane.mapWorldPositions, PEDSeedGenerator.Next(), pedLane);
            CurrentPooledPeds[i].gameObject.SetActive(true);
            ActiveAutomaticPeds.Add(CurrentPooledPeds[i]);

            SimulatorManager.Instance.UpdateSegmentationColors(CurrentPooledPeds[i].gameObject, CurrentPooledPeds[i].GTID);
        }
    }

    public void DespawnPed(PedestrianController ped)
    {
        ped.gameObject.SetActive(false);
        ActiveAutomaticPeds.Remove(ped);
        ped.transform.position = transform.position;
        ped.transform.rotation = Quaternion.identity;

        SimulatorManager.Instance.SegmentationIdMapping.RemoveSegmentationId(ped.GTID);
    }

    public void DespawnAllPeds()
    {
        if (ActiveAutomaticPeds.Count == 0) return;

        for (int i = ActiveAutomaticPeds.Count - 1; i >= 0; i--)
        {
            DespawnPed(ActiveAutomaticPeds[i]);
        }
    }

    #region api
    public void DespawnPedestrianApi(PedestrianController ped)
    {
        DespawnPed(ped);
        ped.StopPEDCoroutines();
        CurrentPooledPeds.Remove(ped);
        Destroy(ped.gameObject);
    }

    public void Reset()
    {
        RandomGenerator = new System.Random(Seed);
        PEDSeedGenerator = new System.Random(Seed);

        List<PedestrianController> peds = new List<PedestrianController>(CurrentPooledPeds);
        peds.ForEach(x =>
        {
            DespawnPedestrianApi(x);
        });
        CurrentPooledPeds.Clear();
        ClearSpawnCallbacks();
        ClearDespawnCallbacks();
    }
    #endregion

    #region utilities
    private int RandomIndex(int max = 1)
    {
        return RandomGenerator.Next(max);
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
        if (modelIndex < 0)
        {
            Debug.LogError("PedestrianManager received an invalid model index. Pedestrian mock cannot be spawned.");
            return;
        }
        if (modelIndex >= PedestrianData.Count)
        {
            Debug.LogError("PedestrianManager received an invalid model index. Pedestrian mock cannot be spawned. Make sure that every cluster machine uses the same pedestrians list.");
            return;
        }
        GameObject ped = Instantiate(pedPrefab, position, rotation, transform);
        ped.SetActive(false);
        var pedController = ped.GetComponent<PedestrianController>();
        pedController.GUID = GUID;
        //Add required components for cluster simulation
        ClusterSimulationUtilities.AddDistributedComponents(ped);
        var model = PedestrianData[modelIndex].Prefab;
        Instantiate(model, ped.transform);
        pedController.SetGroundTruthBox();
        pedController.enabled = false;
        //Force distributed component initialization, as gameobject will stay disabled
        pedController.InitPed(position, 0, new List<Vector3>(), 0);
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
