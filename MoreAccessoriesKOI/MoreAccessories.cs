using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using BepInEx;
using ChaCustom;
using ExtensibleSaveFormat;
using HarmonyLib;
#if EMOTIONCREATORS
using HPlay;
using ADVPart.Manipulate;
using ADVPart.Manipulate.Chara;
#endif
using Illusion.Extensions;
using Illusion.Game;
using Manager;
using MessagePack;
using Sideloader.AutoResolver;
#if KOIKATSU
using Studio;
#endif
using TMPro;
using ToolBox;
using ToolBox.Extensions;
using UILib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Scene = UnityEngine.SceneManagement.Scene;

namespace MoreAccessoriesKOI
{
    [BepInPlugin(GUID: "com.joan6694.illusionplugins.moreaccessories", Name: "MoreAccessories", Version: versionNum)]
    [BepInDependency("com.bepis.bepinex.extendedsave")]
    [BepInDependency("com.bepis.bepinex.sideloader", BepInDependency.DependencyFlags.HardDependency)]
    public class MoreAccessories : GenericPlugin
    {
        public const string versionNum = "1.1.0";

        #region Events
        /// <summary>
        /// Fires when a new accessory UI slot is created in the maker.
        /// </summary>
        public event Action<int, Transform> onCharaMakerSlotAdded;
        #endregion

        #region Private Types
        public class CharAdditionalData
        {
            public List<ChaFileAccessory.PartsInfo> nowAccessories;
            public readonly List<ListInfoBase> infoAccessory = new List<ListInfoBase>();
            public readonly List<GameObject> objAccessory = new List<GameObject>();
            public readonly List<GameObject[]> objAcsMove = new List<GameObject[]>();
            public readonly List<ChaAccessoryComponent> cusAcsCmp = new List<ChaAccessoryComponent>();

#if EMOTIONCREATORS
            public List<int> advState = new List<int>();
#endif
            public List<bool> showAccessories = new List<bool>();
            public readonly Dictionary<int, List<ChaFileAccessory.PartsInfo>> rawAccessoriesInfos = new Dictionary<int, List<ChaFileAccessory.PartsInfo>>();

            public void LoadFrom(CharAdditionalData other)
            {
#if EMOTIONCREATORS
                this.advState.Clear();
#endif
                this.showAccessories.Clear();
                this.rawAccessoriesInfos.Clear();
                this.infoAccessory.Clear();
                foreach (GameObject o in this.objAccessory)
                {
                    if (o != null)
                        Destroy(o);
                }
                this.objAccessory.Clear();
                this.objAcsMove.Clear();
                foreach (ChaAccessoryComponent c in this.cusAcsCmp)
                {
                    if (c != null)
                        Destroy(c);
                }
                this.cusAcsCmp.Clear();


#if EMOTIONCREATORS
                this.advState.AddRange(other.advState);
#endif
                this.showAccessories.AddRange(other.showAccessories);

                foreach (KeyValuePair<int, List<ChaFileAccessory.PartsInfo>> coordPair in other.rawAccessoriesInfos)
                {
                    List<ChaFileAccessory.PartsInfo> parts = new List<ChaFileAccessory.PartsInfo>();
                    foreach (ChaFileAccessory.PartsInfo part in coordPair.Value)
                        parts.Add(MessagePackSerializer.Deserialize<ChaFileAccessory.PartsInfo>(MessagePackSerializer.Serialize(part)));
                    if (coordPair.Value == other.nowAccessories)
                        this.nowAccessories = parts;
                    this.rawAccessoriesInfos.Add(coordPair.Key, parts);
                }

                this.infoAccessory.AddRange(new ListInfoBase[other.infoAccessory.Count]);
                this.objAccessory.AddRange(new GameObject[other.objAccessory.Count]);
                while (this.objAcsMove.Count < other.objAcsMove.Count)
                    this.objAcsMove.Add(new GameObject[2]);
                this.cusAcsCmp.AddRange(new ChaAccessoryComponent[other.cusAcsCmp.Count]);
            }
        }

        internal class CharaMakerSlotData
        {
            public Toggle toggle;
            public CanvasGroup canvasGroup;
            public TextMeshProUGUI text;
            public CvsAccessory cvsAccessory;

#if KOIKATSU
            public GameObject copySlotObject;
            public Toggle copyToggle;
            public TextMeshProUGUI copySourceText;
            public TextMeshProUGUI copyDestinationText;
#endif

            public GameObject transferSlotObject;
            public Toggle transferSourceToggle;
            public Toggle transferDestinationToggle;
            public TextMeshProUGUI transferSourceText;
            public TextMeshProUGUI transferDestinationText;
        }

#if KOIKATSU
        private class StudioSlotData
        {
            public RectTransform slot;
            public Text name;
            public Button onButton;
            public Button offButton;
        }

        private class HSceneSlotData
        {
            public RectTransform slot;
            public TextMeshProUGUI text;
            public Button button;
        }
#elif EMOTIONCREATORS
        private class PlaySceneSlotData
        {
            public RectTransform slot;
            public TextMeshProUGUI text;
            public Button button;
        }

        private class ADVSceneSlotData
        {
            public RectTransform slot;
            public TextMeshProUGUI text;
            public Toggle keep;
            public Toggle wear;
            public Toggle takeOff;
        }
#endif
        #endregion

        #region Private Variables
        public static MoreAccessories _self; //Not internal because other plugins might access this
        private const int _saveVersion = 1;
        private const string _extSaveKey = "moreAccessories";
        private GameObject _charaMakerSlotTemplate;
        private ScrollRect _charaMakerScrollView;
        internal CustomAcsChangeSlot _customAcsChangeSlot;
        internal CustomAcsParentWindow _customAcsParentWin;
        internal CustomAcsMoveWindow[] _customAcsMoveWin;
        internal CustomAcsSelectKind[] _customAcsSelectKind;
        internal CvsAccessory[] _cvsAccessory;
        internal List<CharaMakerSlotData> _additionalCharaMakerSlots;
        internal WeakKeyDictionary<ChaFile, CharAdditionalData> _accessoriesByChar = new WeakKeyDictionary<ChaFile, CharAdditionalData>();
        internal WeakKeyDictionary<ChaFileCoordinate, WeakReference> _charByCoordinate = new WeakKeyDictionary<ChaFileCoordinate, WeakReference>();
        public CharAdditionalData _charaMakerData = null;
        private float _slotUIPositionY;
        internal bool _hasDarkness;
        internal bool _isParty = false;

        private bool _inCharaMaker = false;
        private RectTransform _addButtonsGroup;
#if KOIKATSU
        private ScrollRect _charaMakerCopyScrollView;
        private GameObject _copySlotTemplate;
#endif
        private ScrollRect _charaMakerTransferScrollView;
        private GameObject _transferSlotTemplate;
        private List<UI_RaycastCtrl> _raycastCtrls = new List<UI_RaycastCtrl>();
        private ChaFile _overrideCharaLoadingFilePre;
        private ChaFile _overrideCharaLoadingFilePost;
        private bool _loadAdditionalAccessories = true;
        private CustomFileWindow _loadCoordinatesWindow;

#if KOIKATSU
        private bool _inH;
        internal List<ChaControl> _hSceneFemales;
        private List<HSprite.FemaleDressButtonCategory> _hSceneMultipleFemaleButtons;
        private List<List<HSceneSlotData>> _additionalHSceneSlots = new List<List<HSceneSlotData>>();
        private HSprite _hSprite;
        private HSceneSpriteCategory _hSceneSoloFemaleAccessoryButton;

        private StudioSlotData _studioToggleAll;
        private RectTransform _studioToggleTemplate;
        private bool _inStudio;
        private OCIChar _selectedStudioCharacter;
        private readonly List<StudioSlotData> _additionalStudioSlots = new List<StudioSlotData>();
        private StudioSlotData _studioToggleMain;
        private StudioSlotData _studioToggleSub;
#elif EMOTIONCREATORS
        private bool _inPlay;
        private readonly List<PlaySceneSlotData> _additionalPlaySceneSlots = new List<PlaySceneSlotData>();
        private RectTransform _playButtonTemplate;
        private HPlayHPartAccessoryCategoryUI _playUI;
        private Coroutine _updatePlayUIHandler;

        private AccessoryUICtrl _advUI;
        private readonly List<ADVSceneSlotData> _additionalADVSceneSlots = new List<ADVSceneSlotData>();
        private RectTransform _advToggleTemplate;
#endif
        #endregion

        #region Unity Methods
        protected override void Awake()
        {
            base.Awake();
            _self = this;
            this._hasDarkness = typeof(ChaControl).GetMethod("ChangeShakeAccessory", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance) != null;
            this._isParty = Application.productName == "Koikatsu Party";

            Harmony harmony = new Harmony("com.joan6694.kkplugins.moreaccessories");
            harmony.PatchAllSafe();
            Type uarHooks = typeof(UniversalAutoResolver).GetNestedType("Hooks", BindingFlags.NonPublic | BindingFlags.Static);
            ChaControl_ChangeAccessory_Patches.ManualPatch(harmony);
            harmony.Patch(uarHooks.GetMethod("ExtendedCardLoad", AccessTools.all), new HarmonyMethod(typeof(MoreAccessories), nameof(UAR_ExtendedCardLoad_Prefix)));
            harmony.Patch(uarHooks.GetMethod("ExtendedCardSave", AccessTools.all), postfix: new HarmonyMethod(typeof(MoreAccessories), nameof(UAR_ExtendedCardSave_Postfix)));
            harmony.Patch(uarHooks.GetMethod("ExtendedCoordinateLoad", AccessTools.all), new HarmonyMethod(typeof(MoreAccessories), nameof(UAR_ExtendedCoordLoad_Prefix)));
            harmony.Patch(uarHooks.GetMethod("ExtendedCoordinateSave", AccessTools.all), postfix: new HarmonyMethod(typeof(MoreAccessories), nameof(UAR_ExtendedCoordSave_Postfix)));
        }

