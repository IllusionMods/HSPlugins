
using HSUS.Features;
using ToolBox.Extensions;
#if HONEYSELECT
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using CustomMenu;
using Harmony;
using IllusionUtility.GetUtility;
using Manager;
using ToolBox;
using UnityEngine;
using UnityEngine.UI;

namespace HSUS
{
    public static class SmCharaLoad_Data
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

        public static readonly Dictionary<SmCharaLoad.FileInfo, SmCharaLoad.FileInfoComponent> characters = new Dictionary<SmCharaLoad.FileInfo, SmCharaLoad.FileInfoComponent>();
        public static readonly LinkedList<SmCharaLoad.FileInfoComponent> characterEntryPool = new LinkedList<SmCharaLoad.FileInfoComponent>();
        public static readonly List<EntryDisplay> folders = new List<EntryDisplay>();
        public static Toggle parentFolder;
        public static int lastMenuType;
        public static bool created;
        public static RectTransform container;
        public static List<SmCharaLoad.FileInfo> fileInfos;
        public static readonly List<FolderInfo> folderInfos = new List<FolderInfo>();
        public static List<RectTransform> rectTransforms;
        public static InputField searchBar;
        internal static IEnumerator _createListObject;
        internal static PropertyInfo _translateProperty = null;

        private static SmCharaLoad _originalComponent;

        public static void Init(SmCharaLoad originalComponent)
        {
            Reset();

            _originalComponent = originalComponent;
            lastMenuType = (int)_originalComponent.GetPrivate("nowSubMenuTypeId");
            fileInfos = (List<SmCharaLoad.FileInfo>)_originalComponent.GetPrivate("lstFileInfo");
            rectTransforms = ((List<RectTransform>)_originalComponent.GetPrivate("lstRtfTgl"));

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

            searchBar = CharaMakerSearch.SpawnSearchBar(_originalComponent.transform, SearchChanged, -24f);

            _translateProperty = typeof(Text).GetProperty("Translate", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            if (OptimizeCharaMaker._asyncLoading)
            {
                originalComponent.SetPrivate("chaInfo", originalComponent.customControl.chainfo);
                originalComponent.Init();
                _createListObject = SmCharaLoad_CreateListObject_Patches.CreateListObject(originalComponent, originalComponent.customControl.chainfo);
                HSUS._self._asyncMethods.Add(_createListObject);
            }

            originalComponent.objLineBase.transform.Find("Label").GetComponent<Text>().raycastTarget = false;
            originalComponent.objLineBase.transform.Find("Background/Checkmark").GetComponent<Image>().raycastTarget = false;
        }
        private static void Reset()
        {
            characters.Clear();
            folders.Clear();
            folderInfos.Clear();
            created = false;
            fileInfos = null;
            rectTransforms = null;
            _createListObject = null;
            parentFolder = null;
        }

        public static void SearchChanged(string arg0)
        {
            string search = searchBar.text.Trim();
            int i = 0;
            for (; i < fileInfos.Count; i++)
            {
                SmCharaLoad.FileInfo fi = fileInfos[i];
                if (fi.noAccess == false)
                    characters[fi].gameObject.SetActive(fi.CharaName.IndexOf(search, StringComparison.OrdinalIgnoreCase) != -1);
                else
                    characters[fi].gameObject.SetActive(false);
            }
        }

        public static void ResetSearch()
        {
            InputField.OnChangeEvent searchEvent = searchBar.onValueChanged;
            searchBar.onValueChanged = null;
            searchBar.text = "";
            searchBar.onValueChanged = searchEvent;
            int i = 0;
            for (; i < fileInfos.Count; i++)
            {
                SmCharaLoad.FileInfo fi = fileInfos[i];
                SmCharaLoad.FileInfoComponent component;
                if (characters.TryGetValue(fi, out component))
                    component.gameObject.SetActive(fi.noAccess == false);
            }
        }
    }

    [HarmonyPatch(typeof(SmCharaLoad), "Init")]
    internal static class SmCharaLoad_Init_Patches
    {
        private static bool Prepare()
        {
            return OptimizeCharaMaker._optimizeCharaMaker;
        }

