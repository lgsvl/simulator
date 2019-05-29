/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

public enum CameraStateType
{
    Free,
    Follow
};

public class SimulatorCameraController : MonoBehaviour
{
    private SimulatorControls controls;
    private Vector2 directionInput;
    private float elevationInput;
    private Vector2 mouseInput;
    private float isMouseLeft;
    private float isMouseRight;
    //private float isMouseMiddle;
    //private Vector2 mouseScroll;
    private float zoomInput;

    private Camera thisCamera;
    private Transform pivot;
    private Vector3 offset = new Vector3(0f, 2.25f, -7f);
    
    private float freeSpeed = 10f;
    private float followSpeed = 25f;
    private float boost = 1f;
    private float isBoost = 0f;
    private float targetTiltFree = 0f;
    private float targetLookFree = 0f;
    private Quaternion targetRotFree = Quaternion.identity;
    private Quaternion mouseFollowRot = Quaternion.identity;
    private bool isInvert = true;
    //private bool isDefaultFollow = true;
    private Vector3 targetVelocity = Vector3.zero;
    private Vector3 lastZoom = Vector3.zero;
    public Transform targetObject;

    public CameraStateType currentCameraState = CameraStateType.Free;

    private void Awake()
    {
        thisCamera = GetComponentInChildren<Camera>();

        controls = SimulatorManager.Instance.controls;
        controls.Camera.Direction.started += ctx => directionInput = ctx.ReadValue<Vector2>();
        controls.Camera.Direction.performed += ctx => directionInput = ctx.ReadValue<Vector2>();
        controls.Camera.Direction.canceled += ctx => directionInput = Vector2.zero;
        controls.Camera.Elevation.started += ctx => elevationInput = ctx.ReadValue<float>();
        controls.Camera.Elevation.performed += ctx => elevationInput = ctx.ReadValue<float>();
        controls.Camera.Elevation.canceled += ctx => elevationInput = 0f;

        controls.Camera.MouseDelta.started += ctx => mouseInput = ctx.ReadValue<Vector2>();
        controls.Camera.MouseDelta.performed += ctx => mouseInput = ctx.ReadValue<Vector2>();
        controls.Camera.MouseDelta.canceled += ctx => mouseInput = Vector2.zero;

        controls.Camera.MouseDelta.started += ctx => mouseInput = ctx.ReadValue<Vector2>();
        controls.Camera.MouseDelta.performed += ctx => mouseInput = ctx.ReadValue<Vector2>();
        controls.Camera.MouseDelta.canceled += ctx => mouseInput = Vector2.zero;
        controls.Camera.MouseLeft.performed += ctx => isMouseLeft = ctx.ReadValue<float>();
        controls.Camera.MouseLeft.canceled += ctx => isMouseLeft = ctx.ReadValue<float>();
        controls.Camera.MouseRight.performed += ctx => isMouseRight = ctx.ReadValue<float>();
        controls.Camera.MouseRight.canceled += ctx => isMouseRight = ctx.ReadValue<float>();
        //controls.Camera.MouseMiddle.performed += ctx => ResetFollowRotation();
        // TODO broken in package currently https://github.com/Unity-Technologies/InputSystem/issues/647
        //controls.Camera.MouseScroll.started += ctx => mouseScroll = ctx.ReadValue<Vector2>();
        //controls.Camera.MouseScroll.performed += ctx => mouseScroll = ctx.ReadValue<Vector2>();
        //controls.Camera.MouseScroll.canceled += ctx => mouseScroll = Vector2.zero;
        controls.Camera.Zoom.started += ctx => zoomInput = ctx.ReadValue<float>();
        controls.Camera.Zoom.performed += ctx => zoomInput = ctx.ReadValue<float>();
        controls.Camera.Zoom.canceled += ctx => zoomInput = 0f;

        controls.Camera.Boost.performed += ctx => isBoost = ctx.ReadValue<float>();
        controls.Camera.Boost.canceled += ctx => isBoost = ctx.ReadValue<float>();

        controls.Camera.ToggleState.performed += ctx => SetFreeCameraState();
    }
    
    private void Update()
    {
        switch (currentCameraState)
        {
            case CameraStateType.Free:
                UpdateFreeCamera();
                break;
            case CameraStateType.Follow:
                UpdateFollowCamera();
                break;
        }
    }
    
