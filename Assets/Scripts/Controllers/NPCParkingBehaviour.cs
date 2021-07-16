/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Simulator.Map;
using Simulator.Utilities;

public class NPCParkingBehaviour : NPCLaneFollowBehaviour
{
    private const float maximumLengthOfCarThatCanBeParked = 6;
    private const float minimumAmountOfTimeCarMustBeParked = 30;

    public enum State
    {
        LaneFollowing,
        LookingForparking,
        IsParking,
        IsParked,
        IsLeaving
    }

    public State CurrentState
    {
        get { return _currentState; }
        private set
        {
            if (_currentState != value)
            {
                //on change
            }
            _currentState = value;
        }
    }

    public MapParkingSpace CurrentSpace { get; private set; }
    public bool AbleToPark { get; private set; }

    private List<Vector3> parkingPath;
    private List<Vector3> way;
    private Vector3 middleEnter;
    private Vector3 middleMiddle;
    private Vector3 middleExit;
    private Transform backTransform;

    private bool reverse;
    private float timeWhenParked;
    private State _currentState;

    private Vector3 front
    {
        get
        {
            var bounds = controller.Bounds;
            return transform.TransformPoint(new Vector3(bounds.center.x, bounds.min.y,
                bounds.center.z + bounds.max.z));
        }
    }

    private Vector3 back
    {
        get
        {
            var bounds = controller.Bounds;
            return transform.TransformPoint(new Vector3(bounds.center.x, bounds.min.y,
                bounds.center.z - bounds.max.z));
        }
    }


    public override void Init(int seed)
    {
        base.Init(seed);
        AbleToPark = (back - front).magnitude < maximumLengthOfCarThatCanBeParked;
        CurrentState = State.LaneFollowing;

        GameObject go = new GameObject("Back");
        var bounds = controller.Bounds;
        go.transform.position = new Vector3(bounds.center.x, bounds.min.y + 0.5f, bounds.center.z + bounds.min.z);
        go.transform.SetParent(transform, true);
        backTransform = go.transform;
    }

    public override void PhysicsUpdate()
    {
        if (CurrentState != State.IsParked)
            base.PhysicsUpdate();
    }

    void OnEnable()
    {
        if (CurrentState != State.IsLeaving)
            StartLookingForParking();
    }

    void StartLookingForParking()
    {
        CurrentState = State.LookingForparking;
        StartCoroutine(TryParkCorutine());
    }

    private IEnumerator TryParkCorutine()
    {
        yield return new WaitForSeconds(0.3f);
        while ((CurrentState == State.LookingForparking))
        {
            TryPark();
            yield return new WaitForSeconds(0.3f);
        }
    }

    void TryPark()
    {
        if (!AbleToPark)
            return;

        var space = SimulatorManager.Instance.MapManager.GetClosestParking(transform.position + transform.forward * 25);
        if (space != null && currentMapLane != null
            && (space.mapWorldPositions[0] - transform.position).magnitude < 30
            && Vector3.Dot(space.mapWorldPositions[0] - transform.position, transform.forward) > 0.5f //is in front
            && Vector3.Dot(space.mapWorldPositions[2] - space.mapWorldPositions[1], transform.forward) >
            -0.1f //is in the same dir or perpendicular
            && DistanceTo(space.mapWorldPositions[0], currentMapLane) < 10
            && ParkingManager.instance.TryTake(space))
        {
            InitParking(space);
        }
    }

    float DistanceTo(Vector3 pos, MapTrafficLane lane)
    {
        return Vector3.Distance(ClosestPoint(pos, lane), pos);
    }

    Vector3 ClosestPoint(Vector3 pos, MapTrafficLane lane)
    {
        var iplus1 = SimulatorManager.Instance.MapManager.GetLaneNextIndex(pos, lane);
        return Utility.ClosetPointOnSegment(lane.mapWorldPositions[iplus1], lane.mapWorldPositions[iplus1 - 1], pos);
    }

    Vector3 NextPoint(Vector3 pos, MapTrafficLane lane, float dist)
    {
        var iplus1 = SimulatorManager.Instance.MapManager.GetLaneNextIndex(pos, lane);
        Vector3 inProperDist = lane.mapWorldPositions[iplus1];
        for (int i = iplus1; i < lane.mapWorldPositions.Count; i++)
        {
            if (Vector3.Distance(pos, inProperDist) < dist)
                break;

            inProperDist = lane.mapWorldPositions[iplus1];
        }
        return inProperDist;
    }