        internal static bool Prefix(SmCharaLoad __instance, ref bool ___initEnd, ref byte ___folderInfoSex, CharInfo ___chaInfo, List<SmCharaLoad.FileInfo> ___lstFileInfo)
        {

            if (___initEnd && ___folderInfoSex == ___chaInfo.Sex)
            {
                return false;
            }
            if (null == ___chaInfo)
            {
                return false;
            }
            global::FolderAssist folderAssist = new global::FolderAssist();
            string basePath = global::UserData.Path + ((___chaInfo.Sex != 0) ? "chara/female" : "chara/male");
            string path = basePath + OptimizeCharaMaker._currentCharaPathGame;
            folderAssist.CreateFolderInfoEx(path, new[] { "*.png" });
            CharFile charFile;
            if (___chaInfo.Sex == 0)
            {
                CharMaleFile charMaleFile = new CharMaleFile();
                charFile = charMaleFile;
            }
            else
            {
                CharFemaleFile charFemaleFile = new CharFemaleFile();
                charFile = charFemaleFile;
            }
            ___lstFileInfo.Clear();
            int fileCount = folderAssist.GetFileCount();
            int num = 0;
            for (int i = 0; i < fileCount; i++)
            {
                CharFileInfoPreview charFileInfoPreview = new CharFileInfoPreview();
                if (charFile.LoadBlockData<CharFileInfoPreview>(charFileInfoPreview, folderAssist.lstFile[i].FullPath))
                {
                    if (charFileInfoPreview.sex == (int)___chaInfo.Sex)
                    {
                        if (___chaInfo.Sex != 0 || "ill_Player" != folderAssist.lstFile[i].FileName)
                        {
                            SmCharaLoad.FileInfo fileInfo = new SmCharaLoad.FileInfo();
                            fileInfo.no = num;
                            fileInfo.FullPath = folderAssist.lstFile[i].FullPath;
                            fileInfo.FileName = folderAssist.lstFile[i].FileName;
                            fileInfo.time = folderAssist.lstFile[i].time;
                            fileInfo.CharaName = charFileInfoPreview.name;
                            fileInfo.personality = charFileInfoPreview.personality;
                            fileInfo.limited = (charFileInfoPreview.isConcierge != 0);
                            ___lstFileInfo.Add(fileInfo);
                            num++;
                        }
                    }
                }
            }

            string[] directories = Directory.GetDirectories(path);
            SmCharaLoad_Data.folderInfos.Clear();
            foreach (string directory in directories)
            {
                string localDirectory = directory.Replace("\\", "/").Replace(path, "");
                SmCharaLoad_Data.folderInfos.Add(new SmCharaLoad_Data.FolderInfo() { fullPath = directory, path = localDirectory, name = localDirectory.Substring(1) });
            }
            ___folderInfoSex = ___chaInfo.Sex;
            ___initEnd = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(SmCharaLoad), "CreateListObject")]
    public class SmCharaLoad_CreateListObject_Patches
    {
        public static bool Prepare()
        {
            return OptimizeCharaMaker._optimizeCharaMaker;
        }

        public static bool Prefix(SmCharaLoad __instance, CharInfo ___chaInfo)
        {
            int nowSubMenuTypeId = (int)__instance.GetPrivate("nowSubMenuTypeId");
            if (SmCharaLoad_Data.lastMenuType == nowSubMenuTypeId && SmCharaLoad_Data.created)
                return false;
            SmCharaLoad_Data.ResetSearch();
            if (__instance.imgPrev)
                __instance.imgPrev.enabled = false;

            if (SmCharaLoad_Data._createListObject == null || SmCharaLoad_Data._createListObject.MoveNext() == false)
                SmCharaLoad_Data._createListObject = CreateListObject(__instance, ___chaInfo);

            while (SmCharaLoad_Data._createListObject.MoveNext());

            SmCharaLoad_Data._createListObject = null;
            foreach (SmCharaLoad.FileInfo fi in SmCharaLoad_Data.fileInfos)
                SmCharaLoad_Data.characters[fi].gameObject.SetActive(!fi.noAccess);
            LayoutRebuilder.ForceRebuildLayoutImmediate(__instance.rtfPanel);
            SmCharaLoad_Data.lastMenuType = nowSubMenuTypeId;
            return false;
        }

