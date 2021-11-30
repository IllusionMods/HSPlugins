using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ChaCustom;
using HarmonyLib;
#if EMOTIONCREATORS
using ADVPart.Manipulate.Chara;
using HEdit;
using HPlay;
#endif
using Illusion.Extensions;
using IllusionUtility.GetUtility;
using Manager;
using MessagePack;
using TMPro;
using ToolBox;
using ToolBox.Extensions;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace MoreAccessoriesKOI
{
    #region Patches
#if KOIKATSU
    [HarmonyPatch]
    internal static class VRHScene_Start_Patches
    {
        private static bool Prepare()
        {
            return Type.GetType("VRHScene,Assembly-CSharp.dll") != null;
        }

        private static MethodInfo TargetMethod()
        {
            return Type.GetType("VRHScene,Assembly-CSharp.dll").GetMethod("Start", AccessTools.all);
        }

        private static void Postfix(List<ChaControl> ___lstFemale, HSprite[] ___sprites)
        {
            MoreAccessories._self.SpawnHUI(___lstFemale, ___sprites[0]);
        }
    }

    [HarmonyPatch(typeof(HSceneProc), "Start")]
    internal static class HSceneProc_Start_Patches
    {
        private static void Postfix(List<ChaControl> ___lstFemale, HSprite ___sprite)
        {
            MoreAccessories._self.SpawnHUI(___lstFemale, ___sprite);
        }
    }

    [HarmonyPatch(typeof(HSprite), "Update")]
    public static class HSprite_Update_Patches
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            for (int i = 0; i < instructionsList.Count; i++)
            {
                CodeInstruction inst = instructionsList[i];
                yield return inst;
                if (inst.opcode == OpCodes.Stloc_S && inst.operand != null && inst.operand.ToString().Equals("System.Int32 (5)"))
                {
                    object num2Operand = inst.operand;
                    yield return new CodeInstruction(OpCodes.Ldloc_2); //i
                    yield return new CodeInstruction(OpCodes.Call, typeof(HSprite_Update_Patches).GetMethod(nameof(GetFixedAccessoryCount), BindingFlags.NonPublic | BindingFlags.Static));
                    yield return new CodeInstruction(OpCodes.Stloc_S, num2Operand);

                    while (instructionsList[i].opcode != OpCodes.Blt)
                        ++i;
                }
            }
        }


        private static int GetFixedAccessoryCount(int i)
        {
            ChaControl female = MoreAccessories._self._hSceneFemales[i];
            int res = 0;
            for (int k = 0; k < 20; k++)
                if (female.IsAccessory(k))
                    res++;
            MoreAccessories.CharAdditionalData data;
            if (MoreAccessories._self._accessoriesByChar.TryGetValue(female.chaFile, out data) == false)
                return res;
            foreach (ChaFileAccessory.PartsInfo part in data.nowAccessories)
            {
                if (part.type != 120)
                    res++;
            }
            return res;
        }
    }
#elif EMOTIONCREATORS
    [HarmonyPatch(typeof(HPlayHPartAccessoryCategoryUI), "Start")]
    internal static class HPlayHPartAccessoryCategoryUI_Start_Postfix
    {
        private static void Postfix(HPlayHPartAccessoryCategoryUI __instance)
        {
            MoreAccessories._self.SpawnPlayUI(__instance);
        }
    }

    [HarmonyPatch(typeof(HPlayHPartAccessoryCategoryUI), "Init")]
    internal static class HPlayHPartAccessoryCategoryUI_Init_Postfix
    {
        private static void Postfix()
        {
            MoreAccessories._self.ExecuteDelayed(MoreAccessories._self.UpdatePlayUI, 2);
        }
    }

    [HarmonyPatch(typeof(HPlayHPartClothMenuUI), "Init")]
    internal static class HPlayHPartClothMenuUI_Init_Postfix
    {
        private static void Postfix(HPlayHPartClothMenuUI __instance, Button[] ___btnClothMenus)
        {
            ___btnClothMenus[1].gameObject.SetActive(___btnClothMenus[1].gameObject.activeSelf || MoreAccessories._self._accessoriesByChar[__instance.selectChara.chaFile].objAccessory.Any(o => o != null));
        }
    }

    [HarmonyPatch(typeof(PartInfoClothSetUI), "Start")]
    internal static class PartInfoClothSetUI_Start_Patches
    {
        internal static WeakKeyDictionary<ChaControl, MoreAccessories.CharAdditionalData> _originalAdditionalData = new WeakKeyDictionary<ChaControl, MoreAccessories.CharAdditionalData>();
        private static void Postfix(PartInfoClothSetUI.CoordinateUIInfo[] ___coordinateUIs)
        {
            _originalAdditionalData.Purge();
            for (int i = 0; i < ___coordinateUIs.Length; i++)
            {
                PartInfoClothSetUI.CoordinateUIInfo ui = ___coordinateUIs[i];
                Button.ButtonClickedEvent originalOnClick = ui.btnEntry.onClick;
                ui.btnEntry.onClick = new Button.ButtonClickedEvent();
                int i1 = i;
                ui.btnEntry.onClick.AddListener(() =>
                {
                    ChaControl chaControl = Singleton<HEditData>.Instance.charas[i1];
                    if (_originalAdditionalData.ContainsKey(chaControl) == false && MoreAccessories._self._accessoriesByChar.TryGetValue(chaControl.chaFile, out MoreAccessories.CharAdditionalData originalData))
                    {
                        MoreAccessories.CharAdditionalData newData = new MoreAccessories.CharAdditionalData();
                        newData.LoadFrom(originalData);
                        _originalAdditionalData.Add(chaControl, newData);
                    }
                    originalOnClick.Invoke();
                });
            }
        }
    }

    [HarmonyPatch(typeof(PartInfoClothSetUI), "BackToCoordinate")]
    internal static class PartInfoClothSetUI_BackToCoordinate_Patches
    {
        private static void Prefix(int _charaID)
        {
            ChaControl chara = Singleton<HEditData>.Instance.charas[_charaID];
            MoreAccessories.CharAdditionalData originalData;
            if (PartInfoClothSetUI_Start_Patches._originalAdditionalData.TryGetValue(chara, out originalData) && 
                MoreAccessories._self._accessoriesByChar.TryGetValue(chara.chaFile, out MoreAccessories.CharAdditionalData data))
                data.LoadFrom(originalData);
        }
    }

    [HarmonyPatch(typeof(AccessoryUICtrl), "Init")]
    internal static class AccessoryUICtrl_Init_Patches
    {
        private static void Postfix(AccessoryUICtrl __instance)
        {
            MoreAccessories._self.SpawnADVUI(__instance);
        }
    }

    [HarmonyPatch(typeof(AccessoryUICtrl), "UpdateUI")]
    internal static class AccessoryUICtrl_UpdateUI_Patches
    {
        private static void Postfix()
        {
            MoreAccessories._self.UpdateADVUI();
        }
    }

#endif

    [HarmonyPatch(typeof(ChaFileControl), MethodType.Constructor)]
    internal static class ChaFileControl_Ctor_Patches
    {
        private static void Postfix(ChaFileControl __instance)
        {
#if KOIKATSU
            foreach (ChaFileCoordinate coordinate in __instance.coordinate)
                MoreAccessories._self._charByCoordinate[coordinate] = new WeakReference(__instance);
#elif EMOTIONCREATORS
            MoreAccessories._self._charByCoordinate[__instance.coordinate] = new WeakReference(__instance);
#endif
        }
    }

    [HarmonyPatch(typeof(ChaFile), "CopyCoordinate")]
    internal static class ChaFile_CopyCoordinate_Patches
    {
        private static void Postfix(ChaFile __instance,
#if KOIKATSU
                                    ChaFileCoordinate[] _coordinate
#elif EMOTIONCREATORS
                                    ChaFileCoordinate _coordinate
#endif
                )
        {
            ChaFileControl sourceFile;
            WeakReference r;
#if KOIKATSU
            if (MoreAccessories._self._charByCoordinate.TryGetValue(_coordinate[0], out r) == false || r.IsAlive == false)
#elif EMOTIONCREATORS
            if (MoreAccessories._self._charByCoordinate.TryGetValue(_coordinate, out r) == false || r.IsAlive == false)
#endif
                return;
            else
                sourceFile = (ChaFileControl)r.Target;
            if (__instance == sourceFile)
                return;

            MoreAccessories.CharAdditionalData sourceData;
            if (MoreAccessories._self._accessoriesByChar.TryGetValue(sourceFile, out sourceData) == false)
            {
                sourceData = new MoreAccessories.CharAdditionalData();
                MoreAccessories._self._accessoriesByChar.Add(sourceFile, sourceData);
            }
            MoreAccessories.CharAdditionalData destinationData;
            if (MoreAccessories._self._accessoriesByChar.TryGetValue(__instance, out destinationData) == false)
            {
                destinationData = new MoreAccessories.CharAdditionalData();
                MoreAccessories._self._accessoriesByChar.Add(__instance, destinationData);
            }
            destinationData.LoadFrom(sourceData);
        }
    }

    [HarmonyPatch(typeof(CustomAcsChangeSlot), "Start")]
    internal static class CustomAcsChangeSlot_Start_Patches
    {
        private static void Postfix(CustomAcsChangeSlot __instance)
        {
            MoreAccessories._self._customAcsChangeSlot = __instance;
            MoreAccessories._self._customAcsParentWin = (CustomAcsParentWindow)__instance.GetPrivate("customAcsParentWin");
            MoreAccessories._self._customAcsMoveWin = (CustomAcsMoveWindow[])__instance.GetPrivate("customAcsMoveWin");
            MoreAccessories._self._customAcsSelectKind = (CustomAcsSelectKind[])__instance.GetPrivate("customAcsSelectKind");
            MoreAccessories._self.SpawnMakerUI();
        }
    }

    [HarmonyPatch(typeof(CustomAcsChangeSlot), "UpdateSlotNames")]
    internal static class CustomAcsChangeSlot_UpdateSlotNames_Patches
    {
        private static void Postfix()
        {
            if (MoreAccessories._self._charaMakerData.nowAccessories == null || MoreAccessories._self._charaMakerData.nowAccessories.Count == 0)
                return;
            for (int i = 0; i < MoreAccessories._self._additionalCharaMakerSlots.Count; i++)
            {
                MoreAccessories.CharaMakerSlotData slot = MoreAccessories._self._additionalCharaMakerSlots[i];
                if (slot.toggle.isOn == false || MoreAccessories._self._charaMakerData.nowAccessories.Count >= i)
                    continue;
                if (MoreAccessories._self._charaMakerData.nowAccessories[i].type == 120)
                {
                    slot.text.text = $"スロット{i + 21:00}";
                }
                else if (MoreAccessories._self._charaMakerData.infoAccessory[i] != null)
                {
                    slot.text.text = MoreAccessories._self._charaMakerData.infoAccessory[i].Name;
                }
            }
        }
    }

    [HarmonyPatch(typeof(CustomAcsChangeSlot), "ChangeColorWindow", new[] { typeof(int) })]
    internal static class CustomAcsChangeSlot_ChangeColorWindow_Patches
    {
        private static CvsColor cvsColor;

        private static bool Prefix(CustomAcsChangeSlot __instance, int no)
        {
            if (cvsColor == null)
                cvsColor = (CvsColor)__instance.GetPrivate("cvsColor");
            if (null == cvsColor)
                return false;
            if (!cvsColor.isOpen)
                return false;
            if (no < 20)
            {
                CvsAccessory accessory = MoreAccessories._self.GetCvsAccessory(no);
                if (accessory)
                    accessory.SetDefaultColorWindow(no);
            }
            else
                cvsColor.Close();
            return false;
        }
    }

    [HarmonyPatch(typeof(CustomAcsChangeSlot), "LateUpdate")]
    internal static class CustomAcsChangeSlot_LateUpdate_Patches
    {
        // Token: 0x0600339E RID: 13214 RVA: 0x0010A450 File Offset: 0x00108650
        private static bool Prefix(CustomAcsChangeSlot __instance)
        {
            bool[] array = new bool[2];
            if (((CanvasGroup)__instance.GetPrivate("cgAccessoryTop")).alpha == 1f)
            {
                int selectIndex = MoreAccessories._self.GetSelectedMakerIndex();
                if (selectIndex != -1)
                {
                    CvsAccessory accessory = MoreAccessories._self.GetCvsAccessory(selectIndex);
                    if (accessory.isController01Active && Singleton<CustomBase>.Instance.customSettingSave.drawController[0])
                    {
                        array[0] = true;
                    }
                    if (accessory.isController02Active && Singleton<CustomBase>.Instance.customSettingSave.drawController[1])
                    {
                        array[1] = true;
                    }
                }
            }
            for (int i = 0; i < 2; i++)
            {
                Singleton<CustomBase>.Instance.customCtrl.cmpGuid[i].gameObject.SetActiveIfDifferent(array[i]);
            }
            return false;
        }
    }