        protected override void LevelLoaded(Scene scene, LoadSceneMode loadMode)
        {
            base.LevelLoaded(scene, loadMode);
            switch (loadMode)
            {
                case LoadSceneMode.Single:
                    if (this._binary == Binary.Game)
                    {
                        this._inCharaMaker = false;
#if KOIKATSU
                        this._inH = false;
#elif EMOTIONCREATORS
                        this._inPlay = false;
#endif
                        switch (scene.buildIndex)
                        {
                            //Chara maker
#if KOIKATSU
                            case 2:
#elif EMOTIONCREATORS
                            case 3:
#endif
                                CustomBase.Instance.selectSlot = 0;
                                this._additionalCharaMakerSlots = new List<CharaMakerSlotData>();
                                this._raycastCtrls = new List<UI_RaycastCtrl>();
                                this._inCharaMaker = true;
                                this._loadCoordinatesWindow = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/06_SystemTop/cosFileControl/charaFileWindow").GetComponent<CustomFileWindow>();
                                break;
#if KOIKATSU
                            case 17: //Hscenes
                                this._inH = true;
                                break;
#endif
                        }
                    }
#if KOIKATSU
                    else
                    {
                        if (scene.buildIndex == 1) //Studio
                        {
                            this.SpawnStudioUI();
                            this._inStudio = true;
                        }
                        else
                            this._inStudio = false;
                    }
#endif
                    this._accessoriesByChar.Purge();
                    this._charByCoordinate.Purge();
                    break;
                case LoadSceneMode.Additive:
                    if (this._binary == Binary.Game && scene.buildIndex == 2) //Class chara maker
                    {
                        CustomBase.Instance.selectSlot = 0;
                        this._additionalCharaMakerSlots = new List<CharaMakerSlotData>();
                        this._raycastCtrls = new List<UI_RaycastCtrl>();
                        this._inCharaMaker = true;
                        this._loadCoordinatesWindow = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/06_SystemTop/cosFileControl/charaFileWindow").GetComponent<CustomFileWindow>();
                    }
                    break;
            }
        }

        protected override void Update()
        {
            if (this._inCharaMaker)
            {
                if (this._customAcsChangeSlot != null)
                {
                    if (CustomBase.Instance.updateCustomUI)
                    {
                        for (int i = 0; i < this._additionalCharaMakerSlots.Count && i < this._charaMakerData.nowAccessories.Count; i++)
                        {
                            CharaMakerSlotData slot = this._additionalCharaMakerSlots[i];
                            if (slot.toggle.gameObject.activeSelf == false)
                                continue;
                            if (i + 20 == CustomBase.Instance.selectSlot)
                                slot.cvsAccessory.UpdateCustomUI();
                            slot.cvsAccessory.UpdateSlotName();
                        }
                    }
                }
                if (this._loadCoordinatesWindow == null) //Handling maker with additive loading
                    this._inCharaMaker = false;
            }
#if KOIKATSU
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
#endif
        }

        protected override void LateUpdate()
        {
            if (this._inCharaMaker && this._customAcsChangeSlot != null)
            {
                Transform t;
                if (CustomBase.Instance.selectSlot < 20)
                    t = this._customAcsChangeSlot.items[CustomBase.Instance.selectSlot].cgItem.transform;
                else
                    t = this._additionalCharaMakerSlots[CustomBase.Instance.selectSlot - 20].canvasGroup.transform;
                t.position = new Vector3(t.position.x, this._slotUIPositionY);
            }
        }
        #endregion

        #region Public Methods (aka the stuff other plugins use)
        /// <summary>
        /// Returns the ChaAccessoryComponent of <paramref name="character"/> at <paramref name="index"/>.
        /// </summary>
        /// <param name="character"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public ChaAccessoryComponent GetChaAccessoryComponent(ChaControl character, int index)
        {
            if (index < 20)
                return character.cusAcsCmp[index];
            CharAdditionalData data;
            index -= 20;
            if (this._accessoriesByChar.TryGetValue(character.chaFile, out data) && index < data.cusAcsCmp.Count)
                return data.cusAcsCmp[index];
            return null;
        }

        /// <summary>
        /// Returns the index of a certain ChaAccessoryComponent held by <paramref name="character"/>.
        /// </summary>
        /// <param name="character"></param>
        /// <param name="component"></param>
        /// <returns></returns>
        public int GetChaAccessoryComponentIndex(ChaControl character, ChaAccessoryComponent component)
        {
            int index = character.cusAcsCmp.IndexOf(component);
            if (index == -1)
            {
                CharAdditionalData data;
                if (this._accessoriesByChar.TryGetValue(character.chaFile, out data) == false)
                    return -1;
                index = data.cusAcsCmp.IndexOf(component);
                if (index == -1)
                    return -1;
                index += 20;
            }
            return index;
        }

        /// <summary>
        /// Get the total of accessory UI element in the chara maker (vanilla + additional).
        /// </summary>
        /// <returns></returns>
        public int GetCvsAccessoryCount()
        {
            if (this._inCharaMaker)
                return this._additionalCharaMakerSlots.Count + 20;
            return 0;
        }
        #endregion

        #region Private Methods
        internal void SpawnMakerUI()
        {
            RectTransform container = (RectTransform)GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/04_AccessoryTop").transform;
            this._charaMakerScrollView = UIUtility.CreateScrollView("Slots", container);
            this._charaMakerScrollView.movementType = ScrollRect.MovementType.Clamped;
            this._charaMakerScrollView.horizontal = false;
            this._charaMakerScrollView.scrollSensitivity = 18f;
            if (this._charaMakerScrollView.horizontalScrollbar != null)
                Destroy(this._charaMakerScrollView.horizontalScrollbar.gameObject);
            if (this._charaMakerScrollView.verticalScrollbar != null)
                Destroy(this._charaMakerScrollView.verticalScrollbar.gameObject);
            Destroy(this._charaMakerScrollView.GetComponent<Image>());
            this._charaMakerSlotTemplate = container.GetChild(0).gameObject;
            RectTransform rootCanvas = ((RectTransform)this._charaMakerSlotTemplate.GetComponentInParent<Canvas>().transform);
            LayoutElement element = this._charaMakerScrollView.gameObject.AddComponent<LayoutElement>();
            element.minHeight = rootCanvas.rect.height / 1.298076f;
            element.minWidth = 622f; //Because trying to get the value dynamically fails for some reason so fuck it.
            VerticalLayoutGroup group = this._charaMakerScrollView.content.gameObject.AddComponent<VerticalLayoutGroup>();
            VerticalLayoutGroup parentGroup = container.GetComponent<VerticalLayoutGroup>();
            group.childAlignment = parentGroup.childAlignment;
            group.childControlHeight = parentGroup.childControlHeight;
            group.childControlWidth = parentGroup.childControlWidth;
            group.childForceExpandHeight = parentGroup.childForceExpandHeight;
            group.childForceExpandWidth = parentGroup.childForceExpandWidth;
            group.spacing = parentGroup.spacing;
            this._charaMakerScrollView.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            this.ExecuteDelayed(() =>
            {
                this._slotUIPositionY = this._charaMakerSlotTemplate.transform.parent.position.y;
            }, 15);

            Type kkus = Type.GetType("HSUS.HSUS,KKUS");
            if (kkus != null)
            {
                object self = kkus.GetField("_self", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                float scale = (float)self.GetPrivate("_gameUIScale");
                element.minHeight = element.minHeight / scale + 160f * (1f - scale);
            }

            for (int i = 0; i < 20; i++)
                container.GetChild(0).SetParent(this._charaMakerScrollView.content);

            this._charaMakerScrollView.transform.SetAsFirstSibling();
            Toggle toggleChange = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/04_AccessoryTop/tglChange").GetComponent<Toggle>();
            this._addButtonsGroup = UIUtility.CreateNewUIObject("Add Buttons Group", this._charaMakerScrollView.content);
            element = this._addButtonsGroup.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 224f;
            element.preferredHeight = 32f;
            GameObject textModel = toggleChange.transform.Find("imgOff").GetComponentInChildren<TextMeshProUGUI>().gameObject;

            Button addOneButton = UIUtility.CreateButton("Add One Button", this._addButtonsGroup, "+1");
            addOneButton.transform.SetRect(Vector2.zero, new Vector2(0.5f, 1f));
            addOneButton.colors = toggleChange.colors;
            ((Image)addOneButton.targetGraphic).sprite = toggleChange.transform.Find("imgOff").GetComponent<Image>().sprite;
            Destroy(addOneButton.GetComponentInChildren<Text>().gameObject);
            TextMeshProUGUI text = Instantiate(textModel).GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(addOneButton.transform);
            text.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(5f, 4f), new Vector2(-5f, -4f));
            text.text = "+1";
            addOneButton.onClick.AddListener(this.AddSlot);

            Button addTenButton = UIUtility.CreateButton("Add Ten Button", this._addButtonsGroup, "+10");
            addTenButton.transform.SetRect(new Vector2(0.5f, 0f), Vector2.one);
            addTenButton.colors = toggleChange.colors;
            ((Image)addTenButton.targetGraphic).sprite = toggleChange.transform.Find("imgOff").GetComponent<Image>().sprite;
            Destroy(addTenButton.GetComponentInChildren<Text>().gameObject);
            text = Instantiate(textModel).GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(addTenButton.transform);
            text.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(5f, 4f), new Vector2(-5f, -4f));
            text.text = "+10";
            addTenButton.onClick.AddListener(this.AddTenSlot);
            LayoutRebuilder.ForceRebuildLayoutImmediate(container);

            for (int i = 0; i < this._customAcsChangeSlot.items.Length; i++)
            {
                UI_ToggleGroupCtrl.ItemInfo info = this._customAcsChangeSlot.items[i];
                info.tglItem.onValueChanged = new Toggle.ToggleEvent();
                if (i < 20)
                {
                    int i1 = i;
                    info.tglItem.onValueChanged.AddListener(b =>
                    {
                        this.AccessorySlotToggleCallback(i1, info.tglItem);
                        this.AccessorySlotCanvasGroupCallback(i1, info.tglItem, info.cgItem);
                    });
                }
                else if (i == 20)
                {
                    info.tglItem.onValueChanged.AddListener(b =>
                    {
                        if (info.tglItem.isOn)
                        {
                            this._customAcsChangeSlot.CloseWindow();
#if KOIKATSU
                            CustomBase.Instance.updateCvsAccessoryCopy = true;
#endif
                        }
                        this.AccessorySlotCanvasGroupCallback(-1, info.tglItem, info.cgItem);
                    });
                    ((RectTransform)info.cgItem.transform).anchoredPosition += new Vector2(0f, 40f);
                }
                else if (i == 21)
                {
                    info.tglItem.onValueChanged.AddListener(b =>
                    {
                        if (info.tglItem.isOn)
                        {
                            this._customAcsChangeSlot.CloseWindow();
                            Singleton<CustomBase>.Instance.updateCvsAccessoryChange = true;
                        }
                        this.AccessorySlotCanvasGroupCallback(-2, info.tglItem, info.cgItem);
                    });
                    ((RectTransform)info.cgItem.transform).anchoredPosition += new Vector2(0f, 40f);
                }
            }

