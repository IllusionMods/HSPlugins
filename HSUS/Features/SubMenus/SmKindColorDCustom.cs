
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
using ToolBox;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace HSUS
{
    public static class SmKindColorD_Data
    {
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

        public static readonly Dictionary<int, TypeData> objects = new Dictionary<int, TypeData>();
        public static int previousType;
        public static InputField searchBar;
        public static RectTransform container;
        internal static readonly Dictionary<int, IEnumerator> _methods = new Dictionary<int, IEnumerator>();
        internal static PropertyInfo _translateProperty = null;
        private static SmKindColorD _originalComponent;
        private static ScrollRect _scrollView;

        public static void Init(SmKindColorD originalComponent)
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

            int[] keys = { 39, 40, 41, 42, 43, 9, 8 };
            if (OptimizeCharaMaker._asyncLoading)
            {
                CharInfo chaInfo = originalComponent.customControl.chainfo;
                foreach (int key in keys)
                {
                    IEnumerator method = SmKindColorD_SetCharaInfoSub_Patches.SetCharaInfoSub(originalComponent, key, chaInfo);
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
            if (objects.ContainsKey(previousType) == false)
                return;
            foreach (ObjectData objectData in objects[previousType].objects)
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
            if (objects.ContainsKey(previousType) == false)
                return;
            foreach (ObjectData objectData in objects[previousType].objects)
                objectData.obj.SetActive(true);
        }
    }

    [HarmonyPatch(typeof(SmKindColorD), "SetCharaInfoSub")]
    public class SmKindColorD_SetCharaInfoSub_Patches
    {
        public static bool Prepare()
        {
            return OptimizeCharaMaker._optimizeCharaMaker;
        }

        public static bool Prefix(SmKindColorD __instance, CharInfo ___chaInfo, CharFileInfoCustom ___customInfo, CharFileInfoCustomFemale ___customInfoF)
        {
            SmKindColorD_Data.ResetSearch();
            //SmKindColorD_Data.SearchChanged("");
            int nowSubMenuTypeId = (int)__instance.GetPrivate("nowSubMenuTypeId");
            if (null == ___chaInfo || null == __instance.objListTop || null == __instance.objLineBase || null == __instance.rtfPanel)
                return false;
            if (null != __instance.tglTab)
                __instance.tglTab.isOn = true;
            SmKindColorD_Data.TypeData td;
            if (SmKindColorD_Data.previousType != nowSubMenuTypeId && SmKindColorD_Data.objects.TryGetValue(SmKindColorD_Data.previousType, out td))
                td.parentObject.gameObject.SetActive(false);
            int count = 0;
            int selected = 0;

            IEnumerator method;
            if (SmKindColorD_Data._methods.TryGetValue(nowSubMenuTypeId, out method) == false || method == null || method.MoveNext() == false)
                method = SetCharaInfoSub(__instance, nowSubMenuTypeId, ___chaInfo);

            while (method.MoveNext()) ;

            int num = 0;
            if (___customInfo != null)
                switch (nowSubMenuTypeId)
                {
                    case 39:
                        num = ___customInfoF.texEyeshadowId;
                        break;
                    case 40:
                        num = ___customInfoF.texCheekId;
                        break;
                    case 41:
                        num = ___customInfoF.texLipId;
                        break;
                    case 42:
                        num = ___customInfo.texTattoo_fId;
                        break;
                    case 43:
                        num = ___customInfoF.texMoleId;
                        break;
                    case 9:
                        num = ___customInfo.texTattoo_bId;
                        break;
                    case 8:
                        num = ___customInfoF.texSunburnId;
                        break;
                }
            td = SmKindColorD_Data.objects[nowSubMenuTypeId];
            count = td.objects.Count;
            td.parentObject.gameObject.SetActive(true);
            for (int i = 0; i < td.objects.Count; i++)
            {
                SmKindColorD_Data.ObjectData o = td.objects[i];
                if (o.key == num)
                {
                    selected = i;
                    o.toggle.isOn = true;
                    o.toggle.onValueChanged.Invoke(true);
                }
                else if (o.toggle.isOn)
                    o.toggle.isOn = false;
            }
            float b = 24f * count - 232f;
            float y = Mathf.Min(24f * selected, b);
            __instance.rtfPanel.anchoredPosition = new Vector2(0f, y);

            __instance.OnClickColorDiffuse();
            SmKindColorD_Data._methods[nowSubMenuTypeId] = null;
            SmKindColorD_Data.previousType = nowSubMenuTypeId;
            return false;
        }

        internal static IEnumerator SetCharaInfoSub(SmKindColorD __instance, int nowSubMenuTypeId, CharInfo chaInfo)
        {
            if (SmKindColorD_Data.objects.ContainsKey(nowSubMenuTypeId))
                yield break;
            Dictionary<int, ListTypeTexture> dictionary = null;
            switch (nowSubMenuTypeId)
            {
                case 39:
                    dictionary = chaInfo.ListInfo.GetFemaleTextureList(CharaListInfo.TypeFemaleTexture.cf_t_eyeshadow, true);
                    break;
                case 40:
                    dictionary = chaInfo.ListInfo.GetFemaleTextureList(CharaListInfo.TypeFemaleTexture.cf_t_cheek, true);
                    break;
                case 41:
                    dictionary = chaInfo.ListInfo.GetFemaleTextureList(CharaListInfo.TypeFemaleTexture.cf_t_lip, true);
                    break;
                case 42:
                    dictionary = chaInfo.Sex == 0 ? chaInfo.ListInfo.GetMaleTextureList(CharaListInfo.TypeMaleTexture.cm_t_tattoo_f, true) : chaInfo.ListInfo.GetFemaleTextureList(CharaListInfo.TypeFemaleTexture.cf_t_tattoo_f, true);
                    break;
                case 43:
                    dictionary = chaInfo.ListInfo.GetFemaleTextureList(CharaListInfo.TypeFemaleTexture.cf_t_mole, true);
                    break;
                case 8:
                    dictionary = chaInfo.ListInfo.GetFemaleTextureList(CharaListInfo.TypeFemaleTexture.cf_t_sunburn, true);
                    break;
                case 9:
                    dictionary = chaInfo.Sex == 0 ? chaInfo.ListInfo.GetMaleTextureList(CharaListInfo.TypeMaleTexture.cm_t_tattoo_b, true) : chaInfo.ListInfo.GetFemaleTextureList(CharaListInfo.TypeFemaleTexture.cf_t_tattoo_b, true);
                    break;
            }

            SmKindColorD_Data.TypeData td = new SmKindColorD_Data.TypeData();
            td.parentObject = new GameObject("Type " + nowSubMenuTypeId, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(ToggleGroup)).GetComponent<RectTransform>();
            td.parentObject.SetParent(__instance.objListTop.transform, false);
            td.parentObject.localScale = Vector3.one;
            td.parentObject.localPosition = Vector3.zero;
            td.parentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            SmKindColorD_Data.objects.Add(nowSubMenuTypeId, td);
            ToggleGroup group = td.parentObject.GetComponent<ToggleGroup>();
            td.parentObject.gameObject.SetActive(false);

            int count = 0;
            foreach (KeyValuePair<int, ListTypeTexture> current in dictionary)
            {
                if (CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id) != 0)
                {
                    GameObject gameObject = GameObject.Instantiate(__instance.objLineBase);
                    gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                    TexTypeInfo texTypeInfo = gameObject.AddComponent<TexTypeInfo>();
                    texTypeInfo.id = current.Key;
                    texTypeInfo.typeName = current.Value.Name;
                    texTypeInfo.info = current.Value;
                    gameObject.transform.SetParent(td.parentObject, false);
                    RectTransform rectTransform = gameObject.transform as RectTransform;
                    rectTransform.localScale = new Vector3(1f, 1f, 1f);
                    rectTransform.sizeDelta = new Vector2(SmKindColorD_Data.container.rect.width, 24f);
                    Text component = rectTransform.FindChild("Label").GetComponent<Text>();
                    if (OptimizeCharaMaker._asyncLoading && SmKindColorD_Data._translateProperty != null) // Fuck you translation plugin
                    {
                        SmKindColorD_Data._translateProperty.SetValue(component, false, null);
                        string t = texTypeInfo.typeName;
                        HSUS._self._translationMethod(ref t);
                        component.text = t;
                    }
                    else
                        component.text = texTypeInfo.typeName;
                    __instance.CallPrivate("SetButtonClickHandler", gameObject);
                    Toggle component2 = gameObject.GetComponent<Toggle>();
                    td.keyToOriginalIndex.Add(current.Key, count);
                    td.objects.Add(new SmKindColorD_Data.ObjectData { key = current.Key, obj = gameObject, toggle = component2, text = component, creationDate = File.GetCreationTimeUtc("./abdata/" + texTypeInfo.info.ABPath) });
                    component2.onValueChanged.AddListener(v =>
                    {
                        if (component2.isOn)
                            UnityEngine.Debug.Log(texTypeInfo.info.Id + " " + texTypeInfo.info.ABPath);
                    });
                    component2.@group = @group;
                    gameObject.SetActive(true);
                    int num4 = CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id);
                    Transform transform = rectTransform.FindChild("imgNew");
                    if (transform && num4 == 1)
                    {
                        transform.gameObject.SetActive(true);
                    }
                    ++count;
                    yield return null;
                }
            }
        }
    }
}

#endif