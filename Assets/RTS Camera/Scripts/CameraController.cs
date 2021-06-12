using System;
using ModunGames.Enums;
using UnityEngine;
using UnityEngine.InputSystem;


#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif


// ReSharper disable once CheckNamespace
namespace ModunGames {
    // ReSharper disable once UnusedMember.Global
    public class CameraController : MonoBehaviour {


        #region Fields


        private float _epsilon = .00000001f;


        /// <summary>
        /// To be used for translating.
        /// </summary>
        private Transform _cameraRigTransform;


        private Vector3 _cameraStartingPosition;
        private Quaternion _cameraStartingRotation;


        private Camera _camera;


        /// <summary>
        /// To be used for rotating.
        /// </summary>
        private Transform _cameraTransform;


        private Vector2 _cameraPanButtons;
        private Vector2 _cameraPanEdge;
        private Vector2 _cameraPanGrip;


        private float _originalFOV;
        private float _targetFOV;
        private float _zoomVelocity;


        private float _cameraHeightChange;
        private float _targetCameraHeight;
        private bool _verticalTranslateZoomStarted;
        private int _currentVerticalTranslateCount;
        private int _maximumVerticalTranslateCount = 100;


        private Vector2 _cameraRotate;


        private float _previousTimeSinceStartup;
        private float _calculatedDeltaTime;


        #endregion


        #region Properties


        /// <summary>
        /// The Input mapping.
        /// </summary>
        public InputActions Input { get; set; }


        #endregion


        #region Inspector Fields


        #region General


#if ODIN_INSPECTOR
        [BoxGroup("General", false)]
        [Space(-10)]
        [Title("General Settings")]
        [PropertyTooltip("Forward bottom left coordinate of the cube that bounds the camera in the scene.")]
#endif
        public Vector3 MinimumCameraBounds = new Vector3(0, 0, 0);


#if ODIN_INSPECTOR
        [PropertySpace(0, 10)]
        [BoxGroup("General", false)]
        [PropertyTooltip("Backward top right coordinate of the cube that bounds the camera in the scene.")]
#endif
        public Vector3 MaximumCameraBounds = new Vector3(50, 50, 50);


#if ODIN_INSPECTOR
        [PropertySpace(0, 10)]
        [BoxGroup("General", false)]
        [PropertyTooltip("Determines when the camera calculations are calculated.")]
#endif
        public CameraUpdateMethod UpdateMethod = CameraUpdateMethod.Update;


#if ODIN_INSPECTOR
        [PropertySpace(0, 10)]
        [BoxGroup("General", false)]
        [LabelText("Calculate Delta Time")]
        [ToggleLeft]
        [OnValueChanged("UseCalculatedDeltaTimeChanged")]
        [PropertyTooltip("Time.timeScale is often used to pause the game. This setting determines whether or not the camera will also be paused or not.")]
#endif
        public bool UseCalculatedDeltaTime = true;


#if ODIN_INSPECTOR
        [PropertySpace(0, 10)]
        [BoxGroup("General", false)]
        [EnableIf("!IsButtonPanningEnabled")]
        [ToggleLeft]
        [PropertyTooltip("Time.timeScale is often used to pause the game by setting this value to 0. This setting determines whether or not the camera will also be paused or not.")]
#endif
        public bool IgnoreTimeScale = true;
        

        #endregion


        #region Panning


        #region Button Panning


#if ODIN_INSPECTOR
        [BoxGroup("Panning", false)]
        [BoxGroup("Panning/Button Panning", false)]
        [Space(-10)]
        [Title("Button Panning")]
        [LabelText("Button Panning")]
        [ToggleLeft]
        [OnValueChanged("UpdateListeningToInputEvents")]
#endif
        public bool IsButtonPanningEnabled = true;


#if ODIN_INSPECTOR
        [PropertySpace(0, 10)]
        [BoxGroup("Panning/Button Panning", false)]
        [EnableIf("IsButtonPanningEnabled")]
        [LabelText("Speed")]
        [ValidateInput("PositiveFloat", "This value should be greater than zero.")]
        [SuffixLabel("units/s", true)]
#endif
        public float ButtonPanSpeed = 20.0f;


        #endregion


