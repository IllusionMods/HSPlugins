//#define BETA
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ToolBox;
using ToolBox.Extensions;
using VideoExport.Extensions;
using VideoExport.ScreenshotPlugins;
using Studio;
#if IPA
using IllusionPlugin;
using Harmony;
#elif BEPINEX
using BepInEx;
using HarmonyLib;
#endif
using UnityEngine;
using Resources = UnityEngine.Resources;

namespace VideoExport
{
#if BEPINEX
    [BepInPlugin(GUID: _guid, Name: _name, Version: _versionNum)]
#if KOIKATSU
    [BepInProcess("CharaStudio")]
#elif AISHOUJO || HONEYSELECT2
    [BepInProcess("StudioNEOV2")]
#endif
#endif
    public class VideoExport : GenericPlugin
#if IPA
                               , IEnhancedPlugin
#endif
    {
        private const string _versionNum = "1.2.1";
        private const string _guid = "com.joan6694.illusionplugins.videoexport";
        private const string _name = "VideoExport";

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
        public bool isRecording { get { return this._isRecording; } }
        public int currentRecordingFrame { get { return this._currentRecordingFrame; } }
        public double currentRecordingTime { get { return this._currentRecordingTime; } }
        public int recordingFrameLimit { get { return this._recordingFrameLimit; } }
        #endregion

        #region Unity Methods
        protected override void Awake()
        {
            base.Awake();

            _pluginFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), _name);
            _outputFolder = Path.Combine(_pluginFolder, "Output");
            _globalFramesFolder = Path.Combine(_pluginFolder, "Frames");

            var harmony = HarmonyExtensions.CreateInstance(_guid);

            _configFile = new GenericConfig(_name, this);
            this._selectedPlugin = _configFile.AddInt("selectedScreenshotPlugin", 0, true);
            this._fps = _configFile.AddInt("framerate", 60, true);
            this._autoGenerateVideo = _configFile.AddBool("autoGenerateVideo", true, true);
            this._autoDeleteImages = _configFile.AddBool("autoDeleteImages", true, true);
            this._limitDuration = _configFile.AddBool("limitDuration", false, true);
            this._selectedLimitDuration = (LimitDurationType)_configFile.AddInt("selectedLimitDurationType", (int)LimitDurationType.Frames, true);
            this._limitDurationNumber = _configFile.AddFloat("limitDurationNumber", 0, true);
            this._selectedExtension = (ExtensionsType)_configFile.AddInt("selectedExtension", (int)ExtensionsType.MP4, true);
            this._resize = _configFile.AddBool("resize", false, true);
            this._resizeX = _configFile.AddInt("resizeX", Screen.width, true);
            this._resizeY = _configFile.AddInt("resizeY", Screen.height, true);
            this._selectedUpdateDynamicBones = (UpdateDynamicBonesType)_configFile.AddInt("selectedUpdateDynamicBonesMode", (int)UpdateDynamicBonesType.Default, true);
            this._prewarmLoopCount = _configFile.AddInt("prewarmLoopCount", 3, true);
            this._imagesPrefix = _configFile.AddString("imagesPrefix", "", true);
            this._imagesPostfix = _configFile.AddString("imagesPostfix", "", true);
            this._language = (Language)_configFile.AddInt("language", (int)Language.English, true);
            this.SetLanguage(this._language);
            string newOutputFolder = _configFile.AddString("outputFolder", _outputFolder, true, _currentDictionary.GetString(TranslationKey.VideosFolderDesc));
            if (Directory.Exists(newOutputFolder))
                _outputFolder = newOutputFolder;
            string newGlobalFramesFolder = _configFile.AddString("framesFolder", _globalFramesFolder, true, _currentDictionary.GetString(TranslationKey.FramesFolderDesc));
            if (Directory.Exists(newGlobalFramesFolder))
                _globalFramesFolder = newGlobalFramesFolder;

            this._extensions.Add(new MP4Extension());
            this._extensions.Add(new WEBMExtension());
            this._extensions.Add(new GIFExtension());
            this._extensions.Add(new AVIExtension());

            this._extensionsNames = Enum.GetNames(typeof(ExtensionsType));

