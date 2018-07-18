/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;

public enum TrafLightState { RED, GREEN, YELLOW, NONE}

public class TrafficLightContainer : MonoBehaviour {
    public TrafLightState currentState = TrafLightState.RED;

    public TrafLightState State
    {
        get
        {
            return currentState;           
        }
    }

    void Awake()
    {
        currentState = TrafLightState.RED;
    }

    public void Set(TrafLightState state)
    {
        currentState = state;
        foreach(var light in GetComponentsInChildren<TrafficLightVisuals>())
        {
            light.Set(state);
        }
    }
}
