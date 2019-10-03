/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator.Map;
using System;
using UnityEngine;

public enum CameraStateType
{
    Free = 0,
    Follow = 1,
    Cinematic = 2
};

public class SimulatorCameraController : MonoBehaviour
{
    private SimulatorControls controls;
    private Vector2 directionInput;
    private float elevationInput;
    private Vector2 mouseInput;
    private float mouseLeft;
    private float mouseRight;
    //private float isMouseMiddle;
    //private Vector2 mouseScroll;
    private float zoomInput;

    private Camera thisCamera;
    private Transform pivot;
    private Vector3 offset = new Vector3(0f, 2.25f, -7f);
    
    private float freeSpeed = 10f;
    private float followSpeed = 25f;
    private float boost = 0f;
    private float targetTiltFree = 0f;
    private float targetLookFree = 0f;
    private Quaternion mouseFollowRot = Quaternion.identity;
    private bool inverted = true;
    private bool defaultFollow = true;
    private Vector3 targetVelocity = Vector3.zero;
    private Vector3 lastZoom = Vector3.zero;
    public Transform targetObject;

    [Range(1f, 20f)]
    public float cinematicSpeed = 5f;
    private MapLane currentMapLane;
    private Vector3 cinematicStart;
    private Vector3 cinematicEnd;
    private Vector3 cinematicOffset = new Vector3(0f, 10f, 0f);

    public CameraStateType CurrentCameraState = CameraStateType.Free;
    
    private void Awake()
    {
        thisCamera = GetComponentInChildren<Camera>();

        controls = SimulatorManager.Instance.controls;

        if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Linux && Application.isEditor)
        {
            // empty
        }
        else
        {
            controls.Camera.Direction.started += ctx => directionInput = ctx.ReadValue<Vector2>();
            controls.Camera.Direction.performed += ctx => directionInput = ctx.ReadValue<Vector2>();
            controls.Camera.Direction.canceled += ctx => directionInput = Vector2.zero;
            controls.Camera.Elevation.started += ctx => elevationInput = ctx.ReadValue<float>();
            controls.Camera.Elevation.performed += ctx => elevationInput = ctx.ReadValue<float>();
            controls.Camera.Elevation.canceled += ctx => elevationInput = 0f;

            controls.Camera.Zoom.started += ctx => zoomInput = ctx.ReadValue<float>();
            controls.Camera.Zoom.performed += ctx => zoomInput = ctx.ReadValue<float>();
            controls.Camera.Zoom.canceled += ctx => zoomInput = 0f;

            controls.Camera.Boost.performed += ctx => boost = ctx.ReadValue<float>();
            controls.Camera.Boost.canceled += ctx => boost = ctx.ReadValue<float>();

            controls.Camera.ToggleState.performed += ctx => SetFreeCameraState();

            controls.Camera.CinematicNewPath.performed += ctx => GetCinematicMapLane(true);
            controls.Camera.CinematicResetPath.performed += ctx => ResetCinematicMapLane();
        }

        controls.Camera.MouseDelta.started += ctx => mouseInput = ctx.ReadValue<Vector2>();
        controls.Camera.MouseDelta.performed += ctx => mouseInput = ctx.ReadValue<Vector2>();
        controls.Camera.MouseDelta.canceled += ctx => mouseInput = Vector2.zero;

        controls.Camera.MouseLeft.performed += ctx => mouseLeft = ctx.ReadValue<float>();
        controls.Camera.MouseLeft.canceled += ctx => mouseLeft = ctx.ReadValue<float>();
        controls.Camera.MouseRight.performed += ctx => mouseRight = ctx.ReadValue<float>();
        controls.Camera.MouseRight.canceled += ctx => mouseRight = ctx.ReadValue<float>();

        //controls.Camera.MouseMiddle.performed += ctx => ResetFollowRotation();

