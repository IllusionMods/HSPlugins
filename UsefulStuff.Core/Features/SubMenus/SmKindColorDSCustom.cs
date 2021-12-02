
using ToolBox.Extensions;
using HSUS.Features;
#if HONEYSELECT
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using CustomMenu;
using Harmony;
using ToolBox;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace HSUS
{
    public static class SmKindColorDS_Data
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
        public static RectTransform container;
        public static InputField searchBar;
        internal static readonly Dictionary<int, IEnumerator> _methods = new Dictionary<int, IEnumerator>();
        internal static PropertyInfo _translateProperty = null;

        private static SmKindColorDS _originalComponent;
        private static ScrollRect _scrollView;

        public static void Init(SmKindColorDS originalComponent)
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

            if (OptimizeCharaMaker._asyncLoading)
            {
                CharInfo chaInfo = originalComponent.customControl.chainfo;
                for (int i = 33; i < 45; i++)
                {
                    IEnumerator method = SmKindColorDS_SetCharaInfoSub_Patches.SetCharaInfoSub(originalComponent, i, chaInfo);
                    _methods.Add(i, method);
                    HSUS._self._asyncMethods.Add(method);
                }
            }
            else
            {
                for (int i = 33; i < 45; i++)
                    _methods.Add(i, null);
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

    [HarmonyPatch(typeof(SmKindColorDS), "SetCharaInfoSub")]
    public class SmKindColorDS_SetCharaInfoSub_Patches
    {
        public static bool Prepare()
        {
            return OptimizeCharaMaker._optimizeCharaMaker;
        }

        public static bool Prefix(SmKindColorDS __instance, CharInfo ___chaInfo, CharFileInfoCustom ___customInfo, CharFileInfoCustomFemale ___customInfoF, CharFileInfoCustomMale ___customInfoM)
        {
            SmKindColorDS_Data.ResetSearch();
            int nowSubMenuTypeId = (int)__instance.GetPrivate("nowSubMenuTypeId");
            if (null == ___chaInfo || null == __instance.objListTop || null == __instance.objLineBase || null == __instance.rtfPanel)
                return false;
            if (null != __instance.tglTab)
                __instance.tglTab.isOn = true;
            SmKindColorDS_Data.TypeData td;
            if (SmKindColorDS_Data.previousType != nowSubMenuTypeId && SmKindColorDS_Data.objects.TryGetValue(SmKindColorDS_Data.previousType, out td))
                td.parentObject.gameObject.SetActive(false);
            int count = 0;
            int selected = 0;

            IEnumerator method;
            if (SmKindColorDS_Data._methods.TryGetValue(nowSubMenuTypeId, out method) == false || method == null || method.MoveNext() == false)
                method = SetCharaInfoSub(__instance, nowSubMenuTypeId, ___chaInfo);

            while (method.MoveNext()) ;

            int num = 0;
            switch (nowSubMenuTypeId)
            {
                case 33:
                    if (___customInfo != null)
                        num = ___customInfo.matEyebrowId;
                    break;
                case 34:
                    if (___customInfoF != null)
                        num = ___customInfoF.matEyelashesId;
                    break;
                case 35:
                case 39:
                case 40:
                case 41:
                case 42:
                case 43:
                    if (nowSubMenuTypeId != 11)
                        break;
                    if (___customInfo != null)
                        num = ___customInfoF.matUnderhairId;
                    break;
                case 36:
                    if (___customInfo != null)
                        num = ___customInfo.matEyeLId;
                    break;
                case 37:
                    if (___customInfo != null)
                        num = ___customInfo.matEyeRId;
                    break;

                case 38:
                    if (___customInfoF != null)
                        num = ___customInfoF.matEyeHiId;
                    break;
                case 44:
                    if (___customInfo != null)
                        num = ___customInfoM.matBeardId;
                    break;
                default:
                    goto case 43;
            }
            td = SmKindColorDS_Data.objects[nowSubMenuTypeId];
            count = td.objects.Count;
            td.parentObject.gameObject.SetActive(true);
            for (int i = 0; i < td.objects.Count; i++)
            {
                SmKindColorDS_Data.ObjectData o = td.objects[i];
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

            __instance.SetPrivate("nowChanging", true);
            if (___customInfo != null)
            {
                float value = 1f;
                float value2 = 1f;
                switch (nowSubMenuTypeId)
                {
                    case 33:
                        value = ___customInfo.eyebrowColor.specularIntensity;
                        value2 = ___customInfo.eyebrowColor.specularSharpness;
                        break;
                    case 34:
                        value = ___customInfoF.eyelashesColor.specularIntensity;
                        value2 = ___customInfoF.eyelashesColor.specularSharpness;
                        break;
                    case 35:
                    case 39:
                    case 40:
                    case 41:
                    case 42:
                    case 43:
                        if (nowSubMenuTypeId != 11)
                            break;
                        value = ___customInfoF.underhairColor.specularIntensity;
                        value2 = ___customInfoF.underhairColor.specularSharpness;
                        break;
                    case 36:
                        value = ___customInfo.eyeLColor.specularIntensity;
                        value2 = ___customInfo.eyeLColor.specularSharpness;
                        break;
                    case 37:
                        value = ___customInfo.eyeRColor.specularIntensity;
                        value2 = ___customInfo.eyeRColor.specularSharpness;
                        break;
                    case 38:
                        value = ___customInfoF.eyeHiColor.specularIntensity;
                        value2 = ___customInfoF.eyeHiColor.specularSharpness;
                        break;
                    case 44:
                        value = ___customInfoM.beardColor.specularIntensity;
                        value2 = ___customInfoM.beardColor.specularSharpness;
                        break;
                    default:
                        goto case 43;
                }
                if (__instance.sldIntensity)
                    __instance.sldIntensity.value = value;
                if (__instance.inputIntensity)
                    __instance.inputIntensity.text = (string)__instance.CallPrivate("ChangeTextFromFloat", value);
                if (__instance.sldSharpness)
                    __instance.sldSharpness.value = value2;
                if (__instance.inputSharpness)
                    __instance.inputSharpness.text = (string)__instance.CallPrivate("ChangeTextFromFloat", value2);
            }
            __instance.SetPrivate("nowChanging", false);
            __instance.OnClickColorSpecular();
            __instance.OnClickColorDiffuse();
            SmKindColorDS_Data._methods[nowSubMenuTypeId] = null;
            SmKindColorDS_Data.previousType = nowSubMenuTypeId;
            return false;
        }

        internal static IEnumerator SetCharaInfoSub(SmKindColorDS __instance, int nowSubMenuTypeId, CharInfo chaInfo)
        {
            if (SmKindColorDS_Data.objects.ContainsKey(nowSubMenuTypeId))
                yield break;
            Dictionary<int, ListTypeMaterial> dictionary = null;
            switch (nowSubMenuTypeId)
            {
                case 33:
                    dictionary = chaInfo.Sex == 0 ? chaInfo.ListInfo.GetMaleMaterialList(CharaListInfo.TypeMaleMaterial.cm_m_eyebrow, true) : chaInfo.ListInfo.GetFemaleMaterialList(CharaListInfo.TypeFemaleMaterial.cf_m_eyebrow, true);
                    if (__instance.btnIntegrate)
                    {
                        __instance.btnIntegrate.gameObject.SetActive(true);
                        Text[] componentsInChildren = __instance.btnIntegrate.GetComponentsInChildren<Text>(true);
                        if (componentsInChildren.Length != 0)
                            componentsInChildren[0].text = chaInfo.Sex == 0 ? "髪の毛とヒゲも同じ色に合わせる" : "髪の毛とアンダーヘアも同じ色に合わせる";
                    }
                    if (__instance.btnReference01)
                    {
                        __instance.btnReference01.gameObject.SetActive(true);
                        Text[] componentsInChildren2 = __instance.btnReference01.GetComponentsInChildren<Text>(true);
                        if (componentsInChildren2.Length != 0)
                            componentsInChildren2[0].text = "髪の毛の色に合わせる";
                    }
                    if (__instance.btnReference02)
                    {
                        __instance.btnReference02.gameObject.SetActive(true);
                        Text[] componentsInChildren3 = __instance.btnReference02.GetComponentsInChildren<Text>(true);
                        if (componentsInChildren3.Length != 0)
                            componentsInChildren3[0].text = chaInfo.Sex == 0 ? "ヒゲの色に合わせる" : "アンダーヘアの色に合わせる";
                    }
                    break;

                case 34:
                    dictionary = chaInfo.ListInfo.GetFemaleMaterialList(CharaListInfo.TypeFemaleMaterial.cf_m_eyelashes, true);
                    if (__instance.btnIntegrate)
                        __instance.btnIntegrate.gameObject.SetActive(false);
                    if (__instance.btnReference01)
                        __instance.btnReference01.gameObject.SetActive(false);
                    if (__instance.btnReference02)
                        __instance.btnReference02.gameObject.SetActive(false);
                    break;

                case 35:
                case 39:
                case 40:
                case 41:
                case 42:
                case 43:
                    if (nowSubMenuTypeId != 11)
                        break;
                    dictionary = chaInfo.ListInfo.GetFemaleMaterialList(CharaListInfo.TypeFemaleMaterial.cf_m_underhair, true);
                    if (__instance.btnIntegrate)
                    {
                        __instance.btnIntegrate.gameObject.SetActive(true);
                        Text[] componentsInChildren4 = __instance.btnIntegrate.GetComponentsInChildren<Text>(true);
                        if (componentsInChildren4.Length != 0)
                            componentsInChildren4[0].text = "髪の毛と眉毛も同じ色に合わせる";
                    }
                    if (__instance.btnReference01)
                    {
                        __instance.btnReference01.gameObject.SetActive(true);
                        Text[] componentsInChildren5 = __instance.btnReference01.GetComponentsInChildren<Text>(true);
                        if (componentsInChildren5.Length != 0)
                            componentsInChildren5[0].text = "髪の毛の色に合わせる";
                    }
                    if (__instance.btnReference02)
                    {
                        __instance.btnReference02.gameObject.SetActive(true);
                        Text[] componentsInChildren6 = __instance.btnReference02.GetComponentsInChildren<Text>(true);
                        if (componentsInChildren6.Length != 0)
                            componentsInChildren6[0].text = "眉毛の色に合わせる";
                    }
                    break;

                case 36:
                    dictionary = chaInfo.Sex == 0 ? chaInfo.ListInfo.GetMaleMaterialList(CharaListInfo.TypeMaleMaterial.cm_m_eyeball, true) : chaInfo.ListInfo.GetFemaleMaterialList(CharaListInfo.TypeFemaleMaterial.cf_m_eyeball, true);
                    if (__instance.btnIntegrate)
                    {
                        __instance.btnIntegrate.gameObject.SetActive(true);
                        Text[] componentsInChildren7 = __instance.btnIntegrate.GetComponentsInChildren<Text>(true);
                        if (componentsInChildren7.Length != 0)
                            componentsInChildren7[0].text = "右目も同じ色に合わせる";
                    }
                    if (__instance.btnReference01)
                        __instance.btnReference01.gameObject.SetActive(false);
                    if (__instance.btnReference02)
                        __instance.btnReference02.gameObject.SetActive(false);
                    break;

                case 37:
                    dictionary = chaInfo.Sex == 0 ? chaInfo.ListInfo.GetMaleMaterialList(CharaListInfo.TypeMaleMaterial.cm_m_eyeball, true) : chaInfo.ListInfo.GetFemaleMaterialList(CharaListInfo.TypeFemaleMaterial.cf_m_eyeball, true);
                    if (__instance.btnIntegrate)
                    {
                        __instance.btnIntegrate.gameObject.SetActive(true);
                        Text[] componentsInChildren8 = __instance.btnIntegrate.GetComponentsInChildren<Text>(true);
                        if (componentsInChildren8.Length != 0)
                            componentsInChildren8[0].text = "左目も同じ色に合わせる";
                    }
                    if (__instance.btnReference01)
                        __instance.btnReference01.gameObject.SetActive(false);
                    if (__instance.btnReference02)
                        __instance.btnReference02.gameObject.SetActive(false);
                    break;

                case 38:
                    dictionary = chaInfo.ListInfo.GetFemaleMaterialList(CharaListInfo.TypeFemaleMaterial.cf_m_eyehi, true);
                    if (__instance.btnIntegrate)
                        __instance.btnIntegrate.gameObject.SetActive(false);
                    if (__instance.btnReference01)
                        __instance.btnReference01.gameObject.SetActive(false);
                    if (__instance.btnReference02)
                        __instance.btnReference02.gameObject.SetActive(false);
                    break;
                case 44:
                    dictionary = chaInfo.ListInfo.GetMaleMaterialList(CharaListInfo.TypeMaleMaterial.cm_m_beard, true);
                    if (__instance.btnIntegrate)
                    {
                        __instance.btnIntegrate.gameObject.SetActive(true);
                        Text[] componentsInChildren9 = __instance.btnIntegrate.GetComponentsInChildren<Text>(true);
                        if (componentsInChildren9.Length != 0)
                            componentsInChildren9[0].text = "髪の毛と眉毛も同じ色に合わせる";
                    }
                    if (__instance.btnReference01)
                    {
                        __instance.btnReference01.gameObject.SetActive(true);
                        Text[] componentsInChildren10 = __instance.btnReference01.GetComponentsInChildren<Text>(true);
                        if (componentsInChildren10.Length != 0)
                            componentsInChildren10[0].text = "髪の毛の色に合わせる";
                    }
                    if (__instance.btnReference02)
                    {
                        __instance.btnReference02.gameObject.SetActive(true);
                        Text[] componentsInChildren11 = __instance.btnReference02.GetComponentsInChildren<Text>(true);
                        if (componentsInChildren11.Length != 0)
                            componentsInChildren11[0].text = "眉毛の色に合わせる";
                    }
                    break;
                default:
                    goto case 43;
            }

            if (dictionary == null)
                yield break;
            SmKindColorDS_Data.TypeData td = new SmKindColorDS_Data.TypeData();
            td.parentObject = new GameObject("Type " + nowSubMenuTypeId, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(ToggleGroup)).GetComponent<RectTransform>();
            td.parentObject.SetParent(__instance.objListTop.transform, false);
            td.parentObject.localScale = Vector3.one;
            td.parentObject.localPosition = Vector3.zero;
            td.parentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            SmKindColorDS_Data.objects.Add(nowSubMenuTypeId, td);
            ToggleGroup group = td.parentObject.GetComponent<ToggleGroup>();
            td.parentObject.gameObject.SetActive(false);

            int count = 0;
            foreach (KeyValuePair<int, ListTypeMaterial> current in dictionary)
            {
                if (CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id) == 0)
                    continue;
                GameObject gameObject = GameObject.Instantiate(__instance.objLineBase);
                gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                MatTypeInfo matTypeInfo = gameObject.AddComponent<MatTypeInfo>();
                matTypeInfo.id = current.Key;
                matTypeInfo.typeName = current.Value.Name;
                matTypeInfo.info = current.Value;
                gameObject.transform.SetParent(td.parentObject, false);
                RectTransform rectTransform = gameObject.transform as RectTransform;
                rectTransform.localScale = Vector3.one;
                rectTransform.sizeDelta = new Vector2(SmKindColorDS_Data.container.rect.width, 24f);
                Text component = rectTransform.FindChild("Label").GetComponent<Text>();
                if (OptimizeCharaMaker._asyncLoading && SmKindColorDS_Data._translateProperty != null) // Fuck you translation plugin
                {
                    SmKindColorDS_Data._translateProperty.SetValue(component, false, null);
                    string t = matTypeInfo.typeName;
                    HSUS._self._translationMethod(ref t);
                    component.text = t;
                }
                else
                    component.text = matTypeInfo.typeName;
                __instance.CallPrivate("SetButtonClickHandler", gameObject);
                Toggle component2 = gameObject.GetComponent<Toggle>();
                td.keyToOriginalIndex.Add(current.Key, count);
                td.objects.Add(new SmKindColorDS_Data.ObjectData { key = current.Key, obj = gameObject, text = component, toggle = component2, creationDate = File.GetCreationTimeUtc("./abdata/" + matTypeInfo.info.ABPath) });
                component2.onValueChanged.AddListener(v =>
                {
                    if (component2.isOn)
                        UnityEngine.Debug.Log(matTypeInfo.info.Id + " " + matTypeInfo.info.ABPath);
                });
                component2.@group = @group;
                gameObject.SetActive(true);
                int num4 = CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id);
                Transform transform = rectTransform.FindChild("imgNew");
                if (transform && num4 == 1)
                    transform.gameObject.SetActive(true);
                ++count;
                yield return null;
            }
        }
    }
}

#endif