/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class TrafficLightSet
{
    public List<TrafficLightContainer> lights;
    public float activeTime = 15f;
}

public class TrafficLight : MonoBehaviour {

    public List<TrafficLightSet> lightSets;
    public float yellowTime = 3f;
    public float allRedTime = 1.5f;

    public GameObject Stop;

    private int currentState = 0;

    void Start()
    {
        if (lightSets.Count > 0)
        {
            StartCoroutine(LoopLight());
        }
    }

    IEnumerator LoopLight() {
        yield return new WaitForSeconds(Random.Range(0, 5f));
        while(true)
        {

            yield return null;
            TrafficLightSet currentSet = lightSets[currentState++];
            if (currentState >= lightSets.Count)
                currentState = 0;
             
            foreach (var state in currentSet.lights)
            {
                state.Set(TrafLightState.GREEN);
            }

            
            yield return new WaitForSeconds(currentSet.activeTime);

            foreach (var state in currentSet.lights)
            {
                state.Set(TrafLightState.YELLOW);
            }

            yield return new WaitForSeconds(yellowTime);

            foreach (var state in currentSet.lights)
            {
                state.Set(TrafLightState.RED);
            }

            yield return new WaitForSeconds(allRedTime);

        }
	}
}

