/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Simulator.Api;
using Simulator.Map;
using Simulator.Utilities;
using Simulator;

public class NPCController : MonoBehaviour
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
    private bool isPhysicsSimple = true;
    private BoxCollider simpleBoxCollider;
    private BoxCollider complexBoxCollider;
    private Vector3 lastRBPosition;
    private Vector3 simpleVelocity;
    private Quaternion lastRBRotation;
    private Vector3 simpleAngularVelocity;
    private Rigidbody rb;
    private Bounds bounds;
    private RaycastHit frontClosestHitInfo = new RaycastHit();
    private RaycastHit leftClosestHitInfo = new RaycastHit();
    private RaycastHit rightClosestHitInfo = new RaycastHit();
    private RaycastHit groundCheckInfo = new RaycastHit();
    private float frontRaycastDistance = 20f;
    private float stopHitDistance = 5f;
    private float stopLineDistance = 15f;
    private bool atStopTarget;

    private float brakeTorque = 0f;
    private float motorTorque = 0f;
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
    public MapLane currentMapLane;
    public MapLane prevMapLane;
    public MapIntersection currentIntersection = null;
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
    public Vector3 currentVelocity = Vector3.zero;
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

    // mats
    private Renderer bodyRenderer;
    private int headLightMatIndex;
    private int brakeLightMatIndex;
    private int indicatorLeftMatIndex;
    private int indicatorRightMatIndex;
    private int indicatorReverseMatIndex;

    // lights
    private Light[] allLights;
    private List<Light> headLights = new List<Light>();
    private List<Light> brakeLights = new List<Light>();
    private List<Light> indicatorLeftLights = new List<Light>();
    private List<Light> indicatorRightLights = new List<Light>();
    private Light indicatorReverseLight;

    private enum NPCLightStateTypes
    {
        Off,
        Low,
        High
    };
    private NPCLightStateTypes currentNPCLightState = NPCLightStateTypes.Off;
    private Color runningLightEmissionColor = new Color(0.65f, 0.65f, 0.65f);
    //private float fogDensityThreshold = 0.01f;
    private float lowBeamEmission = 200f;
    private float highBeamEmission = 300f;

    private bool isLaneDataSet = false;
    public bool isFrontDetectWithinStopDistance = false;
    public bool isRightDetectWithinStopDistance = false;
    public bool isLeftDetectWithinStopDistance = false;
    public bool isFrontLeftDetect = false;
    public bool isFrontRightDetect = false;
    public bool hasReachedStopSign = false;
    public bool isStopLight = false;
    public bool isStopSign = false;
    private bool isReverse = false;
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
    private PID speed_pid;
    private PID steer_pid;

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
    private WaypointQueue wpQ;
    private Queue<Vector3> splinePointQ = new Queue<Vector3>();
    private List<Vector3> splineWayPoints = new List<Vector3>();
    private List<Vector3> nextSplineWayPoints = new List<Vector3>();
    public float lookAheadDistance = 2.0f;
    private System.Random RandomGenerator;
    private NPCManager Manager;
    private Coroutine[] Coroutines = new Coroutine[System.Enum.GetNames(typeof(CoroutineID)).Length];

    private enum CoroutineID
    {
        WaitStopSign = 0,
        WaitTrafficLight = 1,
        DelayChangeLane = 2,
        DelayOffTurnSignals = 3,
        WaitToDodge = 4,
        StartTurnSignal = 5,
        StartHazardSignal = 6,
    }

    private CatmullRom spline = new CatmullRom();
    #endregion

    #region mono
    private void OnEnable()
    {
        SimulatorManager.Instance.EnvironmentEffectsManager.TimeOfDayChanged += OnTimeOfDayChange;
        GetSimulatorTimeOfDay();
        speed_pid = new PID();
        steer_pid = new PID();
        steer_pid.SetWindupGuard(1f);
    }

    private void OnDisable()
    {
        ResetData();
        SimulatorManager.Instance.EnvironmentEffectsManager.TimeOfDayChanged -= OnTimeOfDayChange;
    }

    public void PhysicsUpdate()
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
                steeringCenter = new Vector3(0.5f * (RL.x + RR.x), 0.5f * (RL.y + RR.y), 0.5f * (RL.z + RR.z));
                NPCTurn();
                NPCMove();
            }
        }

        WheelMovementComplex();
    }

    private void OnDestroy()
    {
        Resources.UnloadUnusedAssets();
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
    public void Init(int seed)
    {
        Manager = SimulatorManager.Instance.NPCManager;
        RandomGenerator = new System.Random(seed);
        wpQ = new WaypointQueue(seed);
        SetNeededComponents();
        ResetData();
    }

    public void InitLaneData(MapLane lane)
    {
        ResetData();
        wpQ.setStartLane(lane);
        normalSpeed = RandomGenerator.NextFloat(normalSpeedRange.x, normalSpeedRange.y);
        currentMapLane = lane;
        SetLaneData(currentMapLane.mapWorldPositions);
        isLaneDataSet = true;
    }

    private void SetNeededComponents()
    {
        groundHitBitmask = LayerMask.GetMask("Default");
        carCheckBlockBitmask = LayerMask.GetMask("Agent", "NPC", "Pedestrian");

        rb = GetComponent<Rigidbody>();
        var allRenderers = GetComponentsInChildren<Renderer>().ToList();
        allLights = GetComponentsInChildren<Light>();
        
        foreach (Renderer child in allRenderers)
        {
            if (child.name.Contains("RightFront"))
                wheelFR = child.transform;
            if (child.name.Contains("LeftFront"))
                wheelFL = child.transform;
            if (child.name.Contains("LeftRear"))
                wheelRL = child.transform;
            if (child.name.Contains("RightRear"))
                wheelRR = child.transform;
            if (child.name.Contains("Body"))
            {
                bodyRenderer = child;
                var allBodyMats = child.materials;
                for (int i = 0; i < allBodyMats.Length; i++)
                {
                    if (allBodyMats[i].name.Contains("LightHead"))
                        headLightMatIndex = i;
                    if (allBodyMats[i].name.Contains("LightBrake"))
                        brakeLightMatIndex = i;
                    if (allBodyMats[i].name.Contains("IndicatorLeft"))
                        indicatorLeftMatIndex = i;
                    if (allBodyMats[i].name.Contains("IndicatorRight"))
                        indicatorRightMatIndex = i;
                    if (allBodyMats[i].name.Contains("IndicatorReverse"))
                        indicatorReverseMatIndex = i;
                }
            }
        }

        foreach (Light light in allLights)
        {
            if (light.name.Contains("Head"))
                headLights.Add(light);
            if (light.name.Contains("Brake"))
                brakeLights.Add(light);
            if (light.name.Contains("IndicatorLeft"))
                indicatorLeftLights.Add(light);
            if (light.name.Contains("IndicatorRight"))
                indicatorRightLights.Add(light);
            if (light.name.Contains("IndicatorReverse"))
                indicatorReverseLight = light;
        }

        // simple collider
        bounds = new Bounds(transform.position, Vector3.zero);
        foreach (Renderer renderer in allRenderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }
        simpleBoxCollider = gameObject.AddComponent<BoxCollider>();
        simpleBoxCollider.size = bounds.size;
        simpleBoxCollider.center = new Vector3(simpleBoxCollider.center.x, bounds.size.y / 2, simpleBoxCollider.center.z);
        rb.centerOfMass = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z + bounds.max.z * 0.5f);

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

        // complex colliders
        wheelColliderHolder = new GameObject("WheelColliderHolder");
        wheelColliderHolder.transform.SetParent(transform.GetChild(0));

        GameObject goFR = new GameObject("RightFront");
        goFR.transform.SetParent(wheelColliderHolder.transform);
        wheelColliderFR = goFR.AddComponent<WheelCollider>();
        wheelColliderFR.mass = 30f;
        wheelColliderFR.suspensionDistance = 0.3f;
        wheelColliderFR.forceAppPointDistance = 0.2f;
        wheelColliderFR.suspensionSpring = new JointSpring { spring = 35000f, damper = 8000, targetPosition = 0.5f };
        wheelColliderFR.forwardFriction = new WheelFrictionCurve { extremumSlip = 0.4f, extremumValue = 1f, asymptoteSlip = 0.8f, asymptoteValue = 0.5f, stiffness = 1.2f };
        wheelColliderFR.sidewaysFriction = new WheelFrictionCurve { extremumSlip = 0.2f, extremumValue = 1f, asymptoteSlip = 1.5f, asymptoteValue = 1f, stiffness = 2.2f };
        origPosWheelFR = wheelFR.localPosition;
        goFR.transform.localPosition = wheelFR.localPosition;
        wheelColliderFR.center = new Vector3(0f, goFR.transform.localPosition.y / 2, 0f);
        wheelColliderFR.radius = wheelFR.GetComponent<Renderer>().bounds.extents.z;
        wheelColliderFR.ConfigureVehicleSubsteps(5.0f, 30, 10);
        wheelColliderFR.wheelDampingRate = wheelDampingRate;

        GameObject goFL = new GameObject("LeftFront");
        goFL.transform.SetParent(wheelColliderHolder.transform);
        wheelColliderFL = goFL.AddComponent<WheelCollider>();
        wheelColliderFL.mass = 30f;
        wheelColliderFL.suspensionDistance = 0.3f;
        wheelColliderFL.forceAppPointDistance = 0.2f;
        wheelColliderFL.suspensionSpring = new JointSpring { spring = 35000f, damper = 8000, targetPosition = 0.5f };
        wheelColliderFL.forwardFriction = new WheelFrictionCurve { extremumSlip = 0.4f, extremumValue = 1f, asymptoteSlip = 0.8f, asymptoteValue = 0.5f, stiffness = 1.2f };
        wheelColliderFL.sidewaysFriction = new WheelFrictionCurve { extremumSlip = 0.2f, extremumValue = 1f, asymptoteSlip = 1.5f, asymptoteValue = 1f, stiffness = 2.2f };
        origPosWheelFL = wheelFL.localPosition;
        goFL.transform.localPosition = wheelFL.localPosition;
        wheelColliderFL.center = new Vector3(0f, goFL.transform.localPosition.y / 2, 0f);
        wheelColliderFL.radius = wheelFL.GetComponent<Renderer>().bounds.extents.z;
        wheelColliderFL.ConfigureVehicleSubsteps(5.0f, 30, 10);
        wheelColliderFL.wheelDampingRate = wheelDampingRate;

        GameObject goRL = new GameObject("LeftRear");
        goRL.transform.SetParent(wheelColliderHolder.transform);
        wheelColliderRL = goRL.AddComponent<WheelCollider>();
        wheelColliderRL.mass = 30f;
        wheelColliderRL.suspensionDistance = 0.3f;
        wheelColliderRL.forceAppPointDistance = 0.2f;
        wheelColliderRL.suspensionSpring = new JointSpring { spring = 35000f, damper = 8000, targetPosition = 0.5f };
        wheelColliderRL.forwardFriction = new WheelFrictionCurve { extremumSlip = 0.4f, extremumValue = 1f, asymptoteSlip = 0.8f, asymptoteValue = 0.5f, stiffness = 1.2f };
        wheelColliderRL.sidewaysFriction = new WheelFrictionCurve { extremumSlip = 0.2f, extremumValue = 1f, asymptoteSlip = 1.5f, asymptoteValue = 1f, stiffness = 2.2f };
        origPosWheelRL = wheelRL.localPosition;
        goRL.transform.localPosition = wheelRL.localPosition;
        wheelColliderRL.center = new Vector3(0f, goRL.transform.localPosition.y / 2, 0f);
        wheelColliderRL.radius = wheelRL.GetComponent<Renderer>().bounds.extents.z;
        wheelColliderRL.ConfigureVehicleSubsteps(5.0f, 30, 10);
        wheelColliderRL.wheelDampingRate = wheelDampingRate;

        GameObject goRR = new GameObject("RightRear");
        goRR.transform.SetParent(wheelColliderHolder.transform);
        wheelColliderRR = goRR.AddComponent<WheelCollider>();
        wheelColliderRR.mass = 30f;
        wheelColliderRR.suspensionDistance = 0.3f;
        wheelColliderRR.forceAppPointDistance = 0.2f;
        wheelColliderRR.suspensionSpring = new JointSpring { spring = 35000f, damper = 8000, targetPosition = 0.5f };
        wheelColliderRR.forwardFriction = new WheelFrictionCurve { extremumSlip = 0.4f, extremumValue = 1f, asymptoteSlip = 0.8f, asymptoteValue = 0.5f, stiffness = 1.2f };
        wheelColliderRR.sidewaysFriction = new WheelFrictionCurve { extremumSlip = 0.2f, extremumValue = 1f, asymptoteSlip = 1.5f, asymptoteValue = 1f, stiffness = 2.2f };
        origPosWheelRR = wheelRR.localPosition;
        goRR.transform.localPosition = wheelRR.localPosition;
        wheelColliderRR.center = new Vector3(0f, goRR.transform.localPosition.y / 2, 0f);
        wheelColliderRR.radius = wheelRR.GetComponent<Renderer>().bounds.extents.z;
        wheelColliderRR.ConfigureVehicleSubsteps(5.0f, 30, 10);
        wheelColliderRR.wheelDampingRate = wheelDampingRate;

        // front transforms
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
        
        isPhysicsSimple = Manager.isSimplePhysics;
        simpleBoxCollider.enabled = isPhysicsSimple;
        complexBoxCollider.enabled = !isPhysicsSimple;
        wheelColliderHolder.SetActive(!isPhysicsSimple);
        if (isPhysicsSimple)
            normalSpeed = RandomGenerator.NextFloat(normalSpeedRange.x, normalSpeedRange.y);
        else
            normalSpeed = RandomGenerator.NextFloat(complexPhysicsSpeedRange.x, complexPhysicsSpeedRange.y);
    }
    #endregion

    #region spawn
    private void EvaluateDistanceFromFocus()
    {
        if (Manager.isSpawnAreaLimited && SimulatorManager.Instance.AgentManager.GetDistanceToActiveAgent(transform.position) > Manager.despawnDistance)
        {
            Despawn();
        }
    }

    private void Despawn()
    {
        if (Control == ControlType.Automatic)
        {
            ResetData();
            Manager.DespawnNPC(gameObject);
        }
    }

    public void StopNPCCoroutines()
    {
        foreach (Coroutine coroutine in Coroutines)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
    }

    private void ResetData()
    {
        StopNPCCoroutines();
        currentMapLane = null;
        currentIntersection = null;
        foreach (var intersection in SimulatorManager.Instance.MapManager.intersections)
            intersection.ExitStopSignQueue(this);
        prevMapLane = null;
        ResetLights();
        currentSpeed = 0f;
        currentStopTime = 0f;
        path = 0f;
        currentVelocity = Vector3.zero;
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
            rb.MovePosition(rb.position + transform.forward * currentSpeed * Time.fixedDeltaTime);
        else
            ApplyTorque();
    }

    private void NPCTurn()
    {
        if (isPhysicsSimple)
        {
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, currentTurn * Time.fixedDeltaTime, 0f));
        }
        else
        {
            float dt = Time.fixedDeltaTime;
            float steer = wheelColliderFL.steerAngle;

            float deltaAngle;
            steer_pid.UpdateErrors(Time.fixedDeltaTime, steer, targetTurn);
            deltaAngle = -steer_pid.Run();

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
        var prev = Manager.isSimplePhysics;
        if (prev != isPhysicsSimple)
            isPhysicsSimple = prev;
        else
            return;

        simpleBoxCollider.enabled = isPhysicsSimple;
        complexBoxCollider.enabled = !isPhysicsSimple;
        wheelColliderHolder.SetActive(!isPhysicsSimple);
        if (isPhysicsSimple && Control != ControlType.FollowLane && Control != ControlType.Waypoints)
            normalSpeed = RandomGenerator.NextFloat(normalSpeedRange.x, normalSpeedRange.y);
        else
            normalSpeed = RandomGenerator.NextFloat(complexPhysicsSpeedRange.x, complexPhysicsSpeedRange.y);
    }

    private void ApplyTorque()
    {
        // Maintain speed at target speed
        float FRICTION_COEFFICIENT = 0.7f; // for dry wheel/pavement -- wet is about 0.4
        speed_pid.UpdateErrors(Time.fixedDeltaTime, currentSpeed_measured, targetSpeed);
        float deltaVel = -speed_pid.Run();
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

    public void SetPhysicsMode(bool isPhysicsSimple)
    {
        Manager.isSimplePhysics = isPhysicsSimple;
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
        currentTurn += turnAdjustRate * Time.fixedDeltaTime * (targetTurn - currentTurn);

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
            if (isCurve)
                targetSpeed = Mathf.Lerp(targetSpeed, normalSpeed * 0.25f, Time.fixedDeltaTime * 20f);

            if (IsYieldToIntersectionLane())
            {
                if (currentMapLane != null)
                    if (currentIndex < 2)
                        targetSpeed = normalSpeed * 0.1f;
                    else
                        elapsedAccelerateTime = speedAdjustRate = targetSpeed = currentSpeed = 0f;
            }
        }
        
        if (isFrontDetectWithinStopDistance || isRightDetectWithinStopDistance || isLeftDetectWithinStopDistance)
            targetSpeed = SetFrontDetectSpeed();

        if (isForcedStop)
        {
            targetSpeed = 0f;
        }

        if (targetSpeed > currentSpeed && elapsedAccelerateTime <= 5f)
        {
            speedAdjustRate = Mathf.Lerp(minSpeedAdjustRate, maxSpeedAdjustRate, elapsedAccelerateTime / 5f);
            elapsedAccelerateTime += Time.fixedDeltaTime;
        }
        else
        {
            speedAdjustRate = maxSpeedAdjustRate;
            elapsedAccelerateTime = 0f;
        }

        currentSpeed += speedAdjustRate * Time.fixedDeltaTime * (targetSpeed - currentSpeed);
        currentSpeed = currentSpeed < 0.01f ? 0f : currentSpeed;
        currentVelocity = isPhysicsSimple ? (rb.position - lastRBPosition) / Time.fixedDeltaTime : rb.velocity;
        currentSpeed_measured = isPhysicsSimple ? (((rb.position - lastRBPosition) / Time.fixedDeltaTime).magnitude) * 2.23693629f : rb.velocity.magnitude * 2.23693629f; // MPH
        if (isPhysicsSimple && Time.fixedDeltaTime > 0)
        {
            simpleVelocity = (rb.position - lastRBPosition) / Time.fixedDeltaTime;

            Vector3 euler1 = lastRBRotation.eulerAngles;
            Vector3 euler2 = rb.rotation.eulerAngles;
            Vector3 diff = euler2 - euler1;
            for (int i = 0; i < 3; i++)
            {
                diff[i] = (diff[i] + 180) % 360 - 180;
            }
            simpleAngularVelocity = diff / Time.fixedDeltaTime * Mathf.Deg2Rad;
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
        yield return Manager.WaitUntilFixed(() => distanceToStopTarget <= stopLineDistance);
        isStopSign = true;
        currentStopTime = 0f;
        hasReachedStopSign = false;
        yield return Manager.WaitUntilFixed(() => distanceToStopTarget < minTargetDistance);
        prevMapLane.stopLine.intersection.EnterStopSignQueue(this);
        hasReachedStopSign = true;
        yield return Manager.WaitForFixedSeconds(stopSignWaitTime);
        yield return Manager.WaitUntilFixed(() => prevMapLane.stopLine.intersection.CheckStopSignQueue(this));
        hasReachedStopSign = false;
        isStopSign = false;
    }

    IEnumerator WaitTrafficLight()
    {
        currentStopTime = 0f;
        yield return Manager.WaitUntilFixed(() => distanceToStopTarget <= stopLineDistance);
        if (prevMapLane.stopLine.currentState == MapData.SignalLightStateType.Green) yield break; // light is green so just go
        isStopLight = true;
        yield return Manager.WaitUntilFixed(() => atStopTarget); // wait if until reaching stop line
        yield return Manager.WaitUntilFixed(() => prevMapLane.stopLine.currentState == MapData.SignalLightStateType.Green); // wait until green light
        if (isLeftTurn || isRightTurn)
            yield return Manager.WaitForFixedSeconds(RandomGenerator.NextFloat(1f, 2f)); // wait to creep out on turn
        isStopLight = false;
    }

    public void RemoveFromStopSignQueue()
    {
        prevMapLane?.stopLine?.intersection?.ExitStopSignQueue(this);
    }

    private void StopTimeDespawnCheck()
    {
        if (!Manager.isDespawnTimer) return;

        if (isStopLight || isStopSign || (currentSpeed_measured < 0.03))
            currentStopTime += Time.fixedDeltaTime;
        if (currentStopTime > 30f)
            Despawn();
    }

    private bool IsYieldToIntersectionLane() // TODO stopping car
    {
        var state = false;

        if (currentMapLane != null)
        {
            if (currentMapLane.isStopSignIntersetionLane) // if stop sign intersection check yield lanes for npc in front
            {
                for (int i = 0; i < Manager.currentPooledNPCs.Count; i++)
                {
                    if (Manager.currentPooledNPCs[i].gameObject.activeInHierarchy)
                    {
                        for (int k = 0; k < currentMapLane.yieldToLanes.Count; k++)
                        {
                            if (Manager.currentPooledNPCs[i].currentMapLane != null)
                            {
                                if (Manager.currentPooledNPCs[i].currentMapLane == currentMapLane.yieldToLanes[k])
                                {
                                    if (Vector3.Dot(Manager.currentPooledNPCs[i].transform.position - transform.position, transform.forward) > 0.5f)
                                    {
                                        state = true;
                                    }
                                }
                            }
                        }
                    }
                }

            }
            else // if signal light intersection just yield until light is yellow or red
            {
                if (currentMapLane.yieldToLanes.Count > 0)
                    state = true;
            }
        }

        if (prevMapLane != null && prevMapLane.stopLine != null) // light is yellow/red so oncoming traffic should be stopped already if past stopline
            if (prevMapLane.stopLine.currentState == MapData.SignalLightStateType.Yellow || prevMapLane.stopLine.currentState == MapData.SignalLightStateType.Red)
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
            ApiManager.Instance?.AddWaypointReached(gameObject, currentIndex);

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
                currentIntersection = wpQ.previousLane.stopLine?.intersection;
                prevMapLane = wpQ.previousLane;
                if (prevMapLane.stopLine.intersection != null) // null if map not setup right TODO add check to report missing stopline
                {
                    if (prevMapLane.stopLine.isStopSign) // stop sign
                    {
                        Coroutines[(int)CoroutineID.WaitStopSign] = Manager.StartCoroutine(WaitStopSign());
                        wpQ.StopTarget.isStopAhead = false;
                    }
                    else
                    {
                        Coroutines[(int)CoroutineID.WaitTrafficLight] = Manager.StartCoroutine(WaitTrafficLight());
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
                ApiManager.Instance?.AddStopLine(gameObject);
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
        if (currentMapLane?.nextConnectedLanes.Count >= 1) // choose next path and set waypoints
        {
            currentMapLane = currentMapLane.nextConnectedLanes[RandomGenerator.Next(0, currentMapLane.nextConnectedLanes.Count)];
            SetLaneData(currentMapLane.mapWorldPositions);
            SetTurnSignal();
            Coroutines[(int)CoroutineID.DelayChangeLane] = Manager.StartCoroutine(DelayChangeLane());
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
        if (!currentMapLane.isTrafficLane) yield break;
        if (RandomGenerator.Next(0, 3) == 1) yield break;
        if (!(laneChange)) yield break;

        if (currentMapLane.leftLaneForward != null)
        {
            isLeftTurn = true;
            isRightTurn = false;
            SetNPCTurnSignal();
        }
        else if (currentMapLane.rightLaneForward != null)
        {
            isRightTurn = true;
            isLeftTurn = false;
            SetNPCTurnSignal();
        }

        yield return Manager.WaitForFixedSeconds(RandomGenerator.NextFloat(0f, 2f));

        if (currentIndex >= laneData.Count - 2)
        {
            isLeftTurn = isRightTurn = false;
            yield break;
        }

        SetLaneChange();
    }

    private void SetLaneChange()
    {
        ApiManager.Instance?.AddLaneChange(gameObject);

        if (currentMapLane.leftLaneForward != null)
        {
            if (!isFrontLeftDetect)
            {
                currentMapLane = currentMapLane.leftLaneForward;
                SetChangeLaneData(currentMapLane.mapWorldPositions);
                Coroutines[(int)CoroutineID.DelayOffTurnSignals] = Manager.StartCoroutine(DelayOffTurnSignals());
            }
        }
        else if (currentMapLane.rightLaneForward != null)
        {
            if (!isFrontRightDetect)
            {
                currentMapLane = currentMapLane.rightLaneForward;
                SetChangeLaneData(currentMapLane.mapWorldPositions);
                Coroutines[(int)CoroutineID.DelayOffTurnSignals] = Manager.StartCoroutine(DelayOffTurnSignals());
            }
        }
    }

    public void ForceLaneChange(bool isLeft)
    {
        if (isLeft)
        {
            if (currentMapLane.leftLaneForward != null)
            {
                if (!isFrontLeftDetect)
                {
                    currentMapLane = currentMapLane.leftLaneForward;
                    SetChangeLaneData(currentMapLane.mapWorldPositions);
                    Coroutines[(int)CoroutineID.DelayOffTurnSignals] = Manager.StartCoroutine(DelayOffTurnSignals());
                    ApiManager.Instance?.AddLaneChange(gameObject);
                }
            }
        }
        else
        {
            if (currentMapLane.rightLaneForward != null)
            {
                if (!isFrontRightDetect)
                {
                    currentMapLane = currentMapLane.rightLaneForward;
                    SetChangeLaneData(currentMapLane.mapWorldPositions);
                    Coroutines[(int)CoroutineID.DelayOffTurnSignals] = Manager.StartCoroutine(DelayOffTurnSignals());
                    ApiManager.Instance?.AddLaneChange(gameObject);
                }
            }
        }
    }

    private void GetDodge()
    {
        if (currentMapLane == null) return;
        if (isDodge) return;
        if (IsYieldToIntersectionLane()) return;

        if (isLeftDetectWithinStopDistance || isRightDetectWithinStopDistance)
        {
            var npcC = isLeftDetectWithinStopDistance ? leftClosestHitInfo.collider.GetComponent<NPCController>() : rightClosestHitInfo.collider.GetComponent<NPCController>();
            var aC = isLeftDetectWithinStopDistance ? leftClosestHitInfo.collider.transform.root.GetComponent<AgentController>() : rightClosestHitInfo.collider.transform.root.GetComponent<AgentController>();

            if (currentMapLane.isTrafficLane)
            {
                if (npcC != null)
                {
                    isFrontDetectWithinStopDistance = true;
                    frontClosestHitInfo = isLeftDetectWithinStopDistance ? leftClosestHitInfo : rightClosestHitInfo;
                }
                else if (aC != null)
                {
                    isFrontDetectWithinStopDistance = true;
                    frontClosestHitInfo = isLeftDetectWithinStopDistance ? leftClosestHitInfo : rightClosestHitInfo;
                    if (!isWaitingToDodge)
                        Coroutines[(int)CoroutineID.WaitToDodge] = Manager.StartCoroutine(WaitToDodge(aC, isLeftDetectWithinStopDistance));
                }
                else
                {
                    if (leftClosestHitInfo.collider.gameObject.GetComponent<NPCController>() == null && leftClosestHitInfo.collider.transform.root.GetComponent<AgentController>() == null)
                        SetDodge(!isLeftDetectWithinStopDistance);
                }
            }
            else // intersection lane
            {
                if (npcC != null)
                {
                    if ((isLeftTurn && npcC.isLeftTurn || isRightTurn && npcC.isRightTurn) && Vector3.Dot(transform.TransformDirection(Vector3.forward), npcC.transform.TransformDirection(Vector3.forward)) < -0.7f)
                        if (currentIndex > 1)
                            SetDodge(isLeftTurn, true);
                }
            }
        }
    }

    IEnumerator WaitToDodge(AgentController aC, bool isLeft)
    {
        isWaitingToDodge = true;
        float elapsedTime = 0f;
        while (elapsedTime < 5f)
        {
            if (aC.GetComponent<Rigidbody>().velocity.magnitude > 0.01f)
            {
                isWaitingToDodge = false;
                yield break;
            }
            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
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
        if ((isLeft && currentMapLane.leftLaneForward == null && !isShortDodge) || (!isLeft && currentMapLane.rightLaneForward == null && !isShortDodge)) return;

        isDodge = true;

        if (isShortDodge)
        {
            dodgeTarget = Quaternion.Euler(0f, shortDodgeAngle, 0f) * (startTransform.forward * 5f);
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
        yield return Manager.WaitForFixedSeconds(3f);
        isLeftTurn = isRightTurn = false;
        SetNPCTurnSignal();
    }

    private void SetTurnSignal(bool forceLeftTS = false, bool forceRightTS = false)
    {
        isLeftTurn = false;
        isRightTurn = false;
        if (currentMapLane != null)
        {
            switch (currentMapLane.laneTurnType)
            {
                case MapData.LaneTurnType.NO_TURN:
                    isLeftTurn = false;
                    isRightTurn = false;
                    break;
                case MapData.LaneTurnType.LEFT_TURN:
                    isLeftTurn = true;
                    break;
                case MapData.LaneTurnType.RIGHT_TURN:
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
        if (currentMapLane == null) return;
        Vector3 heading = (currentMapLane.mapWorldPositions[currentMapLane.mapWorldPositions.Count - 1] - currentMapLane.mapWorldPositions[0]).normalized;
        Vector3 perp = Vector3.Cross(transform.forward, heading);
        tempPath = Vector3.Dot(perp, transform.up);
        if (tempPath < -0.2f)
            isLeftTurn = true;
        else if (tempPath > 0.2f)
            isRightTurn = true;
    }

    private void GetIsTurn()
    {
        if (currentMapLane == null) return;
        path = transform.InverseTransformPoint(currentTarget).x;
        isCurve = path < -1f || path > 1f ? true : false;
    }
    #endregion

    #region lights
    private void GetSimulatorTimeOfDay()
    {
        switch (SimulatorManager.Instance.EnvironmentEffectsManager.currentTimeOfDayState)
        {
            case TimeOfDayStateTypes.Day:
                currentNPCLightState = NPCLightStateTypes.Off;
                break;
            case TimeOfDayStateTypes.Night:
                currentNPCLightState = NPCLightStateTypes.Low;
                break;
            case TimeOfDayStateTypes.Sunrise:
                currentNPCLightState = NPCLightStateTypes.Off;
                break;
            case TimeOfDayStateTypes.Sunset:
                currentNPCLightState = NPCLightStateTypes.Low;
                break;
        }
        SetLights((int)currentNPCLightState);
    }

    private void OnTimeOfDayChange(TimeOfDayStateTypes state)
    {
        switch (state)
        {
            case TimeOfDayStateTypes.Day:
                currentNPCLightState = NPCLightStateTypes.Off;
                break;
            case TimeOfDayStateTypes.Night:
                currentNPCLightState = NPCLightStateTypes.Low;
                break;
            case TimeOfDayStateTypes.Sunrise:
                currentNPCLightState = NPCLightStateTypes.Off;
                break;
            case TimeOfDayStateTypes.Sunset:
                currentNPCLightState = NPCLightStateTypes.Low;
                break;
        }
        SetLights((int)currentNPCLightState);
    }

    public void SetLights(int state)
    {
        currentNPCLightState = (NPCLightStateTypes)state;
        SetHeadLights();
        SetRunningLights();
    }

    private void SetHeadLights()
    {
        switch (currentNPCLightState)
        {
            case NPCLightStateTypes.Off:
                foreach (var light in headLights)
                    light.enabled = false;
                if (bodyRenderer != null)
                {
                    var mats = bodyRenderer.materials;
                    mats[headLightMatIndex].SetVector("_EmissiveColor", Color.black);
                    bodyRenderer.materials = mats;
                }
                break;
            case NPCLightStateTypes.Low:
                foreach (var light in headLights)
                {
                    light.enabled = true;
                    light.intensity = 25f;
                    light.range = 200.0f;
                }
                if (bodyRenderer != null)
                {
                    var mats = bodyRenderer.materials;
                    mats[headLightMatIndex].SetVector("_EmissiveColor", Color.white * lowBeamEmission);
                    bodyRenderer.materials = mats;
                }
                break;
            case NPCLightStateTypes.High:
                foreach (var light in headLights)
                {
                    light.enabled = true;
                    light.intensity = 75f;
                    light.range = 400.0f;
                }
                if (bodyRenderer != null)
                {
                    var mats = bodyRenderer.materials;
                    mats[headLightMatIndex].SetVector("_EmissiveColor", Color.white * highBeamEmission);
                    bodyRenderer.materials = mats;
                }
                break;
        }
    }
    
    private void SetRunningLights()
    {
        switch (currentNPCLightState)
        {
            case NPCLightStateTypes.Off:
                foreach (var light in brakeLights)
                    light.enabled = false;
                if (bodyRenderer != null)
                {
                    var mats = bodyRenderer.materials;
                    mats[brakeLightMatIndex].SetVector("_EmissiveColor", Color.black);
                    bodyRenderer.materials = mats;
                }
                break;
            case NPCLightStateTypes.Low:
            case NPCLightStateTypes.High:
                foreach (var light in brakeLights)
                {
                    light.enabled = true;
                    light.intensity = 1f;
                    light.range = 25.0f;
                }
                if (bodyRenderer != null)
                {
                    var mats = bodyRenderer.materials;
                    mats[brakeLightMatIndex].SetVector("_EmissiveColor", new Color(0.5f, 0f, 0f) * 10);
                    bodyRenderer.materials = mats;
                    mats = null;
                }
                break;
        }
    }

    private void SetBrakeLights(bool state)
    {
        switch (currentNPCLightState)
        {
            case NPCLightStateTypes.Off:
                if (state)
                {
                    foreach (var light in brakeLights)
                    {
                        light.enabled = true;
                        light.intensity = 5f;
                        light.range = 50.0f;
                    }
                    if (bodyRenderer != null)
                    {
                        var mats = bodyRenderer.materials;
                        mats[brakeLightMatIndex].SetVector("_EmissiveColor", Color.red * 50);
                        bodyRenderer.materials = mats;
                    }
                }
                else
                {
                    foreach (var light in brakeLights)
                        light.enabled = false;
                    if (bodyRenderer != null)
                    {
                        var mats = bodyRenderer.materials;
                        mats[brakeLightMatIndex].SetVector("_EmissiveColor", Color.black);
                        bodyRenderer.materials = mats;
                    }
                }
                break;
            case NPCLightStateTypes.Low:
            case NPCLightStateTypes.High:
                if (state)
                {
                    foreach (var light in brakeLights)
                    {
                        light.enabled = true;
                        light.intensity = 5f;
                        light.range = 50.0f;
                    }
                    if (bodyRenderer != null)
                    {
                        var mats = bodyRenderer.materials;
                        mats[brakeLightMatIndex].SetVector("_EmissiveColor", Color.red * 50);
                        bodyRenderer.materials = mats;
                    }
                }
                else
                {
                    foreach (var light in brakeLights)
                    {
                        light.enabled = true;
                        light.intensity = 1f;
                        light.range = 25.0f;
                    }
                    if (bodyRenderer != null)
                    {
                        var mats = bodyRenderer.materials;
                        mats[brakeLightMatIndex].SetVector("_EmissiveColor", new Color(0.5f, 0f, 0f) * 10);
                        bodyRenderer.materials = mats;
                    }
                }
                break;
        }
    }
    
    private void ToggleBrakeLights()
    {
        if (targetSpeed < 2f || isStopLight || isFrontDetectWithinStopDistance || (isStopSign && distanceToStopTarget < stopLineDistance))
            SetBrakeLights(true);
        else
            SetBrakeLights(false);
    }

    public void SetNPCTurnSignal(bool isForced = false, bool isLeft = false, bool isRight = false)
    {
        if (isForced)
        {
            isLeftTurn = isLeft;
            isRightTurn = isRight;
        }

        if (turnSignalIE != null)
            StopCoroutine(turnSignalIE);
        turnSignalIE = StartTurnSignal();
        Coroutines[(int)CoroutineID.StartTurnSignal] = Manager.StartCoroutine(turnSignalIE);
    }

    public void SetNPCHazards(bool state = false)
    {
        if (hazardSignalIE != null)
            StopCoroutine(hazardSignalIE);
        
        isLeftTurn = state;
        isRightTurn = state;
        
        if (state)
        {
            hazardSignalIE = StartHazardSignal();
            Coroutines[(int)CoroutineID.StartHazardSignal] = Manager.StartCoroutine(hazardSignalIE);
        }
    }

    private IEnumerator StartTurnSignal()
    {
        while (isLeftTurn || isRightTurn)
        {
            SetTurnIndicator(true);
            yield return Manager.WaitForFixedSeconds(0.5f);
            SetTurnIndicator(false);
            yield return Manager.WaitForFixedSeconds(0.5f);
        }
        SetTurnIndicator(isReset: true);
    }

    private IEnumerator StartHazardSignal()
    {
        while (isLeftTurn && isRightTurn)
        {
            SetTurnIndicator(true, isHazard: true);
            yield return Manager.WaitForFixedSeconds(0.5f);
            SetTurnIndicator(false, isHazard: true);
            yield return Manager.WaitForFixedSeconds(0.5f);
        }
        SetTurnIndicator(isReset: true);
    }

    private void SetTurnIndicator(bool state = false, bool isReset = false, bool isHazard = false)
    {
        if (isHazard)
        {
            var mats = bodyRenderer.materials;
            mats[indicatorLeftMatIndex].SetVector("_EmissiveColor", state ? Color.yellow * 10 : Color.black);
            mats[indicatorRightMatIndex].SetVector("_EmissiveColor", state ? Color.yellow * 10 : Color.black);
            bodyRenderer.materials = mats;
            foreach (var light in indicatorLeftLights)
                light.enabled = state;
            foreach (var light in indicatorRightLights)
                light.enabled = state;
        }
        else
        {
            var mats = bodyRenderer.materials;
            mats[isLeftTurn ? indicatorLeftMatIndex : indicatorRightMatIndex].SetVector("_EmissiveColor", state ? Color.yellow * 10 : Color.black);
            bodyRenderer.materials = mats;
            foreach (var light in isLeftTurn ? indicatorLeftLights : indicatorRightLights)
                light.enabled = state;
        }

        if (isReset)
        {
            var mats = bodyRenderer.materials;
            mats[indicatorLeftMatIndex].SetVector("_EmissiveColor", Color.black);
            mats[indicatorRightMatIndex].SetVector("_EmissiveColor", Color.black);
            bodyRenderer.materials = mats;
            foreach (var light in indicatorLeftLights)
                light.enabled = false;
            foreach (var light in indicatorRightLights)
                light.enabled = false;
        }
    }

    private void ToggleIndicatorReverse()
    {
        isReverse = !isReverse;
        SetIndicatorReverse(isReverse);
    }

    private void SetIndicatorReverse(bool state)
    {
        var mats = bodyRenderer.materials;
        mats[indicatorReverseMatIndex].SetVector("_EmissiveColor", state ? Color.white * 10 : Color.black);
        bodyRenderer.materials = mats;
        indicatorReverseLight.enabled = state;
    }

    private void ResetLights()
    {
        currentNPCLightState = NPCLightStateTypes.Off;
        SetHeadLights();
        SetRunningLights();
        SetBrakeLights(false);
        if (turnSignalIE != null)
            StopCoroutine(turnSignalIE);
        if (hazardSignalIE != null)
            StopCoroutine(hazardSignalIE);
        SetTurnIndicator(isReset: true);
        SetIndicatorReverse(false);
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

            float theta = (currentSpeed * Time.fixedDeltaTime / wheelColliderFR.radius) * Mathf.Rad2Deg;

            Quaternion finalQ = Quaternion.LookRotation(steerVector);
            Vector3 finalE = finalQ.eulerAngles;
            finalQ = Quaternion.Euler(0f, finalE.y, 0f);

            wheelFR.rotation = Quaternion.RotateTowards(wheelFR.rotation, finalQ, Time.fixedDeltaTime * 50f);
            wheelFL.rotation = Quaternion.RotateTowards(wheelFL.rotation, finalQ, Time.fixedDeltaTime * 50f);
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
        Physics.Raycast(frontRight.position, frontRight.forward, out rightClosestHitInfo, frontRaycastDistance / 2, carCheckBlockBitmask);
        Physics.Raycast(frontLeft.position, frontLeft.forward, out leftClosestHitInfo, frontRaycastDistance / 2, carCheckBlockBitmask);
        isFrontLeftDetect = Physics.CheckSphere(frontLeft.position - (frontLeft.right * 2), 1f, carCheckBlockBitmask);
        isFrontRightDetect = Physics.CheckSphere(frontRight.position + (frontRight.right * 2), 1f, carCheckBlockBitmask);

        isFrontDetectWithinStopDistance = (frontClosestHitInfo.collider) && frontClosestHitInfo.distance < stopHitDistance;
        isRightDetectWithinStopDistance = (rightClosestHitInfo.collider) && rightClosestHitInfo.distance < stopHitDistance / 2;
        isLeftDetectWithinStopDistance = (leftClosestHitInfo.collider) && leftClosestHitInfo.distance < stopHitDistance / 2;

        // ground collision
        groundCheckInfo = new RaycastHit();
        if (!Physics.Raycast(transform.position + Vector3.up, Vector3.down, out groundCheckInfo, 5f, groundHitBitmask))
            Despawn();

        //if (frontClosestHitInfo.collider != null)
        //    Debug.DrawLine(frontCenter.position, frontClosestHitInfo.point, Color.blue, 0.25f);
        //if (leftClosestHitInfo.collider != null)
        //    Debug.DrawLine(frontLeft.position, leftClosestHitInfo.point, Color.yellow, 0.25f);
        //if (rightClosestHitInfo.collider != null)
        //    Debug.DrawLine(frontRight.position, rightClosestHitInfo.point, Color.red, 0.25f);
    }

    private float SetFrontDetectSpeed()
    {
        var blocking = frontClosestHitInfo.transform;
        blocking = blocking ?? rightClosestHitInfo.transform;
        blocking = blocking ?? leftClosestHitInfo.transform;

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

        var lane = SimulatorManager.Instance.MapManager.GetClosestLane(position);
        InitLaneData(lane);

        int index = -1;
        float minDist = float.PositiveInfinity;
        Vector3 closest = Vector3.zero;

        // choose closest waypoint
        for (int i = 0; i < lane.mapWorldPositions.Count - 1; i++)
        {
            var p0 = lane.mapWorldPositions[i];
            var p1 = lane.mapWorldPositions[i + 1];

            var p = Utility.ClosetPointOnSegment(p0, p1, position);

            float d = Vector3.SqrMagnitude(position - p);
            if (d < minDist)
            {
                minDist = d;
                index = i;
                closest = p;
            }
        }

        if (closest != lane.mapWorldPositions[index])
        {
            index++;
        }

        currentTarget = lane.mapWorldPositions[index];
        currentIndex = index;

        stopTarget = lane.mapWorldPositions[lane.mapWorldPositions.Count - 1];
        currentIntersection = lane.stopLine?.intersection;

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
        if (currentMapLane?.stopLine != null) // check if stopline is connected to current path
        {
            currentIntersection = currentMapLane.stopLine?.intersection;
            stopTarget = currentMapLane.mapWorldPositions[currentMapLane.mapWorldPositions.Count - 1];
            prevMapLane = currentMapLane;
            if (prevMapLane.stopLine.intersection != null) // null if map not setup right TODO add check to report missing stopline
            {
                if (prevMapLane.stopLine.isStopSign) // stop sign
                {
                    Coroutines[(int)CoroutineID.WaitStopSign] = Manager.StartCoroutine(WaitStopSign());
                }
                else
                {
                    Coroutines[(int)CoroutineID.WaitTrafficLight] = Manager.StartCoroutine(WaitTrafficLight());
                }
            }
        }
    }
    
    public void SetFollowWaypoints(List<DriveWaypoint> waypoints, bool loop)
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
        if (collision.gameObject.layer == LayerMask.NameToLayer("Agent"))
        {
            isForcedStop = true;
            SetNPCHazards(true);

            ApiManager.Instance?.AddCollision(gameObject, collision);
            SIM.LogSimulation(SIM.Simulation.NPCCollision);
        }
    }
}
