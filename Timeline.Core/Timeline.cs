using Studio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Xml;
using BepInEx.Logging;
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
#elif AISHOUJO || HONEYSELECT2
using CharaUtils;
using ExtensibleSaveFormat;
#endif

namespace Timeline
{
#if BEPINEX
    [BepInPlugin(GUID, Name, Version)]
#if KOIKATSU || SUNSHINE
    [BepInProcess("CharaStudio")]
#elif AISHOUJO || HONEYSELECT2
    [BepInProcess("StudioNEOV2")]
#endif
    [BepInDependency("com.bepis.bepinex.extendedsave")]
#endif
    public class Timeline : GenericPlugin
#if IPA
                            , IEnhancedPlugin
#endif
    {
        #region Constants
        public const string Name = "Timeline";
        public const string Version = "1.1.5";
        public const string GUID = "com.joan6694.illusionplugins.timeline";
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
        private class HeaderDisplay
        {
            public GameObject gameObject;
            public LayoutElement layoutElement;
            public RectTransform container;
            public Text name;
            public InputField inputField;

            public bool expanded = true;
            public GroupNode<InterpolableGroup> group;
        }

        private class InterpolableDisplay
        {
            public GameObject gameObject;
            public RectTransform container;
            public CanvasGroup group;
            public Toggle enabled;
            public Text name;
            public InputField inputField;
            public Image background;
            public Image selectedOutline;
            public RawImage gridBackground;

            public LeafNode<Interpolable> interpolable;
        }

        private class InterpolableModelDisplay
        {
            public GameObject gameObject;
            public Text name;

            public InterpolableModel model;
        }

        private class KeyframeDisplay
        {
            public GameObject gameObject;
            public RawImage image;

            public Keyframe keyframe;
        }

        private class CurveKeyframeDisplay
        {
            public GameObject gameObject;
            public RawImage image;
            public PointerDownHandler pointerDownHandler;
            public ScrollHandler scrollHandler;
            public DragHandler dragHandler;
            public PointerEnterHandler pointerEnterHandler;
        }

        private class SingleFileDisplay
        {
            public Toggle toggle;
            public Text text;
        }

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
        private readonly Tree<Interpolable, InterpolableGroup> _interpolablesTree = new Tree<Interpolable, InterpolableGroup>();

        private const float _baseGridWidth = 300f;
        private const int _interpolableHeight = 32;
        private const float _curveGridCellSizePercent = 1f / 24f;
        private Canvas _ui;
        private Sprite _linkSprite;
        private Sprite _colorSprite;
        private Sprite _renameSprite;
        private Sprite _newFolderSprite;
        private Sprite _addSprite;
        private Sprite _addToFolderSprite;
        private Sprite _chevronUpSprite;
        private Sprite _chevronDownSprite;
        private Sprite _deleteSprite;
        private Sprite _checkboxSprite;
        private Sprite _checkboxCompositeSprite;
        private Sprite _selectAllSprite;

        private RectTransform _timelineWindow;
        private GameObject _helpPanel;
        private RectTransform _cursor;
        private RectTransform _grid;
        private RawImage _gridImage;
        private RectTransform _gridTop;
        private bool _isDraggingCursor;
        private ScrollRect _verticalScrollView;
        private ScrollRect _horizontalScrollView;
        private Toggle _allToggle;
        private InputField _interpolablesSearchField;
        private InputField _frameRateInputField;
        private InputField _timeInputField;
        private InputField _durationInputField;
        private InputField _blockLengthInputField;
        private InputField _divisionsInputField;
        private InputField _speedInputField;
        private GameObject _singleFilePrefab;
        private GameObject _singleFilesPanel;
        private RectTransform _singleFilesContainer;
        private InputField _singleFileNameField;
        private readonly List<SingleFileDisplay> _displayedSingleFiles = new List<SingleFileDisplay>();
        private float _zoomLevel = 1f;
        private RectTransform _textsContainer;
        private readonly List<Text> _timeTexts = new List<Text>();
        private RectTransform _resizeHandle;
        private GameObject _keyframeWindow;
        private Text _keyframeInterpolableNameText;
        private Button _keyframeSelectPrevButton;
        private Button _keyframeSelectNextButton;
        private InputField _keyframeTimeTextField;
        private Button _keyframeUseCurrentTimeButton;
        private Text _keyframeValueText;
        private Button _keyframeUseCurrentValueButton;
        private Text _keyframeDeleteButtonText;
        private GameObject _headerPrefab;
        private readonly List<HeaderDisplay> _displayedOwnerHeader = new List<HeaderDisplay>();
        private GameObject _interpolablePrefab;
        private GameObject _interpolableModelPrefab;
        private readonly List<InterpolableDisplay> _displayedInterpolables = new List<InterpolableDisplay>();
        private readonly List<InterpolableModelDisplay> _displayedInterpolableModels = new List<InterpolableModelDisplay>();
        private readonly List<float> _gridHeights = new List<float>();
        private readonly List<RawImage> _interpolableSeparators = new List<RawImage>();
        private RectTransform _keyframesContainer;
        private RectTransform _miscContainer;
        private GameObject _keyframePrefab;
        private readonly List<KeyframeDisplay> _displayedKeyframes = new List<KeyframeDisplay>();
        private Material _keyframesBackgroundMaterial;
        private Text _tooltip;
        private GameObject _curveKeyframePrefab;
        private RawImage _curveContainer;
        private readonly Texture2D _curveTexture = new Texture2D(512, 1, TextureFormat.RFloat, false, true);
        private InputField _curveTimeInputField;
        private Slider _curveTimeSlider;
        private InputField _curveValueInputField;
        private Slider _curveValueSlider;
        private InputField _curveInTangentInputField;
        private Slider _curveInTangentSlider;
        private InputField _curveOutTangentInputField;
        private Slider _curveOutTangentSlider;
        private RectTransform _cursor2;
        private readonly List<CurveKeyframeDisplay> _displayedCurveKeyframes = new List<CurveKeyframeDisplay>();
        private readonly AnimationCurve _linePreset = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        private readonly AnimationCurve _topPreset = new AnimationCurve(new UnityEngine.Keyframe(0f, 0f, 2f, 2f), new UnityEngine.Keyframe(1f, 1f, 0f, 0f));
        private readonly AnimationCurve _bottomPreset = new AnimationCurve(new UnityEngine.Keyframe(0f, 0f, 0f, 0f), new UnityEngine.Keyframe(1f, 1f, 2f, 2f));
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
        private readonly List<KeyValuePair<float, Keyframe>> _copiedKeyframes = new List<KeyValuePair<float, Keyframe>>();
        private readonly List<KeyValuePair<float, Keyframe>> _cutKeyframes = new List<KeyValuePair<float, Keyframe>>();
        private readonly Dictionary<KeyframeDisplay, float> _selectedKeyframesXOffset = new Dictionary<KeyframeDisplay, float>();
        private double _keyframeSelectionSize;
        private int _selectedKeyframeCurvePointIndex = -1;
        private ObjectCtrlInfo _selectedOCI;
        private GuideObject _selectedGuideObject;
        private readonly AnimationCurve _copiedKeyframeCurve = new AnimationCurve();

        private bool _isAreaSelecting;
        private Vector2 _areaSelectFirstPoint;
        private RectTransform _selectionArea;
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
                    TimelineButton.UpdateButton();
                }
            }
        }
        #endregion

        internal static ConfigEntry<KeyboardShortcut> ConfigMainWindowShortcut { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> ConfigPlayPauseShortcut { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> ConfigKeyframeCopyShortcut { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> ConfigKeyframeCutShortcut { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> ConfigKeyframePasteShortcut { get; private set; }


        #region Unity Methods
        protected override void Awake()
        {
            base.Awake();

            ConfigMainWindowShortcut = Config.Bind("Config", "Open Timeline UI", new KeyboardShortcut(KeyCode.T, KeyCode.LeftControl));
            ConfigPlayPauseShortcut = Config.Bind("Config", "Play or Pause Timeline", new KeyboardShortcut(KeyCode.T, KeyCode.LeftShift));
            ConfigKeyframeCopyShortcut = Config.Bind("Config", "Copy Keyframes", new KeyboardShortcut(KeyCode.C, KeyCode.LeftControl));
            ConfigKeyframeCutShortcut = Config.Bind("Config", "Cut Keyframes", new KeyboardShortcut(KeyCode.X, KeyCode.LeftControl));
            ConfigKeyframePasteShortcut = Config.Bind("Config", "PasteKeyframes", new KeyboardShortcut(KeyCode.V, KeyCode.LeftControl));

            _self = this;
            Logger = base.Logger;

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
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
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

            if (ConfigMainWindowShortcut.Value.IsDown())
            {
                ToggleUiVisible();
            }
            if (ConfigPlayPauseShortcut.Value.IsDown())
            {
                if (_isPlaying)
                    Pause();
                else
                    Play();
            }

            _totalActiveExpressions = _allExpressions.Count(e => e.enabled && e.gameObject.activeInHierarchy);
            _currentExpressionIndex = 0;

            //This bullshit is obligatory because when a node is selected on an object that is not selected in the workspace, the selected object doesn't get switched.
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
                    UpdateKeyframeWindow(false);
                }
            }

            if (_toDelete.Count != 0)
            {
                RemoveInterpolables(_toDelete);
                _toDelete.Clear();
            }
            if (_tooltip.transform.parent.gameObject.activeSelf)
            {
                Vector2 localPoint;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)_tooltip.transform.parent.parent, Input.mousePosition, _ui.worldCamera, out localPoint))
                    _tooltip.transform.parent.position = _tooltip.transform.parent.parent.TransformPoint(localPoint);
            }

            if (_ui.gameObject.activeSelf)
            {
                if (ConfigKeyframeCopyShortcut.Value.IsDown())
                    CopyKeyframes();
                else if (ConfigKeyframeCutShortcut.Value.IsDown())
                    CutKeyframes();
                else if (ConfigKeyframePasteShortcut.Value.IsDown())
                    PasteKeyframes();

                if (_speedInputField.isFocused == false)
                    _speedInputField.text = Time.timeScale.ToString("0.#####");
            }

            InterpolateBefore();

            TimelineButton.OnUpdate();
        }

        private void ToggleUiVisible()
        {
            _ui.gameObject.SetActive(!_ui.gameObject.activeSelf);
            if (_ui.gameObject.activeSelf)
                this.ExecuteDelayed2(() =>
                {
                    UpdateInterpolablesView();
                    this.ExecuteDelayed2(
                        () => // I know that's weird but it prevents the grid sometimes disappearing, fuck unity 5.3 I guess
                        {
                            _grid.parent.gameObject.SetActive(false);
                            _grid.parent.gameObject.SetActive(true);
                            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)_grid.parent);
                        }, 4);
                }, 2);
            else
            {
                UIUtility.HideContextMenu();
                TimelineButton.UpdateButton();
            }
        }

        private void PostLateUpdate()
        {
            if (_ui.gameObject.activeSelf && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(2)) && UIUtility.IsContextMenuDisplayed() && UIUtility.WasClickInContextMenu() == false)
            {
                UIUtility.HideContextMenu();
                TimelineButton.UpdateButton();
            }

            InterpolateAfter();
        }
        #endregion

        #region Public Methods
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

        public static void Pause()
        {
            isPlaying = false;
        }

        public static void Stop()
        {
            _self._playbackTime = 0f;
            _self.UpdateCursor();
            _self.Interpolate(true);
            _self.Interpolate(false);
            isPlaying = false;
        }

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
                Interpolable interpolable = new Interpolable(_selectedOCI, model);
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

        private void Init()
        {
            UIUtility.Init();

            BuiltInInterpolables.Populate();

            if (Camera.main.GetComponent<Expression>() == null)
                Camera.main.gameObject.AddComponent<Expression>();
            _allGuideObjects = (Dictionary<Transform, GuideObject>)GuideObjectManager.Instance.GetPrivate("dicGuideObject");
            _selectedGuideObjects = (HashSet<GuideObject>)GuideObjectManager.Instance.GetPrivate("hashSelectObject");
#if HONEYSELECT
            AssetBundle bundle = AssetBundle.LoadFromMemory(Assembly.GetExecutingAssembly().GetResource("Timeline.Resources.TimelineResources.unity3d"));
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
            AssetBundle bundle = AssetBundle.LoadFromMemory(Assembly.GetExecutingAssembly().GetResource("Timeline.Resources.TimelineResourcesKoi.unity3d"));
#endif
            GameObject uiPrefab = bundle.LoadAsset<GameObject>("Canvas");
            _ui = GameObject.Instantiate(uiPrefab).GetComponent<Canvas>();
            CanvasGroup alphaGroup = _ui.GetComponent<CanvasGroup>();
            uiPrefab.hideFlags |= HideFlags.HideInHierarchy;
            _keyframePrefab = bundle.LoadAsset<GameObject>("Keyframe");
            _keyframePrefab.hideFlags |= HideFlags.HideInHierarchy;
            _keyframesBackgroundMaterial = bundle.LoadAsset<Material>("KeyframesBackground");
            _interpolablePrefab = bundle.LoadAsset<GameObject>("Interpolable");
            _interpolablePrefab.hideFlags |= HideFlags.HideInHierarchy;
            _interpolableModelPrefab = bundle.LoadAsset<GameObject>("InterpolableModel");
            _interpolableModelPrefab.hideFlags |= HideFlags.HideInHierarchy;
            _curveKeyframePrefab = bundle.LoadAsset<GameObject>("CurveKeyframe");
            _curveKeyframePrefab.hideFlags |= HideFlags.HideInHierarchy;
            _headerPrefab = bundle.LoadAsset<GameObject>("Header");
            _headerPrefab.hideFlags |= HideFlags.HideInHierarchy;
            _singleFilePrefab = bundle.LoadAsset<GameObject>("SingleFile");
            _singleFilePrefab.hideFlags |= HideFlags.HideInHierarchy;

            _ui.transform.Find("Timeline Window/Help Panel/Main Container/Scroll View/Viewport/Content/Text").GetComponent<Text>().text = System.Text.Encoding.Default.GetString(Assembly.GetExecutingAssembly().GetResource("Timeline.Resources.Help.txt"));

            foreach (Sprite sprite in bundle.LoadAllAssets<Sprite>())
            {
                switch (sprite.name)
                {
                    case "Link":
                        _linkSprite = sprite;
                        break;
                    case "Color":
                        _colorSprite = sprite;
                        break;
                    case "Rename":
                        _renameSprite = sprite;
                        break;
                    case "NewFolder":
                        _newFolderSprite = sprite;
                        break;
                    case "Add":
                        _addSprite = sprite;
                        break;
                    case "AddToFolder":
                        _addToFolderSprite = sprite;
                        break;
                    case "ChevronUp":
                        _chevronUpSprite = sprite;
                        break;
                    case "ChevronDown":
                        _chevronDownSprite = sprite;
                        break;
                    case "Delete":
                        _deleteSprite = sprite;
                        break;
                    case "Checkbox":
                        _checkboxSprite = sprite;
                        break;
                    case "CheckboxComposite":
                        _checkboxCompositeSprite = sprite;
                        break;
                    case "SelectAll":
                        _selectAllSprite = sprite;
                        break;
                }
            }

            bundle.Unload(false);

            _tooltip = _ui.transform.Find("Tooltip/Text").GetComponent<Text>();

            //Timeline window
            _timelineWindow = (RectTransform)_ui.transform.Find("Timeline Window");
            UIUtility.MakeObjectDraggable((RectTransform)_ui.transform.Find("Timeline Window/Top Container"), _timelineWindow, (RectTransform)_ui.transform);
            _helpPanel = _ui.transform.Find("Timeline Window/Help Panel").gameObject;
            _singleFilesPanel = _ui.transform.Find("Timeline Window/Single Files Panel").gameObject;
            _singleFilesContainer = (RectTransform)_singleFilesPanel.transform.Find("Main Container/Scroll View/Viewport/Content");
            _singleFileNameField = _singleFilesPanel.transform.Find("Main Container/Buttons/Name").GetComponent<InputField>();
            _verticalScrollView = _ui.transform.Find("Timeline Window/Main Container/Timeline/Interpolables").GetComponent<ScrollRect>();
            _horizontalScrollView = _ui.transform.Find("Timeline Window/Main Container/Timeline/Scroll View").GetComponent<ScrollRect>();
            _allToggle = _ui.transform.Find("Timeline Window/Main Container/Timeline/Interpolables/Top/All").GetComponent<Toggle>();
            _interpolablesSearchField = _ui.transform.Find("Timeline Window/Main Container/Search").GetComponent<InputField>();
            _grid = (RectTransform)_ui.transform.Find("Timeline Window/Main Container/Timeline/Scroll View/Viewport/Content/Grid Container");
            _gridImage = _ui.transform.Find("Timeline Window/Main Container/Timeline/Scroll View/Viewport/Content/Grid Container/Grid/Viewport/Background").GetComponent<RawImage>();
            _gridImage.material = new Material(_gridImage.material);
            _gridTop = (RectTransform)_ui.transform.Find("Timeline Window/Main Container/Timeline/Scroll View/Viewport/Content/Grid Container/Texts/Background");
            _cursor = (RectTransform)_ui.transform.Find("Timeline Window/Main Container/Timeline/Scroll View/Viewport/Content/Grid Container/Cursor");
            _frameRateInputField = _ui.transform.Find("Timeline Window/Buttons/Play Buttons/FrameRate").GetComponent<InputField>();
            _timeInputField = _ui.transform.Find("Timeline Window/Buttons/Time").GetComponent<InputField>();
            _blockLengthInputField = _ui.transform.Find("Timeline Window/Buttons/Block Divisions/Block Length").GetComponent<InputField>();
            _divisionsInputField = _ui.transform.Find("Timeline Window/Buttons/Block Divisions/Divisions").GetComponent<InputField>();
            _durationInputField = _ui.transform.Find("Timeline Window/Buttons/Duration").GetComponent<InputField>();
            _speedInputField = _ui.transform.Find("Timeline Window/Buttons/Speed").GetComponent<InputField>();
            _textsContainer = (RectTransform)_ui.transform.Find("Timeline Window/Main Container/Timeline/Scroll View/Viewport/Content/Grid Container/Texts");
            _keyframesContainer = (RectTransform)_ui.transform.Find("Timeline Window/Main Container/Timeline/Scroll View/Viewport/Content/Grid Container/Grid/Viewport/Content");
            _selectionArea = (RectTransform)_ui.transform.Find("Timeline Window/Main Container/Timeline/Scroll View/Viewport/Content/Grid Container/Grid/Viewport/Content/Selection");
            _miscContainer = (RectTransform)_ui.transform.Find("Timeline Window/Main Container/Timeline/Scroll View/Viewport/Content/Grid Container/Grid/Viewport/Misc Content");
            _resizeHandle = (RectTransform)_ui.transform.Find("Timeline Window/Resize Handle");

            _ui.transform.Find("Timeline Window/Buttons/Play Buttons/Play").GetComponent<Button>().onClick.AddListener(Play);
            _ui.transform.Find("Timeline Window/Buttons/Play Buttons/Pause").GetComponent<Button>().onClick.AddListener(Pause);
            _ui.transform.Find("Timeline Window/Buttons/Play Buttons/Stop").GetComponent<Button>().onClick.AddListener(Stop);
            _ui.transform.Find("Timeline Window/Buttons/Play Buttons/PrevFrame").GetComponent<Button>().onClick.AddListener(PreviousFrame);
            _ui.transform.Find("Timeline Window/Buttons/Play Buttons/NextFrame").GetComponent<Button>().onClick.AddListener(NextFrame);
            _ui.transform.Find("Timeline Window/Buttons/Single Files").GetComponent<Button>().onClick.AddListener(ToggleSingleFilesPanel);
            _singleFileNameField.onValueChanged.AddListener((s) => UpdateSingleFileSelection());
            _singleFilesPanel.transform.Find("Main Container/Buttons/Load").GetComponent<Button>().onClick.AddListener(LoadSingleFile);
            _singleFilesPanel.transform.Find("Main Container/Buttons/Save").GetComponent<Button>().onClick.AddListener(SaveSingleFile);
            _singleFilesPanel.transform.Find("Main Container/Buttons/Delete").GetComponent<Button>().onClick.AddListener(DeleteSingleFile);
            _ui.transform.Find("Timeline Window/Buttons/Help").GetComponent<Button>().onClick.AddListener(ToggleHelp);

            _frameRateInputField.onEndEdit.AddListener(UpdateDesiredFrameRate);
            _timeInputField.onEndEdit.AddListener(UpdatePlaybackTime);
            _durationInputField.onEndEdit.AddListener(UpdateDuration);
            _blockLengthInputField.onEndEdit.AddListener(UpdateBlockLength);
            _blockLengthInputField.text = _blockLength.ToString();
            _divisionsInputField.onEndEdit.AddListener(UpdateDivisions);
            _divisionsInputField.text = _divisions.ToString();
            _speedInputField.onEndEdit.AddListener(UpdateSpeed);
            _keyframesContainer.gameObject.AddComponent<PointerDownHandler>().onPointerDown = OnKeyframeContainerMouseDown;
            _gridTop.gameObject.AddComponent<PointerDownHandler>().onPointerDown = OnGridTopMouse;
            _ui.transform.Find("Timeline Window/Top Container").gameObject.AddComponent<ScrollHandler>().onScroll = e =>
            {
                if (Input.GetKey(KeyCode.LeftControl))
                {
                    if (e.scrollDelta.y > 0)
                        alphaGroup.alpha = Mathf.Min(alphaGroup.alpha + 0.05f, 1f);
                    else
                        alphaGroup.alpha = Mathf.Max(alphaGroup.alpha - 0.05f, 0.1f);
                    e.Reset();
                }
            };
            DragHandler handler = _gridTop.gameObject.AddComponent<DragHandler>();
            //handler.onBeginDrag = (e) =>
            //{
            //    this.OnGridTopMouse(e);
            //    e.Reset();
            //};
            handler.onDrag = (e) =>
            {
                isPlaying = false;
                _isDraggingCursor = true;
                OnGridTopMouse(e);
                e.Reset();
            };
            handler.onEndDrag = (e) =>
            {
                _isDraggingCursor = false;
                OnGridTopMouse(e);
                e.Reset();
            };
            _gridTop.gameObject.AddComponent<ScrollHandler>().onScroll = e =>
            {
                if (e.scrollDelta.y > 0)
                    ZoomIn();
                else
                    ZoomOut();
                e.Reset();
            };
            _verticalScrollView.onValueChanged.AddListener(ScrollKeyframes);
            _keyframesContainer.gameObject.AddComponent<ScrollHandler>().onScroll = e =>
            {
                if (Input.GetKey(KeyCode.LeftControl))
                {
                    if (e.scrollDelta.y > 0)
                        ZoomIn();
                    else
                        ZoomOut();
                    e.Reset();
                }
                else if (Input.GetKey(KeyCode.LeftAlt))
                {
                    ScaleKeyframeSelection(e.scrollDelta.y);
                    e.Reset();
                }
                else if (Input.GetKey(KeyCode.LeftShift) == false)
                {
                    _verticalScrollView.OnScroll(e);
                    e.Reset();
                }
                else
                    _horizontalScrollView.OnScroll(e);
            };
            handler = _keyframesContainer.gameObject.AddComponent<DragHandler>();
            handler.onInitializePotentialDrag = (e) =>
            {
                PotentiallyBeginAreaSelect(e);
                e.Reset();
            };
            handler.onBeginDrag = (e) =>
            {
                BeginAreaSelect(e);
                e.Reset();
            };
            handler.onDrag = (e) =>
            {
                UpdateAreaSelect(e);
                e.Reset();
            };
            handler.onEndDrag = (e) =>
            {
                EndAreaSelect(e);
                e.Reset();
            };
            _allToggle.onValueChanged.AddListener(b => UpdateInterpolablesView());
            _interpolablesSearchField.onValueChanged.AddListener(InterpolablesSearch);
            handler = _resizeHandle.gameObject.AddComponent<DragHandler>();
            handler.onDrag = OnResizeWindow;

            //Keyframe window
            _keyframeWindow = _ui.transform.Find("Keyframe Window").gameObject;
            UIUtility.MakeObjectDraggable((RectTransform)_keyframeWindow.transform.Find("Top Container"), (RectTransform)_keyframeWindow.transform, (RectTransform)_ui.transform);
            _keyframeInterpolableNameText = _keyframeWindow.transform.Find("Main Container/Main Fields/Interpolable Name").GetComponent<Text>();
            _keyframeSelectPrevButton = _keyframeWindow.transform.Find("Main Container/Main Fields/Prev Next/Prev").GetComponent<Button>();
            _keyframeSelectNextButton = _keyframeWindow.transform.Find("Main Container/Main Fields/Prev Next/Next").GetComponent<Button>();
            _keyframeTimeTextField = _keyframeWindow.transform.Find("Main Container/Main Fields/Time/InputField").GetComponent<InputField>();
            _keyframeUseCurrentTimeButton = _keyframeWindow.transform.Find("Main Container/Main Fields/Use Current Time").GetComponent<Button>();
            _keyframeValueText = _keyframeWindow.transform.Find("Main Container/Main Fields/Value/Background/Text").GetComponent<Text>();
            _keyframeUseCurrentValueButton = _keyframeWindow.transform.Find("Main Container/Main Fields/Use Current").GetComponent<Button>();
            Button deleteButton = _keyframeWindow.transform.Find("Main Container/Main Fields/Delete").GetComponent<Button>();
            _keyframeDeleteButtonText = deleteButton.GetComponentInChildren<Text>();

            _curveContainer = _keyframeWindow.transform.Find("Main Container/Curve Fields/Curve/Grid/Spline").GetComponent<RawImage>();
            _curveContainer.material = new Material(_curveContainer.material);
            _curveTimeInputField = _keyframeWindow.transform.Find("Main Container/Curve Fields/Fields/Curve Point Time/InputField").GetComponent<InputField>();
            _curveTimeSlider = _keyframeWindow.transform.Find("Main Container/Curve Fields/Fields/Curve Point Time/Slider").GetComponent<Slider>();
            _curveValueInputField = _keyframeWindow.transform.Find("Main Container/Curve Fields/Fields/Curve Point Value/InputField").GetComponent<InputField>();
            _curveValueSlider = _keyframeWindow.transform.Find("Main Container/Curve Fields/Fields/Curve Point Value/Slider").GetComponent<Slider>();
            _curveInTangentInputField = _keyframeWindow.transform.Find("Main Container/Curve Fields/Fields/Curve Point InTangent/InputField").GetComponent<InputField>();
            _curveInTangentSlider = _keyframeWindow.transform.Find("Main Container/Curve Fields/Fields/Curve Point InTangent/Slider").GetComponent<Slider>();
            _curveOutTangentInputField = _keyframeWindow.transform.Find("Main Container/Curve Fields/Fields/Curve Point OutTangent/InputField").GetComponent<InputField>();
            _curveOutTangentSlider = _keyframeWindow.transform.Find("Main Container/Curve Fields/Fields/Curve Point OutTangent/Slider").GetComponent<Slider>();
            _cursor2 = (RectTransform)_ui.transform.Find("Keyframe Window/Main Container/Curve Fields/Curve/Grid/Cursor");

            _keyframeWindow.transform.Find("Close").GetComponent<Button>().onClick.AddListener(CloseKeyframeWindow);
            _keyframeSelectPrevButton.onClick.AddListener(SelectPreviousKeyframe);
            _keyframeSelectNextButton.onClick.AddListener(SelectNextKeyframe);
            _keyframeUseCurrentTimeButton.onClick.AddListener(UseCurrentTime);
            _keyframeWindow.transform.Find("Main Container/Main Fields/Drag At Current Time").GetComponent<Button>().onClick.AddListener(DragAtCurrentTime);
            _keyframeUseCurrentValueButton.onClick.AddListener(UseCurrentValue);
            deleteButton.onClick.AddListener(DeleteSelectedKeyframes);


            _keyframeWindow.transform.Find("Main Container/Curve Fields/Fields/Presets/Line").GetComponent<Button>().onClick.AddListener(() => ApplyKeyframeCurvePreset(_linePreset));
            _keyframeWindow.transform.Find("Main Container/Curve Fields/Fields/Presets/Top").GetComponent<Button>().onClick.AddListener(() => ApplyKeyframeCurvePreset(_topPreset));
            _keyframeWindow.transform.Find("Main Container/Curve Fields/Fields/Presets/Bottom").GetComponent<Button>().onClick.AddListener(() => ApplyKeyframeCurvePreset(_bottomPreset));
            _keyframeWindow.transform.Find("Main Container/Curve Fields/Fields/Presets/Hermite").GetComponent<Button>().onClick.AddListener(() => ApplyKeyframeCurvePreset(_hermitePreset));
            _keyframeWindow.transform.Find("Main Container/Curve Fields/Fields/Presets/Stairs").GetComponent<Button>().onClick.AddListener(() => ApplyKeyframeCurvePreset(_stairsPreset));
            _keyframeWindow.transform.Find("Main Container/Curve Fields/Fields/Buttons/Copy").GetComponent<Button>().onClick.AddListener(CopyKeyframeCurve);
            _keyframeWindow.transform.Find("Main Container/Curve Fields/Fields/Buttons/Paste").GetComponent<Button>().onClick.AddListener(PasteKeyframeCurve);
            _keyframeWindow.transform.Find("Main Container/Curve Fields/Fields/Buttons/Invert").GetComponent<Button>().onClick.AddListener(InvertKeyframeCurve);

            _keyframeTimeTextField.onEndEdit.AddListener(UpdateSelectedKeyframeTime);

            _curveContainer.gameObject.AddComponent<PointerDownHandler>().onPointerDown = OnCurveMouseDown;
            _curveTimeInputField.onEndEdit.AddListener(UpdateCurvePointTime);
            _curveTimeSlider.onValueChanged.AddListener(UpdateCurvePointTime);
            _curveValueInputField.onEndEdit.AddListener(UpdateCurvePointValue);
            _curveValueSlider.onValueChanged.AddListener(UpdateCurvePointValue);
            _curveInTangentInputField.onEndEdit.AddListener(UpdateCurvePointInTangent);
            _curveInTangentSlider.onValueChanged.AddListener(UpdateCurvePointInTangent);
            _curveOutTangentInputField.onEndEdit.AddListener(UpdateCurvePointOutTangent);
            _curveOutTangentSlider.onValueChanged.AddListener(UpdateCurvePointOutTangent);

            _ui.gameObject.SetActive(false);
            _helpPanel.gameObject.SetActive(false);
            _singleFilesPanel.gameObject.SetActive(false);
            _keyframeWindow.gameObject.SetActive(false);
            _tooltip.transform.parent.gameObject.SetActive(false);

            UpdateInterpolablesView();

            _loaded = true;

            // Wrap in a try since it can crash if KKAPI is not installed
            try { StartCoroutine(TimelineButton.Init(() => _interpolables.Count > 0, () => _ui.gameObject.activeSelf, ToggleUiVisible, Logger)); }
            catch (Exception ex) { Logger.LogError(ex); }
        }

        private void ScrollKeyframes(Vector2 arg0)
        {
            _keyframesContainer.anchoredPosition = new Vector2(_keyframesContainer.anchoredPosition.x, _verticalScrollView.content.anchoredPosition.y);
            _miscContainer.anchoredPosition = new Vector2(_miscContainer.anchoredPosition.x, _verticalScrollView.content.anchoredPosition.y);
        }

        private void InterpolateBefore()
        {
            if (_isPlaying)
            {
                _playbackTime = (Time.time - _startTime) % _duration;
                UpdateCursor();
                Interpolate(true);
            }
        }

        private void InterpolateAfter()
        {
            if (_isPlaying)
                Interpolate(false);
        }

        private void Interpolate(bool before)
        {
            _interpolablesTree.Recurse((node, depth) =>
            {
                if (node.type != INodeType.Leaf)
                    return;
                Interpolable interpolable = ((LeafNode<Interpolable>)node).obj;
                if (interpolable.enabled == false)
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

                KeyValuePair<float, Keyframe> left = default;
                KeyValuePair<float, Keyframe> right = default;
                foreach (KeyValuePair<float, Keyframe> keyframePair in interpolable.keyframes)
                {
                    if (keyframePair.Key <= _playbackTime)
                        left = keyframePair;
                    else
                    {
                        right = keyframePair;
                        break;
                    }
                }

                bool res = true;
                if (left.Value != null && right.Value != null)
                {
                    float normalizedTime = (_playbackTime - left.Key) / (right.Key - left.Key);
                    normalizedTime = left.Value.curve.Evaluate(normalizedTime);
                    if (before)
                        res = interpolable.InterpolateBefore(left.Value.value, right.Value.value, normalizedTime);
                    else
                        res = interpolable.InterpolateAfter(left.Value.value, right.Value.value, normalizedTime);
                }
                else if (left.Value != null)
                {
                    if (before)
                        res = interpolable.InterpolateBefore(left.Value.value, left.Value.value, 0);
                    else
                        res = interpolable.InterpolateAfter(left.Value.value, left.Value.value, 0);
                }
                else if (right.Value != null)
                {
                    if (before)
                        res = interpolable.InterpolateBefore(right.Value.value, right.Value.value, 0);
                    else
                        res = interpolable.InterpolateAfter(right.Value.value, right.Value.value, 0);
                }
                if (res == false)
                    _toDelete.Add(interpolable);
            });
        }

        private float ParseTime(string timeString)
        {
            string[] timeComponents = timeString.Split(':');
            if (timeComponents.Length != 2)
                return -1;
            int minutes;
            if (int.TryParse(timeComponents[0], out minutes) == false || minutes < 0)
                return -1;
            float seconds;
            if (float.TryParse(timeComponents[1], out seconds) == false)
                return -1;
            return minutes * 60 + seconds;
        }

        #region Main Window
        private void UpdateCursor()
        {
            _cursor.anchoredPosition = new Vector2((_playbackTime * _grid.rect.width) / _duration, _cursor.anchoredPosition.y);
            UpdateCursor2();
            _timeInputField.text = $"{Mathf.FloorToInt(_playbackTime / 60):00}:{(_playbackTime % 60):00.000}";
        }

        private void UpdateDesiredFrameRate(string s)
        {
            int res;
            if (int.TryParse(_frameRateInputField.text, out res) && res >= 1)
                _desiredFrameRate = res;
            _frameRateInputField.text = _desiredFrameRate.ToString();
        }

        private void UpdatePlaybackTime(string s)
        {
            if (_isPlaying == false)
            {
                float time = ParseTime(_timeInputField.text);
                if (time < 0)
                    return;
                SeekPlaybackTime(time % _duration);
            }
        }

        private void UpdateDuration(string s)
        {
            float time = ParseTime(_durationInputField.text);
            if (time < 0)
                return;
            _duration = time;
            UpdateGrid();
        }

        private void UpdateBlockLength(string arg0)
        {
            float res;
            if (float.TryParse(_blockLengthInputField.text, out res) && res >= 0.01f)
            {
                _blockLength = res;
                UpdateGrid();
            }
            _blockLengthInputField.text = _blockLength.ToString();
        }

        private void UpdateDivisions(string arg0)
        {
            int res;
            if (int.TryParse(_divisionsInputField.text, out res) && res >= 1)
            {
                _divisions = res;
                UpdateGridMaterial();
            }
            _divisionsInputField.text = _divisions.ToString();
        }

        private void UpdateSpeed(string arg0)
        {
            float s;
            if (float.TryParse(_speedInputField.text, out s) && s >= 0)
                Time.timeScale = s;
        }

        private void ZoomOut()
        {
            _zoomLevel -= 0.05f * _zoomLevel;
            if (_zoomLevel < 0.1f)
                _zoomLevel = 0.1f;
            float position = _horizontalScrollView.horizontalNormalizedPosition;
            UpdateGrid();
            _horizontalScrollView.horizontalNormalizedPosition = position;
        }

        private void ZoomIn()
        {
            _zoomLevel += 0.05f * _zoomLevel;
            if (_zoomLevel > 64f)
                _zoomLevel = 64f;
            float position = _horizontalScrollView.horizontalNormalizedPosition;
            UpdateGrid();
            _horizontalScrollView.horizontalNormalizedPosition = position;
        }

        private void ToggleHelp()
        {
            _helpPanel.gameObject.SetActive(!_helpPanel.gameObject.activeSelf);
        }

        private void InterpolablesSearch(string arg0)
        {
            UpdateInterpolablesView();
        }

        private void UpdateInterpolablesView()
        {
            bool showAll = _allToggle.isOn;
            int interpolableDisplayIndex = 0;
            int headerDisplayIndex = 0;
            //Dictionary<int, Interpolable> usedInterpolables = new Dictionary<int, Interpolable>();
            _gridHeights.Clear();
            float height = 0;
            UpdateInterpolablesViewTree(_interpolablesTree.tree, showAll, ref interpolableDisplayIndex, ref headerDisplayIndex, ref height);
            int interpolableModelDisplayIndex = 0;
            foreach (KeyValuePair<string, List<InterpolableModel>> ownerPair in _interpolableModelsDictionary.OrderBy(p => _hardCodedOwnerOrder.TryGetValue(p.Key, out int order) ? order : int.MaxValue))
            {
                HeaderDisplay header = GetHeaderDisplay(headerDisplayIndex);
                header.gameObject.transform.SetAsLastSibling();
                header.container.offsetMin = Vector2.zero;
                header.group = null;
                header.name.text = ownerPair.Key;
                height += _interpolableHeight;
                _gridHeights.Add(height);

                if (header.expanded)
                {
                    foreach (InterpolableModel model in ownerPair.Value)
                    {
                        //Interpolable usedInterpolable;
                        if ( /*usedInterpolables.TryGetValue(model.GetHashCode(), out usedInterpolable) ||*/ model.IsCompatibleWithTarget(_selectedOCI) == false)
                            continue;

                        if (model.name.IndexOf(_interpolablesSearchField.text, StringComparison.OrdinalIgnoreCase) == -1)
                            continue;

                        InterpolableModelDisplay display = GetInterpolableModelDisplay(interpolableModelDisplayIndex);
                        display.gameObject.transform.SetAsLastSibling();
                        display.model = model;
                        display.name.text = model.name;
                        height += _interpolableHeight;
                        _gridHeights.Add(height);
                        ++interpolableModelDisplayIndex;
                    }
                }

                ++headerDisplayIndex;
            }

            for (; headerDisplayIndex < _displayedOwnerHeader.Count; headerDisplayIndex++)
                _displayedOwnerHeader[headerDisplayIndex].gameObject.SetActive(false);

            for (; interpolableDisplayIndex < _displayedInterpolables.Count; ++interpolableDisplayIndex)
            {
                InterpolableDisplay display = _displayedInterpolables[interpolableDisplayIndex];
                display.gameObject.SetActive(false);
                display.gridBackground.gameObject.SetActive(false);
            }

            for (; interpolableModelDisplayIndex < _displayedInterpolableModels.Count; ++interpolableModelDisplayIndex)
                _displayedInterpolableModels[interpolableModelDisplayIndex].gameObject.SetActive(false);

            UpdateInterpolableSelection();

            this.ExecuteDelayed2(UpdateGrid);

            this.ExecuteDelayed2(UpdateSeparators, 2);

            TimelineButton.UpdateButton();
        }

        private void UpdateInterpolablesViewTree(List<INode> nodes, bool showAll, ref int interpolableDisplayIndex, ref int headerDisplayIndex, ref float height, int indent = 0)
        {
            foreach (INode node in nodes)
            {
                switch (node.type)
                {
                    case INodeType.Leaf:
                        Interpolable interpolable = ((LeafNode<Interpolable>)node).obj;
                        if (ShouldShowInterpolable(interpolable, showAll) == false)
                            continue;

                        InterpolableDisplay display = GetInterpolableDisplay(interpolableDisplayIndex);
                        display.gameObject.transform.SetAsLastSibling();
                        display.container.offsetMin = new Vector2(indent, 0f);
                        display.interpolable = (LeafNode<Interpolable>)node;
                        display.group.alpha = interpolable.useOciInHash == false || interpolable.oci != null && interpolable.oci == _selectedOCI ? 1f : 0.75f;
                        display.enabled.onValueChanged = new Toggle.ToggleEvent();
                        display.enabled.isOn = interpolable.enabled;
                        display.enabled.onValueChanged.AddListener(b => interpolable.enabled = display.enabled.isOn);
                        if (string.IsNullOrEmpty(interpolable.alias))
                        {
                            if (showAll && interpolable.oci != null && ReferenceEquals(interpolable.parameter, interpolable.oci.guideObject) == false)
                                display.name.text = interpolable.name + " (" + interpolable.oci.guideObject.transformTarget.name + ")";
                            else
                                display.name.text = interpolable.name;
                        }
                        else
                            display.name.text = interpolable.alias;
                        display.gridBackground.gameObject.SetActive(true);
                        display.gridBackground.rectTransform.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -height - _interpolableHeight), new Vector2(0f, -height));
                        UpdateInterpolableColor(display, interpolable.color);
                        height += _interpolableHeight;
                        _gridHeights.Add(height);
                        ++interpolableDisplayIndex;
                        break;
                    case INodeType.Group:
                        GroupNode<InterpolableGroup> group = (GroupNode<InterpolableGroup>)node;

                        if (_interpolablesTree.Any(group, leafNode => ShouldShowInterpolable(leafNode.obj, showAll)) == false)
                            break;

                        HeaderDisplay headerDisplay = GetHeaderDisplay(headerDisplayIndex, true);
                        headerDisplay.gameObject.transform.SetAsLastSibling();
                        headerDisplay.container.offsetMin = new Vector2(indent, 0f);
                        headerDisplay.group = (GroupNode<InterpolableGroup>)node;
                        headerDisplay.name.text = group.obj.name;
                        height += _interpolableHeight * 2f / 3f;
                        _gridHeights.Add(height);
                        ++headerDisplayIndex;
                        if (group.obj.expanded)
                            UpdateInterpolablesViewTree(((GroupNode<InterpolableGroup>)node).children, showAll, ref interpolableDisplayIndex, ref headerDisplayIndex, ref height, indent + 8);
                        break;
                }
            }
        }

        private bool ShouldShowInterpolable(Interpolable interpolable, bool showAll)
        {
            if (showAll == false && ((interpolable.oci != null && interpolable.oci != _selectedOCI) || !interpolable.ShouldShow()))
                return false;
            //if (usedInterpolables.ContainsKey(interpolable.GetBaseHashCode()) == false)
            //    usedInterpolables.Add(interpolable.GetBaseHashCode(), interpolable);

            if (interpolable.name.IndexOf(_interpolablesSearchField.text, StringComparison.OrdinalIgnoreCase) == -1)
                return false;
            return true;
        }

        private void UpdateInterpolableColor(InterpolableDisplay display, Color c)
        {
            display.background.color = c;
            display.name.color = c.GetContrastingColor();
            display.gridBackground.color = new Color(c.r, c.g, c.b, 0.825f);
        }

        private void UpdateSeparators()
        {
            int i = 0;
            foreach (float height in _gridHeights)
            {
                RawImage separator;
                if (i < _interpolableSeparators.Count)
                    separator = _interpolableSeparators[i];
                else
                {
                    separator = UIUtility.CreateRawImage("Separator", _miscContainer);
                    separator.color = new Color(0f, 0f, 0f, 0.5f);
                    _interpolableSeparators.Add(separator);
                }
                separator.gameObject.SetActive(true);
                separator.rectTransform.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -height - 1.5f), new Vector2(0f, -height + 1.5f));

                ++i;
            }

            //for (int y = _interpolableHeight; y < this._verticalScrollView.content.rect.height; y += _interpolableHeight)
            //{
            //    RawImage separator;
            //    if (i < this._interpolableSeparators.Count)
            //        separator = this._interpolableSeparators[i];
            //    else
            //    {
            //        separator = UIUtility.CreateRawImage("Separator", this._miscContainer);
            //        separator.color = new Color(0f, 0f, 0f, 0.5f);
            //        this._interpolableSeparators.Add(separator);
            //    }
            //    separator.gameObject.SetActive(true);
            //    separator.rectTransform.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -y - 1.5f), new Vector2(0f, -y + 1.5f));
            //    ++i;
            //}
            for (; i < _interpolableSeparators.Count; i++)
                _interpolableSeparators[i].gameObject.SetActive(false);
        }

        private InterpolableDisplay GetInterpolableDisplay(int i)
        {
            InterpolableDisplay display;
            if (i < _displayedInterpolables.Count)
                display = _displayedInterpolables[i];
            else
            {
                display = new InterpolableDisplay();
                display.gameObject = GameObject.Instantiate(_interpolablePrefab);
                display.gameObject.hideFlags = HideFlags.None;
                display.group = display.gameObject.GetComponent<CanvasGroup>();
                display.container = (RectTransform)display.gameObject.transform.Find("Container");
                display.enabled = display.container.Find("Enabled").GetComponent<Toggle>();
                display.name = display.container.Find("Label").GetComponent<Text>();
                display.inputField = display.container.Find("InputField").GetComponent<InputField>();
                display.background = display.container.GetComponent<Image>();
                display.selectedOutline = display.container.Find("SelectedOutline").GetComponent<Image>();
                display.gridBackground = UIUtility.CreateRawImage($"Interpolable{i} Background", _miscContainer);
                display.background.material = new Material(display.background.material);

                display.gameObject.transform.SetParent(_verticalScrollView.content);
                display.gameObject.transform.localPosition = Vector3.zero;
                display.gameObject.transform.localScale = Vector3.one;
                display.gridBackground.transform.SetAsFirstSibling();
                display.gridBackground.raycastTarget = false;
                display.gridBackground.material = new Material(_keyframesBackgroundMaterial);
                display.inputField.gameObject.SetActive(false);
                display.container.gameObject.AddComponent<PointerDownHandler>().onPointerDown = (e) =>
                {
                    Interpolable interpolable = display.interpolable.obj;
                    switch (e.button)
                    {
                        case PointerEventData.InputButton.Left:
                            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                                SelectAddInterpolable(interpolable);
                            else if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                            {
                                Interpolable lastSelected = _selectedInterpolables.LastOrDefault();
                                if (lastSelected != null)
                                {
                                    Interpolable selectingNow = interpolable;
                                    int selectingNowIndex = _displayedInterpolables.FindIndex(elem => elem.interpolable.obj == selectingNow);
                                    int lastSelectedIndex = _displayedInterpolables.FindIndex(elem => elem.interpolable.obj == lastSelected);
                                    if (selectingNowIndex < lastSelectedIndex)
                                    {
                                        int temp = selectingNowIndex;
                                        selectingNowIndex = lastSelectedIndex;
                                        lastSelectedIndex = temp;
                                    }

                                    SelectAddInterpolable(_displayedInterpolables.Where((elem, index) => index > lastSelectedIndex && index < selectingNowIndex).Select(elem => elem.interpolable.obj).ToArray());
                                    SelectAddInterpolable(selectingNow);
                                }
                                else
                                    SelectAddInterpolable(interpolable);
                            }
                            else if (Input.GetKey(KeyCode.LeftAlt))
                            {
                                GuideObject linkedGuideObject = interpolable.parameter as GuideObject;
                                if (linkedGuideObject == null && interpolable.oci != null)
                                    linkedGuideObject = interpolable.oci.guideObject;
                                if (linkedGuideObject != null)
                                    GuideObjectManager.Instance.selectObject = linkedGuideObject;
                            }
                            else
                                SelectInterpolable(interpolable);

                            break;
                        case PointerEventData.InputButton.Middle:
                            if (Input.GetKey(KeyCode.LeftControl))
                                RemoveInterpolable(interpolable);
                            break;
                        case PointerEventData.InputButton.Right:
                            if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)_ui.transform, e.position, e.pressEventCamera, out Vector2 localPoint))
                            {
                                if (_selectedInterpolables.Count == 0 || _selectedInterpolables.Contains(interpolable) == false)
                                    SelectInterpolable(interpolable);

                                List<Interpolable> currentlySelectedInterpolables = new List<Interpolable>(_selectedInterpolables);

                                List<AContextMenuElement> elements = new List<AContextMenuElement>();
                                if (currentlySelectedInterpolables.Count == 1)
                                {
                                    Interpolable selectedInterpolable = currentlySelectedInterpolables[0];
                                    GuideObject linkedGuideObject = selectedInterpolable.parameter as GuideObject;
                                    if (linkedGuideObject == null && selectedInterpolable.oci != null)
                                        linkedGuideObject = selectedInterpolable.oci.guideObject;

                                    if (linkedGuideObject != null)
                                    {
                                        elements.Add(new LeafElement()
                                        {
                                            icon = _linkSprite,
                                            text = "Select linked GuideObject",
                                            onClick = p => { GuideObjectManager.Instance.selectObject = linkedGuideObject; }
                                        });
                                    }

                                    elements.Add(new LeafElement()
                                    {
                                        icon = _renameSprite,
                                        text = "Rename",
                                        onClick = p =>
                                        {
                                            display.inputField.gameObject.SetActive(true);
                                            display.inputField.onEndEdit = new InputField.SubmitEvent();
                                            display.inputField.text = string.IsNullOrEmpty(selectedInterpolable.alias) ? selectedInterpolable.name : selectedInterpolable.alias;
                                            display.inputField.onEndEdit.AddListener(s =>
                                            {
                                                selectedInterpolable.alias = display.inputField.text.Trim();
                                                display.inputField.gameObject.SetActive(false);
                                                UpdateInterpolablesView();
                                            });
                                            display.inputField.ActivateInputField();
                                            display.inputField.Select();
                                        }
                                    });
                                }
                                else
                                {
                                    elements.Add(new LeafElement()
                                    {
                                        icon = _newFolderSprite,
                                        text = "Group together",
                                        onClick = p =>
                                        {
                                            _interpolablesTree.GroupTogether(currentlySelectedInterpolables, new InterpolableGroup() { name = "New Group" });
                                            UpdateInterpolablesView();
                                        }
                                    });
                                }
                                elements.Add(new LeafElement()
                                {
                                    icon = _selectAllSprite,
                                    text = "Select keyframes",
                                    onClick = p =>
                                    {
                                        List<KeyValuePair<float, Keyframe>> toSelect = new List<KeyValuePair<float, Keyframe>>();
                                        foreach (Interpolable selected in currentlySelectedInterpolables)
                                            toSelect.AddRange(selected.keyframes);
                                        SelectKeyframes(toSelect);
                                    }
                                });
                                elements.Add(new LeafElement()
                                {
                                    icon = _selectAllSprite,
                                    text = "Select keyframes before cursor",
                                    onClick = p =>
                                    {
                                        List<KeyValuePair<float, Keyframe>> toSelect = new List<KeyValuePair<float, Keyframe>>();
                                        float currentTime = _playbackTime % _duration;
                                        foreach (Interpolable selected in currentlySelectedInterpolables)
                                            toSelect.AddRange(selected.keyframes.Where(k => k.Key < currentTime));
                                        SelectKeyframes(toSelect);
                                    }
                                });
                                elements.Add(new LeafElement()
                                {
                                    icon = _selectAllSprite,
                                    text = "Select keyframes after cursor",
                                    onClick = p =>
                                    {
                                        List<KeyValuePair<float, Keyframe>> toSelect = new List<KeyValuePair<float, Keyframe>>();
                                        float currentTime = _playbackTime % _duration;
                                        foreach (Interpolable selected in currentlySelectedInterpolables)
                                            toSelect.AddRange(selected.keyframes.Where(k => k.Key >= currentTime));
                                        SelectKeyframes(toSelect);
                                    }
                                });
                                elements.Add(new LeafElement()
                                {
                                    icon = _colorSprite,
                                    text = "Color",
                                    onClick = p =>
                                    {
#if HONEYSELECT
                                        Studio.Studio.Instance.colorPaletteCtrl.visible = true;
                                        Studio.Studio.Instance.colorMenu.updateColorFunc = null;
                                        if (currentlySelectedInterpolables.Count == 1)
                                            Studio.Studio.Instance.colorMenu.SetColor(currentlySelectedInterpolables[0].color, UI_ColorInfo.ControlType.PickerRect);
                                        Studio.Studio.Instance.colorMenu.updateColorFunc = col =>
                                        {
                                            foreach (Interpolable interp in currentlySelectedInterpolables)
                                            {
                                                InterpolableDisplay disp = this._displayedInterpolables.Find(id => id.interpolable.obj == interp);
                                                interp.color = col;
                                                this.UpdateInterpolableColor(disp, col);
                                            }
                                        };
#elif KOIKATSU
                                        Studio.Studio.Instance.colorPalette.visible = false;
                                        Studio.Studio.Instance.colorPalette.Setup("Interpolable Color", currentlySelectedInterpolables[0].color, (col) =>
                                        {
                                            foreach (Interpolable interp in currentlySelectedInterpolables)
                                            {
                                                InterpolableDisplay disp = _displayedInterpolables.Find(id => id.interpolable.obj == interp);
                                                interp.color = col;
                                                UpdateInterpolableColor(disp, col);
                                            }
                                        }, true);

#endif
                                    }
                                });

                                elements.Add(new LeafElement()
                                {
                                    icon = _addSprite,
                                    text = currentlySelectedInterpolables.Count == 1 ? "Add keyframe at cursor" : "Add keyframes at cursor",
                                    onClick = p =>
                                    {
                                        float time = _playbackTime % _duration;
                                        foreach (Interpolable selectedInterpolable in currentlySelectedInterpolables)
                                            AddKeyframe(selectedInterpolable, time);
                                        UpdateGrid();
                                    }
                                });
                                List<AContextMenuElement> treeGroups = GetInterpolablesTreeGroups(currentlySelectedInterpolables.Select(elem => (INode)_interpolablesTree.GetLeafNode(elem)));
                                if (treeGroups.Count != 1)
                                {
                                    elements.Add(new GroupElement()
                                    {
                                        icon = _addToFolderSprite,
                                        text = "Parent to",
                                        elements = treeGroups
                                    });
                                }
                                elements.Add(new LeafElement()
                                {
                                    icon = _checkboxSprite,
                                    text = currentlySelectedInterpolables.Count == 1 ? "Disable" : "Disable all",
                                    onClick = p =>
                                    {
                                        foreach (Interpolable selectedInterpolable in currentlySelectedInterpolables)
                                            selectedInterpolable.enabled = false;
                                        UpdateInterpolablesView();
                                    }
                                });
                                elements.Add(new LeafElement()
                                {
                                    icon = _checkboxCompositeSprite,
                                    text = currentlySelectedInterpolables.Count == 1 ? "Enable" : "Enable all",
                                    onClick = p =>
                                    {
                                        foreach (Interpolable selectedInterpolable in currentlySelectedInterpolables)
                                            selectedInterpolable.enabled = true;
                                        UpdateInterpolablesView();
                                    }
                                });
                                elements.Add(new LeafElement()
                                {
                                    icon = _chevronUpSprite,
                                    text = "Move up",
                                    onClick = p =>
                                    {
                                        _interpolablesTree.MoveUp(currentlySelectedInterpolables.Select(elem => (INode)_interpolablesTree.GetLeafNode(elem)));
                                        UpdateInterpolablesView();
                                    }
                                });
                                elements.Add(new LeafElement()
                                {
                                    icon = _chevronDownSprite,
                                    text = "Move down",
                                    onClick = p =>
                                    {
                                        _interpolablesTree.MoveDown(currentlySelectedInterpolables.Select(elem => (INode)_interpolablesTree.GetLeafNode(elem)));
                                        UpdateInterpolablesView();
                                    }
                                });
                                elements.Add(new LeafElement()
                                {
                                    icon = _deleteSprite,
                                    text = "Delete",
                                    onClick = p =>
                                    {
                                        string message = currentlySelectedInterpolables.Count > 1
                                                ? "Are you sure you want to delete these Interpolables?"
                                                : "Are you sure you want to delete this Interpolable?";
                                        UIUtility.DisplayConfirmationDialog(result =>
                                        {
                                            if (result)
                                                RemoveInterpolables(currentlySelectedInterpolables);
                                        }, message);
                                    }
                                });
                                UIUtility.ShowContextMenu(_ui, localPoint, elements, 220);
                            }
                            break;
                    }
                };
                _displayedInterpolables.Add(display);
            }
            display.gameObject.SetActive(true);
            return display;
        }

        private InterpolableModelDisplay GetInterpolableModelDisplay(int i)
        {
            InterpolableModelDisplay display;
            if (i < _displayedInterpolableModels.Count)
                display = _displayedInterpolableModels[i];
            else
            {
                display = new InterpolableModelDisplay();
                display.gameObject = GameObject.Instantiate(_interpolableModelPrefab);
                display.gameObject.hideFlags = HideFlags.None;
                display.name = display.gameObject.transform.Find("Label").GetComponent<Text>();

                display.gameObject.transform.SetParent(_verticalScrollView.content);
                display.gameObject.transform.localPosition = Vector3.zero;
                display.gameObject.transform.localScale = Vector3.one;
                _displayedInterpolableModels.Add(display);
            }
            display.gameObject.SetActive(true);
            return display;
        }

        private HeaderDisplay GetHeaderDisplay(int i, bool treeHeader = false)
        {
            HeaderDisplay display;
            if (i < _displayedOwnerHeader.Count)
                display = _displayedOwnerHeader[i];
            else
            {
                display = new HeaderDisplay();
                display.gameObject = GameObject.Instantiate(_headerPrefab);
                display.gameObject.hideFlags = HideFlags.None;
                display.layoutElement = display.gameObject.GetComponent<LayoutElement>();
                display.container = (RectTransform)display.gameObject.transform.Find("Container");
                display.name = display.container.Find("Text").GetComponent<Text>();
                display.inputField = display.container.Find("InputField").GetComponent<InputField>();

                display.gameObject.transform.SetParent(_verticalScrollView.content);
                display.gameObject.transform.localPosition = Vector3.zero;
                display.gameObject.transform.localScale = Vector3.one;
                display.inputField.gameObject.SetActive(false);

                display.container.gameObject.AddComponent<PointerDownHandler>().onPointerDown = (e) =>
                {
                    switch (e.button)
                    {
                        case PointerEventData.InputButton.Left:
                            if (display.group != null)
                                display.group.obj.expanded = !display.group.obj.expanded;
                            else
                                display.expanded = !display.expanded;
                            UpdateInterpolablesView();
                            break;
                        case PointerEventData.InputButton.Middle:
                            if (display.group != null && Input.GetKey(KeyCode.LeftControl))
                            {
                                List<Interpolable> interpolables = new List<Interpolable>();
                                _interpolablesTree.Recurse(display.group, (n, d) =>
                                {
                                    if (n.type == INodeType.Leaf)
                                        interpolables.Add(((LeafNode<Interpolable>)n).obj);
                                });

                                RemoveInterpolables(interpolables);
                                _interpolablesTree.Remove(display.group);
                            }
                            break;
                        case PointerEventData.InputButton.Right:
                            if (display.group != null && RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)_ui.transform, e.position, e.pressEventCamera, out Vector2 localPoint))
                            {
                                if (_selectedInterpolables.Count != 0)
                                    ClearSelectedInterpolables();

                                List<AContextMenuElement> elements = new List<AContextMenuElement>();

                                elements.Add(new LeafElement()
                                {
                                    icon = _renameSprite,
                                    text = "Rename",
                                    onClick = p =>
                                    {
                                        display.inputField.gameObject.SetActive(true);
                                        display.inputField.onEndEdit = new InputField.SubmitEvent();
                                        display.inputField.text = display.@group.obj.name;
                                        display.inputField.onEndEdit.AddListener(s =>
                                        {
                                            string newName = display.inputField.text.Trim();
                                            if (newName.Length != 0)
                                                display.group.obj.name = newName;
                                            display.inputField.gameObject.SetActive(false);
                                            UpdateInterpolablesView();
                                        });
                                        display.inputField.ActivateInputField();
                                        display.inputField.Select();
                                    }
                                });
                                elements.Add(new LeafElement()
                                {
                                    icon = _selectAllSprite,
                                    text = "Select Interpolables under",
                                    onClick = p =>
                                    {
                                        List<Interpolable> toSelect = new List<Interpolable>();
                                        _interpolablesTree.Recurse(display.group, (n, d) =>
                                        {
                                            if (n.type == INodeType.Leaf)
                                                toSelect.Add(((LeafNode<Interpolable>)n).obj);
                                        });
                                        SelectInterpolable(toSelect.ToArray());
                                    }
                                });
                                elements.Add(new LeafElement()
                                {
                                    icon = _selectAllSprite,
                                    text = "Select keyframes",
                                    onClick = p =>
                                    {
                                        List<KeyValuePair<float, Keyframe>> toSelect = new List<KeyValuePair<float, Keyframe>>();
                                        _interpolablesTree.Recurse(display.group, (n, d) =>
                                        {
                                            if (n.type == INodeType.Leaf)
                                                toSelect.AddRange(((LeafNode<Interpolable>)n).obj.keyframes);
                                        });
                                        SelectKeyframes(toSelect);
                                    }
                                });
                                elements.Add(new LeafElement()
                                {
                                    icon = _selectAllSprite,
                                    text = "Select keyframes before cursor",
                                    onClick = p =>
                                    {
                                        List<KeyValuePair<float, Keyframe>> toSelect = new List<KeyValuePair<float, Keyframe>>();
                                        float currentTime = _playbackTime % _duration;
                                        _interpolablesTree.Recurse(display.group, (n, d) =>
                                        {
                                            if (n.type == INodeType.Leaf)
                                                toSelect.AddRange(((LeafNode<Interpolable>)n).obj.keyframes.Where(k => k.Key < currentTime));
                                        });
                                        SelectKeyframes(toSelect);
                                    }
                                });
                                elements.Add(new LeafElement()
                                {
                                    icon = _selectAllSprite,
                                    text = "Select keyframes after cursor",
                                    onClick = p =>
                                    {
                                        List<KeyValuePair<float, Keyframe>> toSelect = new List<KeyValuePair<float, Keyframe>>();
                                        float currentTime = _playbackTime % _duration;
                                        _interpolablesTree.Recurse(display.group, (n, d) =>
                                        {
                                            if (n.type == INodeType.Leaf)
                                                toSelect.AddRange(((LeafNode<Interpolable>)n).obj.keyframes.Where(k => k.Key >= currentTime));
                                        });
                                        SelectKeyframes(toSelect);
                                    }
                                });
                                elements.Add(new LeafElement()
                                {
                                    icon = _addSprite,
                                    text = "Add keyframes at cursor",
                                    onClick = p =>
                                    {
                                        float time = _playbackTime % _duration;
                                        _interpolablesTree.Recurse(display.group, (n, d) =>
                                        {
                                            if (n.type == INodeType.Leaf)
                                                AddKeyframe(((LeafNode<Interpolable>)n).obj, time);
                                        });
                                        UpdateGrid();
                                    }
                                });
                                List<AContextMenuElement> treeGroups = GetInterpolablesTreeGroups(new List<INode> { display.group });
                                if (treeGroups.Count != 1)
                                {
                                    elements.Add(new GroupElement()
                                    {
                                        icon = _addToFolderSprite,
                                        text = "Parent to",
                                        elements = treeGroups
                                    });
                                }
                                elements.Add(new LeafElement()
                                {
                                    icon = _checkboxSprite,
                                    text = "Disable",
                                    onClick = p =>
                                    {
                                        _interpolablesTree.Recurse(display.group, (n, d) =>
                                        {
                                            if (n.type == INodeType.Leaf)
                                                ((LeafNode<Interpolable>)n).obj.enabled = false;
                                        });
                                        UpdateInterpolablesView();
                                    }
                                });
                                elements.Add(new LeafElement()
                                {
                                    icon = _checkboxCompositeSprite,
                                    text = "Enable",
                                    onClick = p =>
                                    {
                                        _interpolablesTree.Recurse(display.group, (n, d) =>
                                        {
                                            if (n.type == INodeType.Leaf)
                                                ((LeafNode<Interpolable>)n).obj.enabled = true;
                                        });
                                        UpdateInterpolablesView();
                                    }
                                });
                                elements.Add(new LeafElement()
                                {
                                    icon = _chevronUpSprite,
                                    text = "Move up",
                                    onClick = p =>
                                    {
                                        _interpolablesTree.MoveUp(display.group);
                                        UpdateInterpolablesView();
                                    }
                                });
                                elements.Add(new LeafElement()
                                {
                                    icon = _chevronDownSprite,
                                    text = "Move down",
                                    onClick = p =>
                                    {
                                        _interpolablesTree.MoveDown(display.group);
                                        UpdateInterpolablesView();
                                    }
                                });
                                elements.Add(new LeafElement()
                                {
                                    icon = _deleteSprite,
                                    text = "Delete",
                                    onClick = p =>
                                    {
                                        UIUtility.DisplayConfirmationDialog(result =>
                                        {
                                            if (result)
                                            {
                                                List<Interpolable> interpolables = new List<Interpolable>();
                                                _interpolablesTree.Recurse(display.group, (n, d) =>
                                                {
                                                    if (n.type == INodeType.Leaf)
                                                        interpolables.Add(((LeafNode<Interpolable>)n).obj);
                                                });

                                                RemoveInterpolables(interpolables);
                                                _interpolablesTree.Remove(display.group);
                                                UpdateInterpolablesView();
                                            }
                                        }, "Are you sure you want to delete this group?");
                                    }
                                });
                                UIUtility.ShowContextMenu(_ui, localPoint, elements, 180);
                            }
                            break;
                    }
                };

                _displayedOwnerHeader.Add(display);
            }
            display.gameObject.SetActive(true);
            display.layoutElement.preferredHeight = treeHeader ? _interpolableHeight * 2f / 3f : _interpolableHeight;
            return display;
        }

        private List<AContextMenuElement> GetInterpolablesTreeGroups(IEnumerable<INode> toParent)
        {
            IEnumerable<IGroupNode> parents = toParent.Where(n => n.parent != null).Select(n => n.parent).Distinct();
            IGroupNode toIgnore = null;
            if (parents.Count() == 1)
                toIgnore = parents.FirstOrDefault();
            List<AContextMenuElement> elements = RecurseInterpolablesTreeGroups(_interpolablesTree.tree, toParent, toIgnore);
            elements.Insert(0, new LeafElement()
            {
                text = "Nothing",
                onClick = p =>
                {
                    _interpolablesTree.ParentTo(toParent, null);
                    UpdateInterpolablesView();
                }
            });

            return elements;
        }

        private List<AContextMenuElement> RecurseInterpolablesTreeGroups(List<INode> nodes, IEnumerable<INode> toParent, IGroupNode toIgnore)
        {
            List<AContextMenuElement> elements = new List<AContextMenuElement>();

            foreach (INode node in nodes)
            {
                if (node.type != INodeType.Group)
                    continue;
                GroupNode<InterpolableGroup> group = (GroupNode<InterpolableGroup>)node;
                if (node != toIgnore)
                {
                    elements.Add(new LeafElement()
                    {
                        icon = _addToFolderSprite,
                        text = group.obj.name,
                        onClick = p =>
                        {
                            _interpolablesTree.ParentTo(toParent, group);
                            UpdateInterpolablesView();
                        }
                    });
                }
                if (group.children.Count(n => n.type == INodeType.Group) != 0)
                {
                    elements.Add(new GroupElement()
                    {
                        text = group.obj.name,
                        elements = RecurseInterpolablesTreeGroups(group.children, toParent, toIgnore)
                    });
                }
            }
            return elements;
        }

        private void HighlightInterpolable(Interpolable interpolable)
        {
            StartCoroutine(HighlightInterpolable_Routine(interpolable));
        }

        private IEnumerator HighlightInterpolable_Routine(Interpolable interpolable)
        {
            InterpolableDisplay display = _displayedInterpolables.FirstOrDefault(d => d.interpolable.obj == interpolable);
            if (display != null)
            {
                Color first = interpolable.color.GetContrastingColor();
                Color second = first.GetContrastingColor();
                float startTime = Time.unscaledTime;
                while (Time.unscaledTime - startTime < 0.25f)
                {
                    UpdateInterpolableColor(display, Color.Lerp(interpolable.color, first, (Time.unscaledTime - startTime) * 4f));
                    yield return null;
                }
                startTime = Time.unscaledTime;
                while (Time.unscaledTime - startTime < 1f)
                {
                    UpdateInterpolableColor(display, Color.Lerp(second, first, (Mathf.Cos((Time.unscaledTime - startTime) * Mathf.PI * 4) + 1f) / 2f));
                    yield return null;
                }
                startTime = Time.unscaledTime;
                while (Time.unscaledTime - startTime < 0.25f)
                {
                    UpdateInterpolableColor(display, Color.Lerp(first, interpolable.color, (Time.unscaledTime - startTime) * 4f));
                    yield return null;
                }
                UpdateInterpolableColor(display, interpolable.color);
            }
        }

        private void PotentiallyBeginAreaSelect(PointerEventData e)
        {
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_keyframesContainer, e.position, e.pressEventCamera, out localPoint))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    float time = 10f * localPoint.x / (_baseGridWidth * _zoomLevel);
                    float beat = _blockLength / _divisions;
                    float mod = time % beat;
                    if (mod / beat > 0.5f)
                        time += beat - mod;
                    else
                        time -= mod;
                    localPoint.x = time * (_baseGridWidth * _zoomLevel) / 10f;
                }
                _areaSelectFirstPoint = localPoint;
            }
            _isAreaSelecting = false;
        }

        private void BeginAreaSelect(PointerEventData e)
        {
            _isAreaSelecting = true;
            _selectionArea.gameObject.SetActive(true);
        }

        private void UpdateAreaSelect(PointerEventData e)
        {
            if (_isAreaSelecting == false)
                return;
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_keyframesContainer, e.position, e.pressEventCamera, out localPoint))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    float time = 10f * localPoint.x / (_baseGridWidth * _zoomLevel);
                    float beat = _blockLength / _divisions;
                    float mod = time % beat;
                    if (mod / beat > 0.5f)
                        time += beat - mod;
                    else
                        time -= mod;
                    localPoint.x = time * (_baseGridWidth * _zoomLevel) / 10f;
                }
                Vector2 min = new Vector2(Mathf.Min(_areaSelectFirstPoint.x, localPoint.x), Mathf.Min(_areaSelectFirstPoint.y, localPoint.y));
                Vector2 max = new Vector2(Mathf.Max(_areaSelectFirstPoint.x, localPoint.x), Mathf.Max(_areaSelectFirstPoint.y, localPoint.y));

                _selectionArea.offsetMin = min;
                _selectionArea.offsetMax = max;
            }
        }

        private void EndAreaSelect(PointerEventData e)
        {
            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_keyframesContainer, e.position, e.pressEventCamera, out localPoint))
                return;
            float firstTime = 10f * _areaSelectFirstPoint.x / (_baseGridWidth * _zoomLevel);
            float secondTime = 10f * localPoint.x / (_baseGridWidth * _zoomLevel);
            if (Input.GetKey(KeyCode.LeftShift))
            {
                float beat = _blockLength / _divisions;
                float mod = secondTime % beat;
                if (mod / beat > 0.5f)
                    secondTime += beat - mod;
                else
                    secondTime -= mod;
            }
            if (secondTime < firstTime)
            {
                float temp = firstTime;
                firstTime = secondTime;
                secondTime = temp;
            }
            float minY = Mathf.Min(_areaSelectFirstPoint.y, localPoint.y);
            float maxY = Mathf.Max(_areaSelectFirstPoint.y, localPoint.y);

            _selectionArea.gameObject.SetActive(false);
            _isAreaSelecting = false;

            List<KeyValuePair<float, Keyframe>> toSelect = new List<KeyValuePair<float, Keyframe>>();
            foreach (InterpolableDisplay display in _displayedInterpolables)
            {
                if (display.gameObject.activeSelf == false)
                    break;
                float height = ((RectTransform)display.gameObject.transform).anchoredPosition.y;

                if (height > minY && height < maxY)
                {
                    foreach (KeyValuePair<float, Keyframe> pair in display.interpolable.obj.keyframes)
                    {
                        if (pair.Key >= firstTime && pair.Key <= secondTime)
                            toSelect.Add(pair);
                    }

                }
            }
            if (Input.GetKey(KeyCode.LeftControl))
                SelectAddKeyframes(toSelect);
            else
                SelectKeyframes(toSelect);
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
            UpdateInterpolableSelection();
        }

        private void SelectInterpolable(params Interpolable[] interpolables)
        {
            _selectedInterpolables.Clear();
            SelectAddInterpolable(interpolables);
        }

        private void ClearSelectedInterpolables()
        {
            _selectedInterpolables.Clear();
            UpdateInterpolableSelection();
        }

        private void UpdateInterpolableSelection()
        {
            foreach (InterpolableDisplay display in _displayedInterpolables)
            {
                bool selected = _selectedInterpolables.Any(e => e == display.interpolable.obj);
                display.selectedOutline.gameObject.SetActive(selected);
                display.background.material.SetFloat("_DrawChecker", selected ? 1f : 0f);
                display.gridBackground.material.SetFloat("_DrawChecker", selected ? 1f : 0f);
                display.name.fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
                // Forcing the texture to refresh
                display.background.enabled = false;
                display.background.enabled = true;
                display.gridBackground.enabled = false;
                display.gridBackground.enabled = true;
            }
        }

        private void UpdateGrid()
        {
            _durationInputField.text = $"{Mathf.FloorToInt(_duration / 60):00}:{(_duration % 60):00.00}";

            _horizontalScrollView.content.sizeDelta = new Vector2(_baseGridWidth * _zoomLevel * _duration / 10f, _horizontalScrollView.content.sizeDelta.y);
            UpdateGridMaterial();
            int max = Mathf.CeilToInt(_duration / _blockLength);
            int textIndex = 0;
            for (int i = 1; i < max; i++)
            {
                Text t;
                if (textIndex < _timeTexts.Count)
                    t = _timeTexts[textIndex];
                else
                {
                    t = UIUtility.CreateText("Time " + textIndex, _textsContainer);
                    t.alignByGeometry = true;
                    t.alignment = TextAnchor.MiddleCenter;
                    t.color = Color.white;
                    t.raycastTarget = false;
                    t.rectTransform.SetRect(Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(60f, 0f));
                    _timeTexts.Add(t);
                }
                t.text = $"{Mathf.FloorToInt((i * _blockLength) / 60):00}:{((i * _blockLength) % 60):00.##}";
                t.gameObject.SetActive(true);
                t.rectTransform.anchoredPosition = new Vector2(i * _blockLength * _baseGridWidth * _zoomLevel / 10, t.rectTransform.anchoredPosition.y);
                ++textIndex;
            }
            for (; textIndex < _timeTexts.Count; textIndex++)
                _timeTexts[textIndex].gameObject.SetActive(false);


            bool showAll = _allToggle.isOn;
            int keyframeIndex = 0;
            int interpolableIndex = 0;
            UpdateKeyframesTree(_interpolablesTree.tree, showAll, ref interpolableIndex, ref keyframeIndex);

            for (; keyframeIndex < _displayedKeyframes.Count; ++keyframeIndex)
            {
                KeyframeDisplay display = _displayedKeyframes[keyframeIndex];
                display.gameObject.SetActive(false);
                display.keyframe = null;
            }

            UpdateKeyframeSelection();

            UpdateCursor();

            this.ExecuteDelayed2(() => _keyframesContainer.sizeDelta = new Vector2(_keyframesContainer.sizeDelta.x, _verticalScrollView.content.rect.height), 2);
        }

        private void UpdateKeyframesTree(List<INode> nodes, bool showAll, ref int interpolableIndex, ref int keyframeIndex)
        {
            foreach (INode node in nodes)
            {
                switch (node.type)
                {
                    case INodeType.Leaf:
                        Interpolable interpolable = ((LeafNode<Interpolable>)node).obj;
                        if (showAll == false && ((interpolable.oci != null && interpolable.oci != _selectedOCI) || !interpolable.ShouldShow()))
                            continue;

                        if (interpolable.name.IndexOf(_interpolablesSearchField.text, StringComparison.OrdinalIgnoreCase) == -1)
                            continue;

                        InterpolableDisplay interpolableDisplay = _displayedInterpolables[interpolableIndex];

                        foreach (KeyValuePair<float, Keyframe> keyframePair in interpolable.keyframes)
                        {
                            KeyframeDisplay display;
                            if (keyframeIndex < _displayedKeyframes.Count)
                                display = _displayedKeyframes[keyframeIndex];
                            else
                            {
                                display = new KeyframeDisplay();
                                display.gameObject = GameObject.Instantiate(_keyframePrefab);
                                display.gameObject.hideFlags = HideFlags.None;
                                display.image = display.gameObject.transform.Find("RawImage").GetComponent<RawImage>();

                                display.gameObject.transform.SetParent(_keyframesContainer);
                                display.gameObject.transform.localPosition = Vector3.zero;
                                display.gameObject.transform.localScale = Vector3.one;

                                PointerEnterHandler pointerEnter = display.gameObject.AddComponent<PointerEnterHandler>();
                                pointerEnter.onPointerEnter = (e) =>
                                {
                                    _tooltip.transform.parent.gameObject.SetActive(true);
                                    float t = display.keyframe.parent.keyframes.First(k => k.Value == display.keyframe).Key;
                                    _tooltip.text = $"T: {Mathf.FloorToInt(t / 60):00}:{t % 60:00.########}\nV: {display.keyframe.value}";
                                };
                                pointerEnter.onPointerExit = (e) => { _tooltip.transform.parent.gameObject.SetActive(false); };
                                PointerDownHandler pointerDown = display.gameObject.AddComponent<PointerDownHandler>();
                                pointerDown.onPointerDown = (e) =>
                                {
                                    if (Input.GetKey(KeyCode.LeftAlt))
                                        return;
                                    switch (e.button)
                                    {
                                        case PointerEventData.InputButton.Left:
                                            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                                                SelectAddKeyframes(display.keyframe.parent.keyframes.First(k => k.Value == display.keyframe));
                                            else if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                                            {
                                                KeyValuePair<float, Keyframe> lastSelected = _selectedKeyframes.LastOrDefault(k => k.Value.parent == display.keyframe.parent);
                                                if (lastSelected.Value != null)
                                                {
                                                    KeyValuePair<float, Keyframe> selectingNow = display.keyframe.parent.keyframes.First(k => k.Value == display.keyframe);
                                                    float minTime;
                                                    float maxTime;
                                                    if (lastSelected.Key < selectingNow.Key)
                                                    {
                                                        minTime = lastSelected.Key;
                                                        maxTime = selectingNow.Key;
                                                    }
                                                    else
                                                    {
                                                        minTime = selectingNow.Key;
                                                        maxTime = lastSelected.Key;
                                                    }
                                                    SelectAddKeyframes(display.keyframe.parent.keyframes.Where(k => k.Key > minTime && k.Key < maxTime));
                                                    SelectAddKeyframes(selectingNow);
                                                }
                                                else
                                                    SelectAddKeyframes(display.keyframe.parent.keyframes.First(k => k.Value == display.keyframe));
                                            }
                                            else
                                                SelectKeyframes(display.keyframe.parent.keyframes.First(k => k.Value == display.keyframe));

                                            break;
                                        case PointerEventData.InputButton.Right:
                                            SeekPlaybackTime(display.keyframe.parent.keyframes.First(k => k.Value == display.keyframe).Key);
                                            break;
                                        case PointerEventData.InputButton.Middle:
                                            if (Input.GetKey(KeyCode.LeftControl))
                                            {
                                                List<KeyValuePair<float, Keyframe>> toDelete = new List<KeyValuePair<float, Keyframe>>();
                                                if (Input.GetKey(KeyCode.LeftShift))
                                                    toDelete.AddRange(_selectedKeyframes);
                                                KeyValuePair<float, Keyframe> kPair = display.keyframe.parent.keyframes.FirstOrDefault(k => k.Value == display.keyframe);
                                                if (kPair.Value != null)
                                                    toDelete.Add(kPair);
                                                if (toDelete.Count != 0)
                                                {
                                                    DeleteKeyframes(toDelete);
                                                    _tooltip.transform.parent.gameObject.SetActive(false);
                                                }
                                            }
                                            break;
                                    }
                                };

                                DragHandler dragHandler = display.gameObject.AddComponent<DragHandler>();
                                dragHandler.onBeginDrag = e =>
                                {
                                    if (Input.GetKey(KeyCode.LeftAlt) == false)
                                        return;
                                    Vector2 localPoint;
                                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_keyframesContainer, e.position, e.pressEventCamera, out localPoint))
                                    {
                                        _selectedKeyframesXOffset.Clear();
                                        foreach (KeyValuePair<float, Keyframe> selectedKeyframe in _selectedKeyframes)
                                        {
                                            KeyframeDisplay selectedDisplay = _displayedKeyframes.Find(d => d.keyframe == selectedKeyframe.Value);
                                            _selectedKeyframesXOffset.Add(selectedDisplay, ((RectTransform)selectedDisplay.gameObject.transform).anchoredPosition.x - localPoint.x);
                                        }
                                    }
                                    if (_selectedKeyframesXOffset.Count != 0)
                                        isPlaying = false;
                                    e.Reset();
                                };
                                dragHandler.onDrag = e =>
                                {
                                    if (_selectedKeyframesXOffset.Count == 0)
                                        return;
                                    Vector2 localPoint;
                                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_keyframesContainer, e.position, e.pressEventCamera, out localPoint))
                                    {
                                        float x = localPoint.x;
                                        foreach (KeyValuePair<KeyframeDisplay, float> pair in _selectedKeyframesXOffset)
                                        {
                                            float localX = localPoint.x + pair.Value;
                                            if (localX < 0f)
                                                x = localPoint.x - localX;
                                        }

                                        if (Input.GetKey(KeyCode.LeftShift))
                                        {
                                            float time = 10f * x / (_baseGridWidth * _zoomLevel);
                                            float beat = _blockLength / _divisions;
                                            float mod = time % beat;
                                            if (mod / beat > 0.5f)
                                                time += beat - mod;
                                            else
                                                time -= mod;
                                            x = (time * _baseGridWidth * _zoomLevel) / 10f - _selectedKeyframesXOffset[display];
                                        }

                                        foreach (KeyValuePair<KeyframeDisplay, float> pair in _selectedKeyframesXOffset)
                                        {
                                            RectTransform rt = ((RectTransform)pair.Key.gameObject.transform);
                                            rt.anchoredPosition = new Vector2(x + pair.Value, rt.anchoredPosition.y);
                                        }
                                    }
                                    e.Reset();
                                };

                                dragHandler.onEndDrag = e =>
                                {
                                    if (_selectedKeyframesXOffset.Count == 0)
                                        return;
                                    foreach (KeyValuePair<KeyframeDisplay, float> pair in _selectedKeyframesXOffset)
                                    {
                                        RectTransform rt = ((RectTransform)pair.Key.gameObject.transform);
                                        float time = 10f * rt.anchoredPosition.x / (_baseGridWidth * _zoomLevel);
                                        MoveKeyframe(pair.Key.keyframe, time);

                                        int index = _selectedKeyframes.FindIndex(k => k.Value == pair.Key.keyframe);
                                        _selectedKeyframes[index] = new KeyValuePair<float, Keyframe>(time, pair.Key.keyframe);
                                    }

                                    e.Reset();
                                    UpdateKeyframeWindow(false);
                                    _selectedKeyframesXOffset.Clear();
                                };

                                _displayedKeyframes.Add(display);
                            }
                            display.gameObject.SetActive(true);
                            ((RectTransform)display.gameObject.transform).anchoredPosition = new Vector2(_baseGridWidth * _zoomLevel * keyframePair.Key / 10f, ((RectTransform)interpolableDisplay.gameObject.transform).anchoredPosition.y);
                            display.keyframe = keyframePair.Value;
                            ++keyframeIndex;
                        }
                        ++interpolableIndex;
                        break;
                    case INodeType.Group:
                        GroupNode<InterpolableGroup> group = (GroupNode<InterpolableGroup>)node;
                        if (group.obj.expanded)
                            UpdateKeyframesTree(group.children, showAll, ref interpolableIndex, ref keyframeIndex);
                        break;
                }
            }
        }

        private void UpdateGridMaterial()
        {
            _gridImage.material.SetFloat("_TilingX", _duration / 10f);
            _gridImage.material.SetFloat("_BlockLength", _blockLength);
            _gridImage.material.SetFloat("_Divisions", _divisions);
            _gridImage.enabled = false;
            _gridImage.enabled = true;
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
            _keyframeSelectionSize = _selectedKeyframes.Max(k => k.Key) - _selectedKeyframes.Min(k => k.Key);
            UpdateKeyframeSelection();
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
        }

        private void UpdateKeyframeSelection()
        {
            foreach (KeyframeDisplay display in _displayedKeyframes)
                display.image.color = _selectedKeyframes.Any(k => k.Value == display.keyframe) ? Color.green : Color.red;
        }

        private void ScaleKeyframeSelection(float scrollDelta)
        {
            float min = float.PositiveInfinity;
            float max = float.NegativeInfinity;
            foreach (KeyValuePair<float, Keyframe> pair in _selectedKeyframes)
            {
                if (pair.Key < min)
                    min = pair.Key;
                if (pair.Key > max)
                    max = pair.Key;
            }
            if (Mathf.Approximately(min, max))
                return;
            double currentSize = max - min;

            double newSize;
            bool conflicting;
            int multiplier = 1;
            do
            {
                conflicting = false;
                double sizeMultiplier = Math.Round(Math.Round(currentSize * 10) / _keyframeSelectionSize + multiplier * (scrollDelta > 0 ? 1 : -1)) / 10;
                bool clamped = false;
                if (sizeMultiplier < 0.1)
                {
                    clamped = true;
                    sizeMultiplier = 0.1;
                }
                newSize = sizeMultiplier * _keyframeSelectionSize;
                foreach (KeyValuePair<float, Keyframe> pair in _selectedKeyframes)
                {
                    float newTime = (float)(((pair.Key - min) * newSize) / currentSize + min);
                    if (pair.Value.parent.keyframes.TryGetValue(newTime, out Keyframe otherKeyframe) && otherKeyframe != pair.Value)
                    {
                        conflicting = true;
                        ++multiplier;
                        break;
                    }
                }
                if (clamped && conflicting)
                    return;
            } while (conflicting);

            for (int i = 0; i < _selectedKeyframes.Count; i++)
            {
                KeyValuePair<float, Keyframe> pair = _selectedKeyframes[i];
                float newTime = (float)(((pair.Key - min) * newSize) / currentSize + min);
                MoveKeyframe(pair.Value, newTime);
                _selectedKeyframes[i] = new KeyValuePair<float, Keyframe>(newTime, pair.Value);
            }
            UpdateKeyframeWindow(false);
            UpdateGrid();
        }

        private void OnKeyframeContainerMouseDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Middle && Input.GetKey(KeyCode.LeftControl) == false && RectTransformUtility.ScreenPointToLocalPointInRectangle(_keyframesContainer, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                float time = 10f * localPoint.x / (_baseGridWidth * _zoomLevel);
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    float beat = _blockLength / _divisions;
                    float mod = time % beat;
                    if (mod / beat > 0.5f)
                        time += beat - mod;
                    else
                        time -= mod;
                }
                if (Input.GetKey(KeyCode.LeftAlt) && _selectedInterpolables.Count != 0)
                {
                    foreach (Interpolable selectedInterpolable in _selectedInterpolables)
                        AddKeyframe(selectedInterpolable, time);
                    UpdateGrid();
                }
                else
                {
                    if (_selectedInterpolables.Count != 0)
                        ClearSelectedInterpolables();
                    InterpolableModel model = null;
                    float distance = float.MaxValue;
                    foreach (InterpolableDisplay display in _displayedInterpolables)
                    {
                        if (!display.gameObject.activeSelf)
                            continue;
                        float distance2 = Mathf.Abs(localPoint.y - ((RectTransform)display.gameObject.transform).anchoredPosition.y);
                        if (distance2 < distance)
                        {
                            distance = distance2;
                            model = display.interpolable.obj;
                        }
                    }
                    foreach (InterpolableModelDisplay display in _displayedInterpolableModels)
                    {
                        if (!display.gameObject.activeSelf)
                            continue;
                        float distance2 = Mathf.Abs(localPoint.y - ((RectTransform)display.gameObject.transform).anchoredPosition.y);
                        if (distance2 < distance)
                        {
                            distance = distance2;
                            model = display.model;
                        }
                    }
                    if (model != null)
                    {
                        Interpolable interpolable;
                        if (model is Interpolable)
                            interpolable = (Interpolable)model;
                        else
                            interpolable = AddInterpolable(model);

                        if (interpolable != null)
                        {
                            AddKeyframe(interpolable, time);
                            UpdateGrid();
                        }
                    }
                }
            }
        }

        private void AddKeyframe(Interpolable interpolable, float time)
        {
            try
            {
                Keyframe keyframe;
                KeyValuePair<float, Keyframe> pair = interpolable.keyframes.LastOrDefault(k => k.Key < time);
                if (pair.Value != null)
                    keyframe = new Keyframe(interpolable.GetValue(), interpolable, pair.Value.curve);
                else
                    keyframe = new Keyframe(interpolable.GetValue(), interpolable, AnimationCurve.Linear(0f, 0f, 1f, 1f));
                interpolable.keyframes.Add(time, keyframe);
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
            CopyKeyframes();
            if (_selectedKeyframes.Count != 0)
                DeleteKeyframes(_selectedKeyframes, false);
        }

        private void PasteKeyframes()
        {
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
                        if (pair.Key >= time)
                            MoveKeyframe(pair.Value, (float)(pair.Key + duration));
                    }
                }
            }
            else if (_copiedKeyframes.Any(k => k.Value.parent.keyframes.ContainsKey(time + k.Key - startOffset)))
                return;
            foreach (KeyValuePair<float, Keyframe> pair in _copiedKeyframes)
            {
                float finalTime = time + pair.Key - startOffset;
                Keyframe newKeyframe = new Keyframe(pair.Value);
                pair.Value.parent.keyframes.Add(finalTime, newKeyframe);
                // This is dumb as shit but I have no choice
                toSelect.Add(new KeyValuePair<float, Keyframe>(finalTime, newKeyframe));
            }
            SelectKeyframes(toSelect);
            UpdateGrid();
        }

        private void MoveKeyframe(Keyframe keyframe, float destinationTime)
        {
            Logger.LogError(keyframe.parent.keyframes.IndexOfValue(keyframe));
            keyframe.parent.keyframes.RemoveAt(keyframe.parent.keyframes.IndexOfValue(keyframe));
            keyframe.parent.keyframes.Add(destinationTime, keyframe);
            int i = _selectedKeyframes.FindIndex(k => k.Value == keyframe);
            if (i != -1)
                _selectedKeyframes[i] = new KeyValuePair<float, Keyframe>(destinationTime, keyframe);
        }

        private void OnGridTopMouse(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && RectTransformUtility.ScreenPointToLocalPointInRectangle(_gridTop, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                float time = 10f * localPoint.x / (_baseGridWidth * _zoomLevel);
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    float beat = _blockLength / _divisions;
                    float mod = time % beat;
                    if (mod / beat > 0.5f)
                        time += beat - mod;
                    else
                        time -= mod;
                }
                time = Mathf.Clamp(time, 0, _duration);
                SeekPlaybackTime(time);
            }
        }

        private void OnResizeWindow(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && RectTransformUtility.ScreenPointToLocalPointInRectangle(_timelineWindow, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                localPoint.x = Mathf.Clamp(localPoint.x, 615f, ((RectTransform)_ui.transform).rect.width * 0.85f);
                localPoint.y = Mathf.Clamp(localPoint.y, 330f, ((RectTransform)_ui.transform).rect.height * 0.85f);
                _timelineWindow.sizeDelta = localPoint;
            }
        }

        private void SeekPlaybackTime(float t)
        {
            if (t == _playbackTime)
                return;
            _playbackTime = t;
            _startTime = Time.time - _playbackTime;
            bool isPlaying = _isPlaying;
            _isPlaying = true;
            UpdateCursor();
            Interpolate(true);
            Interpolate(false);
            _isPlaying = isPlaying;
        }

        private void ToggleSingleFilesPanel()
        {
            _singleFilesPanel.SetActive(!_singleFilesPanel.activeSelf);
            if (_singleFilesPanel.activeSelf)
                UpdateSingleFilesPanel();
        }

        private void UpdateSingleFilesPanel()
        {
            if (Directory.Exists(_singleFilesFolder) == false)
                return;
            string[] files = Directory.GetFiles(_singleFilesFolder, "*.xml");
            int i = 0;
            for (; i < files.Length; i++)
            {
                SingleFileDisplay display;
                if (i < _displayedSingleFiles.Count)
                    display = _displayedSingleFiles[i];
                else
                {
                    display = new SingleFileDisplay();
                    display.toggle = GameObject.Instantiate(_singleFilePrefab).GetComponent<Toggle>();
                    display.toggle.gameObject.hideFlags = HideFlags.None;
                    display.text = display.toggle.GetComponentInChildren<Text>();

                    display.toggle.transform.SetParent(_singleFilesContainer);
                    display.toggle.transform.localScale = Vector3.one;
                    display.toggle.transform.localPosition = Vector3.zero;
                    display.toggle.group = _singleFilesContainer.GetComponent<ToggleGroup>();
                    _displayedSingleFiles.Add(display);
                }
                string fileName = Path.GetFileNameWithoutExtension(files[i]);

                display.toggle.gameObject.SetActive(true);
                display.toggle.onValueChanged = new Toggle.ToggleEvent();
                display.toggle.onValueChanged.AddListener(b =>
                {
                    if (display.toggle.isOn)
                        _singleFileNameField.text = fileName;
                });
                display.text.text = fileName;
            }

            for (; i < _displayedSingleFiles.Count; ++i)
                _displayedSingleFiles[i].toggle.gameObject.SetActive(false);
            UpdateSingleFileSelection();
        }

        private void UpdateSingleFileSelection()
        {
            foreach (SingleFileDisplay display in _displayedSingleFiles)
            {
                if (display.toggle.gameObject.activeSelf == false)
                    break;
                display.toggle.isOn = string.Compare(_singleFileNameField.text, display.text.text, StringComparison.OrdinalIgnoreCase) == 0;
            }
        }

        private void LoadSingleFile()
        {
            if (_selectedOCI == null)
                return;
            string path = Path.Combine(_singleFilesFolder, _singleFileNameField.text + ".xml");
            if (File.Exists(path))
                LoadSingle(path);
        }

        private void SaveSingleFile()
        {
            if (_selectedOCI == null)
                return;
            string selected = _singleFileNameField.text;
            foreach (char c in Path.GetInvalidPathChars())
                selected = selected.Replace(c.ToString(), "");
            if (string.IsNullOrEmpty(selected))
                return;
            if (Directory.Exists(_singleFilesFolder) == false)
                Directory.CreateDirectory(_singleFilesFolder);
            string path = Path.Combine(_singleFilesFolder, selected + ".xml");
            SaveSingle(path);
            _singleFileNameField.text = selected;
            UpdateSingleFilesPanel();
        }

        private void DeleteSingleFile()
        {
            UIUtility.DisplayConfirmationDialog(result =>
            {
                if (result)
                {
                    string path = Path.Combine(_singleFilesFolder, _singleFileNameField.text + ".xml");
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                        _singleFileNameField.text = "";
                        UpdateSingleFilesPanel();
                    }
                }
            }, "Are you sure you want to delete this file?");
        }
        #endregion

        #region Keyframe Window
        private void OpenKeyframeWindow()
        {
            _keyframeWindow.gameObject.SetActive(true);
        }

        private void CloseKeyframeWindow()
        {
            _keyframeWindow.gameObject.SetActive(false);
            _selectedKeyframeCurvePointIndex = -1;
        }

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

        private void UseCurrentTime()
        {
            float currentTime = _playbackTime % _duration;
            SaveKeyframeTime(currentTime);
            UpdateKeyframeTimeTextField();
        }

        private void DragAtCurrentTime()
        {
            float currentTime = _playbackTime % _duration;
            float min = _selectedKeyframes.Min(k => k.Key);

            // Checking if all keyframes can be moved.
            foreach (KeyValuePair<float, Keyframe> pair in _selectedKeyframes)
            {
                Keyframe potentialDuplicateKeyframe;
                float time = currentTime + pair.Key - min;
                if (pair.Value.parent.keyframes.TryGetValue(time, out potentialDuplicateKeyframe) && potentialDuplicateKeyframe != pair.Value)
                    return;
            }

            foreach (KeyValuePair<float, Keyframe> pair in _selectedKeyframes)
                pair.Value.parent.keyframes.Remove(pair.Key);

            for (int i = 0; i < _selectedKeyframes.Count; i++)
            {
                KeyValuePair<float, Keyframe> pair = _selectedKeyframes[i];
                float time = currentTime + pair.Key - min;
                pair.Value.parent.keyframes.Add(time, pair.Value);
                _selectedKeyframes[i] = new KeyValuePair<float, Keyframe>(time, pair.Value);
            }

            UpdateKeyframeTimeTextField();
            this.ExecuteDelayed2(UpdateCursor2);
            UpdateGrid();
        }

        private void UpdateSelectedKeyframeTime(string s)
        {
            float time = ParseTime(_keyframeTimeTextField.text);
            if (time < 0)
                return;
            SaveKeyframeTime(time);
        }

        private void UseCurrentValue()
        {
            foreach (KeyValuePair<float, Keyframe> pair in _selectedKeyframes)
                pair.Value.value = pair.Value.parent.GetValue();
            UpdateKeyframeValueText();
        }

        private void OnCurveMouseDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Middle && Input.GetKey(KeyCode.LeftControl) == false && RectTransformUtility.ScreenPointToLocalPointInRectangle(_curveContainer.rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                float time = localPoint.x / _curveContainer.rectTransform.rect.width;
                float value = localPoint.y / _curveContainer.rectTransform.rect.height;
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    float mod = time % _curveGridCellSizePercent;
                    if (mod / _curveGridCellSizePercent > 0.5f)
                        time += _curveGridCellSizePercent - mod;
                    else
                        time -= mod;
                    mod = value % _curveGridCellSizePercent;
                    if (mod / _curveGridCellSizePercent > 0.5f)
                        value += _curveGridCellSizePercent - mod;
                    else
                        value -= mod;
                }
                UnityEngine.Keyframe curveKey = new UnityEngine.Keyframe(time, value);
                if (curveKey.time < 0 || curveKey.time > 1 || curveKey.value < 0 || curveKey.value > 1)
                    return;
                _selectedKeyframeCurvePointIndex = _selectedKeyframes[0].Value.curve.AddKey(curveKey);
                SaveKeyframeCurve();
                UpdateCurve();
            }
        }

        private void UpdateCurvePointTime(string s)
        {
            AnimationCurve curve = _selectedKeyframes[0].Value.curve;
            if (_selectedKeyframeCurvePointIndex >= 1 && _selectedKeyframeCurvePointIndex < curve.length - 1)
            {
                UnityEngine.Keyframe curveKey = curve[_selectedKeyframeCurvePointIndex];
                float v;
                if (float.TryParse(_curveTimeInputField.text, out v))
                {
                    v = Mathf.Clamp(v, 0.001f, 0.999f);
                    if (!curve.keys.Any(k => k.time == v))
                    {
                        curveKey.time = v;
                        curve.RemoveKey(_selectedKeyframeCurvePointIndex);
                        _selectedKeyframeCurvePointIndex = curve.AddKey(curveKey);
                        SaveKeyframeCurve();
                    }
                }
            }
            UpdateCurvePointTime();
            UpdateCurve();
        }

        private void UpdateCurvePointTime(float f)
        {
            AnimationCurve curve = _selectedKeyframes[0].Value.curve;
            if (_selectedKeyframeCurvePointIndex >= 1 && _selectedKeyframeCurvePointIndex < curve.length - 1)
            {
                UnityEngine.Keyframe curveKey = curve[_selectedKeyframeCurvePointIndex];
                float v = Mathf.Clamp(_curveTimeSlider.value, 0.001f, 0.999f);
                if (curve.keys.Any(k => k.time == v) == false)
                {
                    curveKey.time = v;
                    curve.RemoveKey(_selectedKeyframeCurvePointIndex);
                    _selectedKeyframeCurvePointIndex = curve.AddKey(curveKey);
                    SaveKeyframeCurve();
                }
            }
            UpdateCurvePointTime();
            UpdateCurve();
        }

        private void UpdateCurvePointTime()
        {
            UnityEngine.Keyframe curveKey;
            AnimationCurve curve = _selectedKeyframes[0].Value.curve;
            if (_selectedKeyframeCurvePointIndex != -1 && _selectedKeyframeCurvePointIndex < curve.length)
                curveKey = curve[_selectedKeyframeCurvePointIndex];
            else
                curveKey = new UnityEngine.Keyframe();
            _curveTimeInputField.text = curveKey.time.ToString("0.00000");
            _curveTimeSlider.SetValueNoCallback(curveKey.time);
        }

        private void UpdateCurvePointValue(string s)
        {
            AnimationCurve curve = _selectedKeyframes[0].Value.curve;
            if (_selectedKeyframeCurvePointIndex >= 1 && _selectedKeyframeCurvePointIndex < curve.length - 1)
            {
                UnityEngine.Keyframe curveKey = curve[_selectedKeyframeCurvePointIndex];
                float v;
                if (float.TryParse(_curveValueInputField.text, out v))
                {
                    curveKey.value = v;
                    curve.RemoveKey(_selectedKeyframeCurvePointIndex);
                    _selectedKeyframeCurvePointIndex = curve.AddKey(curveKey);
                    SaveKeyframeCurve();
                }
            }
            UpdateCurvePointValue();
            UpdateCurve();
        }

        private void UpdateCurvePointValue(float f)
        {
            AnimationCurve curve = _selectedKeyframes[0].Value.curve;
            if (_selectedKeyframeCurvePointIndex >= 1 && _selectedKeyframeCurvePointIndex < curve.length - 1)
            {
                UnityEngine.Keyframe curveKey = curve[_selectedKeyframeCurvePointIndex];
                curveKey.value = _curveValueSlider.value;
                curve.RemoveKey(_selectedKeyframeCurvePointIndex);
                _selectedKeyframeCurvePointIndex = curve.AddKey(curveKey);
                SaveKeyframeCurve();
            }
            UpdateCurvePointValue();
            UpdateCurve();
        }

        private void UpdateCurvePointValue()
        {
            UnityEngine.Keyframe curveKey;
            AnimationCurve curve = _selectedKeyframes[0].Value.curve;
            if (_selectedKeyframeCurvePointIndex != -1 && _selectedKeyframeCurvePointIndex < curve.length)
                curveKey = curve[_selectedKeyframeCurvePointIndex];
            else
                curveKey = new UnityEngine.Keyframe();
            _curveValueInputField.text = curveKey.value.ToString("0.00000");
            _curveValueSlider.SetValueNoCallback(curveKey.value);
        }

        private void UpdateCurvePointInTangent(string s)
        {
            AnimationCurve curve = _selectedKeyframes[0].Value.curve;
            if (_selectedKeyframeCurvePointIndex != -1 && _selectedKeyframeCurvePointIndex < curve.length)
            {
                UnityEngine.Keyframe curveKey = curve[_selectedKeyframeCurvePointIndex];
                float v;
                if (float.TryParse(_curveInTangentInputField.text, out v))
                {
                    if (v == 90f || v == -90f)
                        curveKey.inTangent = float.NegativeInfinity;
                    else
                        curveKey.inTangent = Mathf.Tan(v * Mathf.Deg2Rad);
                    curve.RemoveKey(_selectedKeyframeCurvePointIndex);
                    _selectedKeyframeCurvePointIndex = curve.AddKey(curveKey);
                    SaveKeyframeCurve();
                }
            }
            UpdateCurvePointInTangent();
            UpdateCurve();
        }

        private void UpdateCurvePointInTangent(float f)
        {
            AnimationCurve curve = _selectedKeyframes[0].Value.curve;
            if (_selectedKeyframeCurvePointIndex != -1 && _selectedKeyframeCurvePointIndex < curve.length)
            {
                UnityEngine.Keyframe curveKey = curve[_selectedKeyframeCurvePointIndex];
                if (_curveInTangentSlider.value == 90f || _curveInTangentSlider.value == -90f)
                    curveKey.inTangent = float.PositiveInfinity;
                else
                    curveKey.inTangent = Mathf.Tan(_curveInTangentSlider.value * Mathf.Deg2Rad);
                curve.RemoveKey(_selectedKeyframeCurvePointIndex);
                _selectedKeyframeCurvePointIndex = curve.AddKey(curveKey);
                SaveKeyframeCurve();
            }
            UpdateCurvePointInTangent();
            UpdateCurve();
        }

        private void UpdateCurvePointInTangent()
        {
            UnityEngine.Keyframe curveKey;
            AnimationCurve curve = _selectedKeyframes[0].Value.curve;
            if (_selectedKeyframeCurvePointIndex != -1 && _selectedKeyframeCurvePointIndex < curve.length)
                curveKey = curve[_selectedKeyframeCurvePointIndex];
            else
                curveKey = new UnityEngine.Keyframe();
            float v = Mathf.Atan(curveKey.inTangent) * Mathf.Rad2Deg;
            _curveInTangentInputField.text = v.ToString("0.000");
            _curveInTangentSlider.SetValueNoCallback(v);
        }

        private void UpdateCurvePointOutTangent(string s)
        {
            AnimationCurve curve = _selectedKeyframes[0].Value.curve;
            if (_selectedKeyframeCurvePointIndex != -1 && _selectedKeyframeCurvePointIndex < curve.length)
            {
                UnityEngine.Keyframe curveKey = curve[_selectedKeyframeCurvePointIndex];
                float v;
                if (float.TryParse(_curveOutTangentInputField.text, out v))
                {
                    if (v == 90f || v == -90f)
                        curveKey.outTangent = float.NegativeInfinity;
                    else
                        curveKey.outTangent = Mathf.Tan(v * Mathf.Deg2Rad);
                    curve.RemoveKey(_selectedKeyframeCurvePointIndex);
                    _selectedKeyframeCurvePointIndex = curve.AddKey(curveKey);
                    SaveKeyframeCurve();
                }
            }
            UpdateCurvePointOutTangent();
            UpdateCurve();
        }

        private void UpdateCurvePointOutTangent(float f)
        {
            AnimationCurve curve = _selectedKeyframes[0].Value.curve;
            if (_selectedKeyframeCurvePointIndex != -1 && _selectedKeyframeCurvePointIndex < curve.length)
            {
                UnityEngine.Keyframe curveKey = curve[_selectedKeyframeCurvePointIndex];
                if (_curveOutTangentSlider.value == 90f || _curveOutTangentSlider.value == -90f)
                    curveKey.outTangent = float.NegativeInfinity;
                else
                    curveKey.outTangent = Mathf.Tan(_curveOutTangentSlider.value * Mathf.Deg2Rad);
                curve.RemoveKey(_selectedKeyframeCurvePointIndex);
                _selectedKeyframeCurvePointIndex = curve.AddKey(curveKey);
                SaveKeyframeCurve();
            }
            UpdateCurvePointOutTangent();
            UpdateCurve();
        }

        private void UpdateCurvePointOutTangent()
        {
            UnityEngine.Keyframe curveKey;
            AnimationCurve curve = _selectedKeyframes[0].Value.curve;
            if (_selectedKeyframeCurvePointIndex != -1 && _selectedKeyframeCurvePointIndex < curve.length)
                curveKey = curve[_selectedKeyframeCurvePointIndex];
            else
                curveKey = new UnityEngine.Keyframe();
            float v = Mathf.Atan(curveKey.outTangent) * Mathf.Rad2Deg;
            _curveOutTangentInputField.text = v.ToString("0.000");
            _curveOutTangentSlider.SetValueNoCallback(v);
        }

        private void CopyKeyframeCurve()
        {
            _copiedKeyframeCurve.keys = _selectedKeyframes[0].Value.curve.keys;
        }

        private void PasteKeyframeCurve()
        {
            _selectedKeyframes[0].Value.curve.keys = _copiedKeyframeCurve.keys;
            SaveKeyframeCurve();
            UpdateCurve();
        }

        private void InvertKeyframeCurve()
        {
            AnimationCurve curve = _selectedKeyframes[0].Value.curve;
            UnityEngine.Keyframe[] keys = curve.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                UnityEngine.Keyframe key = keys[i];
                key.time = 1 - key.time;
                key.value = 1 - key.value;
                float tmp = key.inTangent;
                key.inTangent = key.outTangent;
                key.outTangent = tmp;
                keys[i] = key;
            }

            Array.Reverse(keys);
            curve.keys = keys;
            SaveKeyframeCurve();
            UpdateCurve();
        }

        private void ApplyKeyframeCurvePreset(AnimationCurve preset)
        {
            _selectedKeyframes[0].Value.curve = new AnimationCurve(preset.keys);
            SaveKeyframeCurve();
            UpdateCurve();
        }

        private void UpdateCursor2()
        {
            if (!_keyframeWindow.activeSelf)
                return;
            if (_selectedKeyframes.Count == 1)
            {
                KeyValuePair<float, Keyframe> selectedKeyframe = _selectedKeyframes[0];

                if (_playbackTime >= selectedKeyframe.Key)
                {
                    KeyValuePair<float, Keyframe> after = selectedKeyframe.Value.parent.keyframes.FirstOrDefault(k => k.Key > selectedKeyframe.Key);
                    if (after.Value != null && _playbackTime <= after.Key)
                    {
                        _cursor2.gameObject.SetActive(true);

                        float normalizedTime = (_playbackTime - selectedKeyframe.Key) / (after.Key - selectedKeyframe.Key);
                        _cursor2.anchoredPosition = new Vector2(normalizedTime * _curveContainer.rectTransform.rect.width, _cursor2.anchoredPosition.y);
                    }
                    else
                        _cursor2.gameObject.SetActive(false);
                }
                else
                    _cursor2.gameObject.SetActive(false);
            }
            else
                _cursor2.gameObject.SetActive(false);
        }

        private void DeleteSelectedKeyframes()
        {
            UIUtility.DisplayConfirmationDialog(result =>
                    {
                        if (result)
                            DeleteKeyframes(_selectedKeyframes);
                    }, _selectedKeyframes.Count == 1 ? "Are you sure you want to delete this Keyframe?" : "Are you sure you want to delete these Keyframes?"
            );
        }

        private void DeleteKeyframes(params KeyValuePair<float, Keyframe>[] keyframes)
        {
            DeleteKeyframes((IEnumerable<KeyValuePair<float, Keyframe>>)keyframes);
        }

        private void DeleteKeyframes(IEnumerable<KeyValuePair<float, Keyframe>> keyframes, bool removeInterpolables = true)
        {
            float min = float.PositiveInfinity;
            float max = float.NegativeInfinity;
            keyframes = keyframes.ToList();
            foreach (KeyValuePair<float, Keyframe> pair in keyframes)
            {
                if (pair.Value == null) //Just a safeguard.
                    continue;
                if (pair.Key < min)
                    min = pair.Key;
                if (pair.Key > max)
                    max = pair.Key;
                try
                {
                    pair.Value.parent.keyframes.Remove(pair.Key);
                    if (removeInterpolables && pair.Value.parent.keyframes.Count == 0)
                        RemoveInterpolable(pair.Value.parent);
                }
                catch (Exception e)
                {
                    Logger.LogError("Couldn't delete keyframe with time \"" + pair.Key + "\" and value \"" + pair.Value + "\" from interpolable \"" + pair.Value.parent + "\"\n" + e);
                }
            }

            if (Input.GetKey(KeyCode.LeftAlt))
            {
                double duration = max - min + (_blockLength / _divisions);
                //IDK, grouping didn't work so I'm doing it like this
                HashSet<Interpolable> processedParents = new HashSet<Interpolable>();
                foreach (KeyValuePair<float, Keyframe> k in keyframes)
                {
                    if (processedParents.Contains(k.Value.parent) != false)
                        continue;
                    processedParents.Add(k.Value.parent);
                    foreach (KeyValuePair<float, Keyframe> pair in k.Value.parent.keyframes.ToList())
                        if (pair.Key > min)
                            MoveKeyframe(pair.Value, (float)(pair.Key - duration));
                }
            }
            _selectedKeyframes.RemoveAll(elem => elem.Value == null || keyframes.Any(k => k.Value == elem.Value));

            UpdateGrid();
            UpdateKeyframeWindow(false);
        }

        private void SaveKeyframeTime(float time)
        {
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

            UpdateKeyframeTimeTextField();
            this.ExecuteDelayed2(UpdateCursor2);
            UpdateGrid();
        }

        private void SaveKeyframeCurve()
        {
            AnimationCurve modifiedCurve = _selectedKeyframes[0].Value.curve;
            foreach (KeyValuePair<float, Keyframe> pair in _selectedKeyframes)
                pair.Value.curve = new AnimationCurve(modifiedCurve.keys);
        }

        private void UpdateKeyframeWindow(bool changeShowState = true)
        {
            if (_selectedKeyframes.Count == 0)
            {
                CloseKeyframeWindow();
                return;
            }
            if (changeShowState)
                OpenKeyframeWindow();

            IEnumerable<IGrouping<Interpolable, KeyValuePair<float, Keyframe>>> interpolableGroups = _selectedKeyframes.GroupBy(e => e.Value.parent);
            bool singleInterpolable = interpolableGroups.Count() == 1;
            Interpolable first = interpolableGroups.First().Key;
            _keyframeInterpolableNameText.text = singleInterpolable ? (string.IsNullOrEmpty(first.alias) ? first.name : first.alias) : "Multiple selected";
            _keyframeSelectPrevButton.interactable = _selectedKeyframes.Count == 1;
            _keyframeSelectNextButton.interactable = _selectedKeyframes.Count == 1;
            _keyframeTimeTextField.interactable = interpolableGroups.All(g => g.Count() == 1);
            _keyframeUseCurrentTimeButton.interactable = _keyframeTimeTextField.interactable;
            _keyframeDeleteButtonText.text = _selectedKeyframes.Count == 1 ? "Delete" : "Delete all";

            UpdateKeyframeTimeTextField();
            UpdateKeyframeValueText();
            this.ExecuteDelayed2(UpdateCurve);
            this.ExecuteDelayed2(UpdateCursor2);
        }

        private void UpdateKeyframeTimeTextField()
        {
            float t = _selectedKeyframes[0].Key;
            foreach (KeyValuePair<float, Keyframe> pair in _selectedKeyframes)
            {
                if (t != pair.Key)
                {
                    _keyframeTimeTextField.text = "Multiple times";
                    return;
                }
            }
            _keyframeTimeTextField.text = $"{Mathf.FloorToInt(t / 60):00}:{t % 60:00.########}";
        }

        private void UpdateKeyframeValueText()
        {
            object v = _selectedKeyframes[0].Value.value;
            foreach (KeyValuePair<float, Keyframe> pair in _selectedKeyframes)
            {
                if (v.Equals(pair.Value.value) == false)
                {
                    _keyframeValueText.text = "Multiple values";
                    return;
                }
            }
            _keyframeValueText.text = v != null ? v.ToString() : "null";
        }

        private void UpdateCurve()
        {
            if (_selectedKeyframes.Count == 0)
                return;
            AnimationCurve curve = _selectedKeyframes[0].Value.curve;
            foreach (KeyValuePair<float, Keyframe> pair in _selectedKeyframes)
            {
                if (CompareCurves(curve, pair.Value.curve) == false)
                {
                    curve = null;
                    break;
                }
            }
            int length = 0;
            if (curve != null)
            {
                length = curve.length;
                for (int i = 0; i < _curveTexture.width; i++)
                {
                    float v = curve.Evaluate(i / (_curveTexture.width - 1f));
                    _curveTexture.SetPixel(i, 0, new Color(v, v, v, v));
                }
            }
            else
            {
                for (int i = 0; i < _curveTexture.width; i++)
                    _curveTexture.SetPixel(i, 0, new Color(2f, 2f, 2f, 2f));
            }

            _curveTexture.Apply(false);
            _curveContainer.material.mainTexture = _curveTexture;
            _curveContainer.enabled = false;
            _curveContainer.enabled = true;

            int displayIndex = 0;
            for (int i = 0; i < length; ++i)
            {
                UnityEngine.Keyframe curveKeyframe = curve[i];
                CurveKeyframeDisplay display;
                if (displayIndex < _displayedCurveKeyframes.Count)
                    display = _displayedCurveKeyframes[displayIndex];
                else
                {
                    display = new CurveKeyframeDisplay();
                    display.gameObject = GameObject.Instantiate(_curveKeyframePrefab);
                    display.gameObject.hideFlags = HideFlags.None;
                    display.image = display.gameObject.transform.Find("RawImage").GetComponent<RawImage>();

                    display.gameObject.transform.SetParent(_curveContainer.transform);
                    display.gameObject.transform.localScale = Vector3.one;
                    display.gameObject.transform.localPosition = Vector3.zero;

                    display.pointerDownHandler = display.gameObject.AddComponent<PointerDownHandler>();
                    display.scrollHandler = display.gameObject.AddComponent<ScrollHandler>();
                    display.dragHandler = display.gameObject.AddComponent<DragHandler>();
                    display.pointerEnterHandler = display.gameObject.AddComponent<PointerEnterHandler>();

                    _displayedCurveKeyframes.Add(display);
                }

                int i1 = i;
                display.pointerDownHandler.onPointerDown = (e) =>
                {
                    if (e.button == PointerEventData.InputButton.Left)
                    {
                        _selectedKeyframeCurvePointIndex = i1;
                        UpdateCurve();
                    }
                    if (i1 == 0 || i1 == curve.length - 1)
                        return;
                    if (e.button == PointerEventData.InputButton.Middle && Input.GetKey(KeyCode.LeftControl))
                    {
                        foreach (KeyValuePair<float, Keyframe> pair in _selectedKeyframes)
                            pair.Value.curve.RemoveKey(i1);
                        UpdateCurve();
                    }
                };
                display.scrollHandler.onScroll = (e) =>
                {
                    UnityEngine.Keyframe k = curve[i1];
                    float offset = e.scrollDelta.y > 0 ? Mathf.PI / 180f : -Mathf.PI / 180f;
                    foreach (KeyValuePair<float, Keyframe> pair in _selectedKeyframes)
                        pair.Value.curve.RemoveKey(i1);
                    if (Input.GetKey(KeyCode.LeftControl))
                        k.inTangent = Mathf.Tan(Mathf.Atan(k.inTangent) + offset);
                    else if (Input.GetKey(KeyCode.LeftAlt))
                        k.outTangent = Mathf.Tan(Mathf.Atan(k.outTangent) + offset);
                    else
                    {
                        k.inTangent = Mathf.Tan(Mathf.Atan(k.inTangent) + offset);
                        k.outTangent = Mathf.Tan(Mathf.Atan(k.outTangent) + offset);
                    }
                    foreach (KeyValuePair<float, Keyframe> pair in _selectedKeyframes)
                        pair.Value.curve.AddKey(k);
                    UpdateCurve();
                };
                display.dragHandler.onDrag = (e) =>
                {
                    if (i1 == 0 || i1 == curve.length - 1)
                        return;
                    Vector2 localPoint;
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_curveContainer.rectTransform, e.position, e.pressEventCamera, out localPoint))
                    {
                        localPoint.x = Mathf.Clamp(localPoint.x, 0f, _curveContainer.rectTransform.rect.width);
                        localPoint.y = Mathf.Clamp(localPoint.y, 0f, _curveContainer.rectTransform.rect.height);
                        if (Input.GetKey(KeyCode.LeftShift))
                        {
                            Vector2 curveGridCellSize = new Vector2(_curveContainer.rectTransform.rect.width * _curveGridCellSizePercent, _curveContainer.rectTransform.rect.height * _curveGridCellSizePercent);
                            float mod = localPoint.x % curveGridCellSize.x;
                            if (mod / curveGridCellSize.x > 0.5f)
                                localPoint.x += curveGridCellSize.x - mod;
                            else
                                localPoint.x -= mod;
                            mod = localPoint.y % curveGridCellSize.y;
                            if (mod / curveGridCellSize.y > 0.5f)
                                localPoint.y += curveGridCellSize.y - mod;
                            else
                                localPoint.y -= mod;
                        }
                        ((RectTransform)display.gameObject.transform).anchoredPosition = localPoint;
                    }
                };
                display.dragHandler.onEndDrag = (e) =>
                {
                    if (i1 == 0 || i1 == curve.length - 1)
                        return;
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_curveContainer.rectTransform, e.position, e.pressEventCamera, out Vector2 localPoint))
                    {

                        float time = localPoint.x / _curveContainer.rectTransform.rect.width;
                        float value = localPoint.y / _curveContainer.rectTransform.rect.height;
                        if (Input.GetKey(KeyCode.LeftShift))
                        {
                            float mod = time % _curveGridCellSizePercent;
                            if (mod / _curveGridCellSizePercent > 0.5f)
                                time += _curveGridCellSizePercent - mod;
                            else
                                time -= mod;
                            mod = value % _curveGridCellSizePercent;
                            if (mod / _curveGridCellSizePercent > 0.5f)
                                value += _curveGridCellSizePercent - mod;
                            else
                                value -= mod;
                        }
                        if (time > 0 && time < 1 && value >= 0 && value <= 1 && curve.keys.Any(k => k.time == time) == false)
                        {
                            UnityEngine.Keyframe curveKey = curve[i1];
                            curveKey.time = time;
                            curveKey.value = value;
                            foreach (KeyValuePair<float, Keyframe> pair in _selectedKeyframes)
                            {
                                pair.Value.curve.RemoveKey(i1);
                                pair.Value.curve.AddKey(curveKey);
                            }
                        }
                        UpdateCurve();
                    }
                };
                display.pointerEnterHandler.onPointerEnter = (e) =>
                {
                    _tooltip.transform.parent.gameObject.SetActive(true);
                    UnityEngine.Keyframe k = curve[i1];
                    _tooltip.text = $"T: {k.time:0.000}, V: {k.value:0.###}\nIn: {Mathf.Atan(k.inTangent) * Mathf.Rad2Deg:0.#}, Out:{Mathf.Atan(k.outTangent) * Mathf.Rad2Deg:0.#}";
                };
                display.pointerEnterHandler.onPointerExit = (e) => { _tooltip.transform.parent.gameObject.SetActive(false); };

                display.image.color = i == _selectedKeyframeCurvePointIndex ? Color.green : (Color)new Color32(44, 153, 160, 255);
                display.gameObject.SetActive(true);
                ((RectTransform)display.gameObject.transform).anchoredPosition = new Vector2(curveKeyframe.time * _curveContainer.rectTransform.rect.width, curveKeyframe.value * _curveContainer.rectTransform.rect.height);
                ++displayIndex;
            }
            for (; displayIndex < _displayedCurveKeyframes.Count; ++displayIndex)
                _displayedCurveKeyframes[displayIndex].gameObject.SetActive(false);

            UpdateCurvePointTime();
            UpdateCurvePointValue();
            UpdateCurvePointInTangent();
            UpdateCurvePointOutTangent();
        }

        private bool CompareCurves(AnimationCurve x, AnimationCurve y)
        {
            if (x.length != y.length)
                return false;
            for (int i = 0; i < x.length; i++)
            {
                UnityEngine.Keyframe keyX = x.keys[i];
                UnityEngine.Keyframe keyY = y.keys[i];
                if (keyX.time != keyY.time ||
                    keyX.value != keyY.value ||
                    keyX.inTangent != keyY.inTangent ||
                    keyX.outTangent != keyY.outTangent)
                    return false;
            }
            return true;
        }

        #endregion

        #endregion

        #region Saves

