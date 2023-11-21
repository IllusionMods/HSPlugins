using System;
using System.Collections.Generic;
using System.Reflection;
#if IPA
using Harmony;
#elif BEPINEX
using HarmonyLib;
#endif
#if HONEYSELECT
using FBSAssist;
using System.Reflection.Emit;
using System.IO;
using System.Globalization;
using System.Threading;
using UniRx.Triggers;
using Manager;
#endif
using System.Linq;
using Studio;
using ToolBox;
using ToolBox.Extensions;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace HSUS.Features
{
    public class OptimizeNEO : IFeature
    {
#if HONEYSELECT
        private readonly List<MethodInfo> _patchedMethods = new List<MethodInfo>()
        {
            typeof(CharaListInfo).GetMethod("LoadListInfoAll", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static),
            typeof(CharBody).GetMethod("OnDestroy", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static),
            typeof(CharFemaleBody).GetCoroutineMethod("ChangeHairAsync"),
            typeof(CharFemaleBody).GetCoroutineMethod("ChangeHeadAsync"),
            typeof(CharFemaleBody).GetCoroutineMethod("LoadAsync"),
            typeof(CharFemaleBody).GetMethod("Reload", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static),
            typeof(CharFemaleCustom).GetMethod("ChangeBodyDetailTex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static),
            typeof(CharFemaleCustom).GetMethod("ChangeFaceDetailTex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static),
            typeof(CharFemaleCustom).GetMethod("CreateBodyTexture", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static),
            typeof(CharFemaleCustom).GetMethod("CreateFaceTexture", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static),
            typeof(CharFemaleCustom).GetMethod("ReleaseBodyCustomTexture", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static),
            typeof(CharFemaleCustom).GetMethod("ReleaseFaceCustomTexture", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static),
            typeof(CharMale).GetMethod("EndSavePlayerFile", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static),
            typeof(CharMaleBody).GetCoroutineMethod("ChangeHairAsync"),
            typeof(CharMaleBody).GetCoroutineMethod("ChangeHeadAsync"),
            typeof(CharMaleBody).GetCoroutineMethod("LoadAsync"),
            typeof(CharMaleBody).GetMethod("Reload", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static),
            typeof(CharMaleCustom).GetMethod("ChangeFaceDetailTex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static),
            typeof(CharMaleCustom).GetMethod("CreateFaceTexture", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static),
            typeof(CharMaleCustom).GetMethod("ReleaseFaceCustomTexture", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static),
            typeof(GameScene).GetMethod("LoadCharaImage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static),
            typeof(GameScene).GetMethod("LoadMaleImage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static),
            typeof(GameScene).GetMethod("LoadSelectCharaImage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static),
            typeof(HScene).GetMethod("ChangeAnimation", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static),
            typeof(HSceneClothChange).GetMethod("LoadCharaImage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static),
            typeof(HSceneVR).GetMethod("ChangeAnimation", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static),
            typeof(Manager.Character).GetMethod("EndLoadAssetBundle", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static),
            typeof(Manager.Map).GetMethod("LoadMap", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static),
            typeof(Manager.Scene).GetCoroutineMethod("LoadMapScene"),
            typeof(Manager.Scene).GetCoroutineMethod("LoadStart"),
            typeof(Manager.Scene).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).FirstOrDefault(m => m.GetParameters().Length == 0),
            typeof(MapSelectScene).GetMethod("LoadCharaImage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static),
            typeof(MapSelectScene).GetMethod("SetMapImage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static),
            typeof(MorphBase).GetMethod("CreateCalcInfo", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static),
            typeof(VRScene).GetMethod("ChangeAnimation", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static),
        };

        private static readonly HarmonyExtensions.Replacement[] _replacements =
        {
            new HarmonyExtensions.Replacement()
            {
                pattern = new[]
                {
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GC), nameof(GC.Collect))),
                },
                replacer = new[]
                {
                    new CodeInstruction(OpCodes.Call, typeof(OptimizeNEO).GetMethod(nameof(ScheduleGCCollect), BindingFlags.NonPublic | BindingFlags.Static)),
                }
            },
            new HarmonyExtensions.Replacement()
            {
                pattern = new[]
                {
                    new CodeInstruction(OpCodes.Call, typeof(Resources).GetMethod(nameof(Resources.UnloadUnusedAssets), AccessTools.all)),
                },
                replacer = new[]
                {
                    new CodeInstruction(OpCodes.Call, typeof(OptimizeNEO).GetMethod(nameof(ScheduleUnloadUnusedAssets), BindingFlags.NonPublic | BindingFlags.Static)),
                }
            },
        };

        private static int _gcCollectCountdown = -1;
        private static int _unloadUnusedAssetsCountdown = -1;

        internal static bool _isCleaningResources { get { return _gcCollectCountdown >= 0 || _unloadUnusedAssetsCountdown >= 0; } }
#endif

        public void Awake()
        {
#if HONEYSELECT
            HSUS._self._onUpdate += this.Update;

            // Adding those manually because they might not exist depending on the context
            this.AddMethodManually("Studio.MPCharCtrl+CostumeInfo,Assembly-CSharp.dll", "LoadImage");
            this.AddMethodManually("SteamVR_LoadLevel,Assembly-CSharp.dll", "LoadLevel");
            this.AddMethodManually("Studio.BackgroundCtrl,Assembly-CSharp.dll", "Load");
            this.AddMethodManually("Studio.CharaList,Assembly-CSharp.dll", "LoadCharaImage");
            this.AddMethodManually("Studio.SceneLoadScene,Assembly-CSharp.dll", "SetPage");
#endif
        }

        public void LevelLoaded()
        {
#if HONEYSELECT
            if (_optimizeNeo && HSUS._self._binary == Binary.Studio && HSUS._self._level == 3)
            {
                HarmonyMethod transpiler = new HarmonyMethod(typeof(OptimizeNEO), nameof(GeneralTranspiler), new[] { typeof(IEnumerable<CodeInstruction>) });
                foreach (MethodInfo methodInfo in _patchedMethods)
                {
                    if (methodInfo.IsGenericMethod == false)
                        HSUS._self._harmonyInstance.Patch(methodInfo, transpiler: transpiler);
                }
            }
#endif
        }

#if HONEYSELECT
        private void AddMethodManually(string type, string method)
        {
            Type t = Type.GetType(type);
            if (t != null)
            {
                MethodInfo m = t.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (m != null)
                    this._patchedMethods.Add(m);
            }
        }

        private void Update()
        {
            if (_unloadUnusedAssetsCountdown != -1)
                --_unloadUnusedAssetsCountdown;
            if (_unloadUnusedAssetsCountdown == 0)
                Resources.UnloadUnusedAssets();
            if (_gcCollectCountdown != -1)
                --_gcCollectCountdown;
            if (_gcCollectCountdown == 0)
                GC.Collect();
        }

        private static IEnumerable<CodeInstruction> GeneralTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return HarmonyExtensions.ReplaceCodePattern(instructions, _replacements);
        }

        private static void ScheduleGCCollect()
        {
            _gcCollectCountdown = 8;
        }

        private static AsyncOperation ScheduleUnloadUnusedAssets()
        {
            _unloadUnusedAssetsCountdown = 8;
            return null;
        }
#endif

#if HONEYSELECT || KOIKATSU
        [HarmonyPatch(typeof(ItemList), "Awake")]
        public static class ItemList_Awake_Patches
        {
            public static bool Prepare()
            {
                return HSUS.OptimizeStudio.Value;
            }

            public static void Prefix(ItemList __instance)
            {
                ItemList_InitList_Patches.ItemListData data;
                if (ItemList_InitList_Patches._dataByInstance.TryGetValue(__instance, out data) == false)
                {
                    data = new ItemList_InitList_Patches.ItemListData();
                    ItemList_InitList_Patches._dataByInstance.Add(__instance, data);
                }

                Transform transformRoot = (Transform)__instance.GetPrivate("transformRoot");

                RectTransform rt = transformRoot.parent as RectTransform;
                rt.offsetMin += new Vector2(0f, 18f);
                float newY = rt.offsetMin.y;

                data.searchBar = UIUtility.CreateInputField("Search Bar", transformRoot.parent.parent, "Search...");
                Image image = data.searchBar.GetComponent<Image>();
                image.color = UIUtility.grayColor;
                //image.sprite = null;
                rt = data.searchBar.transform as RectTransform;
                rt.localPosition = Vector3.zero;
                rt.localScale = Vector3.one;
#if HONEYSELECT
                rt.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(ImproveNeoUI._improveNeoUI ? 10f : 7f, 16f), new Vector2(ImproveNeoUI._improveNeoUI ? -22f : -14f, newY));
#elif KOIKATSU || AISHOUJO
                rt.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(8f, 20f), new Vector2(-18f, newY));
#endif
                data.searchBar.onValueChanged.AddListener(s => SearchChanged(__instance));
                foreach (Text t in data.searchBar.GetComponentsInChildren<Text>())
                    t.color = Color.white;
            }

            public static void ResetSearch(ItemList instance)
            {
                ItemList_InitList_Patches.ItemListData data;
                if (ItemList_InitList_Patches._dataByInstance.TryGetValue(instance, out data) == false)
                {
                    data = new ItemList_InitList_Patches.ItemListData();
                    ItemList_InitList_Patches._dataByInstance.Add(instance, data);
                }
                if (data.searchBar != null)
                {
                    data.searchBar.text = "";
                    SearchChanged(instance);
                }
            }

            private static void SearchChanged(ItemList instance)
            {
                ItemList_InitList_Patches.ItemListData data;
                if (ItemList_InitList_Patches._dataByInstance.TryGetValue(instance, out data) == false)
                {
                    data = new ItemList_InitList_Patches.ItemListData();
                    ItemList_InitList_Patches._dataByInstance.Add(instance, data);
                }
                string search = data.searchBar.text.Trim();
#if HONEYSELECT
                int currentGroup = (int)instance.GetPrivate("group");
#elif KOIKATSU || AISHOUJO
                var currentGroup = ItemList_InitList_Patches.ItemListData.MakeGroupId(instance.group, instance.category);
#endif
                List<StudioNode> list;
                if (data.objects.TryGetValue(currentGroup, out list) == false)
                    return;
                foreach (StudioNode objectData in list)
                {
                    objectData.active = objectData.textUI.text.IndexOf(search, StringComparison.OrdinalIgnoreCase) != -1;
                }
            }
        }

        [HarmonyPatch(typeof(ItemList), "InitList", new[]
        {
            typeof(int),
#if KOIKATSU
            typeof(int),
#endif
        })]
        public static class ItemList_InitList_Patches
        {
            public class ItemListData
            {
                public static ulong MakeGroupId(int group, int category)
                {
                    return ((ulong)group << 32) | (uint)category;
                }
                public readonly Dictionary<ulong, List<StudioNode>> objects = new Dictionary<ulong, List<StudioNode>>();
                public InputField searchBar;
            }

            public static readonly Dictionary<ItemList, ItemListData> _dataByInstance = new Dictionary<ItemList, ItemListData>();

#if KOIKATSU
            private static ulong _lastGroupId =  ulong.MaxValue;
#endif

            public static bool Prepare()
            {
                return HSUS.OptimizeStudio.Value;
            }

#if HONEYSELECT
            public static bool Prefix(ItemList __instance, int _group)
            {
                ItemListData data;
                if (_dataByInstance.TryGetValue(__instance, out data) == false)
                {
                    data = new ItemListData();
                    _dataByInstance.Add(__instance, data);
                }

                ItemList_Awake_Patches.ResetSearch(__instance);

                int currentGroup = (int)__instance.GetPrivate("group");
                if (currentGroup == _group)
                    return false;
                ((ScrollRect)__instance.GetPrivate("scrollRect")).verticalNormalizedPosition = 1f;

                List<StudioNode> list;
                if (data.objects.TryGetValue(currentGroup, out list))
                    foreach (StudioNode studioNode in list)
                        studioNode.active = false;
                if (data.objects.TryGetValue(_group, out list))
                    foreach (StudioNode studioNode in list)
                        studioNode.active = true;
                else
                {
                    list = new List<StudioNode>();
                    data.objects.Add(_group, list);

                    Transform transformRoot = (Transform)__instance.GetPrivate("transformRoot");
                    GameObject objectNode = (GameObject)__instance.GetPrivate("objectNode");
                    foreach (KeyValuePair<int, Studio.Info.ItemLoadInfo> item in Singleton<Studio.Info>.Instance.dicItemLoadInfo)
                    {
                        if (item.Value.@group != _group)
                            continue;
                        GameObject gameObject = GameObject.Instantiate(objectNode);
                        gameObject.transform.SetParent(transformRoot, false);
                        StudioNode component = gameObject.GetComponent<StudioNode>();
                        component.active = true;
                        int no = item.Key;
                        component.addOnClick = () => { Studio.Studio.Instance.AddItem(no); };
                        component.text = item.Value.name;
                        component.textColor = ((!(item.Value.isColor & item.Value.isColor2)) ? ((!(item.Value.isColor | item.Value.isColor2)) ? Color.white : Color.red) : Color.cyan);
                        if (item.Value.isColor || item.Value.isColor2)
                        {
                            Shadow shadow = (component.textUI).gameObject.AddComponent<Shadow>();
                            shadow.effectColor = Color.black;
                        }
                        list.Add(component);
                    }
                }
                if (!__instance.gameObject.activeSelf)
                    __instance.gameObject.SetActive(true);
                __instance.SetPrivate("group", _group);
                return false;
            }
#elif KOIKATSU
            public static bool Prefix(ItemList __instance, int _group, int _category, ref int ___group, ref int ___category, ScrollRect ___scrollRect, Transform ___transformRoot, GameObject ___objectNode)
            {
                if (___group == _group && ___category == _category)
                {
                    return false;
                }

                ItemListData data;
                if (_dataByInstance.TryGetValue(__instance, out data) == false)
                {
                    data = new ItemListData();
                    _dataByInstance.Add(__instance, data);
                }

                ___scrollRect.verticalNormalizedPosition = 1f;

                List<StudioNode> list;
                if (data.objects.TryGetValue(_lastGroupId, out list))
                    foreach (StudioNode studioNode in list)
                        studioNode.active = false;
                var groupId = ItemListData.MakeGroupId(_group, _category);
                if (data.objects.TryGetValue(groupId, out list))
                    foreach (StudioNode studioNode in list)
                        studioNode.active = true;
                else
                {
                    list = new List<StudioNode>();
                    data.objects.Add(groupId, list);

                    foreach (KeyValuePair<int, Info.ItemLoadInfo> keyValuePair in Singleton<Info>.Instance.dicItemLoadInfo[_group][_category])
                    {
                        GameObject gameObject = GameObject.Instantiate(___objectNode);
                        gameObject.transform.SetParent(___transformRoot, false);
                        StudioNode component = gameObject.GetComponent<StudioNode>();
                        component.active = true;
                        int no = keyValuePair.Key;
                        component.addOnClick = () => __instance.CallPrivate("OnSelect", no);
                        component.text = keyValuePair.Value.name;
                        int num = keyValuePair.Value.color.Count(b => b) + (!keyValuePair.Value.isGlass ? 0 : 1);
                        switch (num)
                        {
                            case 1:
                                component.textColor = Color.red;
                                break;
                            case 2:
                                component.textColor = Color.cyan;
                                break;
                            case 3:
                                component.textColor = Color.green;
                                break;
                            case 4:
                                component.textColor = Color.yellow;
                                break;
                            default:
                                component.textColor = Color.white;
                                break;
                        }
                        if (num != 0 && component.textUI)
                        {
                            Shadow shadow = component.textUI.gameObject.AddComponent<Shadow>();
                            shadow.effectColor = Color.black;
                        }
                        list.Add(component);
                    }
                }
                if (!__instance.gameObject.activeSelf)
                    __instance.gameObject.SetActive(true);
                ___group = _group;
                ___category = _category;
                _lastGroupId = groupId;
                ItemList_Awake_Patches.ResetSearch(__instance);
                return false;
            }
#endif
        }
#endif

#if KOIKATSU || AISHOUJO || HONEYSELECT2
        [HarmonyPatch(typeof(CharaList), "Awake")]
        internal static class CharaList_Awake_Patches
        {
            private static bool Prepare()
            {
                return HSUS.OptimizeStudio.Value;
            }

            private static void Postfix(CharaList __instance)
            {
                Transform viewport = __instance.transform.Find("Scroll View/Viewport");

                RectTransform rt = viewport as RectTransform;
                rt.offsetMin += new Vector2(0f, 18f);
                float newY = rt.offsetMin.y;

                InputField searchBar = UIUtility.CreateInputField("Search Bar", viewport.parent, "Search...");
                searchBar.image.color = UIUtility.grayColor;
                searchBar.transform.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(8f, 11f), new Vector2(-18f, newY));
                List<CharaFileInfo> items = ((CharaFileSort)__instance.GetPrivate("charaFileSort")).cfiList;
                searchBar.onValueChanged.AddListener(s => SearchUpdated(searchBar.text, items));
                foreach (Text t in searchBar.GetComponentsInChildren<Text>())
                    t.color = Color.white;
            }

            private static void SearchUpdated(string text, List<CharaFileInfo> items)
            {
                foreach (CharaFileInfo info in items)
                    info.node.gameObject.SetActive(info.node.text.IndexOf(text, StringComparison.OrdinalIgnoreCase) != -1);
            }
        }

        [HarmonyPatch]
        internal static class CostumeInfo_Init_Patches
        {
            internal static bool Prepare()
            {
                return HSUS.OptimizeStudio.Value && HSUS._self.binary == Binary.Studio;
            }

            internal static MethodInfo TargetMethod()
            {
                return typeof(MPCharCtrl).GetNestedType("CostumeInfo", BindingFlags.NonPublic).GetMethod("Init", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            }

            private static void Postfix(object __instance)
            {
                CharaFileSort fileSort = (CharaFileSort)__instance.GetPrivate("fileSort");
                Transform viewport = fileSort.root.parent;

                RectTransform rt = viewport as RectTransform;
                rt.offsetMin += new Vector2(0f, 18f);

                InputField searchBar = UIUtility.CreateInputField("Search Bar", viewport.parent, "Search...");
                searchBar.image.color = UIUtility.grayColor;
                searchBar.transform.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(8f, 11f), new Vector2(-18f, 33f));
                List<CharaFileInfo> items = fileSort.cfiList;
                searchBar.onValueChanged.AddListener(s => SearchUpdated(searchBar.text, items));
                foreach (Text t in searchBar.GetComponentsInChildren<Text>())
                    t.color = Color.white;
            }

            private static void SearchUpdated(string text, List<CharaFileInfo> items)
            {
                foreach (CharaFileInfo info in items)
                    info.node.gameObject.SetActive(info.node.text.IndexOf(text, StringComparison.OrdinalIgnoreCase) != -1);
            }
        }

#elif HONEYSELECT
        internal class EntryListData
        {
            public class FolderData
            {
                public string fullPath;
                public string name;
                public GameObject displayObject;
                public Button button;
                public Text text;
            }

            public class EntryData
            {
                public GameSceneNode node;
                public Button button;
                public bool enabled;
            }

            public static Dictionary<object, EntryListData> dataByInstance = new Dictionary<object, EntryListData>();

            public string currentPath = "";
            public string previousCurrentPath = "";
            public GameObject parentFolder;
            public readonly List<FolderData> folders = new List<FolderData>();
            public readonly List<EntryData> entries = new List<EntryData>();
            public CharaFileSort sort;
        }

        [HarmonyPatch(typeof(CharaList), "InitCharaList", typeof(bool))]
        internal static class CharaList_InitCharaList_Patches
        {
            private static bool Prepare()
            {
                return _optimizeNeo;
            }

            private static bool Prefix(CharaList __instance, bool _force, CharaFileSort ___charaFileSort, int ___sex, GameObject ___objectNode, RawImage ___imageChara, Button ___buttonLoad, Button ___buttonChange)
            {
                if (__instance.isInit && !_force)
                {
                    return false;
                }
                EntryListData data;
                if (EntryListData.dataByInstance.TryGetValue(__instance, out data) == false)
                {
                    data = new EntryListData();
                    data.sort = ___charaFileSort;
                    EntryListData.dataByInstance.Add(__instance, data);
                }
                ___charaFileSort.DeleteAllNode();
                string basePath;
                if (___sex == 1)
                {
                    basePath = global::UserData.Path + "chara/female";
                    __instance.CallPrivate("InitFemaleList");
                }
                else
                {
                    basePath = global::UserData.Path + "chara/male";
                    __instance.CallPrivate("InitMaleList");
                }
                string path = basePath + data.currentPath;

                if (data.parentFolder == null)
                {
                    data.parentFolder = GameObject.Instantiate(___objectNode);
                    data.parentFolder.transform.SetParent(___charaFileSort.root, false);
                    GameObject.Destroy(data.parentFolder.GetComponent<GameSceneNode>());
                    Text t = data.parentFolder.GetComponentInChildren<Text>();
                    t.text = "../ (Parent folder)";
                    t.alignment = TextAnchor.MiddleCenter;
                    t.fontStyle = FontStyle.BoldAndItalic;
                    data.parentFolder.GetComponent<Image>().color = OptimizeCharaMaker._subFoldersColor;
                    data.parentFolder.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        int index = data.currentPath.LastIndexOf("/", StringComparison.OrdinalIgnoreCase);
                        data.currentPath = data.currentPath.Remove(index);
                        Prefix(__instance, true, ___charaFileSort, ___sex, ___objectNode, ___imageChara, ___buttonLoad, ___buttonChange);
                    });
                }

                data.parentFolder.SetActive(data.currentPath.Length > 1);

                string[] directories = Directory.GetDirectories(path);
                int i = 0;
                for (; i < directories.Length; i++)
                {
                    string directory = directories[i];
                    EntryListData.FolderData folder;
                    if (i < data.folders.Count)
                        folder = data.folders[i];
                    else
                    {
                        folder = new EntryListData.FolderData();
                        folder.displayObject = GameObject.Instantiate(___objectNode);
                        folder.button = folder.displayObject.GetComponent<Button>();
                        folder.text = folder.displayObject.GetComponentInChildren<Text>();

                        folder.displayObject.SetActive(true);
                        folder.displayObject.transform.SetParent(___charaFileSort.root, false);
                        GameObject.Destroy(folder.displayObject.GetComponent<GameSceneNode>());
                        folder.text.alignment = TextAnchor.MiddleCenter;
                        folder.text.fontStyle = FontStyle.BoldAndItalic;
                        folder.displayObject.GetComponent<Image>().color = OptimizeCharaMaker._subFoldersColor;

                        data.folders.Add(folder);
                    }
                    folder.displayObject.SetActive(true);
                    string localDirectory = directory.Replace("\\", "/").Replace(path, "");
                    folder.text.text = localDirectory.Substring(1);
                    folder.fullPath = directory;
                    folder.name = localDirectory.Substring(1);
                    folder.button.onClick = new Button.ButtonClickedEvent();
                    folder.button.onClick.AddListener(() =>
                    {
                        data.currentPath += localDirectory;
                        Prefix(__instance, true, ___charaFileSort, ___sex, ___objectNode, ___imageChara, ___buttonLoad, ___buttonChange);
                    });
                }
                for (; i < data.folders.Count; ++i)
                    data.folders[i].displayObject.SetActive(false);

                int count = ___charaFileSort.cfiList.Count;
                i = 0;
                MethodInfo onSelectCharaMI = __instance.GetType().GetMethod("OnSelectChara", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo loadCharaImageMI = __instance.GetType().GetMethod("LoadCharaImage", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

                for (; i < count; i++)
                {
                    CharaFileInfo info = ___charaFileSort.cfiList[i];
                    info.index = i;
                    EntryListData.EntryData chara;
                    if (i < data.entries.Count)
                        chara = data.entries[i];
                    else
                    {
                        chara = new EntryListData.EntryData();
                        chara.node = GameObject.Instantiate(___objectNode).GetComponent<GameSceneNode>();
                        chara.button = chara.node.GetComponent<Button>();

                        chara.node.gameObject.transform.SetParent(___charaFileSort.root, false);
                        data.entries.Add(chara);
                    }
                    chara.enabled = true;
                    chara.node.gameObject.SetActive(true);
                    info.gameSceneNode = chara.node;
                    info.button = chara.button;
                    info.button.onClick = new Button.ButtonClickedEvent();
                    Action<int> onSelect = (Action<int>)Delegate.CreateDelegate(typeof(Action<int>), __instance, onSelectCharaMI);
                    info.gameSceneNode.AddActionToButton(delegate
                    {
                        onSelect(info.index);
                    });
                    info.gameSceneNode.text = info.name;
                    info.gameSceneNode.listEnterAction.Clear();
                    Action<int> loadImage = (Action<int>)Delegate.CreateDelegate(typeof(Action<int>), __instance, loadCharaImageMI);
                    info.gameSceneNode.listEnterAction.Add(delegate
                    {
                        loadImage(info.index);
                    });
                }
                for (; i < data.entries.Count; ++i)
                {
                    data.entries[i].node.gameObject.SetActive(false);
                    data.entries[i].enabled = false;
                }
                ___imageChara.color = Color.clear;
                if (___charaFileSort.sortKind == -1)
                    ___charaFileSort.Sort(1, false);
                else
                    ___charaFileSort.Sort(___charaFileSort.sortKind, ((bool[])___charaFileSort.GetPrivate("sortType"))[___charaFileSort.sortKind]);
                ___buttonLoad.interactable = false;
                ___buttonChange.interactable = false;
                __instance.GetType().GetProperty("isInit", BindingFlags.Instance | BindingFlags.Public).SetValue(__instance, true, null);
                return false;
            }

        }

        [HarmonyPatch(typeof(CharaList), "InitFemaleList")]
        internal static class CharaList_InitFemaleList_Patches
        {
            private static bool Prepare()
            {
                return _optimizeNeo;
            }

            private static bool Prefix(CharaList __instance, CharaFileSort ___charaFileSort)
            {
                EntryListData data = EntryListData.dataByInstance[__instance];
                string path = global::UserData.Path + "chara/female" + data.currentPath;
                List<string> list = Directory.GetFiles(path, "*.png").ToList();
                ___charaFileSort.cfiList.Clear();
                int count = list.Count;
                for (int i = 0; i < count; i++)
                {
                    CharFemaleFile charFemaleFile = null;
                    if (SceneAssist.Assist.LoadFemaleCustomInfoAndParamerer(ref charFemaleFile, list[i]))
                    {
                        if (!charFemaleFile.femaleCustomInfo.isConcierge)
                        {
                            ___charaFileSort.cfiList.Add(new CharaFileInfo(string.Empty, string.Empty)
                            {
                                file = list[i],
                                name = charFemaleFile.femaleCustomInfo.name,
                                time = File.GetLastWriteTime(list[i])
                            });
                        }
                    }
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(CharaList), "InitMaleList")]
        internal static class CharaList_InitMaleList_Patches
        {
            private static bool Prepare()
            {
                return _optimizeNeo;
            }

            private static bool Prefix(CharaList __instance, CharaFileSort ___charaFileSort)
            {
                EntryListData data = EntryListData.dataByInstance[__instance];
                string path = global::UserData.Path + "chara/male" + data.currentPath;
                List<string> list = Directory.GetFiles(path, "*.png").ToList();
                ___charaFileSort.cfiList.Clear();
                int count = list.Count;
                for (int i = 0; i < count; i++)
                {
                    CharMaleFile charMaleFile = new CharMaleFile();
                    if (Path.GetFileNameWithoutExtension(list[i]) != "ill_Player")
                    {
                        if (charMaleFile.LoadBlockData(charMaleFile.maleCustomInfo, list[i]))
                        {
                            ___charaFileSort.cfiList.Add(new CharaFileInfo(string.Empty, string.Empty)
                            {
                                file = list[i],
                                name = charMaleFile.maleCustomInfo.name,
                                time = File.GetLastWriteTime(list[i])
                            });
                        }
                    }
                }
                return false;
            }
        }

        [HarmonyPatch]
        internal static class CostumeInfo_UpdateInfo_Patches
        {
            private static MethodInfo TargetMethod()
            {
                return AccessTools.Method(Type.GetType("Studio.MPCharCtrl+CostumeInfo,Assembly-CSharp"), "UpdateInfo", new[] { typeof(OCIChar) });
            }

            private static bool Prepare()
            {
                return _optimizeNeo && HSUS._self._binary == Binary.Studio;
            }

            private static void Prefix(object __instance, Button[] ___buttonSort)
            {
                Action<int> onClickSort = (Action<int>)Delegate.CreateDelegate(typeof(Action<int>), __instance, __instance.GetType().GetMethod("OnClickSort", BindingFlags.Instance | BindingFlags.Public));
                ___buttonSort[0].onClick = new Button.ButtonClickedEvent();
                ___buttonSort[0].onClick.AddListener(() =>
                {
                    onClickSort(0);
                });
                ___buttonSort[1].onClick = new Button.ButtonClickedEvent();
                ___buttonSort[1].onClick.AddListener(() =>
                {
                    onClickSort(1);
                });
            }
        }

        [HarmonyPatch]
        internal static class CostumeInfo_InitList_Patches
        {
            private static MethodInfo TargetMethod()
            {
                return AccessTools.Method(Type.GetType("Studio.MPCharCtrl+CostumeInfo,Assembly-CSharp"), "InitList", new[] { typeof(int) });
            }

            private static bool Prepare()
            {
                return _optimizeNeo && HSUS._self._binary == Binary.Studio;
            }

            private static bool Prefix(object __instance, int _sex, ref int ___sex, CharaFileSort ___fileSort, GameObject ___prefabNode, Button ___buttonLoad, RawImage ___imageThumbnail)
            {
                EntryListData data;
                if (EntryListData.dataByInstance.TryGetValue(__instance, out data) == false)
                {
                    data = new EntryListData();
                    data.sort = ___fileSort;
                    EntryListData.dataByInstance.Add(__instance, data);
                }
                if (___sex != _sex)
                    data.currentPath = "";
                else if (data.currentPath == data.previousCurrentPath)
                    return false;

                ___fileSort.DeleteAllNode();
                string basePath = _sex == 1 ? (global::UserData.Path + "coordinate/female") : (global::UserData.Path + "coordinate/male");
                string path = basePath + data.currentPath;

                if (data.parentFolder == null)
                {
                    data.parentFolder = GameObject.Instantiate(___prefabNode);
                    data.parentFolder.transform.SetParent(___fileSort.root, false);
                    GameObject.Destroy(data.parentFolder.GetComponent<GameSceneNode>());
                    Text t = data.parentFolder.GetComponentInChildren<Text>();
                    t.text = "../ (Parent folder)";
                    t.alignment = TextAnchor.MiddleCenter;
                    t.fontStyle = FontStyle.BoldAndItalic;
                    data.parentFolder.GetComponent<Image>().color = OptimizeCharaMaker._subFoldersColor;
                    data.parentFolder.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        int index = data.currentPath.LastIndexOf("/", StringComparison.OrdinalIgnoreCase);
                        data.currentPath = data.currentPath.Remove(index);
                        int refSex = _sex;
                        Prefix(__instance, _sex, ref refSex, ___fileSort, ___prefabNode, ___buttonLoad, ___imageThumbnail);
                    });
                }

                data.parentFolder.SetActive(data.currentPath.Length > 1);

                string[] directories = Directory.GetDirectories(path);
                int i = 0;
                for (; i < directories.Length; i++)
                {
                    EntryListData.FolderData folder;
                    if (i < data.folders.Count)
                        folder = data.folders[i];
                    else
                    {
                        folder = new EntryListData.FolderData();
                        folder.displayObject = GameObject.Instantiate(___prefabNode);
                        folder.text = folder.displayObject.GetComponentInChildren<Text>();
                        folder.button = folder.displayObject.GetComponent<Button>();

                        folder.text.alignment = TextAnchor.MiddleCenter;
                        folder.text.fontStyle = FontStyle.BoldAndItalic;
                        folder.displayObject.GetComponent<Image>().color = OptimizeCharaMaker._subFoldersColor;
                        folder.displayObject.transform.SetParent(___fileSort.root, false);
                        GameObject.Destroy(folder.displayObject.GetComponent<GameSceneNode>());
                        data.folders.Add(folder);
                    }
                    folder.displayObject.SetActive(true);
                    string directory = directories[i];
                    string localDirectory = directory.Replace("\\", "/").Replace(path, "");
                    folder.text.text = localDirectory.Substring(1);
                    folder.fullPath = directory;
                    folder.name = localDirectory.Substring(1);
                    folder.button.onClick = new Button.ButtonClickedEvent();
                    folder.button.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        data.currentPath += localDirectory;
                        int refSex = _sex;
                        Prefix(__instance, _sex, ref refSex, ___fileSort, ___prefabNode, ___buttonLoad, ___imageThumbnail);
                    });
                }
                for (; i < data.folders.Count; ++i)
                    data.folders[i].displayObject.SetActive(false);

                InitFileList(_sex, ___fileSort, data);
                int count = ___fileSort.cfiList.Count;
                MethodInfo onSelectMI = __instance.GetType().GetMethod("OnSelect", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic);
                MethodInfo loadImageMI = __instance.GetType().GetMethod("LoadImage", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic);
                i = 0;
                for (; i < count; i++)
                {
                    EntryListData.EntryData coord;
                    if (i < data.entries.Count)
                        coord = data.entries[i];
                    else
                    {
                        coord = new EntryListData.EntryData();
                        coord.node = GameObject.Instantiate(___prefabNode).GetComponent<GameSceneNode>();
                        coord.button = coord.node.GetComponent<Button>();
                        coord.node.transform.SetParent(___fileSort.root, false);
                        data.entries.Add(coord);
                    }
                    coord.enabled = true;
                    coord.node.gameObject.SetActive(true);
                    CharaFileInfo info = ___fileSort.cfiList[i];
                    info.gameSceneNode = coord.node;
                    info.index = i;
                    info.button = coord.button;
                    Action<int> onSelect = (Action<int>)Delegate.CreateDelegate(typeof(Action<int>), __instance, onSelectMI);
                    info.button.onClick = new Button.ButtonClickedEvent();
                    info.gameSceneNode.AddActionToButton(delegate
                    {
                        onSelect(info.index);
                    });
                    info.gameSceneNode.text = info.name;
                    Action<int> loadImage = (Action<int>)Delegate.CreateDelegate(typeof(Action<int>), __instance, loadImageMI);
                    info.gameSceneNode.listEnterAction.Clear();
                    info.gameSceneNode.listEnterAction.Add(delegate
                    {
                        loadImage(info.index);
                    });
                }
                for (; i < data.entries.Count; ++i)
                {
                    data.entries[i].node.gameObject.SetActive(false);
                    data.entries[i].enabled = false;
                }
                ___sex = _sex;
                if (___fileSort.sortKind == -1)
                    ___fileSort.Sort(1, false);
                else
                    ___fileSort.Sort(___fileSort.sortKind, ((bool[])___fileSort.GetPrivate("sortType"))[___fileSort.sortKind]);
                ___buttonLoad.interactable = false;
                ___imageThumbnail.color = Color.clear;
                data.previousCurrentPath = data.currentPath;
                return false;
            }

            private static void InitFileList(int _sex, CharaFileSort ___fileSort, EntryListData data)
            {
                string path = global::UserData.Path + (_sex != 1 ? "coordinate/male" : "coordinate/female") + data.currentPath;
                List<string> list = Directory.GetFiles(path, "*.png").ToList();
                ___fileSort.cfiList.Clear();
                int count = list.Count;
                CharFileInfoClothes charFileInfoClothes;
                if (_sex == 1)
                    charFileInfoClothes = new CharFileInfoClothesFemale();
                else
                    charFileInfoClothes = new CharFileInfoClothesMale();
                for (int i = 0; i < count; i++)
                {
                    if (charFileInfoClothes.Load(list[i], true))
                    {
                        ___fileSort.cfiList.Add(new CharaFileInfo(list[i], charFileInfoClothes.comment)
                        {
                            time = File.GetLastWriteTime(list[i])
                        });
                    }
                }
            }
        }

        [HarmonyPatch(typeof(CharaFileSort), "DeleteAllNode")]
        internal static class CharaFileSort_DeleteAllNode_Patches
        {
            private static bool Prepare()
            {
                return _optimizeNeo;
            }

            private static bool Prefix(ref int ___m_Select)
            {
                ___m_Select = -1;
                return false;
            }
        }

        [HarmonyPatch(typeof(CharaFileSort), "SortTime", typeof(bool))]
        internal static class CharaFileSort_SortTime_Patches
        {
            private static bool Prepare()
            {
                return _optimizeNeo;
            }

            private static void Postfix(CharaFileSort __instance, bool _ascend, bool[] ___sortType)
            {
                EntryListData data = EntryListData.dataByInstance.FirstOrDefault(e => e.Value.sort == __instance).Value;
                if (data != null)
                {
                    ___sortType[1] = _ascend;
                    CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
                    Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("ja-JP");
                    if (_ascend)
                    {
                        data.folders.Sort((a, b) => Directory.GetLastWriteTime(a.fullPath).CompareTo(Directory.GetLastWriteTime(b.fullPath)));
                    }
                    else
                    {
                        data.folders.Sort((a, b) => Directory.GetLastWriteTime(b.fullPath).CompareTo(Directory.GetLastWriteTime(a.fullPath)));
                    }
                    Thread.CurrentThread.CurrentCulture = currentCulture;
                    for (int i = data.folders.Count - 1; i >= 0; i--)
                    {
                        data.folders[i].displayObject.transform.SetAsFirstSibling();
                    }
                    data.parentFolder.transform.SetAsFirstSibling();
                }
            }
        }

        [HarmonyPatch(typeof(CharaFileSort), "SortName", typeof(bool))]
        internal static class CharaFileSort_SortName_Patches
        {
            private static bool Prepare()
            {
                return _optimizeNeo;
            }

            private static void Postfix(CharaFileSort __instance, bool _ascend, bool[] ___sortType)
            {
                EntryListData data = EntryListData.dataByInstance.FirstOrDefault(e => e.Value.sort == __instance).Value;
                if (data != null)
                {
                    ___sortType[1] = _ascend;
                    CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
                    Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("ja-JP");
                    if (_ascend)
                    {
                        data.folders.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.CurrentCultureIgnoreCase));
                    }
                    else
                    {
                        data.folders.Sort((a, b) => string.Compare(b.name, a.name, StringComparison.CurrentCultureIgnoreCase));
                    }
                    Thread.CurrentThread.CurrentCulture = currentCulture;
                    for (int i = data.folders.Count - 1; i >= 0; i--)
                    {
                        data.folders[i].displayObject.transform.SetAsFirstSibling();
                    }
                    data.parentFolder.transform.SetAsFirstSibling();
                }
            }
        }

        [HarmonyPatch]
        internal static class StudioCharaListSortUtil_ExecuteSort_Patches
        {
            private static bool Prepare()
            {
                return _optimizeNeo && HSUS._self._binary == Binary.Studio && Type.GetType("HSStudioNEOAddon.StudioCharaListSortUtil,HSStudioNEOAddon") != null;
            }

            private static MethodInfo TargetMethod()
            {
                return AccessTools.Method(Type.GetType("HSStudioNEOAddon.StudioCharaListSortUtil,HSStudioNEOAddon"), "ExecuteSort");
            }

            private static void Postfix(CharaFileSort ___charaFileSort)
            {
                EntryListData data = EntryListData.dataByInstance.FirstOrDefault(e => e.Value.sort == ___charaFileSort).Value;
                if (data == null)
                    return;
                foreach (EntryListData.EntryData chara in data.entries)
                    chara.node.gameObject.SetActive(chara.node.gameObject.activeSelf && chara.enabled);
            }
        }

        [HarmonyPatch]
        internal static class TextSlideEffectCtrl_Check_Patches
        {
            private static MethodInfo TargetMethod()
            {
                return AccessTools.Method(Type.GetType("Studio.TextSlideEffectCtrl,Assembly-CSharp"), "Check");
            }

            private static bool Prepare()
            {
                return _optimizeNeo && HSUS._self._binary == Binary.Studio && Type.GetType("Studio.TextSlideEffectCtrl,Assembly-CSharp") != null;
            }

            private static bool Prefix(object __instance, Text ___text, object ___textSlideEffect)
            {
                float preferredWidth = ___text.preferredWidth;
                if (preferredWidth < 104)
                {
                    ObservableLateUpdateTrigger component = ((Component)__instance).GetComponent<ObservableLateUpdateTrigger>();
                    if (component != null)
                        GameObject.Destroy(component);
                    GameObject.Destroy((Component)__instance);
                    GameObject.Destroy((Component)___textSlideEffect);
                    return false;
                }
                ___text.alignment = (TextAnchor)3;
                ___text.horizontalOverflow = (HorizontalWrapMode)1;
                ___text.raycastTarget = true;
                __instance.CallPrivate("AddFunc");
                return false;
            }
        }


