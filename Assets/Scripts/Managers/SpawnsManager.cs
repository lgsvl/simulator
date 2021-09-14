/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using Simulator;
using Simulator.Map;
using UnityEngine;

public class SpawnsManager : MonoBehaviour
{
    // TODO different class for each spawn type
    public enum SpawnAreaType
    {
        TrafficLanes = 0,
        PedestrianLanes = 1,
        ParkingSpaces = 2
    }
    
    public class SpawnPoint
    {
        public Vector3 position;
        public int spawnIndex;
        public Vector3 lookAtPoint;
        public ISpawnable lane;
    }

    private const float SightDistanceLimit = 200.0f;
    
    //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
    [SerializeField]
    protected SpawnAreaType spawnAreaType;
    
    [SerializeField]
    protected float spawnRadius = 6.0f;
#pragma warning restore 0649

    private bool IsInitialized;
    protected MapOrigin MapOrigin;
    protected List<SpawnPoint> SpawnPoints = new List<SpawnPoint>();
    private Vector3 SpawnBoundsSize;
    protected int CurrentIndex;
    private LayerMask VisibleLM;
    public LayerMask NPCSpawnCheckBitmask { get; protected set; }
    private RaycastHit[] RaycastHits = new RaycastHit[5];

    public float FailedSpawnTime { get; protected set; }

    protected virtual void Initialize()
    {
        if (IsInitialized)
            return;

        MapOrigin = MapOrigin.Find();
        SpawnBoundsSize = new Vector3(MapOrigin.NPCSpawnBoundSize, 50f, MapOrigin.NPCSpawnBoundSize);
        VisibleLM = LayerMask.GetMask("Default", "NPC", "Pedestrian", "Obstacle");
        NPCSpawnCheckBitmask = LayerMask.GetMask("NPC", "Agent", "Pedestrian");
        CacheSpawnPoints();
        IsInitialized = true;
    }

    private void CacheSpawnPoints()
    {
        switch (spawnAreaType)
        {
            case SpawnAreaType.TrafficLanes:
                foreach (var lane in SimulatorManager.Instance.MapManager.trafficLanes)
                    CacheLane(lane);
                break;
            case SpawnAreaType.PedestrianLanes:
                foreach (var lane in SimulatorManager.Instance.MapManager.pedestrianLanes)
                    CacheLane(lane);
                break;
            case SpawnAreaType.ParkingSpaces:
                foreach (var lane in SimulatorManager.Instance.MapManager.parkingSpaces)
                    CacheParkingSpace(lane);
                break;
        }
        if (SpawnPoints.Count == 0)
        {
            Debug.LogWarning($"No valid spawn points were found on the {Loader.Instance.CurrentSimulation.Map.Name} map.");
        }
    }

    private void CacheLane(MapLane lane)
    {
        if (lane.DenySpawn)
            return;
        if (lane.mapWorldPositions.Count < 2)
            return;

        var spawnPoint = new SpawnPoint()
        {
            position = lane.mapWorldPositions[0],
            spawnIndex = SpawnPoints.Count,
            lookAtPoint = lane.mapWorldPositions[1],
            lane = lane
        };
        SpawnPoints.Add(spawnPoint);
    }

    private void CacheParkingSpace(MapParkingSpace space)
    {
        if (space.mapWorldPositions.Count < 4)
            return;

        var spawnPoint = new SpawnPoint()
        {
            position = space.Center,
            spawnIndex = SpawnPoints.Count,
            lookAtPoint = space.MiddleExit,
            lane = space
        };
        SpawnPoints.Add(spawnPoint);
    }

    public virtual SpawnPoint GetValidSpawnPoint(Bounds bounds, bool checkVisibility)
    {
        Initialize();
        if (SpawnPoints.Count == 0)
        {
            return null;
        }

        var startingIndex = CurrentIndex;
        SpawnPoint result = null;
        var delayLoop = false;
        var center = bounds.center;

        while (result == null)
        {
            //Wait if all spawn points have been checked
            if (delayLoop)
            {
                delayLoop = false;
                //await Task.Delay(100);
                //Asynchronous method should wait for simulation changes
                //Synchronous method failed to find proper spawn point
                FailedSpawnTime = Time.time;
                return null;
            }

            var spawnPoint = SpawnPoints[CurrentIndex];
            bounds.center = center+spawnPoint.position;
            CurrentIndex = (CurrentIndex + 1) % SpawnPoints.Count;

            if (CurrentIndex == startingIndex)
            {
                delayLoop = true;
            }

            switch (spawnAreaType)
            {
                case SpawnAreaType.TrafficLanes:
                    if (!MapOrigin.IgnoreNPCBounds) // set from map origin to ignore agent bounds checks
                    {
                        if (!WithinSpawnArea(spawnPoint.position))
                            continue;
                    }

                    if (!MapOrigin.IgnoreNPCSpawnable) // set from map origin to ignore spawnable checks
                    {
                        if (!spawnPoint.lane.Spawnable)
                            continue;
                    }

                    //if (!MapOrigin.IgnoreNPCVisible && checkVisibility) // set from map origin to ignore sensor visible check
                    //{
                    //    if (IsVisible(bounds))
                    //        continue;
                    //}
                    break;
                case SpawnAreaType.PedestrianLanes:
                    if (!MapOrigin.IgnorePedBounds)
                    {
                        if (!WithinSpawnArea(spawnPoint.position))
                            continue;
                    }

                    //if (!MapOrigin.IgnorePedVisible && checkVisibility) // set from map origin to ignore sensor visible check
                    //{
                    //    if (IsVisible(bounds))
                    //        continue;
                    //}
                    break;
            }

            if (Physics.CheckSphere(spawnPoint.position, spawnRadius, NPCSpawnCheckBitmask)) // TODO check box with npc bounds
                continue;

            result = spawnPoint;
        }

        return result;
    }