            this.ExecuteDelayed(() =>
            {
                this._cvsAccessory[0].UpdateCustomUI();
                ((Toggle)this._cvsAccessory[0].GetPrivate("tglTakeOverParent")).isOn = false;
                ((Toggle)this._cvsAccessory[0].GetPrivate("tglTakeOverColor")).isOn = false;
            }, 5);

            RectTransform content;
#if KOIKATSU
            container = (RectTransform)GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/04_AccessoryTop/tglCopy/CopyTop/rect").transform;
            this._charaMakerCopyScrollView = UIUtility.CreateScrollView("Slots", container);
            this._charaMakerCopyScrollView.movementType = ScrollRect.MovementType.Clamped;
            this._charaMakerCopyScrollView.horizontal = false;
            this._charaMakerCopyScrollView.scrollSensitivity = 18f;
            if (this._charaMakerCopyScrollView.horizontalScrollbar != null)
                Destroy(this._charaMakerCopyScrollView.horizontalScrollbar.gameObject);
            if (this._charaMakerCopyScrollView.verticalScrollbar != null)
                Destroy(this._charaMakerCopyScrollView.verticalScrollbar.gameObject);
            Destroy(this._charaMakerCopyScrollView.GetComponent<Image>());
            content = (RectTransform)container.Find("grpClothes");
            this._charaMakerCopyScrollView.transform.SetRect(content);
            content.SetParent(this._charaMakerCopyScrollView.viewport);
            Destroy(this._charaMakerCopyScrollView.content.gameObject);
            this._charaMakerCopyScrollView.content = content;
            this._copySlotTemplate = this._charaMakerCopyScrollView.content.GetChild(0).gameObject;
            this._raycastCtrls.Add(container.parent.GetComponent<UI_RaycastCtrl>());
            this._charaMakerCopyScrollView.transform.SetAsFirstSibling();
            this._charaMakerCopyScrollView.transform.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(16f, -570f), new Vector2(-16f, -80f));
#endif

            container = (RectTransform)GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/04_AccessoryTop/tglChange/ChangeTop/rect").transform;
            this._charaMakerTransferScrollView = UIUtility.CreateScrollView("Slots", container);
            this._charaMakerTransferScrollView.movementType = ScrollRect.MovementType.Clamped;
            this._charaMakerTransferScrollView.horizontal = false;
            this._charaMakerTransferScrollView.scrollSensitivity = 18f;
            if (this._charaMakerTransferScrollView.horizontalScrollbar != null)
                Destroy(this._charaMakerTransferScrollView.horizontalScrollbar.gameObject);
            if (this._charaMakerTransferScrollView.verticalScrollbar != null)
                Destroy(this._charaMakerTransferScrollView.verticalScrollbar.gameObject);
            Destroy(this._charaMakerTransferScrollView.GetComponent<Image>());
            content = (RectTransform)container.Find("grpClothes");
            this._charaMakerTransferScrollView.transform.SetRect(content);
            content.SetParent(this._charaMakerTransferScrollView.viewport);
            Destroy(this._charaMakerTransferScrollView.content.gameObject);
            this._charaMakerTransferScrollView.content = content;
            this._transferSlotTemplate = this._charaMakerTransferScrollView.content.GetChild(0).gameObject;
            this._raycastCtrls.Add(container.parent.GetComponent<UI_RaycastCtrl>());
            this._charaMakerTransferScrollView.transform.SetAsFirstSibling();
            this._charaMakerTransferScrollView.transform.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(16f, -530f), new Vector2(-16f, -48f));

            this._charaMakerScrollView.viewport.gameObject.SetActive(false);

            this.ExecuteDelayed(() => //Fixes problems with UI masks overlapping and creating bugs
            {
                this._charaMakerScrollView.viewport.gameObject.SetActive(true);
            }, 5);
            this.ExecuteDelayed(() =>
            {
                this.UpdateMakerUI();
                CustomBase.Instance.updateCustomUI = true;
            }, 2);
        }

#if KOIKATSU
        private void SpawnStudioUI()
        {
            Transform accList = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/01_State/Viewport/Content/Slot").transform;
            this._studioToggleTemplate = accList.Find("Slot20") as RectTransform;

            MPCharCtrl ctrl = ((MPCharCtrl)Studio.Studio.Instance.manipulatePanelCtrl.GetPrivate("charaPanelInfo").GetPrivate("m_MPCharCtrl"));

            this._studioToggleAll = new StudioSlotData();
            this._studioToggleAll.slot = (RectTransform)Instantiate(this._studioToggleTemplate.gameObject).transform;
            this._studioToggleAll.name = this._studioToggleAll.slot.GetComponentInChildren<Text>();
            this._studioToggleAll.onButton = this._studioToggleAll.slot.GetChild(1).GetComponent<Button>();
            this._studioToggleAll.offButton = this._studioToggleAll.slot.GetChild(2).GetComponent<Button>();
            this._studioToggleAll.name.text = "全て";
            this._studioToggleAll.slot.SetParent(this._studioToggleTemplate.parent);
            this._studioToggleAll.slot.localPosition = Vector3.zero;
            this._studioToggleAll.slot.localScale = Vector3.one;
            this._studioToggleAll.onButton.onClick = new Button.ButtonClickedEvent();
            this._studioToggleAll.onButton.onClick.AddListener(() =>
            {
                this._selectedStudioCharacter.charInfo.SetAccessoryStateAll(true);
                ctrl.CallPrivate("UpdateInfo");
                this.UpdateStudioUI();
            });
            this._studioToggleAll.offButton.onClick = new Button.ButtonClickedEvent();
            this._studioToggleAll.offButton.onClick.AddListener(() =>
            {
                this._selectedStudioCharacter.charInfo.SetAccessoryStateAll(false);
                ctrl.CallPrivate("UpdateInfo");
                this.UpdateStudioUI();
            });
            this._studioToggleAll.slot.SetAsLastSibling();

            this._studioToggleMain = new StudioSlotData();
            this._studioToggleMain.slot = (RectTransform)Instantiate(this._studioToggleTemplate.gameObject).transform;
            this._studioToggleMain.name = this._studioToggleMain.slot.GetComponentInChildren<Text>();
            this._studioToggleMain.onButton = this._studioToggleMain.slot.GetChild(1).GetComponent<Button>();
            this._studioToggleMain.offButton = this._studioToggleMain.slot.GetChild(2).GetComponent<Button>();
            this._studioToggleMain.name.text = "メイン";
            this._studioToggleMain.slot.SetParent(this._studioToggleTemplate.parent);
            this._studioToggleMain.slot.localPosition = Vector3.zero;
            this._studioToggleMain.slot.localScale = Vector3.one;
            this._studioToggleMain.onButton.onClick = new Button.ButtonClickedEvent();
            this._studioToggleMain.onButton.onClick.AddListener(() =>
            {
                this._selectedStudioCharacter.charInfo.SetAccessoryStateCategory(0, true);
                ctrl.CallPrivate("UpdateInfo");
                this.UpdateStudioUI();
            });
            this._studioToggleMain.offButton.onClick = new Button.ButtonClickedEvent();
            this._studioToggleMain.offButton.onClick.AddListener(() =>
            {
                this._selectedStudioCharacter.charInfo.SetAccessoryStateCategory(0, false);
                ctrl.CallPrivate("UpdateInfo");
                this.UpdateStudioUI();
            });
            this._studioToggleMain.slot.SetAsLastSibling();

            this._studioToggleSub = new StudioSlotData();
            this._studioToggleSub.slot = (RectTransform)Instantiate(this._studioToggleTemplate.gameObject).transform;
            this._studioToggleSub.name = this._studioToggleSub.slot.GetComponentInChildren<Text>();
            this._studioToggleSub.onButton = this._studioToggleSub.slot.GetChild(1).GetComponent<Button>();
            this._studioToggleSub.offButton = this._studioToggleSub.slot.GetChild(2).GetComponent<Button>();
            this._studioToggleSub.name.text = "サブ";
            this._studioToggleSub.slot.SetParent(this._studioToggleTemplate.parent);
            this._studioToggleSub.slot.localPosition = Vector3.zero;
            this._studioToggleSub.slot.localScale = Vector3.one;
            this._studioToggleSub.onButton.onClick = new Button.ButtonClickedEvent();
            this._studioToggleSub.onButton.onClick.AddListener(() =>
            {
                this._selectedStudioCharacter.charInfo.SetAccessoryStateCategory(1, true);
                ctrl.CallPrivate("UpdateInfo");
                this.UpdateStudioUI();
            });
            this._studioToggleSub.offButton.onClick = new Button.ButtonClickedEvent();
            this._studioToggleSub.offButton.onClick.AddListener(() =>
            {
                this._selectedStudioCharacter.charInfo.SetAccessoryStateCategory(1, false);
                ctrl.CallPrivate("UpdateInfo");
                this.UpdateStudioUI();
            });
            this._studioToggleSub.slot.SetAsLastSibling();

        }

        internal void SpawnHUI(List<ChaControl> females, HSprite hSprite)
        {
            this._hSceneFemales = females;
            this._additionalHSceneSlots = new List<List<HSceneSlotData>>();
            for (int i = 0; i < 2; i++)
                this._additionalHSceneSlots.Add(new List<HSceneSlotData>());
            this._hSprite = hSprite;
            this._hSceneMultipleFemaleButtons = this._hSprite.lstMultipleFemaleDressButton;
            this._hSceneSoloFemaleAccessoryButton = this._hSprite.categoryAccessory;
            this.UpdateHUI();
        }
