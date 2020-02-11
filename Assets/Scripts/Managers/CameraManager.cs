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
    private SimulatorCameraController CameraController;
    
    private void Awake()
    {
        SimulatorCamera = Instantiate(SimulatorCameraPrefab, transform).GetComponentInChildren<Camera>();
        CameraController = SimulatorCamera.GetComponentInParent<SimulatorCameraController>();
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
        CameraController.SetFollowCameraState(agent);
    }

    public void SetFreeCameraState()
    {
        CameraController.SetFreeCameraState();
    }

    public void ToggleCameraState()
    {
        CameraController.IncrementCameraState();
        switch (CameraController.CurrentCameraState)
        {
            case CameraStateType.Free:
                CameraController.SetFreeCameraState();
                break;
            case CameraStateType.Follow:
                CameraController.SetFollowCameraState(SimulatorManager.Instance.AgentManager.CurrentActiveAgent);
                break;
            case CameraStateType.Cinematic:
                CameraController.SetCinematicCameraState();
                break;
        }
        SimulatorManager.Instance.UIManager?.SetCameraButtonState();
    }

    public CameraStateType GetCurrentCameraState()
    {
        return CameraController.CurrentCameraState;
    }

    public void Reset()
    {
        CameraController.SetFollowCameraState(gameObject);
    }
}
