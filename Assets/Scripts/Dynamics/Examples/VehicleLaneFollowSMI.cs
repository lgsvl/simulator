/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;
using Simulator.Map;
using Simulator.Utilities;

public class VehicleLaneFollowSMI : MonoBehaviour, IVehicleDynamics
{
    private bool DebugMode = false;

    private Rigidbody RB;
    private IVehicleActions VA;

    public Vector3 Velocity => RB.velocity;
    public Vector3 AngularVelocity => RB.angularVelocity;
    public Transform BaseLink { get { return BaseLinkTransform; } }
    public Transform BaseLinkTransform;
    public float AccellInput { get; set; } = 0f;
    public float SteerInput { get; set; } = 0f;
    public bool HandBrake { get; set; } = false;
    public float CurrentRPM { get; set; } = 0f;
    public float CurrentGear { get; set; } = 1f;
    public bool Reverse { get; set; } = false;
    public float WheelAngle
    {
        get
        {
            if (Wheels != null && Wheels.Count > 0 && Wheels[0] != null)
            {
                return (Wheels[0].collider.steerAngle + Wheels[1].collider.steerAngle) * 0.5f;
            }
            return 0.0f;
        }
    }
    public float Speed
    {
        get
        {
            return Velocity.magnitude;
        }
    }
    public float _MaxSteeringAngle = 39.4f;
    public float MaxSteeringAngle
    {
        get
        {
            return _MaxSteeringAngle;
        }
        set
        {
            _MaxSteeringAngle = value;
        }
    }
    public IgnitionStatus CurrentIgnitionStatus { get; set; } = IgnitionStatus.On;

    #region controller vars
    protected MeshCollider MainCollider { get; private set; }
    protected Vector3 lastRBPosition { get; private set; }
    protected Quaternion lastRBRotation { get; private set; }
    protected Bounds Bounds { get; private set; }

    protected Vector3 simpleVelocity { get; private set; }
    protected Vector3 simpleAngularVelocity { get; private set; }
    protected Vector3 simpleAcceleration { get; private set; }
    private Transform wheelColliderHolder;

    [System.Serializable]
    public class WheelData
    {
        public Transform transform;
        public WheelCollider collider;
        public bool steering;
        [HideInInspector]
        public Vector3 origPos;
    }
    [Header("Wheels[0].FL Wheels[1].FR Wheels[2].RL Wheels[3].RR")]
    public List<WheelData> Wheels = new List<WheelData>();

    // map data
    public string id { get; set; }
    public Transform AgentTransform => transform;

    // targeting
    private Transform frontCenter;
    private Transform frontCenterHigh;
    private Transform frontLeft;
    private Transform frontRight;

    public float currentSpeed { get; private set; }
    public Vector3 steerVector { get; private set; } = Vector3.forward;
    public bool isLeftTurn = false;
    public bool isRightTurn = false;
    protected bool isForcedStop = false;

    private NPCManager NPCManager;
    private System.Random RandomGenerator = new System.Random();
    private MonoBehaviour FixedUpdateManager;
    protected int _seed;
    protected HashSet<Coroutine> Coroutines = new HashSet<Coroutine>();
    private MapIntersection currentIntersection = null; // TODO
    #endregion

    #region behavior vars
    // physics
    private LayerMask groundHitBitmask;
    private LayerMask carCheckBlockBitmask;
    protected RaycastHit frontClosestHitInfo = new RaycastHit();
    protected RaycastHit frontHighClosestHitInfo = new RaycastHit();
    protected RaycastHit leftClosestHitInfo = new RaycastHit();
    protected RaycastHit rightClosestHitInfo = new RaycastHit();
    protected RaycastHit groundCheckInfo = new RaycastHit();
    protected float frontRaycastDistance = 20f;
    public bool atStopTarget;

    // map data
    public MapTrafficLane currentMapLane;
    public MapTrafficLane prevMapLane;
    protected List<Vector3> laneData;

    // targeting
    protected Vector3 currentTarget;
    protected int currentIndex = 0;
    protected float distanceToCurrentTarget = 0f;
    protected float distanceToStopTarget = 0;
    protected Vector3 stopTarget = Vector3.zero;
    protected float minTargetDistance = 1f;

    private float laneSpeedLimit = 0f;
    private float normalSpeed = 0f;

    public float targetSpeed = 0f;
    public float targetTurn = 0f;
    public float currentTurn = 0f;

    private float speedAdjustRate = 4.0f;
    private float minSpeedAdjustRate = 1f;
    private float maxSpeedAdjustRate = 4f;
    private float elapsedAccelerateTime = 0f;
    private float turnAdjustRate = 10.0f;

