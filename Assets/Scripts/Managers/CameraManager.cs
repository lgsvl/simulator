/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public GameObject SimulatorCameraPrefab;
    public Camera SimulatorCamera { get; private set; }
    public SimulatorCameraController CameraController { get; private set; }

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

    /// <summary>
    /// API command to set free camera position and rotation
    /// </summary>
    public void SetFreeCameraState(Vector3 pos, Vector3 rot)
    {
        CameraController.SetFreeCameraState(pos, rot);
    }

    /// <summary>
    /// API command to set camera state
    /// </summary>
    public void SetCameraState(CameraStateType state)
    {
        switch (state)
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
            case CameraStateType.Driver:
                CameraController.SetDriverViewCameraState();
                break;
        }
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
            case CameraStateType.Driver:
                CameraController.SetDriverViewCameraState();
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
