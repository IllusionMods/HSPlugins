using Studio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using ToolBox;
using ToolBox.Extensions;
using UnityEngine;
using VideoExport.Extensions;
using VideoExport.ScreenshotPlugins;
using Resources = UnityEngine.Resources;
using VideoExport.Core;
using KKAPI.Studio.UI.Toolbars;
using KKAPI.Utilities;
using TimelineCompatibility = ToolBox.TimelineCompatibility;
#if (!KOIKATSU || SUNSHINE)
using Unity.Collections;
#endif

#if IPA
using IllusionPlugin;
using Harmony;
#elif BEPINEX
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
#endif

namespace VideoExport
{
#if BEPINEX
    [BepInPlugin(GUID: GUID, Name: Name, Version: Version)]
#if KOIKATSU
    [BepInProcess("CharaStudio")]
#elif AISHOUJO || HONEYSELECT2
    [BepInProcess("StudioNEOV2")]
#endif
    [BepInDependency(Screencap.ScreenshotManager.GUID, Screencap.ScreenshotManager.Version)]
    [BepInDependency(KKAPI.KoikatuAPI.GUID, KKAPI.KoikatuAPI.VersionConst)]
#endif
    public class VideoExport : GenericPlugin
#if IPA
                               , IEnhancedPlugin
#endif
    {
        public const string Version = "1.9.4";
        public const string GUID = "com.joan6694.illusionplugins.videoexport";
        public const string Name = "VideoExport";

#if IPA
        public override string Name { get { return _name; } }
        public override string Version
        {
            get
            {
                return _versionNum
#if BETA
                    + "b"
#endif
                    ;
            }
        }
        public override string[] Filter { get { return new[] { "StudioNEO_32", "StudioNEO_64" }; } }
#endif

        #region Types
        public enum ImgFormat
        {
            BMP,
            PNG,
            JPG,
#if !HONEYSELECT //Someday I hope...
            EXR
#endif
        }

        private enum Language
        {
            English,
            中文 // Chinese
        }

        internal enum TranslationKey
        {
            CurrentSize,
            Framerate,
            ExportFramerate,
            AutoDeleteImages,
            LimitBy,
            LimitByFrames,
            LimitBySeconds,
            LimitByAnimation,
            LimitByTimeline,
            LimitByPrewarmLoopCount,
            LimitByLoopsToRecord,
            LimitByLimitCount,
            Seconds,
            Frames,
            Resize,
            ResizeDefault,
            DBUpdateMode,
            DBDefault,
            DBEveryFrame,
            EmptyScene,
            CloseStudio,
            ParallelScreenshotEncoding,
            StartRecordingOnNextClick,
            StartRecording,
            StopRecording,
            PrewarmingAnimation,
            RealignTimeline,
            TakingScreenshot,
            ETA,
            Elapsed,
            GeneratingVideo,
            Done,
            GeneratingError,
            VideosFolderDesc,
            FramesFolderDesc,
            BuiltInCaptureTool,
            Win32CaptureTool,
            SizeMultiplier,
            CaptureMode,
            ImageFormat,
            CaptureModeNormal,
            CaptureModeImmediate,
            CaptureModeWin32,
            ScreencapCaptureType,
            ScreencapCaptureTypeNormal,
            ScreencapCaptureType360,
            Screencap3D,
            Rotation,
            RotationNone,
            Rotation90,
            Rotation180,
            Rotation270,
            BitDepthError,
            GIFError,
            Codec,
            HwAccelCodec,
            MP4Quality,
            MP4Preset,
            MP4PresetSlower,
            MP4PresetMedium,
            MP4PresetFaster,
            VP8Quality,
            VP9Quality,
            WEBMDeadline,
            WEBMDeadlineBest,
            WEBMDeadlineGood,
            WEBMDeadlineRealtime,
            WEBPQuality,
            AVIFQuality,
            GIFTool,
            GIFMaxColors,
            GIFDithering,
            ShowTooltips,
            CaptureSettingsHeading,
            VideoSettingsHeading,
            OtherSettingsHeading,
            ScreenshotTool,
            Extension,
            CreateVideo,
            AutoHideUI,
            RemoveAlphaChannel,
            FramerateTooltip,
            ExportFramerateTooltip,
            DBUpdateModeTooltip,
            ScreenshotToolTooltip,
            LimitByTooltip,
            LimitByPrewarmLoopCountTooltip,
            LimitByLoopsToRecordTooltip,
            LimitByLimitCountTooltip,
            RealignTimelineTooltip,
            ParallelEncodingTooltip,
            CaptureModeTooltip,
            ImageFormatTooltip,
            ExtensionTooltip,
            RemoveAlphaChannelTooltip,
            HwAccelCodecTooltip,
            MP4CodecTooltip,
            WEBMCodecTooltip,
            AVIFCodecTooltip,
            MOVCodecTooltip,
            GIFToolTooltip,
            GIFDitheringTooltip,
            MOVPreset,
            MOVPresetTooltip,
            MOVPresetProRes422Proxy,
            MOVPresetProRes422LT,
            MOVPresetProRes422,
            MOVPresetProRes422HQ,
            MOVPresetProRes4444,
            MOVPresetProRes4444XQ,
            GIFSKIQuality,
            GIFSKIMotionQuality,
            GIFSKILossyQuality,
            GIFSKIQualityTooltip,
            GIFSKIMotionQualityTooltip,
            GIFSKILossyQualityTooltip
        }

        private enum LimitDurationType
        {
            Frames,
            Seconds,
            Animation,
            Timeline
        }

        private enum UpdateDynamicBonesType
        {
            Default,
            EveryFrame
        }
        #endregion

        #region Private Variables
        internal static new ManualLogSource Logger;

        internal static string _pluginFolder;
        private ConfigEntry<string> _outputFolder;
        private ConfigEntry<string> _globalFramesFolder;
        internal static GenericConfig _configFile;
        private static readonly TranslationDictionary<TranslationKey> _englishDictionary = new TranslationDictionary<TranslationKey>("VideoExport.Resources.English.xml");
        private static readonly TranslationDictionary<TranslationKey> _chineseDictionary = new TranslationDictionary<TranslationKey>("VideoExport.Resources.中文.xml");
        internal static TranslationDictionary<TranslationKey> _currentDictionary = _englishDictionary;
        private static readonly GUIStyle _customBoxStyle = new GUIStyle { normal = new GUIStyleState { background = Texture2D.whiteTexture } };

        private string[] _limitDurationNames;
        private string[] _extensionsNames;
        private string[] _updateDynamicBonesTypeNames;

        private static bool _showUI = false;
        private static SimpleToolbarToggle _toolbarButton;
        private bool _isRecording = false;
        private bool _breakRecording = false;
        private bool _generatingVideo = false;
        private readonly List<IScreenshotPlugin> _screenshotPlugins = new List<IScreenshotPlugin>();
        private const int _uniqueId = ('V' << 24) | ('I' << 16) | ('D' << 8) | 'E';
        private Rect _windowRect = new Rect(Screen.width / 2 - 320, 100, Styles.WindowWidth, 10);
        private static RectTransform _imguiBackground;
        private string _currentMessage;
        private readonly List<IExtension> _extensions = new List<IExtension>();
        private Color _messageColor = Color.white;
        private float _progressBarPercentage;
        private bool _startOnNextClick = false;
        private Animator _currentAnimator;
        private float _lastAnimationNormalizedTime;
        private bool _animationIsPlaying;
        private int _currentRecordingFrame;
        private double _currentRecordingTime;
        private int _recordingFrameLimit;
        private bool _timelinePresent = false;
        private float _realTimelineDuration;

        private int _selectedPlugin = 0;
        private int _fps = 60;
        private int _exportFps = 60;
        private int[] _presetFps = new[] { 12, 24, 30, 60, 120 };
        private bool _autoGenerateVideo;
        private bool _autoDeleteImages;
        private bool _limitDuration;
        private LimitDurationType _selectedLimitDuration;
        private float _limitDurationNumber = 600;
        private ExtensionsType _selectedExtension;
        private bool _resize;
        private int _resizeX;
        private int _resizeY;
        private UpdateDynamicBonesType _selectedUpdateDynamicBones;
        private bool _realignTimeline = true;
        private int _prewarmLoopCount = 3;
        private bool _clearSceneBeforeEncoding;
        private bool _closeWhenDone;
        private bool _parallelScreenshotEncoding;
        private ConfigEntry<Language> _language;

        private Vector2 _extensionScrollPos;
        private Vector2 _pluginScrollPos;
        private bool _showTooltips = true;
        private bool _showCaptureSection;
        private bool _showVideoSection;
        private bool _showOtherSection;

        private Process _ffmpegProcessMaster;
        private Process _ffmpegProcessSlave;
        private StreamWriter _ffmpegStdin;
        private byte[] _frameDataBuffer;
        private int _frameBufferSize;
        private string _tempDateTime;

        #endregion

        #region Public Accessors (for other plugins probably)
        public bool isRecording { get { return _isRecording; } }
        public int currentRecordingFrame { get { return _currentRecordingFrame; } }
        public double currentRecordingTime { get { return _currentRecordingTime; } }
        public int recordingFrameLimit { get { return _recordingFrameLimit; } }
        #endregion

        internal static ConfigEntry<KeyboardShortcut> ConfigMainWindowShortcut { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> ConfigStartStopShortcut { get; private set; }

        public static bool ShowUI
        {
            get => _showUI;
            set
            {
                if (_showUI != value)
                {
                    _showUI = value;
                    _toolbarButton.Toggled.OnNext(value);

                    if (_imguiBackground == null)
                        _imguiBackground = IMGUIExtensions.CreateUGUIPanelForIMGUI();
                }
            }
        }

        #region Unity Methods
        protected override void Awake()
        {
            base.Awake();

            Logger = base.Logger;

            ConfigMainWindowShortcut = Config.Bind("Config", "Open VideoExport UI", new KeyboardShortcut(KeyCode.E, KeyCode.LeftControl));
            ConfigStartStopShortcut = Config.Bind("Config", "Start or Stop Recording", new KeyboardShortcut(KeyCode.E, KeyCode.LeftControl, KeyCode.LeftShift));

            _pluginFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), Name);

            var harmony = HarmonyExtensions.CreateInstance(GUID);

            _configFile = new GenericConfig(Name, this);
            _selectedPlugin = _configFile.AddInt("selectedScreenshotPlugin", 0, true);
            _fps = _configFile.AddInt("framerate", 60, true);
            _exportFps = _configFile.AddInt("exportFramerate", 60, true);
            _autoGenerateVideo = _configFile.AddBool("autoGenerateVideo", true, true);
            _autoDeleteImages = _configFile.AddBool("autoDeleteImages", true, true);
            _limitDuration = _configFile.AddBool("limitDuration", false, true);
            _selectedLimitDuration = (LimitDurationType)_configFile.AddInt("selectedLimitDurationType", (int)LimitDurationType.Frames, true);
            _limitDurationNumber = _configFile.AddFloat("limitDurationNumber", 0, true);
            _selectedExtension = (ExtensionsType)_configFile.AddInt("selectedExtension", (int)ExtensionsType.MP4, true);
            _resize = _configFile.AddBool("resize", false, true);
            _resizeX = _configFile.AddInt("resizeX", Screen.width, true);
            _resizeY = _configFile.AddInt("resizeY", Screen.height, true);
            _selectedUpdateDynamicBones = (UpdateDynamicBonesType)_configFile.AddInt("selectedUpdateDynamicBonesMode", (int)UpdateDynamicBonesType.Default, true);
            _realignTimeline = _configFile.AddBool("realignTimeline", true, true);
            _prewarmLoopCount = _configFile.AddInt("prewarmLoopCount", 3, true);
            _showCaptureSection = _configFile.AddBool("showCaptureSection", true, true);
            _showVideoSection = _configFile.AddBool("showVideoSection", true, true);
            _showOtherSection = _configFile.AddBool("showOtherSection", true, true);
            _showTooltips = _configFile.AddBool("showTooltips", true, true);
            _parallelScreenshotEncoding = _configFile.AddBool("parallelScreenshotEncoding", false, true);
            _language = Config.Bind(Name, "Language", Language.English, "Interface language");
            _language.SettingChanged += (sender, args) => SetLanguage(_language.Value);
            SetLanguage(_language.Value);

            // If old directories exist, keep using them by default, otherwise use the new defaults
            var outputFolderDefault = Path.Combine(_pluginFolder, "Output");
            if (!Directory.Exists(outputFolderDefault)) outputFolderDefault = Path.Combine(Paths.GameRootPath, "UserData\\VideoExport\\Output");
            _outputFolder = Config.Bind("VideoExport", "outputFolder", outputFolderDefault, new ConfigDescription(_currentDictionary.GetString(TranslationKey.VideosFolderDesc)));

            var framesFolderDefault = Path.Combine(_pluginFolder, "Frames");
            if (!Directory.Exists(framesFolderDefault)) framesFolderDefault = Path.Combine(Paths.GameRootPath, "UserData\\VideoExport\\Frames");
            _globalFramesFolder = Config.Bind("VideoExport", "framesFolder", framesFolderDefault, new ConfigDescription(_currentDictionary.GetString(TranslationKey.FramesFolderDesc)));

            _extensions.Add(new MP4Extension());
            _extensions.Add(new WEBMExtension());
            _extensions.Add(new GIFExtension());
            _extensions.Add(new MOVExtension());
            _extensions.Add(new WEBPExtension());
            _extensions.Add(new AVIFExtension());

            _extensionsNames = Enum.GetNames(typeof(ExtensionsType));
            if ((int)_selectedExtension >= _extensionsNames.Length)
                _selectedExtension = ExtensionsType.MP4;

            _timelinePresent = TimelineCompatibility.Init();

            this.ExecuteDelayed(() =>
            {
                AddScreenshotPlugin<ScreencapPlugin>(harmony);
                AddScreenshotPlugin<Builtin>(harmony);
                AddScreenshotPlugin<ReshadePlugin>(harmony);
#if !KOIKATSU
                AddScreenshotPlugin<Win32Plugin>(harmony);
#endif

                if (_screenshotPlugins.Count == 0)
                    Logger.LogError("No compatible screenshot plugin found, please install one.");

                SetLanguage(_language.Value);

                if (_selectedPlugin < 0 || _selectedPlugin >= _screenshotPlugins.Count)
                {
                    // Panic reset: out of bounds if user has Reshade selected and uninstalls Reshade
                    _selectedPlugin = 0;
                }
            }, 5);

            _toolbarButton = new SimpleToolbarToggle(
                "Open window",
                "Open VideoExport window. It can be used\nto record at high resolution and stable FPS.\nHotkey: " + ConfigMainWindowShortcut.Value,
                () => ResourceUtils.GetEmbeddedResource("ve_toolbar_icon.png", typeof(VideoExport).Assembly).LoadTexture(),
                false, this, val => ShowUI = val);
            ToolbarManager.AddLeftToolbarControl(_toolbarButton);
        }

