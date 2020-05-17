/**
 * Copyright (c) 2019-2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Simulator.Api;
using Simulator.Map;
using Simulator.Utilities;

public class NPCLaneFollowBehaviour : NPCBehaviourBase
{
    #region vars
    private bool DebugMode = false;
    private bool AutomaticMode = true;
    // physics
    public LayerMask groundHitBitmask;
    public LayerMask carCheckBlockBitmask;
    protected RaycastHit frontClosestHitInfo = new RaycastHit();
    protected RaycastHit leftClosestHitInfo = new RaycastHit();
    protected RaycastHit rightClosestHitInfo = new RaycastHit();
    protected RaycastHit groundCheckInfo = new RaycastHit();
    protected float frontRaycastDistance = 20f;
    protected bool atStopTarget;

    // map data
    public MapLane currentMapLane;
    public MapLane prevMapLane;
    public List<Vector3> laneData;

    // targeting
    protected Vector3 currentTarget;
    protected int currentIndex = 0;
    protected float distanceToCurrentTarget = 0f;
    public float distanceToStopTarget = 0;
    protected Vector3 stopTarget = Vector3.zero;
    protected float minTargetDistance = 1f;

    //protected bool doRaycast; // TODO skip update for collision
    //protected float nextRaycast = 0f;
    protected float laneSpeedLimit = 0f;
    protected float normalSpeed = 0f;
    public float targetSpeed = 0f;
    public float targetTurn = 0f;
    public float currentTurn = 0f;
    public float speedAdjustRate = 4.0f;
    protected float minSpeedAdjustRate = 1f;
    protected float maxSpeedAdjustRate = 4f;
    protected float elapsedAccelerateTime = 0f;
    protected float turnAdjustRate = 10.0f;

    public float stopHitDistance = 5f;
    public float stopLineDistance = 15f;
    public float aggressionAdjustRate;
    public int aggression;

    protected bool isLaneDataSet = false;
    public bool isFrontDetectWithinStopDistance = false;
    public bool isRightDetectWithinStopDistance = false;
    public bool isLeftDetectWithinStopDistance = false;
    public bool isFrontLeftDetect = false;
    public bool isFrontRightDetect = false;
    public bool hasReachedStopSign = false;
    public bool isStopLight = false;
    public bool isStopSign = false;
    public float path = 0f;
    public bool isCurve = false;
    public bool laneChange = false;
    public bool isDodge = false;
    public bool isWaitingToDodge = false;

    protected float stopSignWaitTime = 1f;
    protected float currentStopTime = 0f;
    #endregion

    #region mono
    private void Awake()
    {
        groundHitBitmask = LayerMask.GetMask("Default");
        carCheckBlockBitmask = LayerMask.GetMask("Agent", "NPC", "Pedestrian");
    }

    public override void PhysicsUpdate()
    {
        if (isLaneDataSet)
        {
            ToggleBrakeLights();
            CollisionCheck();
            EvaluateTarget();
            GetIsTurn();
            if (AutomaticMode)
            {
                GetDodge();
            }
            SetTargetSpeed();
            SetTargetTurn();
            NPCTurn();
            NPCMove();
            if (AutomaticMode)
            {
                StopTimeDespawnCheck();
                EvaluateDistanceFromFocus();
            }
        }
    }
    #endregion

    #region init
    public override void Init(int seed)
    {
        aggression = 3 - (seed % 3);
        stopHitDistance = 12 / aggression;
        speedAdjustRate = 2 + 2 * aggression;
        maxSpeedAdjustRate = speedAdjustRate; // more aggressive NPCs will accelerate faster
        turnAdjustRate = 50 * aggression;
        ResetData();
    }

    public override void InitLaneData(MapLane lane)
    {
        ResetData();
        laneSpeedLimit = lane.speedLimit;
        if (laneSpeedLimit > 0)
        {
            aggressionAdjustRate = laneSpeedLimit / 11.176f; // give more space at faster speeds
            stopHitDistance = 12 / aggression * aggressionAdjustRate;
        }
        normalSpeed = RandomGenerator.NextFloat(laneSpeedLimit - 3 + aggression, laneSpeedLimit + 1 + aggression);
        currentMapLane = lane;
        SetLaneData(currentMapLane.mapWorldPositions);
        controller.SetLastPosRot(transform.position, transform.rotation);
        isLaneDataSet = true;
    }

    #endregion

    #region spawn
    protected void EvaluateDistanceFromFocus()
    {
        if (!SimulatorManager.Instance.NPCManager.WithinSpawnArea(transform.position) && !SimulatorManager.Instance.NPCManager.IsVisible(gameObject))
        {
            Despawn();
        }
    }

    protected void Despawn()
    {
        if (AutomaticMode)
        {
            ResetData();
            NPCManager.DespawnNPC(controller);
        }
    }

    protected void ResetData()
    {
        controller.StopNPCCoroutines();
        currentMapLane = null;
        laneSpeedLimit = 0f;
        foreach (var intersection in SimulatorManager.Instance.MapManager.intersections)
        {
            intersection.ExitStopSignQueue(controller);
            intersection.ExitIntersectionList(controller);
        }
        prevMapLane = null;
        controller.ResetLights();
        currentSpeed = 0f;
        currentStopTime = 0f;
        path = 0f;
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
        controller.SetLastPosRot(transform.position, transform.rotation);
    }
    #endregion

    #region physics
    public void NPCMove()
    {
        var movement = rb.position + transform.forward * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(new Vector3(movement.x, rb.position.y, movement.z));
    }

    protected void NPCTurn()
    {
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, currentTurn * Time.fixedDeltaTime, 0f));
    }
    #endregion

    #region inputs
    protected virtual void SetTargetTurn()
    {
        controller.steerVector = (currentTarget - controller.frontCenter.position).normalized;

        float steer = Vector3.Angle(controller.steerVector, controller.frontCenter.forward) * 1.5f;
        targetTurn = Vector3.Cross(controller.frontCenter.forward, controller.steerVector).y < 0 ? -steer : steer;
        currentTurn += turnAdjustRate * Time.fixedDeltaTime * (targetTurn - currentTurn);

        if (targetSpeed == 0)
            currentTurn = 0;
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
                tempD = 0f;
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
        prevMapLane.stopLine.intersection.EnterStopSignQueue(controller);
        hasReachedStopSign = true;
        yield return FixedUpdateManager.WaitForFixedSeconds(stopSignWaitTime);
        yield return FixedUpdateManager.WaitUntilFixed(() => prevMapLane.stopLine.intersection.CheckStopSignQueue(controller));
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
            yield return FixedUpdateManager.WaitForFixedSeconds(RandomGenerator.NextFloat(1f, 2f)); // wait to creep out on turn
        isStopLight = false;
    }

    protected void StopTimeDespawnCheck()
    {
        if (isStopLight || isStopSign || (controller.simpleVelocity.magnitude < 0.013f))
        {
            currentStopTime += Time.fixedDeltaTime;
        }
        if (currentStopTime > 60f)
        {
            Debug.Log($"NPC Despawn: Stopped for {currentStopTime} seconds");
            Despawn();
        }
    }

    protected bool IsYieldToIntersectionLane() // TODO stopping car
    {
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
                    if(laneFollow == null) continue;

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

    protected void SetChangeLaneData(List<Vector3> data)
    {
        laneData = new List<Vector3>(data);
        currentIndex = SimulatorManager.Instance.MapManager.GetLaneNextIndex(transform.position, currentMapLane);
        currentTarget = laneData[currentIndex];
        isDodge = false; // ???
    }

    public void OnDrawGizmos()
    {
        if (!DebugMode)
        {
            return;
        }

        if(!isLaneDataSet)
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
        distanceToCurrentTarget = Vector3.Distance(new Vector3(controller.frontCenter.position.x, 0f, controller.frontCenter.position.z), new Vector3(currentTarget.x, 0f, currentTarget.z));
        distanceToStopTarget = Vector3.Distance(new Vector3(controller.frontCenter.position.x, 0f, controller.frontCenter.position.z), new Vector3(stopTarget.x, 0f, stopTarget.z));

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

        // check if we are past the target or reached current target
        if (Vector3.Dot(controller.frontCenter.forward, (currentTarget - controller.frontCenter.position).normalized) < 0 || distanceToCurrentTarget < 1f)
        {
            if (currentIndex == laneData.Count - 2) // reached 2nd to last target index see if stop line is present
            {
                StartStoppingCoroutine();
            }

            if (currentIndex < laneData.Count - 1) // reached target dist and is not at last index of lane data
            {
                currentIndex++;
                currentTarget = laneData[currentIndex];
                controller.Coroutines.Add(FixedUpdateManager.StartCoroutine(DelayChangeLane()));
            }
            else
            {
                GetNextLane();
            }
        }
    }

    protected void GetNextLane()
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

    protected IEnumerator DelayChangeLane()
    {
        if (currentMapLane == null) yield break;
        if (!currentMapLane.isTrafficLane) yield break;
        if (RandomGenerator.Next(100) < 98) yield break;
        if (!laneChange) yield break;

        if (currentMapLane.leftLaneForward != null)
        {
            isLeftTurn = true;
            isRightTurn = false;
            controller.SetNPCTurnSignal();
        }
        else if (currentMapLane.rightLaneForward != null)
        {
            isRightTurn = true;
            isLeftTurn = false;
            controller.SetNPCTurnSignal();
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

        ApiManager.Instance?.AddLaneChange(gameObject);

        if (currentMapLane.leftLaneForward != null)
        {
            if (!isFrontLeftDetect)
            {
                currentMapLane = currentMapLane.leftLaneForward;
                laneSpeedLimit = currentMapLane.speedLimit;
                aggressionAdjustRate = laneSpeedLimit / 11.176f; // 11.176 m/s corresponds to 25 mph
                SetChangeLaneData(currentMapLane.mapWorldPositions);
                controller.Coroutines.Add(FixedUpdateManager.StartCoroutine(DelayOffTurnSignals()));
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
                controller.Coroutines.Add(FixedUpdateManager.StartCoroutine(DelayOffTurnSignals()));
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
                    controller.Coroutines.Add(FixedUpdateManager.StartCoroutine(DelayOffTurnSignals()));
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
                    controller.Coroutines.Add(FixedUpdateManager.StartCoroutine(DelayOffTurnSignals()));
                    ApiManager.Instance?.AddLaneChange(gameObject);
                }
            }
        }
    }

    protected void GetDodge()
    {
        if (currentMapLane == null) return;
        if (isDodge) return;
        if (IsYieldToIntersectionLane()) return;

        if (isLeftDetectWithinStopDistance || isRightDetectWithinStopDistance)
        {
            var npcC = isLeftDetectWithinStopDistance ? leftClosestHitInfo.collider.GetComponentInParent<NPCLaneFollowBehaviour>() : rightClosestHitInfo.collider.GetComponentInParent<NPCLaneFollowBehaviour>();
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
                        controller.Coroutines.Add(FixedUpdateManager.StartCoroutine(WaitToDodge(aC, isLeftDetectWithinStopDistance)));
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

    protected void SetDodge(bool isLeft, bool isShortDodge = false)
    {
        if (isStopSign || isStopLight) return;

        Transform startTransform = isLeft ? controller.frontLeft : controller.frontRight;
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
        controller.SetNPCTurnSignal();
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
        controller.SetNPCTurnSignal();
    }

    protected void GetIsTurn()
    {
        if (currentMapLane == null) return;
        path = transform.InverseTransformPoint(currentTarget).x;
        isCurve = path < -1f || path > 1f ? true : false;
    }
    #endregion

    #region lights
    protected void ToggleBrakeLights()
    {
        if (targetSpeed < 2f || isStopLight || isFrontDetectWithinStopDistance || (isStopSign && distanceToStopTarget < stopLineDistance))
            controller.SetBrakeLights(true);
        else
            controller.SetBrakeLights(false);
    }
    #endregion

    #region utility
    protected void CollisionCheck()
    {
        if (controller.frontCenter == null || controller.frontLeft == null || controller.frontRight == null) return;

        frontClosestHitInfo = new RaycastHit();
        rightClosestHitInfo = new RaycastHit();
        leftClosestHitInfo = new RaycastHit();

        Physics.Raycast(controller.frontCenter.position, controller.frontCenter.forward, out frontClosestHitInfo, frontRaycastDistance, carCheckBlockBitmask);
        Physics.Raycast(controller.frontRight.position, controller.frontRight.forward, out rightClosestHitInfo, frontRaycastDistance / 2, carCheckBlockBitmask);
        Physics.Raycast(controller.frontLeft.position, controller.frontLeft.forward, out leftClosestHitInfo, frontRaycastDistance / 2, carCheckBlockBitmask);
        isFrontLeftDetect = Physics.CheckSphere(controller.frontLeft.position - (controller.frontLeft.right * 2), 1f, carCheckBlockBitmask);
        isFrontRightDetect = Physics.CheckSphere(controller.frontRight.position + (controller.frontRight.right * 2), 1f, carCheckBlockBitmask);

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
        {
            //Debug.Log($"NPC Despawn: ground raycast failed");
            Despawn();
        }

        //if (frontClosestHitInfo.collider != null)
        //    Debug.DrawLine(frontCenter.position, frontClosestHitInfo.point, Color.blue, 0.25f);
        //if (leftClosestHitInfo.collider != null)
        //    Debug.DrawLine(frontLeft.position, leftClosestHitInfo.point, Color.yellow, 0.25f);
        //if (rightClosestHitInfo.collider != null)
        //    Debug.DrawLine(frontRight.position, rightClosestHitInfo.point, Color.red, 0.25f);
    }

    protected float SetFrontDetectSpeed()
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
        controller.currentIntersection = lane.stopLine?.intersection;

        distanceToCurrentTarget = Vector3.Distance(new Vector3(controller.frontCenter.position.x, 0f, controller.frontCenter.position.z), new Vector3(currentTarget.x, 0f, currentTarget.z));
        distanceToStopTarget = Vector3.Distance(new Vector3(controller.frontCenter.position.x, 0f, controller.frontCenter.position.z), new Vector3(stopTarget.x, 0f, stopTarget.z));

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
            controller.currentIntersection = currentMapLane.stopLine?.intersection;
            stopTarget = currentMapLane.mapWorldPositions[currentMapLane.mapWorldPositions.Count - 1];
            prevMapLane = currentMapLane;
            if (prevMapLane.stopLine.intersection != null) // null if map not setup right TODO add check to report missing stopline
            {
                if (prevMapLane.stopLine.isStopSign) // stop sign
                {
                    controller.Coroutines.Add(FixedUpdateManager.StartCoroutine(WaitStopSign()));
                }
                else
                {
                    controller.Coroutines.Add(FixedUpdateManager.StartCoroutine(WaitTrafficLight()));
                }
            }
        }
    }

    void OnDisable()
    {
        ResetData();
    }

    public override void OnAgentCollision(GameObject go)
    {
        isForcedStop = true;
        controller.SetNPCHazards(true);
    }
}