#endif

#if HONEYSELECT
        [HarmonyPatch(typeof(BackgroundList), "InitList")]
        public static class BackgroundList_InitList_Patches
        {
            private static InputField _searchBar;
            private static RectTransform _parent;

            public static bool Prepare()
            {
                return _optimizeNeo && HSUS._self._binary == Binary.Studio;
            }

            public static void Postfix(object __instance)
            {
                _parent = (RectTransform)((RectTransform)__instance.GetPrivate("transformRoot")).parent.parent;

                _searchBar = UIUtility.CreateInputField("Search Bar", _parent, "Search...");
                Image image = _searchBar.GetComponent<Image>();
                image.color = UIUtility.grayColor;
                RectTransform rt = _searchBar.transform as RectTransform;
                rt.localPosition = Vector3.zero;
                rt.localScale = Vector3.one;
                rt.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, -21f), new Vector2(0f, 1f));
                _searchBar.onValueChanged.AddListener(s => SearchChanged(__instance));
                foreach (Text t in _searchBar.GetComponentsInChildren<Text>())
                    t.color = Color.white;
            }

            private static void SearchChanged(object instance)
            {
                Dictionary<int, StudioNode> dicNode = (Dictionary<int, StudioNode>)instance.GetPrivate("dicNode");
                string search = _searchBar.text.Trim();
                foreach (KeyValuePair<int, StudioNode> pair in dicNode)
                {
                    pair.Value.active = pair.Value.textUI.text.IndexOf(search, StringComparison.OrdinalIgnoreCase) != -1;
                }
                LayoutRebuilder.MarkLayoutForRebuild(_parent);
            }
        }
