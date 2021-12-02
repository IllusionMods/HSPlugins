
#if HONEYSELECT
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using CustomMenu;
using Harmony;
using IllusionUtility.GetUtility;
using ToolBox.Extensions;
using UnityEngine;
using UnityEngine.UI;
using HSUS.Features;

namespace HSUS
{
    public static class SmClothesLoad_Data
    {
        public class FolderInfo
        {
            public string fullPath;
            public string path;
            public string name;
            public EntryDisplay display;
        }

        public class EntryDisplay
        {
            public GameObject gameObject;
            public Toggle toggle;
            public Text text;
        }

        public static readonly Dictionary<SmClothesLoad.FileInfo, SmClothesLoad.FileInfoComponent> clothes = new Dictionary<SmClothesLoad.FileInfo, SmClothesLoad.FileInfoComponent>();
        public static readonly LinkedList<SmClothesLoad.FileInfoComponent> clothesEntryPool = new LinkedList<SmClothesLoad.FileInfoComponent>();
        public static readonly List<EntryDisplay> folders = new List<EntryDisplay>();

        public static bool created;
        public static RectTransform container;
        public static readonly List<FolderInfo> folderInfos = new List<FolderInfo>();
        public static Toggle parentFolder;
        public static InputField searchBar;
        internal static IEnumerator _createListObject;
        internal static PropertyInfo _translateProperty = null;

        private static SmClothesLoad _originalComponent;
        public static void Init(SmClothesLoad originalComponent)
        {
            Reset();

            _originalComponent = originalComponent;

            container = _originalComponent.transform.FindDescendant("ListTop").transform as RectTransform;
            VerticalLayoutGroup group = container.gameObject.AddComponent<VerticalLayoutGroup>();
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            ContentSizeFitter fitter = container.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _originalComponent.rtfPanel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            group = _originalComponent.rtfPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;

            List<SmClothesLoad.FileInfo> fileInfos = (List<SmClothesLoad.FileInfo>)originalComponent.GetPrivate("lstFileInfo");

            searchBar = CharaMakerSearch.SpawnSearchBar(_originalComponent.transform, (s) =>
            {
                SearchChanged(fileInfos);
            }, -24f);

            _translateProperty = typeof(Text).GetProperty("Translate", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            if (OptimizeCharaMaker._asyncLoading)
            {
                bool initEnd = false;
                byte folderInfoSex = 0;
                SmClothesLoad_Init_Patches.Prefix(originalComponent, ref initEnd, ref folderInfoSex, originalComponent.customControl.chainfo, fileInfos);
                _createListObject = SmClothesLoad_CreateListObject_Patches.CreateListObject(originalComponent, originalComponent.customControl.chainfo, fileInfos, (List<RectTransform>)originalComponent.GetPrivate("lstRtfTgl"));
                HSUS._self._asyncMethods.Add(_createListObject);
            }

            originalComponent.objLineBase.transform.Find("Label").GetComponent<Text>().raycastTarget = false;
            originalComponent.objLineBase.transform.Find("Background/Checkmark").GetComponent<Image>().raycastTarget = false;
        }

        private static void Reset()
        {
            //folderInfos.Clear();
            folders.Clear();
            clothes.Clear();
            created = false;
            _createListObject = null;
        }

        public static void SearchChanged(List<SmClothesLoad.FileInfo> lstFileInfo)
        {
            string search = searchBar.text.Trim();
            if (clothes != null && clothes.Count != 0)
                foreach (SmClothesLoad.FileInfo fi in lstFileInfo)
                {
                    SmClothesLoad.FileInfoComponent fileInfoComponent;
                    if (clothes.TryGetValue(fi, out fileInfoComponent))
                        fileInfoComponent.gameObject.SetActive(fi.comment.IndexOf(search, StringComparison.OrdinalIgnoreCase) != -1);
                }
        }

        public static void ResetSearch(List<SmClothesLoad.FileInfo> lstFileInfo)
        {
            InputField.OnChangeEvent searchEvent = searchBar.onValueChanged;
            searchBar.onValueChanged = null;
            searchBar.text = "";
            searchBar.onValueChanged = searchEvent;
            if (clothes != null && clothes.Count != 0)
                foreach (SmClothesLoad.FileInfo fi in lstFileInfo)
                {
                    SmClothesLoad.FileInfoComponent fileInfoComponent;
                    if (clothes.TryGetValue(fi, out fileInfoComponent))
                        fileInfoComponent.gameObject.SetActive(true);
                }
        }
    }

