using Studio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using BepInEx.Logging;
using KKAPI.Studio.UI.Toolbars;
using ToolBox;
using ToolBox.Extensions;
using UILib;
using UILib.ContextMenu;
using UILib.EventHandlers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Type = System.Type;
#if IPA
using Harmony;
using IllusionPlugin;
#elif BEPINEX
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
#endif
#if KOIKATSU || SUNSHINE
using Expression = ExpressionBone;
using ExtensibleSaveFormat;
using Sideloader.AutoResolver;
using KKAPI.Studio.UI;
#elif AISHOUJO || HONEYSELECT2
using CharaUtils;
using ExtensibleSaveFormat;
#endif

namespace Timeline
{
#if BEPINEX
    [BepInPlugin(GUID, Name, Version)]
    // ShalltyUtils is part of Timeline now, and the old one only patched the Timeline window that is gone.
    [BepInIncompatibility("com.shallty.shalltyutils")]
#if KOIKATSU || SUNSHINE
    [BepInProcess("CharaStudio")]
    [BepInDependency(Sideloader.Sideloader.GUID, Sideloader.Sideloader.Version)]
#elif AISHOUJO || HONEYSELECT2
    [BepInProcess("StudioNEOV2")]
#endif
    [BepInDependency(KKAPI.KoikatuAPI.GUID, KKAPI.KoikatuAPI.VersionConst)]
#endif
    public partial class Timeline : GenericPlugin
#if IPA
                            , IEnhancedPlugin
#endif
    {
        #region Constants
        public const string Name = "Timeline";
        public const string Version = "2.0.0";
        /// <summary>
        /// Still the original's, deliberately, and it has to stay that way.
        ///
        /// This is a continuation of that plugin, meant to take its place in an install rather than sit
        /// beside it, and other plugins reach for it by this exact string. NodesConstraints orders its
        /// own patch on Expression.LateUpdate with HarmonyAfter("com.joan6694.illusionplugins.timeline"),
        /// which Harmony matches against the id Timeline creates its instance with, which is this. Change
        /// it and that ordering silently goes back to arbitrary, on the very method the after pass of the
        /// interpolation runs from: nothing errors, constraints just start resolving against the pose
        /// from before Timeline wrote it.
        ///
        /// The BepInEx config file is named after it as well, so changing it also hands everyone a fresh
        /// set of settings, which is how the old interface came back the one time this was tried.
        /// </summary>
        public const string GUID = "com.joan6694.illusionplugins.timeline";
        /// <summary>
        /// What interpolables are filed under, which is written into every scene. It stays "Timeline"
        /// whatever the plugin is called, or every existing scene would lose its tracks.
        /// </summary>
        internal const string _ownerId = "Timeline";
#if KOIKATSU || AISHOUJO || HONEYSELECT2
        private const int _saveVersion = 0;
        private const string _extSaveKey = "timeline";
#endif
        #endregion

#if IPA
        public override string Name { get { return _name; } }
        public override string Version { get { return _version; } }
        public override string[] Filter { get { return new[] { "StudioNEO_32", "StudioNEO_64" }; } }
#endif

        #region Private Types

        private class InterpolableGroup
        {
            public string name;
            public bool expanded = true;
        }

        #endregion

        #region Private Variables

        internal static new ManualLogSource Logger;
        internal static Timeline _self;
        private static string _assemblyLocation;
        private static string _singleFilesFolder;
        private static bool _refreshInterpolablesListScheduled = false;
        private bool _loaded = false;
        private static TimelineButton _toolbarButton;

        private int _totalActiveExpressions = 0;
        private int _currentExpressionIndex = 0;
        private readonly HashSet<Expression> _allExpressions = new HashSet<Expression>();
        internal List<InterpolableModel> _interpolableModelsList = new List<InterpolableModel>();
        internal Dictionary<string, List<InterpolableModel>> _interpolableModelsDictionary = new Dictionary<string, List<InterpolableModel>>();
        private readonly Dictionary<string, int> _hardCodedOwnerOrder = new Dictionary<string, int>()
        {
            {_ownerId, 0},
            {"HSPE", 1},
            {"KKPE", 1},
            {"RendererEditor", 2},
            {"NodesConstraints", 3}
        };
        internal Dictionary<Transform, GuideObject> _allGuideObjects;
        internal HashSet<GuideObject> _selectedGuideObjects;
        private readonly List<Interpolable> _toDelete = new List<Interpolable>();
        private readonly Dictionary<int, Interpolable> _interpolables = new Dictionary<int, Interpolable>();
        /// <summary>
        /// Transform tracks that can be split per axis, both directions, keyed by owner and id because
        /// nothing stops two plugins from picking the same interpolable id.
        /// </summary>
        private static readonly Dictionary<string, string[]> _splitsOfCombined = new Dictionary<string, string[]>();
        private static readonly Dictionary<string, string> _combinedOfSplit = new Dictionary<string, string>();
        private readonly Tree<Interpolable, InterpolableGroup> _interpolablesTree = new Tree<Interpolable, InterpolableGroup>();

        private readonly AnimationCurve _linePreset = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        private readonly AnimationCurve _hermitePreset = new AnimationCurve(new UnityEngine.Keyframe(0f, 0f, 0f, 0f), new UnityEngine.Keyframe(1f, 1f, 0f, 0f));
        private readonly AnimationCurve _stairsPreset = new AnimationCurve(new UnityEngine.Keyframe(0f, 0f, 0f, 0f), new UnityEngine.Keyframe(1f, 1f, float.PositiveInfinity, 0f));

        private bool _isPlaying;
        private float _startTime;
        private float _playbackTime;
        private float _duration = 10f;
        private float _blockLength = 10f;
        private int _divisions = 10;
        private int _desiredFrameRate = 60;
        private readonly List<Interpolable> _selectedInterpolables = new List<Interpolable>();
        private readonly List<KeyValuePair<float, Keyframe>> _selectedKeyframes = new List<KeyValuePair<float, Keyframe>>();
        /// <summary>The selection as a set, rebuilt when it changes, for the per marker lookups.</summary>
        private readonly HashSet<Keyframe> _selectedKeyframeSet = new HashSet<Keyframe>();
        private readonly List<KeyValuePair<float, Keyframe>> _copiedKeyframes = new List<KeyValuePair<float, Keyframe>>();
        private ObjectCtrlInfo _selectedOCI;
        private GuideObject _selectedGuideObject;

        #endregion

        #region Accessors
        public static float playbackTime { get { return _self._playbackTime; } }
        public static float duration { get { return _self._duration; } }
        public static bool isPlaying
        {
            get { return _self._isPlaying; }
            set
            {
                if (_self._isPlaying != value)
                {
                    _self._isPlaying = value;
                    _toolbarButton?.UpdateButton();
                }
            }
        }
        #endregion

        internal static ConfigEntry<KeyboardShortcut> ConfigMainWindowShortcut { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> ConfigPlayPauseShortcut { get; private set; }
        internal static ConfigEntry<Autoplay> ConfigAutoplay { get; private set; }
        internal static ConfigEntry<bool> ConfigSyncSelection { get; private set; }
        internal static ConfigEntry<float> ConfigUIScale { get; private set; }
        internal static ConfigEntry<Color> ConfigBackgroundColor { get; private set; }
        internal static ConfigEntry<Color> ConfigAccentColor { get; private set; }
        internal static ConfigEntry<Color> ConfigTextColor { get; private set; }
        internal static ConfigEntry<Color> ConfigPlayheadColor { get; private set; }
        internal static ConfigEntry<float> ConfigUIOpacity { get; private set; }


        internal enum Autoplay
        {
            Ignore,
            Yes,
            No
        }


        #region Unity Methods
        protected override void Awake()
        {
            base.Awake();

            ConfigMainWindowShortcut = Config.Bind("Config", "Open Timeline UI", new KeyboardShortcut(KeyCode.T, KeyCode.LeftControl));
            ConfigPlayPauseShortcut = Config.Bind("Config", "Play or Pause Timeline", new KeyboardShortcut(KeyCode.T, KeyCode.LeftShift));
            ConfigAutoplay = Config.Bind("Config", "Autoplay", Autoplay.Ignore);
            ConfigSyncSelection = Config.Bind("Config", "Sync Selection", true, "Keeps the interpolable list and the Studio selection pointing at the same thing: clicking a track selects the IK or FK node it drives, and clicking a node in the viewport scrolls to and highlights its tracks. Holding left alt does both regardless of this setting.");
            ConfigUIScale = Config.Bind("Config", "Interface Scale", 1f, new ConfigDescription("Scales the whole Timeline interface. Applies immediately. Only affects the Generated interface.", new AcceptableValueRange<float>(0.5f, 2f)));
            ConfigViewState = Config.Bind("Config", "Window Layout", "", "Where the Timeline window was, its size and how it was set up. Written by the window itself; empty it to start over.");


            // BepInEx has no converter for Color, so the settings screen would refuse to bind one.
            // Registering it here is what turns these four entries into real colour pickers.
            if (TomlTypeConverter.CanConvert(typeof(Color)) == false)
            {
                TomlTypeConverter.AddConverter(typeof(Color), new BepInEx.Configuration.TypeConverter
                {
                    ConvertToString = (obj, type) => "#" + ColorUtility.ToHtmlStringRGBA((Color)obj),
                    ConvertToObject = (str, type) =>
                    {
                        Color parsed;
                        return ColorUtility.TryParseHtmlString(str, out parsed) ? parsed : Color.white;
                    }
                });
            }

            const string themeSection = "Interface Theme";
            ConfigBackgroundColor = Config.Bind(themeSection, "Background", _defaultBackground, "Base colour of the interface. Every panel, field and grid shade is derived from it. The Theme window, from the palette on the top row, edits this too.");
            ConfigAccentColor = Config.Bind(themeSection, "Accent", _defaultAccent, "Highlight colour: hovered controls, the selected row outline, the interpolation curve.");
            ConfigTextColor = Config.Bind(themeSection, "Text", _defaultText, "Main text colour. Dimmed and muted variants are derived from it.");
            ConfigPlayheadColor = Config.Bind(themeSection, "Playhead", _defaultPlayhead, "Colour of the playback cursor.");
            ConfigUIOpacity = Config.Bind(themeSection, "Opacity", 1f, new ConfigDescription("Transparency of the whole interface. Applies immediately, and is also what control plus scrollwheel on the title bar changes.", new AcceptableValueRange<float>(0.1f, 1f)));
            // A preset sets all four at once, and re-themes once afterwards rather than after each.
            ConfigBackgroundColor.SettingChanged += (sender, args) => { if (_themeBatch == false) ApplyTimelineTheme(); };
            ConfigAccentColor.SettingChanged += (sender, args) => { if (_themeBatch == false) ApplyTimelineTheme(); };
            ConfigTextColor.SettingChanged += (sender, args) => { if (_themeBatch == false) ApplyTimelineTheme(); };
            ConfigPlayheadColor.SettingChanged += (sender, args) => { if (_themeBatch == false) ApplyTimelineTheme(); };
            BindThemeConfig(Config, themeSection);
            BindKeyframeColor(themeSection);
            BindShalltyConfig(Config);

            _self = this;
            Logger = MakeRelayLog(base.Logger);

            _assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _singleFilesFolder = Path.Combine(_assemblyLocation, Path.Combine(Name, "Single Files"));

#if HONEYSELECT
            HSExtSave.HSExtSave.RegisterHandler("timeline", null, null, this.SceneLoad, this.SceneImport, this.SceneWrite, null, null);
#else
            ExtensibleSaveFormat.ExtendedSave.SceneBeingLoaded += OnSceneLoad;
            ExtensibleSaveFormat.ExtendedSave.SceneBeingImported += OnSceneImport;
            ExtensibleSaveFormat.ExtendedSave.SceneBeingSaved += OnSceneSave;
#endif
            var harmonyInstance = HarmonyExtensions.CreateInstance(GUID);
            harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
            OCI_OnDelete_Patches.ManualPatch(harmonyInstance);
        }

#if HONEYSELECT
        protected override void LevelLoaded(int level)
        {
            if (level == 3)
                this.Init();
        }
#elif SUNSHINE || HONEYSELECT2 || AISHOUJO
        protected override void LevelLoaded(Scene scene, LoadSceneMode mode)
        {
            base.LevelLoaded(scene, mode);
            if (mode == LoadSceneMode.Single && scene.buildIndex == 2)
                Init();
        }

#elif KOIKATSU
        protected override void LevelLoaded(Scene scene, LoadSceneMode mode)
        {
            base.LevelLoaded(scene, mode);
            if (mode == LoadSceneMode.Single && scene.buildIndex == 1)
                Init();
        }
#endif

        protected override void Update()
        {
            if (_loaded == false)
                return;

            TickView();
            TickShallty();

            if (ConfigMainWindowShortcut.Value.IsDown())
                ToggleUiVisible();
            if (ConfigPlayPauseShortcut.Value.IsDown())
            {
                if (_isPlaying)
                    Pause();
                else
                    Play();
            }

            _totalActiveExpressions = _allExpressions.Count(e => e.enabled && e.gameObject.activeInHierarchy);
            _currentExpressionIndex = 0;

            // A node selected on an object that is not the one selected in the workspace does not switch
            // the selected object by itself, so it is followed up to the object it belongs to.
            GuideObject guideObject = _selectedGuideObjects.FirstOrDefault();
            if (_selectedGuideObject != guideObject)
            {
                ObjectCtrlInfo objectCtrlInfo = null;
                _selectedGuideObject = guideObject;
                while (guideObject != null)
                {
                    ObjectCtrlInfo newOCI = Studio.Studio.Instance.dicObjectCtrl.FirstOrDefault(p => p.Value.guideObject == guideObject).Value;
                    if (newOCI != null)
                    {
                        objectCtrlInfo = newOCI;
                        break;
                    }
                    guideObject = guideObject.parentGuide;
                }

                if (_selectedOCI != objectCtrlInfo)
                {
                    _selectedOCI = objectCtrlInfo;
                    UpdateInterpolablesView();
                }
            }

            if (_toDelete.Count != 0)
            {
                try
                {
                    RemoveInterpolables(_toDelete);
                }
                catch (Exception ex)
                {
                    Logger.LogError("Failed to Remove Interpolables from toDelete list: " + ex);
                }
                _toDelete.Clear();
            }

            InterpolateBefore();
        }

        /// <summary>Whether the window is showing.</summary>
        private bool UiVisible
        {
            get { return _view != null && _view.visible; }
        }

        private void ToggleUiVisible()
        {
            if (_view == null)
                return;
            _view.visible = !_view.visible;
            _toolbarButton?.UpdateButton();
        }

        protected override void LateUpdate()
        {
            if (_loaded == false)
                return;
            // Also sampled here, and not only next to the interpolation, so that auto keying still works
            // if that pass is skipped. Sampling twice costs one comparison, it does not record twice.
            RecordSample();
        }

        private void PostLateUpdate()
        {
            // Between the user's turn and the timeline's: whatever moved this frame moved because someone
            // moved it.
            RecordSample();
            InterpolateAfter();
        }
        #endregion

        #region Public Methods
        /// <summary>The Timeline window, or null before it is built.</summary>
        public static RectTransform MainWindowRectTransform => _self == null || _self._view == null ? null : _self._view.window;

        /// <summary>
        /// Start playback or pause it if it's already playing.
        /// </summary>
        public static void Play()
        {
            if (isPlaying == false)
            {
                isPlaying = true;
                _self._startTime = Time.time - _self._playbackTime;
            }
            else
                Pause();
        }

        /// <summary>
        /// Pause playback.
        /// </summary>
        public static void Pause()
        {
            isPlaying = false;
        }

        /// <summary>
        /// Stop playback and move cursor to the beginning.
        /// </summary>
        public static void Stop()
        {
            _self._playbackTime = 0f;
            _self.Interpolate(true);
            _self.Interpolate(false);
            isPlaying = false;
        }
        
        /// <summary>
        /// Move playback cursor to the previous frame (based on desired framerate).
        /// </summary>
        public static void PreviousFrame()
        {
            float beat = 1f / _self._desiredFrameRate;
            float time = _self._playbackTime % _self._duration;
            float mod = time % beat;
            if (mod / beat < 0.5f)
                time -= mod;
            else
                time += beat - mod;
            time -= beat;
            if (time < 0f)
                time = 0f;
            _self.SeekPlaybackTime(time);
        }

        /// <summary>
        /// Move playback cursor to the next frame (based on desired framerate).
        /// </summary>
        public static void NextFrame()
        {
            float beat = 1f / _self._desiredFrameRate;
            float time = _self._playbackTime % _self._duration;
            float mod = time % beat;
            if (mod / beat < 0.5f)
                time -= mod;
            else
                time += beat - mod;
            time += beat;
            if (time > _self._duration)
                time = _self._duration;
            _self.SeekPlaybackTime(time);
        }

        /// <summary>
        /// Move playback cursor to the specified time (in seconds).
        /// </summary>
        public static void Seek(float t)
        {
            _self.SeekPlaybackTime(t);
        }


        /// <summary>
        /// Adds an InterpolableModel to the list.
        /// </summary>
        /// <param name="model"></param>
        public static void AddInterpolableModel(InterpolableModel model)
        {
            List<InterpolableModel> models;
            if (_self._interpolableModelsDictionary.TryGetValue(model.owner, out models) == false)
            {
                models = new List<InterpolableModel>();
                _self._interpolableModelsDictionary.Add(model.owner, models);
            }
            models.Add(model);
            _self._interpolableModelsList.Add(model);
        }

        /// <summary>
        /// Adds an InterpolableModel to the list with a constant parameter
        /// </summary>
        public static void AddInterpolableModelStatic(string owner,
                                                      string id,
                                                      object parameter,
                                                      string name,
                                                      InterpolableDelegate interpolateBefore,
                                                      InterpolableDelegate interpolateAfter,
                                                      Func<ObjectCtrlInfo, bool> isCompatibleWithTarget,
                                                      Func<ObjectCtrlInfo, object, object> getValue,
                                                      Func<object, XmlNode, object> readValueFromXml,
                                                      Action<object, XmlTextWriter, object> writeValueToXml,
                                                      Func<ObjectCtrlInfo, XmlNode, object> readParameterFromXml = null,
                                                      Action<ObjectCtrlInfo, XmlTextWriter, object> writeParameterToXml = null,
                                                      Func<ObjectCtrlInfo, object, object, object, bool> checkIntegrity = null,
                                                      bool useOciInHash = true,
                                                      Func<string, ObjectCtrlInfo, object, string> getFinalName = null,
                                                      Func<ObjectCtrlInfo, object, bool> shouldShow = null)
        {
            AddInterpolableModel(new InterpolableModel(owner, id, parameter, name, interpolateBefore, interpolateAfter, isCompatibleWithTarget, getValue, readValueFromXml, writeValueToXml, readParameterFromXml, writeParameterToXml, checkIntegrity, useOciInHash, getFinalName, shouldShow));
        }

        /// <summary>
        /// Adds an interpolableModel to the list with a dynamic parameter
        /// </summary>
        public static void AddInterpolableModelDynamic(string owner,
                                                       string id,
                                                       string name,
                                                       InterpolableDelegate interpolateBefore,
                                                       InterpolableDelegate interpolateAfter,
                                                       Func<ObjectCtrlInfo, bool> isCompatibleWithTarget,
                                                       Func<ObjectCtrlInfo, object, object> getValue,
                                                       Func<object, XmlNode, object> readValueFromXml,
                                                       Action<object, XmlTextWriter, object> writeValueToXml,
                                                       Func<ObjectCtrlInfo, object> getParameter,
                                                       Func<ObjectCtrlInfo, XmlNode, object> readParameterFromXml = null,
                                                       Action<ObjectCtrlInfo, XmlTextWriter, object> writeParameterToXml = null,
                                                       Func<ObjectCtrlInfo, object, object, object, bool> checkIntegrity = null,
                                                       bool useOciInHash = true,
                                                       Func<string, ObjectCtrlInfo, object, string> getFinalName = null,
                                                       Func<ObjectCtrlInfo, object, bool> shouldShow = null)
        {
            AddInterpolableModel(new InterpolableModel(owner, id, name, interpolateBefore, interpolateAfter, isCompatibleWithTarget, getValue, readValueFromXml, writeValueToXml, getParameter, readParameterFromXml, writeParameterToXml, checkIntegrity, useOciInHash, getFinalName, shouldShow));
        }

        /// <summary>
        /// Refreshes the list of displayed interpolables. This function is quite heavy as it must go through each InterpolableModel and check if it's compatible with the current target.
        /// It is called automatically by Timeline when selecting another Workspace object or GuideObject.
        /// </summary>
        public static void RefreshInterpolablesList()
        {
            if (_refreshInterpolablesListScheduled == false)
            {
                _refreshInterpolablesListScheduled = true;
                _self.ExecuteDelayed2(() =>
                {
                    _refreshInterpolablesListScheduled = false;
                    _self.UpdateInterpolablesView();
                });
            }
        }

        /// <summary>
        /// Get all keyframes that are currently selected.
        /// </summary>
        public static IEnumerable<KeyValuePair<float, Keyframe>> GetSelectedKeyframes()
        {
            return _self._selectedKeyframes;
        }

        /// <summary>
        /// Get all keyframes that are in the current project.
        /// </summary>
        /// <param name="onlyEnabled">Only get keyframes of enabled interpolables</param>
        public static IEnumerable<KeyValuePair<float, Keyframe>> GetAllKeyframes(bool onlyEnabled)
        {
            return GetAllInterpolables(onlyEnabled).SelectMany(x => x.keyframes);
        }

        /// <summary>
        /// Get all interpolables that are in the current project.
        /// </summary>
        /// <param name="onlyEnabled">Only get enabled interpolables</param>
        public static IEnumerable<Interpolable> GetAllInterpolables(bool onlyEnabled)
        {
            return onlyEnabled ? _self._interpolables.Values.Where(x => x.enabled) : _self._interpolables.Values;
        }

        /// <summary>
        /// Is the main timeline window visible?
        /// </summary>
        public static bool InterfaceVisible
        {
            get
            {
                return _self.UiVisible;
            }
            set
            {
                if (_self.UiVisible != value)
                    _self.ToggleUiVisible();
            }
        }

        /// <summary>
        /// Get an estimation of the real duration of the entire timeline, accounting for time scale changes.
        /// Calculation cost is not trivial (expect ~1ms execution cost for 50 seconds of timeline).
        /// </summary>
        public static float EstimateRealDuration()
        {
            float realDuration = 0;
            Interpolable interpolable = _self._interpolables.Values.FirstOrDefault(x => x.id == "timeScale");
            if (interpolable == null || !interpolable.enabled)
                return (Time.timeScale == 0) ? duration : duration / Time.timeScale;

            List<KeyValuePair<float, Keyframe>> keyframes = interpolable.keyframes.TakeWhile(x => x.Key <= duration).ToList();
            if (keyframes.Count == 0)
                return (Time.timeScale == 0) ? duration : duration / Time.timeScale;

            // In the interval [0, firstKeyframe], Timeline uses the value of the first keyframe
            realDuration += keyframes.First().Key / (float)keyframes.First().Value.value;

            KeyValuePair<float, Keyframe> keyframeAfterEnd = interpolable.keyframes.FirstOrDefault(x => x.Key > duration);
            if (!keyframeAfterEnd.Equals(default(KeyValuePair<float, Keyframe>)))
            {
                // In the interval [lastKeyframe, duration], Timeline still interpolates if there is a keyframe outside of the duration window
                KeyValuePair<float, Keyframe> lastKeyframe = keyframes.Last();
                float normalizedTime = (duration - lastKeyframe.Key) / (keyframeAfterEnd.Key - lastKeyframe.Key);
                float normalizedValue = keyframeAfterEnd.Value.curve.Evaluate(normalizedTime);
                float valueAtEnd = (float)lastKeyframe.Value.value + normalizedValue * ((float)keyframeAfterEnd.Value.value - (float)lastKeyframe.Value.value);
                realDuration += IntegrateTimescaleReciprocal(keyframeAfterEnd.Value.curve, (float)lastKeyframe.Value.value, valueAtEnd, duration - lastKeyframe.Key);
            }
            else
            {
                // In the interval [lastKeyframe, duration], Timeline uses the value of the last keyframe
                realDuration += (duration - keyframes.Last().Key) / (float)keyframes.Last().Value.value;
            }

            for (int i = 0; i < keyframes.Count - 1; i++)
            {
                KeyValuePair<float, Keyframe> current = keyframes.ElementAt(i);
                KeyValuePair<float, Keyframe> next = keyframes.ElementAt(i + 1);
                float value = IntegrateTimescaleReciprocal(current.Value.curve, (float)current.Value.value, (float)next.Value.value, next.Key - current.Key);
                realDuration += value;
            }
            return realDuration;
        }

        #endregion

        #region Private Methods
        private Interpolable AddInterpolable(InterpolableModel model)
        {
            bool added = false;
            Interpolable actualInterpolable = null;
            try
            {
                if (model.IsCompatibleWithTarget(_selectedOCI) == false)
                    return null;
                RecordUndo("Add track");
                Interpolable interpolable = new Interpolable(_selectedOCI, model);

                // Asking for a single axis while the combined track still exists means the user wants it
                // split. Do that instead of creating a second track that would fight over the same value.
                Interpolable combined = FindCombinedFor(interpolable);
                if (combined != null)
                {
                    SplitTransformInterpolable(combined);
                    _interpolables.TryGetValue(interpolable.GetHashCode(), out actualInterpolable);
                    return actualInterpolable;
                }

                if (_interpolables.TryGetValue(interpolable.GetHashCode(), out actualInterpolable) == false)
                {
                    _interpolables.Add(interpolable.GetHashCode(), interpolable);
                    _interpolablesTree.AddLeaf(interpolable);
                    actualInterpolable = interpolable;
                    added = true;
                }
                UpdateInterpolablesView();
                return actualInterpolable;
            }
            catch (Exception e)
            {
                Logger.LogError("Couldn't add interpolable with model:\n" + model + "\n" + e);
                if (added)
                {
                    _interpolables.Remove(actualInterpolable.GetHashCode());
                    _interpolablesTree.RemoveLeaf(actualInterpolable);
                    UpdateInterpolablesView();
                }
            }
            return null;
        }

        private void RemoveInterpolable(Interpolable interpolable)
        {
            _interpolables.Remove(interpolable.GetHashCode());
            int selectedIndex = _selectedInterpolables.IndexOf(interpolable);
            if (selectedIndex != -1)
                _selectedInterpolables.RemoveAt(selectedIndex);
            _interpolablesTree.RemoveLeaf(interpolable);
            _selectedKeyframes.RemoveAll(elem => elem.Value.parent == interpolable);
            UpdateInterpolablesView();
            UpdateKeyframeWindow(false);
        }

        private void RemoveInterpolables(IEnumerable<Interpolable> interpolables)
        {
            RecordUndo("Remove tracks");
            if (interpolables == _selectedInterpolables)
                interpolables = interpolables.ToArray();
            foreach (Interpolable interpolable in interpolables)
            {
                if (_interpolables.ContainsKey(interpolable.GetHashCode()))
                    _interpolables.Remove(interpolable.GetHashCode());
                _interpolablesTree.RemoveLeaf(interpolable);

                int index = _selectedInterpolables.IndexOf(interpolable);
                if (index != -1)
                    _selectedInterpolables.RemoveAt(index);
                _selectedKeyframes.RemoveAll(elem => elem.Value.parent == interpolable);
            }
            UpdateInterpolablesView();
            UpdateKeyframeWindow(false);
        }

        /// <summary>Builds everything, and leaves Timeline off rather than half started when that fails.</summary>
        private void Init()
        {
            try
            {
                InitInternal();
            }
            catch (Exception e)
            {
                Logger.LogError("Timeline failed to start up:\n" + e);
                _loaded = false;
            }
        }

        private void InitInternal()
        {
            UIUtility.Init();

            BuiltInInterpolables.Populate();

            if (Camera.main.GetComponent<Expression>() == null)
                Camera.main.gameObject.AddComponent<Expression>();
            _allGuideObjects = (Dictionary<Transform, GuideObject>)GuideObjectManager.Instance.GetPrivate("dicGuideObject");
            // The tracks ShalltyUtils used to provide, so its scenes still open without it.
            Compat.ShalltyTracks.Register(_allGuideObjects);
            _selectedGuideObjects = (HashSet<GuideObject>)GuideObjectManager.Instance.GetPrivate("hashSelectObject");

            BuildView();
            InitShallty();
            UpdateInterpolablesView();

            _loaded = true;

            _toolbarButton = new TimelineButton(this);
            ToolbarManager.AddLeftToolbarControl(_toolbarButton);
        }

        #region Split transform tracks
        private static string SplitKey(string owner, string id)
        {
            return owner + "" + id;
        }

        /// <summary>
        /// Whether two interpolable parameters denote the same thing.
        ///
        /// Reference equality is not enough: plugins are free to build a fresh parameter object on every
        /// getParameter call, and the pose editor's bone tracks do exactly that with HashedPair, which
        /// overrides GetHashCode but not Equals. The hash is what identifies a parameter across those
        /// instances, and is the same thing Timeline keys its interpolable dictionary on.
        /// </summary>
        private static bool SameParameter(object a, object b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (a == null || b == null)
                return false;
            return a.GetType() == b.GetType() && a.GetHashCode() == b.GetHashCode();
        }

        /// <summary>
        /// Declares that a combined transform track can be exchanged for three per axis ones.
        /// Public so plugins can register their own through ToolBox.TimelineCompatibility.
        /// </summary>
        public static void RegisterSplittableTransform(string owner, string combinedId, string[] splitIds)
        {
            if (owner == null || combinedId == null || splitIds == null || splitIds.Length != 3)
                return;
            _splitsOfCombined[SplitKey(owner, combinedId)] = splitIds;
            foreach (string splitId in splitIds)
                _combinedOfSplit[SplitKey(owner, splitId)] = combinedId;
        }

        /// <summary>Whether an interpolable with that owner and id already exists for this parameter.</summary>
        public static bool HasInterpolable(object parameter, string owner, string id)
        {
            if (_self == null || parameter == null)
                return false;
            foreach (Interpolable interpolable in _self._interpolables.Values)
            {
                if (interpolable.id == id && interpolable.owner == owner && SameParameter(interpolable.parameter, parameter))
                    return true;
            }
            return false;
        }

        /// <summary>Whether any of the per axis versions of a combined track exist for this parameter.</summary>
        public static bool HasAnySplit(object parameter, string owner, string combinedId)
        {
            string[] splitIds;
            if (parameter == null || _splitsOfCombined.TryGetValue(SplitKey(owner, combinedId), out splitIds) == false)
                return false;
            foreach (string id in splitIds)
            {
                if (HasInterpolable(parameter, owner, id))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// The existing combined track that would conflict with this axis track, if there is one.
        /// </summary>
        private Interpolable FindCombinedFor(Interpolable splitTrack)
        {
            string combinedId = GetCombinedId(splitTrack.owner, splitTrack.id);
            if (combinedId == null)
                return null;
            foreach (Interpolable interpolable in _interpolables.Values)
            {
                if (interpolable.id == combinedId && interpolable.owner == splitTrack.owner &&
                    SameParameter(interpolable.parameter, splitTrack.parameter))
                    return interpolable;
            }
            return null;
        }

        /// <summary>The combined track an axis track belongs to, or null when it is not a split track.</summary>
        private static string GetCombinedId(string owner, string splitId)
        {
            string combinedId;
            return _combinedOfSplit.TryGetValue(SplitKey(owner, splitId), out combinedId) ? combinedId : null;
        }

        private InterpolableModel GetModel(string owner, string id)
        {
            List<InterpolableModel> models;
            if (_interpolableModelsDictionary.TryGetValue(owner, out models) == false)
                return null;
            return models.FirstOrDefault(m => m.id == id);
        }

        /// <summary>
        /// Turns a combined transform track into three float tracks, one per axis, keeping the keyframe
        /// times and the easing curves. This is what makes it possible to give an axis its own timing.
        /// </summary>
        private void SplitTransformInterpolable(Interpolable combined)
        {
            string[] splitIds;
            if (_splitsOfCombined.TryGetValue(SplitKey(combined.owner, combined.id), out splitIds) == false)
                return;
            RecordUndo("Split track");
            // The split carries the easing curves across, so they have to be saying what the handles say
            // before it starts.
            HandleMath.SyncEasing(combined.keyframes);

            GroupNode<InterpolableGroup> group = (GroupNode<InterpolableGroup>)_interpolablesTree.GetLeafNode(combined)?.parent;

            // Euler angles wrap at 360, so a rotation crossing zero would read as a jump backwards once
            // the axes are independent. Unwrapping keeps each axis continuous across the keyframes.
            bool unwrap = combined.keyframes.Count != 0 && combined.keyframes.Values[0].value is Quaternion;
            var previous = new float[3];
            bool first = true;

            var tracks = new Interpolable[3];
            for (int axis = 0; axis < 3; ++axis)
            {
                InterpolableModel model = GetModel(combined.owner, splitIds[axis]);
                if (model == null)
                    return;

                Interpolable track = new Interpolable(combined.oci, combined.parameter, model);
                if (_interpolables.ContainsKey(track.GetHashCode()))
                    return; // already split
                tracks[axis] = track;
            }

            foreach (KeyValuePair<float, Keyframe> pair in combined.keyframes)
            {
                Vector3 components = ToVector3(pair.Value.value);
                for (int axis = 0; axis < 3; ++axis)
                {
                    float value = axis == 0 ? components.x : axis == 1 ? components.y : components.z;
                    if (unwrap && first == false)
                        value += 360f * Mathf.Round((previous[axis] - value) / 360f);
                    previous[axis] = value;
                    tracks[axis].keyframes.Add(pair.Key, new Keyframe(value, tracks[axis], new AnimationCurve(pair.Value.curve.keys)));
                }
                first = false;
            }

            for (int axis = 0; axis < 3; ++axis)
            {
                tracks[axis].enabled = combined.enabled;
                tracks[axis].color = combined.color;
                tracks[axis].smooth = combined.smooth;
                tracks[axis].extrapolation = combined.extrapolation;
                // Each axis is a single number now, so its handles have one slot rather than three. They
                // are read back out of the easing curves the split copied, which is the one thing the
                // combined track's own per axis handles and the new tracks certainly agree on.
                HandleMath.Convert(tracks[axis].keyframes);
                _interpolables.Add(tracks[axis].GetHashCode(), tracks[axis]);
                _interpolablesTree.AddLeaf(tracks[axis], group);
            }

            RemoveInterpolables(new[] { combined });
            UpdateInterpolablesView();
        }

        /// <summary>
        /// Puts three axis tracks back together. Times are the union of all three, and an axis that has
        /// no keyframe at a given time is sampled there, so nothing shifts.
        /// </summary>
        private void MergeTransformInterpolables(Interpolable anyAxis)
        {
            string combinedId = GetCombinedId(anyAxis.owner, anyAxis.id);
            string[] splitIds;
            if (combinedId == null || _splitsOfCombined.TryGetValue(SplitKey(anyAxis.owner, combinedId), out splitIds) == false)
                return;

            var tracks = new Interpolable[3];
            for (int axis = 0; axis < 3; ++axis)
            {
                foreach (Interpolable interpolable in _interpolables.Values)
                {
                    if (interpolable.id == splitIds[axis] && interpolable.owner == anyAxis.owner &&
                        SameParameter(interpolable.parameter, anyAxis.parameter))
                    {
                        tracks[axis] = interpolable;
                        break;
                    }
                }
            }

            InterpolableModel model = GetModel(anyAxis.owner, combinedId);
            if (model == null)
                return;
            RecordUndo("Merge tracks");
            // The merge reads the X track's easing curve for the shared one, so it has to be saying what
            // that track's handles say before it starts.
            foreach (Interpolable track in tracks)
            {
                if (track != null)
                    HandleMath.SyncEasing(track.keyframes);
            }

            Interpolable combined = new Interpolable(anyAxis.oci, anyAxis.parameter, model);
            if (_interpolables.ContainsKey(combined.GetHashCode()))
                return;

            var times = new List<float>();
            foreach (Interpolable track in tracks)
            {
                if (track == null)
                    continue;
                foreach (float time in track.keyframes.Keys)
                {
                    if (times.Contains(time) == false)
                        times.Add(time);
                }
            }
            times.Sort();

            // Only used for its type and to fill in axes that have no track at all.
            object template = combined.GetValue();
            Vector3 fallback = ToVector3(template);
            foreach (float time in times)
            {
                var value = new Vector3(
                        tracks[0] != null ? SampleFloat(tracks[0], time) : fallback.x,
                        tracks[1] != null ? SampleFloat(tracks[1], time) : fallback.y,
                        tracks[2] != null ? SampleFloat(tracks[2], time) : fallback.z);

                // The X track's curve is the best available guess for the shared one.
                AnimationCurve curve = tracks[0] != null && tracks[0].keyframes.ContainsKey(time)
                        ? new AnimationCurve(tracks[0].keyframes[time].curve.keys)
                        : AnimationCurve.Linear(0f, 0f, 1f, 1f);
                combined.keyframes.Add(time, new Keyframe(FromVector3(template, value), combined, curve));
            }

            Interpolable reference = tracks.FirstOrDefault(t => t != null) ?? anyAxis;
            combined.enabled = reference.enabled;
            combined.color = reference.color;
            combined.smooth = reference.smooth;
            combined.extrapolation = reference.extrapolation;
            // Three tracks with a shaped handle each become one that can only hold a shared shape, so the
            // handles come from the curve above and X is the axis that gets to keep its own.
            HandleMath.Convert(combined.keyframes);

            GroupNode<InterpolableGroup> group = (GroupNode<InterpolableGroup>)_interpolablesTree.GetLeafNode(reference)?.parent;
            _interpolables.Add(combined.GetHashCode(), combined);
            _interpolablesTree.AddLeaf(combined, group);

            RemoveInterpolables(tracks.Where(t => t != null).ToArray());
            UpdateInterpolablesView();
        }

        /// <summary>Value of a float track at an arbitrary time, following its own easing curve.</summary>
        private static float SampleFloat(Interpolable interpolable, float time)
        {
            KeyValuePair<float, Keyframe> left = default;
            KeyValuePair<float, Keyframe> right = default;
            foreach (KeyValuePair<float, Keyframe> pair in interpolable.keyframes)
            {
                if (pair.Key <= time)
                    left = pair;
                else
                {
                    right = pair;
                    break;
                }
            }

            if (left.Value == null)
                return right.Value != null ? (float)right.Value.value : 0f;
            if (right.Value == null)
                return (float)left.Value.value;

            float factor = left.Value.curve.Evaluate((time - left.Key) / (right.Key - left.Key));
            return Mathf.LerpUnclamped((float)left.Value.value, (float)right.Value.value, factor);
        }

        private static Vector3 ToVector3(object value)
        {
            if (value is Vector3)
                return (Vector3)value;
            if (value is Quaternion)
                return ((Quaternion)value).eulerAngles;
            return Vector3.zero;
        }

        private static object FromVector3(object template, Vector3 value)
        {
            if (template is Quaternion)
                return Quaternion.Euler(value);
            return value;
        }
        #endregion

        private void InterpolateBefore()
        {
            if (_isPlaying)
            {
                _playbackTime = (Time.time - _startTime) % _duration;
                Interpolate(true);
                // Only here, where values were actually written. Refreshing it every frame regardless
                // meant that a gizmo drag arriving before this ran was taken for the timeline's own
                // doing, and recording never saw a thing.
                RefreshRecordBaseline();
            }
        }

        private void InterpolateAfter()
        {
            if (_isPlaying)
            {
                Interpolate(false);
                RefreshRecordBaseline();
            }
        }

        private void Interpolate(bool before)
        {
            SampleStrips();
            _interpolablesTree.Recurse((node, depth) =>
            {
                if (node.type != INodeType.Leaf)
                    return;
                Interpolable interpolable = ((LeafNode<Interpolable>)node).obj;
                if (interpolable.enabled == false || IsMutedBySolo(interpolable))
                    return;
                if (before)
                {
                    if (interpolable.canInterpolateBefore == false)
                        return;
                }
                else
                {
                    if (interpolable.canInterpolateAfter == false)
                        return;
                }

                // Outside its own first and last keyframe a cyclic track folds the time back into them.
                // cycles counts the whole repetitions that skipped, which is what an offsetting track
                // stacks its values with so a walk keeps walking instead of snapping back to the start.
                int cycles;
                float trackTime = TrackCycle.Wrap(interpolable.keyframes, interpolable.extrapolation, _playbackTime, out cycles);

                KeyValuePair<float, Keyframe> left = default;
                KeyValuePair<float, Keyframe> right = default;
                int leftIndex = -1;
                int index = 0;
                foreach (KeyValuePair<float, Keyframe> keyframePair in interpolable.keyframes)
                {
                    if (keyframePair.Key <= trackTime)
                    {
                        left = keyframePair;
                        leftIndex = index;
                    }
                    else
                    {
                        right = keyframePair;
                        break;
                    }
                    ++index;
                }

                object leftValue = left.Value == null ? null : left.Value.value;
                object rightValue = right.Value == null ? null : right.Value.value;
                bool offsetting = cycles != 0 && interpolable.extrapolation == TrackExtrapolation.CyclicOffset;
                object firstValue = null;
                object lastValue = null;
                if (offsetting)
                {
                    firstValue = interpolable.keyframes.Values[0].value;
                    lastValue = interpolable.keyframes.Values[interpolable.keyframes.Count - 1].value;
                    leftValue = TrackCycle.Offset(leftValue, firstValue, lastValue, cycles);
                    rightValue = TrackCycle.Offset(rightValue, firstValue, lastValue, cycles);
                }

                bool res = true;
                Nla.StripSample stripSample;
                if (_stripSamples.TryGetValue(interpolable, out stripSample))
                {
                    // A strip speaks for this interpolable right now, so its own keyframes stay quiet.
                    if (before)
                        res = interpolable.InterpolateBefore(stripSample.left, stripSample.right, stripSample.factor);
                    else
                        res = interpolable.InterpolateAfter(stripSample.left, stripSample.right, stripSample.factor);
                }
                else if (TrackCycle.TryLinear(interpolable.keyframes, interpolable.extrapolation, trackTime,
                                              out Keyframe linearFrom, out Keyframe linearTo, out float linearFactor))
                {
                    if (before)
                        res = interpolable.InterpolateBefore(linearFrom.value, linearTo.value, linearFactor);
                    else
                        res = interpolable.InterpolateAfter(linearFrom.value, linearTo.value, linearFactor);
                }
                else if (left.Value != null && right.Value != null)
                {
                    object smoothed;
                    if (interpolable.smooth && KeyframeSpline.TryEvaluate(interpolable.keyframes, leftIndex, trackTime, out smoothed))
                    {
                        if (offsetting)
                            smoothed = TrackCycle.Offset(smoothed, firstValue, lastValue, cycles);
                        // Handed over as a lerp that cannot move: every interpolable applies its value
                        // with LerpUnclamped(left, right, factor), and Lerp(v, v, 0) is v. That lets the
                        // spline drive the result without changing the delegate signature every plugin
                        // registering an interpolable relies on.
                        if (before)
                            res = interpolable.InterpolateBefore(smoothed, smoothed, 0f);
                        else
                            res = interpolable.InterpolateAfter(smoothed, smoothed, 0f);
                    }
                    else if (HandleMath.HasHandles(left.Value, right.Value))
                    {
                        if (HandleMath.IsFactorSpace(left.Value.value))
                        {
                            // A rotation's handles shape when, not what: the slerp still draws the path,
                            // the curve only says how fast it is walked.
                            float factor = HandleMath.Evaluate(interpolable.keyframes, leftIndex, 0, trackTime);
                            if (before)
                                res = interpolable.InterpolateBefore(leftValue, rightValue, factor);
                            else
                                res = interpolable.InterpolateAfter(leftValue, rightValue, factor);
                        }
                        else
                        {
                            // Each component has its own curve, so there is no single factor to hand
                            // over. The value is built outright and passed as a lerp that cannot move,
                            // the same trick the spline uses.
                            object shaped = HandleMath.Value(interpolable.keyframes, leftIndex, trackTime);
                            if (offsetting)
                                shaped = TrackCycle.Offset(shaped, firstValue, lastValue, cycles);
                            if (before)
                                res = interpolable.InterpolateBefore(shaped, shaped, 0f);
                            else
                                res = interpolable.InterpolateAfter(shaped, shaped, 0f);
                        }
                    }
                    else
                    {
                        float normalizedTime = (trackTime - left.Key) / (right.Key - left.Key);
                        normalizedTime = left.Value.curve.Evaluate(normalizedTime);
                        if (before)
                            res = interpolable.InterpolateBefore(leftValue, rightValue, normalizedTime);
                        else
                            res = interpolable.InterpolateAfter(leftValue, rightValue, normalizedTime);
                    }
                }
                else if (left.Value != null)
                {
                    if (before)
                        res = interpolable.InterpolateBefore(leftValue, leftValue, 0);
                    else
                        res = interpolable.InterpolateAfter(leftValue, leftValue, 0);
                }
                else if (right.Value != null)
                {
                    if (before)
                        res = interpolable.InterpolateBefore(rightValue, rightValue, 0);
                    else
                        res = interpolable.InterpolateAfter(rightValue, rightValue, 0);
                }
                if (res == false)
                    _toDelete.Add(interpolable);
            });
        }

        private float ParseTime(string timeString)
        {
            // Seconds alone ("12.5") as well as minutes and seconds ("00:12.50"), and read the same way
            // whatever the system language: a decimal comma is taken for a point.
            string[] timeComponents = timeString.Trim().Replace(',', '.').Split(':');
            if (timeComponents.Length > 2)
                return -1;
            int minutes = 0;
            if (timeComponents.Length == 2 && (int.TryParse(timeComponents[0], out minutes) == false || minutes < 0))
                return -1;
            float seconds;
            if (float.TryParse(timeComponents[timeComponents.Length - 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out seconds) == false || seconds < 0)
                return -1;
            return minutes * 60 + seconds;
        }

        // Estimate the real time duration when timescale changes from startTimescale to endTimescale over duration according to curve.
        private static float IntegrateTimescaleReciprocal(AnimationCurve curve, float startTimescale, float endTimescale, float duration)
        {
            const int STEPS_PER_SECOND = 20;
            int steps = Mathf.FloorToInt(STEPS_PER_SECOND * duration);
            steps = Math.Max(steps, STEPS_PER_SECOND);

            Func<float, float> reciprocal = (t) =>
            {
                float value = startTimescale + curve.Evaluate(t) * (endTimescale - startTimescale);
                return Mathf.Approximately(value, 0f) ? 0f : 1f / value;
            };

            float total = 0f;
            float dt = 1f / steps;

            for (int i = 0; i < steps; i++)
            {
                float t = i * dt;

                float k1 = dt * reciprocal(t);
                float k2 = dt * reciprocal(t + dt / 2);
                float k3 = dt * reciprocal(t + dt / 2);
                float k4 = dt * reciprocal(t + dt);

                total += (k1 + 2 * k2 + 2 * k3 + k4) / 6;
            }

            return total * duration;
        }

        #region Main Window

        private void UpdateDesiredFrameRate(string s)
        {
            int res;
            if (int.TryParse(s, out res) && res >= 1)
                _desiredFrameRate = res;
        }

        /// <summary>Takes the text it is handed, which is the Classic field's own text when that is the caller.</summary>
        private void UpdateDuration(string s)
        {
            float time = ParseTime(s);
            if (time <= 0)
                return;
            _duration = time;
            UpdateGrid();
        }

        private void SelectAddInterpolable(params Interpolable[] interpolables)
        {
            foreach (Interpolable interpolable in interpolables)
            {
                int index = _selectedInterpolables.FindIndex(k => k == interpolable);
                if (index != -1)
                    _selectedInterpolables.RemoveAt(index);
                else
                    _selectedInterpolables.Add(interpolable);
            }
        }

        /// <summary>
        /// Points Studio at whatever the interpolable animates: the IK or FK node it drives, or failing
        /// that the object it belongs to. Bone tracks from the pose editor land on the character, since
        /// the bone itself is a plain transform and not something Studio can select.
        /// </summary>
        private void SelectLinkedGuideObject(Interpolable interpolable)
        {
            // The workspace tree owns the selection. Pointing the gizmo at a node belonging to an object
            // that is not selected there does nothing, so the owner has to be picked first.
            if (interpolable.oci != null && interpolable.oci != _selectedOCI && interpolable.oci.treeNodeObject != null)
            {
                try
                {
                    Studio.Studio.Instance.treeNodeCtrl.SelectSingle(interpolable.oci.treeNodeObject);
                }
                catch (Exception e)
                {
                    Logger.LogWarning("Couldn't select the workspace object for an interpolable: " + e.Message);
                }
            }

            GuideObject linkedGuideObject = interpolable.parameter as GuideObject;
            if (linkedGuideObject == null && interpolable.oci != null)
                linkedGuideObject = interpolable.oci.guideObject;
            if (linkedGuideObject != null && GuideObjectManager.Instance.selectObject != linkedGuideObject)
                GuideObjectManager.Instance.selectObject = linkedGuideObject;
        }

        private void SelectInterpolable(params Interpolable[] interpolables)
        {
            _selectedInterpolables.Clear();
            SelectAddInterpolable(interpolables);
        }

        private void ClearSelectedInterpolables()
        {
            _selectedInterpolables.Clear();
        }

        private void SelectAddKeyframes(params KeyValuePair<float, Keyframe>[] keyframes)
        {
            SelectAddKeyframes((IEnumerable<KeyValuePair<float, Keyframe>>)keyframes);
        }

        private void SelectAddKeyframes(IEnumerable<KeyValuePair<float, Keyframe>> keyframes)
        {
            foreach (KeyValuePair<float, Keyframe> keyframe in keyframes)
            {
                int index = _selectedKeyframes.FindIndex(k => k.Value == keyframe.Value);
                if (index != -1)
                    _selectedKeyframes.RemoveAt(index);
                else
                    _selectedKeyframes.Add(keyframe);
            }
            UpdateKeyframeWindow();
        }

        private void SelectKeyframes(params KeyValuePair<float, Keyframe>[] keyframes)
        {
            SelectKeyframes((IEnumerable<KeyValuePair<float, Keyframe>>)keyframes);
        }

        private void SelectKeyframes(IEnumerable<KeyValuePair<float, Keyframe>> keyframes)
        {
            _selectedKeyframes.Clear();
            if (keyframes.Count() != 0)
                SelectAddKeyframes(keyframes);
            else
                UpdateGrid();
        }

        private void RebuildSelectedKeyframeSet()
        {
            _selectedKeyframeSet.Clear();
            for (int i = 0; i < _selectedKeyframes.Count; ++i)
                _selectedKeyframeSet.Add(_selectedKeyframes[i].Value);
        }

        private void AddKeyframe(Interpolable interpolable, float time)
        {
            RecordUndo("Add keyframe");
            if (FindOccupant(interpolable, time, null) != null)
            {
                Logger.LogMessage("A keyframe already exists at " + time);
                return;
            }
            try
            {
                Keyframe keyframe;
                KeyValuePair<float, Keyframe> pair = interpolable.keyframes.LastOrDefault(k => k.Key < time);
                if (pair.Value != null)
                    keyframe = new Keyframe(interpolable.GetValue(), interpolable, new AnimationCurve(pair.Value.curve.keys));
                else
                    keyframe = new Keyframe(interpolable.GetValue(), interpolable, AnimationCurve.Linear(0f, 0f, 1f, 1f));
                interpolable.keyframes.Add(time, keyframe);
                // A new keyframe joins a track that is already working in handles, and a track that is
                // not gets them here, which is where "made in this version" starts.
                HandleMath.Convert(interpolable.keyframes);
                if (keyframe.handles != null)
                {
                    // Automatic on both sides, so the curve runs through it rather than stopping at it.
                    // Blender defaults the same way, and it is the shape you want nineteen times out of
                    // twenty; the other time is what the handle types are for.
                    keyframe.handles.leftType = HandleType.Auto;
                    keyframe.handles.rightType = HandleType.Auto;
                }
                UpdateGrid();
            }
            catch (Exception e)
            {
                Logger.LogError("couldn't add keyframe to interpolable with value:" + interpolable + "\n" + e);
            }
        }

        private void CopyKeyframes()
        {
            _copiedKeyframes.Clear();
            foreach (KeyValuePair<float, Keyframe> pair in _selectedKeyframes)
                _copiedKeyframes.Add(new KeyValuePair<float, Keyframe>(pair.Key, new Keyframe(pair.Value)));
        }

        private void CutKeyframes()
        {
            RecordUndo("Cut keyframes");
            CopyKeyframes();
            if (_selectedKeyframes.Count != 0)
                DeleteKeyframes(_selectedKeyframes, false);
        }

        private void PasteKeyframes()
        {
            RecordUndo("Paste keyframes");
            if (_copiedKeyframes.Count == 0)
                return;
            List<KeyValuePair<float, Keyframe>> toSelect = new List<KeyValuePair<float, Keyframe>>();
            float time = _playbackTime % _duration;
            if (time == 0f && _playbackTime == _duration)
                time = _duration;
            float startOffset = _copiedKeyframes.Min(k => k.Key);
            if (Input.GetKey(KeyCode.LeftAlt))
            {
                float max = _copiedKeyframes.Max(k => k.Key);
                //// If they keyframe(s) that are at the end of the selection would conflict with any pushed keyframes (those that are currently on the cursor), then cancel
                //if (this._copiedKeyframes.Where(k => Mathf.Approximately(k.Key, max)).Any(k => k.Value.parent.keyframes.ContainsKey(time)))
                //    return;
                double duration = max - startOffset + (_blockLength / _divisions);
                foreach (IGrouping<Interpolable, KeyValuePair<float, Keyframe>> group in _copiedKeyframes.GroupBy(k => k.Value.parent))
                {
                    foreach (KeyValuePair<float, Keyframe> pair in @group.Key.keyframes.Reverse())
                    {
                        if (pair.Key >= time && !TryMoveKeyframe(pair.Value, (float)(pair.Key + duration)))
                            Logger.LogMessage("Couldn't make room at " + pair.Key + ": another keyframe already exists at " + (pair.Key + duration));
                    }
                }
            }
            else if (_copiedKeyframes.Any(k => k.Value.parent.keyframes.ContainsKey(time + k.Key - startOffset)))
                return;
            foreach (KeyValuePair<float, Keyframe> pair in _copiedKeyframes)
            {
                float finalTime = time + pair.Key - startOffset;
                Keyframe newKeyframe = new Keyframe(pair.Value) { keySet = null };
                pair.Value.parent.keyframes.Add(finalTime, newKeyframe);
                // This is dumb as shit but I have no choice
                toSelect.Add(new KeyValuePair<float, Keyframe>(finalTime, newKeyframe));
            }
            SelectKeyframes(toSelect);
            UpdateGrid();
        }

        // Destination times come from pixel round-trips, so a keyframe aimed exactly at another's
        // time can land ~2e-7 off. An exact SortedList lookup misses that and lets a near-duplicate
        // through, which is the collision this is meant to catch.
        private static Keyframe FindOccupant(Interpolable interpolable, float time, Keyframe self)
        {
            SortedList<float, Keyframe> keyframes = interpolable.keyframes;
            for (int i = 0; i < keyframes.Count; i++)
            {
                if (!Mathf.Approximately(keyframes.Keys[i], time))
                    continue;
                Keyframe candidate = keyframes.Values[i];
                if (candidate != self)
                    return candidate;
            }
            return null;
        }

        private bool TryMoveKeyframe(Keyframe keyframe, float destinationTime)
        {
            SortedList<float, Keyframe> keyframes = keyframe.parent.keyframes;
            if (FindOccupant(keyframe.parent, destinationTime, keyframe) != null)
                return false; // slot held by another keyframe: don't remove, don't orphan
            int index = keyframes.IndexOfValue(keyframe);
            if (index < 0)
                return false;
            keyframes.RemoveAt(index);
            keyframes.Add(destinationTime, keyframe);
            int i = _selectedKeyframes.FindIndex(k => k.Value == keyframe);
            if (i != -1)
                _selectedKeyframes[i] = new KeyValuePair<float, Keyframe>(destinationTime, keyframe);
            return true;
        }

        private void SeekPlaybackTime(float t)
        {
            if (t == _playbackTime)
                return;
            _playbackTime = t;
            _startTime = Time.time - _playbackTime;
            bool isPlaying = _isPlaying;
            _isPlaying = true;
            Interpolate(true);
            Interpolate(false);
            _isPlaying = isPlaying;
        }

        #endregion

        #region Keyframe Window

        private void SelectPreviousKeyframe()
        {
            if (_selectedKeyframes.Count != 1)
                return;

            KeyValuePair<float, Keyframe> firstSelected = _selectedKeyframes[0];
            KeyValuePair<float, Keyframe> keyframe = firstSelected.Value.parent.keyframes.LastOrDefault(f => f.Key < firstSelected.Key);
            if (keyframe.Value != null)
                SelectKeyframes(keyframe);
        }

        private void SelectNextKeyframe()
        {
            if (_selectedKeyframes.Count != 1)
                return;

            KeyValuePair<float, Keyframe> firstSelected = _selectedKeyframes[0];
            KeyValuePair<float, Keyframe> keyframe = firstSelected.Value.parent.keyframes.FirstOrDefault(f => f.Key > firstSelected.Key);
            if (keyframe.Value != null)
                SelectKeyframes(keyframe);
        }

        private void UseCurrentValue()
        {
            RecordUndo("Use current value");
            foreach (KeyValuePair<float, Keyframe> pair in _selectedKeyframes)
                pair.Value.value = pair.Value.parent.GetValue();
        }

        private void DeleteKeyframes(params KeyValuePair<float, Keyframe>[] keyframes)
        {
            DeleteKeyframes((IEnumerable<KeyValuePair<float, Keyframe>>)keyframes);
        }

        private void DeleteKeyframes(IEnumerable<KeyValuePair<float, Keyframe>> keyframes, bool removeInterpolables = true)
        {
            RecordUndo("Delete keyframes");
            keyframes = keyframes.ToList();
            Dictionary<Interpolable, float> deletedMin = new Dictionary<Interpolable, float>();
            Dictionary<Interpolable, float> deletedMax = new Dictionary<Interpolable, float>();
            foreach (KeyValuePair<float, Keyframe> pair in keyframes)
            {
                if (pair.Value == null) //Just a safeguard.
                    continue;
                Interpolable parent = pair.Value.parent;
                if (!deletedMin.TryGetValue(parent, out float pMin) || pair.Key < pMin)
                    deletedMin[parent] = pair.Key;
                if (!deletedMax.TryGetValue(parent, out float pMax) || pair.Key > pMax)
                    deletedMax[parent] = pair.Key;
                try
                {
                    parent.keyframes.Remove(pair.Key);
                    if (removeInterpolables && parent.keyframes.Count == 0)
                        RemoveInterpolable(parent);
                }
                catch (Exception e)
                {
                    Logger.LogError("Couldn't delete keyframe with time \"" + pair.Key + "\" and value \"" + pair.Value + "\" from interpolable \"" + parent + "\"\n" + e);
                }
            }

            if (Input.GetKey(KeyCode.LeftAlt))
            {
                float step = _blockLength / _divisions;
                foreach (Interpolable parent in deletedMin.Keys)
                {
                    float parentMin = deletedMin[parent];
                    // Spans a non-contiguous selection as one block, so the shift can overshoot past zero.
                    double duration = deletedMax[parent] - parentMin + step;
                    foreach (KeyValuePair<float, Keyframe> pair in parent.keyframes.ToList())
                    {
                        if (pair.Key <= parentMin)
                            continue;
                        float newTime = (float)(pair.Key - duration);
                        if (newTime < 0f)
                            Logger.LogMessage("Couldn't close gap at " + pair.Key + ": " + newTime + " is before the start of the timeline");
                        else if (!TryMoveKeyframe(pair.Value, newTime))
                            Logger.LogMessage("Couldn't close gap at " + pair.Key + ": another keyframe already exists at " + newTime);
                    }
                }
            }
            _selectedKeyframes.RemoveAll(elem => elem.Value == null || keyframes.Any(k => k.Value == elem.Value));
            SelectNeighbourAfterDelete(deletedMin);

            UpdateGrid();
            UpdateKeyframeWindow(false);
        }

        /// <summary>
        /// Leaves a keyframe selected after a delete: the one nearest where the deleted ones were.
        ///
        /// An empty selection closes the keyframe window and takes the tangent handles off the graph, so
        /// deleting one keyframe used to take away the tools you were in the middle of using. Something
        /// still being selected is also what every other editor does.
        /// </summary>
        private void SelectNeighbourAfterDelete(Dictionary<Interpolable, float> deletedAt)
        {
            if (_selectedKeyframes.Count != 0 || deletedAt.Count == 0)
                return;

            Keyframe nearest = null;
            float nearestTime = 0f;
            float nearestDistance = float.PositiveInfinity;
            foreach (KeyValuePair<Interpolable, float> pair in deletedAt)
            {
                for (int i = 0; i < pair.Key.keyframes.Count; ++i)
                {
                    float distance = Mathf.Abs(pair.Key.keyframes.Keys[i] - pair.Value);
                    if (distance >= nearestDistance)
                        continue;
                    nearestDistance = distance;
                    nearestTime = pair.Key.keyframes.Keys[i];
                    nearest = pair.Key.keyframes.Values[i];
                }
            }

            if (nearest != null)
                SelectKeyframes(new KeyValuePair<float, Keyframe>(nearestTime, nearest));
        }

        private void SaveKeyframeTime(float time)
        {
            RecordUndo("Move keyframes");
            for (int i = 0; i < _selectedKeyframes.Count; i++)
            {
                KeyValuePair<float, Keyframe> pair = _selectedKeyframes[i];
                Keyframe potentialDuplicateKeyframe;
                if (pair.Value.parent.keyframes.TryGetValue(time, out potentialDuplicateKeyframe) && potentialDuplicateKeyframe != pair.Value)
                    continue;
                pair.Value.parent.keyframes.Remove(pair.Key);
                pair.Value.parent.keyframes.Add(time, pair.Value);
                _selectedKeyframes[i] = new KeyValuePair<float, Keyframe>(time, pair.Value);
            }

            UpdateGrid();
        }

        /// <summary>
        /// Re-applies the timeline at the current playback time so an edit is visible right away, even paused.
        /// </summary>
        private void RefreshInterpolation()
        {
            bool wasPlaying = _isPlaying;
            _isPlaying = true;
            Interpolate(true);
            Interpolate(false);
            _isPlaying = wasPlaying;
            // The scene now holds what the timeline says, so this is the reference recording compares
            // against. Without it, editing a keyframe by hand would read back as someone moving things.
            RefreshRecordBaseline();
        }

        #endregion

        #endregion

        #region Saves

#if KOIKATSU || AISHOUJO || HONEYSELECT2
        private void OnSceneLoad(string path)
        {
            ReadShalltyData();
            var node = GetSceneInfo() ?? new XmlDocument().CreateElement("root");
            SceneLoad(path, node);
        }
            
        private void OnSceneImport(string path)
        {
            var node = GetSceneInfo();
            if (node == null)
                return;
            SceneImport(path, node);
        }

        /// <summary>What ShalltyUtils saved in this scene, kept until Timeline's own data has been read.</summary>
        private void ReadShalltyData()
        {
            PluginData data = ExtendedSave.GetSceneExtendedDataById(_shalltyGuid);
            object value;
            _shalltyGroups = data != null && data.data.TryGetValue("keyframesGroupsData", out value) ? value as string : null;
            _shalltyPicker = data != null && data.data.TryGetValue("guideObjectPickerData", out value) ? value as string : null;
        }

        private static XmlNode GetSceneInfo()
        {
            PluginData data = ExtendedSave.GetSceneExtendedDataById(_extSaveKey);
            if (data == null)
                return null;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml((string)data.data["sceneInfo"]);
            return doc.FirstChild;
        }

        private void OnSceneSave(string path)
        {
            using (StringWriter stringWriter = new StringWriter())
            using (XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter))
            {

                xmlWriter.WriteStartElement("root");
                SceneWrite(path, xmlWriter);
                xmlWriter.WriteEndElement();

                PluginData data = new PluginData();
                data.version = Timeline._saveVersion;
                data.data.Add("sceneInfo", stringWriter.ToString());
                ExtendedSave.SetSceneExtendedDataById(_extSaveKey, data);
            }
        }
#endif

        private void SceneLoad(string path, XmlNode node)
        {
            if (node == null)
                return;
            this.ExecuteDelayed2(() =>
            {
                _interpolables.Clear();
                _interpolablesTree.Clear();
                _orphanTracks.Clear();
                _keySets.Clear();
                _rigPending = null;
                _pickerPages.Clear();
                _pickerPage = 0;
                ClearTrimRange();
                _selectedOCI = null;
                _selectedKeyframes.Clear();

                List<KeyValuePair<int, ObjectCtrlInfo>> dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).ToList();
                SceneLoad(node, dic);

                UpdateInterpolablesView();
            }, 20);
        }

        private void SceneImport(string path, XmlNode node)
        {
            Dictionary<int, ObjectCtrlInfo> toIgnore = new Dictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl);
            this.ExecuteDelayed2(() =>
            {
                List<KeyValuePair<int, ObjectCtrlInfo>> dic = Studio.Studio.Instance.dicObjectCtrl.Where(e => toIgnore.ContainsKey(e.Key) == false).OrderBy(e => SceneInfo_Import_Patches._newToOldKeys[e.Key]).ToList();
                SceneLoad(node, dic);

                UpdateInterpolablesView();
            }, 20);
        }

        private void SceneWrite(string path, XmlTextWriter writer)
        {
            List<KeyValuePair<int, ObjectCtrlInfo>> dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).ToList();
            writer.WriteAttributeString("duration", XmlConvert.ToString(_duration));
            writer.WriteAttributeString("blockLength", XmlConvert.ToString(_blockLength));
            writer.WriteAttributeString("divisions", XmlConvert.ToString(_divisions));
            writer.WriteAttributeString("timeScale", XmlConvert.ToString(Time.timeScale));
            foreach (INode node in _interpolablesTree.tree)
                WriteInterpolableTree(node, writer, dic);
            WriteOrphanTracks(writer, dic);
            WriteStrips(writer, dic);
            WriteMarkers(writer);
            WriteKeySets(writer);
            WritePicker(writer, dic);
        }

        private void SceneLoad(XmlNode node, List<KeyValuePair<int, ObjectCtrlInfo>> dic)
        {
            // History from the previous scene refers to tracks that no longer belong to anything.
            ClearHistory();
            ClearStrips();
            // These hold tracks from the scene being replaced, which are about to stop existing.
            _graphHiddenTracks.Clear();
            _graphLockedTracks.Clear();
            _hiddenComponents.Clear();
            _soloInterpolables.Clear();
            HandleMath.converted = 0;
            int orphansBefore = _orphanTracks.Count;
            bool ownKeySets = node.ChildNodes.Cast<XmlNode>().Any(n => n.Name == "keySets");
            BeginKeySetRead(node);
            ReadInterpolableTree(node, dic);
            EndKeySetRead();
            // A scene from before key sets, made with ShalltyUtils: its Keyframe Groups become sets.
            if (ownKeySets)
                _shalltyGroups = null;
            else
                ImportShalltyGroups();
            ReadPicker(node, dic);
            int split = SplitMixedGroups();
            if (split != 0)
                Logger.LogMessage(split + " group(s) held tracks of more than one character and are now one group per character.");
            if (_orphanTracks.Count != orphansBefore)
                ReportOrphanTracks();
            if (HandleMath.converted != 0)
            {
                // Worth saying out loud, because a scene made before handles existed arrives with every
                // one of them Free. That is deliberate - Free is what keeps the shape exactly as it was
                // authored - but it also means none of them follow a retime until you say so.
                Logger.LogMessage($"{HandleMath.converted} segment(s) from an older scene now have handles, " +
                                  "all of them Free so nothing about the animation changed. Select keyframes and " +
                                  "pick Handle type > Auto to have them look after themselves.");
            }
            // After the tree, so a strip channel finds an existing track instead of creating a duplicate.
            ReadStrips(node, dic);
            ReadMarkers(node);

            if (node.Attributes["duration"] != null)
                _duration = XmlConvert.ToSingle(node.Attributes["duration"].Value);
            else
            {
                _duration = 0f;
                foreach (KeyValuePair<int, Interpolable> pair in _interpolables)
                {
                    KeyValuePair<float, Keyframe> last = pair.Value.keyframes.LastOrDefault();
                    if (_duration < last.Key)
                        _duration = last.Key;
                }
                if (Mathf.Approximately(_duration, 0f))
                    _duration = 10f;
            }
            _blockLength = node.Attributes["blockLength"] != null ? XmlConvert.ToSingle(node.Attributes["blockLength"].Value) : 10f;
            _divisions = node.Attributes["divisions"] != null ? XmlConvert.ToInt32(node.Attributes["divisions"].Value) : 10;
            Time.timeScale = node.Attributes["timeScale"] != null ? XmlConvert.ToSingle(node.Attributes["timeScale"].Value) : 1f;

            if (ConfigAutoplay.Value == Autoplay.Yes)
            {
                Stop();
                Play();
            }
            else if (ConfigAutoplay.Value == Autoplay.No)
            {
                Stop();
            }
            else
            {
                // A scene card restores the pose of whatever frame it was saved at, and interpolation is
                // skipped while paused, so without this the scene sits on one frame and the cursor on another.
                Interpolate(true);
                Interpolate(false);
            }
        }

        private void LoadSingle(string path)
        {
            List<KeyValuePair<int, ObjectCtrlInfo>> dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).ToList();
            XmlDocument document = new XmlDocument();
            try
            {
                document.Load(path);
                BeginKeySetRead(document.FirstChild);
                ReadInterpolableTree(document.FirstChild, dic, _selectedOCI);
                EndKeySetRead();
#if KOIKATSU || SUNSHINE
                // A file of tracks only, as ShalltyUtils saved them, leaves the character's animation be.
                bool hasAnimation = document.FirstChild.Attributes?["animationNo"] != null;
                string docGUID = document.FirstChild.Attributes?["GUID"]?.InnerText;
                int docGr = document.FirstChild.ReadInt("animationGroup");
                int docCa = document.FirstChild.ReadInt("animationCategory");
                int docNo = document.FirstChild.ReadInt("animationNo");
                OCIChar character = _selectedOCI as OCIChar;
                StudioResolveInfo resolveInfo = UniversalAutoResolver.GetStudioResolveInfos(docGUID, docNo, false).FirstOrDefault(x => x.Group == docGr && x.Category == docCa);
                if (character != null && hasAnimation)
                {
                    character.LoadAnime(docGr, docCa, resolveInfo != null ? resolveInfo.LocalSlot : docNo);
                }
#else           //AI&HS2 Studio use original ID(management number) for animation zipmods by default
                OCIChar character = _selectedOCI as OCIChar;
                if (character != null && document.FirstChild.Attributes?["animationNo"] != null)
                {
                    character.LoadAnime(document.FirstChild.ReadInt("animationGroup"),
                            document.FirstChild.ReadInt("animationCategory"),
                            document.FirstChild.ReadInt("animationNo"));
                }
#endif
            }
            catch (Exception e)
            {
                Logger.LogError("Could not load data for OCI.\n" + document.FirstChild + "\n" + e);
            }
            UpdateInterpolablesView();
        }

