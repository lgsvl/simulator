/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Simulator;
using UnityEngine;
using Simulator.Api;
using Simulator.Map;
using Simulator.Network.Core.Components;
using Simulator.Network.Core;
using Simulator.Network.Core.Connection;
using Simulator.Network.Core.Identification;
using Simulator.Network.Core.Messaging;
using Simulator.Network.Core.Messaging.Data;
using Simulator.Utilities;

public class NPCController : MonoBehaviour, IMessageSender, IMessageReceiver, IGloballyUniquelyIdentified
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
    [HideInInspector]
    public MeshCollider MainCollider;
    private Vector3 lastRBPosition;
    private Vector3 simpleVelocity;
    private Quaternion lastRBRotation;
    private Vector3 simpleAngularVelocity;
    private Rigidbody rb;
    public Bounds Bounds;
    private RaycastHit frontClosestHitInfo = new RaycastHit();
    private RaycastHit leftClosestHitInfo = new RaycastHit();
    private RaycastHit rightClosestHitInfo = new RaycastHit();
    private RaycastHit groundCheckInfo = new RaycastHit();
    private float frontRaycastDistance = 20f;
    private float stopHitDistance = 5f;
    private float stopLineDistance = 15f;
    private bool atStopTarget;
    private int aggression;
    private float aggressionAdjustRate;

    private Vector3 centerOfMass;
    private GameObject wheelColliderHolder;
    private WheelCollider wheelColliderFR;
    private WheelCollider wheelColliderFL;
    private WheelCollider wheelColliderRL;
    private WheelCollider wheelColliderRR;

    private float wheelDampingRate = 1f;

    // map data
    public string id { get; set; }
    public MapLane currentMapLane;
    public MapLane prevMapLane;
    public MapIntersection currentIntersection = null;
    public List<float> laneSpeed; // used for waypoint mode
    public List<Vector3> laneData;
    public List<Vector3> laneAngle;
    public List<float> laneIdle;
    public List<float> laneTime;
    public List<bool> laneDeactivate;
    public List<float> laneTriggerDistance;
    public bool waypointLoop;

    // targeting
    private Transform frontCenter;
    private Transform frontLeft;
    private Transform frontRight;
    private Vector3 currentTarget;
    private Vector3 switchPos;
    private Vector3 currentTargetDirection;
    private Quaternion targetRot;
    private Quaternion switchRot;
    private float angle;
    private int currentIndex = 0;
    private int lastIndex = -1;
    private float distanceToCurrentTarget = 0f;
    public float distanceToStopTarget = 0;
    private Vector3 stopTarget = Vector3.zero;
    private float minTargetDistance = 1f;

    //private bool doRaycast; // TODO skip update for collision
    //private float nextRaycast = 0f;
    private float laneSpeedLimit = 0f;
    private float normalSpeed = 0f;
    public float targetSpeed = 0f;
    public float currentSpeed = 0f;
    public float currentIdle = 0f;
    public bool currentDeactivate = false;
    public float currentTriggerDistance = 0f;
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
    private System.Collections.Generic.List<UnityEngine.Renderer> allRenderers;
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
    
    //Network
    private MessagesManager messagesManager;
    private string key;
    public string GUID => id;
    public string Key => key ?? (string.IsNullOrEmpty(GUID) ? null : key = $"{GUID}/NPCController");


    private double wakeUpTime;
    [System.NonSerialized]
    static public Dictionary<uint, List<string>> logWaypoint = new Dictionary<uint, List<string>>();
    private bool activateNPC = false;

    // State kept for showing first running over Simulator
    static private bool isGlobalFirstRun = true;
    // State kept for showing log saved
    static private bool doneLog = false;
    // State kept for showing first running for one NPC
    private bool isFirstRun = true;
    // State kept for showing waypoints updated from API
    private bool updatedWaypoints = false;
    // State kept for checking for reaching to waypoint
    private bool checkReachBlocked = false;

    // Waypoint Driving
    private enum WaypointDriveState
    {
        Wait,
        Drive,
        Despawn
    };
    WaypointDriveState waypointDriveState = WaypointDriveState.Wait;

    public enum NPCWaypointState
    {
        None,
        Driving,
        Idle,
        AwaitingTrigger
    };
    private NPCWaypointState thisNPCWaypointState = NPCWaypointState.Driving;

    private enum NPCLightStateTypes
    {
        Off,
        Low,
        High
    };
    private NPCLightStateTypes currentNPCLightState = NPCLightStateTypes.Off;

    private Color runningLightEmissionColor = new Color(0.65f, 0.65f, 0.65f);
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

    private System.Random RandomGenerator;
    private MonoBehaviour FixedUpdateManager;
    private NPCManager NPCManager;
    
    private Coroutine[] Coroutines = new Coroutine[System.Enum.GetNames(typeof(CoroutineID)).Length];
    private int agentLayer;
    public uint GTID { get; set; }
    public string NPCLabel { get; set; }

    public NPCSizeType Size { get; set; } = NPCSizeType.MidSize;
    public Color NPCColor { get; set; } = Color.black;

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
    #endregion

    #region mono
    private void Start()
    {
        messagesManager = Loader.Instance.Network.MessagesManager;
        StartCoroutine(WaitForId(() => messagesManager?.RegisterObject(this)));
    }
    
    private void OnEnable()
    {
        SimulatorManager.Instance.EnvironmentEffectsManager.TimeOfDayChanged += OnTimeOfDayChange;
        GetSimulatorTimeOfDay();
        agentLayer = LayerMask.NameToLayer("Agent");
    }

    private void OnDisable()
    {
        if (Control != ControlType.Waypoints)
        {
            ResetData();    
        } 
        SimulatorManager.Instance.EnvironmentEffectsManager.TimeOfDayChanged -= OnTimeOfDayChange;
    }

    public void PhysicsUpdate()
    {
        if (!gameObject.activeInHierarchy)
            return;

        switch (Control)
        {
            case ControlType.Automatic:
                if (isGlobalFirstRun)
                    FirstRun();

                if (isLaneDataSet)
                {
                    ToggleBrakeLights();
                    CollisionCheck();
                    EvaluateTarget();
                    GetIsTurn();
                    GetDodge();
                    SetTargetSpeed();
                    SetTargetTurn();
                    NPCTurn();
                    NPCMove();
                    WheelMovement();
                    StopTimeDespawnCheck();
                    EvaluateDistanceFromFocus();
                }
                break;
            case ControlType.FollowLane:
                if (isLaneDataSet)
                {
                    ToggleBrakeLights();
                    CollisionCheck();
                    EvaluateTarget();
                    GetIsTurn();
                    SetTargetSpeed();
                    SetTargetTurn();
                    NPCTurn();
                    NPCMove();
                    WheelMovement();
                }
                WheelMovement();
                break;
            case ControlType.Waypoints:
                if (!rb.isKinematic)
                    rb.isKinematic = true;
                if (!MainCollider.isTrigger)
                    MainCollider.isTrigger = true;
                ToggleBrakeLights();
                NPCProcessIdleTime();
                if (isGlobalFirstRun)
                {
                    FirstRun();
                }
                if (isFirstRun && currentIndex == 0 && updatedWaypoints)
                {
                    // The NPC add current pose and moves to initial waypoint.
                    AddPoseToFirstWaypoint();
                }
                if (activateNPC)
                {
                    NPCNextMove();
                }
                break;
            case ControlType.FixedSpeed:
                break;
            case ControlType.Manual:
                break;
            default:
                break;
        }
    }

    private void AddPoseToFirstWaypoint()
    {
        laneData.Insert(0, transform.position);
        laneAngle.Insert(0, transform.eulerAngles);
        laneSpeed.Insert(0, 0f);
        laneTime.Insert(0, 0);
        laneIdle.Insert(0, 0);
        laneDeactivate.Insert(0, false);

        lastIndex = laneData.Count-1;

        float initialMoveDuration = (laneData[1] - laneData[0]).magnitude / laneSpeed[1];

        for (int i=1; i<laneTime.Count; i++)
        {
            laneTime[i] += initialMoveDuration;
        }

        FirstRun();

        updatedWaypoints = false;
        isFirstRun = false;
        checkReachBlocked = false;
    }

    private void FirstRun()
    {
        isGlobalFirstRun = false;
    }

    // After elapsedTime, DebugMsg will save recorded data into file.
    // This function should be called in PhysicsUpdate()
    private void DebugMsg(float elapsedTime)
    {
        if (SimulatorManager.Instance.CurrentTime - SimulatorManager.Instance.NPCManager.startTime > elapsedTime)
        {
            if (!doneLog)
            {
                WriteMsg();
                doneLog = true;
            }
        }
    }

    // This function should be placed at the place where you want to record variables.
    private void DebugMsg()
    {
        if (currentIndex >= laneData.Count - 1)
            return;

        var t = SimulatorManager.Instance.CurrentTime;
        string logMsg = "";
        if (laneTime.Count > 0)
            logMsg = $"NPC{GTID}, Idx: {currentIndex}, pose: {laneData[currentIndex]}, angle: {laneAngle[currentIndex]}, " +
            $"laneTime: {laneTime[currentIndex]}, time: {t}, rel_t: {t - wakeUpTime}";

        if (!logWaypoint.ContainsKey(GTID))
            logWaypoint.Add(GTID, new List<string>());
        else
            logWaypoint[GTID].Add(logMsg);
    }

    private void WriteMsg()
    {
        string filename = null;
        // Todo: Fix file path based on project directory.
        switch (Control)
        {
            case ControlType.Automatic:
                filename = "/data/Simulator/wp_automatic.txt";
                break;
            case ControlType.Waypoints:
                filename = "/data/Simulator/wp_waypoints.txt";
                break;
        }

        using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(filename))
        {
            foreach (var numNPC in logWaypoint.Keys)
            {
                file.WriteLine($"NPC {numNPC}");
                foreach (var oneMsg in logWaypoint[numNPC])
                    file.WriteLine(oneMsg);
            }
        }

        Debug.Log($"Finished Write log to file.");
    }
    private void DebugFrame(int i=1)
    {
        int frame;
        if (SimulatorManager.Instance.IsAPI)
            frame = ((ApiManager)FixedUpdateManager).CurrentFrame;
        else
            frame = ((SimulatorManager)FixedUpdateManager).CurrentFrame;

        if (frame % i == 0)
        {
            print(frame + ": " + gameObject.name.Substring(0, gameObject.name.IndexOf("(")) + " " + transform.position.ToString("F7") + " " + currentSpeed + " " + currentTurn.ToString("F7"));
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == agentLayer)
        {
            ApiManager.Instance?.AddCollision(rb.gameObject, other.attachedRigidbody.gameObject);
            SIM.LogSimulation(SIM.Simulation.NPCCollision);
        } 
    }

    private void OnDestroy()
    {
        Resources.UnloadUnusedAssets();
        messagesManager?.UnregisterObject(this);
    }
    #endregion

    #region init
    public void Init(int seed)
    {
        FixedUpdateManager = SimulatorManager.Instance.FixedUpdateManager;
        NPCManager = SimulatorManager.Instance.NPCManager;
        RandomGenerator = new System.Random(seed);
        aggression = 3 - (seed % 3);
        stopHitDistance = 12 / aggression;
        speedAdjustRate = 2 + 2 * aggression;
        maxSpeedAdjustRate = speedAdjustRate; // more aggressive NPCs will accelerate faster
        turnAdjustRate = 50 * aggression;
        SetNeededComponents();
        ResetData();
    }

    public void InitLaneData(MapLane lane)
    {
        ResetData();
        laneSpeedLimit = lane.speedLimit;
        aggressionAdjustRate = laneSpeedLimit / 11.176f; // give more space at faster speeds
        stopHitDistance = 12 / aggression * aggressionAdjustRate;
        normalSpeed = RandomGenerator.NextFloat(laneSpeedLimit - 3 + aggression, laneSpeedLimit + 1 + aggression);
        currentMapLane = lane;
        SetLaneData(currentMapLane.mapWorldPositions);
        lastRBPosition = transform.position;
        lastRBRotation = transform.rotation;
        isLaneDataSet = true;
    }

    private void SetNeededComponents()
    {
        groundHitBitmask = LayerMask.GetMask("Default");
        carCheckBlockBitmask = LayerMask.GetMask("Agent", "NPC", "Pedestrian");

        rb = GetComponent<Rigidbody>();
        allRenderers = GetComponentsInChildren<Renderer>().ToList();
        allLights = GetComponentsInChildren<Light>();

        Color.RGBToHSV(NPCColor, out float h, out float s, out float v);
        h = Mathf.Clamp01(RandomGenerator.NextFloat(h - 0.01f, h + 0.01f));
        v = Mathf.Clamp01(RandomGenerator.NextFloat(v - 0.1f, v + 0.1f));
        NPCColor = Color.HSVToRGB(h, s, v);

        foreach (Renderer child in allRenderers)
        {
            if (child.name.Contains("RightFront"))
            {
                wheelFR = child.transform;
                DistributeTransform(wheelFR);
            }

            if (child.name.Contains("LeftFront"))
            {
                wheelFL = child.transform;
                DistributeTransform(wheelFL);
            }
            
            if (child.name.Contains("LeftRear"))
            {
                wheelRL = child.transform;
                DistributeTransform(wheelRL);
            }
            
            if (child.name.Contains("RightRear"))
            {
                wheelRR = child.transform;
                DistributeTransform(wheelRR);
            }
            
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
                    if (allBodyMats[i].name.Contains("Body"))
                        allBodyMats[i].SetColor("_BaseColor", NPCColor);
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

        // mesh collider
        foreach (Renderer renderer in allRenderers)
        {
            if (renderer.name.Contains("Body"))
            {
                MainCollider = renderer.gameObject.AddComponent<MeshCollider>();
                MainCollider.convex = true;
                renderer.gameObject.layer = LayerMask.NameToLayer("NPC");
                MainCollider.enabled = true;
            }
        }

        Bounds = new Bounds(transform.position, Vector3.zero);
        foreach (Renderer renderer in allRenderers)
        {
            Bounds.Encapsulate(renderer.bounds);
        }

        rb.centerOfMass = new Vector3(Bounds.center.x, Bounds.center.y, Bounds.center.z + Bounds.max.z * 0.5f);

        // wheel collider holder
        wheelColliderHolder = new GameObject("WheelColliderHolder");
        wheelColliderHolder.transform.SetParent(transform.GetChild(0));
        wheelColliderHolder.SetActive(true);

        // wheel colliders
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

        // GroundTruth Box Collider
        var gtBox = new GameObject("GroundTruthBox");
        var gtBoxCollider = gtBox.AddComponent<BoxCollider>();
        gtBoxCollider.isTrigger = true;
        gtBoxCollider.size = Bounds.size;
        gtBoxCollider.center = new Vector3(gtBoxCollider.center.x, Bounds.size.y / 2, gtBoxCollider.center.z);
        gtBox.transform.parent = transform;
        gtBox.layer = LayerMask.NameToLayer("GroundTruth");

        // front transforms
        GameObject go = new GameObject("Front");
        go.transform.position = new Vector3(Bounds.center.x, Bounds.min.y + 0.5f, Bounds.center.z + Bounds.max.z);
        go.transform.SetParent(transform, true);
        frontCenter = go.transform;
        go = new GameObject("Right");
        go.transform.position = new Vector3(Bounds.center.x + Bounds.max.x, Bounds.min.y + 0.5f, Bounds.center.z + Bounds.max.z);
        go.transform.SetParent(transform, true);
        frontRight = go.transform;
        go = new GameObject("Left");
        go.transform.position = new Vector3(Bounds.center.x - Bounds.max.x, Bounds.min.y + 0.5f, Bounds.center.z + Bounds.max.z);
        go.transform.SetParent(transform, true);
        frontLeft = go.transform;

        normalSpeed = RandomGenerator.NextFloat(laneSpeedLimit - 3 + aggression, laneSpeedLimit + 1 + aggression);
    }

    // api
    public void SetLastPosRot(Vector3 pos, Quaternion rot)
    {
        lastRBPosition = pos;
        lastRBRotation = rot;
    }
    #endregion

    #region spawn
    private void EvaluateDistanceFromFocus()
    {
        if (!SimulatorManager.Instance.NPCManager.WithinSpawnArea(transform.position) && !SimulatorManager.Instance.NPCManager.IsVisible(gameObject))
        {
            Despawn();
        }
    }

    private void Despawn()
    {
        if (Control == ControlType.Automatic)
        {
            ResetData();
            NPCManager.DespawnNPC(gameObject);
        }
    }

    public void StopNPCCoroutines()
    {
        foreach (Coroutine coroutine in Coroutines)
        {
            if (coroutine != null)
            {
                FixedUpdateManager.StopCoroutine(coroutine);
            }
        }
    }

    private void ResetData()
    {
        StopNPCCoroutines();
        currentMapLane = null;
        laneSpeedLimit = 0f;
        currentIntersection = null;
        foreach (var intersection in SimulatorManager.Instance.MapManager.intersections)
        {
            intersection.ExitStopSignQueue(this);
            intersection.ExitIntersectionList(this);
        }   
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
        laneChange = true;
        isStopLight = false;
        isStopSign = false;
        hasReachedStopSign = false;
        isLaneDataSet = false;
        isForcedStop = false;
        lastRBPosition = transform.position;
        lastRBRotation = transform.rotation;
    }
    #endregion

    #region physics
    private void NPCMove()
    {
        if (Control == ControlType.Waypoints)
        {
            if (thisNPCWaypointState == NPCWaypointState.Driving)
            {
                rb.MovePosition(rb.position + currentTargetDirection * currentSpeed * Time.fixedDeltaTime);
            }
        }
        else
        {
            var movement = rb.position + transform.forward * currentSpeed * Time.fixedDeltaTime;
            rb.MovePosition(new Vector3(movement.x, rb.position.y, movement.z));
        }
    }

    private void NPCNextMove()
    {
        Vector3 position;
        Quaternion rotation;
        float time = (float)(SimulatorManager.Instance.CurrentTime - wakeUpTime);

        if (waypointDriveState == WaypointDriveState.Despawn)
        {
            Despawn();
            return;
        }
        else if (waypointDriveState == WaypointDriveState.Wait)
        {
            return;
        }

        if (waypointDriveState != WaypointDriveState.Drive)
        {
            return;
        }

        if (currentIndex < laneData.Count-1)
        {
            // Wait for current time synced with waypoint time
            if (time < laneTime[currentIndex])
            {
                return;
            }
            // Check proximity of current pose to currentIndex+1 index waypoint.
            var distance2 = Vector3.SqrMagnitude(transform.position - laneData[currentIndex+1]);
            if (distance2 < 0.1f && !checkReachBlocked)
            {
                ApiManager.Instance?.AddWaypointReached(gameObject, currentIndex);  // currentIndex is right because of +1 waypoint, intial pose.
                if (currentIndex+1 == laneData.Count-1)
                {
                    checkReachBlocked = true;
                    if (laneIdle[currentIndex+1] == -1 && currentDeactivate)
                    {
                        waypointDriveState = WaypointDriveState.Despawn;
                    }
                    else if (laneIdle[currentIndex+1] == 0)
                        waypointDriveState = WaypointDriveState.Wait;
                }
                else if (time > laneTime[currentIndex+1])
                    currentIndex++;

                // Avoid consecutive AddWaypointReached() before time exceeds laneData[currentIndex+1]
                checkReachBlocked = true;
            }
        }
        else if (currentIndex == laneData.Count-1)
        {
            if (laneIdle[currentIndex] == -1 && currentDeactivate)
            {
                waypointDriveState = WaypointDriveState.Despawn;
            }
            else if (laneIdle[currentIndex] == 0)
                waypointDriveState = WaypointDriveState.Wait;
            return;
        }

        (position, rotation) = NPCPoseInterpolate(time, laneData, laneAngle, laneTime, currentIndex);

        if (!float.IsNaN(position.x))
        {
            rb.MovePosition(position);
            rb.MoveRotation(rotation);
        }

        if (currentIndex < laneData.Count-1)
        {
            if (time > laneTime[currentIndex+1])
            {
                currentIndex++;
                checkReachBlocked = false;
            }
        }
    }

    static float relativeTime(double wakeUpTime)
    {
        return (float)(SimulatorManager.Instance.CurrentTime - wakeUpTime);
    }

    private (Vector3, Quaternion) NPCPoseInterpolate(double time, List<Vector3>poses, List<Vector3>angles, List<float>times, int index)
    {
        // Catmull interpolation needs constrained waypoints input. Zigzag waypoints input makes error for catmull interpolation.
        // Instead, NPCController uses linear interpolation.
        // var pose = CatmullRomInterpolate(time);
        var k = (float)(time - (times[index])) / (times[index+1] - times[index]);
        var pose = Vector3.Slerp(poses[index], poses[index+1], k);
        var rot = Quaternion.Slerp(Quaternion.Euler(angles[index]), Quaternion.Euler(angles[index+1]), k);

        return (pose, rot);
    }

    // As for rotation, Catmull-Rom interpolation doesn't work.
    private Vector3 CatmullRomInterpolate(float time)
    {
        Vector3[] points = new Vector3[4];
        Vector3[] rotations = new Vector3[4];
        float[] times = new float[4];

        Vector3 interpolatedPose = new Vector3();

        var maxIndex = laneTime.Count - 1;

        if (laneData.Count == 2 && laneTime.Count == 2)
        {
            points[1] = laneData[0];
            points[2] = laneData[1];
            points[0] = points[1] + (points[1] - points[2]);
            points[3] = points[2] + (points[2] - points[1]);

            times[1] = laneTime[0];
            times[2] = laneTime[1];
            times[0] = times[1] + (times[1] - times[2]);
            times[3] = times[2] + (times[2] - times[1]);
        }
        else if (laneData.Count == 3 && laneTime.Count == 3)
        {
            if (time >= times[0] && time <= times[1])
            {
                points[1] = laneData[0];
                points[2] = laneData[1];
                points[3] = laneData[2];
                points[0] = points[1] + (points[1] - points[2]);

                times[1] = laneTime[0];
                times[2] = laneTime[1];
                times[3] = laneTime[2];
                times[0] = times[1] + (times[1] - times[2]);
            }

            else if (time >= times[1] && time <= times[2])
            {
                points[0] = laneData[0];
                points[1] = laneData[1];
                points[2] = laneData[2];
                points[3] = points[2] + (points[2] - points[1]);

                times[0] = laneTime[0];
                times[1] = laneTime[1];
                times[2] = laneTime[2];
                times[3] = times[2] + (times[2] - times[1]);
            }
        }
        else if (time <= laneTime[1] && time >= laneTime[0])  // currentIndex == 0, lower bound case
        {
            points[0] = laneData[0] - (laneData[1] - laneData[0]);
            points[1] = laneData[currentIndex];
            points[2] = laneData[currentIndex+1];
            points[3] = laneData[currentIndex+2];

            times[0] = laneTime[0] - (laneTime[1] - laneTime[0]);
            times[1] = laneTime[currentIndex];
            times[2] = laneTime[currentIndex+1];
            times[3] = laneTime[currentIndex+2];
        }
        else if (time <= laneTime[maxIndex-1] && time > laneTime[1])  // 1 <= currentIndex <= maxIndex-1, most of cases
        {
            points[0] = laneData[currentIndex-1];
            points[1] = laneData[currentIndex];
            points[2] = laneData[currentIndex+1];
            points[3] = laneData[currentIndex+2];

            times[0] = laneTime[currentIndex-1];
            times[1] = laneTime[currentIndex];
            times[2] = laneTime[currentIndex+1];
            times[3] = laneTime[currentIndex+2];
        }
        else if (laneTime[maxIndex-1] < time)   // maxIndex-1 <= currentIndex, upper bound case
        {
            points[0] = laneData[currentIndex-1];
            points[1] = laneData[currentIndex];
            points[2] = laneData[currentIndex] + (laneData[currentIndex] - laneData[currentIndex-1]);
            points[3] = points[2] + (laneData[currentIndex-1] - laneData[currentIndex-2]);

            times[0] = laneTime[currentIndex-1];
            times[1] = laneTime[currentIndex];
            times[2] = laneTime[currentIndex] + (laneTime[currentIndex] - laneTime[currentIndex-1]);;
            times[3] = times[2] + (laneTime[currentIndex-1] - laneTime[currentIndex-2]);
        }
        else
        {
            Debug.Log($"Couldn't interpolate.");
        }

        interpolatedPose = CatmullRom(points, times, time);

        return interpolatedPose;
    }

    private void NPCProcessIdleTime()
    {
        if (waypointDriveState == WaypointDriveState.Wait)
        {
            currentIdle = laneIdle[0];
            currentDeactivate = laneDeactivate[currentIndex];
            FixedUpdateManager.StartCoroutine(IdleNPC(currentIdle, currentDeactivate));
        }
        else if (waypointDriveState == WaypointDriveState.Drive)
        {
            currentIdle = laneIdle[currentIndex];
            currentDeactivate = laneDeactivate[currentIndex];
        }

        if (currentIdle == -1f)
            gameObject.SetActive(false);
    }

    private void NPCTurn()
    {
        if (Control == ControlType.Waypoints)
        {
            if (thisNPCWaypointState == NPCWaypointState.Driving)
            {
                    float k = (rb.position - switchPos).magnitude / (currentTarget - switchPos).magnitude;
                    rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, k));
            }
        }
        else
        {
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, currentTurn * Time.fixedDeltaTime, 0f));
        }
    }

    public Vector3 GetVelocity()
    {
        return simpleVelocity;
    }

    public Vector3 GetAngularVelocity()
    {
        return simpleAngularVelocity;
    }
    #endregion

    #region inputs
    private void SetTargetTurn()
    {
        steerVector = (currentTarget - frontCenter.position).normalized;

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
        if (!gameObject.activeInHierarchy)
            return;

        targetSpeed = normalSpeed;

        if (isStopSign)
        {
            if (!hasReachedStopSign)
            {
                targetSpeed = Mathf.Clamp(GetLerpedDistanceToStopTarget() * (normalSpeed), 0f, normalSpeed); // TODO need to fix when target speed > normal speed issue
            }
            else
            {
                targetSpeed = 0f;
            }
        }

        if (isStopLight)
        {
            targetSpeed = Mathf.Clamp(GetLerpedDistanceToStopTarget() * (normalSpeed), 0f, normalSpeed); // TODO need to fix when target speed > normal speed issue
            if (distanceToStopTarget < minTargetDistance)
            {
                targetSpeed = 0f;
            }
        }

        if (!isStopLight && !isStopSign)
        {
            if (isCurve || isRightTurn || isLeftTurn)
            {
                targetSpeed = normalSpeed * 0.5f;
            }

            if (IsYieldToIntersectionLane())
            {
                if (currentMapLane != null)
                {
                    if (currentIndex < 2)
                    {
                        targetSpeed = normalSpeed * 0.1f;
                    }
                    else
                    {
                        elapsedAccelerateTime = speedAdjustRate = targetSpeed = currentSpeed = 0f;
                    }
                }
            }
        }

        if ((isFrontDetectWithinStopDistance || isRightDetectWithinStopDistance || isLeftDetectWithinStopDistance) && !hasReachedStopSign)
        {
            targetSpeed = SetFrontDetectSpeed();
        }

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
        currentVelocity = (rb.position - lastRBPosition) / Time.fixedDeltaTime;
        currentSpeed_measured = (((rb.position - lastRBPosition) / Time.fixedDeltaTime).magnitude) * 2.23693629f; // MPH

        if (Time.fixedDeltaTime > 0)
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
        yield return FixedUpdateManager.WaitUntilFixed(() => distanceToStopTarget <= stopLineDistance);
        isStopSign = true;
        currentStopTime = 0f;
        hasReachedStopSign = false;
        yield return FixedUpdateManager.WaitUntilFixed(() => distanceToStopTarget < minTargetDistance);
        prevMapLane.stopLine.intersection.EnterStopSignQueue(this);
        hasReachedStopSign = true;
        yield return FixedUpdateManager.WaitForFixedSeconds(stopSignWaitTime);
        yield return FixedUpdateManager.WaitUntilFixed(() => prevMapLane.stopLine.intersection.CheckStopSignQueue(this));
        hasReachedStopSign = false;
        isStopSign = false;
    }

    IEnumerator WaitTrafficLight()
    {
        currentStopTime = 0f;
        yield return FixedUpdateManager.WaitUntilFixed(() => distanceToStopTarget <= stopLineDistance);
        if (prevMapLane.stopLine.currentState == MapData.SignalLightStateType.Green) 
            yield break; // light is green so just go
        isStopLight = true;
        yield return FixedUpdateManager.WaitUntilFixed(() => atStopTarget); // wait if until reaching stop line
        if ((isRightTurn && prevMapLane.rightLaneReverse == null) || (isLeftTurn && prevMapLane.leftLaneReverse == null)) // Right on red or left on red
        {
            var waitTime = RandomGenerator.NextFloat(0f, 3f);
            var startTime = currentStopTime;
            yield return FixedUpdateManager.WaitUntilFixed(() => prevMapLane.stopLine.currentState == MapData.SignalLightStateType.Green || currentStopTime - startTime >= waitTime);
            isStopLight = false;
            yield break;
        }
        yield return FixedUpdateManager.WaitUntilFixed(() => prevMapLane.stopLine.currentState == MapData.SignalLightStateType.Green); // wait until green light
        if (isLeftTurn || isRightTurn)
            yield return FixedUpdateManager.WaitForFixedSeconds(RandomGenerator.NextFloat(1f, 2f)); // wait to creep out on turn
        isStopLight = false;
    }

    public void RemoveFromStopSignQueue()
    {
        prevMapLane?.stopLine?.intersection?.ExitStopSignQueue(this);
    }

    private void StopTimeDespawnCheck()
    {
        if (isStopLight || isStopSign || (currentSpeed_measured < 0.03))
        {
            currentStopTime += Time.fixedDeltaTime;
        }
        if (currentStopTime > 60f)
        {
            Despawn();
        }
    }

    private bool IsYieldToIntersectionLane() // TODO stopping car
    {
        var state = false;

        if (currentMapLane != null)
        {
            var threshold = Vector3.Distance(currentMapLane.mapWorldPositions[0], currentMapLane.mapWorldPositions[currentMapLane.mapWorldPositions.Count - 1]) / 6;
            if (Vector3.Distance(transform.position, currentMapLane.mapWorldPositions[0]) < threshold) // If not far enough into lane, NPC will just go
            {
                for (int i = 0; i < NPCManager.CurrentPooledNPCs.Count; i++)
                {
                    if (!NPCManager.CurrentPooledNPCs[i].gameObject.activeInHierarchy)
                    {
                        continue; // Ignore NPCs that have been despawned
                    }
                    for (int k = 0; k < currentMapLane.yieldToLanes.Count; k++)
                    {
                        if (NPCManager.CurrentPooledNPCs[i].currentMapLane == null)
                        {
                            continue;
                        }
                        if (NPCManager.CurrentPooledNPCs[i].currentMapLane == currentMapLane.yieldToLanes[k]) // checks each active NPC if it is in a yieldTo lane
                        {
                            if (Vector3.Dot(NPCManager.CurrentPooledNPCs[i].transform.position - transform.position, transform.forward) > 0.5f) // Only yields if the other NPC is in front
                            {
                                state = true;
                            }
                        }
                        else
                        {
                            for (int j = 0; j < currentMapLane.yieldToLanes[k].prevConnectedLanes.Count; j++) // checks each active NPC if it is approaching a yieldTo lane
                            {
                                if (NPCManager.CurrentPooledNPCs[i].currentMapLane == currentMapLane.yieldToLanes[k].prevConnectedLanes[j])
                                {
                                    var a = NPCManager.CurrentPooledNPCs[i].transform.position;
                                    var b = currentMapLane.yieldToLanes[k].prevConnectedLanes[j].mapWorldPositions[currentMapLane.yieldToLanes[k].prevConnectedLanes[j].mapWorldPositions.Count - 1];

                                    if (Vector3.Distance(a, b) < 40 / aggression) // if other NPC is close enough to intersection, NPC will not make turn
                                    {
                                        state = true;
                                        if (NPCManager.CurrentPooledNPCs[i].currentSpeed < 1f) // if other NPC is yielding to others or stopped for other reasons
                                        {
                                            state = false;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
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

        currentTarget = laneData[++currentIndex];
    }

    private void SetChangeLaneData(List<Vector3> data)
    {
        laneData = new List<Vector3>(data);
        currentIndex = SimulatorManager.Instance.MapManager.GetLaneNextIndex(transform.position, currentMapLane);
        currentTarget = laneData[currentIndex];
        isDodge = false; // ???
    }

    private void EvaluateWaypointTarget()
    {
        var distance2 = Vector3.SqrMagnitude(transform.position - currentTarget);

        if (distance2 < 0.5f * 0.5f)
        {
            switchPos = rb.position;
            switchRot = rb.rotation;
            if (currentIndex != lastIndex)
            {
                ApiManager.Instance?.AddWaypointReached(gameObject, currentIndex);
                lastIndex = currentIndex;
            }

            if (currentTriggerDistance > 0)
            {
                FixedUpdateManager.StartCoroutine(WaitForTriggerNPC(currentTriggerDistance));
            }
            else if (thisNPCWaypointState == NPCWaypointState.Driving && currentIdle > 0f)
            {
                FixedUpdateManager.StartCoroutine(IdleNPC(currentIdle, currentDeactivate));
            }
            else if (thisNPCWaypointState == NPCWaypointState.Driving && currentIdle == -1f)
            {
                gameObject.SetActive(false);
            }
            else if (thisNPCWaypointState == NPCWaypointState.Driving && ++currentIndex < laneData.Count)
            {
                currentTarget = laneData[currentIndex];
                currentTargetDirection = (currentTarget - rb.position).normalized;
                normalSpeed = laneSpeed[currentIndex];
                targetRot = Quaternion.Euler(laneAngle[currentIndex]);
                currentIdle = laneIdle[currentIndex];
                currentDeactivate = laneDeactivate[currentIndex];
                currentTriggerDistance = laneTriggerDistance[currentIndex];
            }
            else if (thisNPCWaypointState == NPCWaypointState.Driving && waypointLoop)
            {
                currentIndex = 0;
                currentTarget = laneData[0];
                currentTargetDirection = (currentTarget - rb.position).normalized;
                normalSpeed = laneSpeed[0];
                targetRot = Quaternion.Euler(laneAngle[0]);
                currentIdle = laneIdle[0];
                currentDeactivate = laneDeactivate[currentIndex];
                currentTriggerDistance = laneTriggerDistance[0];
            }
            else if (thisNPCWaypointState != NPCWaypointState.Idle && thisNPCWaypointState != NPCWaypointState.AwaitingTrigger)
            {
                Control = ControlType.Manual;
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
                Coroutines[(int)CoroutineID.DelayChangeLane] = FixedUpdateManager.StartCoroutine(DelayChangeLane());
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
            currentMapLane = currentMapLane.nextConnectedLanes[RandomGenerator.Next(currentMapLane.nextConnectedLanes.Count)];
            laneSpeedLimit = currentMapLane.speedLimit;
            aggressionAdjustRate = laneSpeedLimit / 11.176f; // 11.176 m/s corresponds to 25 mph
            normalSpeed = RandomGenerator.NextFloat(laneSpeedLimit - 3 + aggression, laneSpeedLimit + 1 + aggression);
            SetLaneData(currentMapLane.mapWorldPositions);
            SetTurnSignal();
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
        if (currentMapLane == null) yield break;
        if (!currentMapLane.isTrafficLane) yield break;
        if (RandomGenerator.Next(100) < 98) yield break;
        if (!laneChange) yield break;

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

        yield return FixedUpdateManager.WaitForFixedSeconds(RandomGenerator.NextFloat(1f, 3f));

        if (currentIndex >= laneData.Count - 2)
        {
            isLeftTurn = isRightTurn = false;
            yield break;
        }

        SetLaneChange();
    }

    private void SetLaneChange()
    {
        if (currentMapLane == null) // Prevent null if despawned during wait
            return;

        ApiManager.Instance?.AddLaneChange(gameObject);

        if (currentMapLane.leftLaneForward != null)
        {
            if (!isFrontLeftDetect)
            {
                currentMapLane = currentMapLane.leftLaneForward;
                laneSpeedLimit = currentMapLane.speedLimit;
                aggressionAdjustRate = laneSpeedLimit / 11.176f; // 11.176 m/s corresponds to 25 mph
                SetChangeLaneData(currentMapLane.mapWorldPositions);
                Coroutines[(int)CoroutineID.DelayOffTurnSignals] = FixedUpdateManager.StartCoroutine(DelayOffTurnSignals());
            }
        }
        else if (currentMapLane.rightLaneForward != null)
        {
            if (!isFrontRightDetect)
            {
                currentMapLane = currentMapLane.rightLaneForward;
                laneSpeedLimit = currentMapLane.speedLimit;
                aggressionAdjustRate = laneSpeedLimit / 11.176f; // 11.176 m/s corresponds to 25 mph
                SetChangeLaneData(currentMapLane.mapWorldPositions);
                Coroutines[(int)CoroutineID.DelayOffTurnSignals] = FixedUpdateManager.StartCoroutine(DelayOffTurnSignals());
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
                    laneSpeedLimit = currentMapLane.speedLimit;
                    aggressionAdjustRate = laneSpeedLimit / 11.176f; // 11.176 m/s corresponds to 25 mph
                    SetChangeLaneData(currentMapLane.mapWorldPositions);
                    Coroutines[(int)CoroutineID.DelayOffTurnSignals] = FixedUpdateManager.StartCoroutine(DelayOffTurnSignals());
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
                    laneSpeedLimit = currentMapLane.speedLimit;
                    aggressionAdjustRate = laneSpeedLimit / 11.176f; // 11.176 m/s corresponds to 25 mph
                    SetChangeLaneData(currentMapLane.mapWorldPositions);
                    Coroutines[(int)CoroutineID.DelayOffTurnSignals] = FixedUpdateManager.StartCoroutine(DelayOffTurnSignals());
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
            var npcC = isLeftDetectWithinStopDistance ? leftClosestHitInfo.collider.GetComponentInParent<NPCController>() : rightClosestHitInfo.collider.GetComponentInParent<NPCController>();
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
                        Coroutines[(int)CoroutineID.WaitToDodge] = FixedUpdateManager.StartCoroutine(WaitToDodge(aC, isLeftDetectWithinStopDistance));
                }
                else
                {
                    if (leftClosestHitInfo.collider?.gameObject?.GetComponentInParent<NPCController>() == null && leftClosestHitInfo.collider?.transform.root.GetComponent<AgentController>() == null)
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
        yield return FixedUpdateManager.WaitForFixedSeconds(3f);
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
                currentNPCLightState = NPCLightStateTypes.Low;
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
                currentNPCLightState = NPCLightStateTypes.Low;
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
        
        if (Loader.Instance.Network.IsMaster)
        {
            var content = new BytesStack();
            content.PushInt(state);
            content.PushEnum<NPCControllerMethodName>((int)NPCControllerMethodName.SetLights);
            var message = new Message(key, content, MessageType.ReliableOrdered);
            BroadcastMessage(message);
        }
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
        
        if (Loader.Instance.Network.IsMaster)
        {
            var content = new BytesStack();
            content.PushBool(state);
            content.PushEnum<NPCControllerMethodName>((int)NPCControllerMethodName.SetBrakeLights);
            var message = new Message(key, content, MessageType.ReliableOrdered);
            BroadcastMessage(message);
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
            FixedUpdateManager.StopCoroutine(turnSignalIE);
        turnSignalIE = StartTurnSignal();
        Coroutines[(int)CoroutineID.StartTurnSignal] = FixedUpdateManager.StartCoroutine(turnSignalIE);
        
        if (Loader.Instance.Network.IsMaster)
        {
            //Force setting turn signals on clients
            var content = new BytesStack();
            content.PushBool(isRightTurn);
            content.PushBool(isLeftTurn);
            content.PushBool(true);
            content.PushEnum<NPCControllerMethodName>((int)NPCControllerMethodName.SetNPCTurnSignal);
            var message = new Message(key, content, MessageType.ReliableOrdered);
            BroadcastMessage(message);
        }
    }

    public void SetNPCHazards(bool state = false)
    {
        if (hazardSignalIE != null)
            FixedUpdateManager.StopCoroutine(hazardSignalIE);

        isLeftTurn = state;
        isRightTurn = state;

        if (state)
        {
            hazardSignalIE = StartHazardSignal();
            Coroutines[(int)CoroutineID.StartHazardSignal] = FixedUpdateManager.StartCoroutine(hazardSignalIE);
        }
        
        if (Loader.Instance.Network.IsMaster)
        {
            var content = new BytesStack();
            content.PushBool(state);
            content.PushEnum<NPCControllerMethodName>((int)NPCControllerMethodName.SetNPCHazards);
            var message = new Message(key, content, MessageType.ReliableOrdered);
            BroadcastMessage(message);
        }
    }

    private IEnumerator StartTurnSignal()
    {
        while (isLeftTurn || isRightTurn)
        {
            SetTurnIndicator(true);
            yield return FixedUpdateManager.WaitForFixedSeconds(0.5f);
            SetTurnIndicator(false);
            yield return FixedUpdateManager.WaitForFixedSeconds(0.5f);
        }
        SetTurnIndicator(isReset: true);
    }

    private IEnumerator StartHazardSignal()
    {
        while (isLeftTurn && isRightTurn)
        {
            SetTurnIndicator(true, isHazard: true);
            yield return FixedUpdateManager.WaitForFixedSeconds(0.5f);
            SetTurnIndicator(false, isHazard: true);
            yield return FixedUpdateManager.WaitForFixedSeconds(0.5f);
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
        
        if (Loader.Instance.Network.IsMaster)
        {
            var content = new BytesStack();
            content.PushBool(state);
            content.PushEnum<NPCControllerMethodName>((int)NPCControllerMethodName.SetIndicatorReverse);
            var message = new Message(key, content, MessageType.ReliableOrdered);
            BroadcastMessage(message);
        }
    }

    private void ResetLights()
    {
        currentNPCLightState = NPCLightStateTypes.Off;
        SetHeadLights();
        SetRunningLights();
        SetBrakeLights(false);
        if (turnSignalIE != null)
            FixedUpdateManager.StopCoroutine(turnSignalIE);
        if (hazardSignalIE != null)
            FixedUpdateManager.StopCoroutine(hazardSignalIE);
        SetTurnIndicator(isReset: true);
        SetIndicatorReverse(false);
        
        if (Loader.Instance.Network.IsMaster)
        {
            var content = new BytesStack();
            content.PushEnum<NPCControllerMethodName>((int)NPCControllerMethodName.ResetLights);
            var message = new Message(key, content, MessageType.ReliableOrdered);
            BroadcastMessage(message);
        }
    }
    #endregion

    #region utility
    private void WheelMovement()
    {
        if (!wheelFR || !wheelFL || !wheelRL || !wheelRR) return;
        if (steerVector == Vector3.zero || currentSpeed < 0.1f) return;

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

        Vector3 pos;
        Quaternion rot;
        wheelColliderFR.GetWorldPose(out pos, out rot);
        wheelFR.position = pos;
        wheelColliderFL.GetWorldPose(out pos, out rot);
        wheelFL.position = pos;
        wheelColliderRL.GetWorldPose(out pos, out rot);
        wheelRL.position = pos;
        wheelColliderRR.GetWorldPose(out pos, out rot);
        wheelRR.position = pos;
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

        if ((currentMapLane.isIntersectionLane || Vector3.Distance(transform.position, currentMapLane.mapWorldPositions[currentMapLane.mapWorldPositions.Count - 1]) < 10) && !isRightTurn && !isLeftTurn)
        {
            stopHitDistance = Mathf.Lerp(4f, 20 / aggression * aggressionAdjustRate, currentSpeed / laneSpeedLimit); // if going straight through an intersection or is approaching the end of the current lane, give more space
        }
        else stopHitDistance = Mathf.Lerp(4f, 12 / aggression * aggressionAdjustRate, currentSpeed / laneSpeedLimit); // higher aggression and/or lower speeds -> lower stophitdistance

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
            {
                tempS = (normalSpeed) * (frontClosestHitInfo.distance / stopHitDistance);
            }
        }
        else if (Vector3.Dot(transform.forward, blocking.transform.forward) < -0.2f && (isRightTurn || isLeftTurn))
        {
            tempS = normalSpeed;
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
                    Coroutines[(int)CoroutineID.WaitStopSign] = FixedUpdateManager.StartCoroutine(WaitStopSign());
                }
                else
                {
                    Coroutines[(int)CoroutineID.WaitTrafficLight] = FixedUpdateManager.StartCoroutine(WaitTrafficLight());
                }
            }
        }
    }

    public void SetFollowWaypoints(List<DriveWaypoint> waypoints, bool loop)
    {
        waypointLoop = loop;

        laneData = waypoints.Select(wp => wp.Position).ToList();
        laneSpeed = waypoints.Select(wp => wp.Speed).ToList();
        laneAngle = waypoints.Select(wp => wp.Angle).ToList();
        laneIdle = waypoints.Select(wp => wp.Idle).ToList();
        laneDeactivate = waypoints.Select(wp => wp.Deactivate).ToList();
        laneTriggerDistance = waypoints.Select(wp => wp.TriggerDistance).ToList();
        laneTime = waypoints.Select(wp => wp.TimeStamp).ToList();

        ResetData();

        currentIndex = 0;
        currentTarget = laneData[0];
        currentTargetDirection = (currentTarget - rb.position).normalized;
        normalSpeed = laneSpeed[0];
        targetRot = Quaternion.Euler(laneAngle[0]);
        currentIdle = laneIdle[0];
        currentDeactivate = laneDeactivate[0];
        currentTriggerDistance = laneTriggerDistance[0];
        isLaneDataSet = true;
        thisNPCWaypointState = NPCWaypointState.Driving;

        Control = ControlType.Waypoints;

        if (laneTime[0] < 0)
        {
        // Set waypoint time base on speed.
        Debug.LogWarning("Waypoint timestamps absent or invalid, caluclating timestamps based on speed.");
        laneTime = new List<float>();
        laneTime.Add(0);
        for (int i=0; i < laneData.Count-1; i++)
        {
            var dp = laneData[i+1] - laneData[i];
            var dt = dp.magnitude/laneSpeed[i];

            laneTime.Add(laneTime.Last()+dt);
        }
        }
        updatedWaypoints = true;
        isFirstRun = true;
    }

    public void SetManualControl()
    {
        Control = ControlType.Manual;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == agentLayer)
        {
            isForcedStop = true;
            SetNPCHazards(true);

            ApiManager.Instance?.AddCollision(gameObject, collision.gameObject, collision);
            SIM.LogSimulation(SIM.Simulation.NPCCollision);
        }
    }

    private IEnumerator IdleNPC(float duration, bool deactivate)
    {
        thisNPCWaypointState = NPCWaypointState.Idle;
        currentIdle = 0;
        Vector3 pos = rb.position;
        if (deactivate)
        {
            gameObject.SetActive(false);
        }
        yield return FixedUpdateManager.WaitForFixedSeconds(duration);
        if (deactivate)
        {
            gameObject.SetActive(true);
        }
        thisNPCWaypointState = NPCWaypointState.Driving;
        wakeUpTime = SimulatorManager.Instance.CurrentTime;
        activateNPC = true;
        waypointDriveState = WaypointDriveState.Drive;

        if (!logWaypoint.ContainsKey(GTID))
            logWaypoint.Add(GTID, new List<string>());
    }

    private IEnumerator WaitForTriggerNPC(float dist)
    {
        if (dist == 0f)
        {
            yield break;
        }

        thisNPCWaypointState = NPCWaypointState.AwaitingTrigger;
        currentTriggerDistance = 0;
        yield return FixedUpdateManager.StartCoroutine(EvaluateEgoToTrigger(laneData[lastIndex], dist));
        thisNPCWaypointState = NPCWaypointState.Driving;
    }

    private IEnumerator EvaluateEgoToTrigger(Vector3 pos, float dist)
    {
        // for ego in list of egos
        var players = SimulatorManager.Instance.AgentManager.ActiveAgents;
        while (true)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (Vector3.Distance(players[i].transform.position, pos) < dist)
                {
                    yield break;
                }
            }
            yield return new WaitForFixedUpdate();
        }
    }


    #region network
    /// <summary>
    /// Method waiting while the GUID in the guidSource is not set
    /// </summary>
    /// <param name="callback">Callback called after GUID is set</param>
    /// <returns>IEnumerator</returns>
    private IEnumerator WaitForId(Action callback)
    {
        while (string.IsNullOrEmpty(id))
            yield return null;
        callback();
    }
    
    /// <summary>
    /// Adds required components to make transform distributed from master to clients
    /// </summary>
    /// <param name="transformToDistribute">Transform that will be distributed</param>
    private void DistributeTransform(Transform transformToDistribute)
    {
        var network = Loader.Instance.Network;
        if (transformToDistribute.gameObject.GetComponent<DistributedTransform>() != null)
            return;
        if (network.IsMaster)
            transformToDistribute.gameObject.AddComponent<DistributedTransform>();
        else if (network.IsClient)
            transformToDistribute.gameObject.AddComponent<DistributedTransform>();
    }
    
    /// <inheritdoc/>
    public void ReceiveMessage(IPeerManager sender, Message message)
    {
        var methodName = message.Content.PopEnum<NPCControllerMethodName>();
        switch (methodName)
        {
            case NPCControllerMethodName.SetLights:
                SetLights(message.Content.PopInt());
                break;
            case NPCControllerMethodName.SetBrakeLights:
                SetBrakeLights(message.Content.PopBool());
                break;
            case NPCControllerMethodName.SetNPCTurnSignal:
                SetNPCTurnSignal(message.Content.PopBool(), message.Content.PopBool(), message.Content.PopBool());
                break;
            case NPCControllerMethodName.SetNPCHazards:
                SetNPCHazards(message.Content.PopBool());
                break;
            case NPCControllerMethodName.SetIndicatorReverse:
                SetIndicatorReverse(message.Content.PopBool());
                break;
            case NPCControllerMethodName.ResetLights:
                ResetLights();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <inheritdoc/>
    public void UnicastMessage(IPEndPoint endPoint, Message message)
    {
        if (!string.IsNullOrEmpty(key))
            messagesManager?.UnicastMessage(endPoint, message);
    }

    /// <inheritdoc/>
    public void BroadcastMessage(Message message)
    {
        if (!string.IsNullOrEmpty(key))
            messagesManager?.BroadcastMessage(message);
    }

    /// <inheritdoc/>
    void IMessageSender.UnicastInitialMessages(IPEndPoint endPoint)
    {
        //TODO support reconnection - send instantiation messages to the peer
    }

    private enum NPCControllerMethodName
    {
        SetLights = 0,
        SetBrakeLights = 1,
        SetNPCTurnSignal = 2,
        SetNPCHazards = 3,
        SetIndicatorReverse = 4,
        ResetLights = 5
    }
    #endregion
    public static Vector3 CatmullRom(Vector3[] points, float[] times, float t)
    {
        Debug.Assert(points.Length == times.Length, $"points.Length = {points.Length}, times.Length = {times.Length}");
        Debug.Assert(points.Length > 1 || times.Length > 1, $"points.Length = {points.Length}, times.Length = {times.Length}");

        Vector3 A1 = (times[1] - t) / (times[1] - times[0]) * points[0] + (t - times[0]) / (times[1] - times[0]) * points[1];
        Vector3 A2 = (times[2] - t) / (times[2] - times[1]) * points[1] + (t - times[1]) / (times[2] - times[1]) * points[2];
        Vector3 A3 = (times[3] - t) / (times[3] - times[2]) * points[2] + (t - times[2]) / (times[3] - times[2]) * points[3];

        Vector3 B1 = (times[2] - t) / (times[2] - times[0]) * A1 + (t - times[0]) / (times[2] - times[0]) * A2;
        Vector3 B2 = (times[3] - t) / (times[3] - times[1]) * A2 + (t - times[1]) / (times[3] - times[1]) * A3;

        return ((times[2] - t) / (times[2] - times[1]) * B1) + ((t - times[1]) / (times[2] - times[1]) * B2);
    }
}