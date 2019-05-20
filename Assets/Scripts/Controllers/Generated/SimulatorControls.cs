// GENERATED AUTOMATICALLY FROM 'Assets/Scripts/Controllers/SimulatorControls.inputactions'

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class SimulatorControls : IInputActionCollection
{
    private InputActionAsset asset;
    public SimulatorControls()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""SimulatorControls"",
    ""maps"": [
        {
            ""name"": ""Vehicle"",
            ""id"": ""eb4888fe-9130-4adc-a90b-bf846f8245e1"",
            ""actions"": [
                {
                    ""name"": ""Direction"",
                    ""id"": ""74a3751d-7b46-43ea-8db5-6ff3c1a8a03d"",
                    ""expectedControlLayout"": """",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                }
            ],
            ""bindings"": [
                {
                    ""name"": ""Move"",
                    ""id"": ""3a85ca28-408b-4166-a1fe-a08853ec7fd7"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Direction"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""up"",
                    ""id"": ""bf5f2a5b-0f73-4539-8504-8901399eeec7"",
                    ""path"": ""<Keyboard>/upArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Direction"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""down"",
                    ""id"": ""94c4c2ae-d7f7-44f3-8555-a0f9f25186e2"",
                    ""path"": ""<Keyboard>/downArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Direction"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""left"",
                    ""id"": ""3cff98fc-c704-4924-aa42-1adcb6f34edd"",
                    ""path"": ""<Keyboard>/leftArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Direction"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""right"",
                    ""id"": ""b93c6619-26b6-4873-9400-d7a5df6e5272"",
                    ""path"": ""<Keyboard>/rightArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Direction"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                }
            ]
        },
        {
            ""name"": ""Camera"",
            ""id"": ""c0640561-3291-49a8-99e5-12c8002fb650"",
            ""actions"": [
                {
                    ""name"": ""Direction"",
                    ""id"": ""68acaa71-e07d-420b-b1d6-a76c02a93b0f"",
                    ""expectedControlLayout"": """",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""Elevation"",
                    ""id"": ""0dad18b8-ab6f-4e94-9476-4b816f14eb47"",
                    ""expectedControlLayout"": """",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""MouseDelta"",
                    ""id"": ""cfde6f4e-1da5-42e8-b6b8-0749b7ff43d9"",
                    ""expectedControlLayout"": ""Vector2"",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""MouseLeft"",
                    ""id"": ""18683cc6-4aa4-44ca-9229-acb759267a4b"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": ""Press(behavior=2)"",
                    ""bindings"": []
                },
                {
                    ""name"": ""MouseRight"",
                    ""id"": ""7ed9f395-05c0-499c-8d3c-0b67a69b4939"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": ""Press(behavior=2)"",
                    ""bindings"": []
                },
                {
                    ""name"": ""MouseMiddle"",
                    ""id"": ""55cc01e4-af34-4fa5-abe3-de3dbc8ef0cb"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": ""Press(behavior=2)"",
                    ""bindings"": []
                },
                {
                    ""name"": ""MouseScroll"",
                    ""id"": ""af4a22ec-1b98-4220-88d1-d85e437099c0"",
                    ""expectedControlLayout"": ""Vector2"",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""MousePosition"",
                    ""id"": ""97be806d-6ef9-4b27-9230-578ccb58996a"",
                    ""expectedControlLayout"": ""Vector2"",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""Boost"",
                    ""id"": ""51845660-a7e4-4312-ab4f-e8bbe45c237d"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                }
            ],
            ""bindings"": [
                {
                    ""name"": ""2D Vector"",
                    ""id"": ""4170f5f9-b8dc-4356-a7d0-f1f00df2dd92"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Direction"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""up"",
                    ""id"": ""2f6b35d7-9eaa-4e67-bd50-5d459f41b874"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Direction"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""down"",
                    ""id"": ""3afeb758-9f75-474e-88ce-c72da6b85c2a"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Direction"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""left"",
                    ""id"": ""00e03210-b2ec-45c6-b3c0-e0133dc021cf"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Direction"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""right"",
                    ""id"": ""d327fe61-f83e-4da4-b093-f517fda042e8"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Direction"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""1D Axis"",
                    ""id"": ""57966831-2e24-4aff-9c30-3f4596b0ce7e"",
                    ""path"": ""1DAxis"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Elevation"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""negative"",
                    ""id"": ""2561c70b-915b-4734-a97e-e2a8f14783ee"",
                    ""path"": ""<Keyboard>/q"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Elevation"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""positive"",
                    ""id"": ""60c998c2-5783-4f0c-aa26-4c14531bdac5"",
                    ""path"": ""<Keyboard>/e"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Elevation"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""a3f3910d-b2dc-439a-b852-f88cb6ccda13"",
                    ""path"": ""<Mouse>/delta"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MouseDelta"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""996bb14a-7db2-479d-b701-4d65ae0e48c1"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MouseLeft"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""e8305f6c-b07a-4547-a50b-1cb5305b7f0d"",
                    ""path"": ""<Mouse>/rightButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MouseRight"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""5a34f929-3e06-421c-8ea8-ddb61c753b59"",
                    ""path"": ""<Mouse>/middleButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MouseMiddle"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""58cf2bdf-e3d0-46c9-8b93-9a2b4e2dad01"",
                    ""path"": ""<Mouse>/scroll"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MouseScroll"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""15d26a75-4c3e-4a98-8b81-dcd7e5e25aeb"",
                    ""path"": ""<Mouse>/position"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MousePosition"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""f62bf67a-00cb-422a-b8cb-fd78a11d76eb"",
                    ""path"": ""<Keyboard>/leftShift"",
                    ""interactions"": ""Press(behavior=2)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Boost"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
        // Vehicle
        m_Vehicle = asset.GetActionMap("Vehicle");
        m_Vehicle_Direction = m_Vehicle.GetAction("Direction");
        // Camera
        m_Camera = asset.GetActionMap("Camera");
        m_Camera_Direction = m_Camera.GetAction("Direction");
        m_Camera_Elevation = m_Camera.GetAction("Elevation");
        m_Camera_MouseDelta = m_Camera.GetAction("MouseDelta");
        m_Camera_MouseLeft = m_Camera.GetAction("MouseLeft");
        m_Camera_MouseRight = m_Camera.GetAction("MouseRight");
        m_Camera_MouseMiddle = m_Camera.GetAction("MouseMiddle");
        m_Camera_MouseScroll = m_Camera.GetAction("MouseScroll");
        m_Camera_MousePosition = m_Camera.GetAction("MousePosition");
        m_Camera_Boost = m_Camera.GetAction("Boost");
    }

    ~SimulatorControls()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes
    {
        get => asset.controlSchemes;
    }

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }

    // Vehicle
    private InputActionMap m_Vehicle;
    private IVehicleActions m_VehicleActionsCallbackInterface;
    private InputAction m_Vehicle_Direction;
    public struct VehicleActions
    {
        private SimulatorControls m_Wrapper;
        public VehicleActions(SimulatorControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @Direction { get { return m_Wrapper.m_Vehicle_Direction; } }
        public InputActionMap Get() { return m_Wrapper.m_Vehicle; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled { get { return Get().enabled; } }
        public InputActionMap Clone() { return Get().Clone(); }
        public static implicit operator InputActionMap(VehicleActions set) { return set.Get(); }
        public void SetCallbacks(IVehicleActions instance)
        {
            if (m_Wrapper.m_VehicleActionsCallbackInterface != null)
            {
                Direction.started -= m_Wrapper.m_VehicleActionsCallbackInterface.OnDirection;
                Direction.performed -= m_Wrapper.m_VehicleActionsCallbackInterface.OnDirection;
                Direction.canceled -= m_Wrapper.m_VehicleActionsCallbackInterface.OnDirection;
            }
            m_Wrapper.m_VehicleActionsCallbackInterface = instance;
            if (instance != null)
            {
                Direction.started += instance.OnDirection;
                Direction.performed += instance.OnDirection;
                Direction.canceled += instance.OnDirection;
            }
        }
    }
    public VehicleActions @Vehicle
    {
        get
        {
            return new VehicleActions(this);
        }
    }

    // Camera
    private InputActionMap m_Camera;
    private ICameraActions m_CameraActionsCallbackInterface;
    private InputAction m_Camera_Direction;
    private InputAction m_Camera_Elevation;
    private InputAction m_Camera_MouseDelta;
    private InputAction m_Camera_MouseLeft;
    private InputAction m_Camera_MouseRight;
    private InputAction m_Camera_MouseMiddle;
    private InputAction m_Camera_MouseScroll;
    private InputAction m_Camera_MousePosition;
    private InputAction m_Camera_Boost;
    public struct CameraActions
    {
        private SimulatorControls m_Wrapper;
        public CameraActions(SimulatorControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @Direction { get { return m_Wrapper.m_Camera_Direction; } }
        public InputAction @Elevation { get { return m_Wrapper.m_Camera_Elevation; } }
        public InputAction @MouseDelta { get { return m_Wrapper.m_Camera_MouseDelta; } }
        public InputAction @MouseLeft { get { return m_Wrapper.m_Camera_MouseLeft; } }
        public InputAction @MouseRight { get { return m_Wrapper.m_Camera_MouseRight; } }
        public InputAction @MouseMiddle { get { return m_Wrapper.m_Camera_MouseMiddle; } }
        public InputAction @MouseScroll { get { return m_Wrapper.m_Camera_MouseScroll; } }
        public InputAction @MousePosition { get { return m_Wrapper.m_Camera_MousePosition; } }
        public InputAction @Boost { get { return m_Wrapper.m_Camera_Boost; } }
        public InputActionMap Get() { return m_Wrapper.m_Camera; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled { get { return Get().enabled; } }
        public InputActionMap Clone() { return Get().Clone(); }
        public static implicit operator InputActionMap(CameraActions set) { return set.Get(); }
        public void SetCallbacks(ICameraActions instance)
        {
            if (m_Wrapper.m_CameraActionsCallbackInterface != null)
            {
                Direction.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnDirection;
                Direction.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnDirection;
                Direction.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnDirection;
                Elevation.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnElevation;
                Elevation.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnElevation;
                Elevation.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnElevation;
                MouseDelta.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnMouseDelta;
                MouseDelta.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnMouseDelta;
                MouseDelta.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnMouseDelta;
                MouseLeft.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnMouseLeft;
                MouseLeft.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnMouseLeft;
                MouseLeft.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnMouseLeft;
                MouseRight.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnMouseRight;
                MouseRight.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnMouseRight;
                MouseRight.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnMouseRight;
                MouseMiddle.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnMouseMiddle;
                MouseMiddle.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnMouseMiddle;
                MouseMiddle.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnMouseMiddle;
                MouseScroll.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnMouseScroll;
                MouseScroll.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnMouseScroll;
                MouseScroll.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnMouseScroll;
                MousePosition.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnMousePosition;
                MousePosition.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnMousePosition;
                MousePosition.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnMousePosition;
                Boost.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnBoost;
                Boost.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnBoost;
                Boost.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnBoost;
            }
            m_Wrapper.m_CameraActionsCallbackInterface = instance;
            if (instance != null)
            {
                Direction.started += instance.OnDirection;
                Direction.performed += instance.OnDirection;
                Direction.canceled += instance.OnDirection;
                Elevation.started += instance.OnElevation;
                Elevation.performed += instance.OnElevation;
                Elevation.canceled += instance.OnElevation;
                MouseDelta.started += instance.OnMouseDelta;
                MouseDelta.performed += instance.OnMouseDelta;
                MouseDelta.canceled += instance.OnMouseDelta;
                MouseLeft.started += instance.OnMouseLeft;
                MouseLeft.performed += instance.OnMouseLeft;
                MouseLeft.canceled += instance.OnMouseLeft;
                MouseRight.started += instance.OnMouseRight;
                MouseRight.performed += instance.OnMouseRight;
                MouseRight.canceled += instance.OnMouseRight;
                MouseMiddle.started += instance.OnMouseMiddle;
                MouseMiddle.performed += instance.OnMouseMiddle;
                MouseMiddle.canceled += instance.OnMouseMiddle;
                MouseScroll.started += instance.OnMouseScroll;
                MouseScroll.performed += instance.OnMouseScroll;
                MouseScroll.canceled += instance.OnMouseScroll;
                MousePosition.started += instance.OnMousePosition;
                MousePosition.performed += instance.OnMousePosition;
                MousePosition.canceled += instance.OnMousePosition;
                Boost.started += instance.OnBoost;
                Boost.performed += instance.OnBoost;
                Boost.canceled += instance.OnBoost;
            }
        }
    }
    public CameraActions @Camera
    {
        get
        {
            return new CameraActions(this);
        }
    }
    public interface IVehicleActions
    {
        void OnDirection(InputAction.CallbackContext context);
    }
    public interface ICameraActions
    {
        void OnDirection(InputAction.CallbackContext context);
        void OnElevation(InputAction.CallbackContext context);
        void OnMouseDelta(InputAction.CallbackContext context);
        void OnMouseLeft(InputAction.CallbackContext context);
        void OnMouseRight(InputAction.CallbackContext context);
        void OnMouseMiddle(InputAction.CallbackContext context);
        void OnMouseScroll(InputAction.CallbackContext context);
        void OnMousePosition(InputAction.CallbackContext context);
        void OnBoost(InputAction.CallbackContext context);
    }
}
