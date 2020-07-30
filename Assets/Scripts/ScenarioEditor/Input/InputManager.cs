/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Input
{
    using System;
    using Agents;
    using Elements;
    using Managers;
    using Network.Core.Threading;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.InputSystem;

    /// <summary>
    /// Input manager that handles all the keyboard and mouse inputs in the Scenario Editor
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        /// <summary>
        /// Input state type that determines input behaviour
        /// </summary>
        private enum InputState
        {
            /// <summary>
            /// Input is idle and allow basic map movement and element selecting
            /// </summary>
            Idle,

            /// <summary>
            /// Input allows map and camera movement
            /// </summary>
            MovingCamera,

            /// <summary>
            /// Input allows dragging element and limited camera movement
            /// </summary>
            DraggingElement,

            /// <summary>
            /// Input allows adding element and limited camera movement
            /// </summary>
            AddingElement
        }

        /// <summary>
        /// Camera mode, sets fixed rotation or allows free rotation
        /// </summary>
        public enum CameraModeType
        {
            /// <summary>
            /// Camera is perpendicular to the ground
            /// </summary>
            TopDown = 0,

            /// <summary>
            /// Camera is leaned by 45 degree to the ground
            /// </summary>
            Leaned45 = 1,

            /// <summary>
            /// Camera can be rotated freely 
            /// </summary>
            Free = 2
        }

        /// <summary>
        /// Modes how current action can be canceled
        /// </summary>
        public enum CancelMode
        {
            /// <summary>
            /// Current action will be canceled as soon as button is released
            /// </summary>
            CancelOnRelease,

            /// <summary>
            /// Current action will be canceled when button is clicked again
            /// </summary>
            CancelOnClick
        }

        /// <summary>
        /// Zoom factor applied when using the mouse wheel zoom
        /// </summary>
        private const float ZoomFactor = 5.0f;

        /// <summary>
        /// Rotation factor applied when camera is rotated with mouse movement
        /// </summary>
        private const float RotationFactor = 3.0f;

        /// <summary>
        /// Movement factor applied when camera is moved by keyboard keys
        /// </summary>
        private const float KeyMoveFactor = 15.0f;

        /// <summary>
        /// Persistence data key for rotation tilt value
        /// </summary>
        private static string RotationTiltKey = "Simulator/ScenarioEditor/InputManager/RotationTilt";

        /// <summary>
        /// Persistence data key for rotation look value
        /// </summary>
        private static string RotationLookKey = "Simulator/ScenarioEditor/InputManager/RotationLook";

        /// <summary>
        /// Persistence data key for camera mode
        /// </summary>
        private static string CameraModeKey = "Simulator/ScenarioEditor/InputManager/CameraMode";

        /// <summary>
        /// Persistence data key for X rotation inversion value
        /// </summary>
        private static string XRotationInversionKey = "Simulator/ScenarioEditor/InputManager/XRotationInversion";

        /// <summary>
        /// Persistence data key for Y rotation inversion value
        /// </summary>
        private static string YRotationInversionKey = "Simulator/ScenarioEditor/InputManager/YRotationInversion";

        /// <summary>
        /// Cached scenario world camera
        /// </summary>
        private Camera scenarioCamera;

        /// <summary>
        /// Cached simulator input controls
        /// </summary>
        private SimulatorControls controls;

        /// <summary>
        /// Is the left mouse button currently pressed
        /// </summary>
        private bool leftMouseButtonPressed;

        /// <summary>
        /// Is the right mouse button currently pressed
        /// </summary>
        private bool rightMouseButtonPressed;

        /// <summary>
        /// Is the middle mouse button currently pressed
        /// </summary>
        private bool middleMouseButtonPressed;

        /// <summary>
        /// Value of the input direction vector, moves camera in selected directions
        /// </summary>
        private Vector2 directionInput;

        /// <summary>
        /// Tilt value for the camera rotation
        /// </summary>
        private float rotationTilt;

        /// <summary>
        /// Look value for the camera rotation
        /// </summary>
        private float rotationLook;

        /// <summary>
        /// Was the rotation changed and has to be saved to the prefs
        /// </summary>
        private bool rotationDirty;

        /// <summary>
        /// Camera mode, sets fixed rotation or allows free rotation
        /// </summary>
        private CameraModeType cameraMode;

        /// <summary>
        /// X rotation inversion value. -1 - inverted rotation, 1 - uninverted rotation
        /// </summary>
        private int xRotationInversion;

        /// <summary>
        /// Y rotation inversion value. -1 - inverted rotation, 1 - uninverted rotation
        /// </summary>
        private int yRotationInversion;

        /// <summary>
        /// Layer mask used when raycasting world
        /// </summary>
        private int raycastLayerMask = ~0;

        /// <summary>
        /// Maximum raycast distance
        /// </summary>
        private float raycastDistance;

        /// <summary>
        /// Cached raycasts hits, some raycasts can be outdated, check if index is lower than <see cref="raycastHitsCount"/>
        /// </summary>
        private RaycastHit[] raycastHits = new RaycastHit[5];

        /// <summary>
        /// Count of the valid raycast hits
        /// </summary>
        private int raycastHitsCount;

        /// <summary>
        /// Current input state type determining input behaviour
        /// </summary>
        private InputState inputState;

        /// <summary>
        /// Can the drag event finish over UI elements, cancel is called if it's not allowed
        /// </summary>
        private bool canDragFinishOverUI;

        /// <summary>
        /// Current cancel mode selected for the operation
        /// </summary>
        private CancelMode cancelMode;

        /// <summary>
        /// Bool determining if the mouse has moved in this frame
        /// </summary>
        private bool mouseMoved;

        /// <summary>
        /// Mouse position from the previous frame
        /// </summary>
        private Vector3 lastMousePosition;

        /// <summary>
        /// Hit position from the previous usage
        /// </summary>
        private Vector3 lastHitPosition;

        /// <summary>
        /// Cached currently managed drag handler
        /// </summary>
        private IDragHandler dragHandler;

        /// <summary>
        /// Cached currently managed add elements handler
        /// </summary>
        private IAddElementsHandler addElementsHandler;

        /// <summary>
        /// Inputs semaphore that allows disabling all the keyboard and mouse processing
        /// </summary>
        public LockingSemaphore InputSemaphore { get; } = new LockingSemaphore();

        /// <summary>
        /// World position of the raycast casted from the mouse pointer
        /// </summary>
        public Vector3 MouseRaycastPosition { get; private set; }

        /// <summary>
        /// Viewport position of the mouse pointer
        /// </summary>
        public Vector2 MouseViewportPosition { get; private set; }

        /// <summary>
        /// Is the rotation locked
        /// </summary>
        public CameraModeType CameraMode
        {
            get => cameraMode;
            set
            {
                if (cameraMode == value) return;
                cameraMode = value;
                PlayerPrefs.SetInt(CameraModeKey, (int) cameraMode);
                switch (cameraMode)
                {
                    case CameraModeType.TopDown:
                        scenarioCamera.transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0f);
                        break;
                    case CameraModeType.Leaned45:
                        scenarioCamera.transform.rotation = Quaternion.Euler(45.0f, 0.0f, 0f);
                        break;
                    case CameraModeType.Free:
                        scenarioCamera.transform.rotation = Quaternion.Euler(rotationTilt, rotationLook, 0f);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Is the X camera rotation enabled
        /// </summary>
        public bool InvertedXRotation
        {
            get
            {
                // -1 - inverted rotation, 1 - uninverted rotation
                if (xRotationInversion == 0)
                    xRotationInversion = PlayerPrefs.GetInt(XRotationInversionKey, 1);
                return xRotationInversion < 0;
            }
            set
            {
                // -1 - inverted rotation, 1 - uninverted rotation
                var intValue = value ? -1 : 1;
                if (intValue == xRotationInversion) return;
                xRotationInversion = intValue;
                PlayerPrefs.SetInt(XRotationInversionKey, intValue);
            }
        }

        /// <summary>
        /// Is the Y camera rotation enabled
        /// </summary>
        public bool InvertedYRotation
        {
            get
            {
                // -1 - inverted rotation, 1 - uninverted rotation
                if (yRotationInversion == 0)
                    yRotationInversion = PlayerPrefs.GetInt(YRotationInversionKey, 1);
                return yRotationInversion < 0;
            }
            set
            {
                // -1 - inverted rotation, 1 - uninverted rotation
                var intValue = value ? -1 : 1;
                if (intValue == yRotationInversion) return;
                yRotationInversion = intValue;
                PlayerPrefs.SetInt(YRotationInversionKey, intValue);
            }
        }

        /// <summary>
        /// Checks if the mouse is over the game window, required for the window mode
        /// </summary>
        private bool IsMouseOverGameWindow => !(0 > Input.mousePosition.x || 0 > Input.mousePosition.y ||
                                                Screen.width < Input.mousePosition.x ||
                                                Screen.height < Input.mousePosition.y);

        /// <summary>
        /// Unity Start method
        /// </summary>
        /// <exception cref="ArgumentException">Invalid setup</exception>
        private void Start()
        {
            scenarioCamera = FindObjectOfType<ScenarioManager>()?.ScenarioCamera;
            if (scenarioCamera == null)
                throw new ArgumentException("Scenario camera reference is required in the ScenarioManager.");
            controls = new SimulatorControls();
            InitControls();
            raycastDistance = scenarioCamera.farClipPlane - scenarioCamera.nearClipPlane;

            //Revert camera rotation from prefs
            var cameraTransform = scenarioCamera.transform;
            var cameraRotation = cameraTransform.rotation.eulerAngles;
            rotationTilt = PlayerPrefs.GetFloat(RotationTiltKey, cameraRotation.x);
            rotationLook = PlayerPrefs.GetFloat(RotationLookKey, cameraRotation.y);
            CameraMode = (CameraModeType) PlayerPrefs.GetInt(CameraModeKey, 0);

            //Setup layers mask
            var ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
            raycastLayerMask &= ~(1 << ignoreRaycastLayer);
        }

        /// <summary>
        /// Unity OnDestroy method
        /// </summary>
        /// <exception cref="ArgumentException">Invalid setup</exception>
        private void OnDestroy()
        {
            DeinitControls();
        }

        /// <summary>
        /// Initializes keyboard and mouse input callbacks
        /// </summary>
        private void InitControls()
        {
            controls.Camera.MouseScroll.performed += MouseScrollOnPerformed;
            controls.Camera.MouseLeft.performed += MouseLeftOnPerformed;
            controls.Camera.MouseRight.performed += MouseRightOnPerformed;
            controls.Camera.MouseMiddle.performed += MouseMiddleOnPerformed;
            controls.Camera.Direction.started += ctx => directionInput = ctx.ReadValue<Vector2>();
            controls.Camera.Direction.performed += ctx => directionInput = ctx.ReadValue<Vector2>();
            controls.Camera.Direction.canceled += ctx => directionInput = Vector2.zero;
            controls.Enable();
        }

        /// <summary>
        /// Deinitializes keyboard and mouse input callbacks
        /// </summary>
        private void DeinitControls()
        {
            controls.Disable();
            controls.Camera.MouseScroll.performed -= MouseScrollOnPerformed;
            controls.Camera.MouseLeft.performed -= MouseLeftOnPerformed;
            controls.Camera.MouseRight.performed -= MouseRightOnPerformed;
            controls.Camera.MouseMiddle.performed -= MouseMiddleOnPerformed;
        }

        #region Events

        /// <summary>
        /// Callback on camera mouse scroll
        /// </summary>
        /// <param name="obj">Callback context</param>
        private void MouseScrollOnPerformed(InputAction.CallbackContext obj)
        {
            if (EventSystem.current.IsPointerOverGameObject() || !IsMouseOverGameWindow) return;

            var zoomValue = obj.ReadValue<Vector2>().y;
            var cameraTransform = scenarioCamera.transform;
            MoveCameraTo(cameraTransform.position + cameraTransform.forward * (zoomValue * ZoomFactor));
        }

        /// <summary>
        /// Callback on camera mouse left
        /// </summary>
        /// <param name="obj">Callback context</param>
        private void MouseLeftOnPerformed(InputAction.CallbackContext obj)
        {
            leftMouseButtonPressed = obj.ReadValue<float>() > 0.0f;

            RaycastHit? furthestHit;
            switch (inputState)
            {
                case InputState.Idle:
                    if (leftMouseButtonPressed &&
                        !EventSystem.current.IsPointerOverGameObject())
                    {
                        ScenarioManager.Instance.SelectedElement = null;
                        furthestHit = GetFurthestHit();
                        if (furthestHit == null)
                            return;

                        ScenarioElement element = null;
                        for (int i = 0; i < raycastHitsCount; i++)
                        {
                            element = raycastHits[i].collider.gameObject.GetComponentInParent<ScenarioElement>();
                            if (element == null) continue;

                            ScenarioManager.Instance.SelectedElement = element;
                            //Override furthest hit with selected element hit
                            furthestHit = raycastHits[i];
                            break;
                        }

                        lastHitPosition = furthestHit.Value.point;
                    }

                    break;
                case InputState.DraggingElement:
                    //Check for drag finish
                    if (!leftMouseButtonPressed)
                    {
                        if (cancelMode == CancelMode.CancelOnClick)
                        {
                            cancelMode = CancelMode.CancelOnRelease;
                            break;
                        }

                        MouseRaycastPosition = lastHitPosition;
                        MouseViewportPosition = scenarioCamera.ScreenToViewportPoint(Input.mousePosition);
                        if (!canDragFinishOverUI && EventSystem.current.IsPointerOverGameObject())
                        {
                            dragHandler.DragCancelled();
                        }
                        else
                        {
                            ScenarioManager.Instance.IsScenarioDirty = true;
                            dragHandler.DragFinished();
                        }

                        dragHandler = null;
                        inputState = InputState.Idle;
                    }

                    break;
                case InputState.AddingElement:
                    if (leftMouseButtonPressed)
                    {
                        //Apply current adding state
                        furthestHit = GetFurthestHit(true);
                        if (furthestHit == null)
                            break;
                        var furthestPoint = furthestHit.Value.point;
                        lastHitPosition = furthestPoint;
                        ScenarioManager.Instance.IsScenarioDirty = true;
                        addElementsHandler.AddElement(furthestPoint);
                    }

                    break;
            }
        }

        /// <summary>
        /// Callback on camera mouse right
        /// </summary>
        /// <param name="obj">Callback context</param>
        private void MouseRightOnPerformed(InputAction.CallbackContext obj)
        {
            rightMouseButtonPressed = obj.ReadValue<float>() > 0.0f;

            switch (inputState)
            {
                case InputState.Idle:
                    if (rightMouseButtonPressed)
                        ScenarioManager.Instance.SelectedElement = null;
                    break;
                case InputState.DraggingElement:
                    //Right mouse button cancels action
                    MouseRaycastPosition = lastHitPosition;
                    MouseViewportPosition = scenarioCamera.ScreenToViewportPoint(Input.mousePosition);
                    dragHandler.DragCancelled();
                    dragHandler = null;
                    inputState = InputState.Idle;
                    break;
                case InputState.AddingElement:
                    //Right mouse button cancels action
                    addElementsHandler.AddingCancelled(lastHitPosition);
                    inputState = InputState.Idle;
                    break;
            }
        }

        /// <summary>
        /// Callback on camera mouse middle
        /// </summary>
        /// <param name="obj">Callback context</param>
        private void MouseMiddleOnPerformed(InputAction.CallbackContext obj)
        {
            middleMouseButtonPressed = obj.ReadValue<float>() > 0.0f;

            switch (inputState)
            {
                case InputState.Idle:
                    if (middleMouseButtonPressed &&
                        !EventSystem.current.IsPointerOverGameObject())
                    {
                        var furthestHit = GetFurthestHit();
                        if (furthestHit == null)
                            return;

                        inputState = InputState.MovingCamera;

                        lastHitPosition = furthestHit.Value.point;
                    }

                    break;
                case InputState.MovingCamera:
                    if (!middleMouseButtonPressed) inputState = InputState.Idle;
                    break;
            }
        }

        #endregion

        /// <summary>
        /// Unity Update method
        /// </summary>
        private void Update()
        {
            if (InputSemaphore.IsLocked)
                return;
            RaycastAll();
            HandleMapInput();
        }

        /// <summary>
        /// Raycasts everything in the move position
        /// </summary>
        private void RaycastAll()
        {
            //TODO Check if raycast is needed
            mouseMoved = (lastMousePosition - Input.mousePosition).magnitude > 1.0f;
            var ray = scenarioCamera.ScreenPointToRay(Input.mousePosition);
            raycastHitsCount = Physics.RaycastNonAlloc(ray, raycastHits, raycastDistance, raycastLayerMask);
            lastMousePosition = Input.mousePosition;
        }

        /// <summary>
        /// Selects the furthest hit from the current raycast hits
        /// </summary>
        /// <param name="ignoreRigidbodies">Should the colliders with rigidbodies be ignored</param>
        /// <returns>Furthest hit, null if there was no valid raycast hit</returns>
        private RaycastHit? GetFurthestHit(bool ignoreRigidbodies = false)
        {
            RaycastHit? furthestHit = null;
            var furthestDistance = 0.0f;
            for (var i = 0; i < raycastHitsCount; i++)
                if (raycastHits[i].distance > furthestDistance &&
                    (!ignoreRigidbodies || raycastHits[i].rigidbody == null))
                {
                    furthestHit = raycastHits[i];
                    furthestDistance = furthestHit.Value.distance;
                }

            return furthestHit;
        }

        /// <summary>
        /// Main loop for the input management
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Invalid input state value</exception>
        private void HandleMapInput()
        {
            Transform cameraTransform = scenarioCamera.transform;
            RaycastHit? furthestHit;
            Vector3 furthestPoint;
            switch (inputState)
            {
                case InputState.MovingCamera:
                    if (IsMouseOverGameWindow && mouseMoved && raycastHitsCount > 0)
                    {
                        furthestHit = GetFurthestHit(true);
                        if (furthestHit == null)
                            return;
                        var cameraPosition = cameraTransform.position;
                        var deltaPosition = lastHitPosition - furthestHit.Value.point;
                        deltaPosition.y = 0.0f;
                        MoveCameraTo(cameraPosition + deltaPosition);
                    }

                    break;
                case InputState.DraggingElement:
                    //Apply current drag state
                    furthestHit = GetFurthestHit(true);
                    if (furthestHit == null)
                        break;
                    furthestPoint = furthestHit.Value.point;
                    lastHitPosition = furthestPoint;
                    MouseRaycastPosition = lastHitPosition;
                    MouseViewportPosition = scenarioCamera.ScreenToViewportPoint(Input.mousePosition);
                    if (mouseMoved)
                        dragHandler.DragMoved();
                    break;

                case InputState.AddingElement:
                    //Apply current adding state
                    furthestHit = GetFurthestHit(true);
                    if (furthestHit == null)
                        break;
                    furthestPoint = furthestHit.Value.point;
                    lastHitPosition = furthestPoint;

                    if (mouseMoved)
                        addElementsHandler.AddingMoved(furthestPoint);
                    break;
            }

            MoveCameraTo(cameraTransform.position +
                         cameraTransform.forward * (KeyMoveFactor * Time.unscaledDeltaTime * directionInput.y) +
                         cameraTransform.right * (KeyMoveFactor * Time.unscaledDeltaTime * directionInput.x));

            if (CameraMode == CameraModeType.Free && !EventSystem.current.IsPointerOverGameObject() &&
                IsMouseOverGameWindow)
            {
                if (rightMouseButtonPressed)
                {
                    rotationLook += Input.GetAxis("Mouse X") * RotationFactor * xRotationInversion;
                    rotationTilt += Input.GetAxis("Mouse Y") * RotationFactor * yRotationInversion;
                    rotationTilt = Mathf.Clamp(rotationTilt, -90, 90);
                    cameraTransform.rotation = Quaternion.Euler(rotationTilt, rotationLook, 0f);
                    rotationDirty = true;
                }
                else if (rotationDirty)
                {
                    PlayerPrefs.SetFloat(RotationLookKey, rotationLook);
                    PlayerPrefs.SetFloat(RotationTiltKey, rotationTilt);
                    rotationDirty = false;
                }
            }
        }

        /// <summary>
        /// Move camera to the clamped position of the current map
        /// </summary>
        /// <param name="position">Requested camera position</param>
        private void MoveCameraTo(Vector3 position)
        {
            var mapBounds = ScenarioManager.Instance.MapManager.CurrentMapBounds;
            position.x = Mathf.Clamp(position.x, mapBounds.min.x, mapBounds.max.x);
            position.y = Mathf.Clamp(position.y, 5.0f, 200.0f);
            position.z = Mathf.Clamp(position.z, mapBounds.min.z, mapBounds.max.z);
            scenarioCamera.transform.position = position;
        }

        /// <summary>
        /// Requests dragging the element, can be ignored if <see cref="InputManager"/> is not idle
        /// </summary>
        /// <param name="dragHandler">Drag handler that will be dragged</param>
        /// <param name="canFinishOverUI">Can the drag event finish over UI elements, cancel is called if it's not allowed</param>
        /// <param name="cancelMode">Current cancel mode selected for the operation</param>
        public void StartDraggingElement(IDragHandler dragHandler, bool canFinishOverUI = false,
            CancelMode cancelMode = CancelMode.CancelOnRelease)
        {
            if (inputState != InputState.Idle) return;
            canDragFinishOverUI = canFinishOverUI;
            this.cancelMode = cancelMode;
            inputState = InputState.DraggingElement;
            this.dragHandler = dragHandler;
            RaycastAll();
            var furthestHit = GetFurthestHit();
            MouseRaycastPosition = furthestHit?.point ?? Vector3.zero;
            MouseViewportPosition = scenarioCamera.ScreenToViewportPoint(Input.mousePosition);
            this.dragHandler.DragStarted();
        }

        /// <summary>
        /// Requests to cancel dragging the element, ignored if requested element is not dragged
        /// </summary>
        /// <param name="dragHandler">Drag handler which dragging will be canceled</param>
        public void CancelDraggingElement(IDragHandler dragHandler)
        {
            if (this.dragHandler != dragHandler)
            {
                Debug.LogWarning("Cannot cancel dragging as passed element is currently not handled.");
                return;
            }

            RaycastAll();
            var furthestHit = GetFurthestHit();
            MouseRaycastPosition = furthestHit?.point ?? Vector3.zero;
            this.dragHandler.DragCancelled();
            this.dragHandler = null;
            inputState = InputState.Idle;
        }

        /// <summary>
        /// Requests adding new elements by the handler, can be ignored if <see cref="InputManager"/> is not idle
        /// </summary>
        /// <param name="addElementsHandler">Add elements handler that will be begin adding</param>
        public bool StartAddingElements(IAddElementsHandler addElementsHandler,
            CancelMode cancelMode = CancelMode.CancelOnClick)
        {
            if (inputState != InputState.Idle) return false;
            this.cancelMode = cancelMode;
            inputState = InputState.AddingElement;
            this.addElementsHandler = addElementsHandler;
            RaycastAll();
            var furthestHit = GetFurthestHit();
            this.addElementsHandler.AddingStarted(furthestHit?.point ?? Vector3.zero);
            return true;
        }

        /// <summary>
        /// Requests to cancel adding elements, ignored if requested element is not actively adding 
        /// </summary>
        /// <param name="addElementsHandler">Add handler which adding will be canceled</param>
        public bool CancelAddingElements(IAddElementsHandler addElementsHandler)
        {
            if (this.addElementsHandler != addElementsHandler)
            {
                Debug.LogError("Cannot cancel adding elements as passed element is currently not handled.");
                return false;
            }

            RaycastAll();
            var furthestHit = GetFurthestHit();
            this.addElementsHandler.AddingCancelled(furthestHit?.point ?? Vector3.zero);
            this.addElementsHandler = null;
            inputState = InputState.Idle;
            return true;
        }

        /// <summary>
        /// Forces new position and rotation for the scenario camera, and saves the changes
        /// </summary>
        /// <param name="position">New camera position</param>
        public void ForceCameraReposition(Vector3 position)
        {
            MoveCameraTo(position);
        }
    }
}