        protected override void Update()
        {
            if (ConfigStartStopShortcut.Value.IsDown())
            {
                if (_isRecording == false)
                    RecordVideo();
                else
                    StopRecording();
            }
            else if (ConfigMainWindowShortcut.Value.IsDown())
            {
                ShowUI = !ShowUI;
            }
            if (_startOnNextClick && Input.GetMouseButtonDown(0))
            {
                RecordVideo();
                _startOnNextClick = false;
            }
            TreeNodeObject treeNode = Studio.Studio.Instance?.treeNodeCtrl.selectNode;
            _currentAnimator = null;
            if (treeNode != null)
            {
                ObjectCtrlInfo info;
                if (Studio.Studio.Instance.dicInfo.TryGetValue(treeNode, out info))
                    _currentAnimator = info.guideObject.transformTarget.GetComponentInChildren<Animator>();
            }

            if (_currentAnimator == null && _selectedLimitDuration == LimitDurationType.Animation)
                _selectedLimitDuration = LimitDurationType.Seconds;
            if (_timelinePresent == false && _selectedLimitDuration == LimitDurationType.Timeline)
                _selectedLimitDuration = LimitDurationType.Seconds;
        }

        protected override void LateUpdate()
        {
            if (_currentAnimator != null)
            {
                AnimatorStateInfo stateInfo = _currentAnimator.GetCurrentAnimatorStateInfo(0);
                _animationIsPlaying = stateInfo.normalizedTime > _lastAnimationNormalizedTime;
                if (stateInfo.loop == false)
                    _animationIsPlaying = _animationIsPlaying && stateInfo.normalizedTime < 1f;
                _lastAnimationNormalizedTime = stateInfo.normalizedTime;
            }
            if (_imguiBackground != null)
            {
                if (ShowUI)
                {
                    _imguiBackground.gameObject.SetActive(true);
                    IMGUIExtensions.FitRectTransformToRect(_imguiBackground, _windowRect);
                }
                else if (_imguiBackground != null)
                    _imguiBackground.gameObject.SetActive(false);
            }
            if (ShowUI)
            {
                _windowRect.height = 10f;
            }
        }