        private void SaveSingle(string path, bool onlySelectedTracks = false)
        {
            using (XmlTextWriter writer = new XmlTextWriter(path, Encoding.UTF8))
            {
                List<KeyValuePair<int, ObjectCtrlInfo>> dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).ToList();
                writer.WriteStartElement("root");

                OCIChar character = onlySelectedTracks ? null : _selectedOCI as OCIChar;

                if (character != null)
                {
#if KOIKATSU || SUNSHINE
                    OICharInfo.AnimeInfo info = character.oiCharInfo.animeInfo;
                    StudioResolveInfo resolveInfo = UniversalAutoResolver.GetStudioResolveInfos(info.no, false).FirstOrDefault(x => x.Group == info.group && x.Category == info.category);
                    writer.WriteAttributeString("GUID", info.no >= UniversalAutoResolver.BaseSlotID && resolveInfo != null ? resolveInfo.GUID : "");
                    writer.WriteValue("animationGroup", info.group);
                    writer.WriteValue("animationCategory", info.category);
                    writer.WriteValue("animationNo", info.no >= UniversalAutoResolver.BaseSlotID && resolveInfo != null ? resolveInfo.Slot : info.no);
#else           //AI&HS2 Studio use original ID(management number) for animation zipmods by default
                    OICharInfo.AnimeInfo info = character.oiCharInfo.animeInfo;
                    writer.WriteValue("animationCategory", info.category);
                    writer.WriteValue("animationGroup", info.group);
                    writer.WriteValue("animationNo", info.no);
#endif
                }

                foreach (INode node in _interpolablesTree.tree)
                    WriteInterpolableTree(node, writer, dic, leafNode => onlySelectedTracks ? _selectedInterpolables.Contains(leafNode.obj) : leafNode.obj.oci == _selectedOCI);
                WriteKeySets(writer);
                writer.WriteEndElement();
            }
        }

        private void ReadInterpolableTree(XmlNode groupNode, List<KeyValuePair<int, ObjectCtrlInfo>> dic, ObjectCtrlInfo overrideOci = null, GroupNode<InterpolableGroup> group = null)
        {
            foreach (XmlNode interpolableNode in groupNode.ChildNodes)
            {
                switch (interpolableNode.Name)
                {
                    case "interpolable":
                        ReadInterpolable(interpolableNode, dic, overrideOci, group);
                        break;
                    case "interpolableGroup":
                        string groupName = interpolableNode.Attributes["name"].Value;
                        GroupNode<InterpolableGroup> newGroup = _interpolablesTree.AddGroup(new InterpolableGroup { name = groupName }, group);
                        ReadInterpolableTree(interpolableNode, dic, overrideOci, newGroup);
                        break;
                }
            }
        }

        private void WriteInterpolableTree(INode interpolableNode, XmlTextWriter writer, List<KeyValuePair<int, ObjectCtrlInfo>> dic, Func<LeafNode<Interpolable>, bool> predicate = null)
        {
            switch (interpolableNode.type)
            {
                case INodeType.Leaf:
                    LeafNode<Interpolable> leafNode = (LeafNode<Interpolable>)interpolableNode;
                    if (predicate == null || predicate(leafNode))
                        WriteInterpolable(leafNode.obj, writer, dic);
                    break;
                case INodeType.Group:
                    GroupNode<InterpolableGroup> group = (GroupNode<InterpolableGroup>)interpolableNode;
                    bool shouldWriteGroup = true;
                    if (predicate != null)
                        shouldWriteGroup = _interpolablesTree.Any(group, predicate);
                    if (shouldWriteGroup)
                    {
                        writer.WriteStartElement("interpolableGroup");
                        writer.WriteAttributeString("name", group.obj.name);

                        foreach (INode child in group.children)
                            WriteInterpolableTree(child, writer, dic, predicate);

                        writer.WriteEndElement();
                    }
                    break;
            }
        }

        private void ReadInterpolable(XmlNode interpolableNode, List<KeyValuePair<int, ObjectCtrlInfo>> dic, ObjectCtrlInfo overrideOci = null, GroupNode<InterpolableGroup> group = null)
        {
            bool added = false;
            Interpolable interpolable = null;
            try
            {
                if (interpolableNode.Name == "interpolable")
                {
                    string ownerId = interpolableNode.Attributes["owner"].Value;
                    ObjectCtrlInfo oci = null;
                    if (overrideOci != null)
                        oci = overrideOci;
                    else if (interpolableNode.Attributes["objectIndex"] != null)
                    {
                        int objectIndex = XmlConvert.ToInt32(interpolableNode.Attributes["objectIndex"].Value);
                        if (objectIndex >= dic.Count)
                            return;
                        oci = dic[objectIndex].Value;
                    }

                    string id = interpolableNode.Attributes["id"].Value;
                    InterpolableModel model = _interpolableModelsList.Find(i => i.owner == ownerId && i.id == id);
                    if (model == null /*|| model.isCompatibleWithTarget(oci) == false*/)
                    {
                        // Its plugin is missing. Kept rather than dropped, see TimelineOrphans.
                        if (overrideOci == null)
                            KeepOrphanTrack(interpolableNode, oci, ownerId, id);
                        return;
                    }
                    if (model.readParameterFromXml != null)
                        interpolable = new Interpolable(oci, model.readParameterFromXml(oci, interpolableNode), model);
                    else
                        interpolable = new Interpolable(oci, model);

                    interpolable.enabled = interpolableNode.Attributes["enabled"] == null || XmlConvert.ToBoolean(interpolableNode.Attributes["enabled"].Value);

                    if (interpolableNode.Attributes["bgColorR"] != null)
                    {
                        interpolable.color = new Color(
                                XmlConvert.ToSingle(interpolableNode.Attributes["bgColorR"].Value),
                                XmlConvert.ToSingle(interpolableNode.Attributes["bgColorG"].Value),
                                XmlConvert.ToSingle(interpolableNode.Attributes["bgColorB"].Value)
                        );
                    }

                    if (interpolableNode.Attributes["alias"] != null)
                        interpolable.alias = interpolableNode.Attributes["alias"].Value;

                    if (interpolableNode.Attributes["smooth"] != null)
                        interpolable.smooth = XmlConvert.ToBoolean(interpolableNode.Attributes["smooth"].Value);

                    if (interpolableNode.Attributes["extrapolation"] != null)
                    {
                        // Stored by name rather than by number, so reordering the enum one day cannot
                        // silently turn every cyclic track in every saved scene into something else.
                        try
                        {
                            interpolable.extrapolation = (TrackExtrapolation)Enum.Parse(
                                    typeof(TrackExtrapolation), interpolableNode.Attributes["extrapolation"].Value);
                        }
                        catch (Exception)
                        {
                            interpolable.extrapolation = TrackExtrapolation.Hold;
                        }
                    }

                    if (_interpolables.ContainsKey(interpolable.GetHashCode()) == false)
                    {
                        _interpolables.Add(interpolable.GetHashCode(), interpolable);
                        _interpolablesTree.AddLeaf(interpolable, group);
                        added = true;
                        foreach (XmlNode keyframeNode in interpolableNode.ChildNodes)
                        {
                            if (keyframeNode.Name == "keyframe")
                            {
                                float time = XmlConvert.ToSingle(keyframeNode.Attributes["time"].Value);

                                object value = interpolable.ReadValueFromXml(keyframeNode);
                                List<UnityEngine.Keyframe> curveKeys = new List<UnityEngine.Keyframe>();
                                foreach (XmlNode curveKeyNode in keyframeNode.ChildNodes)
                                {
                                    if (curveKeyNode.Name == "curveKeyframe")
                                    {
                                        UnityEngine.Keyframe curveKey = new UnityEngine.Keyframe(
                                                XmlConvert.ToSingle(curveKeyNode.Attributes["time"].Value),
                                                XmlConvert.ToSingle(curveKeyNode.Attributes["value"].Value),
                                                XmlConvert.ToSingle(curveKeyNode.Attributes["inTangent"].Value),
                                                XmlConvert.ToSingle(curveKeyNode.Attributes["outTangent"].Value));
                                        curveKeys.Add(curveKey);
                                    }
                                }

                                AnimationCurve curve;
                                if (curveKeys.Count == 0)
                                    curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
                                else
                                    curve = new AnimationCurve(curveKeys.ToArray());

                                Keyframe keyframe = new Keyframe(value, interpolable, curve);
                                if (keyframeNode.Attributes["kind"] != null)
                                {
                                    try
                                    {
                                        keyframe.kind = (KeyframeKind)Enum.Parse(typeof(KeyframeKind), keyframeNode.Attributes["kind"].Value);
                                    }
                                    catch (Exception)
                                    {
                                        keyframe.kind = KeyframeKind.Keyframe;
                                    }
                                }
                                ReadHandles(keyframe, keyframeNode);
                                ReadKeySetOf(keyframe, keyframeNode);
                                interpolable.keyframes.Add(time, keyframe);
                            }
                        }

                        // Scenes made before handles existed are converted here, exactly: an easing
                        // curve is a cubic and a Bezier with its handles a third of the way along is the
                        // same cubic, so nothing about the animation changes and everything after this
                        // point is working in handles.
                        HandleMath.Convert(interpolable.keyframes);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError("Couldn't load interpolable with the following XML:\n" + interpolableNode.OuterXml + "\n" + e);
                if (added)
                    RemoveInterpolable(interpolable);
            }
        }

        /// <summary>
        /// A keyframe's handles, alongside the easing curve rather than instead of it.
        ///
        /// Both forms are written on purpose. The handles are what this Timeline reads back; the easing
        /// curve, rebuilt from them just before saving, is what a Timeline that predates handles reads,
        /// and it ignores these attributes without complaint. The one thing it cannot reproduce is a
        /// handle dragged along the time axis, so what it plays there is an approximation.
        /// </summary>
        private static void WriteHandles(Keyframe keyframe, XmlTextWriter writer)
        {
            if (keyframe.handles == null)
                return;
            if (keyframe.shapedByCurve)
                writer.WriteAttributeString("shapedByCurve", XmlConvert.ToString(true));

            // Both types in one attribute rather than two. It doubles as the marker that says this
            // keyframe has handles at all, which is why it is written even when both are the default:
            // without it a keyframe would come back from a scene as Free, since that is what reading an
            // easing curve produces, and the whole point of an automatic handle is that it keeps
            // working after a neighbour moves.
            writer.WriteAttributeString("handleTypes", keyframe.handles.leftType + " " + keyframe.handles.rightType);

            // Only the two types that remember where they were put are worth the bytes. The other three
            // are worked out from the neighbours every time, so writing them down would be writing down
            // something that is about to be recalculated anyway.
            if (Stored(keyframe.handles.leftType) == false && Stored(keyframe.handles.rightType) == false)
                return;

            var text = new StringBuilder(64);
            for (int i = 0; i < keyframe.handles.left.Length; ++i)
            {
                if (i != 0)
                    text.Append(' ');
                text.Append(XmlConvert.ToString(keyframe.handles.left[i].x)).Append(',')
                    .Append(XmlConvert.ToString(keyframe.handles.left[i].y)).Append(',')
                    .Append(XmlConvert.ToString(keyframe.handles.right[i].x)).Append(',')
                    .Append(XmlConvert.ToString(keyframe.handles.right[i].y));
            }
            writer.WriteAttributeString("handles", text.ToString());
        }

        private static bool Stored(HandleType type)
        {
            return type == HandleType.Free || type == HandleType.Aligned;
        }

        private static void ReadHandles(Keyframe keyframe, XmlNode node)
        {
            if (node.Attributes["handleTypes"] == null)
                return;

            int slots = HandleMath.Slots(keyframe.value);
            if (slots == 0)
                return; // a pose or an animation, which has no curve to put handles on

            keyframe.handles = new KeyframeHandles(slots);
            string[] types = node.Attributes["handleTypes"].Value.Split(' ');
            keyframe.handles.leftType = ParseHandleType(types.Length > 0 ? types[0] : null);
            keyframe.handles.rightType = ParseHandleType(types.Length > 1 ? types[1] : null);
            keyframe.shapedByCurve = node.Attributes["shapedByCurve"] != null;

            if (node.Attributes["handles"] == null)
                return;
            string[] parts = node.Attributes["handles"].Value.Split(' ');
            for (int i = 0; i < parts.Length && i < slots; ++i)
            {
                string[] numbers = parts[i].Split(',');
                if (numbers.Length != 4)
                    continue;
                try
                {
                    keyframe.handles.left[i] = new Vector2(XmlConvert.ToSingle(numbers[0]), XmlConvert.ToSingle(numbers[1]));
                    keyframe.handles.right[i] = new Vector2(XmlConvert.ToSingle(numbers[2]), XmlConvert.ToSingle(numbers[3]));
                }
                catch (Exception)
                {
                    // A malformed handle falls back to zero, which reads as a flat one rather than as a
                    // broken scene.
                }
            }
        }

        private static HandleType ParseHandleType(string text)
        {
            if (string.IsNullOrEmpty(text))
                return HandleType.Auto;
            try
            {
                return (HandleType)Enum.Parse(typeof(HandleType), text);
            }
            catch (Exception)
            {
                return HandleType.Auto;
            }
        }

        private void WriteInterpolable(Interpolable interpolable, XmlTextWriter writer, List<KeyValuePair<int, ObjectCtrlInfo>> dic)
        {
            if (interpolable.keyframes.Count == 0)
                return;
            // The easing curves are what an older Timeline will read, so they are brought back in line
            // with the handles before anything is written.
            HandleMath.SyncEasing(interpolable.keyframes);
            using (StringWriter stream = new StringWriter())
            {
                using (XmlTextWriter localWriter = new XmlTextWriter(stream))
                {
                    try
                    {
                        int objectIndex = -1;
                        if (interpolable.oci != null)
                        {
                            objectIndex = dic.FindIndex(e => e.Value == interpolable.oci);
                            if (objectIndex == -1)
                                return;
                        }

                        localWriter.WriteStartElement("interpolable");
                        localWriter.WriteAttributeString("enabled", XmlConvert.ToString(interpolable.enabled));
                        localWriter.WriteAttributeString("owner", interpolable.owner);
                        if (objectIndex != -1)
                            localWriter.WriteAttributeString("objectIndex", XmlConvert.ToString(objectIndex));
                        localWriter.WriteAttributeString("id", interpolable.id);

                        if (interpolable.writeParameterToXml != null)
                            interpolable.writeParameterToXml(interpolable.oci, localWriter, interpolable.parameter);
                        localWriter.WriteAttributeString("bgColorR", XmlConvert.ToString(interpolable.color.r));
                        localWriter.WriteAttributeString("bgColorG", XmlConvert.ToString(interpolable.color.g));
                        localWriter.WriteAttributeString("bgColorB", XmlConvert.ToString(interpolable.color.b));

                        localWriter.WriteAttributeString("alias", interpolable.alias);

                        // Only written when set, so scenes made before smoothing existed stay byte for
                        // byte the same and older Timeline versions ignore the attribute.
                        if (interpolable.smooth)
                            localWriter.WriteAttributeString("smooth", XmlConvert.ToString(true));

                        if (interpolable.extrapolation != TrackExtrapolation.Hold)
                            localWriter.WriteAttributeString("extrapolation", interpolable.extrapolation.ToString());

                        foreach (KeyValuePair<float, Keyframe> keyframePair in interpolable.keyframes)
                        {
                            localWriter.WriteStartElement("keyframe");
                            localWriter.WriteAttributeString("time", XmlConvert.ToString(keyframePair.Key));
                            // Only written when marked, so the common case costs a scene nothing.
                            if (keyframePair.Value.kind != KeyframeKind.Keyframe)
                                localWriter.WriteAttributeString("kind", keyframePair.Value.kind.ToString());
                            WriteKeySetOf(keyframePair.Value, localWriter);

                            interpolable.WriteValueToXml(localWriter, keyframePair.Value.value);
                            WriteHandles(keyframePair.Value, localWriter);
                            foreach (UnityEngine.Keyframe curveKey in keyframePair.Value.curve.keys)
                            {
                                localWriter.WriteStartElement("curveKeyframe");
                                localWriter.WriteAttributeString("time", XmlConvert.ToString(curveKey.time));
                                localWriter.WriteAttributeString("value", XmlConvert.ToString(curveKey.value));
                                localWriter.WriteAttributeString("inTangent", XmlConvert.ToString(curveKey.inTangent));
                                localWriter.WriteAttributeString("outTangent", XmlConvert.ToString(curveKey.outTangent));
                                localWriter.WriteEndElement();
                            }

                            localWriter.WriteEndElement();
                        }

                        localWriter.WriteEndElement();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError("Couldn't save interpolable with the following value:\n" + interpolable + "\n" + e);
                        return;
                    }
                }
                writer.WriteRaw(stream.ToString());
            }
        }

        private void OnDuplicate(ObjectCtrlInfo source, ObjectCtrlInfo destination)
        {
            this.ExecuteDelayed2(() =>
            {
                List<KeyValuePair<int, ObjectCtrlInfo>> dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).ToList();

                using (StringWriter stream = new StringWriter())
                {
                    using (XmlTextWriter writer = new XmlTextWriter(stream))
                    {
                        writer.WriteStartElement("root");

                        foreach (INode node in _interpolablesTree.tree)
                            WriteInterpolableTree(node, writer, dic, leafNode => leafNode.obj.oci == source);

                        writer.WriteEndElement();
                    }

                    try
                    {
                        XmlDocument document = new XmlDocument();
                        document.LoadXml(stream.ToString());

                        ReadInterpolableTree(document.FirstChild, dic, destination);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError("Could not duplicate data for OCI.\n" + stream + "\n" + e);
                    }

                }
            }, 20);
        }
        #endregion

        #region Patches
#if HONEYSELECT
        [HarmonyPatch(typeof(Expression), "Start")]
#elif KOIKATSU
        [HarmonyPatch(typeof(Expression), "Initialize")]
#endif
        private static class Expression_Start_Patches
        {
            private static void Prefix(Expression __instance)
            {
                _self._allExpressions.Add(__instance);
            }
        }

        [HarmonyPatch(typeof(Expression), "OnDestroy")]
        private static class Expression_OnDestroy_Patches
        {
            private static void Prefix(Expression __instance)
            {
                _self._allExpressions.Remove(__instance);
            }
        }

        [HarmonyPatch(typeof(Expression), "LateUpdate"), HarmonyBefore("com.joan6694.illusionplugins.nodesconstraints")]
        private static class Expression_LateUpdate_Patches
        {
            private static void Postfix()
            {
                _self._currentExpressionIndex++;
                if (_self._currentExpressionIndex == _self._totalActiveExpressions)
                    _self.PostLateUpdate();
            }
        }

        [HarmonyPatch(typeof(Studio.Studio), "Duplicate")]
        private class Studio_Duplicate_Patches
        {
            public static void Postfix(Studio.Studio __instance)
            {
                foreach (KeyValuePair<int, int> pair in SceneInfo_Import_Patches._newToOldKeys)
                {
                    ObjectCtrlInfo source;
                    if (__instance.dicObjectCtrl.TryGetValue(pair.Value, out source) == false)
                        continue;
                    ObjectCtrlInfo destination;
                    if (__instance.dicObjectCtrl.TryGetValue(pair.Key, out destination) == false)
                        continue;
                    if (source is OCIChar && destination is OCIChar || source is OCIItem && destination is OCIItem)
                        _self.OnDuplicate(source, destination);
                }
            }
        }

        [HarmonyPatch(typeof(ObjectInfo), "Load", new[] { typeof(BinaryReader), typeof(Version), typeof(bool), typeof(bool) })]
        private static class ObjectInfo_Load_Patches
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                int count = 0;
                List<CodeInstruction> instructionsList = instructions.ToList();
                foreach (CodeInstruction inst in instructionsList)
                {
                    yield return inst;
                    if (count != 2 && inst.ToString().Contains("ReadInt32"))
                    {
                        ++count;
                        if (count == 2)
                        {
                            yield return new CodeInstruction(OpCodes.Ldarg_0);
                            yield return new CodeInstruction(OpCodes.Call, typeof(ObjectInfo_Load_Patches).GetMethod(nameof(Injected), BindingFlags.NonPublic | BindingFlags.Static));
                        }
                    }
                }
            }

            private static int Injected(int originalIndex, ObjectInfo __instance)
            {
                SceneInfo_Import_Patches._newToOldKeys.Add(__instance.dicKey, originalIndex);
                return originalIndex; //Doing this so other transpilers can use this value if they want
            }
        }

        [HarmonyPatch(typeof(SceneInfo), "Import", new[] { typeof(BinaryReader), typeof(Version) })]
        private static class SceneInfo_Import_Patches //This is here because I fucked up the save format making it impossible to import scenes correctly
        {
            internal static readonly Dictionary<int, int> _newToOldKeys = new Dictionary<int, int>();

            private static void Prefix()
            {
                _newToOldKeys.Clear();
            }
        }
        
        [HarmonyPatch(typeof(Studio.Studio), "InitScene", typeof(bool))]
        private static class Studio_InitScene_Patches
        {
            private static void Postfix()
            {
                _self.SceneLoad(null, new XmlDocument().CreateElement("root"));
            }
        }

        private static void OnGuideClick()
        {
            var manager = GuideObjectManager.Instance;
            GuideObject go = manager.selectObject;

            // Alt still forces it, so the shortcut keeps working when the setting is off.
            if (go == null || _self._loaded == false)
                return;
            if (Input.GetKey(KeyCode.LeftAlt) == false && ConfigSyncSelection.Value == false)
                return;

            var interpolables = _self._interpolables.Where(i => i.Value.parameter is GuideObject g && g == go).Select( pair => pair.Value ).ToArray();

            if (interpolables.Length <= 0)
                return;

            int select = 0;

            if(interpolables.Length > 1)
            {
                //If there is a mode selected in the studio, select that interpolation.
                string keyword = null;

                switch(manager.mode)
                {
                    case 0:
                        keyword = "Position";
                        break;

                    case 1:
                        keyword = "Rotation";
                        break;

                    case 2:
                        keyword = "Scale";
                        break;
                }

                if( keyword != null )
                {
                    for( int i = 0; i < interpolables.Length; ++i )
                        if( interpolables[i].name.Contains(keyword) )
                        {
                            select = i;
                            break;
                        }
                }
            }

            _self.SelectInterpolable(interpolables[select]);
            if (_self._view != null)
                _self._view.Reveal(interpolables[select]);
        }

        [HarmonyPatch(typeof(GuideSelect), nameof(GuideSelect.OnPointerClick), new[] { typeof(PointerEventData) })]
        private static class GuideSelect_OnPointerClick_Patches
        {
            private static void Postfix() => OnGuideClick();
        }

        [HarmonyPatch(typeof(GuideMove), nameof(GuideMove.OnPointerDown), new[] { typeof(PointerEventData) })]
        private static class GuideMove_OnPointerDown_Patches
        {
            private static void Postfix() => OnGuideClick();
        }

        [HarmonyPatch(typeof(GuideRotation), nameof(GuideRotation.OnPointerDown), new[] { typeof(PointerEventData) })]
        private static class GuideRotation_OnPointerDown_Patches
        {
            private static void Postfix() => OnGuideClick();
        }

        [HarmonyPatch(typeof(GuideScale), nameof(GuideScale.OnPointerDown), new[] { typeof(PointerEventData) })]
        private static class GuideScale_OnPointerDown_Patches
        {
            private static void Postfix() => OnGuideClick();
        }

        private static class OCI_OnDelete_Patches
        {
#if IPA
            public static void ManualPatch(HarmonyInstance harmony)
#elif BEPINEX
            public static void ManualPatch(Harmony harmony)
#endif
            {
                IEnumerable<Type> ociTypes = Assembly.GetAssembly(typeof(ObjectCtrlInfo)).GetTypes().Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(ObjectCtrlInfo)));

                foreach (Type t in ociTypes)
                {
                    try
                    {
                        harmony.Patch(t.GetMethod("OnDelete", AccessTools.all), new HarmonyMethod(typeof(OCI_OnDelete_Patches).GetMethod(nameof(Prefix), BindingFlags.NonPublic | BindingFlags.Static)));
                    }
                    catch (Exception e)
                    {
                        Logger.LogWarning("Could not patch OnDelete of type " + t.Name + "\n" + e);
                    }
                }
            }

            private static void Prefix(object __instance)
            {
                ObjectCtrlInfo oci = __instance as ObjectCtrlInfo;
                if (oci != null)
                    _self.RemoveInterpolables(_self._interpolables.Where(i => i.Value.oci == oci).Select(i => i.Value).ToList());
            }
        }

        [HarmonyPatch(typeof(WorkspaceCtrl), nameof(WorkspaceCtrl.OnClickDelete))]
        internal static class WorkspaceCtrl_OnClickDelete_Patches
        {
            private static bool Prefix()
            {
                // Prevent people from deleting objects in studio workspace by accident while timeline window is in focus
                if (Input.GetKey(KeyCode.Delete))
                    return !_self.UiVisible;
                return true;
            }
        }

#if KOIKATSU
        [HarmonyPatch(typeof(ShortcutKeyCtrl), "Update")]
        private static class ShortcutKeyCtrl_Update_Patches
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> instructionList = instructions.ToList();
                for (int i = 0; i < instructionList.Count; i++)
                {
                    CodeInstruction instruction = instructionList[i];
                    if (i != 0 && instruction.opcode == OpCodes.Call && instructionList[i - 1].opcode == OpCodes.Ldc_I4_S && (sbyte)instructionList[i - 1].operand == 99)
                        yield return new CodeInstruction(OpCodes.Call, typeof(ShortcutKeyCtrl_Update_Patches).GetMethod(nameof(PreventKeyIfCtrl), BindingFlags.NonPublic | BindingFlags.Static));
                    else
                        yield return instruction;
                }
            }

            private static bool PreventKeyIfCtrl(KeyCode key)
            {
                return Input.GetKey(KeyCode.LeftControl) == false && Input.GetKeyDown(key);
            }
        }
#endif
        #endregion
    }
}
