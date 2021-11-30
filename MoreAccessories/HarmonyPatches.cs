using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Xml;
using CustomMenu;
using Harmony;
using IllusionUtility.GetUtility;
using IllusionUtility.SetUtility;
using Studio;
using ToolBox.Extensions;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MoreAccessories
{
    [HarmonyPatch(typeof(CharFile))]
    [HarmonyPatch("ChangeCoordinateType")]
    [HarmonyPatch(new[] {typeof(CharDefine.CoordinateType) })]
    public class CharFile_ChangeCoordinateType_Patches
    {
        public static void Postfix(CharFile __instance, bool __result, CharDefine.CoordinateType type)
        {
            if (__result)
            {
                MoreAccessories.CharAdditionalData additionalData;
                if (MoreAccessories._self._accessoriesByChar.TryGetValue(__instance, out additionalData) == false)
                {
                    additionalData = new MoreAccessories.CharAdditionalData();
                    MoreAccessories._self._accessoriesByChar.Add(__instance, additionalData);
                }
                if (additionalData.rawAccessoriesInfos.TryGetValue(type, out additionalData.clothesInfoAccessory) == false)
                {
                    additionalData.clothesInfoAccessory = new List<CharFileInfoClothes.Accessory>();
                    additionalData.rawAccessoriesInfos[type] = additionalData.clothesInfoAccessory;
                }
                while (additionalData.infoAccessory.Count < additionalData.clothesInfoAccessory.Count)
                    additionalData.infoAccessory.Add(null);
                while (additionalData.objAccessory.Count < additionalData.clothesInfoAccessory.Count)
                    additionalData.objAccessory.Add(null);
                while (additionalData.objAcsMove.Count < additionalData.clothesInfoAccessory.Count)
                    additionalData.objAcsMove.Add(null);
                while (additionalData.showAccessory.Count < additionalData.clothesInfoAccessory.Count)
                    additionalData.showAccessory.Add(true);
                MoreAccessories._self.UpdateMakerGUI();
            }
        }

    }
    [HarmonyPatch(typeof(CharBody))]
    [HarmonyPatch("ChangeAccessory")]
    [HarmonyPatch(new[] { typeof(bool) })]
    public class CharBody_ChangeAccessory_Patches
    {
        public static void Postfix(CharBody __instance, bool forceChange)
        {
            MoreAccessories.CharAdditionalData additionalData;
            if (MoreAccessories._self._accessoriesByChar.TryGetValue(__instance.chaFile, out additionalData))
            {
                int i;
                for (i = 0; i < additionalData.clothesInfoAccessory.Count; i++)
                {
                    CharFileInfoClothes.Accessory accessory = additionalData.clothesInfoAccessory[i];
                    ChangeAccessoryAsync(__instance, additionalData, i, accessory.type, accessory.id, accessory.parentKey, forceChange);
                }
                for (; i < additionalData.objAccessory.Count; i++)
                    CleanRemainingAccessory(additionalData, i);
            }
        }

        public static void ChangeAccessoryAsync(CharBody self, MoreAccessories.CharAdditionalData data, int _slotNo, int _acsType, int _acsId, string parentKey, bool forceChange = false)
        {
            ListTypeFbx ltf = null;
            bool load = true;
            bool release = true;
            int typeNum = Enum.GetNames(typeof(CharaListInfo.TypeAccessoryFbx)).Length;
            if (_acsType == -1 || !MathfEx.RangeEqualOn(0, _acsType, typeNum - 1))
            {
                release = true;
                load = false;
            }
            else
            {
                if (_acsId == -1)
                {
                    release = false;
                    load = false;
                }
                if (!forceChange && null != data.objAccessory[_slotNo] && _acsType == data.clothesInfoAccessory[_slotNo].type && _acsId == data.clothesInfoAccessory[_slotNo].id)
                {
                    load = false;
                    release = false;
                }
                if (_acsId != -1)
                {
                    Dictionary<int, ListTypeFbx> work = self.chaInfo.ListInfo.GetAccessoryFbxList((CharaListInfo.TypeAccessoryFbx) _acsType);
                    if (work == null)
                    {
                        release = true;
                        load = false;
                    }
                    else if (!work.TryGetValue(_acsId, out ltf))
                    {
                        release = true;
                        load = false;
                    }
                }
            }
            if (release)
            {
                if (!load)
                {
                    data.clothesInfoAccessory[_slotNo].MemberInitialize();
                }
                if (data.objAccessory[_slotNo])
                {
                    Object.Destroy(data.objAccessory[_slotNo]);
                    data.objAccessory[_slotNo] = null;
                    data.infoAccessory[_slotNo] = null;
                    CharInfo_ReleaseTagObject(data, _slotNo);
                    data.objAcsMove[_slotNo] = null;
                }
            }
            if (load)
            {
                byte weight = 0;
                Transform trfParent = null;
                if (ltf.Parent == "0")
                {
                    weight = 2;
                    trfParent = self.objTop.transform;
                }
                data.objAccessory[_slotNo] = (GameObject) self.CallPrivate("LoadCharaFbxData", typeof(CharaListInfo.TypeAccessoryFbx), _acsType, _acsId, "ca_slot" + (_slotNo + 10).ToString("00"), false, weight, trfParent, 0, false);
                if (data.objAccessory[_slotNo])
                {
                    ListTypeFbxComponent ltfComponent = data.objAccessory[_slotNo].GetComponent<ListTypeFbxComponent>();
                    ltf = (data.infoAccessory[_slotNo] = ltfComponent.ltfData);
                    data.clothesInfoAccessory[_slotNo].type = _acsType;
                    data.clothesInfoAccessory[_slotNo].id = ltf.Id;
                    data.objAcsMove[_slotNo] = data.objAccessory[_slotNo].transform.FindLoop("N_move");
                    CharInfo_CreateTagInfo(data, _slotNo, data.objAccessory[_slotNo]);
                }
                else
                {
                    data.clothesInfoAccessory[_slotNo].type = -1;
                    data.clothesInfoAccessory[_slotNo].id = 0;
                }
            }
            if (data.objAccessory[_slotNo])
            {
                CharClothes_ChangeAccessoryColor(data, _slotNo);
                if (string.IsNullOrEmpty(parentKey))
                {
                    parentKey = ltf.Parent;
                }
                CharClothes_ChangeAccessoryParent(self, data, _slotNo, parentKey);
                CharClothes_UpdateAccessoryMoveFromInfo(data, _slotNo);
            }
        }

        public static void CleanRemainingAccessory(MoreAccessories.CharAdditionalData data, int _slotNo)
        {
            if (data.objAccessory[_slotNo])
            {
                Object.Destroy(data.objAccessory[_slotNo]);
                data.objAccessory[_slotNo] = null;
                data.infoAccessory[_slotNo] = null;
                CharInfo_ReleaseTagObject(data, _slotNo);
                data.objAcsMove[_slotNo] = null;
            }
        }

        private static void CharInfo_CreateTagInfo(MoreAccessories.CharAdditionalData data, int key, GameObject objTag)
        {
            if (objTag == null)
                return;
            FindAssist findAssist = new FindAssist();
            findAssist.Initialize(objTag.transform);
            CharInfo_AddListToTag(data, key, findAssist.GetObjectFromTag("ObjColor"));
        }

        private static void CharInfo_ReleaseTagObject(MoreAccessories.CharAdditionalData data, int key)
        {
            if (data.charInfoDictTagObj.ContainsKey(key))
                data.charInfoDictTagObj[key].Clear();
        }

        public static List<GameObject> CharInfo_GetTagInfo(MoreAccessories.CharAdditionalData data, int key)
        {
            List<GameObject> collection;
            if (data.charInfoDictTagObj.TryGetValue(key, out collection))
            {
                return new List<GameObject>(collection);
            }
            return new List<GameObject>();
        }


        private static void CharInfo_AddListToTag(MoreAccessories.CharAdditionalData data, int key, List<GameObject> add)
        {
            if (add == null)
                return;
            List<GameObject> gameObjectList;
            if (data.charInfoDictTagObj.TryGetValue(key, out gameObjectList))
                gameObjectList.AddRange(add);
            else
                data.charInfoDictTagObj[key] = add;
        }

        public static void CharClothes_ChangeAccessoryColor(MoreAccessories.CharAdditionalData data, int slotNo)
        {
            List<GameObject> tagInfo = CharInfo_GetTagInfo(data, slotNo);
            CharFileInfoClothes.Accessory accessory = data.clothesInfoAccessory[slotNo];
            ColorChange.SetHSColor(tagInfo, accessory.color, true, true, accessory.color2, true, true);
            ColorChange.SetHSColor(tagInfo, accessory.color, true, true, accessory.color2, true, true);
        }

        public static bool CharClothes_ChangeAccessoryParent(CharBody charBody, MoreAccessories.CharAdditionalData data, int slotNo, string parentStr)
        {
            GameObject gameObject = data.objAccessory[slotNo];
            if (null == gameObject)
            {
                return false;
            }
            ListTypeFbxComponent component = gameObject.GetComponent<ListTypeFbxComponent>();
            ListTypeFbx ltfData = component.ltfData;
            if ("0" == ltfData.Parent)
            {
                return false;
            }
            try
            {
                GameObject referenceInfo;
                Transform parentTransform = charBody.transform.Find(parentStr);
                if (parentTransform != null)
                    referenceInfo = parentTransform.gameObject;
                else
                {
                    CharReference.RefObjKey key = (CharReference.RefObjKey)(int)Enum.Parse(typeof(CharReference.RefObjKey), parentStr);
                    referenceInfo = charBody.chaInfo.GetReferenceInfo(key);                    
                }
                if (null == referenceInfo)
                {
                    return false;
                }
                gameObject.transform.SetParent(referenceInfo.transform, false);
                data.clothesInfoAccessory[slotNo].parentKey = parentStr;
            }
            catch (ArgumentException)
            {
                return false;
            }
            return true;
        }

        public static bool CharClothes_UpdateAccessoryMoveFromInfo(MoreAccessories.CharAdditionalData data, int slotNo)
        {
            GameObject gameObject = data.objAcsMove[slotNo];
            if (null == gameObject)
            {
                return false;
            }
            gameObject.transform.SetLocalPosition(data.clothesInfoAccessory[slotNo].addPos.x, data.clothesInfoAccessory[slotNo].addPos.y, data.clothesInfoAccessory[slotNo].addPos.z);
            gameObject.transform.SetLocalRotation(data.clothesInfoAccessory[slotNo].addRot.x, data.clothesInfoAccessory[slotNo].addRot.y, data.clothesInfoAccessory[slotNo].addRot.z);
            gameObject.transform.SetLocalScale(data.clothesInfoAccessory[slotNo].addScl.x, data.clothesInfoAccessory[slotNo].addScl.y, data.clothesInfoAccessory[slotNo].addScl.z);
            return true;
        }
    }

    [HarmonyPatch(typeof(SubMenuControl), "ChangeSubMenu", new []{typeof(string)})]
    public class SubMenuControl_ChangeSubMenu_Patches
    {
        public static bool Prefix(SubMenuControl __instance, string subMenuStr)
        {
            if (subMenuStr.StartsWith("SM_MoreAccessories_"))
            {
                int nowSubMenuTypeId = int.Parse(subMenuStr.Substring(19));
                if (nowSubMenuTypeId < MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory.Count)
                {
                    bool sameSubMenu = __instance.nowSubMenuTypeStr == subMenuStr;
                    __instance.nowSubMenuTypeStr = subMenuStr;
                    __instance.nowSubMenuTypeId = (int)SubMenuControl.SubMenuType.SM_Delete + 1 + nowSubMenuTypeId;
                    for (int i = 0; i < __instance.smItem.Length; i++)
                        if (__instance.smItem[i] != null && !(null == __instance.smItem[i].objTop))
                            __instance.smItem[i].objTop.SetActive(false);
                    if (MoreAccessories._self._smItem != null)
                    {
                        if (null != __instance.textTitle)
                            __instance.textTitle.text = MoreAccessories._self._smItem.menuName;
                        if (null != MoreAccessories._self._smItem.objTop)
                        {
                            MoreAccessories._self._smItem.objTop.SetActive(true);
                            __instance.SetPrivate("objActiveSubItem", MoreAccessories._self._smItem.objTop);
                            if (null != __instance.rtfBasePanel)
                            {
                                RectTransform rectTransform = MoreAccessories._self._smItem.objTop.transform as RectTransform;
                                Vector2 sizeDelta = rectTransform.sizeDelta;
                                __instance.SetPrivate("sizeBasePanelHeight", sizeDelta.y);
                            }
                        }
                    }
                    SubMenuBase component = ((GameObject)__instance.GetPrivate("objActiveSubItem")).GetComponent<SubMenuBase>();
                    if (null != component)
                    {
                        component.SetCharaInfo(__instance.nowSubMenuTypeId, sameSubMenu);
                    }
                    int cosStateFromSelect = __instance.GetCosStateFromSelect();
                    if (cosStateFromSelect != -1 && __instance.customCtrlPanel && __instance.customCtrlPanel.autoClothesState)
                    {
                        __instance.customCtrlPanel.ChangeCosStateSub(cosStateFromSelect);
                    }
                }
                else
                {
                    MoreAccessories._self.UIFallbackToCoordList();
                    
                }
                return false;
            }
            //if (__instance.nowSubMenuTypeStr.StartsWith("SM_MoreAccessories_"))
                MoreAccessories._self._smItem.objTop.SetActive(false);
            return true;
        }
    }

    [HarmonyPatch(typeof(CustomControl), "Init", new[] { typeof(CharInfo), typeof(string), typeof(int), typeof(bool) })]
    public class CustomControl_Init_Patches
    {
        public static void Prefix(CharInfo _chainfo, string _charaFile, int _modeCustom, bool _modeOverScene = false)
        {
            MoreAccessories._self.charaMakerCharInfo = _chainfo;
        }
    }

    [HarmonyPatch(typeof(CustomControl), "ChangeCustomControl", new[] { typeof(CharInfo), typeof(string) })]
    public class CustomControl_ChangeCustomControl_Patches
    {
        public static void Prefix(CharInfo _chainfo, string _charaFile)
        {
            MoreAccessories._self.charaMakerCharInfo = _chainfo;
        }
    }

    [HarmonyPatch(typeof(CharClothes), "SetAccessoryStateAll", new []{typeof(bool)})]
    public class CharClothes_SetAccessoryStateAll_Patches
    {
        public static void Prefix(CharClothes __instance, bool show)
        {
            CharFile chaFile = (CharFile)__instance.GetPrivate("chaFile");
            MoreAccessories.CharAdditionalData additionalData = MoreAccessories._self._accessoriesByChar[chaFile];
            for (int i = 0; i < additionalData.showAccessory.Count; i++)
                additionalData.showAccessory[i] = show;
        }
    }

    [HarmonyPatch(typeof(CharFemaleBody), "UpdateVisible")]
    public class CharFemaleBody_UpdateVisible_Patches
    {
        public static void Postfix(CharFemaleBody __instance)
        {
            MoreAccessories.CharAdditionalData additionalData = MoreAccessories._self._accessoriesByChar[__instance.chaFile];
            for (int i = 0; i < additionalData.objAccessory.Count; i++)
                additionalData.objAccessory[i]?.SetActive(__instance.chaInfo.visibleAll && additionalData.showAccessory[i]);
        }
    }

    [HarmonyPatch(typeof(CharMaleBody), "UpdateVisible")]
    public class CharMaleBody_UpdateVisible_Patches
    {
        public static void Postfix(CharMaleBody __instance)
        {
            MoreAccessories.CharAdditionalData additionalData = MoreAccessories._self._accessoriesByChar[__instance.chaFile];
            for (int i = 0; i < additionalData.objAccessory.Count; i++)
                additionalData.objAccessory[i]?.SetActive(__instance.chaInfo.visibleAll && additionalData.showAccessory[i]);
        }
    }

    [HarmonyPatch(typeof(Studio.Studio), "Duplicate")]
    public class Studio_Duplicate_Patches
    {
        private static SortedList<int, CharFile> _toDuplicate;

        public static void Prefix(Studio.Studio __instance)
        {
            _toDuplicate = new SortedList<int, CharFile>();
            TreeNodeObject[] selectNodes = __instance.treeNodeCtrl.selectNodes;
            for (int i = 0; i < selectNodes.Length; i++)
            {
                ObjectCtrlInfo objectCtrlInfo = null;
                if (__instance.dicInfo.TryGetValue(selectNodes[i], out objectCtrlInfo))
                {
                    objectCtrlInfo.OnSavePreprocessing();
                    OICharInfo charInfo = objectCtrlInfo.objectInfo as OICharInfo;
                    if (charInfo != null)
                    {
                        _toDuplicate.Add(objectCtrlInfo.objectInfo.dicKey, charInfo.charFile);
                    }
                }
            }
        }

        public static void Postfix(Studio.Studio __instance)
        {
            Dictionary<CharFile, CharInfo> charFileToInfo = new Dictionary<CharFile, CharInfo>();
            foreach (CharInfo charInfo in Resources.FindObjectsOfTypeAll<CharInfo>())
            {
                if (charFileToInfo.ContainsKey(charInfo.chaFile) == false)
                    charFileToInfo.Add(charInfo.chaFile, charInfo);
            }
            IEnumerator<KeyValuePair<int, CharFile>> enumerator = _toDuplicate.GetEnumerator();
            foreach (KeyValuePair<int, ObjectInfo> keyValuePair in new SortedDictionary<int, ObjectInfo>(__instance.sceneInfo.dicImport))
            {
                OICharInfo dest = keyValuePair.Value as OICharInfo;
                if (dest != null && enumerator.MoveNext())
                    MoreAccessories._self.DuplicateCharacter(charFileToInfo[enumerator.Current.Value], charFileToInfo[dest.charFile]);
            }
            enumerator.Dispose();
        }
    }

    [HarmonyPatch(typeof(HSceneClothChange), "InitCharaList")]
    public class HSceneClothChange_InitCharaList_Patches
    {
        public static bool _isInitializing = false;
        public static void Prefix()
        {
            _isInitializing = true;
        }

        public static void Postfix()
        {
            _isInitializing = false;
        }
    }

    [HarmonyPatch(typeof(SmClothesCopy), "UpdateCharaInfoSub")]
    public class SmClothesCopy_UpdateCharaInfoSub_Patches
    {
        public static void Postfix(SmClothesCopy __instance)
        {
            for (int k = 0; k < MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory.Count; k++)
            {
                CharFileInfoClothes.Accessory accessory = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[k];
                UI_OnMouseOverMessage mouseOver = MoreAccessories._self._displayedMakerSlots[k].copyOnMouseOver;
                if (accessory != null)
                {
                    if (accessory.type == -1)
                    {
                        mouseOver.active = true;
                        mouseOver.comment = "なし";
                    }
                    else
                    {
                        Dictionary<int, ListTypeFbx> accessoryFbxList = MoreAccessories._self.charaMakerCharInfo.ListInfo.GetAccessoryFbxList((CharaListInfo.TypeAccessoryFbx)accessory.type, true);
                        ListTypeFbx listTypeFbx2 = null;
                        accessoryFbxList.TryGetValue(accessory.id, out listTypeFbx2);
                        if (listTypeFbx2 != null)
                        {
                            mouseOver.active = true;
                            mouseOver.comment = CharDefine.AccessoryTypeName[accessory.type + 1] + "：" + listTypeFbx2.Name;
                        }
                        else
                            mouseOver.active = false;
                    }
                }
                else
                {
                    mouseOver.active = false;
                }
            }
        }
    }

    [HarmonyPatch(typeof(SmClothesCopy), "OnClickAllCheck")]
    public class SmClothesCopy_OnClickAllCheck_Patches
    {
        public static void Postfix()
        {
            foreach (MoreAccessories.MakerSlotData slot in MoreAccessories._self._displayedMakerSlots)
            {
                slot.copyToggle.isOn = true;
            }
        }
    }

    [HarmonyPatch(typeof(SmClothesCopy), "OnClickOnlyAccessoryCheck")]
    public class SmClothesCopy_OnClickOnlyAccessoryCheck_Patches
    {
        public static void Postfix()
        {
            foreach (MoreAccessories.MakerSlotData slot in MoreAccessories._self._displayedMakerSlots)
            {
                slot.copyToggle.isOn = true;
            }
        }
    }

    [HarmonyPatch(typeof(SmClothesCopy), "OnClickAllExclude")]
    public class SmClothesCopy_OnClickAllExclude_Patches
    {
        public static void Postfix()
        {
            foreach (MoreAccessories.MakerSlotData slot in MoreAccessories._self._displayedMakerSlots)
            {
                slot.copyToggle.isOn = false;
            }
        }
    }

    [HarmonyPatch(typeof(SmClothesCopy), "OnClickOnlyClothesCheck")]
    public class SmClothesCopy_OnClickOnlyClothesCheck_Patches
    {
        public static void Postfix()
        {
            foreach (MoreAccessories.MakerSlotData slot in MoreAccessories._self._displayedMakerSlots)
            {
                slot.copyToggle.isOn = false;
            }
        }
    }
    [HarmonyPatch(typeof(SmClothesCopy), "OnClickCopy", new []{typeof(int)})]
    public class SmClothesCopy_OnClickCopy_Patches
    {
        public static void Prefix(int id, CharDefine.CoordinateType[] ___copyType)
        {
            CharDefine.CoordinateType destinationCoords = ___copyType[id];
            List<CharFileInfoClothes.Accessory> destination;
            if (MoreAccessories._self._charaMakerAdditionalData.rawAccessoriesInfos.TryGetValue(destinationCoords, out destination) == false)
            {
                destination = new List<CharFileInfoClothes.Accessory>();
                MoreAccessories._self._charaMakerAdditionalData.rawAccessoriesInfos.Add(destinationCoords, destination);
            }
            for (int k = 0; k < MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory.Count; k++)
            {
                if (MoreAccessories._self._displayedMakerSlots[k].copyToggle.isOn)
                {
                    if (k >= destination.Count)
                        destination.Add(new CharFileInfoClothes.Accessory());
                    destination[k].Copy(MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[k]);
                }
            }
        }
    }


    [HarmonyPatch(typeof(SmClothesColorCtrl), "UpdateCharaInfoSub")]
    public class SmClothesColorCtrl_UpdateCharaInfoSub_Patches
    {
        public static void Postfix(SmClothesCopy __instance)
        {
            for (int k = 0; k < MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory.Count; k++)
            {
                CharFileInfoClothes.Accessory accessory = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[k];
                UI_OnMouseOverMessage mouseOver = MoreAccessories._self._displayedMakerSlots[k].bulkColorOnMouseOver;
                if (accessory != null)
                {
                    if (accessory.type == -1)
                    {
                        mouseOver.active = true;
                        mouseOver.comment = "なし";
                    }
                    else
                    {
                        Dictionary<int, ListTypeFbx> accessoryFbxList = MoreAccessories._self.charaMakerCharInfo.ListInfo.GetAccessoryFbxList((CharaListInfo.TypeAccessoryFbx)accessory.type, true);
                        ListTypeFbx listTypeFbx2 = null;
                        accessoryFbxList.TryGetValue(accessory.id, out listTypeFbx2);
                        if (listTypeFbx2 != null)
                        {
                            mouseOver.active = true;
                            mouseOver.comment = CharDefine.AccessoryTypeName[accessory.type + 1] + "：" + listTypeFbx2.Name;
                        }
                        else
                            mouseOver.active = false;
                    }
                }
                else
                {
                    mouseOver.active = false;
                }
            }
        }
    }


    [HarmonyPatch(typeof(SmClothesColorCtrl), "OnClickAllCheck")]
    public class SmClothesColorCtrl_OnClickAllCheck_Patches
    {
        public static void Postfix()
        {
            foreach (MoreAccessories.MakerSlotData slot in MoreAccessories._self._displayedMakerSlots)
            {
                slot.bulkColorToggle.isOn = true;
            }
        }
    }

    [HarmonyPatch(typeof(SmClothesColorCtrl), "OnClickOnlyAccessoryCheck")]
    public class SmClothesColorCtrl_OnClickOnlyAccessoryCheck_Patches
    {
        public static void Postfix()
        {
            foreach (MoreAccessories.MakerSlotData slot in MoreAccessories._self._displayedMakerSlots)
            {
                slot.bulkColorToggle.isOn = true;
            }
        }
    }

    [HarmonyPatch(typeof(SmClothesColorCtrl), "OnClickAllExclude")]
    public class SmClothesColorCtrl_OnClickAllExclude_Patches
    {
        public static void Postfix()
        {
            foreach (MoreAccessories.MakerSlotData slot in MoreAccessories._self._displayedMakerSlots)
            {
                slot.bulkColorToggle.isOn = false;
            }
        }
    }

    [HarmonyPatch(typeof(SmClothesColorCtrl), "OnClickOnlyClothesCheck")]
    public class SmClothesColorCtrl_OnClickOnlyClothesCheck_Patches
    {
        public static void Postfix()
        {
            foreach (MoreAccessories.MakerSlotData slot in MoreAccessories._self._displayedMakerSlots)
            {
                slot.bulkColorToggle.isOn = false;
            }
        }
    }

    [HarmonyPatch(typeof(SmClothesColorCtrl), "UpdateColorSetting")]
    public class SmClothesColorCtrl_UpdateColorSetting_Patches
    {
        public static void Postfix(SmClothesColorCtrl __instance, HSColorSet[] ___hsColor)
        {
            for (int k = 0; k < MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory.Count; k++)
            {
                Toggle toggle = MoreAccessories._self._displayedMakerSlots[k].bulkColorToggle;
                CharFileInfoClothes.Accessory accessory = MoreAccessories._self._charaMakerAdditionalData.clothesInfoAccessory[k];
                if (toggle.isOn && toggle.interactable)
                {
                    accessory.color.Copy(___hsColor[0]);
                    accessory.color2.Copy(___hsColor[1]);
                    accessory.color.Copy(___hsColor[0]);
                    accessory.color2.Copy(___hsColor[1]);
                    CharBody_ChangeAccessory_Patches.CharClothes_ChangeAccessoryColor(MoreAccessories._self._charaMakerAdditionalData, k);
                }
            }
        }
    }

    [HarmonyPatch(typeof(SmCharaLoad), "OnChangeBtn02")]
    internal static class SmCharaLoad_OnChangeBtn02_Patches
    {
        private static void Prefix(int ___nowSubMenuTypeId, SmCharaLoad.FileInfoComponent ___selFileInfoCmp, CharInfo ___chaInfo)
        {
            if (___nowSubMenuTypeId != 81 || null == ___selFileInfoCmp)
                return;
            XmlNode node = MoreAccessories.GetExtDataFromFile(___selFileInfoCmp.info.FullPath, "<charExtData>");
            if (node != null)
                MoreAccessories._self.OnCharaLoad(___chaInfo.chaFile, node);
        }
    }

    [HarmonyPatch(typeof(SmClothesLoad), "OnChangeBtn01")]
    internal static class SmClothesLoad_OnChangeBtn01_Patches
    {
        private static void Prefix(int ___nowSubMenuTypeId)
        {
            if (___nowSubMenuTypeId == 52)
                MoreAccessories._self._loadAdditionalAccessories = false;
        }

        private static void Postfix()
        {
            MoreAccessories._self._loadAdditionalAccessories = true;
        }
    }

    [HarmonyPatch(typeof(SmClothesLoad), "OnChangeBtn03")]
    internal static class SmClothesLoad_OnChangeBtn03_Patches
    {
        private static void Prefix(int ___nowSubMenuTypeId)
        {
            if (___nowSubMenuTypeId == 51)
                MoreAccessories._self._loadAdditionalAccessories = false;
        }

        private static void Postfix()
        {
            MoreAccessories._self._loadAdditionalAccessories = true;
        }
    }

    [HarmonyPatch]
    internal class HSStudioNEOAddon_StudioCharaListSortUtil_LoadAndChangeCloth_Patches
    {
        internal static bool Prepare()
        {
            return Type.GetType("HSStudioNEOAddon.StudioCharaListSortUtil,HSStudioNEOAddon") != null;
        }
        internal static MethodInfo TargetMethod()
        {
            return Type.GetType("HSStudioNEOAddon.StudioCharaListSortUtil,HSStudioNEOAddon").GetMethod("LoadAndChangeCloth", BindingFlags.Instance | BindingFlags.Public);
        }

        private static void Prefix(object __instance, bool clothOnly, bool accessoryOnly, MPCharCtrl ___mpCharCtrl, CharaFileSort ___charaFileSort, bool ___replaceCharaSameCharaAll)
        {
            if (accessoryOnly)
            {
                OCIChar ocichar = (OCIChar)___mpCharCtrl.GetPrivate("m_OCIChar");
                if (ocichar == null)
                    return;
                string selectPath = ___charaFileSort.selectPath;
                if (!File.Exists(selectPath))
                    return;

                XmlNode node = MoreAccessories.GetExtDataFromFile(selectPath, "<clothesExtData>");
                if (node != null)
                {
                    List<ObjectCtrlInfo> characters = (List<ObjectCtrlInfo>)__instance.CallPrivate("GetTargetChara", ocichar.charInfo.Sex == 0, ___replaceCharaSameCharaAll);
                    foreach (ObjectCtrlInfo info in characters)
                        MoreAccessories._self.OnCoordLoad(((OCIChar)info).charInfo.clothesInfo, node);
                }
            }
            else if (clothOnly)
            {
                MoreAccessories._self._loadAdditionalAccessories = false;
            }
        }

        private static void Postfix()
        {
            MoreAccessories._self._loadAdditionalAccessories = true;
        }
    }

    [HarmonyPatch]
    internal class HSStudioNEOAddon_StudioCharaListSortUtil_ReplaceClothesOnlys_Patches
    {
        internal static bool Prepare()
        {
            return Type.GetType("HSStudioNEOAddon.StudioCharaListSortUtil,HSStudioNEOAddon") != null;
        }
        internal static MethodInfo TargetMethod()
        {
            return AccessTools.Method(Type.GetType("HSStudioNEOAddon.StudioCharaListSortUtil,HSStudioNEOAddon"), "ReplaceClothesOnly");
        }

        private static void Prefix(object __instance, bool ___isMale, CharaFileSort ___charaFileSort, bool ___replaceCharaSameCharaAll)
        {
            string selectPath = ___charaFileSort.selectPath;
            if (!File.Exists(selectPath))
                return;
            Studio.Studio.Instance.ExecuteDelayed(() =>
            {
                XmlNode node = MoreAccessories.GetExtDataFromFile(selectPath, "<charExtData>");
                if (node != null)
                {
                    List<XmlNode> toRemove = new List<XmlNode>();
                    foreach (XmlNode childNode in node.ChildNodes)
                    {
                        if (XmlConvert.ToInt32(childNode.Attributes["type"].Value) != 0)
                            toRemove.Add(childNode);
                    }
                    foreach (XmlNode xmlNode in toRemove)
                        node.RemoveChild(xmlNode);
                    List<ObjectCtrlInfo> characters = (List<ObjectCtrlInfo>)__instance.CallPrivate("GetTargetChara", ___isMale, ___replaceCharaSameCharaAll);
                    foreach (ObjectCtrlInfo info in characters)
                    {
                        OCIChar ociChar = ((OCIChar)info);
                        MoreAccessories._self.OnCoordLoad(ociChar.charInfo.clothesInfo, node);
                        CharBody_ChangeAccessory_Patches.Postfix(ociChar.charBody, true);
                    }
                }
            }, 2);
        }
    }

    [HarmonyPatch]
    internal class HSStudioNEOAddon_StudioCharaListSortUtil_ReplaceClothesOnly3Sets_Patches
    {
        internal static bool Prepare()
        {
            return Type.GetType("HSStudioNEOAddon.StudioCharaListSortUtil,HSStudioNEOAddon") != null;
        }
        internal static MethodInfo TargetMethod()
        {
            return AccessTools.Method(Type.GetType("HSStudioNEOAddon.StudioCharaListSortUtil,HSStudioNEOAddon"), "ReplaceClothesOnly3Sets");
        }

        private static void Postfix(object __instance, bool ___isMale, CharaFileSort ___charaFileSort, bool ___replaceCharaSameCharaAll)
        {
            string selectPath = ___charaFileSort.selectPath;
            if (!File.Exists(selectPath))
                return;
            Studio.Studio.Instance.ExecuteDelayed(() =>
            {
                XmlNode node = MoreAccessories.GetExtDataFromFile(selectPath, "<charExtData>");
                if (node != null)
                {
                    List<ObjectCtrlInfo> characters = (List<ObjectCtrlInfo>)__instance.CallPrivate("GetTargetChara", ___isMale, ___replaceCharaSameCharaAll);
                    foreach (ObjectCtrlInfo info in characters)
                        MoreAccessories._self.OnCharaLoad(((OCIChar)info).charInfo.chaFile, node);
                }
            }, 2);
        }
    }

}
