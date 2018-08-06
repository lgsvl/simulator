/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;

public class TrafficLightState : MonoBehaviour {

    public GameObject red;
    public GameObject yellow;
    public GameObject green;
    public TrafficLightStop stop;

	// Use this for initialization
	void Start () {
        red = transform.Find("red").gameObject;
        green = transform.Find("green").gameObject;
        yellow = transform.Find("yellow").gameObject;
        stop = GetComponentInChildren<TrafficLightStop>();


        red.SetActive(true);
        if(stop != null)
            stop.GetComponent<Collider>().enabled = true;
        yellow.SetActive(false);
        green.SetActive(false);
	}

    public void SetRed()
    {
        if(stop != null)
            stop.trigger.enabled = true;
        red.SetActive(true);
        yellow.SetActive(false);
        green.SetActive(false);
    }

    public void SetYellow()
    {
        if(stop != null)
            stop.trigger.enabled = true;
        red.SetActive(false);
        yellow.SetActive(true);
        green.SetActive(false);
    }

    public void SetGreen()
    {
        if(stop != null)
        {
            stop.trigger.enabled = false;
            stop.Release();
        }
        red.SetActive(false);
        yellow.SetActive(false);
        green.SetActive(true);
    }

}
