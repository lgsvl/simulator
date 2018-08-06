/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;

public class TrafficLightStop : MonoBehaviour {

    public Collider trigger;

    public event System.Action OnRelease;

	// Use this for initialization
	void Start () {
        trigger = GetComponent<Collider>();
	}

    public void Release()
    {
        trigger.enabled = false;
        if(OnRelease != null)
            OnRelease();

        OnRelease = null;

    }

}