#elif EMOTIONCREATORS
        //CharaUICtrl
        internal void SpawnPlayUI(HPlayHPartAccessoryCategoryUI ui)
        {
            this._playUI = ui;
            this._inPlay = true;
            this._additionalPlaySceneSlots.Clear();
            HPlayHPartUI.SelectUITextMesh[] buttons = (HPlayHPartUI.SelectUITextMesh[])ui.GetPrivate("accessoryCategoryUIs");
            this._playButtonTemplate = (RectTransform)buttons[0].btn.transform;
            this._playButtonTemplate.GetComponentInChildren<TextMeshProUGUI>().fontMaterial = new Material(this._playButtonTemplate.GetComponentInChildren<TextMeshProUGUI>().fontMaterial);
            int index = this._playButtonTemplate.parent.GetSiblingIndex();

            ScrollRect scrollView = UIUtility.CreateScrollView("ScrollView", this._playButtonTemplate.parent.parent);
            scrollView.transform.SetSiblingIndex(index);
            scrollView.transform.SetRect(this._playButtonTemplate.parent);
            ((RectTransform)scrollView.transform).offsetMax = new Vector2(this._playButtonTemplate.offsetMin.x + 192f, -88f);
            ((RectTransform)scrollView.transform).offsetMin = new Vector2(this._playButtonTemplate.offsetMin.x, -640f - 88f);
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
            scrollView.content = (RectTransform)this._playButtonTemplate.parent;
            this._playButtonTemplate.parent.SetParent(scrollView.viewport, false);
            this._playButtonTemplate.parent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ((RectTransform)this._playButtonTemplate.parent).anchoredPosition = Vector2.zero;
            this._playButtonTemplate.parent.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(0, 0, 0, 0);
            //foreach (HPlayHPartUI.SelectUITextMesh b in buttons)
            //    ((RectTransform)b.btn.transform).anchoredPosition = Vector2.zero;
        }

        internal void SpawnADVUI(AccessoryUICtrl ui)
        {
            this._advUI = ui;
            this._advToggleTemplate = (RectTransform)((Toggle)((Array)((Array)ui.GetPrivate("toggles")).GetValue(19).GetPrivate("toggles")).GetValue(0)).transform.parent.parent;

            Button[] buttons = (Button[])this._advUI.GetPrivate("buttonALL").GetPrivate("buttons");
            for (int i = 0; i < buttons.Length; i++)
            {
                Button b = buttons[i];
                int i1 = i;
                b.onClick.AddListener(() =>
                {
                    CharAdditionalData ad = this._accessoriesByChar[this._advUI.chaControl.chaFile];
                    for (int j = 0; j < ad.advState.Count; j++)
                        ad.advState[j] = i1 - 1;
                    this.UpdateADVUI();
                });
            }
        }

#endif

        internal void UpdateUI()
        {
            if (this._inCharaMaker)
                this.UpdateMakerUI();
#if KOIKATSU
            else if (this._inStudio)
                this.UpdateStudioUI();
            else if (this._inH)
                this.ExecuteDelayed(this.UpdateHUI);
#elif EMOTIONCREATORS
            else if (this._inPlay)
                this.UpdatePlayUI();
#endif
        }

        private void UpdateMakerUI()
        {
            if (this._customAcsChangeSlot == null)
                return;
            int count = this._charaMakerData.nowAccessories != null ? this._charaMakerData.nowAccessories.Count : 0;
            int i;
            for (i = 0; i < count; i++)
            {
                CharaMakerSlotData info;
                if (i < this._additionalCharaMakerSlots.Count)
                {
                    info = this._additionalCharaMakerSlots[i];
                    info.toggle.gameObject.SetActive(true);
                    if (i + 20 == CustomBase.Instance.selectSlot)
                        info.cvsAccessory.UpdateCustomUI();

                    info.transferSlotObject.SetActive(true);
                }
                else
                {
                    GameObject newSlot = Instantiate(this._charaMakerSlotTemplate, this._charaMakerScrollView.content);
                    info = new CharaMakerSlotData();
                    info.toggle = newSlot.GetComponent<Toggle>();
                    info.text = info.toggle.GetComponentInChildren<TextMeshProUGUI>();
                    info.canvasGroup = info.toggle.transform.GetChild(1).GetComponent<CanvasGroup>();
                    info.cvsAccessory = info.toggle.GetComponentInChildren<CvsAccessory>();
                    info.toggle.onValueChanged = new Toggle.ToggleEvent();
                    info.toggle.isOn = false;
                    int index = i + 20;
                    info.toggle.onValueChanged.AddListener(b =>
                    {
                        this.AccessorySlotToggleCallback(index, info.toggle);
                        this.AccessorySlotCanvasGroupCallback(index, info.toggle, info.canvasGroup);
                    });
                    info.text.text = $"スロット{index + 1:00}";
                    info.cvsAccessory.slotNo = (CvsAccessory.AcsSlotNo)index;
                    newSlot.name = "tglSlot" + (index + 1).ToString("00");
                    info.canvasGroup.Enable(false, false);

#if KOIKATSU
                    info.copySlotObject = Instantiate(this._copySlotTemplate, this._charaMakerCopyScrollView.content);
                    info.copyToggle = info.copySlotObject.GetComponentInChildren<Toggle>();
                    info.copySourceText = info.copySlotObject.transform.Find("srcText00").GetComponent<TextMeshProUGUI>();
                    info.copyDestinationText = info.copySlotObject.transform.Find("dstText00").GetComponent<TextMeshProUGUI>();
                    info.copyToggle.GetComponentInChildren<TextMeshProUGUI>().text = (index + 1).ToString("00");
                    info.copySourceText.text = "なし";
                    info.copyDestinationText.text = "なし";
                    info.copyToggle.onValueChanged = new Toggle.ToggleEvent();
                    info.copyToggle.isOn = false;
                    info.copyToggle.interactable = true;
                    info.copySlotObject.name = "kind" + index.ToString("00");
                    info.copyToggle.graphic.raycastTarget = true;
#endif

                    info.transferSlotObject = Instantiate(this._transferSlotTemplate, this._charaMakerTransferScrollView.content);
                    info.transferSourceToggle = info.transferSlotObject.transform.GetChild(1).GetComponentInChildren<Toggle>();
                    info.transferDestinationToggle = info.transferSlotObject.transform.GetChild(2).GetComponentInChildren<Toggle>();
                    info.transferSourceText = info.transferSourceToggle.GetComponentInChildren<TextMeshProUGUI>();
                    info.transferDestinationText = info.transferDestinationToggle.GetComponentInChildren<TextMeshProUGUI>();
                    info.transferSlotObject.transform.GetChild(0).GetComponentInChildren<TextMeshProUGUI>().text = (index + 1).ToString("00");
                    info.transferSourceText.text = "なし";
                    info.transferDestinationText.text = "なし";
                    info.transferSlotObject.name = "kind" + index.ToString("00");
                    info.transferSourceToggle.onValueChanged = new Toggle.ToggleEvent();
                    info.transferSourceToggle.onValueChanged.AddListener((b) =>
                    {
                        if (info.transferSourceToggle.isOn)
                            CvsAccessory_Patches.CvsAccessoryChange_Start_Patches.SetSourceIndex(index);
                    });
                    info.transferDestinationToggle.onValueChanged = new Toggle.ToggleEvent();
                    info.transferDestinationToggle.onValueChanged.AddListener((b) =>
                    {
                        if (info.transferDestinationToggle.isOn)
                            CvsAccessory_Patches.CvsAccessoryChange_Start_Patches.SetDestinationIndex(index);
                    });
                    info.transferSourceToggle.isOn = false;
                    info.transferDestinationToggle.isOn = false;
                    info.transferSourceToggle.graphic.raycastTarget = true;
                    info.transferDestinationToggle.graphic.raycastTarget = true;

                    this._additionalCharaMakerSlots.Add(info);
                    info.cvsAccessory.UpdateCustomUI();

                    if (this.onCharaMakerSlotAdded != null)
                        this.onCharaMakerSlotAdded(index, newSlot.transform);
                }
                info.cvsAccessory.UpdateSlotName();

            }
            for (; i < this._additionalCharaMakerSlots.Count; i++)
            {
                CharaMakerSlotData slot = this._additionalCharaMakerSlots[i];
                slot.toggle.gameObject.SetActive(false);
                slot.toggle.isOn = false;
                slot.transferSlotObject.SetActive(false);
            }
            this._addButtonsGroup.SetAsLastSibling();
        }

