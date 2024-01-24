using HSUS.Features;
using System;
using System.Collections.Generic;
using System.Reflection;
using ToolBox;
using ToolBox.Extensions;
using UILib;
using UnityEngine;
#if IPA
using IllusionPlugin;
using Harmony;
#elif BEPINEX
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using BepInEx.Logging;
#endif
#if PLAYHOME || KOIKATSU || AISHOUJO || HONEYSELECT2
using UnityEngine.SceneManagement;
#endif

namespace HSUS
{
#if BEPINEX
    [BepInPlugin(GUID, Name, Version)]
#endif
    internal class HSUS : GenericPlugin
#if IPA
    , IEnhancedPlugin
#endif
    {
        public const string Version = "1.14.2";
#if HONEYSELECT
        public const string Name = "HSUS";
        public const string GUID = "com.joan6694.illusionplugins.hsus";
#elif PLAYHOME
        public const string Name = "PHUS";
        public const string GUID = "com.joan6694.illusionplugins.phus";
#elif SUNSHINE
        public const string Name = "KKSUS";
        public const string GUID = "com.joan6694.illusionplugins.kksus";
#elif KOIKATSU
        public const string Name = "KKUS";
        public const string GUID = "com.joan6694.illusionplugins.kkus";
#elif AISHOUJO
        public const string Name = "AIUS";
        public const string GUID = "com.joan6694.illusionplugins.aius";
#elif HONEYSELECT2
        public const string Name = "HS2US";
        public const string GUID = "com.joan6694.illusionplugins.hs2us";
#endif

        #region Private Types
        internal delegate bool TranslationDelegate(ref string text);
        #endregion

        #region Private Variables
        private readonly List<IFeature> _features = new List<IFeature>();
        private readonly OptimizeCharaMaker _optimizeCharaMaker = new OptimizeCharaMaker();
        private readonly UIScale _uiScale = new UIScale();
        private readonly DeleteConfirmation _deleteConfirmation = new DeleteConfirmation();
        private readonly ImproveNeoUI _improveNeoUi = new ImproveNeoUI();
        private readonly OptimizeNEO _optimizeNeo = new OptimizeNEO();
        private readonly GenericFK _genericFK = new GenericFK();
        private readonly ImprovedTransformOperations _improvedTransformOperations = new ImprovedTransformOperations();
        private readonly CameraShortcuts _cameraShortcuts = new CameraShortcuts();
        private readonly SetVisibleToAllSelectedWorkspaceObjects _setVisibleToAllSelectedWorkspaceObjects = new SetVisibleToAllSelectedWorkspaceObjects();
        private readonly AlternativeCenterToObjects _alternativeCenterToObjects = new AlternativeCenterToObjects();
        private readonly FingersFKCopyButtons _fingersFKCopyButtons = new FingersFKCopyButtons();
        private readonly AnimationOptionDisplay _animationOptionDisplay = new AnimationOptionDisplay();
        private readonly FKColors _fkColors = new FKColors();
        private readonly AutomaticMemoryClean _automaticMemoryClean = new AutomaticMemoryClean();
        internal event Action _onUpdate;
#if !KOIKATSU && !AISHOUJO && !HONEYSELECT2
        //Handled by CharacterReplacer
        private readonly DefaultChars _defaultChars = new DefaultChars();
#endif
        private readonly AutoJointCorrection _autoJointCorrection = new AutoJointCorrection();
#if !KOIKATSU
        //Handled by DefaultParamEditor
        private readonly EyesBlink _eyesBlink = new EyesBlink();
        //Vanilla features
        private readonly PostProcessing _postProcessing = new PostProcessing();
#endif
#if !BEPINEX
        //Handled by RuntimeUnityEditor
        private readonly DebugFeature _debugFeature = new DebugFeature();
#endif
        internal static HSUS _self;
        private int _lastScreenWidth;
        private int _lastScreenHeight;
#if HONEYSELECT
        internal TranslationDelegate _translationMethod;
        internal Sprite _searchBarBackground;
        internal Sprite _buttonBackground;
        internal readonly List<IEnumerator> _asyncMethods = new List<IEnumerator>();
#endif
#if IPA
        internal HarmonyInstance _harmonyInstance;
#elif BEPINEX
        internal Harmony _harmonyInstance;
#endif
        #endregion

        #region Public Accessors
#if IPA
        public override string Name { get { return _name; } }
        public override string Version
        {
            get
            {
                return _version
#if BETA
                       + "b"
#endif
                        ;
            }
        }
        public override string[] Filter { get { return new[] { "HoneySelect_64", "HoneySelect_32", "StudioNEO_32", "StudioNEO_64", "Honey Select Unlimited_64", "Honey Select Unlimited_32" }; } }
#endif
        public static HSUS self { get { return _self; } }
        #endregion

        #region Config
        internal static ConfigEntry<float> UIScaleGame { get; private set; }
        internal static ConfigEntry<float> UIScaleStudio { get; private set; }
        internal static ConfigEntry<bool> CharaMakerSearchboxes { get; private set; }
        internal static ConfigEntry<bool> DeleteConfirmationKey { get; private set; }
        internal static ConfigEntry<bool> DeleteConfirmationButton { get; private set; }
        internal static ConfigEntry<bool> ImproveStudioUI { get; private set; }
        internal static ConfigEntry<bool> OptimizeStudio { get; private set; }
        internal static ConfigEntry<bool> GenericFK { get; private set; }
        internal static ConfigEntry<bool> ImprovedTransformOperations { get; private set; }
        internal static ConfigEntry<bool> CameraShortcuts { get; private set; }
        internal static ConfigEntry<float> CamSpeedBaseFactor { get; private set; }
        internal static ConfigEntry<float> CamSpeedFast { get; private set; }
        internal static ConfigEntry<float> CamSpeedSlow { get; private set; }
        internal static ConfigEntry<bool> AlternativeCenterToObjects { get; private set; }
        internal static ConfigEntry<bool> AutomaticMemoryClean { get; private set; }
        internal static ConfigEntry<int> AutomaticMemoryCleanInterval { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> CopyTransformHotkey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> PasteTransformHotkey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> PasteTransformPositionOnlyHotkey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> PasteTransformRotationOnlyHotkey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> PasteTransformScaleOnlyHotkey { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> ResetTransformHotkey { get; private set; }
#if HONEYSELECT
        internal static ConfigEntry<bool> FingersFKCopyButtons { get; private set; }
#endif
#if !KOIKATSU && !AISHOUJO && !HONEYSELECT2
        //Handled by CharacterReplacer
        internal static ConfigEntry<string> DefaultFemaleChar { get; private set; }
        internal static ConfigEntry<string> DefaultMaleChar { get; private set; }
#endif
        internal static new ManualLogSource Logger;

        internal static ConfigEntry<bool> AutoJointCorrection { get; private set; }
        internal static ConfigEntry<AutoJointCorrection.JointCorrectionArea> AutoJointCorrectionValues { get; private set; }
#if !KOIKATSU
        //Handled by DefaultParamEditor
        internal static ConfigEntry<bool> EyesBlink { get; private set; }
        //Vanilla features
        //internal static ConfigEntry<bool> PostProcessingDepthOfField { get; private set; }
        //internal static ConfigEntry<bool> PostProcessingSSAO { get; private set; }
        //internal static ConfigEntry<bool> PostProcessingBloom { get; private set; }
        //internal static ConfigEntry<bool> PostProcessingSelfShadow { get; private set; }
        //internal static ConfigEntry<bool> PostProcessingVignette { get; private set; }
        //internal static ConfigEntry<bool> PostProcessingFog { get; private set; }
        //internal static ConfigEntry<bool> PostProcessingSunShafts { get; private set; }
#endif
#if !BEPINEX
        //Handled by GraphicsSettings
        internal static ConfigEntry<bool> VSync { get; private set; }
        //Handled by RuntimeUnityEditor
        internal static ConfigEntry<bool> Debug { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> DebugHotkey { get; private set; }
#endif
        #endregion

        #region Unity Methods
        protected override void Awake()
        {
            base.Awake();
            Logger = base.Logger;

            UIScaleGame = Config.Bind("Interface", "UI Scale in Game", 1f, "Scale all of the Canvas interfaces in game by this factor.");
            UIScaleStudio = Config.Bind("Interface", "UI Scale in Studio", 1f, "Scale all of the Canvas interfaces in studio by this factor.");
            CharaMakerSearchboxes = Config.Bind("Interface", "Add search boxes to Character Maker", true, "Add search boxes to some of the lists in character maker (searching characters).");
            DeleteConfirmationKey = Config.Bind("Studio controls", "Show confirmation when pressing Delete key", false, "Helps avoid deleting studio objects by accident.");
            DeleteConfirmationButton = Config.Bind("Studio controls", "Show confirmation when clicking Delete interface button", false, "Helps avoid deleting studio objects by accident.");
            ImproveStudioUI = Config.Bind("Interface", "Improve Studio UI", true, "Adjust sizes and positions of some UI elements");
            OptimizeStudio = Config.Bind("Interface", "Add search boxes to Studio", true, "Add search boxes to some of the lists in character maker (searching characters). Changes how some studio lists work.");
            GenericFK = Config.Bind("Studio controls", "GenericFK", true);
            ImprovedTransformOperations = Config.Bind("Studio controls", "Improved Transform Operations", true, "Adds additional buttons to bottom left transform controls for copying and resetting. Allows dragging on the X/Y/Z labels to change values in the text boxes. Increases precision of values in the text boxes.");
            CamSpeedBaseFactor = Config.Bind("Studio camera", "Camera Speed Base Factor", 1f, new ConfigDescription("Set base speed for camera keyboard controls. A value less than 1 is slower than vanilla speed. A value greater than one is faster than vanilla speed.", new AcceptableValueRange<float>(0.1f, 10f)));
            CameraShortcuts = Config.Bind("Studio camera", "Camera Speed Modifier Keys", true, "While moving the camera hold LeftControl to slow camera down and LeftShift to speed it up.");
            CamSpeedFast = Config.Bind("Studio camera", "Speed Modifier Fast", 4f, new ConfigDescription("Speed multiplier when holding LeftShift", new AcceptableValueRange<float>(1f, 100f)));
            CamSpeedSlow = Config.Bind("Studio camera", "Speed Modifier Slow", 0.6f, new ConfigDescription("Speed multiplier when holding LeftControl", new AcceptableValueRange<float>(0.01f, 1f)));
            AlternativeCenterToObjects = Config.Bind("Studio controls", "Alternative Center To Objects", true, "Change how pressing F centers the camera.");
            AutomaticMemoryClean = Config.Bind("Performance", "Automatic Memory Cleaning", false, "Periodically clean memory from unused objects in case the game doesn't do it for whatever reason. When cleanup is performed the game/studio may stutter/lag for a moment.");
            AutomaticMemoryCleanInterval = Config.Bind("Performance", "Automatic Memory Cleaning Interval", 300, "How often to clean memory.");

            CopyTransformHotkey = Config.Bind("Improved Transform Operations", "Copy Transform", new KeyboardShortcut(KeyCode.C, KeyCode.LeftControl));
            PasteTransformHotkey = Config.Bind("Improved Transform Operations", "Paste Transform", new KeyboardShortcut(KeyCode.V, KeyCode.LeftControl));
            PasteTransformPositionOnlyHotkey = Config.Bind("Improved Transform Operations", "Paste Transform (Postition Only)", KeyboardShortcut.Empty);
            PasteTransformRotationOnlyHotkey = Config.Bind("Improved Transform Operations", "Paste Transform (Rotation Only)", KeyboardShortcut.Empty);
            PasteTransformScaleOnlyHotkey = Config.Bind("Improved Transform Operations", "Paste Transform (Scale Only)", KeyboardShortcut.Empty);
            ResetTransformHotkey = Config.Bind("Improved Transform Operations", "Reset Transform", KeyboardShortcut.Empty);
#if HONEYSELECT
            FingersFKCopyButtons = Config.Bind("Studio controls", "FingersFKCopyButtons", true);
#endif
#if !KOIKATSU && !AISHOUJO && !HONEYSELECT2
            DefaultFemaleChar = Config.Bind("Defaults", "DefaultFemaleChar", "");
            DefaultMaleChar = Config.Bind("Defaults", "DefaultMaleChar", "");
#endif
            AutoJointCorrection = Config.Bind("Auto Joint Correction", "Auto Joint Correction", true, "If this is enabled, joint correction is automatically set as set here when adding a new character. Changes to this setting require a studio restart to take effect.");
            AutoJointCorrectionValues = Config.Bind("Auto Joint Correction", "Default values", Features.AutoJointCorrection.JointCorrectionArea.All,
                "Desired initial state of the joint correction toggles. This is applied if the Auto Joint Correction setting is enabled.\n" +
                "- Arm correction affects shoulder, upperarm, elbow and upper hand\n" +
                "- Forearm correction affects mainly the wrist area. It can cause the wrist to deform abnormally.\n" +
                "- Leg correction affects the leg, knee and calf.\n" +
                "- Thigh correction affects mainly the thigh area.\n" +
                "- Crotch and Ankle correction is handled by PE, check its settings if you want to change those.");
#if !KOIKATSU
            EyesBlink = Config.Bind("Defaults", "EyesBlink", true);
            //PostProcessingDepthOfField = Config.Bind("Graphics", "PostProcessingDepthOfField", false);
            //PostProcessingSSAO = Config.Bind("Graphics", "PostProcessingSSAO", true);
            //PostProcessingBloom = Config.Bind("Graphics", "PostProcessingBloom", true);
            //PostProcessingSelfShadow = Config.Bind("Graphics", "PostProcessingSelfShadow", true);
            //PostProcessingVignette = Config.Bind("Graphics", "PostProcessingVignette", true);
            //PostProcessingFog = Config.Bind("Graphics", "PostProcessingFog", false);
            //PostProcessingSunShafts = Config.Bind("Graphics", "PostProcessingSunShafts", false);
#endif
#if !BEPINEX
            // Wait, what? Using bepin config for non-bepin
            VSync = Config.Bind("Graphics", "VSync", true);
            Debug = Config.Bind("Other", "Debug", false);
            DebugHotkey = Config.Bind("Other", "DebugHotkey", new KeyboardShortcut(KeyCode.RightControl));
#endif
            _self = this;

#if HONEYSELECT
            Type t = Type.GetType("UnityEngine.UI.Translation.TextTranslator,UnityEngine.UI.Translation");
            if (t != null)
            {
                MethodInfo info = t.GetMethod("Translate", BindingFlags.Public | BindingFlags.Static);
                if (info != null)
                    this._translationMethod = (TranslationDelegate)Delegate.CreateDelegate(typeof(TranslationDelegate), info);
            }
#endif
            _features.Add(_optimizeCharaMaker);
            _features.Add(_uiScale);
            _features.Add(_deleteConfirmation);
            _features.Add(_improveNeoUi);
            _features.Add(_optimizeNeo);
            _features.Add(_genericFK);
            _features.Add(_improvedTransformOperations);
            _features.Add(_autoJointCorrection);
            _features.Add(_cameraShortcuts);
            _features.Add(_setVisibleToAllSelectedWorkspaceObjects);
            _features.Add(_alternativeCenterToObjects);
            _features.Add(_fingersFKCopyButtons);
            _features.Add(_animationOptionDisplay);
            _features.Add(_fkColors);
            _features.Add(_automaticMemoryClean);
#if !KOIKATSU && !AISHOUJO && !HONEYSELECT2
            _features.Add(_defaultChars);
#endif
#if !KOIKATSU
            _features.Add(_eyesBlink);
            _features.Add(_postProcessing);
#endif
#if !BEPINEX
            _features.Add(_debugFeature);
#endif
            UIUtility.Init();

            _harmonyInstance = HarmonyExtensions.CreateInstance(GUID);
            _harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

            foreach (IFeature feature in _features)
            {
                try
                {
                    feature.Awake();
                }
                catch (Exception e)
                {
                    Logger.LogError("Couldn't call Awake for feature " + feature + ":\n" + e);
                }
            }
        }

#if KOIKATSU || AISHOUJO
        protected override void LevelLoaded(Scene scene, LoadSceneMode mode)
        {
            base.LevelLoaded(scene, mode);
            if (mode != LoadSceneMode.Single)
                _uiScale.RefreshCanvases();
        }
#endif

        protected override void LevelLoaded(int level)
        {
            UIUtility.Init();
            foreach (IFeature feature in _features)
            {
                try
                {
                    feature.LevelLoaded();
                }
                catch (Exception e)
                {
                    Logger.LogError("Couldn't call LevelLoaded for feature " + feature + ":\n" + e);
                }
            }

            _uiScale.RefreshCanvases();
        }

        protected override void Update()
        {
#if !BEPINEX
            if (VSync.Value == false)
                QualitySettings.vSyncCount = 0;
#endif
            if (_lastScreenWidth != Screen.width || _lastScreenHeight != Screen.height)
                OnWindowResize();
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;

#if HONEYSELECT
            if (this._asyncMethods.Count != 0)
            {
                IEnumerator method = this._asyncMethods[0];
                try
                {
                    if (method.MoveNext() == false)
                        this._asyncMethods.RemoveAt(0);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError(e);
                    this._asyncMethods.RemoveAt(0);
                }
            }
#endif
            if (_onUpdate != null)
                _onUpdate();
        }
        #endregion

        #region Private Methods
        private void OnWindowResize()
        {
            this.ExecuteDelayed2(_uiScale.Scale, 2);
        }
        #endregion
    }
}