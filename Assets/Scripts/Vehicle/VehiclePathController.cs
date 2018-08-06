/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;



public class VehiclePathController : MonoBehaviour
{
    public GameObject[] path;
    private int currentWaypointIndex;
    private Vector3 currentWaypoint;

    public float waypointThreshold = 3f;
    public float maxSpeed = 20f;
    public float brakeDistance = 15f;
    public float minBrakeDist = 1.5f;

    public float maxThrottle = 0.8f;
    public float maxBrake = 0.4f;
    public float steerSpeed = 4.0f;
    public float throttleSpeed = 3.0f;
    public float brakeSpeed = 1f;
    private float m_targetSteer = 0.0f;
    private float m_targetThrottle = 0.0f;
    private bool stopped = false;

    private float targetSpeed = 0f;
    private float currentThrottle = 0f;
    public GameObject final;

    private float maxSteer;

    public bool straightAtFinal = true;

    private VehicleController control;

    protected void OnEnable()
    {
        control = GetComponent<VehicleController>();
        maxSteer = control.maxSteeringAngle;
        control.maxSteeringAngle = 60f;

        currentWaypointIndex = 1;
        final = path[path.Length - 1];

        currentWaypoint = path[0].transform.position;
        stopped = false;

        targetSpeed = maxSpeed;
        currentThrottle = 0f;

    }

    public System.Action onStop;



    void Update()
    {

        if (Vector3.Distance(currentWaypoint, transform.position) < waypointThreshold && currentWaypointIndex < path.Length - 1)
        {
            if (++currentWaypointIndex < path.Length - 1)
            {
                currentWaypoint = path[currentWaypointIndex].transform.position;
            }
            else
            {
                currentWaypoint = final.transform.position;
            }

        }
        

        Vector3 steerVector = new Vector3(currentWaypoint.x, transform.position.y, currentWaypoint.z) - transform.position;
        float steer = Vector3.Angle(transform.forward, steerVector);
        m_targetSteer = (Vector3.Cross(transform.forward, steerVector).y < 0 ? -steer : steer) / control.maxSteeringAngle;
        
        // Apply the input progressively



        if (currentWaypoint == final.transform.position)
        {
            Vector3 targetVec = final.transform.position - transform.position;
            if (Vector3.Dot(targetVec, transform.forward) > 0f)
            {
                targetSpeed = Mathf.Clamp01(targetVec.magnitude / brakeDistance) * maxSpeed;
            }
            else
            {
                targetSpeed = 0f;
                if (straightAtFinal)
                    m_targetSteer = 0f;
                if (!stopped && GetComponent<Rigidbody>().velocity.magnitude < 0.04f)
                {
                    //Debug.Log("ONSTOP");
                    stopped = true;
                    onStop();
                }
            }
        }

        float speedDifference = targetSpeed - GetComponent<Rigidbody>().velocity.magnitude;

        if (currentWaypoint == final.transform.position && Vector3.Distance(transform.position, final.transform.position) < minBrakeDist)
        {
            // var brakeRatio = Mathf.Clamp01((Vector3.Distance(transform.position, final.transform.position) - minBrakeDist) / (brakeDistance - minBrakeDist));
            // brakeRatio = 1 - brakeRatio;
            m_targetThrottle = 0f;
            control.accellInput = 0f;
            m_targetSteer = 0f;
            if (!stopped && GetComponent<Rigidbody>().velocity.magnitude < 0.04f)
            {
                // Debug.Log("ONSTOP");
                stopped = true;
                onStop();
            }

        }
        else if (speedDifference < 0)
        {
            //m_targetThrottle = 0f;
            control.accellInput = 0f;
        }
        else
        {
            m_targetThrottle = maxThrottle * Mathf.Clamp(1 - Mathf.Abs(m_targetSteer), 0.7f, 1f);
        }


        currentThrottle = Mathf.MoveTowards(currentThrottle, speedDifference, throttleSpeed * Time.deltaTime);

        control.steerInput = Mathf.MoveTowards(control.steerInput, m_targetSteer, steerSpeed * Time.deltaTime);
     
        control.accellInput = Mathf.Clamp(currentThrottle, -maxBrake, m_targetThrottle);



    }


    public void Clear()
    {
        control.steerInput = 0f;
        control.accellInput = 0f;
        control.maxSteeringAngle = maxSteer;
    }

}
