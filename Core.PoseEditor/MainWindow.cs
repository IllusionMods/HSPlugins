using HSPE.AMModules;
using RootMotion.FinalIK;
using Studio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using ToolBox;
using ToolBox.Extensions;
using UILib;
using UILib.EventHandlers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Vectrosity;
using Input = UnityEngine.Input;
using Path = System.IO.Path;
#if AISHOUJO || HONEYSELECT2
using AIChara;
using Illusion.Extensions;
#endif
#if IPA
using Harmony;
#elif BEPINEX
using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;
#endif
#if KOIKATSU || AISHOUJO || HONEYSELECT2
using ExtensibleSaveFormat;
using TMPro;
#endif

namespace HSPE
{
    public class MainWindow : MonoBehaviour
    {
        #region Public Static Variables
        internal static MainWindow _self;
        internal static int _lastLoadId = 0;
        internal static LoadType _lastLoadType = LoadType.None;
        #endregion

        #region Private Types
        private class FKBoneEntry
        {
            public Toggle toggle;
            public Text text;
            public GameObject target;
        }
        #endregion

        #region Constants
#if HONEYSELECT
        private const string _config = "configNEO.xml";
        internal const string _pluginDir = "Plugins\\HSPE\\";
        private const string _studioSavesDir = "StudioNEOScenes\\";
#elif PLAYHOME
        private const string _config = "config.xml";
        internal const string _pluginDir = "Plugins\\PHPE\\";
#elif KOIKATSU
        private const string _config = "config.xml";
        private const string _extSaveKey = "kkpe";
#elif AISHOUJO
        private const string _config = "config.xml";
        private const string _extSaveKey = "aipe";
#elif HONEYSELECT2
        private const string _config = "config.xml";
        private const string _extSaveKey = "hs2pe";
#endif
        #endregion

        #region Private Variables
        internal PoseController _poseTarget;
        internal CameraEventsDispatcher _cameraEventsDispatcher;
        internal const int _uniqueId = ('H' << 24) | ('S' << 16) | ('P' << 8) | 'E';
        private HashSet<TreeNodeObject> _selectedNodes;
        private TreeNodeObject _lastSelectedNode;
        private readonly List<FullBodyBipedEffector> _ikBoneTargets = new List<FullBodyBipedEffector>();
        private readonly Vector3[] _lastIKBonesPositions = new Vector3[14];
        private readonly Quaternion[] _lastIKBonesRotations = new Quaternion[14];
        private readonly List<FullBodyBipedChain> _ikBendGoalTargets = new List<FullBodyBipedChain>();
        private readonly Vector3[] _lastIKBendGoalsPositions = new Vector3[4];
        private int _lastObjectCount = 0;
        private int _lastIndex = 0;
        private Vector2 _delta;
        private bool _xMove;
        private bool _yMove;
        private bool _zMove;
        private bool _xRot;
        private bool _yRot;
        private bool _zRot;
        internal Rect _advancedModeRect = new Rect(Screen.width - 650, Screen.height - 370, 650, 370);
        private Canvas _ui;
        private GameObject _nothingText;
        private Transform _controls;
        private Slider _movementIntensity;
        private Text _intensityValueText;
        private RectTransform _optionsWindow;
        private float _intensityValue = 1f;
        private readonly Button[] _effectorsButtons = new Button[9];
        private readonly Button[] _bendGoalsButtons = new Button[4];
        private readonly Text[] _effectorsTexts = new Text[9];
        private readonly Text[] _bendGoalsTexts = new Text[4];
        private readonly Button[] _positionButtons = new Button[3];
        private readonly Button[] _rotationButtons = new Button[3];
        private Button _shortcutKeyButton;
        private bool _shortcutRegisterMode = false;
        private bool _positionOperationWorld = false;
        private Toggle _optimizeIKToggle;
        private Image _hspeButtonImage;
        private Toggle _crotchCorrectionToggle;
        private Toggle _leftFootCorrectionToggle;
        private Toggle _rightFootCorrectionToggle;
        private Toggle _crotchCorrectionByDefaultToggle;
        private Toggle _anklesCorrectionByDefaultToggle;
        private int _lastScreenWidth = Screen.width;
        private int _lastScreenHeight = Screen.height;
        private Button _copyLeftArmButton;
        private Button _copyRightArmButton;
        private Button _copyLeftHandButton;
        private Button _copyRightHandButton;
        private Button _copyLeftLegButton;
        private Button _copyRightLegButton;
        private Button _swapPostButton;
        private Transform _ikBonesButtons;
        private Transform _fkBonesButtons;
        private bool _currentModeIK = true;
        private ScrollRect _fkScrollRect;
        private GameObject _fkBoneTogglePrefab;
        private ToggleGroup _fkToggleGroup;
        private Quaternion _lastFKBonesRotation;
        private readonly List<FKBoneEntry> _fkBoneEntries = new List<FKBoneEntry>();
        private Dictionary<Transform, GuideObject> _dicGuideObject = new Dictionary<Transform, GuideObject>();
        private RectTransform _imguiBackground;
        #endregion

        #region Public Accessors
        public Texture2D vectorEndCap { get; private set; }
        public Texture2D vectorMiddle { get; private set; }
        public bool crotchCorrectionByDefault { get { return _crotchCorrectionByDefaultToggle.isOn; } }
        public bool anklesCorrectionByDefault { get { return _anklesCorrectionByDefaultToggle.isOn; } }
        #endregion

        public enum LoadType
        {
            None,
            Load,
            Import,
            External
        }

        #region Unity Methods
        protected virtual void Awake()
        {
            _self = this;

            if (Resources.FindObjectsOfTypeAll<IKExecutionOrder>().Length == 0)
                gameObject.AddComponent<IKExecutionOrder>().IKComponents = new IK[0];

            string path = Path.Combine(Path.Combine(Paths.ConfigPath, HSPE.Name), _config);
            if (File.Exists(path))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(path);

                foreach (XmlNode node in doc.FirstChild.ChildNodes)
                {
                    switch (node.Name)
                    {
                        case "advancedModeWindowSize":
                            if (node.Attributes["x"] != null && node.Attributes["y"] != null)
                            {
                                _advancedModeRect.xMin = _advancedModeRect.xMax - XmlConvert.ToInt32(node.Attributes["x"].Value);
                                _advancedModeRect.yMin = _advancedModeRect.yMax - XmlConvert.ToInt32(node.Attributes["y"].Value);
                            }
                            break;
                        case "femaleShortcuts":
                            foreach (XmlNode shortcut in node.ChildNodes)
                                if (shortcut.Attributes["path"] != null)
                                    BonesEditor._femaleShortcuts.Add(shortcut.Attributes["path"].Value, shortcut.Attributes["path"].Value.Split('/').Last());
                            break;
                        case "maleShortcuts":
                            foreach (XmlNode shortcut in node.ChildNodes)
                                if (shortcut.Attributes["path"] != null)
                                    BonesEditor._maleShortcuts.Add(shortcut.Attributes["path"].Value, shortcut.Attributes["path"].Value.Split('/').Last());
                            break;
                        case "itemShortcuts":
                            foreach (XmlNode shortcut in node.ChildNodes)
                                if (shortcut.Attributes["path"] != null)
                                    BonesEditor._itemShortcuts.Add(shortcut.Attributes["path"].Value, shortcut.Attributes["path"].Value.Split('/').Last());
                            break;
                        case "boneAliases":
                            foreach (XmlNode alias in node.ChildNodes)
                                if (alias.Attributes["key"] != null && alias.Attributes["value"] != null)
                                    BonesEditor._boneAliases.Add(alias.Attributes["key"].Value, alias.Attributes["value"].Value);
                            break;
                        case "blendShapeAliases":
                            foreach (XmlNode alias in node.ChildNodes)
                                if (alias.Attributes["key"] != null && alias.Attributes["value"] != null)
                                    BlendShapesEditor._blendShapeAliases.Add(alias.Attributes["key"].Value, alias.Attributes["value"].Value);
                            break;
                    }
                }
            }
            PoseController.InstallOnParentageEvent();
            OCIChar_ChangeChara_Patches.onChangeChara += OnCharacterReplaced;
            OCIChar_LoadClothesFile_Patches.onLoadClothesFile += OnLoadClothesFile;
#if HONEYSELECT
            OCIChar_SetCoordinateInfo_Patches.onSetCoordinateInfo += this.OnCoordinateReplaced;
#endif
            _dicGuideObject = (Dictionary<Transform, GuideObject>)GuideObjectManager.Instance.GetPrivate("dicGuideObject");
#if IPA
            HSExtSave.HSExtSave.RegisterHandler("hspe", null, null, this.OnSceneLoad, this.OnSceneImport, this.OnSceneSave, null, null);
#elif BEPINEX
            ExtendedSave.CardBeingLoaded += OnCharaLoad;
            ExtendedSave.CardBeingSaved += OnCharaSave;
            ExtendedSave.SceneBeingLoaded += OnSceneLoad;
            ExtendedSave.SceneBeingImported += OnSceneImport;
            ExtendedSave.SceneBeingSaved += OnSceneSave;
#endif
            this.ExecuteDelayed2(() =>
            {
                TimelineCompatibility.Init();
                BonesEditor.TimelineCompatibility.Populate();
                CollidersEditor.TimelineCompatibility.Populate();
                DynamicBonesEditor.TimelineCompatibility.Populate();
                BlendShapesEditor.TimelineCompatibility.Populate();
                BoobsEditor.TimelineCompatibility.Populate();
                IKEditor.TimelineCompatibility.Populate();
            }, 3);
        }

