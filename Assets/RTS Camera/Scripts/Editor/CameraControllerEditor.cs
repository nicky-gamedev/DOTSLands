using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace ModunGames.Editor {
    // Will override the Odin UI if not within pre-processor directive.
#if !ODIN_INSPECTOR
    [CustomEditor(typeof(CameraController))]
#endif
    public class CameraControllerEditor : UnityEditor.Editor {


        #region Fields


        private GUIStyle _titleGUIStyle; 


        #endregion


        #region Inspector Fields


        #region General


        public SerializedProperty MinimumCameraBounds;
        public SerializedProperty MaximumCameraBounds;
        public SerializedProperty UpdateMethod;
        public SerializedProperty UseCalculatedDeltaTime;
        public SerializedProperty IgnoreTimeScale;


        #endregion


        #region Panning


        #region Button Panning


        public SerializedProperty IsButtonPanningEnabled;
        public SerializedProperty ButtonPanSpeed;


        #endregion


        #region Edge Panning


        public SerializedProperty IsEdgePanningEnabled;
        public SerializedProperty EdgePanSpeed;
        public SerializedProperty InnerPanEdgeBorder;
        public SerializedProperty OuterPanEdgeBorder;


        #endregion


        #region Grip Panning


        public SerializedProperty IsGripPanningEnabled;
        public SerializedProperty GripPanSpeed;


        #endregion


        #endregion


        #region Zooming


        #region FOV Zooming


        public SerializedProperty IsFOVZoomingEnabled;
        public SerializedProperty FOVZoomSpeed;
        public SerializedProperty FOVZoomSmoothingTime;
        public SerializedProperty MinimumFOV;
        public SerializedProperty MaximumFOV;


        #endregion


        #region Camera Up / Down


        public SerializedProperty IsVerticalTranslationZoomingEnabled;
        public SerializedProperty AllowScrollZooming;
        public SerializedProperty VerticalTranslationZoomSpeed;
        public SerializedProperty VerticalTranslationScrollZoomSpeed;
        public SerializedProperty VerticalTranslationZoomSmoothingTime;


        #endregion


        #endregion


        #region Rotating


        public SerializedProperty IsRotateHorizontallyEnabled;
        public SerializedProperty IsRotateVerticallyEnabled;
        public SerializedProperty RotateSpeed;
        public SerializedProperty HorizontalSpace;
        public SerializedProperty VerticalSpace;


        #endregion


        #endregion



        // ReSharper disable once UnusedMember.Local
        private void OnEnable() {


            _titleGUIStyle = new GUIStyle {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };


            MinimumCameraBounds = serializedObject.FindProperty("MinimumCameraBounds");
            MaximumCameraBounds = serializedObject.FindProperty("MaximumCameraBounds");
            UpdateMethod = serializedObject.FindProperty("UpdateMethod");
            UseCalculatedDeltaTime = serializedObject.FindProperty("UseCalculatedDeltaTime");
            IgnoreTimeScale = serializedObject.FindProperty("IgnoreTimeScale");

            IsButtonPanningEnabled = serializedObject.FindProperty("IsButtonPanningEnabled");
            ButtonPanSpeed = serializedObject.FindProperty("ButtonPanSpeed");
            IsEdgePanningEnabled = serializedObject.FindProperty("IsEdgePanningEnabled");
            EdgePanSpeed = serializedObject.FindProperty("EdgePanSpeed");
            InnerPanEdgeBorder = serializedObject.FindProperty("InnerPanEdgeBorder");
            OuterPanEdgeBorder = serializedObject.FindProperty("OuterPanEdgeBorder");
            IsGripPanningEnabled = serializedObject.FindProperty("IsGripPanningEnabled");
            GripPanSpeed = serializedObject.FindProperty("GripPanSpeed");

            IsFOVZoomingEnabled = serializedObject.FindProperty("IsFOVZoomingEnabled");
            FOVZoomSpeed = serializedObject.FindProperty("FOVZoomSpeed");
            FOVZoomSmoothingTime = serializedObject.FindProperty("FOVZoomSmoothingTime");
            MinimumFOV = serializedObject.FindProperty("MinimumFOV");
            MaximumFOV = serializedObject.FindProperty("MaximumFOV");
            IsVerticalTranslationZoomingEnabled = serializedObject.FindProperty("IsVerticalTranslationZoomingEnabled");
            AllowScrollZooming = serializedObject.FindProperty("AllowScrollZooming");
            VerticalTranslationZoomSpeed = serializedObject.FindProperty("VerticalTranslationZoomSpeed");
            VerticalTranslationScrollZoomSpeed = serializedObject.FindProperty("VerticalTranslationScrollZoomSpeed");
            VerticalTranslationZoomSmoothingTime = serializedObject.FindProperty("VerticalTranslationZoomSmoothingTime");

            IsRotateHorizontallyEnabled = serializedObject.FindProperty("IsRotateHorizontallyEnabled");
            IsRotateVerticallyEnabled = serializedObject.FindProperty("IsRotateVerticallyEnabled");
            RotateSpeed = serializedObject.FindProperty("RotateSpeed");
            HorizontalSpace = serializedObject.FindProperty("HorizontalSpace");
            VerticalSpace = serializedObject.FindProperty("VerticalSpace");


        }



        /// <summary>
        /// Draws the Inspector UI for the <see cref="CameraController"/>.
        /// </summary>
        public override void OnInspectorGUI() {


            serializedObject.Update();
            
            
            EditorGUILayout.LabelField("General Settings", _titleGUIStyle);
            EditorGUILayout.PropertyField(MinimumCameraBounds);
            EditorGUILayout.PropertyField(MaximumCameraBounds);
            EditorGUILayout.PropertyField(UpdateMethod);
            EditorGUI.BeginChangeCheck();
            UseCalculatedDeltaTime.boolValue = EditorGUILayout.ToggleLeft(new GUIContent("Calculate Delta Time"), UseCalculatedDeltaTime.boolValue);
            if (EditorGUI.EndChangeCheck()) {
                if (!UseCalculatedDeltaTime.boolValue && IgnoreTimeScale.boolValue)
                    IgnoreTimeScale.boolValue = false;
            }
            EditorGUI.BeginDisabledGroup(!UseCalculatedDeltaTime.boolValue);
            IgnoreTimeScale.boolValue = EditorGUILayout.ToggleLeft(new GUIContent("Ignore Time Scale"), IgnoreTimeScale.boolValue);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();


            EditorGUILayout.LabelField("Panning", _titleGUIStyle);

            EditorGUILayout.LabelField("Button Panning", _titleGUIStyle);
            IsButtonPanningEnabled.boolValue = EditorGUILayout.ToggleLeft(new GUIContent("Button Panning"), IsButtonPanningEnabled.boolValue);
            EditorGUI.BeginDisabledGroup(!IsButtonPanningEnabled.boolValue);
            EditorGUILayout.PropertyField(ButtonPanSpeed, new GUIContent("Speed"));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Edge Panning", _titleGUIStyle);
            IsEdgePanningEnabled.boolValue = EditorGUILayout.ToggleLeft(new GUIContent("Edge Panning"), IsEdgePanningEnabled.boolValue);
            EditorGUI.BeginDisabledGroup(!IsEdgePanningEnabled.boolValue);
            EditorGUILayout.PropertyField(EdgePanSpeed, new GUIContent("Speed"));
            EditorGUILayout.PropertyField(InnerPanEdgeBorder, new GUIContent("Inner Border"));
            EditorGUILayout.PropertyField(OuterPanEdgeBorder, new GUIContent("Outer Border"));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Grip Panning", _titleGUIStyle);
            IsGripPanningEnabled.boolValue = EditorGUILayout.ToggleLeft(new GUIContent("Grip Panning"), IsGripPanningEnabled.boolValue);
            EditorGUI.BeginDisabledGroup(!IsGripPanningEnabled.boolValue);
            EditorGUILayout.PropertyField(GripPanSpeed);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();


            EditorGUILayout.LabelField("Zooming", _titleGUIStyle);

            EditorGUILayout.LabelField("FOV Zooming", _titleGUIStyle);
            EditorGUI.BeginChangeCheck();
            IsFOVZoomingEnabled.boolValue = EditorGUILayout.ToggleLeft(new GUIContent("Use FOV Zooming"), IsFOVZoomingEnabled.boolValue);
            if (EditorGUI.EndChangeCheck()) {
                if (IsFOVZoomingEnabled.boolValue && IsVerticalTranslationZoomingEnabled.boolValue)
                    IsVerticalTranslationZoomingEnabled.boolValue = false;
            }
            EditorGUI.BeginDisabledGroup(!IsFOVZoomingEnabled.boolValue);
            EditorGUILayout.PropertyField(FOVZoomSpeed, new GUIContent("Speed"));
            EditorGUILayout.PropertyField(FOVZoomSmoothingTime);
            EditorGUILayout.PropertyField(MinimumFOV);
            EditorGUILayout.PropertyField(MaximumFOV);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Vertical Translation Zooming", _titleGUIStyle);
            EditorGUI.BeginChangeCheck();
            IsVerticalTranslationZoomingEnabled.boolValue = EditorGUILayout.ToggleLeft(new GUIContent("Use Vertical Translation Zooming"), IsVerticalTranslationZoomingEnabled.boolValue);
            if (EditorGUI.EndChangeCheck()) {
                if (IsVerticalTranslationZoomingEnabled.boolValue && IsFOVZoomingEnabled.boolValue)
                    IsFOVZoomingEnabled.boolValue = false;
            }
            EditorGUI.BeginDisabledGroup(!IsVerticalTranslationZoomingEnabled.boolValue);
            AllowScrollZooming.boolValue = EditorGUILayout.ToggleLeft(new GUIContent("Allow Scroll Zooming"), AllowScrollZooming.boolValue);
            EditorGUILayout.PropertyField(VerticalTranslationZoomSpeed, new GUIContent("Speed"));
            EditorGUI.BeginDisabledGroup(!AllowScrollZooming.boolValue);
            EditorGUILayout.PropertyField(VerticalTranslationScrollZoomSpeed, new GUIContent("Speed (Scroll)"));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.PropertyField(VerticalTranslationZoomSmoothingTime);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();


            EditorGUILayout.LabelField("Rotating", _titleGUIStyle);
            IsRotateHorizontallyEnabled.boolValue = EditorGUILayout.ToggleLeft(new GUIContent("Rotate Horizontally"), IsRotateHorizontallyEnabled.boolValue);
            IsRotateVerticallyEnabled.boolValue = EditorGUILayout.ToggleLeft(new GUIContent("Rotate Vertically"), IsRotateVerticallyEnabled.boolValue);
            EditorGUI.BeginDisabledGroup(!IsRotateHorizontallyEnabled.boolValue && !IsRotateVerticallyEnabled.boolValue);
            EditorGUILayout.PropertyField(RotateSpeed);
            EditorGUILayout.PropertyField(HorizontalSpace);
            EditorGUILayout.PropertyField(VerticalSpace);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();


        }


    }
}
