using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CustomMenu;
using IllusionUtility.GetUtility;
using IllusionUtility.SetUtility;
using UILib;
using UnityEngine;
using UnityEngine.UI;
using Vectrosity;
using ToolBox;
using UnityEngine.Events;
using ToolBox.Extensions;

namespace MoreAccessories
{
    public class SmMoreAccessories : SubMenuBase
    {
        #region Private Types
        private class CopySlot
        {
            public Toggle toggle;
            public Text text;
        }

        public class TypeData
        {
            public RectTransform parentObject;
            public List<ObjectData> objects = new List<ObjectData>();
            public readonly Dictionary<int, int> keyToOriginalIndex = new Dictionary<int, int>();
            public bool lastSortByCreationDateReverse = false;
            public bool lastSortByNameReverse = true;
        }

        public class ObjectData
        {
            public int key;
            public Toggle toggle;
            public Text text;
            public GameObject obj;
            public DateTime creationDate;
        }
        #endregion

        #region Protected Variables
        protected readonly float[] movePosValue = {
            0.1f,
            1f,
            10f
        };
        protected readonly float[] moveRotValue = {
            1f,
            5f,
            10f
        };
        protected readonly float[] moveSclValue = {
            0.01f,
            0.1f,
            1f
        };
        protected bool initFlags;
        protected bool nowChanging;
        protected bool nowLoading;
        protected bool updateVisualOnly;
        protected int selectColorNo;
        protected byte modePos = 1;
        protected byte modeRot = 1;
        protected byte modeScl = 1;
        protected int firstIndex;
        protected bool nowTglAllSet;
        protected Toggle[] tglType = new Toggle[13];
        protected int acsType;
        protected Toggle[] tglParent = new Toggle[29];
        protected List<Toggle> additionalTglParent = new List<Toggle>();
        protected Dictionary<string, int> dictParentKey = new Dictionary<string, int>();
        protected Dictionary<string, int> dictAdditionalParentKey = new Dictionary<string, int>();
        protected string[] strParentKey = {
            "AP_Head",
            "AP_Megane",
            "AP_Nose",
            "AP_Mouth",
            "AP_Earring_L",
            "AP_Earring_R",
            "AP_Neck",
            "AP_Chest",
            "AP_Waist",
            "AP_Tikubi_L",
            "AP_Tikubi_R",
            "AP_Shoulder_L",
            "AP_Shoulder_R",
            "AP_Arm_L",
            "AP_Arm_R",
            "AP_Wrist_L",
            "AP_Wrist_R",
            "AP_Hand_L",
            "AP_Hand_R",
            "AP_Index_L",
            "AP_Middle_L",
            "AP_Ring_L",
            "AP_Index_R",
            "AP_Middle_R",
            "AP_Ring_R",
            "AP_Leg_L",
            "AP_Leg_R",
            "AP_Ankle_L",
            "AP_Ankle_R"
        };
        protected List<GameObject> lstTagColor = new List<GameObject>();
        protected bool initEnd;
        protected bool flagMovePos;
        protected bool flagMoveRot;
        protected bool flagMoveScl;
        protected int indexMovePos;
        protected int indexMoveRot;
        protected float downTimeCnt;
        protected float loopTimeCnt;
        protected int indexMoveScl;
        #endregion

        #region Public Variables
        public Toggle tglTab;
        public Toggle tab02;
        public Toggle tab03;
        public Toggle tab04;
        public Toggle tab05;
        public ToggleGroup grpType;
        public ToggleGroup grpParent;
        public GameObject objListTop;
        public GameObject objLineBase;
        public RectTransform rtfPanel;
        public Image[] imgDiffuse;
        public Image[] imgSpecular;
        public Slider sldIntensity;
        public InputField inputIntensity;
        public Slider[] sldSharpness;
        public InputField[] inputSharpness;
        public GameObject objSubColor;
        public InputField inputPosX;
        public InputField inputPosY;
        public InputField inputPosZ;
        public InputField inputRotX;
        public InputField inputRotY;
        public InputField inputRotZ;
        public InputField inputSclX;
        public InputField inputSclY;
        public InputField inputSclZ;
        public Toggle tglReversal;
        #endregion

        #region Private Variables
        public readonly Dictionary<int, TypeData> _types = new Dictionary<int, TypeData>();
        private int _lastType;
        private int _lastId;
        private RectTransform _container;
        private InputField _searchBar;
        private readonly List<CopySlot> _dstCopy = new List<CopySlot>();
        private readonly List<CopySlot> _srcCopy = new List<CopySlot>();
        private Toggle _debugParentToggle;
        private readonly List<VectorLine> _debugVectorLines = new List<VectorLine>();
        private ScrollRect _scrollView;
        private SmAccessory _original;
        internal int _guideObjectMode = 0;
        #endregion

        #region Public Accessors
        public CharFile charFile { get { return this.chaInfo.chaFile; } }
        #endregion

        #region Unity Methods
        private void OnEnable()
        {
            if (this.initEnd)
                MoreAccessories._self.CustomControl_UpdateAcsName();
            this.DebugParentChanged(true);
        }

        public new virtual void Start()
        {
            if (!this.initEnd)
            {
                this.Init();
            }
        }

        public new virtual void Update()
        {
            if (this.flagMovePos)
            {
                this.downTimeCnt += Time.deltaTime;
                if (this.downTimeCnt > 0.5)
                {
                    this.loopTimeCnt += Time.deltaTime;
                    while (this.loopTimeCnt > 0.05000000074505806)
                    {
                        this.OnClickPos(this.indexMovePos);
                        this.loopTimeCnt -= 0.05f;
                    }
                }
            }
            if (this.flagMoveRot)
            {
                this.downTimeCnt += Time.deltaTime;
                if (this.downTimeCnt > 0.5)
                {
                    this.loopTimeCnt += Time.deltaTime;
                    while (this.loopTimeCnt > 0.05000000074505806)
                    {
                        this.OnClickRot(this.indexMoveRot);
                        this.loopTimeCnt -= 0.05f;
                    }
                }
            }
            if (this.flagMoveScl)
            {
                this.downTimeCnt += Time.deltaTime;
                if (this.downTimeCnt > 0.5)
                {
                    this.loopTimeCnt += Time.deltaTime;
                    while (this.loopTimeCnt > 0.05000000074505806)
                    {
                        this.OnClickScl(this.indexMoveScl);
                        this.loopTimeCnt -= 0.05f;
                    }
                }
            }
        }

        void LateUpdate()
        {
            foreach (VectorLine line in this._debugVectorLines)
            {
                line.Draw();
            }
        }

        void OnDisable()
        {
            foreach (VectorLine line in this._debugVectorLines)
            {
                line.active = false;
            }
            MoreAccessories._self._charaMakerGuideObject.SetTransformTarget(null);
        }
        #endregion

        #region Public Methods
        public void PreInit(SmAccessory original)
        {
            this._original = original;
            this.tglTab = this.transform.Find(original.tglTab.transform.GetPathFrom(original.transform)).GetComponent<Toggle>();
            this.tab02 = this.transform.Find(original.tab02.transform.GetPathFrom(original.transform)).GetComponent<Toggle>();
            this.tab03 = this.transform.Find(original.tab03.transform.GetPathFrom(original.transform)).GetComponent<Toggle>();
            this.tab04 = this.transform.Find(original.tab04.transform.GetPathFrom(original.transform)).GetComponent<Toggle>();
            this.tab05 = this.transform.Find(original.tab05.transform.GetPathFrom(original.transform)).GetComponent<Toggle>();
            this.grpType = this.transform.Find(original.grpType.transform.GetPathFrom(original.transform)).GetComponent<ToggleGroup>();
            this.grpParent = this.transform.Find(original.grpParent.transform.GetPathFrom(original.transform)).GetComponent<ToggleGroup>();
            this.objListTop = this.transform.Find(original.objListTop.transform.GetPathFrom(original.transform)).gameObject;
            this.objLineBase = original.objLineBase;
            this.rtfPanel = this.transform.Find(original.rtfPanel.transform.GetPathFrom(original.transform)).GetComponent<RectTransform>();

            this.imgDiffuse = new Image[original.imgDiffuse.Length];
            for (int i = 0; i < this.imgDiffuse.Length; i++)
                this.imgDiffuse[i] = this.transform.Find(original.imgDiffuse[i].transform.GetPathFrom(original.transform)).GetComponent<Image>();

            this.imgSpecular = new Image[original.imgSpecular.Length];
            for (int i = 0; i < this.imgSpecular.Length; i++)
                this.imgSpecular[i] = this.transform.Find(original.imgSpecular[i].transform.GetPathFrom(original.transform)).GetComponent<Image>();

            this.sldIntensity = this.transform.Find(original.sldIntensity.transform.GetPathFrom(original.transform)).GetComponent<Slider>();
            this.inputIntensity = this.transform.Find(original.inputIntensity.transform.GetPathFrom(original.transform)).GetComponent<InputField>();

            this.sldSharpness = new Slider[original.sldSharpness.Length];
            for (int i = 0; i < this.sldSharpness.Length; i++)
                this.sldSharpness[i] = this.transform.Find(original.sldSharpness[i].transform.GetPathFrom(original.transform)).GetComponent<Slider>();

            this.inputSharpness = new InputField[original.inputSharpness.Length];
            for (int i = 0; i < this.inputSharpness.Length; i++)
                this.inputSharpness[i] = this.transform.Find(original.inputSharpness[i].transform.GetPathFrom(original.transform)).GetComponent<InputField>();

            this.objSubColor = this.transform.Find(original.objSubColor.transform.GetPathFrom(original.transform)).gameObject;
            this.inputPosX = this.transform.Find(original.inputPosX.transform.GetListPathFrom(original.transform)).GetComponent<InputField>();
            this.inputPosX.onEndEdit = new InputField.SubmitEvent();
            this.inputPosX.onEndEdit.AddListener((s) => this.OnInputEndPos(0));

            this.inputPosY = this.transform.Find(original.inputPosY.transform.GetListPathFrom(original.transform)).GetComponent<InputField>();
            this.inputPosY.onEndEdit = new InputField.SubmitEvent();
            this.inputPosY.onEndEdit.AddListener((s) => this.OnInputEndPos(1));

            this.inputPosZ = this.transform.Find(original.inputPosZ.transform.GetListPathFrom(original.transform)).GetComponent<InputField>();
            this.inputPosZ.onEndEdit = new InputField.SubmitEvent();
            this.inputPosZ.onEndEdit.AddListener((s) => this.OnInputEndPos(2));


            this.inputRotX = this.transform.Find(original.inputRotX.transform.GetListPathFrom(original.transform)).GetComponent<InputField>();
            this.inputRotX.onEndEdit = new InputField.SubmitEvent();
            this.inputRotX.onEndEdit.AddListener((s) => this.OnInputEndRot(0));

            this.inputRotY = this.transform.Find(original.inputRotY.transform.GetListPathFrom(original.transform)).GetComponent<InputField>();
            this.inputRotY.onEndEdit = new InputField.SubmitEvent();
            this.inputRotY.onEndEdit.AddListener((s) => this.OnInputEndRot(1));

            this.inputRotZ = this.transform.Find(original.inputRotZ.transform.GetListPathFrom(original.transform)).GetComponent<InputField>();
            this.inputRotZ.onEndEdit = new InputField.SubmitEvent();
            this.inputRotZ.onEndEdit.AddListener((s) => this.OnInputEndRot(2));

            this.inputSclX = this.transform.Find(original.inputSclX.transform.GetListPathFrom(original.transform)).GetComponent<InputField>();
            this.inputSclX.onEndEdit = new InputField.SubmitEvent();
            this.inputSclX.onEndEdit.AddListener((s) => this.OnInputEndScl(0));

            this.inputSclY = this.transform.Find(original.inputSclY.transform.GetListPathFrom(original.transform)).GetComponent<InputField>();
            this.inputSclY.onEndEdit = new InputField.SubmitEvent();
            this.inputSclY.onEndEdit.AddListener((s) => this.OnInputEndScl(1));

            this.inputSclZ = this.transform.Find(original.inputSclZ.transform.GetListPathFrom(original.transform)).GetComponent<InputField>();
            this.inputSclZ.onEndEdit = new InputField.SubmitEvent();
            this.inputSclZ.onEndEdit.AddListener((s) => this.OnInputEndScl(2));


            for (int i = 0; i < original.tglDstCopy.Length; i++)
            {
                CopySlot slot = new CopySlot();
                slot.toggle = this.transform.Find(original.tglDstCopy[i].transform.GetPathFrom(original.transform)).GetComponent<Toggle>();
                slot.text = slot.toggle.GetComponentInChildren<Text>();
                slot.text.text = "アクセサリ" + (i + 1).ToString("00");
                slot.toggle.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                this._dstCopy.Add(slot);
            }
            VerticalLayoutGroup group = this._dstCopy[0].toggle.transform.parent.gameObject.AddComponent<VerticalLayoutGroup>();
            group.childForceExpandWidth = true;
            group.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            group = group.transform.parent.gameObject.AddComponent<VerticalLayoutGroup>();
            group.childForceExpandWidth = true;
            group.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            for (int i = 0; i < original.tglSrcCopy.Length; i++)
            {
                CopySlot slot = new CopySlot();
                slot.toggle = this.transform.Find(original.tglSrcCopy[i].transform.GetPathFrom(original.transform)).GetComponent<Toggle>();
                slot.text = slot.toggle.GetComponentInChildren<Text>();
                slot.text.text = "アクセサリ" + (i + 1).ToString("00");
                slot.toggle.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                this._srcCopy.Add(slot);
            }
            group = this._srcCopy[0].toggle.transform.parent.gameObject.AddComponent<VerticalLayoutGroup>();
            group.childForceExpandWidth = true;
            group.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            group = group.transform.parent.gameObject.AddComponent<VerticalLayoutGroup>();
            group.childForceExpandWidth = true;
            group.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            this.tglReversal = this.transform.Find(original.tglReversal.transform.GetPathFrom(original.transform)).GetComponent<Toggle>();

            Toggle[] originalTglType = (Toggle[])original.GetPrivate("tglType");
            this.tglType = new Toggle[originalTglType.Length];

            this.SetChangeValueSharpnessHandler(this.sldSharpness[0], 0);
            this.SetChangeValueSharpnessHandler(this.sldSharpness[1], 1);
            this.SetInputEndSharpnessHandler(this.inputSharpness[0], 0);
            this.SetInputEndSharpnessHandler(this.inputSharpness[1], 1);

            this._container = this.transform.FindDescendant("ListTop").transform as RectTransform;
            group = this._container.gameObject.AddComponent<VerticalLayoutGroup>();
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            ContentSizeFitter fitter = this._container.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            this.rtfPanel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            group = this.rtfPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;

            RectTransform rt = this.transform.FindChild("TabControl/TabItem02") as RectTransform;
            this._debugParentToggle = UIUtility.CreateToggle("Debug Parent Toggle", rt, "Debug Position");
            this._debugParentToggle.transform.SetRect(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -224f), new Vector2(214f, -204f));
            this._debugParentToggle.onValueChanged.AddListener(this.DebugParentChanged);
            this._debugParentToggle.isOn = false;
            Text t = this._debugParentToggle.GetComponentInChildren<Text>();
            t.alignment = TextAnchor.MiddleLeft;
            t.color = Color.white;

