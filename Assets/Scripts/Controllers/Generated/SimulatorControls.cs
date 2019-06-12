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
                },
                {
                    ""name"": ""HeadLights"",
                    ""id"": ""40274b20-ac22-443c-9d73-34806acb1af2"",
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
                    ""id"": ""db041944-2bde-4002-9789-bf423989a8fe"",
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
                    ""id"": ""4a25132c-bf0b-4814-b33c-47027fc3ced8"",
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
                    ""id"": ""55445f93-a66f-40a0-a7c9-791235521dd6"",
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
                    ""id"": ""8e1561cc-c969-46aa-b942-f18bafb8cb1c"",
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
                    ""id"": ""ccc4b2c4-89b6-4187-8a4b-61483dba6a02"",
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
                    ""id"": ""fb2447a3-fbbd-4eb8-8d62-8e6241fd2157"",
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
                    ""id"": ""c99912f9-c925-43bd-98a2-068a5046e01e"",
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
                    ""id"": ""17245b2e-2e67-412e-b916-d522510c025c"",
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
                    ""id"": ""dae1c704-1393-4ac2-91ac-5157897ccf75"",
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
                },
                {
                    ""name"": """",
                    ""id"": ""c07ff75d-6917-4328-a7a3-372e7f1a89fa"",
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
                    ""id"": ""f071a7ab-903c-4f73-9277-245135e92351"",
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
                    ""id"": ""1c9be836-d82f-4f2f-b7fb-17cfc3ba4c42"",
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
                    ""id"": ""102d8903-7ece-4382-8884-6ef9a3895b16"",
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
                    ""id"": ""b8df5b19-f69d-41d2-9468-ab9275906f55"",
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
                    ""id"": ""733fad55-0dc7-4f5f-9524-da094a719742"",
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
                    ""id"": ""9f3653bb-d12f-464e-91a1-1e38d5432b7c"",
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
                    ""id"": ""0b7bdefd-cff1-47e4-82a9-0a835e3d4539"",
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
                    ""id"": ""9b612607-638c-43ac-8787-e9a2df3ed7e6"",
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
                    ""id"": ""28f73087-466a-4ebc-89a7-794827f20e7b"",
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
                    ""interactions"": ""Press(behavior=2)"",
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
                    ""interactions"": ""Press(behavior=2)"",
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
                    ""interactions"": ""Press(behavior=2)"",
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
                    ""interactions"": ""Press(behavior=2)"",
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
                    ""interactions"": """",
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
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Zoom"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true,
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
        }
    ],
    ""controlSchemes"": []
}");
        // Vehicle
        m_Vehicle = asset.GetActionMap("Vehicle");
        m_Vehicle_Direction = m_Vehicle.GetAction("Direction");
        m_Vehicle_HeadLights = m_Vehicle.GetAction("HeadLights");
        m_Vehicle_IndicatorLeft = m_Vehicle.GetAction("IndicatorLeft");
        m_Vehicle_IndicatorRight = m_Vehicle.GetAction("IndicatorRight");
        m_Vehicle_IndicatorHazard = m_Vehicle.GetAction("IndicatorHazard");
        m_Vehicle_FogLights = m_Vehicle.GetAction("FogLights");
        m_Vehicle_ShiftFirst = m_Vehicle.GetAction("ShiftFirst");
        m_Vehicle_ShiftReverse = m_Vehicle.GetAction("ShiftReverse");
        m_Vehicle_ParkingBrake = m_Vehicle.GetAction("ParkingBrake");
        m_Vehicle_Ignition = m_Vehicle.GetAction("Ignition");
        m_Vehicle_InteriorLight = m_Vehicle.GetAction("InteriorLight");
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
        // Simulator
        m_Simulator = asset.GetActionMap("Simulator");
        m_Simulator_ToggleNPCS = m_Simulator.GetAction("ToggleNPCS");
        m_Simulator_ToggleAgent = m_Simulator.GetAction("ToggleAgent");
        m_Simulator_ToggleReset = m_Simulator.GetAction("ToggleReset");
        m_Simulator_ToggleControlsUI = m_Simulator.GetAction("ToggleControlsUI");
        m_Simulator_TogglePedestrians = m_Simulator.GetAction("TogglePedestrians");
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
    private InputAction m_Vehicle_HeadLights;
    private InputAction m_Vehicle_IndicatorLeft;
    private InputAction m_Vehicle_IndicatorRight;
    private InputAction m_Vehicle_IndicatorHazard;
    private InputAction m_Vehicle_FogLights;
    private InputAction m_Vehicle_ShiftFirst;
    private InputAction m_Vehicle_ShiftReverse;
    private InputAction m_Vehicle_ParkingBrake;
    private InputAction m_Vehicle_Ignition;
    private InputAction m_Vehicle_InteriorLight;
    public struct VehicleActions
    {
        private SimulatorControls m_Wrapper;
        public VehicleActions(SimulatorControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @Direction { get { return m_Wrapper.m_Vehicle_Direction; } }
        public InputAction @HeadLights { get { return m_Wrapper.m_Vehicle_HeadLights; } }
        public InputAction @IndicatorLeft { get { return m_Wrapper.m_Vehicle_IndicatorLeft; } }
        public InputAction @IndicatorRight { get { return m_Wrapper.m_Vehicle_IndicatorRight; } }
        public InputAction @IndicatorHazard { get { return m_Wrapper.m_Vehicle_IndicatorHazard; } }
        public InputAction @FogLights { get { return m_Wrapper.m_Vehicle_FogLights; } }
        public InputAction @ShiftFirst { get { return m_Wrapper.m_Vehicle_ShiftFirst; } }
        public InputAction @ShiftReverse { get { return m_Wrapper.m_Vehicle_ShiftReverse; } }
        public InputAction @ParkingBrake { get { return m_Wrapper.m_Vehicle_ParkingBrake; } }
        public InputAction @Ignition { get { return m_Wrapper.m_Vehicle_Ignition; } }
        public InputAction @InteriorLight { get { return m_Wrapper.m_Vehicle_InteriorLight; } }
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
                HeadLights.started -= m_Wrapper.m_VehicleActionsCallbackInterface.OnHeadLights;
                HeadLights.performed -= m_Wrapper.m_VehicleActionsCallbackInterface.OnHeadLights;
                HeadLights.canceled -= m_Wrapper.m_VehicleActionsCallbackInterface.OnHeadLights;
                IndicatorLeft.started -= m_Wrapper.m_VehicleActionsCallbackInterface.OnIndicatorLeft;
                IndicatorLeft.performed -= m_Wrapper.m_VehicleActionsCallbackInterface.OnIndicatorLeft;
                IndicatorLeft.canceled -= m_Wrapper.m_VehicleActionsCallbackInterface.OnIndicatorLeft;
                IndicatorRight.started -= m_Wrapper.m_VehicleActionsCallbackInterface.OnIndicatorRight;
                IndicatorRight.performed -= m_Wrapper.m_VehicleActionsCallbackInterface.OnIndicatorRight;
                IndicatorRight.canceled -= m_Wrapper.m_VehicleActionsCallbackInterface.OnIndicatorRight;
                IndicatorHazard.started -= m_Wrapper.m_VehicleActionsCallbackInterface.OnIndicatorHazard;
                IndicatorHazard.performed -= m_Wrapper.m_VehicleActionsCallbackInterface.OnIndicatorHazard;
                IndicatorHazard.canceled -= m_Wrapper.m_VehicleActionsCallbackInterface.OnIndicatorHazard;
                FogLights.started -= m_Wrapper.m_VehicleActionsCallbackInterface.OnFogLights;
                FogLights.performed -= m_Wrapper.m_VehicleActionsCallbackInterface.OnFogLights;
                FogLights.canceled -= m_Wrapper.m_VehicleActionsCallbackInterface.OnFogLights;
                ShiftFirst.started -= m_Wrapper.m_VehicleActionsCallbackInterface.OnShiftFirst;
                ShiftFirst.performed -= m_Wrapper.m_VehicleActionsCallbackInterface.OnShiftFirst;
                ShiftFirst.canceled -= m_Wrapper.m_VehicleActionsCallbackInterface.OnShiftFirst;
                ShiftReverse.started -= m_Wrapper.m_VehicleActionsCallbackInterface.OnShiftReverse;
                ShiftReverse.performed -= m_Wrapper.m_VehicleActionsCallbackInterface.OnShiftReverse;
                ShiftReverse.canceled -= m_Wrapper.m_VehicleActionsCallbackInterface.OnShiftReverse;
                ParkingBrake.started -= m_Wrapper.m_VehicleActionsCallbackInterface.OnParkingBrake;
                ParkingBrake.performed -= m_Wrapper.m_VehicleActionsCallbackInterface.OnParkingBrake;
                ParkingBrake.canceled -= m_Wrapper.m_VehicleActionsCallbackInterface.OnParkingBrake;
                Ignition.started -= m_Wrapper.m_VehicleActionsCallbackInterface.OnIgnition;
                Ignition.performed -= m_Wrapper.m_VehicleActionsCallbackInterface.OnIgnition;
                Ignition.canceled -= m_Wrapper.m_VehicleActionsCallbackInterface.OnIgnition;
                InteriorLight.started -= m_Wrapper.m_VehicleActionsCallbackInterface.OnInteriorLight;
                InteriorLight.performed -= m_Wrapper.m_VehicleActionsCallbackInterface.OnInteriorLight;
                InteriorLight.canceled -= m_Wrapper.m_VehicleActionsCallbackInterface.OnInteriorLight;
            }
            m_Wrapper.m_VehicleActionsCallbackInterface = instance;
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
    private InputAction m_Camera_ToggleState;
    private InputAction m_Camera_Zoom;
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
    public interface IVehicleActions
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