    [HarmonyPatch(typeof(SmClothesLoad), "Init")]
    internal static class SmClothesLoad_Init_Patches
    {
        internal static void Prefix(SmClothesLoad __instance, ref bool ___initEnd, ref byte ___folderInfoSex, CharInfo ___chaInfo, List<SmClothesLoad.FileInfo> ___lstFileInfo)
        {
            if (___initEnd && ___folderInfoSex == ___chaInfo.Sex)
                return;
            if (null == ___chaInfo)
                return;
            global::FolderAssist folderAssist = new global::FolderAssist();

            string path = global::UserData.Path + (___chaInfo.Sex != 0 ? "coordinate/female" : "coordinate/male") + OptimizeCharaMaker._currentClothesPathGame;
            folderAssist.CreateFolderInfoEx(path, new[] {"*.png"});
            CharFileInfoClothes charFileInfoClothes;
            if (___chaInfo.Sex == 0)
                charFileInfoClothes = new CharFileInfoClothesMale();
            else
                charFileInfoClothes = new CharFileInfoClothesFemale();
            ___lstFileInfo.Clear();
            int fileCount = folderAssist.GetFileCount();
            int num = 0;
            for (int i = 0; i < fileCount; i++)
            {
                if (charFileInfoClothes.Load(folderAssist.lstFile[i].FullPath, false))
                {
                    if (charFileInfoClothes.clothesTypeSex == ___chaInfo.Sex)
                    {
                        SmClothesLoad.FileInfo fileInfo = new SmClothesLoad.FileInfo
                        {
                            no = num,
                            fullPath = folderAssist.lstFile[i].FullPath,
                            fileName = folderAssist.lstFile[i].FileName,
                            time = folderAssist.lstFile[i].time,
                            comment = charFileInfoClothes.comment
                        };
                        ___lstFileInfo.Add(fileInfo);
                        num++;
                    }
                }
            }

            string[] directories = Directory.GetDirectories(path);
            SmClothesLoad_Data.folderInfos.Clear();
            foreach (string directory in directories)
            {
                string localDirectory = directory.Replace("\\", "/").Replace(path, "");
                SmClothesLoad_Data.folderInfos.Add(new SmClothesLoad_Data.FolderInfo() { fullPath = directory, path = localDirectory, name = localDirectory.Substring(1) });
            }
            ___folderInfoSex = ___chaInfo.Sex;
            ___initEnd = true;
        }
    }

    [HarmonyPatch(typeof(SmClothesLoad), "CreateListObject")]
    public class SmClothesLoad_CreateListObject_Patches
    {
        public static bool Prepare()
        {
            return OptimizeCharaMaker._optimizeCharaMaker;
        }

        public static bool Prefix(SmClothesLoad __instance, CharInfo ___chaInfo, List<SmClothesLoad.FileInfo> ___lstFileInfo, List<RectTransform> ___lstRtfTgl)
        {
            if (__instance.imgPrev)
                __instance.imgPrev.enabled = false;
            if (SmClothesLoad_Data.created == false)
            {
                if (SmClothesLoad_Data.searchBar != null)
                    SmClothesLoad_Data.ResetSearch(___lstFileInfo);

                if (SmClothesLoad_Data._createListObject == null || SmClothesLoad_Data._createListObject.MoveNext() == false)
                    SmClothesLoad_Data._createListObject = CreateListObject(__instance, ___chaInfo, ___lstFileInfo, ___lstRtfTgl);

                while (SmClothesLoad_Data._createListObject.MoveNext());

                SmClothesLoad_Data._createListObject = null;
            }

            return false;
        }

