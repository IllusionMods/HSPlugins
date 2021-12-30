﻿using HSUS.Features;
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
#endif
#if PLAYHOME || KOIKATSU || AISHOUJO || HONEYSELECT2
using UnityEngine.SceneManagement;
#endif

namespace HSUS
{
#if BEPINEX
    [BepInPlugin(_guid, _name, _version)]
#endif
    internal class HSUS : GenericPlugin
#if IPA
    , IEnhancedPlugin
#endif
    {
        internal const string _version = "1.10.0";
#if HONEYSELECT
        internal const string _name = "HSUS";
        internal const string _guid = "com.joan6694.illusionplugins.hsus";
#elif PLAYHOME
        internal const string _name = "PHUS";
        internal const string _guid = "com.joan6694.illusionplugins.phus";
#elif SUNSHINE
        internal const string _name = "KKSUS";
        internal const string _guid = "com.joan6694.illusionplugins.kksus";
#elif KOIKATSU
        internal const string _name = "KKUS";
        internal const string _guid = "com.joan6694.illusionplugins.kkus";
#elif AISHOUJO
        internal const string _name = "AIUS";
        internal const string _guid = "com.joan6694.illusionplugins.aius";
#elif HONEYSELECT2
        internal const string _name = "HS2US";
        internal const string _guid = "com.joan6694.illusionplugins.hs2us";
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
#if !KOIKATSU
        //Handled by DefaultParamEditor
        private readonly AutoJointCorrection _autoJointCorrection = new AutoJointCorrection();
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
        internal static ConfigEntry<bool> OptimizeCharaMaker { get; private set; }
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
#if HONEYSELECT
        internal static ConfigEntry<bool> FingersFKCopyButtons { get; private set; }
#endif
#if !KOIKATSU && !AISHOUJO && !HONEYSELECT2
        //Handled by CharacterReplacer
        internal static ConfigEntry<string> DefaultFemaleChar { get; private set; }
        internal static ConfigEntry<string> DefaultMaleChar { get; private set; }
#endif
#if !KOIKATSU
        //Handled by DefaultParamEditor
        internal static ConfigEntry<bool> AutoJointCorrection { get; private set; }
        internal static ConfigEntry<bool> EyesBlink { get; private set; }
        //Vanilla features
        internal static ConfigEntry<bool> PostProcessingDepthOfField { get; private set; }
        internal static ConfigEntry<bool> PostProcessingSSAO { get; private set; }
        internal static ConfigEntry<bool> PostProcessingBloom { get; private set; }
        internal static ConfigEntry<bool> PostProcessingSelfShadow { get; private set; }
        internal static ConfigEntry<bool> PostProcessingVignette { get; private set; }
        internal static ConfigEntry<bool> PostProcessingFog { get; private set; }
        internal static ConfigEntry<bool> PostProcessingSunShafts { get; private set; }
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

            UIScaleGame = Config.Bind("Config", "UIScaleGame", 1f);
            UIScaleStudio = Config.Bind("Config", "UIScaleStudio", 1f);
            OptimizeCharaMaker = Config.Bind("Config", "OptimizeCharaMaker", true);
            DeleteConfirmationKey = Config.Bind("Config", "DeleteConfirmationKey", true);
            DeleteConfirmationButton = Config.Bind("Config", "DeleteConfirmationButton", false);
            ImproveStudioUI = Config.Bind("Config", "ImproveStudioUI", true);
            OptimizeStudio = Config.Bind("Config", "OptimizeStudio", true);
            GenericFK = Config.Bind("Config", "GenericFK", true);
            ImprovedTransformOperations = Config.Bind("Config", "ImprovedTransformOperations", true);
            CameraShortcuts = Config.Bind("Config", "CameraShortcuts", true);
            CamSpeedBaseFactor = Config.Bind("Config", "CamSpeedBaseFactor", 1f, new ConfigDescription("Set base speed for camera keyboard controls. A value less than 1 is slower than vanilla speed. A value greater than one is faster than vanilla speed.", new AcceptableValueRange<float>(0.1f, 10f)));
            CamSpeedFast = Config.Bind("Config", "CamSpeedFast", 4f, new ConfigDescription("Speed multiplier for Fast Mode", new AcceptableValueRange<float>(1f, 100f)));
            CamSpeedSlow = Config.Bind("Config", "CamSpeedSlow", 0.6f, new ConfigDescription("Speed multiplier for Slow Mode", new AcceptableValueRange<float>(0.01f, 1f)));
            AlternativeCenterToObjects = Config.Bind("Config", "AlternativeCenterToObjects", true);
            AutomaticMemoryClean = Config.Bind("Config", "AutomaticMemoryClean", true);
            AutomaticMemoryCleanInterval = Config.Bind("Config", "AutomaticMemoryCleanInterval", 300);
#if HONEYSELECT
            FingersFKCopyButtons = Config.Bind("Config", "FingersFKCopyButtons", true);
#endif
#if !KOIKATSU && !AISHOUJO && !HONEYSELECT2
            DefaultFemaleChar = Config.Bind("Config", "DefaultFemaleChar", "");
            DefaultMaleChar = Config.Bind("Config", "DefaultMaleChar", "");
#endif
#if !KOIKATSU
            AutoJointCorrection = Config.Bind("Config", "AutoJointCorrection", true);
            EyesBlink = Config.Bind("Config", "EyesBlink", true);
            PostProcessingDepthOfField = Config.Bind("Config", "PostProcessingDepthOfField", false);
            PostProcessingSSAO = Config.Bind("Config", "PostProcessingSSAO", true);
            PostProcessingBloom = Config.Bind("Config", "PostProcessingBloom", true);
            PostProcessingSelfShadow = Config.Bind("Config", "PostProcessingSelfShadow", true);
            PostProcessingVignette = Config.Bind("Config", "PostProcessingVignette", true);
            PostProcessingFog = Config.Bind("Config", "PostProcessingFog", false);
            PostProcessingSunShafts = Config.Bind("Config", "PostProcessingSunShafts", false);
#endif
#if !BEPINEX
            VSync = Config.Bind("Config", "VSync", true);
            Debug = Config.Bind("Config", "Debug", false);
            DebugHotkey = Config.Bind("Config", "DebugHotkey", new KeyboardShortcut(KeyCode.RightControl));
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
            _features.Add(_cameraShortcuts);
            _features.Add(_alternativeCenterToObjects);
            _features.Add(_fingersFKCopyButtons);
            _features.Add(_animationOptionDisplay);
            _features.Add(_fkColors);
            _features.Add(_automaticMemoryClean);
#if !KOIKATSU && !AISHOUJO && !HONEYSELECT2
            _features.Add(_defaultChars);
#endif
#if !KOIKATSU
            _features.Add(_autoJointCorrection);
            _features.Add(_eyesBlink);
            _features.Add(_postProcessing);
#endif
#if !BEPINEX
            _features.Add(_debugFeature);
#endif
            UIUtility.Init();

            _harmonyInstance = HarmonyExtensions.CreateInstance(_guid);
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

            foreach (IFeature feature in _features)
            {
                try
                {
                    feature.Awake();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError(_name + ": Couldn't call Awake for feature " + feature + ":\n" + e);
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
                    UnityEngine.Debug.LogError(_name + ": Couldn't call LevelLoaded for feature " + feature + ":\n" + e);
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