#endif

#if HONEYSELECT
        [HarmonyPatch(typeof(StartScene), "Start")]
        public static class StartScene_Start_Patches
        {
            public static bool Prepare()
            {
                return _optimizeNeo;
            }

            public static bool Prefix(System.Object __instance)
            {
                if (__instance as StartScene)
                {
                    Studio.Info.Instance.LoadExcelData();
                    Scene.Instance.SetFadeColor(Color.black);
                    Scene.Instance.LoadReserv("Studio", true);
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(GuideObject), "Start")]
        public static class GuideObject_Start_Patches
        {
            public static bool Prepare(HarmonyInstance instance)
            {
                return _optimizeNeo;
            }

            public static void Postfix(GuideObject __instance)
            {
                Action a = () => GuideObject_LateUpdate_Patches.ScheduleForUpdate(__instance);
                Action<Vector3> a2 = (v) => GuideObject_LateUpdate_Patches.ScheduleForUpdate(__instance);
                __instance.changeAmount.onChangePos = (Action)Delegate.Combine(__instance.changeAmount.onChangePos, a);
                __instance.changeAmount.onChangeRot = (Action)Delegate.Combine(__instance.changeAmount.onChangeRot, a);
                __instance.changeAmount.onChangeScale = (Action<Vector3>)Delegate.Combine(__instance.changeAmount.onChangeScale, a2);
                GuideObject_LateUpdate_Patches.ScheduleForUpdate(__instance);
                __instance.ExecuteDelayed(() =>
                {
                    GuideObject_LateUpdate_Patches.ScheduleForUpdate(__instance); //Probably for HSPE
                }, 4);
            }
        }

        [HarmonyPatch]
        public static class ABMStudioNEOSaveLoadHandler_OnLoad_Patches
        {
            private static bool Prepare()
            {
                return HSUS._self._binary == Binary.Studio && _optimizeNeo && Type.GetType("AdditionalBoneModifier.ABMStudioNEOSaveLoadHandler,AdditionalBoneModifierStudioNEO") != null;
            }

            private static MethodInfo TargetMethod()
            {
                return Type.GetType("AdditionalBoneModifier.ABMStudioNEOSaveLoadHandler,AdditionalBoneModifierStudioNEO").GetMethod("OnLoad", BindingFlags.Public | BindingFlags.Instance);
            }

            private static void Postfix()
            {
                GuideObjectManager.Instance.ExecuteDelayed(() =>
                {
                    foreach (KeyValuePair<Transform, GuideObject> pair in (Dictionary<Transform, GuideObject>)GuideObjectManager.Instance.GetPrivate("dicGuideObject"))
                    {
                        GuideObject_LateUpdate_Patches.ScheduleForUpdate(pair.Value);
                    }
                });
            }
        }

        [HarmonyPatch(typeof(GuideObject), "LateUpdate")]
        public static class GuideObject_LateUpdate_Patches
        {
            public static bool Prepare()
            {
                return _optimizeNeo;
            }

            private static readonly Dictionary<GuideObject, bool> _instanceData = new Dictionary<GuideObject, bool>(); //Apparently, doing this is faster than having a simple HashSet...

            public static void ScheduleForUpdate(GuideObject obj)
            {
                if (_instanceData.ContainsKey(obj) == false)
                    _instanceData.Add(obj, true);
                else
                    _instanceData[obj] = true;
            }


            public static bool Prefix(GuideObject __instance, GameObject[] ___roots)
            {
                __instance.transform.position = __instance.transformTarget.position;
                __instance.transform.rotation = __instance.transformTarget.rotation;
                if (_instanceData.TryGetValue(__instance, out bool b) && b)
                {
                    switch (__instance.mode)
                    {
                        case GuideObject.Mode.Local:
                            ___roots[0].transform.rotation = __instance.parent != null ? __instance.parent.rotation : Quaternion.identity;
                            break;
                        case GuideObject.Mode.World:
                            ___roots[0].transform.rotation = Quaternion.identity;
                            break;
                    }
                    if (__instance.calcScale)
                    {
                        Vector3 localScale = __instance.transformTarget.localScale;
                        Vector3 lossyScale = __instance.transformTarget.lossyScale;
                        Vector3 vector3 = !__instance.enableScale ? Vector3.one : __instance.changeAmount.scale;
                        __instance.transformTarget.localScale = new Vector3(localScale.x / lossyScale.x * vector3.x, localScale.y / lossyScale.y * vector3.y, localScale.z / lossyScale.z * vector3.z);
                    }
                    _instanceData[__instance] = false;
                }
                return false;
            }

        }


        [HarmonyPatch(typeof(Studio.Studio), "Duplicate")]
        public static class Studio_Duplicate_Patches
        {
            public static bool Prepare()
            {
                return _optimizeNeo;
            }

            public static void Postfix(Studio.Studio __instance)
            {
                foreach (GuideObject guideObject in Resources.FindObjectsOfTypeAll<GuideObject>())
                {

                    GuideObject_LateUpdate_Patches.ScheduleForUpdate(guideObject);
                }
            }
        }

        [HarmonyPatch(typeof(ChangeAmount), "set_scale", new[] { typeof(Vector3) })]
        public static class ChangeAmount_set_scale_Patches
        {
            public static bool Prepare()
            {
                return _optimizeNeo;
            }

            public static void Postfix(ChangeAmount __instance)
            {
                foreach (KeyValuePair<TreeNodeObject, ObjectCtrlInfo> pair in Studio.Studio.Instance.dicInfo)
                {
                    if (pair.Value.guideObject.changeAmount == __instance)
                    {
                        Recurse(pair.Key, info => GuideObject_LateUpdate_Patches.ScheduleForUpdate(info.guideObject));
                        break;
                    }
                }
            }

            private static void Recurse(TreeNodeObject node, Action<ObjectCtrlInfo> action)
            {
                if (Studio.Studio.Instance.dicInfo.TryGetValue(node, out ObjectCtrlInfo info))
                    action(info);
                foreach (TreeNodeObject child in node.child)
                    Recurse(child, action);
            }
        }

        [HarmonyPatch(typeof(GuideObject), "set_calcScale", new[] { typeof(bool) })]
        public static class GuideObject_set_calcScale_Patches
        {
            public static bool Prepare()
            {
                return _optimizeNeo && HSUS._self._binary == Binary.Studio;
            }

            public static void Postfix(GuideObject __instance)
            {
                GuideObject_LateUpdate_Patches.ScheduleForUpdate(__instance);
            }
        }

        [HarmonyPatch(typeof(GuideObject), "set_enableScale", new[] { typeof(bool) })]
        public static class GuideObject_set_enableScale_Patches
        {
            public static bool Prepare()
            {
                return _optimizeNeo;
            }

            public static void Postfix(GuideObject __instance)
            {
                GuideObject_LateUpdate_Patches.ScheduleForUpdate(__instance);
            }
        }

        [HarmonyPatch(typeof(GuideObject), "set_enablePos", new[] { typeof(bool) })]
        public static class GuideObject_set_enablePos_Patches
        {
            public static bool Prepare()
            {
                return _optimizeNeo;
            }

            public static void Postfix(GuideObject __instance)
            {
                GuideObject_LateUpdate_Patches.ScheduleForUpdate(__instance);
            }
        }

        [HarmonyPatch(typeof(GuideObject), "set_enableRot", new[] { typeof(bool) })]
        public static class GuideObject_set_enableRot_Patches
        {
            public static bool Prepare()
            {
                return _optimizeNeo;
            }

            public static void Postfix(GuideObject __instance)
            {
                GuideObject_LateUpdate_Patches.ScheduleForUpdate(__instance);
            }
        }

        [HarmonyPatch(typeof(OCIChar), "OnAttach", new[] { typeof(TreeNodeObject), typeof(ObjectCtrlInfo) })]
        [HarmonyPatch(typeof(OCIFolder), "OnAttach", new[] { typeof(TreeNodeObject), typeof(ObjectCtrlInfo) })]
        [HarmonyPatch(typeof(OCIItem), "OnAttach", new[] { typeof(TreeNodeObject), typeof(ObjectCtrlInfo) })]
        [HarmonyPatch(typeof(OCILight), "OnAttach", new[] { typeof(TreeNodeObject), typeof(ObjectCtrlInfo) })]
        [HarmonyPatch(typeof(OCIPathMove), "OnAttach", new[] { typeof(TreeNodeObject), typeof(ObjectCtrlInfo) })]
        public static class ObjectCtrlInfo_OnAttach_Patches
        {
            public static bool Prepare()
            {
                return _optimizeNeo;
            }

            public static void Postfix(ObjectCtrlInfo __instance, TreeNodeObject _parent, ObjectCtrlInfo _child)
            {
                GuideObject_LateUpdate_Patches.ScheduleForUpdate(__instance.guideObject);
            }
        }

        [HarmonyPatch(typeof(OCIChar), "OnLoadAttach", new[] { typeof(TreeNodeObject), typeof(ObjectCtrlInfo) })]
        [HarmonyPatch(typeof(OCIFolder), "OnLoadAttach", new[] { typeof(TreeNodeObject), typeof(ObjectCtrlInfo) })]
        [HarmonyPatch(typeof(OCIItem), "OnLoadAttach", new[] { typeof(TreeNodeObject), typeof(ObjectCtrlInfo) })]
        [HarmonyPatch(typeof(OCILight), "OnLoadAttach", new[] { typeof(TreeNodeObject), typeof(ObjectCtrlInfo) })]
        [HarmonyPatch(typeof(OCIPathMove), "OnLoadAttach", new[] { typeof(TreeNodeObject), typeof(ObjectCtrlInfo) })]
        public static class ObjectCtrlInfo_OnLoadAttach_Patches
        {
            public static bool Prepare()
            {
                return _optimizeNeo;
            }

            public static void Postfix(ObjectCtrlInfo __instance, TreeNodeObject _parent, ObjectCtrlInfo _child)
            {
                GuideObject_LateUpdate_Patches.ScheduleForUpdate(__instance.guideObject);
            }
        }

        [HarmonyPatch(typeof(OCIChar), "OnDetach")]
        [HarmonyPatch(typeof(OCIFolder), "OnDetach")]
        [HarmonyPatch(typeof(OCIItem), "OnDetach")]
        [HarmonyPatch(typeof(OCILight), "OnDetach")]
        [HarmonyPatch(typeof(OCIPathMove), "OnDetach")]
        public static class ObjectCtrlInfo_OnDetach_Patches
        {
            public static bool Prepare()
            {
                return _optimizeNeo;
            }

            public static void Postfix(ObjectCtrlInfo __instance)
            {
                GuideObject_LateUpdate_Patches.ScheduleForUpdate(__instance.guideObject);
            }
        }

        [HarmonyPatch(typeof(TreeNodeCtrl), "SetSelectNode", new[] { typeof(TreeNodeObject) })]
        public static class TreeNodeCtrl_SetSelectNode_Patches
        {
            public static bool Prepare()
            {
                return _optimizeNeo;
            }

            public static void Postfix(TreeNodeCtrl __instance, TreeNodeObject _node)
            {
                ObjectCtrlInfo objectCtrlInfo = TryGetLoop(_node);
                if (objectCtrlInfo != null)
                    GuideObject_LateUpdate_Patches.ScheduleForUpdate(objectCtrlInfo.guideObject);
            }

            private static ObjectCtrlInfo TryGetLoop(TreeNodeObject _node)
            {
                if (_node == null)
                    return null;
                if (Studio.Studio.Instance.dicInfo.TryGetValue(_node, out ObjectCtrlInfo result))
                    return result;
                return TryGetLoop(_node.parent);
            }
        }
#endif

#if !PLAYHOME
        [HarmonyPatch(typeof(WorkspaceCtrl), "Awake")]
        internal static class WorkspaceCtrl_Awake_Patches
        {
            private static List<TreeNodeObject> _treeNodeList;
            internal static InputField _search;
            internal static bool _ignoreSearch = false;

            private static bool Prepare()
            {
                return HSUS.OptimizeStudio.Value;
            }

            private static void Postfix(WorkspaceCtrl __instance, TreeNodeCtrl ___treeNodeCtrl)
            {
                RectTransform viewport = __instance.transform.Find("Image Bar/Scroll View").GetComponent<ScrollRect>().viewport;
                _search = UIUtility.CreateInputField("Search", viewport.parent, "Search...");
#if HONEYSELECT
                float height = 18f;
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
                float height = 22f;
#endif
                _search.transform.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(viewport.offsetMin.x, viewport.offsetMin.y - 2f), new Vector2(viewport.offsetMax.x, viewport.offsetMin.y + height));
                viewport.offsetMin += new Vector2(0f, height);
                foreach (Text t in _search.GetComponentsInChildren<Text>())
                    t.color = Color.white;
                Image image = _search.GetComponent<Image>();
                image.color = UIUtility.grayColor;
                _search.onValueChanged.AddListener(OnSearchChanged);
                _treeNodeList = (List<TreeNodeObject>)___treeNodeCtrl.GetPrivate("m_TreeNodeObject");
            }

            internal static void OnSearchChanged(string s = "")
            {
                if (_ignoreSearch)
                    return;
                foreach (TreeNodeObject o in _treeNodeList)
                {
                    if (o.parent == null)
                        PartialSearch(o);
                }
            }

            internal static void PartialSearch(TreeNodeObject o)
            {
                if (_ignoreSearch)
                    return;
                string searchText = _search.text;
                RecurseAny(o, t => t.textName.IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) != -1,
                        (t, r) => t.gameObject.SetActive(r && AllParentsOpen(t)));

            }

            private static bool RecurseAny(TreeNodeObject obj, Func<TreeNodeObject, bool> func, Action<TreeNodeObject, bool> onResult)
            {
                bool res = func(obj);
                foreach (TreeNodeObject child in obj.child)
                {
                    if (RecurseAny(child, func, onResult))
                        res = true;
                }
                onResult(obj, res);
                return res;
            }

            private static bool AllParentsOpen(TreeNodeObject self)
            {
                if (self.parent == null)
                    return true;
                TreeNodeObject parent = self.parent;
                while (parent != null)
                {
                    if (parent.treeState == TreeNodeObject.TreeState.Close)
                        return false;
                    parent = parent.parent;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(WorkspaceCtrl), "OnClickDuplicate")]
        internal static class WorkspaceCtrl_OnClickDuplicate_Patches
        {
            private static bool Prepare()
            {
                return HSUS.OptimizeStudio.Value && HSUS._self.binary == Binary.Studio;
            }

            private static void Postfix()
            {
                if (WorkspaceCtrl_Awake_Patches._search.text == string.Empty)
                    return;
                WorkspaceCtrl_Awake_Patches.OnSearchChanged();
            }
        }

        [HarmonyPatch(typeof(WorkspaceCtrl), "OnClickParent")]
        internal static class WorkspaceCtrl_OnClickParent_Patches
        {
            private static bool Prepare()
            {
                return HSUS.OptimizeStudio.Value && HSUS._self.binary == Binary.Studio;
            }

            private static void Postfix()
            {
                if (WorkspaceCtrl_Awake_Patches._search.text == string.Empty)
                    return;
                WorkspaceCtrl_Awake_Patches.OnSearchChanged();
            }
        }

        [HarmonyPatch(typeof(WorkspaceCtrl), "OnParentage", new[] { typeof(TreeNodeObject), typeof(TreeNodeObject) })]
        internal static class WorkspaceCtrl_OnParentage_Patches
        {
            private static bool Prepare()
            {
                return HSUS.OptimizeStudio.Value && HSUS._self.binary == Binary.Studio;
            }

            private static void Postfix()
            {
                if (WorkspaceCtrl_Awake_Patches._search.text == string.Empty)
                    return;
                WorkspaceCtrl_Awake_Patches.OnSearchChanged();
            }
        }

        [HarmonyPatch(typeof(WorkspaceCtrl), "OnClickDelete")]
        internal static class WorkspaceCtrl_OnClickDelete_Patches
        {
            private static bool Prepare()
            {
                return HSUS.OptimizeStudio.Value && HSUS._self.binary == Binary.Studio;
            }

            private static void Postfix()
            {
                if (WorkspaceCtrl_Awake_Patches._search.text == string.Empty)
                    return;
                WorkspaceCtrl_Awake_Patches.OnSearchChanged();
            }
        }

        [HarmonyPatch(typeof(WorkspaceCtrl), "OnClickFolder")]
        internal static class WorkspaceCtrl_OnClickFolder_Patches
        {
            private static bool Prepare()
            {
                return HSUS.OptimizeStudio.Value && HSUS._self.binary == Binary.Studio;
            }

            private static void Postfix()
            {
                if (WorkspaceCtrl_Awake_Patches._search.text == string.Empty)
                    return;
                WorkspaceCtrl_Awake_Patches.OnSearchChanged();
            }
        }

        [HarmonyPatch(typeof(WorkspaceCtrl), "OnClickRemove")]
        internal static class WorkspaceCtrl_OnClickRemove_Patches
        {
            private static bool Prepare()
            {
                return HSUS.OptimizeStudio.Value && HSUS._self.binary == Binary.Studio;
            }

            private static void Postfix()
            {
                if (WorkspaceCtrl_Awake_Patches._search.text == string.Empty)
                    return;
                WorkspaceCtrl_Awake_Patches.OnSearchChanged();
            }
        }

        [HarmonyPatch(typeof(WorkspaceCtrl), "UpdateUI")]
        internal static class WorkspaceCtrl_UpdateUI_Patches
        {
            private static bool Prepare()
            {
                return HSUS.OptimizeStudio.Value && HSUS._self.binary == Binary.Studio;
            }

            private static void Postfix()
            {
                WorkspaceCtrl_Awake_Patches.OnSearchChanged();
            }
        }

        [HarmonyPatch(typeof(TreeNodeObject), "SetTreeState", new[] { typeof(TreeNodeObject.TreeState) })]
        [HarmonyPatch(typeof(TreeNodeObject), "set_treeState", new[] { typeof(TreeNodeObject.TreeState) })]
        internal static class TreeNodeObject_SetTreeState_MultiPatch
        {
            private static bool Prepare()
            {
                return HSUS.OptimizeStudio.Value;
            }

            private static void Postfix(TreeNodeObject __instance)
            {
                if (WorkspaceCtrl_Awake_Patches._search.text == string.Empty)
                    return;
                WorkspaceCtrl_Awake_Patches.PartialSearch(__instance);
            }
        }

        [HarmonyPatch(typeof(Studio.Studio), "LoadScene", new[] { typeof(string) })]
        internal static class Studio_LoadScene_MultiPatch
        {
            private static bool Prepare()
            {
                return HSUS.OptimizeStudio.Value;
            }

            private static void Prefix()
            {
                WorkspaceCtrl_Awake_Patches._ignoreSearch = true;
            }

            private static void Postfix()
            {
                WorkspaceCtrl_Awake_Patches._ignoreSearch = false;
            }
        }