        internal static IEnumerator CreateListObject(SmClothesLoad __instance, CharInfo ___chaInfo, List<SmClothesLoad.FileInfo> ___lstFileInfo, List<RectTransform> ___lstRtfTgl)
        {
            if (__instance.objListTop.transform.childCount > 0 && SmClothesLoad_Data.clothes.ContainsValue(__instance.objListTop.transform.GetComponentInChildren<SmClothesLoad.FileInfoComponent>()) == false)
            {
                ___lstRtfTgl.Clear();
                for (int j = 0; j < __instance.objListTop.transform.childCount; j++)
                {
                    GameObject go = __instance.objListTop.transform.GetChild(j).gameObject;
                    GameObject.Destroy(go);
                }
                SmClothesLoad_Data.folders.Clear();
                SmClothesLoad_Data.clothes.Clear();
                SmClothesLoad_Data.parentFolder = null;
                yield return null;
            }

            if (SmClothesLoad_Data.parentFolder == null)
            {
                SmClothesLoad_Data.parentFolder = GameObject.Instantiate(__instance.objLineBase).GetComponent<Toggle>();
                SmClothesLoad_Data.parentFolder.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                SmClothesLoad_Data.parentFolder.transform.FindLoop("Background").GetComponent<Image>().color = OptimizeCharaMaker._subFoldersColor;
                SmClothesLoad_Data.parentFolder.transform.SetParent(__instance.objListTop.transform, false);
                SmClothesLoad_Data.parentFolder.transform.localScale = Vector3.one;
                Text text = SmClothesLoad_Data.parentFolder.GetComponentInChildren<Text>();
                SmClothesLoad_Data.parentFolder.onValueChanged.AddListener((b) =>
                {
                    if (SmClothesLoad_Data.parentFolder.isOn)
                    {
                        bool initEnd = false;
                        byte folderInfoSex = 0;
                        SmClothesLoad_Data.created = false;
                        int index = OptimizeCharaMaker._currentClothesPathGame.LastIndexOf("/", StringComparison.OrdinalIgnoreCase);
                        OptimizeCharaMaker._currentClothesPathGame = OptimizeCharaMaker._currentClothesPathGame.Remove(index);
                        SmClothesLoad_Init_Patches.Prefix(__instance, ref initEnd, ref folderInfoSex, ___chaInfo, ___lstFileInfo);
                        Prefix(__instance, ___chaInfo, ___lstFileInfo, ___lstRtfTgl);
                        __instance.UpdateSort();
                    }
                });
                text.fontStyle = FontStyle.BoldAndItalic;
                text.text = "../ (Parent folder)";
            }

            SmClothesLoad_Data.parentFolder.isOn = false;
            SmClothesLoad_Data.parentFolder.transform.SetAsLastSibling();
            SmClothesLoad_Data.parentFolder.gameObject.SetActive(OptimizeCharaMaker._currentClothesPathGame.Length > 1);

            int i = 0;
            for (; i < SmClothesLoad_Data.folderInfos.Count; i++)
            {
                SmClothesLoad_Data.FolderInfo fi = SmClothesLoad_Data.folderInfos[i];
                SmClothesLoad_Data.EntryDisplay display;
                if (i < SmClothesLoad_Data.folders.Count)
                    display = SmClothesLoad_Data.folders[i];
                else
                {
                    display = new SmClothesLoad_Data.EntryDisplay();
                    display.gameObject = GameObject.Instantiate(__instance.objLineBase);
                    display.toggle = display.gameObject.GetComponent<Toggle>();
                    display.text = display.gameObject.GetComponentInChildren<Text>();
                    display.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                    display.gameObject.transform.FindLoop("Background").GetComponent<Image>().color = OptimizeCharaMaker._subFoldersColor;
                    display.gameObject.transform.SetParent(__instance.objListTop.transform, false);
                    display.gameObject.transform.localScale = Vector3.one;
                    ___lstRtfTgl.Add((RectTransform)display.gameObject.transform);
                    display.text.fontStyle = FontStyle.BoldAndItalic;
                    SmClothesLoad_Data.folders.Add(display);
                }
                fi.display = display;
                display.gameObject.SetActive(true);
                display.gameObject.transform.SetAsLastSibling();
                display.toggle.onValueChanged = new Toggle.ToggleEvent();
                display.toggle.onValueChanged.AddListener((b) =>
                {
                    if (display.toggle.isOn)
                    {
                        bool initEnd = false;
                        byte folderInfoSex = 0;
                        SmClothesLoad_Data.created = false;
                        OptimizeCharaMaker._currentClothesPathGame += fi.path;
                        SmClothesLoad_Init_Patches.Prefix(__instance, ref initEnd, ref folderInfoSex, ___chaInfo, ___lstFileInfo);
                        Prefix(__instance, ___chaInfo, ___lstFileInfo, ___lstRtfTgl);
                        __instance.UpdateSort();
                    }
                });
                display.toggle.isOn = false;
                display.text.text = fi.name;
                yield return null;
            }
            for(;i < SmClothesLoad_Data.folders.Count; ++i)
                SmClothesLoad_Data.folders[i].gameObject.SetActive(false);

            foreach (KeyValuePair<SmClothesLoad.FileInfo, SmClothesLoad.FileInfoComponent> pair in SmClothesLoad_Data.clothes)
            {
                pair.Value.gameObject.SetActive(false);
                SmClothesLoad_Data.clothesEntryPool.AddLast(pair.Value);
            }
            SmClothesLoad_Data.clothes.Clear();

            ToggleGroup component = __instance.objListTop.GetComponent<ToggleGroup>();
            i = 0;
            for (; i < ___lstFileInfo.Count; i++)
            {
                SmClothesLoad.FileInfo fi = ___lstFileInfo[i];
                SmClothesLoad.FileInfoComponent fileInfoComponent;
                if (i < SmClothesLoad_Data.clothes.Count)
                {
                    fileInfoComponent = SmClothesLoad_Data.clothesEntryPool.Last.Value;
                    SmClothesLoad_Data.clothesEntryPool.RemoveFirst();
                }
                else
                {
                    fileInfoComponent = GameObject.Instantiate(__instance.objLineBase).AddComponent<SmClothesLoad.FileInfoComponent>();
                    fileInfoComponent.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                    fileInfoComponent.transform.SetParent(__instance.objListTop.transform, false);
                    GameObject gameObject2 = null;
                    gameObject2 = fileInfoComponent.transform.FindLoop("Background");
                    if (gameObject2)
                        fileInfoComponent.imgBG = gameObject2.GetComponent<Image>();
                    gameObject2 = fileInfoComponent.transform.FindLoop("Checkmark");
                    if (gameObject2)
                        fileInfoComponent.imgCheck = gameObject2.GetComponent<Image>();
                    gameObject2 = fileInfoComponent.transform.FindLoop("Label");
                    if (gameObject2)
                        fileInfoComponent.text = gameObject2.GetComponent<Text>();
                    fileInfoComponent.tgl = fileInfoComponent.GetComponent<Toggle>();
                    fileInfoComponent.tgl.group = component;
                    __instance.CallPrivate("SetButtonClickHandler", fileInfoComponent.gameObject);
                    RectTransform rectTransform = (RectTransform)fileInfoComponent.transform;
                    rectTransform.localScale = Vector3.one;
                    ___lstRtfTgl.Add(rectTransform);
                }

                fileInfoComponent.info = fi;
                fileInfoComponent.tgl.isOn = false;
                if (OptimizeCharaMaker._asyncLoading && SmClothesLoad_Data._translateProperty != null) // Fuck you translation plugin
                {
                    SmClothesLoad_Data._translateProperty.SetValue(fileInfoComponent.text, false, null);
                    string t = fileInfoComponent.info.comment;
                    HSUS._self._translationMethod(ref t);
                    fileInfoComponent.text.text = t;
                }
                else
                    fileInfoComponent.text.text = fileInfoComponent.info.comment;
                fileInfoComponent.gameObject.SetActive(true);
                SmClothesLoad_Data.clothes.Add(fi, fileInfoComponent);
                yield return null;
            }

            SmClothesLoad_Data.created = true;
            LayoutRebuilder.MarkLayoutForRebuild(__instance.rtfPanel);
        }
    }