        internal static IEnumerator CreateListObject(SmCharaLoad __instance, CharInfo ___chaInfo)
        {
            if (SmCharaLoad_Data.created)
                yield break;
            ToggleGroup component = __instance.objListTop.GetComponent<ToggleGroup>();

            if (SmCharaLoad_Data.parentFolder == null)
            {
                SmCharaLoad_Data.parentFolder = GameObject.Instantiate(__instance.objLineBase).GetComponent<Toggle>();
                SmCharaLoad_Data.parentFolder.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                SmCharaLoad_Data.parentFolder.transform.FindLoop("Background").GetComponent<Image>().color = OptimizeCharaMaker._subFoldersColor;
                SmCharaLoad_Data.parentFolder.transform.SetParent(__instance.objListTop.transform, false);
                SmCharaLoad_Data.parentFolder.transform.localScale = Vector3.one;
                ((RectTransform)SmCharaLoad_Data.parentFolder.transform).sizeDelta = new Vector2(SmCharaLoad_Data.container.rect.width, 24f);
                Text text = SmCharaLoad_Data.parentFolder.GetComponentInChildren<Text>();
                SmCharaLoad_Data.parentFolder.onValueChanged.AddListener((b) =>
                {
                    if (SmCharaLoad_Data.parentFolder.isOn)
                    {
                        bool initEnd = false;
                        byte folderInfoSex = 0;
                        SmCharaLoad_Data.created = false;
                        int index = OptimizeCharaMaker._currentCharaPathGame.LastIndexOf("/", StringComparison.OrdinalIgnoreCase);
                        OptimizeCharaMaker._currentCharaPathGame = OptimizeCharaMaker._currentCharaPathGame.Remove(index);
                        SmCharaLoad_Init_Patches.Prefix(__instance, ref initEnd, ref folderInfoSex, ___chaInfo, SmCharaLoad_Data.fileInfos);
                        Prefix(__instance, ___chaInfo);
                    }
                });
                text.fontStyle = FontStyle.BoldAndItalic;
                text.text = "../ (Parent folder)";
            }

            SmCharaLoad_Data.parentFolder.isOn = false;
            SmCharaLoad_Data.parentFolder.transform.SetAsLastSibling();
            SmCharaLoad_Data.parentFolder.gameObject.SetActive(OptimizeCharaMaker._currentCharaPathGame.Length > 1);

            int i = 0;
            for (; i < SmCharaLoad_Data.folderInfos.Count; i++)
            {
                SmCharaLoad_Data.FolderInfo fi = SmCharaLoad_Data.folderInfos[i];
                SmCharaLoad_Data.EntryDisplay display;
                if (i < SmCharaLoad_Data.folders.Count)
                    display = SmCharaLoad_Data.folders[i];
                else
                {
                    GameObject gameObject = GameObject.Instantiate(__instance.objLineBase);
                    gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                    gameObject.transform.FindLoop("Background").GetComponent<Image>().color = OptimizeCharaMaker._subFoldersColor;
                    gameObject.transform.SetParent(__instance.objListTop.transform, false);
                    gameObject.transform.localScale = Vector3.one;
                    ((RectTransform)gameObject.transform).sizeDelta = new Vector2(SmCharaLoad_Data.container.rect.width, 24f);
                    display = new SmCharaLoad_Data.EntryDisplay
                    {
                        gameObject = gameObject,
                        toggle = gameObject.GetComponent<Toggle>(),
                        text = gameObject.GetComponentInChildren<Text>()
                    };
                    display.text.fontStyle = FontStyle.BoldAndItalic;
                    SmCharaLoad_Data.folders.Add(display);
                }
                fi.display = display;
                display.gameObject.transform.SetAsLastSibling();
                display.gameObject.SetActive(true);
                display.toggle.isOn = false;
                display.toggle.onValueChanged = new Toggle.ToggleEvent();
                display.toggle.onValueChanged.AddListener((b) =>
                {
                    if (display.toggle.isOn)
                    {
                        bool initEnd = false;
                        byte folderInfoSex = 0;
                        SmCharaLoad_Data.created = false;
                        OptimizeCharaMaker._currentCharaPathGame += fi.path;
                        SmCharaLoad_Init_Patches.Prefix(__instance, ref initEnd, ref folderInfoSex, ___chaInfo, SmCharaLoad_Data.fileInfos);
                        Prefix(__instance, ___chaInfo);
                    }
                });
                display.text.text = fi.name;
                yield return null;
            }
            for (; i < SmCharaLoad_Data.folders.Count; ++i)
                SmCharaLoad_Data.folders[i].gameObject.SetActive(false);

            i = 0;

            foreach (KeyValuePair<SmCharaLoad.FileInfo, SmCharaLoad.FileInfoComponent> pair in SmCharaLoad_Data.characters)
            {
                pair.Value.gameObject.SetActive(false);
                SmCharaLoad_Data.characterEntryPool.AddLast(pair.Value);
            }
            SmCharaLoad_Data.characters.Clear();

            for (; i < SmCharaLoad_Data.fileInfos.Count; i++)
            {
                SmCharaLoad.FileInfo fi = SmCharaLoad_Data.fileInfos[i];
                SmCharaLoad.FileInfoComponent fileInfoComponent;
                if (i < SmCharaLoad_Data.characters.Count)
                {
                    fileInfoComponent = SmCharaLoad_Data.characterEntryPool.Last.Value;
                    SmCharaLoad_Data.characterEntryPool.RemoveFirst();
                }
                else
                {
                    GameObject gameObject = GameObject.Instantiate(__instance.objLineBase);
                    gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                    fileInfoComponent = gameObject.AddComponent<SmCharaLoad.FileInfoComponent>();
                    fileInfoComponent.tgl = gameObject.GetComponent<Toggle>();
                    GameObject gameObject2 = gameObject.transform.FindLoop("Background");
                    if (gameObject2)
                    {
                        fileInfoComponent.imgBG = gameObject2.GetComponent<Image>();
                    }
                    gameObject2 = gameObject.transform.FindLoop("Checkmark");
                    if (gameObject2)
                    {
                        fileInfoComponent.imgCheck = gameObject2.GetComponent<Image>();
                    }
                    gameObject2 = gameObject.transform.FindLoop("Label");
                    if (gameObject2)
                    {
                        fileInfoComponent.text = gameObject2.GetComponent<Text>();
                    }
                    fileInfoComponent.tgl.group = component;
                    gameObject.transform.SetParent(__instance.objListTop.transform, false);
                    gameObject.transform.localScale = Vector3.one;
                    ((RectTransform)gameObject.transform).sizeDelta = new Vector2(SmCharaLoad_Data.container.rect.width, 24f);
                    SmCharaLoad_Data.rectTransforms.Add((RectTransform)gameObject.transform);

                    __instance.CallPrivate("SetButtonClickHandler", gameObject);
                }
                fileInfoComponent.gameObject.transform.SetAsLastSibling();
                fileInfoComponent.gameObject.SetActive(true);
                fileInfoComponent.info = fi;

                if (OptimizeCharaMaker._asyncLoading && SmCharaLoad_Data._translateProperty != null) // Fuck you translation plugin
                {
                    SmCharaLoad_Data._translateProperty.SetValue(fileInfoComponent.text, false, null);
                    string t = fileInfoComponent.info.CharaName;
                    HSUS._self._translationMethod(ref t);
                    fileInfoComponent.text.text = t;
                }
                else
                    fileInfoComponent.text.text = fileInfoComponent.info.CharaName;
                SmCharaLoad_Data.characters.Add(fi, fileInfoComponent);
                yield return null;
            }

            SmCharaLoad_Data.created = true;
            __instance.UpdateSort();
        }
    }

