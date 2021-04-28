/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using Simulator.Map;
using System.Linq;
using System.Net;
using Simulator;
using Simulator.Network.Core.Components;
using Simulator.Network.Core.Connection;
using Simulator.Network.Core.Messaging;
using Simulator.Network.Core.Messaging.Data;
using Simulator.Network.Shared;
using Simulator.Network.Shared.Messages;
using static Simulator.Web.Config;

public class NPCManager : MonoBehaviour, IMessageSender, IMessageReceiver
{
    public Dictionary<NPCSizeType, int> NPCFrequencyWeights = new Dictionary<NPCSizeType, int> ()
    {
        [NPCSizeType.Compact]       = 5,
        [NPCSizeType.MidSize]       = 6,
        [NPCSizeType.Luxury]        = 2,
        [NPCSizeType.Sport]         = 1,
        [NPCSizeType.LightTruck]    = 2,
        [NPCSizeType.SUV]           = 4,
        [NPCSizeType.MiniVan]       = 3,
        [NPCSizeType.Large]         = 2,
        [NPCSizeType.Emergency]     = 1,
        [NPCSizeType.Bus]           = 1,
        [NPCSizeType.Trailer]       = 0,
        [NPCSizeType.Motorcycle]    = 1,
        [NPCSizeType.Bicycle]       = 1,
    };

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

    public List<NPCAssetData> NPCVehicles = new List<NPCAssetData>();

    // loosely based on ppg 2017 trends https://news.ppg.com/automotive-color-trends/
    public List<NPCColors> NPCColorData = new List<NPCColors>();

    public SpawnsManager spawnsManager;

    [Serializable]
    public struct NPCSpawnData
    {
        public bool Active;
        public string GenId;
        public NPCAssetData Template;
        public Vector3 Position;
        public Quaternion Rotation;
        public Color Color;
        public int Seed;
    };

    // startTime kept for recording when Simulator starts.
    // Used for deciding when to record log file.
    [System.NonSerialized]
    public double StartTime = 0f;
    private MapOrigin MapOrigin;

    public bool NPCActive { get; set; } = false;
    [HideInInspector]
    public List<NPCController> CurrentPooledNPCs = new List<NPCController>();
    private bool DebugSpawnArea = false;
    private int NPCMaxCount = 0;
    private  int ActiveNPCCount = 0;
    private System.Random RandomGenerator;
    public System.Random NPCSeedGenerator { get; private set; } // Only use this for initializing a new NPC
    private int Seed = new System.Random().Next();
    private List<NPCController> APINPCs = new List<NPCController>();
    public string Key => "NPCManager"; //Network IMessageSender key

    private CameraManager SimCameraManager;
    private Camera SimulatorCamera;
    private MapManager MapManager;

    private bool InitSpawn = true;
    private Ray TestRay;
    private RaycastHit[] RayCastHits = new RaycastHit[1];

    public delegate void SpawnCallbackType(NPCController controller);
    List<SpawnCallbackType> SpawnCallbacks = new List<SpawnCallbackType>();

    public delegate void DespawnCallbackType(NPCController controller);
    List<DespawnCallbackType> DespawnCallbacks = new List<DespawnCallbackType>();

    public void RegisterSpawnCallback(SpawnCallbackType callback)
    {
        SpawnCallbacks.Add(callback);
    }

