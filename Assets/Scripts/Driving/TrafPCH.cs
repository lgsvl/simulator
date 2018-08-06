/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TrafPCH: MonoBehaviour
{
    public Transform raycastOrigin;
    public CarAutoPath path;
    public int currentWaypointIndex;
    public  RoadPathNode currentNode;
    private Vector3 currentWaypoint;
    private RoadPathNode nextNode;
    private int nextWaypointIndex;
    private Vector3 nextWaypoint;
    private RoadPathNode prevNode;
    private Vector3 prevWaypoint;

    private Vector3 prevTangent;
    private Vector3 currentTangent;

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


    private float targetSpeed = 0f;

    public float steerTargetDist = 16f;

    public bool reverse = false;

    public bool backArc = false;

    public bool straightAtFinal = true;


    public float predictLength = 10f;
    public float normalAdd = 5f;
    public float pathRadius = 1f;

    public float maxTurnAngle = 50f;
    public float turnAngleDivider = 60f;

    public float currentSpeed;
    public float currentTurn;

    RaycastHit hitInfo;

    void Start()
    {
        hitInfo = new RaycastHit();
    }

    public void Init()
    {

        currentWaypoint = currentNode.position;
        nextWaypointIndex = currentWaypointIndex;
        UpdateNextWaypoint();

        prevWaypoint = currentWaypoint;
        prevNode = currentNode;
   

        currentWaypoint = nextWaypoint;
        currentNode = nextNode;
        currentWaypointIndex = nextWaypointIndex;
        UpdateNextWaypoint();


        if (prevNode.tangent == Vector3.zero)
        {
            prevTangent = (currentWaypoint - prevWaypoint).normalized;
        }
        else
        {
            prevTangent = (prevNode.tangent- prevWaypoint).normalized;
        }

        if (currentNode.tangent == Vector3.zero)
        {
            currentTangent = (nextWaypoint - currentWaypoint).normalized;
        }
        else
        {
            currentTangent = (currentNode.tangent - currentWaypoint).normalized;
        }


        targetSpeed = maxSpeed;

    }

    void FixedUpdate()
    {
        MoveCar();
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
        return transform.position + GetComponent<Rigidbody>().velocity * Time.deltaTime * predictLength;
    }


    private Vector3 GetNormalPoint(Vector3 predicted, Vector3 A, Vector3 B)
    {
        Vector3 ap = (predicted - A);
        Vector3 ab = (B - A).normalized;

        float dot = Vector3.Dot(ap, ab);
        float dotNorm = Mathf.Abs(Vector3.Dot(ap.normalized, ab));

        return A + (ab * dot) + ab * (normalAdd * (1 - dotNorm));

    }


    Vector3 seek(Vector3 target, Vector3 location)
    {
        Vector3 desired = (target - location).normalized;
        return desired * maxSpeed;
    }

    float currentPc = 0f;

    void Update()
    {

        /*

        var predicted = GetPredictedPoint();

        var normal = GetNormalPoint(predicted, currentWaypoint, nextWaypoint);

        if(Vector3.Dot(normal - nextWaypoint, nextWaypoint - currentWaypoint) >= 0)
        {
            currentWaypoint = nextWaypoint;
            currentNode = nextNode;
            currentWaypointIndex = nextWaypointIndex;

            UpdateNextWaypoint();

            predicted = GetPredictedPoint();
            normal = GetNormalPoint(predicted, currentWaypoint, nextWaypoint);
        }

        */








        if (currentNode.isInintersection)
            targetSpeed = 4f;
        else
            targetSpeed = maxSpeed;
        
        // if (Vector3.Distance(predicted, normal) < pathRadius)
        //     m_targetSteer = 0f;

        if (CheckFrontBlocked(out hitInfo))
        {
            targetSpeed = Mathf.Clamp(hitInfo.distance / 2f - 1f, 0f, targetSpeed);
        }



        float speedDifference = targetSpeed - GetComponent<Rigidbody>().velocity.magnitude;





        // m_CarControl.steerInput = Mathf.MoveTowards(m_CarControl.steerInput, m_targetSteer, steerSpeed * Time.deltaTime);
        currentTurn = Mathf.Lerp(currentTurn, m_targetSteer, steerSpeed * Time.deltaTime);

        currentSpeed += Mathf.Clamp(speedDifference * Time.deltaTime * throttleSpeed, -maxBrake, maxSpeed);
    }


    private bool CheckFrontBlocked(out RaycastHit blockedInfo)
    {
        Collider[] colls = Physics.OverlapSphere(raycastOrigin.position, 0.2f, 1 << LayerMask.NameToLayer("NPC"));
        foreach (var c in colls)
        {
            if (c.transform.root != transform.root)
            {
                blockedInfo = new RaycastHit();
                blockedInfo.distance = 0f;
                return true;
            }
        }

        if (Physics.Raycast(raycastOrigin.position, raycastOrigin.forward, out blockedInfo, brakeDistance, 1 << LayerMask.NameToLayer("NPC") | 1 << LayerMask.NameToLayer("Duckiebot")))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    void MoveCar()
    {

        float thisDist = Vector3.Distance(currentWaypoint, prevWaypoint);

        currentPc += currentSpeed / thisDist * Time.fixedDeltaTime;


        if (currentPc > 1f)
        {

            prevWaypoint = currentWaypoint;
            prevNode = currentNode;
            prevTangent = currentTangent;

            currentWaypoint = nextWaypoint;
            currentNode = nextNode;
            currentWaypointIndex = nextWaypointIndex;
            UpdateNextWaypoint();

            if (currentNode.tangent == Vector3.zero)
                currentTangent = (nextWaypoint - currentWaypoint).normalized;
            else
            {
                currentTangent = (currentNode.tangent - currentWaypoint).normalized;
            }


            currentPc -= 1f;

            currentPc = (currentPc * thisDist) / Vector3.Distance(currentWaypoint, prevWaypoint);

        }

        Vector3 currentSpot = HermiteMath.HermiteVal(prevWaypoint, currentWaypoint, prevTangent, currentTangent, currentPc);



        //transform.Rotate(0f, currentTurn * Time.deltaTime, 0f);
        RaycastHit hit;
        Physics.Raycast(transform.position + Vector3.up * 5, -transform.up, out hit, 100f, ~(1 << LayerMask.NameToLayer("NPC")));

        Vector3 hTarget = new Vector3(currentSpot.x, hit.point.y, currentSpot.z);


        Vector3 tangent;
        if (currentPc < 0.95f)
        {
            tangent = HermiteMath.HermiteVal(prevWaypoint, currentWaypoint, prevTangent, currentTangent, currentPc + 0.05f) - currentSpot;
        }
        else
        {
            tangent = currentSpot - HermiteMath.HermiteVal(prevWaypoint, currentWaypoint, prevTangent, currentTangent, currentPc - 0.05f);
        }
        tangent.y = 0f;
        tangent = tangent.normalized;

        GetComponent<Rigidbody>().MoveRotation(Quaternion.FromToRotation(Vector3.up, hit.normal) * Quaternion.LookRotation(tangent));
        GetComponent<Rigidbody>().MovePosition(hTarget);
        //transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
    }

}
