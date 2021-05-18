/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using Simulator;
using Simulator.Sensors;

public interface IAgentController : ITriggerAgent
{
    bool Active { get; set; }
    bool Enabled { get; set; }
    GameObject AgentGameObject { get; }
    AgentConfig Config { get; set; }
    ISensorsController AgentSensorsController { get; set; }
    uint GTID { get; set; }
    Vector3 Velocity { get; }
    Bounds Bounds { get; set; }
    List<Transform> CinematicCameraTransforms { get; }
    Transform DriverViewTransform { get; set; }
    List<SensorBase> AgentSensors { get; }
    float AccelInput { get; set; }
    float SteerInput { get; set; }
    float BrakeInput { get; set; }
    event Action<IAgentController> SensorsChanged;

    void ResetPosition();
    void ResetSavedPosition(Vector3 pos, Quaternion rot);
    void Init();

    void ApplyControl(bool sticky, float steering, float acceleration);
    void DisableControl();
}
