/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using System.Collections.Generic;
using Simulator.Api;

public class VehicleController : AgentController
{
    private IVehicleDynamics dynamics;
    private VehicleActions actions;
    private IAgentController AgentController;
    private Rigidbody rb;

    private List<IVehicleInputs> inputs = new List<IVehicleInputs>();

    private string vehicleName;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 lastRBPosition;
    private Vector3 simpleVelocity;
    private Vector3 simpleAcceleration;

    public Vector2 DirectionInput { get; set; } = Vector2.zero;
    public float AccelInput { get; set; } = 0f;
    public float SteerInput { get; set; } = 0f;
    public float BrakeInput { get; set; } = 0f;
    
    public override Vector3 Velocity => simpleVelocity;
    public override Vector3 Acceleration => simpleAcceleration;

    private float turnSignalTriggerThreshold = 0.2f;
    private float turnSignalOffThreshold = 0.1f;
    private bool resetTurnIndicator = false;
    private double startTime;
    private long elapsedTime = 0;

    // api do not remove
    private bool sticky = false;
    private float stickySteering;
    private float stickAcceleraton;

    public void Update()
    {
        UpdateInput();
        UpdateLights();
        UpdateElapsedTime();
    }

    public void FixedUpdate()
    {
        UpdateInputAPI();

        if (Time.fixedDeltaTime > 0)
        {
            var previousVelocity = simpleVelocity;
            var position = rb.position;
            simpleVelocity = (position - lastRBPosition) / Time.fixedDeltaTime;
            simpleAcceleration = simpleVelocity - previousVelocity;
            lastRBPosition = position;
        }
    }

    public override void Init()
    {
        startTime = SimulatorManager.Instance.CurrentTime;
        vehicleName = Config.Name;
        dynamics = GetComponent<IVehicleDynamics>();
        actions = GetComponent<VehicleActions>();
        AgentController = GetComponent<IAgentController>();
        rb = GetComponent<Rigidbody>();
        inputs.AddRange(GetComponentsInChildren<IVehicleInputs>());
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    private void UpdateInput()
    {
        if (sticky) return;

        SteerInput = AccelInput = BrakeInput = 0f;
        
        // get all inputs
        foreach (var input in inputs)
        {
            SteerInput += input.SteerInput;
            AccelInput += input.AccelInput;
            BrakeInput += input.BrakeInput;
        }

        // clamp if over
        SteerInput = Mathf.Clamp(SteerInput, -1f, 1f);
        AccelInput = Mathf.Clamp(AccelInput, -1f, 1f);
        BrakeInput = Mathf.Clamp01(BrakeInput); // TODO use for all input types just wheel now
    }

    private void UpdateInputAPI()
    {
        if (!sticky) return;
        
        SteerInput = stickySteering;
        AccelInput = stickAcceleraton;
    }

    private void UpdateLights()
    {
        if (actions == null)
            return;
        // brakes
        if (AccelInput < 0 || BrakeInput > 0)
            actions.BrakeLights = true;
        else
            actions.BrakeLights = false;

        // reverse
        actions.ReverseLights = dynamics.Reverse;

        // turn indicator reset on turn
        if (actions.LeftTurnSignal)
        {
            if (resetTurnIndicator)
            {
                if (SteerInput > -turnSignalOffThreshold)
                    actions.LeftTurnSignal = resetTurnIndicator = false;
                
            }
            else
            {
                if (SteerInput < -turnSignalTriggerThreshold)
                    resetTurnIndicator = true;
            }
        }

        if (actions.RightTurnSignal)
        {
            if (resetTurnIndicator)
            {
                if (SteerInput < turnSignalOffThreshold)
                    actions.RightTurnSignal = resetTurnIndicator = false;
            }
            else
            {
                if (SteerInput > turnSignalTriggerThreshold)
                    resetTurnIndicator = true;
            }
        }
    }

    private void UpdateElapsedTime()
    {
        if (SimulatorManager.Instance != null)
        {
            elapsedTime = SimulatorManager.Instance.GetElapsedTime(startTime);
        }
    }

    private void OnDisable()
    {
        //
    }

    public override void ResetPosition()
    {
        if (dynamics == null) return;

        dynamics.ForceReset(initialPosition, initialRotation);
    }

    public override void ResetSavedPosition(Vector3 pos, Quaternion rot)
    {
        if (dynamics == null) return;

        dynamics.ForceReset(pos, rot);
    }

    // api
    public void ApplyControl(bool sticky, float steering, float acceleration)
    {
        this.sticky = sticky;
        stickySteering = steering;
        stickAcceleraton = acceleration;
    }

    public void ResetStickyControl()
    {
        sticky = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        int layerMask = LayerMask.GetMask("Obstacle", "Agent", "Pedestrian", "NPC");
        int layer = collision.gameObject.layer;
        string otherLayer = LayerMask.LayerToName(layer);
        var otherRB = collision.gameObject.GetComponent<Rigidbody>();
        var otherVel = otherRB != null ? otherRB.velocity : Vector3.zero;
        if ((layerMask & (1 << layer)) != 0)
        {
            ApiManager.Instance?.AddCollision(gameObject, collision.gameObject, collision);
            SimulatorManager.Instance.AnalysisManager.IncrementEgoCollision(AgentController.GTID, transform.position, dynamics.RB.velocity, otherVel, otherLayer);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        int layerMask = LayerMask.GetMask("Obstacle", "Agent", "Pedestrian", "NPC");
        int layer = other.gameObject.layer;
        string otherLayer = LayerMask.LayerToName(layer);
        var otherRB = other.gameObject.GetComponent<Rigidbody>();
        var otherVel = otherRB != null ? otherRB.velocity : Vector3.zero;
        if ((layerMask & (1 << layer)) != 0)
        {
            ApiManager.Instance?.AddCollision(gameObject, other.attachedRigidbody.gameObject);
            SimulatorManager.Instance.AnalysisManager.IncrementEgoCollision(AgentController.GTID, transform.position, dynamics.RB.velocity, otherVel, otherLayer);
        }
    }
}