        #region Edge Panning


#if ODIN_INSPECTOR
        [BoxGroup("Panning/Edge Panning", false)]
        [Space(-10)]
        [Title("Edge Panning")]
        [LabelText("Edge Panning")]
        [ToggleLeft]
        [OnValueChanged("UpdateListeningToInputEvents")]
#endif
        public bool IsEdgePanningEnabled = true;


#if ODIN_INSPECTOR
        [BoxGroup("Panning/Edge Panning", false)]
        [EnableIf("IsEdgePanningEnabled")]
        [LabelText("Speed")]
        [ValidateInput("PositiveFloat", "This value should be greater than zero.")]
        [SuffixLabel("units/s", true)]
#endif
        public float EdgePanSpeed = 20.0f;


#if ODIN_INSPECTOR
        [BoxGroup("Panning/Edge Panning", false)]
        [EnableIf("IsEdgePanningEnabled")]
        [LabelText("Inner border")]
        [SuffixLabel("pixels", true)]
        [PropertyTooltip("Number of pixels of the inner border of the game window (inside the game window) in which the cursor still performs a pan operation.")]
#endif
        public uint InnerPanEdgeBorder = 20;


#if ODIN_INSPECTOR
        [PropertySpace(0, 10)]
        [BoxGroup("Panning/Edge Panning", false)]
        [EnableIf("IsEdgePanningEnabled")]
        [LabelText("Outer border")]
        [SuffixLabel("pixels", true)]
        [PropertyTooltip("Number of pixels of the outer border of the game window (outside of the game window) in which the cursor still performs a pan operation.")]
#endif
        public uint OuterPanEdgeBorder = 0;


        #endregion


        #region Grip Panning


#if ODIN_INSPECTOR
        [BoxGroup("Panning/Grip Panning", false)]
        [Space(-10)]
        [Title("Grip Panning")]
        [LabelText("Grip Panning")]
        [ToggleLeft]
        [OnValueChanged("UpdateListeningToInputEvents")]
#endif
        public bool IsGripPanningEnabled = true;


#if ODIN_INSPECTOR
        [PropertySpace(0, 10)]
        [BoxGroup("Panning/Grip Panning", false)]
        [EnableIf("IsGripPanningEnabled")]
        [LabelText("Speed")]
        [ValidateInput("PositiveFloat", "This value should be greater than zero.")]
        [SuffixLabel("units/s", true)]
#endif
        public float GripPanSpeed = 20.0f;


        #endregion


        #endregion


        #region Zooming


        #region FOV Zooming


#if ODIN_INSPECTOR
        [BoxGroup("Zooming", false)]
        [BoxGroup("Zooming/FOV Zooming", false)]
        [Space(-10)]
        [Title("FOV Zooming")]
        [LabelText("Use FOV Zooming")]
        [ToggleLeft]
        [OnValueChanged("UpdateListeningToInputEvents")]
        [OnValueChanged("FOVZoomingChanged")]
        [PropertyTooltip("The camera's field of view (FOV) is changed as a zoom effect.")]
#endif
        public bool IsFOVZoomingEnabled;


#if ODIN_INSPECTOR
        [BoxGroup("Zooming/FOV Zooming", false)]
        [EnableIf("IsFOVZoomingEnabled")]
        [LabelText("Speed")]
        [ValidateInput("PositiveFloat", "This value should be greater than zero.")]
        [SuffixLabel("units/s", true)]
#endif
        public float FOVZoomSpeed = 5.0f;


#if ODIN_INSPECTOR
        [BoxGroup("Zooming/FOV Zooming", false)]
        [EnableIf("IsFOVZoomingEnabled")]
        [LabelText("Zoom Smoothing")]
        [ValidateInput("PositiveFloat", "This value should be greater than zero.")]
        [SuffixLabel("seconds", true)]
        [PropertyTooltip("The time to reach the zoom target.")]
#endif
        public float FOVZoomSmoothingTime = .2f;


#if ODIN_INSPECTOR
        [BoxGroup("Zooming/FOV Zooming", false)]
        [EnableIf("IsFOVZoomingEnabled")]
        [ValidateInput("FOVCheckMin")]
#endif
        public uint MinimumFOV = 10;


#if ODIN_INSPECTOR
        [BoxGroup("Zooming/FOV Zooming", false)]
        [EnableIf("IsFOVZoomingEnabled")]
        [ValidateInput("FOVCheckMax")]
#endif
        public uint MaximumFOV = 100;


        #endregion