        protected override void OnGUI()
        {
            if (ShowUI == false)
                return;
            _windowRect = GUILayout.Window(_uniqueId + 1, _windowRect, Window, "Video Export " + Version);
            IMGUIExtensions.DrawBackground(_windowRect);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _configFile.SetInt("selectedScreenshotPlugin", _selectedPlugin);
            _configFile.SetInt("framerate", _fps);
            _configFile.SetInt("exportFramerate", _exportFps);
            _configFile.SetBool("autoGenerateVideo", _autoGenerateVideo);
            _configFile.SetBool("autoDeleteImages", _autoDeleteImages);
            _configFile.SetBool("limitDuration", _limitDuration);
            _configFile.SetInt("selectedLimitDurationType", (int)_selectedLimitDuration);
            _configFile.SetFloat("limitDurationNumber", _limitDurationNumber);
            _configFile.SetInt("selectedExtension", (int)_selectedExtension);
            _configFile.SetBool("resize", _resize);
            _configFile.SetInt("resizeX", _resizeX);
            _configFile.SetInt("resizeY", _resizeY);
            _configFile.SetInt("selectedUpdateDynamicBonesMode", (int)_selectedUpdateDynamicBones);
            _configFile.SetBool("realignTimeline", _realignTimeline);
            _configFile.SetInt("prewarmLoopCount", _prewarmLoopCount);
            _configFile.SetBool("showCaptureSection", _showCaptureSection);
            _configFile.SetBool("showVideoSection", _showVideoSection);
            _configFile.SetBool("showOtherSection", _showOtherSection);
            _configFile.SetBool("showTooltips", _showTooltips);
            _configFile.SetBool("parallelScreenshotEncoding", _parallelScreenshotEncoding);
            foreach (IScreenshotPlugin plugin in _screenshotPlugins)
                plugin.SaveParams();
            foreach (IExtension extension in _extensions)
                extension.SaveParams();
            _configFile.Save();
        }
        #endregion

        #region Public Methods
        public void RecordVideo()
        {
            if (_isRecording == false)
                StartCoroutine(RecordVideo_Routine_Safe());
        }

        public void StopRecording()
        {
            _breakRecording = true;
        }
        #endregion

        #region Private Methods
#if IPA
        private void AddScreenshotPlugin<T>(Harmony harmony) where T : IScreenshotPlugin, new()
#elif BEPINEX
        private void AddScreenshotPlugin<T>(Harmony harmony) where T : IScreenshotPlugin, new()
#endif
        {
            try
            {
                var plugin = new T();
                if (plugin.Init(harmony))
                    _screenshotPlugins.Add(plugin);
            }
            catch (Exception e)
            {
                Logger.LogError("Couldn't add screenshot plugin " + typeof(T).FullName + ".\n" + e);
            }

        }