        private void Start()
        {
            _cameraEventsDispatcher = Camera.main.gameObject.AddComponent<CameraEventsDispatcher>();
            SpawnGUI();
            _crotchCorrectionByDefaultToggle.isOn = HSPE.ConfigCrotchCorrectionByDefault.Value;
            _anklesCorrectionByDefaultToggle.isOn = HSPE.ConfigAnklesCorrectionByDefault.Value;
            _selectedNodes = (HashSet<TreeNodeObject>)Studio.Studio.Instance.treeNodeCtrl.GetPrivate("hashSelectNode");

            BoneReorganizer.Init(_fkScrollRect);
        }

        protected virtual void Update()
        {
            if (_lastScreenWidth != Screen.width || _lastScreenHeight != Screen.height)
                OnWindowResize();
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;

            int objectCount = Studio.Studio.Instance.dicObjectCtrl.Count;
            if (objectCount != _lastObjectCount)
            {
                if (objectCount > _lastObjectCount)
                    OnObjectAdded();
                _lastIndex = Studio.Studio.Instance.sceneInfo.CheckNewIndex();
            }
            _lastObjectCount = Studio.Studio.Instance.dicObjectCtrl.Count;


            PoseController last = _poseTarget;
            TreeNodeObject treeNodeObject = _selectedNodes.FirstOrDefault();
            if (_lastSelectedNode != treeNodeObject)
            {
                if (treeNodeObject != null)
                {
                    ObjectCtrlInfo info;
                    if (Studio.Studio.Instance.dicInfo.TryGetValue(treeNodeObject, out info))
                        _poseTarget = info.guideObject.transformTarget.GetComponent<PoseController>();
                }
                else
                    _poseTarget = null;
            }
            _lastSelectedNode = treeNodeObject;

            if (last != _poseTarget)
                OnTargetChange(last);
            GUILogic();

            StaticUpdate();
        }

        // "Static" Update stuff for the other classes
        private void StaticUpdate()
        {
            if (AdvancedModeModule._repeatCalled)
                AdvancedModeModule._repeatTimer += Time.unscaledDeltaTime;
            else
                AdvancedModeModule._repeatTimer = 0f;
            AdvancedModeModule._repeatCalled = false;

            BoneReorganizer.Update();
        }

        protected virtual void OnGUI()
        {
            if (_poseTarget != null && PoseController._drawAdvancedMode)
            {
                _imguiBackground.gameObject.SetActive(true);
                IMGUIExtensions.DrawBackground(_advancedModeRect);
                _advancedModeRect = GUILayout.Window(_uniqueId, _advancedModeRect, _poseTarget.AdvancedModeWindow, "Advanced mode");
                IMGUIExtensions.FitRectTransformToRect(_imguiBackground, _advancedModeRect);
            }
            else if (_imguiBackground != null)
            {
                _imguiBackground.gameObject.SetActive(false);

                if (_poseTarget != null && _poseTarget._blendShapesEditor != null)
                {
                    _poseTarget._blendShapesEditor.DisableSubWindow();
                }
            }
        }

