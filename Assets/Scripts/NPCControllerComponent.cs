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
    public enum ControlType
    {
        Automatic,
        FollowLane,
        Waypoints,
        FixedSpeed,
        Manual,
    }

    [HideInInspector]
    public ControlType Control = ControlType.Automatic;

    #region vars
    public bool DEBUG = false;

    // physics
    public LayerMask groundHitBitmask;
    public LayerMask carCheckBlockBitmask;
    private bool isPhysicsSimple = false;
    private BoxCollider simpleBoxCollider;
    private BoxCollider complexBoxCollider;
    private Vector3 lastRBPosition;
    private Vector3 simpleVelocity;
    private Quaternion lastRBRotation;
    private Vector3 simpleAngularVelocity;
    private Vector3 angularVelocity;
    private Rigidbody rb;
    private Bounds bounds;
    private RaycastHit frontClosestHitInfo = new RaycastHit();
    private RaycastHit leftClosestHitInfo = new RaycastHit();
    private RaycastHit rightClosestHitInfo = new RaycastHit();
    private RaycastHit groundCheckInfo = new RaycastHit();
    private float frontRaycastDistance = 20f;
    private float stopHitDistance = 7f;
    private float stopLineDistance = 15f;
    private bool atStopTarget;

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
    public List<float> laneSpeed; // used for waypoint mode
    public List<Vector3> laneData;
    public bool waypointLoop;

    // targeting
    private Transform frontCenter;
    private Transform frontLeft;
    private Transform frontRight;
    private Vector3 currentTarget;
    private Quaternion targetRot;
    private float angle;
    private int currentIndex = 0;
    private float distanceToCurrentTarget = 0f;
    public float distanceToStopTarget = 0;
    private Vector3 stopTarget = Vector3.zero;
    private float minTargetDistance = 1f;

    //private bool doRaycast; // TODO skip update for collision
    //private float nextRaycast = 0f;
    private Vector2 normalSpeedRange = new Vector2(10f, 12f);
    private Vector2 complexPhysicsSpeedRange = new Vector2(15f, 22f);
    private float normalSpeed = 0f;
    public float targetSpeed = 0f;
    public float currentSpeed = 0f;
    public float currentSpeed_measured = 0f;
    public float targetTurn = 0f;
    public float currentTurn = 0f;
    private Vector3 steerVector;
    public float speedAdjustRate = 4.0f;
    private float minSpeedAdjustRate = 1f;
    private float maxSpeedAdjustRate = 4f;
    private float elapsedAccelerateTime = 0f;
    private float turnAdjustRate = 10.0f;

    // wheel visuals
    private Transform wheelFL;
    private Transform wheelFR;
    private Transform wheelRL;
    private Transform wheelRR;
    private Vector3 origPosWheelFL;
    private Vector3 origPosWheelFR;
    private Vector3 origPosWheelRL;
    private Vector3 origPosWheelRR;

    // renderers
    private List<Renderer> allRenderers = new List<Renderer>();
    private List<Renderer> headLightRenderers = new List<Renderer>();
    private List<Renderer> turnSignalRightRenderers = new List<Renderer>();
    private List<Renderer> turnSignalLeftRenderers = new List<Renderer>();
    private List<Renderer> tailLightRenderers = new List<Renderer>();
    private List<Renderer> brakeLightRenderers = new List<Renderer>();

    // lights
    private Light[] allLights;
    private List<Light> headLights = new List<Light>();
    private enum NPCLightStateTypes
    {
        Off,
        Low,
        High
    };
    private NPCLightStateTypes currentNPCLightState = NPCLightStateTypes.Off;
    private Color runningLightEmissionColor = new Color(0.65f, 0.65f, 0.65f);
    //private float fogDensityThreshold = 0.01f;
    private float lowBeamEmission = 2.4f;
    private float highBeamEmission = 4.0f;

    private bool isLaneDataSet = false;
    public bool isFrontDetectWithinStopDistance = false;
    public bool isRightDetectWithinStopDistance = false;
    public bool isLeftDetectWithinStopDistance = false;
    public bool isFrontLeftDetect = false;
    public bool isFrontRightDetect = false;
    public bool hasReachedStopSign = false;
    public bool isStopLight = false;
    public bool isStopSign = false;

    public bool isForcedStop = false;
    public float path = 0f;
    public float tempPath = 0f;
    public bool isCurve = false;
    public bool laneChange = false;
    public bool isLeftTurn = false;
    public bool isRightTurn = false;
    public bool isDodge = false;
    public bool isWaitingToDodge = false;
    private IEnumerator turnSignalIE;

    private IEnumerator hazardSignalIE;

    private float stopSignWaitTime = 1f;
    private float currentStopTime = 0f;
    private Control.PID speed_pid;
    private Control.PID steer_pid;

    public float steer_PID_kp = 1.0f;
    public float steer_PID_kd = 0.05f;
    public float steer_PID_ki = 0f;
    public float speed_PID_kp = 0.1f;
    public float speed_PID_kd = 0f;
    public float speed_PID_ki = 0f;
    public float maxSteerRate = 20f;
    private Vector3 steeringCenter;
    private Vector3[] SplineKnots = new Vector3[4]; // we need 4 knots per spline
    public int nSplinePoints = 10; // number of waypoints per spline segment
    private WaypointQueue wpQ = new WaypointQueue();
    private Queue<Vector3> splinePointQ = new Queue<Vector3>();
    private List<Vector3> splineWayPoints = new List<Vector3>(); 
    private List<Vector3> nextSplineWayPoints = new List<Vector3>();
    public float lookAheadDistance = 2.0f;
    

    private Splines.CatmullRom spline = new Splines.CatmullRom();

    #endregion

    #region mono
    private void OnEnable()
    {
        Missive.AddListener<DayNightMissive>(OnDayNightChange);
        GetDayNightState();
        speed_pid = new Control.PID();
        steer_pid = new Control.PID();
        steer_pid.SetWindupGuard(1f);
    }

    private void OnDisable()
    {
        ResetData();
        Missive.RemoveListener<DayNightMissive>(OnDayNightChange);
    }

    private void Update()
    {
        TogglePhysicsMode();

        if (Control == ControlType.Automatic)
        {
            if (isLaneDataSet)
            {
                StopTimeDespawnCheck();
                ToggleBrakeLights();
                CollisionCheck();
                EvaluateDistanceFromFocus();                  
                if (isPhysicsSimple)
                {
                    EvaluateTarget();
                }
                else
                {
                    SplineTargetTracker();
                }
                GetIsTurn();
                GetDodge();
                SetTargetSpeed();
            }
        }
        else if (Control == ControlType.FollowLane)
        {
            if (isLaneDataSet)
            {
                ToggleBrakeLights();
                CollisionCheck();
                EvaluateTarget();
                GetIsTurn();
                SetTargetSpeed();
            }
        }
        else if (Control == ControlType.Waypoints)
        {
            ToggleBrakeLights();
            CollisionCheck();
            EvaluateWaypointTarget();
            GetIsTurn();
            SetTargetSpeed();
        }

        WheelMovementSimple();
    }

    private void FixedUpdate()
    {
        if (Control == ControlType.Automatic ||
            Control == ControlType.FollowLane ||
            Control == ControlType.Waypoints)
        {
            if (isLaneDataSet)
            {
                SetTargetTurn();
                speed_pid.SetKValues(speed_PID_kp, speed_PID_kd, speed_PID_ki);
                steer_pid.SetKValues(steer_PID_kp, steer_PID_kd, steer_PID_ki);
                // update the location of the steering center at each frame based on the rear wheel collider positions
                var RL = wheelColliderRL.transform.TransformPoint(wheelColliderRL.center);
                var RR = wheelColliderRR.transform.TransformPoint(wheelColliderRR.center);
                steeringCenter = new Vector3(0.5f*(RL.x + RR.x), 0.5f*(RL.y + RR.y), 0.5f*(RL.z + RR.z));
                NPCTurn();
                NPCMove();
            }
        }

        WheelMovementComplex();
    }
    #endregion
    
    // public void OnDrawGizmos()
    // {
    //     foreach (Vector3 point in SplineKnots)
    //     {
    //         Gizmos.color = Color.yellow;
    //         Gizmos.DrawSphere(point, 1f);
    //         Gizmos.color = Color.red;
    //         Gizmos.DrawCube(currentTarget, new Vector3(1f, 1f, 1f));
    //     }
    //     foreach (Vector3 point in nextSplineWayPoints)
    //     {
    //         Gizmos.color = Color.green;
    //         Gizmos.DrawSphere(point, 0.5f);
    //     }
    //     Gizmos.color = Color.cyan;
    //     Gizmos.DrawSphere(steeringCenter, 1f);
    // }


    #region init
    public void Init()
    {
        GetNeededComponents();
        CreateCollider();
        CreatePhysicsColliders();
        CreateFrontTransforms();
        ResetData();
    }

    public void InitLaneData(MapLaneSegmentBuilder seg)
    {
        ResetData();
        wpQ.setStartLane(seg);
        normalSpeed = Random.Range(normalSpeedRange.x, normalSpeedRange.y);
        currentMapLaneSegmentBuilder = seg;
        SetLaneData(currentMapLaneSegmentBuilder.segment.targetWorldPositions);
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
                origPosWheelFR = wheelFR.localPosition;
                child.localPosition = wheelFR.localPosition;
                wheelColliderFR = child.GetComponent<WheelCollider>();
                wheelColliderFR.center = new Vector3(0f, child.localPosition.y / 2, 0f);
                wheelColliderFR.radius = wheelFR.GetComponent<Renderer>().bounds.extents.z;
                wheelColliderFR.ConfigureVehicleSubsteps(5.0f, 30, 10);
                wheelColliderFR.wheelDampingRate = wheelDampingRate;
            }
            else if (child.name.Contains("FL"))
            {
                origPosWheelFL = wheelFL.localPosition;
                child.localPosition = wheelFL.localPosition;
                wheelColliderFL = child.GetComponent<WheelCollider>();
                wheelColliderFL.center = new Vector3(0f, child.localPosition.y / 2, 0f);
                wheelColliderFL.radius = wheelFL.GetComponent<Renderer>().bounds.extents.z;
                wheelColliderFL.ConfigureVehicleSubsteps(5.0f, 30, 10);
                wheelColliderFL.wheelDampingRate = wheelDampingRate;
            }
            else if (child.name.Contains("RL"))
            {
                origPosWheelRL = wheelRL.localPosition;
                child.localPosition = wheelRL.localPosition;
                wheelColliderRL = child.GetComponent<WheelCollider>();
                wheelColliderRL.center = new Vector3(0f, child.localPosition.y / 2, 0f);
                wheelColliderRL.radius = wheelRL.GetComponent<Renderer>().bounds.extents.z;
                wheelColliderRL.ConfigureVehicleSubsteps(5.0f, 30, 10);
                wheelColliderRL.wheelDampingRate = wheelDampingRate;
            }
            else if (child.name.Contains("RR"))
            {
                origPosWheelRR = wheelRR.localPosition;
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
        go.transform.position = new Vector3(bounds.center.x + bounds.max.x + 0.1f, bounds.min.y + 1f, bounds.center.z + bounds.max.z);
        go.transform.SetParent(transform, true);
        frontRight = go.transform;
        go = new GameObject("Left");
        go.transform.position = new Vector3(bounds.center.x - bounds.max.x - 0.1f, bounds.min.y + 1f, bounds.center.z + bounds.max.z);
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
        if (FindObjectOfType<NPCManager>() == null) return;
        if (NPCManager.Instance.IsVisible(gameObject)) return;

        ResetData();
        NPCManager.Instance.DespawnNPC(gameObject);
    }

    private void ResetData()
    {
        StopAllCoroutines();
        currentMapLaneSegmentBuilder = null;
        currentIntersectionComponent = null;
        if (prevMapLaneSegmentBuilder?.stopLine?.mapIntersectionBuilder != null)
            prevMapLaneSegmentBuilder.stopLine.mapIntersectionBuilder.ExitStopSignQueue(this);
        prevMapLaneSegmentBuilder = null;
        currentNPCLightState = NPCLightStateTypes.Off;
        allRenderers.ForEach(x => SetNPCLightRenderers(x));
        currentSpeed = 0f;
        currentStopTime = 0f;
        path = 0f;
        currentSpeed_measured = 0f;
        rb.angularVelocity = Vector3.zero;
        rb.velocity = Vector3.zero;
        isCurve = false;
        isLeftTurn = false;
        isRightTurn = false;
        isWaitingToDodge = false;
        isDodge = false;
        laneChange = false;
        isStopLight = false;
        isStopSign = false;
        hasReachedStopSign = false;
        isLaneDataSet = false;
        isForcedStop = false;
        if (!isPhysicsSimple)
        {
            splinePointQ.Clear();
        }
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
        if (isPhysicsSimple)
        {
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, currentTurn * Time.deltaTime, 0f)); 
        }
        else
        {
            float dt = Time.fixedDeltaTime;
            float steer = wheelColliderFL.steerAngle;

            float deltaAngle;
            steer_pid.UpdateErrors(Time.fixedDeltaTime, steer, targetTurn);
            deltaAngle = - steer_pid.Run();

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
        if (Control != ControlType.Waypoints && Control != ControlType.FollowLane)
        {
            if (isPhysicsSimple)
                normalSpeed = Random.Range(normalSpeedRange.x, normalSpeedRange.y);
            else
                normalSpeed = Random.Range(complexPhysicsSpeedRange.x, complexPhysicsSpeedRange.y);
        }
    }

    private void ApplyTorque()
    {
        // Maintain speed at target speed
        float FRICTION_COEFFICIENT = 0.7f; // for dry wheel/pavement -- wet is about 0.4
        speed_pid.UpdateErrors(Time.fixedDeltaTime, currentSpeed_measured, targetSpeed);
        float deltaVel = - speed_pid.Run();
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

    public Vector3 GetVelocity()
    {
        return isPhysicsSimple ? simpleVelocity : rb.velocity;
    }

    public Vector3 GetAngularVelocity()
    {
        return isPhysicsSimple ? simpleAngularVelocity : rb.angularVelocity;
    }
    #endregion

    #region inputs
    private void SetTargetTurn()
    {
        if (isPhysicsSimple)
        {
            steerVector = (currentTarget - frontCenter.position).normalized;
        }
        else
        {
            steerVector = (currentTarget - steeringCenter).normalized;
        }
       
        float steer = Vector3.Angle(steerVector, frontCenter.forward) * 1.5f;
        targetTurn = Vector3.Cross(frontCenter.forward, steerVector).y < 0 ? -steer : steer;
        currentTurn += turnAdjustRate * Time.deltaTime * (targetTurn - currentTurn);

        if (targetSpeed == 0)
            currentTurn = 0;
    }

    public void ForceEStop(bool isStop)
    {
        isForcedStop = isStop;
    }

    private void SetTargetSpeed()
    {
        targetSpeed = normalSpeed;

        if (isStopSign)
        {
            if (!hasReachedStopSign)
                targetSpeed = Mathf.Clamp(GetLerpedDistanceToStopTarget() * (normalSpeed), 0f, normalSpeed); // TODO need to fix when target speed > normal speed issue
            else
                targetSpeed = 0f;
        }

        if (isStopLight)
        {
            targetSpeed = Mathf.Clamp(GetLerpedDistanceToStopTarget() * (normalSpeed), 0f, normalSpeed); // TODO need to fix when target speed > normal speed issue
            if (distanceToStopTarget < minTargetDistance)
                targetSpeed = 0f;
        }

        if (!isStopLight && !isStopSign)
        {
            if (isFrontDetectWithinStopDistance)
                targetSpeed = SetFrontDetectSpeed();

            if (isCurve)
                targetSpeed = Mathf.Lerp(targetSpeed, normalSpeed * 0.25f, Time.deltaTime * 20f);

            if (IsYieldToIntersectionLane())
            {
                if (currentMapLaneSegmentBuilder != null)
                    if (currentIndex < 2)
                        targetSpeed = normalSpeed * 0.1f;
                else
                    elapsedAccelerateTime = speedAdjustRate = targetSpeed = currentSpeed = 0f;
            }
        }

        if (isForcedStop)
        {
            targetSpeed = 0f;
        }

        if (targetSpeed > currentSpeed && elapsedAccelerateTime <= 5f)
        {
            speedAdjustRate = Mathf.Lerp(minSpeedAdjustRate, maxSpeedAdjustRate, elapsedAccelerateTime / 5f);
            elapsedAccelerateTime += Time.deltaTime;
        }
        else
        {
            speedAdjustRate = maxSpeedAdjustRate;
            elapsedAccelerateTime = 0f;
        }

        currentSpeed += speedAdjustRate * Time.deltaTime * (targetSpeed - currentSpeed);
        currentSpeed = currentSpeed < 0.01f ? 0f : currentSpeed;
        currentSpeed = currentSpeed > normalSpeed ? normalSpeed : currentSpeed;

        currentSpeed_measured = isPhysicsSimple ? (((rb.position - lastRBPosition) / Time.deltaTime).magnitude) * 2.23693629f : rb.velocity.magnitude * 2.23693629f; // MPH
        if (isPhysicsSimple && Time.deltaTime > 0)
        {
            simpleVelocity = (rb.position - lastRBPosition) / Time.deltaTime;
        
            Vector3 euler1 = lastRBRotation.eulerAngles;
            Vector3 euler2 = rb.rotation.eulerAngles;
            Vector3 diff = euler2 - euler1;
            for (int i=0; i<3; i++)
            {
                diff[i] = (diff[i] + 180) % 360 - 180;
            }
            simpleAngularVelocity = diff / Time.deltaTime * Mathf.Deg2Rad;
        }

        lastRBPosition = rb.position;
        lastRBRotation = rb.rotation;
    }

    private float GetLerpedDistanceToStopTarget()
    {
        float tempD = 0f;
        
        if (isFrontDetectWithinStopDistance) // raycast
        {
            tempD = frontClosestHitInfo.distance / stopHitDistance;
            if (frontClosestHitInfo.distance < stopHitDistance)
                tempD = 0f;
        }
        else // stop target
        {
            tempD = distanceToStopTarget > stopLineDistance ? stopLineDistance : distanceToStopTarget / stopLineDistance;
            if (distanceToStopTarget < minTargetDistance)
                tempD = 0f;
        }

        return tempD;
    }
    #endregion

    #region stopline
    IEnumerator WaitStopSign()
    {
        yield return new WaitUntil(() => distanceToStopTarget <= stopLineDistance);
        isStopSign = true;
        currentStopTime = 0f;
        hasReachedStopSign = false;
        yield return new WaitUntil(() => distanceToStopTarget < minTargetDistance);
        prevMapLaneSegmentBuilder.stopLine.mapIntersectionBuilder.EnterStopSignQueue(this);
        hasReachedStopSign = true;
        yield return new WaitForSeconds(stopSignWaitTime);
        yield return new WaitUntil(() => prevMapLaneSegmentBuilder.stopLine.mapIntersectionBuilder.CheckStopSignQueue(this));
        hasReachedStopSign = false;
        isStopSign = false;
    }

    IEnumerator WaitTrafficLight()
    {
        currentStopTime = 0f;
        yield return new WaitUntil(() => distanceToStopTarget <= stopLineDistance);
        if (prevMapLaneSegmentBuilder.stopLine.currentState == TrafficLightSetState.Green) yield break; // light is green so just go
        isStopLight = true;
        yield return new WaitUntil(() => prevMapLaneSegmentBuilder.stopLine.currentState == TrafficLightSetState.Green);
        if (isLeftTurn || isRightTurn)
            yield return new WaitForSeconds(Random.Range(1f, 2f));
        isStopLight = false;
    }

    public void RemoveFromStopSignQueue()
    {
        prevMapLaneSegmentBuilder?.stopLine?.mapIntersectionBuilder?.ExitStopSignQueue(this);
    }

    private void StopTimeDespawnCheck()
    {
        if (!NPCManager.Instance.isDespawnTimer) return;

        if (isStopLight || isStopSign || (currentSpeed_measured < 0.03))
            currentStopTime += Time.deltaTime;
        if (currentStopTime > 30f)
            Despawn();
    }

    private bool IsYieldToIntersectionLane()
    {
        bool state = false;
        if (currentMapLaneSegmentBuilder != null) // check each active vehicle if they are on a yield to lane 
        {
            for (int i = 0; i < NPCManager.Instance.currentPooledNPCs.Count; i++)
            {
                if (NPCManager.Instance.currentPooledNPCs[i].activeInHierarchy)
                {
                    var npcC = NPCManager.Instance.currentPooledNPCs[i].GetComponent<NPCControllerComponent>();
                    if (npcC)
                    {
                        for (int k = 0; k < currentMapLaneSegmentBuilder.yieldToLanes.Count; k++)
                        {
                            if (npcC.currentMapLaneSegmentBuilder != null)
                            {
                                if (npcC.currentMapLaneSegmentBuilder == currentMapLaneSegmentBuilder.yieldToLanes[k])
                                    state = true;
                            }
                        }
                    }
                }
            }
        }
        if (prevMapLaneSegmentBuilder != null && prevMapLaneSegmentBuilder.stopLine != null) // light is red so oncoming traffic should be stopped already if past stopline
            if (prevMapLaneSegmentBuilder.stopLine.currentState == TrafficLightSetState.Red && Vector3.Dot(transform.TransformDirection(Vector3.forward), prevMapLaneSegmentBuilder.stopLine.transform.TransformDirection(Vector3.forward)) > 0.7f)
                state = false;

        if (currentMapLaneSegmentBuilder != null) // already in intersection so just go
            if (currentIndex > 1)
                state = false;

        return state;
    }
    #endregion

    #region targeting
    public void SetLaneData(List<Vector3> data)
    {
        currentIndex = 0;
        laneData = new List<Vector3>(data);
        isDodge = false;

        if (isPhysicsSimple)
        {
            currentTarget = laneData[++currentIndex];
        }
        else
        {
            for (int i = 0; i < SplineKnots.Length; i++)
            {
                SplineKnots[i] = wpQ.Dequeue();
            }

            spline.SetPoints(SplineKnots);
            splineWayPoints = spline.GetSplineWayPoints(nSplinePoints);

            if (splinePointQ.Count == 0)
            {
                foreach (Vector3 pt in splineWayPoints)
                {
                    splinePointQ.Enqueue(pt);
                }
            }

            currentTarget = splinePointQ.Dequeue();
        }
    }

    private void SetChangeLaneData(List<Vector3> data)
    {
        laneData = new List<Vector3>(data);
        currentTarget = laneData[currentIndex];
        isDodge = false; // ???
    }

    private void EvaluateWaypointTarget()
    {
        var distance2 = Vector3.SqrMagnitude(transform.position - currentTarget);

        if (distance2 < 1f)
        {
            Api.ApiManager.Instance.AddWaypointReached(gameObject, currentIndex);

            if (++currentIndex < laneData.Count)
            {
                currentTarget = laneData[currentIndex];
                normalSpeed = laneSpeed[currentIndex];
            }
            else if (waypointLoop)
            {
                currentIndex = 0;
                currentTarget = laneData[0];
                normalSpeed = laneSpeed[0];
            }
            else
            {
                Control = ControlType.Manual;
            }
        }
    }

    private void SplineTargetTracker()
    {
        if (!gameObject.activeInHierarchy) return;

        distanceToCurrentTarget = Vector3.Distance(new Vector3(frontCenter.position.x, 0f, frontCenter.position.z), new Vector3(currentTarget.x, 0f, currentTarget.z));      
        if (Vector3.Dot(frontCenter.forward, (currentTarget - frontCenter.position).normalized) < 0 || distanceToCurrentTarget < lookAheadDistance)
        {
            if (splinePointQ.Count == 0)
            {
                // move spline
                SplineKnots[0] = SplineKnots[1];
                SplineKnots[1] = SplineKnots[2];
                SplineKnots[2] = SplineKnots[3];
                SplineKnots[3] = wpQ.Dequeue();
                
                spline.SetPoints(SplineKnots);
                nextSplineWayPoints = spline.GetSplineWayPoints(nSplinePoints);
                foreach (Vector3 pt in nextSplineWayPoints)
                {
                    splinePointQ.Enqueue(pt);
                }
            }

            currentTarget = splinePointQ.Dequeue();

            // Check if target is a stop target
            if (wpQ.StopTarget.isStopAhead && wpQ.previousLane?.stopLine != null)
            {
                distanceToStopTarget = Vector3.Distance(new Vector3(frontCenter.position.x, 0f, frontCenter.position.z), new Vector3(wpQ.StopTarget.waypoint.x, 0f, wpQ.StopTarget.waypoint.z));
                currentIntersectionComponent = wpQ.previousLane.stopLine?.mapIntersectionBuilder?.intersectionC;
                prevMapLaneSegmentBuilder = wpQ.previousLane;
                if (prevMapLaneSegmentBuilder.stopLine.mapIntersectionBuilder != null) // null if map not setup right TODO add check to report missing stopline
                {
                    if (prevMapLaneSegmentBuilder.stopLine.mapIntersectionBuilder.isStopSign) // stop sign
                    {
                        StartCoroutine(WaitStopSign());
                        wpQ.StopTarget.isStopAhead = false;
                    }
                    else
                    {
                        StartCoroutine(WaitTrafficLight());
                    }
                }
            }
        }
    }

    private void EvaluateTarget()
    {
        distanceToCurrentTarget = Vector3.Distance(new Vector3(frontCenter.position.x, 0f, frontCenter.position.z), new Vector3(currentTarget.x, 0f, currentTarget.z));
        distanceToStopTarget = Vector3.Distance(new Vector3(frontCenter.position.x, 0f, frontCenter.position.z), new Vector3(stopTarget.x, 0f, stopTarget.z));

        if (distanceToStopTarget < 1f)
        {
            if (!atStopTarget)
            {
                Api.ApiManager.Instance?.AddStopLine(gameObject);
                atStopTarget = true;
            }
        }
        else
        {
            atStopTarget = false;
        }

        if (Vector3.Dot(frontCenter.forward, (currentTarget - frontCenter.position).normalized) < 0 || distanceToCurrentTarget < 1f)
        {
            if (currentIndex == laneData.Count - 2) // reached 2nd to last target index see if stop line is present
            {
                StartStoppingCoroutine();
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
    
    private void GetNextLane()
    {
        // last index of current lane data
        if (currentMapLaneSegmentBuilder?.nextConnectedLanes.Count >= 1) // choose next path and set waypoints
        {
            currentMapLaneSegmentBuilder = currentMapLaneSegmentBuilder.nextConnectedLanes[(int)Random.Range(0, currentMapLaneSegmentBuilder.nextConnectedLanes.Count)];
            SetLaneData(currentMapLaneSegmentBuilder.segment.targetWorldPositions);
            SetTurnSignal();
            StartCoroutine(DelayChangeLane());
        }
        else // issue getting new waypoints so despawn
        {
            // TODO raycast to see adjacent lanes? Need system
            Despawn();
        }
    }

    private IEnumerator DelayChangeLane()
    {
        if (Control == ControlType.Waypoints) yield break;
        if (!currentMapLaneSegmentBuilder.isTrafficLane) yield break;
        if (Random.Range(0, 3) == 1) yield break;
        if (!(laneChange)) yield break;

        if (currentMapLaneSegmentBuilder.leftForward != null)
        {
            isLeftTurn = true;
            SetNPCTurnSignal();
        }
        else if (currentMapLaneSegmentBuilder.rightForward != null)
        {
            isRightTurn = true;
            SetNPCTurnSignal();
        }

        yield return new WaitForSeconds(Random.Range(0f, 2f));

        if (currentIndex >= laneData.Count - 2)
        {
            isLeftTurn = isRightTurn = false;
            yield break;
        }

        SetLaneChange();
    }

    private void SetLaneChange()
    {
        Api.ApiManager.Instance?.AddLaneChange(gameObject);

        if (currentMapLaneSegmentBuilder.leftForward != null)
        {
            if (!isFrontLeftDetect)
            {
                currentMapLaneSegmentBuilder = currentMapLaneSegmentBuilder.leftForward;
                SetChangeLaneData(currentMapLaneSegmentBuilder.segment.targetWorldPositions);
                StartCoroutine(DelayOffTurnSignals());
            }
        }
        else if (currentMapLaneSegmentBuilder.rightForward != null)
        {
            if (!isFrontRightDetect)
            {
                currentMapLaneSegmentBuilder = currentMapLaneSegmentBuilder.rightForward;
                SetChangeLaneData(currentMapLaneSegmentBuilder.segment.targetWorldPositions);
                StartCoroutine(DelayOffTurnSignals());
            }
        }
    }

    public void ForceLaneChange(bool isLeft)
    {
        if (isLeft)
        {
            if (currentMapLaneSegmentBuilder.leftForward != null)
            {
                if (!isFrontLeftDetect)
                {
                    currentMapLaneSegmentBuilder = currentMapLaneSegmentBuilder.leftForward;
                    SetChangeLaneData(currentMapLaneSegmentBuilder.segment.targetWorldPositions);
                    StartCoroutine(DelayOffTurnSignals());
                    Api.ApiManager.Instance?.AddLaneChange(gameObject);
                }
            }
        }
        else
        {
            if (currentMapLaneSegmentBuilder.rightForward != null)
            {
                if (!isFrontRightDetect)
                {
                    currentMapLaneSegmentBuilder = currentMapLaneSegmentBuilder.rightForward;
                    SetChangeLaneData(currentMapLaneSegmentBuilder.segment.targetWorldPositions);
                    StartCoroutine(DelayOffTurnSignals());
                    Api.ApiManager.Instance?.AddLaneChange(gameObject);
                }
            }
        }
    }

    private void GetDodge()
    {
        // dodge
        if (isDodge) return;
        if (isFrontDetectWithinStopDistance)
        {
            NPCControllerComponent npcC = frontClosestHitInfo.collider.gameObject.GetComponent<NPCControllerComponent>();
            VehicleController vC = frontClosestHitInfo.collider.transform.root.GetComponent<VehicleController>();

            if (npcC)
            {
                if ((isLeftTurn && npcC.isLeftTurn || isRightTurn && npcC.isRightTurn) && Vector3.Dot(transform.TransformDirection(Vector3.forward), npcC.transform.TransformDirection(Vector3.forward)) < -0.7f)
                    if (currentIndex > 1)
                        SetDodge(isLeftTurn, true);
            }
            else if (vC)
            {
                //
            }
            else
            {
                //
            }
        }

        if (isLeftDetectWithinStopDistance || isRightDetectWithinStopDistance)
        {
            if (currentMapLaneSegmentBuilder == null) return;
            if (!currentMapLaneSegmentBuilder.isTrafficLane) return;

            // ignore npc or vc for now
            if (isLeftDetectWithinStopDistance)
            {
                var npcC = leftClosestHitInfo.collider.GetComponent<NPCControllerComponent>();
                if (npcC != null)
                {
                    isFrontDetectWithinStopDistance = true;
                    frontClosestHitInfo = leftClosestHitInfo;
                }

                var vC = leftClosestHitInfo.collider.transform.root.GetComponent<VehicleController>();
                if (vC != null)
                {
                    isFrontDetectWithinStopDistance = true;
                    frontClosestHitInfo = leftClosestHitInfo;
                    if (!isWaitingToDodge)
                        StartCoroutine(WaitToDodge(vC, true));
                }
                if (leftClosestHitInfo.collider.gameObject.GetComponent<NPCControllerComponent>() == null && leftClosestHitInfo.collider.transform.root.GetComponent<VehicleController>() == null)
                    SetDodge(false);
            }

            if (isRightDetectWithinStopDistance && !isDodge)
            {
                var npcC = rightClosestHitInfo.collider.GetComponent<NPCControllerComponent>();
                if (npcC != null)
                {
                    isFrontDetectWithinStopDistance = true;
                    frontClosestHitInfo = rightClosestHitInfo;
                }

                var vC = rightClosestHitInfo.collider.transform.root.GetComponent<VehicleController>();
                if (vC != null)
                {
                    isFrontDetectWithinStopDistance = true;
                    frontClosestHitInfo = rightClosestHitInfo;
                    if (!isWaitingToDodge)
                        StartCoroutine(WaitToDodge(vC, false));
                }
                if (rightClosestHitInfo.collider.gameObject.GetComponent<NPCControllerComponent>() == null && rightClosestHitInfo.collider.transform.root.GetComponent<VehicleController>() == null)
                    SetDodge(true);
            }
        }
    }

    IEnumerator WaitToDodge(VehicleController vC, bool isLeft)
    {
        isWaitingToDodge = true;
        float elapsedTime = 0f;
        while (elapsedTime < 5f)
        {
            if (vC.GetComponent<Rigidbody>().velocity.magnitude > 0.01f)
            {
                isWaitingToDodge = false;
                yield break;
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (!isLeft)
            SetDodge(true);
        else
            SetDodge(false);
        isWaitingToDodge = false;
    }

    private void SetDodge(bool isLeft, bool isShortDodge = false)
    {
        if (isStopSign || isStopLight) return;

        Transform startTransform = isLeft ? frontLeft : frontRight;
        float firstDodgeAngle = isLeft ? -15f : 15f;
        float secondDodgeAngle = isLeft ? -5f : 5f;
        float shortDodgeAngle = isLeft ? -40f : 40f;
        Vector3 dodgeTarget;
        var dodgeData = new List<Vector3>();

        if ((isLeft && isFrontLeftDetect && !isShortDodge) || (!isLeft && isFrontRightDetect && !isShortDodge)) return;
        if ((isLeft && currentMapLaneSegmentBuilder.leftForward == null && !isShortDodge) || (!isLeft && currentMapLaneSegmentBuilder.rightForward == null && !isShortDodge)) return;

        isDodge = true;

        if (isShortDodge)
        {
            dodgeTarget = Quaternion.Euler(0f, shortDodgeAngle, 0f) * (startTransform.forward * 4f);
            Vector3 tempV = startTransform.position + (dodgeTarget.normalized * dodgeTarget.magnitude);
            dodgeData.Add(new Vector3(tempV.x, laneData[currentIndex].y, tempV.z));
            //Debug.DrawRay(startTransform.position, dodgeTarget, Color.blue, 0.25f);
            //if (currentIndex != laneData.Count - 1)
            //    laneData.RemoveRange(currentIndex, laneData.Count - currentIndex);
        }
        else
        {
            dodgeTarget = Quaternion.Euler(0f, firstDodgeAngle, 0f) * (startTransform.forward);
            Vector3 tempV = startTransform.position + (dodgeTarget.normalized * dodgeTarget.magnitude);
            dodgeData.Add(new Vector3(tempV.x, laneData[currentIndex].y, tempV.z));
            //Debug.DrawRay(startTransform.position, dodgeTarget, Color.red, 0.25f);
            dodgeTarget = Quaternion.Euler(0f, secondDodgeAngle, 0f) * (startTransform.forward * 10f);
            tempV = startTransform.position + (dodgeTarget.normalized * dodgeTarget.magnitude);
            dodgeData.Add(new Vector3(tempV.x, laneData[currentIndex].y, tempV.z));
            //Debug.DrawRay(startTransform.position, dodgeTarget, Color.yellow, 0.25f);
            if (Vector3.Distance(startTransform.position + (dodgeTarget.normalized * dodgeTarget.magnitude), laneData[currentIndex]) < 12 && currentIndex != laneData.Count - 1)
            {
                if (Control == ControlType.Waypoints)
                {
                    laneSpeed.RemoveAt(currentIndex);
                }
                laneData.RemoveAt(currentIndex);
            }
        }
        laneData.InsertRange(currentIndex, dodgeData);
        if (Control == ControlType.Waypoints)
        {
            laneSpeed.InsertRange(currentIndex, Enumerable.Repeat(laneSpeed[currentIndex], dodgeData.Count));
        }
        currentTarget = laneData[currentIndex];

    }

    private IEnumerator DelayOffTurnSignals()
    {
        yield return new WaitForSeconds(3f);
        isLeftTurn = isRightTurn = false;
        SetNPCTurnSignal();
    }

    private void SetTurnSignal(bool forceLeftTS = false, bool forceRightTS = false)
    {
        isLeftTurn = false;
        isRightTurn = false;
        if (currentMapLaneSegmentBuilder != null)
        {
            switch (currentMapLaneSegmentBuilder.laneTurnType)
            {
                case LaneTurnType.None:
                    isLeftTurn = false;
                    isRightTurn = false;
                    break;
                case LaneTurnType.Left:
                    isLeftTurn = true;
                    break;
                case LaneTurnType.Right:
                    isRightTurn = true;
                    break;
                default:
                    break;
            }
        }
        SetNPCTurnSignal();
    }

    private void GetIsLeftOrRightTurn()
    {
        if (currentMapLaneSegmentBuilder == null) return;
        Vector3 heading = (currentMapLaneSegmentBuilder.segment.targetWorldPositions[currentMapLaneSegmentBuilder.segment.targetWorldPositions.Count - 1] - currentMapLaneSegmentBuilder.segment.targetWorldPositions[0]).normalized;
        Vector3 perp = Vector3.Cross(transform.forward, heading);
        tempPath = Vector3.Dot(perp, transform.up);
        if (tempPath < -0.2f)
            isLeftTurn = true;
        else if (tempPath > 0.2f)
            isRightTurn = true;
    }

    private void GetIsTurn()
    {
        if (currentMapLaneSegmentBuilder == null) return;
        path = transform.InverseTransformPoint(currentTarget).x;
        isCurve = path < -1f || path > 1f ? true : false;
    }
    #endregion

    #region lights
    public void ForceNPCLights(int intensity)
    {
        switch(intensity)
        {
            case 0:
                currentNPCLightState = NPCLightStateTypes.Off;
                break;
            case 1:
                currentNPCLightState = NPCLightStateTypes.Low;
                break;
            case 2:
                currentNPCLightState = NPCLightStateTypes.High;
                break;
            default:
                break;
        }
        headLights.ForEach(x => SetNPCLights(x));
        headLightRenderers.ForEach(x => SetNPCLightRenderers(x, true));
        tailLightRenderers.ForEach(x => SetNPCLightRenderers(x));
        brakeLightRenderers.ForEach(x => SetNPCLightRenderers(x));
    }

    public void ForceNPCHazards(bool isOn)
    {
        if (hazardSignalIE != null)
        {
            StopCoroutine(hazardSignalIE);
        }

        if (isOn)
        {
            isLeftTurn = true;
            isRightTurn = true;
            hazardSignalIE = StartHazardSignal();
            StartCoroutine(hazardSignalIE);
        }
        else
        {
            isLeftTurn = false;
            isRightTurn = false;
            StopCoroutine(hazardSignalIE);
        }
    }

    public void ForceNPCTurnSignal(bool isLeftTS = false, bool isRightTS = false)
    {
        if (turnSignalIE != null)
        {
            StopCoroutine(turnSignalIE);
        }

        if (isLeftTS && isRightTS)
        {
            ForceNPCHazards(true);
            return;
        }

        if (isLeftTS)
        {
            isLeftTurn = true;
            isRightTurn = false;
            turnSignalIE = StartTurnSignal();
            StartCoroutine(turnSignalIE);
        }
        else if (isRightTS)
        {
            isLeftTurn = false;
            isRightTurn = true;
            turnSignalIE = StartTurnSignal();
            StartCoroutine(turnSignalIE);
        }
        else 
        {
            isLeftTurn = false;
            isRightTurn = false;
        }
    }

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

    private void ToggleBrakeLights()
    {
        if (targetSpeed < 2f || isStopLight || isFrontDetectWithinStopDistance || (isStopSign && distanceToStopTarget < stopLineDistance))
            brakeLightRenderers.ForEach(x => SetNPCLightRenderers(x, false, true));
        else
            brakeLightRenderers.ForEach(x => SetNPCLightRenderers(x));
    }

    private void SetNPCTurnSignal()
    {
        if (turnSignalLeftRenderers == null || turnSignalRightRenderers == null) return;
        if (turnSignalIE != null)
            StopCoroutine(turnSignalIE);
        turnSignalIE = StartTurnSignal();
        StartCoroutine(turnSignalIE);
    }

    private IEnumerator StartTurnSignal()
    {
        if (turnSignalLeftRenderers == null || turnSignalRightRenderers == null)
        {
            Debug.Log("Missing turn signals! Make sure SignalLightRight and SignalLightLeft are present");
            yield break;
        }
        while (isLeftTurn || isRightTurn)
        {
            if (isLeftTurn) turnSignalLeftRenderers.ForEach(r => SetTurnLight(r, true));
            if (isRightTurn) turnSignalRightRenderers.ForEach(r => SetTurnLight(r, true));
            yield return new WaitForSeconds(0.5f);
            if (isLeftTurn) turnSignalLeftRenderers.ForEach(r => SetTurnLight(r, false));
            if (isRightTurn) turnSignalRightRenderers.ForEach(r => SetTurnLight(r, false));
            yield return new WaitForSeconds(0.5f);
        }
        turnSignalLeftRenderers.ForEach(r => SetTurnLight(r, false));
        turnSignalRightRenderers.ForEach(r => SetTurnLight(r, false));
        yield break;
    }

    private IEnumerator StartHazardSignal()
    {
        if (turnSignalLeftRenderers == null || turnSignalRightRenderers == null)
        {
            Debug.Log("Missing turn signals! Make sure SignalLightRight and SignalLightLeft are present");
            yield break;
        }
        while (isLeftTurn && isRightTurn)
        {
            turnSignalLeftRenderers.ForEach(r => SetTurnLight(r, true));
            turnSignalRightRenderers.ForEach(r => SetTurnLight(r, true));
            yield return new WaitForSeconds(0.5f);
            turnSignalLeftRenderers.ForEach(r => SetTurnLight(r, false));
            turnSignalRightRenderers.ForEach(r => SetTurnLight(r, false));
            yield return new WaitForSeconds(0.5f);
        }
        turnSignalLeftRenderers.ForEach(r => SetTurnLight(r, false));
        turnSignalRightRenderers.ForEach(r => SetTurnLight(r, false));
        yield break;
    }

    private void SetTurnLight(Renderer rend, bool state)
    {
        if (state)
        {
            foreach (var mat in rend.materials)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.white);
            }
        }
        else
        {
            foreach (var mat in rend.materials)
            {
                mat.SetColor("_EmissionColor", Color.black);
                mat.DisableKeyword("_EMISSION");
            }
        }
    }

    private void SetNPCLightRenderers(Renderer renderer, bool isHeadLightRenderer = false, bool isBrake = false)
    {
        switch (currentNPCLightState)
        {
            case NPCLightStateTypes.Off:
                if (isBrake)
                {
                    foreach (var mat in renderer.materials)
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", Color.white);
                    }
                }
                else
                {
                    foreach (var mat in renderer.materials)
                    {
                        mat.DisableKeyword("_EMISSION");
                    }
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
        if (steerVector == Vector3.zero || currentSpeed < 0.1f) return;

        if (isPhysicsSimple)
        {
            if (wheelFR.localPosition != origPosWheelFR)
                wheelFR.localPosition = origPosWheelFR;
            if (wheelFL.localPosition != origPosWheelFL)
                wheelFL.localPosition = origPosWheelFL;
            if (wheelRL.localPosition != origPosWheelRL)
                wheelRL.localPosition = origPosWheelRL;
            if (wheelRR.localPosition != origPosWheelRR)
                wheelRR.localPosition = origPosWheelRR;

            float theta = (currentSpeed * Time.deltaTime / wheelColliderFR.radius) * Mathf.Rad2Deg;

            Quaternion finalQ = Quaternion.LookRotation(steerVector);
            Vector3 finalE = finalQ.eulerAngles;
            finalQ = Quaternion.Euler(0f, finalE.y, 0f);
            
            wheelFR.rotation = Quaternion.RotateTowards(wheelFR.rotation, finalQ, Time.deltaTime * 50f);
            wheelFL.rotation = Quaternion.RotateTowards(wheelFL.rotation, finalQ, Time.deltaTime * 50f);
            wheelFR.transform.Rotate(Vector3.right, theta, Space.Self);
            wheelFL.transform.Rotate(Vector3.right, theta, Space.Self);
            wheelRL.transform.Rotate(Vector3.right, theta, Space.Self);
            wheelRR.transform.Rotate(Vector3.right, theta, Space.Self);
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

    //private void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.green;
    //    Gizmos.DrawWireSphere(frontLeft.position - (frontLeft.right * 2), 1f);
    //    Gizmos.DrawWireSphere(frontRight.position + (frontRight.right * 2), 1f);
    //}

    private void CollisionCheck()
    {
        if (frontCenter == null || frontLeft == null || frontRight == null) return;
        
        frontClosestHitInfo = new RaycastHit();
        rightClosestHitInfo = new RaycastHit();
        leftClosestHitInfo = new RaycastHit();

        Physics.Raycast(frontCenter.position, frontCenter.forward, out frontClosestHitInfo, frontRaycastDistance, carCheckBlockBitmask);
        Physics.Raycast(frontRight.position, frontRight.forward, out rightClosestHitInfo, frontRaycastDistance, carCheckBlockBitmask);
        Physics.Raycast(frontLeft.position, frontLeft.forward, out leftClosestHitInfo, frontRaycastDistance, carCheckBlockBitmask);
        isFrontLeftDetect = Physics.CheckSphere(frontLeft.position - (frontLeft.right * 2), 1f, carCheckBlockBitmask);
        isFrontRightDetect = Physics.CheckSphere(frontRight.position + (frontRight.right * 2), 1f, carCheckBlockBitmask);
        
        isFrontDetectWithinStopDistance = (frontClosestHitInfo.collider) && frontClosestHitInfo.distance < stopHitDistance;
        isRightDetectWithinStopDistance = (rightClosestHitInfo.collider) && rightClosestHitInfo.distance < stopHitDistance;
        isLeftDetectWithinStopDistance = (leftClosestHitInfo.collider) && leftClosestHitInfo.distance < stopHitDistance;

        // ground collision
        groundCheckInfo = new RaycastHit();
        if (!Physics.Raycast(transform.position + Vector3.up, Vector3.down, out groundCheckInfo, 5f, groundHitBitmask))
            Despawn();
    }

    private float SetFrontDetectSpeed()
    {
        var blocking = frontClosestHitInfo.transform;

        float tempS = 0f;
        if (Vector3.Dot(transform.forward, blocking.transform.forward) > 0.7f) // detected is on similar vector
        {
            if (frontClosestHitInfo.distance > stopHitDistance)
                tempS = (normalSpeed) * (frontClosestHitInfo.distance / stopHitDistance);
        }
        return tempS;
    }
    #endregion

    public void SetFollowClosestLane(float maxSpeed, bool isLaneChange)
    {
        laneChange = isLaneChange;
        Control = ControlType.FollowLane;

        var position = transform.position;

        var seg = MapManager.Instance.GetClosestLane(position);
        InitLaneData(seg);
        var segment = seg.segment;

        int index = -1;
        float minDist = float.PositiveInfinity;
        Vector3 closest = Vector3.zero;

        // choose closest waypoint
        for (int i = 0; i < segment.targetWorldPositions.Count - 1; i++)
        {
            var p0 = segment.targetWorldPositions[i];
            var p1 = segment.targetWorldPositions[i + 1];

            var p = MapManager.ClosetPointOnSegment(p0, p1, position);

            float d = Vector3.SqrMagnitude(position - p);
            if (d < minDist)
            {
                minDist = d;
                index = i;
                closest = p;
            }
        }

        if (closest != segment.targetWorldPositions[index])
        {
            index++;
        }

        currentTarget = segment.targetWorldPositions[index];
        currentIndex = index;

        stopTarget = segment.targetWorldPositions[segment.targetWorldPositions.Count - 1];
        currentIntersectionComponent = seg.stopLine?.mapIntersectionBuilder?.intersectionC;

        distanceToCurrentTarget = Vector3.Distance(new Vector3(frontCenter.position.x, 0f, frontCenter.position.z), new Vector3(currentTarget.x, 0f, currentTarget.z));
        distanceToStopTarget = Vector3.Distance(new Vector3(frontCenter.position.x, 0f, frontCenter.position.z), new Vector3(stopTarget.x, 0f, stopTarget.z));

        if (currentIndex >= laneData.Count - 2)
        {
            StartStoppingCoroutine();
        }

        normalSpeed = maxSpeed;
    }

    void StartStoppingCoroutine()
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
                else
                {
                    StartCoroutine(WaitTrafficLight());
                }
            }
        }
    }

    public void SetFollowWaypoints(List<Api.DriveWaypoint> waypoints, bool loop)
    {
        waypointLoop = loop;

        laneData = waypoints.Select(wp => wp.Position).ToList();
        laneSpeed = waypoints.Select(wp => wp.Speed).ToList();

        ResetData();

        currentIndex = 0;
        currentTarget = laneData[0];
        normalSpeed = laneSpeed[0];
        isLaneDataSet = true;

        Control = ControlType.Waypoints;
    }

    public void SetManualControl()
    {
        Control = ControlType.Manual;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer != LayerMask.NameToLayer("Ground And Road") && collision.gameObject.layer != LayerMask.NameToLayer("NPC"))
        {
            isForcedStop = true;
            ForceNPCHazards(true);

            if (ROSAgentManager.Instance.currentMode == StartModeTypes.API)
                Api.ApiManager.Instance.AddCollision(gameObject, collision);
        } 
    }
}