        private void LimitCount()
        {
            switch (_selectedLimitDuration)
            {
                case LimitDurationType.Frames:
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label(new GUIContent(_currentDictionary.GetString(TranslationKey.LimitByLimitCount), _currentDictionary.GetString(TranslationKey.LimitByLimitCountTooltip).Replace("\\n", "\n")), GUILayout.ExpandWidth(false));
                            string s = GUILayout.TextField(_limitDurationNumber.ToString("0"));
                            int res;
                            if (int.TryParse(s, out res) == false || res < 1)
                                res = 1;
                            _limitDurationNumber = res;
                        }
                        GUILayout.EndHorizontal();

                        float totalSeconds = _limitDurationNumber / _fps;
                        GUILayout.Label($"{totalSeconds:0.0000} {_currentDictionary.GetString(TranslationKey.Seconds)}, {Mathf.RoundToInt(_limitDurationNumber)} {_currentDictionary.GetString(TranslationKey.Frames)}");

                        break;
                    }
                case LimitDurationType.Seconds:
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label(new GUIContent(_currentDictionary.GetString(TranslationKey.LimitByLimitCount), _currentDictionary.GetString(TranslationKey.LimitByLimitCountTooltip).Replace("\\n", "\n")), GUILayout.ExpandWidth(false));
                            string s = GUILayout.TextField(_limitDurationNumber.ToString("0.000"));
                            float res;
                            if (float.TryParse(s, out res) == false || res <= 0f)
                                res = 0.001f;
                            _limitDurationNumber = res;
                        }
                        GUILayout.EndHorizontal();

                        float totalFrames = _limitDurationNumber * _fps;
                        GUILayout.Label($"{_limitDurationNumber:0.0000} {_currentDictionary.GetString(TranslationKey.Seconds)}, {Mathf.RoundToInt(totalFrames)} {_currentDictionary.GetString(TranslationKey.Frames)} ({totalFrames:0.000})");

                        break;
                    }
                case LimitDurationType.Animation:
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label(new GUIContent(_currentDictionary.GetString(TranslationKey.LimitByPrewarmLoopCount), _currentDictionary.GetString(TranslationKey.LimitByPrewarmLoopCountTooltip).Replace("\\n", "\n")), GUILayout.ExpandWidth(false));
                            string s = GUILayout.TextField(_prewarmLoopCount.ToString());
                            int res;
                            if (int.TryParse(s, out res) == false || res < 0)
                                res = 1;
                            _prewarmLoopCount = res;
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label(new GUIContent(_currentDictionary.GetString(TranslationKey.LimitByLoopsToRecord), _currentDictionary.GetString(TranslationKey.LimitByLoopsToRecordTooltip).Replace("\\n", "\n")), GUILayout.ExpandWidth(false));
                            string s = GUILayout.TextField(_limitDurationNumber.ToString("0.000"));
                            float res;
                            if (float.TryParse(s, out res) == false || res <= 0)
                                res = 0.001f;
                            _limitDurationNumber = res;
                        }
                        GUILayout.EndHorizontal();

                        if (_currentAnimator != null)
                        {
                            AnimatorStateInfo info = _currentAnimator.GetCurrentAnimatorStateInfo(0);
                            float totalLength = info.length * _limitDurationNumber;
                            float totalFrames = totalLength * _fps;
                            GUILayout.Label($"{totalLength:0.0000} {_currentDictionary.GetString(TranslationKey.Seconds)}, {Mathf.RoundToInt(totalFrames)} {_currentDictionary.GetString(TranslationKey.Frames)} ({totalFrames:0.000})");
                        }
                        break;
                    }
                case LimitDurationType.Timeline:
                    {
                        GUILayout.BeginHorizontal();
                        {
                            _realignTimeline = GUILayout.Toggle(_realignTimeline, new GUIContent(_currentDictionary.GetString(TranslationKey.RealignTimeline), _currentDictionary.GetString(TranslationKey.RealignTimelineTooltip).Replace("\\n", "\n")));
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        {

                            GUILayout.Label(new GUIContent(_currentDictionary.GetString(TranslationKey.LimitByPrewarmLoopCount), _currentDictionary.GetString(TranslationKey.LimitByPrewarmLoopCountTooltip).Replace("\\n", "\n")), GUILayout.ExpandWidth(false));
                            int.TryParse(GUILayout.TextField(_prewarmLoopCount.ToString()), out _prewarmLoopCount);
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label(new GUIContent(_currentDictionary.GetString(TranslationKey.LimitByLoopsToRecord), _currentDictionary.GetString(TranslationKey.LimitByLoopsToRecordTooltip).Replace("\\n", "\n")), GUILayout.ExpandWidth(false));
                            string s = GUILayout.TextField(_limitDurationNumber.ToString("0.000"));
                            float res;
                            if (float.TryParse(s, out res) == false || res <= 0)
                                res = 0.001f;
                            _limitDurationNumber = res;
                        }
                        GUILayout.EndHorizontal();

                        if (_timelinePresent)
                        {
                            if (!_isRecording)
                                _realTimelineDuration = TimelineCompatibility.EstimateRealDuration();

                            float totalLength = _realTimelineDuration * _limitDurationNumber;
                            float totalFrames = totalLength * _fps;
                            GUILayout.Label($"{totalLength:0.0000} {_currentDictionary.GetString(TranslationKey.Seconds)}, {Mathf.RoundToInt(totalFrames)} {_currentDictionary.GetString(TranslationKey.Frames)} ({totalFrames:0.000})");
                        }
                        break;
                    }
            }
        }

        private void WindowCaptureSection()
        {
            IScreenshotPlugin plugin = _screenshotPlugins[(int)_selectedPlugin];

            GUILayout.BeginVertical("Box");
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(new GUIContent(_currentDictionary.GetString(TranslationKey.CaptureSettingsHeading)), Styles.SectionLabelStyle);
                    if (GUILayout.Button(_showCaptureSection ? "-" : "+", GUILayout.Width(30)))
                    {
                        _showCaptureSection = !_showCaptureSection;
                    }
                }
                GUILayout.EndHorizontal();
                if (!_showCaptureSection)
                {
                    GUILayout.EndVertical();
                    return;
                }

                Vector2 currentSize = plugin.currentSize;
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label($"{_currentDictionary.GetString(TranslationKey.CurrentSize)}: {currentSize.x:#}x{currentSize.y:#}");
                    GUILayout.FlexibleSpace();
                    _showTooltips = GUILayout.Toggle(_showTooltips, _currentDictionary.GetString(TranslationKey.ShowTooltips));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal("Box");
                {
                    GUILayout.BeginVertical(GUILayout.Width(Styles.WindowWidth / 3));
                    {
                        GUILayout.Label(new GUIContent(_currentDictionary.GetString(TranslationKey.ScreenshotTool), _currentDictionary.GetString(TranslationKey.ScreenshotToolTooltip).Replace("\\n", "\n")), Styles.CenteredLabelStyle);
                        _pluginScrollPos = GUILayout.BeginScrollView(_pluginScrollPos, GUILayout.Height(120));
                        {
                            _selectedPlugin = GUILayout.SelectionGrid(_selectedPlugin, _screenshotPlugins.Select(p => p.name).ToArray(), 1);
                        }
                        GUILayout.EndScrollView();

                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical("Box");
                    plugin.DisplayParams();
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginVertical("Box");
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label(new GUIContent(_currentDictionary.GetString(TranslationKey.Framerate), _currentDictionary.GetString(TranslationKey.FramerateTooltip).Replace("\\n", "\n")));

                        if (GUILayout.Button("-", GUILayout.Width(30)))
                        {
                            int index = Array.BinarySearch(_presetFps, _fps);
                            if (index < 0)
                                index = ~index;
                            _fps = _presetFps[Math.Max(0, index - 1)];
                        }
                        string fpsString = GUILayout.TextField(_fps.ToString(), GUILayout.Width(50));
                        if (int.TryParse(fpsString, out int res))
                            _fps = Mathf.Clamp(res, 1, 10000);

                        if (GUILayout.Button("+", GUILayout.Width(30)))
                        {
                            int index = Array.BinarySearch(_presetFps, _fps);
                            if (index < 0)
                                index = ~index;
                            _fps = _presetFps[Math.Min(_presetFps.Length - 1, index + 1)];
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label(new GUIContent(_currentDictionary.GetString(TranslationKey.ExportFramerate), _currentDictionary.GetString(TranslationKey.ExportFramerateTooltip).Replace("\\n", "\n")));

                        if (GUILayout.Button("-", GUILayout.Width(30)))
                        {
                            int index = Array.BinarySearch(_presetFps, _exportFps);
                            if (index < 0)
                                index = ~index;
                            _exportFps = _presetFps[Math.Max(0, index - 1)];
                        }
                        string exportFpsString = GUILayout.TextField(_exportFps.ToString(), GUILayout.Width(50));
                        if (int.TryParse(exportFpsString, out int res))
                            _exportFps = Mathf.Clamp(res, 1, _fps);
                        if (GUILayout.Button("+", GUILayout.Width(30)))
                        {
                            int index = Array.BinarySearch(_presetFps, _exportFps);
                            if (index < 0)
                                index = ~index;
                            _exportFps = _presetFps[Math.Min(_presetFps.Length - 1, index + 1)];
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label(new GUIContent(_currentDictionary.GetString(TranslationKey.DBUpdateMode), _currentDictionary.GetString(TranslationKey.DBUpdateModeTooltip).Replace("\\n", "\n")));
                        _selectedUpdateDynamicBones = (UpdateDynamicBonesType)GUILayout.SelectionGrid((int)_selectedUpdateDynamicBones, _updateDynamicBonesTypeNames, 2);
                    }
                    GUILayout.EndHorizontal();

                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical("Box");
                {
                    GUILayout.BeginVertical();
                    {
                        _limitDuration = GUILayout.Toggle(_limitDuration, new GUIContent(_currentDictionary.GetString(TranslationKey.LimitBy), _currentDictionary.GetString(TranslationKey.LimitByTooltip).Replace("\\n", "\n")));
                        if (_limitDuration)
                        {
                            _selectedLimitDuration = (LimitDurationType)GUILayout.SelectionGrid((int)_selectedLimitDuration, _limitDurationNames, 4);
                            LimitCount();
                        }
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }

        private void WindowVideoSection()
        {
            IScreenshotPlugin plugin = _screenshotPlugins[(int)_selectedPlugin];
            IExtension extension = _extensions[(int)_selectedExtension];
            bool prevGuiEnabled = GUI.enabled;
            Vector2 currentSize = plugin.currentSize;

            GUILayout.BeginVertical("Box");
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(new GUIContent(_currentDictionary.GetString(TranslationKey.VideoSettingsHeading)), Styles.SectionLabelStyle);
                    if (GUILayout.Button(_showVideoSection ? "-" : "+", GUILayout.Width(30)))
                    {
                        _showVideoSection = !_showVideoSection;
                    }
                }
                GUILayout.EndHorizontal();
                if (!_showVideoSection)
                {
                    GUILayout.EndVertical();
                    return;
                }

                GUILayout.BeginHorizontal();
                {
                    _autoGenerateVideo = GUILayout.Toggle(_autoGenerateVideo, new GUIContent(_currentDictionary.GetString(TranslationKey.CreateVideo)));
                    if (!_autoGenerateVideo)
                    {
                        GUILayout.EndHorizontal();
                        GUILayout.EndVertical();
                        return;
                    }

                    GUILayout.FlexibleSpace();

                    GUILayout.BeginHorizontal();
                    {
                        _resize = GUILayout.Toggle(_resize, _currentDictionary.GetString(TranslationKey.Resize), GUILayout.ExpandWidth(false));
                        prevGuiEnabled = GUI.enabled;
                        GUI.enabled = _resize && prevGuiEnabled;

                        string s = GUILayout.TextField(_resizeX.ToString(), GUILayout.Width(50));
                        int res;
                        if (int.TryParse(s, out res) == false || res < 1)
                            res = 1;
                        _resizeX = res;

                        s = GUILayout.TextField(_resizeY.ToString(), GUILayout.Width(50));
                        if (int.TryParse(s, out res) == false || res < 1)
                            res = 1;
                        _resizeY = res;

                        if (GUILayout.Button("Default", GUILayout.ExpandWidth(false)))
                        {
                            _resizeX = Screen.width;
                            _resizeY = Screen.height;
                        }
                        if (_resizeX > currentSize.x)
                            _resizeX = (int)currentSize.x;
                        if (_resizeY > currentSize.y)
                            _resizeY = (int)currentSize.y;

                        GUI.enabled = prevGuiEnabled;
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal("Box");
                {
                    GUILayout.BeginVertical(GUILayout.Width(Styles.WindowWidth / 5));
                    {
                        GUILayout.Label(new GUIContent(_currentDictionary.GetString(TranslationKey.Extension), _currentDictionary.GetString(TranslationKey.ExtensionTooltip).Replace("\\n", "\n")), Styles.CenteredLabelStyle);
                        _extensionScrollPos = GUILayout.BeginScrollView(_extensionScrollPos, GUILayout.Height(160));
                        {
                            _selectedExtension = (ExtensionsType)GUILayout.SelectionGrid((int)_selectedExtension, _extensionsNames, 1);
                        }
                        GUILayout.EndScrollView();
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical("Box");
                    extension.DisplayParams();
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        void WindowOtherSection()
        {
            IScreenshotPlugin screenshotPlugin = _screenshotPlugins[_selectedPlugin];
            string imageExtension = screenshotPlugin.extension;
            string tempName = DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ss");
            string framesFolder = Path.Combine(_globalFramesFolder.Value, tempName);

            IExtension extension = _extensions[(int)_selectedExtension];
            string arguments = extension.GetArguments(SimplifyPath(framesFolder), imageExtension, screenshotPlugin.bitDepth, _exportFps, screenshotPlugin.transparency, _resize, _resizeX, _resizeY, SimplifyPath(Path.Combine(_outputFolder.Value, tempName)));

            GUILayout.BeginVertical("Box");
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(new GUIContent(_currentDictionary.GetString(TranslationKey.OtherSettingsHeading)), Styles.SectionLabelStyle);
                    if (GUILayout.Button(_showOtherSection ? "-" : "+", GUILayout.Width(30)))
                    {
                        _showOtherSection = !_showOtherSection;
                    }
                }
                GUILayout.EndHorizontal();
                if (!_showOtherSection)
                {
                    GUILayout.EndVertical();
                    return;
                }

                GUILayout.BeginHorizontal("Box");
                {
                    GUILayout.BeginVertical();
                    _autoDeleteImages = GUILayout.Toggle(_autoDeleteImages, _currentDictionary.GetString(TranslationKey.AutoDeleteImages));
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical();
                    _clearSceneBeforeEncoding = GUILayout.Toggle(_clearSceneBeforeEncoding, _currentDictionary.GetString(TranslationKey.EmptyScene), Styles.DangerToggleStyle);
                    _closeWhenDone = GUILayout.Toggle(_closeWhenDone, _currentDictionary.GetString(TranslationKey.CloseStudio), Styles.DangerToggleStyle);
                    _parallelScreenshotEncoding = GUILayout.Toggle(_parallelScreenshotEncoding, new GUIContent(_currentDictionary.GetString(TranslationKey.ParallelScreenshotEncoding), _currentDictionary.GetString(TranslationKey.ParallelEncodingTooltip)), Styles.DangerToggleStyle);
                    GUILayout.EndVertical();

                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private void WindowRecordSection()
        {
            IScreenshotPlugin plugin = _screenshotPlugins[(int)_selectedPlugin];
            IExtension extension = _extensions[(int)_selectedExtension];

            bool prevGuiEnabled = GUI.enabled;

            GUI.enabled = _generatingVideo == false && _startOnNextClick == false &&
                          (_limitDuration == false || _selectedLimitDuration != LimitDurationType.Animation || (_currentAnimator && _currentAnimator.speed > 0.001f && _animationIsPlaying));
            _startOnNextClick = GUILayout.Toggle(_startOnNextClick, _currentDictionary.GetString(TranslationKey.StartRecordingOnNextClick));

            GUILayout.BeginHorizontal();
            {
                string reason;
                if (_autoGenerateVideo == false || extension.IsCompatibleWithPlugin(plugin, out reason))
                {
                    if (_isRecording == false)
                    {
                        if (GUILayout.Button(_currentDictionary.GetString(TranslationKey.StartRecording)))
                            RecordVideo();
                    }
                    else
                    {
                        if (GUILayout.Button(_currentDictionary.GetString(TranslationKey.StopRecording)))
                            StopRecording();
                    }
                }
                else
                {
                    Color c = GUI.color;
                    GUI.color = Color.yellow;
                    GUILayout.Label("Video format is incompatible with the current screenshot plugin or its settings. Reason: " + reason);
                    GUI.color = c;
                }
            }
            GUILayout.EndHorizontal();

            GUI.enabled = prevGuiEnabled;

            GUIStyle customLabel = GUI.skin.label;
            TextAnchor cachedAlignment = customLabel.alignment;
            customLabel.alignment = TextAnchor.UpperCenter;
            GUILayout.Label(_currentMessage);
            customLabel.alignment = cachedAlignment;

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Box("", _customBoxStyle, GUILayout.Width((_windowRect.width - 20) * Mathf.Clamp(_progressBarPercentage, 0f, 1f)), GUILayout.Height(10));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void Window(int id)
        {
            Styles.BeginVESkin();

            GUILayout.BeginVertical();
            if (_screenshotPlugins.Count == 0)
            {
                GUILayout.Label("No screenshot plugins available. Update BepisPlugins and check log for related errors.");
            }
            else
            {
                if (_selectedPlugin < 0 || _selectedPlugin >= _screenshotPlugins.Count)
                    _selectedPlugin = 0;

                WindowCaptureSection();
                GUILayout.Space(Styles.SectionSpacing);
                WindowVideoSection();
                GUILayout.Space(Styles.SectionSpacing);
                WindowOtherSection();
                GUILayout.Space(Styles.SectionSpacing);
                WindowRecordSection();
            }
            GUILayout.EndVertical();

            ShowTooltip();

            Styles.EndVESkin();

            GUI.DragWindow();
        }

        void ShowTooltip()
        {
            if (_showTooltips && Event.current.type == EventType.Repaint && !string.IsNullOrEmpty(GUI.tooltip))
            {
                Vector2 mousePos = Event.current.mousePosition;
                Vector2 size = GUI.skin.label.CalcSize(new GUIContent(GUI.tooltip));
                Rect tooltipRect = new Rect(mousePos.x, mousePos.y + 18, size.x + 12, size.y + 12);

                tooltipRect.x = Mathf.Clamp(tooltipRect.x, 0, _windowRect.width - tooltipRect.width);

                GUI.Box(tooltipRect, GUI.tooltip, Styles.TooltipStyle);
            }
        }

        private void SetLanguage(Language language)
        {
            _language.Value = language;
            switch (language)
            {
                case Language.English:
                    _currentDictionary = _englishDictionary;
                    break;
                case Language.中文:
                    _currentDictionary = _chineseDictionary;
                    break;
            }

            _limitDurationNames = new[]
            {
                _currentDictionary.GetString(TranslationKey.LimitByFrames),
                _currentDictionary.GetString(TranslationKey.LimitBySeconds),
                _currentDictionary.GetString(TranslationKey.LimitByAnimation),
                _currentDictionary.GetString(TranslationKey.LimitByTimeline)
            };

            _updateDynamicBonesTypeNames = new[]
            {
                _currentDictionary.GetString(TranslationKey.DBDefault),
                _currentDictionary.GetString(TranslationKey.DBEveryFrame)
            };

            foreach (IScreenshotPlugin plugin in _screenshotPlugins)
                plugin.UpdateLanguage();
            foreach (IExtension extension in _extensions)
                extension.UpdateLanguage();
        }

        private IEnumerator RecordVideo_Routine_Safe()
        {
            var coroutine = RecordVideo_Routine();
            while (true)
            {
                try
                {
                    if (!coroutine.MoveNext())
                        break;
                }
                catch
                {
                    // Make sure the UI gets unlocked if there's an unexpected crash
                    _isRecording = false;
                    throw;
                }

                yield return coroutine.Current;
            }
        }
        private IEnumerator RecordVideo_Routine()
        {
            _isRecording = true;
            _messageColor = Color.white;

            Animator currentAnimator = _currentAnimator;

            IScreenshotPlugin screenshotPlugin = _screenshotPlugins[_selectedPlugin];

            //string tempName = DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ss");
            _tempDateTime = DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ss");
            string framesFolder = Path.Combine(_globalFramesFolder.Value, _tempDateTime);

            if (Directory.Exists(framesFolder) == false)
                Directory.CreateDirectory(framesFolder);

            int cachedCaptureFramerate = Time.captureFramerate;
            Time.captureFramerate = _fps;

            if (_selectedUpdateDynamicBones == UpdateDynamicBonesType.EveryFrame)
            {
                foreach (DynamicBone dynamicBone in Resources.FindObjectsOfTypeAll<DynamicBone>())
                    dynamicBone.m_UpdateRate = -1;
                foreach (DynamicBone_Ver02 dynamicBone in Resources.FindObjectsOfTypeAll<DynamicBone_Ver02>())
                    dynamicBone.UpdateRate = -1;
            }

            _currentRecordingFrame = 0;
            _currentRecordingTime = 0;
            screenshotPlugin.OnStartRecording();

            if (_limitDuration)
            {
                if (_prewarmLoopCount > 0)
                {
                    switch (_selectedLimitDuration)
                    {
                        case LimitDurationType.Animation:
                            int j = 0;
                            float lastNormalizedTime = 0;
                            while (true)
                            {
                                AnimatorStateInfo info = currentAnimator.GetCurrentAnimatorStateInfo(0);
                                float currentNormalizedTime = info.normalizedTime % 1f;
                                if (lastNormalizedTime > 0.5f && currentNormalizedTime < lastNormalizedTime)
                                    j++;
                                lastNormalizedTime = currentNormalizedTime;
                                if (j == _prewarmLoopCount)
                                {
                                    if (!info.loop)
                                        yield return new WaitForEndOfFrame();
                                    break;
                                }
                                yield return new WaitForEndOfFrame();
                                _currentMessage = $"{_currentDictionary.GetString(TranslationKey.PrewarmingAnimation)} {j + 1}/{_prewarmLoopCount}";
                                _progressBarPercentage = lastNormalizedTime;
                            }
                            break;
                        case LimitDurationType.Timeline:
                            if (TimelineCompatibility.GetIsPlaying() == false)
                                TimelineCompatibility.Play();
                            j = 0;
                            float lastTime = 0;
                            while (true)
                            {
                                float duration = TimelineCompatibility.GetDuration();
                                float currentTime = TimelineCompatibility.GetPlaybackTime() % duration;
                                if (currentTime < lastTime)
                                    j++;
                                lastTime = currentTime;
                                if (j == _prewarmLoopCount)
                                    break;
                                yield return new WaitForEndOfFrame();
                                _currentMessage = $"{_currentDictionary.GetString(TranslationKey.PrewarmingAnimation)} {j + 1}/{_prewarmLoopCount}";
                                _progressBarPercentage = lastTime / duration;
                            }
                            break;
                    }
                }
            }
            else
            {
                yield return new WaitForEndOfFrame();
            }

            DateTime startTime = DateTime.Now;
            _progressBarPercentage = 0f;

            int limit = 1;
            if (_limitDuration)
            {
                switch (_selectedLimitDuration)
                {
                    case LimitDurationType.Frames:
                        limit = Mathf.RoundToInt(_limitDurationNumber);
                        break;
                    case LimitDurationType.Seconds:
                        limit = Mathf.RoundToInt(_limitDurationNumber * _fps);
                        break;
                    case LimitDurationType.Animation:
                        limit = Mathf.RoundToInt(currentAnimator.GetCurrentAnimatorStateInfo(0).length * _limitDurationNumber * _fps);
                        break;
                    case LimitDurationType.Timeline:
                        limit = Mathf.RoundToInt(TimelineCompatibility.EstimateRealDuration() * _limitDurationNumber * _fps);
                        break;
                }
                _recordingFrameLimit = limit;
            }
            else
            {
                _recordingFrameLimit = -1;
            }

            if (_selectedLimitDuration == LimitDurationType.Timeline)
            {
                if (_realignTimeline)
                {
                    TimelineCompatibility.Stop();
                    TimelineCompatibility.Play();
                    yield return new WaitForEndOfFrame();
                    TimelineCompatibility.Stop();

                    // Wait for dynamic bones and other physics to settle
                    for (int x = 0; x < _fps; x++)
                        yield return new WaitForEndOfFrame();
                }

                if (TimelineCompatibility.GetIsPlaying() == false)
                    TimelineCompatibility.Play();
            }

            ParallelScreenshotEncoder parallelEncoder = null;
            if (_parallelScreenshotEncoding)
                parallelEncoder = new ParallelScreenshotEncoder();

            int exportInterval = _fps / _exportFps;
            TimeSpan elapsed = TimeSpan.Zero;
            int i = 0;
            string imageExtension = screenshotPlugin.extension;

            if (screenshotPlugin is ReshadePlugin)
            {
                // For the ReshadePlugin, we cannot capture the current frame because Reshade runs after the end of frame.
                // The Reshade addon stores the frame after reshade finishes effects. That buffer will be one frame behind us.
                // Because of that, we skip a frame at the start.
                yield return new WaitForEndOfFrame();

                if (_limitDuration && _selectedLimitDuration != LimitDurationType.Timeline)
                {
                    // For branches which did not wait for end of frame before this, the above call only puts them at the end of the current frame.
                    // We have to do another wait to get to the next frame.
                    yield return new WaitForEndOfFrame();
                }
            }

            bool error = false;
            if (_autoGenerateVideo)
            {
                _generatingVideo = false;

                if (_clearSceneBeforeEncoding)
                    Studio.Studio.Instance.InitScene(false);

                _messageColor = Color.yellow;
                if (Directory.Exists(_outputFolder.Value) == false)
                    Directory.CreateDirectory(_outputFolder.Value);
                _currentMessage = _currentDictionary.GetString(TranslationKey.GeneratingVideo);
                // Need to match the timing of the first frame, excluding the timeline.
                if (_selectedLimitDuration != LimitDurationType.Timeline)
                {
                    yield return null;
                }
                    
                IExtension extension = _extensions[(int)_selectedExtension];

                //string fileName = SimplifyPath(Path.Combine(_outputFolder.Value, tempName));
                string fileName = SimplifyPath(Path.Combine(_outputFolder.Value, _tempDateTime));
                string arguments = extension.GetArguments("-", imageExtension, screenshotPlugin.bitDepth, _exportFps, screenshotPlugin.transparency, _resize, _resizeX, _resizeY, fileName);

                if (screenshotPlugin is ReshadePlugin)
                {
                    int index = arguments.IndexOf("-pix_fmt") + 9;
                    arguments = arguments.Remove(index, 4);
                    arguments = arguments.Insert(index, "rgba");
                    int indexVflip = arguments.IndexOf(", vflip");

                    if (indexVflip >= 1)
                    {
                        arguments = arguments.Remove(indexVflip, 7);
                    }

                    indexVflip = arguments.IndexOf("vflip");

                    if (indexVflip >= 1)
                    {
                        arguments = arguments.Remove(indexVflip, 5);
                    }
                    
                    index = arguments.IndexOf("-vf");

                    if (arguments.Contains("-vf \"\""))
                    {
                        arguments = arguments.Remove(index, 6);
                    }
                    else if (arguments[index + 5] == ',') 
                    {
                        arguments = arguments.Remove(index + 5, 1);
                    }
                }

                IScreenshotPlugin plugin = _screenshotPlugins[(int)_selectedPlugin];
                Vector2 currentSize = plugin.currentSize;
                string targetSize = currentSize.x + "x" + currentSize.y;

                arguments = "-s " + targetSize + " " + arguments;

                int totalFrames = i * _exportFps / _fps;

                if (error == false)
                {
                    _ffmpegProcessMaster = StartExternalProcess(extension.GetExecutable(), arguments, extension.canProcessStandardOutput, extension.canProcessStandardError, true);

                    StartCoroutine(HandleProcessOutput(_ffmpegProcessMaster, totalFrames, extension.canProcessStandardOutput, val => error = val, true));

                    InitializeRecoder((int)currentSize.x, (int)currentSize.y);
                }

                // The related variable name is palettegen, but its function is closer to all of processing of gif.
                if (_selectedExtension == ExtensionsType.GIF && (extension as GIFExtension)?.IsPaletteGenRequired() == true)
                {
                    string arguments_palettegen = (extension as GIFExtension).GetArgumentsPaletteGen(SimplifyPath(framesFolder), "", "", imageExtension, screenshotPlugin.bitDepth, _exportFps, screenshotPlugin.transparency, _resize, _resizeX, _resizeY, (int)currentSize.x, (int)currentSize.y, fileName);

                    _ffmpegProcessSlave = StartExternalProcess(extension.GetExecutable(), arguments_palettegen, false, extension.canProcessStandardError, false);

                    StartCoroutine(HandleProcessOutput(_ffmpegProcessSlave, totalFrames, false, val => error = val, false));
                }
            }
            else
            {
                _messageColor = Color.green;
                _currentMessage = _currentDictionary.GetString(TranslationKey.Done);
            }

            for (; ; i++)
            {
                if (_limitDuration && i >= limit)
                    StopRecording();
                if (_breakRecording)
                {
                    _breakRecording = false;
                    break;
                }

                if (i % exportInterval == 0)
                {
                    string savePath = Path.Combine(framesFolder, $"{i / exportInterval}.{imageExtension}");

#if !HONEYSELECT
                    if (screenshotPlugin.IsTextureCaptureAvailable() == true)
                    {
                        Texture2D texture = screenshotPlugin.CaptureTexture();
                        if (texture)
                        {
                            if (_parallelScreenshotEncoding)
                            {
                                // WARNING: Textures are scheduled for destruction by the encoder threads.
                                parallelEncoder.QueueScreenshotDestructive(texture, screenshotPlugin.imageFormat, savePath);
                            }
                            else
                            {
                                if (_autoGenerateVideo)
                                {
#if (!KOIKATSU || SUNSHINE)
                                    _frameDataBuffer = GetNativeRawData(texture);
#else
                                    _frameDataBuffer = texture.GetRawTextureData();
#endif
                                    _ffmpegStdin.BaseStream.Write(_frameDataBuffer, 0, _frameBufferSize);
                                    _ffmpegStdin.BaseStream.Flush();
                                }
                                else
                                {
                                    //TextureEncoder.EncodeAndWriteTexture(texture, screenshotPlugin.imageFormat, savePath);
                                    TextureEncoder.EncodeAndWriteTexture(texture, screenshotPlugin.imageFormat, screenshotPlugin.transparency, savePath);
                                }

                                Destroy(texture);
                                texture = null;
                            }
                        }
                    }
                    else
                    {
                        byte[] frame = screenshotPlugin.Capture(savePath);
                        if (frame != null)
                            File.WriteAllBytes(savePath, frame);
                    }

#else
                    byte[] frame = screenshotPlugin.Capture(savePath);
                    if (frame != null)
                        File.WriteAllBytes(savePath, frame);
#endif

                            }

                elapsed = DateTime.Now - startTime;

                TimeSpan remaining = TimeSpan.FromSeconds((limit - i - 1) * elapsed.TotalSeconds / (i + 1));

                if (_limitDuration)
                    _progressBarPercentage = (i + 1f) / limit;
                else
                    _progressBarPercentage = (i % _fps) / (float)_fps;

                _currentMessage = $"{_currentDictionary.GetString(TranslationKey.TakingScreenshot)} {i + 1}{(_limitDuration ? $"/{limit} {_progressBarPercentage * 100:0.0}%\n{_currentDictionary.GetString(TranslationKey.ETA)}: {remaining.Hours:0}:{remaining.Minutes:00}:{remaining.Seconds:00} {_currentDictionary.GetString(TranslationKey.Elapsed)}: {elapsed.Hours:0}:{elapsed.Minutes:00}:{elapsed.Seconds:00}" : "")}";

                _currentRecordingFrame = i + 1;
                _currentRecordingTime = (i + 1) / (double)_fps;
                yield return new WaitForEndOfFrame();
            }

            if (_parallelScreenshotEncoding)
            {
                bool success = parallelEncoder.WaitForAll();
                if (!success)
                    Logger.LogWarning("Parallel encoder could not finish in a reasonable time.");
            }

            screenshotPlugin.OnEndRecording();
            Time.captureFramerate = cachedCaptureFramerate;

            Logger.LogInfo($"Time spent taking screenshots: {elapsed.Hours:0}:{elapsed.Minutes:00}:{elapsed.Seconds:00}");

            foreach (DynamicBone dynamicBone in Resources.FindObjectsOfTypeAll<DynamicBone>())
                dynamicBone.m_UpdateRate = 60;
            foreach (DynamicBone_Ver02 dynamicBone in Resources.FindObjectsOfTypeAll<DynamicBone_Ver02>())
                dynamicBone.UpdateRate = 60;

            if (_autoGenerateVideo)
            {
                _ffmpegStdin.Close();
                _frameDataBuffer = null;
            }
            
            _generatingVideo = false;
            Logger.LogInfo($"Time spent generating video: {elapsed.Hours:0}:{elapsed.Minutes:00}:{elapsed.Seconds:00}");

            _progressBarPercentage = 1;

            if (_autoDeleteImages && error == false)
            {
                var retry = false;
                try
                {
                    Directory.Delete(framesFolder, true);
                }
                catch
                {
                    retry = true;
                }
                if (retry)
                {
                    // Try waiting for any remaining file locks to release
                    yield return new WaitForSecondsRealtime(1);
                    try
                    {
                        Directory.Delete(framesFolder, true);
                    }
                    catch (Exception e)
                    {
                        Logger.LogWarning("Failed to auto delete images: " + e);
                    }
                }
            }
            _isRecording = false;
            Resources.UnloadUnusedAssets();
            GC.Collect();

            if (_closeWhenDone)
#if HONEYSELECT2 || SUNSHINE
                Illusion.Game.Utils.Scene.GameEnd(false);
#else
                Manager.Scene.Instance.GameEnd(false);
#endif
        }

        private Process StartExternalProcess(string exe, string arguments, bool redirectStandardOutput, bool redirectStandardError)
        {
            IExtension extension = _extensions[(int)_selectedExtension];

            Logger.LogInfo($"Starting process: {exe} {arguments}");
            Process proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Directory.GetCurrentDirectory() + "\\",
                    RedirectStandardOutput = redirectStandardOutput,
                    RedirectStandardError = redirectStandardError,
                    RedirectStandardInput = true
                }
            };

            if (redirectStandardOutput)
            {
                proc.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        foreach (char c in e.Data)
                            extension.ProcessStandardOutput(c);
                        extension.ProcessStandardOutput('\n');
                    }
                };
            }
            proc.Start();

            _ffmpegStdin = new StreamWriter(proc.StandardInput.BaseStream);

            if (redirectStandardOutput)
            {
                proc.BeginOutputReadLine();
            }

            return proc;
        }

        private Process StartExternalProcess(string exe, string arguments, bool redirectStandardOutput, bool redirectStandardError, bool redirectStandardInput)
        {
            IExtension extension = _extensions[(int)_selectedExtension];

            Logger.LogInfo($"Starting process: {exe} {arguments}");
            Process proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Directory.GetCurrentDirectory() + "\\",
                    RedirectStandardOutput = redirectStandardOutput,
                    RedirectStandardError = redirectStandardError,
                    RedirectStandardInput = redirectStandardInput
                }
            };

            if (redirectStandardOutput)
            {
                proc.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        foreach (char c in e.Data)
                            extension.ProcessStandardOutput(c);
                        extension.ProcessStandardOutput('\n');
                    }
                };
            }

            return proc;
        }

        private IEnumerator HandleProcessOutput(Process proc, int totalFrames, bool hasProgressOutput, Action<bool> setError)
        {
            IExtension extension = _extensions[(int)_selectedExtension];
            extension.ResetProgress();

            DateTime startTime = DateTime.Now;
            TimeSpan elapsed = TimeSpan.Zero;

            while (proc.HasExited == false)
            {
                elapsed = DateTime.Now - startTime;

                if (hasProgressOutput)
                {
                    TimeSpan eta = TimeSpan.FromSeconds((totalFrames - extension.progress) * elapsed.TotalSeconds / extension.progress);
                    _progressBarPercentage = extension.progress / (float)totalFrames;
                    _currentMessage = $"{_currentDictionary.GetString(TranslationKey.GeneratingVideo)} {extension.progress}/{totalFrames} {_progressBarPercentage * 100:0.0}%\n{_currentDictionary.GetString(TranslationKey.ETA)}: {eta.Hours:0}:{eta.Minutes:00}:{eta.Seconds:00} {_currentDictionary.GetString(TranslationKey.Elapsed)}: {elapsed.Hours:0}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
                }
                else
                {
                    _progressBarPercentage = (float)((elapsed.TotalSeconds % 6) / 6);
                    _currentMessage = $"{_currentDictionary.GetString(TranslationKey.GeneratingVideo)} {_currentDictionary.GetString(TranslationKey.Elapsed)}: {elapsed.Hours:0}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
                }

                yield return null;
                proc.Refresh();
            }

            proc.WaitForExit();

            var errorOut = proc.StandardError.ReadToEnd()?.Trim();
            if (!string.IsNullOrEmpty(errorOut))
                Logger.LogError(errorOut);

            yield return null;
            if (proc.ExitCode == 0)
            {
                _messageColor = Color.green;
                _currentMessage = _currentDictionary.GetString(TranslationKey.Done);
            }
            else
            {
                _messageColor = Color.red;
                _currentMessage = _currentDictionary.GetString(TranslationKey.GeneratingError);
                setError(true);
            }
            proc.Close();
        }

        private IEnumerator HandleProcessOutput(Process proc, int totalFrames, bool hasProgressOutput, Action<bool> setError, bool isMaster)
        {
            if (!isMaster)
            {
                yield return StartCoroutine(WaitForFFmpegExit());
            }

            proc.Start();

            if (proc.StartInfo.RedirectStandardInput)
            {
                _ffmpegStdin = new StreamWriter(proc.StandardInput.BaseStream);
            }

            if (proc.StartInfo.RedirectStandardOutput)
            {
                proc.BeginOutputReadLine();
            }

            IExtension extension = _extensions[(int)_selectedExtension];
            extension.ResetProgress();

            DateTime startTime = DateTime.Now;
            TimeSpan elapsed = TimeSpan.Zero;

            while (proc.HasExited == false)
            {
                elapsed = DateTime.Now - startTime;

                if (hasProgressOutput)
                {
                    TimeSpan eta = TimeSpan.FromSeconds((totalFrames - extension.progress) * elapsed.TotalSeconds / extension.progress);
                    _progressBarPercentage = extension.progress / (float)totalFrames;
                    _currentMessage = $"{_currentDictionary.GetString(TranslationKey.GeneratingVideo)} {extension.progress}/{totalFrames} {_progressBarPercentage * 100:0.0}%\n{_currentDictionary.GetString(TranslationKey.ETA)}: {eta.Hours:0}:{eta.Minutes:00}:{eta.Seconds:00} {_currentDictionary.GetString(TranslationKey.Elapsed)}: {elapsed.Hours:0}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
                }
                else
                {
                    _progressBarPercentage = (float)((elapsed.TotalSeconds % 6) / 6);
                    _currentMessage = $"{_currentDictionary.GetString(TranslationKey.GeneratingVideo)} {_currentDictionary.GetString(TranslationKey.Elapsed)}: {elapsed.Hours:0}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
                }

                yield return null;
                proc.Refresh();
            }

            proc.WaitForExit();

            var errorOut = proc.StandardError.ReadToEnd()?.Trim();
            if (!string.IsNullOrEmpty(errorOut))
                Logger.LogError(errorOut);

            yield return null;
            if (proc.ExitCode == 0)
            {
                _messageColor = Color.green;
                _currentMessage = _currentDictionary.GetString(TranslationKey.Done);
            }
            else
            {
                _messageColor = Color.red;
                _currentMessage = _currentDictionary.GetString(TranslationKey.GeneratingError);
                setError(true);
            }
            proc.Close();

            if (!isMaster)
            {
                string fileName = SimplifyPath(Path.Combine(_outputFolder.Value, _tempDateTime));
                string palettePath = $"{fileName}.mov";
                if (File.Exists(palettePath))
                    File.Delete(palettePath);
            }
        }

        private string SimplifyPath(string path)
        {
            string currentDirectory = Directory.GetCurrentDirectory().ToLowerInvariant().Replace('/', '\\');
            if (path.ToLowerInvariant().Replace('/', '\\').StartsWith(currentDirectory))
            {
                path = path.Remove(0, currentDirectory.Length);
                if (path.StartsWith("/") || path.StartsWith("\\"))
                    path = path.Remove(0, 1);
            }
            return path;
        }
        #endregion

        private bool IsFFmpegProcessRunning()
        {
            if (_ffmpegProcessMaster == null)
            {
                return false;
            }

            try
            {
                return !_ffmpegProcessMaster.HasExited;
            }
            catch
            {
                return false;
            }
        }

        public IEnumerator WaitForFFmpegExit()
        {
            while (IsFFmpegProcessRunning())
            {
                yield return null;
            }
        }

        private void InitializeRecoder(int width, int height)
        {
            int bytesPerPixel = 4; // aspects of ARGB

            _frameBufferSize = width * height * bytesPerPixel;

            if (_frameDataBuffer == null || _frameDataBuffer.Length != _frameBufferSize)
            {
                _frameDataBuffer = new byte[_frameBufferSize];
            }
        }

#if (!KOIKATSU || SUNSHINE)
        public byte[] GetNativeRawData(Texture2D texture)
        {
            NativeArray<byte> nativeData = texture.GetRawTextureData<byte>();
            nativeData.CopyTo(_frameDataBuffer);
            nativeData.Dispose();

            return _frameDataBuffer;
        }
#endif
    }
}
