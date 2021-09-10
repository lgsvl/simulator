// GENERATED AUTOMATICALLY FROM 'Assets/Scripts/Controllers/SimulatorControls.inputactions'

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class @SimulatorControls : IInputActionCollection, IDisposable
{
    public InputActionAsset asset { get; }
    public @SimulatorControls()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""SimulatorControls"",
    ""maps"": [
        {
            ""name"": ""Camera"",
            ""id"": ""c0640561-3291-49a8-99e5-12c8002fb650"",
            ""actions"": [
                {
                    ""name"": ""Direction"",
                    ""type"": ""Value"",
                    ""id"": ""68acaa71-e07d-420b-b1d6-a76c02a93b0f"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Elevation"",
                    ""type"": ""Value"",
                    ""id"": ""0dad18b8-ab6f-4e94-9476-4b816f14eb47"",
                    ""expectedControlType"": ""Axis"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""MouseDelta"",
                    ""type"": ""Value"",
                    ""id"": ""cfde6f4e-1da5-42e8-b6b8-0749b7ff43d9"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""MouseLeft"",
                    ""type"": ""Button"",
                    ""id"": ""18683cc6-4aa4-44ca-9229-acb759267a4b"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press(behavior=2)""
                },
                {
                    ""name"": ""MouseRight"",
                    ""type"": ""Button"",
                    ""id"": ""7ed9f395-05c0-499c-8d3c-0b67a69b4939"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press(behavior=2)""
                },
                {
                    ""name"": ""MouseMiddle"",
                    ""type"": ""Button"",
                    ""id"": ""55cc01e4-af34-4fa5-abe3-de3dbc8ef0cb"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press(behavior=2)""
                },
                {
                    ""name"": ""MouseScroll"",
                    ""type"": ""Value"",
                    ""id"": ""af4a22ec-1b98-4220-88d1-d85e437099c0"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""MousePosition"",
                    ""type"": ""Value"",
                    ""id"": ""97be806d-6ef9-4b27-9230-578ccb58996a"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Boost"",
                    ""type"": ""Button"",
                    ""id"": ""51845660-a7e4-4312-ab4f-e8bbe45c237d"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""ToggleState"",
                    ""type"": ""Button"",
                    ""id"": ""937f6067-9820-460d-99c5-e050ce658467"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Zoom"",
                    ""type"": ""Value"",
                    ""id"": ""1138b450-1c54-49a7-b808-8665b1326312"",
                    ""expectedControlType"": ""Axis"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""CinematicNewPath"",
                    ""type"": ""Button"",
                    ""id"": ""665b9005-f4bd-473b-9c46-4fd20642ac7d"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""CinematicResetPath"",
                    ""type"": ""Button"",
                    ""id"": ""2c242fd3-0465-42fe-9be4-7a4b71ba5221"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
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
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""2f6b35d7-9eaa-4e67-bd50-5d459f41b874"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": ""Hold,Press(behavior=2)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Direction"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""3afeb758-9f75-474e-88ce-c72da6b85c2a"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": ""Hold,Press(behavior=2)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Direction"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""00e03210-b2ec-45c6-b3c0-e0133dc021cf"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": ""Hold,Press(behavior=2)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Direction"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""d327fe61-f83e-4da4-b093-f517fda042e8"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": ""Hold,Press(behavior=2)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Direction"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
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
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""negative"",
                    ""id"": ""2561c70b-915b-4734-a97e-e2a8f14783ee"",
                    ""path"": ""<Keyboard>/e"",
                    ""interactions"": ""Hold,Press(behavior=2)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Elevation"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""positive"",
                    ""id"": ""60c998c2-5783-4f0c-aa26-4c14531bdac5"",
                    ""path"": ""<Keyboard>/q"",
                    ""interactions"": ""Hold,Press(behavior=2)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Elevation"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
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
                    ""isPartOfComposite"": false
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
                    ""isPartOfComposite"": false
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
                    ""isPartOfComposite"": false
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
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""58cf2bdf-e3d0-46c9-8b93-9a2b4e2dad01"",
                    ""path"": ""<Mouse>/scroll"",
                    ""interactions"": """",
                    ""processors"": ""NormalizeVector2"",
                    ""groups"": """",
                    ""action"": ""MouseScroll"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
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
                    ""isPartOfComposite"": false
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
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""bdea920d-94b5-472f-9b5a-7e803779bdbe"",
                    ""path"": ""<Keyboard>/backquote"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ToggleState"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""1D Axis"",
                    ""id"": ""69b301f7-1da0-458f-b576-b859b8e6c621"",
                    ""path"": ""1DAxis"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Zoom"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""negative"",
                    ""id"": ""71ae58a1-d333-4b1e-84a4-b6235ccc0274"",
                    ""path"": ""<Keyboard>/#(S)"",
                    ""interactions"": ""Hold,Press(behavior=2)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Zoom"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""positive"",
                    ""id"": ""b7b7eeee-ce7f-4272-bcf7-c2e5688c1fa2"",
                    ""path"": ""<Keyboard>/#(W)"",
                    ""interactions"": ""Hold,Press(behavior=2)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Zoom"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""12c41498-87d7-425f-968f-992e002859ea"",
                    ""path"": ""<Keyboard>/#(C)"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""CinematicNewPath"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""a7a9a91f-7fd6-4d86-8815-2efe6d2f2528"",
                    ""path"": ""<Keyboard>/#(V)"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""CinematicResetPath"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Simulator"",
            ""id"": ""04811f95-5e48-44b8-98ce-b3957e6df2cd"",
            ""actions"": [
                {
                    ""name"": ""ToggleNPCS"",
                    ""type"": ""Button"",
                    ""id"": ""c3de16aa-60a2-49a7-8a4d-51f32b875b63"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""ToggleAgent"",
                    ""type"": ""Button"",
                    ""id"": ""e2c98be3-db48-4767-9803-56e3a731d9e8"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""ToggleReset"",
                    ""type"": ""Button"",
                    ""id"": ""02a13f64-a6f0-4d2c-9a12-188a987f1ee7"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""ToggleControlsUI"",
                    ""type"": ""Button"",
                    ""id"": ""18aa8941-8b9f-4bfe-b14c-88d6e6641ba2"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""TogglePedestrians"",
                    ""type"": ""Button"",
                    ""id"": ""b39bbbcd-97f5-4b45-b155-e7374f654c05"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""4250e116-5443-4648-a14d-13bf03610f68"",
                    ""path"": ""<Keyboard>/#(N)"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ToggleNPCS"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""8ca59c40-2cbb-4ded-ab52-42ca749508ad"",
                    ""path"": ""<Keyboard>/#(1)"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ToggleAgent"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""1d4824af-64f9-4aab-838c-25b91e25c65c"",
                    ""path"": ""<Keyboard>/#(2)"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ToggleAgent"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""22c7acfd-2b2c-4e61-a1d7-857e39676d06"",
                    ""path"": ""<Keyboard>/#(3)"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ToggleAgent"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""9876ad5e-bc08-453e-9387-4bcacba2d8e2"",
                    ""path"": ""<Keyboard>/#(4)"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ToggleAgent"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""1a09b806-4b09-4efd-a90b-62a7094b4e66"",
                    ""path"": ""<Keyboard>/#(5)"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ToggleAgent"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""8f32d53c-cb31-400b-98bd-2ec169ee0dcc"",
                    ""path"": ""<Keyboard>/#(6)"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ToggleAgent"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""4aed9ed5-4ce5-4631-a3f3-40c09915c900"",
                    ""path"": ""<Keyboard>/#(7)"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ToggleAgent"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""48616910-8ed6-487a-b24b-37c0b7450845"",
                    ""path"": ""<Keyboard>/#(8)"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ToggleAgent"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""8fa9dca5-a786-4ea1-af89-2020f38fa7a5"",
                    ""path"": ""<Keyboard>/#(9)"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ToggleAgent"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""700504bd-58fc-427e-a362-f0e99844fcae"",
                    ""path"": ""<Keyboard>/#(0)"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ToggleAgent"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""40648d43-6029-42c0-96c2-6e4d36606851"",
                    ""path"": ""<Keyboard>/f12"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ToggleReset"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""d75a1e4c-2d5f-4bc4-b37d-64c97df9e597"",
                    ""path"": ""<Keyboard>/f1"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ToggleControlsUI"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""964ab0a7-980a-4c43-b777-8d20a97f4ae3"",
                    ""path"": ""<Keyboard>/#(P)"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""TogglePedestrians"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
        // Camera
        m_Camera = asset.FindActionMap("Camera", throwIfNotFound: true);
        m_Camera_Direction = m_Camera.FindAction("Direction", throwIfNotFound: true);
        m_Camera_Elevation = m_Camera.FindAction("Elevation", throwIfNotFound: true);
        m_Camera_MouseDelta = m_Camera.FindAction("MouseDelta", throwIfNotFound: true);
        m_Camera_MouseLeft = m_Camera.FindAction("MouseLeft", throwIfNotFound: true);
        m_Camera_MouseRight = m_Camera.FindAction("MouseRight", throwIfNotFound: true);
        m_Camera_MouseMiddle = m_Camera.FindAction("MouseMiddle", throwIfNotFound: true);
        m_Camera_MouseScroll = m_Camera.FindAction("MouseScroll", throwIfNotFound: true);
        m_Camera_MousePosition = m_Camera.FindAction("MousePosition", throwIfNotFound: true);
        m_Camera_Boost = m_Camera.FindAction("Boost", throwIfNotFound: true);
        m_Camera_ToggleState = m_Camera.FindAction("ToggleState", throwIfNotFound: true);
        m_Camera_Zoom = m_Camera.FindAction("Zoom", throwIfNotFound: true);
        m_Camera_CinematicNewPath = m_Camera.FindAction("CinematicNewPath", throwIfNotFound: true);
        m_Camera_CinematicResetPath = m_Camera.FindAction("CinematicResetPath", throwIfNotFound: true);
        // Simulator
        m_Simulator = asset.FindActionMap("Simulator", throwIfNotFound: true);
        m_Simulator_ToggleNPCS = m_Simulator.FindAction("ToggleNPCS", throwIfNotFound: true);
        m_Simulator_ToggleAgent = m_Simulator.FindAction("ToggleAgent", throwIfNotFound: true);
        m_Simulator_ToggleReset = m_Simulator.FindAction("ToggleReset", throwIfNotFound: true);
        m_Simulator_ToggleControlsUI = m_Simulator.FindAction("ToggleControlsUI", throwIfNotFound: true);
        m_Simulator_TogglePedestrians = m_Simulator.FindAction("TogglePedestrians", throwIfNotFound: true);
    }

    public void Dispose()
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

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

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

    // Camera
    private readonly InputActionMap m_Camera;
    private ICameraActions m_CameraActionsCallbackInterface;
    private readonly InputAction m_Camera_Direction;
    private readonly InputAction m_Camera_Elevation;
    private readonly InputAction m_Camera_MouseDelta;
    private readonly InputAction m_Camera_MouseLeft;
    private readonly InputAction m_Camera_MouseRight;
    private readonly InputAction m_Camera_MouseMiddle;
    private readonly InputAction m_Camera_MouseScroll;
    private readonly InputAction m_Camera_MousePosition;
    private readonly InputAction m_Camera_Boost;
    private readonly InputAction m_Camera_ToggleState;
    private readonly InputAction m_Camera_Zoom;
    private readonly InputAction m_Camera_CinematicNewPath;
    private readonly InputAction m_Camera_CinematicResetPath;
    public struct CameraActions
    {
        private @SimulatorControls m_Wrapper;
        public CameraActions(@SimulatorControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @Direction => m_Wrapper.m_Camera_Direction;
        public InputAction @Elevation => m_Wrapper.m_Camera_Elevation;
        public InputAction @MouseDelta => m_Wrapper.m_Camera_MouseDelta;
        public InputAction @MouseLeft => m_Wrapper.m_Camera_MouseLeft;
        public InputAction @MouseRight => m_Wrapper.m_Camera_MouseRight;
        public InputAction @MouseMiddle => m_Wrapper.m_Camera_MouseMiddle;
        public InputAction @MouseScroll => m_Wrapper.m_Camera_MouseScroll;
        public InputAction @MousePosition => m_Wrapper.m_Camera_MousePosition;
        public InputAction @Boost => m_Wrapper.m_Camera_Boost;
        public InputAction @ToggleState => m_Wrapper.m_Camera_ToggleState;
        public InputAction @Zoom => m_Wrapper.m_Camera_Zoom;
        public InputAction @CinematicNewPath => m_Wrapper.m_Camera_CinematicNewPath;
        public InputAction @CinematicResetPath => m_Wrapper.m_Camera_CinematicResetPath;
        public InputActionMap Get() { return m_Wrapper.m_Camera; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(CameraActions set) { return set.Get(); }
        public void SetCallbacks(ICameraActions instance)
        {
            if (m_Wrapper.m_CameraActionsCallbackInterface != null)
            {
                @Direction.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnDirection;
                @Direction.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnDirection;
                @Direction.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnDirection;
                @Elevation.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnElevation;
                @Elevation.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnElevation;
                @Elevation.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnElevation;
                @MouseDelta.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnMouseDelta;
                @MouseDelta.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnMouseDelta;
                @MouseDelta.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnMouseDelta;
                @MouseLeft.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnMouseLeft;
                @MouseLeft.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnMouseLeft;
                @MouseLeft.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnMouseLeft;
                @MouseRight.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnMouseRight;
                @MouseRight.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnMouseRight;
                @MouseRight.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnMouseRight;
                @MouseMiddle.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnMouseMiddle;
                @MouseMiddle.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnMouseMiddle;
                @MouseMiddle.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnMouseMiddle;
                @MouseScroll.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnMouseScroll;
                @MouseScroll.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnMouseScroll;
                @MouseScroll.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnMouseScroll;
                @MousePosition.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnMousePosition;
                @MousePosition.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnMousePosition;
                @MousePosition.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnMousePosition;
                @Boost.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnBoost;
                @Boost.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnBoost;
                @Boost.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnBoost;
                @ToggleState.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnToggleState;
                @ToggleState.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnToggleState;
                @ToggleState.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnToggleState;
                @Zoom.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnZoom;
                @Zoom.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnZoom;
                @Zoom.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnZoom;
                @CinematicNewPath.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnCinematicNewPath;
                @CinematicNewPath.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnCinematicNewPath;
                @CinematicNewPath.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnCinematicNewPath;
                @CinematicResetPath.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnCinematicResetPath;
                @CinematicResetPath.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnCinematicResetPath;
                @CinematicResetPath.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnCinematicResetPath;
            }
            m_Wrapper.m_CameraActionsCallbackInterface = instance;
            if (instance != null)
            {
                @Direction.started += instance.OnDirection;
                @Direction.performed += instance.OnDirection;
                @Direction.canceled += instance.OnDirection;
                @Elevation.started += instance.OnElevation;
                @Elevation.performed += instance.OnElevation;
                @Elevation.canceled += instance.OnElevation;
                @MouseDelta.started += instance.OnMouseDelta;
                @MouseDelta.performed += instance.OnMouseDelta;
                @MouseDelta.canceled += instance.OnMouseDelta;
                @MouseLeft.started += instance.OnMouseLeft;
                @MouseLeft.performed += instance.OnMouseLeft;
                @MouseLeft.canceled += instance.OnMouseLeft;
                @MouseRight.started += instance.OnMouseRight;
                @MouseRight.performed += instance.OnMouseRight;
                @MouseRight.canceled += instance.OnMouseRight;
                @MouseMiddle.started += instance.OnMouseMiddle;
                @MouseMiddle.performed += instance.OnMouseMiddle;
                @MouseMiddle.canceled += instance.OnMouseMiddle;
                @MouseScroll.started += instance.OnMouseScroll;
                @MouseScroll.performed += instance.OnMouseScroll;
                @MouseScroll.canceled += instance.OnMouseScroll;
                @MousePosition.started += instance.OnMousePosition;
                @MousePosition.performed += instance.OnMousePosition;
                @MousePosition.canceled += instance.OnMousePosition;
                @Boost.started += instance.OnBoost;
                @Boost.performed += instance.OnBoost;
                @Boost.canceled += instance.OnBoost;
                @ToggleState.started += instance.OnToggleState;
                @ToggleState.performed += instance.OnToggleState;
                @ToggleState.canceled += instance.OnToggleState;
                @Zoom.started += instance.OnZoom;
                @Zoom.performed += instance.OnZoom;
                @Zoom.canceled += instance.OnZoom;
                @CinematicNewPath.started += instance.OnCinematicNewPath;
                @CinematicNewPath.performed += instance.OnCinematicNewPath;
                @CinematicNewPath.canceled += instance.OnCinematicNewPath;
                @CinematicResetPath.started += instance.OnCinematicResetPath;
                @CinematicResetPath.performed += instance.OnCinematicResetPath;
                @CinematicResetPath.canceled += instance.OnCinematicResetPath;
            }
        }
    }
    public CameraActions @Camera => new CameraActions(this);

    // Simulator
    private readonly InputActionMap m_Simulator;
    private ISimulatorActions m_SimulatorActionsCallbackInterface;
    private readonly InputAction m_Simulator_ToggleNPCS;
    private readonly InputAction m_Simulator_ToggleAgent;
    private readonly InputAction m_Simulator_ToggleReset;
    private readonly InputAction m_Simulator_ToggleControlsUI;
    private readonly InputAction m_Simulator_TogglePedestrians;
    public struct SimulatorActions
    {
        private @SimulatorControls m_Wrapper;
        public SimulatorActions(@SimulatorControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @ToggleNPCS => m_Wrapper.m_Simulator_ToggleNPCS;
        public InputAction @ToggleAgent => m_Wrapper.m_Simulator_ToggleAgent;
        public InputAction @ToggleReset => m_Wrapper.m_Simulator_ToggleReset;
        public InputAction @ToggleControlsUI => m_Wrapper.m_Simulator_ToggleControlsUI;
        public InputAction @TogglePedestrians => m_Wrapper.m_Simulator_TogglePedestrians;
        public InputActionMap Get() { return m_Wrapper.m_Simulator; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(SimulatorActions set) { return set.Get(); }
        public void SetCallbacks(ISimulatorActions instance)
        {
            if (m_Wrapper.m_SimulatorActionsCallbackInterface != null)
            {
                @ToggleNPCS.started -= m_Wrapper.m_SimulatorActionsCallbackInterface.OnToggleNPCS;
                @ToggleNPCS.performed -= m_Wrapper.m_SimulatorActionsCallbackInterface.OnToggleNPCS;
                @ToggleNPCS.canceled -= m_Wrapper.m_SimulatorActionsCallbackInterface.OnToggleNPCS;
                @ToggleAgent.started -= m_Wrapper.m_SimulatorActionsCallbackInterface.OnToggleAgent;
                @ToggleAgent.performed -= m_Wrapper.m_SimulatorActionsCallbackInterface.OnToggleAgent;
                @ToggleAgent.canceled -= m_Wrapper.m_SimulatorActionsCallbackInterface.OnToggleAgent;
                @ToggleReset.started -= m_Wrapper.m_SimulatorActionsCallbackInterface.OnToggleReset;
                @ToggleReset.performed -= m_Wrapper.m_SimulatorActionsCallbackInterface.OnToggleReset;
                @ToggleReset.canceled -= m_Wrapper.m_SimulatorActionsCallbackInterface.OnToggleReset;
                @ToggleControlsUI.started -= m_Wrapper.m_SimulatorActionsCallbackInterface.OnToggleControlsUI;
                @ToggleControlsUI.performed -= m_Wrapper.m_SimulatorActionsCallbackInterface.OnToggleControlsUI;
                @ToggleControlsUI.canceled -= m_Wrapper.m_SimulatorActionsCallbackInterface.OnToggleControlsUI;
                @TogglePedestrians.started -= m_Wrapper.m_SimulatorActionsCallbackInterface.OnTogglePedestrians;
                @TogglePedestrians.performed -= m_Wrapper.m_SimulatorActionsCallbackInterface.OnTogglePedestrians;
                @TogglePedestrians.canceled -= m_Wrapper.m_SimulatorActionsCallbackInterface.OnTogglePedestrians;
            }
            m_Wrapper.m_SimulatorActionsCallbackInterface = instance;
            if (instance != null)
            {
                @ToggleNPCS.started += instance.OnToggleNPCS;
                @ToggleNPCS.performed += instance.OnToggleNPCS;
                @ToggleNPCS.canceled += instance.OnToggleNPCS;
                @ToggleAgent.started += instance.OnToggleAgent;
                @ToggleAgent.performed += instance.OnToggleAgent;
                @ToggleAgent.canceled += instance.OnToggleAgent;
                @ToggleReset.started += instance.OnToggleReset;
                @ToggleReset.performed += instance.OnToggleReset;
                @ToggleReset.canceled += instance.OnToggleReset;
                @ToggleControlsUI.started += instance.OnToggleControlsUI;
                @ToggleControlsUI.performed += instance.OnToggleControlsUI;
                @ToggleControlsUI.canceled += instance.OnToggleControlsUI;
                @TogglePedestrians.started += instance.OnTogglePedestrians;
                @TogglePedestrians.performed += instance.OnTogglePedestrians;
                @TogglePedestrians.canceled += instance.OnTogglePedestrians;
            }
        }
    }
    public SimulatorActions @Simulator => new SimulatorActions(this);
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
        void OnToggleState(InputAction.CallbackContext context);
        void OnZoom(InputAction.CallbackContext context);
        void OnCinematicNewPath(InputAction.CallbackContext context);
        void OnCinematicResetPath(InputAction.CallbackContext context);
    }
    public interface ISimulatorActions
    {
        void OnToggleNPCS(InputAction.CallbackContext context);
        void OnToggleAgent(InputAction.CallbackContext context);
        void OnToggleReset(InputAction.CallbackContext context);
        void OnToggleControlsUI(InputAction.CallbackContext context);
        void OnTogglePedestrians(InputAction.CallbackContext context);
    }
}
