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
using Control;

public class NPCControllerComponent : MonoBehaviour
{
    #region vars
    public bool DEBUG = false;

    // physics
    public LayerMask groundHitBitmask;
    public LayerMask carCheckBlockBitmask;
    private bool isPhysicsSimple = false;
    private BoxCollider simpleBoxCollider;
    private BoxCollider complexBoxCollider;
    private Vector3 lastRBPosition;
    private Rigidbody rb;
    private Bounds bounds;
    private RaycastHit frontClosestHitInfo = new RaycastHit();
    private RaycastHit groundCheckInfo = new RaycastHit();
    private float frontRaycastDistance = 20f;
    private float stopDistance = 6f;
    
    private float brakeTorque = 0f;
    private float motorTorque = 0f;
    public AnimationCurve distSpeedCurve;
    public AnimationCurve brakeSpeedCurve;
    private Vector3 centerOfMass;
    private GameObject wheelColliderHolder;
    private WheelCollider wheelColliderFR;
    private WheelCollider wheelColliderFL;
    private WheelCollider wheelColliderRL;
    private WheelCollider wheelColliderRR;
    
    private float maxMotorTorque = 350f; //torque at peak of torque curve
    private float maxBrakeTorque = 3000f; //torque at max brake
    private float maxSteeringAngle = 39.4f; //steering range is [-maxSteeringAngle, maxSteeringAngle]
    private float wheelDampingRate = 1f;
    
    // map data
    public string id { get; set; }
    public MapLaneSegmentBuilder currentMapLaneSegmentBuilder;
    public MapLaneSegmentBuilder prevMapLaneSegmentBuilder;
    public IntersectionComponent currentIntersectionComponent;
    private List<Vector3> laneData = new List<Vector3>();

    // targeting
    private Transform frontCenter;
    private Transform frontLeft;
    private Transform frontRight;
    private Vector3 currentTarget;
    private int currentIndex = 0;
    private float distanceToCurrentTarget = 0f;
    private float distanceToStopTarget = 0;
    private float totalDistanceToStopTarget = 0f;
    private float distanceToLastTarget = 0f;
    private Vector3 stopTarget = Vector3.zero;

    //private bool doRaycast; // TODO skip update for collision
    //private float nextRaycast = 0f;
    private Vector2 normalSpeedRange = new Vector2(11f, 13f);
    private float normalSpeed = 0f;
    public float targetSpeed = 0f;
    public float currentSpeed = 0f;
    public float currentSpeed_measured = 0f;
    public float targetTurn = 0f;
    private float currentTurn = 0f;
    private float speedAdjustRate = 10.0f; // 4f
    private float turnAdjustRate = 10.0f;

    // wheel visuals
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
    public bool isFrontDetectWithinStopDistance = false;
    public bool hasReachedStopSign = false;
    public bool isStop = false;
    private float path = 0f;
    public bool isLeftTurn = false;
    public bool isRightTurn = false;

    private float stopSignWaitTime = 1f;
    private float currentStopTime = 0f;
    private Control.PID speed_pid;
    private Control.PID steer_pid;

    public float steer_PID_kp = 0.025f;
    public float steer_PID_kd = 0f;
    public float steer_PID_ki = 0f;
    public float speed_PID_kp = 0.1f;
    public float speed_PID_kd = 0f;
    public float speed_PID_ki = 0f;
    public float maxSteerRate = 20f;

    #endregion

    #region mono
    private void OnEnable()
    {
        Missive.AddListener<DayNightMissive>(OnDayNightChange);
        GetDayNightState();
        speed_pid = new Control.PID();
        steer_pid = new Control.PID();
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
        TogglePhysicsMode();
        
        CollisionCheck();
        EvaluateDistanceFromFocus();
        EvaluateTarget();
        SetTargetTurnSimple();
        SetTargetSpeed();
        WheelMovementSimple();
    }

    private void FixedUpdate()
    {
        if (!isLaneDataSet) return;
        WheelMovementComplex();
        SetTargetTurnComplex();
        speed_pid.SetKValues(speed_PID_kp, speed_PID_kd, speed_PID_ki);
        steer_pid.SetKValues(steer_PID_kp, steer_PID_kd, steer_PID_ki);
        NPCTurn();
        NPCMove();
    }
    #endregion

    #region init
    public void Init()
    {
        GetNeededComponents();
        CreateCollider();
        CreatePhysicsColliders();
        CreateFrontTransforms();
    }