            Sprite toggle_b = null, toggle_c = null;
            foreach (Sprite s in Resources.FindObjectsOfTypeAll<Sprite>())
            {
                switch (s.name)
                {
                    case "toggle_b":
                        toggle_b = s;
                        break;
                    case "toggle_c":
                        toggle_c = s;
                        break;
                }
            }
            this._debugParentToggle.transform.Find("Background").GetComponent<Image>().sprite = toggle_b;
            ((Image)this._debugParentToggle.graphic).sprite = toggle_c;

            {
                float size = 0.012f;
                Vector3 topLeftForward = (Vector3.up + Vector3.left + Vector3.forward) * size,
                    topRightForward = (Vector3.up + Vector3.right + Vector3.forward) * size,
                    bottomLeftForward = ((Vector3.down + Vector3.left + Vector3.forward) * size),
                    bottomRightForward = ((Vector3.down + Vector3.right + Vector3.forward) * size),
                    topLeftBack = (Vector3.up + Vector3.left + Vector3.back) * size,
                    topRightBack = (Vector3.up + Vector3.right + Vector3.back) * size,
                    bottomLeftBack = (Vector3.down + Vector3.left + Vector3.back) * size,
                    bottomRightBack = (Vector3.down + Vector3.right + Vector3.back) * size;
                this._debugVectorLines.Add(VectorLine.SetLine(Color.white, topLeftForward, topRightForward));
                this._debugVectorLines.Add(VectorLine.SetLine(Color.white, topRightForward, bottomRightForward));
                this._debugVectorLines.Add(VectorLine.SetLine(Color.white, bottomRightForward, bottomLeftForward));
                this._debugVectorLines.Add(VectorLine.SetLine(Color.white, bottomLeftForward, topLeftForward));
                this._debugVectorLines.Add(VectorLine.SetLine(Color.white, topLeftBack, topRightBack));
                this._debugVectorLines.Add(VectorLine.SetLine(Color.white, topRightBack, bottomRightBack));
                this._debugVectorLines.Add(VectorLine.SetLine(Color.white, bottomRightBack, bottomLeftBack));
                this._debugVectorLines.Add(VectorLine.SetLine(Color.white, bottomLeftBack, topLeftBack));
                this._debugVectorLines.Add(VectorLine.SetLine(Color.white, topLeftBack, topLeftForward));
                this._debugVectorLines.Add(VectorLine.SetLine(Color.white, topRightBack, topRightForward));
                this._debugVectorLines.Add(VectorLine.SetLine(Color.white, bottomRightBack, bottomRightForward));
                this._debugVectorLines.Add(VectorLine.SetLine(Color.white, bottomLeftBack, bottomLeftForward));

                this._debugVectorLines.Add(VectorLine.SetLine(Color.red, Vector3.zero, Vector3.right * size * 2));
                this._debugVectorLines.Add(VectorLine.SetLine(Color.green, Vector3.zero, Vector3.up * size * 2));
                this._debugVectorLines.Add(VectorLine.SetLine(Color.Lerp(Color.blue, Color.cyan, 0.5f), Vector3.zero, Vector3.forward * size * 2));

                foreach (VectorLine line in this._debugVectorLines)
                {
                    line.lineWidth = 2f;
                    line.active = false;
                }
            }


            MoreAccessories._self._charaMakerGuideObject.onPositionDelta = this.OnGuideObjectPositionDelta;
            MoreAccessories._self._charaMakerGuideObject.onRotationDelta = this.OnGuideObjectRotationDelta;
            MoreAccessories._self._charaMakerGuideObject.onScaleDelta = this.OnGuideObjectScaleDelta;

            this.tab03.onValueChanged.AddListener((b) =>
            {
                MoreAccessories._self._charaMakerGuideObject.SetTransformTarget(this.tab03.isOn ? MoreAccessories._self._charaMakerAdditionalData.objAcsMove[this.GetSlotNoFromSubMenuSelect()]?.transform : null);
            });

            ((RectTransform)this.transform.FindChild("TabControl/TabItem03/Correct/Position")).anchoredPosition += new Vector2(0f, 10f);
            ((RectTransform)this.transform.FindChild("TabControl/TabItem03/Correct/Rotation")).anchoredPosition += new Vector2(0f, 14f);
            RectTransform scalingmenu = ((RectTransform)this.transform.FindChild("TabControl/TabItem03/Correct/Scaling"));
            scalingmenu.anchoredPosition += new Vector2(0f, 18f);

            RectTransform modes = UIUtility.CreateNewUIObject("GuideObject Modes", this.transform.FindChild("TabControl/TabItem03/Correct"));
            modes.SetRect(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(scalingmenu.offsetMin.x, scalingmenu.offsetMin.y - 24f), new Vector2(scalingmenu.offsetMax.x, scalingmenu.offsetMin.y - 2));
            ToggleGroup toggleGroup = modes.gameObject.AddComponent<ToggleGroup>();

            Text label = UIUtility.CreateText("Label", modes, "Guide Object");
            label.rectTransform.SetRect(Vector2.zero, new Vector2(0.25f, 1f));
            Toggle moveToggle = UIUtility.CreateToggle("Move Toggle", modes, "移動量");
            moveToggle.transform.SetRect(new Vector2(0.25f, 0f), new Vector2(0.5f, 1f));
            moveToggle.group = toggleGroup;
            moveToggle.onValueChanged.AddListener((b) =>
            {
                if (moveToggle.isOn)
                {
                    this._guideObjectMode = 0;
                    MoreAccessories._self._charaMakerGuideObject.SetMode(0);
                }
            });
            t = moveToggle.GetComponentInChildren<Text>();
            t.alignment = TextAnchor.MiddleLeft;
            t.color = Color.white;
            moveToggle.transform.Find("Background").GetComponent<Image>().sprite = toggle_b;
            ((Image)moveToggle.graphic).sprite = toggle_c;
            moveToggle.isOn = true;

            Toggle rotateToggle = UIUtility.CreateToggle("Rotate Toggle", modes, "回転量");
            rotateToggle.transform.SetRect(new Vector2(0.5f, 0f), new Vector2(0.75f, 1f));
            rotateToggle.group = toggleGroup;
            rotateToggle.onValueChanged.AddListener((b) =>
            {
                if (rotateToggle.isOn)
                {
                    this._guideObjectMode = 1;
                    MoreAccessories._self._charaMakerGuideObject.SetMode(1);                    
                }
            });
            t = rotateToggle.GetComponentInChildren<Text>();
            t.alignment = TextAnchor.MiddleLeft;
            t.color = Color.white;
            rotateToggle.transform.Find("Background").GetComponent<Image>().sprite = toggle_b;
            ((Image)rotateToggle.graphic).sprite = toggle_c;
            rotateToggle.isOn = false;

            Toggle scaleToggle = UIUtility.CreateToggle("Scale Toggle", modes, "拡縮量");
            scaleToggle.transform.SetRect(new Vector2(0.75f, 0f), Vector2.one);
            scaleToggle.group = toggleGroup;
            scaleToggle.onValueChanged.AddListener((b) =>
            {
                if (scaleToggle.isOn)
                {
                    this._guideObjectMode = 2;
                    MoreAccessories._self._charaMakerGuideObject.SetMode(2);
                }
            });
            t = scaleToggle.GetComponentInChildren<Text>();
            t.alignment = TextAnchor.MiddleLeft;
            t.color = Color.white;
            scaleToggle.transform.Find("Background").GetComponent<Image>().sprite = toggle_b;
            ((Image)scaleToggle.graphic).sprite = toggle_c;
            scaleToggle.isOn = false;

        }

