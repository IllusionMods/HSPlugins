using System;
using System.IO;
using System.Reflection;
using AIChara;
using CharaCustom;
using HarmonyLib;
using Studio;
using UnityEngine;

namespace MoreAccessoriesAI.Patches
{
    public static class Various
    {
        public static void PatchAll(Harmony harmony)
        {
            harmony.PatchAll(typeof(Various));
            //Doing those manually because for some reason they can't be done automatically, idk.
            harmony.Patch(typeof(ChaFile).GetConstructor(new Type[0]), postfix: new HarmonyMethod(typeof(Various).GetMethod(nameof(ChaFile_Ctor_Postfix), BindingFlags.NonPublic | BindingFlags.Static)));
            harmony.Patch(AccessTools.Method(typeof(ChaFile), "CopyCoordinate"), postfix: new HarmonyMethod(typeof(Various).GetMethod(nameof(ChaFile_CopyCoordinate_Postfix), BindingFlags.NonPublic | BindingFlags.Static)));
        }

        private static void ChaFile_Ctor_Postfix(ChaFile __instance)
        {
            MoreAccessories._self._charByCoordinates.Add(__instance.coordinate, __instance);
        }

        private static void ChaFile_CopyCoordinate_Postfix(ChaFile __instance, ChaFileCoordinate _coordinate)
        {
            MoreAccessories.AdditionalData sourceData;
            ChaFile sourceFile = MoreAccessories._self.GetCharaByCoordinate(_coordinate);
            if (sourceFile != null)
            {
                if (MoreAccessories._self._charAdditionalData.TryGetValue(sourceFile, out sourceData) == false)
                {
                    sourceData = new MoreAccessories.AdditionalData();
                    MoreAccessories._self._charAdditionalData.Add(sourceFile, sourceData);
                }
            }
            else
                sourceData = new MoreAccessories.AdditionalData();
            if (MoreAccessories._self._charAdditionalData.TryGetValue(__instance, out MoreAccessories.AdditionalData destinationData) == false)
            {
                destinationData = new MoreAccessories.AdditionalData();
                MoreAccessories._self._charAdditionalData.Add(__instance, destinationData);
            }
            if (sourceData == destinationData)
                return;
            destinationData.LoadFrom(sourceData);
        }

        [HarmonyPatch(typeof(CustomBase), "ChangeAcsSlotName", typeof(int)), HarmonyPostfix]
        private static void CustomBase_ChangeAcsSlotName_Postfix(CustomBase __instance, int slotNo)
        {
            if (MoreAccessories._self._makerAdditionalData == null)
                return;
            if (slotNo == -1)
            {
                for (int i = 0; i < MoreAccessories._self._makerAdditionalData.parts.Count && i < MoreAccessories._self._makerSlots.Count; i++)
                {
                    ChaFileAccessory.PartsInfo part = MoreAccessories._self._makerAdditionalData.parts[i];
                    if (part.type == 350)
                    {
                        MoreAccessories._self._makerSlots[i].slotText.text = (i + 21).ToString("00");
                    }
                    else
                    {
                        ListInfoBase listInfo = __instance.chaCtrl.lstCtrl.GetListInfo((ChaListDefine.CategoryNo)part.type, part.id);
                        MoreAccessories._self._makerSlots[i].slotText.text = $"{i + 21:00} {listInfo.Name}";
                    }
                }
            }
            else if (slotNo >= 20)
            {
                slotNo -= 20;
                if (slotNo < MoreAccessories._self._makerAdditionalData.parts.Count && slotNo < MoreAccessories._self._makerSlots.Count)
                {
                    ChaFileAccessory.PartsInfo part = MoreAccessories._self._makerAdditionalData.parts[slotNo];
                    if (part.type == 350)
                    {
                        MoreAccessories._self._makerSlots[slotNo].slotText.text = (slotNo + 21).ToString("00");
                    }
                    else
                    {
                        ListInfoBase listInfo = __instance.chaCtrl.lstCtrl.GetListInfo((ChaListDefine.CategoryNo)part.type, part.id);
                        MoreAccessories._self._makerSlots[slotNo].slotText.text = $"{slotNo + 21:00} {listInfo.Name}";
                    }
                }
            }
        }

        [HarmonyPatch(typeof(CustomBase), "ChangeAcsSlotColor", typeof(int)), HarmonyPostfix]
        private static void CustomBase_ChangeAcsSlotColor_Postfix(CustomBase __instance, int slotNo)
        {
            if (MoreAccessories._self._makerAdditionalData == null)
                return;
            slotNo -= 20;
            for (int i = 0; i < MoreAccessories._self._makerSlots.Count; i++)
                MoreAccessories._self._makerSlots[i].slotText.color = i != slotNo ? new Color32(235, 226, 215, 255) : new Color32(204, 197, 59, 255);
        }