    private float stopHitDistance = 5f;
    private float stopLineDistance = 15f;
    private float stopSignWaitTime = 1f; // TODO 3sec
    private float currentStopTime = 0f;

    private float aggressionAdjustRate;
    private int aggression;

    private bool isLaneDataSet = false;
    private bool isFrontDetectWithinStopDistance = false;
    private bool isFrontDetectHighWithinStopDistance = false;
    private bool isRightDetectWithinStopDistance = false;
    private bool isLeftDetectWithinStopDistance = false;
    private bool isFrontLeftDetect = false;
    private bool isFrontRightDetect = false;
    private bool hasReachedStopSign = false;
    private bool isStopLight = false;
    private bool isStopSign = false;
    private bool isCurve = false;
    private bool laneChange = false;
    private bool isDodge = false;
    private bool isWaitingToDodge = false;

    private Collider[] MaxHitColliders = new Collider[5];
    #endregion

    #region mono
    private void Awake()
    {
        SimulatorManager.Instance.CameraManager.CameraController.UseFixedUpdate = true;
        groundHitBitmask = LayerMask.GetMask("Default");
        carCheckBlockBitmask = LayerMask.GetMask("Agent", "NPC", "Pedestrian", "Obstacle");
        RB = GetComponent<Rigidbody>();
        VA = GetComponent<IVehicleActions>();
        MainCollider = GetComponentInChildren<MeshCollider>();
        Init(RandomGenerator.Next());
    }

    private void Start()
    {
        InitLaneData();
    }

    public void FixedUpdate()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (Time.fixedDeltaTime > 0)
        {
            var previousVelocity = simpleVelocity;
            simpleVelocity = (RB.position - lastRBPosition) / Time.fixedDeltaTime;
            simpleAcceleration = simpleVelocity - previousVelocity;

            Vector3 euler1 = lastRBRotation.eulerAngles;
            Vector3 euler2 = RB.rotation.eulerAngles;
            Vector3 diff = euler2 - euler1;
            for (int i = 0; i < 3; i++)
            {
                diff[i] = (diff[i] + 180) % 360 - 180;
            }
            simpleAngularVelocity = diff / Time.fixedDeltaTime * Mathf.Deg2Rad;
            SetLastPosRot(RB.position, RB.rotation);
        }

        // behavior physics update
        if (isLaneDataSet)
        {
            ToggleBrakeLights();
            CollisionCheck();
            EvaluateTarget();
            SetTargetSpeed();
            SetTargetTurn();
            NPCTurn();
            NPCMove();
        }