#if KOIKATSU
        internal void UpdateStudioUI()
        {
            if (this._selectedStudioCharacter == null)
                return;
            CharAdditionalData additionalData = this._accessoriesByChar[this._selectedStudioCharacter.charInfo.chaFile];
            int i;
            for (i = 0; i < additionalData.nowAccessories.Count; i++)
            {
                StudioSlotData slot;
                ChaFileAccessory.PartsInfo accessory = additionalData.nowAccessories[i];
                if (i < this._additionalStudioSlots.Count)
                {
                    slot = this._additionalStudioSlots[i];
                }
                else
                {
                    slot = new StudioSlotData();
                    slot.slot = (RectTransform)Instantiate(this._studioToggleTemplate.gameObject).transform;
                    slot.name = slot.slot.GetComponentInChildren<Text>();
                    slot.onButton = slot.slot.GetChild(1).GetComponent<Button>();
                    slot.offButton = slot.slot.GetChild(2).GetComponent<Button>();
                    slot.name.text = "スロット" + (21 + i);
                    slot.slot.SetParent(this._studioToggleTemplate.parent);
                    slot.slot.localPosition = Vector3.zero;
                    slot.slot.localScale = Vector3.one;
                    int i1 = i;
                    slot.onButton.onClick = new Button.ButtonClickedEvent();
                    slot.onButton.onClick.AddListener(() =>
                    {
                        this._accessoriesByChar[this._selectedStudioCharacter.charInfo.chaFile].showAccessories[i1] = true;
                        slot.onButton.image.color = Color.green;
                        slot.offButton.image.color = Color.white;
                    });
                    slot.offButton.onClick = new Button.ButtonClickedEvent();
                    slot.offButton.onClick.AddListener(() =>
                    {
                        this._accessoriesByChar[this._selectedStudioCharacter.charInfo.chaFile].showAccessories[i1] = false;
                        slot.offButton.image.color = Color.green;
                        slot.onButton.image.color = Color.white;
                    });
                    this._additionalStudioSlots.Add(slot);
                }
                slot.slot.gameObject.SetActive(true);
                slot.onButton.interactable = accessory != null && accessory.type != 120;
                slot.onButton.image.color = slot.onButton.interactable && additionalData.showAccessories[i] ? Color.green : Color.white;
                slot.offButton.interactable = accessory != null && accessory.type != 120;
                slot.offButton.image.color = slot.onButton.interactable && !additionalData.showAccessories[i] ? Color.green : Color.white;
            }
            for (; i < this._additionalStudioSlots.Count; ++i)
                this._additionalStudioSlots[i].slot.gameObject.SetActive(false);
            this._studioToggleSub.slot.SetAsFirstSibling();
            this._studioToggleMain.slot.SetAsFirstSibling();
            this._studioToggleAll.slot.SetAsFirstSibling();
        }

        private void UpdateHUI()
        {
            if (this._hSprite == null)
                return;
            for (int i = 0; i < this._hSceneFemales.Count; i++)
            {
                ChaControl female = this._hSceneFemales[i];

                CharAdditionalData additionalData = this._accessoriesByChar[female.chaFile];
                int j;
                List<HSceneSlotData> additionalSlots = this._additionalHSceneSlots[i];
                Transform buttonsParent = this._hSceneFemales.Count == 1 ? this._hSceneSoloFemaleAccessoryButton.transform : this._hSceneMultipleFemaleButtons[i].accessory.transform;
                for (j = 0; j < additionalData.nowAccessories.Count; j++)
                {
                    HSceneSlotData slot;
                    if (j < additionalSlots.Count)
                        slot = additionalSlots[j];
                    else
                    {
                        slot = new HSceneSlotData();
                        slot.slot = (RectTransform)Instantiate(buttonsParent.GetChild(0).gameObject).transform;
                        slot.text = slot.slot.GetComponentInChildren<TextMeshProUGUI>(true);
                        slot.button = slot.slot.GetComponentInChildren<Button>(true);
                        slot.slot.SetParent(buttonsParent);
                        slot.slot.localPosition = Vector3.zero;
                        slot.slot.localScale = Vector3.one;
                        int i1 = j;
                        slot.button.onClick = new Button.ButtonClickedEvent();
                        slot.button.onClick.AddListener(() =>
                        {
                            if (!Input.GetMouseButtonUp(0))
                                return;
                            if (!this._hSprite.IsSpriteAciotn())
                                return;
                            additionalData.showAccessories[i1] = !additionalData.showAccessories[i1];
                            Utils.Sound.Play(SystemSE.sel);
                        });
                        additionalSlots.Add(slot);
                    }
                    GameObject objAccessory = additionalData.objAccessory[j];
                    if (objAccessory == null)
                        slot.slot.gameObject.SetActive(false);
                    else
                    {
                        slot.slot.gameObject.SetActive(true);
                        ListInfoComponent component = objAccessory.GetComponent<ListInfoComponent>();
                        slot.text.text = component.data.Name;
                    }
                }

                for (; j < additionalSlots.Count; ++j)
                    additionalSlots[j].slot.gameObject.SetActive(false);
            }
        }
#elif EMOTIONCREATORS
        internal void UpdatePlayUI()
        {
            if (this._playUI == null || this._playButtonTemplate == null || this._playUI.selectChara == null)
                return;
            if (this._updatePlayUIHandler == null)
                this._updatePlayUIHandler = this.StartCoroutine(this.UpdatePlayUI_Routine());
        }

        // So, this thing is actually a coroutine because if I don't do the following, TextMeshPro start disappearing because their material is destroyed.
        // IDK, fuck you unity I guess
        private IEnumerator UpdatePlayUI_Routine()
        {
            while (this._playButtonTemplate.gameObject.activeInHierarchy == false)
                yield return null;
            ChaControl character = this._playUI.selectChara;

            CharAdditionalData additionalData = this._accessoriesByChar[character.chaFile];
            int j;
            for (j = 0; j < additionalData.nowAccessories.Count; j++)
            {
                PlaySceneSlotData slot;
                if (j < this._additionalPlaySceneSlots.Count)
                    slot = this._additionalPlaySceneSlots[j];
                else
                {
                    slot = new PlaySceneSlotData();
                    slot.slot = (RectTransform)Instantiate(this._playButtonTemplate.gameObject).transform;
                    slot.text = slot.slot.GetComponentInChildren<TextMeshProUGUI>(true);
                    slot.text.fontMaterial = new Material(slot.text.fontMaterial);
                    slot.button = slot.slot.GetComponentInChildren<Button>(true);
                    slot.slot.SetParent(this._playButtonTemplate.parent);
                    slot.slot.localPosition = Vector3.zero;
                    slot.slot.localScale = Vector3.one;
                    int i1 = j;
                    slot.button.onClick = new Button.ButtonClickedEvent();
                    slot.button.onClick.AddListener(() =>
                    {
                        additionalData.showAccessories[i1] = !additionalData.showAccessories[i1];
                    });
                    this._additionalPlaySceneSlots.Add(slot);
                }
                GameObject objAccessory = additionalData.objAccessory[j];
                if (objAccessory == null)
                    slot.slot.gameObject.SetActive(false);
                else
                {
                    slot.slot.gameObject.SetActive(true);
                    ListInfoComponent component = objAccessory.GetComponent<ListInfoComponent>();
                    slot.text.text = component.data.Name;
                }
            }

            for (; j < this._additionalPlaySceneSlots.Count; ++j)
                this._additionalPlaySceneSlots[j].slot.gameObject.SetActive(false);
            this._updatePlayUIHandler = null;
        }

        internal void UpdateADVUI()
        {
            if (this._advUI == null)
                return;

            CharAdditionalData additionalData = this._accessoriesByChar[this._advUI.chaControl.chaFile];
            int i = 0;
            for (; i < additionalData.nowAccessories.Count; ++i)
            {
                ADVSceneSlotData slot;
                if (i < this._additionalADVSceneSlots.Count)
                    slot = this._additionalADVSceneSlots[i];
                else
                {
                    slot = new ADVSceneSlotData();
                    slot.slot = (RectTransform)Instantiate(this._advToggleTemplate.gameObject).transform;
                    slot.slot.SetParent(this._advToggleTemplate.parent);
                    slot.slot.localPosition = Vector3.zero;
                    slot.slot.localRotation = Quaternion.identity;
                    slot.slot.localScale = Vector3.one;
                    slot.text = slot.slot.Find("TextMeshPro").GetComponent<TextMeshProUGUI>();
                    slot.keep = slot.slot.Find("Root/Button -1").GetComponent<Toggle>();
                    slot.wear = slot.slot.Find("Root/Button 0").GetComponent<Toggle>();
                    slot.takeOff = slot.slot.Find("Root/Button 1").GetComponent<Toggle>();
                    slot.text.text = "スロット" + (21 + i);

                    slot.keep.onValueChanged = new Toggle.ToggleEvent();
                    int i1 = i;
                    slot.keep.onValueChanged.AddListener(b =>
                    {
                        CharAdditionalData ad = this._accessoriesByChar[this._advUI.chaControl.chaFile];
                        ad.advState[i1] = -1;
                    });
                    slot.wear.onValueChanged = new Toggle.ToggleEvent();
                    slot.wear.onValueChanged.AddListener(b =>
                    {
                        CharAdditionalData ad = this._accessoriesByChar[this._advUI.chaControl.chaFile];
                        ad.advState[i1] = 0;
                        this._advUI.chaControl.SetAccessoryState(i1 + 20, true);
                    });
                    slot.takeOff.onValueChanged = new Toggle.ToggleEvent();
                    slot.takeOff.onValueChanged.AddListener(b =>
                    {
                        CharAdditionalData ad = this._accessoriesByChar[this._advUI.chaControl.chaFile];
                        ad.advState[i1] = 1;
                        this._advUI.chaControl.SetAccessoryState(i1 + 20, false);
                    });

                    this._additionalADVSceneSlots.Add(slot);
                }
                slot.slot.gameObject.SetActive(true);
                slot.keep.SetIsOnNoCallback(additionalData.advState[i] == -1);
                slot.keep.interactable = additionalData.objAccessory[i] != null;
                slot.wear.SetIsOnNoCallback(additionalData.advState[i] == 0);
                slot.wear.interactable = additionalData.objAccessory[i] != null;
                slot.takeOff.SetIsOnNoCallback(additionalData.advState[i] == 1);
                slot.takeOff.interactable = additionalData.objAccessory[i] != null;
            }
            for (; i < this._additionalADVSceneSlots.Count; i++)
                this._additionalADVSceneSlots[i].slot.gameObject.SetActive(false);
            RectTransform parent = (RectTransform)this._advToggleTemplate.parent.parent;
            parent.offsetMin = new Vector2(0, parent.offsetMax.y - 66 - 34 * (additionalData.nowAccessories.Count + 21));
            this.ExecuteDelayed(() =>
            {
                //Fuck you I'm going to bed
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)this._advToggleTemplate.parent.parent);
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)this._advToggleTemplate.parent.parent.parent);
            });
        }
