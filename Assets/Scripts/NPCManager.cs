/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NPCManager : MonoBehaviour
{
    #region Singleton
    private static NPCManager _instance = null;
    public static NPCManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<NPCManager>();
                if (_instance == null)
                    Debug.LogError("<color=red>NPCManager Not Found!</color>");
            }
            return _instance;
        }
    }
    #endregion

    #region util
    private NPCControllerComponent npcControllerComponent;
    private int NPCSpawnCheckBitmask = -1;
    private float checkRadius = 6f;
    private Collider[] collidersBuffer = new Collider[1];
    private MapLaneSegmentBuilder seg;
    private Bounds npcColliderBounds;
    private Plane[] activeCameraPlanes = new Plane[] { };
    private Camera activeCamera;
    #endregion

    #region vars
    // npc
    public bool isSpawnAreaVisible = false;
    public bool isSpawnAreaLimited = true;
    public bool isSimplePhysics = false;
    public GameObject wheelColliderPrefab;
    public Vector3 spawnArea = Vector3.zero;
    public float despawnDistance = 300f;
    private Bounds spawnBounds = new Bounds();
    private Color spawnColor = Color.magenta;
    private Vector3 spawnPos;
    private Transform spawnT;

    public GameObject npcPrefab;
    public List<GameObject> npcVehicles = new List<GameObject>();
    public int npcCount = 0;
    private int activeNPCCount = 0;
    [HideInInspector]
    public List<GameObject> currentPooledNPCs = new List<GameObject>();
    [HideInInspector]
    public bool isInit = false;
    private bool isNPCActive = false;
    #endregion

    #region mono
    private void Awake()
    {
        if (_instance == null)
            _instance = this;

        if (_instance != this)
            DestroyImmediate(gameObject);

        if (spawnT == null)
            spawnT = transform;

        if (activeCamera == null)
            activeCamera = Camera.main;
    }

    private IEnumerator Start()
    {
        if (MapManager.Instance == null) yield break;

        while (!MapManager.Instance.isInit) yield return null;

        NPCSpawnCheckBitmask = 1 << LayerMask.NameToLayer("NPC") | 1 << LayerMask.NameToLayer("Duckiebot");

        SpawnNPCPool();
        isInit = true;
    }

    private void Update()
    {
        if (isNPCActive)
        {
            if (activeNPCCount < npcCount)
                SetNPCOnMap();
        }
        else
        {
            DespawnAllNPC();
        }
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }
    #endregion

    #region npc
    private void SpawnNPCPool()
    {
        for (int i = 0; i < currentPooledNPCs.Count; i++)
        {
            Destroy(currentPooledNPCs[i]);
        }
        currentPooledNPCs.Clear();
        activeNPCCount = 0;

        int poolCount = Mathf.FloorToInt(npcCount + (npcCount * 0.1f));
        for (int i = 0; i < poolCount; i++)
        {
            GameObject go = Instantiate(npcPrefab, transform);
            go.name = Instantiate(npcVehicles[RandomIndex(npcVehicles.Count)], go.transform).name;
            string genId = System.Guid.NewGuid().ToString();
            npcControllerComponent = go.GetComponent<NPCControllerComponent>();
            npcControllerComponent.id = genId;
            npcControllerComponent.Init();
            go.name = go.name + genId;
            currentPooledNPCs.Add(go);
            go.SetActive(false);
        }
        SegmentationManager.Instance.OverrideMaterialsNPCsSpawned(currentPooledNPCs);
    }

    private void SetNPCOnMap()
    {
        for (int i = 0; i < currentPooledNPCs.Count; i++)
        {
            if (!currentPooledNPCs[i].activeInHierarchy)
            {
                seg = MapManager.Instance.GetRandomLane();

                if (seg.segment.targetWorldPositions == null || seg.segment.targetWorldPositions.Count == 0)
                    continue;

                if (seg.segment.targetWorldPositions.Count < 2)
                    continue;

                var start = seg.segment.targetWorldPositions[0];
                //var end = seg.segment.targetWorldPositions[seg.segment.targetWorldPositions.Count - 1];
                //var estAvgPoint = (start + end) * 0.5f;

                if (isSpawnAreaLimited)
                {
                    if (IsPositionWithinSpawnArea(start)) // || IsPositionWithinSpawnArea(estAvgPoint) || IsPositionWithinSpawnArea(end))
                    {
                        if (!Physics.CheckSphere(seg.segment.targetWorldPositions[0], checkRadius, NPCSpawnCheckBitmask))
                        {
                            spawnPos = seg.segment.targetWorldPositions[0];
                            currentPooledNPCs[i].transform.position = spawnPos;
                            if (!IsVisible(currentPooledNPCs[i]))
                            {
                                currentPooledNPCs[i].GetComponent<NPCControllerComponent>().InitLaneData(seg);
                                currentPooledNPCs[i].SetActive(true);
                                currentPooledNPCs[i].transform.LookAt(seg.segment.targetWorldPositions[1]); // TODO check if index 1 is valid
                                activeNPCCount++;
                            }
                            else
                            {
                                currentPooledNPCs[i].transform.position = transform.position;
                                currentPooledNPCs[i].transform.rotation = Quaternion.identity;
                                currentPooledNPCs[i].SetActive(false);
                            }
                        }
                    }
                }
                else
                {
                    if (!Physics.CheckSphere(seg.segment.targetWorldPositions[0], checkRadius, NPCSpawnCheckBitmask))
                    {
                        spawnPos = seg.segment.targetWorldPositions[0];
                        currentPooledNPCs[i].transform.position = spawnPos;
                        currentPooledNPCs[i].GetComponent<NPCControllerComponent>().InitLaneData(seg);
                        currentPooledNPCs[i].SetActive(true);
                        currentPooledNPCs[i].transform.LookAt(seg.segment.targetWorldPositions[1]); // TODO check if index 1 is valid
                        activeNPCCount++;
                    }
                }
            }
        }
    }

    public Transform GetRandomActiveNPC()
    {
        if (currentPooledNPCs.Count == 0) return transform;

        int index = (int)Random.Range(0, currentPooledNPCs.Count);
        while (!currentPooledNPCs[index].activeInHierarchy)
        {
            index = (int)Random.Range(0, currentPooledNPCs.Count);
        }
        return currentPooledNPCs[index].transform;
    }

    public void DespawnNPC(GameObject npc)
    {
        activeNPCCount--;
        npc.SetActive(false);
        npc.transform.position = transform.position;
        npc.transform.rotation = Quaternion.identity;
    }

    private void DespawnAllNPC()
    {
        if (activeNPCCount == 0) return;
        StopAllCoroutines();

        for (int i = 0; i < currentPooledNPCs.Count; i++)
        {
            DespawnNPC(currentPooledNPCs[i]);
            var intersections = FindObjectsOfType<MapIntersectionBuilder>().ToList();
            foreach (var item in intersections)
                item.stopQueue.Clear();
        }
        activeNPCCount = 0;
    }

    public void ToggleNPCS(bool state)
    {
        if (state)
        {
            isNPCActive = false;
            DespawnAllNPC();
            isNPCActive = true;
        }
        else
        {
            isNPCActive = false;
        }
    }

    public void ToggleNPCPhysicsMode(bool state)
    {
        isSimplePhysics = !state;
    }
    #endregion

    #region utilities
    private int RandomIndex(int max = 1)
    {
        return (int)Random.Range(0, max);
    }

    public bool IsPositionWithinSpawnArea(Vector3 pos)
    {
        Transform tempT = ROSAgentManager.Instance?.GetCurrentActiveAgent()?.transform;
        if (tempT != null)
            spawnT = tempT;

        spawnBounds = new Bounds(spawnT.position, spawnArea);
        if (spawnBounds.Contains(pos))
            return true;
        else
            return false;
    }

    private void DrawSpawnArea()
    {
        Transform tempT = ROSAgentManager.Instance?.GetCurrentActiveAgent()?.transform;
        if (tempT != null)
            spawnT = tempT;
        Gizmos.matrix = spawnT.localToWorldMatrix;
        Gizmos.color = spawnColor;
        Gizmos.DrawWireCube(Vector3.zero, spawnArea);
    }

    private void OnDrawGizmosSelected()
    {
        if (!isSpawnAreaVisible) return;
        DrawSpawnArea();
    }

    public bool IsVisible(GameObject npc)
    {
        Camera tempCam = ROSAgentManager.Instance?.GetCurrentActiveAgent().GetComponent<AgentSetup>()?.mainCamera;
        if (tempCam != null)
            activeCamera = tempCam;
        npcColliderBounds = npc.GetComponent<Collider>().bounds;
        activeCameraPlanes = GeometryUtility.CalculateFrustumPlanes(activeCamera);
        if (GeometryUtility.TestPlanesAABB(activeCameraPlanes, npcColliderBounds))
            return true;
        else
            return false;
    }
    #endregion
}