        #region Camera Up / Down


#if ODIN_INSPECTOR
        [BoxGroup("Zooming/Vertical Translation Zooming", false)]
        [Space(-10)]
        [Title("Vertical Translation Zooming")]
        [LabelText("Use vertical translation zooming")]
        [ToggleLeft]
        [OnValueChanged("UpdateListeningToInputEvents")]
        [OnValueChanged("TranslationZoomingChanged")]
        [PropertyTooltip("The camera is translated vertically as a zoom effect.")]
#endif
        public bool IsVerticalTranslationZoomingEnabled = true;


#if ODIN_INSPECTOR
        [BoxGroup("Zooming/Vertical Translation Zooming", false)]
        [LabelText("Allow scroll zooming")]
        [ToggleLeft]
        [PropertyTooltip("The camera can be translated vertically using a scrolling operation.")]
#endif
        public bool AllowScrollZooming = true;


#if ODIN_INSPECTOR
        [BoxGroup("Zooming/Vertical Translation Zooming", false)]
        [EnableIf("IsVerticalTranslationZoomingEnabled")]
        [LabelText("Speed")]
        [ValidateInput("PositiveFloat", "This value should be greater than zero.")]
        [SuffixLabel("units/s", true)]
#endif
        public float VerticalTranslationZoomSpeed = 15.0f;


#if ODIN_INSPECTOR
        [BoxGroup("Zooming/Vertical Translation Zooming", false)]
        [EnableIf("IsVerticalTranslationZoomingEnabled")]
        [LabelText("Speed (Scroll)")]
        [ValidateInput("PositiveFloat", "This value should be greater than zero.")]
        [SuffixLabel("units/s", true)]
        [EnableIf("AllowScrollZooming")]
#endif
        public float VerticalTranslationScrollZoomSpeed = 5.0f;


#if ODIN_INSPECTOR
        [BoxGroup("Zooming/Vertical Translation Zooming", false)]
        [EnableIf("IsVerticalTranslationZoomingEnabled")]
        [LabelText("Zoom Smoothing")]
        [ValidateInput("PositiveFloat", "This value should be greater than zero.")]
        [SuffixLabel("seconds", true)]
        [PropertyTooltip("The time to reach the vertical translation zoom target height.")]
#endif
        public float VerticalTranslationZoomSmoothingTime = .2f;


        #endregion


        #endregion


        #region Rotating


#if ODIN_INSPECTOR
        [BoxGroup("Rotating", false)]
        [Space(-10)]
        [Title("Rotating")]
        [LabelText("Rotate Horizontally")]
        [ToggleLeft]
        [OnValueChanged("UpdateListeningToInputEvents")]
#endif
        public bool IsRotateHorizontallyEnabled = true;


#if ODIN_INSPECTOR
        [BoxGroup("Rotating", false)]
        [LabelText("Rotate Vertically")]
        [ToggleLeft]
        [OnValueChanged("UpdateListeningToInputEvents")]
#endif
        public bool IsRotateVerticallyEnabled = true;


#if ODIN_INSPECTOR
        [BoxGroup("Rotating", false)]
        [EnableIf("@this.IsRotateHorizontallyEnabled || this.IsRotateVerticallyEnabled")]
        [LabelText("Speed")]
        [ValidateInput("PositiveFloat", "This value should be greater than zero.")]
        [SuffixLabel("units/s", true)]
#endif
        public float RotateSpeed = 30.0f;


#if ODIN_INSPECTOR
        [BoxGroup("Rotating", false)]
        [EnableIf("@this.IsRotateHorizontallyEnabled || this.IsRotateVerticallyEnabled")]
        [LabelText("Horizontal Rotation Axis")]
#endif
        public Space HorizontalSpace = Space.World;


#if ODIN_INSPECTOR
        [PropertySpace(0, 10)]
        [BoxGroup("Rotating", false)]
        [EnableIf("@this.IsRotateHorizontallyEnabled || this.IsRotateVerticallyEnabled")]
        [LabelText("Vertical Rotation Axis")]
#endif
        public Space VerticalSpace = Space.Self;


        #endregion


        #endregion


        #region MonoBehaviour Overrides


        /// <summary>
        /// Once the objects are instantiated, awake is called before start. Use it to setup references to other objects.
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        protected virtual void Awake() {


            // Initialize the time since startup for manual delta time calculation.
            _previousTimeSinceStartup = Time.realtimeSinceStartup;


            // Create an input mapping instance.
            Input = new InputActions();


            // Cache the Transform for convenience (and small performance boost).
            _cameraRigTransform = gameObject.transform;
            _targetCameraHeight = _cameraRigTransform.position.y;
            _cameraStartingPosition = _cameraRigTransform.position;


            // Try to get the Camera instance.
            if (_camera != null) return;


            // First, check self.
            _camera = gameObject.GetComponent<Camera>();


            if (_camera != null) {
                _originalFOV = _camera.fieldOfView;
                _targetFOV = _originalFOV;
                _cameraTransform = _camera.transform;
                _cameraStartingRotation = _cameraTransform.rotation;
                return;
            }


            // Secondly, if no Camera has been found, check children.
            Camera[] cameras = gameObject.GetComponentsInChildren<Camera>();

            if (cameras == null || cameras.Length <= 0) return;


            // Find the first camera that is enabled.
            foreach (Camera t in cameras) {

                if (!t.enabled) continue;

                _camera = t;
                _originalFOV = _camera.fieldOfView;
                _targetFOV = _originalFOV;
                _cameraTransform = _camera.transform;
                _cameraStartingRotation = _cameraTransform.rotation;

                break;

            }


        }