    public void SetLaneData(MapLaneSegmentBuilder seg)
    {
        currentIntersectionComponent = null;
        prevMapLaneSegmentBuilder = null;
        currentMapLaneSegmentBuilder = seg;
        SetLaneData(currentMapLaneSegmentBuilder.segment.targetWorldPositions);
        currentSpeed = 0f;
        currentSpeed_measured = currentSpeed;
        normalSpeed = Random.Range(normalSpeedRange.x, normalSpeedRange.y);
        targetSpeed = normalSpeed;
        rb.angularVelocity = Vector3.zero;
        rb.velocity = Vector3.zero;
        isLeftTurn = false;
        isRightTurn = false;
        path = 0f;
        hasReachedStopSign = false;
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
        simpleBoxCollider = gameObject.AddComponent<BoxCollider>();
        simpleBoxCollider.size = bounds.size;
        simpleBoxCollider.center = new Vector3(simpleBoxCollider.center.x, bounds.size.y / 2, simpleBoxCollider.center.z);
        rb.centerOfMass = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z + bounds.max.z * 0.5f);
    }

    private void CreatePhysicsColliders()
    {
        Bounds boundsPhy = new Bounds(transform.position, Vector3.zero);
        foreach (Renderer renderer in allRenderers)
        {
            if (renderer.name.Contains("Main") || renderer.name.Contains("Cab")) // TODO better way, tags?
                boundsPhy = renderer.bounds;
                
            if (renderer.name.Contains("Trailer") || renderer.name.Contains("Underside"))
                boundsPhy.Encapsulate(renderer.bounds);
        }
        complexBoxCollider = gameObject.AddComponent<BoxCollider>();
        complexBoxCollider.size = simpleBoxCollider.size; //new Vector3(boundsPhy.size.x, boundsPhy.size.y * 0.5f, boundsPhy.size.z); // TODO fit better
        complexBoxCollider.center = new Vector3(simpleBoxCollider.center.x, bounds.size.y / 2 + 0.5f, simpleBoxCollider.center.z);  //boundsPhy.center;

        // wheel colliders
        if (NPCManager.Instance == null || NPCManager.Instance.wheelColliderPrefab == null) return;
        
        wheelColliderHolder = Instantiate(NPCManager.Instance.wheelColliderPrefab, Vector3.zero, Quaternion.identity, transform.GetChild(0));
        foreach (Transform child in wheelColliderHolder.transform)
        {
            if (child.name.Contains("FR"))
            {
                child.localPosition = wheelFR.localPosition;
                wheelColliderFR = child.GetComponent<WheelCollider>();
                wheelColliderFR.center = new Vector3(0f, child.localPosition.y / 2, 0f);
                wheelColliderFR.radius = wheelFR.GetComponent<Renderer>().bounds.extents.z;
                wheelColliderFR.ConfigureVehicleSubsteps(5.0f, 30, 10);
                wheelColliderFR.wheelDampingRate = wheelDampingRate;
            }
            else if (child.name.Contains("FL"))
            {
                child.localPosition = wheelFL.localPosition;
                wheelColliderFL = child.GetComponent<WheelCollider>();
                wheelColliderFL.center = new Vector3(0f, child.localPosition.y / 2, 0f);
                wheelColliderFL.radius = wheelFL.GetComponent<Renderer>().bounds.extents.z;
                wheelColliderFL.ConfigureVehicleSubsteps(5.0f, 30, 10);
                wheelColliderFL.wheelDampingRate = wheelDampingRate;
            }
            else if (child.name.Contains("RL"))
            {
                child.localPosition = wheelRL.localPosition;
                wheelColliderRL = child.GetComponent<WheelCollider>();
                wheelColliderRL.center = new Vector3(0f, child.localPosition.y / 2, 0f);
                wheelColliderRL.radius = wheelRL.GetComponent<Renderer>().bounds.extents.z;
                wheelColliderRL.ConfigureVehicleSubsteps(5.0f, 30, 10);
                wheelColliderRL.wheelDampingRate = wheelDampingRate;
            }
            else if (child.name.Contains("RR"))
            {
                child.localPosition = wheelRR.localPosition;
                wheelColliderRR = child.GetComponent<WheelCollider>();
                wheelColliderRR.center = new Vector3(0f, child.localPosition.y / 2, 0f);
                wheelColliderRR.radius = wheelRR.GetComponent<Renderer>().bounds.extents.z;
                wheelColliderRR.ConfigureVehicleSubsteps(5.0f, 30, 10);
                wheelColliderRR.wheelDampingRate = wheelDampingRate;
            }
        }
    }

    private void CreateFrontTransforms()
    {
        GameObject go = new GameObject("Front");
        go.transform.position = new Vector3(bounds.center.x, bounds.min.y + 1f, bounds.center.z + bounds.max.z);
        go.transform.SetParent(transform, true);
        frontCenter = go.transform;
        go = new GameObject("Right");
        go.transform.position = new Vector3(bounds.center.x + bounds.max.x, bounds.min.y + 1f, bounds.center.z + bounds.max.z);
        go.transform.SetParent(transform, true);
        frontRight = go.transform;
        go = new GameObject("Left");
        go.transform.position = new Vector3(bounds.center.x - bounds.max.x, bounds.min.y + 1f, bounds.center.z + bounds.max.z);
        go.transform.SetParent(transform, true);
        frontLeft = go.transform;
    }
    #endregion

    #region spawn
    private void EvaluateDistanceFromFocus()
    {
        if (NPCManager.Instance.isSpawnAreaLimited && ROSAgentManager.Instance?.GetDistanceToActiveAgent(transform.position) > NPCManager.Instance?.despawnDistance)
        {
            Despawn();
        }
    }

    private void Despawn()
    {
        StopAllCoroutines();
        isLaneDataSet = false;
        isStop = false;
        isRightTurn = false;
        isLeftTurn = false;
        hasReachedStopSign = false;
        if (prevMapLaneSegmentBuilder?.stopLine?.mapIntersectionBuilder != null)
            prevMapLaneSegmentBuilder.stopLine.mapIntersectionBuilder.ExitQueue(this);
        currentMapLaneSegmentBuilder = null;
        currentIntersectionComponent = null;
        prevMapLaneSegmentBuilder = null;
        NPCManager.Instance?.DespawnNPC(gameObject);
    }
    #endregion

    #region lane
    public void GetNextLane()
    {
        // last index of current lane data
        if (currentMapLaneSegmentBuilder?.nextConnectedLanes.Count >= 1) // choose next path and set waypoints
        {
            MapLaneSegmentBuilder tempMSB = currentMapLaneSegmentBuilder;
            currentMapLaneSegmentBuilder = currentMapLaneSegmentBuilder.nextConnectedLanes[(int)Random.Range(0, currentMapLaneSegmentBuilder.nextConnectedLanes.Count)];
            SetLaneData(currentMapLaneSegmentBuilder.segment.targetWorldPositions);

            if (tempMSB.stopLine != null)
            {
                path = transform.InverseTransformPoint(currentMapLaneSegmentBuilder.segment.targetWorldPositions[currentMapLaneSegmentBuilder.segment.targetWorldPositions.Count - 1]).x;
                if (path < -1f)
                {
                    isLeftTurn = true;
                    isRightTurn = false;
                }
                else if (path > 1f)
                {
                    isLeftTurn = false;
                    isRightTurn = true;
                }
                else
                {
                    isLeftTurn = false;
                    isRightTurn = false;
                }
            }
            else
            {
                isLeftTurn = false;
                isRightTurn = false;
            }
            
        }
        else // issue getting new waypoints so despawn
        {
            // TODO raycast to see adjacent lanes? Need system
            Despawn();
        }
    }

    IEnumerator WaitStopSign()
    {
        yield return new WaitUntil(() => distanceToLastTarget < 25f);
        isStop = true;
        hasReachedStopSign = false;
        totalDistanceToStopTarget = Vector3.Distance(new Vector3(frontCenter.position.x, 0f, frontCenter.position.z), new Vector3(stopTarget.x, 0f, stopTarget.z));
        brakeLightRenderers.ForEach(x => SetNPCLightRenderers(x, false, true));
        yield return new WaitUntil(() => hasReachedStopSign);
        prevMapLaneSegmentBuilder.stopLine.mapIntersectionBuilder.EnterQueue(this);
        yield return new WaitForSeconds(stopSignWaitTime);
        yield return new WaitUntil(() => prevMapLaneSegmentBuilder.stopLine.mapIntersectionBuilder.CheckQueue(this));
        prevMapLaneSegmentBuilder.stopLine.mapIntersectionBuilder.ExitQueue(this);
        brakeLightRenderers.ForEach(x => SetNPCLightRenderers(x));
        isStop = false;
    }

    IEnumerator WaitTrafficLight()
    {
        yield return new WaitUntil(() => distanceToLastTarget < 25f);
        isStop = true;
        totalDistanceToStopTarget = Vector3.Distance(new Vector3(frontCenter.position.x, 0f, frontCenter.position.z), new Vector3(stopTarget.x, 0f, stopTarget.z));
        brakeLightRenderers.ForEach(x => SetNPCLightRenderers(x, false, true));
        yield return new WaitUntil(() => prevMapLaneSegmentBuilder.stopLine.currentState == TrafficLightSetState.Green);
        brakeLightRenderers.ForEach(x => SetNPCLightRenderers(x));
        isStop = false;
    }
    #endregion

    #region physics
    private void NPCMove()
    {
        if (isPhysicsSimple)
            rb.MovePosition(rb.position + transform.forward * currentSpeed * Time.deltaTime);
        else
            ApplyTorque();
    }

    private void NPCTurn()
    {
        if (currentSpeed <= 0f) return;

        if (isPhysicsSimple)
        {
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, currentTurn * Time.deltaTime, 0f));
        }
        else
        {
            float dt = Time.fixedDeltaTime;
            float steer = wheelColliderFL.steerAngle;

            // using (System.IO.StreamWriter w = System.IO.File.AppendText("/home/hadi/pid.txt"))
            // {
            // w.WriteLine(steer - targetTurn);
            // }

            float deltaAngle = -steer_pid.Run(dt, steer, targetTurn);

            if (Mathf.Abs(deltaAngle) > maxSteerRate * dt)
            {
                deltaAngle = Mathf.Sign(deltaAngle) * maxSteerRate * dt;
            }

            steer += deltaAngle;

            steer = Mathf.Min(steer, maxSteeringAngle);
            steer = Mathf.Max(steer, -maxSteeringAngle);
            wheelColliderFL.steerAngle = steer;
            wheelColliderFR.steerAngle = steer;
        }
    }

    private void TogglePhysicsMode()
    {
        isPhysicsSimple = NPCManager.Instance.isSimplePhysics;
        simpleBoxCollider.enabled = isPhysicsSimple;
        complexBoxCollider.enabled = !isPhysicsSimple;
        wheelColliderHolder.SetActive(!isPhysicsSimple);
    }

    private void ApplyTorque()
    {
        // Maintain speed at target speed
        float FRICTION_COEFFICIENT = 0.7f; // for dry wheel/pavement -- wet is about 0.4
        float deltaVel = - speed_pid.Run(Time.fixedDeltaTime, currentSpeed_measured, targetSpeed);
        float deltaAccel = deltaVel / Time.fixedDeltaTime;
        float deltaTorque = 0.25f * rb.mass * Mathf.Abs(Physics.gravity.y) * wheelColliderFR.radius * FRICTION_COEFFICIENT * deltaAccel;

        if (deltaTorque > 0)
        {
            motorTorque += deltaTorque;
            if (motorTorque > maxMotorTorque)
            {
                motorTorque = maxMotorTorque;
            }
            brakeTorque = 0;
        }
        else
        {
            motorTorque = 0;
            brakeTorque -= deltaTorque;
            if (brakeTorque > maxBrakeTorque)
            {
                brakeTorque = maxBrakeTorque;
            }
        }

        wheelColliderFR.brakeTorque = brakeTorque;
        wheelColliderFL.brakeTorque = brakeTorque;
        wheelColliderRL.brakeTorque = brakeTorque;
        wheelColliderRR.brakeTorque = brakeTorque;
        wheelColliderFR.motorTorque = motorTorque;
        wheelColliderFL.motorTorque = motorTorque;
        wheelColliderRL.motorTorque = motorTorque;
        wheelColliderRR.motorTorque = motorTorque;
    }
    #endregion

    #region inputs
    private void SetTargetTurnSimple()
    {
        if (isPhysicsSimple)
        {
            Vector3 steerVector = (currentTarget - frontCenter.position).normalized;
            float steer = Vector3.Angle(steerVector, frontCenter.forward) * 1.5f; // magic number that helps make turns better TODO why
            targetTurn = Vector3.Cross(frontCenter.forward, steerVector).y < 0 ? -steer : steer;
            currentTurn = Mathf.Lerp(currentTurn, targetTurn, Time.deltaTime * turnAdjustRate);
        }
    }

    private void SetTargetTurnComplex()
    {
        if (!isPhysicsSimple)
        {
            Vector3 steerVector = (currentTarget - frontCenter.position).normalized;
            float steer = Vector3.Angle(steerVector, frontCenter.forward);
            targetTurn = Vector3.Cross(frontCenter.forward, steerVector).y < 0 ? -steer : steer;
            currentTurn += turnAdjustRate * Time.deltaTime * (targetTurn - currentTurn);
        }
    }

    private void SetTargetSpeed()
    {
        targetSpeed = normalSpeed; //always assume target speed is normal speed and then reduce as needed

        if (isStop)
        {
            targetSpeed = isFrontDetectWithinStopDistance ? SetFrontDetectSpeed() : distSpeedCurve.Evaluate(1.0f - (distanceToStopTarget / totalDistanceToStopTarget)) * normalSpeed;
            if (distanceToStopTarget < 1f || targetSpeed < 0.5f)
            {
                hasReachedStopSign = true;
                targetSpeed = 0f;
            }
        }

        if (isFrontDetectWithinStopDistance)
        {
            targetSpeed = SetFrontDetectSpeed();
        }

        // right of way wip
        //if (currentIntersectionComponent != null)
        //{
        //    if (currentIntersectionComponent.IsOnComing(transform) && isLeftTurn)
        //    {
        //        targetSpeed = 0f;
        //    }
        //}


        if (!isStop && !isFrontDetectWithinStopDistance)
        {
            if (currentIndex < laneData.Count - 1)
            {
                float angle = Vector3.Angle(transform.forward, (laneData[currentIndex + 1] - laneData[currentIndex]).normalized);
                Vector3 cross = Vector3.Cross(transform.forward, (laneData[currentIndex + 1] - laneData[currentIndex]).normalized);
                if (angle > 25)
                {
                    targetSpeed = Mathf.Lerp(targetSpeed, normalSpeed * 0.25f, Time.deltaTime * 15f);// *= 0.5f; // Mathf.Lerp(currentSpeed, normalSpeed * 0.25f, Time.deltaTime * 15f);
                }
            }
        }

        currentSpeed += speedAdjustRate * Time.deltaTime * (targetSpeed - currentSpeed); // TODO should be in simple physics only
        currentSpeed_measured = isPhysicsSimple ? ((rb.position - lastRBPosition) / Time.deltaTime).magnitude : rb.velocity.magnitude * 2.23693629f; // MPH
        lastRBPosition = rb.position;
    }
    #endregion

    #region targeting
    public void SetLaneData(List<Vector3> data)
    {
        currentIndex = 0; // TODO better way?
        laneData = data;
        currentTarget = laneData[++currentIndex];
    }
    
    private void EvaluateTarget()
    {
        distanceToCurrentTarget = Vector3.Distance(new Vector3(frontCenter.position.x, 0f, frontCenter.position.z), new Vector3(currentTarget.x, 0f, currentTarget.z));
        distanceToStopTarget = Vector3.Distance(new Vector3(frontCenter.position.x, 0f, frontCenter.position.z), new Vector3(stopTarget.x, 0f, stopTarget.z));
        distanceToLastTarget = Vector3.Distance(frontCenter.position, laneData[laneData.Count - 1]);
        
        if (Vector3.Dot(frontCenter.forward, (currentTarget - frontCenter.position)) < 0 || distanceToCurrentTarget < 2f)
        {
            if (currentIndex == laneData.Count - 2) // reached 2nd to last target index see if stop line is present
            {
                if (currentMapLaneSegmentBuilder?.stopLine != null) // check if stopline is connected to current path
                {
                    currentIntersectionComponent = currentMapLaneSegmentBuilder.stopLine?.mapIntersectionBuilder?.intersectionC;
                    stopTarget = currentMapLaneSegmentBuilder.segment.targetWorldPositions[currentMapLaneSegmentBuilder.segment.targetWorldPositions.Count - 1];
                    prevMapLaneSegmentBuilder = currentMapLaneSegmentBuilder;
                    if (prevMapLaneSegmentBuilder.stopLine.mapIntersectionBuilder != null) // null if map not setup right TODO add check to report missing stopline
                    {
                        if (prevMapLaneSegmentBuilder.stopLine.mapIntersectionBuilder.isStopSign) // stop sign
                        {
                            StartCoroutine(WaitStopSign());
                        }
                        else if (prevMapLaneSegmentBuilder.stopLine.currentState == TrafficLightSetState.Red || prevMapLaneSegmentBuilder.stopLine.currentState == TrafficLightSetState.Yellow) // traffic light
                        {
                            StartCoroutine(WaitTrafficLight());
                        }
                    }
                }
            }

            if (currentIndex < laneData.Count - 1) // reached target dist and is not at last index of lane data
            {
                currentIndex++;
                currentTarget = laneData[currentIndex];
            }
            else
            {
                GetNextLane();
            }
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
    private void WheelMovementSimple()
    {
        if (!wheelFR || !wheelFL || !wheelRL || !wheelRR) return;
        
        if (isPhysicsSimple)
        {
            theta = currentSpeed * Time.fixedDeltaTime / radius;
            newX = lastX + theta * Mathf.Rad2Deg;
            lastX = newX;
            if (lastX > 360)
                lastX -= 360;

            wheelFR.localRotation = Quaternion.Euler(newX, currentTurn, 0);
            wheelFL.localRotation = Quaternion.Euler(newX, currentTurn, 0);
            wheelRL.localRotation = Quaternion.Euler(newX, 0, 0);
            wheelRR.localRotation = Quaternion.Euler(newX, 0, 0);
        }
    }

    private void WheelMovementComplex()
    {
        if (!wheelFR || !wheelFL || !wheelRL || !wheelRR) return;

        if (!isPhysicsSimple)
        {
            Vector3 pos;
            Quaternion rot;
            wheelColliderFR.GetWorldPose(out pos, out rot);
            wheelFR.position = pos;
            wheelFR.rotation = rot;
            wheelColliderFL.GetWorldPose(out pos, out rot);
            wheelFL.position = pos;
            wheelFL.rotation = rot;
            wheelColliderRL.GetWorldPose(out pos, out rot);
            wheelRL.position = pos;
            wheelRL.rotation = rot;
            wheelColliderRR.GetWorldPose(out pos, out rot);
            wheelRR.position = pos;
            wheelRR.rotation = rot;
        }
    }

    private void CollisionCheck()
    {
        if (frontCenter == null || frontLeft == null || frontRight == null) return;
        
        frontClosestHitInfo = new RaycastHit();
        
        if (Physics.Raycast(frontCenter.position, frontCenter.forward, out frontClosestHitInfo, frontRaycastDistance, carCheckBlockBitmask))
        {
            //Debug.DrawLine(frontCenter.position, frontClosestHitInfo.point, Color.blue, 0.25f);
        }
        else if (Physics.Raycast(frontRight.position, frontRight.forward, out frontClosestHitInfo, frontRaycastDistance, carCheckBlockBitmask))
        {
            //Debug.DrawLine(frontCenter.position, frontClosestHitInfo.point, Color.red, 0.25f);
        }
        else if (Physics.Raycast(frontLeft.position, frontLeft.forward, out frontClosestHitInfo, frontRaycastDistance, carCheckBlockBitmask))
        {
            //Debug.DrawLine(frontCenter.position, frontClosestHitInfo.point, Color.yellow, 0.25f);
        }
        isFrontDetectWithinStopDistance = (frontClosestHitInfo.collider) && frontClosestHitInfo.distance < stopDistance;
        
        // ground collision
        groundCheckInfo = new RaycastHit();
        if (!Physics.Raycast(transform.position + Vector3.up, Vector3.down, out groundCheckInfo, 5f, groundHitBitmask))
            Despawn();
    }

    private float SetFrontDetectSpeed()
    {
        var blocking = frontClosestHitInfo.transform;

        var npcC = frontClosestHitInfo.transform?.GetComponent<NPCControllerComponent>();
        var vC = frontClosestHitInfo.transform?.GetComponent<VehicleController>();
        float tempS = 0f;

        if (Vector3.Dot(transform.forward, blocking.transform.forward) > 0.7f) // detected is on similar vector
        {
            if (frontClosestHitInfo.distance > 1f)
                tempS = normalSpeed * (frontClosestHitInfo.distance / stopDistance);
            else
                tempS = 0f;
        }
        return tempS;
    }
    #endregion
}
