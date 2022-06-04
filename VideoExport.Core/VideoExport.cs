using Studio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx.Logging;
using ToolBox;
using ToolBox.Extensions;
using UnityEngine;
using VideoExport.Extensions;
using VideoExport.ScreenshotPlugins;
using Resources = UnityEngine.Resources;
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
    [BepInDependency(Screencap.ScreenshotManager.GUID, BepInDependency.DependencyFlags.SoftDependency)]
#endif
    public class VideoExport : GenericPlugin
#if IPA
                               , IEnhancedPlugin
#endif
    {
        public const string Version = "1.2.4";
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
        private enum Language
        {
            English,
            中文 // Chinese
        }

        internal enum TranslationKey
        {
            ScreenshotPlugin,
            CurrentSize,
            Framerate,
            AutoGenerateVideo,
            AutoDeleteImages,
            LimitBy,
            LimitByFrames,
            LimitBySeconds,
            LimitByAnimation,
            LimitByTimeline,
            LimitByPrewarmLoopCount,
            LimitByLoopsToRecord,
            LimitByLimitCount,
            Estimated,
            Seconds,
            Frames,
            Resize,
            ResizeDefault,
            Format,
            DBUpdateMode,
            DBDefault,
            DBEveryFrame,
            Prefix,
            Suffix,
            ExampleResult,
            EmptyScene,
            CloseStudio,
            StartRecordingOnNextClick,
            StartRecording,
            StopRecording,
            PrewarmingAnimation,
            TakingScreenshot,
            ETA,
            Elapsed,
            GeneratingVideo,
            Done,
            GeneratingError,
            VideosFolderDesc,
            FramesFolderDesc,
            BuiltInCaptureTool,
            SizeMultiplier,
            CaptureMode,
            ImageFormat,
            CaptureModeNormal,
            CaptureModeImmediate,
            CaptureModeWin32,
            Transparent,
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
            AVIWarning,
            AVIQuality,
            GIFWarning,
            GIFError,
            MP4Codec,
            H265Warning,
            MP4Quality,
            MP4Preset,
            MP4PresetVerySlow,
            MP4PresetSlower,
            MP4PresetSlow,
            MP4PresetMedium,
            MP4PresetFast,
            MP4PresetFaster,
            MP4PresetVeryFast,
            MP4PresetSuperFast,
            MP4PresetUltraFast,
            WEBMCodec,
            VP9Warning,
            VP8Quality,
            VP9Quality,
            WEBMDeadline,
            WEBMDeadlineBest,
            WEBMDeadlineGood,
            WEBMDeadlineRealtime,
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
        private static string _outputFolder;
        private static string _globalFramesFolder;
        internal static GenericConfig _configFile;
        private static readonly TranslationDictionary<TranslationKey> _englishDictionary = new TranslationDictionary<TranslationKey>("VideoExport.Resources.English.xml");
        private static readonly TranslationDictionary<TranslationKey> _chineseDictionary = new TranslationDictionary<TranslationKey>("VideoExport.Resources.中文.xml");
        internal static TranslationDictionary<TranslationKey> _currentDictionary = _englishDictionary;
        private static readonly GUIStyle _customBoxStyle = new GUIStyle { normal = new GUIStyleState { background = Texture2D.whiteTexture } };

        private string[] _limitDurationNames;
        private string[] _extensionsNames;
        private string[] _updateDynamicBonesTypeNames;

        internal static bool _showUi = false;
        private bool _isRecording = false;
        private bool _breakRecording = false;
        private bool _generatingVideo = false;
        private readonly List<IScreenshotPlugin> _screenshotPlugins = new List<IScreenshotPlugin>();
        private const int _uniqueId = ('V' << 24) | ('I' << 16) | ('D' << 8) | 'E';
        private Rect _windowRect = new Rect(Screen.width / 2 - 160, 100, 320, 10);
        private RectTransform _imguiBackground;
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

        private int _selectedPlugin = 0;
        private int _fps = 60;
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
        private int _prewarmLoopCount = 3;
        private string _imagesPrefix = "";
        private string _imagesPostfix = "";
        private bool _clearSceneBeforeEncoding;
        private bool _closeWhenDone;
        private Language _language;
        #endregion

        #region Public Accessors (for other plugins probably)
        public bool isRecording { get { return _isRecording; } }
        public int currentRecordingFrame { get { return _currentRecordingFrame; } }
        public double currentRecordingTime { get { return _currentRecordingTime; } }
        public int recordingFrameLimit { get { return _recordingFrameLimit; } }
        #endregion

        internal static ConfigEntry<KeyboardShortcut> ConfigMainWindowShortcut { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> ConfigStartStopShortcut { get; private set; }
        #region Unity Methods
        protected override void Awake()
        {
            base.Awake();

            Logger = base.Logger;

            ConfigMainWindowShortcut = Config.Bind("Config", "Open VideoExport UI", new KeyboardShortcut(KeyCode.E, KeyCode.LeftControl));
            ConfigStartStopShortcut = Config.Bind("Config", "Start or Stop Recording", new KeyboardShortcut(KeyCode.E, KeyCode.LeftControl, KeyCode.LeftShift));

            _pluginFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), Name);
            var oldOutputFolder = Path.Combine(_pluginFolder, "Output");
            _outputFolder = Directory.Exists(oldOutputFolder) ? oldOutputFolder : Path.Combine(Path.Combine(Paths.GameRootPath, "UserData"), "Output");
            var oldFramesFolder = Path.Combine(_pluginFolder, "Frames");
            _globalFramesFolder = Directory.Exists(oldFramesFolder) ? oldFramesFolder : Path.Combine(Path.Combine(Paths.GameRootPath, "UserData"), "Frames");

            var harmony = HarmonyExtensions.CreateInstance(GUID);

            _configFile = new GenericConfig(Name, this);
            _selectedPlugin = _configFile.AddInt("selectedScreenshotPlugin", 0, true);
            _fps = _configFile.AddInt("framerate", 60, true);
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
            _prewarmLoopCount = _configFile.AddInt("prewarmLoopCount", 3, true);
            _imagesPrefix = _configFile.AddString("imagesPrefix", "", true);
            _imagesPostfix = _configFile.AddString("imagesPostfix", "", true);
            _language = (Language)_configFile.AddInt("language", (int)Language.English, true);
            SetLanguage(_language);
            string newOutputFolder = _configFile.AddString("outputFolder", _outputFolder, true, _currentDictionary.GetString(TranslationKey.VideosFolderDesc));
            if (Directory.Exists(newOutputFolder))
                _outputFolder = newOutputFolder;
            string newGlobalFramesFolder = _configFile.AddString("framesFolder", _globalFramesFolder, true, _currentDictionary.GetString(TranslationKey.FramesFolderDesc));
            if (Directory.Exists(newGlobalFramesFolder))
                _globalFramesFolder = newGlobalFramesFolder;

            _extensions.Add(new MP4Extension());
            _extensions.Add(new WEBMExtension());
            _extensions.Add(new GIFExtension());
            _extensions.Add(new AVIExtension());

            _extensionsNames = Enum.GetNames(typeof(ExtensionsType));

            _timelinePresent = TimelineCompatibility.Init();

            this.ExecuteDelayed(() =>
            {
#if HONEYSELECT
                this.AddScreenshotPlugin(new HoneyShot(), harmony);
                this.AddScreenshotPlugin(new PlayShot24ZHNeo(), harmony);
#endif
                AddScreenshotPlugin(new ScreencapPlugin(), harmony);
                if (Type.GetType("System.Drawing.Graphics, System.Drawing", false) != null)
                {
                    // Need to do it this way because KK blows up with a type load exception if it sees the Bitmap type anywhere in the method body
                    new Action(() => AddScreenshotPlugin(new Bitmap(), harmony))();
                }

                if (_screenshotPlugins.Count == 0)
                    Logger.LogError("No compatible screenshot plugin found, please install one.");

                SetLanguage(_language);
            }, 5);
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
                _showUi = !_showUi;
                if (_imguiBackground == null)
                    _imguiBackground = IMGUIExtensions.CreateUGUIPanelForIMGUI();
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
                if (_showUi)
                {
                    _imguiBackground.gameObject.SetActive(true);
                    IMGUIExtensions.FitRectTransformToRect(_imguiBackground, _windowRect);
                }
                else if (_imguiBackground != null)
                    _imguiBackground.gameObject.SetActive(false);
            }
            if (_showUi)
                _windowRect.height = 10f;
        }

        protected override void OnGUI()
        {
            if (_showUi == false)
                return;
            _windowRect = GUILayout.Window(_uniqueId, _windowRect, Window, "Video Export " + Version
#if BETA
                                                                                               + "b"
#endif
            );
            IMGUIExtensions.DrawBackground(_windowRect);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _configFile.SetInt("language", (int)_language);
            _configFile.SetInt("selectedScreenshotPlugin", _selectedPlugin);
            _configFile.SetInt("framerate", _fps);
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
            _configFile.SetInt("prewarmLoopCount", _prewarmLoopCount);
            _configFile.SetString("imagesPrefix", _imagesPrefix);
            _configFile.SetString("imagesPostfix", _imagesPostfix);
            _configFile.SetInt("language", (int)_language);
            _configFile.SetString("outputFolder", _outputFolder);
            _configFile.SetString("framesFolder", _globalFramesFolder);
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
                StartCoroutine(RecordVideo_Routine());
        }

        public void StopRecording()
        {
            _breakRecording = true;
        }
        #endregion

        #region Private Methods