        /// <summary>
        /// Update is called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        protected virtual void Update() {


            if (UpdateMethod != CameraUpdateMethod.Update || Math.Abs(Time.timeScale) < _epsilon && !IgnoreTimeScale) return;


            // Calculate delta time.
            CalculateDeltaTime();


            // Handle panning.
            (Vector2 panDirection, float panSpeed) = GetPanVariables();
            PanCamera(panDirection, panSpeed);


            // Handle zooming.
            ZoomCamera();


            // Handle rotation.
            RotateCamera();


        }


        /// <summary>
        /// LateUpdate is called every frame. LateUpdate is called after all Update functions have been called.
        /// This is useful to order script execution. For example a follow camera should always be implemented in
        /// LateUpdate because it tracks objects that might have moved inside Update.
        /// </summary>
        // ReSharper disable once UnusedMember.Local
        protected virtual void LateUpdate() {


            if (UpdateMethod != CameraUpdateMethod.LateUpdate || Math.Abs(Time.timeScale) < _epsilon && !IgnoreTimeScale) return;


            // Calculate delta time.
            CalculateDeltaTime();


            // Handle panning.
            (Vector2 panDirection, float panSpeed) = GetPanVariables();
            PanCamera(panDirection, panSpeed);


            // Handle zooming.
            ZoomCamera();


            // Handle rotation.
            RotateCamera();


        }


        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        // ReSharper disable once UnusedMember.Local
        protected virtual void OnEnable() {

            SubscribeToInputEvents();
            UpdateListeningToInputEvents();

        }


        /// <summary>
        /// This function is called when the behaviour becomes disabled.
        /// 
        /// This is also called when the object is destroyed and can be used for any cleanup code.
        /// 
        /// When scripts are reloaded after compilation has finished, OnDisable will be called,
        /// followed by an OnEnable after the script has been loaded.
        /// </summary>
        // ReSharper disable once UnusedMember.Local
        protected virtual void OnDisable() {

            UnsubscribeToInputEvents();
            Input.Disable();

        }


        #endregion


        #region Input


        /// <summary>
        /// Subscribe to the input events we're listening to.
        /// </summary>
        private void SubscribeToInputEvents() {


            if (Input == null) return;


            Input.Gameplay.CameraPanButtons.performed += OnCameraPanButtons;
            Input.Gameplay.CameraPanButtons.canceled += OnCameraPanButtons;

            Input.Gameplay.CameraPanEdge.performed += OnCameraPanEdge;
            Input.Gameplay.CameraPanEdge.canceled += OnCameraPanEdge;

            Input.Gameplay.CameraPanGrip.performed += OnCameraPanGrip;
            Input.Gameplay.CameraPanGrip.canceled += OnCameraPanGrip;

            Input.Gameplay.CameraPanGripDelta.performed += OnCameraPanGripDelta;
            Input.Gameplay.CameraPanGripDelta.canceled += OnCameraPanGripDelta;

            Input.Gameplay.CameraZoom.performed += OnCameraZoom;

            Input.Gameplay.CameraVerticalTranslate.performed += OnCameraVerticalTranslate;
            Input.Gameplay.CameraVerticalTranslate.canceled += OnCameraVerticalTranslate;

            Input.Gameplay.CameraRotate.performed += OnCameraRotate;
            Input.Gameplay.CameraRotate.canceled += OnCameraRotate;

            Input.Gameplay.CameraReset.performed += OnCameraReset;


        }


        /// <summary>
        /// Unsubscribe from the input events we're listening to.
        /// </summary>
        private void UnsubscribeToInputEvents() {


            if (Input == null) return;


            Input.Gameplay.CameraPanButtons.performed -= OnCameraPanButtons;
            Input.Gameplay.CameraPanButtons.canceled -= OnCameraPanButtons;

            Input.Gameplay.CameraPanEdge.performed -= OnCameraPanEdge;
            Input.Gameplay.CameraPanEdge.canceled -= OnCameraPanEdge;

            Input.Gameplay.CameraPanGrip.performed -= OnCameraPanGrip;
            Input.Gameplay.CameraPanGrip.canceled -= OnCameraPanGrip;

            Input.Gameplay.CameraPanGripDelta.performed -= OnCameraPanGripDelta;
            Input.Gameplay.CameraPanGripDelta.canceled -= OnCameraPanGripDelta;

            Input.Gameplay.CameraZoom.performed -= OnCameraZoom;

            Input.Gameplay.CameraVerticalTranslate.performed -= OnCameraVerticalTranslate;
            Input.Gameplay.CameraVerticalTranslate.canceled -= OnCameraVerticalTranslate;

            Input.Gameplay.CameraRotate.performed -= OnCameraRotate;
            Input.Gameplay.CameraRotate.canceled -= OnCameraRotate;

            Input.Gameplay.CameraReset.performed -= OnCameraReset;


        }