    [HarmonyPatch(typeof(SmClothesLoad), "ExecuteSaveNew")]
    public class SmClothesLoad_ExecuteSaveNew_Patches
    {
        public static bool Prefix(SmClothesLoad __instance, CharInfo ___chaInfo, ref bool ___nowSave, CharFileInfoClothes ___clothesInfo, List<SmClothesLoad.FileInfo> ___lstFileInfo)
        {
            SmClothesLoad_Data.created = false;
            if (null == ___chaInfo || ___chaInfo.chaFile == null)
            {
                return false;
            }
            if (___nowSave)
            {
                return false;
            }
            ___nowSave = true;
            __instance.CreateClothesPngBefore();
            __instance.StartCoroutine(ExecuteSaveNewCoroutine(__instance, ___chaInfo, ___clothesInfo, ___lstFileInfo));
            return false;
        }

        private static IEnumerator ExecuteSaveNewCoroutine(SmClothesLoad __instance, CharInfo ___chaInfo, CharFileInfoClothes ___clothesInfo, List<SmClothesLoad.FileInfo> ___lstFileInfo)
        {
            yield return new WaitForEndOfFrame();
            __instance.CreateClothesPng();
            string filename = string.Empty;
            if (___chaInfo.Sex == 0)
                filename = "coordM_" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
            else
                filename = "coordF_" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
            string fullPath = ___clothesInfo.ConvertClothesFilePath(filename);
            fullPath = Path.GetDirectoryName(fullPath) + OptimizeCharaMaker._currentClothesPathGame + "/" + Path.GetFileName(fullPath);
            __instance.customControl.CustomSaveClothesAssist(fullPath);
            SmClothesLoad.FileInfo fi = new SmClothesLoad.FileInfo();
            fi.no = 0;
            fi.time = DateTime.Now;
            fi.fileName = filename;
            fi.fullPath = fullPath;
            fi.comment = ___clothesInfo.comment;
            ___lstFileInfo.Add(fi);
            __instance.CreateListObject();
            __instance.UpdateSort();
            __instance.StartCoroutine(__instance.CreateClothesPngAfter());
            yield return null;
        }
    }

