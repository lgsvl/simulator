/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;
using System.Linq;

public interface ITrafficSpawner
{
    void SetTraffic(bool state);
    bool GetState();
}

public class TrafSpawner : MonoBehaviour, ITrafficSpawner {

    static TrafSpawner instance;

    public TrafSystem system;
    public TrafPerformanceManager perfManager;
    public TrafNetworkManager trafNetManager;

    public GameObject[] prefabs;
    public GameObject[] fixedPrefabs;

    public int numberToSpawn = 50;

    public int lowDensity = 120;
    public int mediumDensity = 250;
    public const int heavyDensity = 500;

    public int maxIdent = 20;
    public int maxSub = 4;
    public float checkRadius = 6f;

    public int totalTrafficCarCount = 0;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    void Start()
    {
        if (perfManager == null)
        {
            perfManager = TrafPerformanceManager.GetInstance();
        }
        if (trafNetManager == null)
        {
            trafNetManager = TrafNetworkManager.GetInstance<TrafNetworkManager>();
        }
        //AdminManager.Instance.register(this);
    }

    public static TrafSpawner GetInstance()
    {
        return instance;
    }

    public static bool CheckSilentRespawnEligibility(CarAIController car, Camera cam)
    {
        var camToPosDist = (car.transform.position - cam.transform.position).magnitude;
        if (camToPosDist > 325f || camToPosDist > 90f && car.allRenderers.All<Renderer>(r => !r.isVisible))
        { return true; }
        else
        { return false; }
    }

    public void SpawnHeaps()
    {
        system.ResetIntersections();

        for (int i = 0; i < numberToSpawn; i++)
        {
            Spawn();
        }

        StartCoroutine(DelayPrintTrafficCarInfo(0.1f));
    }

    IEnumerator DelayPrintTrafficCarInfo(float t)
    {
        yield return new WaitForSeconds(t);
        Debug.Log("Number of traffic cars: " + totalTrafficCarCount);
    }

    //Spawn randomly
    public bool Spawn(bool mustSpawn = false, bool unseenArea = false, CarAIController sameCar = null)
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
        } while (go == null && mustSpawn && tryCount < 101);

        if (mustSpawn && tryCount > 100 && go == null)
        {
            Debug.Log("Run out of try counts to make a legit car spawn");
            return false;
        }
        else
        {
            return true;
        }
    }

    public GameObject Spawn(int id, int subId, bool unseenArea = false, CarAIController sameCar = null)
    {
        float distance = Random.value * 0.8f + 0.1f;
        TrafEntry entry = system.GetEntry(id, subId);

        if (entry == null)
            return null;

        InterpolatedPosition pos = entry.GetInterpolatedPosition(distance);

        if (!Physics.CheckSphere(pos.position, checkRadius, 1 << LayerMask.NameToLayer("NPC")))
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

            if (sameCar == null) //If it is a new spawn
            {
                //assign userid
                if (trafNetManager.freeIdPool.Count == 0)
                {
                    carAI.carID = trafNetManager.GeneratePseudoRandomUID();
                }
                else
                {
                    carAI.carID = trafNetManager.freeIdPool.Dequeue();
                }

                carAI.RandomSelectCarPaintTexture();

                ++totalTrafficCarCount;
                if (perfManager != null)
                    perfManager.AddAICar(carAI);
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

    public void KillTrafficCars()
    {
        var set = TrafPerformanceManager.GetInstance().GetCarSet();
        if (set == null)
        { return; }

        foreach (CarAIController carAI in set)
        { GameObject.Destroy(carAI.gameObject); }
    }

    public void ReSpawnTrafficCars()
    {
        if (spawned)
        { KillTrafficCars(); }
        ReSpawnTrafficCarsHigh(true);
    }

    private void ReSpawnTrafficCarsHigh(bool delayExecute = false)
    {
        if (delayExecute)
        {
            StartCoroutine(SpawnTrafficCarsHigh_());
        }
        else
        {
            numberToSpawn = heavyDensity;
            SpawnHeaps();
            spawned = true;
        }
    }

    private void SpawnTrafficCarsMedium(bool delayExecute = false)
    {
        if (delayExecute)
        {
            StartCoroutine(SpawnTrafficCarsMedium_());
        }
        else
        {
            numberToSpawn = mediumDensity;
            SpawnHeaps();
            spawned = true;
        }
    }

    private void SpawnTrafficCarsLow(bool delayExecute = false)
    {
        if (delayExecute)
        {
            StartCoroutine(SpawnTrafficCarsLow_());
        }
        else
        {
            numberToSpawn = lowDensity;
            SpawnHeaps();
            spawned = true;
        }
    }

    private IEnumerator SpawnTrafficCarsHigh_()
    {
        yield return null;
        numberToSpawn = heavyDensity;
        SpawnHeaps();
        spawned = true;
    }

    private IEnumerator SpawnTrafficCarsMedium_()
    {
        yield return null;
        numberToSpawn = mediumDensity;
        SpawnHeaps();
        spawned = true;
    }

    private IEnumerator SpawnTrafficCarsLow_()
    {
        yield return null;
        numberToSpawn = lowDensity;
        SpawnHeaps();
        spawned = true;
    }

    bool spawned = false;

    public bool GetState()
    {
        return spawned;
    }

    public void SetTraffic(bool state)
    {
        if(spawned && !state)
        {
            KillTrafficCars();
            spawned = false;
        }
        else if(!spawned && state)
        {
            SpawnHeaps();
            spawned = true;
        }
    }

    void OnGUI()
    {
        if(Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.U && Event.current.modifiers == EventModifiers.Shift)
        {
            numberToSpawn = mediumDensity;
            if (spawned)
                KillTrafficCars();
            else
            {
                //close to initial car position
                Spawn(18, 0);
                Spawn(18, 0);
                Spawn(18, 0);
            }

            spawned = !spawned;
        }
        else if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.U)
        {
            if (spawned)
            { KillTrafficCars(); }
            SpawnTrafficCarsMedium();
        }
        else if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.H)
        {
            if (spawned)
            { KillTrafficCars(); }
            ReSpawnTrafficCarsHigh(true);
        }

        else if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.L)
        {
            if (spawned)
            { KillTrafficCars(); }
            SpawnTrafficCarsLow();
        }
    }
}