        protected virtual void OnDestroy()
        {
            string folder = Path.Combine(Paths.ConfigPath, HSPE.Name);
            if (Directory.Exists(folder) == false)
                Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, _config);
            using (FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                using (XmlTextWriter xmlWriter = new XmlTextWriter(fileStream, Encoding.UTF8))
                {
                    xmlWriter.Formatting = Formatting.Indented;
                    xmlWriter.WriteStartElement("root");
                    xmlWriter.WriteAttributeString("version", HSPE.Version.ToString());

                    xmlWriter.WriteStartElement("advancedModeWindowSize");
                    xmlWriter.WriteAttributeString("x", XmlConvert.ToString((int)_advancedModeRect.width));
                    xmlWriter.WriteAttributeString("y", XmlConvert.ToString((int)_advancedModeRect.height));
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteStartElement("femaleShortcuts");
                    foreach (KeyValuePair<string, string> kvp in BonesEditor._femaleShortcuts)
                    {
                        xmlWriter.WriteStartElement("shortcut");
                        xmlWriter.WriteAttributeString("path", kvp.Key);
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteStartElement("maleShortcuts");
                    foreach (KeyValuePair<string, string> kvp in BonesEditor._maleShortcuts)
                    {
                        xmlWriter.WriteStartElement("shortcut");
                        xmlWriter.WriteAttributeString("path", kvp.Key);
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteStartElement("itemShortcuts");
                    foreach (KeyValuePair<string, string> kvp in BonesEditor._itemShortcuts)
                    {
                        xmlWriter.WriteStartElement("shortcut");
                        xmlWriter.WriteAttributeString("path", kvp.Key);
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteStartElement("boneAliases");
                    foreach (KeyValuePair<string, string> kvp in BonesEditor._boneAliases)
                    {
                        xmlWriter.WriteStartElement("alias");
                        xmlWriter.WriteAttributeString("key", kvp.Key);
                        xmlWriter.WriteAttributeString("value", kvp.Value);
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteStartElement("blendShapeAliases");
                    foreach (KeyValuePair<string, string> kvp in BlendShapesEditor._blendShapeAliases)
                    {
                        xmlWriter.WriteStartElement("alias");
                        xmlWriter.WriteAttributeString("key", kvp.Key);
                        xmlWriter.WriteAttributeString("value", kvp.Value);
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteEndElement();
                }
            }
        }
        #endregion

        #region GUI
        private void SpawnGUI()
        {
            UIUtility.Init();

#if HONEYSELECT || PLAYHOME
            AssetBundle bundle = AssetBundle.LoadFromMemory(Assembly.GetExecutingAssembly().GetResource("HSPE.Resources.HSPEResources.unity3d"));
#elif KOIKATSU
            AssetBundle bundle = AssetBundle.LoadFromMemory(Assembly.GetExecutingAssembly().GetResource("HSPE.Resources.KKPEResources.unity3d"));
#elif AISHOUJO
            AssetBundle bundle = AssetBundle.LoadFromMemory(Assembly.GetExecutingAssembly().GetResource("HSPE.Resources.AIPEResources.unity3d"));
#elif HONEYSELECT2
            AssetBundle bundle = AssetBundle.LoadFromMemory(Assembly.GetExecutingAssembly().GetResource("HSPE.Resources.HS2PEResources.unity3d"));
#endif
            Texture2D texture = bundle.LoadAsset<Texture2D>("Icon");
            vectorEndCap = bundle.LoadAsset<Texture2D>("VectorEndCap");
            vectorMiddle = bundle.LoadAsset<Texture2D>("VectorMiddle");
            VectorLine.SetEndCap("vector", EndCap.Back, 0f, -1f, 1f, 4f, vectorMiddle, vectorEndCap);
            VectorLine.canvas.sortingOrder -= 40;

            {
                IEnumerable<Transform> children = GameObject.Find("StudioScene/Canvas System Menu/01_Button").transform.Children();
                Vector2 anchoredPosition = new Vector2(0f, float.MaxValue);
                foreach (Transform child in children)
                {
                    RectTransform rChild = (RectTransform)child;
                    if (rChild.anchoredPosition.x >= anchoredPosition.x && rChild.anchoredPosition.y <= anchoredPosition.y)
                        anchoredPosition = rChild.anchoredPosition;
                }

                while (children.FirstOrDefault(c => (((RectTransform)c).anchoredPosition - anchoredPosition).sqrMagnitude < 4f) != null)
                    anchoredPosition.y += 40f;

                RectTransform original = (RectTransform)children.First();
                Button hspeButton = Instantiate(original.gameObject).GetComponent<Button>();
                hspeButton.name = "Button HSPE";
                hspeButton.interactable = true;
                hspeButton.transform.SetParent(original.parent, true);
                hspeButton.transform.localScale = original.localScale;
                ((RectTransform)hspeButton.transform).anchoredPosition = anchoredPosition;
                hspeButton.onClick = new Button.ButtonClickedEvent();
                hspeButton.onClick.AddListener(() =>
                {
                    _ui.gameObject.SetActive(!_ui.gameObject.activeSelf);
                    _hspeButtonImage.color = _ui.gameObject.activeSelf ? Color.green : Color.white;
                });
                hspeButton.gameObject.AddComponent<PointerDownHandler>().onPointerDown += eventData =>
                {
                    if (eventData.button == PointerEventData.InputButton.Right && _poseTarget != null)
                        _poseTarget.ToggleAdvancedMode();
                };
                _hspeButtonImage = hspeButton.targetGraphic as Image;
                _hspeButtonImage.sprite = Sprite.Create(texture, new Rect(0f, 0f, 32, 32), new Vector2(16, 16));
                _hspeButtonImage.color = Color.white;
            }

            GameObject uiPrefab;
#if HONEYSELECT || PLAYHOME
            uiPrefab = bundle.LoadAsset<GameObject>("HSPECanvas");
#elif KOIKATSU
            uiPrefab = bundle.LoadAsset<GameObject>("KKPECanvas");
#elif AISHOUJO
            uiPrefab = bundle.LoadAsset<GameObject>("AIPECanvas");
#elif HONEYSELECT2
            uiPrefab = bundle.LoadAsset<GameObject>("HS2PECanvas");
#endif
            _ui = Instantiate(uiPrefab).GetComponent<Canvas>();
            uiPrefab.hideFlags |= HideFlags.HideInHierarchy;
            _fkBoneTogglePrefab = bundle.LoadAsset<GameObject>("FKBoneTogglePrefab");
            _fkBoneTogglePrefab.hideFlags = HideFlags.HideInHierarchy;
            bundle.Unload(false);

            RectTransform bg = (RectTransform)_ui.transform.Find("BG");
            Transform topContainer = bg.Find("Top Container");
            UIUtility.MakeObjectDraggable((RectTransform)topContainer, bg);

            Toggle ikToggle = _ui.transform.Find("BG/Top Container/Buttons/IK").GetComponent<Toggle>();
            ikToggle.onValueChanged.AddListener((b) =>
            {
                _ikBonesButtons.gameObject.SetActive(ikToggle.isOn);
                _fkBonesButtons.gameObject.SetActive(!ikToggle.isOn);
                _currentModeIK = ikToggle.isOn;
            });
            Toggle fkToggle = _ui.transform.Find("BG/Top Container/Buttons/FK").GetComponent<Toggle>();
            fkToggle.onValueChanged.AddListener((b) =>
            {
                _fkBonesButtons.gameObject.SetActive(fkToggle.isOn);
                _ikBonesButtons.gameObject.SetActive(!fkToggle.isOn);
                _currentModeIK = !fkToggle.isOn;
            });

            _nothingText = _ui.transform.Find("BG/Nothing Text").gameObject;
            _controls = _ui.transform.Find("BG/Controls");
            _ikBonesButtons = _ui.transform.Find("BG/Controls/IK Bones Buttons");
            _fkBonesButtons = _ui.transform.Find("BG/Controls/FK Bones Buttons");

            Button rightShoulder = _ui.transform.Find("BG/Controls/IK Bones Buttons/Right Shoulder Button").GetComponent<Button>();
            rightShoulder.onClick.AddListener(() => SetBoneTarget(FullBodyBipedEffector.RightShoulder));
            Text t = rightShoulder.GetComponentInChildren<Text>();
            _effectorsButtons[(int)FullBodyBipedEffector.RightShoulder] = rightShoulder;
            _effectorsTexts[(int)FullBodyBipedEffector.RightShoulder] = t;

            Button leftShoulder = _ui.transform.Find("BG/Controls/IK Bones Buttons/Left Shoulder Button").GetComponent<Button>();
            leftShoulder.onClick.AddListener(() => SetBoneTarget(FullBodyBipedEffector.LeftShoulder));
            t = leftShoulder.GetComponentInChildren<Text>();
            _effectorsButtons[(int)FullBodyBipedEffector.LeftShoulder] = leftShoulder;
            _effectorsTexts[(int)FullBodyBipedEffector.LeftShoulder] = t;

            Button rightArmBendGoal = _ui.transform.Find("BG/Controls/IK Bones Buttons/Right Arm Bend Goal Button").GetComponent<Button>();
            rightArmBendGoal.onClick.AddListener(() => SetBendGoalTarget(FullBodyBipedChain.RightArm));
            t = rightArmBendGoal.GetComponentInChildren<Text>();
            _bendGoalsButtons[(int)FullBodyBipedChain.RightArm] = rightArmBendGoal;
            _bendGoalsTexts[(int)FullBodyBipedChain.RightArm] = t;

            Button leftArmBendGoal = _ui.transform.Find("BG/Controls/IK Bones Buttons/Left Arm Bend Goal Button").GetComponent<Button>();
            leftArmBendGoal.onClick.AddListener(() => SetBendGoalTarget(FullBodyBipedChain.LeftArm));
            t = leftArmBendGoal.GetComponentInChildren<Text>();
            _bendGoalsButtons[(int)FullBodyBipedChain.LeftArm] = leftArmBendGoal;
            _bendGoalsTexts[(int)FullBodyBipedChain.LeftArm] = t;

            Button rightHand = _ui.transform.Find("BG/Controls/IK Bones Buttons/Right Hand Button").GetComponent<Button>();
            rightHand.onClick.AddListener(() => SetBoneTarget(FullBodyBipedEffector.RightHand));
            t = rightHand.GetComponentInChildren<Text>();
            _effectorsButtons[(int)FullBodyBipedEffector.RightHand] = rightHand;
            _effectorsTexts[(int)FullBodyBipedEffector.RightHand] = t;

            Button leftHand = _ui.transform.Find("BG/Controls/IK Bones Buttons/Left Hand Button").GetComponent<Button>();
            leftHand.onClick.AddListener(() => SetBoneTarget(FullBodyBipedEffector.LeftHand));
            t = leftHand.GetComponentInChildren<Text>();
            _effectorsButtons[(int)FullBodyBipedEffector.LeftHand] = leftHand;
            _effectorsTexts[(int)FullBodyBipedEffector.LeftHand] = t;

            Button body = _ui.transform.Find("BG/Controls/IK Bones Buttons/Body Button").GetComponent<Button>();
            body.onClick.AddListener(() => SetBoneTarget(FullBodyBipedEffector.Body));
            t = body.GetComponentInChildren<Text>();
            _effectorsButtons[(int)FullBodyBipedEffector.Body] = body;
            _effectorsTexts[(int)FullBodyBipedEffector.Body] = t;

            Button rightThigh = _ui.transform.Find("BG/Controls/IK Bones Buttons/Right Thigh Button").GetComponent<Button>();
            rightThigh.onClick.AddListener(() => SetBoneTarget(FullBodyBipedEffector.RightThigh));
            t = rightThigh.GetComponentInChildren<Text>();
            _effectorsButtons[(int)FullBodyBipedEffector.RightThigh] = rightThigh;
            _effectorsTexts[(int)FullBodyBipedEffector.RightThigh] = t;

            Button leftThigh = _ui.transform.Find("BG/Controls/IK Bones Buttons/Left Thigh Button").GetComponent<Button>();
            leftThigh.onClick.AddListener(() => SetBoneTarget(FullBodyBipedEffector.LeftThigh));
            t = leftThigh.GetComponentInChildren<Text>();
            _effectorsButtons[(int)FullBodyBipedEffector.LeftThigh] = leftThigh;
            _effectorsTexts[(int)FullBodyBipedEffector.LeftThigh] = t;

            Button rightLegBendGoal = _ui.transform.Find("BG/Controls/IK Bones Buttons/Right Leg Bend Goal Button").GetComponent<Button>();
            rightLegBendGoal.onClick.AddListener(() => SetBendGoalTarget(FullBodyBipedChain.RightLeg));
            t = rightLegBendGoal.GetComponentInChildren<Text>();
            _bendGoalsButtons[(int)FullBodyBipedChain.RightLeg] = rightLegBendGoal;
            _bendGoalsTexts[(int)FullBodyBipedChain.RightLeg] = t;

            Button leftLegBendGoal = _ui.transform.Find("BG/Controls/IK Bones Buttons/Left Leg Bend Goal Button").GetComponent<Button>();
            leftLegBendGoal.onClick.AddListener(() => SetBendGoalTarget(FullBodyBipedChain.LeftLeg));
            t = leftLegBendGoal.GetComponentInChildren<Text>();
            _bendGoalsButtons[(int)FullBodyBipedChain.LeftLeg] = leftLegBendGoal;
            _bendGoalsTexts[(int)FullBodyBipedChain.LeftLeg] = t;

            Button rightFoot = _ui.transform.Find("BG/Controls/IK Bones Buttons/Right Foot Button").GetComponent<Button>();
            rightFoot.onClick.AddListener(() => SetBoneTarget(FullBodyBipedEffector.RightFoot));
            t = rightFoot.GetComponentInChildren<Text>();
            _effectorsButtons[(int)FullBodyBipedEffector.RightFoot] = rightFoot;
            _effectorsTexts[(int)FullBodyBipedEffector.RightFoot] = t;

            Button leftFoot = _ui.transform.Find("BG/Controls/IK Bones Buttons/Left Foot Button").GetComponent<Button>();
            leftFoot.onClick.AddListener(() => SetBoneTarget(FullBodyBipedEffector.LeftFoot));
            t = leftFoot.GetComponentInChildren<Text>();
            _effectorsButtons[(int)FullBodyBipedEffector.LeftFoot] = leftFoot;
            _effectorsTexts[(int)FullBodyBipedEffector.LeftFoot] = t;

            _fkScrollRect = _fkBonesButtons.GetComponentInChildren<ScrollRect>();
            _fkScrollRect.movementType = ScrollRect.MovementType.Clamped;
            _fkToggleGroup = _fkBonesButtons.GetComponentInChildren<ToggleGroup>();

            Button xMoveButton = _ui.transform.Find("BG/Controls/Buttons/MoveRotateButtons/X Move Button").GetComponent<Button>();
            xMoveButton.onClick.AddListener(() => EventSystem.current.SetSelectedGameObject(null));
            xMoveButton.gameObject.AddComponent<PointerDownHandler>().onPointerDown += (eventData) =>
            {
                if (eventData.button == PointerEventData.InputButton.Left)
                    _xMove = true;
            };
            _positionButtons[0] = xMoveButton;

            Button yMoveButton = _ui.transform.Find("BG/Controls/Buttons/MoveRotateButtons/Y Move Button").GetComponent<Button>();
            yMoveButton.onClick.AddListener(() => EventSystem.current.SetSelectedGameObject(null));
            yMoveButton.gameObject.AddComponent<PointerDownHandler>().onPointerDown += (eventData) =>
            {
                if (eventData.button == PointerEventData.InputButton.Left)
                    _yMove = true;
            };
            _positionButtons[1] = yMoveButton;

            Button zMoveButton = _ui.transform.Find("BG/Controls/Buttons/MoveRotateButtons/Z Move Button").GetComponent<Button>();
            zMoveButton.onClick.AddListener(() => EventSystem.current.SetSelectedGameObject(null));
            zMoveButton.gameObject.AddComponent<PointerDownHandler>().onPointerDown += (eventData) =>
            {
                if (eventData.button == PointerEventData.InputButton.Left)
                    _zMove = true;
            };
            _positionButtons[2] = zMoveButton;

            Button rotXButton = _ui.transform.Find("BG/Controls/Buttons/MoveRotateButtons/Rot X Button").GetComponent<Button>();
            rotXButton.onClick.AddListener(() => EventSystem.current.SetSelectedGameObject(null));
            rotXButton.gameObject.AddComponent<PointerDownHandler>().onPointerDown += (eventData) =>
            {
                if (eventData.button == PointerEventData.InputButton.Left)
                    _xRot = true;
            };
            _rotationButtons[0] = rotXButton;

            Button rotYButton = _ui.transform.Find("BG/Controls/Buttons/MoveRotateButtons/Rot Y Button").GetComponent<Button>();
            rotYButton.onClick.AddListener(() => EventSystem.current.SetSelectedGameObject(null));
            rotYButton.gameObject.AddComponent<PointerDownHandler>().onPointerDown += (eventData) =>
            {
                if (eventData.button == PointerEventData.InputButton.Left)
                    _yRot = true;
            };
            _rotationButtons[1] = rotYButton;

            Button rotZButton = _ui.transform.Find("BG/Controls/Buttons/MoveRotateButtons/Rot Z Button").GetComponent<Button>();
            rotZButton.onClick.AddListener(() => EventSystem.current.SetSelectedGameObject(null));
            rotZButton.gameObject.AddComponent<PointerDownHandler>().onPointerDown += (eventData) =>
            {
                if (eventData.button == PointerEventData.InputButton.Left)
                    _zRot = true;
            };
            _rotationButtons[2] = rotZButton;

            _copyLeftArmButton = _ui.transform.Find("BG/Controls/Other buttons/Copy Limbs/Copy Right Arm Button").GetComponent<Button>();
            _copyLeftArmButton.onClick.AddListener(() =>
            {
                if (_poseTarget != null)
                    ((CharaPoseController)_poseTarget).CopyLimbToTwin(FullBodyBipedChain.RightArm, OIBoneInfo.BoneGroup.RightArm);
            });

            _copyRightArmButton = _ui.transform.Find("BG/Controls/Other buttons/Copy Limbs/Copy Left Arm Button").GetComponent<Button>();
            _copyRightArmButton.onClick.AddListener(() =>
            {
                if (_poseTarget != null)
                    ((CharaPoseController)_poseTarget).CopyLimbToTwin(FullBodyBipedChain.LeftArm, OIBoneInfo.BoneGroup.LeftArm);
            });

            _copyLeftHandButton = _ui.transform.Find("BG/Controls/Other buttons/Copy Limbs/Copy Right Hand Button").GetComponent<Button>();
            _copyLeftHandButton.onClick.AddListener(() =>
            {
                if (_poseTarget != null)
                    ((CharaPoseController)_poseTarget).CopyHandToTwin(OIBoneInfo.BoneGroup.RightHand);
            });

            _copyRightHandButton = _ui.transform.Find("BG/Controls/Other buttons/Copy Limbs/Copy Left Hand Button").GetComponent<Button>();
            _copyRightHandButton.onClick.AddListener(() =>
            {
                if (_poseTarget != null)
                    ((CharaPoseController)_poseTarget).CopyHandToTwin(OIBoneInfo.BoneGroup.LeftHand);
            });

            _copyLeftLegButton = _ui.transform.Find("BG/Controls/Other buttons/Copy Limbs/Copy Right Leg Button").GetComponent<Button>();
            _copyLeftLegButton.onClick.AddListener(() =>
            {
                if (_poseTarget != null)
                    ((CharaPoseController)_poseTarget).CopyLimbToTwin(FullBodyBipedChain.RightLeg, OIBoneInfo.BoneGroup.RightLeg);
            });

            _copyRightLegButton = _ui.transform.Find("BG/Controls/Other buttons/Copy Limbs/Copy Left LegButton").GetComponent<Button>();
            _copyRightLegButton.onClick.AddListener(() =>
            {
                if (_poseTarget != null)
                    ((CharaPoseController)_poseTarget).CopyLimbToTwin(FullBodyBipedChain.LeftLeg, OIBoneInfo.BoneGroup.LeftLeg);
            });

            _swapPostButton = _ui.transform.Find("BG/Controls/Other buttons/Other/Swap Pose Button").GetComponent<Button>();
            _swapPostButton.onClick.AddListener(() =>
            {
                if (_poseTarget != null)
                    ((CharaPoseController)_poseTarget).SwapPose();
            });

            Button advancedModeButton = _ui.transform.Find("BG/Controls/Buttons/Simple Options/Advanced Mode Button").GetComponent<Button>();
            advancedModeButton.onClick.AddListener(() =>
            {
                if (_poseTarget != null)
                    _poseTarget.ToggleAdvancedMode();
            });

            _movementIntensity = _ui.transform.Find("BG/Controls/Buttons/Simple Options/Intensity Container/Movement Intensity Slider").GetComponent<Slider>();
            _movementIntensity.onValueChanged.AddListener(value =>
            {
                value = _movementIntensity.value;
                value -= 7;
                _intensityValue = Mathf.Pow(2, value);
                _intensityValueText.text = _intensityValue >= 1f ? "x" + _intensityValue.ToString("0.##") : "/" + (1f / _intensityValue).ToString("0.##");
            });

            _intensityValueText = _ui.transform.Find("BG/Controls/Buttons/Simple Options/Intensity Container/Movement Intensity Value").GetComponent<Text>();

            Button positionOp = _ui.transform.Find("BG/Controls/Buttons/Simple Options/Pos Operation Container/Position Operation").GetComponent<Button>();
            Text buttonText = positionOp.GetComponentInChildren<Text>();
            positionOp.onClick.AddListener(() =>
            {
                _positionOperationWorld = !_positionOperationWorld;
                buttonText.text = _positionOperationWorld ? "World" : "Local";
            });

            _optimizeIKToggle = _ui.transform.Find("BG/Controls/Buttons/Simple Options/Optimize IK Container/Optimize IK").GetComponent<Toggle>();
            _optimizeIKToggle.onValueChanged.AddListener((b) =>
            {
                if (_poseTarget != null)
                    ((CharaPoseController)_poseTarget).optimizeIK = _optimizeIKToggle.isOn;
            });

            Button optionsButton = _ui.transform.Find("BG/Controls/Buttons/Simple Options/Options Button").GetComponent<Button>();
            optionsButton.onClick.AddListener(() =>
            {
                if (_shortcutRegisterMode)
                    _shortcutKeyButton.onClick.Invoke();
                _optionsWindow.gameObject.SetActive(!_optionsWindow.gameObject.activeSelf);
            });
            _ui.gameObject.SetActive(false);

            SetBoneTarget(FullBodyBipedEffector.Body);
            //this.OnTargetChange(null);

            _optionsWindow = (RectTransform)_ui.transform.Find("Options Window");

            topContainer = _optionsWindow.Find("Top Container");
            UIUtility.MakeObjectDraggable(topContainer as RectTransform, _optionsWindow);

            Vector2 sizeDelta = bg.sizeDelta;
#if HONEYSELECT
            Text xMoveText = xMoveButton.GetComponentInChildren<Text>();
            Text yMoveText = yMoveButton.GetComponentInChildren<Text>();
            Text zMoveText = zMoveButton.GetComponentInChildren<Text>();
            Text xRotText = rotXButton.GetComponentInChildren<Text>();
            Text yRotText = rotYButton.GetComponentInChildren<Text>();
            Text zRotText = rotZButton.GetComponentInChildren<Text>();
            int moveFontSize = xMoveText.fontSize;
            int rotFontSize = xRotText.fontSize;
#endif

            Button normalButton = _ui.transform.Find("Options Window/Options/Main Window Size Container/Normal Button").GetComponent<Button>();
            normalButton.onClick.AddListener(() =>
            {
                HSPE.ConfigMainWindowSize.Value = 1f;
                bg.sizeDelta = sizeDelta * HSPE.ConfigMainWindowSize.Value;
#if HONEYSELECT
                xMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
                yMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
                zMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
                xRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
                yRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
                zRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
#endif
            });

            Button largeButton = _ui.transform.Find("Options Window/Options/Main Window Size Container/Large Button").GetComponent<Button>();
            largeButton.onClick.AddListener(() =>
            {
                HSPE.ConfigMainWindowSize.Value = 1.25f;
                bg.sizeDelta = sizeDelta * HSPE.ConfigMainWindowSize.Value;
#if HONEYSELECT
                xMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
                yMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
                zMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
                xRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
                yRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
                zRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
#endif
            });

            Button veryLargeButton = _ui.transform.Find("Options Window/Options/Main Window Size Container/Very Large Button").GetComponent<Button>();
            veryLargeButton.onClick.AddListener(() =>
            {
                HSPE.ConfigMainWindowSize.Value = 1.5f;
                bg.sizeDelta = sizeDelta * HSPE.ConfigMainWindowSize.Value;
#if HONEYSELECT
                xMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
                yMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
                zMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
                xRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
                yRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
                zRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
#endif
            });
            bg.sizeDelta = sizeDelta * HSPE.ConfigMainWindowSize.Value;
#if HONEYSELECT
            xMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
            yMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
            zMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
            xRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
            yRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
            zRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
#endif
            _optionsWindow.anchoredPosition += new Vector2((sizeDelta.x * HSPE.ConfigMainWindowSize.Value) - sizeDelta.x, 0f);

            Slider xSlider = _ui.transform.Find("Options Window/Options/Advanced Mode Window Size Container/Width Slider").GetComponent<Slider>();
            xSlider.onValueChanged.AddListener((f) =>
            {
                _advancedModeRect.xMin = _advancedModeRect.xMax - xSlider.value;
            });
            xSlider.maxValue = Screen.width * 0.8f;
            xSlider.value = _advancedModeRect.width;

            Slider ySlider = _ui.transform.Find("Options Window/Options/Advanced Mode Window Size Container/Height Slider").GetComponent<Slider>();
            ySlider.onValueChanged.AddListener((f) =>
            {
                _advancedModeRect.yMin = _advancedModeRect.yMax - ySlider.value;
            });
            ySlider.maxValue = Screen.height * 0.8f;
            ySlider.value = _advancedModeRect.height;

            _shortcutKeyButton = _ui.transform.Find("Options Window/Options/Shortcut Key Container/Listener Button").GetComponent<Button>();
            Text text = _shortcutKeyButton.GetComponentInChildren<Text>();
            _shortcutKeyButton.onClick.AddListener(() =>
            {
                _shortcutRegisterMode = !_shortcutRegisterMode;
                text.text = _shortcutRegisterMode ? "Press a Key" : HSPE.ConfigMainWindowShortcut.Value.ToString();
            });

            _crotchCorrectionByDefaultToggle = _ui.transform.Find("Options Window/Options/Joint Correction Container/Crotch/Toggle").GetComponent<Toggle>();
            _anklesCorrectionByDefaultToggle = _ui.transform.Find("Options Window/Options/Joint Correction Container/Ankles/Toggle").GetComponent<Toggle>();

            _crotchCorrectionByDefaultToggle.onValueChanged.AddListener(x => HSPE.ConfigCrotchCorrectionByDefault.Value = x);
            _anklesCorrectionByDefaultToggle.onValueChanged.AddListener(x => HSPE.ConfigAnklesCorrectionByDefault.Value = x);

            _optionsWindow.gameObject.SetActive(false);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_ui.transform.GetChild(0).transform as RectTransform);

            // Additional UI
#if HONEYSELECT || PLAYHOME
            {
                RectTransform parent = GameObject.Find("StudioScene").transform.Find("Canvas Main Menu/02_Manipulate/00_Chara/06_Joint") as RectTransform;
                RawImage container = UIUtility.CreateRawImage("Additional Container", parent);
                container.rectTransform.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, -60f), Vector2.zero);
                container.color = new Color32(105, 108, 111, 255);

                GameObject textPrefab = parent.Find("Text Left Leg").gameObject;
                Text crotchText = Instantiate(textPrefab).GetComponent<Text>();
                crotchText.rectTransform.SetParent(parent);
                crotchText.rectTransform.localPosition = Vector3.zero;
                crotchText.rectTransform.SetRect(textPrefab.transform);
                crotchText.rectTransform.localScale = textPrefab.transform.localScale;
                crotchText.rectTransform.SetParent(container.rectTransform);
                crotchText.rectTransform.offsetMin += new Vector2(0f, -20f);
                crotchText.rectTransform.offsetMax += new Vector2(0f, -20f);
                crotchText.text = " Crotch";
                GameObject togglePrefab = parent.Find("Toggle Left Leg").gameObject;
                this._crotchCorrectionToggle = Instantiate(togglePrefab).GetComponent<Toggle>();
                RectTransform rt = this._crotchCorrectionToggle.transform as RectTransform;
                rt.SetParent(parent);
                rt.localPosition = Vector3.zero;
                rt.SetRect(togglePrefab.transform);
                rt.localScale = togglePrefab.transform.localScale;
                rt.SetParent(container.rectTransform);
                rt.offsetMin += new Vector2(0f, -20f);
                rt.offsetMax += new Vector2(0f, -20f);
                this._crotchCorrectionToggle.onValueChanged = new Toggle.ToggleEvent();
                this._crotchCorrectionToggle.onValueChanged.AddListener((b) =>
                {
                    if (this._poseTarget != null)
                        ((CharaPoseController)this._poseTarget).crotchJointCorrection = this._crotchCorrectionToggle.isOn;
                });

                Text leftFootText = Instantiate(textPrefab).GetComponent<Text>();
                leftFootText.rectTransform.SetParent(parent);
                leftFootText.rectTransform.localPosition = Vector3.zero;
                leftFootText.rectTransform.SetRect(textPrefab.transform);
                leftFootText.rectTransform.localScale = textPrefab.transform.localScale;
                leftFootText.rectTransform.SetParent(container.rectTransform);
                leftFootText.rectTransform.offsetMin += new Vector2(0f, -40f);
                leftFootText.rectTransform.offsetMax += new Vector2(0f, -40f);
                leftFootText.text = " Left Ankle";
                this._leftFootCorrectionToggle = Instantiate(togglePrefab).GetComponent<Toggle>();
                rt = this._leftFootCorrectionToggle.transform as RectTransform;
                rt.SetParent(parent);
                rt.localPosition = Vector3.zero;
                rt.SetRect(togglePrefab.transform);
                rt.localScale = togglePrefab.transform.localScale;
                rt.SetParent(container.rectTransform);
                rt.offsetMin += new Vector2(0f, -40f);
                rt.offsetMax += new Vector2(0f, -40f);
                this._leftFootCorrectionToggle.onValueChanged = new Toggle.ToggleEvent();
                this._leftFootCorrectionToggle.onValueChanged.AddListener((b) =>
                {
                    if (this._poseTarget != null)
                        ((CharaPoseController)this._poseTarget).leftFootJointCorrection = this._leftFootCorrectionToggle.isOn;
                });

                Text rightFootText = Instantiate(textPrefab).GetComponent<Text>();
                rightFootText.rectTransform.SetParent(parent);
                rightFootText.rectTransform.localPosition = Vector3.zero;
                rightFootText.rectTransform.SetRect(textPrefab.transform);
                rightFootText.rectTransform.localScale = textPrefab.transform.localScale;
                rightFootText.rectTransform.SetParent(container.rectTransform);
                rightFootText.rectTransform.offsetMin += new Vector2(0f, -60f);
                rightFootText.rectTransform.offsetMax += new Vector2(0f, -60f);
                rightFootText.text = " Right Ankle";
                this._rightFootCorrectionToggle = Instantiate(togglePrefab).GetComponent<Toggle>();
                rt = this._rightFootCorrectionToggle.transform as RectTransform;
                rt.SetParent(parent);
                rt.localPosition = Vector3.zero;
                rt.SetRect(togglePrefab.transform);
                rt.localScale = togglePrefab.transform.localScale;
                rt.SetParent(container.rectTransform);
                rt.offsetMin += new Vector2(0f, -60f);
                rt.offsetMax += new Vector2(0f, -60f);
                this._rightFootCorrectionToggle.onValueChanged = new Toggle.ToggleEvent();
                this._rightFootCorrectionToggle.onValueChanged.AddListener((b) =>
                {
                    if (this._poseTarget != null)
                        ((CharaPoseController)this._poseTarget).rightFootJointCorrection = this._rightFootCorrectionToggle.isOn;
                });
            }
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
            {
                RectTransform parent = GameObject.Find("StudioScene").transform.Find("Canvas Main Menu/02_Manipulate/00_Chara/06_Joint") as RectTransform;
                RawImage container = UIUtility.CreateRawImage("Additional Container", parent);
                container.rectTransform.SetRect(Vector2.zero, Vector2.zero, new Vector2(0f, -60f), new Vector2(parent.rect.width, 0f));
                container.color = new Color32(110, 110, 116, 223);

                GameObject prefab = parent.Find("Left Leg (1)").gameObject;
                RectTransform crotchContainer = Instantiate(prefab).transform as RectTransform;
                crotchContainer.SetParent(container.rectTransform);
                crotchContainer.pivot = new Vector2(0f, 1f);
                crotchContainer.localPosition = Vector3.zero;
                crotchContainer.localScale = prefab.transform.localScale;
                crotchContainer.anchoredPosition = new Vector2(0f, 0f);
                crotchContainer.GetComponentInChildren<TextMeshProUGUI>().text = "Crotch";

                _crotchCorrectionToggle = crotchContainer.GetComponentInChildren<Toggle>();
                _crotchCorrectionToggle.onValueChanged = new Toggle.ToggleEvent();
                _crotchCorrectionToggle.onValueChanged.AddListener((b) =>
                {
                    if (_poseTarget != null)
                        ((CharaPoseController)_poseTarget).crotchJointCorrection = _crotchCorrectionToggle.isOn;
                });

                RectTransform leftFootContainer = Instantiate(prefab).transform as RectTransform;
                leftFootContainer.SetParent(container.rectTransform);
                leftFootContainer.pivot = new Vector2(0f, 1f);
                leftFootContainer.localPosition = Vector3.zero;
                leftFootContainer.localScale = prefab.transform.localScale;
                leftFootContainer.anchoredPosition = new Vector2(0f, -20f);
                leftFootContainer.GetComponentInChildren<TextMeshProUGUI>().text = "Left Ankle";
                _leftFootCorrectionToggle = leftFootContainer.GetComponentInChildren<Toggle>();
                _leftFootCorrectionToggle.onValueChanged = new Toggle.ToggleEvent();
                _leftFootCorrectionToggle.onValueChanged.AddListener((b) =>
                {
                    if (_poseTarget != null)
                        ((CharaPoseController)_poseTarget).leftFootJointCorrection = _leftFootCorrectionToggle.isOn;
                });

                RectTransform rightFootContainer = Instantiate(prefab).transform as RectTransform;

                rightFootContainer.SetParent(container.rectTransform);
                rightFootContainer.pivot = new Vector2(0f, 1f);
                rightFootContainer.localPosition = Vector3.zero;
                rightFootContainer.localScale = prefab.transform.localScale;
                rightFootContainer.anchoredPosition = new Vector2(0f, -40f);

                rightFootContainer.GetComponentInChildren<TextMeshProUGUI>().text = "Right Ankle";
                _rightFootCorrectionToggle = rightFootContainer.GetComponentInChildren<Toggle>();
                _rightFootCorrectionToggle.onValueChanged = new Toggle.ToggleEvent();
                _rightFootCorrectionToggle.onValueChanged.AddListener((b) =>
                {
                    if (_poseTarget != null)
                        ((CharaPoseController)_poseTarget).rightFootJointCorrection = _rightFootCorrectionToggle.isOn;
                });
            }
#endif
            foreach (Text childText in _ui.GetComponentsInChildren<Text>(true))
                childText.font = UIUtility.defaultFont;

            _imguiBackground = IMGUIExtensions.CreateUGUIPanelForIMGUI();
        }

        private void OnWindowResize()
        {
            if (_advancedModeRect.xMax > Screen.width)
                _advancedModeRect.x -= _advancedModeRect.xMax - Screen.width;
            if (_advancedModeRect.yMax > Screen.height)
                _advancedModeRect.y -= _advancedModeRect.yMax - Screen.height;
        }


        private void SetBoneTarget(FullBodyBipedEffector bone)
        {
            ResetBoneButtons();
            if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
            {
                _ikBoneTargets.Clear();
                _ikBendGoalTargets.Clear();
                _ikBoneTargets.Add(bone);
            }
            else
            {
                if (_ikBoneTargets.Contains(bone))
                    _ikBoneTargets.Remove(bone);
                else
                    _ikBoneTargets.Add(bone);
            }
            SelectBoneButtons();
            EventSystem.current.SetSelectedGameObject(null);
        }

        private void SetBendGoalTarget(FullBodyBipedChain bendGoal)
        {
            ResetBoneButtons();
            if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
            {
                _ikBoneTargets.Clear();
                _ikBendGoalTargets.Clear();
                _ikBendGoalTargets.Add(bendGoal);
            }
            else
            {
                if (_ikBendGoalTargets.Contains(bendGoal))
                    _ikBendGoalTargets.Remove(bendGoal);
                else
                    _ikBendGoalTargets.Add(bendGoal);
            }
            SelectBoneButtons();
            EventSystem.current.SetSelectedGameObject(null);
        }

        private void ResetBoneButtons()
        {
            foreach (FullBodyBipedChain bendGoal in _ikBendGoalTargets)
            {
                Button b = _bendGoalsButtons[(int)bendGoal];
                ColorBlock cb = b.colors;
                cb.normalColor = Color.Lerp(Color.blue, Color.white, 0.5f);
                cb.highlightedColor = cb.normalColor;
                b.colors = cb;
                _bendGoalsTexts[(int)bendGoal].fontStyle = FontStyle.Normal;
            }
            foreach (FullBodyBipedEffector effector in _ikBoneTargets)
            {
                Button b = _effectorsButtons[(int)effector];
                ColorBlock cb = b.colors;
                cb.normalColor = Color.Lerp(Color.red, Color.white, 0.5f);
                cb.highlightedColor = cb.normalColor;
                b.colors = cb;
                _effectorsTexts[(int)effector].fontStyle = FontStyle.Normal;
            }
        }

        private void SelectBoneButtons()
        {
            foreach (FullBodyBipedChain bendGoal in _ikBendGoalTargets)
            {
                Button b = _bendGoalsButtons[(int)bendGoal];
                ColorBlock cb = b.colors;
                cb.normalColor = UIUtility.lightGreenColor;
                cb.highlightedColor = cb.normalColor;
                b.colors = cb;
                _bendGoalsTexts[(int)bendGoal].fontStyle = FontStyle.Bold;
            }
            foreach (FullBodyBipedEffector effector in _ikBoneTargets)
            {
                Button b = _effectorsButtons[(int)effector];
                ColorBlock cb = b.colors;
                cb.normalColor = UIUtility.lightGreenColor;
                cb.highlightedColor = cb.normalColor;
                b.colors = cb;
                _effectorsTexts[(int)effector].fontStyle = FontStyle.Bold;
            }
        }

        private void GUILogic()
        {
            if (_shortcutRegisterMode)
            {
                if (Input.inputString.Length != 0)
                {
                    try
                    {
                        KeyCode kc = (KeyCode)Enum.Parse(typeof(KeyCode), Input.inputString.ToUpper());
                        if (kc != KeyCode.Escape && kc != KeyCode.Return && kc != KeyCode.Mouse0 && kc != KeyCode.Mouse1 && kc != KeyCode.Mouse2 && kc != KeyCode.Mouse3 && kc != KeyCode.Mouse4 && kc != KeyCode.Mouse5 && kc != KeyCode.Mouse6)
                        {
                            HSPE.ConfigMainWindowShortcut.Value = new KeyboardShortcut(kc);
                            _shortcutKeyButton.onClick.Invoke();
                        }
                    }
                    catch { }
                }
            }
            else if (HSPE.ConfigMainWindowShortcut.Value.IsDown())
            {
                _ui.gameObject.SetActive(!_ui.gameObject.activeSelf);
                _hspeButtonImage.color = _ui.gameObject.activeSelf ? Color.green : Color.white;
            }

            if (Input.GetMouseButtonUp(0))
            {
                _xMove = false;
                _yMove = false;
                _zMove = false;
                _xRot = false;
                _yRot = false;
                _zRot = false;
            }

            if (_ui.gameObject.activeSelf == false || _poseTarget == null)
                return;
            CharaPoseController charaPoseTarget = _poseTarget as CharaPoseController;
            bool isCharacter = charaPoseTarget != null;
            if (_xMove || _yMove || _zMove || _xRot || _yRot || _zRot)
            {
                _delta += (new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * (Input.GetKey(KeyCode.LeftShift) ? 4f : 1f) / (Input.GetKey(KeyCode.LeftControl) ? 6f : 1f)) / 10f;

                if (_poseTarget._currentDragType == PoseController.DragType.None)
                    _poseTarget.StartDrag(_xMove || _yMove || _zMove ? PoseController.DragType.Position : PoseController.DragType.Rotation);
                if (_currentModeIK)
                {
                    if (isCharacter)
                    {
                        for (int i = 0; i < _ikBoneTargets.Count; ++i)
                        {
                            bool changePosition = false;
                            bool changeRotation = false;
                            Vector3 newPosition = _lastIKBonesPositions[i];
                            Quaternion newRotation = _lastIKBonesRotations[i];
                            if (_xMove)
                            {
                                newPosition.x += _delta.y * _intensityValue;
                                changePosition = true;
                            }
                            if (_yMove)
                            {
                                newPosition.y += _delta.y * _intensityValue;
                                changePosition = true;
                            }
                            if (_zMove)
                            {
                                newPosition.z += _delta.y * _intensityValue;
                                changePosition = true;
                            }
                            if (_xRot)
                            {
                                newRotation *= Quaternion.AngleAxis(_delta.x * 20f * _intensityValue, Vector3.right);
                                changeRotation = true;
                            }
                            if (_yRot)
                            {
                                newRotation *= Quaternion.AngleAxis(_delta.x * 20f * _intensityValue, Vector3.up);
                                changeRotation = true;
                            }
                            if (_zRot)
                            {
                                newRotation *= Quaternion.AngleAxis(_delta.x * 20f * _intensityValue, Vector3.forward);
                                changeRotation = true;
                            }
                            FullBodyBipedEffector bone = _ikBoneTargets[i];
                            if (changePosition && charaPoseTarget.IsPartEnabled(bone))
                                charaPoseTarget.SetBoneTargetPosition(bone, newPosition, _positionOperationWorld);
                            if (changeRotation && charaPoseTarget.IsPartEnabled(bone))
                                charaPoseTarget.SetBoneTargetRotation(_ikBoneTargets[i], newRotation);
                        }
                        for (int i = 0; i < _ikBendGoalTargets.Count; ++i)
                        {
                            Vector3 newPosition = _lastIKBendGoalsPositions[i];
                            if (_xMove)
                                newPosition.x += _delta.y * _intensityValue;
                            if (_yMove)
                                newPosition.y += _delta.y * _intensityValue;
                            if (_zMove)
                                newPosition.z += _delta.y * _intensityValue;
                            FullBodyBipedChain bendGoal = _ikBendGoalTargets[i];
                            if (charaPoseTarget.IsPartEnabled(bendGoal))
                                charaPoseTarget.SetBendGoalPosition(bendGoal, newPosition, _positionOperationWorld);
                        }
                    }
                }
                else
                {
                    bool changeRotation = false;
                    Quaternion newRotation = _lastFKBonesRotation;
                    if (_xRot)
                    {
                        newRotation *= Quaternion.AngleAxis(_delta.x * 20f * _intensityValue, Vector3.right);
                        changeRotation = true;
                    }
                    if (_yRot)
                    {
                        newRotation *= Quaternion.AngleAxis(_delta.x * 20f * _intensityValue, Vector3.up);
                        changeRotation = true;
                    }
                    if (_zRot)
                    {
                        newRotation *= Quaternion.AngleAxis(_delta.x * 20f * _intensityValue, Vector3.forward);
                        changeRotation = true;
                    }
                    if (changeRotation)
                        _poseTarget.SeFKBoneTargetRotation(GuideObjectManager.Instance.selectObject, newRotation);
                }
            }
            else
            {
                _delta = Vector2.zero;
                if (_poseTarget._currentDragType != PoseController.DragType.None)
                    _poseTarget.StopDrag();
                if (_currentModeIK)
                {
                    if (isCharacter)
                    {
                        for (int i = 0; i < _ikBoneTargets.Count; ++i)
                        {
                            _lastIKBonesPositions[i] = charaPoseTarget.GetBoneTargetPosition(_ikBoneTargets[i], _positionOperationWorld);
                            _lastIKBonesRotations[i] = charaPoseTarget.GetBoneTargetRotation(_ikBoneTargets[i]);
                        }
                        for (int i = 0; i < _ikBendGoalTargets.Count; ++i)
                            _lastIKBendGoalsPositions[i] = charaPoseTarget.GetBendGoalPosition(_ikBendGoalTargets[i], _positionOperationWorld);
                    }
                }
                else
                {
                    if (GuideObjectManager.Instance.selectObject != null)
                        _lastFKBonesRotation = _poseTarget.GetFKBoneTargetRotation(GuideObjectManager.Instance.selectObject);
                }
            }

            if (_currentModeIK)
            {
                for (int i = 0; i < _effectorsButtons.Length; i++)
                    _effectorsButtons[i].interactable = isCharacter && charaPoseTarget.IsPartEnabled((FullBodyBipedEffector)i);

                for (int i = 0; i < _bendGoalsButtons.Length; i++)
                    _bendGoalsButtons[i].interactable = isCharacter && charaPoseTarget.IsPartEnabled((FullBodyBipedChain)i);

                for (int i = 0; i < _positionButtons.Length; i++)
                    _positionButtons[i].interactable = isCharacter && charaPoseTarget.target.ikEnabled;
            }

            bool interactableRotation = false;
            if (_currentModeIK)
            {
                if (isCharacter)
                    interactableRotation = charaPoseTarget.target.ikEnabled && _ikBendGoalTargets.Count == 0 && _ikBoneTargets.Intersect(CharaPoseController.nonRotatableEffectors).Any() == false;
            }
            else
            {
                OCIChar.BoneInfo bone;
                interactableRotation = _poseTarget.target.fkEnabled && GuideObjectManager.Instance.selectObject != null && _poseTarget.target.fkObjects.TryGetValue(GuideObjectManager.Instance.selectObject.transformTarget.gameObject, out bone) && bone.active;
            }

            for (int i = 0; i < _rotationButtons.Length; i++)
                _rotationButtons[i].interactable = interactableRotation;
        }
        #endregion

        #region Public Methods
        public void OnDuplicate(ObjectCtrlInfo source, ObjectCtrlInfo destination)
        {
            PoseController destinationController;
            var sourceController = source.guideObject.transformTarget.gameObject.GetComponent<PoseController>();
            if (destination is OCIChar)
                destinationController = destination.guideObject.transformTarget.gameObject.AddComponent<CharaPoseController>();
            else
            {
                destinationController = destination.guideObject.transformTarget.gameObject.AddComponent<PoseController>();
                if (!HSPE.ConfigDisableAdvancedModeOnCopy.Value)
                    destinationController.enabled = sourceController.enabled;
                else destinationController.enabled = false;
            }
            destinationController.LoadFrom(sourceController);
        }
        #endregion

        #region Private Methods
        private void OnObjectAdded()
        {
            foreach (KeyValuePair<int, ObjectCtrlInfo> kvp in Studio.Studio.Instance.dicObjectCtrl)
            {
                if (kvp.Key >= _lastIndex)
                {
                    // Note: Objects copied by the "obj copy" studio button will have their PoseControllers copied as well, so the AddComponent calls below won't run in those cases
                    switch (kvp.Value.objectInfo.kind)
                    {
                        case 0:
                            if (kvp.Value.guideObject.transformTarget.GetComponent<PoseController>() == null)
                                kvp.Value.guideObject.transformTarget.gameObject.AddComponent<CharaPoseController>();
                            break;
                        case 1:
                            if (kvp.Value.guideObject.transformTarget.GetComponent<PoseController>() == null)
                                kvp.Value.guideObject.transformTarget.gameObject.AddComponent<PoseController>();
                            break;
                    }
                }
            }
        }

        private void OnTargetChange(PoseController last)
        {
            if (_poseTarget != null)
            {
                bool isCharacter = _poseTarget.target.type == GenericOCITarget.Type.Character;
                if (isCharacter)
                {
                    CharaPoseController poseTarget = (CharaPoseController)_poseTarget;

                    _optimizeIKToggle.isOn = poseTarget.optimizeIK;
                    _crotchCorrectionToggle.isOn = poseTarget.crotchJointCorrection;
                    _leftFootCorrectionToggle.isOn = poseTarget.leftFootJointCorrection;
                    _rightFootCorrectionToggle.isOn = poseTarget.rightFootJointCorrection;
                }
                _optimizeIKToggle.interactable = isCharacter;
                _copyLeftArmButton.interactable = isCharacter;
                _copyRightArmButton.interactable = isCharacter;
                _copyLeftHandButton.interactable = isCharacter;
                _copyRightHandButton.interactable = isCharacter;
                _copyLeftLegButton.interactable = isCharacter;
                _copyRightLegButton.interactable = isCharacter;
                _swapPostButton.interactable = isCharacter;
                _nothingText.gameObject.SetActive(false);
                _controls.gameObject.SetActive(true);
                _poseTarget.target.RefreshFKBones();
                RefreshFKBonesList();
            }
            else
            {
                _nothingText.gameObject.SetActive(true);
                _controls.gameObject.SetActive(false);
            }
            PoseController.SelectionChanged(_poseTarget);
        }

        private void OnCharacterReplaced(OCIChar chara)
        {
            if (_poseTarget != null && _poseTarget.target.oci == chara)
                this.ExecuteDelayed2(RefreshFKBonesList);
        }

        private void OnLoadClothesFile(OCIChar chara)
        {
            if (_poseTarget != null && _poseTarget.target.oci == chara)
                this.ExecuteDelayed2(RefreshFKBonesList);
        }

#if HONEYSELECT || KOIKATSU
#if HONEYSELECT
        private void OnCoordinateReplaced(OCIChar chara, CharDefine.CoordinateType coord, bool force)
#elif KOIKATSU
        private void OnCoordinateReplaced(OCIChar chara, ChaFileDefine.CoordinateType type, bool force)
#endif
        {
            if (_poseTarget != null && _poseTarget.target.oci == chara)
                this.ExecuteDelayed2(RefreshFKBonesList);
        }
#endif

        private void RefreshFKBonesList()
        {
            int i = 0;
            List<KeyValuePair<GameObject, OCIChar.BoneInfo>> list = _poseTarget.target.fkObjects.ToList();

            for (; i < list.Count; ++i)
            {
                FKBoneEntry entry;
                KeyValuePair<GameObject, OCIChar.BoneInfo> pair = list[i];
                if (i < _fkBoneEntries.Count)
                    entry = _fkBoneEntries[i];
                else
                {
                    entry = new FKBoneEntry();
                    entry.toggle = GameObject.Instantiate(_fkBoneTogglePrefab).GetComponent<Toggle>();
                    entry.toggle.gameObject.hideFlags = HideFlags.None;
                    entry.text = entry.toggle.GetComponentInChildren<Text>();

                    entry.toggle.transform.SetParent(_fkScrollRect.content);
                    entry.toggle.transform.localScale = Vector3.one;
                    entry.toggle.group = _fkToggleGroup;
                    entry.text.font = UIUtility.defaultFont;
                    _fkBoneEntries.Add(entry);
                }
                if (pair.Key == null)
                {
                    entry.toggle.gameObject.SetActive(false);
                    entry.target = null;
                    continue;
                }

                entry.text.text = pair.Key.name;

                entry.toggle.onValueChanged = new Toggle.ToggleEvent();
                entry.toggle.isOn = GuideObjectManager.Instance.selectObject.transformTarget == pair.Key.transform;
                entry.toggle.onValueChanged.AddListener((b) =>
                {
                    if (entry.toggle.isOn)
                    {
                        if (_poseTarget.target.fkEnabled == false || pair.Value.active == false)
                        {
                            entry.toggle.isOn = false;
                            SelectCurrentFKBoneEntry();
                            return;
                        }
                        GuideObjectManager.Instance.selectObject = _dicGuideObject[pair.Key.transform];
                    }
                });
                entry.target = pair.Key;
                entry.toggle.gameObject.SetActive(true);
            }
            for (; i < _fkBoneEntries.Count; ++i)
            {
                FKBoneEntry entry = _fkBoneEntries[i];

                entry.toggle.gameObject.SetActive(false);
                entry.target = null;
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(_fkScrollRect.content);
        }

        private FKBoneEntry SelectCurrentFKBoneEntry()
        {
            FKBoneEntry entry = null;
            if (GuideObjectManager.Instance.selectObject != null)
            {
                entry = _fkBoneEntries.Find(e => e.target == GuideObjectManager.Instance.selectObject.transformTarget.gameObject);
                if (entry != null)
                {
                    Toggle.ToggleEvent cachedEvent = entry.toggle.onValueChanged;
                    entry.toggle.onValueChanged = new Toggle.ToggleEvent();
                    entry.toggle.isOn = true;
                    entry.toggle.onValueChanged = cachedEvent;
                }
            }
            return entry;
        }

        [HarmonyPatch(typeof(GuideSelect), "OnPointerClick", new[] { typeof(PointerEventData) })]
        private static class GuideSelect_OnPointerClick_Patches
        {
            private static void Postfix()
            {
                _self.ExecuteDelayed2(() =>
                {
                    _self._fkToggleGroup.SetAllTogglesOff();
                    FKBoneEntry entry = _self.SelectCurrentFKBoneEntry();
                    if (entry != null)
                    {
                        _self._fkScrollRect.content.anchoredPosition = new Vector2(_self._fkScrollRect.content.anchoredPosition.x, _self._fkScrollRect.transform.InverseTransformPoint(_self._fkScrollRect.content.position).y - _self._fkScrollRect.transform.InverseTransformPoint(entry.toggle.transform.position).y - 10);
                        _self._fkScrollRect.normalizedPosition = new Vector2(_self._fkScrollRect.normalizedPosition.x, Mathf.Clamp01(_self._fkScrollRect.normalizedPosition.y));
                    }
                });
            }
        }

        [HarmonyPatch(typeof(Studio.CameraControl), "InputMouseProc")]
        private static class CameraControl_LateUpdate_Patches
        {
            private static bool Prefix()
            {
                if (_self._poseTarget != null && _self._poseTarget.isDraggingDynamicBone)
                    return false;
                return true;
            }
        }
        #endregion

        #region Saves
#if IPA
        private void OnSceneLoad(string scenePath, XmlNode node)
        {
            this._lastObjectCount = 0;
            scenePath = Path.GetFileNameWithoutExtension(scenePath) + ".sav";
#if HONEYSELECT
            string dir = _pluginDir + _studioSavesDir;
            string path = dir + scenePath;
            if (File.Exists(path))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(path);
                node = doc;
            }
#endif
            if (node != null)
                node = node.FirstChild;
            this.ExecuteDelayed(() =>
            {
                this.LoadDefaultVersion(node, new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).ToList());
            }, 3);
        }

