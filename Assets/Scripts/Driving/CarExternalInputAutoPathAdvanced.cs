/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CarExternalInputAutoPathAdvanced : MonoBehaviour
{
    public Transform raycastOrigin;
    public CarAutoPath path;
    private int currentWaypointIndex;
    private RoadPathNode currentNode;
    private Vector3 currentWaypoint;
    private RoadPathNode nextNode;
    private int nextWaypointIndex;
    private Vector3 nextWaypoint;

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

    private float targetSpeed = 0f;
    private float currentThrottle = 0f;

    private float maxSteer;

    public float steerTargetDist = 16f;

    public bool reverse = false;

    public bool backArc = false;

    public bool straightAtFinal = true;


    public float predictLength = 10f;
    public float normalAdd = 5f;
    public float pathRadius = 1f;

    private VehicleController vehicleController;

    public void Init()
    {
        vehicleController = GetComponent<VehicleController>();

        maxSteer = vehicleController.maxSteeringAngle;
        vehicleController.maxSteeringAngle = 45f;

        currentWaypointIndex = 0;
        currentNode = path.pathNodes[currentWaypointIndex++];
        currentWaypoint = currentNode.position;

        UpdateNextWaypoint();


        targetSpeed = maxSpeed;
        currentThrottle = 0f;
    }

    private void UpdateNextWaypoint()
    {
        nextWaypointIndex = currentWaypointIndex + 1;
        if (nextWaypointIndex >= path.pathNodes.Count)
            nextWaypointIndex = 0;

        nextNode = path.pathNodes[nextWaypointIndex];
        nextWaypoint = nextNode.position;

    }

    private Vector3 GetPredictedPoint()
    {
        return transform.position + GetComponent<Rigidbody>().velocity.normalized * predictLength; 
    }


    private Vector3 GetNormalPoint(Vector3 predicted, Vector3 A, Vector3 B)
    {
        Vector3 ap = predicted - A;
        Vector3 ab = (B - A).normalized;

        return A + (ab * Vector3.Dot(ap, ab)) + ab * normalAdd ; 

    }


    Vector3 seek(Vector3 target, Vector3 location)
    {
        Vector3 desired = (target - location).normalized;
        return desired * maxSpeed;
    }

    void Update()
    {

        var predicted = GetPredictedPoint();

        var normal = GetNormalPoint(predicted, currentWaypoint, nextWaypoint);
        

        //check if we are heading past the current waypoint
        if (Vector3.Dot(normal - nextWaypoint, nextWaypoint - currentWaypoint) >= 0)
        {
            currentWaypoint = nextWaypoint;
            currentNode = nextNode;
            currentWaypointIndex = nextWaypointIndex;

            UpdateNextWaypoint();

            predicted = GetPredictedPoint();
            normal = GetNormalPoint(predicted, currentWaypoint, nextWaypoint);
        }





        if (currentNode.isInintersection)
            targetSpeed = 4f;
        else
            targetSpeed = maxSpeed;

        Vector3 steerVector = new Vector3(normal.x, transform.position.y, normal.z) - transform.position;

        //Vector3 desired = seek(new Vector3(normal.x, transform.position.y, normal.z), transform.position);
        //Vector3 steers = desired -  rigidbody.velocity;
        //steers.limit(maxforce);
        //applyForce(steers);

        float steer = Vector3.Angle(transform.forward, steerVector);
        m_targetSteer = (Vector3.Cross(transform.forward, steerVector).y < 0 ? -steer : steer) / vehicleController.maxSteeringAngle;

        if (Vector3.Distance(predicted, normal) < pathRadius)
            m_targetSteer = 0f;

        float speedDifference = targetSpeed - GetComponent<Rigidbody>().velocity.magnitude;

        if (speedDifference < 0)
        {
            //m_targetThrottle = 0f;
            //  m_CarControl.motorInput = 0f;
            // m_targetBrake = (rigidbody.velocity.magnitude / targetSpeed) * maxBrake;
        }
        else
        {
            m_targetThrottle = maxThrottle * Mathf.Clamp(1 - Mathf.Abs(m_targetSteer), 0.2f, 1f);
            speedDifference *= m_targetThrottle;
        }

        
        currentThrottle = Mathf.Clamp(Mathf.MoveTowards(currentThrottle, speedDifference/2f, throttleSpeed * Time.deltaTime), -maxBrake, maxThrottle);

        // m_CarControl.steerInput = Mathf.MoveTowards(m_CarControl.steerInput, m_targetSteer, steerSpeed * Time.deltaTime);
        vehicleController.steerInput = Mathf.Lerp(vehicleController.steerInput, m_targetSteer, steerSpeed * Time.deltaTime);
        vehicleController.accellInput = currentThrottle;
        //vehicleController.motorInput = Mathf.Clamp(currentThrottle, 0f, m_targetThrottle);
        //vehicleController.brakeInput = Mathf.Abs(Mathf.Clamp(currentThrottle, -maxBrake, 0f));



    }


    public void OnDisable()
    {
        vehicleController.steerInput = 0f;
        vehicleController.accellInput = 0f;
        vehicleController.maxSteeringAngle = maxSteer;

        GetComponent<VehicleInputController>().enabled = true;
    }

}
