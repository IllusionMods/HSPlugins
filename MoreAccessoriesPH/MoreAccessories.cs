using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using BepInEx;
using Character;
using ExtensibleSaveFormat;
using HarmonyLib;
using MoreAccessoriesPH.Patches;
using Studio;
using ToolBox;
using ToolBox.Extensions;
using UILib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MoreAccessoriesPH
{
    [BepInPlugin(_guid, _name, _version)]
    [BepInDependency("com.bepis.bepinex.extendedsave")]
    public class MoreAccessories : GenericPlugin
    {
        #region Types
        public class AdditionalData
        {
            public class AccessoryData
            {
                public AccessoryCustom accessoryCustom;
                public object acceObj;
                public bool show = true;

                public AccessoryData() { }

                public AccessoryData(AccessoryData other)
                {
                    this.accessoryCustom = new AccessoryCustom(other.accessoryCustom);
                    this.show = other.show;
                }

            }

            public List<AccessoryData> accessories = new List<AccessoryData>();

            public AdditionalData()
            {

            }

            public AdditionalData(AdditionalData other)
            {
                this.LoadFrom(other);
            }

            public void LoadFrom(AdditionalData other)
            {
                this.Clear();
                foreach (AccessoryData data in other.accessories)
                    this.accessories.Add(new AccessoryData(data));
            }

            public void Clear()
            {
                foreach (AccessoryData d in this.accessories)
                {
                    if (d.acceObj != null)
                    {
                        Destroy((GameObject)d.acceObj.GetPrivate("obj"));
                        d.acceObj = null;
                    }
                }
                this.accessories.Clear();
            }
        }

        internal class MakerSlot
        {
            public ToggleButton slotButton;
            public Text accessoryText;
            public Text textOn;
            public Text textOff;

            public RectTransform panel;
            public DropDownUI typeDropDown;
            public ItemSelectUISets itemSelectedSet;
            public DropDownUI parentDropDown;
            public Toggle posEditSwitch;
            public ColorChangeButton mainColor;
            public ColorChangeButton mainSpecColor;
            public InputSliderUI mainSpecular;
            public InputSliderUI mainSmooth;
            public ColorChangeButton subColor;
            public ColorChangeButton subSpecColor;
            public InputSliderUI subSpecular;
            public InputSliderUI subSmooth;

            public Toggle copySrcToggle;
            public Toggle copyDstToggle;
            public Text copySrcText;
            public Text copyDstText;
        }

        internal class StudioSlot
        {
            public GameObject slot;
            public Text slotText;
            public Button onButton;
            public Button offButton;
        }
        #endregion

        #region Private Variables
        private const string _name = "MoreAccessories";
        private const string _version = "1.0.2";
        private const string _guid = "com.joan6694.illusionplugins.moreaccessories";
        private const string _extSaveKey = "moreAccessories";
        private const int _saveVersion = 0;

        internal static MoreAccessories _self;
        internal readonly Dictionary<CustomParameter, AdditionalData> _charAdditionalData = new Dictionary<CustomParameter, AdditionalData>();
        private readonly Dictionary<CustomParameter, Human> _humansByCustomParameter = new Dictionary<CustomParameter, Human>();
        private readonly Dictionary<Accessories, Human> _humansByAccessories = new Dictionary<Accessories, Human>();
        internal readonly Dictionary<AccessoryParameter, Human> _humansByAccessoryParameter = new Dictionary<AccessoryParameter, Human>();

        #region Maker
        private Human _makerHuman;
        private AdditionalData _makerAdditionalData;
        internal Accessories _mannequinAccessories;
        internal AccessoryParameter _mannequinAccessoryParameter;
        internal AdditionalData _mannequinAdditionalData;

        private bool _inMaker;
        private EditMode _editMode;
        private AccessoryCustomEdit _accessoryCustomEdit;

        private ToggleButton[] _toggles;
        private RectTransform[] _tabMains;
        private Text[] _acceNameTexts;
        private DropDownUI[] _typeDropdowns;
        private ItemSelectUISets[] _itemSelSets;
        private DropDownUI[] _parentDropdowns;
        private Toggle[] _posEditSwitchs;
        private ColorChangeButton[] _mainColor;
        private ColorChangeButton[] _mainSpecColor;
        private InputSliderUI[] _mainSpecular;
        private InputSliderUI[] _mainSmooth;
        private ColorChangeButton[] _subColor;
        private ColorChangeButton[] _subSpecColor;
        private InputSliderUI[] _subSpecular;
        private InputSliderUI[] _subSmooth;


        private RadioButtonGroup _makerRadioButtonGroup;
        private GameObject _makerToggleButtonTemplate;
        private RectTransform _makerAddButtons;
        private GameObject _makerPanelTemplate;
        internal readonly List<MakerSlot> _makerSlots = new List<MakerSlot>();
        private ToggleGroup _copySrcGroup;
        private ToggleGroup _copyDstGroup;
        private GameObject _copyDstToggleTemplate;
        private GameObject _copySrcToggleTemplate;
        #endregion

        #region Studio
        private RectTransform _studioListTop;
        private GameObject _studioToggleTemplate;
        private StudioSlot _studioToggleAll;
        private OCIChar _selectedStudioCharacter;
        internal readonly List<StudioSlot> _studioSlots = new List<StudioSlot>();
        private bool _inStudio;
        #endregion

        #endregion

        #region Unity Methods
        protected override void Awake()
        {
            base.Awake();
            _self = this;

            ExtendedSave.CardBeingLoaded += this.OnCharaLoad;
            ExtendedSave.CardBeingSaved += this.OnCharaSave;
            ExtendedSave.CoordinateBeingLoaded += this.OnCoordLoad;
            ExtendedSave.CoordinateBeingSaved += this.OnCoordSave;

            var harmony = HarmonyExtensions.CreateInstance(_guid);
            harmony.Patch(AccessTools.Method(typeof(EditMode), "Setup", new[] { typeof(Human), typeof(EditScene) }), postfix: new HarmonyMethod(typeof(MoreAccessories), nameof(EditMode_Setup_Postfix)));
            harmony.Patch(AccessTools.Method(typeof(AccessoryCustomEdit), "Setup", new[] { typeof(Human), typeof(EditEquipShow) }), postfix: new HarmonyMethod(typeof(MoreAccessories), nameof(AccessoryCustomEdit_Setup_Postfix)));
            harmony.Patch(AccessTools.Method(typeof(CustomParameter), "Copy", new[] { typeof(CustomParameter), typeof(int) }), postfix: new HarmonyMethod(typeof(MoreAccessories), nameof(CustomParameter_Copy_Postfix)));
            harmony.Patch(AccessTools.Constructor(typeof(CustomParameter), new[] { typeof(CustomParameter) }), postfix: new HarmonyMethod(typeof(MoreAccessories), nameof(CustomParameter_Copy_Postfix)));
            harmony.Patch(AccessTools.Method(typeof(Female), "Awake"), postfix: new HarmonyMethod(typeof(MoreAccessories), nameof(Female_Awake_Postfix)));
            harmony.Patch(AccessTools.Method(typeof(Male), "Awake"), postfix: new HarmonyMethod(typeof(MoreAccessories), nameof(Male_Awake_Postfix)));
            harmony.Patch(AccessTools.Method(typeof(Mannequin), "Awake"), postfix: new HarmonyMethod(typeof(MoreAccessories), nameof(Mannequin_Awake_Postfix)));

            Accessories_Patches.PatchAll(harmony);
            AccessoryCustomEdit_Patches.PatchAll(harmony);
            AcceCopyHelperUI_Patches.PatchAll(harmony);
            Various_Patches.PatchAll(harmony);
        }

        protected override void LevelLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            base.LevelLoaded(scene, mode);
            if (mode == LoadSceneMode.Single)
            {
                switch (this._binary)
                {
                    case Binary.Game:
                        // UI is spawned under different conditions
                        break;
                    case Binary.Studio:
                        if (scene.buildIndex == 1)
                            this.SpawnStudioUI();
                        break;
                }
            }
        }

        protected override void Update()
        {
            base.Update();
            if (this._inStudio)
            {
                TreeNodeObject treeNodeObject = Studio.Studio.Instance.treeNodeCtrl.selectNode;
                if (treeNodeObject != null)
                {
                    ObjectCtrlInfo info;
                    if (Studio.Studio.Instance.dicInfo.TryGetValue(treeNodeObject, out info))
                    {
                        OCIChar selected = info as OCIChar;
                        if (selected != this._selectedStudioCharacter)
                        {
                            this._selectedStudioCharacter = selected;
                            this.UpdateStudioUI();
                        }
                    }
                }
            }
            if (this._inMaker)
            {
                if (this._editMode == null)
                {
                    this._makerAdditionalData = null;
                    this._makerSlots.Clear();
                    this._inMaker = false;
                }
            }
        }
        #endregion

        #region Public Methods
        public AdditionalData GetAdditionalData(CustomParameter customParameter)
        {
            if (this._charAdditionalData.TryGetValue(customParameter, out AdditionalData additionalData))
                return additionalData;
            return null;
        }
        #endregion

        #region Private Methods
        private void SpawnMakerUI(EditMode editMode, AcceCopyHelperUI acceCopyHelper)
        {
            this._editMode = editMode;

            UIUtility.Init();

            this._accessoryCustomEdit = editMode.transform.Find("Canvas/Accessory").GetComponent<AccessoryCustomEdit>();
            this._makerRadioButtonGroup = this._accessoryCustomEdit.transform.Find("Tabs").GetComponent<RadioButtonGroup>();
            this._makerToggleButtonTemplate = this._makerRadioButtonGroup.transform.Find("Slot 01").gameObject;
            this._makerPanelTemplate = this._accessoryCustomEdit.transform.Find("Mains/Slot01").gameObject;

            ScrollRect scrollView = UIUtility.CreateScrollView("ScrollView", this._accessoryCustomEdit.transform);
            scrollView.transform.SetRect(this._makerRadioButtonGroup.transform);
            ((RectTransform)scrollView.transform).offsetMin += new Vector2(-220f, -25f);
            scrollView.viewport.GetComponent<Image>().sprite = null;
            scrollView.movementType = ScrollRect.MovementType.Clamped;
            scrollView.horizontal = false;
            scrollView.scrollSensitivity = 18f;
            if (scrollView.horizontalScrollbar != null)
                Destroy(scrollView.horizontalScrollbar.gameObject);
            if (scrollView.verticalScrollbar != null)
                Destroy(scrollView.verticalScrollbar.gameObject);
            Destroy(scrollView.GetComponent<Image>());
            Destroy(scrollView.content.gameObject);
            scrollView.content = (RectTransform)this._makerRadioButtonGroup.transform;
            this._makerRadioButtonGroup.transform.SetParent(scrollView.viewport, false);
            ((RectTransform)this._makerRadioButtonGroup.transform).offsetMin += new Vector2(10f, 0f);
            ((RectTransform)this._makerRadioButtonGroup.transform).offsetMax += new Vector2(10f, 0f);
            ((RectTransform)this._makerRadioButtonGroup.transform).anchoredPosition = Vector2.zero;
            this._makerRadioButtonGroup.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            this._makerPanelTemplate.transform.parent.SetAsLastSibling();

            this._makerAddButtons = UIUtility.CreateNewUIObject("AddButtons", this._makerRadioButtonGroup.transform);
            LayoutElement layout = this._makerAddButtons.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 25f;
            layout.preferredWidth = 100f;
            this._makerAddButtons.SetSiblingIndex(11);

            GameObject buttonTemplate = GameObject.Find("EditScene/Canvas/LeftDownButtons/Button_Custom_F00");
            if (buttonTemplate == null)
                buttonTemplate = GameObject.Find("H Edit Canvas/GameObject/Button End");

            Button addOneButton = Instantiate(buttonTemplate).GetComponent<Button>();
            Destroy(addOneButton.GetComponent<LayoutElement>());
            addOneButton.gameObject.SetActive(true);
            addOneButton.name = "AddOne";
            addOneButton.transform.SetParent(this._makerAddButtons);
            addOneButton.transform.localPosition = Vector3.zero;
            addOneButton.transform.localScale = Vector3.one;
            addOneButton.transform.SetRect(0f, 0f, 0.5f, 1f);
            addOneButton.GetComponentInChildren<Text>().text = "+1";
            addOneButton.onClick = new Button.ButtonClickedEvent();
            addOneButton.onClick.AddListener(this.AddOneSlot);

            Button addTenButton = Instantiate(buttonTemplate).GetComponent<Button>();
            Destroy(addTenButton.GetComponent<LayoutElement>());
            addTenButton.gameObject.SetActive(true);
            addTenButton.name = "AddTen";
            addTenButton.transform.SetParent(this._makerAddButtons);
            addTenButton.transform.localPosition = Vector3.zero;
            addTenButton.transform.localScale = Vector3.one;
            addTenButton.transform.SetRect(0.5f, 0f, 1f, 1f);
            addTenButton.GetComponentInChildren<Text>().text = "+10";
            addTenButton.onClick = new Button.ButtonClickedEvent();
            addTenButton.onClick.AddListener(this.AddTenSlots);

            Transform copyArea = acceCopyHelper.transform.Find("HideableArea/HideableMask/BG/Area").transform;
            this._copyDstGroup = copyArea.Find("Dst").GetComponent<ToggleGroup>();
            this._copySrcGroup = copyArea.Find("Src").GetComponent<ToggleGroup>();
            this._copyDstToggleTemplate = this._copyDstGroup.transform.Find("Toggle (0)").gameObject;
            this._copySrcToggleTemplate = this._copySrcGroup.transform.Find("Toggle (0)").gameObject;

            scrollView = UIUtility.CreateScrollView("ScrollView", copyArea);
            scrollView.transform.SetRect();
            scrollView.viewport.GetComponent<Image>().sprite = null;
            scrollView.movementType = ScrollRect.MovementType.Clamped;
            scrollView.horizontal = false;
            scrollView.scrollSensitivity = 18f;
            if (scrollView.horizontalScrollbar != null)
                Destroy(scrollView.horizontalScrollbar.gameObject);
            if (scrollView.verticalScrollbar != null)
                Destroy(scrollView.verticalScrollbar.gameObject);
            Destroy(scrollView.GetComponent<Image>());
            this._copyDstGroup.transform.SetParent(scrollView.content, true);
            this._copySrcGroup.transform.SetParent(scrollView.content, true);
            ((RectTransform)this._copyDstGroup.transform).anchoredPosition = Vector3.zero;
            ((RectTransform)this._copySrcGroup.transform).anchoredPosition = Vector3.zero;

            this._copyDstGroup.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            this._copySrcGroup.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollView.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ((RectTransform)this._copySrcGroup.transform).rect.height);

            this._inMaker = true;
            this.ExecuteDelayed(this.UpdateMakerUI);
            this.ExecuteDelayed(this.UpdateMakerUI, 5);
        }

        private void SpawnStudioUI()
        {
            this._studioListTop = (RectTransform)GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/01_State/Viewport/Content/Slot").transform;
            this._studioToggleTemplate = this._studioListTop.Find("Slot01").gameObject;

            MPCharCtrl ctrl = Resources.FindObjectsOfTypeAll<MPCharCtrl>()[0];

            this._studioToggleAll = new StudioSlot();
            this._studioToggleAll.slot = GameObject.Instantiate(this._studioToggleTemplate);
            this._studioToggleAll.slotText = this._studioToggleAll.slot.transform.GetChild(0).GetComponent<Text>();
            this._studioToggleAll.onButton = this._studioToggleAll.slot.transform.GetChild(1).GetComponent<Button>();
            this._studioToggleAll.offButton = this._studioToggleAll.slot.transform.GetChild(2).GetComponent<Button>();
            this._studioToggleAll.slotText.text = "全て";

            this._studioToggleAll.slot.transform.SetParent(this._studioListTop);
            this._studioToggleAll.slot.transform.localPosition = Vector3.zero;
            this._studioToggleAll.slot.transform.localScale = Vector3.one;
            this._studioToggleAll.onButton.onClick = new Button.ButtonClickedEvent();
            this._studioToggleAll.onButton.onClick.AddListener(() =>
            {
                for (int i = 0; i < 10; i++)
                    this._selectedStudioCharacter.charInfo.ShowAccessory(i, true);
                AdditionalData data = this.GetAdditionalData(this._selectedStudioCharacter.oiCharInfo.charFile);
                if (data != null)
                {
                    for (int i = 0; i < data.accessories.Count; i++)
                    {
                        this._selectedStudioCharacter.charInfo.ShowAccessory(i + 10, true);
                        data.accessories[i].show = true;
                    }
                }
                ctrl.CallPrivate("UpdateInfo");
                this.UpdateStudioUI();
            });
            this._studioToggleAll.offButton.onClick = new Button.ButtonClickedEvent();
            this._studioToggleAll.offButton.onClick.AddListener(() =>
            {
                for (int i = 0; i < 10; i++)
                    this._selectedStudioCharacter.charInfo.ShowAccessory(i, false);
                AdditionalData data = this.GetAdditionalData(this._selectedStudioCharacter.oiCharInfo.charFile);
                if (data != null)
                {
                    for (int i = 0; i < data.accessories.Count; i++)
                    {
                        this._selectedStudioCharacter.charInfo.ShowAccessory(i + 10, false);
                        data.accessories[i].show = false;
                    }
                }
                ctrl.CallPrivate("UpdateInfo");
                this.UpdateStudioUI();
            });
            this._studioToggleAll.slot.transform.SetAsLastSibling();

            this._inStudio = true;
            this.ExecuteDelayed(this.UpdateStudioUI);
        }

        private void UpdateUI()
        {
            switch (this._binary)
            {
                case Binary.Game:
                    if (this._inMaker)
                        this.UpdateMakerUI();
                    break;
                case Binary.Studio:
                    if (this._inStudio)
                        this.UpdateStudioUI();
                    break;
            }
        }

        private void UpdateMakerUI()
        {
            if (this._inMaker == false)
                return;
            int i = 0;
            for (; i < this._makerAdditionalData.accessories.Count; ++i)
            {
                MakerSlot slot;
                if (i < this._makerSlots.Count)
                    slot = this._makerSlots[i];
                else
                {
                    int index = i + 10;

                    slot = new MakerSlot();
                    slot.slotButton = Instantiate(this._makerToggleButtonTemplate).GetComponent<ToggleButton>();
                    slot.accessoryText = slot.slotButton.transform.Find("AcceText").GetComponent<Text>();
                    slot.textOn = slot.slotButton.transform.Find("Button_on").GetComponentInChildren<Text>();
                    slot.textOff = slot.slotButton.transform.Find("Button_off").GetComponentInChildren<Text>();

                    slot.panel = (RectTransform)Instantiate(this._makerPanelTemplate).transform;
                    while (slot.panel.childCount != 0)
                        DestroyImmediate(slot.panel.GetChild(0).gameObject);
                    slot.typeDropDown = this._editMode.CreateDropDownUI(slot.panel.gameObject, "タイプ", this._typeDropdowns[0].dropdown.options, null);
                    slot.typeDropDown.SetPrivate("act", this._typeDropdowns[0].GetPrivate("act"));
                    slot.itemSelectedSet = this._editMode.CreateItemSelectUISets(slot.panel.gameObject, "アクセサリ", null, (UnityAction<CustomSelectSet>)this._itemSelSets[0].GetPrivate("act"));
                    slot.parentDropDown = this._editMode.CreateDropDownUI(slot.panel.gameObject, "親", this._parentDropdowns[0].dropdown.options, null);
                    slot.parentDropDown.SetPrivate("act", this._parentDropdowns[0].GetPrivate("act"));
                    slot.posEditSwitch = Instantiate((Toggle)this._accessoryCustomEdit.GetPrivate("posEditSwitchOriginal"));
                    slot.posEditSwitch.gameObject.SetActive(true);
                    slot.posEditSwitch.transform.SetParent(slot.panel, false);
                    slot.posEditSwitch.onValueChanged = this._posEditSwitchs[0].onValueChanged;
                    this._editMode.CreateSpace(slot.panel.gameObject);
                    slot.mainColor = this._editMode.CreateColorChangeButton(slot.panel.gameObject, "メインの色", Color.white, false, (Action<Color>)this._mainColor[0].GetPrivate("onChangeAct"));
                    slot.mainSpecColor = this._editMode.CreateColorChangeButton(slot.panel.gameObject, "ツヤの色", Color.white, false, (Action<Color>)this._mainSpecColor[0].GetPrivate("onChangeAct"));
                    slot.mainSpecular = this._editMode.CreateInputSliderUI(slot.panel.gameObject, "ツヤの強さ", 0f, 100f, false, 0f, null);
                    slot.mainSpecular.SetPrivate("onChangeAction", this._mainSpecular[0].GetPrivate("onChangeAction"));
                    slot.mainSmooth = this._editMode.CreateInputSliderUI(slot.panel.gameObject, "ツヤの質感", 0f, 100f, false, 0f, null);
                    slot.mainSmooth.SetPrivate("onChangeAction", this._mainSmooth[0].GetPrivate("onChangeAction"));
                    slot.subColor = this._editMode.CreateColorChangeButton(slot.panel.gameObject, "サブの色", Color.white, false, (Action<Color>)this._subColor[0].GetPrivate("onChangeAct"));
                    slot.subSpecColor = this._editMode.CreateColorChangeButton(slot.panel.gameObject, "ツヤの色", Color.white, false, (Action<Color>)this._subSpecColor[0].GetPrivate("onChangeAct"));
                    slot.subSpecular = this._editMode.CreateInputSliderUI(slot.panel.gameObject, "ツヤの強さ", 0f, 100f, false, 0f, null);
                    slot.subSpecular.SetPrivate("onChangeAction", this._subSpecular[0].GetPrivate("onChangeAction"));
                    slot.subSmooth = this._editMode.CreateInputSliderUI(slot.panel.gameObject, "ツヤの質感", 0f, 100f, false, 0f, null);
                    slot.subSmooth.SetPrivate("onChangeAction", this._subSmooth[0].GetPrivate("onChangeAction"));
                    slot.copySrcToggle = Instantiate(this._copySrcToggleTemplate).GetComponent<Toggle>();
                    slot.copyDstToggle = Instantiate(this._copyDstToggleTemplate).GetComponent<Toggle>();
                    slot.copySrcText = slot.copySrcToggle.GetComponentInChildren<Text>();
                    slot.copyDstText = slot.copyDstToggle.GetComponentInChildren<Text>();

                    slot.slotButton.transform.SetParent(this._makerToggleButtonTemplate.transform.parent);
                    slot.slotButton.transform.localPosition = Vector3.zero;
                    slot.slotButton.transform.localScale = Vector3.one;
                    slot.slotButton.transform.SetSiblingIndex(index);
                    slot.slotButton.name = $"Slot {index + 1}";
                    slot.panel.SetParent(this._makerPanelTemplate.transform.parent);
                    slot.panel.name = $"Slot{index + 1}";
                    slot.panel.transform.localPosition = this._makerPanelTemplate.transform.localPosition;
                    slot.panel.transform.localScale = this._makerPanelTemplate.transform.localScale;
                    slot.panel.SetRect(this._makerPanelTemplate.transform);
                    slot.panel.transform.SetSiblingIndex(index);

                    slot.accessoryText.text = "";
                    slot.textOn.text = (index + 1).ToString();
                    slot.textOff.text = (index + 1).ToString();

                    slot.copySrcToggle.transform.SetParent(this._copySrcToggleTemplate.transform.parent);
                    slot.copyDstToggle.transform.SetParent(this._copyDstToggleTemplate.transform.parent);
                    slot.copySrcToggle.transform.localScale = Vector3.one;
                    slot.copyDstToggle.transform.localScale = Vector3.one;
                    slot.copySrcToggle.name = $"Toggle ({index})";
                    slot.copyDstToggle.name = $"Toggle ({index})";
                    slot.copySrcText.text = $"{index + 1}:なし";
                    slot.copyDstText.text = $"{index + 1}:なし";

                    slot.slotButton.ChangeValue(false, false);

                    this._makerSlots.Add(slot);
                }
                slot.slotButton.gameObject.SetActive(true);
                slot.copySrcToggle.gameObject.SetActive(true);
                slot.copyDstToggle.gameObject.SetActive(true);
                this._copySrcGroup.RegisterToggle(slot.copySrcToggle);
                this._copyDstGroup.RegisterToggle(slot.copyDstToggle);
            }
            for (; i < this._makerSlots.Count; ++i)
            {
                MakerSlot slot = this._makerSlots[i];
                slot.slotButton.gameObject.SetActive(false);
                slot.panel.gameObject.SetActive(false);
                slot.copySrcToggle.gameObject.SetActive(false);
                slot.copyDstToggle.gameObject.SetActive(false);
                this._copySrcGroup.UnregisterToggle(slot.copySrcToggle);
                this._copyDstGroup.UnregisterToggle(slot.copyDstToggle);

                slot.typeDropDown.SetValue((int)(ACCESSORY_TYPE.NONE + 1));
                slot.itemSelectedSet.ChangeDatas(null, true);
                slot.itemSelectedSet.SetSelectedFromDataID(-1);
                slot.itemSelectedSet.ApplyFromID(-1);
                slot.parentDropDown.SetValue((int)ACCESSORY_ATTACHTYPE.NONE);
            }
            if (this._makerAdditionalData.accessories.Count + 10 != this._toggles.Length)
            {
                this.UpdateAccessoryCustomEditFields();
                this._makerRadioButtonGroup.SetToggleButtons(this._toggles);
            }
            if ((int)this._accessoryCustomEdit.GetPrivate("nowTab") >= this._makerAdditionalData.accessories.Count)
                this._accessoryCustomEdit.SetPrivate("nowTab", -1);
            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)this._makerRadioButtonGroup.transform);
            this.ResizeCopyWindow();
        }

        internal void ResizeCopyWindow()
        {
            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)this._copySrcGroup.transform.parent);
            this.ExecuteDelayed(() =>
            {
                ((RectTransform)this._copySrcGroup.transform.parent).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ((RectTransform)this._copySrcGroup.transform).rect.height);
            });
        }

        private void UpdateAccessoryCustomEditFields()
        {
            int count = this._makerAdditionalData.accessories.Count + 10;

            Array.Resize(ref this._toggles, count);
            Array.Resize(ref this._tabMains, count);
            Array.Resize(ref this._acceNameTexts, count);
            Array.Resize(ref this._typeDropdowns, count);
            Array.Resize(ref this._itemSelSets, count);
            Array.Resize(ref this._parentDropdowns, count);
            Array.Resize(ref this._posEditSwitchs, count);
            Array.Resize(ref this._mainColor, count);
            Array.Resize(ref this._mainSpecColor, count);
            Array.Resize(ref this._mainSpecular, count);
            Array.Resize(ref this._mainSmooth, count);
            Array.Resize(ref this._subColor, count);
            Array.Resize(ref this._subSpecColor, count);
            Array.Resize(ref this._subSpecular, count);
            Array.Resize(ref this._subSmooth, count);

            for (int i = 10; i < count; i++)
            {
                MakerSlot slot = this._makerSlots[i - 10];
                this._toggles[i] = slot.slotButton;
                this._tabMains[i] = slot.panel;
                this._acceNameTexts[i] = slot.accessoryText;
                this._typeDropdowns[i] = slot.typeDropDown;
                this._itemSelSets[i] = slot.itemSelectedSet;
                this._parentDropdowns[i] = slot.parentDropDown;
                this._posEditSwitchs[i] = slot.posEditSwitch;
                this._mainColor[i] = slot.mainColor;
                this._mainSpecColor[i] = slot.mainSpecColor;
                this._mainSpecular[i] = slot.mainSpecular;
                this._mainSmooth[i] = slot.mainSmooth;
                this._subColor[i] = slot.subColor;
                this._subSpecColor[i] = slot.subSpecColor;
                this._subSpecular[i] = slot.subSpecular;
                this._subSmooth[i] = slot.subSmooth;
            }

            this._accessoryCustomEdit.SetPrivate("toggles", this._toggles);
            this._accessoryCustomEdit.SetPrivate("tabMains", this._tabMains);
            this._accessoryCustomEdit.SetPrivate("acceNameTexts", this._acceNameTexts);

            this._accessoryCustomEdit.SetPrivate("typeDropdowns", this._typeDropdowns);
            this._accessoryCustomEdit.SetPrivate("itemSelSets", this._itemSelSets);
            this._accessoryCustomEdit.SetPrivate("parentDropdowns", this._parentDropdowns);
            this._accessoryCustomEdit.SetPrivate("posEditSwitchs", this._posEditSwitchs);
            this._accessoryCustomEdit.SetPrivate("mainColor", this._mainColor);
            this._accessoryCustomEdit.SetPrivate("mainSpecColor", this._mainSpecColor);
            this._accessoryCustomEdit.SetPrivate("mainSpecular", this._mainSpecular);
            this._accessoryCustomEdit.SetPrivate("mainSmooth", this._mainSmooth);
            this._accessoryCustomEdit.SetPrivate("subColor", this._subColor);
            this._accessoryCustomEdit.SetPrivate("subSpecColor", this._subSpecColor);
            this._accessoryCustomEdit.SetPrivate("subSpecular", this._subSpecular);
            this._accessoryCustomEdit.SetPrivate("subSmooth", this._subSmooth);
        }

        private void AddOneSlot()
        {
            this._makerAdditionalData.accessories.Add(new AdditionalData.AccessoryData() { accessoryCustom = new AccessoryCustom(this._makerHuman.sex) });
            this.UpdateMakerUI();
        }

        private void AddTenSlots()
        {
            for (int i = 0; i < 10; i++)
                this._makerAdditionalData.accessories.Add(new AdditionalData.AccessoryData() { accessoryCustom = new AccessoryCustom(this._makerHuman.sex) });
            this.UpdateMakerUI();
        }

        private void UpdateStudioUI()
        {
            if (this._selectedStudioCharacter == null || this._inStudio == false)
                return;

            AdditionalData additionalData;
            if (this._charAdditionalData.TryGetValue(this._selectedStudioCharacter.oiCharInfo.charFile, out additionalData) == false)
            {
                additionalData = new AdditionalData();
                this._charAdditionalData.Add(this._selectedStudioCharacter.oiCharInfo.charFile, additionalData);
            }
            int i;
            for (i = 0; i < additionalData.accessories.Count; i++)
            {
                StudioSlot slot;
                AdditionalData.AccessoryData accessory = additionalData.accessories[i];
                if (i < this._studioSlots.Count)
                    slot = this._studioSlots[i];
                else
                {
                    int index = i + 10;

                    slot = new StudioSlot();
                    slot.slot = GameObject.Instantiate(this._studioToggleTemplate);
                    slot.slotText = slot.slot.transform.GetChild(0).GetComponent<Text>();
                    slot.onButton = slot.slot.transform.GetChild(1).GetComponent<Button>();
                    slot.offButton = slot.slot.transform.GetChild(2).GetComponent<Button>();

                    slot.slot.name = $"Slot{index + 1}";
                    slot.slotText.name = $"TextMeshPro Slot{index + 1}";
                    slot.onButton.name = $"Button Slot{index + 1} 1";
                    slot.offButton.name = $"Button Slot{index + 1} 2";
                    slot.slotText.text = "スロ" + (index + 1);
                    slot.slot.transform.SetParent(this._studioListTop);
                    slot.slot.transform.localPosition = Vector3.zero;
                    slot.slot.transform.localScale = Vector3.one;
                    slot.onButton.onClick = new Button.ButtonClickedEvent();
                    slot.onButton.onClick.AddListener(() =>
                    {
                        this._selectedStudioCharacter.charInfo.ShowAccessory(index, true);
                        accessory.show = true;
                        slot.onButton.image.color = Color.green;
                        slot.offButton.image.color = Color.white;
                    });
                    slot.offButton.onClick = new Button.ButtonClickedEvent();
                    slot.offButton.onClick.AddListener(() =>
                    {
                        this._selectedStudioCharacter.charInfo.ShowAccessory(index, false);
                        accessory.show = false;
                        slot.offButton.image.color = Color.green;
                        slot.onButton.image.color = Color.white;
                    });
                    this._studioSlots.Add(slot);
                }
                slot.slot.SetActive(true);
                slot.onButton.interactable = accessory.accessoryCustom != null && accessory.accessoryCustom.type != ACCESSORY_TYPE.NONE;
                slot.onButton.image.color = slot.onButton.interactable && accessory.show ? Color.green : Color.white;
                slot.offButton.interactable = accessory.accessoryCustom != null && accessory.accessoryCustom.type != ACCESSORY_TYPE.NONE;
                slot.offButton.image.color = slot.onButton.interactable && !accessory.show ? Color.green : Color.white;

            }
            for (; i < this._studioSlots.Count; ++i)
                this._studioSlots[i].slot.SetActive(false);
            this._studioToggleAll.slot.transform.SetAsLastSibling();
        }

        internal AdditionalData GetAdditionalData(Accessories accessories)
        {
            if (accessories == this._mannequinAccessories)
                return this._mannequinAdditionalData;
            if (this._humansByAccessories.TryGetValue(accessories, out Human human))
                return this.GetAdditionalData(human.customParam);
            return null;
        }

        internal AdditionalData GetAdditionalData(AccessoryParameter accessoryParameter)
        {
            if (accessoryParameter == this._mannequinAccessoryParameter)
                return this._mannequinAdditionalData;
            if (this._humansByAccessoryParameter.TryGetValue(accessoryParameter, out Human human))
                return this.GetAdditionalData(human.customParam);
            return null;
        }
        #endregion

        #region Patches
        private static void Female_Awake_Postfix(Female __instance)
        {
            _self._humansByCustomParameter.Add(__instance.customParam, __instance);
            _self._humansByAccessories.Add(__instance.accessories, __instance);
            _self._humansByAccessoryParameter.Add(__instance.customParam.acce, __instance);
        }

        private static void Male_Awake_Postfix(Male __instance)
        {
            _self._humansByCustomParameter.Add(__instance.customParam, __instance);
            _self._humansByAccessories.Add(__instance.accessories, __instance);
            _self._humansByAccessoryParameter.Add(__instance.customParam.acce, __instance);
        }

        private static void Mannequin_Awake_Postfix(Mannequin __instance, AccessoryParameter ___acceParan)
        {
            _self._mannequinAccessories = __instance.accessories;
            _self._mannequinAccessoryParameter = ___acceParan;
            _self._mannequinAdditionalData = new AdditionalData();
        }

        private static void EditMode_Setup_Postfix(EditMode __instance, Human human, AccessoryCustomEdit ___acce)
        {
            _self._makerHuman = human;
            if (_self._charAdditionalData.TryGetValue(human.customParam, out _self._makerAdditionalData) == false)
            {
                _self._makerAdditionalData = new AdditionalData();
                _self._charAdditionalData.Add(human.customParam, _self._makerAdditionalData);
                _self._humansByCustomParameter[human.customParam] = human;
                _self._humansByAccessories[human.accessories] = human;
                _self._humansByAccessoryParameter[human.customParam.acce] = human;
            }
            _self.SpawnMakerUI(__instance, (AcceCopyHelperUI)___acce.GetPrivate("helperUI"));
        }

        private static void AccessoryCustomEdit_Setup_Postfix(Human human,
                                                              ToggleButton[] ___toggles,
                                                              RectTransform[] ___tabMains,
                                                              Text[] ___acceNameTexts,
                                                              DropDownUI[] ___typeDropdowns,
                                                              ItemSelectUISets[] ___itemSelSets,
                                                              DropDownUI[] ___parentDropdowns,
                                                              Toggle[] ___posEditSwitchs,
                                                              ColorChangeButton[] ___mainColor,
                                                              ColorChangeButton[] ___mainSpecColor,
                                                              InputSliderUI[] ___mainSpecular,
                                                              InputSliderUI[] ___mainSmooth,
                                                              ColorChangeButton[] ___subColor,
                                                              ColorChangeButton[] ___subSpecColor,
                                                              InputSliderUI[] ___subSpecular,
                                                              InputSliderUI[] ___subSmooth)
        {
            _self._toggles = ___toggles;
            _self._tabMains = ___tabMains;
            _self._acceNameTexts = ___acceNameTexts;
            _self._typeDropdowns = ___typeDropdowns;
            _self._itemSelSets = ___itemSelSets;
            _self._parentDropdowns = ___parentDropdowns;
            _self._posEditSwitchs = ___posEditSwitchs;
            _self._mainColor = ___mainColor;
            _self._mainSpecColor = ___mainSpecColor;
            _self._mainSpecular = ___mainSpecular;
            _self._mainSmooth = ___mainSmooth;
            _self._subColor = ___subColor;
            _self._subSpecColor = ___subSpecColor;
            _self._subSpecular = ___subSpecular;
            _self._subSmooth = ___subSmooth;
        }

        private static void CustomParameter_Copy_Postfix(CustomParameter __instance, CustomParameter copy)
        {
            if (__instance == copy)
                return;
            if (_self._charAdditionalData.TryGetValue(copy, out AdditionalData sourceData) == false)
            {
                sourceData = new AdditionalData();
                _self._charAdditionalData.Add(copy, sourceData);
            }
            if (_self._charAdditionalData.TryGetValue(__instance, out AdditionalData destinationData) == false)
            {
                destinationData = new AdditionalData();
                _self._charAdditionalData.Add(__instance, destinationData);
            }
            if (sourceData == destinationData)
                return;
            destinationData.LoadFrom(sourceData);

            if (_self._humansByCustomParameter.TryGetValue(copy, out Human human))
                _self._humansByCustomParameter[__instance] = human;

            if (_self._humansByAccessoryParameter.TryGetValue(copy.acce, out human))
                _self._humansByAccessoryParameter[__instance.acce] = human;

            if (destinationData == _self._makerAdditionalData)
                _self.UpdateMakerUI();
        }
        #endregion

        #region Saves
        private void OnCharaLoad(CustomParameter file)
        {
            PluginData pluginData = ExtendedSave.GetExtendedDataById(file, _extSaveKey);

            AdditionalData additionalData;
            if (this._charAdditionalData.TryGetValue(file, out additionalData) == false)
            {
                additionalData = new AdditionalData();
                this._charAdditionalData.Add(file, additionalData);
            }
            additionalData.Clear();

            XmlNode node = null;
            if (pluginData != null && pluginData.data.TryGetValue("additionalAccessories", out object xmlData))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml((string)xmlData);
                node = doc.FirstChild;
            }

            if (node != null)
                this.LoadAdditionalData(additionalData, node, file.Sex);

            this.ExecuteDelayed(this.UpdateUI);
        }

        private void OnCharaSave(CustomParameter file)
        {
            AdditionalData additionalData;
            if (this._charAdditionalData.TryGetValue(file, out additionalData) == false)
                return;

            using (StringWriter stringWriter = new StringWriter())
            using (XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter))
            {
                this.SaveAdditionalData(additionalData, xmlWriter);

                PluginData pluginData = new PluginData();
                pluginData.version = _saveVersion;
                pluginData.data.Add("additionalAccessories", stringWriter.ToString());
                ExtendedSave.SetExtendedDataById(file, _extSaveKey, pluginData);
            }
        }

        private void OnCoordLoad(CustomParameter file)
        {
            PluginData pluginData = ExtendedSave.GetExtendedCoordDataById(file, _extSaveKey);

            AdditionalData additionalData;
            if (this._charAdditionalData.TryGetValue(file, out additionalData) == false)
            {
                additionalData = new AdditionalData();
                this._charAdditionalData.Add(file, additionalData);
            }
            additionalData.Clear();

            XmlNode node = null;
            if (pluginData != null && pluginData.data.TryGetValue("additionalAccessories", out object xmlData))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml((string)xmlData);
                node = doc.FirstChild;
            }

            if (node != null)
                this.LoadAdditionalData(additionalData, node, file.Sex);

            this.ExecuteDelayed(this.UpdateUI);
        }


        private void OnCoordSave(CustomParameter file)
        {
            AdditionalData additionalData;
            if (this._charAdditionalData.TryGetValue(file, out additionalData) == false)
                return;

            using (StringWriter stringWriter = new StringWriter())
            using (XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter))
            {
                this.SaveAdditionalData(additionalData, xmlWriter);

                PluginData pluginData = new PluginData();
                pluginData.version = _saveVersion;
                pluginData.data.Add("additionalAccessories", stringWriter.ToString());
                ExtendedSave.SetExtendedCoordDataById(file, _extSaveKey, pluginData);
            }
        }

        private void LoadAdditionalData(AdditionalData data, XmlNode node, SEX sex)
        {
            foreach (XmlNode childNode in node.ChildNodes)
            {
                switch (childNode.Name)
                {
                    case "accessory":
                        AccessoryCustom accessory = new AccessoryCustom(sex);
                        accessory.type = (ACCESSORY_TYPE)XmlConvert.ToInt32(childNode.Attributes["type"].Value);
                        bool show = true;
                        if (accessory.type != ACCESSORY_TYPE.NONE)
                        {
                            accessory.id = XmlConvert.ToInt32(childNode.Attributes["id"].Value);
                            accessory.nowAttach = (ACCESSORY_ATTACH)XmlConvert.ToInt32(childNode.Attributes["nowAttach"].Value);

                            accessory.addPos = new Vector3(
                                    XmlConvert.ToSingle(childNode.Attributes["addPosX"].Value),
                                    XmlConvert.ToSingle(childNode.Attributes["addPosY"].Value),
                                    XmlConvert.ToSingle(childNode.Attributes["addPosZ"].Value)
                                    );
                            accessory.addRot = new Vector3(
                                    XmlConvert.ToSingle(childNode.Attributes["addRotX"].Value),
                                    XmlConvert.ToSingle(childNode.Attributes["addRotY"].Value),
                                    XmlConvert.ToSingle(childNode.Attributes["addRotZ"].Value)
                                    );
                            accessory.addScl = new Vector3(
                                    XmlConvert.ToSingle(childNode.Attributes["addSclX"].Value),
                                    XmlConvert.ToSingle(childNode.Attributes["addSclY"].Value),
                                    XmlConvert.ToSingle(childNode.Attributes["addSclZ"].Value)
                                    );

                            if (childNode.Attributes["mainColor1R"] != null)
                            {
                                accessory.color = new ColorParameter_PBR2();
                                accessory.color.mainColor1 = new Color(
                                        XmlConvert.ToSingle(childNode.Attributes["mainColor1R"].Value),
                                        XmlConvert.ToSingle(childNode.Attributes["mainColor1G"].Value),
                                        XmlConvert.ToSingle(childNode.Attributes["mainColor1B"].Value),
                                        XmlConvert.ToSingle(childNode.Attributes["mainColor1A"].Value)
                                );
                                accessory.color.specColor1 = new Color(
                                        XmlConvert.ToSingle(childNode.Attributes["specColor1R"].Value),
                                        XmlConvert.ToSingle(childNode.Attributes["specColor1G"].Value),
                                        XmlConvert.ToSingle(childNode.Attributes["specColor1B"].Value),
                                        XmlConvert.ToSingle(childNode.Attributes["specColor1A"].Value)
                                );
                                accessory.color.specular1 = XmlConvert.ToSingle(childNode.Attributes["specular1"].Value);
                                accessory.color.smooth1 = XmlConvert.ToSingle(childNode.Attributes["smooth1"].Value);

                                accessory.color.mainColor2 = new Color(
                                        XmlConvert.ToSingle(childNode.Attributes["mainColor2R"].Value),
                                        XmlConvert.ToSingle(childNode.Attributes["mainColor2G"].Value),
                                        XmlConvert.ToSingle(childNode.Attributes["mainColor2B"].Value),
                                        XmlConvert.ToSingle(childNode.Attributes["mainColor2A"].Value)
                                );
                                accessory.color.specColor2 = new Color(
                                        XmlConvert.ToSingle(childNode.Attributes["specColor2R"].Value),
                                        XmlConvert.ToSingle(childNode.Attributes["specColor2G"].Value),
                                        XmlConvert.ToSingle(childNode.Attributes["specColor2B"].Value),
                                        XmlConvert.ToSingle(childNode.Attributes["specColor2A"].Value)
                                );
                                accessory.color.specular2 = XmlConvert.ToSingle(childNode.Attributes["specular2"].Value);
                                accessory.color.smooth2 = XmlConvert.ToSingle(childNode.Attributes["smooth2"].Value);
                            }

                            if (this._inStudio && childNode.Attributes["show"] != null)
                                show = XmlConvert.ToBoolean(childNode.Attributes["show"].Value);
                        }
                        data.accessories.Add(new AdditionalData.AccessoryData() { accessoryCustom = accessory, show = show });
                        break;
                }
            }
        }

        private void SaveAdditionalData(AdditionalData data, XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("additionalAccessories");
            xmlWriter.WriteAttributeString("version", _version);
            for (int index = 0; index < data.accessories.Count; index++)
            {
                AdditionalData.AccessoryData dataAccessory = data.accessories[index];
                AccessoryCustom accessory = dataAccessory.accessoryCustom;
                xmlWriter.WriteStartElement("accessory");
                xmlWriter.WriteAttributeString("type", XmlConvert.ToString((int)accessory.type));

                if (accessory.type != ACCESSORY_TYPE.NONE)
                {
                    xmlWriter.WriteAttributeString("id", XmlConvert.ToString(accessory.id));
                    xmlWriter.WriteAttributeString("nowAttach", XmlConvert.ToString((int)accessory.nowAttach));

                    xmlWriter.WriteAttributeString("addPosX", XmlConvert.ToString(accessory.addPos.x));
                    xmlWriter.WriteAttributeString("addPosY", XmlConvert.ToString(accessory.addPos.y));
                    xmlWriter.WriteAttributeString("addPosZ", XmlConvert.ToString(accessory.addPos.z));

                    xmlWriter.WriteAttributeString("addRotX", XmlConvert.ToString(accessory.addRot.x));
                    xmlWriter.WriteAttributeString("addRotY", XmlConvert.ToString(accessory.addRot.y));
                    xmlWriter.WriteAttributeString("addRotZ", XmlConvert.ToString(accessory.addRot.z));

                    xmlWriter.WriteAttributeString("addSclX", XmlConvert.ToString(accessory.addScl.x));
                    xmlWriter.WriteAttributeString("addSclY", XmlConvert.ToString(accessory.addScl.y));
                    xmlWriter.WriteAttributeString("addSclZ", XmlConvert.ToString(accessory.addScl.z));

                    if (accessory.color != null)
                    {
                        xmlWriter.WriteAttributeString("mainColor1R", XmlConvert.ToString(accessory.color.mainColor1.r));
                        xmlWriter.WriteAttributeString("mainColor1G", XmlConvert.ToString(accessory.color.mainColor1.g));
                        xmlWriter.WriteAttributeString("mainColor1B", XmlConvert.ToString(accessory.color.mainColor1.b));
                        xmlWriter.WriteAttributeString("mainColor1A", XmlConvert.ToString(accessory.color.mainColor1.a));

                        xmlWriter.WriteAttributeString("specColor1R", XmlConvert.ToString(accessory.color.specColor1.r));
                        xmlWriter.WriteAttributeString("specColor1G", XmlConvert.ToString(accessory.color.specColor1.g));
                        xmlWriter.WriteAttributeString("specColor1B", XmlConvert.ToString(accessory.color.specColor1.b));
                        xmlWriter.WriteAttributeString("specColor1A", XmlConvert.ToString(accessory.color.specColor1.a));

                        xmlWriter.WriteAttributeString("specular1", XmlConvert.ToString(accessory.color.specular1));
                        xmlWriter.WriteAttributeString("smooth1", XmlConvert.ToString(accessory.color.smooth1));

                        xmlWriter.WriteAttributeString("mainColor2R", XmlConvert.ToString(accessory.color.mainColor2.r));
                        xmlWriter.WriteAttributeString("mainColor2G", XmlConvert.ToString(accessory.color.mainColor2.g));
                        xmlWriter.WriteAttributeString("mainColor2B", XmlConvert.ToString(accessory.color.mainColor2.b));
                        xmlWriter.WriteAttributeString("mainColor2A", XmlConvert.ToString(accessory.color.mainColor2.a));

                        xmlWriter.WriteAttributeString("specColor2R", XmlConvert.ToString(accessory.color.specColor2.r));
                        xmlWriter.WriteAttributeString("specColor2G", XmlConvert.ToString(accessory.color.specColor2.g));
                        xmlWriter.WriteAttributeString("specColor2B", XmlConvert.ToString(accessory.color.specColor2.b));
                        xmlWriter.WriteAttributeString("specColor2A", XmlConvert.ToString(accessory.color.specColor2.a));

                        xmlWriter.WriteAttributeString("specular2", XmlConvert.ToString(accessory.color.specular2));
                        xmlWriter.WriteAttributeString("smooth2", XmlConvert.ToString(accessory.color.smooth2));
                    }
                    if (this._inStudio)
                        xmlWriter.WriteAttributeString("show", XmlConvert.ToString(dataAccessory.show));
                }
                xmlWriter.WriteEndElement();
            }
            xmlWriter.WriteEndElement();
        }
        #endregion
    }
}
