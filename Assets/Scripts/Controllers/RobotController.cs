/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;
using Simulator.Api;

public class RobotController : AgentController
{
    public List<Transform> cinematicCameraTransforms;

    public override Vector3 Velocity { get; } = Vector3.zero;
    public override Vector3 Acceleration { get; } = Vector3.zero;

    public override List<Transform> CinematicCameraTransforms => cinematicCameraTransforms;

    private IVehicleDynamics Dynamics;
    private IAgentController Controller;
    private List<IVehicleInputs> Inputs = new List<IVehicleInputs>();

    private Vector3 InitialPosition;
    private Quaternion InitialRotation;

    public void Update()
    {
        UpdateInput();
    }

    public override void Init()
    {
        Dynamics = GetComponent<IVehicleDynamics>();
        Controller = GetComponent<IAgentController>();
        Inputs.AddRange(GetComponentsInChildren<IVehicleInputs>());
        InitialPosition = transform.position;
        InitialRotation = transform.rotation;
        Bounds = new Bounds(new Vector3(0.0f, 0.125f, 0.0f), new Vector3(1f, 0.5f, 1f));
    }

    private void UpdateInput()
    {
        SteerInput = AccelInput = BrakeInput = 0f;

        // get all inputs
        foreach (var input in Inputs)
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

    public override void ResetPosition()
    {
        if (Dynamics == null)
        {
            return;
        }

        Dynamics.ForceReset(InitialPosition, InitialRotation);
    }

    public override void ResetSavedPosition(Vector3 pos, Quaternion rot)
    {
        if (Dynamics == null)
        {
            return;
        }

        Dynamics.ForceReset(pos, rot);
    }

    public override void ApplyControl(bool sticky, float steering, float acceleration)
    {
        // see vehicle controller
    }

    void OnCollisionEnter(Collision collision)
    {
        int layerMask = LayerMask.GetMask("Obstacle", "Agent", "Pedestrian", "NPC");
        int layer = collision.gameObject.layer;
        string otherLayer = LayerMask.LayerToName(layer);
        var otherRB = collision.gameObject.GetComponent<IAgentController>();
        var otherVel = otherRB != null ? otherRB.Velocity : Vector3.zero;
        if ((layerMask & (1 << layer)) != 0)
        {
            ApiManager.Instance?.AddCollision(gameObject, collision.gameObject, collision);
            SimulatorManager.Instance.AnalysisManager.IncrementEgoCollision(Controller.GTID, transform.position, Dynamics.Velocity, otherVel, otherLayer);
        }
    }

    private void OnTriggerEnter(Collider collider)
    {
        int layerMask = LayerMask.GetMask("Obstacle", "Agent", "Pedestrian", "NPC");
        int layer = collider.gameObject.layer;
        string otherLayer = LayerMask.LayerToName(layer);
        var otherRB = collider.gameObject.GetComponent<IAgentController>();
        var otherVel = otherRB != null ? otherRB.Velocity : Vector3.zero;
        if ((layerMask & (1 << layer)) != 0)
        {
            ApiManager.Instance?.AddCollision(gameObject, collider.gameObject);
            SimulatorManager.Instance.AnalysisManager.IncrementEgoCollision(Controller.GTID, transform.position, Dynamics.Velocity, otherVel, otherLayer);
        }
    }
}