        /// <summary>
        /// Updates whether or not to listen for a particular input event.
        /// </summary>
        private void UpdateListeningToInputEvents() {


            if (Input == null) return;


            // Button panning.
            if (IsButtonPanningEnabled && !Input.Gameplay.CameraPanButtons.enabled) {
                Input.Gameplay.CameraPanButtons.Enable();
            } else if (!IsButtonPanningEnabled && Input.Gameplay.CameraPanButtons.enabled) {
                Input.Gameplay.CameraPanButtons.Disable();
            }


            // Edge panning.
            if (IsEdgePanningEnabled && !Input.Gameplay.CameraPanEdge.enabled) {
                Input.Gameplay.CameraPanEdge.Enable();
            } else if (!IsEdgePanningEnabled && Input.Gameplay.CameraPanEdge.enabled) {
                Input.Gameplay.CameraPanEdge.Disable();
            }


            // Grip panning.
            if (IsGripPanningEnabled && !Input.Gameplay.CameraPanGrip.enabled) {
                Input.Gameplay.CameraPanGrip.Enable();
            } else if (!IsGripPanningEnabled && Input.Gameplay.CameraPanGrip.enabled) {
                Input.Gameplay.CameraPanGrip.Disable();
                Input.Gameplay.CameraPanGripDelta.Disable();
            }


            // FOV Zooming.
            if (IsFOVZoomingEnabled && !Input.Gameplay.CameraZoom.enabled) {
                Input.Gameplay.CameraZoom.Enable();
            } else if (!IsFOVZoomingEnabled && Input.Gameplay.CameraZoom.enabled) {
                Input.Gameplay.CameraZoom.Disable();
            }


            // Vertical translation zooming.
            if (IsVerticalTranslationZoomingEnabled && !Input.Gameplay.CameraVerticalTranslate.enabled) {
                Input.Gameplay.CameraVerticalTranslate.Enable();
            } else if (!IsVerticalTranslationZoomingEnabled && Input.Gameplay.CameraVerticalTranslate.enabled) {
                Input.Gameplay.CameraVerticalTranslate.Disable();
            }


            // Rotating.
            if ((IsRotateHorizontallyEnabled || IsRotateVerticallyEnabled) && !Input.Gameplay.CameraRotate.enabled) {
                Input.Gameplay.CameraRotate.Enable();
            } else if (!(IsRotateHorizontallyEnabled || IsRotateVerticallyEnabled) && Input.Gameplay.CameraRotate.enabled) {
                Input.Gameplay.CameraRotate.Disable();
            }


            // Reset.
            if (!Input.Gameplay.CameraReset.enabled)
                Input.Gameplay.CameraReset.Enable();


        }


        #region Input Event Handlers


        /// <summary>
        /// Handles the CameraPanButtons events.
        /// </summary>
        /// <param name="context"></param>
        private void OnCameraPanButtons(InputAction.CallbackContext context) {

            if (!IsButtonPanningEnabled) return;

            // Store movement for next LateUpdate.
            _cameraPanButtons = context.ReadValue<Vector2>();

        }


        /// <summary>
        /// Handles the CameraZoom events.
        /// </summary>
        /// <param name="context"></param>
        private void OnCameraZoom(InputAction.CallbackContext context) {


            if (!IsFOVZoomingEnabled) return;


            // Store zoom for next LateUpdate.

            // Calculate new target Field of View for the camera.
            if (Math.Abs(_targetFOV - _camera.fieldOfView) < _epsilon) {
                _targetFOV = _camera.fieldOfView + context.ReadValue<Vector2>().y * FOVZoomSpeed;
            } else {
                _targetFOV += context.ReadValue<Vector2>().y * FOVZoomSpeed;
            }


            // Clamp it within bounds.
            _targetFOV = Mathf.Clamp(_targetFOV, MinimumFOV, MaximumFOV);


        }


