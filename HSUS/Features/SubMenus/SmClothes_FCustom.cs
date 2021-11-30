
using ToolBox.Extensions;
using HSUS.Features;
#if HONEYSELECT
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CustomMenu;
using Harmony;
using IllusionUtility.GetUtility;
using ToolBox;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace HSUS
{
    public static class SmClothes_F_Data
    {
        public class TypeData
        {
            public RectTransform parentObject;
            public readonly List<SmClothes_F.IdTglInfo> lstIdTgl = new List<SmClothes_F.IdTglInfo>();
            public readonly Dictionary<int, int> keyToOriginalIndex = new Dictionary<int, int>();
            public List<ObjectData> objects = new List<ObjectData>();
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

        public static readonly Dictionary<int, TypeData> objects = new Dictionary<int, TypeData>();
        public static int previousType = -1;
        public static RectTransform container;
        public static InputField searchBar;
        internal static readonly Dictionary<int, IEnumerator> _methods = new Dictionary<int, IEnumerator>();
        internal static PropertyInfo _translateProperty = null;

        private static SmClothes_F _originalComponent;
        private static ScrollRect _scrollView;

        public static void Init(SmClothes_F originalComponent)
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

            searchBar = CharaMakerSearch.SpawnSearchBar(_originalComponent.transform.Find("TabControl/TabItem01"), SearchChanged);
            CharaMakerSort.SpawnSortButtons(_originalComponent.transform.Find("TabControl/TabItem01"), SortByName, SortByCreationDate, ResetSort);
            CharaMakerCycleButtons.SpawnCycleButtons(_originalComponent.transform.Find("TabControl/TabItem01"), CycleUp, CycleDown);
            _scrollView = _originalComponent.transform.Find("TabControl/TabItem01/ScrollView").GetComponent<ScrollRect>();


            _translateProperty = typeof(Text).GetProperty("Translate", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            int[] keys = {57, 58, 59, 60, 61, 62, 63, 64, 76, 77};
            if (OptimizeCharaMaker._asyncLoading)
            {
                CharInfo chaInfo = originalComponent.customControl.chainfo;
                foreach (int key in keys)
                {
                    IEnumerator method = SmClothes_F_SetCharaInfoSub_Patches.SetCharaInfoSub(originalComponent, key, chaInfo);
                    _methods.Add(key, method);
                    HSUS._self._asyncMethods.Add(method);
                }
            }
            else
            {
                foreach (int key in keys)
                    _methods.Add(key, null);
            }

            originalComponent.objLineBase.transform.Find("Label").GetComponent<Text>().raycastTarget = false;
            originalComponent.objLineBase.transform.Find("Background/Checkmark").GetComponent<Image>().raycastTarget = false;
            if (OptimizeCharaMaker._removeIsNew)
                UnityEngine.GameObject.Destroy(originalComponent.objLineBase.transform.Find("imgNew").gameObject);
        }

        private static void Reset()
        {
            objects.Clear();
            _methods.Clear();
        }

        public static void SearchChanged(string arg0)
        {
            string search = searchBar.text.Trim();
            TypeData data;
            if (objects.TryGetValue((int)_originalComponent.GetPrivate("nowSubMenuTypeId"), out data) == false)
                return;
            foreach (ObjectData objectData in data.objects)
            {
                bool active = objectData.obj.activeSelf;
                ToggleGroup group = objectData.toggle.group;
                objectData.obj.SetActive(objectData.text.text.IndexOf(search, StringComparison.OrdinalIgnoreCase) != -1);
                if (active && objectData.obj.activeSelf == false)
                    group.RegisterToggle(objectData.toggle);
            }
        }

        private static void ResetSort()
        {
            TypeData data;
            if (objects.TryGetValue((int)_originalComponent.GetPrivate("nowSubMenuTypeId"), out data) == false)
                return;
            CharaMakerSort.GenericIntSort(data.objects, objectData => data.keyToOriginalIndex[objectData.key], objectData => objectData.obj);
        }

        private static void SortByName()
        {
            TypeData data;
            if (objects.TryGetValue((int)_originalComponent.GetPrivate("nowSubMenuTypeId"), out data) == false)
                return;
            data.lastSortByNameReverse = !data.lastSortByNameReverse;
            CharaMakerSort.GenericStringSort(data.objects, objectData => objectData.text.text, objectData => objectData.obj, data.lastSortByNameReverse);
        }

        private static void SortByCreationDate()
        {
            TypeData data;
            if (objects.TryGetValue((int)_originalComponent.GetPrivate("nowSubMenuTypeId"), out data) == false)
                return;
            data.lastSortByCreationDateReverse = !data.lastSortByCreationDateReverse;
            CharaMakerSort.GenericDateSort(data.objects, objectData => objectData.creationDate, objectData => objectData.obj, data.lastSortByCreationDateReverse);
        }

        private static void CycleUp()
        {
            TypeData data;
            if (objects.TryGetValue((int)_originalComponent.GetPrivate("nowSubMenuTypeId"), out data) == false)
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

        private static void CycleDown()
        {
            TypeData data;
            if (objects.TryGetValue((int)_originalComponent.GetPrivate("nowSubMenuTypeId"), out data) == false)
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

        public static void ResetSearch()
        {
            InputField.OnChangeEvent searchEvent = searchBar.onValueChanged;
            searchBar.onValueChanged = null;
            searchBar.text = "";
            searchBar.onValueChanged = searchEvent;

            TypeData data;
            if (objects.TryGetValue((int)_originalComponent.GetPrivate("nowSubMenuTypeId"), out data) == false)
                return;
            foreach (ObjectData objectData in data.objects)
                objectData.obj.SetActive(true);
        }
    }

    [HarmonyPatch(typeof(SmClothes_F))]
    [HarmonyPatch("SetCharaInfoSub")]
    public class SmClothes_F_SetCharaInfoSub_Patches
    {
        public static bool Prepare()
        {
            return OptimizeCharaMaker._optimizeCharaMaker;
        }

        public static bool Prefix(SmClothes_F __instance, CharInfo ___chaInfo, CharFileInfoClothesFemale ___clothesInfoF)
        {
            SmClothes_F_Data.ResetSearch();
            int nowSubMenuTypeId = (int)__instance.GetPrivate("nowSubMenuTypeId");
            if (null == ___chaInfo || null == __instance.objListTop || null == __instance.objLineBase || null == __instance.rtfPanel)
                return false;
            if (__instance.tglTab != null)
                __instance.tglTab.isOn = true;

            IEnumerator method;
            if (SmClothes_F_Data._methods.TryGetValue(nowSubMenuTypeId, out method) == false || method == null || method.MoveNext() == false)
                method = SetCharaInfoSub(__instance, nowSubMenuTypeId, ___chaInfo);

            foreach (KeyValuePair<int, SmClothes_F_Data.TypeData> pair in SmClothes_F_Data.objects)
            {
                if (pair.Value.parentObject.gameObject.activeSelf)
                    pair.Value.parentObject.gameObject.SetActive(false);
            }

            while (method.MoveNext());

            SmClothes_F_Data.TypeData td = SmClothes_F_Data.objects[nowSubMenuTypeId];

            int num = 0;
            if (___clothesInfoF != null)
                switch (nowSubMenuTypeId)
                {
                    case 57:
                        num = ___clothesInfoF.clothesId[0];
                        break;
                    case 58:
                        num = ___clothesInfoF.clothesId[1];
                        break;
                    case 59:
                        num = ___clothesInfoF.clothesId[2];
                        break;
                    case 60:
                        num = ___clothesInfoF.clothesId[3];
                        break;
                    case 61:
                        num = ___clothesInfoF.clothesId[7];
                        break;
                    case 62:
                        num = ___clothesInfoF.clothesId[8];
                        break;
                    case 63:
                        num = ___clothesInfoF.clothesId[9];
                        break;
                    case 64:
                        num = ___clothesInfoF.clothesId[10];
                        break;
                    case 76:
                        num = ___clothesInfoF.clothesId[5];
                        break;
                    case 77:
                        num = ___clothesInfoF.clothesId[6];
                        break;
                }

            td.parentObject.gameObject.SetActive(true);
            int selected = 0;

            for (int i = 0; i < td.objects.Count; i++)
            {
                SmClothes_F_Data.ObjectData o = td.objects[i];
                if (o.key == num)
                {
                    selected = i;
                    o.toggle.isOn = true;
                    o.toggle.onValueChanged.Invoke(true);
                }
                else if (o.toggle.isOn)
                    o.toggle.isOn = false;
            }

            float b = 24f * td.objects.Count - 232f;
            float y = Mathf.Min(24f * selected, b);
            __instance.rtfPanel.anchoredPosition = new Vector2(0f, y);

            __instance.SetPrivate("nowChanging", true);
            if (___clothesInfoF != null)
            {
                float value = 1f;
                float value2 = 1f;
                float value3 = 1f;
                switch (nowSubMenuTypeId)
                {
                    case 57:
                        value = ___clothesInfoF.clothesColor[0].specularIntensity;
                        value2 = ___clothesInfoF.clothesColor[0].specularSharpness;
                        value3 = ___clothesInfoF.clothesColor2[0].specularSharpness;
                        SmClothes_F_UpdateTopListEnable_Patches.Prefix(__instance, ___chaInfo, ___clothesInfoF);
                        break;
                    case 58:
                        value = ___clothesInfoF.clothesColor[1].specularIntensity;
                        value2 = ___clothesInfoF.clothesColor[1].specularSharpness;
                        value3 = ___clothesInfoF.clothesColor2[1].specularSharpness;
                        SmClothes_F_UpdateBotListEnable_Patches.Prefix(__instance, ___chaInfo, ___clothesInfoF);
                        break;
                    case 59:
                        value = ___clothesInfoF.clothesColor[2].specularIntensity;
                        value2 = ___clothesInfoF.clothesColor[2].specularSharpness;
                        value3 = ___clothesInfoF.clothesColor2[2].specularSharpness;
                        break;
                    case 60:
                        value = ___clothesInfoF.clothesColor[3].specularIntensity;
                        value2 = ___clothesInfoF.clothesColor[3].specularSharpness;
                        value3 = ___clothesInfoF.clothesColor2[3].specularSharpness;
                        break;
                    case 61:
                        value = ___clothesInfoF.clothesColor[7].specularIntensity;
                        value2 = ___clothesInfoF.clothesColor[7].specularSharpness;
                        value3 = ___clothesInfoF.clothesColor2[7].specularSharpness;
                        break;
                    case 62:
                        value = ___clothesInfoF.clothesColor[8].specularIntensity;
                        value2 = ___clothesInfoF.clothesColor[8].specularSharpness;
                        value3 = ___clothesInfoF.clothesColor2[8].specularSharpness;
                        break;
                    case 63:
                        value = ___clothesInfoF.clothesColor[9].specularIntensity;
                        value2 = ___clothesInfoF.clothesColor[9].specularSharpness;
                        value3 = ___clothesInfoF.clothesColor2[9].specularSharpness;
                        break;
                    case 64:
                        value = ___clothesInfoF.clothesColor[10].specularIntensity;
                        value2 = ___clothesInfoF.clothesColor[10].specularSharpness;
                        value3 = ___clothesInfoF.clothesColor2[10].specularSharpness;
                        break;
                    case 76:
                        value = ___clothesInfoF.clothesColor[5].specularIntensity;
                        value2 = ___clothesInfoF.clothesColor[5].specularSharpness;
                        value3 = ___clothesInfoF.clothesColor2[5].specularSharpness;
                        break;
                    case 77:
                        value = ___clothesInfoF.clothesColor[6].specularIntensity;
                        value2 = ___clothesInfoF.clothesColor[6].specularSharpness;
                        value3 = ___clothesInfoF.clothesColor2[6].specularSharpness;
                        break;
                }

                if (__instance.sldIntensity)
                    __instance.sldIntensity.value = value;
                if (__instance.inputIntensity)
                    __instance.inputIntensity.text = (string)__instance.CallPrivate("ChangeTextFromFloat", value);
                if (__instance.sldSharpness[0])
                    __instance.sldSharpness[0].value = value2;
                if (__instance.inputSharpness[0])
                    __instance.inputSharpness[0].text = (string)__instance.CallPrivate("ChangeTextFromFloat", value2);
                if (__instance.sldSharpness[1])
                    __instance.sldSharpness[1].value = value3;
                if (__instance.inputSharpness[1])
                    __instance.inputSharpness[1].text = (string)__instance.CallPrivate("ChangeTextFromFloat", value3);
            }

            __instance.SetPrivate("nowChanging", false);
            __instance.OnClickColorSpecular(1);
            __instance.OnClickColorSpecular(0);
            __instance.OnClickColorDiffuse(1);
            __instance.OnClickColorDiffuse(0);

            SmClothes_F_Data._methods[nowSubMenuTypeId] = null;
            SmClothes_F_Data.previousType = nowSubMenuTypeId;

            return false;
        }

        internal static IEnumerator SetCharaInfoSub(SmClothes_F __instance, int nowSubMenuTypeId, CharInfo chaInfo)
        {
            if (SmClothes_F_Data.objects.ContainsKey(nowSubMenuTypeId) != false)
                yield break;
            int count = 0;
            Dictionary<int, ListTypeFbx> dictionary = null;
            switch (nowSubMenuTypeId)
            {
                case 57:
                    dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_top);
                    break;
                case 58:
                    dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_bot);
                    break;
                case 59:
                    dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_bra);
                    break;
                case 60:
                    dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_shorts);
                    break;
                case 61:
                    dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_gloves);
                    break;
                case 62:
                    dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_panst);
                    break;
                case 63:
                    dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_socks);
                    break;
                case 64:
                    dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_shoes);
                    break;
                case 76:
                    dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_swimtop);
                    break;
                case 77:
                    dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_swimbot);
                    break;
            }
            SmClothes_F_Data.TypeData td = new SmClothes_F_Data.TypeData();
            td.parentObject = new GameObject("Type " + nowSubMenuTypeId, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(ToggleGroup)).GetComponent<RectTransform>();
            td.parentObject.SetParent(__instance.objListTop.transform, false);
            td.parentObject.localScale = Vector3.one;
            td.parentObject.localPosition = Vector3.zero;
            td.parentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            SmClothes_F_Data.objects.Add(nowSubMenuTypeId, td);
            ToggleGroup group = td.parentObject.GetComponent<ToggleGroup>();
            td.parentObject.gameObject.SetActive(false);
            foreach (KeyValuePair<int, ListTypeFbx> current in dictionary)
            {
                bool flag = false;
                if (chaInfo.customInfo.isConcierge)
                {
                    flag = CharaListInfo.CheckSitriClothesID(current.Value.Category, current.Value.Id);
                }
                if (CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id) == 0 && !flag)
                    continue;
                GameObject gameObject = GameObject.Instantiate(__instance.objLineBase);
                gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                FbxTypeInfo fbxTypeInfo = gameObject.AddComponent<FbxTypeInfo>();
                fbxTypeInfo.id = current.Key;
                fbxTypeInfo.typeName = current.Value.Name;
                fbxTypeInfo.info = current.Value;
                gameObject.transform.SetParent(td.parentObject.transform, false);
                RectTransform rectTransform = gameObject.transform as RectTransform;
                rectTransform.localScale = new Vector3(1f, 1f, 1f);
                rectTransform.sizeDelta = new Vector2(SmClothes_F_Data.container.rect.width, 24f);
                Text component = rectTransform.FindChild("Label").GetComponent<Text>();
                if (OptimizeCharaMaker._asyncLoading && SmClothes_F_Data._translateProperty != null) // Fuck you translation plugin
                {
                    SmClothes_F_Data._translateProperty.SetValue(component, false, null);
                    string t = fbxTypeInfo.typeName;
                    HSUS._self._translationMethod(ref t);
                    component.text = t;
                }
                else
                    component.text = fbxTypeInfo.typeName;
                __instance.CallPrivate("SetButtonClickHandler", gameObject);
                Toggle component2 = gameObject.GetComponent<Toggle>();
                td.keyToOriginalIndex.Add(current.Key, count);
                td.objects.Add(new SmClothes_F_Data.ObjectData { key = current.Key, obj = gameObject, toggle = component2, text = component, creationDate = File.GetCreationTimeUtc("./abdata/" + fbxTypeInfo.info.ABPath)});
                component2.onValueChanged.AddListener(v =>
                {
                    if (component2.isOn)
                        UnityEngine.Debug.Log(fbxTypeInfo.info.Id + " " + fbxTypeInfo.info.ABPath);
                });
                component2.group = group;
                gameObject.SetActive(true);
                if (!flag)
                {
                    int num4 = CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id);
                    Transform transform = rectTransform.FindChild("imgNew");
                    if (transform && num4 == 1)
                        transform.gameObject.SetActive(true);
                }
                SmClothes_F.IdTglInfo idTglInfo = new SmClothes_F.IdTglInfo();
                idTglInfo.coorde = int.Parse(current.Value.Etc[1]);
                idTglInfo.tgl = component2;
                GameObject gameObject2 = gameObject.transform.FindLoop("Background");
                if (gameObject2)
                    idTglInfo.imgBG = gameObject2.GetComponent<Image>();
                gameObject2 = gameObject.transform.FindLoop("Checkmark");
                if (gameObject2)
                    idTglInfo.imgCheck = gameObject2.GetComponent<Image>();
                gameObject2 = gameObject.transform.FindLoop("Label");
                if (gameObject2)
                    idTglInfo.text = gameObject2.GetComponent<Text>();
                td.lstIdTgl.Add(idTglInfo);
                count++;
                yield return null;
            }
        }
    }

    [HarmonyPatch(typeof(SmClothes_F), "UpdateTopListEnable")]
    public class SmClothes_F_UpdateTopListEnable_Patches
    {
        public static bool Prepare()
        {
            return OptimizeCharaMaker._optimizeCharaMaker;
        }

        public static bool Prefix(SmClothes_F __instance, CharInfo ___chaInfo, CharFileInfoClothesFemale ___clothesInfoF)
        {
            if (null == ___chaInfo)
            {
                return false;
            }
            int nowSubMenuTypeId = (int)__instance.GetPrivate("nowSubMenuTypeId");
            List<SmClothes_F.IdTglInfo> lstIdTgl = SmClothes_F_Data.objects[nowSubMenuTypeId].lstIdTgl;
            int key = ___clothesInfoF.clothesId[1];
            Dictionary<int, ListTypeFbx> femaleFbxList = ___chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_bot);
            ListTypeFbx fbx;
            if (femaleFbxList.TryGetValue(key, out fbx) && fbx.Etc[1].Equals("1"))
            {
                foreach (SmClothes_F.IdTglInfo idTglInfo in lstIdTgl)
                {
                    if (idTglInfo.coorde != 1)
                    {
                        idTglInfo.tgl.interactable = true;
                        idTglInfo.imgBG.color = Color.white;
                        idTglInfo.imgCheck.color = Color.white;
                        idTglInfo.text.color = Color.white;
                    }
                    else
                    {
                        idTglInfo.tgl.interactable = false;
                        idTglInfo.imgBG.color = Color.gray;
                        idTglInfo.imgCheck.color = Color.gray;
                        idTglInfo.text.color = Color.gray;
                    }
                }
            }
            else
            {
                foreach (SmClothes_F.IdTglInfo idTglInfo in lstIdTgl)
                {
                    idTglInfo.tgl.interactable = true;
                    idTglInfo.imgBG.color = Color.white;
                    idTglInfo.imgCheck.color = Color.white;
                    idTglInfo.text.color = Color.white;
                }
            }
            return false;
        }
    }


    [HarmonyPatch(typeof(SmClothes_F), "UpdateBotListEnable")]
    public class SmClothes_F_UpdateBotListEnable_Patches
    {
        public static bool Prepare()
        {
            return OptimizeCharaMaker._optimizeCharaMaker;
        }

        public static bool Prefix(SmClothes_F __instance, CharInfo ___chaInfo, CharFileInfoClothesFemale ___clothesInfoF)
        {
            if (null == ___chaInfo)
            {
                return false;
            }
            int nowSubMenuTypeId = (int)__instance.GetPrivate("nowSubMenuTypeId");
            List<SmClothes_F.IdTglInfo> lstIdTgl = SmClothes_F_Data.objects[nowSubMenuTypeId].lstIdTgl;
            int key = ___clothesInfoF.clothesId[0];
            Dictionary<int, ListTypeFbx> femaleFbxList = ___chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_top);
            ListTypeFbx fbx;
            if (femaleFbxList.TryGetValue(key, out fbx) && fbx.Etc[1].Equals("1"))
            {
                foreach (SmClothes_F.IdTglInfo idTglInfo in lstIdTgl)
                {
                    if (idTglInfo.coorde != 1)
                    {
                        idTglInfo.tgl.interactable = true;
                        idTglInfo.imgBG.color = Color.white;
                        idTglInfo.imgCheck.color = Color.white;
                        idTglInfo.text.color = Color.white;
                    }
                    else
                    {
                        idTglInfo.tgl.interactable = false;
                        idTglInfo.imgBG.color = Color.gray;
                        idTglInfo.imgCheck.color = Color.gray;
                        idTglInfo.text.color = Color.gray;
                    }
                }
            }
            else
            {
                foreach (SmClothes_F.IdTglInfo idTglInfo in lstIdTgl)
                {
                    idTglInfo.tgl.interactable = true;
                    idTglInfo.imgBG.color = Color.white;
                    idTglInfo.imgCheck.color = Color.white;
                    idTglInfo.text.color = Color.white;
                }
            }
            return false;
        }
    }
}
#endif