        private void OnSceneImport(string scenePath, XmlNode node)
        {
            scenePath = Path.GetFileNameWithoutExtension(scenePath) + ".sav";
#if HONEYSELECT
            string dir = _pluginDir + _studioSavesDir;
            string path = dir + scenePath;
            if (File.Exists(path))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(path);
                node = doc;
            }
#endif
            if (node != null)
                node = node.FirstChild;
            Dictionary<int, ObjectCtrlInfo> toIgnore = new Dictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl);
            this.ExecuteDelayed(() =>
            {
                this.LoadDefaultVersion(node, Studio.Studio.Instance.dicObjectCtrl.Where(e => toIgnore.ContainsKey(e.Key) == false).OrderBy(e => SceneInfo_Import_Patches._newToOldKeys[e.Key]).ToList());
            }, 3);
        }

        private void OnSceneSave(string scenePath, XmlTextWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("root");
            xmlWriter.WriteAttributeString("version", HSPE._versionNum);
            SortedDictionary<int, ObjectCtrlInfo> dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl);
            foreach (KeyValuePair<int, ObjectCtrlInfo> kvp in dic)
            {
                if (kvp.Value is OCIChar)
                {
                    xmlWriter.WriteStartElement("characterInfo");
#if PLAYHOME
                    xmlWriter.WriteAttributeString("name", ((OCIChar)kvp.Value).charInfo.fileStatus.name);
#else
                    xmlWriter.WriteAttributeString("name", ((OCIChar)kvp.Value).charInfo.customInfo.name);
#endif
                    xmlWriter.WriteAttributeString("index", XmlConvert.ToString(kvp.Key));

                    this.SaveElement(kvp.Value, xmlWriter);

                    xmlWriter.WriteEndElement();
                }
                else if (kvp.Value is OCIItem)
                {
                    xmlWriter.WriteStartElement("itemInfo");
                    xmlWriter.WriteAttributeString("name", ((OCIItem)kvp.Value).treeNodeObject.textName);
                    xmlWriter.WriteAttributeString("index", XmlConvert.ToString(kvp.Key));

                    this.SaveElement(kvp.Value, xmlWriter);

                    xmlWriter.WriteEndElement();
                }
            }
            xmlWriter.WriteEndElement();
        }

        private void LoadDefaultVersion(XmlNode node, List<KeyValuePair<int, ObjectCtrlInfo>> dic)
        {
            //this.ExecuteDelayed(() => {
            //    HSPE.Logger.LogError("objects in scene " + Studio.Studio.Instance.dicObjectCtrl.Count);
            //}, 6);
            if (node == null || node.Name != "root")
                return;
            string v = node.Attributes["version"].Value;
            int i = 0;
            foreach (XmlNode childNode in node.ChildNodes)
            {
                switch (childNode.Name)
                {
                    case "characterInfo":
                        OCIChar ociChar = null;
                        while (i < dic.Count && (ociChar = dic[i].Value as OCIChar) == null)
                            ++i;
                        if (i == dic.Count)
                            break;
                        this.LoadElement(ociChar, childNode);
                        ++i;
                        break;
                    case "itemInfo":
                        OCIItem ociItem = null;
                        while (i < dic.Count && (ociItem = dic[i].Value as OCIItem) == null)
                            ++i;
                        if (i == dic.Count)
                            break;
                        this.LoadElement(ociItem, childNode);
                        ++i;
                        break;
                }
            }
        }