#if IPA
        private void AddScreenshotPlugin(IScreenshotPlugin plugin, HarmonyInstance harmony)
#elif BEPINEX
        private void AddScreenshotPlugin(IScreenshotPlugin plugin, Harmony harmony)
#endif
        {
            try
            {
                if (plugin.Init(harmony))
                    _screenshotPlugins.Add(plugin);
            }
            catch (Exception e)
            {
                Logger.LogError("Couldn't add screenshot plugin " + plugin + ".\n" + e);
            }

        }

        private void Window(int id)
        {
            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Language.English.ToString()))
                    SetLanguage(Language.English);
                if (GUILayout.Button(Language.中文.ToString()))
                    SetLanguage(Language.中文);
                GUILayout.EndHorizontal();

                GUI.enabled = _isRecording == false;

                if (_screenshotPlugins.Count > 1)
                {
                    GUILayout.Label(_currentDictionary.GetString(TranslationKey.ScreenshotPlugin));
                    _selectedPlugin = GUILayout.SelectionGrid(_selectedPlugin, _screenshotPlugins.Select(p => p.name).ToArray(), Mathf.Clamp(_screenshotPlugins.Count, 1, 3));
                }

                IScreenshotPlugin plugin = _screenshotPlugins[_selectedPlugin];
                plugin.DisplayParams();

                Vector2 currentSize = plugin.currentSize;
                GUILayout.Label($"{_currentDictionary.GetString(TranslationKey.CurrentSize)}: {currentSize.x:#}x{currentSize.y:#}");

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(_currentDictionary.GetString(TranslationKey.Framerate), GUILayout.ExpandWidth(false));
                    _fps = Mathf.RoundToInt(GUILayout.HorizontalSlider(_fps, 1, 120));
                    string s = GUILayout.TextField(_fps.ToString(), GUILayout.Width(50));
                    int res;
                    if (int.TryParse(s, out res) == false || res < 1)
                        res = 1;
                    _fps = res;
                }
                GUILayout.EndHorizontal();

                bool guiEnabled = GUI.enabled;
                GUI.enabled = true;

                GUILayout.BeginHorizontal();
                {
                    _autoGenerateVideo = GUILayout.Toggle(_autoGenerateVideo, _currentDictionary.GetString(TranslationKey.AutoGenerateVideo));
                    _autoDeleteImages = GUILayout.Toggle(_autoDeleteImages, _currentDictionary.GetString(TranslationKey.AutoDeleteImages));
                }
                GUILayout.EndHorizontal();

                GUI.enabled = guiEnabled;

                GUILayout.BeginHorizontal();
                {
                    _limitDuration = GUILayout.Toggle(_limitDuration, _currentDictionary.GetString(TranslationKey.LimitBy), GUILayout.ExpandWidth(false));
                    guiEnabled = GUI.enabled;
                    GUI.enabled = _limitDuration && guiEnabled;
                    _selectedLimitDuration = (LimitDurationType)GUILayout.SelectionGrid((int)_selectedLimitDuration, _limitDurationNames, 2);

                    GUI.enabled = guiEnabled;
                }
                GUILayout.EndHorizontal();

                {
                    guiEnabled = GUI.enabled;
                    GUI.enabled = _limitDuration && guiEnabled;
                    switch (_selectedLimitDuration)
                    {
                        case LimitDurationType.Frames:
                            {
                                GUILayout.BeginHorizontal();

                                {
                                    GUILayout.Label(_currentDictionary.GetString(TranslationKey.LimitByLimitCount), GUILayout.ExpandWidth(false));
                                    string s = GUILayout.TextField(_limitDurationNumber.ToString("0"));
                                    int res;
                                    if (int.TryParse(s, out res) == false || res < 1)
                                        res = 1;
                                    _limitDurationNumber = res;

                                }
                                GUILayout.EndHorizontal();

                                float totalSeconds = _limitDurationNumber / _fps;
                                GUILayout.Label($"{_currentDictionary.GetString(TranslationKey.Estimated)} {totalSeconds:0.0000} {_currentDictionary.GetString(TranslationKey.Seconds)}, {Mathf.RoundToInt(_limitDurationNumber)} {_currentDictionary.GetString(TranslationKey.Frames)}");

                                break;
                            }
                        case LimitDurationType.Seconds:
                            {
                                GUILayout.BeginHorizontal();

                                {
                                    GUILayout.Label(_currentDictionary.GetString(TranslationKey.LimitByLimitCount), GUILayout.ExpandWidth(false));
                                    string s = GUILayout.TextField(_limitDurationNumber.ToString("0.000"));
                                    float res;
                                    if (float.TryParse(s, out res) == false || res <= 0f)
                                        res = 0.001f;
                                    _limitDurationNumber = res;
                                }
                                GUILayout.EndHorizontal();

                                float totalFrames = _limitDurationNumber * _fps;
                                GUILayout.Label($"{_currentDictionary.GetString(TranslationKey.Estimated)} {_limitDurationNumber:0.0000} {_currentDictionary.GetString(TranslationKey.Seconds)}, {Mathf.RoundToInt(totalFrames)} {_currentDictionary.GetString(TranslationKey.Frames)} ({totalFrames:0.000})");

                                break;
                            }
                        case LimitDurationType.Animation:
                            {
                                GUILayout.BeginHorizontal();
                                {
                                    GUILayout.Label(_currentDictionary.GetString(TranslationKey.LimitByPrewarmLoopCount), GUILayout.ExpandWidth(false));
                                    string s = GUILayout.TextField(_prewarmLoopCount.ToString());
                                    int res;
                                    if (int.TryParse(s, out res) == false || res < 0)
                                        res = 1;
                                    _prewarmLoopCount = res;
                                }
                                GUILayout.EndHorizontal();

                                GUILayout.BeginHorizontal();
                                {
                                    GUILayout.Label(_currentDictionary.GetString(TranslationKey.LimitByLoopsToRecord), GUILayout.ExpandWidth(false));
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
                                    GUILayout.Label($"{_currentDictionary.GetString(TranslationKey.Estimated)} {totalLength:0.0000} {_currentDictionary.GetString(TranslationKey.Seconds)}, {Mathf.RoundToInt(totalFrames)} {_currentDictionary.GetString(TranslationKey.Frames)} ({totalFrames:0.000})");
                                }
                                break;
                            }
                        case LimitDurationType.Timeline:
                            {
                                GUILayout.BeginHorizontal();
                                {
                                    GUILayout.Label(_currentDictionary.GetString(TranslationKey.LimitByPrewarmLoopCount), GUILayout.ExpandWidth(false));
                                    string s = GUILayout.TextField(_prewarmLoopCount.ToString());
                                    int res;
                                    if (int.TryParse(s, out res) == false || res < 0)
                                        res = 1;
                                    _prewarmLoopCount = res;
                                }
                                GUILayout.EndHorizontal();

                                GUILayout.BeginHorizontal();
                                {
                                    GUILayout.Label(_currentDictionary.GetString(TranslationKey.LimitByLoopsToRecord), GUILayout.ExpandWidth(false));
                                    string s = GUILayout.TextField(_limitDurationNumber.ToString("0.000"));
                                    float res;
                                    if (float.TryParse(s, out res) == false || res <= 0)
                                        res = 0.001f;
                                    _limitDurationNumber = res;
                                }
                                GUILayout.EndHorizontal();

                                if (_timelinePresent)
                                {
                                    float totalLength = TimelineCompatibility.GetDuration() * _limitDurationNumber;
                                    float totalFrames = totalLength * _fps;
                                    GUILayout.Label($"{_currentDictionary.GetString(TranslationKey.Estimated)} {totalLength:0.0000} {_currentDictionary.GetString(TranslationKey.Seconds)}, {Mathf.RoundToInt(totalFrames)} {_currentDictionary.GetString(TranslationKey.Frames)} ({totalFrames:0.000})");
                                }
                                break;
                            }
                    }

                    GUI.enabled = guiEnabled;
                }

                IExtension extension = _extensions[(int)_selectedExtension];

                if (_autoGenerateVideo)
                {
                    GUILayout.BeginHorizontal();
                    {
                        _resize = GUILayout.Toggle(_resize, _currentDictionary.GetString(TranslationKey.Resize), GUILayout.ExpandWidth(false));
                        guiEnabled = GUI.enabled;
                        GUI.enabled = _resize && guiEnabled;

                        GUILayout.FlexibleSpace();

                        string s = GUILayout.TextField(_resizeX.ToString(), GUILayout.Width(50));
                        int res;
                        if (int.TryParse(s, out res) == false || res < 1)
                            res = 1;
                        _resizeX = res;

                        s = GUILayout.TextField(_resizeY.ToString(), GUILayout.Width(50));
                        if (int.TryParse(s, out res) == false || res < 1)
                            res = 1;
                        _resizeY = res;

                        if (GUILayout.Button(_currentDictionary.GetString(TranslationKey.ResizeDefault), GUILayout.ExpandWidth(false)))
                        {
                            _resizeX = Screen.width;
                            _resizeY = Screen.height;
                        }
                        if (_resizeX > currentSize.x)
                            _resizeX = (int)currentSize.x;
                        if (_resizeY > currentSize.y)
                            _resizeY = (int)currentSize.y;

                        GUI.enabled = guiEnabled;
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label(_currentDictionary.GetString(TranslationKey.Format), GUILayout.ExpandWidth(false));
                        _selectedExtension = (ExtensionsType)GUILayout.SelectionGrid((int)_selectedExtension, _extensionsNames, 4);
                    }
                    GUILayout.EndHorizontal();

                    extension.DisplayParams();
                }

                GUILayout.Label(_currentDictionary.GetString(TranslationKey.DBUpdateMode));
                _selectedUpdateDynamicBones = (UpdateDynamicBonesType)GUILayout.SelectionGrid((int)_selectedUpdateDynamicBones, _updateDynamicBonesTypeNames, 2);

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(_currentDictionary.GetString(TranslationKey.Prefix), GUILayout.ExpandWidth(false));
                    string s = GUILayout.TextField(_imagesPrefix);
                    if (s != _imagesPrefix)
                    {
                        StringBuilder builder = new StringBuilder(s);
                        foreach (char chr in Path.GetInvalidFileNameChars())
                            builder.Replace(chr.ToString(), "");
                        foreach (char chr in Path.GetInvalidPathChars())
                            builder.Replace(chr.ToString(), "");
                        _imagesPrefix = builder.ToString();
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(_currentDictionary.GetString(TranslationKey.Suffix), GUILayout.ExpandWidth(false));
                    string s = GUILayout.TextField(_imagesPostfix);
                    if (s != _imagesPostfix)
                    {
                        StringBuilder builder = new StringBuilder(s);
                        foreach (char chr in Path.GetInvalidFileNameChars())
                            builder.Replace(chr.ToString(), "");
                        foreach (char chr in Path.GetInvalidPathChars())
                            builder.Replace(chr.ToString(), "");
                        _imagesPostfix = builder.ToString();
                    }
                }
                GUILayout.EndHorizontal();

                string actualExtension = plugin.extension;

                GUILayout.Label($"{_currentDictionary.GetString(TranslationKey.ExampleResult)}: {_imagesPrefix}123{_imagesPostfix}.{actualExtension}");

                guiEnabled = GUI.enabled;
                GUI.enabled = true;
                Color c = GUI.color;
                GUI.color = Color.red;

                _clearSceneBeforeEncoding = GUILayout.Toggle(_clearSceneBeforeEncoding, _currentDictionary.GetString(TranslationKey.EmptyScene));
                _closeWhenDone = GUILayout.Toggle(_closeWhenDone, _currentDictionary.GetString(TranslationKey.CloseStudio));

                GUI.color = c;
                GUI.enabled = guiEnabled;

                _startOnNextClick = GUILayout.Toggle(_startOnNextClick, _currentDictionary.GetString(TranslationKey.StartRecordingOnNextClick));

                GUI.enabled = _generatingVideo == false && _startOnNextClick == false &&
                              (_limitDuration == false || _selectedLimitDuration != LimitDurationType.Animation || (_currentAnimator.speed > 0.001f && _animationIsPlaying));

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
                        GUI.color = Color.yellow;
                        GUILayout.Label("Video format is incompatible with the current screenshot plugin or its settings. Reason: " + reason);
                        GUI.color = c;
                    }
                }

                GUI.enabled = true;

                GUILayout.EndHorizontal();
                c = GUI.color;
                GUI.color = _messageColor;

                GUIStyle customLabel = GUI.skin.label;
                TextAnchor cachedAlignment = customLabel.alignment;
                customLabel.alignment = TextAnchor.UpperCenter;
                GUILayout.Label(_currentMessage);
                customLabel.alignment = cachedAlignment;

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Box("", _customBoxStyle, GUILayout.Width((_windowRect.width - 20) * _progressBarPercentage), GUILayout.Height(10));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUI.color = c;
            }
            GUILayout.EndHorizontal();
            GUI.DragWindow();

        }

        private void SetLanguage(Language language)
        {
            _language = language;
            switch (_language)
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

        private IEnumerator RecordVideo_Routine()
        {
            _isRecording = true;
            _messageColor = Color.white;

            Animator currentAnimator = _currentAnimator;

            IScreenshotPlugin screenshotPlugin = _screenshotPlugins[_selectedPlugin];

            string tempName = DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ss");
            string framesFolder = Path.Combine(_globalFramesFolder, tempName);

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
                        limit = Mathf.RoundToInt(TimelineCompatibility.GetDuration() * _limitDurationNumber * _fps);
                        break;
                }
                _recordingFrameLimit = limit;
            }
            else
            {
                _recordingFrameLimit = -1;
            }
            TimeSpan elapsed = TimeSpan.Zero;
            int i = 0;
            string imageExtension = screenshotPlugin.extension;
            for (; ; i++)
            {
                if (_limitDuration && i >= limit)
                    StopRecording();
                if (_breakRecording)
                {
                    _breakRecording = false;
                    break;
                }

                if (i % 5 == 0)
                {
                    Resources.UnloadUnusedAssets();
                    GC.Collect();
                }
                string savePath = Path.Combine(framesFolder, $"{_imagesPrefix}{i}{_imagesPostfix}.{imageExtension}");
                byte[] frame = screenshotPlugin.Capture(savePath);
                if (frame != null)
                    File.WriteAllBytes(savePath, frame);

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
            screenshotPlugin.OnEndRecording();
            Time.captureFramerate = cachedCaptureFramerate;

            Logger.LogInfo($"Time spent taking screenshots: {elapsed.Hours:0}:{elapsed.Minutes:00}:{elapsed.Seconds:00}");

            foreach (DynamicBone dynamicBone in Resources.FindObjectsOfTypeAll<DynamicBone>())
                dynamicBone.m_UpdateRate = 60;
            foreach (DynamicBone_Ver02 dynamicBone in Resources.FindObjectsOfTypeAll<DynamicBone_Ver02>())
                dynamicBone.UpdateRate = 60;

            bool error = false;
            if (_autoGenerateVideo && i != 0)
            {
                _generatingVideo = true;

                if (_clearSceneBeforeEncoding)
                    Studio.Studio.Instance.InitScene(false);

                _messageColor = Color.yellow;
                if (Directory.Exists(_outputFolder) == false)
                    Directory.CreateDirectory(_outputFolder);
                _currentMessage = _currentDictionary.GetString(TranslationKey.GeneratingVideo);
                yield return null;
                IExtension extension = _extensions[(int)_selectedExtension];

                string arguments = extension.GetArguments(SimplifyPath(framesFolder), _imagesPrefix, _imagesPostfix, imageExtension, screenshotPlugin.bitDepth, _fps, screenshotPlugin.transparency, _resize, _resizeX, _resizeY, SimplifyPath(Path.Combine(_outputFolder, tempName)));
                startTime = DateTime.Now;
                Process proc = StartExternalProcess(extension.GetExecutable(), arguments, extension.canProcessStandardOutput, extension.canProcessStandardError);
                while (proc.HasExited == false)
                {
                    if (extension.canProcessStandardOutput)
                    {
                        int outputPeek = proc.StandardOutput.Peek();
                        for (int j = 0; j < outputPeek; j++)
                            extension.ProcessStandardOutput((char)proc.StandardOutput.Read());
                        yield return null;
                    }

                    elapsed = DateTime.Now - startTime;

                    if (extension.progress != 0)
                    {
                        TimeSpan eta = TimeSpan.FromSeconds((i - extension.progress) * elapsed.TotalSeconds / extension.progress);
                        _progressBarPercentage = extension.progress / (float)i;
                        _currentMessage = $"{_currentDictionary.GetString(TranslationKey.GeneratingVideo)} {extension.progress}/{i} {_progressBarPercentage * 100:0.0}%\n{_currentDictionary.GetString(TranslationKey.ETA)}: {eta.Hours:0}:{eta.Minutes:00}:{eta.Seconds:00} {_currentDictionary.GetString(TranslationKey.Elapsed)}: {elapsed.Hours:0}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
                    }
                    else
                        _progressBarPercentage = (float)((elapsed.TotalSeconds % 6) / 6);

                    Resources.UnloadUnusedAssets();
                    GC.Collect();
                    yield return null;
                    proc.Refresh();
                }

                proc.WaitForExit();
                Logger.LogError(proc.StandardError.ReadToEnd());

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
                    error = true;
                }
                proc.Close();
                _generatingVideo = false;
                Logger.LogInfo($"Time spent generating video: {elapsed.Hours:0}:{elapsed.Minutes:00}:{elapsed.Seconds:00}");
            }
            else
            {
                _messageColor = Color.green;
                _currentMessage = _currentDictionary.GetString(TranslationKey.Done);
            }
            _progressBarPercentage = 1;

            if (_autoDeleteImages && error == false)
                Directory.Delete(framesFolder, true);
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
                    RedirectStandardError = redirectStandardError
                }
            };
            proc.Start();

            return proc;
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

    }
}
