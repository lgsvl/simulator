/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public GameObject SimulatorCameraPrefab;
    public Camera SimulatorCamera { get; private set; }
    private SimulatorCameraController cameraController;

    private void Awake()
    {
        SimulatorCamera = Instantiate(SimulatorCameraPrefab, transform).GetComponentInChildren<Camera>();
        cameraController = SimulatorCamera.GetComponentInParent<SimulatorCameraController>();
    }

    private void OnEnable()
    {
        SimulatorManager.Instance.AgentManager.AgentChanged += OnAgentChange;
    }

    private void OnDisable()
    {
        SimulatorManager.Instance.AgentManager.AgentChanged -= OnAgentChange;
    }

    private void OnAgentChange(GameObject agent)
    {
        cameraController.ResetCamera(agent);
    }

    public void ResetCamera()
    {
        cameraController.SetFreeCameraState();
    }
}
