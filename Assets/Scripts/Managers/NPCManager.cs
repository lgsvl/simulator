/**
 * Copyright (c) 2019 LG Electronics, Inc.
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

public class NPCManager : MonoBehaviour, IMessageSender, IMessageReceiver
{
    [System.Serializable]
    public struct NPCS
    {
        public GameObject Prefab;
        public NPCSizeType NPCType;
    }
    public List<NPCS> NPCVehicles = new List<NPCS>();

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
    // loosely based on ppg 2017 trends https://news.ppg.com/automotive-color-trends/
    public List<NPCColors> NPCColorData = new List<NPCColors>();

    [Serializable]
    public struct NPCSpawnData
    {
        public bool Active;
        public string GenId;
        public NPCS Template;
        public Vector3 Position;
        public Quaternion Rotation;
        public Color Color;
        public int Seed;
    };

    // startTime kept for recording when Simulator starts.
    // Used for deciding when to record log file.
    [System.NonSerialized]
    public double startTime = 0f;
    private MapOrigin MapOrigin;
    private bool InitSpawn = true;

    public bool NPCActive { get; set; } = false;
    [HideInInspector]
    public List<NPCController> CurrentPooledNPCs = new List<NPCController>();
    private LayerMask NPCSpawnCheckBitmask;
    private Vector3 SpawnBoundsSize;
    private bool DebugSpawnArea = false;
    private int NPCMaxCount = 0;
    private  int ActiveNPCCount = 0;
    private System.Random RandomGenerator;
    public System.Random NPCSeedGenerator { get; private set; } // Only use this for initializing a new NPC
    private int Seed = new System.Random().Next();
    private List<NPCController> APINPCs = new List<NPCController>();
    public string Key => "NPCManager"; //Network IMessageSender key

    private Camera SimulatorCamera;
    private MapManager MapManager;

    public delegate void DespawnCallbackType(NPCController controller);
    List<DespawnCallbackType> DespawnCallbacks = new List<DespawnCallbackType>();

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
        NPCSpawnCheckBitmask = LayerMask.GetMask("NPC", "Agent");
        SpawnBoundsSize = new Vector3(MapOrigin.NPCSpawnBoundSize, 50f, MapOrigin.NPCSpawnBoundSize);
        NPCMaxCount = MapOrigin.NPCMaxCount;
        SimulatorCamera = SimulatorManager.Instance.CameraManager.SimulatorCamera;
        MapManager = SimulatorManager.Instance.MapManager;

        NPCVehicles.Clear();
        foreach (var data in Simulator.Web.Config.NPCVehicles)
        {
            NPCVehicles.Add(new NPCS{
                NPCType = data.Value.NPCType,
                Prefab = data.Value.prefab,
            });
           
            if(NPCColorData.Count(d => d.Type == data.Value.NPCType) == 0)
            {
                Debug.LogWarning($"NPC of type {data.Value.NPCType} loaded but no colors to pick configured for this type");
            }
        }

        var network = Loader.Instance.Network;
        network.MessagesManager?.RegisterObject(this);
        if (!SimulatorManager.Instance.IsAPI && !network.IsClient)
        {
            SpawnNPCPool();
            if (NPCActive)
                SetNPCOnMap();
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
            ClusterSimulationUtilities.AddDistributedComponents(go);

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

        int poolCount = Mathf.FloorToInt(NPCMaxCount + (NPCMaxCount * 0.1f));
        for (int i = 0; i < poolCount; i++)
        {
            var template = GetWeightedRandomNPC();
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
                BroadcastMessage(GetSpawnMessage(spawnData));
        }
        return pooledNPCs;
    }

    public void SetNPCOnMap()
    {
        for (int i = 0; i < CurrentPooledNPCs.Count; i++)
        {
            if (CurrentPooledNPCs[i].gameObject.activeInHierarchy)
            {
                continue;
            }

            var lane = MapManager.GetLane(RandomGenerator.Next(MapManager.trafficLanes.Count));
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
                if (!lane.Spawnable)
                {
                    if (IsVisible(CurrentPooledNPCs[i].gameObject))
                    {
                        continue;
                    }
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

            //Force snapshots resend after changing the transform position
            if (Loader.Instance.Network.IsMaster)
            {
                var rb = CurrentPooledNPCs[i].GetComponent<DistributedRigidbody>();
                if (rb != null)
                    rb.BroadcastSnapshot(true);
            }
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

    public void DespawnNPC(NPCController npc)
    {
        npc.gameObject.SetActive(false);
        npc.transform.position = transform.position;
        npc.transform.rotation = Quaternion.identity;

        npc.StopNPCCoroutines();
        npc.enabled = false;

        if (NPCActive)
            ActiveNPCCount--;

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
            obj.currentIntersection.npcsInIntersection.Remove(obj.transform);

        CurrentPooledNPCs.Remove(obj);
        Destroy(obj.gameObject);
    }

    public void DespawnAllNPC()
    {
        if (ActiveNPCCount == 0) return;

        for (int i = 0; i < CurrentPooledNPCs.Count; i++)
        {
            DespawnNPC(CurrentPooledNPCs[i]);
        }
        foreach (var item in FindObjectsOfType<MapIntersection>())
        {
            item.stopQueue.Clear();
        }

        ActiveNPCCount = 0;
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
        if(NPCFrequencyWeights.ContainsKey(type))
        {
            return NPCFrequencyWeights[type];
        }
        return 1;
    }

    private NPCS GetWeightedRandomNPC()
    {
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

        throw new System.Exception("NPC size weights are incorrectly set!");
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

    public bool WithinSpawnArea(Vector3 pos)
    {
        var spawnT = SimulatorManager.Instance.AgentManager.CurrentActiveAgent?.transform;
        spawnT = spawnT ?? transform;
        var spawnBounds = new Bounds(spawnT.position, SpawnBoundsSize);
        return spawnBounds.Contains(pos);
    }

    public bool IsVisible(GameObject npc)
    {
        var activeAgents = SimulatorManager.Instance.AgentManager.ActiveAgents;
        var npcColliderBounds = npc.GetComponent<NPCController>().MainCollider.bounds;

        var activeCameraPlanes = GeometryUtility.CalculateFrustumPlanes(SimulatorCamera);
        if (GeometryUtility.TestPlanesAABB(activeCameraPlanes, npcColliderBounds))
            return true;

        foreach (var activeAgent in activeAgents)
        {
            var activeAgentController = activeAgent.AgentGO.GetComponent<AgentController>();
            foreach (var sensor in activeAgentController.AgentSensors)
            {
                if (sensor.CheckVisible(npcColliderBounds))
                    return true;
            }
        }
        return false;
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
                    Destroy(CurrentPooledNPCs[npcId].gameObject);
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
