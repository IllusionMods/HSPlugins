using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Character;
using HarmonyLib;
using MoreAccessoriesPH.Helpers;
using ToolBox.Extensions;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MoreAccessoriesPH.Patches
{
    public static class Various_Patches
    {
        #region Methods
        public static void PatchAll(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(Female), "Apply"), transpiler: new HarmonyMethod(typeof(Various_Patches), nameof(Human_Apply_Transpiler)));
            harmony.Patch(AccessTools.Method(typeof(Male), "Apply"), transpiler: new HarmonyMethod(typeof(Various_Patches), nameof(Human_Apply_Transpiler)));
            harmony.Patch(AccessTools.Method(typeof(Female), "ApplyCoordinate"), transpiler: new HarmonyMethod(typeof(Various_Patches), nameof(Human_Apply_Transpiler)));
            harmony.Patch(AccessTools.Method(typeof(Male), "ApplyCoordinate"), transpiler: new HarmonyMethod(typeof(Various_Patches), nameof(Human_Apply_Transpiler)));
            harmony.Patch(AccessTools.Method(typeof(AccessoryParameter), "Copy"), postfix: new HarmonyMethod(typeof(Various_Patches), nameof(AccessoryParameter_Copy_Postfix)));
            harmony.Patch(AccessTools.Method(typeof(Mannequin), "FromHuman"), transpiler: new HarmonyMethod(typeof(Various_Patches), nameof(Mannequin_Apply_Transpiler)));
            harmony.Patch(AccessTools.Method(typeof(HWearAcceChangeUI), "ChangeAcceShow_AllOn"), postfix: new HarmonyMethod(typeof(Various_Patches), nameof(HWearAcceChangeUI_ChangeAcceShow_AllOn_Postfix)));
            harmony.Patch(AccessTools.Method(typeof(HWearAcceChangeUI), "ChangeAcceShow_AllOff"), postfix: new HarmonyMethod(typeof(Various_Patches), nameof(HWearAcceChangeUI_ChangeAcceShow_AllOff_Postfix)));
        }
        #endregion

        #region Patches
        private static IEnumerable<CodeInstruction> Human_Apply_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo mi = typeof(Resources).GetMethod(nameof(Resources.UnloadUnusedAssets), BindingFlags.Public | BindingFlags.Static);
            List<CodeInstruction> codeInstructions = instructions.ToList();
            foreach (CodeInstruction instruction in codeInstructions)
            {
                if (instruction.opcode == OpCodes.Call && instruction.operand == mi)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, typeof(Various_Patches).GetMethod(nameof(InstantiateHumanAdditionalAccessories), BindingFlags.NonPublic | BindingFlags.Static));
                }
                yield return instruction;
            }
        }

        private static IEnumerable<CodeInstruction> Mannequin_Apply_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo mi = typeof(Resources).GetMethod(nameof(Resources.UnloadUnusedAssets), BindingFlags.Public | BindingFlags.Static);
            List<CodeInstruction> codeInstructions = instructions.ToList();
            foreach (CodeInstruction instruction in codeInstructions)
            {
                if (instruction.opcode == OpCodes.Call && instruction.operand == mi)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, typeof(Various_Patches).GetMethod(nameof(InstantiateMannequinAdditionalAccessories), BindingFlags.NonPublic | BindingFlags.Static));
                }
                yield return instruction;
            }
        }

        private static void InstantiateHumanAdditionalAccessories(Human human)
        {
            if (!MoreAccessories._self._charAdditionalData.TryGetValue(human.customParam, out MoreAccessories.AdditionalData additionalData))
                return;
            for (int i = 0; i < additionalData.accessories.Count; i++)
                human.accessories.AccessoryInstantiate(human.customParam.acce, i + 10, false, null);
        }

        private static void InstantiateMannequinAdditionalAccessories(Mannequin mannequin)
        {
            if (MoreAccessories._self._mannequinAdditionalData == null)
                return;
            for (int i = 0; i < MoreAccessories._self._mannequinAdditionalData.accessories.Count; i++)
                MoreAccessories._self._mannequinAccessories.AccessoryInstantiate(MoreAccessories._self._mannequinAccessoryParameter, i + 10, false, null);
        }

        private static void AccessoryParameter_Copy_Postfix(AccessoryParameter __instance, AccessoryParameter source)
        {
            if (__instance == source)
                return;
            MoreAccessories.AdditionalData sourceData = MoreAccessories._self.GetAdditionalData(source);
            MoreAccessories.AdditionalData destinationData = MoreAccessories._self.GetAdditionalData(__instance);
            if (sourceData == destinationData)
                return;
            if (destinationData != null)
            {
                if (sourceData != null)
                    destinationData.LoadFrom(sourceData);
                else
                    destinationData.Clear();
            }
        }

        private static void HWearAcceChangeUI_ChangeAcceShow_AllOn_Postfix(Female ___female)
        {
            MoreAccessories.AdditionalData data = MoreAccessories._self.GetAdditionalData(___female.customParam);
            if (data == null)
                return;
            for (int i = 0; i < data.accessories.Count; i++)
            {
                ___female.accessories.SetShow(i + 10, true);
            }
        }

        private static void HWearAcceChangeUI_ChangeAcceShow_AllOff_Postfix(Female ___female)
        {
            MoreAccessories.AdditionalData data = MoreAccessories._self.GetAdditionalData(___female.customParam);
            if (data == null)
                return;
            for (int i = 0; i < data.accessories.Count; i++)
            {
                ___female.accessories.SetShow(i + 10, false);
            }
        }
        #endregion
    }
}