#if KOIKATSU
    [HarmonyPatch(typeof(ChaControl), "ChangeCoordinateType", new[] { typeof(ChaFileDefine.CoordinateType), typeof(bool) })]
    internal static class ChaControl_ChangeCoordinateType_Patches
    {
        private static void Prefix(ChaControl __instance, ChaFileDefine.CoordinateType type, bool changeBackCoordinateType)
        {
            MoreAccessories.CharAdditionalData data;
            List<ChaFileAccessory.PartsInfo> accessories;
            if (MoreAccessories._self._accessoriesByChar.TryGetValue(__instance.chaFile, out data) == false)
            {
                data = new MoreAccessories.CharAdditionalData();
                MoreAccessories._self._accessoriesByChar.Add(__instance.chaFile, data);
            }
            if (data.rawAccessoriesInfos.TryGetValue((int)type, out accessories) == false)
            {
                accessories = new List<ChaFileAccessory.PartsInfo>();
                data.rawAccessoriesInfos.Add((int)type, accessories);
            }
            data.nowAccessories = accessories;
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
            MoreAccessories._self.ExecuteDelayed(MoreAccessories._self.OnCoordTypeChange);
        }
    }
#endif

    [HarmonyPatch(typeof(ChaControl), "UpdateVisible")]
    internal static class ChaControl_UpdateVisible_Patches
    {
        private static void Postfix(ChaControl __instance)
        {
            MoreAccessories.CharAdditionalData data;
            if (MoreAccessories._self._accessoriesByChar.TryGetValue(__instance.chaFile, out data) == false)
                return;

            bool flag2 = true;
            if (Singleton<Scene>.Instance.NowSceneNames.Any(s => s == "H"))
            {
                flag2 = (__instance.sex != 0 || Manager.Config.EtcData.VisibleBody);
            }

#if EMOTIONCREATORS
            bool[] array10 = new bool[]
            {
                true,
                __instance.objClothes[0] && __instance.objClothes[0].activeSelf,
                __instance.objClothes[1] && __instance.objClothes[1].activeSelf,
                __instance.objClothes[2] && __instance.objClothes[2].activeSelf,
                __instance.objClothes[3] && __instance.objClothes[3].activeSelf,
                __instance.objClothes[4] && __instance.objClothes[4].activeSelf,
                __instance.objClothes[5] && __instance.objClothes[5].activeSelf,
                __instance.objClothes[6] && __instance.objClothes[6].activeSelf,
                __instance.objClothes[7] && __instance.objClothes[7].activeSelf
            };
            bool[] array11 = new bool[]
            {
                true,
                false,
                false,
                false,
                false,
                true,
                true,
                true,
                true
            };
            array11[1] = (__instance.fileStatus.clothesState[0] != 1);
            array11[2] = (__instance.fileStatus.clothesState[1] != 1);
            array11[3] = (__instance.fileStatus.clothesState[2] != 1);
            array11[4] = (__instance.fileStatus.clothesState[3] != 1 && __instance.fileStatus.clothesState[3] != 2);
            bool[] array12 = array11;

            bool flag3 = false;
            if (Singleton<Scene>.Instance.NowSceneNames.Any(s => s == "HPlayScene") && Singleton<HPlayData>.IsInstance() && Singleton<HPlayData>.Instance.basePart != null && Singleton<HPlayData>.Instance.basePart.kind == 0)
            {
                flag3 = (__instance.sex == 0 && __instance.fileStatus.visibleSimple);
            }
#endif

            for (int i = 0; i < data.nowAccessories.Count; i++)
            {
                GameObject objAccessory = data.objAccessory[i];
#if KOIKATSU
                if (objAccessory == null)
                    continue;

                bool flag9 = false;
                if (!__instance.fileStatus.visibleHeadAlways && data.nowAccessories[i].partsOfHead)
                {
                    flag9 = true;
                }
                if (!__instance.fileStatus.visibleBodyAlways || !flag2)
                {
                    flag9 = true;
                }

                objAccessory.SetActive(__instance.visibleAll &&
                                       data.showAccessories[i] &&
                                       __instance.fileStatus.visibleSimple == false &&
                                       !flag9);
#elif EMOTIONCREATORS
                ChaFileAccessory.PartsInfo part = data.nowAccessories[i];

                bool flag10 = array10[part.hideCategory];
                if (flag10 && part.hideTiming == 0)
                {
                    flag10 = array12[part.hideCategory];
                }
                bool flag11 = false;
                if (!__instance.fileStatus.visibleHeadAlways && part.partsOfHead)
                {
                    flag11 = true;
                }
                if (!__instance.fileStatus.visibleBodyAlways || !flag2)
                {
                    flag11 = true;
                }
                if (YS_Assist.SetActiveControl(objAccessory, __instance.visibleAll, data.showAccessories[i], flag10, !flag3, !flag11))
                {
                    __instance.updateShape = true;
                }
#endif
            }
        }
    }

    [HarmonyPatch(typeof(ChaControl), "SetAccessoryStateAll")]
    internal static class ChaControl_SetAccessoryStateAll_Patches
    {
        private static void Postfix(ChaControl __instance, bool show)
        {
            MoreAccessories.CharAdditionalData data;
            if (MoreAccessories._self._accessoriesByChar.TryGetValue(__instance.chaFile, out data) == false)
                return;
            for (int i = 0; i < data.nowAccessories.Count; i++)
                data.showAccessories[i] = show;
        }
    }

#if KOIKATSU
    [HarmonyPatch(typeof(ChaControl), "SetAccessoryStateCategory")]
    internal static class ChaControl_SetAccessoryStateCategory_Patches
    {
        private static void Postfix(ChaControl __instance, int cateNo, bool show)
        {
            MoreAccessories.CharAdditionalData data;
            if (MoreAccessories._self._accessoriesByChar.TryGetValue(__instance.chaFile, out data) == false)
                return;
            for (int i = 0; i < data.nowAccessories.Count; i++)
                if (data.nowAccessories[i].hideCategory == cateNo)
                    data.showAccessories[i] = show;
        }
    }

    [HarmonyPatch(typeof(ChaControl), "GetAccessoryCategoryCount", new[] { typeof(int) })]
    internal static class ChaControl_GetAccessoryCategoryCount_Patches
    {
        private static void Postfix(ChaControl __instance, int cateNo, ref int __result)
        {
            if (__result == -1)
                return;
            MoreAccessories.CharAdditionalData data;
            if (MoreAccessories._self._accessoriesByChar.TryGetValue(__instance.chaFile, out data) == false)
                return;
            foreach (ChaFileAccessory.PartsInfo part in data.nowAccessories)
            {
                if (part.hideCategory == cateNo)
                    __result++;
            }
        }
    }
