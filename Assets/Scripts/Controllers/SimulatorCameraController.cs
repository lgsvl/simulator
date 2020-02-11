/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator.Map;
using System.Collections.Generic;
using UnityEngine;

public enum CameraStateType
{
    Free = 0,
    Follow = 1,
    Cinematic = 2
};

public enum CinematicStateType
{
    Static,
    Follow,
    Rotate,
    Stuck
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
    public CinematicStateType CurrentCinematicState = CinematicStateType.Follow;
    private float elapsedCinematicTime = 0f;
    private float cinematicCycleDuration = 8f;
    private List<Transform> cinematicCameraTransforms;

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

            controls.Camera.ToggleState.performed += ctx => ToggleFreeCinematicState();

            controls.Camera.CinematicNewPath.performed += ctx => GetCinematicFollowMapLane();
            controls.Camera.CinematicResetPath.performed += ctx => ResetCinematicMapLane();
        }

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
        elapsedCinematicTime += Time.unscaledDeltaTime;

        var step = cinematicSpeed * Time.unscaledDeltaTime;
        switch (CurrentCinematicState)
        {
            case CinematicStateType.Static:
                thisCamera.transform.LookAt(targetObject);
                break;
            case CinematicStateType.Follow:
                transform.position = Vector3.MoveTowards(transform.position, cinematicEnd, step);
                thisCamera.transform.LookAt(targetObject);
                CheckFollow();
                break;
            case CinematicStateType.Rotate:
                transform.RotateAround(targetObject.position, Vector3.up, step * 5f);
                thisCamera.transform.LookAt(targetObject);
                break;
            case CinematicStateType.Stuck:
                break;
        }

        if (elapsedCinematicTime > cinematicCycleDuration)
        {
            RandomCinematicState();
            elapsedCinematicTime = 0f;
        }
    }

    private void GetCinematicFollowMapLane()
    {
        var lanesNear = new List<MapLane>();
        for (int i = 0; i < SimulatorManager.Instance.MapManager.trafficLanes.Count; i++)
        {
            float dist = Vector3.Distance(SimulatorManager.Instance.MapManager.trafficLanes[i].mapWorldPositions[0], targetObject.position);
            if (dist < 50)
            {
                lanesNear.Add(SimulatorManager.Instance.MapManager.trafficLanes[i]);
            }
        }

        if (lanesNear.Count != 0)
        {
            var randIndex = Random.Range(0, lanesNear.Count);
            currentMapLane = lanesNear[randIndex];
            cinematicStart = currentMapLane.mapWorldPositions[0];
            cinematicStart += cinematicOffset;
            cinematicEnd = currentMapLane.mapWorldPositions[currentMapLane.mapWorldPositions.Count - 1];
            cinematicEnd += cinematicOffset;
            transform.position = cinematicStart;
        }
        else
        {
            RandomCinematicState(); // no close lanes just use a different cinematic state
        }
    }

    private void CheckFollow()
    {
        if (Vector3.Distance(transform.position, cinematicEnd) < 0.001f)
        {
            if (currentMapLane.nextConnectedLanes == null || currentMapLane.nextConnectedLanes.Count == 0)
            {
                RandomCinematicState();
            }
            else
            {
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
        SimulatorManager.Instance?.UIManager?.ResetCinematicAlpha();
        Debug.Assert(target != null);
        transform.SetParent(SimulatorManager.Instance?.CameraManager.transform);
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
        SimulatorManager.Instance?.UIManager?.ResetCinematicAlpha();
        transform.SetParent(SimulatorManager.Instance?.CameraManager.transform);
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
        SimulatorManager.Instance?.UIManager?.FadeOutIn(1f);
        CurrentCameraState = CameraStateType.Cinematic;
        targetObject = SimulatorManager.Instance.AgentManager.CurrentActiveAgent.transform;
        transform.position = targetObject.position + offset;
        thisCamera.transform.localRotation = Quaternion.identity;
        thisCamera.transform.localPosition = Vector3.zero;
        elapsedCinematicTime = 0f;
        cinematicCameraTransforms = targetObject.GetComponent<VehicleActions>().CinematicCameraTransforms;
        RandomCinematicState();
        SimulatorManager.Instance.UIManager?.SetCameraButtonState();
    }

    private void ToggleFreeCinematicState()
    {
        CurrentCameraState = CurrentCameraState == CameraStateType.Cinematic ? CameraStateType.Free : CameraStateType.Cinematic;
        switch (CurrentCameraState)
        {
            case CameraStateType.Free:
                SetFreeCameraState();
                break;
            case CameraStateType.Cinematic:
                SetCinematicCameraState();
                break;
            default:
                break;
        }
    }

    public void IncrementCameraState()
    {
        CurrentCameraState = (int)CurrentCameraState == System.Enum.GetValues(typeof(CameraStateType)).Length - 1 ? CameraStateType.Free : CurrentCameraState + 1;
    }

    private void RandomCinematicState()
    {
        SimulatorManager.Instance?.UIManager?.FadeOutIn(1f);
        currentMapLane = null;
        thisCamera.transform.localRotation = Quaternion.identity;
        thisCamera.transform.localPosition = Vector3.zero;

        var temp = (CinematicStateType)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(CinematicStateType)).Length);
        while (temp == CurrentCinematicState)
        {
            temp = (CinematicStateType)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(CinematicStateType)).Length);
        }
        CurrentCinematicState = temp;

        switch (CurrentCinematicState)
        {
            case CinematicStateType.Static:
                transform.SetParent(SimulatorManager.Instance?.CameraManager.transform);
                transform.position = (UnityEngine.Random.insideUnitSphere * 20) + targetObject.transform.position;
                transform.position = new Vector3(transform.position.x, targetObject.position.y + 10f, transform.position.z);
                break;
            case CinematicStateType.Follow:
                transform.SetParent(SimulatorManager.Instance?.CameraManager.transform);
                GetCinematicFollowMapLane();
                break;
            case CinematicStateType.Rotate:
                transform.position = targetObject.position + offset;
                transform.rotation = targetObject.transform.rotation;
                transform.SetParent(targetObject);
                break;
            case CinematicStateType.Stuck:
                var rando = Random.Range(0, cinematicCameraTransforms.Count);
                transform.position = cinematicCameraTransforms[rando].transform.position;
                transform.rotation = cinematicCameraTransforms[rando].transform.rotation;
                transform.SetParent(cinematicCameraTransforms[rando].transform);
                break;
        }
    }
}