    public void InitParking(Simulator.Map.MapParkingSpace space)
    {
        CurrentSpace = space;
        parkingPath = new List<Vector3>();
        var first = transform.position + transform.forward * 4;
        parkingPath.Add(first);

        middleEnter = space.MiddleEnter;
        middleExit = space.MiddleExit;
        middleMiddle = space.Center;
        var parkingSpaceDirection = (middleExit - middleEnter).normalized;

        var closestOnLane = ClosestPoint(middleEnter, currentMapLane);
        var carDirection = (closestOnLane - transform.position).normalized;
        var dot = Vector3.Dot(middleEnter - closestOnLane, carDirection);
        closestOnLane += carDirection * dot; //proper point near parking space

        parkingPath.Add(closestOnLane - transform.forward);
        parkingPath.Add(middleEnter);
        parkingPath.Add((middleEnter + 2 * middleExit) / 3f);
        parkingPath.Add(middleExit + parkingSpaceDirection * 2);
        var path = new CatmullRom(parkingPath, 20, false);
        way = path.GetPoints().Select(s => s.position).ToList();
        SetLaneData(way);
        CurrentState = State.IsParking;
        turnAdjustRate = 150; //50-150 normally
        frontDetectRadius = 1.2f;
    }

    public void TryInitLeaving()
    {
        if (Time.time - timeWhenParked < minimumAmountOfTimeCarMustBeParked) return;

        //check if area is free
        var lane = SimulatorManager.Instance.MapManager.GetClosestLane(transform.position);
        foreach (var npc in NPCManager.CurrentPooledNPCs)
        {
            var laneFollow = npc.ActiveBehaviour as NPCLaneFollowBehaviour;
            if (laneFollow != null && npc.isActiveAndEnabled && lane == laneFollow.currentMapLane)
            {
                var parking = npc.ActiveBehaviour as NPCParkingBehaviour;
                if (parking == null || parking.CurrentState != State.IsParked)
                {
                    if ((npc.transform.position - transform.position).magnitude < 50)
                        return;
                }
            }

        }

        //make npc ready to drive
        CurrentState = State.IsLeaving;
        foreach (var light in controller.GetLights())
        {
            light.gameObject.SetActive(true);
        }

        ChangePhysic(true);

        //set current lane
        InitLaneData(lane);

        //check direction
        if (Vector3.Dot((CurrentSpace.MiddleExit - CurrentSpace.MiddleEnter).normalized,
                (currentMapLane.mapWorldPositions.Last() - currentMapLane.mapWorldPositions.First()).normalized) > 0.85f) //is parallel
        {
            parkingPath = new List<Vector3>();
            parkingPath.Add(middleMiddle);
            parkingPath.Add(ClosestPoint(middleExit, currentMapLane));
            parkingPath.Add(NextPoint(parkingPath.Last(), currentMapLane, 10));
            var path = new CatmullRom(parkingPath, 20, false);
            way = path.GetPoints().Select(s => s.position).ToList();
            SetLaneData(way);
        }
        else
        {
            controller.enabled = true;
            reverse = true;
            //way.Reverse();
            //SetLaneData(way);
        }
        (NPCManager as ParkingNPCManager).ChangeActiveCountBy(1);
    }


    //THIS IS OK
    public override void SetLaneData(List<Vector3> d)
    {
        if (CurrentState != State.IsParking)
        {
            base.SetLaneData(d);
        }
    }

    protected override void SetChangeLaneData(List<Vector3> d)
    {
        if (CurrentState != State.IsParking)
        {
            base.SetLaneData(d);
        }
    }
    //OR THIS
    //protected override void SetLaneChange()
    //{
    //    if (state == State.LaneFollowing || state == State.LookingForparking)
    //        base.SetLaneChange();
    //}

    //protected override void EvaluateTarget()
    //{
    //    if (state == State.LaneFollowing || state == State.LookingForparking)
    //        base.EvaluateTarget();
    //}