    public bool WithinSpawnArea(Vector3 pos)
    {
        var currentAgent = SimulatorManager.Instance.AgentManager.CurrentActiveAgent;
        var spawnT = currentAgent == null ? transform : currentAgent.transform;
        var spawnBounds = new Bounds(spawnT.position, SpawnBoundsSize);
        return spawnBounds.Contains(pos);
    }

    private void OnDrawGizmosSelected()
    {
        var spawnT = SimulatorManager.Instance.AgentManager.CurrentActiveAgent?.transform;
        spawnT = spawnT ?? transform;
        Gizmos.matrix = spawnT.localToWorldMatrix;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, SpawnBoundsSize);
    }

    public bool IsVisible(Bounds npcColliderBounds)
    {
        var activeAgents = SimulatorManager.Instance.AgentManager.ActiveAgents;
        var simCameraManager = SimulatorManager.Instance.CameraManager;

        if (simCameraManager.GetCurrentCameraState() == CameraStateType.Cinematic) // only check if in cinematic mode
        {
            var activeCameraPlanes = GeometryUtility.CalculateFrustumPlanes(simCameraManager.SimulatorCamera);
            if (GeometryUtility.TestPlanesAABB(activeCameraPlanes, npcColliderBounds) &&
                CheckVisibilityNonAlloc(simCameraManager.SimulatorCamera.transform.position,
                    npcColliderBounds, simCameraManager.SimulatorCamera.farClipPlane))
            {
                return true;
            }
        }

        foreach (var activeAgent in activeAgents)
        {
            var activeAgentController = activeAgent.AgentGO.GetComponent<IAgentController>();
            var agentTransform = activeAgentController.AgentGameObject.transform;
            if (CheckVisibilityNonAlloc(agentTransform.position, npcColliderBounds))
            {
                return true;
            }
        }
        return false;
    }
    
    public bool CheckVisibilityNonAlloc(Vector3 watcherPosition, Bounds checkedBounds, float visibilityDistanceLimit = SightDistanceLimit)
    {
        var ray = new Ray {origin = watcherPosition};
        var distance = Vector3.Distance(checkedBounds.center, ray.origin) + checkedBounds.extents.magnitude;
        if (distance > visibilityDistanceLimit)
        {
            return false;
        }

        var raycastStep = distance * Mathf.Tan(1.5f * Mathf.Deg2Rad);
        ray.direction = checkedBounds.center - ray.origin;
        if (RaycastNonAlloc(ray, distance))
        {
            return true;
        }

        //Check bounding box corners
        ray.direction = checkedBounds.min - ray.origin;
        if (RaycastNonAlloc(ray, distance))
        {
            return true;
        }

        ray.direction = checkedBounds.max - ray.origin;
        if (RaycastNonAlloc(ray, distance))
        {
            return true;
        }

        var directionMinX = checkedBounds.min.x - ray.origin.x;
        var directionMaxX = checkedBounds.max.x - ray.origin.x;
        var directionMinY = checkedBounds.min.y - ray.origin.y;
        var directionMaxY = checkedBounds.max.y - ray.origin.y;
        var directionMinZ = checkedBounds.min.z - ray.origin.z;
        var directionMaxZ = checkedBounds.max.z - ray.origin.z;

        ray.direction = new Vector3(directionMinX, directionMinY, directionMaxZ);
        if (RaycastNonAlloc(ray, distance))
        {
            return true;
        }
        ray.direction = new Vector3(directionMinX, directionMaxY, directionMinZ);
        if (RaycastNonAlloc(ray, distance))
        {
            return true;
        }
        ray.direction = new Vector3(directionMaxX, directionMinY, directionMinZ);
        if (RaycastNonAlloc(ray, distance))
        {
            return true;
        }
        ray.direction = new Vector3(directionMinX, directionMaxY, directionMaxZ);
        if (RaycastNonAlloc(ray, distance))
        {
            return true;
        }
        ray.direction = new Vector3(directionMaxX, directionMinY, directionMaxZ);
        if (RaycastNonAlloc(ray, distance))
        {
            return true;
        }
        ray.direction = new Vector3(directionMaxX, directionMaxY, directionMinZ);
        if (RaycastNonAlloc(ray, distance))
        {
            return true;
        }

        //Check bounding box interior
        for (var x = checkedBounds.min.x + raycastStep; x < checkedBounds.max.x; x += raycastStep)
        {
            for (var y = checkedBounds.min.y + raycastStep; y < checkedBounds.max.y; y += raycastStep)
            {
                for (var z = checkedBounds.min.z + raycastStep; z < checkedBounds.max.z; z += raycastStep)
                {
                    ray.direction = new Vector3(x - ray.origin.x, y - ray.origin.y, z - ray.origin.z);
                    if (RaycastNonAlloc(ray, distance))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool RaycastNonAlloc(Ray ray, float distance)
    {
        var hits = Physics.RaycastNonAlloc(ray, RaycastHits, distance, VisibleLM);
        if (hits == 0)
        {
            return true;
        }

        for (var i = 0; i < hits; i++)
        {
            if (RaycastHits[i].triangleIndex != -1)
            {
                return false;
            }
        }

        return false;
    }
}