    [HarmonyPatch(typeof(SmCharaLoad), "ExecuteSaveNew")]
    public class SmCharaLoad_ExecuteSaveNew_Patches
    {
        private static bool Prepare()
        {
            return OptimizeCharaMaker._optimizeCharaMaker;
        }

        public static bool Prefix(SmCharaLoad __instance, CharInfo ___chaInfo, CharFileInfoCustom ___customInfo, List<SmCharaLoad.FileInfo> ___lstFileInfo)
        {
            SmCharaLoad_Data.created = false;

            if (null == ___chaInfo || ___chaInfo.chaFile == null)
            {
                return false;
            }
            string text = string.Empty;
            if (___chaInfo.Sex == 0)
            {
                text = "charaM_" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
            }
            else
            {
                text = "charaF_" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
            }
            if (___chaInfo.Sex == 1)
            {
                CharFileInfoParameterFemale charFileInfoParameterFemale = ___chaInfo.chaFile.parameterInfo as CharFileInfoParameterFemale;
                charFileInfoParameterFemale.InitParameter = true;
                global::Singleton<Info>.Instance.InitState(charFileInfoParameterFemale, ___chaInfo.customInfo.personality, true, false);
            }
            string fullPath = ___chaInfo.chaFile.ConvertCharaFilePath(text);
            fullPath = Path.GetDirectoryName(fullPath) + OptimizeCharaMaker._currentCharaPathGame + "/" + Path.GetFileName(fullPath);
            __instance.customControl.CustomSaveCharaAssist(fullPath);
            SmCharaLoad.FileInfo fileInfo = new SmCharaLoad.FileInfo();
            fileInfo.no = 0;
            fileInfo.time = DateTime.Now;
            fileInfo.FileName = text;
            fileInfo.FullPath = fullPath;
            fileInfo.CharaName = ___customInfo.name;
            fileInfo.personality = ___customInfo.personality;
            fileInfo.limited = false;
            ___lstFileInfo.Add(fileInfo);
            __instance.CreateListObject();
            __instance.UpdateSort();
            return false;
        }
    }

