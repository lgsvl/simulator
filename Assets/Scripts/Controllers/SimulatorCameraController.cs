/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

public class SimulatorCameraController : MonoBehaviour
{
    public class CameraData
    {
        public float yaw;
        public float pitch;
        public float roll;
        public float x;
        public float y;
        public float z;

        public CameraData(Transform t)
        {
            yaw = t.eulerAngles.y;
            pitch = t.eulerAngles.x;
            roll = t.eulerAngles.z;
            x = t.position.x;
            y = t.position.y;
            z = t.position.z;
        }

        public void Translate(Vector3 translation)
        {
            Vector3 rotatedTranslation = Quaternion.Euler(pitch, yaw, roll) * translation;

            x += rotatedTranslation.x;
            y += rotatedTranslation.y;
            z += rotatedTranslation.z;
        }

        public void LerpTowards(CameraData toData, float positionLerpPct, float rotationLerpPct)
        {
            yaw = Mathf.Lerp(yaw, toData.yaw, rotationLerpPct);
            pitch = Mathf.Lerp(pitch, toData.pitch, rotationLerpPct);
            roll = Mathf.Lerp(roll, toData.roll, rotationLerpPct);

            x = Mathf.Lerp(x, toData.x, positionLerpPct);
            y = Mathf.Lerp(y, toData.y, positionLerpPct);
            z = Mathf.Lerp(z, toData.z, positionLerpPct);
        }

        public void UpdateTransform(Transform t)
        {
            t.eulerAngles = new Vector3(pitch, yaw, roll);
            t.position = new Vector3(x, y, z);
        }
    }

    private CameraData targetData;
    private CameraData lerpData;
    private Camera thisCamera;
    private LayerMask focusMask;

    private SimulatorControls controls;
    private Vector2 directionInput;
    private float elevationInput;
    private Vector2 mouseInput;
    private float isMouseLeft;
    private float isMouseRight;
    private float isMouseMiddle;
    //private Vector2 mouseScroll;
    //private Vector2 mousePosition;
    private bool isInvert = true;
    private float isBoost;

    public Transform focus;

    [Header("Movement Settings")]
    [Tooltip("Exponential boost factor on translation, controllable by mouse wheel.")]
    public float boost = 3.5f;

    [Tooltip("Time it takes to interpolate camera position 99% of the way to the target."), Range(0.001f, 1f)]
    public float positionLerpTime = 0.01f;

    [Tooltip("Time it takes to interpolate camera rotation 99% of the way to the target."), Range(0.001f, 1f)]
    public float rotationLerpTime = 0.01f;

    public bool followFocusWithVelocity = false;

    private void Awake()
    {
        thisCamera = GetComponent<Camera>();
        focusMask = 1 << LayerMask.NameToLayer("NPC") | 1 << LayerMask.NameToLayer("Agent");

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
        controls.Camera.MouseMiddle.performed += ctx => isMouseMiddle = ctx.ReadValue<float>();
        controls.Camera.MouseMiddle.canceled += ctx => isMouseMiddle = ctx.ReadValue<float>();
        //controls.Camera.MouseScroll.started += ctx => mouseScroll = ctx.ReadValue<Vector2>();
        //controls.Camera.MouseScroll.performed += ctx => mouseScroll = ctx.ReadValue<Vector2>();
        //controls.Camera.MouseScroll.canceled += ctx => mouseScroll = Vector2.zero;
        //controls.Camera.MousePosition.started += ctx => mousePosition = ctx.ReadValue<Vector2>();
        //controls.Camera.MousePosition.performed += ctx => mousePosition = ctx.ReadValue<Vector2>();
        //controls.Camera.MousePosition.canceled += ctx => mousePosition = Vector2.zero;

        controls.Camera.Boost.performed += ctx => isBoost = ctx.ReadValue<float>();
        controls.Camera.Boost.canceled += ctx => isBoost = ctx.ReadValue<float>();
    }

    private void OnEnable()
    {
        targetData = new CameraData(transform);
        lerpData = new CameraData(transform);
    }
    
    void Update()
    {
        if (isMouseRight == 1)
        {
            targetData.yaw += mouseInput.x * 0.1f;
            targetData.pitch += mouseInput.y * 0.1f * (isInvert ? -1 : 1);
        }

        // Translation
        var translation = new Vector3(directionInput.x, elevationInput, directionInput.y) * Time.deltaTime;

        // Speed up movement when shift key held
        if (isBoost == 1)
        {
            translation *= 10.0f;
        }

        // Modify movement by a boost factor (defined in Inspector and modified in play mode through the mouse scroll wheel)
        //boost += mouseScroll.y * 0.2f;
        translation *= Mathf.Pow(2.0f, boost);

        targetData.Translate(translation);

        // Calculate the lerp amount, such that we get 99% of the way to our target in the specified time
        var positionLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / positionLerpTime) * Time.deltaTime);
        var rotationLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / rotationLerpTime) * Time.deltaTime);
        lerpData.LerpTowards(targetData, positionLerpPct, rotationLerpPct);

        lerpData.UpdateTransform(transform);
    }
}
