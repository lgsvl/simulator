/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using System.Collections;
using Simulator;
using Simulator.Map;
using Simulator.Network.Core.Components;

public class ParkingNPCManager : NPCManager
{
    private float maxParkingFillRate = 0.9f;

    private float spawnPause = 5;
    private float despawnPause = 5;

    //per minute
    public int SpawnRate
    {
        set => spawnPause = value / 60f;
    }

    public int DepawnRate
    {
        set => despawnPause = value / 60f;
    }

    private SpawnsManager parkingSpawnsManager;

    private int extraNPCPoolSize;

    protected override int NPCPoolSize => base.NPCPoolSize + extraNPCPoolSize;

    private float lastTimeInit;
    private NPCParkingBehaviour selectedNPC;
    private MapParkingSpace lastSpace;

    protected override void Start()
    {
        parkingSpawnsManager = gameObject.AddComponent<ParkingSpawnsManager>();
        extraNPCPoolSize = (int)(maxParkingFillRate * ParkingManager.instance.AllSpaces);
        base.Start();

        foreach (var npc in CurrentPooledNPCs)
        {
            npc.SetBehaviour<NPCParkingBehaviour>();
        }

        //StartCoroutine(SpawnWorker());
        //StartCoroutine(DespawnWorker());
        StartCoroutine(LeavingParkingWorker());
    }

    public override void SetNPCOnMap(bool isInitialSpawn = false)
    {
        base.SetNPCOnMap(isInitialSpawn);

        if (isInitialSpawn)
        {
            TrySpawn(CurrentPooledNPCs.Count);
        }
    }

    private void TrySpawn(int count, int startIndex = 0)
    {
        var sentinel = CurrentPooledNPCs.Count;
        var i = startIndex - 1;
        while (count > 0)
        {
            i++;
            if (sentinel-- < 0) return;
            if (i >= CurrentPooledNPCs.Count)
            {
                i = 0;
            }
            var currentNPC = CurrentPooledNPCs[i];
            if (currentNPC.gameObject.activeInHierarchy)
                continue;

            var spawnPoint = parkingSpawnsManager.GetValidSpawnPoint(currentNPC.Bounds, false); // we do not spawn on road
            if (spawnPoint == null)
                return;

            var parking = (spawnPoint.lane as MapParkingSpace);
            if (!ParkingManager.instance.TryTake(parking))
                continue;

            if (ParkingManager.instance.Fillrate >= maxParkingFillRate)
            {
                return;
            }
            if (currentNPC.Bounds.size.z > parking.Length)
            {
                continue;
            }

            currentNPC.transform.position = spawnPoint.position;
            currentNPC.transform.LookAt(spawnPoint.lookAtPoint);
            currentNPC.GTID = ++SimulatorManager.Instance.GTIDs;
            currentNPC.gameObject.SetActive(true);
            (currentNPC.ActiveBehaviour as NPCParkingBehaviour).SwitchedToParked(false, spawnPoint.lane as MapParkingSpace);

            StartCoroutine(TurnOffRigidBody(currentNPC));

            //Force snapshots resend after changing the transform position
            if (Loader.Instance.Network.IsMaster)
            {
                var rb = CurrentPooledNPCs[i].GetComponent<DistributedRigidbody>();
                if (rb != null)
                {
                    rb.BroadcastSnapshot(true);
                }
            }
            count--;
        }
    }

    private IEnumerator DespawnWorker()
    {
        while (true)
        {
            yield return new WaitForSeconds(despawnPause);
            int index = Random.Range(0, CurrentPooledNPCs.Count);

            var currentNPC = CurrentPooledNPCs[index];
            if (!currentNPC.gameObject.activeInHierarchy)
                continue;

            var behaviour = currentNPC.ActiveBehaviour as NPCParkingBehaviour;
            if (behaviour.CurrentState == NPCParkingBehaviour.State.IsParked)
            {
                behaviour.Despawn();
            }
        }
    }

