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

namespace MoreAccessoriesPH.Patches
{
    public static class AccessoryCustomEdit_Patches
    {
        private static readonly HarmonyExtensions.Replacement[] _replacements = 
        {
            //acceParam.slot
            new HarmonyExtensions.Replacement
            {
                pattern = new []
                {
                    new CodeInstruction(OpCodes.Ldfld, typeof(AccessoryParameter).GetField("slot", BindingFlags.Public | BindingFlags.Instance)), 
                },
                replacer = new []
                {
                    new CodeInstruction(OpCodes.Call, typeof(AccessoryCustomEdit_Patches).GetMethod(nameof(GetAcceParamSlotList), BindingFlags.NonPublic | BindingFlags.Static)), 
                }
            },
            //list[index]
            new HarmonyExtensions.Replacement
            {
                pattern = new []
                {
                    new CodeInstruction(OpCodes.Ldelem_Ref), 
                },
                replacer = new []
                {
                    new CodeInstruction(OpCodes.Call, typeof(AccessoryCustomEdit_Patches).GetMethod(nameof(GetListElement), BindingFlags.NonPublic | BindingFlags.Static)), 
                }
            },
            //list[index] = value
            new HarmonyExtensions.Replacement
            {
                pattern = new []
                {
                    new CodeInstruction(OpCodes.Stelem_Ref), 
                },
                replacer = new []
                {
                    new CodeInstruction(OpCodes.Call, typeof(AccessoryCustomEdit_Patches).GetMethod(nameof(SetListElement), BindingFlags.NonPublic | BindingFlags.Static)), 
                }
            },
            //[].length
            new HarmonyExtensions.Replacement
            {
                pattern = new []
                {
                    new CodeInstruction(OpCodes.Ldlen), 
                },
                replacer = new []
                {
                    new CodeInstruction(OpCodes.Call, typeof(AccessoryCustomEdit_Patches).GetMethod(nameof(GetListCount), BindingFlags.NonPublic | BindingFlags.Static)), 
                }
            },
        };

        #region Methods
        public static void PatchAll(Harmony harmony)
        {
            HarmonyMethod transpiler = new HarmonyMethod(typeof(AccessoryCustomEdit_Patches), nameof(GeneralTranspiler), new[] {typeof(IEnumerable<CodeInstruction>)});

            foreach (MethodInfo methodInfo in typeof(AccessoryCustomEdit).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (methodInfo.DeclaringType != typeof(AccessoryCustomEdit))
                    continue;
                try
                {
                    harmony.Patch(methodInfo, transpiler: transpiler);
                }
                catch (Exception e)
                {
                    Debug.LogError("MoreAccessories: Could not patch:\n" + e);
                }
            }
            harmony.Patch(AccessTools.Method(typeof(AccessoryCustomEdit), "LoadedCoordinate"), postfix: new HarmonyMethod(typeof(AccessoryCustomEdit_Patches), nameof(AccessoryCustomEdit_LoadedCoordinate_Postfix)));
        }

        private static IEnumerable<CodeInstruction> GeneralTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return HarmonyExtensions.ReplaceCodePattern(instructions, _replacements);
        }
        #endregion

        #region Patches
        private static void AccessoryCustomEdit_LoadedCoordinate_Postfix(AccessoryCustomEdit __instance, Human ___human)
        {
            MoreAccessories.AdditionalData data = MoreAccessories._self.GetAdditionalData(___human.customParam);
            if (data != null)
            {
                for (int i = 0; i < data.accessories.Count; i++)
                    __instance.CallPrivate("UpdateSlotUI", i + 10);
            }
        }
        #endregion

        #region Replacers
        private static object GetListElement(IList list, int i)
        {
            return list[i];
        }

        private static void SetListElement(IList list, int i, object obj)
        {
            list[i] = obj;
        }
        
        private static int GetListCount(IList list)
        {
            return list.Count;
        }

        private static PseudoAggregateList<AccessoryCustom> GetAcceParamSlotList(AccessoryParameter acceParam)
        {
            return new PseudoAggregateList<AccessoryCustom>(i => acceParam.slot[i], i => MoreAccessories._self.GetAdditionalData(acceParam).accessories[i].accessoryCustom,
                    (i, obj) => acceParam.slot[i] = obj, (i, obj) => MoreAccessories._self.GetAdditionalData(acceParam).accessories[i].accessoryCustom = obj,
                    () => acceParam.slot.Length, () =>
                    {
                        MoreAccessories.AdditionalData data = MoreAccessories._self.GetAdditionalData(acceParam);
                        return data != null ? data.accessories.Count : 0;
                    });
        }
        #endregion
    }
}