        public virtual void Init()
        {
            Type type = Type.GetType("HSUS.HSUS,HSUS");
            if (type != null)
            {
                PropertyInfo propertyInfo = type.GetProperty("optimizeCharaMaker");
                if (propertyInfo != null)
                {
                    PropertyInfo selfProperty = type.GetProperty("self");
                    if (selfProperty != null && (bool)propertyInfo.GetValue(selfProperty.GetValue(null, null), null))
                    {
                        this._searchBar = (InputField)Type.GetType("HSUS.CharaMakerSearch,HSUS").CallPrivate("SpawnSearchBar", this.transform.Find("TabControl/TabItem01"), new UnityAction<string>(this.SearchChanged), -24f);
                        Type.GetType("HSUS.CharaMakerSort,HSUS").CallPrivate("SpawnSortButtons", this.transform.Find("TabControl/TabItem01"), new UnityAction(SortByName), new UnityAction(SortByCreationDate), new UnityAction(ResetSort));
                        Type.GetType("HSUS.CharaMakerCycleButtons,HSUS").CallPrivate("SpawnCycleButtons", this.transform.Find("TabControl/TabItem01"), new UnityAction(CycleUp), new UnityAction(CycleDown));
                        _scrollView = this.transform.Find("TabControl/TabItem01/ScrollView").GetComponent<ScrollRect>();

                    }
                }
            }

            {
                RectTransform parentCategory = this.transform.Find("TabControl/TabItem02/ParentCategory") as RectTransform;
                RectTransform defaultContainer = UIUtility.CreateNewUIObject("Default Container", parentCategory);
                defaultContainer.SetRect(0f, 1f, 1f, 1f, 0f, -196f, -4f, 0f);
                defaultContainer.gameObject.AddComponent<LayoutElement>().preferredHeight = 196f;
                int i = 0;
                while (parentCategory.childCount != 1)
                {
                    Transform child = parentCategory.GetChild(i);
                    if (child != defaultContainer)
                    {
                        child.SetParent(defaultContainer, true);
                        child.localPosition -= new Vector3(8f, 0f);
                    }
                    else
                        ++i;
                }

                ScrollRect scrollRect = UIUtility.CreateScrollView("ScrollView", parentCategory);
                scrollRect.transform.SetRect(defaultContainer);
                Destroy(scrollRect.GetComponent<Image>());
                scrollRect.scrollSensitivity *= 30f;

                foreach (Sprite s in Resources.FindObjectsOfTypeAll<Sprite>())
                {
                    switch (s.name)
                    {
                        case "rect_all":
                            scrollRect.verticalScrollbar.GetComponent<Image>().sprite = s;
                            break;
                        case "scl_handle":
                            scrollRect.verticalScrollbar.handleRect.GetComponent<Image>().sprite = s;
                            break;
                    }
                }

                defaultContainer.SetParent(scrollRect.content, true);
                VerticalLayoutGroup group = scrollRect.content.gameObject.AddComponent<VerticalLayoutGroup>();
                group.childForceExpandWidth = true;
                group.childForceExpandHeight = false;
                group.padding = new RectOffset(8, 0, 0, 0);
                group.spacing = 2f;
                scrollRect.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                RawImage separator = UIUtility.CreateRawImage("Separator", scrollRect.content);
                separator.gameObject.AddComponent<LayoutElement>().preferredHeight = 2f;

                List<string> moreAttachPointsPaths = Manager.Game.Instance.customSceneInfo.isFemale ? MoreAccessories._self._femaleMoreAttachPointsPaths : MoreAccessories._self._maleMoreAttachPointsPaths;
                Dictionary<string, string> moreAttachPointsAliases = Manager.Game.Instance.customSceneInfo.isFemale ? MoreAccessories._self._femaleMoreAttachPointsAliases : MoreAccessories._self._maleMoreAttachPointsAliases;
                for (int index = 0; index < moreAttachPointsPaths.Count; index++)
                {
                    string path = moreAttachPointsPaths[index];
                    Toggle toggle = GameObject.Instantiate(defaultContainer.GetChild(0).gameObject).GetComponent<Toggle>();
                    toggle.transform.SetParent(scrollRect.content);
                    toggle.isOn = false;
                    string n = moreAttachPointsAliases[path];
                    if (n.Length == 0)
                        n = this.chaBody.transform.Find(path).name;
                    toggle.GetComponentInChildren<Text>().text = n;
                    toggle.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                    toggle.transform.localScale *= 1.5f;
                    toggle.onValueChanged = new Toggle.ToggleEvent();
                    int index1 = index;
                    toggle.onValueChanged.AddListener((b) => this.OnChangeAccessoryParent(index1 + 29));
                    this.additionalTglParent.Add(toggle);
                    this.dictAdditionalParentKey.Add(path, index1 + 29);
                }
            }


            for (int i = 0; i < this.strParentKey.Length; i++)
                this.dictParentKey[this.strParentKey[i]] = i;
            GameObject gameObject = this.transform.FindLoop("TypeCategory");
            if (gameObject)
            {
                for (int j = 0; j < this.tglType.Length; j++)
                {
                    string name = "Cate" + j.ToString("00");
                    GameObject gameObject2 = gameObject.transform.FindLoop(name);
                    if (gameObject2)
                        this.tglType[j] = gameObject2.GetComponent<Toggle>();
                }
            }
            GameObject gameObject3 = this.transform.FindLoop("ParentCategory");
            if (gameObject3)
            {
                for (int k = 0; k < this.tglParent.Length; k++)
                {
                    string name2 = "Cate" + k.ToString("00");
                    GameObject gameObject4 = gameObject3.transform.FindLoop(name2);
                    if (gameObject4)
                        this.tglParent[k] = gameObject4.GetComponent<Toggle>();
                }
            }
            this.initEnd = true;
        }


        public virtual void OnEnableSetListAccessoryName()
        {
            int count = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory.Count + 10;
            for (int i = 0; i < count; i++)
            {
                string acsName;
                if (i < 10)
                    acsName = this.customControl.GetAcsName(i, 14, true, false);
                else
                    acsName = MoreAccessories._self.CustomControl_GetAcsName(i - 10, 14, true, false);
                this._dstCopy[i].text.text = acsName;
                this._srcCopy[i].text.text = acsName;
            }
        }

        public void UpdateUI()
        {
            MoreAccessories.CharAdditionalData additionalData = MoreAccessories._self._charaMakerAdditionalData;
            int i;
            for (i = 0; i < additionalData.clothesInfoAccessory.Count + 10; i++)
            {
                CopySlot dst;
                CopySlot src;
                if (i < this._dstCopy.Count)
                {
                    dst = this._dstCopy[i];
                    src = this._srcCopy[i];
                    dst.toggle.gameObject.SetActive(true);
                    src.toggle.gameObject.SetActive(true);
                }
                else
                {
                    dst = new CopySlot();
                    dst.toggle = Instantiate(this._dstCopy[0].toggle.gameObject).GetComponent<Toggle>();
                    dst.toggle.transform.SetParent(this._dstCopy[0].toggle.transform.parent, false);
                    dst.toggle.transform.SetAsLastSibling();
                    dst.toggle.isOn = false;
                    dst.text = dst.toggle.GetComponentInChildren<Text>();
                    dst.text.text = "アクセサリ" + (i + 1).ToString("00");
                    dst.toggle.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                    this._dstCopy.Add(dst);

                    src = new CopySlot();
                    src.toggle = Instantiate(this._srcCopy[0].toggle.gameObject).GetComponent<Toggle>();
                    src.toggle.transform.SetParent(this._srcCopy[0].toggle.transform.parent, false);
                    src.toggle.transform.SetAsLastSibling();
                    src.toggle.isOn = false;
                    src.text = src.toggle.GetComponentInChildren<Text>();
                    src.text.text = "アクセサリ" + (i + 1).ToString("00");
                    src.toggle.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                    this._srcCopy.Add(src);
                }
            }
            for (; i < this._dstCopy.Count; i++)
            {
                this._dstCopy[i].toggle.gameObject.SetActive(false);
                this._srcCopy[i].toggle.gameObject.SetActive(false);
            }
            this.OnEnableSetListAccessoryName();
        }

        public virtual int CheckDstSelectNo()
        {
            for (int i = 0; i < this._dstCopy.Count; i++)
                if (this._dstCopy[i].toggle.isOn)
                    return i;
            return 0;
        }

        public virtual int CheckSrcSelectNo()
        {
            for (int i = 0; i < this._srcCopy.Count; i++)
                if (this._srcCopy[i].toggle.isOn)
                    return i;
            return 0;
        }

        public virtual void OnCopyAll()
        {
            int num = this.CheckDstSelectNo();
            int num2 = this.CheckSrcSelectNo();
            if (num == num2)
                return;
            CharFileInfoClothes.Accessory src;
            if (num2 < 10)
                src = this.clothesInfo.accessory[num2];
            else
                src = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[num2 - 10];
            CharFileInfoClothes.Accessory dst;
            if (num < 10)
                dst = this.clothesInfo.accessory[num];
            else
                dst = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[num - 10];
            dst.Copy(src);
            if (this.tglReversal.isOn)
            {
                switch (dst.parentKey)
                {
                    case "AP_Earring_L":
                        dst.parentKey = "AP_Earring_R";
                        break;
                    case "AP_Earring_R":
                        dst.parentKey = "AP_Earring_L";
                        break;
                    case "AP_Tikubi_L":
                        dst.parentKey = "AP_Tikubi_R";
                        break;
                    case "AP_Tikubi_R":
                        dst.parentKey = "AP_Tikubi_L";
                        break;
                    case "AP_Shoulder_L":
                        dst.parentKey = "AP_Shoulder_R";
                        break;
                    case "AP_Shoulder_R":
                        dst.parentKey = "AP_Shoulder_L";
                        break;
                    case "AP_Arm_L":
                        dst.parentKey = "AP_Arm_R";
                        break;
                    case "AP_Arm_R":
                        dst.parentKey = "AP_Arm_L";
                        break;
                    case "AP_Wrist_L":
                        dst.parentKey = "AP_Wrist_R";
                        break;
                    case "AP_Wrist_R":
                        dst.parentKey = "AP_Wrist_L";
                        break;
                    case "AP_Hand_L":
                        dst.parentKey = "AP_Hand_R";
                        break;
                    case "AP_Hand_R":
                        dst.parentKey = "AP_Hand_L";
                        break;
                    case "AP_Index_L":
                        dst.parentKey = "AP_Index_R";
                        break;
                    case "AP_Index_R":
                        dst.parentKey = "AP_Index_L";
                        break;
                    case "AP_Middle_L":
                        dst.parentKey = "AP_Middle_R";
                        break;
                    case "AP_Middle_R":
                        dst.parentKey = "AP_Middle_L";
                        break;
                    case "AP_Ring_L":
                        dst.parentKey = "AP_Ring_R";
                        break;
                    case "AP_Ring_R":
                        dst.parentKey = "AP_Ring_L";
                        break;
                    case "AP_Leg_L":
                        dst.parentKey = "AP_Leg_R";
                        break;
                    case "AP_Leg_R":
                        dst.parentKey = "AP_Leg_L";
                        break;
                    case "AP_Ankle_L":
                        dst.parentKey = "AP_Ankle_R";
                        break;
                    case "AP_Ankle_R":
                        dst.parentKey = "AP_Ankle_L";
                        break;
                }
            }
            if (num < 10)
                this.chaInfo.chaBody.ChangeAccessory(num, dst.type, dst.id, dst.parentKey, true);
            else
                CharBody_ChangeAccessory_Patches.ChangeAccessoryAsync(this.chaInfo.chaBody, MoreAccessories._self._charaMakerAdditionalData, num - 10, dst.type, dst.id, dst.parentKey, true);

            this._original.customControl.UpdateAcsName();
            MoreAccessories._self.CustomControl_UpdateAcsName();
            this.OnEnableSetListAccessoryName();
            this.UpdateCharaInfoSub();

            if (num < 10)
            {
                CharFileInfoClothes info = this.coordinateInfo.GetInfo(this.statusInfo.coordinateType);
                info.accessory[num].Copy(this.clothesInfo.accessory[num]);
            }
        }