        if (currentSpeed > 0.1f && Wheels != null && Wheels.Count > 0)
        {
            WheelMovement();
        }
    }
    #endregion

    #region init
    public void Init(int seed)
    {
        FixedUpdateManager = SimulatorManager.Instance.FixedUpdateManager;
        RandomGenerator = new System.Random(seed);
        _seed = seed;
        SetNeededComponents();

        aggression = 3 - (_seed % 3);
        stopHitDistance = 12 / aggression;
        speedAdjustRate = 2 + 2 * aggression;
        maxSpeedAdjustRate = speedAdjustRate; // more aggressive NPCs will accelerate faster
        turnAdjustRate = 50 * aggression;
    }

    private void InitLaneData()
    {
        ResetData();

        var lane = SimulatorManager.Instance.MapManager.GetClosestLane(transform.position);

        laneSpeedLimit = lane.speedLimit;
        if (laneSpeedLimit > 0)
        {
            aggressionAdjustRate = laneSpeedLimit / 11.176f; // give more space at faster speeds
            stopHitDistance = 12 / aggression * aggressionAdjustRate;
        }
        normalSpeed = RandomGenerator.NextFloat(laneSpeedLimit, laneSpeedLimit + 1 + aggression);
        currentMapLane = lane;

        int index = -1;
        float minDist = float.PositiveInfinity;
        Vector3 closest = Vector3.zero;

        // choose closest waypoint
        for (int i = 0; i < lane.mapWorldPositions.Count - 1; i++)
        {
            var p0 = lane.mapWorldPositions[i];
            var p1 = lane.mapWorldPositions[i + 1];

            var p = Utility.ClosetPointOnSegment(p0, p1, transform.position);

            float d = Vector3.SqrMagnitude(transform.position - p);
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

        laneData = new List<Vector3>(lane.mapWorldPositions);
        isDodge = false;
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

        SetLastPosRot(transform.position, transform.rotation);
        isLaneDataSet = true;
    }

    private void SetNeededComponents()
    {
        wheelColliderHolder = transform.Find("WheelColliderHolder");
        foreach (var wheel in Wheels)
        {
            wheel.origPos = wheelColliderHolder.transform.InverseTransformPoint(wheel.transform.position);
        }

        var allRenderers = GetComponentsInChildren<Renderer>().ToList();
        Bounds = new Bounds(transform.position, Vector3.zero);
        foreach (Renderer renderer in allRenderers)
        {
            Bounds.Encapsulate(renderer.bounds); // renderer.bounds is world space 
        }

        RB.centerOfMass = Bounds.center + new Vector3(0, 0, Bounds.extents.z * 0.3f);

        // front transforms
        GameObject go = new GameObject("Front");
        go.transform.position = new Vector3(Bounds.center.x, Bounds.min.y + 0.5f, Bounds.center.z + Bounds.max.z);
        go.transform.SetParent(transform, true);
        frontCenter = go.transform;
        go = new GameObject("FrontHigh");
        go.transform.position = new Vector3(Bounds.center.x, Bounds.max.y, Bounds.center.z + Bounds.max.z);
        go.transform.SetParent(transform, true);
        frontCenterHigh = go.transform;
        go = new GameObject("Right");
        go.transform.position = new Vector3(Bounds.center.x + Bounds.max.x, Bounds.min.y + 0.5f, Bounds.center.z + Bounds.max.z);
        go.transform.SetParent(transform, true);
        frontRight = go.transform;
        go = new GameObject("Left");
        go.transform.position = new Vector3(Bounds.center.x - Bounds.max.x, Bounds.min.y + 0.5f, Bounds.center.z + Bounds.max.z);
        go.transform.SetParent(transform, true);
        frontLeft = go.transform;
    }

    public void SetLastPosRot(Vector3 pos, Quaternion rot)
    {
        lastRBPosition = pos;
        lastRBRotation = rot;
    }

    private void ResetData()
    {
        if (FixedUpdateManager != null)
        {
            foreach (Coroutine coroutine in Coroutines)
            {
                if (coroutine != null)
                {
                    FixedUpdateManager.StopCoroutine(coroutine);
                }
            }
        }
        StopAllCoroutines();

        RB.angularVelocity = Vector3.zero;
        RB.velocity = Vector3.zero;
        simpleVelocity = Vector3.zero;
        simpleAngularVelocity = Vector3.zero;
        currentMapLane = null;
        prevMapLane = null;
        currentIntersection = null;
        currentIndex = 0;
        laneSpeedLimit = 0f;
        currentSpeed = 0f;
        currentStopTime = 0f;
        foreach (var intersection in SimulatorManager.Instance.MapManager.intersections)
        {
            //intersection.ExitStopSignQueue(controller); // TODO Agent enter queues
            //intersection.ExitIntersectionList(controller);
        }
        //ResetLights();
        isLeftTurn = false;
        isRightTurn = false;
        isForcedStop = false;
        isCurve = false;
        isWaitingToDodge = false;
        isDodge = false;
        laneChange = true;
        isStopLight = false;
        isStopSign = false;
        hasReachedStopSign = false;
        isLaneDataSet = false;
        SetLastPosRot(transform.position, transform.rotation);
    }
    #endregion

    #region physics
    public float MovementSpeed { get; set; }
    public Vector3 Acceleration => simpleAcceleration;
    public Vector3 GetVelocity => simpleVelocity;
    public Vector3 GetAngularVelocity => simpleAngularVelocity;

    private void NPCMove()
    {
        var movement = RB.position + transform.forward * currentSpeed * Time.fixedDeltaTime;
        RB.MovePosition(new Vector3(movement.x, RB.position.y, movement.z));
    }

    private void NPCTurn()
    {
        RB.MoveRotation(RB.rotation * Quaternion.Euler(0f, currentTurn * Time.fixedDeltaTime, 0f));
    }
    #endregion

    #region inputs
    public void ForceEStop(bool isStop)
    {
        isForcedStop = isStop;
    }

    protected virtual void SetTargetTurn()
    {
        steerVector = (currentTarget - frontCenter.position).normalized;

        float steer = Vector3.Angle(steerVector, frontCenter.forward) * 1.5f;
        targetTurn = Vector3.Cross(frontCenter.forward, steerVector).y < 0 ? -steer : steer;
        currentTurn += turnAdjustRate * Time.fixedDeltaTime * (targetTurn - currentTurn);

        if (targetSpeed == 0)
        {
            currentTurn = 0;
        }
    }

    protected virtual void SetTargetSpeed()
    {
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
    }

    protected float GetLerpedDistanceToStopTarget()
    {
        float tempD = 0f;
        if (isFrontDetectWithinStopDistance) // raycast
        {
            tempD = frontClosestHitInfo.distance / stopHitDistance;
            if (frontClosestHitInfo.distance < stopHitDistance)
            {
                tempD = 0f;
            }
        }
        else // stop target
        {
            tempD = distanceToStopTarget > stopLineDistance ? stopLineDistance : distanceToStopTarget / stopLineDistance;
            if (distanceToStopTarget < minTargetDistance)
            {
                tempD = 0f;
            }
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
        //prevMapLane.stopLine.intersection.EnterStopSignQueue(controller);
        hasReachedStopSign = true;
        yield return FixedUpdateManager.WaitForFixedSeconds(stopSignWaitTime);
        //yield return FixedUpdateManager.WaitUntilFixed(() => prevMapLane.stopLine.intersection.CheckStopSignQueue(controller));
        hasReachedStopSign = false;
        isStopSign = false;
    }

    IEnumerator WaitTrafficLight()
    {
        currentStopTime = 0f;
        yield return FixedUpdateManager.WaitUntilFixed(() => distanceToStopTarget <= stopLineDistance);
        if (prevMapLane.stopLine.currentState == MapData.SignalLightStateType.Green)
        {
            yield break; // light is green so just go
        }

        isStopLight = true;
        yield return FixedUpdateManager.WaitUntilFixed(() => atStopTarget); // wait if until reaching stop line
        if ((isRightTurn && prevMapLane.rightLaneReverse == null))
        {
            var waitTime = RandomGenerator.NextFloat(0f, 3f);
            var startTime = currentStopTime;
            yield return FixedUpdateManager.WaitUntilFixed(() => prevMapLane.stopLine.currentState == MapData.SignalLightStateType.Green || currentStopTime - startTime >= waitTime);
            isStopLight = false;
            yield break;
        }

        yield return FixedUpdateManager.WaitUntilFixed(() => prevMapLane.stopLine.currentState == MapData.SignalLightStateType.Green); // wait until green light
        if (isLeftTurn || isRightTurn)
        {
            yield return FixedUpdateManager.WaitForFixedSeconds(RandomGenerator.NextFloat(1f, 2f)); // wait to creep out on turn
        }

        isStopLight = false;
    }

    protected void StopTimeDespawnCheck()
    {
        if (isStopLight || isStopSign || (simpleVelocity.magnitude < 0.013f))
        {
            currentStopTime += Time.fixedDeltaTime;
        }

        if (currentStopTime > 200f)
        {
            Debug.Log($"Stopped for {currentStopTime} seconds");
            Despawn();
        }
    }

    protected bool IsYieldToIntersectionLane() // TODO stopping car
    {
        if (NPCManager == null)
        {
            NPCManager = SimulatorManager.Instance.NPCManager;
        }

        var state = false;
        if (currentMapLane != null)
        {
            var threshold = Vector3.Distance(currentMapLane.mapWorldPositions[0], currentMapLane.mapWorldPositions[currentMapLane.mapWorldPositions.Count - 1]) / 6;
            if (Vector3.Distance(transform.position, currentMapLane.mapWorldPositions[0]) < threshold) // If not far enough into lane, NPC will just go
            {
                for (int i = 0; i < NPCManager.CurrentPooledNPCs.Count; i++)
                {
                    var npc = NPCManager.CurrentPooledNPCs[i];
                    if (!npc.gameObject.activeInHierarchy)
                    {
                        continue; // Ignore NPCs that have been despawned
                    }

                    var laneFollow = npc.GetComponent<NPCLaneFollowBehaviour>();
                    if (laneFollow == null)
                    {
                        continue;
                    }

                    for (int k = 0; k < currentMapLane.yieldToLanes.Count; k++)
                    {
                        if (laneFollow.currentMapLane == null)
                        {
                            continue;
                        }
                        if (laneFollow.currentMapLane == currentMapLane.yieldToLanes[k]) // checks each active NPC if it is in a yieldTo lane
                        {
                            if (Vector3.Dot(NPCManager.CurrentPooledNPCs[i].transform.position - transform.position, transform.forward) > 0.5f) // Only yields if the other NPC is in front
                            {
                                state = true;
                            }
                        }
                        else
                        {
                            if (currentMapLane.yieldToLanes[k] == null)
                            {
                                Debug.LogWarning($"MapLane YieldToLane index {k} is missing please fix", currentMapLane.gameObject);
                                return false;
                            }
                            for (int j = 0; j < currentMapLane.yieldToLanes[k].prevConnectedLanes.Count; j++) // checks each active NPC if it is approaching a yieldTo lane
                            {
                                if (laneFollow.currentMapLane == currentMapLane.yieldToLanes[k].prevConnectedLanes[j])
                                {
                                    var a = NPCManager.CurrentPooledNPCs[i].transform.position;
                                    var b = currentMapLane.yieldToLanes[k].prevConnectedLanes[j].mapWorldPositions[currentMapLane.yieldToLanes[k].prevConnectedLanes[j].mapWorldPositions.Count - 1];

                                    if (Vector3.Distance(a, b) < 40 / aggression) // if other NPC is close enough to intersection, NPC will not make turn
                                    {
                                        state = true;
                                        if (laneFollow.currentSpeed < 1f) // if other NPC is yielding to others or stopped for other reasons
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
        {
            if (prevMapLane.stopLine.currentState == MapData.SignalLightStateType.Yellow || prevMapLane.stopLine.currentState == MapData.SignalLightStateType.Red)
            {
                state = false;
            }
        }

        // check for ped in road
        if (Physics.OverlapSphereNonAlloc(frontCenter.position + Vector3.forward, 2f, MaxHitColliders, 1 << LayerMask.NameToLayer("Pedestrian")) > 0)
        {
            state = true;
        }

        // check for ego
        if (Physics.OverlapSphereNonAlloc(frontCenter.position, 1.5f, MaxHitColliders, 1 << LayerMask.NameToLayer("NPC")) > 0)
        {
            state = true;
        }

        // check for npc
        var currentLayer = gameObject.layer;
        MainCollider.gameObject.layer = 2; // move collider off raycast layer to check
        if (Physics.OverlapSphereNonAlloc(frontCenter.position, 1.5f, MaxHitColliders, 1 << LayerMask.NameToLayer("Agent")) > 0)
        {
            state = true;
        }
        MainCollider.gameObject.layer = currentLayer;

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

    protected void SetChangeLaneData(List<Vector3> data)
    {
        laneData = new List<Vector3>(data);
        currentIndex = SimulatorManager.Instance.MapManager.GetLaneNextIndex(transform.position, currentMapLane);
        currentTarget = laneData[currentIndex];
        isDodge = false;
    }

    public void OnDrawGizmos()
    {
        if (!DebugMode)
        {
            return;
        }

        if (!isLaneDataSet)
        {
            return;
        }

        for (int i = 0; i < laneData.Count-1; i++)
        {
            Debug.DrawLine(laneData[i], laneData[i+1], currentIndex == i ? Color.yellow : Color.red);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(currentTarget, 0.5f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(stopTarget, 0.5f);
    }

    protected void EvaluateTarget()
    {
        distanceToCurrentTarget = Vector3.Distance(new Vector3(frontCenter.position.x, 0f, frontCenter.position.z), new Vector3(currentTarget.x, 0f, currentTarget.z));
        distanceToStopTarget = Vector3.Distance(new Vector3(frontCenter.position.x, 0f, frontCenter.position.z), new Vector3(stopTarget.x, 0f, stopTarget.z));

        if (distanceToStopTarget < 1f)
        {
            if (!atStopTarget)
            {
                atStopTarget = true;
            }
        }
        else
        {
            atStopTarget = false;
        }

        // check if we are past the target or reached current target
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
                Coroutines.Add(FixedUpdateManager.StartCoroutine(DelayChangeLane()));
            }
            else
            {
                // GetNextLane
                // last index of current lane data
                if (currentMapLane?.nextConnectedLanes.Count >= 1) // choose next path and set waypoints
                {
                    currentMapLane = currentMapLane.nextConnectedLanes[RandomGenerator.Next(currentMapLane.nextConnectedLanes.Count)];
                    laneSpeedLimit = currentMapLane.speedLimit;
                    aggressionAdjustRate = laneSpeedLimit / 11.176f; // 11.176 m/s corresponds to 25 mph
                    normalSpeed = RandomGenerator.NextFloat(laneSpeedLimit, laneSpeedLimit + 1 + aggression);
                    SetLaneData(currentMapLane.mapWorldPositions);
                    SetTurnSignal();
                }
                else
                {
                    Despawn(); // issue getting new waypoints so despawn
                }
            }
        }

        // isTurn
        if (currentMapLane == null)
        {
            return;
        }

        var path = transform.InverseTransformPoint(currentTarget).x;
        isCurve = path < -1f || path > 1f ? true : false;
    }

    protected IEnumerator DelayChangeLane()
    {
        if (currentMapLane == null)
            yield break;

        if (!currentMapLane.isTrafficLane)
            yield break;

        if (RandomGenerator.Next(100) < 98)
            yield break;

        if (!laneChange)
            yield break;

        if (currentMapLane.leftLaneForward != null)
        {
            isLeftTurn = true;
            isRightTurn = false;
            VA.LeftTurnSignal = isLeftTurn;
        }
        else if (currentMapLane.rightLaneForward != null)
        {
            isRightTurn = true;
            isLeftTurn = false;
            VA.LeftTurnSignal = isRightTurn;
        }

        yield return FixedUpdateManager.WaitForFixedSeconds(RandomGenerator.NextFloat(1f, 3f));

        if (currentIndex >= laneData.Count - 2)
        {
            isLeftTurn = isRightTurn = false;
            yield break;
        }

        SetLaneChange();
    }

    protected void SetLaneChange()
    {
        if (currentMapLane == null) // Prevent null if despawned during wait
            return;

        if (currentMapLane.leftLaneForward != null)
        {
            if (!isFrontLeftDetect)
            {
                currentMapLane = currentMapLane.leftLaneForward;
                laneSpeedLimit = currentMapLane.speedLimit;
                aggressionAdjustRate = laneSpeedLimit / 11.176f; // 11.176 m/s corresponds to 25 mph
                SetChangeLaneData(currentMapLane.mapWorldPositions);
                Coroutines.Add(FixedUpdateManager.StartCoroutine(DelayOffTurnSignals()));
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
                Coroutines.Add(FixedUpdateManager.StartCoroutine(DelayOffTurnSignals()));
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
                    Coroutines.Add(FixedUpdateManager.StartCoroutine(DelayOffTurnSignals()));
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
                    Coroutines.Add(FixedUpdateManager.StartCoroutine(DelayOffTurnSignals()));
                }
            }
        }
    }

    protected void GetDodge()
    {
        if (currentMapLane == null)
            return;

        if (isDodge)
            return;

        if (IsYieldToIntersectionLane())
            return;

        if (isLeftDetectWithinStopDistance || isRightDetectWithinStopDistance)
        {
            var npcC = isLeftDetectWithinStopDistance ? leftClosestHitInfo.collider.GetComponentInParent<NPCLaneFollowBehaviour>() : rightClosestHitInfo.collider.GetComponentInParent<NPCLaneFollowBehaviour>();
            var aC = isLeftDetectWithinStopDistance ? leftClosestHitInfo.collider.transform.root.GetComponent<IAgentController>() : rightClosestHitInfo.collider.transform.root.GetComponent<IAgentController>();

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
                    {
                        Coroutines.Add(FixedUpdateManager.StartCoroutine(WaitToDodge(aC, isLeftDetectWithinStopDistance)));
                    }
                }
                else
                {
                    if (leftClosestHitInfo.collider?.gameObject?.GetComponentInParent<NPCController>() == null && leftClosestHitInfo.collider?.transform.root.GetComponent<IAgentController>() == null)
                    {
                        SetDodge(!isLeftDetectWithinStopDistance);
                    }
                }
            }
            else // intersection lane
            {
                if (npcC != null)
                {
                    if ((isLeftTurn && npcC.isLeftTurn || isRightTurn && npcC.isRightTurn) && Vector3.Dot(transform.TransformDirection(Vector3.forward), npcC.transform.TransformDirection(Vector3.forward)) < -0.7f)
                    {
                        if (currentIndex > 1)
                        {
                            SetDodge(isLeftTurn, true);
                        }
                    }
                }
            }
        }
    }

    IEnumerator WaitToDodge(IAgentController aC, bool isLeft)
    {
        isWaitingToDodge = true;
        float elapsedTime = 0f;
        while (elapsedTime < 5f)
        {
            if (aC.AgentGameObject.GetComponent<Rigidbody>().velocity.magnitude > 0.01f)
            {
                isWaitingToDodge = false;
                yield break;
            }
            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        if (!isLeft)
        {
            SetDodge(true);
        }
        else
        {
            SetDodge(false);
        }
        isWaitingToDodge = false;
    }

    protected void SetDodge(bool isLeft, bool isShortDodge = false)
    {
        if (isStopSign || isStopLight) return;

        Transform startTransform = isLeft ? frontLeft : frontRight;
        float firstDodgeAngle = isLeft ? -15f : 15f;
        float secondDodgeAngle = isLeft ? -5f : 5f;
        float shortDodgeAngle = isLeft ? -40f : 40f;
        Vector3 dodgeTarget;
        var dodgeData = new List<Vector3>();

        if ((isLeft && isFrontLeftDetect && !isShortDodge) || (!isLeft && isFrontRightDetect && !isShortDodge))
            return;

        if ((isLeft && currentMapLane.leftLaneForward == null && !isShortDodge) || (!isLeft && currentMapLane.rightLaneForward == null && !isShortDodge))
            return;

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
                laneData.RemoveAt(currentIndex);
            }
        }

        laneData.InsertRange(currentIndex, dodgeData);

        currentTarget = laneData[currentIndex];
    }

    protected IEnumerator DelayOffTurnSignals()
    {
        yield return FixedUpdateManager.WaitForFixedSeconds(3f);
        isLeftTurn = isRightTurn = false;
        VA.LeftTurnSignal = isLeftTurn;
        VA.RightTurnSignal = isRightTurn;
    }

    protected void SetTurnSignal(bool forceLeftTS = false, bool forceRightTS = false)
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
        VA.LeftTurnSignal = isLeftTurn;
        VA.RightTurnSignal = isRightTurn;
    }
    #endregion

    #region utility
    public void WheelMovement()
    {
        float theta = (simpleVelocity.magnitude * Time.fixedDeltaTime / Wheels[0].collider.radius) * Mathf.Rad2Deg;
        Quaternion finalQ = Quaternion.LookRotation(steerVector);
        Vector3 finalE = finalQ.eulerAngles;
        finalQ = Quaternion.Euler(0f, finalE.y, 0f);

        foreach (var wheel in Wheels)
        {
            MoveWheel(wheel.transform, wheel.collider, wheel.origPos, theta, finalQ, wheel.steering);
        }
    }

    private void MoveWheel(Transform wheel, WheelCollider collider, Vector3 origPos, float theta, Quaternion Q, bool steering)
    {
        if (wheel.localPosition != origPos)
        {
            wheel.localPosition = origPos;
        }

        if (steering)
        {
            wheel.rotation = Quaternion.RotateTowards(wheel.rotation, Q, Time.fixedDeltaTime * 50f);
        }

        wheel.transform.Rotate(Vector3.right, theta, Space.Self);

        Vector3 pos;
        Quaternion rot;
        collider.GetWorldPose(out pos, out rot);
        wheel.position = pos;
    }

    protected void CollisionCheck()
    {
        if (frontCenter == null || frontLeft == null || frontRight == null || frontCenterHigh == null)
        {
            return;
        }

        frontClosestHitInfo = new RaycastHit();
        frontHighClosestHitInfo = new RaycastHit();
        rightClosestHitInfo = new RaycastHit();
        leftClosestHitInfo = new RaycastHit();

        Physics.Raycast(frontCenter.position, frontCenter.forward, out frontClosestHitInfo, frontRaycastDistance, carCheckBlockBitmask);
        Physics.Raycast(frontCenterHigh.position, frontCenterHigh.forward, out frontHighClosestHitInfo, frontRaycastDistance, carCheckBlockBitmask);
        Physics.Raycast(frontRight.position, frontRight.forward, out rightClosestHitInfo, frontRaycastDistance / 2, carCheckBlockBitmask);
        Physics.Raycast(frontLeft.position, frontLeft.forward, out leftClosestHitInfo, frontRaycastDistance / 2, carCheckBlockBitmask);
        isFrontLeftDetect = Physics.CheckSphere(frontLeft.position - (frontLeft.right * 2), 1f, carCheckBlockBitmask);
        isFrontRightDetect = Physics.CheckSphere(frontRight.position + (frontRight.right * 2), 1f, carCheckBlockBitmask);

        if ((currentMapLane.isIntersectionLane || Vector3.Distance(transform.position, currentMapLane.mapWorldPositions[currentMapLane.mapWorldPositions.Count - 1]) < 10) && !isRightTurn && !isLeftTurn)
        {
            // if going straight through an intersection or is approaching the end of the current lane, give more space
            stopHitDistance = Mathf.Lerp(4f, 20 / aggression * aggressionAdjustRate, currentSpeed / laneSpeedLimit);
        }
        else
        {
            // higher aggression and/or lower speeds -> lower stophitdistance
            stopHitDistance = Mathf.Lerp(4f, 12 / aggression * aggressionAdjustRate, currentSpeed / laneSpeedLimit);
        }

        isFrontDetectWithinStopDistance = (frontClosestHitInfo.collider) && frontClosestHitInfo.distance < stopHitDistance;
        isFrontDetectHighWithinStopDistance = (frontHighClosestHitInfo.collider) && frontHighClosestHitInfo.distance < stopHitDistance;
        if (isFrontDetectHighWithinStopDistance)
        {
            isFrontDetectWithinStopDistance = true;
            frontClosestHitInfo = frontHighClosestHitInfo;
        }
        isRightDetectWithinStopDistance = (rightClosestHitInfo.collider) && rightClosestHitInfo.distance < stopHitDistance / 2;
        isLeftDetectWithinStopDistance = (leftClosestHitInfo.collider) && leftClosestHitInfo.distance < stopHitDistance / 2;

        // ground collision
        groundCheckInfo = new RaycastHit();
        if (!Physics.Raycast(transform.position + transform.up, -transform.up, out groundCheckInfo, 5f, groundHitBitmask))
        {
            Debug.Log("Not on ground!");
            Despawn();
        }

        // debug
        //if (frontClosestHitInfo.collider != null)
        //    Debug.DrawLine(controller.frontCenter.position, frontClosestHitInfo.point, Color.blue, 0.25f);
        //if (frontHighClosestHitInfo.collider != null)
        //    Debug.DrawLine(controller.frontCenterHigh.position, frontHighClosestHitInfo.point, Color.green, 0.25f);
        //if (leftClosestHitInfo.collider != null)
        //    Debug.DrawLine(controller.frontLeft.position, leftClosestHitInfo.point, Color.yellow, 0.25f);
        //if (rightClosestHitInfo.collider != null)
        //    Debug.DrawLine(controller.frontRight.position, rightClosestHitInfo.point, Color.red, 0.25f);
    }

    protected float SetFrontDetectSpeed()
    {
        var blocking = frontClosestHitInfo.transform;
        blocking = blocking ?? rightClosestHitInfo.transform;
        blocking = blocking ?? leftClosestHitInfo.transform;

        float tempS = 0f;
        // TODO logic has changed and this is causing an issue with behavior SetFrontDetectSpeed should never have frontClosestHitInfo.distance > stopHitDistance
        if (Vector3.Dot(transform.forward, blocking.transform.forward) > 0.7f) // detected is on similar vector
        {
            if (frontClosestHitInfo.distance > stopHitDistance)
            {
                tempS = (normalSpeed) * (frontClosestHitInfo.distance / stopHitDistance);
            }
        }
        //else if (Vector3.Dot(transform.forward, blocking.transform.forward) < -0.2f && (isRightTurn || isLeftTurn))
        //{
        //    tempS = normalSpeed;
        //}
        return tempS;
    }
    #endregion

    #region misc
    private void Despawn()
    {
        if (SimulatorManager.InstanceAvailable)
        {
            SimulatorManager.Instance.AgentManager.ResetAgent();
        }
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
                    Coroutines.Add(FixedUpdateManager.StartCoroutine(WaitStopSign()));
                }
                else
                {
                    Coroutines.Add(FixedUpdateManager.StartCoroutine(WaitTrafficLight()));
                }
            }
        }
    }
    #endregion

    #region lights
    protected void ToggleBrakeLights()
    {
        if (targetSpeed < 2f || isStopLight || isFrontDetectWithinStopDistance || isFrontDetectHighWithinStopDistance || (isStopSign && distanceToStopTarget < stopLineDistance))
        {
            VA.BrakeLights = true;
        }
        else
        {
            VA.BrakeLights = false;
        }
    }
    #endregion

    public bool ForceReset(Vector3 pos, Quaternion rot)
    {
        RB.Sleep();
        RB.MovePosition(pos);
        RB.MoveRotation(rot);
        transform.SetPositionAndRotation(pos, rot);
        foreach (var wheel in Wheels)
        {
            wheel.collider.brakeTorque = Mathf.Infinity;
            wheel.collider.brakeTorque = Mathf.Infinity;
            wheel.collider.motorTorque = 0f;
            wheel.collider.motorTorque = 0f;
        }
        InitLaneData();
        return true;
    }

    public bool GearboxShiftDown()
    {
        return false;
    }

    public bool GearboxShiftUp()
    {
        return false;
    }

    public bool SetHandBrake(bool state)
    {
        return false;
    }

    public bool ShiftFirstGear()
    {
        return false;
    }

    public bool ShiftReverse()
    {
        return false;
    }

    public bool ShiftReverseAutoGearBox()
    {
        return false;
    }

    public bool ToggleHandBrake()
    {
        return false;
    }

    public bool ToggleIgnition()
    {
        return false;
    }

    public bool ToggleReverse()
    {
        return false;
    }
}
