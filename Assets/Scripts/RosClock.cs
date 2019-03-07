/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

public class RosClock : MonoBehaviour, Ros.IRosClient
{
    Ros.Bridge Bridge;
    public string SimulatorTopic = "/clock";
    private volatile bool is_connected = false;

    public void OnRosBridgeAvailable(Ros.Bridge bridge)
    {
        Debug.Log("rosbridge for /clock available");
        Bridge = bridge;
        Bridge.AddPublisher(this);
    }

    // Use this for initialization
    void Start ()
    {

    }
	
	// Update is called once per frame
	void Update ()
    {

    }

    // Update is called in fixed Rate
    void FixedUpdate()
    {
        if (is_connected)
        {
            var clock_msg = new Ros.Clock();
            clock_msg.clock = Ros.Time.Now();
            Bridge.Publish(SimulatorTopic, clock_msg);
        }
    }

    public void OnRosConnected()
    {
        Bridge.AddPublisher<Ros.Clock>(SimulatorTopic);
        is_connected = true;
    }
}