#endif

        private void AddSlot()
        {
            if (_self._accessoriesByChar.TryGetValue(CustomBase.Instance.chaCtrl.chaFile, out this._charaMakerData) == false)
            {
                this._charaMakerData = new CharAdditionalData();
                this._accessoriesByChar.Add(CustomBase.Instance.chaCtrl.chaFile, this._charaMakerData);
            }
            if (this._charaMakerData.nowAccessories == null)
            {
                this._charaMakerData.nowAccessories = new List<ChaFileAccessory.PartsInfo>();
                this._charaMakerData.rawAccessoriesInfos.Add(CustomBase.Instance.chaCtrl.fileStatus.GetCoordinateType(), this._charaMakerData.nowAccessories);
            }
            ChaFileAccessory.PartsInfo partsInfo = new ChaFileAccessory.PartsInfo();
            this._charaMakerData.nowAccessories.Add(partsInfo);
            while (this._charaMakerData.infoAccessory.Count < this._charaMakerData.nowAccessories.Count)
                this._charaMakerData.infoAccessory.Add(null);
            while (this._charaMakerData.objAccessory.Count < this._charaMakerData.nowAccessories.Count)
                this._charaMakerData.objAccessory.Add(null);
            while (this._charaMakerData.objAcsMove.Count < this._charaMakerData.nowAccessories.Count)
                this._charaMakerData.objAcsMove.Add(new GameObject[2]);
            while (this._charaMakerData.cusAcsCmp.Count < this._charaMakerData.nowAccessories.Count)
                this._charaMakerData.cusAcsCmp.Add(null);
            while (this._charaMakerData.showAccessories.Count < this._charaMakerData.nowAccessories.Count)
                this._charaMakerData.showAccessories.Add(true);
            this.UpdateMakerUI();
        }

        private void AddTenSlot()
        {
            if (_self._accessoriesByChar.TryGetValue(CustomBase.Instance.chaCtrl.chaFile, out this._charaMakerData) == false)
            {
                this._charaMakerData = new CharAdditionalData();
                this._accessoriesByChar.Add(CustomBase.Instance.chaCtrl.chaFile, this._charaMakerData);
            }
            if (this._charaMakerData.nowAccessories == null)
            {
                this._charaMakerData.nowAccessories = new List<ChaFileAccessory.PartsInfo>();
                this._charaMakerData.rawAccessoriesInfos.Add(CustomBase.Instance.chaCtrl.fileStatus.GetCoordinateType(), this._charaMakerData.nowAccessories);
            }
            for (int i = 0; i < 10; i++)
            {
                ChaFileAccessory.PartsInfo partsInfo = new ChaFileAccessory.PartsInfo();
                this._charaMakerData.nowAccessories.Add(partsInfo);
            }
            while (this._charaMakerData.infoAccessory.Count < this._charaMakerData.nowAccessories.Count)
                this._charaMakerData.infoAccessory.Add(null);
            while (this._charaMakerData.objAccessory.Count < this._charaMakerData.nowAccessories.Count)
                this._charaMakerData.objAccessory.Add(null);
            while (this._charaMakerData.objAcsMove.Count < this._charaMakerData.nowAccessories.Count)
                this._charaMakerData.objAcsMove.Add(new GameObject[2]);
            while (this._charaMakerData.cusAcsCmp.Count < this._charaMakerData.nowAccessories.Count)
                this._charaMakerData.cusAcsCmp.Add(null);
            while (this._charaMakerData.showAccessories.Count < this._charaMakerData.nowAccessories.Count)
                this._charaMakerData.showAccessories.Add(true);
            this.UpdateMakerUI();
        }

        private void AccessorySlotToggleCallback(int index, Toggle toggle)
        {
            if (toggle.isOn)
            {
                CustomBase.Instance.selectSlot = index;
                bool open = this.GetPart(index).type != 120;
                this._customAcsParentWin.ChangeSlot(index, open);
                foreach (CustomAcsMoveWindow customAcsMoveWindow in this._customAcsMoveWin)
                    customAcsMoveWindow.ChangeSlot(index, open);
                foreach (CustomAcsSelectKind customAcsSelectKind in this._customAcsSelectKind)
                    customAcsSelectKind.ChangeSlot(index, open);

                Singleton<CustomBase>.Instance.selectSlot = index;
                if (index < 20)
                    Singleton<CustomBase>.Instance.SetUpdateCvsAccessory(index, true);
                else
                {
                    CvsAccessory accessory = this.GetCvsAccessory(index);
                    if (index == CustomBase.Instance.selectSlot)
                        accessory.UpdateCustomUI();
                    accessory.UpdateSlotName();
                }
                if ((int)this._customAcsChangeSlot.GetPrivate("backIndex") != index)
                    this._customAcsChangeSlot.ChangeColorWindow(index);
                this._customAcsChangeSlot.SetPrivate("backIndex", index);
            }
        }

        private void AccessorySlotCanvasGroupCallback(int index, Toggle toggle, CanvasGroup canvasGroup)
        {
            for (int i = 0; i < this._customAcsChangeSlot.items.Length; i++)
            {
                UI_ToggleGroupCtrl.ItemInfo info = this._customAcsChangeSlot.items[i];
                if (info.cgItem != null)
                    info.cgItem.Enable(false, false);
            }
            for (int i = 0; i < this._additionalCharaMakerSlots.Count; i++)
            {
                CharaMakerSlotData info = this._additionalCharaMakerSlots[i];
                if (info.canvasGroup != null)
                    info.canvasGroup.Enable(false, false);
            }
            if (toggle.isOn && canvasGroup)
                canvasGroup.Enable(true, false);
        }

        internal void OnCoordTypeChange()
        {
            if (this._inCharaMaker)
            {
                if (CustomBase.Instance.selectSlot >= 20 && !this._additionalCharaMakerSlots[CustomBase.Instance.selectSlot - 20].toggle.gameObject.activeSelf)
                {
                    Toggle toggle = this._customAcsChangeSlot.items[0].tglItem;
                    toggle.isOn = true;
                    CustomBase.Instance.selectSlot = 0;
                }
            }
            this.UpdateUI();
        }

        internal int GetSelectedMakerIndex()
        {
            for (int i = 0; i < 20; i++)
            {
                UI_ToggleGroupCtrl.ItemInfo info = this._customAcsChangeSlot.items[i];
                if (info.tglItem.isOn)
                    return i;
            }
            for (int i = 0; i < this._additionalCharaMakerSlots.Count; i++)
            {
                CharaMakerSlotData slot = this._additionalCharaMakerSlots[i];
                if (slot.toggle.isOn)
                    return i + 20;
            }
            return -1;
        }

        internal ChaFileAccessory.PartsInfo GetPart(int index)
        {
            if (index < 20)
                return CustomBase.Instance.chaCtrl.nowCoordinate.accessory.parts[index];
            return this._charaMakerData.nowAccessories[index - 20];
        }

        internal void SetPart(int index, ChaFileAccessory.PartsInfo value)
        {
            if (index < 20)
                CustomBase.Instance.chaCtrl.nowCoordinate.accessory.parts[index] = value;
            else
                this._charaMakerData.nowAccessories[index - 20] = value;
        }

        internal int GetPartsLength()
        {
            return this._charaMakerData.nowAccessories.Count + 20;
        }

        internal CvsAccessory GetCvsAccessory(int index)
        {
            if (index < 20)
                return this._cvsAccessory[index];
            return this._additionalCharaMakerSlots[index - 20].cvsAccessory;
        }
        #endregion

        #region Saves

        #region Sideloader
        [HarmonyPatch(typeof(UniversalAutoResolver), "IterateCardPrefixes")]
        private static class SideloaderAutoresolverHooks_IterateCardPrefixes_Patches
        {
            //private static bool Prepare()
            //{
            //    return Type.GetType($"Sideloader.AutoResolver.UniversalAutoResolver,{_sideloaderDll}") != null;
            //}

            //private static MethodInfo TargetMethod()
            //{
            //    return Type.GetType($"Sideloader.AutoResolver.UniversalAutoResolver,{_sideloaderDll}")
            //               .GetMethod("IterateCardPrefixes", BindingFlags.NonPublic | BindingFlags.Static);
            //}

            private static void Prefix(ChaFile file)
            {
                if (_self._overrideCharaLoadingFilePost == null)
                    _self._overrideCharaLoadingFilePost = file;
            }

            private static void Postfix()
            {
                _self._overrideCharaLoadingFilePost = null;
            }
        }

        [HarmonyPatch(typeof(UniversalAutoResolver), "IterateCoordinatePrefixes")]
        private static class SideloaderAutoresolverHooks_IterateCoordinatePrefixes_Patches
        {
            private static object _sideLoaderChaFileAccessoryPartsInfoProperties;
#if KOIKATSU
            private static PropertyInfo _resolveInfoProperty;

            private static bool Prepare()
            {
                _resolveInfoProperty = Type.GetType($"Sideloader.AutoResolver.ResolveInfo,Sideloader")
                                           .GetProperty("Property", BindingFlags.Public | BindingFlags.Instance);
                return true;
            }

            [HarmonyBefore("com.deathweasel.bepinex.guidmigration")]
            private static void Prefix(object extInfo)
            {
                if (extInfo != null)
                {
                    int i = 0;
                    foreach (object o in (IList)extInfo)
                    {
                        string property = (string)_resolveInfoProperty.GetValue(o, null);
                        if (property.StartsWith("outfit.")) //Sorry to whoever reads this, I fucked up
                        {
                            char[] array = property.ToCharArray();
                            array[6] = array[7];
                            array[7] = '.';
                            _resolveInfoProperty.SetValue(o, new string(array), null);
                        }
                        ++i;
                    }
                }
            }
#endif

            private static void Postfix(object action, ChaFileCoordinate coordinate, object extInfo, string prefix)
            {
                if (_sideLoaderChaFileAccessoryPartsInfoProperties == null)
                {
#if KOIKATSU
                    _sideLoaderChaFileAccessoryPartsInfoProperties = Type.GetType($"Sideloader.AutoResolver.StructReference,Sideloader")
#elif EMOTIONCREATORS
                    _sideLoaderChaFileAccessoryPartsInfoProperties = Type.GetType($"Sideloader.AutoResolver.StructReference,EC_Sideloader")
#endif
                                                                         .GetProperty("ChaFileAccessoryPartsInfoProperties", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).GetValue(null, null);
                }

                WeakReference o;
                ChaFileControl owner = null;
                if (_self._charByCoordinate.TryGetValue(coordinate, out o) == false || o.IsAlive == false)
                //if (_self._overrideCharaLoadingFilePost != null)
                //    owner = _self._overrideCharaLoadingFilePost;
                //else
                {
                    foreach (KeyValuePair<int, ChaControl> pair in Character.Instance.dictEntryChara)
                    {
                        if (pair.Value.nowCoordinate == coordinate)
                        {
                            owner = pair.Value.chaFile;
                            break;
                        }
#if KOIKATSU
                        foreach (ChaFileCoordinate c in pair.Value.chaFile.coordinate)
#elif EMOTIONCREATORS
                        ChaFileCoordinate c = pair.Value.chaFile.coordinate;
#endif
                        {
                            if (c == coordinate)
                            {
                                owner = pair.Value.chaFile;
                                goto DOUBLEBREAK;
                            }
                        }
                    }
                }
                else
                    owner = (ChaFileControl)o.Target;
                DOUBLEBREAK:

                if (owner == null)
                    return;

                CharAdditionalData additionalData;
                if (_self._accessoriesByChar.TryGetValue(owner, out additionalData) == false)
                    return;

                if (string.IsNullOrEmpty(prefix))
                {
                    for (int j = 0; j < additionalData.nowAccessories.Count; j++)
                        ((Delegate)action).DynamicInvoke(_sideLoaderChaFileAccessoryPartsInfoProperties, additionalData.nowAccessories[j], extInfo, $"{prefix}accessory{j + 20}.");
                }
                else
                {
#if KOIKATSU
                    string coordId = prefix.Replace("outfit", "").Replace(".", "");
                    if (int.TryParse(coordId, out int result) == false)
                        return;
#elif EMOTIONCREATORS
                    int result = 0;
#endif
                    List<ChaFileAccessory.PartsInfo> parts;
                    if (additionalData.rawAccessoriesInfos.TryGetValue(result, out parts) == false)
                        return;
                    for (int j = 0; j < parts.Count; j++)
                        ((Delegate)action).DynamicInvoke(_sideLoaderChaFileAccessoryPartsInfoProperties, parts[j], extInfo, $"{prefix}accessory{j + 20}.");
                }
            }
        }

        private static void UAR_ExtendedCardLoad_Prefix(ChaFile file)
        {
            _self.OnActualCharaLoad(file);
        }

        private static void UAR_ExtendedCardSave_Postfix(ChaFile file)
        {
            _self.OnActualCharaSave(file);
        }

        private static void UAR_ExtendedCoordLoad_Prefix(ChaFileCoordinate file)
        {
            _self.OnActualCoordLoad(file);
        }

        private static void UAR_ExtendedCoordSave_Postfix(ChaFileCoordinate file)
        {
            _self.OnActualCoordSave(file);
        }
        #endregion

        [HarmonyPatch(typeof(ChaFileControl), "LoadFileLimited", new[] { typeof(string), typeof(byte), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
        private static class ChaFileControl_LoadFileLimited_Patches
        {
            private static void Prefix(ChaFileControl __instance, bool coordinate = true)
            {
                if (_self._inCharaMaker && _self._customAcsChangeSlot != null)
                {
                    if (_self._overrideCharaLoadingFilePost == null)
                        _self._overrideCharaLoadingFilePost = __instance;
                    _self._loadAdditionalAccessories = coordinate;
                }
            }

            private static void Postfix()
            {
                _self._overrideCharaLoadingFilePost = null;
                _self._loadAdditionalAccessories = true;
            }
        }

        [HarmonyPatch(typeof(ChaFileControl), "LoadCharaFile", typeof(BinaryReader), typeof(bool), typeof(bool))]
        private static class ChaFileControl_LoadCharaFile_Patches
        {
            private static void Prefix(ChaFileControl __instance)
            {
                if (_self._overrideCharaLoadingFilePost == null)
                    _self._overrideCharaLoadingFilePost = __instance;
            }
            private static void Postfix()
            {
                _self._overrideCharaLoadingFilePost = null;
            }
        }

        [HarmonyPatch(typeof(CustomControl), "Entry")]
        private static class CustomScene_Initialize_Patches
        {
            private static void Prefix(ChaControl entryChara)
            {
                _self._overrideCharaLoadingFilePre = entryChara.chaFile;
            }
            private static void Postfix()
            {
                _self._overrideCharaLoadingFilePre = null;
            }
        }

        [HarmonyPatch(typeof(ChaFile), "CopyAll", typeof(ChaFile))]
        private static class ChaFile_CopyAll_Patches
        {
            private static void Prefix(ChaFile __instance, ChaFile _chafile)
            {
                if (__instance == _chafile)
                    return;
                if (_self._accessoriesByChar.TryGetValue(_chafile, out CharAdditionalData srcData) == false)
                {
                    srcData = new CharAdditionalData();
                    _self._accessoriesByChar.Add(_chafile, srcData);
                }
                if (_self._accessoriesByChar.TryGetValue(__instance, out CharAdditionalData dstData) == false)
                {
                    dstData = new CharAdditionalData();
                    _self._accessoriesByChar.Add(__instance, dstData);
                }
                dstData.LoadFrom(srcData);

                if (dstData.rawAccessoriesInfos.TryGetValue(__instance.status.GetCoordinateType(), out dstData.nowAccessories) == false)
                {
                    dstData.nowAccessories = new List<ChaFileAccessory.PartsInfo>();
                    dstData.rawAccessoriesInfos.Add(__instance.status.GetCoordinateType(), dstData.nowAccessories);
                }
            }
        }

        private void OnActualCharaLoad(ChaFile file)
        {
            if (this._loadAdditionalAccessories == false)
                return;

            PluginData pluginData = ExtendedSave.GetExtendedDataById(file, _extSaveKey);

            CharAdditionalData data;
            if (this._accessoriesByChar.TryGetValue(file, out data) == false)
            {
                data = new CharAdditionalData();
                this._accessoriesByChar.Add(file, data);
            }
            else
            {
                foreach (KeyValuePair<int, List<ChaFileAccessory.PartsInfo>> pair in data.rawAccessoriesInfos)
                    pair.Value.Clear();
            }
            XmlNode node = null;
            if (pluginData != null && pluginData.data.TryGetValue("additionalAccessories", out object xmlData))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml((string)xmlData);
                node = doc.FirstChild;
            }
            if (node != null)
            {
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    switch (childNode.Name)
                    {
                        case "accessorySet":
                            int coordinateType = XmlConvert.ToInt32(childNode.Attributes["type"].Value);
                            List<ChaFileAccessory.PartsInfo> parts;

                            if (data.rawAccessoriesInfos.TryGetValue(coordinateType, out parts) == false)
                            {
                                parts = new List<ChaFileAccessory.PartsInfo>();
                                data.rawAccessoriesInfos.Add(coordinateType, parts);
                            }

                            foreach (XmlNode accessoryNode in childNode.ChildNodes)
                            {
                                ChaFileAccessory.PartsInfo part = new ChaFileAccessory.PartsInfo();
                                part.type = XmlConvert.ToInt32(accessoryNode.Attributes["type"].Value);
                                if (part.type != 120)
                                {
                                    part.id = XmlConvert.ToInt32(accessoryNode.Attributes["id"].Value);
                                    part.parentKey = accessoryNode.Attributes["parentKey"].Value;

                                    for (int i = 0; i < 2; i++)
                                    {
                                        for (int j = 0; j < 3; j++)
                                        {
                                            part.addMove[i, j] = new Vector3
                                            {
                                                x = XmlConvert.ToSingle(accessoryNode.Attributes[$"addMove{i}{j}x"].Value),
                                                y = XmlConvert.ToSingle(accessoryNode.Attributes[$"addMove{i}{j}y"].Value),
                                                z = XmlConvert.ToSingle(accessoryNode.Attributes[$"addMove{i}{j}z"].Value)
                                            };
                                        }
                                    }
                                    for (int i = 0; i < 4; i++)
                                    {
                                        part.color[i] = new Color
                                        {
                                            r = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}r"].Value),
                                            g = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}g"].Value),
                                            b = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}b"].Value),
                                            a = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}a"].Value)
                                        };
                                    }
                                    part.hideCategory = XmlConvert.ToInt32(accessoryNode.Attributes["hideCategory"].Value);
#if EMOTIONCREATORS
                                    if (accessoryNode.Attributes["hideTiming"] != null)
                                        part.hideTiming = XmlConvert.ToInt32(accessoryNode.Attributes["hideTiming"].Value);
#endif
                                    if (this._hasDarkness)
                                        part.SetPrivateProperty("noShake", accessoryNode.Attributes["noShake"] != null && XmlConvert.ToBoolean(accessoryNode.Attributes["noShake"].Value));
                                }
                                parts.Add(part);
                            }
                            break;
#if KOIKATSU
                        case "visibility":
                            if (this._inStudio)
                            {
                                data.showAccessories = new List<bool>();
                                foreach (XmlNode grandChildNode in childNode.ChildNodes)
                                    data.showAccessories.Add(grandChildNode.Attributes?["value"] == null || XmlConvert.ToBoolean(grandChildNode.Attributes["value"].Value));
                            }
                            break;
#endif
                    }
                }

            }
            if (data.rawAccessoriesInfos.TryGetValue(file.status.GetCoordinateType(), out data.nowAccessories) == false)
            {
                data.nowAccessories = new List<ChaFileAccessory.PartsInfo>();
                data.rawAccessoriesInfos.Add(file.status.GetCoordinateType(), data.nowAccessories);
            }
            while (data.infoAccessory.Count < data.nowAccessories.Count)
                data.infoAccessory.Add(null);
            while (data.objAccessory.Count < data.nowAccessories.Count)
                data.objAccessory.Add(null);
            while (data.objAcsMove.Count < data.nowAccessories.Count)
                data.objAcsMove.Add(new GameObject[2]);
            while (data.cusAcsCmp.Count < data.nowAccessories.Count)
                data.cusAcsCmp.Add(null);
            while (data.showAccessories.Count < data.nowAccessories.Count)
                data.showAccessories.Add(true);
