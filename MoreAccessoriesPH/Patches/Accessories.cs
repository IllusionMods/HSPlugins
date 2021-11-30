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
    public static class Accessories_Patches
    {
        private static readonly HarmonyExtensions.Replacement[] _replacements =
        {
            //this.acceObjs
            new HarmonyExtensions.Replacement
            {
                pattern = new []
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, typeof(Accessories).GetField("acceObjs", BindingFlags.NonPublic | BindingFlags.Instance)),
                },
                replacer = new []
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call, typeof(Accessories_Patches).GetMethod(nameof(GetAcceObjList), BindingFlags.NonPublic | BindingFlags.Static)),
                }
            },
            //this.showFlags
            new HarmonyExtensions.Replacement
            {
                pattern = new []
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, typeof(Accessories).GetField("showFlags", BindingFlags.NonPublic | BindingFlags.Instance)),
                },
                replacer = new []
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call, typeof(Accessories_Patches).GetMethod(nameof(GetShowFlagsList), BindingFlags.NonPublic | BindingFlags.Static)),
                }
            },
            //acceParam.slot
            new HarmonyExtensions.Replacement
            {
                pattern = new []
                {
                    new CodeInstruction(OpCodes.Ldfld, typeof(AccessoryParameter).GetField("slot", BindingFlags.Public | BindingFlags.Instance)),
                },
                replacer = new []
                {
                    new CodeInstruction(OpCodes.Call, typeof(Accessories_Patches).GetMethod(nameof(GetAcceParamSlotList), BindingFlags.NonPublic | BindingFlags.Static)),
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
                    new CodeInstruction(OpCodes.Call, typeof(Accessories_Patches).GetMethod(nameof(GetListElement), BindingFlags.NonPublic | BindingFlags.Static)),
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
                    new CodeInstruction(OpCodes.Call, typeof(Accessories_Patches).GetMethod(nameof(SetListElement), BindingFlags.NonPublic | BindingFlags.Static)),
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
                    new CodeInstruction(OpCodes.Call, typeof(Accessories_Patches).GetMethod(nameof(GetListCount), BindingFlags.NonPublic | BindingFlags.Static)),
                }
            },
        };
        private static object _acceObjsCached;

        #region Methods
        public static void PatchAll(Harmony harmony)
        {
            HarmonyMethod transpiler = new HarmonyMethod(typeof(Accessories_Patches), nameof(GeneralTranspiler), new[] { typeof(IEnumerable<CodeInstruction>) });

            foreach (MethodInfo methodInfo in typeof(Accessories).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (methodInfo.DeclaringType != typeof(Accessories))
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

            harmony.Patch(typeof(Accessories).GetNestedType("AcceObj", BindingFlags.NonPublic).GetMethod("UpdateColorCustom", BindingFlags.Public | BindingFlags.Instance), transpiler: transpiler);
            harmony.Patch(AccessTools.PropertyGetter(typeof(Accessories), "objAcs"), new HarmonyMethod(typeof(Accessories_Patches), nameof(Accessories_get_objAcs_Prefix)));

            //HoneyPot stuff
            MoreAccessories._self.ExecuteDelayed(() =>
            {
                Type honeyPot = Type.GetType("ClassLibrary4.HoneyPot,HoneyPot");
                if (honeyPot != null)
                {
                    harmony.Patch(AccessTools.Method(honeyPot, "setAccsShader"), new HarmonyMethod(typeof(Accessories_Patches), nameof(HoneyPot_setAccsShader_Prefix)), new HarmonyMethod(typeof(Accessories_Patches), nameof(HoneyPot_setAccsShader_Postfix)));
                }
            }, 60);
        }

        private static IEnumerable<CodeInstruction> GeneralTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return HarmonyExtensions.ReplaceCodePattern(instructions, _replacements);
        }
        #endregion

        #region Patches
        private static bool Accessories_get_objAcs_Prefix(Accessories __instance, ref GameObject[] __result)
        {
            __result = GetAcceObjList(__instance).Select(v => v != null ? (GameObject)v.GetPrivate("obj") : null).ToArray();
            return false;
        }

        private static void HoneyPot_setAccsShader_Prefix(Human h)
        {
            if (h.accessories != null)
            {
                _acceObjsCached = h.accessories.GetPrivate("acceObjs");

                PseudoAggregateList<object> list = GetAcceObjList(h.accessories);
                Array arr = (Array)Activator.CreateInstance(_acceObjsCached.GetType(), list.Count);
                for (int i = 0; i < arr.Length; i++)
                    arr.SetValue(list[i], i);
                h.accessories.SetPrivate("acceObjs", arr);
            }
        }

        private static void HoneyPot_setAccsShader_Postfix(Human h)
        {
            if (h.accessories != null)
                h.accessories.SetPrivate("acceObjs", _acceObjsCached);
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

        private static PseudoAggregateList<object> GetAcceObjList(Accessories accessories)
        {
            Array acceObjs = (Array)accessories.GetPrivate("acceObjs");
            return new PseudoAggregateList<object>(acceObjs.GetValue, i => MoreAccessories._self.GetAdditionalData(accessories).accessories[i].acceObj,
                    (i, obj) => acceObjs.SetValue(obj, i), (i, obj) => MoreAccessories._self.GetAdditionalData(accessories).accessories[i].acceObj = obj,
                    () => acceObjs.Length, () =>
                    {
                        MoreAccessories.AdditionalData data = MoreAccessories._self.GetAdditionalData(accessories);
                        return data != null ? data.accessories.Count : 0;
                    });
        }

        private static bool[] GetShowFlagsList(Accessories accessories)
        {
            //TODO make that better
            List<bool> showFlags = ((bool[])accessories.GetPrivate("showFlags")).ToList();
            showFlags.AddRange(MoreAccessories._self.GetAdditionalData(accessories).accessories.Select(e => e.show));
            return showFlags.ToArray();
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