        public virtual void OnCopyCorrect()
        {
            int num = this.CheckDstSelectNo();
            int num2 = this.CheckSrcSelectNo();
            if (num == num2)
                return;

            CharFileInfoClothes.Accessory src;
            if (num2 < 10)
                src = this.clothesInfo.accessory[num2];
            else
                src = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[num2 - 10];
            CharFileInfoClothes.Accessory dst;
            if (num < 10)
                dst = this.clothesInfo.accessory[num];
            else
                dst = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[num - 10];

            dst.addPos = src.addPos;
            dst.addRot = src.addRot;
            dst.addScl = src.addScl;
            this.CharClothes_UpdateAccessoryMoveFromInfo(num);
            if (num < 10)
            {
                CharFileInfoClothes info = this.coordinateInfo.GetInfo(this.statusInfo.coordinateType);
                info.accessory[num].Copy(this.clothesInfo.accessory[num]);
            }
        }

        public virtual void OnCopyCorrectReversalLR()
        {
            int num = this.CheckDstSelectNo();
            int num2 = this.CheckSrcSelectNo();
            if (num == num2)
                return;

            CharFileInfoClothes.Accessory src;
            if (num2 < 10)
                src = this.clothesInfo.accessory[num2];
            else
                src = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[num2 - 10];
            CharFileInfoClothes.Accessory dst;
            if (num < 10)
                dst = this.clothesInfo.accessory[num];
            else
                dst = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[num - 10];

            dst.addPos = src.addPos;
            dst.addRot = src.addRot + new Vector3(0f, 180f, 0f);
            if (dst.addRot.y >= 360.0)
                dst.addRot.y -= 360f;
            dst.addScl = src.addScl;
            this.CharClothes_UpdateAccessoryMoveFromInfo(num);

            if (num < 10)
            {
                CharFileInfoClothes info = this.coordinateInfo.GetInfo(this.statusInfo.coordinateType);
                info.accessory[num].Copy(this.clothesInfo.accessory[num]);
            }
        }

        public virtual void OnCopyCorrectReversalTB()
        {
            int num = this.CheckDstSelectNo();
            int num2 = this.CheckSrcSelectNo();
            if (num == num2)
                return;

            CharFileInfoClothes.Accessory src;
            if (num2 < 10)
                src = this.clothesInfo.accessory[num2];
            else
                src = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[num2 - 10];
            CharFileInfoClothes.Accessory dst;
            if (num < 10)
                dst = this.clothesInfo.accessory[num];
            else
                dst = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[num - 10];

            dst.addPos = src.addPos;
            dst.addRot = src.addRot + new Vector3(180f, 0f, 0f);
            if (dst.addRot.x >= 360.0)
                dst.addRot.x -= 360f;
            dst.addScl = src.addScl;
            this.CharClothes_UpdateAccessoryMoveFromInfo(num);
            if (num < 10)
            {
                CharFileInfoClothes info = this.coordinateInfo.GetInfo(this.statusInfo.coordinateType);
                info.accessory[num].Copy(this.clothesInfo.accessory[num]);
            }
        }

        public virtual int GetParentIndexFromParentKey(string key)
        {
            return this.dictParentKey.TryGetValue(key, out int ret) ? ret : (this.dictAdditionalParentKey.TryGetValue(key, out ret) ? ret : 0);
        }

        public virtual int GetSlotNoFromSubMenuSelect()
        {
            return this.nowSubMenuTypeId - (int)SubMenuControl.SubMenuType.SM_Delete - 1;
        }