#if EMOTIONCREATORS
            while (data.advState.Count < data.nowAccessories.Count)
                data.advState.Add(-1);
#endif


            if (
#if KOIKATSU
                    this._inH ||
#endif
                    this._inCharaMaker
            )
                this.ExecuteDelayed(this.UpdateUI);
            else
                this.UpdateUI();
            this._accessoriesByChar.Purge();
            this._charByCoordinate.Purge();
        }

        private void OnActualCharaSave(ChaFile file)
        {
            CharAdditionalData data;
            if (this._accessoriesByChar.TryGetValue(file, out data) == false)
                return;

            using (StringWriter stringWriter = new StringWriter())
            using (XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter))
            {
                int maxCount = 0;
                xmlWriter.WriteStartElement("additionalAccessories");
                xmlWriter.WriteAttributeString("version", MoreAccessories.versionNum);
                foreach (KeyValuePair<int, List<ChaFileAccessory.PartsInfo>> pair in data.rawAccessoriesInfos)
                {
                    if (pair.Value.Count == 0)
                        continue;
                    xmlWriter.WriteStartElement("accessorySet");
                    xmlWriter.WriteAttributeString("type", XmlConvert.ToString((int)pair.Key));
                    if (maxCount < pair.Value.Count)
                        maxCount = pair.Value.Count;

                    for (int index = 0; index < pair.Value.Count; index++)
                    {
                        ChaFileAccessory.PartsInfo part = pair.Value[index];
                        xmlWriter.WriteStartElement("accessory");
                        xmlWriter.WriteAttributeString("type", XmlConvert.ToString(part.type));

                        if (part.type != 120)
                        {
                            xmlWriter.WriteAttributeString("id", XmlConvert.ToString(part.id));
                            xmlWriter.WriteAttributeString("parentKey", part.parentKey);

                            for (int i = 0; i < 2; i++)
                            {
                                for (int j = 0; j < 3; j++)
                                {
                                    Vector3 v = part.addMove[i, j];
                                    xmlWriter.WriteAttributeString($"addMove{i}{j}x", XmlConvert.ToString(v.x));
                                    xmlWriter.WriteAttributeString($"addMove{i}{j}y", XmlConvert.ToString(v.y));
                                    xmlWriter.WriteAttributeString($"addMove{i}{j}z", XmlConvert.ToString(v.z));
                                }
                            }
                            for (int i = 0; i < 4; i++)
                            {
                                Color c = part.color[i];
                                xmlWriter.WriteAttributeString($"color{i}r", XmlConvert.ToString(c.r));
                                xmlWriter.WriteAttributeString($"color{i}g", XmlConvert.ToString(c.g));
                                xmlWriter.WriteAttributeString($"color{i}b", XmlConvert.ToString(c.b));
                                xmlWriter.WriteAttributeString($"color{i}a", XmlConvert.ToString(c.a));
                            }
                            xmlWriter.WriteAttributeString("hideCategory", XmlConvert.ToString(part.hideCategory));
#if EMOTIONCREATORS
                            xmlWriter.WriteAttributeString("hideTiming", XmlConvert.ToString(part.hideTiming));
#endif
                            if (this._hasDarkness)
                                xmlWriter.WriteAttributeString("noShake", XmlConvert.ToString((bool)part.GetPrivateProperty("noShake")));
                        }
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();

                }

#if KOIKATSU
                if (this._inStudio)
                {
                    xmlWriter.WriteStartElement("visibility");
                    for (int i = 0; i < maxCount && i < data.showAccessories.Count; i++)
                    {
                        xmlWriter.WriteStartElement("visible");
                        xmlWriter.WriteAttributeString("value", XmlConvert.ToString(data.showAccessories[i]));
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();
                }
#endif

                xmlWriter.WriteEndElement();

                PluginData pluginData = new PluginData();
                pluginData.version = MoreAccessories._saveVersion;
                pluginData.data.Add("additionalAccessories", stringWriter.ToString());
                ExtendedSave.SetExtendedDataById(file, _extSaveKey, pluginData);
            }
        }

        private void OnActualCoordLoad(ChaFileCoordinate file)
        {
            if (this._inCharaMaker && this._loadCoordinatesWindow != null && this._loadCoordinatesWindow.tglCoordeLoadAcs != null && this._loadCoordinatesWindow.tglCoordeLoadAcs.isOn == false)
                this._loadAdditionalAccessories = false;
            if (this._loadAdditionalAccessories == false) // This stuff is done this way because some user might want to change _loadAdditionalAccessories manually through reflection.
            {
                this._loadAdditionalAccessories = true;
                return;
            }

            WeakReference o;
            ChaFileControl chaFile = null;
            if (_self._charByCoordinate.TryGetValue(file, out o) == false || o.IsAlive == false)
            {
                foreach (KeyValuePair<int, ChaControl> pair in Character.Instance.dictEntryChara)
                {
                    if (pair.Value.nowCoordinate == file)
                    {
                        chaFile = pair.Value.chaFile;
                        break;
                    }
                }
            }
            else
                chaFile = (ChaFileControl)o.Target;
            if (chaFile == null)
                return;
            CharAdditionalData data;
            if (this._accessoriesByChar.TryGetValue(chaFile, out data) == false)
            {
                data = new CharAdditionalData();
                this._accessoriesByChar.Add(chaFile, data);
            }
#if KOIKATSU
            if (this._inH)
                data.nowAccessories = new List<ChaFileAccessory.PartsInfo>();
            else
#endif
            {
                if (data.rawAccessoriesInfos.TryGetValue(chaFile.status.GetCoordinateType(), out data.nowAccessories) == false)
                {
                    data.nowAccessories = new List<ChaFileAccessory.PartsInfo>();
                    data.rawAccessoriesInfos.Add(chaFile.status.GetCoordinateType(), data.nowAccessories);
                }
                else
                    data.nowAccessories.Clear();
            }

            XmlNode node = null;
            PluginData pluginData = ExtendedSave.GetExtendedDataById(file, _extSaveKey);
            if (pluginData != null && pluginData.data.TryGetValue("additionalAccessories", out object xmlData))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml((string)xmlData);
                node = doc.FirstChild;
            }
            if (node != null)
            {
                foreach (XmlNode accessoryNode in node.ChildNodes)
                {
                    ChaFileAccessory.PartsInfo part = new ChaFileAccessory.PartsInfo();
                    part.type = XmlConvert.ToInt32(accessoryNode.Attributes["type"].Value);
                    if (part.type != 120)
                    {
                        part.id = XmlConvert.ToInt32(accessoryNode.Attributes["id"].Value);
                        part.parentKey = accessoryNode.Attributes["parentKey"].Value;

                        for (int i = 0; i < 2; i++)
                        {
                            for (int j = 0; j < 3; j++)
                            {
                                part.addMove[i, j] = new Vector3
                                {
                                    x = XmlConvert.ToSingle(accessoryNode.Attributes[$"addMove{i}{j}x"].Value),
                                    y = XmlConvert.ToSingle(accessoryNode.Attributes[$"addMove{i}{j}y"].Value),
                                    z = XmlConvert.ToSingle(accessoryNode.Attributes[$"addMove{i}{j}z"].Value)
                                };
                            }
                        }
                        for (int i = 0; i < 4; i++)
                        {
                            part.color[i] = new Color
                            {
                                r = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}r"].Value),
                                g = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}g"].Value),
                                b = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}b"].Value),
                                a = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}a"].Value)
                            };
                        }
                        part.hideCategory = XmlConvert.ToInt32(accessoryNode.Attributes["hideCategory"].Value);