    private void SetFreeCameraState()
    {
        currentCameraState = CameraStateType.Free;
    }

    private void UpdateFreeCamera()
    {
        if (isMouseRight == 1)
        {
            Cursor.visible = false;
            targetTiltFree += mouseInput.y * 0.1f * (isInvert ? -1 : 1);
            targetTiltFree = Mathf.Clamp(targetTiltFree, -35, 85);
            targetLookFree += mouseInput.x * 0.1f;
            targetRotFree = Quaternion.Euler(targetTiltFree, targetLookFree, 0f);
            mouseFollowRot = Quaternion.Slerp(transform.rotation, targetRotFree, Time.deltaTime * 20f);
            transform.rotation = mouseFollowRot;
        }
        else
        {
            Cursor.visible = true;
        }
        boost = isBoost == 1 ? 10f : 1f;
        transform.position = Vector3.MoveTowards(transform.position, (transform.rotation * new Vector3(directionInput.x, elevationInput, directionInput.y)) + transform.position, Time.deltaTime * freeSpeed * boost);
    }

    //private void ResetFollowRotation()
    //{
    //    isDefaultFollow = true;
    //}

    private void UpdateFollowCamera()
    {
        Debug.Assert(targetObject != null);
        
        boost = isBoost == 1 ? 10f : 1f;

        var dist = Vector3.Distance(thisCamera.transform.position, targetObject.position);
        if (dist < 3)
            thisCamera.transform.localPosition = Vector3.MoveTowards(thisCamera.transform.localPosition, thisCamera.transform.InverseTransformPoint(targetObject.position), -Time.deltaTime);
        else if (dist > 30)
            thisCamera.transform.localPosition = Vector3.MoveTowards(thisCamera.transform.localPosition, thisCamera.transform.InverseTransformPoint(targetObject.position), Time.deltaTime);
        else if (zoomInput != 0)
            thisCamera.transform.localPosition = Vector3.MoveTowards(thisCamera.transform.localPosition, thisCamera.transform.InverseTransformPoint(targetObject.position), Time.deltaTime * zoomInput * 10f * boost);
        
        if (isMouseRight == 1)
        {
            //isDefaultFollow = false;
            Cursor.visible = false;
            targetTiltFree += mouseInput.y * 0.1f * (isInvert ? -1 : 1);
            targetTiltFree = Mathf.Clamp(targetTiltFree, -35, 85);
            targetLookFree += mouseInput.x * 0.1f;
            targetRotFree = Quaternion.Euler(targetTiltFree, targetLookFree, 0f);
            mouseFollowRot = Quaternion.Slerp(transform.rotation, targetRotFree, Time.deltaTime * 20f);
            transform.rotation = mouseFollowRot;
        }
        else
        {
            Cursor.visible = true;
            var lookRot = Quaternion.LookRotation(Vector3.RotateTowards(transform.forward, (mouseFollowRot * targetObject.forward), followSpeed * Time.deltaTime, 1f));
            transform.rotation = lookRot;
            //if (isDefaultFollow)
            //{
            //    var lookRot = Quaternion.LookRotation(Vector3.RotateTowards(transform.forward, targetObject.forward, followSpeed * Time.deltaTime, 1f));
            //    transform.rotation = lookRot;
            //}
            //else
            //{
            //    transform.rotation = mouseFollowRot;
            //}
        }
        transform.position = Vector3.SmoothDamp(transform.position, targetObject.position, ref targetVelocity, 0.1f);
    }

    public void ResetCamera(GameObject target)
    {
        Debug.Assert(target != null);
        targetObject = target.transform;
        transform.position = targetObject.position;
        transform.rotation = targetObject.rotation;
        thisCamera.transform.localRotation = Quaternion.identity;
        thisCamera.transform.localPosition = Vector3.zero;
        thisCamera.transform.localPosition = thisCamera.transform.InverseTransformPoint(targetObject.position) + offset;
        //isDefaultFollow = true;
        targetTiltFree = 0f;
        targetLookFree = 0f;
        targetRotFree = Quaternion.identity;
        mouseFollowRot = Quaternion.identity;
        currentCameraState = CameraStateType.Follow;
    }
}