    protected override void SetTargetTurn()
    {
        if (CurrentState == State.IsParking)
        {
            controller.steerVector = (currentTarget - controller.frontCenter.position).normalized;
            float steer = Vector3.Angle(controller.steerVector, controller.frontCenter.forward) * 3.5f;
            targetTurn = Vector3.Cross(controller.frontCenter.forward, controller.steerVector).y < 0 ? -steer : steer;
            currentTurn = Mathf.Lerp(currentTurn, targetTurn, 0.5f);
            if (targetSpeed == 0)
            {
                currentTurn = 0;
            }
        }
        else if (CurrentState == State.IsLeaving)
        {
            if (reverse)
            {
                var laneDirection = (currentMapLane.mapWorldPositions.Last() -
                                     currentMapLane.mapWorldPositions.First());
                var laneRightDirection = Quaternion.AngleAxis(90, Vector3.up) * laneDirection.normalized;
                var projectionOnRight = -Vector3.Dot(laneRightDirection, transform.forward);
                var progress = Vector3.Dot((transform.position - middleEnter), (middleExit - middleEnter).normalized);
                if (progress > -0.5f)
                {
                    targetTurn = 10 * projectionOnRight;
                }
                else
                {
                    targetTurn = 30 * projectionOnRight;
                }
                if (projectionOnRight < 0.1)
                {
                    ReturnToDrive();
                }
                currentTurn = Mathf.Lerp(currentTurn, targetTurn, 0.5f);
            }
            if (targetSpeed == 0)
            {
                currentTurn = 0;
            }
        }
        else
        {
            base.SetTargetTurn();
        }
    }

    protected override void SetTargetSpeed()
    {
        base.SetTargetSpeed();
        bool isAvoiding = !(targetSpeed > 0f);

        if (CurrentState == State.IsParking && isAvoiding && frontClosestHitInfo.transform != null)
        {
            var other = frontClosestHitInfo.transform.GetComponent<NPCParkingBehaviour>();
            if (other != null && other.CurrentState == State.IsParked)
            {
                isAvoiding = false;
            }
        }
        else if (isFrontLeftDetect || isFrontRightDetect)
        {
            isAvoiding = false;
        }

        if (CurrentState == State.IsParking && !isAvoiding)
        {
            var dist = Vector3.Distance(middleMiddle, transform.position);
            if (dist > 0.5f &&
                Vector3.Dot(middleMiddle - transform.position, transform.forward) > 0f)
            {
                targetSpeed = Mathf.Clamp(dist, 3, 4);
                if (targetSpeed <= 0f)
                {
                    currentSpeed += 4 * speedAdjustRate * Time.fixedDeltaTime * (targetSpeed - currentSpeed);
                }
                else
                {
                    currentSpeed += speedAdjustRate * Time.fixedDeltaTime * (targetSpeed - currentSpeed);
                }
            }
            else
            {
                (NPCManager as ParkingNPCManager).ChangeActiveCountBy(-1);
                SwitchedToParked();
            }
        }

        if (CurrentState == State.IsParked)
        {
            targetSpeed = 0;
        }

        if (CurrentState == State.IsLeaving)
        {
            var closest = ClosestPoint(transform.position, currentMapLane);
            var closestFromparking= ClosestPoint(middleMiddle, currentMapLane);
            var distanceToLane = (closest - transform.position).magnitude;
            if (reverse)
            {
                currentSpeed = targetSpeed = -2;

                if (Vector3.Dot((transform.position - closest), (middleMiddle - closestFromparking).normalized) < 0)
                {
                    GoToLane();
                }

                var hits = Physics.OverlapSphereNonAlloc(backTransform.position, frontDetectRadius, MaxHitColliders,
                    1 << LayerMask.NameToLayer("NPC"));
                if (hits > 0)
                {
                    for (int i = 0; i < hits; i++)
                    {
                        var d = MaxHitColliders[i].GetComponentInParent<NPCParkingBehaviour>();
                        if (d == null || d!=this && d.CurrentState != State.IsParked)
                        {
                            GoToLane();
                        }
                    }
                }
            }
            else if (distanceToLane < 0.5f && Math.Abs(Vector3.Dot((currentTarget - transform.position).normalized, transform.forward)) > 0.95f ||
                     (way.Last() - transform.position).magnitude < 1f)
            {
                ReturnToDrive();
            }
        }
    }

    private void GoToLane()
    {
        reverse = false;
        parkingPath = new List<Vector3>();
        parkingPath.Add(transform.position);
        parkingPath.Add(ClosestPoint(controller.frontCenter.position, currentMapLane));
        parkingPath.Add(NextPoint(parkingPath.Last(), currentMapLane, 10));
        var path = new CatmullRom(parkingPath, 20, false);
        way = path.GetPoints().Select(s => s.position).ToList();
        SetLaneData(way);
    }