#elif BEPINEX
        private void OnCharaLoad(ChaFile file)
        {
            PluginData data = ExtendedSave.GetExtendedDataById(file, _extSaveKey);
            if (data == null)
                return;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml((string)data.data["characterInfo"]);
            XmlNode node = doc.FirstChild;
            if (node == null)
                return;
            string v = node.Attributes["version"].Value;
            this.ExecuteDelayed2(() =>
            {
                foreach (KeyValuePair<int, ObjectCtrlInfo> pair in Studio.Studio.Instance.dicObjectCtrl)
                {
                    OCIChar ociChar = pair.Value as OCIChar;
                    if (ociChar != null && ociChar.charInfo.chaFile == file)
                        LoadElement(ociChar, node);
                }
            });
        }

        private void OnCharaSave(ChaFile file)
        {
            foreach (KeyValuePair<int, ObjectCtrlInfo> pair in Studio.Studio.Instance.dicObjectCtrl)
            {
                OCIChar ociChar = pair.Value as OCIChar;
                if (ociChar != null && ociChar.charInfo.chaFile == file)
                {
                    using (StringWriter stringWriter = new StringWriter())
                    using (XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter))
                    {
                        xmlWriter.WriteStartElement("characterInfo");

                        xmlWriter.WriteAttributeString("version", HSPE.Version);
                        xmlWriter.WriteAttributeString("name", ociChar.charInfo.chaFile.parameter.fullname);

                        SaveElement(ociChar, xmlWriter);

                        xmlWriter.WriteEndElement();

                        PluginData data = new PluginData();
                        data.version = HSPE.saveVersion;
                        data.data.Add("characterInfo", stringWriter.ToString());
                        ExtendedSave.SetExtendedDataById(file, _extSaveKey, data);
                    }
                }
            }
        }

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
            this.ExecuteDelayed2(() =>
            {
                _lastLoadType = LoadType.Load;
                LoadSceneGeneric(node, Studio.Studio.Instance.dicObjectCtrl);
            }, 3);
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
            this.ExecuteDelayed2(() =>
            {
                var changedKeys = Studio.Studio.Instance.sceneInfo.dicChangeKey;

                var newItems = Studio.Studio.Instance.sceneInfo.dicImport
                    .Where(d => changedKeys.ContainsKey(d.Key) && Studio.Studio.Instance.dicObjectCtrl.ContainsKey(d.Key))
                    .ToDictionary(m => changedKeys[m.Key], f => Studio.Studio.Instance.dicObjectCtrl[f.Key]);

                _lastLoadType = LoadType.Import;
                LoadSceneGeneric(node, newItems);
            }, 3);
        }

        /// <summary>
        /// Other plugins should use this to force load some data.
        /// </summary>
        /// <param name="node"></param>
        public void ExternalLoadScene(XmlNode node)
        {
            _lastLoadType = LoadType.External;
            LoadSceneGeneric(node, Studio.Studio.Instance.dicObjectCtrl);
        }

        private void LoadSceneGeneric(XmlNode node, Dictionary<int, ObjectCtrlInfo> dic)
        {
            if (node == null || node.Name != "root")
                return;

            // Unique identifier for this load session.
            _lastLoadId = UnityEngine.Random.Range(1, 1000000);

            foreach (XmlNode childNode in node.ChildNodes)
            {
                if (childNode.Name.Equals("itemInfo") == false)
                {
                    continue;
                }

                var attribute = childNode.Attributes?["index"];

                if (attribute == null)
                {
                    continue;
                }

                if (int.TryParse(attribute.Value, out var index) == false)
                {
                    continue;
                }

                if (dic.TryGetValue(index, out var objectCtrlInfo) && objectCtrlInfo is OCIItem ociItem)
                {
                    LoadElement(ociItem, childNode);
                }
                else
                {
                    HSPE.Logger.LogWarning($"[HSPE] Failed to find item of index {index}! It will be skipped.");
                }
                /*
                switch (childNode.Name)
                {
                    case "itemInfo":
                        OCIItem ociItem = null;
                        while (i < dic.Count && (ociItem = dic[i].Value as OCIItem) == null)
                            ++i;
                        if (i == dic.Count)
                            break;
                        HSPE.Logger.LogMessage($"{i} matched {dic[i].Value.objectInfo.dicKey}, if this isn't right, kys.");
                        LoadElement(ociItem, childNode);
                        ++i;
                        break;
                }
                */
            }
        }

        private void OnSceneSave(string path)
        {
            using (StringWriter stringWriter = new StringWriter())
            using (XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter))
            {

                xmlWriter.WriteStartElement("root");
                xmlWriter.WriteAttributeString("version", HSPE.Version);
                SortedDictionary<int, ObjectCtrlInfo> dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl);
                foreach (KeyValuePair<int, ObjectCtrlInfo> kvp in dic)
                {
                    OCIItem item = kvp.Value as OCIItem;
                    if (item != null)
                    {
                        xmlWriter.WriteStartElement("itemInfo");
                        xmlWriter.WriteAttributeString("name", item.treeNodeObject.textName);
                        xmlWriter.WriteAttributeString("index", XmlConvert.ToString(kvp.Key));

                        SaveElement(item, xmlWriter);

                        xmlWriter.WriteEndElement();
                    }
                }
                xmlWriter.WriteEndElement();

                PluginData data = new PluginData();
                data.version = HSPE.saveVersion;
                data.data.Add("sceneInfo", stringWriter.ToString());
                ExtendedSave.SetSceneExtendedDataById(_extSaveKey, data);
            }
        }
