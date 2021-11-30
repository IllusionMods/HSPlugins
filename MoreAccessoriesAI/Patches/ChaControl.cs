using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using AIChara;
using HarmonyLib;
using ToolBox.Extensions;
using UnityEngine;

namespace MoreAccessoriesAI.Patches
{
    public static class ChaControl_Patches
    {
        #region Private Variables
        private static readonly List<HarmonyExtensions.Replacement> _replacements = new List<HarmonyExtensions.Replacement>()
        {
            //nowCoordinate.accessory.parts[slotNo]
            new HarmonyExtensions.Replacement()
            {
                pattern = new[]
                {
                    new CodeInstruction(OpCodes.Call, typeof(ChaControl).GetMethod("get_nowCoordinate", AccessTools.all)),
                    new CodeInstruction(OpCodes.Ldfld, typeof(ChaFileCoordinate).GetField("accessory", AccessTools.all)),
                    new CodeInstruction(OpCodes.Callvirt, typeof(ChaFileAccessory).GetMethod("get_parts", AccessTools.all)),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldelem_Ref)
                },
                replacer = new[]
                {
                    new CodeInstruction(OpCodes.Nop),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Call, typeof(ChaControl_Patches).GetMethod(nameof(GetPartsInfo), AccessTools.all)),
                    new CodeInstruction(OpCodes.Nop),
                    new CodeInstruction(OpCodes.Nop),
                }
            },
            //cmpAccessory[slotNo]
            new HarmonyExtensions.Replacement() 
            {
                pattern = new[]
                {
                    new CodeInstruction(OpCodes.Call, typeof(ChaInfo).GetMethod("get_cmpAccessory", AccessTools.all)),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldelem_Ref)
                },
                replacer = new[]
                {
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Call, typeof(ChaControl_Patches).GetMethod(nameof(GetCmpAccessory), AccessTools.all)),
                    new CodeInstruction(OpCodes.Nop),
                }
            },
            //cmpAccessory[slotNo] (2)
            new HarmonyExtensions.Replacement() 
            {
                pattern = new[]
                {
                    new CodeInstruction(OpCodes.Call, typeof(ChaInfo).GetMethod("get_cmpAccessory", AccessTools.all)),
                    new CodeInstruction(OpCodes.Ldarg_S, 4),
                    new CodeInstruction(OpCodes.Ldelem_Ref)
                },
                replacer = new[]
                {
                    new CodeInstruction(OpCodes.Ldarg_S, 4),
                    new CodeInstruction(OpCodes.Call, typeof(ChaControl_Patches).GetMethod(nameof(GetCmpAccessory), AccessTools.all)),
                    new CodeInstruction(OpCodes.Nop),
                }
            },
            //objAccessory[slotNo]
            new HarmonyExtensions.Replacement() 
            {
                pattern = new[]
                {
                    new CodeInstruction(OpCodes.Call, typeof(ChaInfo).GetMethod("get_objAccessory", AccessTools.all)),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldelem_Ref)
                },
                replacer = new[]
                {
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Call, typeof(ChaControl_Patches).GetMethod(nameof(GetObjAccessory), AccessTools.all)),
                    new CodeInstruction(OpCodes.Nop),
                }
            },
            //infoAccessory[slotNo]
            new HarmonyExtensions.Replacement() 
            {
                pattern = new[]
                {
                    new CodeInstruction(OpCodes.Call, typeof(ChaInfo).GetMethod("get_infoAccessory", AccessTools.all)),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldelem_Ref)
                },
                replacer = new[]
                {
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Call, typeof(ChaControl_Patches).GetMethod(nameof(GetInfoAccessory), AccessTools.all)),
                    new CodeInstruction(OpCodes.Nop),
                }
            },
            //trfAcsMove[,]
            new HarmonyExtensions.Replacement() 
            {
                pattern = new[]
                {
                    new CodeInstruction(OpCodes.Call, typeof(ChaInfo).GetMethod("get_trfAcsMove", AccessTools.all)),
                },
                replacer = new[]
                {
                    new CodeInstruction(OpCodes.Nop),
                }
            },
            //Transform[,].Get
            new HarmonyExtensions.Replacement() 
            {
                pattern = new[]
                {
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Transform[,]), "Get", new []{typeof(int), typeof(int)})),
                },
                replacer = new[]
                {
                    new CodeInstruction(OpCodes.Call, typeof(ChaControl_Patches).GetMethod(nameof(GetTrfAcsMove), AccessTools.all)),
                }
            },
            //Transform[,].Set
            new HarmonyExtensions.Replacement() 
            {
                pattern = new[]
                {
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Transform[,]), "Set", new []{typeof(int), typeof(int), typeof(Transform)})),
                },
                replacer = new[]
                {
                    new CodeInstruction(OpCodes.Call, typeof(ChaControl_Patches).GetMethod(nameof(SetTrfAcsMove), AccessTools.all)),
                }
            },
            //MathfEx.RangeEqualOn<int>
            new HarmonyExtensions.Replacement() 
            {
                pattern = new[]
                {
                    new CodeInstruction(OpCodes.Call, typeof(MathfEx).GetMethod("RangeEqualOn", BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(typeof(int))),
                },
                replacer = new[]
                {
                    new CodeInstruction(OpCodes.Call, typeof(ChaControl_Patches).GetMethod(nameof(MathfEx_RangeEqualOn), AccessTools.all)),
                }
            },
        };
        #endregion

        #region Methods
        public static void PatchAll(Harmony harmony)
        {
            HarmonyMethod transpiler = new HarmonyMethod(typeof(ChaControl_Patches), nameof(GeneralTranspiler), new[] {typeof(IEnumerable<CodeInstruction>)});

            foreach (MethodInfo methodInfo in typeof(ChaControl).GetMethods(AccessTools.all))
            {
                if (methodInfo.GetParameters().Any(p => p.Name.Equals("slotNo") && p.ParameterType == typeof(int)) == false)
                    continue;
                try
                {
                    harmony.Patch(methodInfo, transpiler: transpiler);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError("MoreAccessories: Could not patch:\n" + e);
                }
            }
            harmony.PatchAll(typeof(ChaControl_Patches));
        }

        private static IEnumerable<CodeInstruction> GeneralTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return HarmonyExtensions.ReplaceCodePattern(instructions, _replacements);
        }
        #endregion

        #region Replacers
        private static ChaFileAccessory.PartsInfo GetPartsInfo(ChaControl self, int slotNo)
        {
            if (slotNo < 20)
                return self.nowCoordinate.accessory.parts[slotNo];
            return MoreAccessories._self.GetAdditionalDataByCharacter(self.chaFile).parts[slotNo - 20];
        }

        private static CmpAccessory GetCmpAccessory(ChaControl self, int slotNo)
        {
            if (slotNo < 20)
                return self.cmpAccessory[slotNo];
            return MoreAccessories._self.GetAdditionalDataByCharacter(self.chaFile).objects[slotNo - 20].cmp;
        }

        private static GameObject GetObjAccessory(ChaControl self, int slotNo)
        {
            if (slotNo < 20)
                return self.objAccessory[slotNo];
            return MoreAccessories._self.GetAdditionalDataByCharacter(self.chaFile).objects[slotNo - 20].obj;
        }

        private static ListInfoBase GetInfoAccessory(ChaControl self, int slotNo)
        {
            if (slotNo < 20)
                return self.infoAccessory[slotNo];
            return MoreAccessories._self.GetAdditionalDataByCharacter(self.chaFile).objects[slotNo - 20].info;
        }

        private static Transform GetTrfAcsMove(ChaControl self, int slotNo, int i)
        {
            if (slotNo < 20)
                return self.trfAcsMove[slotNo, i];
            return MoreAccessories._self.GetAdditionalDataByCharacter(self.chaFile).objects[slotNo - 20].move[i];
        }

        private static void SetTrfAcsMove(ChaControl self, int slotNo, int i, Transform t)
        {
            if (slotNo < 20)
                self.trfAcsMove[slotNo, i] = t;
            else
                MoreAccessories._self.GetAdditionalDataByCharacter(self.chaFile).objects[slotNo - 20].move[i] = t;
        }

        private static bool MathfEx_RangeEqualOn(int min, int n, int max)
        {
            if (min == 0 && max == 19)
                return true;
            return MathfEx.RangeEqualOn(min, n, max);
        }
        #endregion

        #region Manual Patches
        [HarmonyPatch(typeof(ChaControl), "Initialize"), HarmonyPostfix]
        private static void ChaControl_Initialize_Postfix(ChaControl __instance)
        {
            MoreAccessories._self._charControlByChar.Add(__instance.chaFile, __instance);
        }
        [HarmonyPatch(typeof(ChaControl), "OnDestroy"), HarmonyPrefix]
        private static void ChaControl_OnDestroy_Prefix(ChaControl __instance)
        {
            if (MoreAccessories._self._charControlByChar.ContainsKey(__instance.chaFile))
                MoreAccessories._self._charControlByChar.Remove(__instance.chaFile);
            MoreAccessories._self.ExecuteDelayed(MoreAccessories._self.PurgeUselessEntries);
        }

        [HarmonyPatch(typeof(ChaControl), "LateUpdateForce"), HarmonyPostfix]
        private static void LateUpdateForce_Postfix(ChaControl __instance)
        {
            if (MoreAccessories._self._charAdditionalData.TryGetValue(__instance.chaFile, out MoreAccessories.AdditionalData additionalData) == false)
                return;
            if (!__instance.IsVisibleInCamera)
            {
                foreach (MoreAccessories.AdditionalData.AccessoryObject o in additionalData.objects)
                {
                    if (o.cmp != null)
                        o.cmp.EnableDynamicBones(false);
                }
            }
            else
            {
                for (int i = 0; i < additionalData.objects.Count; i++)
                {
                    MoreAccessories.AdditionalData.AccessoryObject o = MoreAccessories._self.GetAdditionalDataByCharacter(__instance.chaFile).objects[i];
                    if (o.cmp != null)
                    {
                        ChaFileAccessory.PartsInfo part = MoreAccessories._self.GetAdditionalDataByCharacter(__instance.chaFile).parts[i];
                        o.cmp.EnableDynamicBones(part.noShake && o.cmp.isVisible);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ChaControl), "SetAccessoryState", typeof(int), typeof(bool)), HarmonyPrefix]
        private static bool SetAccessoryState_Prefix(ChaControl __instance, int slotNo, bool show)
        {
            if (slotNo < 20)
                return true;
            MoreAccessories._self.GetAdditionalDataByCharacter(__instance.chaFile).objects[slotNo - 20].show = show;
            return false;
        }

        [HarmonyPatch(typeof(ChaControl), "SetAccessoryStateAll", typeof(bool)), HarmonyPostfix]
        private static void SetAccessoryStateAll_Postfix(ChaControl __instance, bool show)
        {
            if (MoreAccessories._self._charAdditionalData.TryGetValue(__instance.chaFile, out MoreAccessories.AdditionalData additionalData) == false)
                return;
            foreach (MoreAccessories.AdditionalData.AccessoryObject o in additionalData.objects)
                o.show = show;
        }


        [HarmonyPatch(typeof(ChaControl), "SetAccessoryPos", typeof(int), typeof(int), typeof(float), typeof(bool), typeof(int)), HarmonyPrefix]
        private static bool SetAccessoryPos_Prefix(ChaControl __instance, int slotNo, int correctNo, float value, bool add, int flags, ref bool __result)
        {
            if (slotNo < 20)
                return true;
            slotNo -= 20;
            MoreAccessories.AdditionalData additionalData = MoreAccessories._self.GetAdditionalDataByCharacter(__instance.chaFile);
            Transform transform = additionalData.objects[slotNo].move[correctNo];
            if (null == transform)
            {
                __result = false;
                return false;
            }
            ChaFileAccessory.PartsInfo part = additionalData.parts[slotNo];
            if ((flags & 1) != 0)
                part.addMove[correctNo, 0].x = float.Parse(((!add ? 0f : part.addMove[correctNo, 0].x) + value).ToString("f1"));
            if ((flags & 2) != 0)
                part.addMove[correctNo, 0].y = float.Parse(((!add ? 0f : part.addMove[correctNo, 0].y) + value).ToString("f1"));
            if ((flags & 4) != 0)
                part.addMove[correctNo, 0].z = float.Parse(((!add ? 0f : part.addMove[correctNo, 0].z) + value).ToString("f1"));
            transform.localPosition = new Vector3(part.addMove[correctNo, 0].x * 0.1f, part.addMove[correctNo, 0].y * 0.1f, part.addMove[correctNo, 0].z * 0.1f);
            __result = true;
            return false;
        }

        [HarmonyPatch(typeof(ChaControl), "SetAccessoryRot", typeof(int), typeof(int), typeof(float), typeof(bool), typeof(int)), HarmonyPrefix]
        private static bool SetAccessoryRot_Prefix(ChaControl __instance, int slotNo, int correctNo, float value, bool add, int flags, ref bool __result)
        {
            if (slotNo < 20)
                return true;
            slotNo -= 20;
            MoreAccessories.AdditionalData additionalData = MoreAccessories._self.GetAdditionalDataByCharacter(__instance.chaFile);
            Transform transform = additionalData.objects[slotNo].move[correctNo];
            if (null == transform)
            {
                __result = false;
                return false;
            }
            ChaFileAccessory.PartsInfo part = additionalData.parts[slotNo];
            if ((flags & 1) != 0)
            {
                float t = (int)((!add ? 0f : part.addMove[correctNo, 1].x) + value);
                part.addMove[correctNo, 1].x = Mathf.Repeat(t, 360f);
            }
            if ((flags & 2) != 0)
            {
                float t2 = (int)((!add ? 0f : part.addMove[correctNo, 1].y) + value);
                part.addMove[correctNo, 1].y = Mathf.Repeat(t2, 360f);
            }
            if ((flags & 4) != 0)
            {
                float t3 = (int)((!add ? 0f : part.addMove[correctNo, 1].z) + value);
                part.addMove[correctNo, 1].z = Mathf.Repeat(t3, 360f);
            }
            transform.localEulerAngles = new Vector3(part.addMove[correctNo, 1].x, part.addMove[correctNo, 1].y, part.addMove[correctNo, 1].z);
            __result = true;
            return false;
        }

        [HarmonyPatch(typeof(ChaControl), "SetAccessoryScl", typeof(int), typeof(int), typeof(float), typeof(bool), typeof(int)), HarmonyPrefix]
        private static bool SetAccessoryScl_Prefix(ChaControl __instance, int slotNo, int correctNo, float value, bool add, int flags, ref bool __result)
        {
            if (slotNo < 20)
                return true;
            slotNo -= 20;
            MoreAccessories.AdditionalData additionalData = MoreAccessories._self.GetAdditionalDataByCharacter(__instance.chaFile);
            Transform transform = additionalData.objects[slotNo].move[correctNo];
            if (null == transform)
            {
                __result = false;
                return false;
            }
            ChaFileAccessory.PartsInfo part = additionalData.parts[slotNo];
            if ((flags & 1) != 0)
                part.addMove[correctNo, 2].x = float.Parse(((!add ? 0f : part.addMove[correctNo, 2].x) + value).ToString("f2"));
            if ((flags & 2) != 0)
                part.addMove[correctNo, 2].y = float.Parse(((!add ? 0f : part.addMove[correctNo, 2].y) + value).ToString("f2"));
            if ((flags & 4) != 0)
                part.addMove[correctNo, 2].z = float.Parse(((!add ? 0f : part.addMove[correctNo, 2].z) + value).ToString("f2"));
            transform.localScale = new Vector3(part.addMove[correctNo, 2].x, part.addMove[correctNo, 2].y, part.addMove[correctNo, 2].z);
            __result = true;
            return false;
        }

        [HarmonyPatch(typeof(ChaControl), "UpdateAccessoryMoveFromInfo", typeof(int)), HarmonyPrefix]
        private static bool UpdateAccessoryMoveFromInfo_Prefix(ChaControl __instance, int slotNo, ref bool __result)
        {
            if (slotNo < 20)
                return true;
            slotNo -= 20;
            MoreAccessories.AdditionalData additionalData = MoreAccessories._self.GetAdditionalDataByCharacter(__instance.chaFile);
            ChaFileAccessory.PartsInfo part = additionalData.parts[slotNo];
            for (int i = 0; i < 2; i++)
            {
                Transform transform = additionalData.objects[slotNo].move[i];
                if (!(null == transform))
                {
                    transform.localPosition = new Vector3(part.addMove[i, 0].x * 0.1f, part.addMove[i, 0].y * 0.1f, part.addMove[i, 0].z * 0.1f);
                    transform.localEulerAngles = new Vector3(part.addMove[i, 1].x, part.addMove[i, 1].y, part.addMove[i, 1].z);
                    transform.localScale = new Vector3(part.addMove[i, 2].x, part.addMove[i, 2].y, part.addMove[i, 2].z);
                }
            }
            __result = true;
            return false;
        }

        [HarmonyPatch(typeof(ChaControl), "UpdateAccessoryMoveAllFromInfo"), HarmonyPostfix]
        private static void UpdateAccessoryMoveAllFromInfo_Postfix(ChaControl __instance)
        {
            List<ChaFileAccessory.PartsInfo> list = MoreAccessories._self.GetAdditionalDataByCharacter(__instance.chaFile).parts;
            for (int i = 0; i < list.Count; i++)
                __instance.UpdateAccessoryMoveFromInfo(i + 20);
        }


        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), "ChangeAccessory", typeof(bool))]
        private static void ChangeAccessory_Postfix(ChaControl __instance, bool forceChange)
        {
            if (MoreAccessories._self._charAdditionalData.TryGetValue(__instance.chaFile, out MoreAccessories.AdditionalData additionalData) == false)
                return;
            for (int i = 0; i < additionalData.parts.Count; i++)
            {
                ChaFileAccessory.PartsInfo part = MoreAccessories._self.GetAdditionalDataByCharacter(__instance.chaFile).parts[i];
                ChangeAccessoryAsync_Prefix(__instance, i, part.type, part.id, part.parentKey, forceChange);
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), "ChangeAccessory", typeof(int), typeof(int), typeof(int), typeof(string), typeof(bool))]
        private static bool ChangeAccessory_Prefix(ChaControl __instance, int slotNo, int type, int id, string parentKey, bool forceChange)
        {
            if (slotNo < 20)
                return true;
            ChangeAccessoryAsync_Prefix(__instance, slotNo - 20, type, id, parentKey, forceChange);
            return false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), "ChangeAccessoryAsync", typeof(bool))]
        private static void ChangeAccessoryAsync_Postfix(ChaControl __instance, bool forceChange)
        {
            if (MoreAccessories._self._charAdditionalData.TryGetValue(__instance.chaFile, out MoreAccessories.AdditionalData additionalData) == false)
                return;
            for (int i = 0; i < additionalData.parts.Count; i++)
            {
                ChaFileAccessory.PartsInfo part = MoreAccessories._self.GetAdditionalDataByCharacter(__instance.chaFile).parts[i];
                ChangeAccessoryAsync_Prefix(__instance, i, part.type, part.id, part.parentKey, forceChange);
            }
        }

        private static void ChangeAccessoryAsync_Prefix(ChaControl __instance, int slotNo, int type, int id, string parentKey, bool forceChange)
        {
            MoreAccessories.AdditionalData additionalData = MoreAccessories._self.GetAdditionalDataByCharacter(__instance.chaFile);
            ChaFileAccessory.PartsInfo part = additionalData.parts[slotNo];
            MoreAccessories.AdditionalData.AccessoryObject dataObject = additionalData.objects[slotNo];

            ListInfoBase lib = null;
            bool load = true;
            bool release = true;
            if (type == 350 || !MathfEx.RangeEqualOn(351, type, 363))
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
                int num = dataObject.info != null ? dataObject.info.Category : -1;
                int num2 = dataObject.info != null ? dataObject.info.Id : -1;
                if (!forceChange && null != dataObject.obj && type == num && id == num2)
                {
                    load = false;
                    release = false;
                }
                if (id != -1)
                {
                    Dictionary<int, ListInfoBase> categoryInfo = __instance.lstCtrl.GetCategoryInfo((ChaListDefine.CategoryNo)type);
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
                }
            }
            if (release)
            {
                if (!load)
                {
                    part.MemberInit();
                    part.type = 350;
                }
                if (dataObject.obj)
                {
                    __instance.SafeDestroy(dataObject.obj);
                    dataObject.obj = null;
                    dataObject.info = null;
                    dataObject.cmp = null;
                    for (int i = 0; i < 2; i++)
                    {
                        dataObject.move[i] = null;
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
                    trfParent = __instance.objTop.transform;
                }
                dataObject.obj = (GameObject)__instance.CallPrivate("LoadCharaFbxData", type, id, "ca_slot" + (slotNo + 20).ToString("00"), false, weight, trfParent, -1, false);
                if (dataObject.obj)
                {
                    ListInfoComponent component = dataObject.obj.GetComponent<ListInfoComponent>();
                    lib = (dataObject.info = component.data);
                    dataObject.cmp = dataObject.obj.GetComponent<CmpAccessory>();
                    if (dataObject.cmp)
                        dataObject.cmp.InitDynamicBones();
                    //if (lib.GetInfo(ChaListDefine.KeyType.MainData) == "p_dummy" || null == dataObject.cmp) //wtf illusion
                    //{
                    //}
                    part.type = type;
                    part.id = lib.Id;
                    if (null != dataObject.cmp)
                    {
                        dataObject.move[0] = dataObject.cmp.trfMove01;
                        dataObject.move[1] = dataObject.cmp.trfMove02;
                    }
                }
            }
            if (dataObject.obj)
            {
                if (string.Empty == parentKey)
                    parentKey = lib.GetInfo(ChaListDefine.KeyType.Parent);
                __instance.ChangeAccessoryParent(slotNo + 20, parentKey);
                __instance.UpdateAccessoryMoveFromInfo(slotNo + 20);
                part.partsOfHead = ChaAccessoryDefine.CheckPartsOfHead(parentKey);

                if (dataObject.cmp != null && dataObject.cmp.typeHair)
                {
                    __instance.ChangeSettingHairTypeAccessoryShader(slotNo + 20);
                    __instance.ChangeHairTypeAccessoryColor(slotNo + 20);
                }
                else
                {
                    if (__instance.loadWithDefaultColorAndPtn)
                        __instance.SetAccessoryDefaultColor(slotNo + 20);
                    __instance.ChangeAccessoryColor(slotNo + 20);
                }
            }
        }
        #endregion
    }
}