    private void ReturnToDrive()
    {
        reverse = false;
        CurrentState = State.LaneFollowing;
        InitLaneData(currentMapLane);
    }

    public void SwitchedToParked(bool turnOffPhisic = true, MapParkingSpace space = null)
    {
        if (space != null)
        {
            CurrentSpace = space;
        }

        CurrentState = State.IsParked;
        if (turnOffPhisic)
        {
            ChangePhysic(false);
        }

        controller.enabled = false;
        foreach (var light in controller.GetLights())
        {
            light.gameObject.SetActive(false);
        }

        targetSpeed = currentSpeed = 0;
        ResetData();
        timeWhenParked = Time.time;
    }

    public void ChangePhysic(bool on)
    {
        rb.isKinematic = !on;
        var wheels = GetComponentsInChildren<WheelCollider>();
        foreach (var wheel in wheels)
        {
            wheel.enabled = on;
        }
    }

    public override void Despawn()
    {
        if (CurrentState == State.IsParking)
        {
            return;
        }

        base.Despawn();
        ChangePhysic(true);
        if (CurrentSpace != null)
        {
            CurrentState = State.LaneFollowing;
            ParkingManager.instance.FreeUp(CurrentSpace);
            CurrentSpace = null;
        }

        if (CurrentState == State.IsParked)
        {
            foreach (var light in controller.GetLights())
            {
                light.gameObject.SetActive(true);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (way == null)
            return;

        for (int i = 1; i < way.Count; i++)
        {
            Gizmos.DrawLine(way[i], way[i - 1]);
            Gizmos.DrawSphere(way[i], 0.05f);
        }

        Gizmos.color = Color.green;
        for (int i = 1; i < parkingPath.Count; i++)
        {
            Gizmos.DrawLine(parkingPath[i], parkingPath[i - 1]);
            Gizmos.DrawSphere(parkingPath[i], 0.05f);
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(controller.frontCenter.position, controller.frontLeft.position);
        Gizmos.DrawLine(controller.frontCenter.position, controller.frontRight.position);
        Gizmos.DrawLine(front, back);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(currentTarget, 0.5f);
        if (currentMapLane != null)
        {
            for (int i = 1; i < currentMapLane.mapWorldPositions.Count; i++)
            {
                Gizmos.DrawLine(currentMapLane.mapWorldPositions[i], currentMapLane.mapWorldPositions[i - 1]);
            }
        }

        Gizmos.color = Color.blue;
        for (int i = 1; i < laneData.Count; i++)
        {
            Gizmos.DrawLine(laneData[i], laneData[i - 1]);
        }

        Gizmos.DrawSphere(controller.frontCenter.position, 1.25f);
    }
}

class ParkingManager
{
    public int AllSpaces => SimulatorManager.Instance.MapManager.parkingSpaces.Count;
    public int TookSpaces => spacesTook.Count;
    public float Fillrate => 1f * spacesTook.Count / AllSpaces;

    public static ParkingManager instance = new ParkingManager();
    private HashSet<MapParkingSpace> spacesTook = new HashSet<MapParkingSpace>();
    private Dictionary<string, MapParkingSpace> allSpaces;

    public bool IsFree(MapParkingSpace space)
    {
        return !spacesTook.Contains(space);
    }

    public bool TryTake(MapParkingSpace space)
    {
        bool took = spacesTook.Contains(space);
        if (took)
        {
            return false;
        }

        spacesTook.Add(space);
        return true;
    }

    public void FreeUp(MapParkingSpace space)
    {
        spacesTook.Remove(space);
    }
}

#region CatmullRom
public class CatmullRom
{
    // TODO this already exists but may be missing functionality.  Why not a single static class?
    //Struct to keep position, normal and tangent of a spline point
    [System.Serializable]
    public struct CatmullRomPoint
    {
        public Vector3 position;
        public Vector3 tangent;
        public Vector3 normal;

        public CatmullRomPoint(Vector3 position, Vector3 tangent, Vector3 normal)
        {
            this.position = position;
            this.tangent = tangent;
            this.normal = normal;
        }
    }

    private int resolution; //Amount of points between control points. [Tesselation factor]
    private bool closedLoop;

    private CatmullRomPoint[] splinePoints; //Generated spline points

    private Vector3[] controlPoints;

    //Returns spline points. Count is contorolPoints * resolution + [resolution] points if closed loop.
    public CatmullRomPoint[] GetPoints()
    {
        if (splinePoints == null)
        {
            throw new System.NullReferenceException("Spline not Initialized!");
        }

        return splinePoints;
    }

    public CatmullRom(Transform[] controlPoints, int resolution, bool closedLoop)
    {
        if (controlPoints == null || controlPoints.Length <= 2 || resolution < 2)
        {
            throw new ArgumentException("Catmull Rom Error: Too few control points or resolution too small");
        }

        this.controlPoints = new Vector3[controlPoints.Length];
        for (int i = 0; i < controlPoints.Length; i++)
        {
            this.controlPoints[i] = controlPoints[i].position;
        }

        this.resolution = resolution;
        this.closedLoop = closedLoop;

        GenerateSplinePoints();
    }

    public CatmullRom(IList<Vector3> controlPoints, int resolution, bool closedLoop)
    {
        if (controlPoints == null || controlPoints.Count <= 2 || resolution < 2)
        {
            throw new ArgumentException("Catmull Rom Error: Too few control points or resolution too small: " +
                                        controlPoints.Count);
        }

        this.controlPoints = new Vector3[controlPoints.Count];
        for (int i = 0; i < controlPoints.Count; i++)
        {
            this.controlPoints[i] = controlPoints[i];
        }
        this.resolution = resolution;
        this.closedLoop = closedLoop;

        GenerateSplinePoints();
    }

    //Updates control points
    public void Update(Transform[] controlPoints)
    {
        if (controlPoints.Length <= 0 || controlPoints == null)
        {
            throw new ArgumentException("Invalid control points");
        }

        this.controlPoints = new Vector3[controlPoints.Length];
        for (int i = 0; i < controlPoints.Length; i++)
        {
            this.controlPoints[i] = controlPoints[i].position;
        }

        GenerateSplinePoints();
    }

    //Updates resolution and closed loop values
    public void Update(int resolution, bool closedLoop)
    {
        if (resolution < 2)
        {
            throw new ArgumentException("Invalid Resolution. Make sure it's >= 1");
        }
        this.resolution = resolution;
        this.closedLoop = closedLoop;

        GenerateSplinePoints();
    }

    //Draws a line between every point and the next.
    public void DrawSpline(Color color)
    {
        if (ValidatePoints())
        {
            for (int i = 0; i < splinePoints.Length; i++)
            {
                if (i == splinePoints.Length - 1 && closedLoop)
                {
                    Debug.DrawLine(splinePoints[i].position, splinePoints[0].position, color);
                }
                else if (i < splinePoints.Length - 1)
                {
                    Debug.DrawLine(splinePoints[i].position, splinePoints[i + 1].position, color);
                }
            }
        }
    }

    public void DrawNormals(float extrusion, Color color)
    {
        if (ValidatePoints())
        {
            for (int i = 0; i < splinePoints.Length; i++)
            {
                Debug.DrawLine(splinePoints[i].position,
                    splinePoints[i].position + splinePoints[i].normal * extrusion, color);
            }
        }
    }

    public void DrawTangents(float extrusion, Color color)
    {
        if (ValidatePoints())
        {
            for (int i = 0; i < splinePoints.Length; i++)
            {
                Debug.DrawLine(splinePoints[i].position,
                    splinePoints[i].position + splinePoints[i].tangent * extrusion, color);
            }
        }
    }

    //Validates if splinePoints have been set already. Throws nullref exception.
    private bool ValidatePoints()
    {
        if (splinePoints == null)
        {
            throw new NullReferenceException("Spline not initialized!");
        }
        return splinePoints != null;
    }

    //Sets the length of the point array based on resolution/closed loop.
    private void InitializeProperties()
    {
        int pointsToCreate;
        if (closedLoop)
        {
            pointsToCreate =
                resolution *
                controlPoints.Length; //Loops back to the beggining, so no need to adjust for arrays starting at 0
        }
        else
        {
            pointsToCreate = resolution * (controlPoints.Length - 1);
        }

        splinePoints = new CatmullRomPoint[pointsToCreate];
    }

    //Math stuff to generate the spline points
    private void GenerateSplinePoints()
    {
        InitializeProperties();

        Vector3 p0, p1; //Start point, end point
        Vector3 m0, m1; //Tangents

        // First for loop goes through each individual control point and connects it to the next, so 0-1, 1-2, 2-3 and so on
        int closedAdjustment = closedLoop ? 0 : 1;
        for (int currentPoint = 0; currentPoint < controlPoints.Length - closedAdjustment; currentPoint++)
        {
            bool closedLoopFinalPoint = (closedLoop && currentPoint == controlPoints.Length - 1);

            p0 = controlPoints[currentPoint];

            if (closedLoopFinalPoint)
            {
                p1 = controlPoints[0];
            }
            else
            {
                p1 = controlPoints[currentPoint + 1];
            }

            // m0
            if (currentPoint == 0) // Tangent M[k] = (P[k+1] - P[k-1]) / 2
            {
                if (closedLoop)
                {
                    m0 = p1 - controlPoints[controlPoints.Length - 1];
                }
                else
                {
                    m0 = p1 - p0;
                }
            }
            else
            {
                m0 = p1 - controlPoints[currentPoint - 1];
            }

            // m1
            if (closedLoop)
            {
                if (currentPoint == controlPoints.Length - 1) //Last point case
                {
                    m1 = controlPoints[(currentPoint + 2) % controlPoints.Length] - p0;
                }
                else if (currentPoint == 0) //First point case
                {
                    m1 = controlPoints[currentPoint + 2] - p0;
                }
                else
                {
                    m1 = controlPoints[(currentPoint + 2) % controlPoints.Length] - p0;
                }
            }
            else
            {
                if (currentPoint < controlPoints.Length - 2)
                {
                    m1 = controlPoints[(currentPoint + 2) % controlPoints.Length] - p0;
                }
                else
                {
                    m1 = p1 - p0;
                }
            }

            m0 *= 0.5f; //Doing this here instead of  in every single above statement
            m1 *= 0.5f;

            float pointStep = 1.0f / resolution;

            if ((currentPoint == controlPoints.Length - 2 && !closedLoop) || closedLoopFinalPoint) //Final point
            {
                pointStep = 1.0f / (resolution - 1); // last point of last segment should reach p1
            }

            // Creates [resolution] points between this control point and the next
            for (int tesselatedPoint = 0; tesselatedPoint < resolution; tesselatedPoint++)
            {
                float t = tesselatedPoint * pointStep;

                CatmullRomPoint point = Evaluate(p0, p1, m0, m1, t);

                splinePoints[currentPoint * resolution + tesselatedPoint] = point;
            }
        }
    }

    //Evaluates curve at t[0, 1]. Returns point/normal/tan struct. [0, 1] means clamped between 0 and 1.
    public static CatmullRomPoint Evaluate(Vector3 start, Vector3 end, Vector3 tanPoint1, Vector3 tanPoint2,
        float t)
    {
        Vector3 position = CalculatePosition(start, end, tanPoint1, tanPoint2, t);
        Vector3 tangent = CalculateTangent(start, end, tanPoint1, tanPoint2, t);
        Vector3 normal = NormalFromTangent(tangent);

        return new CatmullRomPoint(position, tangent, normal);
    }

    //Calculates curve position at t[0, 1]
    public static Vector3 CalculatePosition(Vector3 start, Vector3 end, Vector3 tanPoint1, Vector3 tanPoint2,
        float t)
    {
        // Hermite curve formula:
        // (2t^3 - 3t^2 + 1) * p0 + (t^3 - 2t^2 + t) * m0 + (-2t^3 + 3t^2) * p1 + (t^3 - t^2) * m1
        Vector3 position = (2.0f * t * t * t - 3.0f * t * t + 1.0f) * start
                           + (t * t * t - 2.0f * t * t + t) * tanPoint1
                           + (-2.0f * t * t * t + 3.0f * t * t) * end
                           + (t * t * t - t * t) * tanPoint2;

        return position;
    }

    //Calculates tangent at t[0, 1]
    public static Vector3 CalculateTangent(Vector3 start, Vector3 end, Vector3 tanPoint1, Vector3 tanPoint2,
        float t)
    {
        // Calculate tangents
        // p'(t) = (6t² - 6t)p0 + (3t² - 4t + 1)m0 + (-6t² + 6t)p1 + (3t² - 2t)m1
        Vector3 tangent = (6 * t * t - 6 * t) * start
                          + (3 * t * t - 4 * t + 1) * tanPoint1
                          + (-6 * t * t + 6 * t) * end
                          + (3 * t * t - 2 * t) * tanPoint2;

        return tangent.normalized;
    }

    //Calculates normal vector from tangent
    public static Vector3 NormalFromTangent(Vector3 tangent)
    {
        return Vector3.Cross(tangent, Vector3.up).normalized / 2;
    }
}
#endregion