    public void DeregisterSpawnCallback(SpawnCallbackType callback)
    {
        if (!SpawnCallbacks.Remove(callback))
        {
            Debug.LogError("Error in DeregisterDespawnCallback. " + callback + " is not registered before.");
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
            Debug.LogError("Error in DeregisterDespawnCallback. " + callback + " is not registered before.");
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
        NPCSeedGenerator = new System.Random(Seed);
    }

    private void Start()
    {
        MapOrigin = MapOrigin.Find();
        spawnsManager = GetComponent<SpawnsManager>();
        NPCMaxCount = MapOrigin.NPCMaxCount;
        SimCameraManager = SimulatorManager.Instance.CameraManager;
        SimulatorCamera = SimCameraManager.SimulatorCamera;
        MapManager = SimulatorManager.Instance.MapManager;
        InitSpawn = true;

        NPCVehicles.Clear();

        if (Loader.Instance.CurrentSimulation == null)
        {
            return;
        }

        if (Loader.Instance.CurrentSimulation.NPCs == null)
        {
            Loader.Instance.CurrentSimulation.NPCs = Simulator.Web.Config.NPCVehicles.Values.ToArray();
        }

        foreach (var data in Loader.Instance.CurrentSimulation.NPCs)
        {
            if (data.Enabled)
            {
                GameObject obj = null;
                foreach (var item in Simulator.Web.Config.NPCVehicles.Values)
                {
                    if (item.Name == data.Name)
                    {
                        obj = item.Prefab;
                    }
                }
                NPCVehicles.Add(new NPCAssetData
                {
                    NPCType = data.NPCType,
                    Prefab = obj,
                });

                if (NPCColorData.Count(d => d.Type == data.NPCType) == 0)
                {
                    Debug.LogWarning($"NPC of type {data.NPCType} loaded but no colors to pick configured for this type");
                }
            }
        }

        var network = Loader.Instance.Network;
        network.MessagesManager?.RegisterObject(this);
        if (!SimulatorManager.Instance.IsAPI && !network.IsClient)
        {
            SpawnNPCPool();
            if (NPCActive)
                SetNPCOnMap(true);
        }
    }

    private void OnDestroy()
    {
        Loader.Instance.Network.MessagesManager?.UnregisterObject(this);
    }

    public void PhysicsUpdate()
    {
        if (NPCActive)
        {
            if (ActiveNPCCount < NPCMaxCount)
            {
                SetNPCOnMap();
            }
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

    #region npc
    public void ToggleNPC()
    {
        NPCActive = !NPCActive;
    }

    public NPCController SpawnNPC(NPCSpawnData spawnData)
    {
        var go = new GameObject();
        go.SetActive(spawnData.Active);
        go.transform.SetParent(transform);
        go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        go.layer = LayerMask.NameToLayer("NPC");
        go.tag = "Car";
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 2000;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        var NPCController = go.AddComponent<NPCController>();
        var npc_name = Instantiate(spawnData.Template.Prefab, go.transform).name;
        go.name = npc_name + spawnData.GenId;
        NPCController.Size = spawnData.Template.NPCType;
        NPCController.NPCColor = spawnData.Color;
        NPCController.NPCLabel = GetNPCLabel(npc_name);
        NPCController.id = spawnData.GenId;
        NPCController.GTID = ++SimulatorManager.Instance.GTIDs;
        NPCController.Init(spawnData.Seed);
        go.transform.SetPositionAndRotation(spawnData.Position, spawnData.Rotation);
        NPCController.SetLastPosRot(spawnData.Position, spawnData.Rotation);
        NPCController.SetBehaviour<NPCLaneFollowBehaviour>();
        CurrentPooledNPCs.Add(NPCController);

        SimulatorManager.Instance.UpdateSegmentationColors(go);

        //Add required components for distributing rigidbody from master to clients
        if (Loader.Instance.Network.IsClusterSimulation)
        {
            ClusterSimulationUtilities.AddDistributedComponents(go);
        }

        foreach (var callback in SpawnCallbacks)
        {
            callback(NPCController);
        }

        return NPCController;
    }

    public List<NPCController> SpawnNPCPool()
    {
        var pooledNPCs = new List<NPCController>();
        for (int i = 0; i < CurrentPooledNPCs.Count; i++)
        {
            if (Loader.Instance.Network.IsMaster)
            {
                var index = CurrentPooledNPCs.IndexOf(CurrentPooledNPCs[i]);
                BroadcastMessage(GetDespawnMessage(index));
            }

            Destroy(CurrentPooledNPCs[i]);
        }
        CurrentPooledNPCs.Clear();
        ActiveNPCCount = 0;

        for (int i = 0; i < NPCMaxCount; i++)
        {
            var template = GetWeightedRandomNPC();
            if (template == null)
            {
                Debug.LogWarning("NPC size weights are incorrect, pooling stopped");
                return null;
            }
            var spawnData = new NPCSpawnData
            {
                Active = false,
                GenId = System.Guid.NewGuid().ToString(),
                Template = template,
                Position = Vector3.zero,
                Rotation = Quaternion.identity,
                Color = GetWeightedRandomColor(template.NPCType),
                Seed = NPCSeedGenerator.Next(),
            };
            pooledNPCs.Add(SpawnNPC(spawnData));
            if (Loader.Instance.Network.IsMaster)
            {
                BroadcastMessage(GetSpawnMessage(spawnData));
            }
        }
        return pooledNPCs;
    }

    public void SetNPCOnMap(bool isInitialSpawn = false)
    {
        //Wait 1s if spawning failed to limit the raycasting checks
        if (!isInitialSpawn && spawnsManager.FailedSpawnTime + 1.0f > Time.time)
            return;

        for (int i = 0; i < CurrentPooledNPCs.Count; i++)
        {
            if (CurrentPooledNPCs[i].gameObject.activeInHierarchy)
                continue;

            var spawnPoint = spawnsManager.GetValidSpawnPoint(CurrentPooledNPCs[i].Bounds, !isInitialSpawn);
            if (spawnPoint == null)
                return;

            CurrentPooledNPCs[i].transform.position = spawnPoint.position;
            CurrentPooledNPCs[i].transform.LookAt(spawnPoint.lookAtPoint);
            CurrentPooledNPCs[i].InitLaneData(spawnPoint.lane as MapTrafficLane);
            CurrentPooledNPCs[i].GTID = ++SimulatorManager.Instance.GTIDs;
            CurrentPooledNPCs[i].gameObject.SetActive(true);
            CurrentPooledNPCs[i].enabled = true;
            ActiveNPCCount++;

            //Force snapshots resend after changing the transform position
            if (Loader.Instance.Network.IsMaster)
            {
                var rb = CurrentPooledNPCs[i].GetComponent<DistributedRigidbody>();
                if (rb != null)
                {
                    rb.BroadcastSnapshot(true);
                }
            }
        }
    }

    public Transform GetRandomActiveNPC()
    {
        if (CurrentPooledNPCs.Count == 0)
        {
            return transform;
        }

        int index = RandomGenerator.Next(CurrentPooledNPCs.Count);
        while (!CurrentPooledNPCs[index].gameObject.activeInHierarchy)
        {
            index = RandomGenerator.Next(CurrentPooledNPCs.Count);
        }
        return CurrentPooledNPCs[index].transform;
    }

    public void DespawnNPC(NPCController npc)
    {
        npc.gameObject.SetActive(false);
        npc.transform.position = transform.position;
        npc.transform.rotation = Quaternion.identity;

        npc.StopNPCCoroutines();
        npc.enabled = false;

        if (NPCActive)
        {
            ActiveNPCCount--;
        }

        foreach (var callback in DespawnCallbacks)
        {
            callback(npc);
        }
    }

    public void DestroyNPC(NPCController obj)
    {
        if (obj == null)
        {
            return;
        }

        if (Loader.Instance.Network.IsMaster)
        {
            var index = CurrentPooledNPCs.IndexOf(obj);
            BroadcastMessage(GetDespawnMessage(index));
        }

        obj.StopNPCCoroutines();

        if (obj.currentIntersection != null)
        {
            obj.currentIntersection.npcsInIntersection.Remove(obj.transform);
        }

        CurrentPooledNPCs.Remove(obj);
        Destroy(obj.gameObject);
    }

    public void DespawnAllNPC()
    {
        if (ActiveNPCCount == 0)
            return;

        for (int i = 0; i < CurrentPooledNPCs.Count; i++)
        {
            DespawnNPC(CurrentPooledNPCs[i]);
        }
        foreach (var item in FindObjectsOfType<MapIntersection>())
        {
            item.stopQueue.Clear();
        }

        ActiveNPCCount = 0;
        InitSpawn = true;
    }

    public void Reset()
    {
        RandomGenerator = new System.Random(Seed);
        NPCSeedGenerator = new System.Random(Seed);

        List<NPCController> npcs = new List<NPCController>(CurrentPooledNPCs);
        foreach (var npc in npcs)
        {
            DestroyNPC(npc);
        }

        CurrentPooledNPCs.Clear();
        ClearDespawnCallbacks();
        ClearSpawnCallbacks();
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

    int GetNPCFrequencyWeight(NPCSizeType type)
    {
        if (NPCFrequencyWeights.ContainsKey(type))
        {
            return NPCFrequencyWeights[type];
        }
        return 1;
    }

    private NPCAssetData GetWeightedRandomNPC()
    {
        if (NPCVehicles.Count == 0)
        {
            Debug.LogWarning("NPC count is 0, please clone and build npcs");
            return null;
        }

        int totalWeight = NPCVehicles.Where(npc => HasSizeFlag(npc.NPCType)).Sum(npc => GetNPCFrequencyWeight(npc.NPCType));
        int rnd = RandomGenerator.Next(totalWeight);

        foreach (var npc in NPCVehicles)
        {
            if (npc.NPCType != NPCSizeType.Trailer && HasSizeFlag(npc.NPCType))
            {
                int weight = GetNPCFrequencyWeight(npc.NPCType);
                if (rnd < weight)
                {
                    return npc;
                }
                rnd -= weight;
            }
        }
        return null;
    }

    public Color GetWeightedRandomColor(NPCSizeType type)
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

    private void OnDrawGizmosSelected()
    {
        if (!DebugSpawnArea)
            return;

        spawnsManager.DrawSpawnArea();
    }
    #endregion

    #region network
    private DistributedMessage GetSpawnMessage(NPCSpawnData data)
    {
        var message = MessagesPool.Instance.GetMessage(
            ByteCompression.RotationMaxRequiredBytes +
            ByteCompression.PositionRequiredBytes +
            12 +
            BytesStack.GetMaxByteCount(data.GenId)+
            1 +
            ByteCompression.RequiredBytes<NPCManagerCommandType>());
        message.AddressKey = Key;
        var indexOfPrefab = NPCVehicles.FindIndex(npc => npc.Equals(data.Template));
        message.Content.PushCompressedRotation(data.Rotation);
        message.Content.PushCompressedPosition(data.Position);
        message.Content.PushCompressedColor(data.Color, 1);
        message.Content.PushInt(data.Seed);
        message.Content.PushInt(indexOfPrefab, 2);
        message.Content.PushString(data.GenId);
        message.Content.PushBool(data.Active);
        message.Content.PushEnum<NPCManagerCommandType>((int) NPCManagerCommandType.SpawnNPC);
        message.Type = DistributedMessageType.ReliableOrdered;
        return message;
    }

    private void SpawnNPCMock(DistributedMessage message)
    {
        var data = new NPCSpawnData()
        {
            Active = message.Content.PopBool(),
            GenId = message.Content.PopString(),
            Template = NPCVehicles[message.Content.PopInt(2)],
            Seed = message.Content.PopInt(),
            Color = message.Content.PopDecompressedColor(1),
            Position = message.Content.PopDecompressedPosition(),
            Rotation = message.Content.PopDecompressedRotation()
        };
        var npc = SpawnNPC(data);
        Destroy(npc.ActiveBehaviour);
        var rb = npc.GetComponentInChildren<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
    }

    private DistributedMessage GetDespawnMessage(int orderNumber)
    {
        var message = MessagesPool.Instance.GetMessage(6);
        message.AddressKey = Key;
        message.Content.PushInt(orderNumber, 2);
        message.Content.PushEnum<NPCManagerCommandType>((int) NPCManagerCommandType.DespawnNPC);
        message.Type = DistributedMessageType.ReliableOrdered;
        return message;
    }

    public void ReceiveMessage(IPeerManager sender, DistributedMessage distributedMessage)
    {
        var commandType = distributedMessage.Content.PopEnum<NPCManagerCommandType>();
        switch (commandType)
        {
            case NPCManagerCommandType.SpawnNPC:
                SpawnNPCMock(distributedMessage);
                break;
            case NPCManagerCommandType.DespawnNPC:
                var npcId = distributedMessage.Content.PopInt(2);
                //TODO Preserve despawn command if it arrives before spawn command
                if (npcId >= 0 && npcId < CurrentPooledNPCs.Count)
                {
                    var controller = CurrentPooledNPCs[npcId];
                    if (controller != null)
                    {
                        Destroy(controller.gameObject);
                    }
                }
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
