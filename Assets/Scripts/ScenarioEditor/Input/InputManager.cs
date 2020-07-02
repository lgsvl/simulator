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
            /// Input allows rotating element and limited camera movement
            /// </summary>
            RotatingElement,

            /// <summary>
            /// Input allows adding element and limited camera movement
            /// </summary>
            AddingElement
        }

        /// <summary>
        /// Zoom factor applied when using the mouse wheel zoom
        /// </summary>
        private const float ZoomFactor = 10.0f;

        /// <summary>
        /// Rotation factor applied when camera is rotated with mouse movement
        /// </summary>
        private const float RotationFactor = 3.0f;

        /// <summary>
        /// Movement factor applied when camera is moved by keyboard keys
        /// </summary>
        private const float KeyMoveFactor = 10.0f;

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
        /// Tilt value for the camera rotation
        /// </summary>
        private float rotationTilt;

        /// <summary>
        /// Look value for the camera rotation
        /// </summary>
        private float rotationLook;

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
        /// Cached currently managed rotate handler
        /// </summary>
        private IRotateHandler rotateHandler;

        /// <summary>
        /// Cached currently managed add elements handler
        /// </summary>
        private IAddElementsHandler addElementsHandler;

        /// <summary>
        /// Inputs semaphore that allows disabling all the keyboard and mouse processing
        /// </summary>
        public LockingSemaphore InputSemaphore { get; } = new LockingSemaphore();

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
            raycastDistance = scenarioCamera.farClipPlane - scenarioCamera.nearClipPlane;
            RecacheCameraRotation();
            var ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
            raycastLayerMask &= ~(1 << ignoreRaycastLayer);
        }

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
        /// Recaches the camera rotation, required when the camera object is moved by an external script
        /// </summary>
        public void RecacheCameraRotation()
        {
            var cameraEuler = scenarioCamera.transform.rotation.eulerAngles;
            rotationTilt = cameraEuler.x;
            rotationLook = cameraEuler.y;
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
            RaycastHit? furthestHit;
            Vector3 furthestPoint;
            switch (inputState)
            {
                case InputState.Idle:
                    if (Input.GetMouseButtonDown(0) &&
                        !EventSystem.current.IsPointerOverGameObject())
                    {
                        furthestHit = GetFurthestHit();
                        if (furthestHit == null)
                            return;

                        ScenarioElement element = null;
                        for (int i = 0; i < raycastHitsCount; i++)
                        {
                            element = raycastHits[i].collider.gameObject.GetComponentInParent<ScenarioElement>();
                            if (element == null) continue;

                            element.Selected();
                            ScenarioManager.Instance.SelectedElement = element;
                            //Override furthest hit with selected element hit
                            furthestHit = raycastHits[i];
                            break;
                        }

                        if (element == null)
                            inputState = InputState.MovingCamera;

                        lastHitPosition = furthestHit.Value.point;
                    }

                    if (Input.GetMouseButtonDown(1))
                        ScenarioManager.Instance.SelectedElement = null;

                    break;
                case InputState.MovingCamera:
                    if (Input.GetMouseButtonUp(0))
                    {
                        inputState = InputState.Idle;
                        break;
                    }

                    if (IsMouseOverGameWindow && mouseMoved && raycastHitsCount > 0)
                    {
                        furthestHit = GetFurthestHit(true);
                        if (furthestHit == null)
                            return;
                        var cameraTransform = scenarioCamera.transform;
                        var cameraPosition = cameraTransform.position;
                        var deltaPosition = lastHitPosition - furthestHit.Value.point;
                        deltaPosition.y = 0.0f;
                        MoveCameraTo(cameraPosition + deltaPosition);
                    }

                    break;
                case InputState.DraggingElement:
                    //Check if drag was canceled
                    if (Input.GetMouseButtonUp(1) || Input.GetMouseButtonDown(1))
                    {
                        dragHandler.DragCancelled(lastHitPosition);
                        dragHandler = null;
                        inputState = InputState.Idle;
                        break;
                    }
                    //Check for drag finish
                    if (Input.GetMouseButtonUp(0))
                    {
                        if (EventSystem.current.IsPointerOverGameObject())
                            dragHandler.DragCancelled(lastHitPosition);
                        else
                        {
                            ScenarioManager.Instance.IsScenarioDirty = true;
                            dragHandler.DragFinished(lastHitPosition);
                        }

                        dragHandler = null;
                        inputState = InputState.Idle;
                        break;
                    }
                    
                    //Apply current drag state
                    furthestHit = GetFurthestHit(true);
                    if (furthestHit == null)
                        break;
                    furthestPoint = furthestHit.Value.point;
                    lastHitPosition = furthestPoint;

                    if (mouseMoved)
                        dragHandler.DragMoved(furthestPoint);
                    break;

                case InputState.RotatingElement:
                    if (Input.GetMouseButtonUp(0))
                    {
                        ScenarioManager.Instance.IsScenarioDirty = true;
                        rotateHandler.RotationFinished(scenarioCamera.ScreenToViewportPoint(Input.mousePosition));
                        rotateHandler = null;
                        inputState = InputState.Idle;
                        break;
                    }

                    if (Input.GetMouseButtonUp(1) || Input.GetMouseButtonDown(1))
                    {
                        rotateHandler.RotationCancelled(scenarioCamera.ScreenToViewportPoint(Input.mousePosition));
                        rotateHandler = null;
                        inputState = InputState.Idle;
                        break;
                    }

                    if (mouseMoved)
                        rotateHandler.RotationChanged(scenarioCamera.ScreenToViewportPoint(Input.mousePosition));
                    break;

                case InputState.AddingElement:
                    //Check if adding was canceled
                    if (Input.GetMouseButtonUp(1) || Input.GetMouseButtonDown(1))
                    {
                        addElementsHandler.AddingCancelled(lastHitPosition);
                        inputState = InputState.Idle;
                        break;
                    }
                    
                    //Apply current adding state
                    furthestHit = GetFurthestHit(true);
                    if (furthestHit == null)
                        break;
                    furthestPoint = furthestHit.Value.point;
                    lastHitPosition = furthestPoint;
                    if (Input.GetMouseButtonDown(0))
                    {
                        ScenarioManager.Instance.IsScenarioDirty = true;
                        addElementsHandler.AddElement(furthestPoint);
                        break;
                    }

                    if (mouseMoved)
                        addElementsHandler.AddingMoved(furthestPoint);
                    break;
                default: throw new ArgumentOutOfRangeException();
            }

            //TODO advanced zoom and limit zooming
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                var cameraTransform = scenarioCamera.transform;
                if (Input.GetKey(KeyCode.UpArrow))
                    MoveCameraTo(cameraTransform.position +
                                 cameraTransform.forward * (KeyMoveFactor * Time.unscaledDeltaTime));
                if (Input.GetKey(KeyCode.DownArrow))
                    MoveCameraTo(cameraTransform.position -
                                 cameraTransform.forward * (KeyMoveFactor * Time.unscaledDeltaTime));
                if (Input.GetKey(KeyCode.RightArrow))
                    MoveCameraTo(cameraTransform.position +
                                 cameraTransform.right * (KeyMoveFactor * Time.unscaledDeltaTime));
                if (Input.GetKey(KeyCode.LeftArrow))
                    MoveCameraTo(cameraTransform.position -
                                 cameraTransform.right * (KeyMoveFactor * Time.unscaledDeltaTime));

                if (IsMouseOverGameWindow)
                {
                    var scrollValue = Input.GetAxis("Mouse ScrollWheel");
                    MoveCameraTo(cameraTransform.position + cameraTransform.forward * (scrollValue * ZoomFactor));

                    if (Input.GetMouseButton(1))
                    {
                        rotationLook += Input.GetAxis("Mouse X") * RotationFactor * xRotationInversion;
                        rotationTilt += Input.GetAxis("Mouse Y") * RotationFactor * yRotationInversion;
                        rotationTilt = Mathf.Clamp(rotationTilt, -90, 90);
                        cameraTransform.rotation = Quaternion.Euler(rotationTilt, rotationLook, 0f);
                    }
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
        public void StartDraggingElement(IDragHandler dragHandler)
        {
            if (inputState != InputState.Idle) return;
            inputState = InputState.DraggingElement;
            this.dragHandler = dragHandler;
            RaycastAll();
            var furthestHit = GetFurthestHit();
            this.dragHandler.DragStarted(furthestHit?.point ?? Vector3.zero);
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
            this.dragHandler.DragCancelled(furthestHit?.point ?? Vector3.zero);
            this.dragHandler = null;
            inputState = InputState.Idle;
        }

        /// <summary>
        /// Requests rotating the element, can be ignored if <see cref="InputManager"/> is not idle
        /// </summary>
        /// <param name="rotateHandler">Rotate handler that will be rotated</param>
        public void StartRotatingElement(IRotateHandler rotateHandler)
        {
            if (inputState != InputState.Idle) return;
            inputState = InputState.RotatingElement;
            this.rotateHandler = rotateHandler;
            this.rotateHandler.RotationStarted(scenarioCamera.ScreenToViewportPoint(Input.mousePosition));
        }

        /// <summary>
        /// Requests to cancel rotate the element, ignored if requested element is not rotated
        /// </summary>
        /// <param name="rotateHandler">Rotate handler which rotation will be canceled</param>
        public void CancelRotatingElement(IRotateHandler rotateHandler)
        {
            if (this.rotateHandler != rotateHandler)
            {
                Debug.LogWarning("Cannot cancel rotating as passed element is currently not handled.");
                return;
            }

            this.rotateHandler.RotationCancelled(scenarioCamera.ScreenToViewportPoint(Input.mousePosition));
            this.rotateHandler = null;
            inputState = InputState.Idle;
        }

        /// <summary>
        /// Requests adding new elements by the handler, can be ignored if <see cref="InputManager"/> is not idle
        /// </summary>
        /// <param name="addElementsHandler">Add elements handler that will be begin adding</param>
        public bool StartAddingElements(IAddElementsHandler addElementsHandler)
        {
            if (inputState != InputState.Idle) return false;
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
    }
}