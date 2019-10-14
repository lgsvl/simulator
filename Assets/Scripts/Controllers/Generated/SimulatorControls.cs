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
            ""name"": ""Camera"",
            ""id"": ""c0640561-3291-49a8-99e5-12c8002fb650"",
            ""actions"": [
                {
                    ""name"": ""Direction"",
                    ""id"": ""68acaa71-e07d-420b-b1d6-a76c02a93b0f"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": true,
                    ""passThrough"": true,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""Elevation"",
                    ""id"": ""0dad18b8-ab6f-4e94-9476-4b816f14eb47"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": true,
                    ""passThrough"": true,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""MouseDelta"",
                    ""id"": ""cfde6f4e-1da5-42e8-b6b8-0749b7ff43d9"",
                    ""expectedControlLayout"": ""Vector2"",
                    ""continuous"": true,
                    ""passThrough"": false,
                    ""initialStateCheck"": true,
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
                    ""continuous"": true,
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
                    ""continuous"": true,
                    ""passThrough"": false,
                    ""initialStateCheck"": true,
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
                },
                {
                    ""name"": ""ToggleState"",
                    ""id"": ""937f6067-9820-460d-99c5-e050ce658467"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""Zoom"",
                    ""id"": ""1138b450-1c54-49a7-b808-8665b1326312"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": true,
                    ""passThrough"": true,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""CinematicNewPath"",
                    ""id"": ""665b9005-f4bd-473b-9c46-4fd20642ac7d"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""CinematicResetPath"",
                    ""id"": ""2c242fd3-0465-42fe-9be4-7a4b71ba5221"",
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
                    ""interactions"": ""Hold,Press(behavior=2)"",
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
                    ""interactions"": ""Hold,Press(behavior=2)"",
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
                    ""interactions"": ""Hold,Press(behavior=2)"",
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
                    ""interactions"": ""Hold,Press(behavior=2)"",
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
                    ""path"": ""<Keyboard>/e"",
                    ""interactions"": ""Hold,Press(behavior=2)"",
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
                    ""path"": ""<Keyboard>/q"",
                    ""interactions"": ""Hold,Press(behavior=2)"",
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
                    ""processors"": ""NormalizeVector2"",
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
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
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
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
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
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
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
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
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
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
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
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                }
            ]
        },
        {
            ""name"": ""Simulator"",
            ""id"": ""04811f95-5e48-44b8-98ce-b3957e6df2cd"",
            ""actions"": [
                {
                    ""name"": ""ToggleNPCS"",
                    ""id"": ""c3de16aa-60a2-49a7-8a4d-51f32b875b63"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""ToggleAgent"",
                    ""id"": ""e2c98be3-db48-4767-9803-56e3a731d9e8"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""ToggleReset"",
                    ""id"": ""02a13f64-a6f0-4d2c-9a12-188a987f1ee7"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""ToggleControlsUI"",
                    ""id"": ""18aa8941-8b9f-4bfe-b14c-88d6e6641ba2"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""TogglePedestrians"",
                    ""id"": ""b39bbbcd-97f5-4b45-b155-e7374f654c05"",
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
                    ""name"": """",
                    ""id"": ""4250e116-5443-4648-a14d-13bf03610f68"",
                    ""path"": ""<Keyboard>/#(N)"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ToggleNPCS"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
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
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
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
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
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
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
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
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
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
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
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
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
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
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
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
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
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
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
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
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
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
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
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
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
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
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                }
            ]
        },
        {
            ""name"": ""VehicleKeyboard"",
            ""id"": ""a7c84f6e-a555-4bc5-905a-357e97a77b4c"",
            ""actions"": [
                {
                    ""name"": ""Direction"",
                    ""id"": ""a4c84d4e-ab73-4109-b6d3-87cef0688a48"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": true,
                    ""passThrough"": true,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""HeadLights"",
                    ""id"": ""2da8992e-466b-42bd-9300-0854e7aba0df"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""IndicatorLeft"",
                    ""id"": ""be797ba1-c73a-41a4-ac0a-7269a1f21865"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""IndicatorRight"",
                    ""id"": ""456e0d9b-1590-495a-a810-22068dde68ad"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""IndicatorHazard"",
                    ""id"": ""949c9a20-2be9-4a66-99b8-fb9c720a3c89"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""FogLights"",
                    ""id"": ""6ebbb86e-7e78-4e78-8238-8f12e931443d"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""ShiftFirst"",
                    ""id"": ""e216bb61-1dbf-4936-8963-26a8cb3d4fde"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""ShiftReverse"",
                    ""id"": ""e5606c54-98ab-4c0a-95ba-47e452129b1c"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""ParkingBrake"",
                    ""id"": ""11cc056b-8dec-44d6-a5af-c41873912c49"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""Ignition"",
                    ""id"": ""546e7144-7b87-4a19-b456-b3edf510c512"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": false,
                    ""passThrough"": false,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""InteriorLight"",
                    ""id"": ""ee3edbee-7490-48da-91aa-1426cbb4e154"",
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
                    ""name"": ""Keyboard"",
                    ""id"": ""4a3944ef-8c2c-447c-bc20-2765dd295ca9"",
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
                    ""id"": ""dec83688-608f-4659-ab71-6f83cf44a947"",
                    ""path"": ""<Keyboard>/upArrow"",
                    ""interactions"": ""Hold,Press(behavior=2)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Direction"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""down"",
                    ""id"": ""8c7b7735-c16e-4c0c-827a-18e37bb9d7c6"",
                    ""path"": ""<Keyboard>/downArrow"",
                    ""interactions"": ""Hold,Press(behavior=2)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Direction"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""left"",
                    ""id"": ""af310a91-e63e-475e-94a0-d754a765b3d8"",
                    ""path"": ""<Keyboard>/leftArrow"",
                    ""interactions"": ""Hold,Press(behavior=2)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Direction"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""right"",
                    ""id"": ""aa75c26c-86ae-46cc-89b0-2d7eb13035e2"",
                    ""path"": ""<Keyboard>/rightArrow"",
                    ""interactions"": ""Hold,Press(behavior=2)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Direction"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""4e90a450-ce78-4ddb-8e50-c199db820746"",
                    ""path"": ""<Keyboard>/#(H)"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""HeadLights"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""975ef4d1-cdbe-4cc3-81c4-512cd70c11fa"",
                    ""path"": ""<Keyboard>/#(,)"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""IndicatorLeft"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""40b6b649-1dae-456c-b067-630619715b18"",
                    ""path"": ""<Keyboard>/#(.)"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""IndicatorRight"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""7946afeb-995e-4933-b7bf-5fe4ea1df09f"",
                    ""path"": ""<Keyboard>/#(M)"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""IndicatorHazard"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""43be803b-bfc1-425e-837e-bc0b586d82dd"",
                    ""path"": ""<Keyboard>/#(F)"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""FogLights"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""3e6f878e-765f-4ca8-84b8-ecab778a16ae"",
                    ""path"": ""<Keyboard>/pageUp"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ShiftFirst"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""0bc2ba47-4a58-4cad-97d2-228ab460619f"",
                    ""path"": ""<Keyboard>/pageDown"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ShiftReverse"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""ec8cc402-0c57-4804-9c5a-38663a225ea2"",
                    ""path"": ""<Keyboard>/rightShift"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ParkingBrake"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""133d24b4-434c-42f3-993e-d4c5456e7ca4"",
                    ""path"": ""<Keyboard>/end"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Ignition"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""a2d4dbb8-8282-444f-8af1-c8405b87e322"",
                    ""path"": ""<Keyboard>/#(I)"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""InteriorLight"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                }
            ]
        },
        {
            ""name"": ""VehicleWheel"",
            ""id"": ""037577cd-9d0e-462a-937f-029d6de72d02"",
            ""actions"": [
                {
                    ""name"": ""Accel"",
                    ""id"": ""888616c7-579c-4efd-8527-708e683af1eb"",
                    ""expectedControlLayout"": """",
                    ""continuous"": true,
                    ""passThrough"": true,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""Steer"",
                    ""id"": ""455577db-93bb-4060-849b-d5608bfb6bad"",
                    ""expectedControlLayout"": """",
                    ""continuous"": true,
                    ""passThrough"": true,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""Brake"",
                    ""id"": ""27a09f05-a77b-4209-b710-af353ec2bc76"",
                    ""expectedControlLayout"": """",
                    ""continuous"": true,
                    ""passThrough"": true,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""ButtonA"",
                    ""id"": ""59312a63-d87f-4991-a404-426b8dfcaa04"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": true,
                    ""passThrough"": true,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""ButtonB"",
                    ""id"": ""7f46a0eb-0050-49d4-bd15-dd187a4ff30c"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": true,
                    ""passThrough"": true,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""ButtonX"",
                    ""id"": ""f8c5c1e7-bfad-48f3-a844-2bda89c29c45"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": true,
                    ""passThrough"": true,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""ButtonY"",
                    ""id"": ""c4f8a72b-afe3-448d-896c-4b366b62b997"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": true,
                    ""passThrough"": true,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""ButtonRB"",
                    ""id"": ""c22f26a3-873d-4afe-9085-8b3fd32fff3c"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": true,
                    ""passThrough"": true,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""ButtonLB"",
                    ""id"": ""37d85537-6481-4742-864c-c36a343fb6c4"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": true,
                    ""passThrough"": true,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""ButtonStart"",
                    ""id"": ""e384c3ab-2f9b-4e19-b583-3c653f088a80"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": true,
                    ""passThrough"": true,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""ButtonSelect"",
                    ""id"": ""ae7c3ac6-1379-4238-a41f-fc9091edc2fa"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": true,
                    ""passThrough"": true,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""ButtonRSB"",
                    ""id"": ""83e310ca-be8a-4d82-a1ea-1dbc08131851"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": true,
                    ""passThrough"": true,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""ButtonLSB"",
                    ""id"": ""f948e86f-4ed4-48ef-9772-f5e5fac8ec37"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": true,
                    ""passThrough"": true,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""ButtonCenter"",
                    ""id"": ""9d981352-e4c1-4fd7-b199-8e339f31d4d6"",
                    ""expectedControlLayout"": ""Button"",
                    ""continuous"": true,
                    ""passThrough"": true,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                },
                {
                    ""name"": ""DPad"",
                    ""id"": ""1470b2f1-7873-4929-926b-fc30937dea0a"",
                    ""expectedControlLayout"": """",
                    ""continuous"": true,
                    ""passThrough"": true,
                    ""initialStateCheck"": false,
                    ""processors"": """",
                    ""interactions"": """",
                    ""bindings"": []
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""bed5fb37-63ac-4233-850e-a9e14a6a203b"",
                    ""path"": ""<HID::Logitech G920 Driving Force Racing Wheel for Xbox One Joystick>/y"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Accel"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""e46b7fbe-610e-4421-a188-f631133056e7"",
                    ""path"": ""<Linux::LogitechInc::LogitechG920DrivingForceRacingWheel>/Stick/y"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Accel"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""9556cee8-daea-4154-b0b5-39cf87329e20"",
                    ""path"": ""<HID::Logitech G920 Driving Force Racing Wheel for Xbox One Joystick>/x"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Steer"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""2f564dea-9879-4600-99be-7b33abf9d05b"",
                    ""path"": ""<Linux::LogitechInc::LogitechG920DrivingForceRacingWheel>/Stick/x"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Steer"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""62d31aaa-3ef4-44f1-aa07-65401643ca96"",
                    ""path"": ""<HID::Logitech G920 Driving Force Racing Wheel for Xbox One Joystick>/z"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Brake"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""9c4a7659-7989-4829-a2bb-b6067a77b358"",
                    ""path"": ""<Linux::LogitechInc::LogitechG920DrivingForceRacingWheel>/Z"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Brake"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""d9ddbe73-def5-4e00-afd9-9cd54179ac5d"",
                    ""path"": ""<HID::Logitech G920 Driving Force Racing Wheel for Xbox One Joystick>/button1"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ButtonA"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""8537f36d-f294-4103-b2a1-825657ad8c1b"",
                    ""path"": ""<Linux::LogitechInc::LogitechG920DrivingForceRacingWheel>/Trigger"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ButtonA"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""4dbfa0de-c0e9-4235-b1cb-179f8f54ce88"",
                    ""path"": ""<HID::Logitech G920 Driving Force Racing Wheel for Xbox One Joystick>/button2"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ButtonB"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""cc976bf4-0745-4116-98d3-e2a76713a068"",
                    ""path"": ""<Linux::LogitechInc::LogitechG920DrivingForceRacingWheel>/Thumb"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ButtonB"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""12cddf58-a50f-4b3c-bd49-1dd09dc4bc5a"",
                    ""path"": ""<HID::Logitech G920 Driving Force Racing Wheel for Xbox One Joystick>/button3"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ButtonX"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""8140ae56-83bc-43c4-91b3-72c738a65e42"",
                    ""path"": ""<Linux::LogitechInc::LogitechG920DrivingForceRacingWheel>/Thumb2"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ButtonX"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""7c48d30f-d4b7-4918-83fc-d141309b1bff"",
                    ""path"": ""<HID::Logitech G920 Driving Force Racing Wheel for Xbox One Joystick>/button4"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ButtonY"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""9378683b-1749-48e6-bf91-f42543a61ff6"",
                    ""path"": ""<Linux::LogitechInc::LogitechG920DrivingForceRacingWheel>/Top"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ButtonY"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""f3428280-2044-4494-a71d-8876ccc378db"",
                    ""path"": ""<HID::Logitech G920 Driving Force Racing Wheel for Xbox One Joystick>/button5"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ButtonRB"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""636e0b69-3e2b-4879-91c6-00b1161766cd"",
                    ""path"": ""<Linux::LogitechInc::LogitechG920DrivingForceRacingWheel>/Top2"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ButtonRB"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""cc92cf1c-2647-4b2e-99c4-2ef7883f4dcb"",
                    ""path"": ""<HID::Logitech G920 Driving Force Racing Wheel for Xbox One Joystick>/button6"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ButtonLB"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""da0c14b8-1c5d-4d3b-8fcc-0676a90e32d3"",
                    ""path"": ""<Linux::LogitechInc::LogitechG920DrivingForceRacingWheel>/Pinkie"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ButtonLB"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""a1fbb4a0-cd63-4a72-84bc-e27bb2fa6387"",
                    ""path"": ""<HID::Logitech G920 Driving Force Racing Wheel for Xbox One Joystick>/button7"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ButtonStart"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""2257a097-7d75-48ca-a15c-3a673a3b8e63"",
                    ""path"": ""<Linux::LogitechInc::LogitechG920DrivingForceRacingWheel>/Base"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ButtonStart"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""978313b2-9e48-4479-ae0c-defb6a04acfa"",
                    ""path"": ""<HID::Logitech G920 Driving Force Racing Wheel for Xbox One Joystick>/button8"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ButtonSelect"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""f0272537-424d-431c-8333-30bb462c1db5"",
                    ""path"": ""<Linux::LogitechInc::LogitechG920DrivingForceRacingWheel>/Base2"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ButtonSelect"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""533f135e-d206-4e92-8448-e104dcceedc8"",
                    ""path"": ""<HID::Logitech G920 Driving Force Racing Wheel for Xbox One Joystick>/button9"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ButtonRSB"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""9e65f0c6-ca81-425c-8c8c-ea4970967ee7"",
                    ""path"": ""<Linux::LogitechInc::LogitechG920DrivingForceRacingWheel>/Base3"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ButtonRSB"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""c8b5f0be-5ae7-4af0-a08c-8750cd61b429"",
                    ""path"": ""<HID::Logitech G920 Driving Force Racing Wheel for Xbox One Joystick>/button10"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ButtonLSB"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""8d82b876-f25b-460d-b0cf-6e937ef9676e"",
                    ""path"": ""<Linux::LogitechInc::LogitechG920DrivingForceRacingWheel>/Base4"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ButtonLSB"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""5084feaa-62d9-47db-995c-a8c3b28ef2ca"",
                    ""path"": ""<HID::Logitech G920 Driving Force Racing Wheel for Xbox One Joystick>/button11"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ButtonCenter"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": """",
                    ""id"": ""6119989e-a435-4d18-b3c3-c05bbb1b6448"",
                    ""path"": ""<Linux::LogitechInc::LogitechG920DrivingForceRacingWheel>/Base5"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ButtonCenter"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""2D Vector"",
                    ""id"": ""b4a74245-fb08-41e8-ad8d-1f61857d5d8f"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""DPad"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""up"",
                    ""id"": ""a7325f94-c632-413e-9e47-dee272324d3c"",
                    ""path"": ""<HID::Logitech G920 Driving Force Racing Wheel for Xbox One Joystick>/dpad/up"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""DPad"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""down"",
                    ""id"": ""6f2aeda4-1b33-4970-9d5f-3c7211868a15"",
                    ""path"": ""<HID::Logitech G920 Driving Force Racing Wheel for Xbox One Joystick>/dpad/down"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""DPad"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""left"",
                    ""id"": ""a4e08757-fc90-4c4e-a67c-d3ef308650e6"",
                    ""path"": ""<HID::Logitech G920 Driving Force Racing Wheel for Xbox One Joystick>/dpad/left"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""DPad"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""right"",
                    ""id"": ""d3c50b20-52f4-47b2-96f4-edb5cbee34a9"",
                    ""path"": ""<HID::Logitech G920 Driving Force Racing Wheel for Xbox One Joystick>/dpad/right"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""DPad"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""2D Vector"",
                    ""id"": ""c17d2f10-b018-407f-be1e-dc7e4203aaff"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""DPad"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""up"",
                    ""id"": ""9fd13902-02a1-48c7-a622-65ddacfacc3e"",
                    ""path"": ""<Linux::LogitechInc::LogitechG920DrivingForceRacingWheel>/Hat/up"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""DPad"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""down"",
                    ""id"": ""2a0446a0-646f-4b32-9693-7c30637ea4eb"",
                    ""path"": ""<Linux::LogitechInc::LogitechG920DrivingForceRacingWheel>/Hat/down"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""DPad"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""left"",
                    ""id"": ""cb91eeab-35ac-41dc-8d2f-34195b8a5310"",
                    ""path"": ""<Linux::LogitechInc::LogitechG920DrivingForceRacingWheel>/Hat/left"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""DPad"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                },
                {
                    ""name"": ""right"",
                    ""id"": ""f2f38962-9b2c-406c-bf86-e4a31a582614"",
                    ""path"": ""<Linux::LogitechInc::LogitechG920DrivingForceRacingWheel>/Hat/right"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""DPad"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
                    ""modifiers"": """"
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
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
        m_Camera_ToggleState = m_Camera.GetAction("ToggleState");
        m_Camera_Zoom = m_Camera.GetAction("Zoom");
        m_Camera_CinematicNewPath = m_Camera.GetAction("CinematicNewPath");
        m_Camera_CinematicResetPath = m_Camera.GetAction("CinematicResetPath");
        // Simulator
        m_Simulator = asset.GetActionMap("Simulator");
        m_Simulator_ToggleNPCS = m_Simulator.GetAction("ToggleNPCS");
        m_Simulator_ToggleAgent = m_Simulator.GetAction("ToggleAgent");
        m_Simulator_ToggleReset = m_Simulator.GetAction("ToggleReset");
        m_Simulator_ToggleControlsUI = m_Simulator.GetAction("ToggleControlsUI");
        m_Simulator_TogglePedestrians = m_Simulator.GetAction("TogglePedestrians");
        // VehicleKeyboard
        m_VehicleKeyboard = asset.GetActionMap("VehicleKeyboard");
        m_VehicleKeyboard_Direction = m_VehicleKeyboard.GetAction("Direction");
        m_VehicleKeyboard_HeadLights = m_VehicleKeyboard.GetAction("HeadLights");
        m_VehicleKeyboard_IndicatorLeft = m_VehicleKeyboard.GetAction("IndicatorLeft");
        m_VehicleKeyboard_IndicatorRight = m_VehicleKeyboard.GetAction("IndicatorRight");
        m_VehicleKeyboard_IndicatorHazard = m_VehicleKeyboard.GetAction("IndicatorHazard");
        m_VehicleKeyboard_FogLights = m_VehicleKeyboard.GetAction("FogLights");
        m_VehicleKeyboard_ShiftFirst = m_VehicleKeyboard.GetAction("ShiftFirst");
        m_VehicleKeyboard_ShiftReverse = m_VehicleKeyboard.GetAction("ShiftReverse");
        m_VehicleKeyboard_ParkingBrake = m_VehicleKeyboard.GetAction("ParkingBrake");
        m_VehicleKeyboard_Ignition = m_VehicleKeyboard.GetAction("Ignition");
        m_VehicleKeyboard_InteriorLight = m_VehicleKeyboard.GetAction("InteriorLight");
        // VehicleWheel
        m_VehicleWheel = asset.GetActionMap("VehicleWheel");
        m_VehicleWheel_Accel = m_VehicleWheel.GetAction("Accel");
        m_VehicleWheel_Steer = m_VehicleWheel.GetAction("Steer");
        m_VehicleWheel_Brake = m_VehicleWheel.GetAction("Brake");
        m_VehicleWheel_ButtonA = m_VehicleWheel.GetAction("ButtonA");
        m_VehicleWheel_ButtonB = m_VehicleWheel.GetAction("ButtonB");
        m_VehicleWheel_ButtonX = m_VehicleWheel.GetAction("ButtonX");
        m_VehicleWheel_ButtonY = m_VehicleWheel.GetAction("ButtonY");
        m_VehicleWheel_ButtonRB = m_VehicleWheel.GetAction("ButtonRB");
        m_VehicleWheel_ButtonLB = m_VehicleWheel.GetAction("ButtonLB");
        m_VehicleWheel_ButtonStart = m_VehicleWheel.GetAction("ButtonStart");
        m_VehicleWheel_ButtonSelect = m_VehicleWheel.GetAction("ButtonSelect");
        m_VehicleWheel_ButtonRSB = m_VehicleWheel.GetAction("ButtonRSB");
        m_VehicleWheel_ButtonLSB = m_VehicleWheel.GetAction("ButtonLSB");
        m_VehicleWheel_ButtonCenter = m_VehicleWheel.GetAction("ButtonCenter");
        m_VehicleWheel_DPad = m_VehicleWheel.GetAction("DPad");
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
    private InputAction m_Camera_ToggleState;
    private InputAction m_Camera_Zoom;
    private InputAction m_Camera_CinematicNewPath;
    private InputAction m_Camera_CinematicResetPath;
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
        public InputAction @ToggleState { get { return m_Wrapper.m_Camera_ToggleState; } }
        public InputAction @Zoom { get { return m_Wrapper.m_Camera_Zoom; } }
        public InputAction @CinematicNewPath { get { return m_Wrapper.m_Camera_CinematicNewPath; } }
        public InputAction @CinematicResetPath { get { return m_Wrapper.m_Camera_CinematicResetPath; } }
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
                ToggleState.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnToggleState;
                ToggleState.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnToggleState;
                ToggleState.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnToggleState;
                Zoom.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnZoom;
                Zoom.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnZoom;
                Zoom.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnZoom;
                CinematicNewPath.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnCinematicNewPath;
                CinematicNewPath.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnCinematicNewPath;
                CinematicNewPath.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnCinematicNewPath;
                CinematicResetPath.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnCinematicResetPath;
                CinematicResetPath.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnCinematicResetPath;
                CinematicResetPath.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnCinematicResetPath;
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
                ToggleState.started += instance.OnToggleState;
                ToggleState.performed += instance.OnToggleState;
                ToggleState.canceled += instance.OnToggleState;
                Zoom.started += instance.OnZoom;
                Zoom.performed += instance.OnZoom;
                Zoom.canceled += instance.OnZoom;
                CinematicNewPath.started += instance.OnCinematicNewPath;
                CinematicNewPath.performed += instance.OnCinematicNewPath;
                CinematicNewPath.canceled += instance.OnCinematicNewPath;
                CinematicResetPath.started += instance.OnCinematicResetPath;
                CinematicResetPath.performed += instance.OnCinematicResetPath;
                CinematicResetPath.canceled += instance.OnCinematicResetPath;
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

    // Simulator
    private InputActionMap m_Simulator;
    private ISimulatorActions m_SimulatorActionsCallbackInterface;
    private InputAction m_Simulator_ToggleNPCS;
    private InputAction m_Simulator_ToggleAgent;
    private InputAction m_Simulator_ToggleReset;
    private InputAction m_Simulator_ToggleControlsUI;
    private InputAction m_Simulator_TogglePedestrians;
    public struct SimulatorActions
    {
        private SimulatorControls m_Wrapper;
        public SimulatorActions(SimulatorControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @ToggleNPCS { get { return m_Wrapper.m_Simulator_ToggleNPCS; } }
        public InputAction @ToggleAgent { get { return m_Wrapper.m_Simulator_ToggleAgent; } }
        public InputAction @ToggleReset { get { return m_Wrapper.m_Simulator_ToggleReset; } }
        public InputAction @ToggleControlsUI { get { return m_Wrapper.m_Simulator_ToggleControlsUI; } }
        public InputAction @TogglePedestrians { get { return m_Wrapper.m_Simulator_TogglePedestrians; } }
        public InputActionMap Get() { return m_Wrapper.m_Simulator; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled { get { return Get().enabled; } }
        public InputActionMap Clone() { return Get().Clone(); }
        public static implicit operator InputActionMap(SimulatorActions set) { return set.Get(); }
        public void SetCallbacks(ISimulatorActions instance)
        {
            if (m_Wrapper.m_SimulatorActionsCallbackInterface != null)
            {
                ToggleNPCS.started -= m_Wrapper.m_SimulatorActionsCallbackInterface.OnToggleNPCS;
                ToggleNPCS.performed -= m_Wrapper.m_SimulatorActionsCallbackInterface.OnToggleNPCS;
                ToggleNPCS.canceled -= m_Wrapper.m_SimulatorActionsCallbackInterface.OnToggleNPCS;
                ToggleAgent.started -= m_Wrapper.m_SimulatorActionsCallbackInterface.OnToggleAgent;
                ToggleAgent.performed -= m_Wrapper.m_SimulatorActionsCallbackInterface.OnToggleAgent;
                ToggleAgent.canceled -= m_Wrapper.m_SimulatorActionsCallbackInterface.OnToggleAgent;
                ToggleReset.started -= m_Wrapper.m_SimulatorActionsCallbackInterface.OnToggleReset;
                ToggleReset.performed -= m_Wrapper.m_SimulatorActionsCallbackInterface.OnToggleReset;
                ToggleReset.canceled -= m_Wrapper.m_SimulatorActionsCallbackInterface.OnToggleReset;
                ToggleControlsUI.started -= m_Wrapper.m_SimulatorActionsCallbackInterface.OnToggleControlsUI;
                ToggleControlsUI.performed -= m_Wrapper.m_SimulatorActionsCallbackInterface.OnToggleControlsUI;
                ToggleControlsUI.canceled -= m_Wrapper.m_SimulatorActionsCallbackInterface.OnToggleControlsUI;
                TogglePedestrians.started -= m_Wrapper.m_SimulatorActionsCallbackInterface.OnTogglePedestrians;
                TogglePedestrians.performed -= m_Wrapper.m_SimulatorActionsCallbackInterface.OnTogglePedestrians;
                TogglePedestrians.canceled -= m_Wrapper.m_SimulatorActionsCallbackInterface.OnTogglePedestrians;
            }
            m_Wrapper.m_SimulatorActionsCallbackInterface = instance;
            if (instance != null)
            {
                ToggleNPCS.started += instance.OnToggleNPCS;
                ToggleNPCS.performed += instance.OnToggleNPCS;
                ToggleNPCS.canceled += instance.OnToggleNPCS;
                ToggleAgent.started += instance.OnToggleAgent;
                ToggleAgent.performed += instance.OnToggleAgent;
                ToggleAgent.canceled += instance.OnToggleAgent;
                ToggleReset.started += instance.OnToggleReset;
                ToggleReset.performed += instance.OnToggleReset;
                ToggleReset.canceled += instance.OnToggleReset;
                ToggleControlsUI.started += instance.OnToggleControlsUI;
                ToggleControlsUI.performed += instance.OnToggleControlsUI;
                ToggleControlsUI.canceled += instance.OnToggleControlsUI;
                TogglePedestrians.started += instance.OnTogglePedestrians;
                TogglePedestrians.performed += instance.OnTogglePedestrians;
                TogglePedestrians.canceled += instance.OnTogglePedestrians;
            }
        }
    }
    public SimulatorActions @Simulator
    {
        get
        {
            return new SimulatorActions(this);
        }
    }

    // VehicleKeyboard
    private InputActionMap m_VehicleKeyboard;
    private IVehicleKeyboardActions m_VehicleKeyboardActionsCallbackInterface;
    private InputAction m_VehicleKeyboard_Direction;
    private InputAction m_VehicleKeyboard_HeadLights;
    private InputAction m_VehicleKeyboard_IndicatorLeft;
    private InputAction m_VehicleKeyboard_IndicatorRight;
    private InputAction m_VehicleKeyboard_IndicatorHazard;
    private InputAction m_VehicleKeyboard_FogLights;
    private InputAction m_VehicleKeyboard_ShiftFirst;
    private InputAction m_VehicleKeyboard_ShiftReverse;
    private InputAction m_VehicleKeyboard_ParkingBrake;
    private InputAction m_VehicleKeyboard_Ignition;
    private InputAction m_VehicleKeyboard_InteriorLight;
    public struct VehicleKeyboardActions
    {
        private SimulatorControls m_Wrapper;
        public VehicleKeyboardActions(SimulatorControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @Direction { get { return m_Wrapper.m_VehicleKeyboard_Direction; } }
        public InputAction @HeadLights { get { return m_Wrapper.m_VehicleKeyboard_HeadLights; } }
        public InputAction @IndicatorLeft { get { return m_Wrapper.m_VehicleKeyboard_IndicatorLeft; } }
        public InputAction @IndicatorRight { get { return m_Wrapper.m_VehicleKeyboard_IndicatorRight; } }
        public InputAction @IndicatorHazard { get { return m_Wrapper.m_VehicleKeyboard_IndicatorHazard; } }
        public InputAction @FogLights { get { return m_Wrapper.m_VehicleKeyboard_FogLights; } }
        public InputAction @ShiftFirst { get { return m_Wrapper.m_VehicleKeyboard_ShiftFirst; } }
        public InputAction @ShiftReverse { get { return m_Wrapper.m_VehicleKeyboard_ShiftReverse; } }
        public InputAction @ParkingBrake { get { return m_Wrapper.m_VehicleKeyboard_ParkingBrake; } }
        public InputAction @Ignition { get { return m_Wrapper.m_VehicleKeyboard_Ignition; } }
        public InputAction @InteriorLight { get { return m_Wrapper.m_VehicleKeyboard_InteriorLight; } }
        public InputActionMap Get() { return m_Wrapper.m_VehicleKeyboard; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled { get { return Get().enabled; } }
        public InputActionMap Clone() { return Get().Clone(); }
        public static implicit operator InputActionMap(VehicleKeyboardActions set) { return set.Get(); }
        public void SetCallbacks(IVehicleKeyboardActions instance)
        {
            if (m_Wrapper.m_VehicleKeyboardActionsCallbackInterface != null)
            {
                Direction.started -= m_Wrapper.m_VehicleKeyboardActionsCallbackInterface.OnDirection;
                Direction.performed -= m_Wrapper.m_VehicleKeyboardActionsCallbackInterface.OnDirection;
                Direction.canceled -= m_Wrapper.m_VehicleKeyboardActionsCallbackInterface.OnDirection;
                HeadLights.started -= m_Wrapper.m_VehicleKeyboardActionsCallbackInterface.OnHeadLights;
                HeadLights.performed -= m_Wrapper.m_VehicleKeyboardActionsCallbackInterface.OnHeadLights;
                HeadLights.canceled -= m_Wrapper.m_VehicleKeyboardActionsCallbackInterface.OnHeadLights;
                IndicatorLeft.started -= m_Wrapper.m_VehicleKeyboardActionsCallbackInterface.OnIndicatorLeft;
                IndicatorLeft.performed -= m_Wrapper.m_VehicleKeyboardActionsCallbackInterface.OnIndicatorLeft;
                IndicatorLeft.canceled -= m_Wrapper.m_VehicleKeyboardActionsCallbackInterface.OnIndicatorLeft;
                IndicatorRight.started -= m_Wrapper.m_VehicleKeyboardActionsCallbackInterface.OnIndicatorRight;
                IndicatorRight.performed -= m_Wrapper.m_VehicleKeyboardActionsCallbackInterface.OnIndicatorRight;
                IndicatorRight.canceled -= m_Wrapper.m_VehicleKeyboardActionsCallbackInterface.OnIndicatorRight;
                IndicatorHazard.started -= m_Wrapper.m_VehicleKeyboardActionsCallbackInterface.OnIndicatorHazard;
                IndicatorHazard.performed -= m_Wrapper.m_VehicleKeyboardActionsCallbackInterface.OnIndicatorHazard;
                IndicatorHazard.canceled -= m_Wrapper.m_VehicleKeyboardActionsCallbackInterface.OnIndicatorHazard;
                FogLights.started -= m_Wrapper.m_VehicleKeyboardActionsCallbackInterface.OnFogLights;
                FogLights.performed -= m_Wrapper.m_VehicleKeyboardActionsCallbackInterface.OnFogLights;
                FogLights.canceled -= m_Wrapper.m_VehicleKeyboardActionsCallbackInterface.OnFogLights;
                ShiftFirst.started -= m_Wrapper.m_VehicleKeyboardActionsCallbackInterface.OnShiftFirst;
                ShiftFirst.performed -= m_Wrapper.m_VehicleKeyboardActionsCallbackInterface.OnShiftFirst;
                ShiftFirst.canceled -= m_Wrapper.m_VehicleKeyboardActionsCallbackInterface.OnShiftFirst;
                ShiftReverse.started -= m_Wrapper.m_VehicleKeyboardActionsCallbackInterface.OnShiftReverse;
                ShiftReverse.performed -= m_Wrapper.m_VehicleKeyboardActionsCallbackInterface.OnShiftReverse;
                ShiftReverse.canceled -= m_Wrapper.m_VehicleKeyboardActionsCallbackInterface.OnShiftReverse;
                ParkingBrake.started -= m_Wrapper.m_VehicleKeyboardActionsCallbackInterface.OnParkingBrake;
                ParkingBrake.performed -= m_Wrapper.m_VehicleKeyboardActionsCallbackInterface.OnParkingBrake;
                ParkingBrake.canceled -= m_Wrapper.m_VehicleKeyboardActionsCallbackInterface.OnParkingBrake;
                Ignition.started -= m_Wrapper.m_VehicleKeyboardActionsCallbackInterface.OnIgnition;
                Ignition.performed -= m_Wrapper.m_VehicleKeyboardActionsCallbackInterface.OnIgnition;
                Ignition.canceled -= m_Wrapper.m_VehicleKeyboardActionsCallbackInterface.OnIgnition;
                InteriorLight.started -= m_Wrapper.m_VehicleKeyboardActionsCallbackInterface.OnInteriorLight;
                InteriorLight.performed -= m_Wrapper.m_VehicleKeyboardActionsCallbackInterface.OnInteriorLight;
                InteriorLight.canceled -= m_Wrapper.m_VehicleKeyboardActionsCallbackInterface.OnInteriorLight;
            }
            m_Wrapper.m_VehicleKeyboardActionsCallbackInterface = instance;
            if (instance != null)
            {
                Direction.started += instance.OnDirection;
                Direction.performed += instance.OnDirection;
                Direction.canceled += instance.OnDirection;
                HeadLights.started += instance.OnHeadLights;
                HeadLights.performed += instance.OnHeadLights;
                HeadLights.canceled += instance.OnHeadLights;
                IndicatorLeft.started += instance.OnIndicatorLeft;
                IndicatorLeft.performed += instance.OnIndicatorLeft;
                IndicatorLeft.canceled += instance.OnIndicatorLeft;
                IndicatorRight.started += instance.OnIndicatorRight;
                IndicatorRight.performed += instance.OnIndicatorRight;
                IndicatorRight.canceled += instance.OnIndicatorRight;
                IndicatorHazard.started += instance.OnIndicatorHazard;
                IndicatorHazard.performed += instance.OnIndicatorHazard;
                IndicatorHazard.canceled += instance.OnIndicatorHazard;
                FogLights.started += instance.OnFogLights;
                FogLights.performed += instance.OnFogLights;
                FogLights.canceled += instance.OnFogLights;
                ShiftFirst.started += instance.OnShiftFirst;
                ShiftFirst.performed += instance.OnShiftFirst;
                ShiftFirst.canceled += instance.OnShiftFirst;
                ShiftReverse.started += instance.OnShiftReverse;
                ShiftReverse.performed += instance.OnShiftReverse;
                ShiftReverse.canceled += instance.OnShiftReverse;
                ParkingBrake.started += instance.OnParkingBrake;
                ParkingBrake.performed += instance.OnParkingBrake;
                ParkingBrake.canceled += instance.OnParkingBrake;
                Ignition.started += instance.OnIgnition;
                Ignition.performed += instance.OnIgnition;
                Ignition.canceled += instance.OnIgnition;
                InteriorLight.started += instance.OnInteriorLight;
                InteriorLight.performed += instance.OnInteriorLight;
                InteriorLight.canceled += instance.OnInteriorLight;
            }
        }
    }
    public VehicleKeyboardActions @VehicleKeyboard
    {
        get
        {
            return new VehicleKeyboardActions(this);
        }
    }

    // VehicleWheel
    private InputActionMap m_VehicleWheel;
    private IVehicleWheelActions m_VehicleWheelActionsCallbackInterface;
    private InputAction m_VehicleWheel_Accel;
    private InputAction m_VehicleWheel_Steer;
    private InputAction m_VehicleWheel_Brake;
    private InputAction m_VehicleWheel_ButtonA;
    private InputAction m_VehicleWheel_ButtonB;
    private InputAction m_VehicleWheel_ButtonX;
    private InputAction m_VehicleWheel_ButtonY;
    private InputAction m_VehicleWheel_ButtonRB;
    private InputAction m_VehicleWheel_ButtonLB;
    private InputAction m_VehicleWheel_ButtonStart;
    private InputAction m_VehicleWheel_ButtonSelect;
    private InputAction m_VehicleWheel_ButtonRSB;
    private InputAction m_VehicleWheel_ButtonLSB;
    private InputAction m_VehicleWheel_ButtonCenter;
    private InputAction m_VehicleWheel_DPad;
    public struct VehicleWheelActions
    {
        private SimulatorControls m_Wrapper;
        public VehicleWheelActions(SimulatorControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @Accel { get { return m_Wrapper.m_VehicleWheel_Accel; } }
        public InputAction @Steer { get { return m_Wrapper.m_VehicleWheel_Steer; } }
        public InputAction @Brake { get { return m_Wrapper.m_VehicleWheel_Brake; } }
        public InputAction @ButtonA { get { return m_Wrapper.m_VehicleWheel_ButtonA; } }
        public InputAction @ButtonB { get { return m_Wrapper.m_VehicleWheel_ButtonB; } }
        public InputAction @ButtonX { get { return m_Wrapper.m_VehicleWheel_ButtonX; } }
        public InputAction @ButtonY { get { return m_Wrapper.m_VehicleWheel_ButtonY; } }
        public InputAction @ButtonRB { get { return m_Wrapper.m_VehicleWheel_ButtonRB; } }
        public InputAction @ButtonLB { get { return m_Wrapper.m_VehicleWheel_ButtonLB; } }
        public InputAction @ButtonStart { get { return m_Wrapper.m_VehicleWheel_ButtonStart; } }
        public InputAction @ButtonSelect { get { return m_Wrapper.m_VehicleWheel_ButtonSelect; } }
        public InputAction @ButtonRSB { get { return m_Wrapper.m_VehicleWheel_ButtonRSB; } }
        public InputAction @ButtonLSB { get { return m_Wrapper.m_VehicleWheel_ButtonLSB; } }
        public InputAction @ButtonCenter { get { return m_Wrapper.m_VehicleWheel_ButtonCenter; } }
        public InputAction @DPad { get { return m_Wrapper.m_VehicleWheel_DPad; } }
        public InputActionMap Get() { return m_Wrapper.m_VehicleWheel; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled { get { return Get().enabled; } }
        public InputActionMap Clone() { return Get().Clone(); }
        public static implicit operator InputActionMap(VehicleWheelActions set) { return set.Get(); }
        public void SetCallbacks(IVehicleWheelActions instance)
        {
            if (m_Wrapper.m_VehicleWheelActionsCallbackInterface != null)
            {
                Accel.started -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnAccel;
                Accel.performed -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnAccel;
                Accel.canceled -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnAccel;
                Steer.started -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnSteer;
                Steer.performed -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnSteer;
                Steer.canceled -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnSteer;
                Brake.started -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnBrake;
                Brake.performed -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnBrake;
                Brake.canceled -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnBrake;
                ButtonA.started -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnButtonA;
                ButtonA.performed -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnButtonA;
                ButtonA.canceled -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnButtonA;
                ButtonB.started -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnButtonB;
                ButtonB.performed -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnButtonB;
                ButtonB.canceled -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnButtonB;
                ButtonX.started -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnButtonX;
                ButtonX.performed -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnButtonX;
                ButtonX.canceled -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnButtonX;
                ButtonY.started -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnButtonY;
                ButtonY.performed -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnButtonY;
                ButtonY.canceled -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnButtonY;
                ButtonRB.started -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnButtonRB;
                ButtonRB.performed -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnButtonRB;
                ButtonRB.canceled -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnButtonRB;
                ButtonLB.started -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnButtonLB;
                ButtonLB.performed -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnButtonLB;
                ButtonLB.canceled -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnButtonLB;
                ButtonStart.started -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnButtonStart;
                ButtonStart.performed -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnButtonStart;
                ButtonStart.canceled -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnButtonStart;
                ButtonSelect.started -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnButtonSelect;
                ButtonSelect.performed -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnButtonSelect;
                ButtonSelect.canceled -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnButtonSelect;
                ButtonRSB.started -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnButtonRSB;
                ButtonRSB.performed -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnButtonRSB;
                ButtonRSB.canceled -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnButtonRSB;
                ButtonLSB.started -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnButtonLSB;
                ButtonLSB.performed -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnButtonLSB;
                ButtonLSB.canceled -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnButtonLSB;
                ButtonCenter.started -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnButtonCenter;
                ButtonCenter.performed -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnButtonCenter;
                ButtonCenter.canceled -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnButtonCenter;
                DPad.started -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnDPad;
                DPad.performed -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnDPad;
                DPad.canceled -= m_Wrapper.m_VehicleWheelActionsCallbackInterface.OnDPad;
            }
            m_Wrapper.m_VehicleWheelActionsCallbackInterface = instance;
            if (instance != null)
            {
                Accel.started += instance.OnAccel;
                Accel.performed += instance.OnAccel;
                Accel.canceled += instance.OnAccel;
                Steer.started += instance.OnSteer;
                Steer.performed += instance.OnSteer;
                Steer.canceled += instance.OnSteer;
                Brake.started += instance.OnBrake;
                Brake.performed += instance.OnBrake;
                Brake.canceled += instance.OnBrake;
                ButtonA.started += instance.OnButtonA;
                ButtonA.performed += instance.OnButtonA;
                ButtonA.canceled += instance.OnButtonA;
                ButtonB.started += instance.OnButtonB;
                ButtonB.performed += instance.OnButtonB;
                ButtonB.canceled += instance.OnButtonB;
                ButtonX.started += instance.OnButtonX;
                ButtonX.performed += instance.OnButtonX;
                ButtonX.canceled += instance.OnButtonX;
                ButtonY.started += instance.OnButtonY;
                ButtonY.performed += instance.OnButtonY;
                ButtonY.canceled += instance.OnButtonY;
                ButtonRB.started += instance.OnButtonRB;
                ButtonRB.performed += instance.OnButtonRB;
                ButtonRB.canceled += instance.OnButtonRB;
                ButtonLB.started += instance.OnButtonLB;
                ButtonLB.performed += instance.OnButtonLB;
                ButtonLB.canceled += instance.OnButtonLB;
                ButtonStart.started += instance.OnButtonStart;
                ButtonStart.performed += instance.OnButtonStart;
                ButtonStart.canceled += instance.OnButtonStart;
                ButtonSelect.started += instance.OnButtonSelect;
                ButtonSelect.performed += instance.OnButtonSelect;
                ButtonSelect.canceled += instance.OnButtonSelect;
                ButtonRSB.started += instance.OnButtonRSB;
                ButtonRSB.performed += instance.OnButtonRSB;
                ButtonRSB.canceled += instance.OnButtonRSB;
                ButtonLSB.started += instance.OnButtonLSB;
                ButtonLSB.performed += instance.OnButtonLSB;
                ButtonLSB.canceled += instance.OnButtonLSB;
                ButtonCenter.started += instance.OnButtonCenter;
                ButtonCenter.performed += instance.OnButtonCenter;
                ButtonCenter.canceled += instance.OnButtonCenter;
                DPad.started += instance.OnDPad;
                DPad.performed += instance.OnDPad;
                DPad.canceled += instance.OnDPad;
            }
        }
    }
    public VehicleWheelActions @VehicleWheel
    {
        get
        {
            return new VehicleWheelActions(this);
        }
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
    public interface IVehicleKeyboardActions
    {
        void OnDirection(InputAction.CallbackContext context);
        void OnHeadLights(InputAction.CallbackContext context);
        void OnIndicatorLeft(InputAction.CallbackContext context);
        void OnIndicatorRight(InputAction.CallbackContext context);
        void OnIndicatorHazard(InputAction.CallbackContext context);
        void OnFogLights(InputAction.CallbackContext context);
        void OnShiftFirst(InputAction.CallbackContext context);
        void OnShiftReverse(InputAction.CallbackContext context);
        void OnParkingBrake(InputAction.CallbackContext context);
        void OnIgnition(InputAction.CallbackContext context);
        void OnInteriorLight(InputAction.CallbackContext context);
    }
    public interface IVehicleWheelActions
    {
        void OnAccel(InputAction.CallbackContext context);
        void OnSteer(InputAction.CallbackContext context);
        void OnBrake(InputAction.CallbackContext context);
        void OnButtonA(InputAction.CallbackContext context);
        void OnButtonB(InputAction.CallbackContext context);
        void OnButtonX(InputAction.CallbackContext context);
        void OnButtonY(InputAction.CallbackContext context);
        void OnButtonRB(InputAction.CallbackContext context);
        void OnButtonLB(InputAction.CallbackContext context);
        void OnButtonStart(InputAction.CallbackContext context);
        void OnButtonSelect(InputAction.CallbackContext context);
        void OnButtonRSB(InputAction.CallbackContext context);
        void OnButtonLSB(InputAction.CallbackContext context);
        void OnButtonCenter(InputAction.CallbackContext context);
        void OnDPad(InputAction.CallbackContext context);
    }
}