        public virtual void OnChangeAccessoryType(int newType)
        {
            int num = newType + 1;
            if (this.initFlags || null == this.chaInfo || null == this.grpType)
                return;
            Toggle toggle = this.tglType[num];
            if (null == toggle || !toggle.isOn)
                return;
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            if (MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].type == newType)
                return;
            this.nowLoading = true;
            this.ChangeAccessoryTypeList(newType, -1);
            this.nowLoading = false;
            CharBody_ChangeAccessory_Patches.ChangeAccessoryAsync(this.chaBody, MoreAccessories._self._charaMakerAdditionalData, slotNoFromSubMenuSelect, newType, this.firstIndex, string.Empty);
            MoreAccessories._self._charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].Copy(MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect]);
            this.UpdateShowTab();
            this.CharClothes_ResetAccessoryMove(slotNoFromSubMenuSelect);
            MoreAccessories._self.CustomControl_UpdateAcsName();

            this.UpdateDebugParentLines();
        }

        public virtual void UpdateOnEnableSelectParent()
        {
            if (this.clothesInfo == null)
                return;
            this.nowTglAllSet = true;
            for (int i = 0; i < this.tglParent.Length; i++)
                this.tglParent[i].isOn = false;
            foreach (Toggle toggle in this.additionalTglParent)
                toggle.isOn = false;
            this.nowTglAllSet = false;
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            int parentIndexFromParentKey = this.GetParentIndexFromParentKey(MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].parentKey);
            this.updateVisualOnly = true;
            if (parentIndexFromParentKey < 29)
                this.tglParent[parentIndexFromParentKey].isOn = true;
            else
                this.additionalTglParent[parentIndexFromParentKey - 29].isOn = true;
            this.updateVisualOnly = false;

            this.UpdateDebugParentLines();
        }

        public virtual void OnChangeAccessoryParentDefault()
        {
            if (this.clothesInfo == null)
                return;
            this.nowTglAllSet = true;
            for (int i = 0; i < this.tglParent.Length; i++)
                this.tglParent[i].isOn = false;
            foreach (Toggle toggle in this.additionalTglParent)
                toggle.isOn = false;
            this.nowTglAllSet = false;
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            int id = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].id;
            string accessoryDefaultParentStr = this.chaClothes.GetAccessoryDefaultParentStr(this.acsType, id);
            int parentIndexFromParentKey = this.GetParentIndexFromParentKey(accessoryDefaultParentStr);
            this.tglParent[parentIndexFromParentKey].isOn = true;

            this.UpdateDebugParentLines();
        }

        public virtual void OnChangeAccessoryParent(int newParent)
        {
            if (this.nowTglAllSet || this.updateVisualOnly || this.initFlags || null == this.chaInfo || null == this.grpParent)
                return;
            Toggle toggle;
            if (newParent < 29)
                toggle = this.tglParent[newParent];
            else
                toggle = this.additionalTglParent[newParent - 29];
            if (null == toggle || !toggle.isOn)
                return;
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            string parentKey = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].parentKey;
            string newParentKey;
            if (newParent < 29)
                newParentKey = this.strParentKey[newParent];
            else
                newParentKey = (Manager.Game.Instance.customSceneInfo.isFemale ? MoreAccessories._self._femaleMoreAttachPointsPaths : MoreAccessories._self._maleMoreAttachPointsPaths)[newParent - 29];
            if (parentKey == newParentKey)
                return;
            CharBody_ChangeAccessory_Patches.CharClothes_ChangeAccessoryParent(this.chaBody, MoreAccessories._self._charaMakerAdditionalData, slotNoFromSubMenuSelect, newParentKey);
            //MoreAccessories.self.charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].parentKey = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].parentKey;
            this.CharClothes_ResetAccessoryMove(slotNoFromSubMenuSelect);

            this.UpdateDebugParentLines();
        }

        public virtual void OnChangeIndex(GameObject objClick, bool value)
        {
            Toggle component = objClick.GetComponent<Toggle>();
            if (!component.isOn)
                return;
            FbxTypeInfo component2 = objClick.GetComponent<FbxTypeInfo>();
            if (null == component2)
                return;
            if (this.clothesInfo != null)
            {
                int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
                string parentKey = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].parentKey;
                int id = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].id;
                string accessoryDefaultParentStr = this.chaClothes.GetAccessoryDefaultParentStr(this.acsType, id);
                string parentKey2;
                string b = parentKey2 = this.chaClothes.GetAccessoryDefaultParentStr(this.acsType, component2.id);
                if (accessoryDefaultParentStr != parentKey && accessoryDefaultParentStr == b)
                    parentKey2 = parentKey;
                if (!this.nowLoading)
                {
                    CharBody_ChangeAccessory_Patches.ChangeAccessoryAsync(this.chaBody, MoreAccessories._self._charaMakerAdditionalData, slotNoFromSubMenuSelect, this.acsType, component2.id, parentKey2);
                    MoreAccessories._self._charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].Copy(MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect]);
                    this.UpdateShowTab();
                }
            }
            if (!this.chaInfo.customInfo.isConcierge && CharaListInfo.CheckCustomID(component2.info.Category, component2.info.Id) != 2)
            {
                CharaListInfo.AddCustomID(component2.info.Category, component2.info.Id, 2);
                Transform transform = objClick.transform.FindChild("imgNew");
                if (transform)
                    transform.gameObject.SetActive(false);
            }
            MoreAccessories._self.CustomControl_UpdateAcsName();
        }

        public virtual void OnClickColorDiffuse(int no)
        {
            if (!this.colorMenu)
                return;
            this.selectColorNo = no;
            this.colorMenu.updateColorFunc = this.UpdateColorDiffuse;
            Color white = Color.white;
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            if (this.selectColorNo == 0)
            {
                this.colorMenu.ChangeWindowTitle("[Accessory " + (11 + slotNoFromSubMenuSelect) + "] Color");
                white = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color.rgbDiffuse;
            }
            else
            {
                this.colorMenu.ChangeWindowTitle("[Accessory " + (11 + slotNoFromSubMenuSelect) + "] Shine Color");
                white = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color2.rgbDiffuse;
            }
            this.colorMenu.SetColor(white, UI_ColorInfo.ControlType.PresetsSample);
        }

        public virtual void UpdateColorDiffuse(Color color)
        {
            if (this.imgDiffuse[this.selectColorNo])
                this.imgDiffuse[this.selectColorNo].color = new Color(color.r, color.g, color.b);
            if (this.clothesInfo == null)
                return;
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            if (this.selectColorNo == 0)
            {
                MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color.SetDiffuseRGB(color);
                MoreAccessories._self._charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].color.SetDiffuseRGB(color);
            }
            else
            {
                MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color2.SetDiffuseRGB(color);
                MoreAccessories._self._charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].color2.SetDiffuseRGB(color);
            }
            CharBody_ChangeAccessory_Patches.CharClothes_ChangeAccessoryColor(MoreAccessories._self._charaMakerAdditionalData, slotNoFromSubMenuSelect);
        }

        public virtual void OnClickColorSpecular(int no)
        {
            if (!this.colorMenu)
                return;
            this.selectColorNo = no;
            this.colorMenu.updateColorFunc = this.UpdateColorSpecular;
            Color white = Color.white;
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            if (this.selectColorNo == 0)
            {
                this.colorMenu.ChangeWindowTitle("【アクセサリ(スロット" + (11 + slotNoFromSubMenuSelect) + ")】ツヤの色");
                white = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color.rgbSpecular;
            }
            else
            {
                this.colorMenu.ChangeWindowTitle("【アクセサリ(スロット" + (11 + slotNoFromSubMenuSelect) + ")】サブカラ\u30fcのツヤの色");
                white = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color2.rgbSpecular;
            }
            this.colorMenu.SetColor(white, UI_ColorInfo.ControlType.PresetsSample);
        }

        public virtual void UpdateColorSpecular(Color color)
        {
            if (this.imgSpecular[this.selectColorNo])
                this.imgSpecular[this.selectColorNo].color = new Color(color.r, color.g, color.b);
            if (this.clothesInfo == null)
                return;
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            if (this.selectColorNo == 0)
            {
                MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color.SetSpecularRGB(color);
                MoreAccessories._self._charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].color.SetSpecularRGB(color);
            }
            else
            {
                MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color2.SetSpecularRGB(color);
                MoreAccessories._self._charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].color2.SetSpecularRGB(color);
            }
            CharBody_ChangeAccessory_Patches.CharClothes_ChangeAccessoryColor(MoreAccessories._self._charaMakerAdditionalData, slotNoFromSubMenuSelect);
        }

        public virtual void OnValueChangeIntensity(float value)
        {
            if (this.nowChanging)
                return;
            if (this.clothesInfo != null)
            {
                int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
                MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color.specularIntensity = value;
                MoreAccessories._self._charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].color.specularIntensity = value;
                CharBody_ChangeAccessory_Patches.CharClothes_ChangeAccessoryColor(MoreAccessories._self._charaMakerAdditionalData, slotNoFromSubMenuSelect);
            }
            this.nowChanging = true;
            if (this.inputIntensity)
                this.inputIntensity.text = this.ChangeTextFromFloat(value);
            this.nowChanging = false;
        }

        public virtual void OnInputEndIntensity(string text)
        {
            if (this.nowChanging)
                return;
            float num = this.ChangeFloatFromText(ref text);
            if (this.clothesInfo != null)
            {
                float specularIntensity = num;
                int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
                MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color.specularIntensity = specularIntensity;
                MoreAccessories._self._charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].color.specularIntensity = specularIntensity;
                CharBody_ChangeAccessory_Patches.CharClothes_ChangeAccessoryColor(MoreAccessories._self._charaMakerAdditionalData, slotNoFromSubMenuSelect);
            }
            this.nowChanging = true;
            if (this.sldIntensity)
                this.sldIntensity.value = num;
            if (this.inputIntensity)
                this.inputIntensity.text = text;
            this.nowChanging = false;
        }

        public virtual void OnClickIntensity()
        {
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            float num = this.chaInfo.Sex != 0 ? this.defClothesInfoF.accessory[slotNoFromSubMenuSelect].color.specularIntensity : this.defClothesInfoM.accessory[slotNoFromSubMenuSelect].color.specularIntensity;
            MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color.specularIntensity = num;
            MoreAccessories._self._charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].color.specularIntensity = num;
            CharBody_ChangeAccessory_Patches.CharClothes_ChangeAccessoryColor(MoreAccessories._self._charaMakerAdditionalData, slotNoFromSubMenuSelect);
            this.nowChanging = true;
            float value = num;
            if (this.sldIntensity)
                this.sldIntensity.value = value;
            if (this.inputIntensity)
                this.inputIntensity.text = this.ChangeTextFromFloat(value);
            this.nowChanging = false;
        }

        public virtual void OnValueChangeSharpness(int no, float value)
        {
            if (this.nowChanging)
                return;
            if (this.clothesInfo != null)
            {
                int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
                if (no == 0)
                {
                    MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color.specularSharpness = value;
                    MoreAccessories._self._charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].color.specularSharpness = value;
                }
                else
                {
                    MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color2.specularSharpness = value;
                    MoreAccessories._self._charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].color2.specularSharpness = value;
                }
                CharBody_ChangeAccessory_Patches.CharClothes_ChangeAccessoryColor(MoreAccessories._self._charaMakerAdditionalData, slotNoFromSubMenuSelect);
            }
            this.nowChanging = true;
            if (this.inputSharpness[no])
                this.inputSharpness[no].text = this.ChangeTextFromFloat(value);
            this.nowChanging = false;
        }

        public virtual void OnInputEndSharpness(int no, string text)
        {
            if (this.nowChanging)
                return;
            float num = this.ChangeFloatFromText(ref text);
            if (this.clothesInfo != null)
            {
                float specularSharpness = num;
                int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
                if (no == 0)
                {
                    MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color.specularSharpness = specularSharpness;
                    MoreAccessories._self._charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].color.specularSharpness = specularSharpness;
                }
                else
                {
                    MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color2.specularSharpness = specularSharpness;
                    MoreAccessories._self._charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].color2.specularSharpness = specularSharpness;
                }
                CharBody_ChangeAccessory_Patches.CharClothes_ChangeAccessoryColor(MoreAccessories._self._charaMakerAdditionalData, slotNoFromSubMenuSelect);
            }
            this.nowChanging = true;
            if (this.sldSharpness[no])
                this.sldSharpness[no].value = num;
            if (this.inputSharpness[no])
                this.inputSharpness[no].text = text;
            this.nowChanging = false;
        }

        public virtual void OnClickSharpness(int no)
        {
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            float num = no != 0 ? MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color2.specularSharpness : MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color.specularSharpness;
               
            if (no == 0)
            {
                MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color.specularSharpness = num;
                MoreAccessories._self._charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].color.specularSharpness = num;
            }
            else
            {
                MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color2.specularSharpness = num;
                MoreAccessories._self._charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].color2.specularSharpness = num;
            }
            CharBody_ChangeAccessory_Patches.CharClothes_ChangeAccessoryColor(MoreAccessories._self._charaMakerAdditionalData, slotNoFromSubMenuSelect);
            this.nowChanging = true;
            float value = num;
            if (this.sldSharpness[no])
                this.sldSharpness[no].value = value;
            if (this.inputSharpness[no])
                this.inputSharpness[no].text = this.ChangeTextFromFloat(value);
            this.nowChanging = false;
        }

        public virtual void OnChangeModePos(UI_Parameter param)
        {
            if (!param)
                return;
            Toggle component = param.GetComponent<Toggle>();
            if (component && component.isOn)
                this.modePos = (byte)param.index;
        }

        public virtual void OnChangeModeRot(UI_Parameter param)
        {
            if (!param)
                return;
            Toggle component = param.GetComponent<Toggle>();
            if (component && component.isOn)
                this.modeRot = (byte)param.index;
        }

        public virtual void OnChangeModeScl(UI_Parameter param)
        {
            if (!param)
                return;
            Toggle component = param.GetComponent<Toggle>();
            if (component && component.isOn)
                this.modeScl = (byte)param.index;
        }

        public virtual string GetTextFormValue(float value, int keta = 0)
        {
            float num = value;
            for (int num2 = 0; num2 < keta; num2++)
                num = (float)(num * 10.0);
            num = Mathf.Round(num);
            float num3 = 1f;
            for (int num4 = 0; num4 < keta; num4++)
                num3 = (float)(num3 * 0.10000000149011612);
            num *= num3;
            switch (keta)
            {
                case 0:
                    return num.ToString();
                case 1:
                    return num.ToString("0.0");
                case 2:
                    return num.ToString("0.00");
                case 3:
                    return num.ToString("0.000");
                default:
                    return string.Empty;
            }
        }

        public virtual void OnPointerDownPos(int index)
        {
            this.OnClickPos(index);
            this.indexMovePos = index;
            this.flagMovePos = true;
            this.downTimeCnt = 0f;
            this.loopTimeCnt = 0f;
        }

        public virtual void OnPointerDownRot(int index)
        {
            this.OnClickRot(index);
            this.indexMoveRot = index;
            this.flagMoveRot = true;
            this.downTimeCnt = 0f;
            this.loopTimeCnt = 0f;
        }

        public virtual void OnPointerDownScl(int index)
        {
            this.OnClickScl(index);
            this.indexMoveScl = index;
            this.flagMoveScl = true;
            this.downTimeCnt = 0f;
            this.loopTimeCnt = 0f;
        }

        public virtual void OnPointerUp()
        {
            this.flagMovePos = false;
            this.flagMoveRot = false;
            this.flagMoveScl = false;
            this.downTimeCnt = 0f;
            this.loopTimeCnt = 0f;
        }

        public virtual void OnClickPos(int index)
        {
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            float num = (float)(this.movePosValue[this.modePos] * 0.0099999997764825821);
            switch (index)
            {
                case 0:
                    this.CharClothes_SetAccessoryPos(slotNoFromSubMenuSelect, (float)(0.0 - num), true, 1);
                    this.nowChanging = true;
                    float value = (float)(MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addPos.x * 100.0);
                    if (this.inputPosX)
                        this.inputPosX.text = this.GetTextFormValue(value, 1);
                    this.nowChanging = false;
                    break;
                case 1:
                    this.CharClothes_SetAccessoryPos(slotNoFromSubMenuSelect, num, true, 1);
                    this.nowChanging = true;
                    float value2 = (float)(MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addPos.x * 100.0);
                    if (this.inputPosX)
                        this.inputPosX.text = this.GetTextFormValue(value2, 1);
                    this.nowChanging = false;
                    break;
                case 2:
                    this.CharClothes_SetAccessoryPos(slotNoFromSubMenuSelect, (float)(0.0 - num), true, 2);
                    this.nowChanging = true;
                    float value3 = (float)(MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addPos.y * 100.0);
                    if (this.inputPosY)
                        this.inputPosY.text = this.GetTextFormValue(value3, 1);
                    this.nowChanging = false;
                    break;
                case 3:
                    this.CharClothes_SetAccessoryPos(slotNoFromSubMenuSelect, num, true, 2);
                    this.nowChanging = true;
                    float value4 = (float)(MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addPos.y * 100.0);
                    if (this.inputPosY)
                        this.inputPosY.text = this.GetTextFormValue(value4, 1);
                    this.nowChanging = false;
                    break;
                case 4:
                    this.CharClothes_SetAccessoryPos(slotNoFromSubMenuSelect, (float)(0.0 - num), true, 4);
                    this.nowChanging = true;
                    float value5 = (float)(MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addPos.z * 100.0);
                    if (this.inputPosZ)
                        this.inputPosZ.text = this.GetTextFormValue(value5, 1);
                    this.nowChanging = false;
                    break;
                case 5:
                    this.CharClothes_SetAccessoryPos(slotNoFromSubMenuSelect, num, true, 4);
                    this.nowChanging = true;
                    float value6 = (float)(MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addPos.z * 100.0);
                    if (this.inputPosZ)
                        this.inputPosZ.text = this.GetTextFormValue(value6, 1);
                    this.nowChanging = false;
                    break;
            }
            MoreAccessories._self._charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].addPos = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addPos;
        }

        public virtual void OnClickRot(int index)
        {
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            float num = this.moveRotValue[this.modeRot];
            switch (index)
            {
                case 0:
                    this.CharClothes_SetAccessoryRot(slotNoFromSubMenuSelect, (float)(0.0 - num), true, 1);
                    this.nowChanging = true;
                    float x = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addRot.x;
                    if (this.inputRotX)
                        this.inputRotX.text = this.GetTextFormValue(x);
                    this.nowChanging = false;
                    break;
                case 1:
                    this.CharClothes_SetAccessoryRot(slotNoFromSubMenuSelect, num, true, 1);
                    this.nowChanging = true;
                    float x2 = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addRot.x;
                    if (this.inputRotX)
                        this.inputRotX.text = this.GetTextFormValue(x2);
                    this.nowChanging = false;
                    break;
                case 2:
                    this.CharClothes_SetAccessoryRot(slotNoFromSubMenuSelect, (float)(0.0 - num), true, 2);
                    this.nowChanging = true;
                    float y = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addRot.y;
                    if (this.inputRotY)
                        this.inputRotY.text = this.GetTextFormValue(y);
                    this.nowChanging = false;
                    break;
                case 3:
                    this.CharClothes_SetAccessoryRot(slotNoFromSubMenuSelect, num, true, 2);
                    this.nowChanging = true;
                    float y2 = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addRot.y;
                    if (this.inputRotY)
                        this.inputRotY.text = this.GetTextFormValue(y2);
                    this.nowChanging = false;
                    break;
                case 4:
                    this.CharClothes_SetAccessoryRot(slotNoFromSubMenuSelect, (float)(0.0 - num), true, 4);
                    this.nowChanging = true;
                    float z = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addRot.z;
                    if (this.inputRotZ)
                        this.inputRotZ.text = this.GetTextFormValue(z);
                    this.nowChanging = false;
                    break;
                case 5:
                    this.CharClothes_SetAccessoryRot(slotNoFromSubMenuSelect, num, true, 4);
                    this.nowChanging = true;
                    float z2 = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addRot.z;
                    if (this.inputRotZ)
                        this.inputRotZ.text = this.GetTextFormValue(z2);
                    this.nowChanging = false;
                    break;
            }
            MoreAccessories._self._charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].addRot = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addRot;
        }

        public virtual void OnClickScl(int index)
        {
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            float num = this.moveSclValue[this.modeScl];
            switch (index)
            {
                case 0:
                    this.CharClothes_SetAccessoryScl(slotNoFromSubMenuSelect, (float)(0.0 - num), true, 1);
                    this.nowChanging = true;
                    float x = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addScl.x;
                    if (this.inputSclX)
                        this.inputSclX.text = this.GetTextFormValue(x, 2);
                    this.nowChanging = false;
                    break;
                case 1:
                    this.CharClothes_SetAccessoryScl(slotNoFromSubMenuSelect, num, true, 1);
                    this.nowChanging = true;
                    float x2 = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addScl.x;
                    if (this.inputSclX)
                        this.inputSclX.text = this.GetTextFormValue(x2, 2);
                    this.nowChanging = false;
                    break;
                case 2:
                    this.CharClothes_SetAccessoryScl(slotNoFromSubMenuSelect, (float)(0.0 - num), true, 2);
                    this.nowChanging = true;
                    float y = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addScl.y;
                    if (this.inputSclY)
                        this.inputSclY.text = this.GetTextFormValue(y, 2);
                    this.nowChanging = false;
                    break;
                case 3:
                    this.CharClothes_SetAccessoryScl(slotNoFromSubMenuSelect, num, true, 2);
                    this.nowChanging = true;
                    float y2 = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addScl.y;
                    if (this.inputSclY)
                        this.inputSclY.text = this.GetTextFormValue(y2, 2);
                    this.nowChanging = false;
                    break;
                case 4:
                    this.CharClothes_SetAccessoryScl(slotNoFromSubMenuSelect, (float)(0.0 - num), true, 4);
                    this.nowChanging = true;
                    float z = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addScl.z;
                    if (this.inputSclZ)
                        this.inputSclZ.text = this.GetTextFormValue(z, 2);
                    this.nowChanging = false;
                    break;
                case 5:
                    this.CharClothes_SetAccessoryScl(slotNoFromSubMenuSelect, num, true, 4);
                    this.nowChanging = true;
                    float z2 = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addScl.z;
                    if (this.inputSclZ)
                        this.inputSclZ.text = this.GetTextFormValue(z2, 2);
                    this.nowChanging = false;
                    break;
            }
            MoreAccessories._self._charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].addScl = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addScl;
        }

        public virtual void OnInputEndPos(int index)
        {
            if (this.nowChanging)
                return;
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            switch (index)
            {
                case 0:
                    float num = (float)(string.Empty != this.inputPosX.text ? (float)double.Parse(this.inputPosX.text) * 0.0099999997764825821 : 0.0);
                    this.CharClothes_SetAccessoryPos(slotNoFromSubMenuSelect, num, false, 1);
                    this.nowChanging = true;
                    float value = (float)(MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addPos.x * 100.0);
                    if (this.inputPosX)
                        this.inputPosX.text = this.GetTextFormValue(value, 1);
                    this.nowChanging = false;
                    break;
                case 1:
                    float num2 = (float)(string.Empty != this.inputPosY.text ? (float)double.Parse(this.inputPosY.text) * 0.0099999997764825821 : 0.0);
                    this.CharClothes_SetAccessoryPos(slotNoFromSubMenuSelect, num2, false, 2);
                    this.nowChanging = true;
                    float value2 = (float)(MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addPos.y * 100.0);
                    if (this.inputPosY)
                        this.inputPosY.text = this.GetTextFormValue(value2, 1);
                    this.nowChanging = false;
                    break;
                case 2:
                    float num3 = (float)(string.Empty != this.inputPosZ.text ? (float)double.Parse(this.inputPosZ.text) * 0.0099999997764825821 : 0.0);
                    this.CharClothes_SetAccessoryPos(slotNoFromSubMenuSelect, num3, false, 4);
                    this.nowChanging = true;
                    float value3 = (float)(MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addPos.z * 100.0);
                    if (this.inputPosZ)
                        this.inputPosZ.text = this.GetTextFormValue(value3, 1);
                    this.nowChanging = false;
                    break;
            }
            MoreAccessories._self._charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].addPos = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addPos;
        }

        public virtual void OnInputEndRot(int index)
        {
            if (this.nowChanging)
                return;
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            switch (index)
            {
                case 0:
                    float num = (float)(string.Empty != this.inputRotX.text ? (float)double.Parse(this.inputRotX.text) : 0.0);
                    this.CharClothes_SetAccessoryRot(slotNoFromSubMenuSelect, num, false, 1);
                    this.nowChanging = true;
                    float x = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addRot.x;
                    if (this.inputRotX)
                        this.inputRotX.text = this.GetTextFormValue(x);
                    this.nowChanging = false;
                    break;
                case 1:
                    float num2 = (float)(string.Empty != this.inputRotY.text ? (float)double.Parse(this.inputRotY.text) : 0.0);
                    this.CharClothes_SetAccessoryRot(slotNoFromSubMenuSelect, num2, false, 2);
                    this.nowChanging = true;
                    float y = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addRot.y;
                    if (this.inputRotY)
                        this.inputRotY.text = this.GetTextFormValue(y);
                    this.nowChanging = false;
                    break;
                case 2:
                    float num3 = (float)(string.Empty != this.inputRotZ.text ? (float)double.Parse(this.inputRotZ.text) : 0.0);
                    this.CharClothes_SetAccessoryRot(slotNoFromSubMenuSelect, num3, false, 4);
                    this.nowChanging = true;
                    float z = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addRot.z;
                    if (this.inputRotZ)
                        this.inputRotZ.text = this.GetTextFormValue(z);
                    this.nowChanging = false;
                    break;
            }
            MoreAccessories._self._charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].addRot = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addRot;
        }

        public virtual void OnInputEndScl(int index)
        {
            if (this.nowChanging)
                return;
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            switch (index)
            {
                case 0:
                    float num = (float)(string.Empty != this.inputSclX.text ? (float)double.Parse(this.inputSclX.text) : 0.0);
                    this.CharClothes_SetAccessoryScl(slotNoFromSubMenuSelect, num, false, 1);
                    this.nowChanging = true;
                    float x = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addScl.x;
                    if (this.inputSclX)
                        this.inputSclX.text = this.GetTextFormValue(x, 2);
                    this.nowChanging = false;
                    break;
                case 1:
                    float num2 = (float)(string.Empty != this.inputSclY.text ? (float)double.Parse(this.inputSclY.text) : 0.0);
                    this.CharClothes_SetAccessoryScl(slotNoFromSubMenuSelect, num2, false, 2);
                    this.nowChanging = true;
                    float y = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addScl.y;
                    if (this.inputSclY)
                        this.inputSclY.text = this.GetTextFormValue(y, 2);
                    this.nowChanging = false;
                    break;
                case 2:
                    float num3 = (float)(string.Empty != this.inputSclZ.text ? (float)double.Parse(this.inputSclZ.text) : 0.0);
                    this.CharClothes_SetAccessoryScl(slotNoFromSubMenuSelect, num3, false, 4);
                    this.nowChanging = true;
                    float z = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addScl.z;
                    if (this.inputSclZ)
                        this.inputSclZ.text = this.GetTextFormValue(z, 2);
                    this.nowChanging = false;
                    break;
            }
            MoreAccessories._self._charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].addScl = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addScl;
        }

        public virtual void OnClickResetPos(int index)
        {
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            switch (index)
            {
                case 0:
                    this.CharClothes_SetAccessoryPos(slotNoFromSubMenuSelect, 0f, false, 1);
                    this.nowChanging = true;
                    if (this.inputPosX)
                        this.inputPosX.text = "0.0";
                    this.nowChanging = false;
                    break;
                case 1:
                    this.CharClothes_SetAccessoryPos(slotNoFromSubMenuSelect, 0f, false, 2);
                    this.nowChanging = true;
                    if (this.inputPosY)
                        this.inputPosY.text = "0.0";
                    this.nowChanging = false;
                    break;
                case 2:
                    this.CharClothes_SetAccessoryPos(slotNoFromSubMenuSelect, 0f, false, 4);
                    this.nowChanging = true;
                    if (this.inputPosZ)
                        this.inputPosZ.text = "0.0";
                    this.nowChanging = false;
                    break;
            }
            MoreAccessories._self._charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].addPos = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addPos;
        }

        public virtual void OnClickResetRot(int index)
        {
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            switch (index)
            {
                case 0:
                    this.CharClothes_SetAccessoryRot(slotNoFromSubMenuSelect, 0f, false, 1);
                    this.nowChanging = true;
                    if (this.inputRotX)
                        this.inputRotX.text = "0";
                    this.nowChanging = false;
                    break;
                case 1:
                    this.CharClothes_SetAccessoryRot(slotNoFromSubMenuSelect, 0f, false, 2);
                    this.nowChanging = true;
                    if (this.inputRotY)
                        this.inputRotY.text = "0";
                    this.nowChanging = false;
                    break;
                case 2:
                    this.CharClothes_SetAccessoryRot(slotNoFromSubMenuSelect, 0f, false, 4);
                    this.nowChanging = true;
                    if (this.inputRotZ)
                        this.inputRotZ.text = "0";
                    this.nowChanging = false;
                    break;
            }
            MoreAccessories._self._charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].addRot = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addRot;
        }

        public virtual void OnClickResetScl(int index)
        {
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            switch (index)
            {
                case 0:
                    this.CharClothes_SetAccessoryScl(slotNoFromSubMenuSelect, 1f, false, 1);
                    this.nowChanging = true;
                    if (this.inputSclX)
                        this.inputSclX.text = "1.00";
                    this.nowChanging = false;
                    break;
                case 1:
                    this.CharClothes_SetAccessoryScl(slotNoFromSubMenuSelect, 1f, false, 2);
                    this.nowChanging = true;
                    if (this.inputSclY)
                        this.inputSclY.text = "1.00";
                    this.nowChanging = false;
                    break;
                case 2:
                    this.CharClothes_SetAccessoryScl(slotNoFromSubMenuSelect, 1f, false, 4);
                    this.nowChanging = true;
                    if (this.inputSclZ)
                        this.inputSclZ.text = "1.00";
                    this.nowChanging = false;
                    break;
            }
            MoreAccessories._self._charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].addScl = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addScl;
        }

        public virtual void MoveInfoAllSet()
        {
            if (this.clothesInfo == null)
                return;
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            this.nowChanging = true;
            float num = (float)(MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addPos.x * 100.0);
            if (this.inputPosX)
                this.inputPosX.text = this.GetTextFormValue(num, 1);
            num = (float)(MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addPos.y * 100.0);
            if (this.inputPosY)
                this.inputPosY.text = this.GetTextFormValue(num, 1);
            num = (float)(MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addPos.z * 100.0);
            if (this.inputPosZ)
                this.inputPosZ.text = this.GetTextFormValue(num, 1);
            num = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addRot.x;
            if (this.inputRotX)
                this.inputRotX.text = this.GetTextFormValue(num);
            num = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addRot.y;
            if (this.inputRotY)
                this.inputRotY.text = this.GetTextFormValue(num);
            num = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addRot.z;
            if (this.inputRotZ)
                this.inputRotZ.text = this.GetTextFormValue(num);
            num = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addScl.x;
            if (this.inputSclX)
                this.inputSclX.text = this.GetTextFormValue(num, 2);
            num = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addScl.y;
            if (this.inputSclY)
                this.inputSclY.text = this.GetTextFormValue(num, 2);
            num = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addScl.z;
            if (this.inputSclZ)
                this.inputSclZ.text = this.GetTextFormValue(num, 2);
            this.nowChanging = false;
            MoreAccessories._self._charaMakerGuideObject.SetTransformTarget(this.tab03.gameObject.activeInHierarchy && this.tab03.isOn ? MoreAccessories._self._charaMakerAdditionalData.objAcsMove[slotNoFromSubMenuSelect]?.transform : null);
        }

        public virtual void UpdateShowTab()
        {
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            if (this.tab02)
                this.tab02.gameObject.SetActive(false);
            if (this.tab03)
                this.tab03.gameObject.SetActive(false);
            if (this.tab04)
                this.tab04.gameObject.SetActive(false);
            bool flag = false;
            
            GameObject exists = MoreAccessories._self._charaMakerAdditionalData.objAccessory[slotNoFromSubMenuSelect];
            if (exists)
            {
                ListTypeFbxComponent component = MoreAccessories._self._charaMakerAdditionalData.objAccessory[slotNoFromSubMenuSelect].GetComponent<ListTypeFbxComponent>();
                if (component && "0" == component.ltfData.Parent)
                    flag = true;
            }
            else
                flag = true;
            if (this.tab02)
                this.tab02.gameObject.SetActive(!flag);
            if (this.tab03)
                this.tab03.gameObject.SetActive(!flag);
            this.lstTagColor.Clear();
            this.lstTagColor = CharBody_ChangeAccessory_Patches.CharInfo_GetTagInfo(MoreAccessories._self._charaMakerAdditionalData, slotNoFromSubMenuSelect);
            if (this.tab04)
                this.tab04.gameObject.SetActive((byte)(this.lstTagColor.Count != 0 ? 1 : 0) != 0);
            bool active = ColorChange.CheckChangeSubColor(this.lstTagColor);
            if (this.objSubColor)
                this.objSubColor.SetActive(active);
            MoreAccessories._self._charaMakerGuideObject.SetTransformTarget(this.tab03.gameObject.activeInHierarchy && this.tab03.isOn ? MoreAccessories._self._charaMakerAdditionalData.objAcsMove[slotNoFromSubMenuSelect]?.transform : null);
        }

        public override void SetCharaInfoSub()
        {
            if (!this.initEnd)
                this.Init();
            if (null == this.chaInfo)
                return;
            this.initFlags = true;
            if (null != this.tglTab)
                this.tglTab.isOn = true;
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            int num = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].type + 1;
            this.nowTglAllSet = true;
            for (int i = 0; i < this.tglType.Length; i++)
                this.tglType[i].isOn = false;
            this.nowTglAllSet = false;
            this.tglType[num].isOn = true;
            this.ChangeAccessoryTypeList(MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].type, MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].id);
            this.UpdateShowTab();
            this.MoveInfoAllSet();

            MoreAccessories._self._charaMakerGuideObject.SetTransformTarget(this.tab03.gameObject.activeInHierarchy && this.tab03.isOn ? MoreAccessories._self._charaMakerAdditionalData.objAcsMove[slotNoFromSubMenuSelect]?.transform : null);
            this.UpdateDebugParentLines();
            this.initFlags = false;
        }

        public virtual void ChangeAccessoryTypeList(int newType, int newId)
        {
            this.acsType = newType;
            if (this.chaInfo == null || this.objListTop == null || this.objLineBase == null || this.rtfPanel == null)
                return;
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            int count = 0;
            int selectedIndex = 0;
            TypeData td;
            if (this._lastType != this.nowSubMenuTypeId && this._types.TryGetValue(this._lastType, out td))
                td.parentObject.gameObject.SetActive(false);
            if (newType == -1)
            {
                this.rtfPanel.sizeDelta = new Vector2(this.rtfPanel.sizeDelta.x, 0f);
                this.rtfPanel.anchoredPosition = new Vector2(0f, 0f);
                if (this.tab02)
                    this.tab02.gameObject.SetActive(false);
                if (this.tab03)
                    this.tab03.gameObject.SetActive(false);
                if (this.tab04)
                    this.tab04.gameObject.SetActive(false);
            }
            else
            {
                if (this._types.TryGetValue(newType, out td) == false)
                {
                    td = new TypeData();
                    td.parentObject = new GameObject("Type " + newType, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(ToggleGroup)).GetComponent<RectTransform>();
                    td.parentObject.SetParent(this.objListTop.transform, false);
                    td.parentObject.localScale = Vector3.one;
                    td.parentObject.localPosition = Vector3.zero;
                    td.parentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    this._types.Add(newType, td);
                    ToggleGroup group = td.parentObject.GetComponent<ToggleGroup>();
                    td.parentObject.gameObject.SetActive(false);

                    Dictionary<int, ListTypeFbx> dictionary = null;
                    CharaListInfo.TypeAccessoryFbx type = (CharaListInfo.TypeAccessoryFbx)((int)Enum.ToObject(typeof(CharaListInfo.TypeAccessoryFbx), newType));
                    dictionary = this.chaInfo.ListInfo.GetAccessoryFbxList(type, true);
                    count = 0;
                    foreach (KeyValuePair<int, ListTypeFbx> current in dictionary)
                    {
                        bool flag = false;
                        if (this.chaInfo.customInfo.isConcierge)
                            flag = CharaListInfo.CheckSitriClothesID(current.Value.Category, current.Value.Id);
                        if (CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id) == 0 && !flag)
                            continue;
                        if (this.chaInfo.Sex == 0)
                        {
                            if ("0" == current.Value.PrefabM)
                                continue;
                        }
                        else if ("0" == current.Value.PrefabF)
                            continue;
                        if (count == 0)
                            this.SetPrivate("firstIndex", current.Key);
                        GameObject gameObject = GameObject.Instantiate(this.objLineBase);
                        gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                        FbxTypeInfo fbxTypeInfo = gameObject.AddComponent<FbxTypeInfo>();
                        fbxTypeInfo.id = current.Key;
                        fbxTypeInfo.typeName = current.Value.Name;
                        fbxTypeInfo.info = current.Value;
                        gameObject.transform.SetParent(td.parentObject, false);
                        RectTransform rectTransform = gameObject.transform as RectTransform;
                        rectTransform.localScale = Vector3.one;
                        Text component = rectTransform.FindChild("Label").GetComponent<Text>();
                        component.text = fbxTypeInfo.typeName;

                        this.SetButtonClickHandler(gameObject);
                        Toggle component2 = gameObject.GetComponent<Toggle>();
                        td.keyToOriginalIndex.Add(current.Key, count);
                        td.objects.Add(new ObjectData {obj = gameObject, key = current.Key, toggle = component2, text = component, creationDate = File.GetCreationTimeUtc("./abdata/" + fbxTypeInfo.info.ABPath)});
                        component2.onValueChanged.AddListener(v =>
                        {
                            if (component2.isOn)
                                UnityEngine.Debug.Log(fbxTypeInfo.info.Id + " " + fbxTypeInfo.info.ABPath);
                        });
                        component2.group = group;
                        gameObject.SetActive(true);
                        if (!flag)
                        {
                            int num3 = CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id);
                            Transform transform = rectTransform.FindChild("imgNew");
                            if (transform && num3 == 1)
                                transform.gameObject.SetActive(true);
                        }
                        count++;
                    }
                }

                td.parentObject.gameObject.SetActive(true);
                foreach (ObjectData o in td.objects)
                {
                    if (count == 0)
                        this.SetPrivate("firstIndex", o.key);
                    if (newId == -1)
                        o.toggle.isOn = count == 0;
                    else if (o.key == newId)
                        o.toggle.isOn = true;
                    else
                        o.toggle.isOn = false;
                    if (o.toggle.isOn)
                    {
                        selectedIndex = count;
                        o.toggle.onValueChanged.Invoke(true);
                    }
                    ++count;
                }

                float b = 24f * count - 168f;
                float y = Mathf.Min(24f * selectedIndex, b);
                this.rtfPanel.anchoredPosition = new Vector2(0f, y);

                if (this.tab02)
                    this.tab02.gameObject.SetActive(true);
                if (this.tab03)
                    this.tab03.gameObject.SetActive(true);
            }
            this.nowChanging = true;
            if (this.clothesInfo != null)
            {
                float specularIntensity = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color.specularIntensity;
                float specularSharpness = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color.specularSharpness;
                float specularSharpness2 = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color2.specularSharpness;
                if (this.sldIntensity)
                    this.sldIntensity.value = specularIntensity;
                if (this.inputIntensity)
                    this.inputIntensity.text = this.ChangeTextFromFloat(specularIntensity);
                if (this.sldSharpness[0])
                    this.sldSharpness[0].value = specularSharpness;
                if (this.inputSharpness[0])
                    this.inputSharpness[0].text = this.ChangeTextFromFloat(specularSharpness);
                if (this.sldSharpness[1])
                    this.sldSharpness[1].value = specularSharpness2;
                if (this.inputSharpness[1])
                    this.inputSharpness[1].text = this.ChangeTextFromFloat(specularSharpness2);
            }
            this.SetPrivate("nowChanging", false);
            this.OnClickColorSpecular(1);
            this.OnClickColorSpecular(0);
            this.OnClickColorDiffuse(1);
            this.OnClickColorDiffuse(0);
            this._lastType = newType;
            this._lastId = newId;
        }

        public override void UpdateCharaInfoSub()
        {
            if (null == this.chaInfo)
                return;
            this.initFlags = true;
            if (null != this.tab05)
                this.tab05.isOn = true;
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            int num = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].type + 1;
            this.nowTglAllSet = true;
            for (int i = 0; i < this.tglType.Length; i++)
                this.tglType[i].isOn = false;
            this.nowTglAllSet = false;
            this.tglType[num].isOn = true;
            this.ChangeAccessoryTypeList(MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].type, MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].id);
            this.UpdateShowTab();
            this.MoveInfoAllSet();
            this.initFlags = false;
            MoreAccessories._self._charaMakerGuideObject.SetTransformTarget(this.tab03.gameObject.activeInHierarchy && this.tab03.isOn ? MoreAccessories._self._charaMakerAdditionalData.objAcsMove[slotNoFromSubMenuSelect]?.transform : null);
        }

        public virtual void SetButtonClickHandler(GameObject clickObj)
        {
            ButtonPlaySE buttonPlaySE = clickObj.AddComponent<ButtonPlaySE>();
            if (buttonPlaySE)
            {
                buttonPlaySE._Type = ButtonPlaySE.Type.Click;
                buttonPlaySE._SE = ButtonPlaySE.SE.sel;
            }
            Toggle component = clickObj.GetComponent<Toggle>();
            component.onValueChanged.AddListener(delegate(bool value)
            {
                this.OnChangeIndex(clickObj, value);
            });
        }

        public virtual void SetChangeValueSharpnessHandler(Slider sld, int no)
        {
            if (!(null == sld))
            {
                sld.onValueChanged.AddListener(delegate(float value)
                {
                    this.OnValueChangeSharpness(no, value);
                });
            }
        }

        public virtual void SetInputEndSharpnessHandler(InputField inp, int no)
        {
            if (!(null == inp))
            {
                inp.onEndEdit.AddListener(delegate(string value)
                {
                    this.OnInputEndSharpness(no, value);
                });
            }
        }
        #endregion

        #region Private Methods
        private void UpdateDebugParentLines()
        {
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            GameObject obj = MoreAccessories._self._charaMakerAdditionalData.objAccessory[slotNoFromSubMenuSelect];
            
            Transform t = null;
            if (obj != null)
                t = obj.transform;
            foreach (VectorLine line in this._debugVectorLines)
            {
                line.drawTransform = t;
            }
        }

        private void DebugParentChanged(bool b)
        {
            foreach (VectorLine line in this._debugVectorLines)
            {
                line.active = this._debugParentToggle.isOn;
                line.Draw();
            }
        }

        private void OnGuideObjectPositionDelta(Vector3 deltaPos)
        {
            int index = this.GetSlotNoFromSubMenuSelect();
            GameObject target = MoreAccessories._self._charaMakerAdditionalData.objAcsMove[index];
            CharFileInfoClothes.Accessory targetAccessory = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[index];
            target.transform.localPosition += deltaPos;
            targetAccessory.addPos = target.transform.localPosition;


            float num = (float)(targetAccessory.addPos.x * 100f);
            if (this.inputPosX)
                this.inputPosX.text = this.GetTextFormValue(num, 1);
            num = (float)(targetAccessory.addPos.y * 100f);
            if (this.inputPosY)
                this.inputPosY.text = this.GetTextFormValue(num, 1);
            num = (float)(targetAccessory.addPos.z * 100f);
            if (this.inputPosZ)
                this.inputPosZ.text = this.GetTextFormValue(num, 1);
        }

        private void OnGuideObjectRotationDelta(Vector3 deltaRot)
        {
            int index = this.GetSlotNoFromSubMenuSelect();
            GameObject target = MoreAccessories._self._charaMakerAdditionalData.objAcsMove[index];
            CharFileInfoClothes.Accessory targetAccessory = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[index];
            target.transform.localRotation *= Quaternion.Euler(deltaRot);
            targetAccessory.addRot = target.transform.localEulerAngles;

            float num = targetAccessory.addRot.x;
            if (this.inputRotX)
                this.inputRotX.text = this.GetTextFormValue(num);
            num = targetAccessory.addRot.y;
            if (this.inputRotY)
                this.inputRotY.text = this.GetTextFormValue(num);
            num = targetAccessory.addRot.z;
            if (this.inputRotZ)
                this.inputRotZ.text = this.GetTextFormValue(num);
        }

        private void OnGuideObjectScaleDelta(Vector3 deltaScale)
        {
            int index = this.GetSlotNoFromSubMenuSelect();
            GameObject target = MoreAccessories._self._charaMakerAdditionalData.objAcsMove[index];
            CharFileInfoClothes.Accessory targetAccessory = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[index];
            target.transform.localScale += deltaScale;
            targetAccessory.addScl = target.transform.localScale;

            float num = targetAccessory.addScl.x;
            if (this.inputSclX)
                this.inputSclX.text = this.GetTextFormValue(num, 2);
            num = targetAccessory.addScl.y;
            if (this.inputSclY)
                this.inputSclY.text = this.GetTextFormValue(num, 2);
            num = targetAccessory.addScl.z;
            if (this.inputSclZ)
                this.inputSclZ.text = this.GetTextFormValue(num, 2);
        }

        public void SearchChanged(string arg0)
        {
            string search = this._searchBar.text.Trim();
            if (this._types.ContainsKey(this._lastType) == false)
                return;
            foreach (ObjectData objectData in this._types[this._lastType].objects)
            {
                bool active = objectData.obj.activeSelf;
                ToggleGroup group = objectData.toggle.group;
                objectData.obj.SetActive(objectData.text.text.IndexOf(search, StringComparison.OrdinalIgnoreCase) != -1);
                if (active && objectData.obj.activeSelf == false)
                    group.RegisterToggle(objectData.toggle);
            }
        }

        private void ResetSort()
        {
            TypeData data;
            if (this._types.TryGetValue(this._lastType, out data) == false)
                return;
            GenericIntSort(data.objects, objectData => data.keyToOriginalIndex[objectData.key], objectData => objectData.obj);
        }

        private void SortByName()
        {
            TypeData data;
            if (this._types.TryGetValue(this._lastType, out data) == false)
                return;
            data.lastSortByNameReverse = !data.lastSortByNameReverse;
            GenericStringSort(data.objects, objectData => objectData.text.text, objectData => objectData.obj, data.lastSortByNameReverse);
        }

        private void SortByCreationDate()
        {
            TypeData data;
            if (this._types.TryGetValue(this._lastType, out data) == false)
                return;
            data.lastSortByCreationDateReverse = !data.lastSortByCreationDateReverse;
            GenericDateSort(data.objects, objectData => objectData.creationDate, objectData => objectData.obj, data.lastSortByCreationDateReverse);
        }

        private void CycleUp()
        {
            TypeData data;
            if (this._types.TryGetValue(this._lastType, out data) == false)
                return;
            Toggle lastToggle = null;
            foreach (ObjectData objectData in data.objects)
            {
                if (objectData.obj.activeSelf == false && objectData.toggle.isOn == false)
                    continue;
                if (objectData.toggle.isOn && lastToggle != null)
                {
                    objectData.toggle.isOn = false;
                    lastToggle.isOn = true;
                    if (_scrollView.normalizedPosition.y > 0.0001f || _scrollView.transform.InverseTransformPoint(lastToggle.transform.position).y > 0f)
                        _scrollView.content.anchoredPosition = (Vector2)_scrollView.transform.InverseTransformPoint(_scrollView.content.position) - (Vector2)_scrollView.transform.InverseTransformPoint(lastToggle.transform.position);
                    break;
                }
                lastToggle = objectData.toggle;
            }
        }

        private void CycleDown()
        {
            TypeData data;
            if (this._types.TryGetValue(this._lastType, out data) == false)
                return;
            Toggle lastToggle = null;
            foreach (ObjectData objectData in data.objects)
            {
                if (objectData.obj.activeSelf == false && objectData.toggle.isOn == false)
                    continue;
                if (lastToggle != null && lastToggle.isOn)
                {
                    lastToggle.isOn = false;
                    objectData.toggle.isOn = true;
                    if (_scrollView.normalizedPosition.y > 0.0001f)
                        _scrollView.content.anchoredPosition = (Vector2)_scrollView.transform.InverseTransformPoint(_scrollView.content.position) - (Vector2)_scrollView.transform.InverseTransformPoint(objectData.toggle.transform.position);
                    break;
                }
                lastToggle = objectData.toggle;
            }
        }
        public void ResetSearch()
        {
            InputField.OnChangeEvent searchEvent = this._searchBar.onValueChanged;
            this._searchBar.onValueChanged = null;
            this._searchBar.text = "";
            this._searchBar.onValueChanged = searchEvent;
            if (this._types.ContainsKey(this._lastType) == false)
                return;
            foreach (ObjectData objectData in this._types[this._lastType].objects)
                objectData.obj.SetActive(true);
        }

        private bool CharClothes_ResetAccessoryMove(int slotNo, int type = 7)
        {
            bool flag = true;
            if ((type & 1) != 0)
                flag &= this.CharClothes_SetAccessoryPos(slotNo, 0.0f, false, 7);
            if ((type & 2) != 0)
                flag &= this.CharClothes_SetAccessoryRot(slotNo, 0.0f, false, 7);
            if ((type & 4) != 0)
                flag &= this.CharClothes_SetAccessoryScl(slotNo, 1f, false, 7);
            return flag;
        }

        public bool CharClothes_SetAccessoryPos(int slotNo, float value, bool _add, int flags = 7)
        {
            GameObject gameObject = MoreAccessories._self._charaMakerAdditionalData.objAcsMove[slotNo];
            if (null == gameObject)
                return false;
            if ((flags & 1) != 0)
            {
                float num = (!_add ? 0.0f : MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNo].addPos.x) + value;
                MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNo].addPos.x = Mathf.Clamp(num, -1f, 1f);
            }
            if ((flags & 2) != 0)
            {
                float num = (!_add ? 0.0f : MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNo].addPos.y) + value;
                MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNo].addPos.y = Mathf.Clamp(num, -1f, 1f);
            }
            if ((flags & 4) != 0)
            {
                float num = (!_add ? 0.0f : MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNo].addPos.z) + value;
                MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNo].addPos.z = Mathf.Clamp(num, -1f, 1f);
            }
            gameObject.transform.SetLocalPosition(MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNo].addPos.x, MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNo].addPos.y, MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNo].addPos.z);
            return true;
        }

        private bool CharClothes_SetAccessoryRot(int slotNo, float value, bool _add, int flags = 7)
        {
            GameObject gameObject = MoreAccessories._self._charaMakerAdditionalData.objAcsMove[slotNo];
            if (null == gameObject)
                return false;
            if ((flags & 1) != 0)
            {
                float t = (!_add ? 0.0f : MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNo].addRot.x) + value;
                MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNo].addRot.x = Mathf.Repeat(t, 360f);
            }
            if ((flags & 2) != 0)
            {
                float t = (!_add ? 0.0f : MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNo].addRot.y) + value;
                MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNo].addRot.y = Mathf.Repeat(t, 360f);
            }
            if ((flags & 4) != 0)
            {
                float t = (!_add ? 0.0f : MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNo].addRot.z) + value;
                MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNo].addRot.z = Mathf.Repeat(t, 360f);
            }
            gameObject.transform.SetLocalRotation(MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNo].addRot.x, MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNo].addRot.y, MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNo].addRot.z);
            return true;
        }

        private bool CharClothes_SetAccessoryScl(int slotNo, float value, bool _add, int flags = 7)
        {
            GameObject gameObject = MoreAccessories._self._charaMakerAdditionalData.objAcsMove[slotNo];
            if (null == gameObject)
                return false;
            if ((flags & 1) != 0)
            {
                float num = (!_add ? 0.0f : MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNo].addScl.x) + value;
                MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNo].addScl.x = num;
            }
            if ((flags & 2) != 0)
            {
                float num = (!_add ? 0.0f : MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNo].addScl.y) + value;
                MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNo].addScl.y = num;
            }
            if ((flags & 4) != 0)
            {
                float num = (!_add ? 0.0f : MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNo].addScl.z) + value;
                MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNo].addScl.z = num;
            }
            gameObject.transform.SetLocalScale(MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNo].addScl.x, MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNo].addScl.y, MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNo].addScl.z);
            return true;
        }

        private bool CharClothes_UpdateAccessoryMoveFromInfo(int slotNo)
        {
            GameObject gameObject;
            if (slotNo < 10)
                gameObject = this.chaInfo.chaBody.objAcsMove[slotNo];
            else
                gameObject = MoreAccessories._self._charaMakerAdditionalData.objAcsMove[slotNo - 10];
            if (null == gameObject)
                return false;
            CharFileInfoClothes.Accessory accessory;
            if (slotNo < 10)
                accessory = this.clothesInfo.accessory[slotNo];
            else
                accessory = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[slotNo - 10];
            gameObject.transform.SetLocalPosition(accessory.addPos.x, accessory.addPos.y, accessory.addPos.z);
            gameObject.transform.SetLocalRotation(accessory.addRot.x, accessory.addRot.y, accessory.addRot.z);
            gameObject.transform.SetLocalScale(accessory.addScl.x, accessory.addScl.y, accessory.addScl.z);
            return true;
        }

        internal static void GenericIntSort<T>(List<T> list, Func<T, int> getIntFunc, Func<T, GameObject> getGameObjectFunc, bool reverse = false)
        {
            list.Sort((x, y) => reverse ? getIntFunc(y).CompareTo(getIntFunc(x)) : getIntFunc(x).CompareTo(getIntFunc(y)));
            foreach (T elem in list)
                getGameObjectFunc(elem).transform.SetAsLastSibling();
        }

        internal static void GenericStringSort<T>(List<T> list, Func<T, string> getStringFunc, Func<T, GameObject> getGameObjectFunc, bool reverse = false)
        {
            list.Sort((x, y) => reverse ? string.Compare(getStringFunc(y), getStringFunc(x), StringComparison.CurrentCultureIgnoreCase) : string.Compare(getStringFunc(x), getStringFunc(y), StringComparison.CurrentCultureIgnoreCase));
            foreach (T elem in list)
                getGameObjectFunc(elem).transform.SetAsLastSibling();
        }

        internal static void GenericDateSort<T>(List<T> list, Func<T, DateTime> getDateFunc, Func<T, GameObject> getGameObjectFunc, bool reverse = false)
        {
            list.Sort((x, y) => reverse ? getDateFunc(y).CompareTo(getDateFunc(x)) : getDateFunc(x).CompareTo(getDateFunc(y)));
            foreach (T elem in list)
                getGameObjectFunc(elem).transform.SetAsLastSibling();
        }
        #endregion
    }
}