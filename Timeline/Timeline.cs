using System;
using System.Collections;
#if IPA
using Harmony;
using IllusionPlugin;
#elif BEPINEX
using HarmonyLib;
using BepInEx;
#endif
#if KOIKATSU
using Expression = ExpressionBone;
using ExtensibleSaveFormat;
#endif
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Xml;
using Studio;
using ToolBox;
using ToolBox.Extensions;
using UILib;
using UILib.ContextMenu;
using UILib.EventHandlers;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Type = System.Type;

namespace Timeline
{
#if BEPINEX
    [BepInPlugin(_guid, _name, _version)]
    [BepInProcess("CharaStudio")]
    [BepInDependency("com.bepis.bepinex.extendedsave")]
#endif
    public class Timeline : GenericPlugin
#if IPA
                            , IEnhancedPlugin
#endif
    {
        #region Constants
        internal const string _name = "Timeline";
        private const string _version = "1.1.0";
        private const string _guid = "com.joan6694.illusionplugins.timeline";
        internal const string _ownerId = "Timeline";
#if KOIKATSU
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

        private bool _isPlaying = false;
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
        public static bool isPlaying { get { return _self._isPlaying; } }
        #endregion

        #region Unity Methods
        protected override void Awake()
        {
            base.Awake();
            _self = this;

            _assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _singleFilesFolder = Path.Combine(_assemblyLocation, Path.Combine(_name, "Single Files"));

#if HONEYSELECT
            HSExtSave.HSExtSave.RegisterHandler("timeline", null, null, this.SceneLoad, this.SceneImport, this.SceneWrite, null, null);
#else
            ExtensibleSaveFormat.ExtendedSave.SceneBeingLoaded += this.OnSceneLoad;
            ExtensibleSaveFormat.ExtendedSave.SceneBeingImported += this.OnSceneImport;
            ExtensibleSaveFormat.ExtendedSave.SceneBeingSaved += this.OnSceneSave;
#endif
            var harmonyInstance = HarmonyExtensions.CreateInstance(_guid);
            harmonyInstance.PatchAllSafe();
            OCI_OnDelete_Patches.ManualPatch(harmonyInstance);
        }

#if HONEYSELECT
        protected override void LevelLoaded(int level)
        {
            if (level == 3)
                this.Init();
        }
#elif KOIKATSU
        protected override void LevelLoaded(Scene scene, LoadSceneMode mode)
        {
            base.LevelLoaded(scene, mode);
            if (mode == LoadSceneMode.Single && scene.buildIndex == 1)
                this.Init();
        }
#endif

        protected override void Update()
        {
            if (this._loaded == false)
                return;

            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.T))
            {
                this._ui.gameObject.SetActive(!this._ui.gameObject.activeSelf);
                if (this._ui.gameObject.activeSelf)
                    this.ExecuteDelayed(() =>
                    {
                        this.UpdateInterpolablesView();
                        this.ExecuteDelayed(() => // I know that's weird but it prevents the grid sometimes disappearing, fuck unity 5.3 I guess
                        {
                            this._grid.parent.gameObject.SetActive(false);
                            this._grid.parent.gameObject.SetActive(true);
                            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)this._grid.parent);
                        }, 4);
                    }, 2);
                else
                    UIUtility.HideContextMenu();
            }
            if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.T))
            {
                if (this._isPlaying)
                    Pause();
                else
                    Play();
            }

            this._totalActiveExpressions = this._allExpressions.Count(e => e.enabled && e.gameObject.activeInHierarchy);
            this._currentExpressionIndex = 0;

            //This bullshit is obligatory because when a node is selected on an object that is not selected in the workspace, the selected object doesn't get switched.
            GuideObject guideObject = this._selectedGuideObjects.FirstOrDefault();
            if (this._selectedGuideObject != guideObject)
            {
                ObjectCtrlInfo objectCtrlInfo = null;
                this._selectedGuideObject = guideObject;
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

                if (this._selectedOCI != objectCtrlInfo)
                {
                    this._selectedOCI = objectCtrlInfo;
                    this.UpdateInterpolablesView();
                    this.UpdateKeyframeWindow(false);
                }
            }

            if (this._toDelete.Count != 0)
            {
                this.RemoveInterpolables(this._toDelete);
                this._toDelete.Clear();
            }
            if (this._tooltip.transform.parent.gameObject.activeSelf)
            {
                Vector2 localPoint;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)this._tooltip.transform.parent.parent, Input.mousePosition, this._ui.worldCamera, out localPoint))
                    this._tooltip.transform.parent.position = this._tooltip.transform.parent.parent.TransformPoint(localPoint);
            }

            if (this._ui.gameObject.activeSelf)
            {
                if (Input.GetKey(KeyCode.LeftControl))
                {
                    if (Input.GetKeyDown(KeyCode.C))
                        this.CopyKeyframes();
                    else if (Input.GetKeyDown(KeyCode.X))
                        this.CutKeyframes();
                    else if (Input.GetKeyDown(KeyCode.V))
                        this.PasteKeyframes();
                }

                if (this._speedInputField.isFocused == false)
                    this._speedInputField.text = Time.timeScale.ToString("0.#####");
            }

            this.InterpolateBefore();
        }

        private void PostLateUpdate()
        {
            if (this._ui.gameObject.activeSelf && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(2)) && UIUtility.IsContextMenuDisplayed() && UIUtility.WasClickInContextMenu() == false)
                UIUtility.HideContextMenu();

            this.InterpolateAfter();
        }
        #endregion

        #region Public Methods
        public static void Play()
        {
            if (_self._isPlaying == false)
            {
                _self._isPlaying = true;
                _self._startTime = Time.time - _self._playbackTime;
            }
            else
                Pause();
        }

        public static void Pause()
        {
            _self._isPlaying = false;
        }

        public static void Stop()
        {
            _self._playbackTime = 0f;
            _self.UpdateCursor();
            _self.Interpolate(true);
            _self.Interpolate(false);
            _self._isPlaying = false;
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
                _self.ExecuteDelayed(() =>
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
                if (model.IsCompatibleWithTarget(this._selectedOCI) == false)
                    return null;
                Interpolable interpolable = new Interpolable(this._selectedOCI, model);
                if (this._interpolables.TryGetValue(interpolable.GetHashCode(), out actualInterpolable) == false)
                {
                    this._interpolables.Add(interpolable.GetHashCode(), interpolable);
                    this._interpolablesTree.AddLeaf(interpolable);
                    actualInterpolable = interpolable;
                    added = true;
                }
                this.UpdateInterpolablesView();
                return actualInterpolable;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(_name + ": Couldn't add interpolable with model:\n" + model + "\n" + e);
                if (added)
                {
                    this._interpolables.Remove(actualInterpolable.GetHashCode());
                    this._interpolablesTree.RemoveLeaf(actualInterpolable);
                    this.UpdateInterpolablesView();
                }
            }
            return null;
        }

        private void RemoveInterpolable(Interpolable interpolable)
        {
            this._interpolables.Remove(interpolable.GetHashCode());
            int selectedIndex = this._selectedInterpolables.IndexOf(interpolable);
            if (selectedIndex != -1)
                this._selectedInterpolables.RemoveAt(selectedIndex);
            this._interpolablesTree.RemoveLeaf(interpolable);
            this._selectedKeyframes.RemoveAll(elem => elem.Value.parent == interpolable);
            this.UpdateInterpolablesView();
            this.UpdateKeyframeWindow(false);
        }

        private void RemoveInterpolables(IEnumerable<Interpolable> interpolables)
        {
            if (interpolables == this._selectedInterpolables)
                interpolables = interpolables.ToArray();
            foreach (Interpolable interpolable in interpolables)
            {
                if (this._interpolables.ContainsKey(interpolable.GetHashCode()))
                    this._interpolables.Remove(interpolable.GetHashCode());
                this._interpolablesTree.RemoveLeaf(interpolable);

                int index = this._selectedInterpolables.IndexOf(interpolable);
                if (index != -1)
                    this._selectedInterpolables.RemoveAt(index);
                this._selectedKeyframes.RemoveAll(elem => elem.Value.parent == interpolable);
            }
            this.UpdateInterpolablesView();
            this.UpdateKeyframeWindow(false);
        }

        private void Init()
        {
            UIUtility.Init();

            BuiltInInterpolables.Populate();

            if (Camera.main.GetComponent<Expression>() == null)
                Camera.main.gameObject.AddComponent<Expression>();
            this._allGuideObjects = (Dictionary<Transform, GuideObject>)GuideObjectManager.Instance.GetPrivate("dicGuideObject");
            this._selectedGuideObjects = (HashSet<GuideObject>)GuideObjectManager.Instance.GetPrivate("hashSelectObject");
#if HONEYSELECT
            AssetBundle bundle = AssetBundle.LoadFromMemory(Assembly.GetExecutingAssembly().GetResource("Timeline.Resources.TimelineResources.unity3d"));
#elif KOIKATSU
            AssetBundle bundle = AssetBundle.LoadFromMemory(Assembly.GetExecutingAssembly().GetResource("Timeline.Resources.TimelineResourcesKoi.unity3d"));
#endif
            GameObject uiPrefab = bundle.LoadAsset<GameObject>("Canvas");
            this._ui = GameObject.Instantiate(uiPrefab).GetComponent<Canvas>();
            CanvasGroup alphaGroup = this._ui.GetComponent<CanvasGroup>();
            uiPrefab.hideFlags |= HideFlags.HideInHierarchy;
            this._keyframePrefab = bundle.LoadAsset<GameObject>("Keyframe");
            this._keyframePrefab.hideFlags |= HideFlags.HideInHierarchy;
            this._keyframesBackgroundMaterial = bundle.LoadAsset<Material>("KeyframesBackground");
            this._interpolablePrefab = bundle.LoadAsset<GameObject>("Interpolable");
            this._interpolablePrefab.hideFlags |= HideFlags.HideInHierarchy;
            this._interpolableModelPrefab = bundle.LoadAsset<GameObject>("InterpolableModel");
            this._interpolableModelPrefab.hideFlags |= HideFlags.HideInHierarchy;
            this._curveKeyframePrefab = bundle.LoadAsset<GameObject>("CurveKeyframe");
            this._curveKeyframePrefab.hideFlags |= HideFlags.HideInHierarchy;
            this._headerPrefab = bundle.LoadAsset<GameObject>("Header");
            this._headerPrefab.hideFlags |= HideFlags.HideInHierarchy;
            this._singleFilePrefab = bundle.LoadAsset<GameObject>("SingleFile");
            this._singleFilePrefab.hideFlags |= HideFlags.HideInHierarchy;

            this._ui.transform.Find("Timeline Window/Help Panel/Main Container/Scroll View/Viewport/Content/Text").GetComponent<Text>().text = System.Text.Encoding.Default.GetString(Assembly.GetExecutingAssembly().GetResource("Timeline.Resources.Help.txt"));

            foreach (Sprite sprite in bundle.LoadAllAssets<Sprite>())
            {
                switch (sprite.name)
                {
                    case "Link":
                        this._linkSprite = sprite;
                        break;
                    case "Color":
                        this._colorSprite = sprite;
                        break;
                    case "Rename":
                        this._renameSprite = sprite;
                        break;
                    case "NewFolder":
                        this._newFolderSprite = sprite;
                        break;
                    case "Add":
                        this._addSprite = sprite;
                        break;
                    case "AddToFolder":
                        this._addToFolderSprite = sprite;
                        break;
                    case "ChevronUp":
                        this._chevronUpSprite = sprite;
                        break;
                    case "ChevronDown":
                        this._chevronDownSprite = sprite;
                        break;
                    case "Delete":
                        this._deleteSprite = sprite;
                        break;
                    case "Checkbox":
                        this._checkboxSprite = sprite;
                        break;
                    case "CheckboxComposite":
                        this._checkboxCompositeSprite = sprite;
                        break;
                    case "SelectAll":
                        this._selectAllSprite = sprite;
                        break;
                }
            }

            bundle.Unload(false);

            this._tooltip = this._ui.transform.Find("Tooltip/Text").GetComponent<Text>();

            //Timeline window
            this._timelineWindow = (RectTransform)this._ui.transform.Find("Timeline Window");
            UIUtility.MakeObjectDraggable((RectTransform)this._ui.transform.Find("Timeline Window/Top Container"), this._timelineWindow, (RectTransform)this._ui.transform);
            this._helpPanel = this._ui.transform.Find("Timeline Window/Help Panel").gameObject;
            this._singleFilesPanel = this._ui.transform.Find("Timeline Window/Single Files Panel").gameObject;
            this._singleFilesContainer = (RectTransform)this._singleFilesPanel.transform.Find("Main Container/Scroll View/Viewport/Content");
            this._singleFileNameField = this._singleFilesPanel.transform.Find("Main Container/Buttons/Name").GetComponent<InputField>();
            this._verticalScrollView = this._ui.transform.Find("Timeline Window/Main Container/Timeline/Interpolables").GetComponent<ScrollRect>();
            this._horizontalScrollView = this._ui.transform.Find("Timeline Window/Main Container/Timeline/Scroll View").GetComponent<ScrollRect>();
            this._allToggle = this._ui.transform.Find("Timeline Window/Main Container/Timeline/Interpolables/Top/All").GetComponent<Toggle>();
            this._interpolablesSearchField = this._ui.transform.Find("Timeline Window/Main Container/Search").GetComponent<InputField>();
            this._grid = (RectTransform)this._ui.transform.Find("Timeline Window/Main Container/Timeline/Scroll View/Viewport/Content/Grid Container");
            this._gridImage = this._ui.transform.Find("Timeline Window/Main Container/Timeline/Scroll View/Viewport/Content/Grid Container/Grid/Viewport/Background").GetComponent<RawImage>();
            this._gridImage.material = new Material(this._gridImage.material);
            this._gridTop = (RectTransform)this._ui.transform.Find("Timeline Window/Main Container/Timeline/Scroll View/Viewport/Content/Grid Container/Texts/Background");
            this._cursor = (RectTransform)this._ui.transform.Find("Timeline Window/Main Container/Timeline/Scroll View/Viewport/Content/Grid Container/Cursor");
            this._frameRateInputField = this._ui.transform.Find("Timeline Window/Buttons/Play Buttons/FrameRate").GetComponent<InputField>();
            this._timeInputField = this._ui.transform.Find("Timeline Window/Buttons/Time").GetComponent<InputField>();
            this._blockLengthInputField = this._ui.transform.Find("Timeline Window/Buttons/Block Divisions/Block Length").GetComponent<InputField>();
            this._divisionsInputField = this._ui.transform.Find("Timeline Window/Buttons/Block Divisions/Divisions").GetComponent<InputField>();
            this._durationInputField = this._ui.transform.Find("Timeline Window/Buttons/Duration").GetComponent<InputField>();
            this._speedInputField = this._ui.transform.Find("Timeline Window/Buttons/Speed").GetComponent<InputField>();
            this._textsContainer = (RectTransform)this._ui.transform.Find("Timeline Window/Main Container/Timeline/Scroll View/Viewport/Content/Grid Container/Texts");
            this._keyframesContainer = (RectTransform)this._ui.transform.Find("Timeline Window/Main Container/Timeline/Scroll View/Viewport/Content/Grid Container/Grid/Viewport/Content");
            this._selectionArea = (RectTransform)this._ui.transform.Find("Timeline Window/Main Container/Timeline/Scroll View/Viewport/Content/Grid Container/Grid/Viewport/Content/Selection");
            this._miscContainer = (RectTransform)this._ui.transform.Find("Timeline Window/Main Container/Timeline/Scroll View/Viewport/Content/Grid Container/Grid/Viewport/Misc Content");
            this._resizeHandle = (RectTransform)this._ui.transform.Find("Timeline Window/Resize Handle");

            this._ui.transform.Find("Timeline Window/Buttons/Play Buttons/Play").GetComponent<Button>().onClick.AddListener(Play);
            this._ui.transform.Find("Timeline Window/Buttons/Play Buttons/Pause").GetComponent<Button>().onClick.AddListener(Pause);
            this._ui.transform.Find("Timeline Window/Buttons/Play Buttons/Stop").GetComponent<Button>().onClick.AddListener(Stop);
            this._ui.transform.Find("Timeline Window/Buttons/Play Buttons/PrevFrame").GetComponent<Button>().onClick.AddListener(PreviousFrame);
            this._ui.transform.Find("Timeline Window/Buttons/Play Buttons/NextFrame").GetComponent<Button>().onClick.AddListener(NextFrame);
            this._ui.transform.Find("Timeline Window/Buttons/Single Files").GetComponent<Button>().onClick.AddListener(this.ToggleSingleFilesPanel);
            this._singleFileNameField.onValueChanged.AddListener((s) => this.UpdateSingleFileSelection());
            this._singleFilesPanel.transform.Find("Main Container/Buttons/Load").GetComponent<Button>().onClick.AddListener(this.LoadSingleFile);
            this._singleFilesPanel.transform.Find("Main Container/Buttons/Save").GetComponent<Button>().onClick.AddListener(this.SaveSingleFile);
            this._singleFilesPanel.transform.Find("Main Container/Buttons/Delete").GetComponent<Button>().onClick.AddListener(this.DeleteSingleFile);
            this._ui.transform.Find("Timeline Window/Buttons/Help").GetComponent<Button>().onClick.AddListener(this.ToggleHelp);

            this._frameRateInputField.onEndEdit.AddListener(this.UpdateDesiredFrameRate);
            this._timeInputField.onEndEdit.AddListener(this.UpdatePlaybackTime);
            this._durationInputField.onEndEdit.AddListener(this.UpdateDuration);
            this._blockLengthInputField.onEndEdit.AddListener(this.UpdateBlockLength);
            this._blockLengthInputField.text = this._blockLength.ToString();
            this._divisionsInputField.onEndEdit.AddListener(this.UpdateDivisions);
            this._divisionsInputField.text = this._divisions.ToString();
            this._speedInputField.onEndEdit.AddListener(this.UpdateSpeed);
            this._keyframesContainer.gameObject.AddComponent<PointerDownHandler>().onPointerDown = this.OnKeyframeContainerMouseDown;
            this._gridTop.gameObject.AddComponent<PointerDownHandler>().onPointerDown = this.OnGridTopMouse;
            this._ui.transform.Find("Timeline Window/Top Container").gameObject.AddComponent<ScrollHandler>().onScroll = e =>
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
            DragHandler handler = this._gridTop.gameObject.AddComponent<DragHandler>();
            //handler.onBeginDrag = (e) =>
            //{
            //    this.OnGridTopMouse(e);
            //    e.Reset();
            //};
            handler.onDrag = (e) =>
            {
                this._isPlaying = false;
                this._isDraggingCursor = true;
                this.OnGridTopMouse(e);
                e.Reset();
            };
            handler.onEndDrag = (e) =>
            {
                this._isDraggingCursor = false;
                this.OnGridTopMouse(e);
                e.Reset();
            };
            this._gridTop.gameObject.AddComponent<ScrollHandler>().onScroll = e =>
            {
                if (e.scrollDelta.y > 0)
                    this.ZoomIn();
                else
                    this.ZoomOut();
                e.Reset();
            };
            this._verticalScrollView.onValueChanged.AddListener(this.ScrollKeyframes);
            this._keyframesContainer.gameObject.AddComponent<ScrollHandler>().onScroll = e =>
            {
                if (Input.GetKey(KeyCode.LeftControl))
                {
                    if (e.scrollDelta.y > 0)
                        this.ZoomIn();
                    else
                        this.ZoomOut();
                    e.Reset();
                }
                else if (Input.GetKey(KeyCode.LeftAlt))
                {
                    this.ScaleKeyframeSelection(e.scrollDelta.y);
                    e.Reset();
                }
                else if (Input.GetKey(KeyCode.LeftShift) == false)
                {
                    this._verticalScrollView.OnScroll(e);
                    e.Reset();
                }
                else
                    this._horizontalScrollView.OnScroll(e);
            };
            handler = this._keyframesContainer.gameObject.AddComponent<DragHandler>();
            handler.onInitializePotentialDrag = (e) =>
            {
                this.PotentiallyBeginAreaSelect(e);
                e.Reset();
            };
            handler.onBeginDrag = (e) =>
            {
                this.BeginAreaSelect(e);
                e.Reset();
            };
            handler.onDrag = (e) =>
            {
                this.UpdateAreaSelect(e);
                e.Reset();
            };
            handler.onEndDrag = (e) =>
            {
                this.EndAreaSelect(e);
                e.Reset();
            };
            this._allToggle.onValueChanged.AddListener(b => this.UpdateInterpolablesView());
            this._interpolablesSearchField.onValueChanged.AddListener(this.InterpolablesSearch);
            handler = this._resizeHandle.gameObject.AddComponent<DragHandler>();
            handler.onDrag = this.OnResizeWindow;

            //Keyframe window
            this._keyframeWindow = this._ui.transform.Find("Keyframe Window").gameObject;
            UIUtility.MakeObjectDraggable((RectTransform)this._keyframeWindow.transform.Find("Top Container"), (RectTransform)this._keyframeWindow.transform, (RectTransform)this._ui.transform);
            this._keyframeInterpolableNameText = this._keyframeWindow.transform.Find("Main Container/Main Fields/Interpolable Name").GetComponent<Text>();
            this._keyframeSelectPrevButton = this._keyframeWindow.transform.Find("Main Container/Main Fields/Prev Next/Prev").GetComponent<Button>();
            this._keyframeSelectNextButton = this._keyframeWindow.transform.Find("Main Container/Main Fields/Prev Next/Next").GetComponent<Button>();
            this._keyframeTimeTextField = this._keyframeWindow.transform.Find("Main Container/Main Fields/Time/InputField").GetComponent<InputField>();
            this._keyframeUseCurrentTimeButton = this._keyframeWindow.transform.Find("Main Container/Main Fields/Use Current Time").GetComponent<Button>();
            this._keyframeValueText = this._keyframeWindow.transform.Find("Main Container/Main Fields/Value/Background/Text").GetComponent<Text>();
            this._keyframeUseCurrentValueButton = this._keyframeWindow.transform.Find("Main Container/Main Fields/Use Current").GetComponent<Button>();
            Button deleteButton = this._keyframeWindow.transform.Find("Main Container/Main Fields/Delete").GetComponent<Button>();
            this._keyframeDeleteButtonText = deleteButton.GetComponentInChildren<Text>();

            this._curveContainer = this._keyframeWindow.transform.Find("Main Container/Curve Fields/Curve/Grid/Spline").GetComponent<RawImage>();
            this._curveContainer.material = new Material(this._curveContainer.material);
            this._curveTimeInputField = this._keyframeWindow.transform.Find("Main Container/Curve Fields/Fields/Curve Point Time/InputField").GetComponent<InputField>();
            this._curveTimeSlider = this._keyframeWindow.transform.Find("Main Container/Curve Fields/Fields/Curve Point Time/Slider").GetComponent<Slider>();
            this._curveValueInputField = this._keyframeWindow.transform.Find("Main Container/Curve Fields/Fields/Curve Point Value/InputField").GetComponent<InputField>();
            this._curveValueSlider = this._keyframeWindow.transform.Find("Main Container/Curve Fields/Fields/Curve Point Value/Slider").GetComponent<Slider>();
            this._curveInTangentInputField = this._keyframeWindow.transform.Find("Main Container/Curve Fields/Fields/Curve Point InTangent/InputField").GetComponent<InputField>();
            this._curveInTangentSlider = this._keyframeWindow.transform.Find("Main Container/Curve Fields/Fields/Curve Point InTangent/Slider").GetComponent<Slider>();
            this._curveOutTangentInputField = this._keyframeWindow.transform.Find("Main Container/Curve Fields/Fields/Curve Point OutTangent/InputField").GetComponent<InputField>();
            this._curveOutTangentSlider = this._keyframeWindow.transform.Find("Main Container/Curve Fields/Fields/Curve Point OutTangent/Slider").GetComponent<Slider>();
            this._cursor2 = (RectTransform)this._ui.transform.Find("Keyframe Window/Main Container/Curve Fields/Curve/Grid/Cursor");

            this._keyframeWindow.transform.Find("Close").GetComponent<Button>().onClick.AddListener(this.CloseKeyframeWindow);
            this._keyframeSelectPrevButton.onClick.AddListener(this.SelectPreviousKeyframe);
            this._keyframeSelectNextButton.onClick.AddListener(this.SelectNextKeyframe);
            this._keyframeUseCurrentTimeButton.onClick.AddListener(this.UseCurrentTime);
            this._keyframeWindow.transform.Find("Main Container/Main Fields/Drag At Current Time").GetComponent<Button>().onClick.AddListener(this.DragAtCurrentTime);
            this._keyframeUseCurrentValueButton.onClick.AddListener(this.UseCurrentValue);
            deleteButton.onClick.AddListener(this.DeleteSelectedKeyframes);


            this._keyframeWindow.transform.Find("Main Container/Curve Fields/Fields/Presets/Line").GetComponent<Button>().onClick.AddListener(() => this.ApplyKeyframeCurvePreset(this._linePreset));
            this._keyframeWindow.transform.Find("Main Container/Curve Fields/Fields/Presets/Top").GetComponent<Button>().onClick.AddListener(() => this.ApplyKeyframeCurvePreset(this._topPreset));
            this._keyframeWindow.transform.Find("Main Container/Curve Fields/Fields/Presets/Bottom").GetComponent<Button>().onClick.AddListener(() => this.ApplyKeyframeCurvePreset(this._bottomPreset));
            this._keyframeWindow.transform.Find("Main Container/Curve Fields/Fields/Presets/Hermite").GetComponent<Button>().onClick.AddListener(() => this.ApplyKeyframeCurvePreset(this._hermitePreset));
            this._keyframeWindow.transform.Find("Main Container/Curve Fields/Fields/Presets/Stairs").GetComponent<Button>().onClick.AddListener(() => this.ApplyKeyframeCurvePreset(this._stairsPreset));
            this._keyframeWindow.transform.Find("Main Container/Curve Fields/Fields/Buttons/Copy").GetComponent<Button>().onClick.AddListener(this.CopyKeyframeCurve);
            this._keyframeWindow.transform.Find("Main Container/Curve Fields/Fields/Buttons/Paste").GetComponent<Button>().onClick.AddListener(this.PasteKeyframeCurve);
            this._keyframeWindow.transform.Find("Main Container/Curve Fields/Fields/Buttons/Invert").GetComponent<Button>().onClick.AddListener(this.InvertKeyframeCurve);

            this._keyframeTimeTextField.onEndEdit.AddListener(this.UpdateSelectedKeyframeTime);

            this._curveContainer.gameObject.AddComponent<PointerDownHandler>().onPointerDown = this.OnCurveMouseDown;
            this._curveTimeInputField.onEndEdit.AddListener(this.UpdateCurvePointTime);
            this._curveTimeSlider.onValueChanged.AddListener(this.UpdateCurvePointTime);
            this._curveValueInputField.onEndEdit.AddListener(this.UpdateCurvePointValue);
            this._curveValueSlider.onValueChanged.AddListener(this.UpdateCurvePointValue);
            this._curveInTangentInputField.onEndEdit.AddListener(this.UpdateCurvePointInTangent);
            this._curveInTangentSlider.onValueChanged.AddListener(this.UpdateCurvePointInTangent);
            this._curveOutTangentInputField.onEndEdit.AddListener(this.UpdateCurvePointOutTangent);
            this._curveOutTangentSlider.onValueChanged.AddListener(this.UpdateCurvePointOutTangent);

            this._ui.gameObject.SetActive(false);
            this._helpPanel.gameObject.SetActive(false);
            this._singleFilesPanel.gameObject.SetActive(false);
            this._keyframeWindow.gameObject.SetActive(false);
            this._tooltip.transform.parent.gameObject.SetActive(false);

            this.UpdateInterpolablesView();

            this._loaded = true;
        }

        private void ScrollKeyframes(Vector2 arg0)
        {
            this._keyframesContainer.anchoredPosition = new Vector2(this._keyframesContainer.anchoredPosition.x, this._verticalScrollView.content.anchoredPosition.y);
            this._miscContainer.anchoredPosition = new Vector2(this._miscContainer.anchoredPosition.x, this._verticalScrollView.content.anchoredPosition.y);
        }

        private void InterpolateBefore()
        {
            if (this._isPlaying)
            {
                this._playbackTime = (Time.time - this._startTime) % this._duration;
                this.UpdateCursor();
                this.Interpolate(true);
            }
        }

        private void InterpolateAfter()
        {
            if (this._isPlaying)
                this.Interpolate(false);
        }

        private void Interpolate(bool before)
        {
            this._interpolablesTree.Recurse((node, depth) =>
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
                    if (keyframePair.Key <= this._playbackTime)
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
                    float normalizedTime = (this._playbackTime - left.Key) / (right.Key - left.Key);
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
                    this._toDelete.Add(interpolable);
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
            this._cursor.anchoredPosition = new Vector2((this._playbackTime * this._grid.rect.width) / this._duration, this._cursor.anchoredPosition.y);
            this.UpdateCursor2();
            this._timeInputField.text = $"{Mathf.FloorToInt(this._playbackTime / 60):00}:{(this._playbackTime % 60):00.000}";
        }

        private void UpdateDesiredFrameRate(string s)
        {
            int res;
            if (int.TryParse(this._frameRateInputField.text, out res) && res >= 1)
                this._desiredFrameRate = res;
            this._frameRateInputField.text = this._desiredFrameRate.ToString();
        }

        private void UpdatePlaybackTime(string s)
        {
            if (this._isPlaying == false)
            {
                float time = this.ParseTime(this._timeInputField.text);
                if (time < 0)
                    return;
                this.SeekPlaybackTime(time % this._duration);
            }
        }

        private void UpdateDuration(string s)
        {
            float time = this.ParseTime(this._durationInputField.text);
            if (time < 0)
                return;
            this._duration = time;
            this.UpdateGrid();
        }

        private void UpdateBlockLength(string arg0)
        {
            float res;
            if (float.TryParse(this._blockLengthInputField.text, out res) && res >= 0.01f)
            {
                this._blockLength = res;
                this.UpdateGrid();
            }
            this._blockLengthInputField.text = this._blockLength.ToString();
        }

        private void UpdateDivisions(string arg0)
        {
            int res;
            if (int.TryParse(this._divisionsInputField.text, out res) && res >= 1)
            {
                this._divisions = res;
                this.UpdateGridMaterial();
            }
            this._divisionsInputField.text = this._divisions.ToString();
        }

        private void UpdateSpeed(string arg0)
        {
            float s;
            if (float.TryParse(this._speedInputField.text, out s) && s >= 0)
                Time.timeScale = s;
        }

        private void ZoomOut()
        {
            this._zoomLevel -= 0.05f * this._zoomLevel;
            if (this._zoomLevel < 0.1f)
                this._zoomLevel = 0.1f;
            float position = this._horizontalScrollView.horizontalNormalizedPosition;
            this.UpdateGrid();
            this._horizontalScrollView.horizontalNormalizedPosition = position;
        }

        private void ZoomIn()
        {
            this._zoomLevel += 0.05f * this._zoomLevel;
            if (this._zoomLevel > 64f)
                this._zoomLevel = 64f;
            float position = this._horizontalScrollView.horizontalNormalizedPosition;
            this.UpdateGrid();
            this._horizontalScrollView.horizontalNormalizedPosition = position;
        }

        private void ToggleHelp()
        {
            this._helpPanel.gameObject.SetActive(!this._helpPanel.gameObject.activeSelf);
        }

        private void InterpolablesSearch(string arg0)
        {
            this.UpdateInterpolablesView();
        }

        private void UpdateInterpolablesView()
        {
            bool showAll = this._allToggle.isOn;
            int interpolableDisplayIndex = 0;
            int headerDisplayIndex = 0;
            //Dictionary<int, Interpolable> usedInterpolables = new Dictionary<int, Interpolable>();
            this._gridHeights.Clear();
            float height = 0;
            this.UpdateInterpolablesViewTree(this._interpolablesTree.tree, showAll, ref interpolableDisplayIndex, ref headerDisplayIndex, ref height);
            int interpolableModelDisplayIndex = 0;
            foreach (KeyValuePair<string, List<InterpolableModel>> ownerPair in this._interpolableModelsDictionary.OrderBy(p => this._hardCodedOwnerOrder.TryGetValue(p.Key, out int order) ? order : int.MaxValue))
            {
                HeaderDisplay header = this.GetHeaderDisplay(headerDisplayIndex);
                header.gameObject.transform.SetAsLastSibling();
                header.container.offsetMin = Vector2.zero;
                header.group = null;
                header.name.text = ownerPair.Key;
                height += _interpolableHeight;
                this._gridHeights.Add(height);

                if (header.expanded)
                {
                    foreach (InterpolableModel model in ownerPair.Value)
                    {
                        //Interpolable usedInterpolable;
                        if ( /*usedInterpolables.TryGetValue(model.GetHashCode(), out usedInterpolable) ||*/ model.IsCompatibleWithTarget(this._selectedOCI) == false)
                            continue;

                        if (model.name.IndexOf(this._interpolablesSearchField.text, StringComparison.OrdinalIgnoreCase) == -1)
                            continue;

                        InterpolableModelDisplay display = this.GetInterpolableModelDisplay(interpolableModelDisplayIndex);
                        display.gameObject.transform.SetAsLastSibling();
                        display.model = model;
                        display.name.text = model.name;
                        height += _interpolableHeight;
                        this._gridHeights.Add(height);
                        ++interpolableModelDisplayIndex;
                    }
                }

                ++headerDisplayIndex;
            }

            for (; headerDisplayIndex < this._displayedOwnerHeader.Count; headerDisplayIndex++)
                this._displayedOwnerHeader[headerDisplayIndex].gameObject.SetActive(false);

            for (; interpolableDisplayIndex < this._displayedInterpolables.Count; ++interpolableDisplayIndex)
            {
                InterpolableDisplay display = this._displayedInterpolables[interpolableDisplayIndex];
                display.gameObject.SetActive(false);
                display.gridBackground.gameObject.SetActive(false);
            }

            for (; interpolableModelDisplayIndex < this._displayedInterpolableModels.Count; ++interpolableModelDisplayIndex)
                this._displayedInterpolableModels[interpolableModelDisplayIndex].gameObject.SetActive(false);

            this.UpdateInterpolableSelection();

            this.ExecuteDelayed(this.UpdateGrid);

            this.ExecuteDelayed(this.UpdateSeparators, 2);
        }

        private void UpdateInterpolablesViewTree(List<INode> nodes, bool showAll, ref int interpolableDisplayIndex, ref int headerDisplayIndex, ref float height, int indent = 0)
        {
            foreach (INode node in nodes)
            {
                switch (node.type)
                {
                    case INodeType.Leaf:
                        Interpolable interpolable = ((LeafNode<Interpolable>)node).obj;
                        if (this.ShouldShowInterpolable(interpolable, showAll) == false)
                            continue;

                        InterpolableDisplay display = this.GetInterpolableDisplay(interpolableDisplayIndex);
                        display.gameObject.transform.SetAsLastSibling();
                        display.container.offsetMin = new Vector2(indent, 0f);
                        display.interpolable = (LeafNode<Interpolable>)node;
                        display.group.alpha = interpolable.useOciInHash == false || interpolable.oci != null && interpolable.oci == this._selectedOCI ? 1f : 0.75f;
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
                        this.UpdateInterpolableColor(display, interpolable.color);
                        height += _interpolableHeight;
                        this._gridHeights.Add(height);
                        ++interpolableDisplayIndex;
                        break;
                    case INodeType.Group:
                        GroupNode<InterpolableGroup> group = (GroupNode<InterpolableGroup>)node;

                        if (this._interpolablesTree.Any(group, leafNode => this.ShouldShowInterpolable(leafNode.obj, showAll)) == false)
                            break;

                        HeaderDisplay headerDisplay = this.GetHeaderDisplay(headerDisplayIndex, true);
                        headerDisplay.gameObject.transform.SetAsLastSibling();
                        headerDisplay.container.offsetMin = new Vector2(indent, 0f);
                        headerDisplay.group = (GroupNode<InterpolableGroup>)node;
                        headerDisplay.name.text = group.obj.name;
                        height += _interpolableHeight * 2f / 3f;
                        this._gridHeights.Add(height);
                        ++headerDisplayIndex;
                        if (group.obj.expanded)
                            this.UpdateInterpolablesViewTree(((GroupNode<InterpolableGroup>)node).children, showAll, ref interpolableDisplayIndex, ref headerDisplayIndex, ref height, indent + 8);
                        break;
                }
            }
        }

        private bool ShouldShowInterpolable(Interpolable interpolable, bool showAll)
        {
            if (showAll == false && ((interpolable.oci != null && interpolable.oci != this._selectedOCI) || !interpolable.ShouldShow()))
                return false;
            //if (usedInterpolables.ContainsKey(interpolable.GetBaseHashCode()) == false)
            //    usedInterpolables.Add(interpolable.GetBaseHashCode(), interpolable);

            if (interpolable.name.IndexOf(this._interpolablesSearchField.text, StringComparison.OrdinalIgnoreCase) == -1)
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
            foreach (float height in this._gridHeights)
            {
                RawImage separator;
                if (i < this._interpolableSeparators.Count)
                    separator = this._interpolableSeparators[i];
                else
                {
                    separator = UIUtility.CreateRawImage("Separator", this._miscContainer);
                    separator.color = new Color(0f, 0f, 0f, 0.5f);
                    this._interpolableSeparators.Add(separator);
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
            for (; i < this._interpolableSeparators.Count; i++)
                this._interpolableSeparators[i].gameObject.SetActive(false);
        }

        private InterpolableDisplay GetInterpolableDisplay(int i)
        {
            InterpolableDisplay display;
            if (i < this._displayedInterpolables.Count)
                display = this._displayedInterpolables[i];
            else
            {
                display = new InterpolableDisplay();
                display.gameObject = GameObject.Instantiate(this._interpolablePrefab);
                display.gameObject.hideFlags = HideFlags.None;
                display.group = display.gameObject.GetComponent<CanvasGroup>();
                display.container = (RectTransform)display.gameObject.transform.Find("Container");
                display.enabled = display.container.Find("Enabled").GetComponent<Toggle>();
                display.name = display.container.Find("Label").GetComponent<Text>();
                display.inputField = display.container.Find("InputField").GetComponent<InputField>();
                display.background = display.container.GetComponent<Image>();
                display.selectedOutline = display.container.Find("SelectedOutline").GetComponent<Image>();
                display.gridBackground = UIUtility.CreateRawImage($"Interpolable{i} Background", this._miscContainer);
                display.background.material = new Material(display.background.material);

                display.gameObject.transform.SetParent(this._verticalScrollView.content);
                display.gameObject.transform.localPosition = Vector3.zero;
                display.gameObject.transform.localScale = Vector3.one;
                display.gridBackground.transform.SetAsFirstSibling();
                display.gridBackground.raycastTarget = false;
                display.gridBackground.material = new Material(this._keyframesBackgroundMaterial);
                display.inputField.gameObject.SetActive(false);
                display.container.gameObject.AddComponent<PointerDownHandler>().onPointerDown = (e) =>
                {
                    Interpolable interpolable = display.interpolable.obj;
                    switch (e.button)
                    {
                        case PointerEventData.InputButton.Left:
                            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                                this.SelectAddInterpolable(interpolable);
                            else if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                            {
                                Interpolable lastSelected = this._selectedInterpolables.LastOrDefault();
                                if (lastSelected != null)
                                {
                                    Interpolable selectingNow = interpolable;
                                    int selectingNowIndex = this._displayedInterpolables.FindIndex(elem => elem.interpolable.obj == selectingNow);
                                    int lastSelectedIndex = this._displayedInterpolables.FindIndex(elem => elem.interpolable.obj == lastSelected);
                                    if (selectingNowIndex < lastSelectedIndex)
                                    {
                                        int temp = selectingNowIndex;
                                        selectingNowIndex = lastSelectedIndex;
                                        lastSelectedIndex = temp;
                                    }

                                    this.SelectAddInterpolable(this._displayedInterpolables.Where((elem, index) => index > lastSelectedIndex && index < selectingNowIndex).Select(elem => elem.interpolable.obj).ToArray());
                                    this.SelectAddInterpolable(selectingNow);
                                }
                                else
                                    this.SelectAddInterpolable(interpolable);
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
                                this.SelectInterpolable(interpolable);

                            break;
                        case PointerEventData.InputButton.Middle:
                            if (Input.GetKey(KeyCode.LeftControl))
                                this.RemoveInterpolable(interpolable);
                            break;
                        case PointerEventData.InputButton.Right:
                            if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)this._ui.transform, e.position, e.pressEventCamera, out Vector2 localPoint))
                            {
                                if (this._selectedInterpolables.Count == 0 || this._selectedInterpolables.Contains(interpolable) == false)
                                    this.SelectInterpolable(interpolable);

                                List<Interpolable> currentlySelectedInterpolables = new List<Interpolable>(this._selectedInterpolables);

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
                                            icon = this._linkSprite,
                                            text = "Select linked GuideObject",
                                            onClick = p => { GuideObjectManager.Instance.selectObject = linkedGuideObject; }
                                        });
                                    }

                                    elements.Add(new LeafElement()
                                    {
                                        icon = this._renameSprite,
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
                                                this.UpdateInterpolablesView();
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
                                        icon = this._newFolderSprite,
                                        text = "Group together",
                                        onClick = p =>
                                        {
                                            this._interpolablesTree.GroupTogether(currentlySelectedInterpolables, new InterpolableGroup() { name = "New Group" });
                                            this.UpdateInterpolablesView();
                                        }
                                    });
                                }
                                elements.Add(new LeafElement()
                                {
                                    icon = this._selectAllSprite,
                                    text = "Select keyframes",
                                    onClick = p =>
                                    {
                                        List<KeyValuePair<float, Keyframe>> toSelect = new List<KeyValuePair<float, Keyframe>>();
                                        foreach (Interpolable selected in currentlySelectedInterpolables)
                                            toSelect.AddRange(selected.keyframes);
                                        this.SelectKeyframes(toSelect);
                                    }
                                });
                                elements.Add(new LeafElement()
                                {
                                    icon = this._selectAllSprite,
                                    text = "Select keyframes before cursor",
                                    onClick = p =>
                                    {
                                        List<KeyValuePair<float, Keyframe>> toSelect = new List<KeyValuePair<float, Keyframe>>();
                                        float currentTime = this._playbackTime % this._duration;
                                        foreach (Interpolable selected in currentlySelectedInterpolables)
                                            toSelect.AddRange(selected.keyframes.Where(k => k.Key < currentTime));
                                        this.SelectKeyframes(toSelect);
                                    }
                                });
                                elements.Add(new LeafElement()
                                {
                                    icon = this._selectAllSprite,
                                    text = "Select keyframes after cursor",
                                    onClick = p =>
                                    {
                                        List<KeyValuePair<float, Keyframe>> toSelect = new List<KeyValuePair<float, Keyframe>>();
                                        float currentTime = this._playbackTime % this._duration;
                                        foreach (Interpolable selected in currentlySelectedInterpolables)
                                            toSelect.AddRange(selected.keyframes.Where(k => k.Key >= currentTime));
                                        this.SelectKeyframes(toSelect);
                                    }
                                });
                                elements.Add(new LeafElement()
                                {
                                    icon = this._colorSprite,
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
                                                InterpolableDisplay disp = this._displayedInterpolables.Find(id => id.interpolable.obj == interp);
                                                interp.color = col;
                                                this.UpdateInterpolableColor(disp, col);
                                            }
                                        }, true);

#endif
                                    }
                                });

                                elements.Add(new LeafElement()
                                {
                                    icon = this._addSprite,
                                    text = currentlySelectedInterpolables.Count == 1 ? "Add keyframe at cursor" : "Add keyframes at cursor",
                                    onClick = p =>
                                    {
                                        float time = this._playbackTime % this._duration;
                                        foreach (Interpolable selectedInterpolable in currentlySelectedInterpolables)
                                            this.AddKeyframe(selectedInterpolable, time);
                                        this.UpdateGrid();
                                    }
                                });
                                List<AContextMenuElement> treeGroups = this.GetInterpolablesTreeGroups(currentlySelectedInterpolables.Select(elem => (INode)this._interpolablesTree.GetLeafNode(elem)));
                                if (treeGroups.Count != 1)
                                {
                                    elements.Add(new GroupElement()
                                    {
                                        icon = this._addToFolderSprite,
                                        text = "Parent to",
                                        elements = treeGroups
                                    });
                                }
                                elements.Add(new LeafElement()
                                {
                                    icon = this._checkboxSprite,
                                    text = currentlySelectedInterpolables.Count == 1 ? "Disable" : "Disable all",
                                    onClick = p =>
                                    {
                                        foreach (Interpolable selectedInterpolable in currentlySelectedInterpolables)
                                            selectedInterpolable.enabled = false;
                                        this.UpdateInterpolablesView();
                                    }
                                });
                                elements.Add(new LeafElement()
                                {
                                    icon = this._checkboxCompositeSprite,
                                    text = currentlySelectedInterpolables.Count == 1 ? "Enable" : "Enable all",
                                    onClick = p =>
                                    {
                                        foreach (Interpolable selectedInterpolable in currentlySelectedInterpolables)
                                            selectedInterpolable.enabled = true;
                                        this.UpdateInterpolablesView();
                                    }
                                });
                                elements.Add(new LeafElement()
                                {
                                    icon = this._chevronUpSprite,
                                    text = "Move up",
                                    onClick = p =>
                                    {
                                        this._interpolablesTree.MoveUp(currentlySelectedInterpolables.Select(elem => (INode)this._interpolablesTree.GetLeafNode(elem)));
                                        this.UpdateInterpolablesView();
                                    }
                                });
                                elements.Add(new LeafElement()
                                {
                                    icon = this._chevronDownSprite,
                                    text = "Move down",
                                    onClick = p =>
                                    {
                                        this._interpolablesTree.MoveDown(currentlySelectedInterpolables.Select(elem => (INode)this._interpolablesTree.GetLeafNode(elem)));
                                        this.UpdateInterpolablesView();
                                    }
                                });
                                elements.Add(new LeafElement()
                                {
                                    icon = this._deleteSprite,
                                    text = "Delete",
                                    onClick = p =>
                                    {
                                        string message = currentlySelectedInterpolables.Count > 1
                                                ? "Are you sure you want to delete these Interpolables?"
                                                : "Are you sure you want to delete this Interpolable?";
                                        UIUtility.DisplayConfirmationDialog(result =>
                                        {
                                            if (result)
                                                this.RemoveInterpolables(currentlySelectedInterpolables);
                                        }, message);
                                    }
                                });
                                UIUtility.ShowContextMenu(this._ui, localPoint, elements, 220);
                            }
                            break;
                    }
                };
                this._displayedInterpolables.Add(display);
            }
            display.gameObject.SetActive(true);
            return display;
        }

        private InterpolableModelDisplay GetInterpolableModelDisplay(int i)
        {
            InterpolableModelDisplay display;
            if (i < this._displayedInterpolableModels.Count)
                display = this._displayedInterpolableModels[i];
            else
            {
                display = new InterpolableModelDisplay();
                display.gameObject = GameObject.Instantiate(this._interpolableModelPrefab);
                display.gameObject.hideFlags = HideFlags.None;
                display.name = display.gameObject.transform.Find("Label").GetComponent<Text>();

                display.gameObject.transform.SetParent(this._verticalScrollView.content);
                display.gameObject.transform.localPosition = Vector3.zero;
                display.gameObject.transform.localScale = Vector3.one;
                this._displayedInterpolableModels.Add(display);
            }
            display.gameObject.SetActive(true);
            return display;
        }

        private HeaderDisplay GetHeaderDisplay(int i, bool treeHeader = false)
        {
            HeaderDisplay display;
            if (i < this._displayedOwnerHeader.Count)
                display = this._displayedOwnerHeader[i];
            else
            {
                display = new HeaderDisplay();
                display.gameObject = GameObject.Instantiate(this._headerPrefab);
                display.gameObject.hideFlags = HideFlags.None;
                display.layoutElement = display.gameObject.GetComponent<LayoutElement>();
                display.container = (RectTransform)display.gameObject.transform.Find("Container");
                display.name = display.container.Find("Text").GetComponent<Text>();
                display.inputField = display.container.Find("InputField").GetComponent<InputField>();

                display.gameObject.transform.SetParent(this._verticalScrollView.content);
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
                            this.UpdateInterpolablesView();
                            break;
                        case PointerEventData.InputButton.Middle:
                            if (display.group != null && Input.GetKey(KeyCode.LeftControl))
                            {
                                List<Interpolable> interpolables = new List<Interpolable>();
                                this._interpolablesTree.Recurse(display.group, (n, d) =>
                                {
                                    if (n.type == INodeType.Leaf)
                                        interpolables.Add(((LeafNode<Interpolable>)n).obj);
                                });

                                this.RemoveInterpolables(interpolables);
                                this._interpolablesTree.Remove(display.group);
                            }
                            break;
                        case PointerEventData.InputButton.Right:
                            if (display.group != null && RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)this._ui.transform, e.position, e.pressEventCamera, out Vector2 localPoint))
                            {
                                if (this._selectedInterpolables.Count != 0)
                                    this.ClearSelectedInterpolables();

                                List<AContextMenuElement> elements = new List<AContextMenuElement>();

                                elements.Add(new LeafElement()
                                {
                                    icon = this._renameSprite,
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
                                            this.UpdateInterpolablesView();
                                        });
                                        display.inputField.ActivateInputField();
                                        display.inputField.Select();
                                    }
                                });
                                elements.Add(new LeafElement()
                                {
                                    icon = this._selectAllSprite,
                                    text = "Select Interpolables under",
                                    onClick = p =>
                                    {
                                        List<Interpolable> toSelect = new List<Interpolable>();
                                        this._interpolablesTree.Recurse(display.group, (n, d) =>
                                        {
                                            if (n.type == INodeType.Leaf)
                                                toSelect.Add(((LeafNode<Interpolable>)n).obj);
                                        });
                                        this.SelectInterpolable(toSelect.ToArray());
                                    }
                                });
                                elements.Add(new LeafElement()
                                {
                                    icon = this._selectAllSprite,
                                    text = "Select keyframes",
                                    onClick = p =>
                                    {
                                        List<KeyValuePair<float, Keyframe>> toSelect = new List<KeyValuePair<float, Keyframe>>();
                                        this._interpolablesTree.Recurse(display.group, (n, d) =>
                                        {
                                            if (n.type == INodeType.Leaf)
                                                toSelect.AddRange(((LeafNode<Interpolable>)n).obj.keyframes);
                                        });
                                        this.SelectKeyframes(toSelect);
                                    }
                                });
                                elements.Add(new LeafElement()
                                {
                                    icon = this._selectAllSprite,
                                    text = "Select keyframes before cursor",
                                    onClick = p =>
                                    {
                                        List<KeyValuePair<float, Keyframe>> toSelect = new List<KeyValuePair<float, Keyframe>>();
                                        float currentTime = this._playbackTime % this._duration;
                                        this._interpolablesTree.Recurse(display.group, (n, d) =>
                                        {
                                            if (n.type == INodeType.Leaf)
                                                toSelect.AddRange(((LeafNode<Interpolable>)n).obj.keyframes.Where(k => k.Key < currentTime));
                                        });
                                        this.SelectKeyframes(toSelect);
                                    }
                                });
                                elements.Add(new LeafElement()
                                {
                                    icon = this._selectAllSprite,
                                    text = "Select keyframes after cursor",
                                    onClick = p =>
                                    {
                                        List<KeyValuePair<float, Keyframe>> toSelect = new List<KeyValuePair<float, Keyframe>>();
                                        float currentTime = this._playbackTime % this._duration;
                                        this._interpolablesTree.Recurse(display.group, (n, d) =>
                                        {
                                            if (n.type == INodeType.Leaf)
                                                toSelect.AddRange(((LeafNode<Interpolable>)n).obj.keyframes.Where(k => k.Key >= currentTime));
                                        });
                                        this.SelectKeyframes(toSelect);
                                    }
                                });
                                elements.Add(new LeafElement()
                                {
                                    icon = this._addSprite,
                                    text = "Add keyframes at cursor",
                                    onClick = p =>
                                    {
                                        float time = this._playbackTime % this._duration;
                                        this._interpolablesTree.Recurse(display.group, (n, d) =>
                                        {
                                            if (n.type == INodeType.Leaf)
                                                this.AddKeyframe(((LeafNode<Interpolable>)n).obj, time);
                                        });
                                        this.UpdateGrid();
                                    }
                                });
                                List<AContextMenuElement> treeGroups = this.GetInterpolablesTreeGroups(new List<INode> { display.group });
                                if (treeGroups.Count != 1)
                                {
                                    elements.Add(new GroupElement()
                                    {
                                        icon = this._addToFolderSprite,
                                        text = "Parent to",
                                        elements = treeGroups
                                    });
                                }
                                elements.Add(new LeafElement()
                                {
                                    icon = this._checkboxSprite,
                                    text = "Disable",
                                    onClick = p =>
                                    {
                                        this._interpolablesTree.Recurse(display.group, (n, d) =>
                                        {
                                            if (n.type == INodeType.Leaf)
                                                ((LeafNode<Interpolable>)n).obj.enabled = false;
                                        });
                                        this.UpdateInterpolablesView();
                                    }
                                });
                                elements.Add(new LeafElement()
                                {
                                    icon = this._checkboxCompositeSprite,
                                    text = "Enable",
                                    onClick = p =>
                                    {
                                        this._interpolablesTree.Recurse(display.group, (n, d) =>
                                        {
                                            if (n.type == INodeType.Leaf)
                                                ((LeafNode<Interpolable>)n).obj.enabled = true;
                                        });
                                        this.UpdateInterpolablesView();
                                    }
                                });
                                elements.Add(new LeafElement()
                                {
                                    icon = this._chevronUpSprite,
                                    text = "Move up",
                                    onClick = p =>
                                    {
                                        this._interpolablesTree.MoveUp(display.group);
                                        this.UpdateInterpolablesView();
                                    }
                                });
                                elements.Add(new LeafElement()
                                {
                                    icon = this._chevronDownSprite,
                                    text = "Move down",
                                    onClick = p =>
                                    {
                                        this._interpolablesTree.MoveDown(display.group);
                                        this.UpdateInterpolablesView();
                                    }
                                });
                                elements.Add(new LeafElement()
                                {
                                    icon = this._deleteSprite,
                                    text = "Delete",
                                    onClick = p =>
                                    {
                                        UIUtility.DisplayConfirmationDialog(result =>
                                        {
                                            if (result)
                                            {
                                                List<Interpolable> interpolables = new List<Interpolable>();
                                                this._interpolablesTree.Recurse(display.group, (n, d) =>
                                                {
                                                    if (n.type == INodeType.Leaf)
                                                        interpolables.Add(((LeafNode<Interpolable>)n).obj);
                                                });

                                                this.RemoveInterpolables(interpolables);
                                                this._interpolablesTree.Remove(display.group);
                                                this.UpdateInterpolablesView();
                                            }
                                        }, "Are you sure you want to delete this group?");
                                    }
                                });
                                UIUtility.ShowContextMenu(this._ui, localPoint, elements, 180);
                            }
                            break;
                    }
                };

                this._displayedOwnerHeader.Add(display);
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
            List<AContextMenuElement> elements = this.RecurseInterpolablesTreeGroups(this._interpolablesTree.tree, toParent, toIgnore);
            elements.Insert(0, new LeafElement()
            {
                text = "Nothing",
                onClick = p =>
                {
                    this._interpolablesTree.ParentTo(toParent, null);
                    this.UpdateInterpolablesView();
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
                        icon = this._addToFolderSprite,
                        text = group.obj.name,
                        onClick = p =>
                        {
                            this._interpolablesTree.ParentTo(toParent, group);
                            this.UpdateInterpolablesView();
                        }
                    });
                }
                if (group.children.Count(n => n.type == INodeType.Group) != 0)
                {
                    elements.Add(new GroupElement()
                    {
                        text = group.obj.name,
                        elements = this.RecurseInterpolablesTreeGroups(group.children, toParent, toIgnore)
                    });
                }
            }
            return elements;
        }

        private void HighlightInterpolable(Interpolable interpolable)
        {
            this.StartCoroutine(this.HighlightInterpolable_Routine(interpolable));
        }

        private IEnumerator HighlightInterpolable_Routine(Interpolable interpolable)
        {
            InterpolableDisplay display = this._displayedInterpolables.FirstOrDefault(d => d.interpolable.obj == interpolable);
            if (display != null)
            {
                Color first = interpolable.color.GetContrastingColor();
                Color second = first.GetContrastingColor();
                float startTime = Time.unscaledTime;
                while (Time.unscaledTime - startTime < 0.25f)
                {
                    this.UpdateInterpolableColor(display, Color.Lerp(interpolable.color, first, (Time.unscaledTime - startTime) * 4f));
                    yield return null;
                }
                startTime = Time.unscaledTime;
                while (Time.unscaledTime - startTime < 1f)
                {
                    this.UpdateInterpolableColor(display, Color.Lerp(second, first, (Mathf.Cos((Time.unscaledTime - startTime) * Mathf.PI * 4) + 1f) / 2f));
                    yield return null;
                }
                startTime = Time.unscaledTime;
                while (Time.unscaledTime - startTime < 0.25f)
                {
                    this.UpdateInterpolableColor(display, Color.Lerp(first, interpolable.color, (Time.unscaledTime - startTime) * 4f));
                    yield return null;
                }
                this.UpdateInterpolableColor(display, interpolable.color);
            }
        }

        private void PotentiallyBeginAreaSelect(PointerEventData e)
        {
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(this._keyframesContainer, e.position, e.pressEventCamera, out localPoint))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    float time = 10f * localPoint.x / (_baseGridWidth * this._zoomLevel);
                    float beat = this._blockLength / this._divisions;
                    float mod = time % beat;
                    if (mod / beat > 0.5f)
                        time += beat - mod;
                    else
                        time -= mod;
                    localPoint.x = time * (_baseGridWidth * this._zoomLevel) / 10f;
                }
                this._areaSelectFirstPoint = localPoint;
            }
            this._isAreaSelecting = false;
        }

        private void BeginAreaSelect(PointerEventData e)
        {
            this._isAreaSelecting = true;
            this._selectionArea.gameObject.SetActive(true);
        }

        private void UpdateAreaSelect(PointerEventData e)
        {
            if (this._isAreaSelecting == false)
                return;
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(this._keyframesContainer, e.position, e.pressEventCamera, out localPoint))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    float time = 10f * localPoint.x / (_baseGridWidth * this._zoomLevel);
                    float beat = this._blockLength / this._divisions;
                    float mod = time % beat;
                    if (mod / beat > 0.5f)
                        time += beat - mod;
                    else
                        time -= mod;
                    localPoint.x = time * (_baseGridWidth * this._zoomLevel) / 10f;
                }
                Vector2 min = new Vector2(Mathf.Min(this._areaSelectFirstPoint.x, localPoint.x), Mathf.Min(this._areaSelectFirstPoint.y, localPoint.y));
                Vector2 max = new Vector2(Mathf.Max(this._areaSelectFirstPoint.x, localPoint.x), Mathf.Max(this._areaSelectFirstPoint.y, localPoint.y));

                this._selectionArea.offsetMin = min;
                this._selectionArea.offsetMax = max;
            }
        }

        private void EndAreaSelect(PointerEventData e)
        {
            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(this._keyframesContainer, e.position, e.pressEventCamera, out localPoint))
                return;
            float firstTime = 10f * this._areaSelectFirstPoint.x / (_baseGridWidth * this._zoomLevel);
            float secondTime = 10f * localPoint.x / (_baseGridWidth * this._zoomLevel);
            if (Input.GetKey(KeyCode.LeftShift))
            {
                float beat = this._blockLength / this._divisions;
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
            float minY = Mathf.Min(this._areaSelectFirstPoint.y, localPoint.y);
            float maxY = Mathf.Max(this._areaSelectFirstPoint.y, localPoint.y);

            this._selectionArea.gameObject.SetActive(false);
            this._isAreaSelecting = false;

            List<KeyValuePair<float, Keyframe>> toSelect = new List<KeyValuePair<float, Keyframe>>();
            foreach (InterpolableDisplay display in this._displayedInterpolables)
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
                this.SelectAddKeyframes(toSelect);
            else
                this.SelectKeyframes(toSelect);
        }

        private void SelectAddInterpolable(params Interpolable[] interpolables)
        {
            foreach (Interpolable interpolable in interpolables)
            {
                int index = this._selectedInterpolables.FindIndex(k => k == interpolable);
                if (index != -1)
                    this._selectedInterpolables.RemoveAt(index);
                else
                    this._selectedInterpolables.Add(interpolable);
            }
            this.UpdateInterpolableSelection();
        }

        private void SelectInterpolable(params Interpolable[] interpolables)
        {
            this._selectedInterpolables.Clear();
            this.SelectAddInterpolable(interpolables);
        }

        private void ClearSelectedInterpolables()
        {
            this._selectedInterpolables.Clear();
            this.UpdateInterpolableSelection();
        }

        private void UpdateInterpolableSelection()
        {
            foreach (InterpolableDisplay display in this._displayedInterpolables)
            {
                bool selected = this._selectedInterpolables.Any(e => e == display.interpolable.obj);
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
            this._durationInputField.text = $"{Mathf.FloorToInt(this._duration / 60):00}:{(this._duration % 60):00.00}";

            this._horizontalScrollView.content.sizeDelta = new Vector2(_baseGridWidth * this._zoomLevel * this._duration / 10f, this._horizontalScrollView.content.sizeDelta.y);
            this.UpdateGridMaterial();
            int max = Mathf.CeilToInt(this._duration / this._blockLength);
            int textIndex = 0;
            for (int i = 1; i < max; i++)
            {
                Text t;
                if (textIndex < this._timeTexts.Count)
                    t = this._timeTexts[textIndex];
                else
                {
                    t = UIUtility.CreateText("Time " + textIndex, this._textsContainer);
                    t.alignByGeometry = true;
                    t.alignment = TextAnchor.MiddleCenter;
                    t.color = Color.white;
                    t.raycastTarget = false;
                    t.rectTransform.SetRect(Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(60f, 0f));
                    this._timeTexts.Add(t);
                }
                t.text = $"{Mathf.FloorToInt((i * this._blockLength) / 60):00}:{((i * this._blockLength) % 60):00.##}";
                t.gameObject.SetActive(true);
                t.rectTransform.anchoredPosition = new Vector2(i * this._blockLength * _baseGridWidth * this._zoomLevel / 10, t.rectTransform.anchoredPosition.y);
                ++textIndex;
            }
            for (; textIndex < this._timeTexts.Count; textIndex++)
                this._timeTexts[textIndex].gameObject.SetActive(false);


            bool showAll = this._allToggle.isOn;
            int keyframeIndex = 0;
            int interpolableIndex = 0;
            this.UpdateKeyframesTree(this._interpolablesTree.tree, showAll, ref interpolableIndex, ref keyframeIndex);

            for (; keyframeIndex < this._displayedKeyframes.Count; ++keyframeIndex)
            {
                KeyframeDisplay display = this._displayedKeyframes[keyframeIndex];
                display.gameObject.SetActive(false);
                display.keyframe = null;
            }

            this.UpdateKeyframeSelection();

            this.UpdateCursor();

            this.ExecuteDelayed(() => this._keyframesContainer.sizeDelta = new Vector2(this._keyframesContainer.sizeDelta.x, this._verticalScrollView.content.rect.height), 2);
        }

        private void UpdateKeyframesTree(List<INode> nodes, bool showAll, ref int interpolableIndex, ref int keyframeIndex)
        {
            foreach (INode node in nodes)
            {
                switch (node.type)
                {
                    case INodeType.Leaf:
                        Interpolable interpolable = ((LeafNode<Interpolable>)node).obj;
                        if (showAll == false && ((interpolable.oci != null && interpolable.oci != this._selectedOCI) || !interpolable.ShouldShow()))
                            continue;

                        if (interpolable.name.IndexOf(this._interpolablesSearchField.text, StringComparison.OrdinalIgnoreCase) == -1)
                            continue;

                        InterpolableDisplay interpolableDisplay = this._displayedInterpolables[interpolableIndex];

                        foreach (KeyValuePair<float, Keyframe> keyframePair in interpolable.keyframes)
                        {
                            KeyframeDisplay display;
                            if (keyframeIndex < this._displayedKeyframes.Count)
                                display = this._displayedKeyframes[keyframeIndex];
                            else
                            {
                                display = new KeyframeDisplay();
                                display.gameObject = GameObject.Instantiate(this._keyframePrefab);
                                display.gameObject.hideFlags = HideFlags.None;
                                display.image = display.gameObject.transform.Find("RawImage").GetComponent<RawImage>();

                                display.gameObject.transform.SetParent(this._keyframesContainer);
                                display.gameObject.transform.localPosition = Vector3.zero;
                                display.gameObject.transform.localScale = Vector3.one;

                                PointerEnterHandler pointerEnter = display.gameObject.AddComponent<PointerEnterHandler>();
                                pointerEnter.onPointerEnter = (e) =>
                                {
                                    this._tooltip.transform.parent.gameObject.SetActive(true);
                                    float t = display.keyframe.parent.keyframes.First(k => k.Value == display.keyframe).Key;
                                    this._tooltip.text = $"T: {Mathf.FloorToInt(t / 60):00}:{t % 60:00.########}\nV: {display.keyframe.value}";
                                };
                                pointerEnter.onPointerExit = (e) => { this._tooltip.transform.parent.gameObject.SetActive(false); };
                                PointerDownHandler pointerDown = display.gameObject.AddComponent<PointerDownHandler>();
                                pointerDown.onPointerDown = (e) =>
                                {
                                    if (Input.GetKey(KeyCode.LeftAlt))
                                        return;
                                    switch (e.button)
                                    {
                                        case PointerEventData.InputButton.Left:
                                            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                                                this.SelectAddKeyframes(display.keyframe.parent.keyframes.First(k => k.Value == display.keyframe));
                                            else if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                                            {
                                                KeyValuePair<float, Keyframe> lastSelected = this._selectedKeyframes.LastOrDefault(k => k.Value.parent == display.keyframe.parent);
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
                                                    this.SelectAddKeyframes(display.keyframe.parent.keyframes.Where(k => k.Key > minTime && k.Key < maxTime));
                                                    this.SelectAddKeyframes(selectingNow);
                                                }
                                                else
                                                    this.SelectAddKeyframes(display.keyframe.parent.keyframes.First(k => k.Value == display.keyframe));
                                            }
                                            else
                                                this.SelectKeyframes(display.keyframe.parent.keyframes.First(k => k.Value == display.keyframe));

                                            break;
                                        case PointerEventData.InputButton.Right:
                                            this.SeekPlaybackTime(display.keyframe.parent.keyframes.First(k => k.Value == display.keyframe).Key);
                                            break;
                                        case PointerEventData.InputButton.Middle:
                                            if (Input.GetKey(KeyCode.LeftControl))
                                            {
                                                List<KeyValuePair<float, Keyframe>> toDelete = new List<KeyValuePair<float, Keyframe>>();
                                                if (Input.GetKey(KeyCode.LeftShift))
                                                    toDelete.AddRange(this._selectedKeyframes);
                                                KeyValuePair<float, Keyframe> kPair = display.keyframe.parent.keyframes.FirstOrDefault(k => k.Value == display.keyframe);
                                                if (kPair.Value != null)
                                                    toDelete.Add(kPair);
                                                if (toDelete.Count != 0)
                                                {
                                                    this.DeleteKeyframes(toDelete);
                                                    this._tooltip.transform.parent.gameObject.SetActive(false);
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
                                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(this._keyframesContainer, e.position, e.pressEventCamera, out localPoint))
                                    {
                                        this._selectedKeyframesXOffset.Clear();
                                        foreach (KeyValuePair<float, Keyframe> selectedKeyframe in this._selectedKeyframes)
                                        {
                                            KeyframeDisplay selectedDisplay = this._displayedKeyframes.Find(d => d.keyframe == selectedKeyframe.Value);
                                            this._selectedKeyframesXOffset.Add(selectedDisplay, ((RectTransform)selectedDisplay.gameObject.transform).anchoredPosition.x - localPoint.x);
                                        }
                                    }
                                    if (this._selectedKeyframesXOffset.Count != 0)
                                        this._isPlaying = false;
                                    e.Reset();
                                };
                                dragHandler.onDrag = e =>
                                {
                                    if (this._selectedKeyframesXOffset.Count == 0)
                                        return;
                                    Vector2 localPoint;
                                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(this._keyframesContainer, e.position, e.pressEventCamera, out localPoint))
                                    {
                                        float x = localPoint.x;
                                        foreach (KeyValuePair<KeyframeDisplay, float> pair in this._selectedKeyframesXOffset)
                                        {
                                            float localX = localPoint.x + pair.Value;
                                            if (localX < 0f)
                                                x = localPoint.x - localX;
                                        }

                                        if (Input.GetKey(KeyCode.LeftShift))
                                        {
                                            float time = 10f * x / (_baseGridWidth * this._zoomLevel);
                                            float beat = this._blockLength / this._divisions;
                                            float mod = time % beat;
                                            if (mod / beat > 0.5f)
                                                time += beat - mod;
                                            else
                                                time -= mod;
                                            x = (time * _baseGridWidth * this._zoomLevel) / 10f - this._selectedKeyframesXOffset[display];
                                        }

                                        foreach (KeyValuePair<KeyframeDisplay, float> pair in this._selectedKeyframesXOffset)
                                        {
                                            RectTransform rt = ((RectTransform)pair.Key.gameObject.transform);
                                            rt.anchoredPosition = new Vector2(x + pair.Value, rt.anchoredPosition.y);
                                        }
                                    }
                                    e.Reset();
                                };

                                dragHandler.onEndDrag = e =>
                                {
                                    if (this._selectedKeyframesXOffset.Count == 0)
                                        return;
                                    foreach (KeyValuePair<KeyframeDisplay, float> pair in this._selectedKeyframesXOffset)
                                    {
                                        RectTransform rt = ((RectTransform)pair.Key.gameObject.transform);
                                        float time = 10f * rt.anchoredPosition.x / (_baseGridWidth * this._zoomLevel);
                                        this.MoveKeyframe(pair.Key.keyframe, time);

                                        int index = this._selectedKeyframes.FindIndex(k => k.Value == pair.Key.keyframe);
                                        this._selectedKeyframes[index] = new KeyValuePair<float, Keyframe>(time, pair.Key.keyframe);
                                    }

                                    e.Reset();
                                    this.UpdateKeyframeWindow(false);
                                    this._selectedKeyframesXOffset.Clear();
                                };

                                this._displayedKeyframes.Add(display);
                            }
                            display.gameObject.SetActive(true);
                            ((RectTransform)display.gameObject.transform).anchoredPosition = new Vector2(_baseGridWidth * this._zoomLevel * keyframePair.Key / 10f, ((RectTransform)interpolableDisplay.gameObject.transform).anchoredPosition.y);
                            display.keyframe = keyframePair.Value;
                            ++keyframeIndex;
                        }
                        ++interpolableIndex;
                        break;
                    case INodeType.Group:
                        GroupNode<InterpolableGroup> group = (GroupNode<InterpolableGroup>)node;
                        if (group.obj.expanded)
                            this.UpdateKeyframesTree(group.children, showAll, ref interpolableIndex, ref keyframeIndex);
                        break;
                }
            }
        }

        private void UpdateGridMaterial()
        {
            this._gridImage.material.SetFloat("_TilingX", this._duration / 10f);
            this._gridImage.material.SetFloat("_BlockLength", this._blockLength);
            this._gridImage.material.SetFloat("_Divisions", this._divisions);
            this._gridImage.enabled = false;
            this._gridImage.enabled = true;
        }

        private void SelectAddKeyframes(params KeyValuePair<float, Keyframe>[] keyframes)
        {
            this.SelectAddKeyframes((IEnumerable<KeyValuePair<float, Keyframe>>)keyframes);
        }

        private void SelectAddKeyframes(IEnumerable<KeyValuePair<float, Keyframe>> keyframes)
        {
            foreach (KeyValuePair<float, Keyframe> keyframe in keyframes)
            {
                int index = this._selectedKeyframes.FindIndex(k => k.Value == keyframe.Value);
                if (index != -1)
                    this._selectedKeyframes.RemoveAt(index);
                else
                    this._selectedKeyframes.Add(keyframe);
            }
            this._keyframeSelectionSize = this._selectedKeyframes.Max(k => k.Key) - this._selectedKeyframes.Min(k => k.Key);
            this.UpdateKeyframeSelection();
            this.UpdateKeyframeWindow();
        }

        private void SelectKeyframes(params KeyValuePair<float, Keyframe>[] keyframes)
        {
            this.SelectKeyframes((IEnumerable<KeyValuePair<float, Keyframe>>)keyframes);
        }

        private void SelectKeyframes(IEnumerable<KeyValuePair<float, Keyframe>> keyframes)
        {
            this._selectedKeyframes.Clear();
            if (keyframes.Count() != 0)
                this.SelectAddKeyframes(keyframes);
        }

        private void UpdateKeyframeSelection()
        {
            foreach (KeyframeDisplay display in this._displayedKeyframes)
                display.image.color = this._selectedKeyframes.Any(k => k.Value == display.keyframe) ? Color.green : Color.red;
        }

        private void ScaleKeyframeSelection(float scrollDelta)
        {
            float min = float.PositiveInfinity;
            float max = float.NegativeInfinity;
            foreach (KeyValuePair<float, Keyframe> pair in this._selectedKeyframes)
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
                double sizeMultiplier = Math.Round(Math.Round(currentSize * 10) / this._keyframeSelectionSize + multiplier * (scrollDelta > 0 ? 1 : -1)) / 10;
                bool clamped = false;
                if (sizeMultiplier < 0.1)
                {
                    clamped = true;
                    sizeMultiplier = 0.1;
                }
                newSize = sizeMultiplier * this._keyframeSelectionSize;
                foreach (KeyValuePair<float, Keyframe> pair in this._selectedKeyframes)
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

            for (int i = 0; i < this._selectedKeyframes.Count; i++)
            {
                KeyValuePair<float, Keyframe> pair = this._selectedKeyframes[i];
                float newTime = (float)(((pair.Key - min) * newSize) / currentSize + min);
                this.MoveKeyframe(pair.Value, newTime);
                this._selectedKeyframes[i] = new KeyValuePair<float, Keyframe>(newTime, pair.Value);
            }
            this.UpdateKeyframeWindow(false);
            this.UpdateGrid();
        }

        private void OnKeyframeContainerMouseDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Middle && Input.GetKey(KeyCode.LeftControl) == false && RectTransformUtility.ScreenPointToLocalPointInRectangle(this._keyframesContainer, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                float time = 10f * localPoint.x / (_baseGridWidth * this._zoomLevel);
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    float beat = this._blockLength / this._divisions;
                    float mod = time % beat;
                    if (mod / beat > 0.5f)
                        time += beat - mod;
                    else
                        time -= mod;
                }
                if (Input.GetKey(KeyCode.LeftAlt) && this._selectedInterpolables.Count != 0)
                {
                    foreach (Interpolable selectedInterpolable in this._selectedInterpolables)
                        this.AddKeyframe(selectedInterpolable, time);
                    this.UpdateGrid();
                }
                else
                {
                    if (this._selectedInterpolables.Count != 0)
                        this.ClearSelectedInterpolables();
                    InterpolableModel model = null;
                    float distance = float.MaxValue;
                    foreach (InterpolableDisplay display in this._displayedInterpolables)
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
                    foreach (InterpolableModelDisplay display in this._displayedInterpolableModels)
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
                            interpolable = this.AddInterpolable(model);

                        if (interpolable != null)
                        {
                            this.AddKeyframe(interpolable, time);
                            this.UpdateGrid();
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
                this.UpdateGrid();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(_name + ": couldn't add keyframe to interpolable with value:" + interpolable + "\n" + e);
            }
        }

        private void CopyKeyframes()
        {
            this._copiedKeyframes.Clear();
            foreach (KeyValuePair<float, Keyframe> pair in this._selectedKeyframes)
                this._copiedKeyframes.Add(new KeyValuePair<float, Keyframe>(pair.Key, new Keyframe(pair.Value)));
        }

        private void CutKeyframes()
        {
            this.CopyKeyframes();
            if (this._selectedKeyframes.Count != 0)
                this.DeleteKeyframes(this._selectedKeyframes, false);
        }

        private void PasteKeyframes()
        {
            if (this._copiedKeyframes.Count == 0)
                return;
            List<KeyValuePair<float, Keyframe>> toSelect = new List<KeyValuePair<float, Keyframe>>();
            float time = this._playbackTime % this._duration;
            if (time == 0f && this._playbackTime == this._duration)
                time = this._duration;
            float startOffset = this._copiedKeyframes.Min(k => k.Key);
            if (Input.GetKey(KeyCode.LeftAlt))
            {
                float max = this._copiedKeyframes.Max(k => k.Key);
                //// If they keyframe(s) that are at the end of the selection would conflict with any pushed keyframes (those that are currently on the cursor), then cancel
                //if (this._copiedKeyframes.Where(k => Mathf.Approximately(k.Key, max)).Any(k => k.Value.parent.keyframes.ContainsKey(time)))
                //    return;
                double duration = max - startOffset + (this._blockLength / this._divisions);
                foreach (IGrouping<Interpolable, KeyValuePair<float, Keyframe>> group in this._copiedKeyframes.GroupBy(k => k.Value.parent))
                {
                    foreach (KeyValuePair<float, Keyframe> pair in @group.Key.keyframes.Reverse())
                    {
                        if (pair.Key >= time)
                            this.MoveKeyframe(pair.Value, (float)(pair.Key + duration));
                    }
                }
            }
            else if (this._copiedKeyframes.Any(k => k.Value.parent.keyframes.ContainsKey(time + k.Key - startOffset)))
                return;
            foreach (KeyValuePair<float, Keyframe> pair in this._copiedKeyframes)
            {
                float finalTime = time + pair.Key - startOffset;
                Keyframe newKeyframe = new Keyframe(pair.Value);
                pair.Value.parent.keyframes.Add(finalTime, newKeyframe);
                // This is dumb as shit but I have no choice
                toSelect.Add(new KeyValuePair<float, Keyframe>(finalTime, newKeyframe));
            }
            this.SelectKeyframes(toSelect);
            this.UpdateGrid();
        }

        private void MoveKeyframe(Keyframe keyframe, float destinationTime)
        {
            UnityEngine.Debug.LogError(keyframe.parent.keyframes.IndexOfValue(keyframe));
            keyframe.parent.keyframes.RemoveAt(keyframe.parent.keyframes.IndexOfValue(keyframe));
            keyframe.parent.keyframes.Add(destinationTime, keyframe);
            int i = this._selectedKeyframes.FindIndex(k => k.Value == keyframe);
            if (i != -1)
                this._selectedKeyframes[i] = new KeyValuePair<float, Keyframe>(destinationTime, keyframe);
        }

        private void OnGridTopMouse(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && RectTransformUtility.ScreenPointToLocalPointInRectangle(this._gridTop, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                float time = 10f * localPoint.x / (_baseGridWidth * this._zoomLevel);
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    float beat = this._blockLength / this._divisions;
                    float mod = time % beat;
                    if (mod / beat > 0.5f)
                        time += beat - mod;
                    else
                        time -= mod;
                }
                time = Mathf.Clamp(time, 0, this._duration);
                this.SeekPlaybackTime(time);
            }
        }

        private void OnResizeWindow(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && RectTransformUtility.ScreenPointToLocalPointInRectangle(this._timelineWindow, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                localPoint.x = Mathf.Clamp(localPoint.x, 615f, ((RectTransform)this._ui.transform).rect.width * 0.85f);
                localPoint.y = Mathf.Clamp(localPoint.y, 330f, ((RectTransform)this._ui.transform).rect.height * 0.85f);
                this._timelineWindow.sizeDelta = localPoint;
            }
        }

        private void SeekPlaybackTime(float t)
        {
            if (t == this._playbackTime)
                return;
            this._playbackTime = t;
            this._startTime = Time.time - this._playbackTime;
            bool isPlaying = this._isPlaying;
            this._isPlaying = true;
            this.UpdateCursor();
            this.Interpolate(true);
            this.Interpolate(false);
            this._isPlaying = isPlaying;
        }

        private void ToggleSingleFilesPanel()
        {
            this._singleFilesPanel.SetActive(!this._singleFilesPanel.activeSelf);
            if (this._singleFilesPanel.activeSelf)
                this.UpdateSingleFilesPanel();
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
                if (i < this._displayedSingleFiles.Count)
                    display = this._displayedSingleFiles[i];
                else
                {
                    display = new SingleFileDisplay();
                    display.toggle = GameObject.Instantiate(this._singleFilePrefab).GetComponent<Toggle>();
                    display.toggle.gameObject.hideFlags = HideFlags.None;
                    display.text = display.toggle.GetComponentInChildren<Text>();

                    display.toggle.transform.SetParent(this._singleFilesContainer);
                    display.toggle.transform.localScale = Vector3.one;
                    display.toggle.transform.localPosition = Vector3.zero;
                    display.toggle.group = this._singleFilesContainer.GetComponent<ToggleGroup>();
                    this._displayedSingleFiles.Add(display);
                }
                string fileName = Path.GetFileNameWithoutExtension(files[i]);

                display.toggle.gameObject.SetActive(true);
                display.toggle.onValueChanged = new Toggle.ToggleEvent();
                display.toggle.onValueChanged.AddListener(b =>
                {
                    if (display.toggle.isOn)
                        this._singleFileNameField.text = fileName;
                });
                display.text.text = fileName;
            }

            for (; i < this._displayedSingleFiles.Count; ++i)
                this._displayedSingleFiles[i].toggle.gameObject.SetActive(false);
            this.UpdateSingleFileSelection();
        }

        private void UpdateSingleFileSelection()
        {
            foreach (SingleFileDisplay display in this._displayedSingleFiles)
            {
                if (display.toggle.gameObject.activeSelf == false)
                    break;
                display.toggle.isOn = string.Compare(this._singleFileNameField.text, display.text.text, StringComparison.OrdinalIgnoreCase) == 0;
            }
        }

        private void LoadSingleFile()
        {
            if (this._selectedOCI == null)
                return;
            string path = Path.Combine(_singleFilesFolder, this._singleFileNameField.text + ".xml");
            if (File.Exists(path))
                this.LoadSingle(path);
        }

        private void SaveSingleFile()
        {
            if (this._selectedOCI == null)
                return;
            string selected = this._singleFileNameField.text;
            foreach (char c in Path.GetInvalidPathChars())
                selected = selected.Replace(c.ToString(), "");
            if (string.IsNullOrEmpty(selected))
                return;
            if (Directory.Exists(_singleFilesFolder) == false)
                Directory.CreateDirectory(_singleFilesFolder);
            string path = Path.Combine(_singleFilesFolder, selected + ".xml");
            this.SaveSingle(path);
            this._singleFileNameField.text = selected;
            this.UpdateSingleFilesPanel();
        }

        private void DeleteSingleFile()
        {
            UIUtility.DisplayConfirmationDialog(result =>
            {
                if (result)
                {
                    string path = Path.Combine(_singleFilesFolder, this._singleFileNameField.text + ".xml");
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                        this._singleFileNameField.text = "";
                        this.UpdateSingleFilesPanel();
                    }
                }
            }, "Are you sure you want to delete this file?");
        }
        #endregion

        #region Keyframe Window
        private void OpenKeyframeWindow()
        {
            this._keyframeWindow.gameObject.SetActive(true);
        }

        private void CloseKeyframeWindow()
        {
            this._keyframeWindow.gameObject.SetActive(false);
            this._selectedKeyframeCurvePointIndex = -1;
        }

        private void SelectPreviousKeyframe()
        {
            if (this._selectedKeyframes.Count != 1)
                return;
            KeyValuePair<float, Keyframe> firstSelected = this._selectedKeyframes[0];
            KeyValuePair<float, Keyframe> keyframe = firstSelected.Value.parent.keyframes.LastOrDefault(f => f.Key < firstSelected.Key);
            if (keyframe.Value != null)
                this.SelectKeyframes(keyframe);
        }

        private void SelectNextKeyframe()
        {
            if (this._selectedKeyframes.Count != 1)
                return;
            KeyValuePair<float, Keyframe> firstSelected = this._selectedKeyframes[0];
            KeyValuePair<float, Keyframe> keyframe = firstSelected.Value.parent.keyframes.FirstOrDefault(f => f.Key > firstSelected.Key);
            if (keyframe.Value != null)
                this.SelectKeyframes(keyframe);
        }

        private void UseCurrentTime()
        {
            float currentTime = this._playbackTime % this._duration;
            this.SaveKeyframeTime(currentTime);
            this.UpdateKeyframeTimeTextField();
        }

        private void DragAtCurrentTime()
        {
            float currentTime = this._playbackTime % this._duration;
            float min = this._selectedKeyframes.Min(k => k.Key);

            // Checking if all keyframes can be moved.
            foreach (KeyValuePair<float, Keyframe> pair in this._selectedKeyframes)
            {
                Keyframe potentialDuplicateKeyframe;
                float time = currentTime + pair.Key - min;
                if (pair.Value.parent.keyframes.TryGetValue(time, out potentialDuplicateKeyframe) && potentialDuplicateKeyframe != pair.Value)
                    return;
            }

            foreach (KeyValuePair<float, Keyframe> pair in this._selectedKeyframes)
                pair.Value.parent.keyframes.Remove(pair.Key);

            for (int i = 0; i < this._selectedKeyframes.Count; i++)
            {
                KeyValuePair<float, Keyframe> pair = this._selectedKeyframes[i];
                float time = currentTime + pair.Key - min;
                pair.Value.parent.keyframes.Add(time, pair.Value);
                this._selectedKeyframes[i] = new KeyValuePair<float, Keyframe>(time, pair.Value);
            }

            this.UpdateKeyframeTimeTextField();
            this.ExecuteDelayed(this.UpdateCursor2);
            this.UpdateGrid();
        }

        private void UpdateSelectedKeyframeTime(string s)
        {
            float time = this.ParseTime(this._keyframeTimeTextField.text);
            if (time < 0)
                return;
            this.SaveKeyframeTime(time);
        }

        private void UseCurrentValue()
        {
            foreach (KeyValuePair<float, Keyframe> pair in this._selectedKeyframes)
                pair.Value.value = pair.Value.parent.GetValue();
            this.UpdateKeyframeValueText();
        }

        private void OnCurveMouseDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Middle && Input.GetKey(KeyCode.LeftControl) == false && RectTransformUtility.ScreenPointToLocalPointInRectangle(this._curveContainer.rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                float time = localPoint.x / this._curveContainer.rectTransform.rect.width;
                float value = localPoint.y / this._curveContainer.rectTransform.rect.height;
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
                this._selectedKeyframeCurvePointIndex = this._selectedKeyframes[0].Value.curve.AddKey(curveKey);
                this.SaveKeyframeCurve();
                this.UpdateCurve();
            }
        }

        private void UpdateCurvePointTime(string s)
        {
            AnimationCurve curve = this._selectedKeyframes[0].Value.curve;
            if (this._selectedKeyframeCurvePointIndex >= 1 && this._selectedKeyframeCurvePointIndex < curve.length - 1)
            {
                UnityEngine.Keyframe curveKey = curve[this._selectedKeyframeCurvePointIndex];
                float v;
                if (float.TryParse(this._curveTimeInputField.text, out v))
                {
                    v = Mathf.Clamp(v, 0.001f, 0.999f);
                    if (!curve.keys.Any(k => k.time == v))
                    {
                        curveKey.time = v;
                        curve.RemoveKey(this._selectedKeyframeCurvePointIndex);
                        this._selectedKeyframeCurvePointIndex = curve.AddKey(curveKey);
                        this.SaveKeyframeCurve();
                    }
                }
            }
            this.UpdateCurvePointTime();
            this.UpdateCurve();
        }

        private void UpdateCurvePointTime(float f)
        {
            AnimationCurve curve = this._selectedKeyframes[0].Value.curve;
            if (this._selectedKeyframeCurvePointIndex >= 1 && this._selectedKeyframeCurvePointIndex < curve.length - 1)
            {
                UnityEngine.Keyframe curveKey = curve[this._selectedKeyframeCurvePointIndex];
                float v = Mathf.Clamp(this._curveTimeSlider.value, 0.001f, 0.999f);
                if (curve.keys.Any(k => k.time == v) == false)
                {
                    curveKey.time = v;
                    curve.RemoveKey(this._selectedKeyframeCurvePointIndex);
                    this._selectedKeyframeCurvePointIndex = curve.AddKey(curveKey);
                    this.SaveKeyframeCurve();
                }
            }
            this.UpdateCurvePointTime();
            this.UpdateCurve();
        }

        private void UpdateCurvePointTime()
        {
            UnityEngine.Keyframe curveKey;
            AnimationCurve curve = this._selectedKeyframes[0].Value.curve;
            if (this._selectedKeyframeCurvePointIndex != -1 && this._selectedKeyframeCurvePointIndex < curve.length)
                curveKey = curve[this._selectedKeyframeCurvePointIndex];
            else
                curveKey = new UnityEngine.Keyframe();
            this._curveTimeInputField.text = curveKey.time.ToString("0.00000");
            this._curveTimeSlider.SetValueNoCallback(curveKey.time);
        }

        private void UpdateCurvePointValue(string s)
        {
            AnimationCurve curve = this._selectedKeyframes[0].Value.curve;
            if (this._selectedKeyframeCurvePointIndex >= 1 && this._selectedKeyframeCurvePointIndex < curve.length - 1)
            {
                UnityEngine.Keyframe curveKey = curve[this._selectedKeyframeCurvePointIndex];
                float v;
                if (float.TryParse(this._curveValueInputField.text, out v))
                {
                    curveKey.value = v;
                    curve.RemoveKey(this._selectedKeyframeCurvePointIndex);
                    this._selectedKeyframeCurvePointIndex = curve.AddKey(curveKey);
                    this.SaveKeyframeCurve();
                }
            }
            this.UpdateCurvePointValue();
            this.UpdateCurve();
        }

        private void UpdateCurvePointValue(float f)
        {
            AnimationCurve curve = this._selectedKeyframes[0].Value.curve;
            if (this._selectedKeyframeCurvePointIndex >= 1 && this._selectedKeyframeCurvePointIndex < curve.length - 1)
            {
                UnityEngine.Keyframe curveKey = curve[this._selectedKeyframeCurvePointIndex];
                curveKey.value = this._curveValueSlider.value;
                curve.RemoveKey(this._selectedKeyframeCurvePointIndex);
                this._selectedKeyframeCurvePointIndex = curve.AddKey(curveKey);
                this.SaveKeyframeCurve();
            }
            this.UpdateCurvePointValue();
            this.UpdateCurve();
        }

        private void UpdateCurvePointValue()
        {
            UnityEngine.Keyframe curveKey;
            AnimationCurve curve = this._selectedKeyframes[0].Value.curve;
            if (this._selectedKeyframeCurvePointIndex != -1 && this._selectedKeyframeCurvePointIndex < curve.length)
                curveKey = curve[this._selectedKeyframeCurvePointIndex];
            else
                curveKey = new UnityEngine.Keyframe();
            this._curveValueInputField.text = curveKey.value.ToString("0.00000");
            this._curveValueSlider.SetValueNoCallback(curveKey.value);
        }

        private void UpdateCurvePointInTangent(string s)
        {
            AnimationCurve curve = this._selectedKeyframes[0].Value.curve;
            if (this._selectedKeyframeCurvePointIndex != -1 && this._selectedKeyframeCurvePointIndex < curve.length)
            {
                UnityEngine.Keyframe curveKey = curve[this._selectedKeyframeCurvePointIndex];
                float v;
                if (float.TryParse(this._curveInTangentInputField.text, out v))
                {
                    if (v == 90f || v == -90f)
                        curveKey.inTangent = float.NegativeInfinity;
                    else
                        curveKey.inTangent = Mathf.Tan(v * Mathf.Deg2Rad);
                    curve.RemoveKey(this._selectedKeyframeCurvePointIndex);
                    this._selectedKeyframeCurvePointIndex = curve.AddKey(curveKey);
                    this.SaveKeyframeCurve();
                }
            }
            this.UpdateCurvePointInTangent();
            this.UpdateCurve();
        }

        private void UpdateCurvePointInTangent(float f)
        {
            AnimationCurve curve = this._selectedKeyframes[0].Value.curve;
            if (this._selectedKeyframeCurvePointIndex != -1 && this._selectedKeyframeCurvePointIndex < curve.length)
            {
                UnityEngine.Keyframe curveKey = curve[this._selectedKeyframeCurvePointIndex];
                if (this._curveInTangentSlider.value == 90f || this._curveInTangentSlider.value == -90f)
                    curveKey.inTangent = float.PositiveInfinity;
                else
                    curveKey.inTangent = Mathf.Tan(this._curveInTangentSlider.value * Mathf.Deg2Rad);
                curve.RemoveKey(this._selectedKeyframeCurvePointIndex);
                this._selectedKeyframeCurvePointIndex = curve.AddKey(curveKey);
                this.SaveKeyframeCurve();
            }
            this.UpdateCurvePointInTangent();
            this.UpdateCurve();
        }

        private void UpdateCurvePointInTangent()
        {
            UnityEngine.Keyframe curveKey;
            AnimationCurve curve = this._selectedKeyframes[0].Value.curve;
            if (this._selectedKeyframeCurvePointIndex != -1 && this._selectedKeyframeCurvePointIndex < curve.length)
                curveKey = curve[this._selectedKeyframeCurvePointIndex];
            else
                curveKey = new UnityEngine.Keyframe();
            float v = Mathf.Atan(curveKey.inTangent) * Mathf.Rad2Deg;
            this._curveInTangentInputField.text = v.ToString("0.000");
            this._curveInTangentSlider.SetValueNoCallback(v);
        }

        private void UpdateCurvePointOutTangent(string s)
        {
            AnimationCurve curve = this._selectedKeyframes[0].Value.curve;
            if (this._selectedKeyframeCurvePointIndex != -1 && this._selectedKeyframeCurvePointIndex < curve.length)
            {
                UnityEngine.Keyframe curveKey = curve[this._selectedKeyframeCurvePointIndex];
                float v;
                if (float.TryParse(this._curveOutTangentInputField.text, out v))
                {
                    if (v == 90f || v == -90f)
                        curveKey.outTangent = float.NegativeInfinity;
                    else
                        curveKey.outTangent = Mathf.Tan(v * Mathf.Deg2Rad);
                    curve.RemoveKey(this._selectedKeyframeCurvePointIndex);
                    this._selectedKeyframeCurvePointIndex = curve.AddKey(curveKey);
                    this.SaveKeyframeCurve();
                }
            }
            this.UpdateCurvePointOutTangent();
            this.UpdateCurve();
        }

        private void UpdateCurvePointOutTangent(float f)
        {
            AnimationCurve curve = this._selectedKeyframes[0].Value.curve;
            if (this._selectedKeyframeCurvePointIndex != -1 && this._selectedKeyframeCurvePointIndex < curve.length)
            {
                UnityEngine.Keyframe curveKey = curve[this._selectedKeyframeCurvePointIndex];
                if (this._curveOutTangentSlider.value == 90f || this._curveOutTangentSlider.value == -90f)
                    curveKey.outTangent = float.NegativeInfinity;
                else
                    curveKey.outTangent = Mathf.Tan(this._curveOutTangentSlider.value * Mathf.Deg2Rad);
                curve.RemoveKey(this._selectedKeyframeCurvePointIndex);
                this._selectedKeyframeCurvePointIndex = curve.AddKey(curveKey);
                this.SaveKeyframeCurve();
            }
            this.UpdateCurvePointOutTangent();
            this.UpdateCurve();
        }

        private void UpdateCurvePointOutTangent()
        {
            UnityEngine.Keyframe curveKey;
            AnimationCurve curve = this._selectedKeyframes[0].Value.curve;
            if (this._selectedKeyframeCurvePointIndex != -1 && this._selectedKeyframeCurvePointIndex < curve.length)
                curveKey = curve[this._selectedKeyframeCurvePointIndex];
            else
                curveKey = new UnityEngine.Keyframe();
            float v = Mathf.Atan(curveKey.outTangent) * Mathf.Rad2Deg;
            this._curveOutTangentInputField.text = v.ToString("0.000");
            this._curveOutTangentSlider.SetValueNoCallback(v);
        }

        private void CopyKeyframeCurve()
        {
            this._copiedKeyframeCurve.keys = this._selectedKeyframes[0].Value.curve.keys;
        }

        private void PasteKeyframeCurve()
        {
            this._selectedKeyframes[0].Value.curve.keys = this._copiedKeyframeCurve.keys;
            this.SaveKeyframeCurve();
            this.UpdateCurve();
        }

        private void InvertKeyframeCurve()
        {
            AnimationCurve curve = this._selectedKeyframes[0].Value.curve;
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
            this.SaveKeyframeCurve();
            this.UpdateCurve();
        }

        private void ApplyKeyframeCurvePreset(AnimationCurve preset)
        {
            this._selectedKeyframes[0].Value.curve = new AnimationCurve(preset.keys);
            this.SaveKeyframeCurve();
            this.UpdateCurve();
        }

        private void UpdateCursor2()
        {
            if (!this._keyframeWindow.activeSelf)
                return;
            if (this._selectedKeyframes.Count == 1)
            {
                KeyValuePair<float, Keyframe> selectedKeyframe = this._selectedKeyframes[0];

                if (this._playbackTime >= selectedKeyframe.Key)
                {
                    KeyValuePair<float, Keyframe> after = selectedKeyframe.Value.parent.keyframes.FirstOrDefault(k => k.Key > selectedKeyframe.Key);
                    if (after.Value != null && this._playbackTime <= after.Key)
                    {
                        this._cursor2.gameObject.SetActive(true);

                        float normalizedTime = (this._playbackTime - selectedKeyframe.Key) / (after.Key - selectedKeyframe.Key);
                        this._cursor2.anchoredPosition = new Vector2(normalizedTime * this._curveContainer.rectTransform.rect.width, this._cursor2.anchoredPosition.y);
                    }
                    else
                        this._cursor2.gameObject.SetActive(false);
                }
                else
                    this._cursor2.gameObject.SetActive(false);
            }
            else
                this._cursor2.gameObject.SetActive(false);
        }

        private void DeleteSelectedKeyframes()
        {
            UIUtility.DisplayConfirmationDialog(result => 
                    {
                        if (result)
                            this.DeleteKeyframes(this._selectedKeyframes);
                    }, this._selectedKeyframes.Count == 1 ? "Are you sure you want to delete this Keyframe?" : "Are you sure you want to delete these Keyframes?"
            );
        }

        private void DeleteKeyframes(params KeyValuePair<float, Keyframe>[] keyframes)
        {
            this.DeleteKeyframes((IEnumerable<KeyValuePair<float, Keyframe>>)keyframes);
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
                        this.RemoveInterpolable(pair.Value.parent);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError(_name + ": Couldn't delete keyframe with time \"" + pair.Key + "\" and value \"" + pair.Value + "\" from interpolable \"" + pair.Value.parent + "\"\n" + e);
                }
            }

            if (Input.GetKey(KeyCode.LeftAlt))
            {
                double duration = max - min + (this._blockLength / this._divisions);
                //IDK, grouping didn't work so I'm doing it like this
                HashSet<Interpolable> processedParents = new HashSet<Interpolable>();
                foreach (KeyValuePair<float, Keyframe> k in keyframes)
                {
                    if (processedParents.Contains(k.Value.parent) != false)
                        continue;
                    processedParents.Add(k.Value.parent);
                    foreach (KeyValuePair<float, Keyframe> pair in k.Value.parent.keyframes.ToList())
                        if (pair.Key > min)
                            this.MoveKeyframe(pair.Value, (float)(pair.Key - duration));
                }
            }
            this._selectedKeyframes.RemoveAll(elem => elem.Value == null || keyframes.Any(k => k.Value == elem.Value));

            this.UpdateGrid();
            this.UpdateKeyframeWindow(false);
        }

        private void SaveKeyframeTime(float time)
        {
            for (int i = 0; i < this._selectedKeyframes.Count; i++)
            {
                KeyValuePair<float, Keyframe> pair = this._selectedKeyframes[i];
                Keyframe potentialDuplicateKeyframe;
                if (pair.Value.parent.keyframes.TryGetValue(time, out potentialDuplicateKeyframe) && potentialDuplicateKeyframe != pair.Value)
                    continue;
                pair.Value.parent.keyframes.Remove(pair.Key);
                pair.Value.parent.keyframes.Add(time, pair.Value);
                this._selectedKeyframes[i] = new KeyValuePair<float, Keyframe>(time, pair.Value);
            }

            this.UpdateKeyframeTimeTextField();
            this.ExecuteDelayed(this.UpdateCursor2);
            this.UpdateGrid();
        }

        private void SaveKeyframeCurve()
        {
            AnimationCurve modifiedCurve = this._selectedKeyframes[0].Value.curve;
            foreach (KeyValuePair<float, Keyframe> pair in this._selectedKeyframes)
                pair.Value.curve = new AnimationCurve(modifiedCurve.keys);
        }

        private void UpdateKeyframeWindow(bool changeShowState = true)
        {
            if (this._selectedKeyframes.Count == 0)
            {
                this.CloseKeyframeWindow();
                return;
            }
            if (changeShowState)
                this.OpenKeyframeWindow();

            IEnumerable<IGrouping<Interpolable, KeyValuePair<float, Keyframe>>> interpolableGroups = this._selectedKeyframes.GroupBy(e => e.Value.parent);
            bool singleInterpolable = interpolableGroups.Count() == 1;
            Interpolable first = interpolableGroups.First().Key;
            this._keyframeInterpolableNameText.text = singleInterpolable ? (string.IsNullOrEmpty(first.alias) ? first.name : first.alias) : "Multiple selected";
            this._keyframeSelectPrevButton.interactable = this._selectedKeyframes.Count == 1;
            this._keyframeSelectNextButton.interactable = this._selectedKeyframes.Count == 1;
            this._keyframeTimeTextField.interactable = interpolableGroups.All(g => g.Count() == 1);
            this._keyframeUseCurrentTimeButton.interactable = this._keyframeTimeTextField.interactable;
            this._keyframeDeleteButtonText.text = this._selectedKeyframes.Count == 1 ? "Delete" : "Delete all";

            this.UpdateKeyframeTimeTextField();
            this.UpdateKeyframeValueText();
            this.ExecuteDelayed(this.UpdateCurve);
            this.ExecuteDelayed(this.UpdateCursor2);
        }

        private void UpdateKeyframeTimeTextField()
        {
            float t = this._selectedKeyframes[0].Key;
            foreach (KeyValuePair<float, Keyframe> pair in this._selectedKeyframes)
            {
                if (t != pair.Key)
                {
                    this._keyframeTimeTextField.text = "Multiple times";
                    return;
                }
            }
            this._keyframeTimeTextField.text = $"{Mathf.FloorToInt(t / 60):00}:{t % 60:00.########}";
        }

        private void UpdateKeyframeValueText()
        {
            object v = this._selectedKeyframes[0].Value.value;
            foreach (KeyValuePair<float, Keyframe> pair in this._selectedKeyframes)
            {
                if (v.Equals(pair.Value.value) == false)
                {
                    this._keyframeValueText.text = "Multiple values";
                    return;
                }
            }
            this._keyframeValueText.text = v != null ? v.ToString() : "null";
        }

        private void UpdateCurve()
        {
            if (this._selectedKeyframes.Count == 0)
                return;
            AnimationCurve curve = this._selectedKeyframes[0].Value.curve;
            foreach (KeyValuePair<float, Keyframe> pair in this._selectedKeyframes)
            {
                if (this.CompareCurves(curve, pair.Value.curve) == false)
                {
                    curve = null;
                    break;
                }
            }
            int length = 0;
            if (curve != null)
            {
                length = curve.length;
                for (int i = 0; i < this._curveTexture.width; i++)
                {
                    float v = curve.Evaluate(i / (this._curveTexture.width - 1f));
                    this._curveTexture.SetPixel(i, 0, new Color(v, v, v, v));
                }
            }
            else
            {
                for (int i = 0; i < this._curveTexture.width; i++)
                    this._curveTexture.SetPixel(i, 0, new Color(2f, 2f, 2f, 2f));
            }

            this._curveTexture.Apply(false);
            this._curveContainer.material.mainTexture = this._curveTexture;
            this._curveContainer.enabled = false;
            this._curveContainer.enabled = true;

            int displayIndex = 0;
            for (int i = 0; i < length; ++i)
            {
                UnityEngine.Keyframe curveKeyframe = curve[i];
                CurveKeyframeDisplay display;
                if (displayIndex < this._displayedCurveKeyframes.Count)
                    display = this._displayedCurveKeyframes[displayIndex];
                else
                {
                    display = new CurveKeyframeDisplay();
                    display.gameObject = GameObject.Instantiate(this._curveKeyframePrefab);
                    display.gameObject.hideFlags = HideFlags.None;
                    display.image = display.gameObject.transform.Find("RawImage").GetComponent<RawImage>();

                    display.gameObject.transform.SetParent(this._curveContainer.transform);
                    display.gameObject.transform.localScale = Vector3.one;
                    display.gameObject.transform.localPosition = Vector3.zero;

                    display.pointerDownHandler = display.gameObject.AddComponent<PointerDownHandler>();
                    display.scrollHandler = display.gameObject.AddComponent<ScrollHandler>();
                    display.dragHandler = display.gameObject.AddComponent<DragHandler>();
                    display.pointerEnterHandler = display.gameObject.AddComponent<PointerEnterHandler>();

                    this._displayedCurveKeyframes.Add(display);
                }

                int i1 = i;
                display.pointerDownHandler.onPointerDown = (e) =>
                {
                    if (e.button == PointerEventData.InputButton.Left)
                    {
                        this._selectedKeyframeCurvePointIndex = i1;
                        this.UpdateCurve();
                    }
                    if (i1 == 0 || i1 == curve.length - 1)
                        return;
                    if (e.button == PointerEventData.InputButton.Middle && Input.GetKey(KeyCode.LeftControl))
                    {
                        foreach (KeyValuePair<float, Keyframe> pair in this._selectedKeyframes)
                            pair.Value.curve.RemoveKey(i1);
                        this.UpdateCurve();
                    }
                };
                display.scrollHandler.onScroll = (e) =>
                {
                    UnityEngine.Keyframe k = curve[i1];
                    float offset = e.scrollDelta.y > 0 ? Mathf.PI / 180f : -Mathf.PI / 180f;
                    foreach (KeyValuePair<float, Keyframe> pair in this._selectedKeyframes)
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
                    foreach (KeyValuePair<float, Keyframe> pair in this._selectedKeyframes)
                        pair.Value.curve.AddKey(k);
                    this.UpdateCurve();
                };
                display.dragHandler.onDrag = (e) =>
                {
                    if (i1 == 0 || i1 == curve.length - 1)
                        return;
                    Vector2 localPoint;
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(this._curveContainer.rectTransform, e.position, e.pressEventCamera, out localPoint))
                    {
                        localPoint.x = Mathf.Clamp(localPoint.x, 0f, this._curveContainer.rectTransform.rect.width);
                        localPoint.y = Mathf.Clamp(localPoint.y, 0f, this._curveContainer.rectTransform.rect.height);
                        if (Input.GetKey(KeyCode.LeftShift))
                        {
                            Vector2 curveGridCellSize = new Vector2(this._curveContainer.rectTransform.rect.width * _curveGridCellSizePercent, this._curveContainer.rectTransform.rect.height * _curveGridCellSizePercent);
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
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(this._curveContainer.rectTransform, e.position, e.pressEventCamera, out Vector2 localPoint))
                    {

                        float time = localPoint.x / this._curveContainer.rectTransform.rect.width;
                        float value = localPoint.y / this._curveContainer.rectTransform.rect.height;
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
                            foreach (KeyValuePair<float, Keyframe> pair in this._selectedKeyframes)
                            {
                                pair.Value.curve.RemoveKey(i1);
                                pair.Value.curve.AddKey(curveKey);
                            }
                        }
                        this.UpdateCurve();
                    }
                };
                display.pointerEnterHandler.onPointerEnter = (e) =>
                {
                    this._tooltip.transform.parent.gameObject.SetActive(true);
                    UnityEngine.Keyframe k = curve[i1];
                    this._tooltip.text = $"T: {k.time:0.000}, V: {k.value:0.###}\nIn: {Mathf.Atan(k.inTangent) * Mathf.Rad2Deg:0.#}, Out:{Mathf.Atan(k.outTangent) * Mathf.Rad2Deg:0.#}";
                };
                display.pointerEnterHandler.onPointerExit = (e) => { this._tooltip.transform.parent.gameObject.SetActive(false); };

                display.image.color = i == this._selectedKeyframeCurvePointIndex ? Color.green : (Color)new Color32(44, 153, 160, 255);
                display.gameObject.SetActive(true);
                ((RectTransform)display.gameObject.transform).anchoredPosition = new Vector2(curveKeyframe.time * this._curveContainer.rectTransform.rect.width, curveKeyframe.value * this._curveContainer.rectTransform.rect.height);
                ++displayIndex;
            }
            for (; displayIndex < this._displayedCurveKeyframes.Count; ++displayIndex)
                this._displayedCurveKeyframes[displayIndex].gameObject.SetActive(false);

            this.UpdateCurvePointTime();
            this.UpdateCurvePointValue();
            this.UpdateCurvePointInTangent();
            this.UpdateCurvePointOutTangent();
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

