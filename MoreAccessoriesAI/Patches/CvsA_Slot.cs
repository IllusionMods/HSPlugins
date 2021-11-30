using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using AIChara;
using CharaCustom;
using HarmonyLib;
using ToolBox.Extensions;
using UnityEngine;

namespace MoreAccessoriesAI.Patches
{
    public static class CvsA_Slot_Patches
    {

        private static readonly List<HarmonyExtensions.Replacement> _replacements = new List<HarmonyExtensions.Replacement>()
        {
            //For anonymous methods
            new HarmonyExtensions.Replacement() //nowAcs.parts[]
            {
                pattern = new[]
                {
                    new CodeInstruction(OpCodes.Call, typeof(CvsBase).GetMethod("get_nowAcs", AccessTools.all)),
                    new CodeInstruction(OpCodes.Callvirt, typeof(ChaFileAccessory).GetMethod("get_parts", AccessTools.all)),
                },
                replacer = new[]
                {
                    new CodeInstruction(OpCodes.Nop),
                    new CodeInstruction(OpCodes.Call, typeof(CvsA_Slot_Patches).GetMethod(nameof(GetPartsInfoList), AccessTools.all)),
                }
            },
            new HarmonyExtensions.Replacement() //chaCtrl.cmpAccessory[]
            {
                pattern = new[]
                {
                    new CodeInstruction(OpCodes.Call, typeof(CvsBase).GetMethod("get_chaCtrl", AccessTools.all)),
                    new CodeInstruction(OpCodes.Callvirt, typeof(ChaInfo).GetMethod("get_cmpAccessory", AccessTools.all)),
                },
                replacer = new[]
                {
                    new CodeInstruction(OpCodes.Nop),
                    new CodeInstruction(OpCodes.Call, typeof(CvsA_Slot_Patches).GetMethod(nameof(GetCmpAccessoryList), AccessTools.all)),
                }
            },
            new HarmonyExtensions.Replacement() //orgAcs.parts[]
            {
                pattern = new[]
                {
                    new CodeInstruction(OpCodes.Call, typeof(CvsBase).GetMethod("get_orgAcs", AccessTools.all)),
                    new CodeInstruction(OpCodes.Callvirt, typeof(ChaFileAccessory).GetMethod("get_parts", AccessTools.all)),
                },
                replacer = new[]
                {
                    new CodeInstruction(OpCodes.Nop),
                    new CodeInstruction(OpCodes.Call, typeof(CvsA_Slot_Patches).GetMethod(nameof(GetOrgPartsInfoList), AccessTools.all)),
                }
            },

            new HarmonyExtensions.Replacement() //[this.SNo]
            {
                pattern = new[]
                {
                    new CodeInstruction(OpCodes.Call, typeof(CvsBase).GetMethod("get_SNo", AccessTools.all)),
                    new CodeInstruction(OpCodes.Ldelem_Ref)
                },
                replacer = new[]
                {
                    new CodeInstruction(OpCodes.Call, typeof(CvsBase).GetMethod("get_SNo", AccessTools.all)),
                    new CodeInstruction(OpCodes.Call, typeof(CvsA_Slot_Patches).GetMethod(nameof(GetIndexFromList), AccessTools.all)),
                }
            },

        };

        public static void PatchAll(Harmony harmony)
        {
            HarmonyMethod transpiler = new HarmonyMethod(typeof(CvsA_Slot_Patches), nameof(GeneralTranspiler), new[] {typeof(IEnumerable<CodeInstruction>)});
            HashSet<Type> ignoredTypes = new HashSet<Type>()
            {
                typeof(MonoBehaviour),
                typeof(Behaviour),
                typeof(Transform),
                typeof(GameObject),
                typeof(Component),
                typeof(CvsBase),
                typeof(UnityEngine.Object),
                typeof(System.Object)
            };

            foreach (MethodInfo methodInfo in typeof(CvsA_Slot).GetMethods(AccessTools.all))
            {
                try
                {
                    if (ignoredTypes.Contains(methodInfo.DeclaringType))
                        continue;
                    harmony.Patch(methodInfo, transpiler: transpiler);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError("MoreAccessories: Could not patch:\n" + e);
                }
            }

            foreach (MethodInfo methodInfo in typeof(CvsA_Slot).GetNestedTypes(BindingFlags.NonPublic).SelectMany(t => t.GetMethods(AccessTools.all)))
            {
                try
                {
                    if (ignoredTypes.Contains(methodInfo.DeclaringType))
                        continue;
                    harmony.Patch(methodInfo, transpiler: transpiler);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError("MoreAccessories: Could not patch:\n" + e);
                }
            }
            harmony.PatchAll(typeof(CvsA_Slot_Patches));
        }

        private static IEnumerable<CodeInstruction> GeneralTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return HarmonyExtensions.ReplaceCodePattern(instructions, _replacements);
        }

        #region Replacers
        private static IList<ChaFileAccessory.PartsInfo> GetPartsInfoList(CvsA_Slot self)
        {
            return new ReadOnlyListExtender<ChaFileAccessory.PartsInfo>(CustomBase.Instance.chaCtrl.nowCoordinate.accessory.parts, MoreAccessories._self._makerAdditionalData.parts);
        }

        private static IList<ChaFileAccessory.PartsInfo> GetOrgPartsInfoList(CvsA_Slot self)
        {
            return new ReadOnlyListExtender<ChaFileAccessory.PartsInfo>(CustomBase.Instance.chaCtrl.chaFile.coordinate.accessory.parts, MoreAccessories._self._makerAdditionalData.parts);
        }

        private static IList<CmpAccessory> GetCmpAccessoryList(CvsA_Slot self)
        {
            return new ReadOnlyListExtender<CmpAccessory>(CustomBase.Instance.chaCtrl.cmpAccessory, MoreAccessories._self._makerAdditionalData.objects.Select(e => e.cmp).ToList());
        }

        private static object GetIndexFromList(IList<object> list, int index)
        {
            if (index < list.Count)
                return list[index];
            return null;
        }

        #endregion

        #region Manual Patches
        [HarmonyPatch(typeof(CvsA_Slot), "UpdateCustomUI"), HarmonyPrefix]
        private static void UpdateCustomUI_Prefix(CvsA_Slot __instance)
        {
            if (MoreAccessories._self._makerAdditionalData != null && __instance.SNo >= MoreAccessories._self._makerAdditionalData.parts.Count + 20)
                __instance.SNo = MoreAccessories._self._makerAdditionalData.parts.Count + 19;
        }
        #endregion
    }
}