    [HarmonyPatch(typeof(SmClothesLoad), "ExecuteDelete")]
    public class SmClothesLoad_ExecuteDelete_Patches
    {
        public static void Prefix()
        {
            SmClothesLoad_Data.created = false;
        }
    }

    [HarmonyPatch(typeof(SmClothesLoad), "OnSortDate")]
    public class SmClothesLoad_OnSortDate_Patches
    {
        public static bool Prepare()
        {
            return OptimizeCharaMaker._optimizeCharaMaker;
        }

        public static void Postfix(SmClothesLoad __instance)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(__instance.rtfPanel);
        }
    }

    [HarmonyPatch(typeof(SmClothesLoad), "OnSortName")]
    public class SmClothesLoad_OnSortName_Patches
    {
        public static bool Prepare()
        {
            return OptimizeCharaMaker._optimizeCharaMaker;
        }

        public static void Postfix(SmClothesLoad __instance)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(__instance.rtfPanel);
        }
    }

    [HarmonyPatch(typeof(SmClothesLoad), "SortDate", new []{typeof(bool)})]
    public class SmClothesLoad_SortDate_Patches
    {
        public static bool Prepare()
        {
            return OptimizeCharaMaker._optimizeCharaMaker;
        }

        public static bool Prefix(SmClothesLoad __instance, bool ascend, List<SmClothesLoad.FileInfo> ___lstFileInfo)
        {
            __instance.SetPrivateExplicit<SmClothesLoad>("ascendDate", ascend);
            if (___lstFileInfo.Count == 0)
                return false;
            __instance.SetPrivateExplicit<SmClothesLoad>("lastSort", (byte)1);
            CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("ja-JP");
            if (ascend)
            {
                ___lstFileInfo.Sort((a, b) => a.time.CompareTo(b.time));
                SmClothesLoad_Data.folderInfos.Sort((a, b) => Directory.GetLastWriteTime(a.fullPath).CompareTo(Directory.GetLastWriteTime(b.fullPath)));
            }
            else
            {
                ___lstFileInfo.Sort((a, b) => b.time.CompareTo(a.time));
                SmClothesLoad_Data.folderInfos.Sort((a, b) => Directory.GetLastWriteTime(b.fullPath).CompareTo(Directory.GetLastWriteTime(a.fullPath)));
            }
            Thread.CurrentThread.CurrentCulture = currentCulture;
            SmClothesLoad_Data.parentFolder.transform.SetAsLastSibling();
            foreach (SmClothesLoad_Data.FolderInfo fi in SmClothesLoad_Data.folderInfos)
            {
                if (fi != null && fi.display != null && fi.display.gameObject != null)
                    fi.display.gameObject.transform.SetAsLastSibling();
            }
            int i = 0;
            foreach (SmClothesLoad.FileInfo fi in ___lstFileInfo)
            {
                fi.no = i;
                SmClothesLoad.FileInfoComponent fileInfoComponent;
                if (SmClothesLoad_Data.clothes.TryGetValue(fi, out fileInfoComponent))
                    fileInfoComponent.transform.SetAsLastSibling();
                ++i;
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(SmClothesLoad), "SortName", new[] { typeof(bool) })]
    public class SmClothesLoad_SortName_Patches
    {
        public static bool Prepare()
        {
            return OptimizeCharaMaker._optimizeCharaMaker;
        }

        public static bool Prefix(SmClothesLoad __instance, bool ascend, List<SmClothesLoad.FileInfo> ___lstFileInfo)
        {
            __instance.SetPrivate("ascendName", ascend);
            if (___lstFileInfo.Count == 0)
                return false;
            __instance.SetPrivate("lastSort", (byte)0);
            CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("ja-JP");
            if (ascend)
            {
                ___lstFileInfo.Sort((a, b) => string.Compare(a.comment, b.comment, StringComparison.CurrentCultureIgnoreCase));
                SmClothesLoad_Data.folderInfos.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.CurrentCultureIgnoreCase));
            }
            else
            {
                ___lstFileInfo.Sort((a, b) => string.Compare(b.comment, a.comment, StringComparison.CurrentCultureIgnoreCase));
                SmClothesLoad_Data.folderInfos.Sort((a, b) => string.Compare(b.name, a.name, StringComparison.CurrentCultureIgnoreCase));
            }
            Thread.CurrentThread.CurrentCulture = currentCulture;
            SmClothesLoad_Data.parentFolder.transform.SetAsLastSibling();
            foreach (SmClothesLoad_Data.FolderInfo fi in SmClothesLoad_Data.folderInfos)
            {
                if (fi != null && fi.display != null && fi.display.gameObject != null)
                    fi.display.gameObject.transform.SetAsLastSibling();
            }
            int i = 0;
            foreach (SmClothesLoad.FileInfo fi in ___lstFileInfo)
            {
                fi.no = i;
                SmClothesLoad.FileInfoComponent fileInfoComponent;
                if (SmClothesLoad_Data.clothes.TryGetValue(fi, out fileInfoComponent))
                    fileInfoComponent.transform.SetAsLastSibling();
                ++i;
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(SmClothesLoad), "UpdateSort")]
    internal static class SmClothesLoad_UpdateSort_Patches
    {
        private static bool Prepare()
        {
            return OptimizeCharaMaker._optimizeCharaMaker;
        }

        private static bool Prefix(SmClothesLoad __instance)
        {
            if ((byte)__instance.GetPrivate("lastSort") == 0)
                __instance.SortName((bool)__instance.GetPrivate("ascendName"));
            else
                __instance.SortDate((bool)__instance.GetPrivate("ascendDate"));
            return false;
        }
    }

}

#endif