#if KOIKATSU
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
            this.SceneLoad(path, node);
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
            this.SceneImport(path, node);
        }

        private void OnSceneSave(string path)
        {
            using (StringWriter stringWriter = new StringWriter())
            using (XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter))
            {

                xmlWriter.WriteStartElement("root");
                this.SceneWrite(path, xmlWriter);
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
            this.ExecuteDelayed(() =>
            {
                this._interpolables.Clear();
                this._interpolablesTree.Clear();
                this._selectedOCI = null;
                this._selectedKeyframes.Clear();

                List<KeyValuePair<int, ObjectCtrlInfo>> dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).ToList();
                this.SceneLoad(node, dic);

                this.UpdateInterpolablesView();
                this.CloseKeyframeWindow();
            }, 20);
        }

        private void SceneImport(string path, XmlNode node)
        {
            Dictionary<int, ObjectCtrlInfo> toIgnore = new Dictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl);
            this.ExecuteDelayed(() =>
            {
                List<KeyValuePair<int, ObjectCtrlInfo>> dic = Studio.Studio.Instance.dicObjectCtrl.Where(e => toIgnore.ContainsKey(e.Key) == false).OrderBy(e => SceneInfo_Import_Patches._newToOldKeys[e.Key]).ToList();
                this.SceneLoad(node, dic);

                this.UpdateInterpolablesView();
            }, 20);
        }

        private void SceneWrite(string path, XmlTextWriter writer)
        {
            List<KeyValuePair<int, ObjectCtrlInfo>> dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).ToList();
            writer.WriteAttributeString("duration", XmlConvert.ToString(this._duration));
            writer.WriteAttributeString("blockLength", XmlConvert.ToString(this._blockLength));
            writer.WriteAttributeString("divisions", XmlConvert.ToString(this._divisions));
            writer.WriteAttributeString("timeScale", XmlConvert.ToString(Time.timeScale));
            foreach (INode node in this._interpolablesTree.tree)
                this.WriteInterpolableTree(node, writer, dic);
        }

        private void SceneLoad(XmlNode node, List<KeyValuePair<int, ObjectCtrlInfo>> dic)
        {
            this.ReadInterpolableTree(node, dic);

            if (node.Attributes["duration"] != null)
                this._duration = XmlConvert.ToSingle(node.Attributes["duration"].Value);
            else
            {
                this._duration = 0f;
                foreach (KeyValuePair<int, Interpolable> pair in this._interpolables)
                {
                    KeyValuePair<float, Keyframe> last = pair.Value.keyframes.LastOrDefault();
                    if (this._duration < last.Key)
                        this._duration = last.Key;
                }
                if (Mathf.Approximately(this._duration, 0f))
                    this._duration = 10f;
            }
            this._blockLength = node.Attributes["blockLength"] != null ? XmlConvert.ToSingle(node.Attributes["blockLength"].Value) : 10f;
            this._divisions = node.Attributes["divisions"] != null ? XmlConvert.ToInt32(node.Attributes["divisions"].Value) : 10;
            Time.timeScale = node.Attributes["timeScale"] != null ? XmlConvert.ToSingle(node.Attributes["timeScale"].Value) : 1f;
            this._blockLengthInputField.text = this._blockLength.ToString();
            this._divisionsInputField.text = this._divisions.ToString();
            this._speedInputField.text = Time.timeScale.ToString("0.#####");
        }

        private void LoadSingle(string path)
        {
            List<KeyValuePair<int, ObjectCtrlInfo>> dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).ToList();
            XmlDocument document = new XmlDocument();
            try
            {
                document.Load(path);
                this.ReadInterpolableTree(document.FirstChild, dic, this._selectedOCI);

                OCIChar character = this._selectedOCI as OCIChar;
                if (character != null)
                {
                    character.LoadAnime(document.FirstChild.ReadInt("animationGroup"),
                            document.FirstChild.ReadInt("animationCategory"),
                            document.FirstChild.ReadInt("animationNo"));
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(_name + ": Could not load data for OCI.\n" + document.FirstChild + "\n" + e);
            }
            this.UpdateInterpolablesView();
        }

        private void SaveSingle(string path)
        {
            using (XmlTextWriter writer = new XmlTextWriter(path, Encoding.UTF8))
            {
                List<KeyValuePair<int, ObjectCtrlInfo>> dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).ToList();
                writer.WriteStartElement("root");

                OCIChar character = this._selectedOCI as OCIChar;
                ;
                if (character != null)
                {
                    OICharInfo.AnimeInfo info = character.oiCharInfo.animeInfo;
                    writer.WriteValue("animationCategory", info.category);
                    writer.WriteValue("animationGroup", info.group);
                    writer.WriteValue("animationNo", info.no);
                }

                foreach (INode node in this._interpolablesTree.tree)
                    this.WriteInterpolableTree(node, writer, dic, leafNode => leafNode.obj.oci == this._selectedOCI);
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
                        this.ReadInterpolable(interpolableNode, dic, overrideOci, group);
                        break;
                    case "interpolableGroup":
                        string groupName = interpolableNode.Attributes["name"].Value;
                        GroupNode<InterpolableGroup> newGroup = this._interpolablesTree.AddGroup(new InterpolableGroup { name = groupName }, group);
                        this.ReadInterpolableTree(interpolableNode, dic, overrideOci, newGroup);
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
                        this.WriteInterpolable(leafNode.obj, writer, dic);
                    break;
                case INodeType.Group:
                    GroupNode<InterpolableGroup> group = (GroupNode<InterpolableGroup>)interpolableNode;
                    bool shouldWriteGroup = true;
                    if (predicate != null)
                        shouldWriteGroup = this._interpolablesTree.Any(group, predicate);
                    if (shouldWriteGroup)
                    {
                        writer.WriteStartElement("interpolableGroup");
                        writer.WriteAttributeString("name", group.obj.name);

                        foreach (INode child in group.children)
                            this.WriteInterpolableTree(child, writer, dic, predicate);

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
                    InterpolableModel model = this._interpolableModelsList.Find(i => i.owner == ownerId && i.id == id);
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

                    if (this._interpolables.ContainsKey(interpolable.GetHashCode()) == false)
                    {
                        this._interpolables.Add(interpolable.GetHashCode(), interpolable);
                        this._interpolablesTree.AddLeaf(interpolable, group);
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
                UnityEngine.Debug.LogError(_name + ": Couldn't load interpolable with the following XML:\n" + interpolableNode.OuterXml + "\n" + e);
                if (added)
                    this.RemoveInterpolable(interpolable);
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
                        UnityEngine.Debug.LogError(_name + ": Couldn't save interpolable with the following value:\n" + interpolable + "\n" + e);
                        return;
                    }
                }
                writer.WriteRaw(stream.ToString());
            }
        }

        private void OnDuplicate(ObjectCtrlInfo source, ObjectCtrlInfo destination)
        {
            this.ExecuteDelayed(() =>
            {
                List<KeyValuePair<int, ObjectCtrlInfo>> dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).ToList();

                using (StringWriter stream = new StringWriter())
                {
                    using (XmlTextWriter writer = new XmlTextWriter(stream))
                    {
                        writer.WriteStartElement("root");

                        foreach (INode node in this._interpolablesTree.tree)
                            this.WriteInterpolableTree(node, writer, dic, leafNode => leafNode.obj.oci == source);

                        writer.WriteEndElement();
                    }

                    try
                    {
                        XmlDocument document = new XmlDocument();
                        document.LoadXml(stream.ToString());

                        this.ReadInterpolableTree(document.FirstChild, dic, destination);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError(_name + ": Could not duplicate data for OCI.\n" + stream + "\n" + e);
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
                        UnityEngine.Debug.LogWarning("Timeline: Could not patch OnDelete of type " + t.Name + "\n" + e);
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
