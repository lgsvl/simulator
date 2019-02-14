/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public string id { get; set; }
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
    private List<Renderer> allRenderers = new List<Renderer>();
    private List<Renderer> headLightRenderers = new List<Renderer>();
    private List<Renderer> turnSignalRightRenderers = new List<Renderer>();
    private List<Renderer> turnSignalLeftRenderers = new List<Renderer>();
    private List<Renderer> tailLightRenderers = new List<Renderer>();
    private List<Renderer> brakeLightRenderers = new List<Renderer>();

    // lights
    private Light[] allLights = new Light[] { };
    private List<Light> headLights = new List<Light>();
    private enum NPCLightStateTypes
    {
        Off,
        Low,
        High
    };
    private NPCLightStateTypes currentNPCLightState = NPCLightStateTypes.Off;
    private Color runningLightEmissionColor = new Color(0.65f, 0.65f, 0.65f);
    private float fogDensityThreshold = 0.01f;
    private float lowBeamEmission = 2.4f;
    private float highBeamEmission = 4.0f;

    private bool isLaneDataSet = false;
    private bool isStop = false;

    private float stopSignWaitTime = 1f;
    private float currentStopTime = 0f;

    #endregion

    #region mono
    private void Awake()
    {
        if (carCheckBlockBitmask == -1)
        {
            carCheckBlockBitmask = ~(1 << LayerMask.NameToLayer("Ground And Road") | 1 << LayerMask.NameToLayer("PlayerConstrain") | 1 << LayerMask.NameToLayer("Sensor Effects") | 1 << LayerMask.NameToLayer("Ground Truth"));
        }

        if (groundHitBitmask == -1)
        {
            groundHitBitmask = 1 << LayerMask.NameToLayer("Ground And Road") | 1 << LayerMask.NameToLayer("Road Shoulder");
        }
        //Init(); // dev
    }

    private void OnEnable()
    {
        Missive.AddListener<DayNightMissive>(OnDayNightChange);
        GetDayNightState();
    }

    private void OnDisable()
    {
        Missive.RemoveListener<DayNightMissive>(OnDayNightChange);
        currentNPCLightState = NPCLightStateTypes.Off;
        allRenderers.ForEach(x => SetNPCLightRenderers(x));
    }

    private void Update()
    {
        if (!isLaneDataSet) return;

        CollisionCheck();

        WheelMovement();
        EvaluateTarget();
        EvaluateDistanceFromFocus();
    }

    private void FixedUpdate()
    {
        if (!isLaneDataSet) return;

        CalculateSpeed(Time.fixedDeltaTime);
        SetTargetSpeed();
        SetTargetTurn();
        NPCMove();
        NPCTurn();
    }
    #endregion

    #region init
    public void Init()
    {
        GetNeededComponents();
        CreateCollider();
        //CreatePhysicsColliders();
        CreateFrontTransforms();
    }

    public void SetLaneData(MapLaneSegmentBuilder seg)
    {
        currentMapLaneSegmentBuilder = seg;
        SetLaneData(currentMapLaneSegmentBuilder.segment.targetWorldPositions);
        currentSpeed = 0f;
        currentSpeed_measured = currentSpeed;
        normalSpeed = Random.Range(normalSpeedRange.x, normalSpeedRange.y);
        targetSpeed = normalSpeed;
        rb.angularVelocity = Vector3.zero;
        rb.velocity = Vector3.zero;
        isLaneDataSet = true;
    }

    private void GetNeededComponents()
    {
        rb = GetComponent<Rigidbody>();
        allRenderers = GetComponentsInChildren<Renderer>().ToList();
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
                headLights.Add(child);
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
        col.center = new Vector3(col.center.x, bounds.size.y / 2, col.center.z);
    }

    private void CreatePhysicsColliders()
    {
        bounds = new Bounds(transform.position, Vector3.zero);
        foreach (Renderer renderer in allRenderers)
        {
            if (renderer.name.Contains("FR") || renderer.name.Contains("FL") || renderer.name.Contains("RL") || renderer.name.Contains("RR")) { }
            else
                bounds.Encapsulate(renderer.bounds);
        }
        BoxCollider col = gameObject.AddComponent<BoxCollider>();
        col.size = bounds.size;
        col.center = bounds.center; //new Vector3(col.center.x, bounds.size.y / 2, col.center.z);
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
    #endregion

    #region spawn
    private void EvaluateDistanceFromFocus()
    {
        if (ROSAgentManager.Instance?.GetDistanceToActiveAgent(transform.position) > NPCManager.Instance?.despawnDistance)
        {
            Despawn();
        }
    }

    private void Despawn()
    {
        StopAllCoroutines();
        isLaneDataSet = false;
        isStop = false;
        if (prevMapLaneSegmentBuilder?.stopLine?.mapIntersectionBuilder != null)
            prevMapLaneSegmentBuilder.stopLine.mapIntersectionBuilder.ExitQueue(this);
        currentMapLaneSegmentBuilder = null;
        prevMapLaneSegmentBuilder = null;
        NPCManager.Instance.DespawnNPC(gameObject);
    }
    #endregion

    #region lane
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
        brakeLightRenderers.ForEach(x => SetNPCLightRenderers(x, false, true));
        prevMapLaneSegmentBuilder.stopLine.mapIntersectionBuilder.EnterQueue(this);
        yield return new WaitForSeconds(stopSignWaitTime);
        yield return new WaitUntil(() => prevMapLaneSegmentBuilder.stopLine.mapIntersectionBuilder.CheckQueue(this));
        prevMapLaneSegmentBuilder.stopLine.mapIntersectionBuilder.ExitQueue(this);
        brakeLightRenderers.ForEach(x => SetNPCLightRenderers(x));
        isStop = false;
    }

    IEnumerator WaitTrafficLight()
    {
        isStop = true;
        brakeLightRenderers.ForEach(x => SetNPCLightRenderers(x, false, true));
        yield return new WaitUntil(() => prevMapLaneSegmentBuilder.stopLine.currentState == TrafficLightSetState.Green);
        brakeLightRenderers.ForEach(x => SetNPCLightRenderers(x));
        isStop = false;
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
    #endregion

    #region lights
    private void OnDayNightChange(DayNightMissive missive)
    {
        switch (missive.state)
        {
            case DayNightStateTypes.Day:
                currentNPCLightState = NPCLightStateTypes.Off;
                break;
            case DayNightStateTypes.Night:
                currentNPCLightState = NPCLightStateTypes.Low;
                break;
            case DayNightStateTypes.Sunrise:
                currentNPCLightState = NPCLightStateTypes.Off;
                break;
            case DayNightStateTypes.Sunset:
                currentNPCLightState = NPCLightStateTypes.Low;
                break;
            default:
                break;
        }
        headLights.ForEach(x => SetNPCLights(x));
        headLightRenderers.ForEach(x => SetNPCLightRenderers(x, true));
        turnSignalRightRenderers.ForEach(x => SetNPCLightRenderers(x));
        turnSignalLeftRenderers.ForEach(x => SetNPCLightRenderers(x));
        tailLightRenderers.ForEach(x => SetNPCLightRenderers(x));
        brakeLightRenderers.ForEach(x => SetNPCLightRenderers(x));
    }

    private void GetDayNightState()
    {
        if (EnvironmentEffectsManager.Instance == null) return;

        switch (EnvironmentEffectsManager.Instance.currentDayNightState)
        {
            case DayNightStateTypes.Day:
                currentNPCLightState = NPCLightStateTypes.Off;
                break;
            case DayNightStateTypes.Night:
                currentNPCLightState = NPCLightStateTypes.Low;
                break;
            case DayNightStateTypes.Sunrise:
                currentNPCLightState = NPCLightStateTypes.Off;
                break;
            case DayNightStateTypes.Sunset:
                currentNPCLightState = NPCLightStateTypes.Low;
                break;
            default:
                break;
        }
        headLights.ForEach(x => SetNPCLights(x));
        headLightRenderers.ForEach(x => SetNPCLightRenderers(x, true));
        turnSignalRightRenderers.ForEach(x => SetNPCLightRenderers(x));
        turnSignalLeftRenderers.ForEach(x => SetNPCLightRenderers(x));
        tailLightRenderers.ForEach(x => SetNPCLightRenderers(x));
        brakeLightRenderers.ForEach(x => SetNPCLightRenderers(x));
    }

    private void SetNPCLights(Light light)
    {
        switch (currentNPCLightState)
        {
            case NPCLightStateTypes.Off:
                light.enabled = false;
                break;
            case NPCLightStateTypes.Low:
                light.enabled = true;
                light.intensity = 0.9f;
                light.range = 40.0f;
                light.spotAngle = 30.0f;
                light.transform.localEulerAngles = new Vector3(10.0f, light.transform.localEulerAngles.y, light.transform.localEulerAngles.z);
                break;
            case NPCLightStateTypes.High:
                light.enabled = true;
                light.intensity = 3.0f;
                light.range = 100.0f;
                light.spotAngle = 70.0f;
                light.transform.localEulerAngles = new Vector3(0.0f, light.transform.localEulerAngles.y, light.transform.localEulerAngles.z);
                break;
            default:
                break;
        }
    }

    private void SetNPCLightRenderers(Renderer renderer, bool isHeadLightRenderer = false, bool isBrake = false)
    {
        switch (currentNPCLightState)
        {
            case NPCLightStateTypes.Off:
                foreach (var mat in renderer.materials)
                {
                    mat.DisableKeyword("_EMISSION");
                }
                break;
            case NPCLightStateTypes.Low:
                foreach (var mat in renderer.materials)
                {
                    mat.EnableKeyword("_EMISSION");
                    if (isHeadLightRenderer)
                        mat.SetColor("_EmissionColor", Color.white * lowBeamEmission);
                    else if (isBrake)
                        mat.SetColor("_EmissionColor", Color.white);
                    else
                        mat.SetColor("_EmissionColor", runningLightEmissionColor);
                }
                break;
            case NPCLightStateTypes.High:
                foreach (var mat in renderer.materials)
                {
                    mat.EnableKeyword("_EMISSION");
                    if (isHeadLightRenderer)    
                        mat.SetColor("_EmissionColor", Color.white * highBeamEmission);
                    else if (isBrake)
                        mat.SetColor("_EmissionColor", Color.white);
                    else
                        mat.SetColor("_EmissionColor", runningLightEmissionColor);
                }
                break;
            default:
                break;
        }
    }
    #endregion

    #region utility
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
        //groundCheckInfo = new RaycastHit();
        //if (!Physics.Raycast(transform.position, Vector3.down, out groundCheckInfo, 5f, groundHitBitmask))
        //    Despawn();
    }
    #endregion
}