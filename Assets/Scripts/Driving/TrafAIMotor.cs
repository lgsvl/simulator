/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class TrafAIMotor : MonoBehaviour
{
    private bool inited = false;

    //low res variables for logic
    private const float lowResTargetDeltaTime = 0.2f; // Should be higher than expected average delta time
    [System.NonSerialized]
    public float lowResTimestamp;
    private float lowResTimeOffset;
    private float deltaTime; // as a temp var

    //low res variables for physics
    private const float lowResFixedDeltaTime = 0.1f; // 5 times to default time step
    [System.NonSerialized]
    public float lowResPhysicsTimestamp;
    private float lowResPhysicsTimeOffset;
    private float lowResPhysicsDeltaTime; // as a temp var

    public bool DEBUG = false;
    public bool forceShiftLaneDebugFlag = false;

    public TrafSystem system;
    private CarAIController CarAICtrl;

    public const float waypointThreshold = 1.0f;
    public const float giveWayRegisterDistance = 33f; //Also act as a turn signal distance
    public const float turnSlowDownDist = 12f;
    public const float frontBrakeRaycastDistance = 24f;
    public const float frontSideRaycastDistance = 8f; // calculated value
    public const float yellowLightGoDistance = 4f;
    public const float stopLength = 6f;

    private int lastIndex; //debugging only for noe
    public int currentIndex;
    private TrafEntry lastEntry; //debugging only for noe
    public TrafEntry currentEntry;
    private TrafEntry nextEntry;
    private bool hasNextEntry;

    private TrafEntry registeredEntry;

    public Rigidbody rb;

    public float currentSpeed;
    public float targetSpeed;
    public float currentTurn;
    public Transform nose;
    public Transform noseLeft;
    public Transform noseRight;

    [HideInInspector]
    public float maxSpeed;
    private bool useRandomMaxSpeedAndBrake = true;
    private readonly static Vector2 maxSpeedRange = new Vector2(10.0f, 16.0f); // 10 - 16
    private readonly static Vector2 maxBrakeRange = new Vector2(8.5f, 18.0f); // 8.5 - 18
    public const float maxTurn = 75f;
    public const float maxAccell = 3f;
    public float maxBrake;
    private bool emergencyHardBrake;
    private float emergencyMaxBrake;
    private float brakeHardRelativeSpeed;
    [System.NonSerialized]
    public bool brakeHard;

    private Vector3 dodgeVector;
    private float intersectionCornerSpeed;
    private float targetHeight;

    public bool hasStopTarget;
    public bool hasGiveWayTarget;
    public Vector3 stopTarget;
    public Vector3 targetTangent;
    private Vector3 target;
    private Vector3 nextTarget;

    private bool doRaycast;

    int dodgeCode;

    float stuckTime;
    float unreachTime;
    const float trafficJamStuckThreshold = 110f;
    const float unreachTimeThreshold = 150f;
    Vector3 lastUpdatePosition;

    public bool triggerShiftToPlayer = false;
    public const float laneShiftEndDistThreshold = 37.5f;
    private readonly static Vector2 shiftLaneAdvanceDist = new Vector2(10f, 12.5f);
    private const float shiftLaneTargetThreshold = 8f; //Can be replaced with collider later
    private bool shiftingLane;
    private Vector3 shiftLaneTarget;
    private TrafEntry shiftLaneEntry;
    private float shiftStartTimestamp;
    private const float shiftingTimeThreshold = 10f;
    public VehicleController playerVehicleCtrl;   

    private RaycastHit heightHit;

    private RaycastHit frontClosestHitInfo;
    private RaycastHit frontLeftHitInfo;
    private RaycastHit frontRightHitInfo;
    private float nextRaycast = 0f;
    [SerializeField]
    private bool somethingInFront;
    [SerializeField]
    private bool frontSideColliding;

    public bool leftTurn;
    public bool rightTurn;
    public bool blockedByTraffic;

    private float stopEnd;

    //117 118 119 120 121 122  100 101 102 165 129 52 53 34 35             

    public bool fixedRoute = false;
    public List<RoadGraphEdge> fixedPath;
    public int currentFixedNode;

    public bool forceToCollideMode;
    public Collider targetCol;
    private float startForceCollidingTime;
    private const float forceCollidingTimeout = 5f;

    public GameObject smokePrefab;
    private GameObject activeSmoke;
    public Transform engine;

    //Util function
    public static float Remap(float value, float from1, float to1, float from2, float to2)
    {
        if (from1 == to1 || from2 == to2)        
            return from2;
        
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    public void ForceCollide(Collider c)
    {
        forceToCollideMode = true;
        targetCol = c;
        startForceCollidingTime = Time.time;
    }

    //Util function to get closest point on a collider
    public static Vector3 GetClosestPointOnCollider(Collider col, Vector3 point)
    {
        var meshCol = col as MeshCollider;
        if ((col as BoxCollider) != null || (col as SphereCollider) != null || (col as CapsuleCollider) != null || (meshCol != null && meshCol.convex))
        {
            return col.ClosestPoint(point);
        }
        else
        {
            RaycastHit hitInfo = new RaycastHit();
            if (col.Raycast(new Ray(point, col.bounds.center - point), out hitInfo, frontSideRaycastDistance + 1f))
            {
                return hitInfo.point;
            }
            else
            {
                return col.ClosestPointOnBounds(point);
            }
        }
    }

    // Calculate the distance between
    // point pt and the segment p1 --> p2.
    private static Vector3 ClosestPointOnSegmentToPoint(Vector3 pt, Vector3 p1, Vector3 p2)
    {
        Vector3 closest = Vector3.zero;
        float dx = p2.x - p1.x;
        float dz = p2.z - p1.z;
        if ((dx == 0) && (dz == 0))
        {
            // It's a point not a line segment.
            return p1;
        }

        // Calculate the t that minimizes the distance.
        float t = ((pt.x - p1.x) * dx + (pt.z - p1.z) * dz) /
            (dx * dx + dz * dz);

        // See if this represents one of the segment's
        // end points or a point in the middle.
        if (t < 0)
        {
            return new Vector2(p1.x, p1.z);
        }
        else if (t > 1)
        {
            return new Vector2(p2.x, p2.z);
        }
        else
        {
            return new Vector2(p1.x + t * dx, p1.z + t * dz);
        }
    }

    public void Init(int index, TrafEntry entry)
    {
        if (inited)
            return;
        inited = true;

        CancelInvoke();
        StopCoroutine(HoldEmergencyHardBrakeState());

        if (activeSmoke != null)
        {
            Destroy(activeSmoke);
        }

        currentIndex = index;
        lastIndex = currentIndex;
        currentEntry = entry;

        target = currentEntry.waypoints[currentIndex];
        lastEntry = currentEntry;
        CheckHeight();
        nextRaycast = 0f;
        //CheckHeight();

        InvokeRepeating("CheckHeight", Random.Range(0.2f, 0.4f), 0.2f);

        CarAICtrl = GetComponent<CarAIController>();
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        
        lowResTimestamp = 0f;
        lowResPhysicsTimestamp = 0f;
        lowResTimeOffset = Random.Range(0.0f, lowResTargetDeltaTime);
        lowResPhysicsTimeOffset = Random.Range(0.0f, lowResFixedDeltaTime);
        lowResTimestamp = Time.time + lowResTimeOffset;
        lowResPhysicsTimestamp = Time.fixedTime + lowResPhysicsTimeOffset;

        if (useRandomMaxSpeedAndBrake)
        {
            maxSpeed = Random.Range(maxSpeedRange.x, maxSpeedRange.y);
            maxBrake = Remap(maxSpeed, maxSpeedRange.x, maxSpeedRange.y, maxBrakeRange.x, maxBrakeRange.y);
        }

        emergencyMaxBrake = Random.Range(17f, 20f);
        lastUpdatePosition = nose.transform.position;
        stuckTime = 0f;
        unreachTime = 0f;
        targetTangent = Vector3.zero;
        nextTarget = Vector3.zero;
        intersectionCornerSpeed = 1f;
        targetHeight = 0f;
        stopEnd = 0f;
        dodgeVector = Vector3.zero;
        brakeHardRelativeSpeed = 0.2f;
        dodgeCode = 0;

        nextEntry = null;
        doRaycast = false;
        hasStopTarget = false;
        hasGiveWayTarget = false;
        shiftLaneEntry = null;
        leftTurn = false;
        rightTurn = false;
        blockedByTraffic = false;
        hasNextEntry = false;
        emergencyHardBrake = false;
        brakeHard = false;
        shiftingLane = false;
        shiftStartTimestamp = Time.time;
        somethingInFront = false;
        registeredEntry = null;
        forceToCollideMode = false;
        targetCol = null;
        activeSmoke = null;
    }

    public float NextRaycastTime()
    {
        return Time.time + Random.Range(0.2f, 0.25f); ;
    }

    public bool IsInAccident()
    {
        if (CarAICtrl != null && CarAICtrl.inAccident)
        {
            return true;
        }
        return false;
    }

    //check for something in front of us, populate blockedInfo if something was found
    private bool CheckFrontBlocked(out RaycastHit closestHit, out int dodgeCode)
    {
        bool ret = false;
        float minHitDistance = 1000f;
        RaycastHit hitInfo = new RaycastHit();
        closestHit = hitInfo;

        float midHitDist = 1000f;
        float leftHitDist = 1000f;
        float rightHitDist = 1000f;
        if (Physics.Raycast(nose.position, nose.forward, out hitInfo, frontBrakeRaycastDistance, ~(1 << LayerMask.NameToLayer("Ground And Road") | 1 << LayerMask.NameToLayer("PlayerConstrain"))))
        {
            midHitDist = hitInfo.distance;
            if (hitInfo.distance < minHitDistance)
            {
                minHitDistance = hitInfo.distance;
                closestHit = hitInfo;
            }
        }

        if (Physics.Raycast(noseRight.position, noseRight.forward, out hitInfo, frontBrakeRaycastDistance, ~(1 << LayerMask.NameToLayer("Ground And Road") | 1 << LayerMask.NameToLayer("PlayerConstrain"))))
        {
            rightHitDist = hitInfo.distance;
            if (hitInfo.distance < minHitDistance)
            {
                minHitDistance = hitInfo.distance;
                closestHit = hitInfo;
            }
        }

        if (Physics.Raycast(noseLeft.position, noseLeft.forward, out hitInfo, frontBrakeRaycastDistance, ~(1 << LayerMask.NameToLayer("Ground And Road") | 1 << LayerMask.NameToLayer("PlayerConstrain"))))
        {
            leftHitDist = hitInfo.distance;
            if (hitInfo.distance < minHitDistance)
            {
                minHitDistance = hitInfo.distance;
                closestHit = hitInfo;
            }
        }       

        if (closestHit.collider != null)
        {
            ret = true;
            var carAI = closestHit.collider.GetComponent<CarAIController>();
            var trafAI = closestHit.collider.GetComponent<TrafAIMotor>();
            var playerCar = closestHit.collider.GetComponentInParent<VehicleController>();
            var cTag = closestHit.collider.GetComponent<CustomTag>();
            if (carAI != null && !carAI.inAccident && trafAI.currentEntry == currentEntry && Vector3.Dot(nose.forward, trafAI.nose.forward) < 0) //If the same entry in car queue, don't dodge
            {
                dodgeCode = 0;
            }
            else if (cTag != null && cTag.tagName == "RoadDivider")
            {
                dodgeCode = 0;
            }
            else
            {
                if (midHitDist >= leftHitDist && midHitDist >= rightHitDist)
                {
                    if (leftHitDist - rightHitDist > 1.25f)
                    {
                        dodgeCode = -1;
                    }
                    else if (rightHitDist - leftHitDist > 1.25f)
                    {
                        dodgeCode = 1;
                    }
                    else
                    {
                        dodgeCode = 0;
                    }
                }
                else
                {
                    if (leftHitDist >= midHitDist && leftHitDist >= rightHitDist && leftHitDist - Mathf.Min(midHitDist, rightHitDist) > 1.25f)
                    {
                        dodgeCode = -1;
                    }
                    else if (rightHitDist >= midHitDist && rightHitDist >= leftHitDist && rightHitDist - Mathf.Min(midHitDist, leftHitDist) > 1.25f)
                    {
                        dodgeCode = 1;
                    }
                    else
                    {
                        dodgeCode = 0;
                    }

                    if (playerCar != null)
                    {
                        if (Mathf.Abs(midHitDist - minHitDistance) < 0.5f && Vector3.Dot(nose.forward, playerCar.transform.forward) > 0f)
                        {
                            dodgeCode = 0;
                        }
                    }
                }
            }
        }
        else
        {
            dodgeCode = 0;
        }

        return ret;
    }

    //TODO: tend to target height over time
    void CheckHeight()
    {
        var bitMask = 1 << LayerMask.NameToLayer("Ground And Road") | 1 << LayerMask.NameToLayer("Road Shoulder");
        if (Physics.Raycast(transform.position + Vector3.up * 3f, -Vector3.up, out heightHit, 15f, bitMask))
        {
            targetHeight = heightHit.point.y;
            if (rb != null)
            {
                rb.position = new Vector3(transform.position.x, Mathf.Lerp(transform.position.y, targetHeight, 0.5f), transform.position.z);
            }
        }        
    }

    void FixedUpdate()
    {
        if(!inited)
            return;

        lowResPhysicsDeltaTime = Time.fixedDeltaTime;
        if (CarAICtrl.simUpdateRate == CarAIController.SimUpdateRate.Low && Time.fixedDeltaTime < lowResFixedDeltaTime)
        {
            lowResPhysicsDeltaTime = Time.fixedTime - lowResPhysicsTimestamp;
            if (lowResPhysicsDeltaTime < lowResFixedDeltaTime)
            {
                return;
            }
            lowResPhysicsTimestamp = Time.fixedTime;
        }

        if (CarAICtrl && CarAICtrl.inAccident)
            return;

        MoveCar();        
    }

    public void CleanForReinit()
    {
        inited = false;
        if (registeredEntry != null)
        { registeredEntry.DeregisterInterest(); }
    }

    void Update()
    {
        if(!inited)
            return;

        deltaTime = Time.deltaTime;
        if (CarAICtrl.simUpdateRate == CarAIController.SimUpdateRate.Low && Time.deltaTime < lowResTargetDeltaTime)
        {
            deltaTime = Time.time - lowResTimestamp;
            if (deltaTime < lowResTargetDeltaTime)
            {
                return;
            }
            lowResTimestamp = Time.time;
        }

        var trafPerfManager = TrafPerformanceManager.Instance;

        if (trafPerfManager.autoAssistingTraffic)
        {
            //If car is stuck for a while then respawn
            if (stuckTime > trafficJamStuckThreshold || unreachTime > unreachTimeThreshold)                
            {
                if (!trafPerfManager.silentAssisting
                    || TrafSpawner.CheckSilentRespawnEligibility(CarAICtrl, Camera.main))
                {
                    CleanForReinit();

                    CarAICtrl.CancelInvoke();
                    if (trafPerfManager.silentAssisting)
                    { CarAICtrl.ReSpawnSilent(); }
                    else
                    { CarAICtrl.ReSpawn(); }
                }
            }
        }

        if ((nose.transform.position - lastUpdatePosition).magnitude < deltaTime)
        { stuckTime += deltaTime; }
        else
        { stuckTime = 0f; }
        lastUpdatePosition = nose.transform.position;

        if (CarAICtrl && CarAICtrl.inAccident)
            return;


        if (forceToCollideMode)
        {
            if (targetCol == null && Time.time - startForceCollidingTime > forceCollidingTimeout)
            {
                Debug.Log("Force Colliding Failed, switch to engine break down.");
                CarAICtrl.SetCarInAccidentState();
                //accident happens
                EngineBreakDown();
                return;
            }
            targetSpeed = 25f;
            target = targetCol.bounds.center;

            if (targetSpeed > currentSpeed)
            {
                currentSpeed += Mathf.Min(maxAccell * deltaTime, targetSpeed - currentSpeed);
                brakeHard = false;
            }
            else
            {
                if (currentSpeed - targetSpeed > brakeHardRelativeSpeed)
                {
                    brakeHard = true;
                }
                else
                {
                    brakeHard = false;
                }

                currentSpeed -= Mathf.Min(maxBrake * deltaTime, currentSpeed - targetSpeed);
                if (currentSpeed < 0)
                    currentSpeed = 0;
            }

            SteerCar();
            return;
        }

        //If the car is not in intersection area and is greater than the first path point and is right before the last path point of each entry
        if (!currentEntry.isIntersection() && currentIndex > 0 && !hasNextEntry)
        {
            if (!shiftingLane)
            {
                leftTurn = rightTurn = false;
            }

            //Handle shifting logic
            var distToEnd = Vector3.Distance(nose.position, currentEntry.waypoints[currentEntry.waypoints.Count - 1]);
            
            if (currentSpeed > 5f && (Random.value < 0.035F * deltaTime || forceShiftLaneDebugFlag || triggerShiftToPlayer))
            {            
                if (!shiftingLane && !currentEntry.isIntersection() && distToEnd > laneShiftEndDistThreshold
                    && !(Vector3.Dot(nose.forward, (currentEntry.waypoints[0] - nose.position).normalized) > 0
                    && Vector3.Dot(nose.forward, (currentEntry.waypoints[1] - nose.position).normalized) > 0))
                {
                    shiftingLane = true;
                    shiftStartTimestamp = Time.time;
                    List<int> subIdCandidates = new List<int>();
                    int perspectiveSubId = -1;
                    if (currentEntry.subIdentifier > 0)
                    {
                        perspectiveSubId = currentEntry.subIdentifier - 1;
                        shiftLaneEntry = system.GetEntry(currentEntry.identifier, perspectiveSubId);
                        if (shiftLaneEntry != null && //If the perspective lane is in same direction
                            Vector3.Dot(currentEntry.waypoints[currentEntry.waypoints.Count - 1] - currentEntry.waypoints[0]
                            , shiftLaneEntry.waypoints[currentEntry.waypoints.Count - 1] - shiftLaneEntry.waypoints[0]) > 0)
                        {
                            subIdCandidates.Add(currentEntry.subIdentifier - 1);
                        }
                    }

                    perspectiveSubId = currentEntry.subIdentifier + 1;
                    shiftLaneEntry = system.GetEntry(currentEntry.identifier, perspectiveSubId);
                    if (shiftLaneEntry != null && //If the perspective lane is in same direction
                        Vector3.Dot(currentEntry.waypoints[currentEntry.waypoints.Count - 1] - currentEntry.waypoints[0]
                        , shiftLaneEntry.waypoints[currentEntry.waypoints.Count - 1] - shiftLaneEntry.waypoints[0]) > 0)
                    {
                        subIdCandidates.Add(currentEntry.subIdentifier + 1);
                    }

                    if (subIdCandidates.Count > 0)
                    {
                        int targetSubId = -1;
                        if (triggerShiftToPlayer)
                        {
                            if (Vector3.Distance(nose.position, playerVehicleCtrl.carCenter.position) > 4f) // still need a little constrain for manual swerving
                            {
                                //Find the lane the player car is in
                                float minDist = 10000;
                                int finalIdCandidate = -1;
                                for (int i = 0; i < subIdCandidates.Count; i++)
                                {
                                    var tentativeDangerLaneEntry = system.GetEntry(currentEntry.identifier, subIdCandidates[i]);

                                    var wayPoints = tentativeDangerLaneEntry.GetPoints();
                                    Vector3 sumCenter = Vector3.zero;
                                    foreach (var point in wayPoints)
                                    {
                                        sumCenter += point;
                                    }
                                    sumCenter /= wayPoints.Length;

                                    var dist = Vector3.Distance(playerVehicleCtrl.carCenter.position, sumCenter);
                                    if (dist < minDist)
                                    {
                                        minDist = dist;
                                        finalIdCandidate = subIdCandidates[i];
                                    }
                                }
                                targetSubId = finalIdCandidate;
                            }

                            if (targetSubId < 0)
                            {
                                shiftingLane = false;
                                shiftLaneEntry = null;
                                triggerShiftToPlayer = false;
                            }
                        }
                        else
                        {
                            targetSubId = subIdCandidates[Random.Range(0, subIdCandidates.Count)];

                            var cols = Physics.OverlapSphere(nose.position, Random.Range(11f, 20f));
                            foreach (var col in cols)
                            {
                                var carFound = col.GetComponentInParent<TrafAIMotor>();

                                if (carFound != null)
                                {
                                    if (carFound == this)
                                    { continue; }

                                    //Check car on the side and side-rear before the actual lane shifting, if unqualified condition met, don't do lane shift
                                    if (targetSubId == carFound.currentEntry.subIdentifier
                                        && Vector3.Dot(nose.forward, (carFound.nose.position - nose.position).normalized) < 0.75f)
                                    {
                                        shiftingLane = false;
                                        shiftLaneEntry = null;
                                    }
                                }
                            }
                        }                    

                        if (shiftingLane)
                        {
                            shiftLaneEntry = system.GetEntry(currentEntry.identifier, targetSubId); //nail down the shiftLaneEntry
                            var targetNode = system.roadGraph.GetNode(currentEntry.identifier, targetSubId);

                            if (targetNode.HasEdges())
                            {
                                //Find closest waypoint
                                var wayPoints = shiftLaneEntry.GetPoints();
                                float min = 10000f;
                                int minDistIndex = -1;
                                for (int i = 0; i < wayPoints.Length; i++)
                                {
                                    var dist = Vector3.Distance(nose.position, wayPoints[i]);
                                    if (dist < min)
                                    {
                                        min = dist;
                                        minDistIndex = i;
                                    }
                                }

                                Vector2 closestPointXZ;
                                int anotherIndex;
                                if (Vector3.Dot(wayPoints[minDistIndex] - nose.position, nose.forward) > 0)
                                {

                                    if (minDistIndex == 0)
                                    { anotherIndex = minDistIndex + 1; }
                                    else
                                    { anotherIndex = minDistIndex - 1; }

                                    closestPointXZ = ClosestPointOnSegmentToPoint(nose.position, wayPoints[minDistIndex], wayPoints[anotherIndex]);
                                    Vector3 closestPoint = new Vector3(closestPointXZ.x, wayPoints[minDistIndex].y, closestPointXZ.y);
                                    shiftLaneTarget = closestPoint + (wayPoints[minDistIndex] - wayPoints[anotherIndex]).normalized * Random.Range(shiftLaneAdvanceDist.x, shiftLaneAdvanceDist.y);
                                }
                                else
                                {
                                    if (minDistIndex == wayPoints.Length - 1)
                                    { anotherIndex = minDistIndex - 1; }
                                    else
                                    { anotherIndex = minDistIndex + 1; }

                                    closestPointXZ = ClosestPointOnSegmentToPoint(nose.position, wayPoints[minDistIndex], wayPoints[anotherIndex]);
                                    Vector3 closestPoint = new Vector3(closestPointXZ.x, wayPoints[minDistIndex].y, closestPointXZ.y);
                                    shiftLaneTarget = closestPoint + (wayPoints[anotherIndex] - wayPoints[minDistIndex]).normalized * Random.Range(shiftLaneAdvanceDist.x, shiftLaneAdvanceDist.y);
                                }

                                if (forceShiftLaneDebugFlag)
                                {
                                    var tempGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                    tempGO.name = "Cube_Debug";
                                    var col = tempGO.GetComponent<Collider>();
                                    if (col != null)
                                    { Destroy(col); }
                                    tempGO.transform.position = shiftLaneTarget;
                                    Destroy(tempGO, 10f);
                                }

                                target = shiftLaneTarget;

                                leftTurn = Vector3.Cross(nose.forward, shiftLaneTarget - nose.position).y < 0;
                                rightTurn = !leftTurn;

                                if (triggerShiftToPlayer)
                                {
                                    triggerShiftToPlayer = false;
                                    //accident happens
                                }
                            }
                            else
                            {
                                if (forceShiftLaneDebugFlag)
                                {
                                    Debug.Log("no edges on perspective lane " + currentEntry.identifier + "_" + currentEntry.subIdentifier + " Do not shift lane");
                                }                                
                            }
                        }                        
                    }
                }                
            }

            //Abort shifting logic
            if(shiftingLane && Time.time - shiftStartTimestamp > shiftingTimeThreshold)
            {
                for (int i = currentIndex; i < currentEntry.waypoints.Count; i++)
                {
                    if (Vector3.Dot(nose.forward, currentEntry.waypoints[i] - nose.position) > 0)
                    {
                        shiftingLane = false;
                        shiftLaneEntry = null;
                        target = currentEntry.waypoints[i];
                        currentIndex = i;
                        break;
                    }
                }
            }

            //When car reached shifted target
            var distToShiftTarget = Vector3.Distance(new Vector3(nose.position.x, 0f, nose.position.z), new Vector3(shiftLaneTarget.x, 0f, shiftLaneTarget.z));
            if (shiftingLane && distToShiftTarget < shiftLaneTargetThreshold && Vector3.Dot(nose.forward, (shiftLaneTarget - nose.position).normalized) < 0)
            {
                shiftingLane = false;
                lastIndex = currentIndex;
                lastEntry = currentEntry;
                currentEntry = shiftLaneEntry;

                var wayPoints = shiftLaneEntry.GetPoints();
                float minDist = 10000f;
                int minDistIndex = wayPoints.Length - 1;
                for (int i = 0; i < wayPoints.Length; i++)
                {
                    if (Vector3.Dot(nose.forward, wayPoints[i] - nose.position) > 0)
                    {
                        var dist = Vector3.Distance(nose.position, wayPoints[i]);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            minDistIndex = i;
                        }
                    }
                }

                currentIndex = minDistIndex;
                target = wayPoints[currentIndex];

                if (forceShiftLaneDebugFlag)
                {
                    var tempGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    tempGO.name = "Sphere_Debug";
                    var col = tempGO.GetComponent<Collider>();
                    if (col != null)
                    { Destroy(col); }
                    tempGO.transform.position = target;
                    Destroy(tempGO, 10f);
                }
            }

            //if the last waypoint in this entry is in range, grab the next path
            if (!shiftingLane && distToEnd <= giveWayRegisterDistance)
            {
                var node = system.roadGraph.GetNode(currentEntry.identifier, currentEntry.subIdentifier);
 
                RoadGraphEdge newNode;

                if (fixedRoute)
                {
                    if(++currentFixedNode >= fixedPath.Count)
                        currentFixedNode = 0;
                    newNode = system.FindJoiningIntersection(node, fixedPath[currentFixedNode]);
                } else
                {
                    newNode = node.SelectRandom();
                }

                if(newNode == null)
                {
                    Debug.Log("no edges on " + currentEntry.identifier + "_" + currentEntry.subIdentifier);
                    Destroy(gameObject);
                    inited = false;
                    return;
                }

                nextEntry = system.GetEntry(newNode.id, newNode.subId);
                nextTarget = nextEntry.waypoints[0];
                hasNextEntry = true;

                nextEntry.RegisterInterest(this); //
                registeredEntry = nextEntry;

                //see if we need to slow down for this intersection
                float angle = Vector3.Angle(nextEntry.path.start.transform.forward, nextEntry.path.end.transform.forward);

                if (angle > 60)
                {
                    Vector3 cross = Vector3.Cross(nextEntry.path.start.transform.forward, nextEntry.path.end.transform.forward);
                    leftTurn = cross.y < 0;
                    rightTurn = cross.y > 0;
                }
                intersectionCornerSpeed = Mathf.Clamp(1 - angle / 90f, 0.4f, 1f);
            }
        }

        var distToTarget = Vector3.Distance(new Vector3(nose.position.x, 0f, nose.position.z), new Vector3(target.x, 0f, target.z));
        //check if we have reached the target waypoint
        if (!shiftingLane && (distToTarget < waypointThreshold || ((distToTarget < 8.0f) && Vector3.Dot(nose.forward, target - nose.position) < 0)))// && !hasStopTarget && !hasGiveWayTarget)
        {
            if(++currentIndex >= currentEntry.waypoints.Count)
            {
                --currentIndex;
                if (currentEntry.isIntersection())
                {
                    currentEntry.DeregisterInterest();
                    registeredEntry = null;

                    var node = system.roadGraph.GetNode(currentEntry.identifier, currentEntry.subIdentifier);
                    var newNode = node.SelectRandom();

                    if(newNode == null)
                    {
                        Debug.Log("no edges on " + currentEntry.identifier + "_" + currentEntry.subIdentifier);
                        Destroy(gameObject);
                        inited = false;
                        return;
                    }

                    lastIndex = currentIndex;
                    lastEntry = currentEntry;
                    currentEntry = system.GetEntry(newNode.id, newNode.subId);

                    nextEntry = null;
                    hasNextEntry = false;

                    targetTangent = (currentEntry.waypoints[1] - currentEntry.waypoints[0]).normalized;

                }
                else
                {
                    if(hasStopTarget || hasGiveWayTarget)
                    {
                        target = nextEntry.waypoints[0];
                    }
                    else // When reach to the end of a non-intersect entry
                    {
                        lastIndex = currentIndex;
                        lastEntry = currentEntry;
                        currentEntry = nextEntry;

                        nextEntry = null;
                        hasNextEntry = false;
                        targetTangent = Vector3.zero;
                    }
                }

                if (!hasStopTarget && !hasGiveWayTarget)
                {
                    currentIndex = 0;
                }


            }
            if(currentIndex > 1)
            {
                targetTangent = Vector3.zero;
            }

            if(!hasStopTarget && !hasGiveWayTarget)
                target = currentEntry.waypoints[currentIndex];

            unreachTime = 0f;
        }
        else
        {
            unreachTime += deltaTime;
        }

        SteerCar();        

        if(!shiftingLane && hasNextEntry && nextEntry.isIntersection() && nextEntry.intersection.stopSign) 
        {
            if(stopEnd == 0f) {
                hasStopTarget = true;
                stopTarget = nextTarget;
                stopEnd = Time.time + stopLength;
            } else if(Time.time > stopEnd) {
                //if (nextEntry.intersection.stopQueue.Count == 0)                
                //    Debug.Log("####### Expected Issue Here #######");                
                if(nextEntry.intersection.stopQueue.Count > 0 && nextEntry.intersection.stopQueue.Peek() == this) {
                    hasGiveWayTarget = false;
                    hasStopTarget = false;
                    stopEnd = 0f;
                } 
            }
        }


        if(!shiftingLane && hasNextEntry && nextEntry.isIntersection() && !nextEntry.intersection.stopSign)
        {
            //check next entry for stop needed
            if(nextEntry.MustGiveWay())
            {
                hasGiveWayTarget = true;
                stopTarget = target;
            }
            else
            {
                hasGiveWayTarget = false;
            }
        }

        if (DEBUG)
        {
            Debug.Log("nextEntry: " + nextEntry.identifier + " " + nextEntry.subIdentifier);
            Debug.Log("nextEntry.light.State: " + nextEntry.light.State);
        }

        if(!shiftingLane && !hasGiveWayTarget && hasNextEntry && nextEntry.light != null)
        {
            if(!hasStopTarget && nextEntry.light.State == TrafLightState.RED)
            {
                //light is red, stop here
                hasStopTarget = true;
                stopTarget = nextTarget;

            }
            else if(hasStopTarget && nextEntry.light.State == TrafLightState.GREEN)
            {

                //green light, go!                   
                hasStopTarget = false;
                return;
            }
            else if(!hasStopTarget && nextEntry.light.State == TrafLightState.YELLOW)
            {
                //yellow, stop if we aren't zooming on through
                //TODO: carry on if we are too fast/close

                if(Vector3.Distance(nextTarget, nose.position) > yellowLightGoDistance * (maxSpeedRange.y / 11f))
                {
                    hasStopTarget = true;
                    stopTarget = nextTarget;
                }
            }
        }

        targetSpeed = maxSpeed;

        doRaycast = false;
        //check in front of us
        if (Time.time > nextRaycast)
        {
            doRaycast = true;
            nextRaycast = NextRaycastTime();
        }

        if (doRaycast)
        {
            frontClosestHitInfo = new RaycastHit();
            //All hit info are for the closest
            somethingInFront = CheckFrontBlocked(out frontClosestHitInfo, out dodgeCode);
        }

        if (somethingInFront && frontClosestHitInfo.collider != null)
        {
            float frontSpeed = targetSpeed;
            var frontCar = frontClosestHitInfo.collider.GetComponentInParent<TrafAIMotor>();
            var playercar = frontClosestHitInfo.collider.GetComponentInParent<VehicleController>();
            if (frontCar)
            {
                frontSpeed = frontCar.currentSpeed * Vector3.Dot(nose.forward, frontCar.nose.forward);
                if (frontSpeed < 0.2f)
                {
                    frontSpeed = 0f;
                    blockedByTraffic = !(hasStopTarget || hasGiveWayTarget);
                }
            }
            else if (playercar)
            {
                frontSpeed = playercar.CurrentSpeed * Vector3.Dot(nose.forward, playercar.transform.forward);
                if (frontSpeed < 0.2f)
                {
                    frontSpeed = 0f;
                    blockedByTraffic = !(hasStopTarget || hasGiveWayTarget);
                }
            }
            else
            {                
                frontSpeed = 0f;
            }

            float calculatedIdealSpeed = targetSpeed;

            var hitDist = frontClosestHitInfo.distance;
            if (hitDist < 0.6f)
            {
                calculatedIdealSpeed = frontSpeed < 1f ? 0 : frontSpeed * 0.5f; //prevent car slowly approaching issue
            }
            else
            {
                if (currentSpeed >= frontSpeed)
                {
                    var safeDistance = (currentSpeed/* - frontSpeed*/) * 2.0f * 1.35f; // 2 seconds reaction time. multiply with a factor since the speed is not true speed
                    if (safeDistance < 2.0f)
                    { safeDistance = 2.0f; }

                    if (hitDist < safeDistance)
                    {
                        if (hitDist < 1.5f)
                        { hitDist = 1.5f; }

                        calculatedIdealSpeed = Remap(hitDist, 1.5f, safeDistance, frontSpeed * 0.95f, currentSpeed);                        
                    }
                }
            }

            targetSpeed = Mathf.Min(targetSpeed, calculatedIdealSpeed); //Affect target speed

            //Check speed difference along with dodge code to determin if need to dodge for safety
            var speedDif = currentSpeed - frontSpeed;
            if (dodgeCode != 0 && speedDif > 0)
            {
                Transform noseSide = dodgeCode == -1 ? noseRight : noseLeft;

                //Determine here again if the car can dodge
                bool canDodge = false;                
                if (currentEntry.isIntersection() || Vector3.Distance(nose.position, currentEntry.waypoints[0]) < 6.0f) //If it is in intersection or just about to get out of intersection
                {
                    var crossY = Vector3.Cross(nose.forward, currentEntry.waypoints[currentEntry.waypoints.Count - 1] - nose.position).y;
                    if ((crossY > 0 && dodgeCode == 1) || (crossY < 0 && dodgeCode == -1))
                    {
                        canDodge = true;
                    }
                }
                else
                {
                    if (frontClosestHitInfo.collider.GetComponent<CarAIController>() || frontClosestHitInfo.collider.GetComponentInParent<VehicleController>()) // If it is a NPC car
                    {
                        canDodge = true;
                    }
                    else //If it is not moving car but regular street obstacles
                    {
                        if (Vector3.Dot(nose.forward, (target - nose.position).normalized) > 0.766f) //0.766 is cos 40 degree, if angle < 40 degree
                        {
                            canDodge = true;
                        }
                    }
                }
                

                if (canDodge)
                {
                    float dist = Vector3.Distance(frontClosestHitInfo.point, noseSide.position);
                    float magnitude = 1.75f - (dist / speedDif);
                    if (magnitude > 0)
                    {
                        magnitude = Mathf.Sqrt(magnitude * (1f / 1.75f)) * 60f;
                        dodgeVector += (magnitude * (dodgeCode == -1 ? -1f : 1f) * nose.right);
                    }
                }
            }
        }
        else
        { blockedByTraffic = false; }

        if (doRaycast) //front side detect
        {
            var capsulePointL = noseLeft.position + frontSideRaycastDistance * 0.5f * (nose.forward - nose.right).normalized;
            var capsulePointR = noseRight.position + frontSideRaycastDistance * 0.5f * (nose.forward + nose.right).normalized;
            var cols = Physics.OverlapCapsule(capsulePointL, capsulePointR, frontSideRaycastDistance, ~(1 << LayerMask.NameToLayer("Ground And Road") | 1 << LayerMask.NameToLayer("Concave Environment Prop")));

            float minReachTime = 1000f;
            Vector3 pickedClosingVel = Vector3.zero;
            frontSideColliding = false;
            dodgeVector = Vector3.zero;

            foreach (var col in cols)
            {
                if (col == GetComponent<Collider>()/* || col == frontHitInfo.collider*/)                
                    continue;

                var otherCar = col.GetComponent<TrafAIMotor>();
                if (otherCar == null)
                {
                    //in terms of static environment obj we need to use second order algorithm, so right now since we are using partially fake physics we ignore it for now.
                    continue;
                }

                var testClosestVec = GetClosestPointOnCollider(col, nose.position) - nose.position;
                if (Vector3.Dot(nose.forward, testClosestVec) > 0)
                {
                    Transform effectiveNoseSide = Vector3.Cross(nose.forward, testClosestVec).y > 0 ? noseRight : noseLeft;

                    //if other car is approach to rear side then don't brake, other car should brake
                    if (Vector3.Cross(otherCar.nose.forward, (effectiveNoseSide.position - otherCar.nose.position).normalized).y < 0)
                        continue;

                    Vector3 closestVec = GetClosestPointOnCollider(col, effectiveNoseSide.position) - effectiveNoseSide.position;
                    Vector3 relativeVel = currentSpeed * nose.forward - otherCar.currentSpeed * otherCar.nose.forward; /*otherRB.GetPointVelocity(closestPoint);*/

                    var closingVel = Vector3.Project(relativeVel, closestVec.normalized); //calculate relative approaching velocity is important          
                    if (closingVel.magnitude != 0)
                    {
                        var reachTime = (closestVec.magnitude - 0.5f) / closingVel.magnitude;
                        reachTime = Mathf.Max(reachTime, 0);
                        if (reachTime < 1.0f)
                        {
                            frontSideColliding = true;
                            if (reachTime < minReachTime)
                            {
                                minReachTime = reachTime;
                                pickedClosingVel = closingVel;
                            }
                        }
                    }
                }
            }

            if (frontSideColliding)
            {
                //var fwdAxisRelVel = Vector3.Project(pickedClosingVel, nose.forward);
                var interpSpeedFromFrontSide = Remap(minReachTime, 0, 1.0f, 0, currentSpeed); 
                dodgeVector += (nose.forward - pickedClosingVel.normalized).normalized * (1f - (minReachTime / 1f));
                //Lerp with the current speed based on the contribution to brake direction. side dir avoid force is not applied yet
                //interpSpeedFromFrontSide = Mathf.Lerp(currentSpeed, interpSpeedFromFrontSide, fwdAxisRelVel.magnitude / pickedClosingVel.magnitude);
                targetSpeed = Mathf.Min(targetSpeed, interpSpeedFromFrontSide); //Affect target speed
            }
        }

        if (hasStopTarget || hasGiveWayTarget)
        {            
            Vector3 targetVec = (stopTarget - nose.position);

            float stopSpeed = Mathf.Clamp(targetVec.magnitude * (Vector3.Dot(targetVec, nose.forward) > 0 ? 1f : 0f) / 3f, 0f, maxSpeed);
            if(stopSpeed < 0.24f)
                stopSpeed = 0f;

            targetSpeed = Mathf.Min(targetSpeed, stopSpeed);
        }

        //slow down if we need to turn
        if(currentEntry.isIntersection() || hasNextEntry)
        {
            if (Vector3.Distance(nose.position, currentEntry.waypoints[currentEntry.waypoints.Count - 1]) < turnSlowDownDist)
            {
                targetSpeed = targetSpeed * intersectionCornerSpeed;
            }
        }
        else
        {
            if (currentEntry.isIntersection())
            {
                targetSpeed = targetSpeed * Mathf.Clamp(1 - (currentTurn / maxTurn), 0.1f, 1f);
            }
            else
            {
                targetSpeed = targetSpeed * Mathf.Clamp(1 - (currentTurn / maxTurn), 0.25f, 1f);
            }
        }

        if (emergencyHardBrake)
        {
            targetSpeed = 0.0f;
        }

        if (targetSpeed > currentSpeed)
        {
            currentSpeed += Mathf.Min(maxAccell * deltaTime, targetSpeed - currentSpeed);
            if (brakeHard) { brakeHard = false; }
        }
        else
        {
            if (currentSpeed - targetSpeed > brakeHardRelativeSpeed)
            {
                if (!brakeHard) { brakeHard = true; }
            }
            else if (brakeHard) { brakeHard = false; }

            currentSpeed -= Mathf.Min((emergencyHardBrake ? emergencyMaxBrake : maxBrake) * deltaTime, currentSpeed - targetSpeed);
            if (currentSpeed < 0)
                currentSpeed = 0;
        }
    }

    public IEnumerator HoldEmergencyHardBrakeState()
    {
        if (emergencyHardBrake)       
            yield break;
        
        emergencyHardBrake = true;
        yield return new WaitForSeconds(Random.Range(3f, 10f));
        emergencyHardBrake = false;
    }

    void SteerCar()
    {
        float targetDist = Vector3.Distance(target, transform.position);
        //head towards target
        Vector3 newTarget = target;
        if(targetTangent != Vector3.zero && targetDist > 6f)
        {
            newTarget = target - (targetTangent * (targetDist - 6f));
        } 
        Vector3 steerVector = (new Vector3(newTarget.x, transform.position.y, newTarget.z) - transform.position).normalized + dodgeVector * Mathf.Clamp((currentSpeed / 4f), 0f, 1f);
        float steer = Vector3.Angle(transform.forward, steerVector) * 1.5f;
        //if(steer > 140f)
        //{
        //    steer = currentTurn;
        //}
        currentTurn = Mathf.Clamp((Vector3.Cross(transform.forward, steerVector).y < 0 ? -steer : steer), -maxTurn, maxTurn);
    }

    void MoveCar()
    {
        //transform.Rotate(0f, currentTurn * Time.deltaTime, 0f);
        GetComponent<Rigidbody>().MoveRotation(Quaternion.FromToRotation(Vector3.up, heightHit.normal) * Quaternion.Euler(0f, transform.eulerAngles.y + currentTurn * lowResPhysicsDeltaTime, 0f));
        GetComponent<Rigidbody>().MovePosition(rb.position + transform.forward * currentSpeed * lowResPhysicsDeltaTime);
        //transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
    }

    public void EngineBreakDown()
    {
        ////Debug
        //{
        //    var go = new GameObject("Engine Breakdown!!!");
        //    go.transform.position = transform.position;
        //    go.AddComponent<DebugTracker>().sourceObj = gameObject;
        //    Destroy(go, 10f); //Debug object will be cleared automatically
        //}

        //Make some fire or smoke to indicate that the car is in trouble, smoke gameobject will eventually be destroyed
        activeSmoke = GameObject.Instantiate<GameObject>(smokePrefab, transform);
        activeSmoke.transform.position = engine.position;
        GameObject.Destroy(activeSmoke, 250f);
    }

    public void GenerateEngineDamageSmoke()
    {
        ////Debug
        //{
        //    var go = new GameObject("Engine Crash and Damage!!!");
        //    go.transform.position = transform.position;
        //    go.AddComponent<DebugTracker>().sourceObj = gameObject;
        //    Destroy(go, 10f); //Debug object will be cleared automatically
        //}

        //Make some fire or smoke to indicate that the car is in trouble, smoke gameobject will eventually be destroyed
        activeSmoke = GameObject.Instantiate<GameObject>(smokePrefab, transform);
        activeSmoke.transform.position = engine.position;
        GameObject.Destroy(activeSmoke, 250f);
    }
}