            this._timelinePresent = TimelineCompatibility.Init();

            this.ExecuteDelayed(() =>
            {
#if HONEYSELECT
                this.AddScreenshotPlugin(new HoneyShot(), harmony);
                this.AddScreenshotPlugin(new PlayShot24ZHNeo(), harmony);
#endif
                this.AddScreenshotPlugin(new Screencap(), harmony);
                this.AddScreenshotPlugin(new Bitmap(), harmony);
                if (this._screenshotPlugins.Count == 0)
                    UnityEngine.Debug.LogError("VideoExport: No compatible screenshot plugin found, please install one.");
                this.SetLanguage(this._language);
            }, 5);
        }

        protected override void Update()
        {
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.E))
            {
                if (this._isRecording == false)
                    this.RecordVideo();
                else
                    this.StopRecording();
            }
            else if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.E))
            {
                _showUi = !_showUi;
                if (this._imguiBackground == null)
                    this._imguiBackground = IMGUIExtensions.CreateUGUIPanelForIMGUI();
            }
            if (this._startOnNextClick && Input.GetMouseButtonDown(0))
            {
                this.RecordVideo();
                this._startOnNextClick = false;
            }
            TreeNodeObject treeNode = Studio.Studio.Instance?.treeNodeCtrl.selectNode;
            this._currentAnimator = null;
            if (treeNode != null)
            {
                ObjectCtrlInfo info;
                if (Studio.Studio.Instance.dicInfo.TryGetValue(treeNode, out info))
                    this._currentAnimator = info.guideObject.transformTarget.GetComponentInChildren<Animator>();
            }

            if (this._currentAnimator == null && this._selectedLimitDuration == LimitDurationType.Animation)
                this._selectedLimitDuration = LimitDurationType.Seconds;
            if (this._timelinePresent == false && this._selectedLimitDuration == LimitDurationType.Timeline)
                this._selectedLimitDuration = LimitDurationType.Seconds;
        }

        protected override void LateUpdate()
        {
            if (this._currentAnimator != null)
            {
                AnimatorStateInfo stateInfo = this._currentAnimator.GetCurrentAnimatorStateInfo(0);
                this._animationIsPlaying = stateInfo.normalizedTime > this._lastAnimationNormalizedTime;
                if (stateInfo.loop == false)
                    this._animationIsPlaying = this._animationIsPlaying && stateInfo.normalizedTime < 1f;
                this._lastAnimationNormalizedTime = stateInfo.normalizedTime;
            }
            if (this._imguiBackground != null)
            {
                if (_showUi)
                {
                    this._imguiBackground.gameObject.SetActive(true);
                    IMGUIExtensions.FitRectTransformToRect(this._imguiBackground, this._windowRect);
                }
                else if (this._imguiBackground != null)
                    this._imguiBackground.gameObject.SetActive(false);
            }
            if (_showUi)
                this._windowRect.height = 10f;
        }

        protected override void OnGUI()
        {
            if (_showUi == false)
                return;
            this._windowRect = GUILayout.Window(_uniqueId, this._windowRect, this.Window, "Video Export " + _versionNum
#if BETA
                                                                                               + "b"
#endif
            );
            IMGUIExtensions.DrawBackground(this._windowRect);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _configFile.SetInt("language", (int)this._language);
            _configFile.SetInt("selectedScreenshotPlugin", this._selectedPlugin);
            _configFile.SetInt("framerate", this._fps);
            _configFile.SetBool("autoGenerateVideo", this._autoGenerateVideo);
            _configFile.SetBool("autoDeleteImages", this._autoDeleteImages);
            _configFile.SetBool("limitDuration", this._limitDuration);
            _configFile.SetInt("selectedLimitDurationType", (int)this._selectedLimitDuration);
            _configFile.SetFloat("limitDurationNumber", this._limitDurationNumber);
            _configFile.SetInt("selectedExtension", (int)this._selectedExtension);
            _configFile.SetBool("resize", this._resize);
            _configFile.SetInt("resizeX", this._resizeX);
            _configFile.SetInt("resizeY", this._resizeY);
            _configFile.SetInt("selectedUpdateDynamicBonesMode", (int)this._selectedUpdateDynamicBones);
            _configFile.SetInt("prewarmLoopCount", this._prewarmLoopCount);
            _configFile.SetString("imagesPrefix", this._imagesPrefix);
            _configFile.SetString("imagesPostfix", this._imagesPostfix);
            _configFile.SetInt("language", (int)this._language);
            _configFile.SetString("outputFolder", _outputFolder);
            _configFile.SetString("framesFolder", _globalFramesFolder);
            foreach (IScreenshotPlugin plugin in this._screenshotPlugins)
                plugin.SaveParams();
            foreach (IExtension extension in this._extensions)
                extension.SaveParams();
            _configFile.Save();
        }
        #endregion

        #region Public Methods
        public void RecordVideo()
        {
            if (this._isRecording == false)
                this.StartCoroutine(this.RecordVideo_Routine());
        }

        public void StopRecording()
        {
            this._breakRecording = true;
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
                    this._screenshotPlugins.Add(plugin);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(_name + ": Couldn't add screenshot plugin " + plugin + ".\n" + e);
            }

        }

        private void Window(int id)
        {
            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Language.English.ToString()))
                    this.SetLanguage(Language.English);
                if (GUILayout.Button(Language.中文.ToString()))
                    this.SetLanguage(Language.中文);
                GUILayout.EndHorizontal();

                GUI.enabled = this._isRecording == false;

                if (this._screenshotPlugins.Count > 1)
                {
                    GUILayout.Label(_currentDictionary.GetString(TranslationKey.ScreenshotPlugin));
                    this._selectedPlugin = GUILayout.SelectionGrid(this._selectedPlugin, this._screenshotPlugins.Select(p => p.name).ToArray(), Mathf.Clamp(this._screenshotPlugins.Count, 1, 3));
                }

                IScreenshotPlugin plugin = this._screenshotPlugins[this._selectedPlugin];
                plugin.DisplayParams();

                Vector2 currentSize = plugin.currentSize;
                GUILayout.Label($"{_currentDictionary.GetString(TranslationKey.CurrentSize)}: {currentSize.x:#}x{currentSize.y:#}");

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(_currentDictionary.GetString(TranslationKey.Framerate), GUILayout.ExpandWidth(false));
                    this._fps = Mathf.RoundToInt(GUILayout.HorizontalSlider(this._fps, 1, 120));
                    string s = GUILayout.TextField(this._fps.ToString(), GUILayout.Width(50));
                    int res;
                    if (int.TryParse(s, out res) == false || res < 1)
                        res = 1;
                    this._fps = res;
                }
                GUILayout.EndHorizontal();

                bool guiEnabled = GUI.enabled;
                GUI.enabled = true;

                GUILayout.BeginHorizontal();
                {
                    this._autoGenerateVideo = GUILayout.Toggle(this._autoGenerateVideo, _currentDictionary.GetString(TranslationKey.AutoGenerateVideo));
                    this._autoDeleteImages = GUILayout.Toggle(this._autoDeleteImages, _currentDictionary.GetString(TranslationKey.AutoDeleteImages));
                }
                GUILayout.EndHorizontal();

                GUI.enabled = guiEnabled;

                GUILayout.BeginHorizontal();
                {
                    this._limitDuration = GUILayout.Toggle(this._limitDuration, _currentDictionary.GetString(TranslationKey.LimitBy), GUILayout.ExpandWidth(false));
                    guiEnabled = GUI.enabled;
                    GUI.enabled = this._limitDuration && guiEnabled;
                    this._selectedLimitDuration = (LimitDurationType)GUILayout.SelectionGrid((int)this._selectedLimitDuration, this._limitDurationNames, 2);

                    GUI.enabled = guiEnabled;
                }
                GUILayout.EndHorizontal();

                {
                    guiEnabled = GUI.enabled;
                    GUI.enabled = this._limitDuration && guiEnabled;
                    switch (this._selectedLimitDuration)
                    {
                        case LimitDurationType.Frames:
                            {
                                GUILayout.BeginHorizontal();

                                {
                                    GUILayout.Label(_currentDictionary.GetString(TranslationKey.LimitByLimitCount), GUILayout.ExpandWidth(false));
                                    string s = GUILayout.TextField(this._limitDurationNumber.ToString("0"));
                                    int res;
                                    if (int.TryParse(s, out res) == false || res < 1)
                                        res = 1;
                                    this._limitDurationNumber = res;

                                }
                                GUILayout.EndHorizontal();

                                float totalSeconds = this._limitDurationNumber / this._fps;
                                GUILayout.Label($"{_currentDictionary.GetString(TranslationKey.Estimated)} {totalSeconds:0.0000} {_currentDictionary.GetString(TranslationKey.Seconds)}, {Mathf.RoundToInt(this._limitDurationNumber)} {_currentDictionary.GetString(TranslationKey.Frames)}");

                                break;
                            }
                        case LimitDurationType.Seconds:
                            {
                                GUILayout.BeginHorizontal();

                                {
                                    GUILayout.Label(_currentDictionary.GetString(TranslationKey.LimitByLimitCount), GUILayout.ExpandWidth(false));
                                    string s = GUILayout.TextField(this._limitDurationNumber.ToString("0.000"));
                                    float res;
                                    if (float.TryParse(s, out res) == false || res <= 0f)
                                        res = 0.001f;
                                    this._limitDurationNumber = res;
                                }
                                GUILayout.EndHorizontal();

                                float totalFrames = this._limitDurationNumber * this._fps;
                                GUILayout.Label($"{_currentDictionary.GetString(TranslationKey.Estimated)} {this._limitDurationNumber:0.0000} {_currentDictionary.GetString(TranslationKey.Seconds)}, {Mathf.RoundToInt(totalFrames)} {_currentDictionary.GetString(TranslationKey.Frames)} ({totalFrames:0.000})");

                                break;
                            }
                        case LimitDurationType.Animation:
                            {
                                GUILayout.BeginHorizontal();
                                {
                                    GUILayout.Label(_currentDictionary.GetString(TranslationKey.LimitByPrewarmLoopCount), GUILayout.ExpandWidth(false));
                                    string s = GUILayout.TextField(this._prewarmLoopCount.ToString());
                                    int res;
                                    if (int.TryParse(s, out res) == false || res < 0)
                                        res = 1;
                                    this._prewarmLoopCount = res;
                                }
                                GUILayout.EndHorizontal();

                                GUILayout.BeginHorizontal();
                                {
                                    GUILayout.Label(_currentDictionary.GetString(TranslationKey.LimitByLoopsToRecord), GUILayout.ExpandWidth(false));
                                    string s = GUILayout.TextField(this._limitDurationNumber.ToString("0.000"));
                                    float res;
                                    if (float.TryParse(s, out res) == false || res <= 0)
                                        res = 0.001f;
                                    this._limitDurationNumber = res;
                                }
                                GUILayout.EndHorizontal();

                                if (this._currentAnimator != null)
                                {
                                    AnimatorStateInfo info = this._currentAnimator.GetCurrentAnimatorStateInfo(0);
                                    float totalLength = info.length * this._limitDurationNumber;
                                    float totalFrames = totalLength * this._fps;
                                    GUILayout.Label($"{_currentDictionary.GetString(TranslationKey.Estimated)} {totalLength:0.0000} {_currentDictionary.GetString(TranslationKey.Seconds)}, {Mathf.RoundToInt(totalFrames)} {_currentDictionary.GetString(TranslationKey.Frames)} ({totalFrames:0.000})");
                                }
                                break;
                            }
                        case LimitDurationType.Timeline:
                            {
                                GUILayout.BeginHorizontal();
                                {
                                    GUILayout.Label(_currentDictionary.GetString(TranslationKey.LimitByPrewarmLoopCount), GUILayout.ExpandWidth(false));
                                    string s = GUILayout.TextField(this._prewarmLoopCount.ToString());
                                    int res;
                                    if (int.TryParse(s, out res) == false || res < 0)
                                        res = 1;
                                    this._prewarmLoopCount = res;
                                }
                                GUILayout.EndHorizontal();

                                GUILayout.BeginHorizontal();
                                {
                                    GUILayout.Label(_currentDictionary.GetString(TranslationKey.LimitByLoopsToRecord), GUILayout.ExpandWidth(false));
                                    string s = GUILayout.TextField(this._limitDurationNumber.ToString("0.000"));
                                    float res;
                                    if (float.TryParse(s, out res) == false || res <= 0)
                                        res = 0.001f;
                                    this._limitDurationNumber = res;
                                }
                                GUILayout.EndHorizontal();

                                if (this._timelinePresent)
                                {
                                    float totalLength = TimelineCompatibility.GetDuration() * this._limitDurationNumber;
                                    float totalFrames = totalLength * this._fps;
                                    GUILayout.Label($"{_currentDictionary.GetString(TranslationKey.Estimated)} {totalLength:0.0000} {_currentDictionary.GetString(TranslationKey.Seconds)}, {Mathf.RoundToInt(totalFrames)} {_currentDictionary.GetString(TranslationKey.Frames)} ({totalFrames:0.000})");
                                }
                                break;
                            }
                    }

                    GUI.enabled = guiEnabled;
                }

                IExtension extension = this._extensions[(int)this._selectedExtension];

                if (this._autoGenerateVideo)
                {
                    GUILayout.BeginHorizontal();
                    {
                        this._resize = GUILayout.Toggle(this._resize, _currentDictionary.GetString(TranslationKey.Resize), GUILayout.ExpandWidth(false));
                        guiEnabled = GUI.enabled;
                        GUI.enabled = this._resize && guiEnabled;

                        GUILayout.FlexibleSpace();

                        string s = GUILayout.TextField(this._resizeX.ToString(), GUILayout.Width(50));
                        int res;
                        if (int.TryParse(s, out res) == false || res < 1)
                            res = 1;
                        this._resizeX = res;

                        s = GUILayout.TextField(this._resizeY.ToString(), GUILayout.Width(50));
                        if (int.TryParse(s, out res) == false || res < 1)
                            res = 1;
                        this._resizeY = res;

                        if (GUILayout.Button(_currentDictionary.GetString(TranslationKey.ResizeDefault), GUILayout.ExpandWidth(false)))
                        {
                            this._resizeX = Screen.width;
                            this._resizeY = Screen.height;
                        }
                        if (this._resizeX > currentSize.x)
                            this._resizeX = (int)currentSize.x;
                        if (this._resizeY > currentSize.y)
                            this._resizeY = (int)currentSize.y;

                        GUI.enabled = guiEnabled;
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label(_currentDictionary.GetString(TranslationKey.Format), GUILayout.ExpandWidth(false));
                        this._selectedExtension = (ExtensionsType)GUILayout.SelectionGrid((int)this._selectedExtension, this._extensionsNames, 4);
                    }
                    GUILayout.EndHorizontal();

                    extension.DisplayParams();
                }

                GUILayout.Label(_currentDictionary.GetString(TranslationKey.DBUpdateMode));
                this._selectedUpdateDynamicBones = (UpdateDynamicBonesType)GUILayout.SelectionGrid((int)this._selectedUpdateDynamicBones, this._updateDynamicBonesTypeNames, 2);

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(_currentDictionary.GetString(TranslationKey.Prefix), GUILayout.ExpandWidth(false));
                    string s = GUILayout.TextField(this._imagesPrefix);
                    if (s != this._imagesPrefix)
                    {
                        StringBuilder builder = new StringBuilder(s);
                        foreach (char chr in Path.GetInvalidFileNameChars())
                            builder.Replace(chr.ToString(), "");
                        foreach (char chr in Path.GetInvalidPathChars())
                            builder.Replace(chr.ToString(), "");
                        this._imagesPrefix = builder.ToString();
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(_currentDictionary.GetString(TranslationKey.Suffix), GUILayout.ExpandWidth(false));
                    string s = GUILayout.TextField(this._imagesPostfix);
                    if (s != this._imagesPostfix)
                    {
                        StringBuilder builder = new StringBuilder(s);
                        foreach (char chr in Path.GetInvalidFileNameChars())
                            builder.Replace(chr.ToString(), "");
                        foreach (char chr in Path.GetInvalidPathChars())
                            builder.Replace(chr.ToString(), "");
                        this._imagesPostfix = builder.ToString();
                    }
                }
                GUILayout.EndHorizontal();

                string actualExtension = plugin.extension;

                GUILayout.Label($"{_currentDictionary.GetString(TranslationKey.ExampleResult)}: {this._imagesPrefix}123{this._imagesPostfix}.{actualExtension}");

                guiEnabled = GUI.enabled;
                GUI.enabled = true;
                Color c = GUI.color;
                GUI.color = Color.red;

                this._clearSceneBeforeEncoding = GUILayout.Toggle(this._clearSceneBeforeEncoding, _currentDictionary.GetString(TranslationKey.EmptyScene));
                this._closeWhenDone = GUILayout.Toggle(this._closeWhenDone, _currentDictionary.GetString(TranslationKey.CloseStudio));

                GUI.color = c;
                GUI.enabled = guiEnabled;

                this._startOnNextClick = GUILayout.Toggle(this._startOnNextClick, _currentDictionary.GetString(TranslationKey.StartRecordingOnNextClick));

                GUI.enabled = this._generatingVideo == false && this._startOnNextClick == false &&
                              (this._limitDuration == false || this._selectedLimitDuration != LimitDurationType.Animation || (this._currentAnimator.speed > 0.001f && this._animationIsPlaying));

                GUILayout.BeginHorizontal();
                {
                    string reason;
                    if (this._autoGenerateVideo == false || extension.IsCompatibleWithPlugin(plugin, out reason))
                    {
                        if (this._isRecording == false)
                        {
                            if (GUILayout.Button(_currentDictionary.GetString(TranslationKey.StartRecording)))
                                this.RecordVideo();
                        }
                        else
                        {
                            if (GUILayout.Button(_currentDictionary.GetString(TranslationKey.StopRecording)))
                                this.StopRecording();
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
                GUI.color = this._messageColor;

                GUIStyle customLabel = GUI.skin.label;
                TextAnchor cachedAlignment = customLabel.alignment;
                customLabel.alignment = TextAnchor.UpperCenter;
                GUILayout.Label(this._currentMessage);
                customLabel.alignment = cachedAlignment;

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Box("", _customBoxStyle, GUILayout.Width((this._windowRect.width - 20) * this._progressBarPercentage), GUILayout.Height(10));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUI.color = c;
            }
            GUILayout.EndHorizontal();
            GUI.DragWindow();

        }

        private void SetLanguage(Language language)
        {
            this._language = language;
            switch (this._language)
            {
                case Language.English:
                    _currentDictionary = _englishDictionary;
                    break;
                case Language.中文:
                    _currentDictionary = _chineseDictionary;
                    break;
            }

            this._limitDurationNames = new[]
            {
                _currentDictionary.GetString(TranslationKey.LimitByFrames),
                _currentDictionary.GetString(TranslationKey.LimitBySeconds),
                _currentDictionary.GetString(TranslationKey.LimitByAnimation),
                _currentDictionary.GetString(TranslationKey.LimitByTimeline)
            };

            this._updateDynamicBonesTypeNames = new[]
            {
                _currentDictionary.GetString(TranslationKey.DBDefault),
                _currentDictionary.GetString(TranslationKey.DBEveryFrame)
            };

            foreach (IScreenshotPlugin plugin in this._screenshotPlugins)
                plugin.UpdateLanguage();
            foreach (IExtension extension in this._extensions)
                extension.UpdateLanguage();
        }

        private IEnumerator RecordVideo_Routine()
        {
            this._isRecording = true;
            this._messageColor = Color.white;

            Animator currentAnimator = this._currentAnimator;

            IScreenshotPlugin screenshotPlugin = this._screenshotPlugins[this._selectedPlugin];

            string tempName = DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ss");
            string framesFolder = Path.Combine(_globalFramesFolder, tempName);

            if (Directory.Exists(framesFolder) == false)
                Directory.CreateDirectory(framesFolder);

            int cachedCaptureFramerate = Time.captureFramerate;
            Time.captureFramerate = this._fps;

            if (this._selectedUpdateDynamicBones == UpdateDynamicBonesType.EveryFrame)
            {
                foreach (DynamicBone dynamicBone in Resources.FindObjectsOfTypeAll<DynamicBone>())
                    dynamicBone.m_UpdateRate = -1;
                foreach (DynamicBone_Ver02 dynamicBone in Resources.FindObjectsOfTypeAll<DynamicBone_Ver02>())
                    dynamicBone.UpdateRate = -1;
            }

            this._currentRecordingFrame = 0;
            this._currentRecordingTime = 0;
            screenshotPlugin.OnStartRecording();

            if (this._limitDuration)
            {
                if (this._prewarmLoopCount > 0)
                {
                    switch (this._selectedLimitDuration)
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
                                if (j == this._prewarmLoopCount)
                                {
                                    if (!info.loop)
                                        yield return new WaitForEndOfFrame();
                                    break;
                                }
                                yield return new WaitForEndOfFrame();
                                this._currentMessage = $"{_currentDictionary.GetString(TranslationKey.PrewarmingAnimation)} {j + 1}/{this._prewarmLoopCount}";
                                this._progressBarPercentage = lastNormalizedTime;
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
                                if (j == this._prewarmLoopCount)
                                    break;
                                yield return new WaitForEndOfFrame();
                                this._currentMessage = $"{_currentDictionary.GetString(TranslationKey.PrewarmingAnimation)} {j + 1}/{this._prewarmLoopCount}";
                                this._progressBarPercentage = lastTime / duration;
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
            this._progressBarPercentage = 0f;

            int limit = 1;
            if (this._limitDuration)
            {
                switch (this._selectedLimitDuration)
                {
                    case LimitDurationType.Frames:
                        limit = Mathf.RoundToInt(this._limitDurationNumber);
                        break;
                    case LimitDurationType.Seconds:
                        limit = Mathf.RoundToInt(this._limitDurationNumber * this._fps);
                        break;
                    case LimitDurationType.Animation:
                        limit = Mathf.RoundToInt(currentAnimator.GetCurrentAnimatorStateInfo(0).length * this._limitDurationNumber * this._fps);
                        break;
                    case LimitDurationType.Timeline:
                        limit = Mathf.RoundToInt(TimelineCompatibility.GetDuration() * this._limitDurationNumber * this._fps);
                        break;
                }
                this._recordingFrameLimit = limit;
            }
            else
            {
                this._recordingFrameLimit = -1;
            }
            TimeSpan elapsed = TimeSpan.Zero;
            int i = 0;
            string imageExtension = screenshotPlugin.extension;
            for (; ; i++)
            {
                if (this._limitDuration && i >= limit)
                    this.StopRecording();
                if (this._breakRecording)
                {
                    this._breakRecording = false;
                    break;
                }

                if (i % 5 == 0)
                {
                    Resources.UnloadUnusedAssets();
                    GC.Collect();
                }
                string savePath = Path.Combine(framesFolder, $"{this._imagesPrefix}{i}{this._imagesPostfix}.{imageExtension}");
                byte[] frame = screenshotPlugin.Capture(savePath);
                if (frame != null)
                    File.WriteAllBytes(savePath, frame);

                elapsed = DateTime.Now - startTime;

                TimeSpan remaining = TimeSpan.FromSeconds((limit - i - 1) * elapsed.TotalSeconds / (i + 1));

                if (this._limitDuration)
                    this._progressBarPercentage = (i + 1f) / limit;
                else
                    this._progressBarPercentage = (i % this._fps) / (float)this._fps;

                this._currentMessage = $"{_currentDictionary.GetString(TranslationKey.TakingScreenshot)} {i + 1}{(this._limitDuration ? $"/{limit} {this._progressBarPercentage * 100:0.0}%\n{_currentDictionary.GetString(TranslationKey.ETA)}: {remaining.Hours:0}:{remaining.Minutes:00}:{remaining.Seconds:00} {_currentDictionary.GetString(TranslationKey.Elapsed)}: {elapsed.Hours:0}:{elapsed.Minutes:00}:{elapsed.Seconds:00}" : "")}";

                this._currentRecordingFrame = i + 1;
                this._currentRecordingTime = (i + 1) / (double)this._fps;
                yield return new WaitForEndOfFrame();
            }
            screenshotPlugin.OnEndRecording();
            Time.captureFramerate = cachedCaptureFramerate;

            UnityEngine.Debug.Log($"Time spent taking screenshots: {elapsed.Hours:0}:{elapsed.Minutes:00}:{elapsed.Seconds:00}");

            foreach (DynamicBone dynamicBone in Resources.FindObjectsOfTypeAll<DynamicBone>())
                dynamicBone.m_UpdateRate = 60;
            foreach (DynamicBone_Ver02 dynamicBone in Resources.FindObjectsOfTypeAll<DynamicBone_Ver02>())
                dynamicBone.UpdateRate = 60;

            bool error = false;
            if (this._autoGenerateVideo && i != 0)
            {
                this._generatingVideo = true;

                if (this._clearSceneBeforeEncoding)
                    Studio.Studio.Instance.InitScene(false);

                this._messageColor = Color.yellow;
                if (Directory.Exists(_outputFolder) == false)
                    Directory.CreateDirectory(_outputFolder);
                this._currentMessage = _currentDictionary.GetString(TranslationKey.GeneratingVideo);
                yield return null;
                IExtension extension = this._extensions[(int)this._selectedExtension];

                string arguments = extension.GetArguments(this.SimplifyPath(framesFolder), this._imagesPrefix, this._imagesPostfix, imageExtension, screenshotPlugin.bitDepth, this._fps, screenshotPlugin.transparency, this._resize, this._resizeX, this._resizeY, this.SimplifyPath(Path.Combine(_outputFolder, tempName)));
                startTime = DateTime.Now;
                Process proc = this.StartExternalProcess(extension.GetExecutable(), arguments, extension.canProcessStandardOutput, extension.canProcessStandardError);
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
                        this._progressBarPercentage = extension.progress / (float)i;
                        this._currentMessage = $"{_currentDictionary.GetString(TranslationKey.GeneratingVideo)} {extension.progress}/{i} {this._progressBarPercentage * 100:0.0}%\n{_currentDictionary.GetString(TranslationKey.ETA)}: {eta.Hours:0}:{eta.Minutes:00}:{eta.Seconds:00} {_currentDictionary.GetString(TranslationKey.Elapsed)}: {elapsed.Hours:0}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
                    }
                    else
                        this._progressBarPercentage = (float)((elapsed.TotalSeconds % 6) / 6);

                    Resources.UnloadUnusedAssets();
                    GC.Collect();
                    yield return null;
                    proc.Refresh();
                }

                proc.WaitForExit();
                UnityEngine.Debug.LogError(proc.StandardError.ReadToEnd());

                yield return null;
                if (proc.ExitCode == 0)
                {
                    this._messageColor = Color.green;
                    this._currentMessage = _currentDictionary.GetString(TranslationKey.Done);
                }
                else
                {
                    this._messageColor = Color.red;
                    this._currentMessage = _currentDictionary.GetString(TranslationKey.GeneratingError);
                    error = true;
                }
                proc.Close();
                this._generatingVideo = false;
                UnityEngine.Debug.Log($"Time spent generating video: {elapsed.Hours:0}:{elapsed.Minutes:00}:{elapsed.Seconds:00}");
            }
            else
            {
                this._messageColor = Color.green;
                this._currentMessage = _currentDictionary.GetString(TranslationKey.Done);
            }
            this._progressBarPercentage = 1;

            if (this._autoDeleteImages && error == false)
                Directory.Delete(framesFolder, true);
            this._isRecording = false;
            Resources.UnloadUnusedAssets();
            GC.Collect();

            if (this._closeWhenDone)
#if HONEYSELECT2
                Illusion.Game.Utils.Scene.GameEnd(false);
#else
                Manager.Scene.Instance.GameEnd(false);
#endif
        }

        private Process StartExternalProcess(string exe, string arguments, bool redirectStandardOutput, bool redirectStandardError)
        {
            UnityEngine.Debug.Log($"{exe} {arguments}");
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