        /// <summary>
        /// Handles the CameraVerticalTranslate events.
        /// </summary>
        /// <param name="context"></param>
        private void OnCameraVerticalTranslate(InputAction.CallbackContext context) {


            if (context.valueType == typeof(float)) {
                _cameraHeightChange = context.ReadValue<float>();
            } else {


                if (!AllowScrollZooming) return;


                // Signal we started a vertical translation due to scrolling.
                _currentVerticalTranslateCount = 0;
                _verticalTranslateZoomStarted = true;


                // Calculate new target height for the camera.
                if (Math.Abs(_targetCameraHeight - _cameraRigTransform.position.y) < _epsilon) {
                    _targetCameraHeight = _cameraRigTransform.position.y + context.ReadValue<Vector2>().y * VerticalTranslationScrollZoomSpeed;
                } else {
                    _targetCameraHeight += context.ReadValue<Vector2>().y * VerticalTranslationScrollZoomSpeed;
                }


                // Clamp it within bounds.
                _targetCameraHeight = Mathf.Clamp(_targetCameraHeight, MinimumCameraBounds.y, MaximumCameraBounds.y);


            }


        }


        /// <summary>
        /// Handles CameraPanEdge events.
        /// </summary>
        /// <param name="context"></param>
        private void OnCameraPanEdge(InputAction.CallbackContext context) {


            if (!IsEdgePanningEnabled) return;


            // Get current mouse position.
            Vector2 position = context.ReadValue<Vector2>();


            // Determine if mouse is in pan area.
            if (!IsCursorInPanEdgeArea(position)) {
                _cameraPanEdge = Vector2.zero;
                return;
            }


            // Create directional vector.
            _cameraPanEdge = ConvertEdgePositionIntoDirectionalVector(position);


        }


        /// <summary>
        /// Handles CameraPanGrip events.
        /// </summary>
        /// <param name="context"></param>
        private void OnCameraPanGrip(InputAction.CallbackContext context) {


            if (!IsGripPanningEnabled) return;


            // This action enables or disables the CameraPanGripDelta action.
            if (context.phase == InputActionPhase.Performed)
                Input.Gameplay.CameraPanGripDelta.Enable();

            if (context.phase == InputActionPhase.Canceled)
                Input.Gameplay.CameraPanGripDelta.Disable();


        }


        /// <summary>
        /// Handles CameraPanGripDelta events.
        /// </summary>
        /// <param name="context"></param>
        private void OnCameraPanGripDelta(InputAction.CallbackContext context) {

            // Store delta for next LateUpdate.
            _cameraPanGrip = context.ReadValue<Vector2>();

        }


        /// <summary>
        /// Handles CameraRotate events.
        /// </summary>
        /// <param name="context"></param>
        private void OnCameraRotate(InputAction.CallbackContext context) {

            if (!IsRotateHorizontallyEnabled && !IsRotateVerticallyEnabled) return;

            // Store rotation for next LateUpdate.
            _cameraRotate = context.ReadValue<Vector2>().Round();

        }


        /// <summary>
        /// Handles CameraReset events.
        /// </summary>
        /// <param name="context"></param>
        private void OnCameraReset(InputAction.CallbackContext context) {

            ResetCamera();

        }


        #endregion


        #region Actual Camera Operations


        /// <summary>
        /// Pans the camera.
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="panSpeed"></param>
        private void PanCamera(Vector2 direction, float panSpeed) {


            if (direction.sqrMagnitude < 0.01) return;


            // Calculate new position.
            float y = _cameraRigTransform.position.y;
            Quaternion rotation = _cameraRigTransform.rotation;


            // Ignore x and z rotation: when x = 90 / -90 or z = 90 / -90 panning will not function correctly, i.e. for x up and down will cease to function and for z left and right.
            _cameraRigTransform.rotation = Quaternion.Euler(0, _cameraRigTransform.eulerAngles.y, 0);


            _cameraRigTransform.Translate(new Vector3(direction.x, 0, direction.y) * panSpeed);
            _cameraRigTransform.position = new Vector3(_cameraRigTransform.position.x, y, _cameraRigTransform.position.z);
            _cameraRigTransform.rotation = rotation;


            // Clamp the position to minimum and maximum bounds.
            _cameraRigTransform.position = _cameraRigTransform.position.Clamp(
                new Vector3(MinimumCameraBounds.x, _cameraRigTransform.position.y, MinimumCameraBounds.z),
                new Vector3(MaximumCameraBounds.x, _cameraRigTransform.position.y, MaximumCameraBounds.z)
            );


        }


        /// <summary>
        /// Zooms the camera.
        /// </summary>
        private void ZoomCamera() {


            if (IsFOVZoomingEnabled)
                FOVZoom();

            if (IsVerticalTranslationZoomingEnabled)
                VerticalTranslateZoom();


        }


        /// <summary>
        /// Zooms the camera using FOV modification.
        /// </summary>
        private void FOVZoom() {

            if (Math.Abs(_targetFOV - _camera.fieldOfView) < _epsilon) return;
            _camera.fieldOfView = Mathf.SmoothDampAngle(_camera.fieldOfView, _targetFOV, ref _zoomVelocity, FOVZoomSmoothingTime);

        }