    private IEnumerator SpawnWorker()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnPause);

            if (ParkingManager.instance.Fillrate < maxParkingFillRate)
            {
                int index = Random.Range(0, CurrentPooledNPCs.Count);
                TrySpawn(1, index);
            }
        }
    }

    private IEnumerator LeavingParkingWorker()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            foreach (var p in CurrentPooledNPCs)
            {
                var park = p.ActiveBehaviour as NPCParkingBehaviour;
                if (park.CurrentState == NPCParkingBehaviour.State.IsParked)
                {
                    park.TryInitLeaving();
                }
            }
        }
    }

    public override void DespawnNPC(NPCController npc)
    {
        var parkingBehaviour = (npc.ActiveBehaviour as NPCParkingBehaviour);
        npc.gameObject.SetActive(false);
        npc.transform.position = transform.position;
        npc.transform.rotation = Quaternion.identity;

        npc.StopNPCCoroutines();
        npc.enabled = false;

        if (NPCActive && parkingBehaviour.CurrentState != NPCParkingBehaviour.State.IsParked)
        {
            ActiveNPCCount--;
        }

        foreach (var callback in DespawnCallbacks)
        {
            callback(npc);
        }
    }

    IEnumerator TurnOffRigidBody(NPCController controller)
    {
        yield return new WaitForSeconds(0.5f);
        (controller.ActiveBehaviour as NPCParkingBehaviour).ChangePhysic(false);
    }

    public void ChangeActiveCountBy(int diff)
    {
        ActiveNPCCount += diff;
    }

    void Update()
    {
        if (Simulator.Input.GetKeyDown(KeyCode.L)) // TODO Demo edit?
        {
            foreach (var p in CurrentPooledNPCs)
            {
                var park = p.ActiveBehaviour as NPCParkingBehaviour;
                if (park.CurrentState == NPCParkingBehaviour.State.IsParked)
                {
                    park.TryInitLeaving();
                }
            }
        }
    }

    bool ForceSomeNPCToParkInSpot(MapParkingSpace space)
    {
        lastTimeInit = Time.time;
        lastSpace = space;
        var lane = SimulatorManager.Instance.MapManager.GetClosestLane(space.transform.position);
        var allNPCs = SimulatorManager.Instance.NPCManager.CurrentPooledNPCs;
        var startOfLane = lane.mapWorldPositions[0];
        var minDist = float.MaxValue;
        NPCParkingBehaviour bestNpc = null;
        var distFromSpace = (startOfLane - space.transform.position).magnitude;
        foreach (var npcController in allNPCs)
        {
            var parking = npcController.ActiveBehaviour as NPCParkingBehaviour;
            if (parking != null)
            {
                if (npcController.isActiveAndEnabled)
                {
                    if (parking.CurrentState == NPCParkingBehaviour.State.IsParking)
                    {
                        if (parking.CurrentSpace == space)
                        {
                            Debug.Log("already parking");
                            return true;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else if (parking.CurrentState == NPCParkingBehaviour.State.IsLeaving)
                    {
                        continue;
                    }
                    var distFromLaneStart = (startOfLane - parking.transform.position).magnitude;
                    var distToParkingSpace = (space.transform.position - parking.transform.position).magnitude;
                    if (parking.currentMapLane == lane && distFromLaneStart < distFromSpace &&
                        distToParkingSpace > 10 ||
                        lane.prevConnectedLanes.Contains(parking.currentMapLane) && distFromLaneStart < 10)

                    {
                        if (distFromLaneStart < minDist)
                        {
                            bestNpc = parking;
                            minDist = distFromLaneStart;
                        }
                    }
                }
            }
        }

        if (bestNpc == null)
        {
            var npc = SpawnNpcAbleToPark(lane);
            if (npc == null)
            {
                return false; //could try again after some time
            }
            bestNpc = npc.ActiveBehaviour as NPCParkingBehaviour;
        }

        bestNpc.InitParking(space);
        selectedNPC = bestNpc;
        return true;
    }

    NPCController SpawnNpcAbleToPark(MapTrafficLane lane)
    {
        // TODO check box with npc bounds
        if (Physics.CheckSphere(lane.mapWorldPositions[0], 3, SpawnsManager.NPCSpawnCheckBitmask))
            return null;

        for (int i = 0; i < CurrentPooledNPCs.Count; i++)
        {
            if (CurrentPooledNPCs[i].gameObject.activeInHierarchy)
                continue;

            var parking = CurrentPooledNPCs[i].ActiveBehaviour as NPCParkingBehaviour;
            if (parking == null || !parking.AbleToPark)
                continue;

            CurrentPooledNPCs[i].transform.position = lane.mapWorldPositions[0];
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
                {
                    rb.BroadcastSnapshot(true);
                }
            }
            return CurrentPooledNPCs[i];
        }
        Debug.LogError("Could not find npc");
        return null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        if (Time.time - lastTimeInit < 0.5f) Gizmos.DrawCube(lastSpace.transform.position, Vector3.one * 2);
        if (selectedNPC != null && selectedNPC.CurrentState == NPCParkingBehaviour.State.IsParking)
        {
            Gizmos.DrawSphere(selectedNPC.transform.position, 1);
            Gizmos.DrawLine(selectedNPC.transform.position, lastSpace.transform.position);
        }
    }
}
