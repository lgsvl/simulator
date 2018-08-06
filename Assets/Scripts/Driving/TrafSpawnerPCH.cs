/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;

public class TrafSpawnerPCH : MonoBehaviour, ITrafficSpawner
{

    public CarAutoPath path;

    public GameObject[] prefabs;

    public int numberToSpawn = 100;

    public int maxSteps = 1000;

    public float checkRadius = 8f;

    public void SpawnHeaps()
    {
        for (int i = 0; i < numberToSpawn; i++)
        {
            Spawn();
        }
    }


    private void UpdateNextWaypoint()
    {

    } 

    public void Spawn()
    {

        int spot = Random.Range(1, maxSteps);
        
        int currentIndex = spot;
        RoadPathNode currentNode = path.pathNodes[spot];


        Vector3 position = currentNode.position;


        if (!Physics.CheckSphere(position, checkRadius, 1 << LayerMask.NameToLayer("NPC")))
        {
            GameObject go = GameObject.Instantiate(prefabs[Random.Range(0, prefabs.Length)], position, Quaternion.identity) as GameObject;

            currentNode = path.pathNodes[currentIndex++];

            var motor = go.GetComponent<TrafPCH>();
            motor.currentNode = currentNode;
            motor.currentWaypointIndex = currentIndex;
//            Debug.Log("IDX " + currentIndex);
            motor.path = path;
            go.transform.LookAt(currentNode.position);
            motor.Init();
        }
        else
        {
//            Debug.Log("taken: " + position);
        }
    }

    public void Kill()
    {
        var allTraffic = Object.FindObjectsOfType(typeof(TrafPCH)) as TrafPCH[];
        foreach (var t in allTraffic)
        {
            GameObject.Destroy(t.gameObject);
        }

    }

    bool spawned = false;

    public bool GetState()
    {
        return spawned;
    }

    public void SetTraffic(bool state)
    {
        if (spawned && !state)
        {
            Kill();
            spawned = false;
        }
        else if (!spawned && state)
        {
            SpawnHeaps();
            spawned = true;
        }
    }

    void OnGUI()
    {
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.U)
        {
            if (spawned)
                Kill();
            else
                SpawnHeaps();

            spawned = !spawned;
        }

    }

}