        [HarmonyPatch(typeof(ChaFileControl), "LoadFileLimited", typeof(string), typeof(byte), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool)), HarmonyPrefix]
        private static void ChaFileControl_LoadFileLimited_Prefix(bool coordinate = true)
        {
            MoreAccessories._self._loadAdditionalAccessories = coordinate;
        }

        [HarmonyPatch(typeof(ChaFileControl), "LoadFileLimited", typeof(string), typeof(byte), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool)), HarmonyPostfix]
        private static void ChaFileControl_LoadFileLimited_Postfix()
        {
            MoreAccessories._self._loadAdditionalAccessories = true;
        }

        [
                HarmonyPatch(typeof(CvsC_ClothesLoad),
#if AISHOUJO
                        "<Start>m__0",
#elif HONEYSELECT2
                        "<Start>b__4_1",
#endif
                        typeof(CustomClothesFileInfo)),
                HarmonyPrefix
        ]
        private static void CvsC_ClothesLoad_Start_Anonymous0_Prefix()
        {
            MoreAccessories._self._loadAdditionalAccessories = false;
        }

        [
                HarmonyPatch(typeof(CvsC_ClothesLoad),
#if AISHOUJO
                        "<Start>m__0",
#elif HONEYSELECT2
                        "<Start>b__4_1",
#endif
                        typeof(CustomClothesFileInfo)),
                HarmonyPostfix
        ]
        private static void CvsC_ClothesLoad_Start_Anonymous0_Postfix()
        {
            MoreAccessories._self._loadAdditionalAccessories = true;
        }

        [HarmonyPatch(typeof(ChaControl), "ChangeNowCoordinate", typeof(string), typeof(bool), typeof(bool)), HarmonyPrefix]
        private static void ChaControl_ChangeNowCoordinate_Prefix(ChaControl __instance)
        {
            MoreAccessories._self._overrideChaFileCoordinate = __instance.nowCoordinate;
        }

        [HarmonyPatch(typeof(ChaControl), "ChangeNowCoordinate", typeof(string), typeof(bool), typeof(bool)), HarmonyPostfix]
        private static void ChaControl_ChangeNowCoordinate_Postfix()
        {
            MoreAccessories._self._overrideChaFileCoordinate = null;
        }

        [HarmonyPatch(typeof(ChaControl), "SetAccessoryState", typeof(int), typeof(bool)), HarmonyPostfix]
        private static void ChaControl_SetAccessoryState_Postfix(ChaControl __instance, int slotNo, bool show)
        {
            if (slotNo >= 20)
                MoreAccessories._self.GetAdditionalDataByCharacter(__instance.chaFile).objects[slotNo - 20].show = show;
        }

        [HarmonyPatch(typeof(ChaControl), "SetAccessoryStateAll", typeof(bool)), HarmonyPostfix]
        private static void ChaControl_SetAccessoryState_Postfix(ChaControl __instance, bool show)
        {
            foreach (MoreAccessories.AdditionalData.AccessoryObject accessoryObject in MoreAccessories._self.GetAdditionalDataByCharacter(__instance.chaFile).objects)
                accessoryObject.show = show;
        }

        [HarmonyPatch(typeof(ChaControl), "UpdateVisible"), HarmonyPostfix]
        private static void ChaControl_UpdateVisible_Postfix(ChaControl __instance, bool ___confBody, bool ___drawSimple)
        {
            MoreAccessories.AdditionalData additionalData = MoreAccessories._self.GetAdditionalDataByCharacter(__instance.chaFile);
            for (int i = 0; i < additionalData.parts.Count; i++)
            {
                MoreAccessories.AdditionalData.AccessoryObject accessoryObject = additionalData.objects[i];
                if (accessoryObject.obj != null)
                {
                    accessoryObject.obj.SetActive(__instance.visibleAll && accessoryObject.show && !___drawSimple && (__instance.fileStatus.visibleHeadAlways || !additionalData.parts[i].partsOfHead) && __instance.fileStatus.visibleBodyAlways && ___confBody);
                }
            }
        }

#if HONEYSELECT2
        [HarmonyPatch(typeof(HSceneSpriteAccessoryCondition), "SetAccessoryCharacter"), HarmonyPostfix]
        private static void HSceneSpriteAccessoryCondition_SetAccessoryCharacter_Postfix()
        {
            MoreAccessories._self.UpdateUI();
        }

        [HarmonyPatch(typeof(HSceneSpriteAccessoryCondition), "OnClickAllAccessory"), HarmonyPostfix]
        private static void HSceneSpriteAccessoryCondition_OnClickAllAccessory_Postfix()
        {
            MoreAccessories._self.UpdateUI();
        }
#endif
    }
}