        /// <summary>
        /// Creates a zooming effect by translating the camera vertically.
        /// </summary>
        private void VerticalTranslateZoom() {


            if (!(Math.Abs(_cameraHeightChange) > _epsilon) &&
                !(Math.Abs(_targetCameraHeight - _cameraRigTransform.position.y) > _epsilon)) {
                _currentVerticalTranslateCount = 0;
                _verticalTranslateZoomStarted = false;
                return;
            }


            // Calculate new position (height).
            if (Math.Abs(_cameraHeightChange) > _epsilon) {

                _cameraRigTransform.position += new Vector3(0, _cameraHeightChange, 0) * VerticalTranslationZoomSpeed * (UseCalculatedDeltaTime ? _calculatedDeltaTime : Time.deltaTime);

                // Make sure the next section (scroll zooming) doesn't trigger.
                _targetCameraHeight = _cameraRigTransform.position.y;

            }


            // Scroll smoothed to new height.
            if (Math.Abs(_targetCameraHeight - _cameraRigTransform.position.y) > _epsilon) {


                if (!_verticalTranslateZoomStarted) {

                    // Make sure to update the target height if the camera's position changed due to outside elements, e.g. user translating the camera in the scene view while the game is running.
                    _targetCameraHeight = _cameraRigTransform.position.y;

                } else {


                    _cameraRigTransform.position = new Vector3(_cameraRigTransform.position.x,
                        Mathf.SmoothDampAngle(_cameraRigTransform.position.y, _targetCameraHeight, ref _zoomVelocity, VerticalTranslationZoomSmoothingTime),
                        _cameraRigTransform.position.z);
                    _currentVerticalTranslateCount += 1;


                    // SmoothDampAngle will not work with very small changes. Making epsilon bigger does not help either as we will still keep trying to smooth.
                    // Therefore, create a hard-cap on how many iterations (frames) will be used for smoothing. After this amount we will stop trying to smooth
                    // to the new height and simply use the current height as the target height.
                    if (_currentVerticalTranslateCount >= _maximumVerticalTranslateCount) {
                        _currentVerticalTranslateCount = 0;
                        _verticalTranslateZoomStarted = false;
                        _targetCameraHeight = _cameraRigTransform.position.y;
                    }


                }


            }


            // Clamp the position to minimum and maximum bounds.
            _cameraRigTransform.position = _cameraRigTransform.position.Clamp(
                new Vector3(_cameraRigTransform.position.x, MinimumCameraBounds.y, _cameraRigTransform.position.z),
                new Vector3(_cameraRigTransform.position.x, MaximumCameraBounds.y, _cameraRigTransform.position.z)
            );


        }


        /// <summary>
        /// Rotates the camera.
        /// </summary>
        private void RotateCamera() {


            if (_cameraRotate.sqrMagnitude < 0.01) return;


            // Rotate horizontally: left / right.
            if (IsRotateHorizontallyEnabled)
                _cameraTransform.Rotate(Vector3.up, _cameraRotate.x * RotateSpeed * (UseCalculatedDeltaTime ? _calculatedDeltaTime : Time.deltaTime), HorizontalSpace);


            // Rotate vertically: up / down.
            if (IsRotateVerticallyEnabled)
                _cameraTransform.Rotate(Vector3.right, _cameraRotate.y * RotateSpeed * (UseCalculatedDeltaTime ? _calculatedDeltaTime : Time.deltaTime), VerticalSpace);


        }


        /// <summary>
        /// Resets the camera to its starting position.
        /// </summary>
        private void ResetCamera() {

            _cameraRigTransform.position = _cameraStartingPosition;
            _cameraTransform.rotation = _cameraStartingRotation;

            _camera.fieldOfView = _originalFOV;
            _targetFOV = _originalFOV;
            _targetCameraHeight = _cameraRigTransform.position.y;

        }


        #endregion


        #endregion


        #region Helper Methods


        /// <summary>
        /// Determines whether the cursor is in the area that triggers edge panning.
        /// </summary>
        /// <param name="cursorPosition"></param>
        /// <returns></returns>
        private bool IsCursorInPanEdgeArea(Vector2 cursorPosition) {


            // x is in left or right area. y is within bounds.
            if ((cursorPosition.x >= -OuterPanEdgeBorder && cursorPosition.x <= InnerPanEdgeBorder ||
                 cursorPosition.x >= Screen.width - InnerPanEdgeBorder && cursorPosition.x <= Screen.width + OuterPanEdgeBorder) &&
                cursorPosition.y >= -OuterPanEdgeBorder && cursorPosition.y <= Screen.height + OuterPanEdgeBorder)
                return true;


            // y is in top or bottom area. x is within bounds
            if ((cursorPosition.y >= Screen.height - InnerPanEdgeBorder && cursorPosition.y <= Screen.height + OuterPanEdgeBorder ||
                 cursorPosition.y >= -OuterPanEdgeBorder && cursorPosition.y <= InnerPanEdgeBorder) &&
                (cursorPosition.x >= -OuterPanEdgeBorder && cursorPosition.x <= Screen.width + OuterPanEdgeBorder))
                return true;


            return false;


        }


