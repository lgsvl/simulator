/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCControllerComponent : MonoBehaviour
{
    #region vars
    // physics
    private Vector3 lastRBPosition;
    private Rigidbody rb;
    private Bounds bounds;
    private RaycastHit groundHit;
    private RaycastHit frontClosestHitInfo = new RaycastHit();
    private RaycastHit groundCheckInfo = new RaycastHit();
    private float minHitDistance = 1000f;
    private bool detectFront = false;
    private const float frontRaycastDistance = 40f;
    private const float stopDistance = 6f;
    private int groundHitBitmask = -1;
    private int carCheckBlockBitmask = -1;

    // inputs
    //public const float maxTurn = 100f;

    // map data
    public string id = "";
    public MapLaneSegmentBuilder currentMapLaneSegmentBuilder;
    public MapLaneSegmentBuilder prevMapLaneSegmentBuilder;
    private List<Vector3> laneData = new List<Vector3>();

    // targeting
    private Transform frontCenter;
    private Transform frontLeft;
    private Transform frontRight;
    private Vector3 currentTarget;
    private int currentIndex = 0;
    //private float distanceToLastTarget = 0f;
    //public Vector3 currentTargetTangent;
    //private Vector3 nextTarget; for intersection checks or new node waypoint[0]
    //private bool doRaycast;
    //private float nextRaycast = 0f;
    private Vector2 normalSpeedRange = new Vector2(8.0f, 10.0f);
    private float normalSpeed = 0f;
    private float targetSpeed;
    private float currentSpeed;
    private float currentSpeed_measured;
    private float targetTurn;
    private float currentTurn = 0f;
    private const float speedAdjustRate = 10.0f; // 4f
    private const float turnAdjustRate = 8.0f;

    // wheels TODO spawn instead
    private Transform wheelFL;
    private Transform wheelFR;
    private Transform wheelRL;
    private Transform wheelRR;
    private float theta = 0f;
    private float newX = 0f;
    private float lastX = 0f;
    private float radius = 0.32f;

    // renderers
    private Renderer[] allRenderers = new Renderer[] { };
    private List<Renderer> headLightRenderers = new List<Renderer>();
    private List<Renderer> turnSignalRightRenderers = new List<Renderer>();
    private List<Renderer> turnSignalLeftRenderers = new List<Renderer>();
    private List<Renderer> tailLightRenderers = new List<Renderer>();
    private List<Renderer> brakeLightRenderers = new List<Renderer>();

    // lights
    private Light[] allLights = new Light[] { };
    private List<Light> lights = new List<Light>();
    
    private bool isInit = false;
    public bool isStop = false;

    private float stopSignWaitTime = 1f;
    private float currentStopTime = 0f;
    
    #endregion

    #region mono
    private void Awake()
    {
        if (carCheckBlockBitmask == -1)
        {
            carCheckBlockBitmask = ~(1 << LayerMask.NameToLayer("Ground And Road") | 1 << LayerMask.NameToLayer("PlayerConstrain") | 1 << LayerMask.NameToLayer("Sensor Effects"));
        }

        if (groundHitBitmask == -1)
        {
            groundHitBitmask = 1 << LayerMask.NameToLayer("Ground And Road") | 1 << LayerMask.NameToLayer("Road Shoulder");
        }
    }

    private void Update()
    {
        if (!isInit) return;

        CollisionCheck();

        WheelMovement();        
        EvaluateTarget();
    }

    private void FixedUpdate()
    {
        if (!isInit) return;

        CalculateSpeed(Time.fixedDeltaTime);
        SetTargetSpeed();
        SetTargetTurn();
        NPCMove();
        NPCTurn();
    }
    #endregion

    #region init
    public void Init(MapLaneSegmentBuilder seg)
    {
        currentMapLaneSegmentBuilder = seg;
        SetLaneData(currentMapLaneSegmentBuilder.segment.targetWorldPositions);

        currentSpeed = 0f;
        currentSpeed_measured = currentSpeed;
        normalSpeed = Random.Range(normalSpeedRange.x, normalSpeedRange.y);
        targetSpeed = normalSpeed;
        //doRaycast = false;
        //nextRaycast = 0f;

        if (!isInit) // all set up is axis aligned so don't set rotation until init
        {
            GetNeededComponents();
            CreateCollider();
            CreateFrontTransforms();

            // static config
            // e.g. API TODO SetBehavior(Dictionary<Key, value>) or WaitOnIntersection(6.0f) or car.atIntersection += Function() { wait(5.0); turnRight(); } ???
        }
        rb.angularVelocity = Vector3.zero;
        rb.velocity = Vector3.zero;

        isInit = true;
    }

    public void GetNextLane()
    {
        // TODO move to method sampled earlier?
        if (currentMapLaneSegmentBuilder?.stopLine != null) // check if stopline is connected to current path
        {
            prevMapLaneSegmentBuilder = currentMapLaneSegmentBuilder;
            if (currentMapLaneSegmentBuilder.stopLine.mapIntersectionBuilder != null) // null if map not setup right TODO add check to report
            {
                if (currentMapLaneSegmentBuilder.stopLine.mapIntersectionBuilder.isStopSign) // stop sign
                {
                    StartCoroutine(WaitStopSign());
                }
                else if (currentMapLaneSegmentBuilder.stopLine.currentState == TrafficLightSetState.Red) // traffic light
                {
                    StartCoroutine(WaitTrafficLight());
                }
            }
        }

        if (currentMapLaneSegmentBuilder?.nextConnectedLanes.Count >= 1) // choose next path and set waypoints
        {
            currentMapLaneSegmentBuilder = currentMapLaneSegmentBuilder.nextConnectedLanes[(int)Random.Range(0, currentMapLaneSegmentBuilder.nextConnectedLanes.Count)];
            SetLaneData(currentMapLaneSegmentBuilder.segment.targetWorldPositions);
        }
        else // issue getting new waypoints so despawn
        {
            Despawn();
        }
    }

    IEnumerator WaitStopSign()
    {
        isStop = true;
        prevMapLaneSegmentBuilder.stopLine.mapIntersectionBuilder.EnterQueue(this); 
        yield return new WaitForSeconds(stopSignWaitTime);
        yield return new WaitUntil(() => prevMapLaneSegmentBuilder.stopLine.mapIntersectionBuilder.CheckQueue(this));
        prevMapLaneSegmentBuilder.stopLine.mapIntersectionBuilder.ExitQueue(this);
        isStop = false;
    }

    IEnumerator WaitTrafficLight()
    {
        isStop = true;
        yield return new WaitUntil(() => prevMapLaneSegmentBuilder.stopLine.currentState == TrafficLightSetState.Green);
        isStop = false;
    }

    private void Despawn()
    {
        StopAllCoroutines();
        isStop = false;
        if (prevMapLaneSegmentBuilder?.stopLine?.mapIntersectionBuilder != null)
            prevMapLaneSegmentBuilder.stopLine.mapIntersectionBuilder.ExitQueue(this);
        currentMapLaneSegmentBuilder = null;
        prevMapLaneSegmentBuilder = null;
        NPCManager.Instance.DespawnNPC(gameObject);
    }

    private void GetNeededComponents()
    {
        rb = GetComponent<Rigidbody>();
        allRenderers = GetComponentsInChildren<Renderer>();
        allLights = GetComponentsInChildren<Light>();

        foreach (Renderer child in allRenderers)
        {
            if (child.name.Contains("FR"))
                wheelFR = child.transform;
            if (child.name.Contains("FL"))
                wheelFL = child.transform;
            if (child.name.Contains("RL"))
                wheelRL = child.transform;
            if (child.name.Contains("RR"))
                wheelRR = child.transform;
            if (child.name.Contains("HeadLights"))
                headLightRenderers.Add(child);
            if (child.name.Contains("SignalLightRight"))
                turnSignalRightRenderers.Add(child);
            if (child.name.Contains("SignalLightLeft"))
                turnSignalLeftRenderers.Add(child);
            if (child.name.Contains("TailLights"))
                tailLightRenderers.Add(child);
            if (child.name.Contains("BrakeLights"))
                brakeLightRenderers.Add(child);
        }

        foreach (Light child in allLights)
        {
            if (child.name.Contains("Light"))
                lights.Add(child);
        }
    }

    private void CreateCollider()
    {
        bounds = new Bounds(transform.position, Vector3.zero);
        foreach (Renderer renderer in allRenderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }
        BoxCollider col = gameObject.AddComponent<BoxCollider>();
        col.size = bounds.size;
        col.center = new Vector3(col.center.x, bounds.size.y/2, col.center.z);
    }

    private void CreateFrontTransforms()
    {
        GameObject go = new GameObject("Front");
        go.transform.position = new Vector3(bounds.center.x, bounds.min.y + 0.5f, bounds.center.z + bounds.max.z);
        go.transform.SetParent(transform, true);
        frontCenter = go.transform;
        go = new GameObject("Right");
        go.transform.position = new Vector3(bounds.center.x + bounds.max.x, bounds.min.y + 0.5f, bounds.center.z + bounds.max.z);
        go.transform.SetParent(transform, true);
        frontRight = go.transform;
        go = new GameObject("Left");
        go.transform.position = new Vector3(bounds.center.x - bounds.max.x, bounds.min.y + 0.5f, bounds.center.z + bounds.max.z);
        go.transform.SetParent(transform, true);
        frontLeft = go.transform;
    }

    private void WheelMovement()
    {
        if (!wheelFR || !wheelFL || !wheelRL || !wheelRR) return;

        theta = currentSpeed_measured * Time.deltaTime / radius;
        newX = lastX + theta * Mathf.Rad2Deg;
        lastX = newX;
        if (lastX > 360)
            lastX -= 360;

        wheelFR.localRotation = Quaternion.Euler(newX, currentTurn, 0);
        wheelFL.localRotation = Quaternion.Euler(newX, currentTurn, 0);
        wheelRL.localRotation = Quaternion.Euler(newX, 0, 0);
        wheelRR.localRotation = Quaternion.Euler(newX, 0, 0);
    }
    #endregion

    #region physics
    private void NPCMove()
    {
        rb.MovePosition(rb.position + transform.forward * currentSpeed * Time.deltaTime);
    }

    private void NPCTurn()
    {
        if (isStop) return;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, currentTurn * Time.deltaTime, 0f));
    }

    private void CalculateSpeed(float delta)
    {
        currentSpeed_measured = ((rb.position - lastRBPosition) / delta).magnitude; // TODO replace with actual velocity
        lastRBPosition = rb.position;
    }
    #endregion

    #region inputs
    private void SetTargetTurn()
    {
        Vector3 steerVector = (currentTarget - frontCenter.position).normalized;
        float steer = Vector3.Angle(frontCenter.forward, steerVector);
        targetTurn = Vector3.Cross(frontCenter.forward, steerVector).y < 0 ? -steer : steer;

        currentTurn += turnAdjustRate * Time.deltaTime * (targetTurn - currentTurn);
    }

    private void SetTargetSpeed()
    {
        targetSpeed = normalSpeed; //always assume target speed is normal speed and then reduce as needed

        if (detectFront && frontClosestHitInfo.distance < stopDistance || isStop)
        {
            targetSpeed = 0f; //hard stop when too close 
        }

        if (currentIndex < laneData.Count - 1)
        {
            float angle = Vector3.Angle(transform.forward, (laneData[currentIndex + 1] - laneData[currentIndex]).normalized);
            Vector3 cross = Vector3.Cross(transform.forward, (laneData[currentIndex + 1] - laneData[currentIndex]).normalized);
            if (angle > 25)
            {
                targetSpeed /= 2f;
            }
        }

        currentSpeed += speedAdjustRate * Time.deltaTime * (targetSpeed - currentSpeed);
    }
    #endregion

    #region targeting
    public void SetLaneData(List<Vector3> data)
    {
        currentIndex = 0; // TODO better way?
        laneData = data;
        currentTarget = laneData[++currentIndex];
    }

    private bool HasReachedTarget()
    {
        return (Vector3.Dot(frontCenter.forward, currentTarget - frontCenter.position) < 0);
    }

    private void EvaluateTarget()
    {
        if (!HasReachedTarget()) return;

        if (currentIndex < laneData.Count - 1)
        {
            currentIndex++;
            currentTarget = laneData[currentIndex];
        }
        else
        {
            GetNextLane();
        }
    }

    private void CollisionCheck()
    {
        if (frontCenter == null || frontLeft == null || frontRight == null) return;
        
        // front collision
        frontClosestHitInfo = new RaycastHit();
        if (Physics.Raycast(frontCenter.position, frontCenter.forward, out frontClosestHitInfo, frontRaycastDistance, carCheckBlockBitmask))
        {
            //Debug.DrawLine(frontCenter.position, frontClosestHitInfo.point, Color.red, 1f);
        }
        else if (Physics.Raycast(frontRight.position, frontRight.forward, out frontClosestHitInfo, frontRaycastDistance, carCheckBlockBitmask))
        {
            //Debug.DrawLine(frontCenter.position, frontClosestHitInfo.point, Color.white, 1f);
        }
        else if (Physics.Raycast(frontLeft.position, frontLeft.forward, out frontClosestHitInfo, frontRaycastDistance, carCheckBlockBitmask))
        {
            //Debug.DrawLine(frontCenter.position, frontClosestHitInfo.point, Color.white, 1f);
        }
        detectFront = frontClosestHitInfo.collider != null && frontClosestHitInfo.distance < minHitDistance;

        // ground collision
        groundCheckInfo = new RaycastHit();
        if (!Physics.Raycast(transform.position, Vector3.down, out groundCheckInfo, 5f, groundHitBitmask))
            Despawn();
    }
    #endregion
}