#endif

    [HarmonyPatch(typeof(ChaControl), "SetAccessoryState")]
    internal static class ChaControl_SetAccessoryState_Patches
    {
        private static void Postfix(ChaControl __instance, int slotNo, bool show)
        {
            if (slotNo < 20)
                return;
            MoreAccessories.CharAdditionalData data;
            if (MoreAccessories._self._accessoriesByChar.TryGetValue(__instance.chaFile, out data) == false)
                return;
            data.showAccessories[slotNo - 20] = show;
        }
    }

    [HarmonyPatch(typeof(ChaControl), "ChangeShakeAccessory", new[] { typeof(int) })]
    internal static class ChaControl_ChangeShakeAccessory_Patches
    {
        private static bool Prepare()
        {
            return (MoreAccessories._self._hasDarkness);

        }
        private static bool Prefix(ChaControl __instance, int slotNo)
        {
            if (slotNo < 20)
                return true;
            MoreAccessories.CharAdditionalData data;
            if (MoreAccessories._self._accessoriesByChar.TryGetValue(__instance.chaFile, out data) == false)
                return false;
            GameObject obj = data.objAccessory[slotNo - 20];
            if (obj != null)
            {
                DynamicBone[] componentsInChildren = obj.GetComponentsInChildren<DynamicBone>(true);
                bool noShake = data.nowAccessories[slotNo - 20].noShake;
                foreach (DynamicBone dynamicBone in componentsInChildren)
                {
                    dynamicBone.enabled = !noShake;
                }
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(ChaFile), "CopyAll")]
    internal static class ChaFile_CopyAll_Patches
    {
        private static void Postfix(ChaFile __instance, ChaFile _chafile)
        {
            MoreAccessories.CharAdditionalData sourceData;
            if (MoreAccessories._self._accessoriesByChar.TryGetValue(_chafile, out sourceData) == false)
                return;
            MoreAccessories.CharAdditionalData destinationData;
            if (MoreAccessories._self._accessoriesByChar.TryGetValue(__instance, out destinationData))
            {
                foreach (KeyValuePair<int, List<ChaFileAccessory.PartsInfo>> pair in destinationData.rawAccessoriesInfos)
                {
                    if (pair.Value != null)
                        pair.Value.Clear();
                }
            }
            else
            {
                destinationData = new MoreAccessories.CharAdditionalData();
                MoreAccessories._self._accessoriesByChar.Add(__instance, destinationData);
            }
            foreach (KeyValuePair<int, List<ChaFileAccessory.PartsInfo>> sourcePair in sourceData.rawAccessoriesInfos)
            {
                if (sourcePair.Value == null || sourcePair.Value.Count == 0)
                    continue;
                List<ChaFileAccessory.PartsInfo> destinationParts;
                if (destinationData.rawAccessoriesInfos.TryGetValue(sourcePair.Key, out destinationParts) == false)
                {
                    destinationParts = new List<ChaFileAccessory.PartsInfo>();
                    destinationData.rawAccessoriesInfos.Add(sourcePair.Key, destinationParts);
                }
                foreach (ChaFileAccessory.PartsInfo sourcePart in sourcePair.Value)
                {
                    {
                        byte[] bytes = MessagePackSerializer.Serialize(sourcePart);
                        destinationParts.Add(MessagePackSerializer.Deserialize<ChaFileAccessory.PartsInfo>(bytes));
                    }
                }
            }
            if (destinationData.rawAccessoriesInfos.TryGetValue(_chafile.status.GetCoordinateType(), out destinationData.nowAccessories) == false)
            {
                destinationData.nowAccessories = new List<ChaFileAccessory.PartsInfo>();
                destinationData.rawAccessoriesInfos.Add(_chafile.status.GetCoordinateType(), destinationData.nowAccessories);
            }
            while (destinationData.infoAccessory.Count < destinationData.nowAccessories.Count)
                destinationData.infoAccessory.Add(null);
            while (destinationData.objAccessory.Count < destinationData.nowAccessories.Count)
                destinationData.objAccessory.Add(null);
            while (destinationData.objAcsMove.Count < destinationData.nowAccessories.Count)
                destinationData.objAcsMove.Add(new GameObject[2]);
            while (destinationData.cusAcsCmp.Count < destinationData.nowAccessories.Count)
                destinationData.cusAcsCmp.Add(null);
            while (destinationData.showAccessories.Count < destinationData.nowAccessories.Count)
                destinationData.showAccessories.Add(true);
        }
    }

    [HarmonyPatch(typeof(CustomAcsParentWindow), "Awake")]
    internal static class CustomAcsParentWindow_Awake_Patches
    {
        private static void Postfix(CustomAcsParentWindow __instance)
        {
            MoreAccessories._self._cvsAccessory = (CvsAccessory[])__instance.GetPrivate("cvsAccessory");
        }
    }

    [HarmonyPatch(typeof(CustomAcsParentWindow), "Start")]
    internal static class CustomAcsParentWindow_Start_Patches
    {
        private static bool Prefix(CustomAcsParentWindow __instance)
        {
            ((CustomAcsParentWindow.TypeReactiveProperty)__instance.GetPrivate("_slotNo")).TakeUntilDestroy(__instance).Subscribe(delegate
            {
                __instance.UpdateWindow();
            });
            if ((Button)__instance.GetPrivate("btnClose"))
            {
                ((Button)__instance.GetPrivate("btnClose")).OnClickAsObservable().Subscribe(delegate
                {
                    if ((Toggle)__instance.GetPrivate("tglReference"))
                    {
                        ((Toggle)__instance.GetPrivate("tglReference")).isOn = false;
                    }
                });
            }
            ((Toggle[])__instance.GetPrivate("tglParent")).Select((p, idx) => new
            {
                toggle = p,
                index = (byte)idx
            }).ToList().ForEach(p =>
            {
                p.toggle.OnValueChangedAsObservable().Subscribe(delegate (bool isOn)
                {
                    if (!(bool)__instance.GetPrivate("updateWin") && isOn)
                    {
                        MoreAccessories._self.GetCvsAccessory((int)__instance.slotNo).UpdateSelectAccessoryParent(p.index);
                    }
                });
            });

            if (MoreAccessories._self._accessoriesByChar.TryGetValue(CustomBase.Instance.chaCtrl.chaFile, out MoreAccessories._self._charaMakerData) == false)
            {
                MoreAccessories._self._charaMakerData = new MoreAccessories.CharAdditionalData();
                MoreAccessories._self._accessoriesByChar.Add(CustomBase.Instance.chaCtrl.chaFile, MoreAccessories._self._charaMakerData);
            }
            if (MoreAccessories._self._charaMakerData.nowAccessories == null)
            {
                MoreAccessories._self._charaMakerData.nowAccessories = new List<ChaFileAccessory.PartsInfo>();
                MoreAccessories._self._charaMakerData.rawAccessoriesInfos.Add(CustomBase.Instance.chaCtrl.fileStatus.GetCoordinateType(), MoreAccessories._self._charaMakerData.nowAccessories);
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(CustomAcsParentWindow), "ChangeSlot", new[] { typeof(int), typeof(bool) })]
    internal static class CustomAcsParentWindow_ChangeSlot_Patches
    {
        private static bool Prefix(CustomAcsParentWindow __instance, int _no, bool open)
        {
            Toggle tglReference = (Toggle)__instance.GetPrivate("tglReference");
            __instance.slotNo = (CustomAcsParentWindow.AcsSlotNo)_no;
            bool isOn = tglReference.isOn;
            tglReference.isOn = false;
            tglReference = MoreAccessories._self.GetCvsAccessory(_no).tglAcsParent;
            __instance.SetPrivate("tglReference", tglReference);
            if (open && isOn)
                tglReference.isOn = true;

            return false;
        }
    }

    [HarmonyPatch(typeof(CustomAcsParentWindow), "UpdateCustomUI", new[] { typeof(int) })]
    internal static class CustomAcsParentWindow_UpdateCustomUI_Patches
    {
        private static bool Prefix(CustomAcsParentWindow __instance, int param, ref int __result)
        {
            __instance.SetPrivate("updateWin", true);
            int index = (int)__instance.slotNo;
            __result = __instance.SelectParent(MoreAccessories._self.GetPart(index).parentKey);
            __instance.SetPrivate("updateWin", false);
            return false;
        }
    }

    [HarmonyPatch(typeof(CustomAcsParentWindow), "UpdateWindow")]
    internal static class CustomAcsParentWindow_UpdateWindow_Patches
    {
        private static TextMeshProUGUI textTitle;

        private static bool Prefix(CustomAcsParentWindow __instance)
        {
            if (textTitle == null)
                textTitle = (TextMeshProUGUI)__instance.GetPrivate("textTitle");
            __instance.SetPrivate("updateWin", true);
            if (textTitle)
            {
                textTitle.text = $"スロット{(int)__instance.slotNo + 1:00}の親を選択";
            }
            int index = (int)__instance.slotNo;
            __instance.SelectParent(MoreAccessories._self.GetPart(index).parentKey);
            __instance.SetPrivate("updateWin", false);
            return false;
        }
    }

    [HarmonyPatch(typeof(CustomAcsMoveWindow), "Start")]
    internal static class CustomAcsMoveWindow_Start_Patches
    {
        private static bool Prefix(CustomAcsMoveWindow __instance)
        {
            for (int i = 0; i < 3; i++)
            {
                Singleton<CustomBase>.Instance.lstTmpInputField.Add(((TMP_InputField[])__instance.GetPrivate("inpPos"))[i]);
                Singleton<CustomBase>.Instance.lstTmpInputField.Add(((TMP_InputField[])__instance.GetPrivate("inpRot"))[i]);
                Singleton<CustomBase>.Instance.lstTmpInputField.Add(((TMP_InputField[])__instance.GetPrivate("inpScl"))[i]);
            }
            ((CustomAcsMoveWindow.TypeReactiveProperty)__instance.GetPrivate("_slotNo")).TakeUntilDestroy(__instance).Subscribe(delegate
            {
                __instance.UpdateWindow();
            });
            Button btnClose = (Button)__instance.GetPrivate("btnClose");
            if (btnClose)
            {
                btnClose.OnClickAsObservable().Subscribe(delegate
                {
                    Toggle tglReference = (Toggle)__instance.GetPrivate("tglReference");
                    if (tglReference)
                    {
                        tglReference.isOn = false;
                    }
                });
            }
            ((Toggle[])__instance.GetPrivate("tglPosRate")).Select((p, idx) => new
            {
                toggle = p,
                index = (byte)idx
            }).ToList().ForEach(p =>
            {
                (from isOn in p.toggle.OnValueChangedAsObservable()
                 where isOn
                 select isOn).Subscribe(delegate
             {
                 Singleton<CustomBase>.Instance.customSettingSave.acsCorrectPosRate[__instance.correctNo] = p.index;
             });
            });
            ((Toggle[])__instance.GetPrivate("tglRotRate")).Select((p, idx) => new
            {
                toggle = p,
                index = (byte)idx
            }).ToList().ForEach(p =>
            {
                (from isOn in p.toggle.OnValueChangedAsObservable()
                 where isOn
                 select isOn).Subscribe(delegate
             {
                 Singleton<CustomBase>.Instance.customSettingSave.acsCorrectRotRate[__instance.correctNo] = p.index;
             });
            });
            ((Toggle[])__instance.GetPrivate("tglSclRate")).Select((p, idx) => new
            {
                toggle = p,
                index = (byte)idx
            }).ToList().ForEach(p =>
            {
                (from isOn in p.toggle.OnValueChangedAsObservable()
                 where isOn
                 select isOn).Subscribe(delegate
             {
                 Singleton<CustomBase>.Instance.customSettingSave.acsCorrectSclRate[__instance.correctNo] = p.index;
             });
            });
            float downTimeCnt = 0f;
            float loopTimeCnt = 0f;
            bool change = false;
            ((Button[])__instance.GetPrivate("btnPos")).Select((p, idx) => new
            {
                btn = p,
                index = idx
            }).ToList().ForEach(p =>
            {
                p.btn.OnClickAsObservable().Subscribe(delegate
                {
                    if (!change)
                    {
                        int num = p.index / 2;
                        int num2 = (p.index % 2 != 0) ? 1 : -1;
                        if (num == 0)
                        {
                            num2 *= -1;
                        }
                        float val = num2 * ((float[])__instance.GetPrivate("movePosValue"))[Singleton<CustomBase>.Instance.customSettingSave.acsCorrectPosRate[__instance.correctNo]];
                        MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).FuncUpdateAcsPosAdd(__instance.correctNo, num, true, val);
                        if (MoreAccessories._self._hasDarkness == false)
                            MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).UpdateAcsMoveHistory();
                        ((TMP_InputField[])__instance.GetPrivate("inpPos"))[num].text = MoreAccessories._self.GetPart(__instance.nSlotNo).addMove[__instance.correctNo, 0][num].ToString();
                        MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).SetControllerTransform(__instance.correctNo);
                    }
                });
                p.btn.UpdateAsObservable().SkipUntil(p.btn.OnPointerDownAsObservable().Do(delegate
                {
                    downTimeCnt = 0f;
                    loopTimeCnt = 0f;
                    change = false;
                })).TakeUntil(p.btn.OnPointerUpAsObservable().Do(delegate
                              {
                                  if (MoreAccessories._self._hasDarkness == false)
                                      if (change)
                                      {
                                          MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).UpdateAcsMoveHistory();
                                      }
                              })
                ).RepeatUntilDestroy(__instance).Subscribe(delegate
                {
                    int num = p.index / 2;
                    int num2 = (p.index % 2 != 0) ? 1 : -1;
                    if (num == 0)
                    {
                        num2 *= -1;
                    }
                    float num3 = num2 * ((float[])__instance.GetPrivate("movePosValue"))[Singleton<CustomBase>.Instance.customSettingSave.acsCorrectPosRate[__instance.correctNo]];
                    float num4 = 0f;
                    downTimeCnt += Time.deltaTime;
                    if (downTimeCnt > 0.3f)
                    {
                        for (loopTimeCnt += Time.deltaTime; loopTimeCnt > 0.05f; loopTimeCnt -= 0.05f)
                        {
                            num4 += num3;
                        }
                        if (num4 != 0f)
                        {
                            MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).FuncUpdateAcsPosAdd(__instance.correctNo, num, true, num4);
                            ((TMP_InputField[])__instance.GetPrivate("inpPos"))[num].text = MoreAccessories._self.GetPart(__instance.nSlotNo).addMove[__instance.correctNo, 0][num].ToString();
                            change = true;
                            MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).SetControllerTransform(__instance.correctNo);
                        }
                    }
                }).AddTo(__instance);
            });
            ((TMP_InputField[])__instance.GetPrivate("inpPos")).Select((p, idx) => new
            {
                inp = p,
                index = idx
            }).ToList().ForEach(p =>
            {
                p.inp.onEndEdit.AsObservable().Subscribe(delegate (string value)
                {
                    int xyz = p.index % 3;
                    float val = CustomBase.ConvertValueFromTextLimit(-100f, 100f, 1, value);
                    p.inp.text = val.ToString();
                    MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).FuncUpdateAcsPosAdd(__instance.correctNo, xyz, false, val);
                    if (MoreAccessories._self._hasDarkness == false)
                        MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).UpdateAcsMoveHistory();
                    MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).SetControllerTransform(__instance.correctNo);
                });
            });
            ((Button[])__instance.GetPrivate("btnPosReset")).Select((p, idx) => new
            {
                btn = p,
                index = idx
            }).ToList().ForEach(p =>
            {
                p.btn.OnClickAsObservable().Subscribe(delegate
                {
                    ((TMP_InputField[])__instance.GetPrivate("inpPos"))[p.index].text = "0";
                    MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).FuncUpdateAcsPosAdd(__instance.correctNo, p.index, false, 0f);
                    if (MoreAccessories._self._hasDarkness == false)
                        MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).UpdateAcsMoveHistory();
                    MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).SetControllerTransform(__instance.correctNo);
                });
            });
            ((Button[])__instance.GetPrivate("btnRot")).Select((p, idx) => new
            {
                btn = p,
                index = idx
            }).ToList().ForEach(p =>
            {
                p.btn.OnClickAsObservable().Subscribe(delegate
                {
                    if (!change)
                    {
                        int num = p.index / 2;
                        int num2 = (p.index % 2 != 0) ? 1 : -1;
                        float val = num2 * ((float[])__instance.GetPrivate("moveRotValue"))[Singleton<CustomBase>.Instance.customSettingSave.acsCorrectRotRate[__instance.correctNo]];
                        MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).FuncUpdateAcsRotAdd(__instance.correctNo, num, true, val);
                        if (MoreAccessories._self._hasDarkness == false)
                            MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).UpdateAcsMoveHistory();
                        ((TMP_InputField[])__instance.GetPrivate("inpRot"))[num].text = MoreAccessories._self.GetPart(__instance.nSlotNo).addMove[__instance.correctNo, 1][num].ToString();
                        MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).SetControllerTransform(__instance.correctNo);
                    }
                });
                p.btn.UpdateAsObservable().SkipUntil(p.btn.OnPointerDownAsObservable().Do(delegate
                {
                    downTimeCnt = 0f;
                    loopTimeCnt = 0f;
                    change = false;
                })).TakeUntil(p.btn.OnPointerUpAsObservable().Do(delegate
                              {
                                  if (MoreAccessories._self._hasDarkness == false)
                                      if (change)
                                      {
                                          MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).UpdateAcsMoveHistory();
                                      }
                              })
                ).RepeatUntilDestroy(__instance).Subscribe(delegate
                {
                    int num = p.index / 2;
                    int num2 = (p.index % 2 != 0) ? 1 : -1;
                    float num3 = num2 * ((float[])__instance.GetPrivate("moveRotValue"))[Singleton<CustomBase>.Instance.customSettingSave.acsCorrectRotRate[__instance.correctNo]];
                    float num4 = 0f;
                    downTimeCnt += Time.deltaTime;
                    if (downTimeCnt > 0.3f)
                    {
                        for (loopTimeCnt += Time.deltaTime; loopTimeCnt > 0.05f; loopTimeCnt -= 0.05f)
                        {
                            num4 += num3;
                        }
                        if (num4 != 0f)
                        {
                            MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).FuncUpdateAcsRotAdd(__instance.correctNo, num, true, num4);
                            ((TMP_InputField[])__instance.GetPrivate("inpRot"))[num].text = MoreAccessories._self.GetPart(__instance.nSlotNo).addMove[__instance.correctNo, 1][num].ToString();
                            change = true;
                            MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).SetControllerTransform(__instance.correctNo);
                        }
                    }
                }).AddTo(__instance);
            });
            ((TMP_InputField[])__instance.GetPrivate("inpRot")).Select((p, idx) => new
            {
                inp = p,
                index = idx
            }).ToList().ForEach(p =>
            {
                p.inp.onEndEdit.AsObservable().Subscribe(delegate (string value)
                {
                    int xyz = p.index % 3;
                    float val = CustomBase.ConvertValueFromTextLimit(0f, 360f, 0, value);
                    p.inp.text = val.ToString();
                    MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).FuncUpdateAcsRotAdd(__instance.correctNo, xyz, false, val);
                    if (MoreAccessories._self._hasDarkness == false)
                        MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).UpdateAcsMoveHistory();
                    MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).SetControllerTransform(__instance.correctNo);
                });
            });
            ((Button[])__instance.GetPrivate("btnRotReset")).Select((p, idx) => new
            {
                btn = p,
                index = idx
            }).ToList().ForEach(p =>
            {
                p.btn.OnClickAsObservable().Subscribe(delegate
                {
                    ((TMP_InputField[])__instance.GetPrivate("inpRot"))[p.index].text = "0";
                    MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).FuncUpdateAcsRotAdd(__instance.correctNo, p.index, false, 0f);
                    if (MoreAccessories._self._hasDarkness == false)
                        MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).UpdateAcsMoveHistory();
                    MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).SetControllerTransform(__instance.correctNo);
                });
            });
            ((Button[])__instance.GetPrivate("btnScl")).Select((p, idx) => new
            {
                btn = p,
                index = idx
            }).ToList().ForEach(p =>
            {
                p.btn.OnClickAsObservable().Subscribe(delegate
                {
                    if (!change)
                    {
                        int num = p.index / 2;
                        int num2 = (p.index % 2 != 0) ? 1 : -1;
                        float val = num2 * ((float[])__instance.GetPrivate("moveSclValue"))[Singleton<CustomBase>.Instance.customSettingSave.acsCorrectSclRate[__instance.correctNo]];
                        MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).FuncUpdateAcsSclAdd(__instance.correctNo, num, true, val);
                        if (MoreAccessories._self._hasDarkness == false)
                            MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).UpdateAcsMoveHistory();
                        ((TMP_InputField[])__instance.GetPrivate("inpScl"))[num].text = MoreAccessories._self.GetPart(__instance.nSlotNo).addMove[__instance.correctNo, 2][num].ToString();
                    }
                });
                p.btn.UpdateAsObservable().SkipUntil(p.btn.OnPointerDownAsObservable().Do(delegate
                {
                    downTimeCnt = 0f;
                    loopTimeCnt = 0f;
                    change = false;
                })).TakeUntil(p.btn.OnPointerUpAsObservable().Do(delegate
                              {
                                  if (MoreAccessories._self._hasDarkness == false)
                                      if (change)
                                      {
                                          MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).UpdateAcsMoveHistory();
                                      }
                              })
                ).RepeatUntilDestroy(__instance).Subscribe(delegate
                {
                    int num = p.index / 2;
                    int num2 = (p.index % 2 != 0) ? 1 : -1;
                    float num3 = num2 * ((float[])__instance.GetPrivate("moveSclValue"))[Singleton<CustomBase>.Instance.customSettingSave.acsCorrectSclRate[__instance.correctNo]];
                    float num4 = 0f;
                    downTimeCnt += Time.deltaTime;
                    if (downTimeCnt > 0.3f)
                    {
                        for (loopTimeCnt += Time.deltaTime; loopTimeCnt > 0.05f; loopTimeCnt -= 0.05f)
                        {
                            num4 += num3;
                        }
                        if (num4 != 0f)
                        {
                            MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).FuncUpdateAcsSclAdd(__instance.correctNo, num, true, num4);
                            ((TMP_InputField[])__instance.GetPrivate("inpScl"))[num].text = MoreAccessories._self.GetPart(__instance.nSlotNo).addMove[__instance.correctNo, 2][num].ToString();
                            change = true;
                        }
                    }
                }).AddTo(__instance);
            });
            ((TMP_InputField[])__instance.GetPrivate("inpScl")).Select((p, idx) => new
            {
                inp = p,
                index = idx
            }).ToList().ForEach(p =>
            {
                p.inp.onEndEdit.AsObservable().Subscribe(delegate (string value)
                {
                    int xyz = p.index % 3;
                    float val = CustomBase.ConvertValueFromTextLimit(0.01f, 100f, 2, value);
                    p.inp.text = val.ToString();
                    MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).FuncUpdateAcsSclAdd(__instance.correctNo, xyz, false, val);
                    if (MoreAccessories._self._hasDarkness == false)
                        MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).UpdateAcsMoveHistory();
                });
            });
            ((Button[])__instance.GetPrivate("btnSclReset")).Select((p, idx) => new
            {
                btn = p,
                index = idx
            }).ToList().ForEach(p =>
            {
                p.btn.OnClickAsObservable().Subscribe(delegate
                {
                    ((TMP_InputField[])__instance.GetPrivate("inpScl"))[p.index].text = "1";
                    MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).FuncUpdateAcsSclAdd(__instance.correctNo, p.index, false, 1f);
                    if (MoreAccessories._self._hasDarkness == false)
                        MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).UpdateAcsMoveHistory();
                });
            });
            ((Button)__instance.GetPrivate("btnCopy")).OnClickAsObservable().Subscribe(delegate
            {
                Singleton<CustomBase>.Instance.vecAcsClipBord[0] = MoreAccessories._self.GetPart(__instance.nSlotNo).addMove[__instance.correctNo, 0];
                Singleton<CustomBase>.Instance.vecAcsClipBord[1] = MoreAccessories._self.GetPart(__instance.nSlotNo).addMove[__instance.correctNo, 1];
                Singleton<CustomBase>.Instance.vecAcsClipBord[2] = MoreAccessories._self.GetPart(__instance.nSlotNo).addMove[__instance.correctNo, 2];
            });
            ((Button)__instance.GetPrivate("btnPaste")).OnClickAsObservable().Subscribe(delegate
            {
                MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).FuncUpdateAcsMovePaste(__instance.correctNo, Singleton<CustomBase>.Instance.vecAcsClipBord);
                if (MoreAccessories._self._hasDarkness == false)
                    MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).UpdateAcsMoveHistory();
                ((TMP_InputField[])__instance.GetPrivate("inpPos"))[0].text = MoreAccessories._self.GetPart(__instance.nSlotNo).addMove[__instance.correctNo, 0].x.ToString();
                ((TMP_InputField[])__instance.GetPrivate("inpPos"))[1].text = MoreAccessories._self.GetPart(__instance.nSlotNo).addMove[__instance.correctNo, 0].y.ToString();
                ((TMP_InputField[])__instance.GetPrivate("inpPos"))[2].text = MoreAccessories._self.GetPart(__instance.nSlotNo).addMove[__instance.correctNo, 0].z.ToString();
                ((TMP_InputField[])__instance.GetPrivate("inpRot"))[0].text = MoreAccessories._self.GetPart(__instance.nSlotNo).addMove[__instance.correctNo, 1].x.ToString();
                ((TMP_InputField[])__instance.GetPrivate("inpRot"))[1].text = MoreAccessories._self.GetPart(__instance.nSlotNo).addMove[__instance.correctNo, 1].y.ToString();
                ((TMP_InputField[])__instance.GetPrivate("inpRot"))[2].text = MoreAccessories._self.GetPart(__instance.nSlotNo).addMove[__instance.correctNo, 1].z.ToString();
                ((TMP_InputField[])__instance.GetPrivate("inpScl"))[0].text = MoreAccessories._self.GetPart(__instance.nSlotNo).addMove[__instance.correctNo, 2].x.ToString();
                ((TMP_InputField[])__instance.GetPrivate("inpScl"))[1].text = MoreAccessories._self.GetPart(__instance.nSlotNo).addMove[__instance.correctNo, 2].y.ToString();
                ((TMP_InputField[])__instance.GetPrivate("inpScl"))[2].text = MoreAccessories._self.GetPart(__instance.nSlotNo).addMove[__instance.correctNo, 2].z.ToString();
                MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).SetControllerTransform(__instance.correctNo);
            });
            ((Button)__instance.GetPrivate("btnAllReset")).OnClickAsObservable().Subscribe(delegate
            {
                for (int j = 0; j < 3; j++)
                {
                    ((TMP_InputField[])__instance.GetPrivate("inpPos"))[j].text = "0";
                    ((TMP_InputField[])__instance.GetPrivate("inpRot"))[j].text = "0";
                    ((TMP_InputField[])__instance.GetPrivate("inpScl"))[j].text = "1";
                }
                MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).FuncUpdateAcsAllReset(__instance.correctNo);
                if (MoreAccessories._self._hasDarkness == false)
                    MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).UpdateAcsMoveHistory();
                MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).SetControllerTransform(__instance.correctNo);
            });



            return false;
        }
    }


    [HarmonyPatch(typeof(CustomAcsMoveWindow), "ChangeSlot", new[] { typeof(int), typeof(bool) })]
    internal static class CustomAcsMoveWindow_ChangeSlot_Patches
    {
        private static bool Prefix(CustomAcsMoveWindow __instance, int _no, bool open)
        {
            Toggle tglReference = (Toggle)__instance.GetPrivate("tglReference");
            __instance.slotNo = (CustomAcsMoveWindow.AcsSlotNo)_no;
            bool isOn = tglReference.isOn;
            tglReference.isOn = false;
            if (__instance.correctNo == 0)
                tglReference = MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).tglAcsMove01;
            else
                tglReference = MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).tglAcsMove02;
            __instance.SetPrivate("tglReference", tglReference);
            if (open && isOn)
                tglReference.isOn = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(CustomAcsMoveWindow), "UpdateCustomUI", new[] { typeof(int) })]
    internal static class CustomAcsMoveWindow_UpdateCustomUI_Patches
    {
        private static bool Prefix(CustomAcsMoveWindow __instance, int param)
        {
            ChaFileAccessory.PartsInfo part = MoreAccessories._self.GetPart(__instance.nSlotNo);
            for (int i = 0; i < 3; i++)
            {
                ((TMP_InputField[])__instance.GetPrivate("inpPos"))[i].text = part.addMove[__instance.correctNo, 0][i].ToString();
                ((TMP_InputField[])__instance.GetPrivate("inpRot"))[i].text = part.addMove[__instance.correctNo, 1][i].ToString();
                ((TMP_InputField[])__instance.GetPrivate("inpScl"))[i].text = part.addMove[__instance.correctNo, 2][i].ToString();
            }
            return false;
        }
    }


    [HarmonyPatch(typeof(CustomAcsMoveWindow), "UpdateDragValue", new[] { typeof(int), typeof(int), typeof(float) })]
    internal static class CustomAcsMoveWindow_UpdateDragValue_Patches
    {
        private static bool Prefix(CustomAcsMoveWindow __instance, int type, int xyz, float move)
        {
            switch (type)
            {
                case 0:
                    {
                        float val = move * ((float[])__instance.GetPrivate("movePosValue"))[Singleton<CustomBase>.Instance.customSettingSave.acsCorrectPosRate[__instance.correctNo]];
                        MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).FuncUpdateAcsPosAdd(__instance.correctNo, xyz, true, val);
                        ((TMP_InputField[])__instance.GetPrivate("inpPos"))[xyz].text = MoreAccessories._self.GetPart(__instance.nSlotNo).addMove[__instance.correctNo, 0][xyz].ToString();
                        break;
                    }
                case 1:
                    {
                        float val2 = move * ((float[])__instance.GetPrivate("moveRotValue"))[Singleton<CustomBase>.Instance.customSettingSave.acsCorrectRotRate[__instance.correctNo]];
                        MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).FuncUpdateAcsRotAdd(__instance.correctNo, xyz, true, val2);
                        ((TMP_InputField[])__instance.GetPrivate("inpRot"))[xyz].text = MoreAccessories._self.GetPart(__instance.nSlotNo).addMove[__instance.correctNo, 1][xyz].ToString();
                        break;
                    }
                case 2:
                    {
                        float val3 = move * ((float[])__instance.GetPrivate("moveSclValue"))[Singleton<CustomBase>.Instance.customSettingSave.acsCorrectSclRate[__instance.correctNo]];
                        MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).FuncUpdateAcsSclAdd(__instance.correctNo, xyz, true, val3);
                        ((TMP_InputField[])__instance.GetPrivate("inpScl"))[xyz].text = MoreAccessories._self.GetPart(__instance.nSlotNo).addMove[__instance.correctNo, 2][xyz].ToString();
                        break;
                    }
            }
            MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).SetControllerTransform(__instance.correctNo);
            return false;
        }
    }

    [HarmonyPatch(typeof(CustomAcsMoveWindow), "UpdateHistory")]
    internal static class CustomAcsMoveWindow_UpdateHistory_Patches
    {
        private static bool Prepare()
        {
            return (MoreAccessories._self._hasDarkness == false);
        }
        private static bool Prefix(CustomAcsMoveWindow __instance)
        {
            MoreAccessories._self.GetCvsAccessory(__instance.nSlotNo).UpdateAcsMoveHistory();
            return false;
        }
    }

    [HarmonyPatch(typeof(CustomAcsSelectKind), "ChangeSlot", new[] { typeof(int), typeof(bool) })]
    internal static class CustomAcsSelectKind_ChangeSlot_Patches
    {
        private static bool Prefix(CustomAcsSelectKind __instance, int _no, bool open)
        {
            CustomSelectWindow selWin = (CustomSelectWindow)__instance.GetPrivate("selWin");
            __instance.slotNo = _no;
            bool isOn = selWin.tglReference.isOn;
            selWin.tglReference.isOn = false;
            selWin.tglReference = MoreAccessories._self.GetCvsAccessory(__instance.slotNo).tglAcsKind;
            if (open && isOn)
                selWin.tglReference.isOn = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(CustomAcsSelectKind), "UpdateCustomUI", new[] { typeof(int) })]
    internal static class CustomAcsSelectKind_UpdateCustomUI_Patches
    {
        private static bool Prefix(CustomAcsSelectKind __instance, int param)
        {
            ((CustomSelectListCtrl)__instance.GetPrivate("listCtrl")).SelectItem(MoreAccessories._self.GetPart(__instance.slotNo).id);
            return false;
        }
    }

    [HarmonyPatch(typeof(CustomAcsSelectKind), "OnSelect", new[] { typeof(int) })]
    internal static class CustomAcsSelectKind_OnSelect_Patches
    {
        private static bool Prefix(CustomAcsSelectKind __instance, int index)
        {
            CustomSelectInfo selectInfoFromIndex = ((CustomSelectListCtrl)__instance.GetPrivate("listCtrl")).GetSelectInfoFromIndex(index);
            if (selectInfoFromIndex != null)
                MoreAccessories._self.GetCvsAccessory(__instance.slotNo).UpdateSelectAccessoryKind(selectInfoFromIndex.name, selectInfoFromIndex.sic.img.sprite, index);
            return false;
        }
    }

    [HarmonyPatch(typeof(ChaControl), "GetAccessoryDefaultParentStr", new[] { typeof(int) })]
    internal static class ChaControl_GetAccessoryDefaultParentStr_Patches
    {

        private static bool Prefix(ChaControl __instance, int slotNo, ref string __result)
        {
            GameObject gameObject;
            if (slotNo < 20)
                gameObject = __instance.objAccessory[slotNo];
            else
                gameObject = MoreAccessories._self._accessoriesByChar[__instance.chaFile].objAccessory[slotNo - 20];
            if (null == gameObject)
            {
                __result = string.Empty;
                return false;
            }
            ListInfoComponent component = gameObject.GetComponent<ListInfoComponent>();
            __result = component.data.GetInfo(ChaListDefine.KeyType.Parent);
            return false;
        }
    }

    [HarmonyPatch(typeof(ChaControl), "GetAccessoryDefaultColor", new[] { typeof(Color), typeof(int), typeof(int) }, new[] { ArgumentType.Out, ArgumentType.Normal, ArgumentType.Normal })]
    internal static class ChaControl_GetAccessoryDefaultColor_Patches
    {
        private static bool Prefix(ChaControl __instance, ref Color color, int slotNo, int no, ref bool __result)
        {
            ChaAccessoryComponent chaAccessoryComponent;
            if (slotNo < 20)
                chaAccessoryComponent = __instance.cusAcsCmp[slotNo];
            else
                chaAccessoryComponent = MoreAccessories._self._accessoriesByChar[__instance.chaFile].cusAcsCmp[slotNo - 20];

            if (null == chaAccessoryComponent)
            {
                __result = false;
                return false;
            }
            if (no == 0 && chaAccessoryComponent.useColor01)
            {
                color = chaAccessoryComponent.defColor01;
                __result = true;
                return false;
            }
            if (no == 1 && chaAccessoryComponent.useColor02)
            {
                color = chaAccessoryComponent.defColor02;
                __result = true;
                return false;
            }
            if (no == 2 && chaAccessoryComponent.useColor03)
            {
                color = chaAccessoryComponent.defColor03;
                __result = true;
                return false;
            }
            if (no == 3 && chaAccessoryComponent.rendAlpha != null && chaAccessoryComponent.rendAlpha.Length != 0)
            {
                color = chaAccessoryComponent.defColor04;
                __result = true;
                return false;
            }
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(ChaControl), "SetAccessoryPos", new[] { typeof(int), typeof(int), typeof(float), typeof(bool), typeof(int) })]
    internal static class ChaControl_SetAccessoryPos_Patches
    {
        private static bool Prefix(ChaControl __instance, int slotNo, int correctNo, float value, bool add, int flags, ref bool __result)
        {
            GameObject gameObject;

            if (slotNo < 20)
                gameObject = __instance.objAcsMove[slotNo, correctNo];
            else
                gameObject = MoreAccessories._self._accessoriesByChar[__instance.chaFile].objAcsMove[slotNo - 20][correctNo];

            if (null == gameObject)
            {
                __result = false;
                return false;
            }
            ChaFileAccessory.PartsInfo part;
            if (slotNo < 20)
                part = __instance.nowCoordinate.accessory.parts[slotNo];
            else
                part = MoreAccessories._self._accessoriesByChar[__instance.chaFile].nowAccessories[slotNo - 20];
            if ((flags & 1) != 0)
            {
                float num = float.Parse((((!add) ? 0f : part.addMove[correctNo, 0].x) + value).ToString("f1"));
                part.addMove[correctNo, 0].x = Mathf.Clamp(num, -100f, 100f);
            }
            if ((flags & 2) != 0)
            {
                float num2 = float.Parse((((!add) ? 0f : part.addMove[correctNo, 0].y) + value).ToString("f1"));
                part.addMove[correctNo, 0].y = Mathf.Clamp(num2, -100f, 100f);
            }
            if ((flags & 4) != 0)
            {
                float num3 = float.Parse((((!add) ? 0f : part.addMove[correctNo, 0].z) + value).ToString("f1"));
                part.addMove[correctNo, 0].z = Mathf.Clamp(num3, -100f, 100f);
            }
            Dictionary<int, ListInfoBase> categoryInfo = __instance.lstCtrl.GetCategoryInfo((ChaListDefine.CategoryNo)part.type);
            ListInfoBase listInfoBase = null;
            categoryInfo.TryGetValue(part.id, out listInfoBase);
            if (listInfoBase.GetInfoInt(ChaListDefine.KeyType.HideHair) == 1)
            {
                gameObject.transform.localPosition = new Vector3(0f, 0f, 0f);
            }
            else
            {
                gameObject.transform.localPosition = new Vector3(part.addMove[correctNo, 0].x * 0.01f, part.addMove[correctNo, 0].y * 0.01f, part.addMove[correctNo, 0].z * 0.01f);
            }
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(ChaControl), "SetAccessoryRot", new[] { typeof(int), typeof(int), typeof(float), typeof(bool), typeof(int) })]
    internal static class ChaControl_SetAccessoryRot_Patches
    {
        private static bool Prefix(ChaControl __instance, int slotNo, int correctNo, float value, bool add, int flags, ref bool __result)
        {
            GameObject gameObject;

            if (slotNo < 20)
                gameObject = __instance.objAcsMove[slotNo, correctNo];
            else
                gameObject = MoreAccessories._self._accessoriesByChar[__instance.chaFile].objAcsMove[slotNo - 20][correctNo];

            if (null == gameObject)
            {
                __result = false;
                return false;
            }
            ChaFileAccessory.PartsInfo part;
            if (slotNo < 20)
                part = __instance.nowCoordinate.accessory.parts[slotNo];
            else
                part = MoreAccessories._self._accessoriesByChar[__instance.chaFile].nowAccessories[slotNo - 20];
            if ((flags & 1) != 0)
            {
                float num = (float)((int)(((!add) ? 0f : part.addMove[correctNo, 1].x) + value));
                part.addMove[correctNo, 1].x = Mathf.Repeat(num, 360f);
            }
            if ((flags & 2) != 0)
            {
                float num2 = (float)((int)((!add ? 0f : part.addMove[correctNo, 1].y) + value));
                part.addMove[correctNo, 1].y = Mathf.Repeat(num2, 360f);
            }
            if ((flags & 4) != 0)
            {
                float num3 = (float)((int)(((!add) ? 0f : part.addMove[correctNo, 1].z) + value));
                part.addMove[correctNo, 1].z = Mathf.Repeat(num3, 360f);
            }
            Dictionary<int, ListInfoBase> categoryInfo = __instance.lstCtrl.GetCategoryInfo((ChaListDefine.CategoryNo)part.type);
            ListInfoBase listInfoBase = null;
            categoryInfo.TryGetValue(part.id, out listInfoBase);
            if (listInfoBase.GetInfoInt(ChaListDefine.KeyType.HideHair) == 1)
            {
                gameObject.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
            }
            else
            {
                gameObject.transform.localEulerAngles = new Vector3(part.addMove[correctNo, 1].x, part.addMove[correctNo, 1].y, part.addMove[correctNo, 1].z);
            }
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(ChaControl), "SetAccessoryScl", new[] { typeof(int), typeof(int), typeof(float), typeof(bool), typeof(int) })]
    internal static class ChaControl_SetAccessoryScl_Patches
    {
        private static bool Prefix(ChaControl __instance, int slotNo, int correctNo, float value, bool add, int flags, ref bool __result)
        {
            GameObject gameObject;

            if (slotNo < 20)
                gameObject = __instance.objAcsMove[slotNo, correctNo];
            else
                gameObject = MoreAccessories._self._accessoriesByChar[__instance.chaFile].objAcsMove[slotNo - 20][correctNo];

            if (null == gameObject)
            {
                __result = false;
                return false;
            }
            ChaFileAccessory.PartsInfo part;
            if (slotNo < 20)
                part = __instance.nowCoordinate.accessory.parts[slotNo];
            else
                part = MoreAccessories._self._accessoriesByChar[__instance.chaFile].nowAccessories[slotNo - 20];
            if ((flags & 1) != 0)
            {
                float num = float.Parse((((!add) ? 0f : part.addMove[correctNo, 2].x) + value).ToString("f2"));
                part.addMove[correctNo, 2].x = Mathf.Clamp(num, -100f, 100f);
            }
            if ((flags & 2) != 0)
            {
                float num2 = float.Parse((((!add) ? 0f : part.addMove[correctNo, 2].y) + value).ToString("f2"));
                part.addMove[correctNo, 2].y = Mathf.Clamp(num2, -100f, 100f);
            }
            if ((flags & 4) != 0)
            {
                float num3 = float.Parse((((!add) ? 0f : part.addMove[correctNo, 2].z) + value).ToString("f2"));
                part.addMove[correctNo, 2].z = Mathf.Clamp(num3, -100f, 100f);
            }
            Dictionary<int, ListInfoBase> categoryInfo = __instance.lstCtrl.GetCategoryInfo((ChaListDefine.CategoryNo)part.type);
            ListInfoBase listInfoBase = null;
            categoryInfo.TryGetValue(part.id, out listInfoBase);
            if (listInfoBase.GetInfoInt(ChaListDefine.KeyType.HideHair) == 1)
            {
                gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
            }
            else
            {
                gameObject.transform.localScale = new Vector3(part.addMove[correctNo, 2].x, part.addMove[correctNo, 2].y, part.addMove[correctNo, 2].z);
            }
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(ChaControl), "UpdateAccessoryMoveFromInfo", new[] { typeof(int) })]
    internal static class ChaControl_UpdateAccessoryMoveFromInfo_Patches
    {
        private static bool Prefix(ChaControl __instance, int slotNo, ref bool __result)
        {
            ChaFileAccessory.PartsInfo part;
            MoreAccessories.CharAdditionalData data = null;
            if (slotNo < 20)
                part = __instance.nowCoordinate.accessory.parts[slotNo];
            else
            {
                data = MoreAccessories._self._accessoriesByChar[__instance.chaFile];
                part = data.nowAccessories[slotNo - 20];
            }
            Dictionary<int, ListInfoBase> categoryInfo = __instance.lstCtrl.GetCategoryInfo((ChaListDefine.CategoryNo)part.type);
            ListInfoBase listInfoBase = null;
            categoryInfo.TryGetValue(part.id, out listInfoBase);
            if (listInfoBase.GetInfoInt(ChaListDefine.KeyType.HideHair) == 1)
            {
                for (int i = 0; i < 2; i++)
                {
                    GameObject gameObject;
                    if (slotNo < 20)
                        gameObject = __instance.objAcsMove[slotNo, i];
                    else
                        gameObject = data.objAcsMove[slotNo - 20][i];
                    if (!(null == gameObject))
                    {
                        gameObject.transform.localPosition = new Vector3(0f, 0f, 0f);
                        gameObject.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
                        gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
                    }
                }
            }
            else
            {
                for (int j = 0; j < 2; j++)
                {
                    GameObject gameObject2;
                    if (slotNo < 20)
                        gameObject2 = __instance.objAcsMove[slotNo, j];
                    else
                        gameObject2 = data.objAcsMove[slotNo - 20][j];
                    if (!(null == gameObject2))
                    {
                        gameObject2.transform.localPosition = new Vector3(part.addMove[j, 0].x * 0.01f, part.addMove[j, 0].y * 0.01f, part.addMove[j, 0].z * 0.01f);
                        gameObject2.transform.localEulerAngles = new Vector3(part.addMove[j, 1].x, part.addMove[j, 1].y, part.addMove[j, 1].z);
                        gameObject2.transform.localScale = new Vector3(part.addMove[j, 2].x, part.addMove[j, 2].y, part.addMove[j, 2].z);
                    }
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(ChaControl), "ChangeAccessoryColor", new[] { typeof(int) })]
    internal static class ChaControl_ChangeAccessoryColor_Patches
    {
        private static bool Prefix(ChaControl __instance, int slotNo, ref bool __result)
        {
            ChaAccessoryComponent chaAccessoryComponent;
            if (slotNo < 20)
                chaAccessoryComponent = __instance.cusAcsCmp[slotNo];
            else
                chaAccessoryComponent = MoreAccessories._self._accessoriesByChar[__instance.chaFile].cusAcsCmp[slotNo - 20];
            if (null == chaAccessoryComponent)
            {
                __result = false;
                return false;
            }
            ChaFileAccessory.PartsInfo partsInfo;
            if (slotNo < 20)
                partsInfo = __instance.nowCoordinate.accessory.parts[slotNo];
            else
                partsInfo = MoreAccessories._self._accessoriesByChar[__instance.chaFile].nowAccessories[slotNo - 20];

            if (chaAccessoryComponent.rendNormal != null)
            {
                foreach (Renderer renderer in chaAccessoryComponent.rendNormal)
                {
                    if (chaAccessoryComponent.useColor01)
                    {
                        renderer.material.SetColor(ChaShader._Color, partsInfo.color[0]);
                    }
                    if (chaAccessoryComponent.useColor02)
                    {
                        renderer.material.SetColor(ChaShader._Color2, partsInfo.color[1]);
                    }
                    if (chaAccessoryComponent.useColor03)
                    {
                        renderer.material.SetColor(ChaShader._Color3, partsInfo.color[2]);
                    }
                }
            }
            if (chaAccessoryComponent.rendAlpha != null)
            {
                foreach (Renderer renderer2 in chaAccessoryComponent.rendAlpha)
                {
                    renderer2.material.SetColor(ChaShader._Color4, partsInfo.color[3]);
                    renderer2.gameObject.SetActiveIfDifferent(partsInfo.color[3].a != 0f);
                }
            }
            if (chaAccessoryComponent.rendHair != null)
            {
                Color startColor = __instance.fileHair.parts[0].startColor;
                foreach (Renderer renderer3 in chaAccessoryComponent.rendHair)
                {
                    renderer3.material.SetColor(ChaShader._Color, startColor);
                    renderer3.material.SetColor(ChaShader._Color2, startColor);
                    renderer3.material.SetColor(ChaShader._Color3, startColor);
                }
            }
            __result = true;
            return false;
        }
    }


    [HarmonyPatch(typeof(ChaControl), "ChangeAccessoryParent", new[] { typeof(int), typeof(string) })]
    internal static class ChaControl_ChangeAccessoryParent_Patches
    {
        private static bool Prefix(ChaControl __instance, int slotNo, string parentStr, ref bool __result)
        {
            GameObject gameObject;
            MoreAccessories.CharAdditionalData additionalData = null;
            if (slotNo < 20)
                gameObject = __instance.objAccessory[slotNo];
            else
            {
                additionalData = MoreAccessories._self._accessoriesByChar[__instance.chaFile];
                gameObject = additionalData.objAccessory[slotNo - 20];

            }
            if (null == gameObject)
            {
                __result = false;
                return false;
            }
            if ("none" == parentStr)
            {
                gameObject.transform.SetParent(null, false);
                __result = true;
                return false;
            }
            ListInfoComponent component = gameObject.GetComponent<ListInfoComponent>();
            ListInfoBase data = component.data;
            if ("0" == data.GetInfo(ChaListDefine.KeyType.Parent))
            {
                __result = false;
                return false;
            }
            try
            {
                ChaReference.RefObjKey key = (ChaReference.RefObjKey)Enum.Parse(typeof(ChaReference.RefObjKey), parentStr);
                GameObject referenceInfo = __instance.GetReferenceInfo(key);
                if (null == referenceInfo)
                {
                    return false;
                }
                gameObject.transform.SetParent(referenceInfo.transform, false);

                if (slotNo < 20)
                {
                    __instance.nowCoordinate.accessory.parts[slotNo].parentKey = parentStr;
                    __instance.nowCoordinate.accessory.parts[slotNo].partsOfHead = ChaAccessoryDefine.CheckPartsOfHead(parentStr);
                }
                else
                {
                    additionalData.nowAccessories[slotNo - 20].parentKey = parentStr;
                    additionalData.nowAccessories[slotNo - 20].partsOfHead = ChaAccessoryDefine.CheckPartsOfHead(parentStr);
                }
            }
            catch (ArgumentException)
            {
                __result = false;
                return false;
            }
            __result = true;
            return true;
        }
    }

    internal static class ChaControl_ChangeAccessory_Patches
    {
        private static MethodInfo _loadCharaFbxData;
#if KOIKATSU
        private static readonly object[] _params = new object[9];
#elif EMOTIONCREATORS
        private static readonly object[] _params = new object[8];
#endif

        internal static void ManualPatch(Harmony harmony)
        {
            foreach (MethodInfo info in typeof(ChaControl).GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                switch (info.Name)
                {
                    case "ChangeAccessory":
                        if (info.GetParameters().Length == 1)
                            harmony.Patch(info, new HarmonyMethod(typeof(ChaControl_ChangeAccessory_Patches).GetMethod(nameof(GroupPrefix), BindingFlags.NonPublic | BindingFlags.Static)), null);
                        else
                            harmony.Patch(info, new HarmonyMethod(typeof(ChaControl_ChangeAccessory_Patches).GetMethod(nameof(IndividualPrefix), BindingFlags.NonPublic | BindingFlags.Static)), null);
                        break;
                    case "ChangeAccessoryAsync":
                        if (info.GetParameters().Length == 1)
                            harmony.Patch(info, new HarmonyMethod(typeof(ChaControl_ChangeAccessory_Patches).GetMethod(nameof(GroupPrefix), BindingFlags.NonPublic | BindingFlags.Static)), null);
                        break;
                }
            }
        }

        private static void GroupPrefix(ChaControl __instance, bool forceChange)
        {
            MoreAccessories.CharAdditionalData data;
            if (MoreAccessories._self._accessoriesByChar.TryGetValue(__instance.chaFile, out data) == false)
                return;
            int i;
            for (i = 0; i < data.nowAccessories.Count; i++)
            {
                ChaFileAccessory.PartsInfo part = data.nowAccessories[i];
                ChangeAccessory(__instance, i, part.type, part.id, part.parentKey, forceChange);
            }
            for (; i < data.objAccessory.Count; i++)
            {
                CleanRemainingAccessory(__instance, data, i);
            }
        }

        private static bool IndividualPrefix(ChaControl __instance, int slotNo, int type, int id, string parentKey, bool forceChange = false)
        {
            if (slotNo >= 20)
            {
                ChangeAccessory(__instance, slotNo - 20, type, id, parentKey, forceChange);
                return false;
            }
            return true;
        }

        private static void ChangeAccessory(ChaControl instance, int slotNo, int type, int id, string parentKey, bool forceChange = false)
        {
            ListInfoBase lib = null;
            bool load = true;
            bool release = true;
            MoreAccessories.CharAdditionalData data = MoreAccessories._self._accessoriesByChar[instance.chaFile];
            if (type == 120 || !MathfEx.RangeEqualOn(121, type, 130))
            {
                release = true;
                load = false;
            }
            else
            {
                if (id == -1)
                {
                    release = false;
                    load = false;
                }
                int num = (data.infoAccessory[slotNo] != null) ? data.infoAccessory[slotNo].Category : -1;
                int num2 = (data.infoAccessory[slotNo] != null) ? data.infoAccessory[slotNo].Id : -1;
                if (!forceChange && null != data.objAccessory[slotNo] && type == num && id == num2)
                {
                    load = false;
                    release = false;
                }
                if (id != -1)
                {
                    Dictionary<int, ListInfoBase> categoryInfo = instance.lstCtrl.GetCategoryInfo((ChaListDefine.CategoryNo)type);
                    if (categoryInfo == null)
                    {
                        release = true;
                        load = false;
                    }
                    else if (!categoryInfo.TryGetValue(id, out lib))
                    {
                        release = true;
                        load = false;
                    }
#if KOIKATSU
                    else if (!instance.hiPoly)
                    {
                        bool flag = true;
                        if (type == 123 && lib.Kind == 1)
                        {
                            flag = false;
                        }
                        if (type == 122 && lib.GetInfoInt(ChaListDefine.KeyType.HideHair) == 1)
                        {
                            flag = false;
                        }
                        if (Manager.Config.EtcData.loadHeadAccessory && type == 122 && lib.Kind == 1)
                        {
                            flag = false;
                        }
                        if (Manager.Config.EtcData.loadAllAccessory)
                        {
                            flag = false;
                        }
                        if (flag)
                        {
                            release = true;
                            load = false;
                        }
                    }
#endif
                }
            }
            if (release)
            {
                if (!load)
                {
                    data.nowAccessories[slotNo].MemberInit();
                    data.nowAccessories[slotNo].type = 120;
                }
                if (data.objAccessory[slotNo])
                {
                    instance.SafeDestroy(data.objAccessory[slotNo]);
                    data.objAccessory[slotNo] = null;
                    data.infoAccessory[slotNo] = null;
                    data.cusAcsCmp[slotNo] = null;
                    for (int i = 0; i < 2; i++)
                    {
                        data.objAcsMove[slotNo][i] = null;
                    }
                }
            }
            if (load)
            {
                byte weight = 0;
                Transform trfParent = null;
                if ("null" == lib.GetInfo(ChaListDefine.KeyType.Parent))
                {
                    weight = 2;
                    trfParent = instance.objTop.transform;
                }
                if (_loadCharaFbxData == null)
                    _loadCharaFbxData = instance.GetType().GetMethod("LoadCharaFbxData", AccessTools.all);
#if KOIKATSU
                _params[0] = true;
                _params[1] = type;
                _params[2] = id;
                _params[3] = "ca_slot" + (slotNo + 20).ToString("00");
                _params[4] = false;
                _params[5] = weight;
                _params[6] = trfParent;
                _params[7] = -1;
                _params[8] = false;
#elif EMOTIONCREATORS
                _params[0] = type;
                _params[1] = id;
                _params[2] = "ca_slot" + (slotNo + 20).ToString("00");
                _params[3] = false;
                _params[4] = weight;
                _params[5] = trfParent;
                _params[6] = -1;
                _params[7] = false;
#endif

                data.objAccessory[slotNo] = (GameObject)_loadCharaFbxData.Invoke(instance, _params); // I'm doing this the reflection way in order to be compatible with other plugins (like RimRemover)
                if (data.objAccessory[slotNo])
                {
                    ListInfoComponent component = data.objAccessory[slotNo].GetComponent<ListInfoComponent>();
                    lib = (data.infoAccessory[slotNo] = component.data);
                    data.cusAcsCmp[slotNo] = data.objAccessory[slotNo].GetComponent<ChaAccessoryComponent>();
                    data.nowAccessories[slotNo].type = type;
                    data.nowAccessories[slotNo].id = lib.Id;
                    data.objAcsMove[slotNo][0] = data.objAccessory[slotNo].transform.FindLoop("N_move");
                    data.objAcsMove[slotNo][1] = data.objAccessory[slotNo].transform.FindLoop("N_move2");
                }
            }
            if (data.objAccessory[slotNo])
            {
                if (instance.loadWithDefaultColorAndPtn)
                {
                    SetAccessoryDefaultColor(instance, slotNo);
                }
                instance.ChangeAccessoryColor(slotNo + 20);
                if (string.Empty == parentKey)
                {
                    parentKey = lib.GetInfo(ChaListDefine.KeyType.Parent);
                }
                instance.ChangeAccessoryParent(slotNo + 20, parentKey);
                instance.UpdateAccessoryMoveFromInfo(slotNo + 20);
                data.nowAccessories[slotNo].partsOfHead = ChaAccessoryDefine.CheckPartsOfHead(parentKey);
#if KOIKATSU
                if (!instance.hiPoly && !Manager.Config.EtcData.loadAllAccessory)
                {
                    DynamicBone[] componentsInChildren = data.objAccessory[slotNo].GetComponentsInChildren<DynamicBone>(true);
                    foreach (DynamicBone dynamicBone in componentsInChildren)
                    {
                        dynamicBone.enabled = false;
                    }
                }
#endif
                if (MoreAccessories._self._hasDarkness)
                    instance.CallPrivate("ChangeShakeAccessory", slotNo + 20);
            }
            instance.SetHideHairAccessory();
        }

        private static void CleanRemainingAccessory(ChaControl instance, MoreAccessories.CharAdditionalData data, int slotNo)
        {
            if (slotNo < data.nowAccessories.Count)
            {
                data.nowAccessories[slotNo].MemberInit();
                data.nowAccessories[slotNo].type = 120;
            }
            if (data.objAccessory[slotNo])
            {
                instance.SafeDestroy(data.objAccessory[slotNo]);
                data.objAccessory[slotNo] = null;
                data.infoAccessory[slotNo] = null;
                data.cusAcsCmp[slotNo] = null;
                for (int i = 0; i < 2; i++)
                {
                    data.objAcsMove[slotNo][i] = null;
                }
                if (MoreAccessories._self._hasDarkness)
                    instance.CallPrivate("ChangeShakeAccessory", slotNo);
            }
            instance.SetHideHairAccessory();
        }

        private static void SetAccessoryDefaultColor(ChaControl instance, int slotNo)
        {
            MoreAccessories.CharAdditionalData data = MoreAccessories._self._accessoriesByChar[instance.chaFile];
            ChaAccessoryComponent chaAccessoryComponent = data.cusAcsCmp[slotNo];
            if (null == chaAccessoryComponent)
            {
                return;
            }
            if (chaAccessoryComponent.useColor01)
            {
                data.nowAccessories[slotNo].color[0] = chaAccessoryComponent.defColor01;
            }
            if (chaAccessoryComponent.useColor02)
            {
                data.nowAccessories[slotNo].color[1] = chaAccessoryComponent.defColor02;
            }
            if (chaAccessoryComponent.useColor03)
            {
                data.nowAccessories[slotNo].color[2] = chaAccessoryComponent.defColor03;
            }
            if (chaAccessoryComponent.rendAlpha != null && chaAccessoryComponent.rendAlpha.Length != 0)
            {
                data.nowAccessories[slotNo].color[3] = chaAccessoryComponent.defColor04;
            }
        }

    }
    #endregion
}
