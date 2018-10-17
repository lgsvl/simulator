/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

public interface ITrafficSpawner
{
    void SetTrafficState(bool state);
    bool GetState();
}

public class TrafSpawner : UnitySingleton<TrafSpawner>, ITrafficSpawner
{
    static TrafSpawner instance;
    bool spawned = false;

    public TrafSystem system;
    public TrafPerformanceManager trafPerfManager;
    public TrafInfoManager trafInfoManager;

    public List<TrafficSpawnArea> trafficSpawnAreas = new List<TrafficSpawnArea>();

    public GameObject[] prefabs;
    public GameObject[] fixedPrefabs;

    public int spawnDensity = 150;

    const int maxIdent = 175;
    const int maxSub = 4;
    public float checkRadius = 6f;

    public int totalTrafficCarCount = 0;

    public int NPCSpawnCheckBitmask = -1;

    List<TrafEntry> utilEntryList = new List<TrafEntry>();

    protected override void Awake()
    {
        base.Awake();
        NPCSpawnCheckBitmask = 1 << LayerMask.NameToLayer("NPC") | 1 << LayerMask.NameToLayer("Duckiebot");
    }

    void Start()
    {
        if (trafPerfManager == null)
        {
            trafPerfManager = TrafPerformanceManager.Instance;
        }
        if (trafInfoManager == null)
        {
            trafInfoManager = TrafInfoManager.Instance;
        }
        //AdminManager.Instance.register(this);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            ReSpawnTrafficCars();
        }
        else if (Input.GetKeyDown(KeyCode.K))
        {
            SetTrafficState(false);
        }
    }

    public static bool CheckSilentRemoveEligibility(CarAIController car, Camera cam)
    {
        var distToCam = (car.transform.position - cam.transform.position).magnitude;
        if (distToCam > (TrafPerformanceManager.Instance.carRendDistanceThreshold + 25f) || (distToCam > 90f && car.allRenderers.All<Renderer>(r => !r.isVisible)))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public static bool CheckSilentRespawnEligibility_Old(CarAIController car, Camera cam)
    {
        var distToCam = (car.transform.position - cam.transform.position).magnitude;
        if (distToCam > (TrafPerformanceManager.Instance.carRendDistanceThreshold + 25f) || (distToCam > 90f && car.allRenderers.All<Renderer>(r => !r.isVisible)))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public static bool CheckSilentRespawnEligibility(CarAIController car, Camera cam)
    {
        var distToCam = (car.transform.position - cam.transform.position).magnitude;
        if (distToCam > (TrafPerformanceManager.Instance.carRendDistanceThreshold + 25f) || (distToCam > 90f && !TrafPerformanceManager.Instance.IsCarInMainView(car)))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void SpawnTrafficCars()
    {
        system.ResetIntersections();

        for (int i = 0; i < spawnDensity; i++)
        {
            SpawnRandom();
        }

        StartCoroutine(DelayPrintTrafficCarInfo(0.1f));
    }

    IEnumerator DelayPrintTrafficCarInfo(float t)
    {
        yield return new WaitForSeconds(t);
        Debug.Log("Number of traffic cars: " + totalTrafficCarCount);
    }

    //Spawn randomly
    public bool SpawnRandom(bool mustSpawn = false, bool unseenArea = false, CarAIController sameCar = null)
    {
        int id, subId;
        int tryCount = 0;
        GameObject go;
        do
        {
            id = Random.Range(0, maxIdent);
            subId = Random.Range(0, maxSub);
            go = Spawn(id, subId, unseenArea, sameCar);
            tryCount++;
        } while (go == null && mustSpawn && tryCount < 41);

        if (mustSpawn && tryCount > 40 && go == null)
        {
            Debug.Log("Run out of try counts to make a legit car spawn");
            return false;
        }
        else
        {
            return true;
        }
    }

    public bool SpawnInArea(bool mustSpawn = false, bool unseenArea = false, CarAIController sameCar = null)
    {
        if (trafficSpawnAreas.Count < 1)
        {
            return false;
        }

        utilEntryList.Clear();
        foreach (var entry in system.entries)
        {
            if (entry.waypoints.Count < 2)
            {
                continue;
            }

            var start = entry.waypoints[0];
            var end = entry.waypoints[entry.waypoints.Count - 1];
            var estAvgPoint = (start + end) * 0.5f;
            
            if (trafficSpawnAreas.Any(x => x.Contains(estAvgPoint)) || trafficSpawnAreas.Any(x => x.Contains(start)) || trafficSpawnAreas.Any(x => x.Contains(end)))
            {
                utilEntryList.Add(entry);
            }
        }

        int tryCount = 0;
        GameObject go;
        do
        {
            var e = utilEntryList[Random.Range(0, utilEntryList.Count)];
            go = Spawn(e, unseenArea, sameCar);
            tryCount++;
        } while (go == null && mustSpawn && tryCount < 41);

        if (mustSpawn && tryCount > 40 && go == null)
        {
            Debug.Log("Run out of try counts to make a legit car spawn within area, try random spawn");
            return SpawnRandom(mustSpawn, unseenArea, sameCar);
        }
        else
        {
            return true;
        }
    }

    public GameObject Spawn(TrafEntry entry, bool unseenArea = false, CarAIController sameCar = null)
    {
        if (entry == null)
            return null;

        float distance = Random.value * 0.8f + 0.1f;

        InterpolatedPosition pos = entry.GetInterpolatedPosition(distance);

        if (!Physics.CheckSphere(pos.position, checkRadius, NPCSpawnCheckBitmask))
        {
            GameObject go;

            if (sameCar != null) //If it is the same car simply reposition it
            {
                go = sameCar.gameObject;
                go.transform.position = pos.position + Vector3.up;
            }
            else
            {
                go = GameObject.Instantiate(prefabs[Random.Range(0, prefabs.Length)], pos.position + Vector3.up * 3.5f, Quaternion.identity) as GameObject;
            }

            go.transform.LookAt(entry.waypoints[pos.targetIndex]);
            go.transform.SetParent(this.transform); // Put all spawned cars under this object for better organization

            var carAI = go.GetComponent<CarAIController>();
            if (unseenArea)
            {
                if (!CheckSilentRespawnEligibility(carAI, Camera.main))
                {
                    if (sameCar == null)
                    { DestroyImmediate(go); }
                    return null;
                }
            }
            else if (sameCar == null) //If it is a new spawn
            {
                //assign userid
                if (trafInfoManager.freeIdPool.Count == 0)
                {
                    carAI.carID = trafInfoManager.GeneratePseudoRandomUID();
                }
                else
                {
                    carAI.carID = trafInfoManager.freeIdPool.Dequeue();
                }

                carAI.RandomSelectCarPaintTexture();

                ++totalTrafficCarCount;
                if (trafPerfManager != null)
                    trafPerfManager.AddAICar(carAI);
            }

            // Init or Reinit AI motor
            TrafAIMotor motor = go.GetComponent<TrafAIMotor>();
            motor.system = system;
            motor.Init(pos.targetIndex, entry);

            // Init or Reinit AI Controller
            carAI.Init();
            return go;
        }
        else
        { return null; }
    }

    public GameObject Spawn(int id, int subId, bool unseenArea = false, CarAIController sameCar = null)
    {
        TrafEntry entry = system.GetEntry(id, subId);
        return Spawn(entry, unseenArea, sameCar);     
    }

    public bool GetState()
    {
        return spawned;
    }

    public void ReSpawnTrafficCars()
    {
        SetTrafficState(false);
        SetTrafficState(true);
    }

    public void KillTrafficCars()
    {
        var set = trafPerfManager.GetCarSet();
        if (set == null)
        { return; }

        foreach (CarAIController carAI in set)
        { GameObject.Destroy(carAI.gameObject); }
    }

    public void SetTrafficState(bool state)
    {
        if(spawned && !state)
        {
            KillTrafficCars();
            spawned = false;
        }
        else if(!spawned && state)
        {
            SpawnTrafficCars();
            spawned = true;
        }
    }
}