#if EMOTIONCREATORS
                        if (accessoryNode.Attributes["hideTiming"] != null)
                            part.hideTiming = XmlConvert.ToInt32(accessoryNode.Attributes["hideTiming"].Value);
#endif
                        if (this._hasDarkness)
                            part.SetPrivateProperty("noShake", accessoryNode.Attributes["noShake"] != null && XmlConvert.ToBoolean(accessoryNode.Attributes["noShake"].Value));
                    }
                    data.nowAccessories.Add(part);
                }
            }

            while (data.infoAccessory.Count < data.nowAccessories.Count)
                data.infoAccessory.Add(null);
            while (data.objAccessory.Count < data.nowAccessories.Count)
                data.objAccessory.Add(null);
            while (data.objAcsMove.Count < data.nowAccessories.Count)
                data.objAcsMove.Add(new GameObject[2]);
            while (data.cusAcsCmp.Count < data.nowAccessories.Count)
                data.cusAcsCmp.Add(null);
            while (data.showAccessories.Count < data.nowAccessories.Count)
                data.showAccessories.Add(true);
#if EMOTIONCREATORS
            while (data.advState.Count < data.nowAccessories.Count)
                data.advState.Add(-1);
#endif

            if (
#if KOIKATSU
                    this._inH ||
#endif
                    this._inCharaMaker
            )
                this.ExecuteDelayed(this.UpdateUI);
            else
                this.UpdateUI();
            this._accessoriesByChar.Purge();
            this._charByCoordinate.Purge();
        }

        private void OnActualCoordSave(ChaFileCoordinate file)
        {
            WeakReference o;
            ChaFileControl chaFile = null;
            if (_self._charByCoordinate.TryGetValue(file, out o) == false || o.IsAlive == false)
            {
                foreach (KeyValuePair<int, ChaControl> pair in Character.Instance.dictEntryChara)
                {
                    if (pair.Value.nowCoordinate == file)
                    {
                        chaFile = pair.Value.chaFile;
                        break;
                    }
                }
            }
            else
                chaFile = (ChaFileControl)o.Target;
            if (chaFile == null)
                return;

            CharAdditionalData data;
            if (this._accessoriesByChar.TryGetValue(chaFile, out data) == false)
                return;
            using (StringWriter stringWriter = new StringWriter())
            using (XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter))
            {
                xmlWriter.WriteStartElement("additionalAccessories");
                xmlWriter.WriteAttributeString("version", MoreAccessories.versionNum);
                foreach (ChaFileAccessory.PartsInfo part in data.nowAccessories)
                {
                    xmlWriter.WriteStartElement("accessory");
                    xmlWriter.WriteAttributeString("type", XmlConvert.ToString(part.type));
                    if (part.type != 120)
                    {
                        xmlWriter.WriteAttributeString("id", XmlConvert.ToString(part.id));
                        xmlWriter.WriteAttributeString("parentKey", part.parentKey);
                        for (int i = 0; i < 2; i++)
                        {
                            for (int j = 0; j < 3; j++)
                            {
                                Vector3 v = part.addMove[i, j];
                                xmlWriter.WriteAttributeString($"addMove{i}{j}x", XmlConvert.ToString(v.x));
                                xmlWriter.WriteAttributeString($"addMove{i}{j}y", XmlConvert.ToString(v.y));
                                xmlWriter.WriteAttributeString($"addMove{i}{j}z", XmlConvert.ToString(v.z));
                            }
                        }
                        for (int i = 0; i < 4; i++)
                        {
                            Color c = part.color[i];
                            xmlWriter.WriteAttributeString($"color{i}r", XmlConvert.ToString(c.r));
                            xmlWriter.WriteAttributeString($"color{i}g", XmlConvert.ToString(c.g));
                            xmlWriter.WriteAttributeString($"color{i}b", XmlConvert.ToString(c.b));
                            xmlWriter.WriteAttributeString($"color{i}a", XmlConvert.ToString(c.a));
                        }
                        xmlWriter.WriteAttributeString("hideCategory", XmlConvert.ToString(part.hideCategory));
#if EMOTIONCREATORS
                        xmlWriter.WriteAttributeString("hideTiming", XmlConvert.ToString(part.hideTiming));
#endif
                        if (this._hasDarkness)
                            xmlWriter.WriteAttributeString("noShake", XmlConvert.ToString((bool)part.GetPrivateProperty("noShake")));
                    }
                    xmlWriter.WriteEndElement();
                }
                xmlWriter.WriteEndElement();

                PluginData pluginData = new PluginData();
                pluginData.version = MoreAccessories._saveVersion;
                pluginData.data.Add("additionalAccessories", stringWriter.ToString());
                ExtendedSave.SetExtendedDataById(file, _extSaveKey, pluginData);
            }
        }
        #endregion
    }
}