    internal static class BoneControllerMgr_GetFileName_Replacement
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            for (int i = 0; i < instructionsList.Count; i++)
            {
                CodeInstruction inst = instructionsList[i];
                if (inst.ToString().Equals("call System.String GetFileName(System.String)"))
                    yield return new CodeInstruction(OpCodes.Call, typeof(BoneControllerMgr_GetFileName_Replacement).GetMethod(nameof(Injected), BindingFlags.NonPublic | BindingFlags.Static));
                else
                    yield return inst;
            }
        }

        private static string Injected(string fullPath)
        {
            return fullPath.Replace("\\", "/");
        }

    }

    [HarmonyPatch(typeof(CharFile), "Load", typeof(string), typeof(bool), typeof(bool))]
    internal static class CharFile_Load_Patches
    {
        internal static readonly Dictionary<CharFile, string> _fullPathByInstance = new Dictionary<CharFile, string>();
        private static bool Prepare()
        {
            return (OptimizeCharaMaker._optimizeCharaMaker && HSUS._self._binary == Binary.Game) || (OptimizeNEO._optimizeNeo && HSUS._self._binary == Binary.Studio);
        }

        private static void Prefix(CharFile __instance, string path)
        {
            if (_fullPathByInstance.ContainsKey(__instance) == false)
                _fullPathByInstance.Add(__instance, path);
            else
                _fullPathByInstance[__instance] = path;
        }
    }

    [HarmonyPatch]
    internal static class BoneControllerMgr_OnLoadClick_Patches
    {
        private static MethodInfo TargetMethod()
        {
            if (HSUS._self._binary == Binary.Game)
                return Type.GetType("AdditionalBoneModifier.BoneControllerMgr,AdditionalBoneModifier").GetMethod("OnLoadClick", BindingFlags.NonPublic | BindingFlags.Instance);
            return null;
        }
        private static bool Prepare()
        {
            return OptimizeCharaMaker._optimizeCharaMaker && HSUS._self._binary == Binary.Game && Type.GetType("AdditionalBoneModifier.BoneControllerMgr,AdditionalBoneModifier") != null;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return BoneControllerMgr_GetFileName_Replacement.Transpiler(instructions);
        }
    }

    [HarmonyPatch]
    internal static class BoneControllerMgr_OnSaveClick_Patches
    {
        private static MethodInfo TargetMethod()
        {
            if (HSUS._self._binary == Binary.Game)
                return Type.GetType("AdditionalBoneModifier.BoneControllerMgr,AdditionalBoneModifier").GetMethod("OnSaveClick", BindingFlags.NonPublic | BindingFlags.Instance);
            return null;
        }

        private static bool Prepare()
        {
            return OptimizeCharaMaker._optimizeCharaMaker && HSUS._self._binary == Binary.Game && Type.GetType("AdditionalBoneModifier.BoneControllerMgr,AdditionalBoneModifier") != null;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return BoneControllerMgr_GetFileName_Replacement.Transpiler(instructions);
        }
    }

    [HarmonyPatch]
    internal static class BoneController_GetExtDataFilePath_Patches
    {
        private static MethodInfo TargetMethod()
        {
            if (HSUS._self._binary == Binary.Game)
            {
                return Type.GetType("AdditionalBoneModifier.BoneController,AdditionalBoneModifier").GetMethods(BindingFlags.Public | BindingFlags.Instance).First(m => m.Name.Equals("GetExtDataFilePath") && m.GetParameters().Length == 1);
            }
            return Type.GetType("AdditionalBoneModifier.BoneController,AdditionalBoneModifierStudioNEO").GetMethods(BindingFlags.Public | BindingFlags.Instance).First(m => m.Name.Equals("GetExtDataFilePath") && m.GetParameters().Length == 1);
        }

        private static bool Prepare()
        {
            return (OptimizeCharaMaker._optimizeCharaMaker && HSUS._self._binary == Binary.Game && Type.GetType("AdditionalBoneModifier.BoneController,AdditionalBoneModifier") != null) || (OptimizeNEO._optimizeNeo && HSUS._self._binary == Binary.Studio && Type.GetType("AdditionalBoneModifier.BoneController,AdditionalBoneModifierStudioNEO") != null);
        }

        private static void Postfix(CharInfo ___charInfo, ref string __result)
        {
            if (__result == null)
                return;
            string fullPath;
            if (CharFile_Load_Patches._fullPathByInstance.TryGetValue(___charInfo.chaFile, out fullPath))
                __result = fullPath + ".bonemod.txt";
        }
    }

    [HarmonyPatch]
    internal static class BoneController_IsEditingCharacter_Patches
    {
        private static MethodInfo TargetMethod()
        {
            if (HSUS._self._binary == Binary.Game)
                return Type.GetType("AdditionalBoneModifier.BoneController,AdditionalBoneModifier").GetMethod("IsEditingCharacter", BindingFlags.NonPublic | BindingFlags.Instance);
            return null;
        }

        private static bool Prepare()
        {
            return OptimizeCharaMaker._optimizeCharaMaker && HSUS._self._binary == Binary.Game && Type.GetType("AdditionalBoneModifier.BoneController,AdditionalBoneModifier") != null;
        }

        private static void Postfix(CharInfo ___charInfo, ref bool __result)
        {
            string fullPath;
            if (CharFile_Load_Patches._fullPathByInstance.TryGetValue(___charInfo.chaFile, out fullPath))
                __result = !File.Exists(fullPath);
        }
    }

    [HarmonyPatch(typeof(SmCharaLoad), "ExecuteDelete")]
    public class SmCharaLoad_ExecuteDelete_Patches
    {
        private static bool Prepare()
        {
            return OptimizeCharaMaker._optimizeCharaMaker;
        }

        public static void Prefix()
        {
            SmCharaLoad_Data.created = false;
        }
    }

    [HarmonyPatch(typeof(SmCharaLoad), "OnSortName")]
    public class SmCharaLoad_OnSortName_Patches
    {
        public static bool Prepare()
        {
            return OptimizeCharaMaker._optimizeCharaMaker;
        }

        public static void Postfix(SmCharaLoad __instance)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(__instance.rtfPanel);
        }
    }

    [HarmonyPatch(typeof(SmCharaLoad), "OnSortDate")]
    public class SmCharaLoad_OnSortDate_Patches
    {
        public static bool Prepare()
        {
            return OptimizeCharaMaker._optimizeCharaMaker;
        }

        public static void Postfix(SmCharaLoad __instance)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(__instance.rtfPanel);
        }
    }


    [HarmonyPatch(typeof(SmCharaLoad), "SortName", new []{typeof(bool)})]
    public class SmCharaLoad_SortName_Patches
    {
        public static bool Prepare()
        {
            return OptimizeCharaMaker._optimizeCharaMaker;
        }

        public static bool Prefix(SmCharaLoad __instance, bool ascend)
        {
            __instance.SetPrivate("ascendName", ascend);
            if (SmCharaLoad_Data.fileInfos.Count == 0)
                return false;
            __instance.SetPrivate("lastSort", (byte)0);
            CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("ja-JP");
            if (ascend)
            {
                SmCharaLoad_Data.fileInfos.Sort((a, b) => string.Compare(a.CharaName, b.CharaName, StringComparison.CurrentCultureIgnoreCase));
                SmCharaLoad_Data.folderInfos.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.CurrentCultureIgnoreCase));
            }
            else
            {
                SmCharaLoad_Data.fileInfos.Sort((a, b) => string.Compare(b.CharaName, a.CharaName, StringComparison.CurrentCultureIgnoreCase));
                SmCharaLoad_Data.folderInfos.Sort((a, b) => string.Compare(b.name, a.name, StringComparison.CurrentCultureIgnoreCase));
            }
            Thread.CurrentThread.CurrentCulture = currentCulture;
            SmCharaLoad_Data.parentFolder.transform.SetAsLastSibling();
            foreach (SmCharaLoad_Data.FolderInfo fi in SmCharaLoad_Data.folderInfos)
                fi.display.gameObject.transform.SetAsLastSibling();
            for (int i = 0; i < SmCharaLoad_Data.fileInfos.Count; i++)
            {
                SmCharaLoad.FileInfo fi = SmCharaLoad_Data.fileInfos[i];
                fi.no = i;
                SmCharaLoad_Data.characters[fi].transform.SetAsLastSibling();
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(SmCharaLoad), "SortDate", new[] { typeof(bool) })]
    public class SmCharaLoad_SortDate_Patches
    {
        public static bool Prepare()
        {
            return OptimizeCharaMaker._optimizeCharaMaker;
        }

        public static bool Prefix(SmCharaLoad __instance, bool ascend)
        {
            __instance.SetPrivate("ascendDate", ascend);
            if (SmCharaLoad_Data.fileInfos.Count == 0)
                return false;
            __instance.SetPrivate("lastSort", (byte)1);
            CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("ja-JP");
            if (ascend)
            {
                SmCharaLoad_Data.fileInfos.Sort((a, b) => a.time.CompareTo(b.time));
                SmCharaLoad_Data.folderInfos.Sort((a, b) => Directory.GetLastWriteTime(a.fullPath).CompareTo(Directory.GetLastWriteTime(b.fullPath)));
            }
            else
            {
                SmCharaLoad_Data.fileInfos.Sort((a, b) => b.time.CompareTo(a.time));
                SmCharaLoad_Data.folderInfos.Sort((a, b) => Directory.GetLastWriteTime(b.fullPath).CompareTo(Directory.GetLastWriteTime(a.fullPath)));
            }
            Thread.CurrentThread.CurrentCulture = currentCulture;
            SmCharaLoad_Data.parentFolder.transform.SetAsLastSibling();
            foreach (SmCharaLoad_Data.FolderInfo fi in SmCharaLoad_Data.folderInfos)
                fi.display.gameObject.transform.SetAsLastSibling();
            for (int i = 0; i < SmCharaLoad_Data.fileInfos.Count; i++)
            {
                SmCharaLoad.FileInfo fi = SmCharaLoad_Data.fileInfos[i];
                fi.no = i;
                SmCharaLoad_Data.characters[fi].transform.SetAsLastSibling();
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(SmCharaLoad), "UpdateSort")]
    internal static class SmCharaLoad_UpdateSort_Patches
    {
        private static bool Prepare()
        {
            return OptimizeCharaMaker._optimizeCharaMaker;
        }

        private static bool Prefix(SmCharaLoad __instance)
        {
            if ((byte)__instance.GetPrivate("lastSort") == 0)
                __instance.SortName((bool)__instance.GetPrivate("ascendName"));
            else
                __instance.SortDate((bool)__instance.GetPrivate("ascendDate"));
            return false;
        }
    }

    [HarmonyPatch(typeof(FusionCtrl), "Init")]
    internal static class FusionCtrl_Init_Patches
    {
        public static bool Prepare()
        {
            return OptimizeCharaMaker._optimizeCharaMaker;
        }

        private static bool Prefix(FusionCtrl __instance)
        {
            if (__instance.btnFusion)
            {
                __instance.btnFusion.interactable = false;
            }
            FolderAssist folderAssist = new FolderAssist();
            string folder;
            if (__instance.customControl.modeSex == 0)
                folder = UserData.Path + "chara/male/";
            else
                folder = UserData.Path + "chara/female/";
            folderAssist.CreateFolderInfoExRecursive(folder, new[]{"*.png"});
            for (int i = 0; i < 2; i++)
            {
                __instance.charList[i].lstFileInfo.Clear();
                int fileCount = folderAssist.GetFileCount();
                int num = 0;
                for (int j = 0; j < fileCount; j++)
                {
                    CharFile charFile;
                    if (__instance.customControl.modeSex == 0)
                    {
                        charFile = new CharMaleFile();
                    }
                    else
                    {
                        charFile = new CharFemaleFile();
                    }
                    if (charFile.LoadBlockData<CharFileInfoPreview>(charFile.previewInfo, folderAssist.lstFile[j].FullPath))
                    {
                        if (charFile.previewInfo.isConcierge == 0)
                        {
                            if (__instance.customControl.modeSex != 0 || "ill_Player" != folderAssist.lstFile[j].FileName)
                            {
                                FusionCtrl.FileInfo fileInfo = new FusionCtrl.FileInfo
                                {
                                    no = num,
                                    FullPath = folderAssist.lstFile[j].FullPath,
                                    FileName = folderAssist.lstFile[j].FileName,
                                    time = folderAssist.lstFile[j].time,
                                    CharaName = charFile.previewInfo.name,
                                    personality = charFile.previewInfo.personality
                                };
                                __instance.charList[i].lstFileInfo.Add(fileInfo);
                                num++;
                            }
                        }
                    }
                }
                __instance.CreateListObject();
                __instance.SortName(0, true);
                __instance.SortDate(0, false);
                __instance.SortName(1, true);
                __instance.SortDate(1, false);
                ToggleGroup component = __instance.charList[i].objListTop.GetComponent<ToggleGroup>();
                component.SetAllTogglesOff();
                if (__instance.charList[i].imgPrev)
                    __instance.charList[i].imgPrev.enabled = false;
            }
            return false;
        }

        private static bool CreateFolderInfoExRecursive(this FolderAssist self, string folder, string[] searchPattern, bool clear = true)
        {
            if (clear)
            {
                self.lstFile.Clear();
            }
            if (!Directory.Exists(folder))
            {
                return false;
            }
            List<string> list = new List<string>();
            foreach (string searchPattern2 in searchPattern)
            {
                list.AddRange(Directory.GetFiles(folder, searchPattern2, SearchOption.AllDirectories));
            }
            string[] array = list.ToArray();
            if (array.Length == 0)
            {
                return false;
            }
            foreach (string text in array)
            {
                global::FolderAssist.FileInfo fileInfo = new global::FolderAssist.FileInfo();
                fileInfo.FullPath = text;
                fileInfo.FileName = Path.GetFileNameWithoutExtension(text);
                fileInfo.time = File.GetLastWriteTime(text);
                self.lstFile.Add(fileInfo);
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(GameScene), "InitCharaList")]
    [HarmonyPatch(typeof(GameScene), "InitMaleList")]
    internal static class GameScene_InitLists_Patches
    {
        public static bool Prepare()
        {
            return OptimizeCharaMaker._optimizeCharaMaker;
        }

        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            for (int i = 0; i < instructionsList.Count; i++)
            {
                CodeInstruction inst = instructionsList[i];
                if (inst.ToString().Equals("call Void GetAllFiles(System.String, System.String, System.Collections.Generic.List`1[System.String])"))
                    yield return new CodeInstruction(OpCodes.Call, typeof(GameScene_InitLists_Patches).GetMethod(nameof(Injected), BindingFlags.NonPublic | BindingFlags.Static));
                else
                    yield return inst;
            }
        }

        private static void Injected(string path, string searchPattern, List<string> lst)
        {
            
            lst.AddRange(Directory.GetFiles(path, searchPattern, SearchOption.AllDirectories));
        }
    }

    [HarmonyPatch(typeof(GameScene), "OnRegisterRoom")]
    internal static class GameScene_OnRegisterRoom_Patches
    {
        public static bool Prepare()
        {
            return OptimizeCharaMaker._optimizeCharaMaker;
        }

        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            for (int i = 0; i < instructionsList.Count; i++)
            {
                CodeInstruction inst = instructionsList[i];
                if (inst.ToString().Equals("call System.String GetFileNameWithoutExtension(System.String)"))
                    yield return new CodeInstruction(OpCodes.Call, typeof(GameScene_OnRegisterRoom_Patches).GetMethod(nameof(Injected), BindingFlags.NonPublic | BindingFlags.Static));
                else
                    yield return inst;
            }
        }

        private static string Injected(string path)
        {
            return path;
        }
    }
}

#endif