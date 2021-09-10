/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Input
{
    using System;
    using System.Threading.Tasks;
    using Agents;
    using Elements;
    using Managers;
    using Network.Core.Threading;
    using UI.Inspector;
    using Undo;
    using Undo.Records;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.InputSystem;

    /// <summary>
    /// Input manager that handles all the keyboard and mouse inputs in the Scenario Editor
    /// </summary>
    public class InputManager : MonoBehaviour, IScenarioEditorExtension
    {
        /// <summary>
        /// Data used for cursor setup
        /// </summary>
        [Serializable]
        public struct CursorData
        {
            /// <summary>
            /// Texture used for this cursor
            /// </summary>
            public Texture2D texture;

            /// <summary>
            /// Mode applied while using this cursor
            /// </summary>
            public CursorMode cursorMode;

            /// <summary>
            /// Position of the cursor on the texture
            /// </summary>
            public Vector2 hotSpot;
        }

        /// <summary>
        /// Input mode type that determines input behaviour
        /// </summary>
        private enum InputModeType
        {
            /// <summary>
            /// Input is idle and allow basic map movement and element selecting
            /// </summary>
            Idle,

            /// <summary>
            /// Input allows dragging element and limited camera movement
            /// </summary>
            DraggingElement,

            /// <summary>
            /// Input allows adding element and limited camera movement
            /// </summary>
            AddingElement,

            /// <summary>
            /// Input allows marking elements that are already in the scenario
            /// </summary>
            MarkingElements
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

        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Cursor settings used while input is marking elements
        /// </summary>
        [SerializeField]
        private CursorData markingCursor;
#pragma warning restore 0649

        /// <inheritdoc/>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Cached scenario world camera
        /// </summary>
        private Camera scenarioCamera;

        /// <summary>
        /// Cached rect transform of the scenario editor inspector
        /// </summary>
        private RectTransform inspectorTransform;

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
        /// Mouse axis delta in this frame
        /// </summary>
        private Vector3 mouseAxisDelta;

        /// <summary>
        /// Mouse axis in the last frame
        /// </summary>
        private Vector2 lastMouseAxis;

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
        /// Starting position of the camera movement event
        /// </summary>
        private Vector3? cameraMoveStart;

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
        /// Current input mode type determining input behaviour
        /// </summary>
        private InputModeType mode;

        /// <summary>
        /// Can the drag event finish over UI elements, cancel is called if it's not allowed
        /// </summary>
        private bool canDragFinishOverInspector;

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
        /// Cached currently managed mark elements handler
        /// </summary>
        private IMarkElementsHandler markElementsHandler;

        /// <summary>
        /// Inputs semaphore that allows disabling all the keyboard and mouse processing
        /// </summary>
        public LockingSemaphore InputSemaphore { get; } = new LockingSemaphore();

        /// <summary>
        /// Semaphore that allows disabling selecting scenario elements
        /// </summary>
        public LockingSemaphore ElementSelectingSemaphore { get; } = new LockingSemaphore();

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
                        rotationTilt = 90.0f;
                        break;
                    case CameraModeType.Leaned45:
                        rotationTilt = 45.0f;
                        break;
                    case CameraModeType.Free:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                scenarioCamera.transform.rotation = Quaternion.Euler(rotationTilt, rotationLook, 0f);
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
        /// Cached scenario world camera
        /// </summary>
        public Camera ScenarioCamera => scenarioCamera;

        /// <summary>
        /// Layer mask used when raycasting world
        /// </summary>
        public int RaycastLayerMask => raycastLayerMask;

        /// <summary>
        /// Maximum raycast distance
        /// </summary>
        public float RaycastDistance => raycastDistance;

        /// <summary>
        /// Current input state type determining input behaviour
        /// </summary>
        private InputModeType Mode
        {
            get => mode;
            set
            {
                mode = value;
                switch (mode)
                {
                    case InputModeType.Idle:
                        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                        break;
                    case InputModeType.DraggingElement:
                        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                        break;
                    case InputModeType.AddingElement:
                        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                        break;
                    case InputModeType.MarkingElements:
                        Cursor.SetCursor(markingCursor.texture, markingCursor.hotSpot, markingCursor.cursorMode);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <inheritdoc/>
        public Task Initialize()
        {
            if (IsInitialized)
                return Task.CompletedTask;
            scenarioCamera = ScenarioManager.Instance.ScenarioCamera;
            if (scenarioCamera == null)
                throw new ArgumentException("Scenario camera reference is required in the ScenarioManager.");
            inspectorTransform = FindObjectOfType<Inspector>().transform as RectTransform;
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
            IsInitialized = true;
            Debug.Log($"{GetType().Name} scenario editor extension has been initialized.");
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Deinitialize()
        {
            if (!IsInitialized)
                return;
            DeinitControls();
            IsInitialized = false;
            Debug.Log($"{GetType().Name} scenario editor extension has been deinitialized.");
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

        #region Input System Events

        /// <summary>
        /// Callback on camera mouse scroll
        /// </summary>
        /// <param name="obj">Callback context</param>
        private void MouseScrollOnPerformed(InputAction.CallbackContext obj)
        {
            if (ScenarioManager.Instance.State != ScenarioManager.InitializationState.Initialized)
                return;
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
            if (ScenarioManager.Instance.State != ScenarioManager.InitializationState.Initialized)
                return;
            leftMouseButtonPressed = obj.ReadValue<float>() > 0.0f;

            RaycastHit? closestHit;
            switch (Mode)
            {
                case InputModeType.Idle:
                    if (leftMouseButtonPressed && ElementSelectingSemaphore.IsUnlocked &&
                        !EventSystem.current.IsPointerOverGameObject())
                    {
                        closestHit = GetClosestHit();
                        if (closestHit == null)
                            return;

                        ScenarioElement element = null;
                        for (int i = 0; i < raycastHitsCount; i++)
                        {
                            element = raycastHits[i].collider.gameObject.GetComponentInParent<ScenarioElement>();
                            if (element == null) continue;

                            ScenarioManager.Instance.SelectedElement = element;
                            break;
                        }
                    }

                    break;
                case InputModeType.DraggingElement:
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
                        closestHit = GetClosestHit(ignoreSelectedElement: true, ignoreTriggers: true);
                        if (closestHit == null ||
                            (!canDragFinishOverInspector && EventSystem.current.IsPointerOverGameObject() &&
                             inspectorTransform.rect.Contains(
                                 inspectorTransform.InverseTransformPoint(Input.mousePosition))))
                        {
                            dragHandler.DragCancelled();
                        }
                        else
                        {
                            ScenarioManager.Instance.IsScenarioDirty = true;
                            dragHandler.DragFinished();
                        }

                        dragHandler = null;
                        Mode = InputModeType.Idle;
                    }

                    break;
                case InputModeType.AddingElement:
                    if (leftMouseButtonPressed && !EventSystem.current.IsPointerOverGameObject())
                    {
                        //Apply current adding state
                        closestHit = GetFurthestHit(true);
                        if (closestHit == null)
                            break;
                        var furthestPoint = closestHit.Value.point;
                        ScenarioManager.Instance.IsScenarioDirty = true;
                        addElementsHandler.AddElement(furthestPoint);
                    }

                    break;
                case InputModeType.MarkingElements:
                    if (leftMouseButtonPressed && ElementSelectingSemaphore.IsUnlocked &&
                        !EventSystem.current.IsPointerOverGameObject())
                    {
                        closestHit = GetFurthestHit();
                        if (closestHit == null)
                            return;

                        ScenarioElement element = null;
                        for (int i = 0; i < raycastHitsCount; i++)
                        {
                            element = raycastHits[i].collider.gameObject.GetComponentInParent<ScenarioElement>();
                            if (element != null) break;
                        }

                        markElementsHandler.MarkElement(element);
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
            if (ScenarioManager.Instance.State != ScenarioManager.InitializationState.Initialized)
                return;
            rightMouseButtonPressed = obj.ReadValue<float>() > 0.0f;

            switch (Mode)
            {
                case InputModeType.Idle:
                    if (rightMouseButtonPressed)
                        ScenarioManager.Instance.SelectedElement = null;
                    break;
                case InputModeType.DraggingElement:
                    //Right mouse button cancels action
                    CancelDraggingElement(dragHandler);
                    break;
                case InputModeType.AddingElement:
                    //Right mouse button cancels action
                    CancelAddingElements(addElementsHandler);
                    break;
                case InputModeType.MarkingElements:
                    //Right mouse button cancels action
                    CancelMarkingElements(markElementsHandler);
                    break;
            }
        }

        /// <summary>
        /// Callback on camera mouse middle
        /// </summary>
        /// <param name="obj">Callback context</param>
        private void MouseMiddleOnPerformed(InputAction.CallbackContext obj)
        {
            if (ScenarioManager.Instance.State != ScenarioManager.InitializationState.Initialized)
                return;
            middleMouseButtonPressed = obj.ReadValue<float>() > 0.0f;
            if (!middleMouseButtonPressed || EventSystem.current.IsPointerOverGameObject())
                cameraMoveStart = null;
            else
            {
                cameraMoveStart = GetFurthestHit(true)?.point;
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
            mouseAxisDelta = new Vector3(-(lastMouseAxis.x - Input.mousePosition.x) * 0.1f,
                -(lastMouseAxis.y - Input.mousePosition.y) * 0.1f, Input.GetAxis("Mouse ScrollWheel"));
            lastMouseAxis = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            RaycastAll();
            if (ElementSelectingSemaphore.IsUnlocked)
                HandleKeyboardActions();
            HandleMapInput();
        }

        /// <summary>
        /// Raycasts everything in the mouse position
        /// </summary>
        private void RaycastAll()
        {
            //TODO Check if raycast is needed
            mouseMoved = (lastMousePosition - Input.mousePosition).magnitude > 1.0f;
            var ray = scenarioCamera.ScreenPointToRay(Input.mousePosition);
            raycastHitsCount = RaycastAll(ray, raycastHits);
            lastMousePosition = Input.mousePosition;
        }

        /// <summary>
        /// Raycasts everything in the move position
        /// </summary>
        /// <param name="ray">Ray that will be used to determine the collision</param>
        /// <param name="hits">Prealocated raycast hits array</param>
        public int RaycastAll(Ray ray, RaycastHit[] hits)
        {
            return Physics.RaycastNonAlloc(ray, hits, raycastDistance, raycastLayerMask);
        }

        /// <summary>
        /// Raycasts everything in the move position
        /// </summary>
        /// <param name="ray">Ray that will be used to determine the collision</param>
        public RaycastHit[] RaycastAll(Ray ray)
        {
            return Physics.RaycastAll(ray, raycastDistance, raycastLayerMask);
        }

        /// <summary>
        /// Selects the furthest hit from the current raycast hits
        /// </summary>
        /// <param name="ignoreSelectedElement">Should the colliders inside selected element be ignored</param>
        /// <param name="ignoreRigidbodies">Should the colliders with rigidbodies be ignored</param>
        /// <param name="ignoreTriggers">Should the triggers be ignored</param>
        /// <returns>Furthest hit, null if there was no valid raycast hit</returns>
        private RaycastHit? GetFurthestHit(bool ignoreSelectedElement = false, bool ignoreRigidbodies = false,
            bool ignoreTriggers = false)
        {
            return GetFurthestHit(raycastHits, raycastHitsCount, ignoreSelectedElement, ignoreRigidbodies,
                ignoreTriggers);
        }

        /// <summary>
        /// Selects the furthest hit from the current raycast hits
        /// </summary>
        /// <param name="hits">Pre-allocated raycast hits array</param>
        /// <param name="hitsCount">Count of the valid raycast hits</param>
        /// <param name="ignoreSelectedElement">Should the colliders inside selected element be ignored</param>
        /// <param name="ignoreRigidbodies">Should the colliders with rigidbodies be ignored</param>
        /// <param name="ignoreTriggers">Should the triggers be ignored</param>
        /// <returns>Furthest hit, null if there was no valid raycast hit</returns>
        public RaycastHit? GetFurthestHit(RaycastHit[] hits, int hitsCount, bool ignoreSelectedElement = false,
            bool ignoreRigidbodies = false, bool ignoreTriggers = false)
        {
            RaycastHit? furthestHit = null;
            var furthestDistance = 0.0f;
            for (var i = 0; i < hitsCount; i++)
                if (hits[i].distance > furthestDistance &&
                    (!ignoreRigidbodies || hits[i].rigidbody == null) &&
                    (!ignoreSelectedElement || ScenarioManager.Instance.SelectedElement == null ||
                     hits[i].transform.GetComponentInParent<ScenarioElement>() !=
                     ScenarioManager.Instance.SelectedElement) &&
                    !(ignoreTriggers && hits[i].collider.isTrigger))
                {
                    furthestHit = hits[i];
                    furthestDistance = furthestHit.Value.distance;
                }

            return furthestHit;
        }

        /// <summary>
        /// Selects the closest hit from the current raycast hits
        /// </summary>
        /// <param name="ignoreSelectedElement">Should the colliders inside selected element be ignored</param>
        /// <param name="ignoreRigidbodies">Should the colliders with rigidbodies be ignored</param>
        /// <param name="ignoreTriggers">Should the triggers be ignored</param>
        /// <returns>Furthest hit, null if there was no valid raycast hit</returns>
        private RaycastHit? GetClosestHit(bool ignoreSelectedElement = false, bool ignoreRigidbodies = false,
            bool ignoreTriggers = false)
        {
            return GetClosestHit(raycastHits, raycastHitsCount, ignoreSelectedElement, ignoreRigidbodies,
                ignoreTriggers);
        }

        /// <summary>
        /// Selects the closest hit from the current raycast hits
        /// </summary>
        /// <param name="hits">Pre-allocated raycast hits array</param>
        /// <param name="hitsCount">Count of the valid raycast hits</param>
        /// <param name="ignoreSelectedElement">Should the colliders inside selected element be ignored</param>
        /// <param name="ignoreRigidbodies">Should the colliders with rigidbodies be ignored</param>
        /// <param name="ignoreTriggers">Should the triggers be ignored</param>
        /// <returns>Furthest hit, null if there was no valid raycast hit</returns>
        public RaycastHit? GetClosestHit(RaycastHit[] hits, int hitsCount, bool ignoreSelectedElement = false,
            bool ignoreRigidbodies = false, bool ignoreTriggers = false)
        {
            RaycastHit? closestHit = null;
            var closestDistance = float.MaxValue;
            for (var i = 0; i < hitsCount; i++)
                if (hits[i].distance < closestDistance &&
                    (!ignoreRigidbodies || hits[i].rigidbody == null) &&
                    (!ignoreSelectedElement || ScenarioManager.Instance.SelectedElement == null ||
                     hits[i].transform.GetComponentInParent<ScenarioElement>() !=
                     ScenarioManager.Instance.SelectedElement) &&
                    !(ignoreTriggers && hits[i].collider.isTrigger))
                {
                    closestHit = hits[i];
                    closestDistance = closestHit.Value.distance;
                }

            return closestHit;
        }

        /// <summary>
        /// Moves the scenario camera to the given scenario element, and selects the element
        /// </summary>
        /// <param name="scenarioElement">Scenario element which will be selected and centered</param>
        public void FocusOnScenarioElement(ScenarioElement scenarioElement)
        {
            var raycastHitsInCenter =
                RaycastAll(ScenarioCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0.5f)));
            if (raycastHitsInCenter.Length == 0)
                return;
            var furthestHit = GetClosestHit(raycastHitsInCenter, raycastHitsInCenter.Length, true, true, true);
            if (!furthestHit.HasValue)
                return;
            var cameraTransform = ScenarioCamera.transform;
            var offset = furthestHit.Value.point - cameraTransform.position;
            ForceCameraReposition(scenarioElement.transform.position - offset, cameraTransform.rotation.eulerAngles);
            ScenarioManager.Instance.SelectedElement = scenarioElement;
        }

        /// <summary>
        /// Handle keyboard actions
        /// </summary>
        private void HandleKeyboardActions()
        {
            if (Input.GetKeyDown(KeyCode.Delete) && EventSystem.current.currentSelectedGameObject == null)
            {
                var element = ScenarioManager.Instance.SelectedElement;
                if (element == null)
                    return;
                ScenarioManager.Instance.SelectedElement = null;
                ScenarioManager.Instance.IsScenarioDirty = true;
                ScenarioManager.Instance.GetExtension<ScenarioUndoManager>()
                    .RegisterRecord(new UndoRemoveElement(element));
                element.RemoveFromMap();
                return;
            }

            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    ScenarioManager.Instance.GetExtension<ScenarioUndoManager>().Undo();
                    return;
                }

                if (Input.GetKeyDown(KeyCode.C) &&
                    ScenarioManager.Instance.SelectedElement != null &&
                    ScenarioManager.Instance.SelectedElement.CanBeCopied)
                {
                    var element = ScenarioManager.Instance.SelectedElement;
                    ScenarioManager.Instance.CopyElement(element);
                    ScenarioManager.Instance.logPanel.EnqueueInfo($"Copied {element.name}.");
                    return;
                }

                if (Input.GetKeyDown(KeyCode.V))
                {
                    var hit = GetClosestHit(true);
                    if (hit.HasValue)
                        ScenarioManager.Instance.PlaceElementCopy(hit.Value.point);
                    return;
                }
            }
        }

        /// <summary>
        /// Main loop for the input management
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Invalid input state value</exception>
        private void HandleMapInput()
        {
            Transform cameraTransform = scenarioCamera.transform;
            var closestHit = GetClosestHit(ignoreSelectedElement: true, ignoreRigidbodies: true, ignoreTriggers: true);
            var furthestHit =
                GetFurthestHit(ignoreSelectedElement: true, ignoreRigidbodies: true, ignoreTriggers: true);
            Vector3? closestPoint = null;
            if (closestHit != null)
                closestPoint = closestHit.Value.point;

            // Move camera with the mouse and key inputs 
            if (cameraMoveStart != null && IsMouseOverGameWindow && mouseMoved && raycastHitsCount > 0 &&
                furthestHit != null)
            {
                var cameraPosition = cameraTransform.position;
                var deltaPosition = cameraMoveStart.Value - furthestHit.Value.point;
                deltaPosition.y = 0.0f;
                MoveCameraTo(cameraPosition + deltaPosition);
            }

            if (EventSystem.current.currentSelectedGameObject == null)
                MoveCameraTo(cameraTransform.position +
                             cameraTransform.forward * (KeyMoveFactor * Time.unscaledDeltaTime * directionInput.y) +
                             cameraTransform.right * (KeyMoveFactor * Time.unscaledDeltaTime * directionInput.x));

            // Check the element adding and dragging events
            switch (Mode)
            {
                case InputModeType.DraggingElement:
                    // Check if object was removed during the drag
                    if (dragHandler == null)
                    {
                        Mode = InputModeType.Idle;
                        break;
                    }

                    if (closestPoint == null)
                        break;
                    MouseRaycastPosition = closestPoint.Value;
                    MouseViewportPosition = scenarioCamera.ScreenToViewportPoint(Input.mousePosition);
                    if (mouseMoved)
                        dragHandler.DragMoved();
                    break;

                case InputModeType.AddingElement:
                    if (closestPoint == null)
                        break;
                    if (mouseMoved)
                        addElementsHandler.AddingMoved(closestPoint.Value);
                    break;
            }

            // Rotate the camera
            if (!EventSystem.current.IsPointerOverGameObject() && IsMouseOverGameWindow)
            {
                if (rightMouseButtonPressed)
                {
                    rotationLook += mouseAxisDelta.x * RotationFactor * xRotationInversion;
                    if (CameraMode == CameraModeType.Free)
                    {
                        rotationTilt += mouseAxisDelta.y * RotationFactor * yRotationInversion;
                        rotationTilt = Mathf.Clamp(rotationTilt, -90, 90);
                    }

                    cameraTransform.rotation = Quaternion.Euler(rotationTilt, rotationLook, 0f);
                    rotationDirty = true;
                }
                else if (rotationDirty)
                {
                    if (CameraMode == CameraModeType.Free)
                        PlayerPrefs.SetFloat(RotationLookKey, rotationLook);
                    PlayerPrefs.SetFloat(RotationTiltKey, rotationTilt);
                    rotationDirty = false;
                }
            }

            if (closestPoint != null)
                lastHitPosition = closestPoint.Value;
        }

        /// <summary>
        /// Move camera to the clamped position of the current map
        /// </summary>
        /// <param name="position">Requested camera position</param>
        private void MoveCameraTo(Vector3 position)
        {
            var mapExtension = ScenarioManager.Instance.GetExtension<ScenarioMapManager>();
            if (mapExtension == null)
                return;
            var mapBounds = mapExtension.CurrentMapBounds;
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
            if (Mode != InputModeType.Idle) return;
            canDragFinishOverInspector = canFinishOverUI;
            this.cancelMode = cancelMode;
            Mode = InputModeType.DraggingElement;
            this.dragHandler = dragHandler;
            RaycastAll();
            var closestHit = GetClosestHit(true);
            if (closestHit != null)
                MouseRaycastPosition = closestHit.Value.point;
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
            var closestHit = GetClosestHit(true);
            if (closestHit != null)
                MouseRaycastPosition = closestHit.Value.point;
            this.dragHandler.DragCancelled();
            this.dragHandler = null;
            Mode = InputModeType.Idle;
        }

        /// <summary>
        /// Requests adding new elements by the handler, can be ignored if <see cref="InputManager"/> is not idle
        /// </summary>
        /// <param name="addElementsHandler">Add elements handler that will add elements</param>
        public bool StartAddingElements(IAddElementsHandler addElementsHandler,
            CancelMode cancelMode = CancelMode.CancelOnClick)
        {
            if (Mode != InputModeType.Idle) return false;
            this.cancelMode = cancelMode;
            Mode = InputModeType.AddingElement;
            this.addElementsHandler = addElementsHandler;
            RaycastAll();
            var closestHit = GetClosestHit();
            this.addElementsHandler.AddingStarted(closestHit?.point ?? Vector3.zero);
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
                Debug.LogWarning("Cannot cancel adding elements as passed element is currently not handled.");
                return false;
            }

            RaycastAll();
            var closestHit = GetClosestHit();
            this.addElementsHandler.AddingCancelled(closestHit?.point ?? Vector3.zero);
            this.addElementsHandler = null;
            Mode = InputModeType.Idle;
            return true;
        }

        /// <summary>
        /// Requests marking scenario elements by the handler, can be ignored if <see cref="InputManager"/> is not idle
        /// </summary>
        /// <param name="markElementsHandler">Mark elements handler that will handle the marking</param>
        public bool StartMarkingElements(IMarkElementsHandler markElementsHandler,
            CancelMode cancelMode = CancelMode.CancelOnClick)
        {
            if (Mode != InputModeType.Idle) return false;
            this.cancelMode = cancelMode;
            Mode = InputModeType.MarkingElements;
            this.markElementsHandler = markElementsHandler;
            this.markElementsHandler.MarkingStarted();
            return true;
        }

        /// <summary>
        /// Requests to cancel marking elements, ignored if requested element is not actively adding 
        /// </summary>
        /// <param name="markElementsHandler">Mark elements handler which marking will be canceled</param>
        public bool CancelMarkingElements(IMarkElementsHandler markElementsHandler)
        {
            if (this.markElementsHandler != markElementsHandler)
            {
                Debug.LogWarning("Cannot cancel marking elements as passed element is currently not handled.");
                return false;
            }

            this.markElementsHandler.MarkingCancelled();
            this.markElementsHandler = null;
            Mode = InputModeType.Idle;
            return true;
        }

        /// <summary>
        /// Forces new position and rotation for the scenario camera, and saves the changes
        /// </summary>
        /// <param name="position">New camera position</param>
        /// <param name="rotation">New camera rotation</param>
        public void ForceCameraReposition(Vector3 position, Vector3 rotation)
        {
            MoveCameraTo(position);
            if (CameraMode == CameraModeType.Free)
            {
                rotationTilt = rotation.x;
                PlayerPrefs.SetFloat(RotationLookKey, rotationLook);
            }

            rotationLook = rotation.y;
            PlayerPrefs.SetFloat(RotationTiltKey, rotationTilt);
            scenarioCamera.transform.rotation = Quaternion.Euler(rotationTilt, rotationLook, 0f);
        }
    }
}