/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;
using Simulator.Map;
using UnityEngine.AI;

public class PedestrianManager : MonoBehaviour
{
    public GameObject pedPrefab;
    public List<GameObject> pedModels = new List<GameObject>();
    private bool _pedestriansActive = false;
    public bool PedestriansActive
    {
        get => _pedestriansActive;
        set
        {
            _pedestriansActive = value;
            TogglePedestrians();
        }
    } 
    public enum PedestrianVolume { LOW = 50, MED = 25, HIGH = 10 };
    public PedestrianVolume pedVolume = PedestrianVolume.LOW;

    [HideInInspector]
    public List<MapPedestrian> pedPaths = new List<MapPedestrian>();
    private List<GameObject> pedPool = new List<GameObject>();
    private List<GameObject> pedActive = new List<GameObject>();

    private void Start()
    {
        InitPedestrians();
    }

    private void InitPedestrians()
    {
        pedPaths.Clear();
        pedPool.Clear();
        pedPaths = new List<MapPedestrian>(FindObjectsOfType<MapPedestrian>());
        for (int i = 0; i < pedPaths.Count; i++)
        {
            foreach (var localPos in pedPaths[i].mapLocalPositions)
                pedPaths[i].mapWorldPositions.Add(pedPaths[i].transform.TransformPoint(localPos)); //Convert ped segment local to world position
            
            pedPaths[i].PedVolume = Mathf.CeilToInt(Vector3.Distance(pedPaths[i].mapWorldPositions[0], pedPaths[i].mapWorldPositions[pedPaths[i].mapWorldPositions.Count - 1]) / (int)pedVolume);

            Debug.Assert(pedPrefab != null && pedModels != null && pedModels.Count != 0);
            pedPrefab.GetComponent<NavMeshAgent>().enabled = false; // disable to prevent warning issues parenting nav agent
            for (int j = 0; j < pedPaths[i].PedVolume; j++)
            {
                GameObject ped = Instantiate(pedPrefab, Vector3.zero, Quaternion.identity, transform);
                pedPool.Add(ped);
                Instantiate(pedModels[(int)Random.Range(0, pedModels.Count)], ped.transform);
                ped.SetActive(false);
            }
        }
    }

    private void TogglePedestrians()
    {
        if (pedPaths == null || pedPaths.Count == 0) return;

        if (PedestriansActive)
        {
            for (int i = 0; i < pedPaths.Count; i++)
            {
                for (int j = 0; j < pedPaths[i].PedVolume; j++)
                    SpawnPedestrian(pedPaths[i]);
            }
        }
        else
        {
            List<PedestrianController> peds = new List<PedestrianController>(FindObjectsOfType<PedestrianController>()); // search to prevent missed peds
            for (int i = 0; i < peds.Count; i++)
                ReturnPedestrianToPool(peds[i].gameObject);

            pedActive.Clear();
        }
    }

    private void SpawnPedestrian(MapPedestrian path)
    {
        if (pedPool.Count == 0) return;

        GameObject ped = pedPool[0];
        ped.transform.SetParent(path.transform);
        pedPool.RemoveAt(0);
        pedActive.Add(ped);
        ped.SetActive(true);
        PedestrianController pedC = ped.GetComponent<PedestrianController>();
        if (pedC != null)
            pedC.InitPed(path.mapWorldPositions);
    }

    private void ReturnPedestrianToPool(GameObject go)
    {
        go.transform.SetParent(transform);
        go.SetActive(false);
        pedActive.Remove(go);
        pedPool.Add(go);
    }

    // api
    public GameObject SpawnPedestrianApi(string name, Vector3 position, Quaternion rotation)
    {
        var prefab = pedModels.Find(obj => obj.name == name);
        if (prefab == null)
        {
            return null;
        }

        GameObject ped = Instantiate(pedPrefab, Vector3.zero, Quaternion.identity, transform);
        Instantiate(prefab, ped.transform);
        ped.GetComponent<PedestrianController>().InitManual(position, rotation);
        ped.GetComponent<NavMeshAgent>().enabled = true;
        return ped;
    }

    public void DespawnPedestrianApi(PedestrianController ped)
    {
        Destroy(ped.gameObject);
    }
}