        /// <summary>
        /// Determines whether the cursor is in the area that triggers edge panning.
        /// </summary>
        /// <param name="cursorPosition"></param>
        /// <returns></returns>
        private Vector2 ConvertEdgePositionIntoDirectionalVector(Vector2 cursorPosition) {


            // Subtract the screen center from the vector before normalizing.
            Vector2 screenCenter = new Vector2((float)Screen.width / 2, (float)Screen.height / 2);


            cursorPosition -= screenCenter;
            cursorPosition.Normalize();


            return cursorPosition;


        }


        /// <summary>
        /// Returns the current pan variables that will drive the pan operation.
        /// </summary>
        /// <returns></returns>
        private (Vector2 panDirection, float panSpeed) GetPanVariables() {


            // We fill in the pan variables by priority. Lower priority panning methods will not be set if a higher
            // priority panning method has already set the variables.
            Vector2 panDirection = Vector2.zero;
            float panSpeed = 0f;

            if (panDirection == Vector2.zero && IsButtonPanningEnabled) {
                panDirection = _cameraPanButtons;
                panSpeed = ButtonPanSpeed * (UseCalculatedDeltaTime ? _calculatedDeltaTime : Time.deltaTime);
            }
            if (panDirection == Vector2.zero && IsEdgePanningEnabled) {
                panDirection = _cameraPanEdge;
                panSpeed = EdgePanSpeed * (UseCalculatedDeltaTime ? _calculatedDeltaTime : Time.deltaTime);
            }
            if (panDirection == Vector2.zero && IsGripPanningEnabled) {
                panDirection = _cameraPanGrip;
                panSpeed = GripPanSpeed * (UseCalculatedDeltaTime ? _calculatedDeltaTime : Time.deltaTime);
            }


            return (panDirection, panSpeed);


        }


        /// <summary>
        /// Calculates delta time to be <see cref="Time.timeScale"/> independent.
        /// </summary>
        private void CalculateDeltaTime() {


            float currentTimeSinceStartup = Time.realtimeSinceStartup;
            _calculatedDeltaTime = currentTimeSinceStartup - _previousTimeSinceStartup;
            _previousTimeSinceStartup = currentTimeSinceStartup;


            // Make sure delta time is never negative.
            if (_calculatedDeltaTime < 0) _calculatedDeltaTime = 0;


        }


        #endregion


#if ODIN_INSPECTOR
        #region [Odin] Validation Methods


        /// <summary>
        /// Checks if the float is a positive value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Local
        private bool PositiveFloat(float value) {
            return value > 0;
        }


        /// <summary>
        /// Checks if the FOV values make sense.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private bool FOVCheckMin(uint value, ref string errorMessage) {


            if (MinimumFOV <= MaximumFOV) return true;

            errorMessage = "Minimum FOV should be smaller than Maximum FOV.";
            return false;


        }


        /// <summary>
        /// Checks if the FOV values make sense.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private bool FOVCheckMax(uint value, ref string errorMessage) {


            if (MinimumFOV <= MaximumFOV) return true;

            errorMessage = "Maximum FOV should be greater than Minimum FOV.";
            return false;


        }


        #endregion
        

        #region [Odin] OnValueChanged Methods


        /// <summary>
        /// Toggles other zooming option off if enabled.
        /// </summary>
        // ReSharper disable once UnusedMember.Local
        private void TranslationZoomingChanged() {

            if (IsVerticalTranslationZoomingEnabled && IsFOVZoomingEnabled)
                IsFOVZoomingEnabled = false;

        }


        /// <summary>
        /// Toggles other zooming option off if enabled.
        /// </summary>
        // ReSharper disable once UnusedMember.Local
        private void FOVZoomingChanged() {

            if (IsFOVZoomingEnabled && IsVerticalTranslationZoomingEnabled)
                IsVerticalTranslationZoomingEnabled = false;

        }


        /// <summary>
        /// Toggles time scale ignoring off if disabled.
        /// </summary>
        // ReSharper disable once UnusedMember.Local
        private void UseCalculatedDeltaTimeChanged() {
        
            if (!UseCalculatedDeltaTime && IgnoreTimeScale)
                IgnoreTimeScale = false;

        }


        #endregion
#endif


    }
}
