using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using AIChara;
using CharaCustom;
using HarmonyLib;
using Manager;
using MessagePack;
using ToolBox.Extensions;
using UnityEngine;

namespace MoreAccessoriesAI.Patches
{
    public static class CvsA_Copy_Patches
    {

        private static readonly List<HarmonyExtensions.Replacement> _replacements = new List<HarmonyExtensions.Replacement>()
        {
            new HarmonyExtensions.Replacement()
            {
                pattern = new[]
                {
                    new CodeInstruction(OpCodes.Call, typeof(CvsBase).GetMethod("get_nowAcs", AccessTools.all)),
                    new CodeInstruction(OpCodes.Callvirt, typeof(ChaFileAccessory).GetMethod("get_parts", AccessTools.all)),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, typeof(CvsA_Copy).GetField("selDst", AccessTools.all)),
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Call, typeof(MessagePackSerializer).GetMethods(AccessTools.all).FirstOrDefault(m =>
                    {
                        if (m.Name != "Deserialize")
                            return false;
                        ParameterInfo[] parameters = m.GetParameters();
                        return parameters.Length == 1 && parameters[0].ParameterType == typeof(byte[]);
                    }).MakeGenericMethod(typeof(ChaFileAccessory.PartsInfo))),
                    new CodeInstruction(OpCodes.Stelem_Ref),
                },
                replacer = new[]
                {
                    new CodeInstruction(OpCodes.Nop),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, typeof(CvsA_Copy).GetField("selDst", AccessTools.all)),
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Call, typeof(MessagePackSerializer).GetMethods(AccessTools.all).FirstOrDefault(m =>
                    {
                        if (m.Name != "Deserialize")
                            return false;
                        ParameterInfo[] parameters = m.GetParameters();
                        return parameters.Length == 1 && parameters[0].ParameterType == typeof(byte[]);
                    }).MakeGenericMethod(typeof(ChaFileAccessory.PartsInfo))),
                    new CodeInstruction(OpCodes.Call, typeof(CvsA_Copy_Patches).GetMethod(nameof(SetPartsInfo), AccessTools.all)),
                    new CodeInstruction(OpCodes.Nop),
                }
            },
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
                    new CodeInstruction(OpCodes.Call, typeof(CvsA_Copy_Patches).GetMethod(nameof(GetPartsInfoList), AccessTools.all)),
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
                    new CodeInstruction(OpCodes.Call, typeof(CvsA_Copy_Patches).GetMethod(nameof(GetCmpAccessoryList), AccessTools.all)),
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
                    new CodeInstruction(OpCodes.Call, typeof(CvsA_Copy_Patches).GetMethod(nameof(GetOrgPartsInfoList), AccessTools.all)),
                }
            },

            new HarmonyExtensions.Replacement() //[this.selSrc]
            {
                pattern = new[]
                {
                    new CodeInstruction(OpCodes.Ldfld, typeof(CvsA_Copy).GetField("selSrc", AccessTools.all)),
                    new CodeInstruction(OpCodes.Ldelem_Ref)
                },
                replacer = new[]
                {
                    new CodeInstruction(OpCodes.Ldfld, typeof(CvsA_Copy).GetField("selSrc", AccessTools.all)),
                    new CodeInstruction(OpCodes.Call, typeof(CvsA_Copy_Patches).GetMethod(nameof(GetIndexFromList), AccessTools.all)),
                }
            },

            new HarmonyExtensions.Replacement() //[this.selDst]
            {
                pattern = new[]
                {
                    new CodeInstruction(OpCodes.Ldfld, typeof(CvsA_Copy).GetField("selDst", AccessTools.all)),
                    new CodeInstruction(OpCodes.Ldelem_Ref)
                },
                replacer = new[]
                {
                    new CodeInstruction(OpCodes.Ldfld, typeof(CvsA_Copy).GetField("selDst", AccessTools.all)),
                    new CodeInstruction(OpCodes.Call, typeof(CvsA_Copy_Patches).GetMethod(nameof(GetIndexFromList), AccessTools.all)),
                }
            },

        };

        public static void PatchAll(Harmony harmony)
        {
            HarmonyMethod transpiler = new HarmonyMethod(typeof(CvsA_Copy_Patches), nameof(GeneralTranspiler), new[] { typeof(IEnumerable<CodeInstruction>) });
            HashSet<Type> ignoredTypes = new HashSet<Type>
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
            HashSet<MethodInfo> ignoredMethods = new HashSet<MethodInfo>
            {
                    typeof(CvsA_Copy).GetMethod("CalculateUI", AccessTools.all)
            };

            foreach (MethodInfo methodInfo in typeof(CvsA_Copy).GetMethods(AccessTools.all))
            {
                try
                {
                    if (ignoredTypes.Contains(methodInfo.DeclaringType) || ignoredMethods.Contains(methodInfo))
                        continue;
                    harmony.Patch(methodInfo, transpiler: transpiler);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError("MoreAccessories: Could not patch:\n" + e);
                }
            }

            foreach (MethodInfo methodInfo in typeof(CvsA_Copy).GetNestedTypes(BindingFlags.NonPublic).SelectMany(t => t.GetMethods(AccessTools.all)))
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
            harmony.PatchAll(typeof(CvsA_Copy_Patches));
        }

        private static IEnumerable<CodeInstruction> GeneralTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return HarmonyExtensions.ReplaceCodePattern(instructions, _replacements);
        }

        #region Replacers
        private static IList<ChaFileAccessory.PartsInfo> GetPartsInfoList(CvsA_Copy self)
        {
            return new ReadOnlyListExtender<ChaFileAccessory.PartsInfo>(CustomBase.Instance.chaCtrl.nowCoordinate.accessory.parts, MoreAccessories._self._makerAdditionalData.parts);
        }

        private static void SetPartsInfo(CvsA_Copy self, int index, ChaFileAccessory.PartsInfo part)
        {
            if (index < 20)
                CustomBase.Instance.chaCtrl.nowCoordinate.accessory.parts[index] = part;
            else
                MoreAccessories._self._makerAdditionalData.parts[index - 20] = part;
        }

        private static IList<ChaFileAccessory.PartsInfo> GetOrgPartsInfoList(CvsA_Copy self)
        {
            return new ReadOnlyListExtender<ChaFileAccessory.PartsInfo>(CustomBase.Instance.chaCtrl.chaFile.coordinate.accessory.parts, MoreAccessories._self._makerAdditionalData.parts);
        }

        private static IList<CmpAccessory> GetCmpAccessoryList(CvsA_Copy self)
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
        [HarmonyPatch(typeof(CvsA_Copy), "CalculateUI"), HarmonyPostfix]
        private static void CalculateUI_Postfix()
        {
            if (MoreAccessories._self._makerAdditionalData == null)
                return;
            for (int i = 0; i < MoreAccessories._self._makerAdditionalData.parts.Count && i < MoreAccessories._self._makerSlots.Count; i++)
            {
                ChaFileAccessory.PartsInfo part = MoreAccessories._self._makerAdditionalData.parts[i];
                ListInfoBase listInfo = Singleton<CustomBase>.Instance.chaCtrl.lstCtrl.GetListInfo((ChaListDefine.CategoryNo)part.type, part.id);
                MoreAccessories.MakerSlot slot = MoreAccessories._self._makerSlots[i];
                if (listInfo == null)
                {
                    slot.copyDstText.text = "なし";
                    slot.copySrcText.text = "なし";
                }
                else
                {
                    TextCorrectLimit.Correct(slot.copyDstText, listInfo.Name, "…");
                    slot.copySrcText.text = slot.copyDstText.text;
                }
            }
        }
        #endregion
    }
}