#endif

#if HONEYSELECT
        [HarmonyPatch(typeof(TreeNodeCtrl), "AddNode", new[] { typeof(string), typeof(TreeNodeObject) })]
        internal static class TreeNodeCtrl_AddNode_MultiPatch
        {
            private static bool Prepare()
            {
                return _optimizeNeo && HSUS._self._translationMethod != null;
            }

            private static void Postfix(TreeNodeObject __result)
            {
                string name = __result.textName;
                HSUS._self._translationMethod(ref name);
                __result.textName = name;
            }
        }

        [HarmonyPatch(typeof(FBSBase), "CalculateBlendShape")]
        internal static class FaceBlendShape_LateUpdate_Patches
        {
            [HarmonyAfter("com.joan6694.hsplugins.instrumentation")]
            private static bool Prefix(FBSBase __instance, float ___correctOpenMax, float ___openRate, TimeProgressCtrl ___blendTimeCtrl, Dictionary<int, float> ___dictBackFace, Dictionary<int, float> ___dictNowFace)
            {
                if (__instance.FBSTarget.Length == 0)
                {
                    return false;
                }
                float b = (___correctOpenMax >= 0f) ? ___correctOpenMax : __instance.OpenMax;
                float num = Mathf.Lerp(__instance.OpenMin, b, ___openRate);
                if (0f <= __instance.FixedRate)
                {
                    num = __instance.FixedRate;
                }
                float num2 = 0f;
                if (___blendTimeCtrl != null)
                {
                    num2 = ___blendTimeCtrl.Calculate();
                }
                bool num2NotOne = Mathf.Approximately(num2, 1f) == false;
                int num3 = (int)Mathf.Clamp(num * 100f, 0f, 100f);
                foreach (FBSTargetInfo fbstargetInfo in __instance.FBSTarget)
                {
                    SkinnedMeshRenderer skinnedMeshRenderer = fbstargetInfo.GetSkinnedMeshRenderer();
                    Dictionary<int, float> dictionary = new Dictionary<int, float>();
                    foreach (FBSTargetInfo.CloseOpen closeOpen in fbstargetInfo.PtnSet)
                    {
                        dictionary[closeOpen.Close] = 0;
                        dictionary[closeOpen.Open] = 0;
                    }
                    if (num2NotOne)
                    {
                        foreach (KeyValuePair<int, float> pair in ___dictBackFace)
                        {
                            FBSTargetInfo.CloseOpen closeOpen = fbstargetInfo.PtnSet[pair.Key];
                            //if (dictionary.ContainsKey(closeOpen.Close) == false)
                            //    dictionary.Add(closeOpen.Close, 0);
                            //if (dictionary.ContainsKey(closeOpen.Open) == false)
                            //    dictionary.Add(closeOpen.Open, 0);
                            dictionary[closeOpen.Close] += pair.Value * (100 - num3) * (1f - num2);
                            dictionary[closeOpen.Open] += pair.Value * num3 * (1f - num2);
                        }
                    }
                    foreach (KeyValuePair<int, float> pair in ___dictNowFace)
                    {
                        FBSTargetInfo.CloseOpen closeOpen = fbstargetInfo.PtnSet[pair.Key];
                        //if (dictionary.ContainsKey(closeOpen.Close) == false)
                        //    dictionary.Add(closeOpen.Close, 0);
                        //if (dictionary.ContainsKey(closeOpen.Open) == false)
                        //    dictionary.Add(closeOpen.Open, 0);
                        dictionary[closeOpen.Close] += pair.Value * (100 - num3) * num2;
                        dictionary[closeOpen.Open] += pair.Value * num3 * num2;
                    }
                    foreach (KeyValuePair<int, float> keyValuePair in dictionary)
                    {
                        if (keyValuePair.Key != -1)
                        {
                            skinnedMeshRenderer.SetBlendShapeWeight(keyValuePair.Key, keyValuePair.Value);
                        }
                    }
                }
                return false;
            }
        }
#endif
    }
}