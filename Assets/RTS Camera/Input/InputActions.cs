// GENERATED AUTOMATICALLY FROM 'Assets/RTS Camera/Input/InputActions.inputactions'

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class @InputActions : IInputActionCollection, IDisposable
{
    public InputActionAsset asset { get; }
    public @InputActions()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""InputActions"",
    ""maps"": [
        {
            ""name"": ""Gameplay"",
            ""id"": ""5bc8a2ef-5b3d-431a-a440-b862bd39bcf1"",
            ""actions"": [
                {
                    ""name"": ""Camera Pan Buttons"",
                    ""type"": ""Value"",
                    ""id"": ""1efe6350-d745-4328-8cc7-7d540d616a58"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Camera Zoom"",
                    ""type"": ""Value"",
                    ""id"": ""1101e4d3-2666-402e-834e-1e514a2e3b88"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Camera Vertical Translate"",
                    ""type"": ""Value"",
                    ""id"": ""8814231b-7b33-4f2a-b247-69c100928cd9"",
                    ""expectedControlType"": """",
                    ""processors"": ""InvertVector2(invertX=false)"",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Camera Pan Edge"",
                    ""type"": ""Button"",
                    ""id"": ""2f7dc506-233c-4031-89c2-67b1c958dc13"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Camera Pan Grip"",
                    ""type"": ""Button"",
                    ""id"": ""09073db3-da69-4cf0-8da0-a8ba3f34b28a"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Hold""
                },
                {
                    ""name"": ""Camera Pan Grip Delta"",
                    ""type"": ""Button"",
                    ""id"": ""723bfd86-a696-43a2-9022-e04d624ff2e4"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Camera Rotate"",
                    ""type"": ""Value"",
                    ""id"": ""598db628-0f4d-4ee5-b886-226834819be7"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": ""InvertVector2(invertX=false)"",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Camera Reset"",
                    ""type"": ""Button"",
                    ""id"": ""1a0a7a07-8d19-4b96-bcda-a9a360a56881"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Left Click"",
                    ""type"": ""Button"",
                    ""id"": ""d6848d3b-57a7-4bfe-88d5-53706fe46052"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Tap""
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""d367bd55-1234-427d-a237-e5c4a10dacbc"",
                    ""path"": ""<Mouse>/scroll"",
                    ""interactions"": """",
                    ""processors"": ""NormalizeVector2,InvertVector2"",
                    ""groups"": ""Keyboard & Mouse"",
                    ""action"": ""Camera Zoom"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""f9955b0b-abba-4531-97f0-50615c3ad8ef"",
                    ""path"": ""<Mouse>/position"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard & Mouse"",
                    ""action"": ""Camera Pan Edge"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""b388bc52-dd8f-44b2-8784-2a4ff1235abf"",
                    ""path"": ""<Mouse>/middleButton"",
                    ""interactions"": ""Hold(duration=0.05,pressPoint=0.1)"",
                    ""processors"": """",
                    ""groups"": ""Keyboard & Mouse"",
                    ""action"": ""Camera Pan Grip"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""21861681-d119-4d40-90bf-2fcf06dd9a7f"",
                    ""path"": ""<Mouse>/delta"",
                    ""interactions"": """",
                    ""processors"": ""NormalizeVector2,InvertVector2"",
                    ""groups"": ""Keyboard & Mouse"",
                    ""action"": ""Camera Pan Grip Delta"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""2D Vector"",
                    ""id"": ""d5309ad3-f23e-4100-9686-ce6e7604d269"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Camera Rotate"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""cf1e321b-0798-4674-b41a-3bea99df91c5"",
                    ""path"": ""<Keyboard>/c"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard & Mouse"",
                    ""action"": ""Camera Rotate"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""3d99556d-b331-4f3e-8e63-e624d89a00fb"",
                    ""path"": ""<Keyboard>/v"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard & Mouse"",
                    ""action"": ""Camera Rotate"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""2a3f2845-36c1-4035-a8d4-51d1c27a3876"",
                    ""path"": ""<Keyboard>/q"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard & Mouse"",
                    ""action"": ""Camera Rotate"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""bc5918dc-b103-49c3-9ce4-d1610a752493"",
                    ""path"": ""<Keyboard>/e"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard & Mouse"",
                    ""action"": ""Camera Rotate"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""WASD"",
                    ""id"": ""e512d896-b4b9-44e1-82c0-f4eb3d04a514"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard & Mouse"",
                    ""action"": ""Camera Pan Buttons"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""734d69a9-b96c-4d0b-82e4-f00df6095e8f"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard & Mouse"",
                    ""action"": ""Camera Pan Buttons"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""db1e2e72-2845-4585-972f-22f759c91db4"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard & Mouse"",
                    ""action"": ""Camera Pan Buttons"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""27f27420-809a-477b-9804-38537513ef22"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard & Mouse"",
                    ""action"": ""Camera Pan Buttons"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""fe3ddd5e-9ac6-4949-8b94-318639584c48"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard & Mouse"",
                    ""action"": ""Camera Pan Buttons"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""Arrows"",
                    ""id"": ""6a314c21-674d-49eb-8819-c63f32b5fc84"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard & Mouse"",
                    ""action"": ""Camera Pan Buttons"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""12a9d921-2c30-4be6-8c0c-2c98e6f3147c"",
                    ""path"": ""<Keyboard>/upArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard & Mouse"",
                    ""action"": ""Camera Pan Buttons"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""d3da7337-0510-4be6-8b55-f19ce315a195"",
                    ""path"": ""<Keyboard>/downArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard & Mouse"",
                    ""action"": ""Camera Pan Buttons"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""0b2abaed-e8d0-4380-a32b-bb25103ae0ce"",
                    ""path"": ""<Keyboard>/leftArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard & Mouse"",
                    ""action"": ""Camera Pan Buttons"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""af9fd996-9cc2-4161-bcc9-133da9e99aec"",
                    ""path"": ""<Keyboard>/rightArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard & Mouse"",
                    ""action"": ""Camera Pan Buttons"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""a8c5d976-02bf-49c1-90c1-acf15965880c"",
                    ""path"": ""<Mouse>/scroll"",
                    ""interactions"": """",
                    ""processors"": ""NormalizeVector2"",
                    ""groups"": ""Keyboard & Mouse"",
                    ""action"": ""Camera Vertical Translate"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""ZX"",
                    ""id"": ""af1e7b47-8c7a-4863-951b-c3e9175446e1"",
                    ""path"": ""1DAxis"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Camera Vertical Translate"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""negative"",
                    ""id"": ""0073653f-aa6d-493b-b651-23bf52c3fe8f"",
                    ""path"": ""<Keyboard>/z"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard & Mouse"",
                    ""action"": ""Camera Vertical Translate"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""positive"",
                    ""id"": ""ff10e467-00ff-4181-a0f2-c2e4c2e3d245"",
                    ""path"": ""<Keyboard>/x"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard & Mouse"",
                    ""action"": ""Camera Vertical Translate"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""1dbaf5f6-9e0b-49f9-8f80-621306416688"",
                    ""path"": ""<Keyboard>/space"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard & Mouse"",
                    ""action"": ""Camera Reset"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""27b64c2d-d63c-4a04-b25c-01ac4a1c8771"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard & Mouse"",
                    ""action"": ""Left Click"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": [
        {
            ""name"": ""Keyboard & Mouse"",
            ""bindingGroup"": ""Keyboard & Mouse"",
            ""devices"": [
                {
                    ""devicePath"": ""<Keyboard>"",
                    ""isOptional"": false,
                    ""isOR"": false
                },
                {
                    ""devicePath"": ""<Mouse>"",
                    ""isOptional"": false,
                    ""isOR"": false
                }
            ]
        }
    ]
}");
        // Gameplay
        m_Gameplay = asset.FindActionMap("Gameplay", throwIfNotFound: true);
        m_Gameplay_CameraPanButtons = m_Gameplay.FindAction("Camera Pan Buttons", throwIfNotFound: true);
        m_Gameplay_CameraZoom = m_Gameplay.FindAction("Camera Zoom", throwIfNotFound: true);
        m_Gameplay_CameraVerticalTranslate = m_Gameplay.FindAction("Camera Vertical Translate", throwIfNotFound: true);
        m_Gameplay_CameraPanEdge = m_Gameplay.FindAction("Camera Pan Edge", throwIfNotFound: true);
        m_Gameplay_CameraPanGrip = m_Gameplay.FindAction("Camera Pan Grip", throwIfNotFound: true);
        m_Gameplay_CameraPanGripDelta = m_Gameplay.FindAction("Camera Pan Grip Delta", throwIfNotFound: true);
        m_Gameplay_CameraRotate = m_Gameplay.FindAction("Camera Rotate", throwIfNotFound: true);
        m_Gameplay_CameraReset = m_Gameplay.FindAction("Camera Reset", throwIfNotFound: true);
        m_Gameplay_LeftClick = m_Gameplay.FindAction("Left Click", throwIfNotFound: true);
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

    // Gameplay
    private readonly InputActionMap m_Gameplay;
    private IGameplayActions m_GameplayActionsCallbackInterface;
    private readonly InputAction m_Gameplay_CameraPanButtons;
    private readonly InputAction m_Gameplay_CameraZoom;
    private readonly InputAction m_Gameplay_CameraVerticalTranslate;
    private readonly InputAction m_Gameplay_CameraPanEdge;
    private readonly InputAction m_Gameplay_CameraPanGrip;
    private readonly InputAction m_Gameplay_CameraPanGripDelta;
    private readonly InputAction m_Gameplay_CameraRotate;
    private readonly InputAction m_Gameplay_CameraReset;
    private readonly InputAction m_Gameplay_LeftClick;
    public struct GameplayActions
    {
        private @InputActions m_Wrapper;
        public GameplayActions(@InputActions wrapper) { m_Wrapper = wrapper; }
        public InputAction @CameraPanButtons => m_Wrapper.m_Gameplay_CameraPanButtons;
        public InputAction @CameraZoom => m_Wrapper.m_Gameplay_CameraZoom;
        public InputAction @CameraVerticalTranslate => m_Wrapper.m_Gameplay_CameraVerticalTranslate;
        public InputAction @CameraPanEdge => m_Wrapper.m_Gameplay_CameraPanEdge;
        public InputAction @CameraPanGrip => m_Wrapper.m_Gameplay_CameraPanGrip;
        public InputAction @CameraPanGripDelta => m_Wrapper.m_Gameplay_CameraPanGripDelta;
        public InputAction @CameraRotate => m_Wrapper.m_Gameplay_CameraRotate;
        public InputAction @CameraReset => m_Wrapper.m_Gameplay_CameraReset;
        public InputAction @LeftClick => m_Wrapper.m_Gameplay_LeftClick;
        public InputActionMap Get() { return m_Wrapper.m_Gameplay; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(GameplayActions set) { return set.Get(); }
        public void SetCallbacks(IGameplayActions instance)
        {
            if (m_Wrapper.m_GameplayActionsCallbackInterface != null)
            {
                @CameraPanButtons.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnCameraPanButtons;
                @CameraPanButtons.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnCameraPanButtons;
                @CameraPanButtons.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnCameraPanButtons;
                @CameraZoom.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnCameraZoom;
                @CameraZoom.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnCameraZoom;
                @CameraZoom.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnCameraZoom;
                @CameraVerticalTranslate.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnCameraVerticalTranslate;
                @CameraVerticalTranslate.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnCameraVerticalTranslate;
                @CameraVerticalTranslate.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnCameraVerticalTranslate;
                @CameraPanEdge.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnCameraPanEdge;
                @CameraPanEdge.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnCameraPanEdge;
                @CameraPanEdge.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnCameraPanEdge;
                @CameraPanGrip.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnCameraPanGrip;
                @CameraPanGrip.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnCameraPanGrip;
                @CameraPanGrip.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnCameraPanGrip;
                @CameraPanGripDelta.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnCameraPanGripDelta;
                @CameraPanGripDelta.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnCameraPanGripDelta;
                @CameraPanGripDelta.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnCameraPanGripDelta;
                @CameraRotate.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnCameraRotate;
                @CameraRotate.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnCameraRotate;
                @CameraRotate.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnCameraRotate;
                @CameraReset.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnCameraReset;
                @CameraReset.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnCameraReset;
                @CameraReset.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnCameraReset;
                @LeftClick.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnLeftClick;
                @LeftClick.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnLeftClick;
                @LeftClick.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnLeftClick;
            }
            m_Wrapper.m_GameplayActionsCallbackInterface = instance;
            if (instance != null)
            {
                @CameraPanButtons.started += instance.OnCameraPanButtons;
                @CameraPanButtons.performed += instance.OnCameraPanButtons;
                @CameraPanButtons.canceled += instance.OnCameraPanButtons;
                @CameraZoom.started += instance.OnCameraZoom;
                @CameraZoom.performed += instance.OnCameraZoom;
                @CameraZoom.canceled += instance.OnCameraZoom;
                @CameraVerticalTranslate.started += instance.OnCameraVerticalTranslate;
                @CameraVerticalTranslate.performed += instance.OnCameraVerticalTranslate;
                @CameraVerticalTranslate.canceled += instance.OnCameraVerticalTranslate;
                @CameraPanEdge.started += instance.OnCameraPanEdge;
                @CameraPanEdge.performed += instance.OnCameraPanEdge;
                @CameraPanEdge.canceled += instance.OnCameraPanEdge;
                @CameraPanGrip.started += instance.OnCameraPanGrip;
                @CameraPanGrip.performed += instance.OnCameraPanGrip;
                @CameraPanGrip.canceled += instance.OnCameraPanGrip;
                @CameraPanGripDelta.started += instance.OnCameraPanGripDelta;
                @CameraPanGripDelta.performed += instance.OnCameraPanGripDelta;
                @CameraPanGripDelta.canceled += instance.OnCameraPanGripDelta;
                @CameraRotate.started += instance.OnCameraRotate;
                @CameraRotate.performed += instance.OnCameraRotate;
                @CameraRotate.canceled += instance.OnCameraRotate;
                @CameraReset.started += instance.OnCameraReset;
                @CameraReset.performed += instance.OnCameraReset;
                @CameraReset.canceled += instance.OnCameraReset;
                @LeftClick.started += instance.OnLeftClick;
                @LeftClick.performed += instance.OnLeftClick;
                @LeftClick.canceled += instance.OnLeftClick;
            }
        }
    }
    public GameplayActions @Gameplay => new GameplayActions(this);
    private int m_KeyboardMouseSchemeIndex = -1;
    public InputControlScheme KeyboardMouseScheme
    {
        get
        {
            if (m_KeyboardMouseSchemeIndex == -1) m_KeyboardMouseSchemeIndex = asset.FindControlSchemeIndex("Keyboard & Mouse");
            return asset.controlSchemes[m_KeyboardMouseSchemeIndex];
        }
    }
    public interface IGameplayActions
    {
        void OnCameraPanButtons(InputAction.CallbackContext context);
        void OnCameraZoom(InputAction.CallbackContext context);
        void OnCameraVerticalTranslate(InputAction.CallbackContext context);
        void OnCameraPanEdge(InputAction.CallbackContext context);
        void OnCameraPanGrip(InputAction.CallbackContext context);
        void OnCameraPanGripDelta(InputAction.CallbackContext context);
        void OnCameraRotate(InputAction.CallbackContext context);
        void OnCameraReset(InputAction.CallbackContext context);
        void OnLeftClick(InputAction.CallbackContext context);
    }
}