#endif

        private void LoadElement(OCIChar oci, XmlNode node)
        {
            PoseController controller = oci.guideObject.transformTarget.GetComponent<PoseController>();
            if (controller == null)
                controller = oci.guideObject.transformTarget.gameObject.AddComponent<CharaPoseController>();
            bool controllerEnabled = true;
            if (node.Attributes != null && node.Attributes["enabled"] != null)
                controllerEnabled = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
            controller.ScheduleLoad(node, e =>
            {
                controller.enabled = controllerEnabled;
            });

        }

        private void LoadElement(OCIItem oci, XmlNode node)
        {
            PoseController controller = oci.guideObject.transformTarget.GetComponent<PoseController>();
            if (controller == null)
                controller = oci.guideObject.transformTarget.gameObject.AddComponent<PoseController>();
            bool controllerEnabled = false;
            if (node.Attributes != null && node.Attributes["enabled"] != null)
                controllerEnabled = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
            controller.ScheduleLoad(node, e =>
            {
                controller.enabled = controllerEnabled;
            });

        }

        private void SaveElement(ObjectCtrlInfo oci, XmlTextWriter xmlWriter)
        {
            PoseController controller = oci.guideObject.transformTarget.GetComponent<PoseController>();
            xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(controller.enabled));
            controller.SaveXml(xmlWriter);
        }
        #endregion
    }
}