        // TODO broken in package currently https://github.com/Unity-Technologies/InputSystem/issues/647
        //controls.Camera.MouseScroll.started += ctx => mouseScroll = ctx.ReadValue<Vector2>();
        //controls.Camera.MouseScroll.performed += ctx => mouseScroll = ctx.ReadValue<Vector2>();
        //controls.Camera.MouseScroll.canceled += ctx => mouseScroll = Vector2.zero;
    }

    private void Start()
    {
        targetTiltFree = transform.eulerAngles.x;
        targetLookFree = transform.eulerAngles.y;
    }

    private void LateUpdate()
    {
        if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Linux && Application.isEditor)
        {
            // this is a temporary workaround for Unity Editor on Linux
            // see https://issuetracker.unity3d.com/issues/linux-editor-keyboard-when-input-handling-is-set-to-both-keyboard-input-stops-working

            if (Input.GetKeyDown(KeyCode.A)) directionInput.x -= 1;
            else if (Input.GetKeyUp(KeyCode.A)) directionInput.x += 1;

            if (Input.GetKeyDown(KeyCode.D)) directionInput.x += 1;
            else if (Input.GetKeyUp(KeyCode.D)) directionInput.x -= 1;

            if (Input.GetKeyDown(KeyCode.W))
            {
                zoomInput += 1;
                directionInput.y += 1;
            }
            else if (Input.GetKeyUp(KeyCode.W))
            {
                zoomInput -= 1;
                directionInput.y -= 1;
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                zoomInput -= 1;
                directionInput.y -= 1;
            }
            else if (Input.GetKeyUp(KeyCode.S))
            {
                zoomInput += 1;
                directionInput.y += 1;
            }

            if (Input.GetKeyDown(KeyCode.E)) elevationInput -= 1;
            else if (Input.GetKeyUp(KeyCode.E)) elevationInput += 1;

            if (Input.GetKeyDown(KeyCode.Q)) elevationInput += 1;
            else if (Input.GetKeyUp(KeyCode.Q)) elevationInput -= 1;

            if (Input.GetKeyDown(KeyCode.LeftShift)) boost += 1;
            else if (Input.GetKeyUp(KeyCode.LeftShift)) boost -= 1;

            if (Input.GetKeyDown(KeyCode.BackQuote)) SetFreeCameraState();
        }

        switch (CurrentCameraState)
        {
            case CameraStateType.Free:
                UpdateFreeCamera();
                break;
            case CameraStateType.Follow:
                UpdateFollowCamera();
                break;
            case CameraStateType.Cinematic:
                UpdateCinematicCamera();
                break;
        }
    }
    
    private void UpdateFreeCamera()
    {
        if (mouseRight == 1)
        {
            targetLookFree += mouseInput.x * 0.25f;
            targetTiltFree += mouseInput.y * 0.1f * (inverted ? -1 : 1);
            targetTiltFree = Mathf.Clamp(targetTiltFree, -90, 90);
            mouseFollowRot = Quaternion.Euler(targetTiltFree, targetLookFree, 0f);
            transform.rotation = mouseFollowRot;
        }

        transform.position = Vector3.MoveTowards(transform.position, (transform.rotation * new Vector3(directionInput.x, elevationInput, directionInput.y)) + transform.position, Time.unscaledDeltaTime * freeSpeed * (boost == 1 ? 10f : 1f));
    }
    
    private void UpdateFollowCamera()
    {
        Debug.Assert(targetObject != null);
        
        var dist = Vector3.Distance(thisCamera.transform.position, targetObject.position);
        if (dist < 3)
            thisCamera.transform.localPosition = Vector3.MoveTowards(thisCamera.transform.localPosition, thisCamera.transform.InverseTransformPoint(targetObject.position), -Time.unscaledDeltaTime);
        else if (dist > 30)
            thisCamera.transform.localPosition = Vector3.MoveTowards(thisCamera.transform.localPosition, thisCamera.transform.InverseTransformPoint(targetObject.position), Time.unscaledDeltaTime);
        else if (zoomInput != 0)
            thisCamera.transform.localPosition = Vector3.MoveTowards(thisCamera.transform.localPosition, thisCamera.transform.InverseTransformPoint(targetObject.position), Time.unscaledDeltaTime * zoomInput * 10f * (boost == 1 ? 10f : 1f));
        
        if (mouseRight == 1)
        {
            defaultFollow = false;
            targetLookFree += mouseInput.x * 0.25f;
            targetTiltFree += mouseInput.y * 0.1f * (inverted ? -1 : 1);
            targetTiltFree = Mathf.Clamp(targetTiltFree, -15, 65);
            mouseFollowRot = Quaternion.Euler(targetTiltFree, targetLookFree, 0f);
            transform.localRotation = mouseFollowRot;
        }
        else
        {
            //transform.rotation = Quaternion.LookRotation(Vector3.RotateTowards(transform.forward, (mouseFollowRot * targetObject.forward), followSpeed * Time.unscaledDeltaTime, 1f)); // TODO new state for follow camera at mouse rotation else mouseFollowRot
            if (defaultFollow)
                transform.rotation = Quaternion.LookRotation(Vector3.RotateTowards(transform.forward, targetObject.forward, followSpeed * Time.unscaledDeltaTime, 1f));
            
            targetTiltFree = transform.eulerAngles.x;
            targetLookFree = transform.eulerAngles.y;

            if (targetTiltFree > 180)
            {
                targetTiltFree -= 360;
            }
        }
        transform.position = Vector3.SmoothDamp(transform.position, targetObject.position, ref targetVelocity, 0.1f);
    }

    private void UpdateCinematicCamera()
    {
        Debug.Assert(targetObject != null);
        
        GetCinematicMapLane();

        var step = cinematicSpeed * Time.unscaledDeltaTime;
        if (currentMapLane != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, cinematicEnd, step);
        }
        else
        {
            var dist = Vector3.Distance(transform.position, targetObject.position);
            if (dist < 3)
                transform.position = Vector3.MoveTowards(transform.position, targetObject.position, -Time.unscaledDeltaTime * cinematicSpeed);
            else if (dist > 30)
                transform.position = Vector3.MoveTowards(transform.position, targetObject.position, Time.unscaledDeltaTime * cinematicSpeed);
            else if (zoomInput != 0)
                transform.position = Vector3.MoveTowards(transform.position, targetObject.position, Time.unscaledDeltaTime * zoomInput * 10f * (boost == 1 ? 10f : 1f));
            transform.position = Vector3.MoveTowards(transform.position, (transform.rotation * new Vector3(0f, elevationInput, 0f)) + transform.position, Time.unscaledDeltaTime * freeSpeed * (boost == 1 ? 10f : 1f));

            transform.RotateAround(targetObject.position, Vector3.up, step);
        }

        thisCamera.transform.LookAt(targetObject);
    }

    private void GetCinematicMapLane(bool isGetNew = false)
    {
        if  (CurrentCameraState != CameraStateType.Cinematic)
        {
            return;
        }

        if (currentMapLane == null || isGetNew)
        {
            for (int i = 0; i < SimulatorManager.Instance.MapManager.trafficLanes.Count; i++)
            {
                int rand = UnityEngine.Random.Range(0, SimulatorManager.Instance.MapManager.trafficLanes.Count);
                float dist = Vector3.Distance(SimulatorManager.Instance.MapManager.trafficLanes[rand].mapWorldPositions[0], targetObject.position);

                if (SimulatorManager.Instance.MapManager.trafficLanes[rand].Spawnable)
                {
                    currentMapLane = SimulatorManager.Instance.MapManager.trafficLanes[rand];
                    cinematicStart = currentMapLane.mapWorldPositions[0];
                    cinematicStart += cinematicOffset;
                    cinematicEnd = currentMapLane.mapWorldPositions[currentMapLane.mapWorldPositions.Count - 1];
                    cinematicEnd += cinematicOffset;
                    transform.position = cinematicStart;
                    if (dist < 100f)
                    {
                        break;
                    }
                }
            }
        }
        else
        {
            if (Vector3.Distance(transform.position, cinematicEnd) < 0.001f) // get next
            {
                if (currentMapLane.nextConnectedLanes == null || currentMapLane.nextConnectedLanes.Count == 0)
                {
                    currentMapLane = null;
                    return;
                }

                int rand = UnityEngine.Random.Range(0, currentMapLane.nextConnectedLanes.Count - 1);
                currentMapLane = currentMapLane.nextConnectedLanes[rand];
                cinematicStart = currentMapLane.mapWorldPositions[0];
                cinematicStart += cinematicOffset;
                cinematicEnd = currentMapLane.mapWorldPositions[currentMapLane.mapWorldPositions.Count - 1];
                cinematicEnd += cinematicOffset;
                transform.position = cinematicStart;
            }
        }
    }

    private void ResetCinematicMapLane()
    {
        if (currentMapLane == null || CurrentCameraState != CameraStateType.Cinematic)
        {
            return;
        }

        cinematicStart = currentMapLane.mapWorldPositions[0];
        cinematicStart += cinematicOffset;
        cinematicEnd = currentMapLane.mapWorldPositions[currentMapLane.mapWorldPositions.Count - 1];
        cinematicEnd += cinematicOffset;
        transform.position = cinematicStart;
    }

    public void SetFollowCameraState(GameObject target)
    {
        SimulatorManager.Instance.UIManager.SetEnvironmentButton(false);
        Debug.Assert(target != null);
        CurrentCameraState = CameraStateType.Follow;
        targetObject = target.transform;
        transform.position = targetObject.position;
        transform.rotation = targetObject.rotation;
        thisCamera.transform.localRotation = Quaternion.identity;
        thisCamera.transform.localPosition = Vector3.zero;
        thisCamera.transform.localPosition = thisCamera.transform.InverseTransformPoint(targetObject.position) + offset;
        defaultFollow = true;
        targetTiltFree = transform.eulerAngles.x;
        targetLookFree = transform.eulerAngles.y;
        SimulatorManager.Instance.UIManager?.SetCameraButtonState();
    }

    public void SetFreeCameraState()
    {
        SimulatorManager.Instance.UIManager.SetEnvironmentButton(false);
        CurrentCameraState = CameraStateType.Free;
        targetObject = null;
        transform.position = thisCamera.transform.position;
        transform.rotation = thisCamera.transform.rotation;
        thisCamera.transform.localRotation = Quaternion.identity;
        thisCamera.transform.localPosition = Vector3.zero;
        targetTiltFree = transform.eulerAngles.x;
        targetLookFree = transform.eulerAngles.y;
        SimulatorManager.Instance.UIManager?.SetCameraButtonState();
    }

    public void SetCinematicCameraState()
    {
        SimulatorManager.Instance.UIManager.SetEnvironmentButton(true);
        CurrentCameraState = CameraStateType.Cinematic;
        targetObject = SimulatorManager.Instance.AgentManager.CurrentActiveAgent.transform;
        transform.position = targetObject.position + offset;
        thisCamera.transform.localRotation = Quaternion.identity;
        thisCamera.transform.localPosition = Vector3.zero;
    }

    public void IncrementCameraState()
    {
        CurrentCameraState = (int)CurrentCameraState == System.Enum.GetValues(typeof(CameraStateType)).Length - 1 ? CameraStateType.Free : CurrentCameraState + 1;
    }
}