#if KOIKATSU || AISHOUJO || HONEYSELECT2
        private void OnSceneLoad(string path)
        {
            PluginData data = ExtendedSave.GetSceneExtendedDataById(_extSaveKey);
            if (data == null)
                return;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml((string)data.data["sceneInfo"]);
            XmlNode node = doc.FirstChild;
            if (node == null)
                return;
            SceneLoad(path, node);
        }

        private void OnSceneImport(string path)
        {
            PluginData data = ExtendedSave.GetSceneExtendedDataById(_extSaveKey);
            if (data == null)
                return;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml((string)data.data["sceneInfo"]);
            XmlNode node = doc.FirstChild;
            if (node == null)
                return;
            SceneImport(path, node);
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
                _selectedOCI = null;
                _selectedKeyframes.Clear();

                List<KeyValuePair<int, ObjectCtrlInfo>> dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).ToList();
                SceneLoad(node, dic);

                UpdateInterpolablesView();
                CloseKeyframeWindow();
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
        }

        private void SceneLoad(XmlNode node, List<KeyValuePair<int, ObjectCtrlInfo>> dic)
        {
            ReadInterpolableTree(node, dic);

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
            _blockLengthInputField.text = _blockLength.ToString();
            _divisionsInputField.text = _divisions.ToString();
            _speedInputField.text = Time.timeScale.ToString("0.#####");
        }

        private void LoadSingle(string path)
        {
            List<KeyValuePair<int, ObjectCtrlInfo>> dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).ToList();
            XmlDocument document = new XmlDocument();
            try
            {
                document.Load(path);
                ReadInterpolableTree(document.FirstChild, dic, _selectedOCI);
#if KOIKATSU || SUNSHINE
                OCIChar character = _selectedOCI as OCIChar;
                StudioResolveInfo resolveInfo = UniversalAutoResolver.LoadedStudioResolutionInfo.FirstOrDefault(x => x.Slot == document.FirstChild.ReadInt("animationNo") && x.GUID == document.FirstChild.Attributes?["GUID"]?.InnerText && x.Group == document.FirstChild.ReadInt("animationGroup") && x.Category == document.FirstChild.ReadInt("animationCategory"));
                if (character != null)
                {
                    character.LoadAnime(document.FirstChild.ReadInt("animationGroup"),
                            document.FirstChild.ReadInt("animationCategory"),
                            resolveInfo != null ? resolveInfo.LocalSlot : document.FirstChild.ReadInt("animationNo"));
                }
#else           //AI&HS2 Studio use original ID(management number) for animation zipmods by default
                OCIChar character = _selectedOCI as OCIChar;
                if (character != null)
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

        private void SaveSingle(string path)
        {
            using (XmlTextWriter writer = new XmlTextWriter(path, Encoding.UTF8))
            {
                List<KeyValuePair<int, ObjectCtrlInfo>> dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).ToList();
                writer.WriteStartElement("root");

                OCIChar character = _selectedOCI as OCIChar;

                if (character != null)
                {
#if KOIKATSU || SUNSHINE
                    OICharInfo.AnimeInfo info = character.oiCharInfo.animeInfo;
                    StudioResolveInfo resolveInfo = UniversalAutoResolver.LoadedStudioResolutionInfo.FirstOrDefault(x => x.LocalSlot == info.no && x.Group == info.group && x.Category == info.category);
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
                    WriteInterpolableTree(node, writer, dic, leafNode => leafNode.obj.oci == _selectedOCI);
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
                    if (model == null /*|| model.isCompatibleWithTarget(oci) == false*/) //Might need to get this back on in the future, depending on how things end up going
                        return;
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
                                interpolable.keyframes.Add(time, keyframe);
                            }
                        }
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

        private void WriteInterpolable(Interpolable interpolable, XmlTextWriter writer, List<KeyValuePair<int, ObjectCtrlInfo>> dic)
        {
            if (interpolable.keyframes.Count == 0)
                return;
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

                        foreach (KeyValuePair<float, Keyframe> keyframePair in interpolable.keyframes)
                        {
                            localWriter.WriteStartElement("keyframe");
                            localWriter.WriteAttributeString("time", XmlConvert.ToString(keyframePair.Key));

                            interpolable.WriteValueToXml(localWriter, keyframePair.Value.value);
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

        [HarmonyPatch(typeof(GuideSelect), "OnPointerClick", new[] { typeof(PointerEventData) })]
        private static class GuideSelect_OnPointerClick_Patches
        {
            private static void Postfix()
            {
                GuideObject go = GuideObjectManager.Instance.selectObject;
                if (go != null && Input.GetKey(KeyCode.LeftAlt))
                    _self.HighlightInterpolable(_self._interpolables.FirstOrDefault(i => i.Value.parameter is GuideObject g && g == go).Value);
            }
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
                    return !_self._ui.gameObject.activeSelf;
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
