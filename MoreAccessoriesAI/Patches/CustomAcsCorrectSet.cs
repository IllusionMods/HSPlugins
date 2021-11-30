using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using AIChara;
using CharaCustom;
using HarmonyLib;
using ToolBox.Extensions;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace MoreAccessoriesAI.Patches
{
    public static class CustomAcsCorrectSet_Patches
    {

        #region Private Variables
        private static readonly List<HarmonyExtensions.Replacement> _replacements = new List<HarmonyExtensions.Replacement>()
        {
            //trfAcsMove[,]
            new HarmonyExtensions.Replacement()
            {
                pattern = new[]
                {
                    new CodeInstruction(OpCodes.Callvirt, typeof(ChaInfo).GetMethod("get_trfAcsMove", AccessTools.all)),
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
                    new CodeInstruction(OpCodes.Call, typeof(CustomAcsCorrectSet_Patches).GetMethod(nameof(GetTrfAcsMove), AccessTools.all)),
                }
            },
            new HarmonyExtensions.Replacement() //nowAcs.parts[]
            {
                pattern = new[]
                {
                    new CodeInstruction(OpCodes.Call, typeof(CustomAcsCorrectSet).GetMethod("get_nowAcs", AccessTools.all)),
                    new CodeInstruction(OpCodes.Callvirt, typeof(ChaFileAccessory).GetMethod("get_parts", AccessTools.all)),
                },
                replacer = new[]
                {
                    new CodeInstruction(OpCodes.Nop),
                    new CodeInstruction(OpCodes.Call, typeof(CustomAcsCorrectSet_Patches).GetMethod(nameof(GetPartsInfoList), AccessTools.all)),
                }
            },
            new HarmonyExtensions.Replacement() //orgAcs.parts[]
            {
                pattern = new[]
                {
                    new CodeInstruction(OpCodes.Call, typeof(CustomAcsCorrectSet).GetMethod("get_orgAcs", AccessTools.all)),
                    new CodeInstruction(OpCodes.Callvirt, typeof(ChaFileAccessory).GetMethod("get_parts", AccessTools.all)),
                },
                replacer = new[]
                {
                    new CodeInstruction(OpCodes.Nop),
                    new CodeInstruction(OpCodes.Call, typeof(CustomAcsCorrectSet_Patches).GetMethod(nameof(GetOrgPartsInfoList), AccessTools.all)),
                }
            },
            new HarmonyExtensions.Replacement() //[slotNo]
            {
                pattern = new[]
                {
                    new CodeInstruction(OpCodes.Call, typeof(CustomAcsCorrectSet).GetMethod("get_slotNo", AccessTools.all)),
                    new CodeInstruction(OpCodes.Ldelem_Ref)
                },
                replacer = new[]
                {
                    new CodeInstruction(OpCodes.Call, typeof(CustomAcsCorrectSet).GetMethod("get_slotNo", AccessTools.all)),
                    new CodeInstruction(OpCodes.Call, typeof(CustomAcsCorrectSet_Patches).GetMethod(nameof(GetIndexFromList), AccessTools.all)),
                }
            },

        };

        private static readonly MethodInfo[] _methodsToPatch = new[]
        {
            typeof(CustomAcsCorrectSet).GetMethod("UpdateCustomUI", AccessTools.all),
            typeof(CustomAcsCorrectSet).GetMethod("UpdateDragValue", AccessTools.all),
            typeof(CustomAcsCorrectSet).GetMethod("SetControllerTransform", AccessTools.all),
            typeof(CustomAcsCorrectSet).GetMethod("SetAccessoryTransform", AccessTools.all),
        };
        #endregion

        public static void PatchAll(Harmony harmony)
        {
            HarmonyMethod transpiler = new HarmonyMethod(typeof(CustomAcsCorrectSet_Patches), nameof(GeneralTranspiler), new[] { typeof(IEnumerable<CodeInstruction>) });

            foreach (MethodInfo methodInfo in _methodsToPatch)
            {
                try
                {
                    harmony.Patch(methodInfo, transpiler: transpiler);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError("MoreAccessories: Could not patch:\n" + e);
                }
            }
            harmony.PatchAll(typeof(CustomAcsCorrectSet_Patches));
        }

        private static IEnumerable<CodeInstruction> GeneralTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return HarmonyExtensions.ReplaceCodePattern(instructions, _replacements);
        }

        #region Replacers
        private static IList<ChaFileAccessory.PartsInfo> GetPartsInfoList(CustomAcsCorrectSet self)
        {
            return new ReadOnlyListExtender<ChaFileAccessory.PartsInfo>(CustomBase.Instance.chaCtrl.nowCoordinate.accessory.parts, MoreAccessories._self._makerAdditionalData.parts);
        }

        private static IList<ChaFileAccessory.PartsInfo> GetOrgPartsInfoList(CustomAcsCorrectSet self)
        {
            return new ReadOnlyListExtender<ChaFileAccessory.PartsInfo>(CustomBase.Instance.chaCtrl.chaFile.coordinate.accessory.parts, MoreAccessories._self._makerAdditionalData.parts);
        }

        private static Transform GetTrfAcsMove(ChaControl self, int slotNo, int i)
        {
            if (slotNo < 20)
                return self.trfAcsMove[slotNo, i];
            return MoreAccessories._self.GetAdditionalDataByCharacter(self.chaFile).objects[slotNo - 20].move[i];
        }
        private static ChaFileAccessory.PartsInfo GetIndexFromList(IList<ChaFileAccessory.PartsInfo> list, int index)
        {
            if (index < list.Count)
                return list[index];
            return null;
        }
        #endregion

        #region Manual Patches
        [HarmonyPatch(typeof(CustomAcsCorrectSet), "Initialize", typeof(int), typeof(int)), HarmonyPrefix]
        private static bool Initialize_Prefix(CustomAcsCorrectSet __instance, int _slotNo, int _correctNo, Text ___title, List<IDisposable> ___lstDisposable,
                                              Toggle[] ___tglPosRate, Toggle[] ___tglRotRate, Toggle[] ___tglSclRate,
                                              Button[] ___btnPos, float[] ___movePosValue, InputField[] ___inpPos, Button[] ___btnPosReset,
                                              Button[] ___btnRot, float[] ___moveRotValue, InputField[] ___inpRot, Button[] ___btnRotReset,
                                              Button[] ___btnScl, float[] ___moveSclValue, InputField[] ___inpScl, Button[] ___btnSclReset,
                                              Button ___btnAllReset, Toggle ___tglDrawCtrl, Toggle[] ___tglCtrlType, CustomGuideObject ___cmpGuid, Slider ___sldCtrlSpeed, Slider ___sldCtrlSize)
        {
            __instance.slotNo = _slotNo;
            __instance.correctNo = _correctNo;
            if (__instance.slotNo == -1 || __instance.correctNo == -1)
                return false;
            if (___title)
                ___title.text = $"調整{__instance.correctNo + 1:00}";
            __instance.UpdateCustomUI();
            if (___lstDisposable != null && ___lstDisposable.Count != 0)
            {
                int count = ___lstDisposable.Count;
                for (int j = 0; j < count; j++)
                {
                    ___lstDisposable[j].Dispose();
                }
            }
            IDisposable disposable;

            ___tglPosRate.Select((p, i) => new
            {
                toggle = p,
                index = (byte)i
            }).ToList().ForEach((p) =>
            {
                disposable = (from isOn in p.toggle.OnValueChangedAsObservable()
                              where isOn
                              select isOn).Subscribe(_ => { Singleton<CustomBase>.Instance.customSettingSave.acsCtrlSetting.correctSetting[__instance.correctNo].posRate = p.index; });
                ___lstDisposable.Add(disposable);
            });
            ___tglRotRate.Select((p, i) => new
            {
                toggle = p,
                index = (byte)i
            }).ToList().ForEach((p) =>
            {
                disposable = (from isOn in p.toggle.OnValueChangedAsObservable()
                              where isOn
                              select isOn).Subscribe(_ => { Singleton<CustomBase>.Instance.customSettingSave.acsCtrlSetting.correctSetting[__instance.correctNo].rotRate = p.index; });
                ___lstDisposable.Add(disposable);
            });
            ___tglSclRate.Select((p, i) => new
            {
                toggle = p,
                index = (byte)i
            }).ToList().ForEach((p) =>
            {
                disposable = (from isOn in p.toggle.OnValueChangedAsObservable()
                              where isOn
                              select isOn).Subscribe(_ => { Singleton<CustomBase>.Instance.customSettingSave.acsCtrlSetting.correctSetting[__instance.correctNo].sclRate = p.index; });
                ___lstDisposable.Add(disposable);
            });
            float downTimeCnt = 0f;
            float loopTimeCnt = 0f;
            bool change = false;
            int[] flag =
            {
                1,
                2,
                4
            };
            ___btnPos.Select((p, i) => new
            {
                btn = p,
                index = i
            }).ToList().ForEach((p) =>
            {
                disposable = p.btn.OnClickAsObservable().Subscribe(_ =>
                {
                    if (!change)
                    {
                        int num = p.index / 2;
                        int num2 = (p.index % 2 != 0) ? 1 : -1;
                        if (num == 0)
                        {
                            num2 *= -1;
                        }
                        float value = num2 * ___movePosValue[Singleton<CustomBase>.Instance.customSettingSave.acsCtrlSetting.correctSetting[__instance.correctNo].posRate];
                        Singleton<CustomBase>.Instance.chaCtrl.SetAccessoryPos(__instance.slotNo, __instance.correctNo, value, true, flag[num]);
                        GetIndexFromList(GetOrgPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 0] = GetIndexFromList(GetPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 0];
                        ___inpPos[num].text = GetIndexFromList(GetPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 0][num].ToString();
                        __instance.SetControllerTransform();
                    }
                });
                ___lstDisposable.Add(disposable);
                disposable = p.btn.UpdateAsObservable().SkipUntil(p.btn.OnPointerDownAsObservable().Do(_ =>
                {
                    downTimeCnt = 0f;
                    loopTimeCnt = 0f;
                    change = false;
                })).TakeUntil(p.btn.OnPointerUpAsObservable()).RepeatUntilDestroy(__instance).Subscribe(_ =>
                {
                    int num = p.index / 2;
                    int num2 = (p.index % 2 != 0) ? 1 : -1;
                    if (num == 0)
                    {
                        num2 *= -1;
                    }
                    float num3 = num2 * ___movePosValue[Singleton<CustomBase>.Instance.customSettingSave.acsCtrlSetting.correctSetting[__instance.correctNo].posRate];
                    float num4 = 0f;
                    downTimeCnt += Time.deltaTime;
                    if (downTimeCnt > 0.3f)
                    {
                        loopTimeCnt += Time.deltaTime;
                        while (loopTimeCnt > 0.05f)
                        {
                            num4 += num3;
                            loopTimeCnt -= 0.05f;
                        }
                        if (num4 != 0f)
                        {
                            Singleton<CustomBase>.Instance.chaCtrl.SetAccessoryPos(__instance.slotNo, __instance.correctNo, num4, true, flag[num]);
                            GetIndexFromList(GetOrgPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 0] = GetIndexFromList(GetPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 0];
                            ___inpPos[num].text = GetIndexFromList(GetPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 0][num].ToString();
                            change = true;
                            __instance.SetControllerTransform();
                        }
                    }
                }).AddTo(__instance);
                ___lstDisposable.Add(disposable);
            });
            ___inpPos.Select((p, i) => new
            {
                inp = p,
                index = i
            }).ToList().ForEach((p) =>
            {
                disposable = p.inp.onEndEdit.AsObservable().Subscribe(value =>
                {
                    int num = p.index % 3;
                    float value2 = CustomBase.ConvertValueFromTextLimit(-100f, 100f, 1, value);
                    p.inp.text = value2.ToString();
                    Singleton<CustomBase>.Instance.chaCtrl.SetAccessoryPos(__instance.slotNo, __instance.correctNo, value2, false, flag[num]);
                    GetIndexFromList(GetOrgPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 0] = GetIndexFromList(GetPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 0];
                    __instance.SetControllerTransform();
                });
                ___lstDisposable.Add(disposable);
            });
            ___btnPosReset.Select((p, i) => new
            {
                btn = p,
                index = i
            }).ToList().ForEach((p) =>
            {
                disposable = p.btn.OnClickAsObservable().Subscribe(_ =>
                {
                    ___inpPos[p.index].text = "0";
                    Singleton<CustomBase>.Instance.chaCtrl.SetAccessoryPos(__instance.slotNo, __instance.correctNo, 0f, false, flag[p.index]);
                    GetIndexFromList(GetOrgPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 0] = GetIndexFromList(GetPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 0];
                    __instance.SetControllerTransform();
                });
                ___lstDisposable.Add(disposable);
            });
            ___btnRot.Select((p, i) => new
            {
                btn = p,
                index = i
            }).ToList().ForEach((p) =>
            {
                disposable = p.btn.OnClickAsObservable().Subscribe(_ =>
                {
                    if (!change)
                    {
                        int num = p.index / 2;
                        int num2 = (p.index % 2 != 0) ? 1 : -1;
                        float value = num2 * ___moveRotValue[Singleton<CustomBase>.Instance.customSettingSave.acsCtrlSetting.correctSetting[__instance.correctNo].rotRate];
                        Singleton<CustomBase>.Instance.chaCtrl.SetAccessoryRot(__instance.slotNo, __instance.correctNo, value, true, flag[num]);
                        GetIndexFromList(GetOrgPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 1] = GetIndexFromList(GetPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 1];
                        ___inpRot[num].text = GetIndexFromList(GetPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 1][num].ToString();
                        __instance.SetControllerTransform();
                    }
                });
                ___lstDisposable.Add(disposable);
                disposable = p.btn.UpdateAsObservable().SkipUntil(p.btn.OnPointerDownAsObservable().Do(_ =>
                {
                    downTimeCnt = 0f;
                    loopTimeCnt = 0f;
                    change = false;
                })).TakeUntil(p.btn.OnPointerUpAsObservable()).RepeatUntilDestroy(__instance).Subscribe(_ =>
                {
                    int num = p.index / 2;
                    int num2 = (p.index % 2 != 0) ? 1 : -1;
                    float num3 = num2 * ___moveRotValue[Singleton<CustomBase>.Instance.customSettingSave.acsCtrlSetting.correctSetting[__instance.correctNo].rotRate];
                    float num4 = 0f;
                    downTimeCnt += Time.deltaTime;
                    if (downTimeCnt > 0.3f)
                    {
                        loopTimeCnt += Time.deltaTime;
                        while (loopTimeCnt > 0.05f)
                        {
                            num4 += num3;
                            loopTimeCnt -= 0.05f;
                        }
                        if (num4 != 0f)
                        {
                            Singleton<CustomBase>.Instance.chaCtrl.SetAccessoryRot(__instance.slotNo, __instance.correctNo, num4, true, flag[num]);
                            GetIndexFromList(GetOrgPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 1] = GetIndexFromList(GetPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 1];
                            ___inpRot[num].text = GetIndexFromList(GetPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 1][num].ToString();
                            change = true;
                            __instance.SetControllerTransform();
                        }
                    }
                }).AddTo(__instance);
                ___lstDisposable.Add(disposable);
            });
            ___inpRot.Select((p, i) => new
            {
                inp = p,
                index = i
            }).ToList().ForEach((p) =>
            {
                disposable = p.inp.onEndEdit.AsObservable().Subscribe(value =>
                {
                    int num = p.index % 3;
                    float value2 = CustomBase.ConvertValueFromTextLimit(0f, 360f, 0, value);
                    p.inp.text = value2.ToString();
                    Singleton<CustomBase>.Instance.chaCtrl.SetAccessoryRot(__instance.slotNo, __instance.correctNo, value2, false, flag[num]);
                    GetIndexFromList(GetOrgPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 1] = GetIndexFromList(GetPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 1];
                    __instance.SetControllerTransform();
                });
                ___lstDisposable.Add(disposable);
            });
            ___btnRotReset.Select((p, i) => new
            {
                btn = p,
                index = i
            }).ToList().ForEach((p) =>
            {
                disposable = p.btn.OnClickAsObservable().Subscribe(_ =>
                {
                    ___inpRot[p.index].text = "0";
                    Singleton<CustomBase>.Instance.chaCtrl.SetAccessoryRot(__instance.slotNo, __instance.correctNo, 0f, false, flag[p.index]);
                    GetIndexFromList(GetOrgPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 1] = GetIndexFromList(GetPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 1];
                    __instance.SetControllerTransform();
                });
                ___lstDisposable.Add(disposable);
            });
            ___btnScl.Select((p, i) => new
            {
                btn = p,
                index = i
            }).ToList().ForEach((p) =>
            {
                disposable = p.btn.OnClickAsObservable().Subscribe(_ =>
                {
                    if (!change)
                    {
                        int num = p.index / 2;
                        int num2 = (p.index % 2 != 0) ? 1 : -1;
                        float value = num2 * ___moveSclValue[Singleton<CustomBase>.Instance.customSettingSave.acsCtrlSetting.correctSetting[__instance.correctNo].sclRate];
                        Singleton<CustomBase>.Instance.chaCtrl.SetAccessoryScl(__instance.slotNo, __instance.correctNo, value, true, flag[num]);
                        GetIndexFromList(GetOrgPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 2] = GetIndexFromList(GetPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 2];
                        ___inpScl[num].text = GetIndexFromList(GetPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 2][num].ToString();
                    }
                });
                ___lstDisposable.Add(disposable);
                disposable = p.btn.UpdateAsObservable().SkipUntil(p.btn.OnPointerDownAsObservable().Do(_ =>
                {
                    downTimeCnt = 0f;
                    loopTimeCnt = 0f;
                    change = false;
                })).TakeUntil(p.btn.OnPointerUpAsObservable()).RepeatUntilDestroy(__instance).Subscribe(_ =>
                {
                    int num = p.index / 2;
                    int num2 = (p.index % 2 != 0) ? 1 : -1;
                    float num3 = num2 * ___moveSclValue[Singleton<CustomBase>.Instance.customSettingSave.acsCtrlSetting.correctSetting[__instance.correctNo].sclRate];
                    float num4 = 0f;
                    downTimeCnt += Time.deltaTime;
                    if (downTimeCnt > 0.3f)
                    {
                        loopTimeCnt += Time.deltaTime;
                        while (loopTimeCnt > 0.05f)
                        {
                            num4 += num3;
                            loopTimeCnt -= 0.05f;
                        }
                        if (num4 != 0f)
                        {
                            Singleton<CustomBase>.Instance.chaCtrl.SetAccessoryScl(__instance.slotNo, __instance.correctNo, num4, true, flag[num]);
                            GetIndexFromList(GetOrgPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 2] = GetIndexFromList(GetPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 2];
                            ___inpScl[num].text = GetIndexFromList(GetPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 2][num].ToString();
                            change = true;
                        }
                    }
                }).AddTo(__instance);
                ___lstDisposable.Add(disposable);
            });
            ___inpScl.Select((p, i) => new
            {
                inp = p,
                index = i
            }).ToList().ForEach((p) =>
            {
                disposable = p.inp.onEndEdit.AsObservable().Subscribe(value =>
                {
                    int num = p.index % 3;
                    float value2 = CustomBase.ConvertValueFromTextLimit(0.01f, 100f, 2, value);
                    p.inp.text = value2.ToString();
                    Singleton<CustomBase>.Instance.chaCtrl.SetAccessoryScl(__instance.slotNo, __instance.correctNo, value2, false, flag[num]);
                    GetIndexFromList(GetOrgPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 2] = GetIndexFromList(GetPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 2];
                });
                ___lstDisposable.Add(disposable);
            });
            ___btnSclReset.Select((p, i) => new
            {
                btn = p,
                index = i
            }).ToList().ForEach((p) =>
            {
                disposable = p.btn.OnClickAsObservable().Subscribe(_ =>
                {
                    ___inpScl[p.index].text = "1";
                    Singleton<CustomBase>.Instance.chaCtrl.SetAccessoryScl(__instance.slotNo, __instance.correctNo, 1f, false, flag[p.index]);
                    GetIndexFromList(GetOrgPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 2] = GetIndexFromList(GetPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 2];
                });
                ___lstDisposable.Add(disposable);
            });
            disposable = ___btnAllReset.OnClickAsObservable().Subscribe(_ =>
            {
                for (int i = 0; i < 3; i++)
                {
                    ___inpPos[i].text = "0";
                    Singleton<CustomBase>.Instance.chaCtrl.SetAccessoryPos(__instance.slotNo, __instance.correctNo, 0f, false, flag[i]);
                    GetIndexFromList(GetOrgPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 0] = GetIndexFromList(GetPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 0];
                    __instance.SetControllerTransform();
                    ___inpRot[i].text = "0";
                    Singleton<CustomBase>.Instance.chaCtrl.SetAccessoryRot(__instance.slotNo, __instance.correctNo, 0f, false, flag[i]);
                    GetIndexFromList(GetOrgPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 1] = GetIndexFromList(GetPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 1];
                    __instance.SetControllerTransform();
                    ___inpScl[i].text = "1";
                    Singleton<CustomBase>.Instance.chaCtrl.SetAccessoryScl(__instance.slotNo, __instance.correctNo, 1f, false, flag[i]);
                    GetIndexFromList(GetOrgPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 2] = GetIndexFromList(GetPartsInfoList(__instance), __instance.slotNo).addMove[__instance.correctNo, 2];
                }
            });
            ___lstDisposable.Add(disposable);
            ___tglDrawCtrl.isOn = CustomBase.Instance.customSettingSave.acsCtrlSetting.correctSetting[__instance.correctNo].draw;
            disposable = ___tglDrawCtrl.OnValueChangedAsObservable().Subscribe(isOn => { CustomBase.Instance.customSettingSave.acsCtrlSetting.correctSetting[__instance.correctNo].draw = isOn; });
            ___lstDisposable.Add(disposable);
            if (___tglCtrlType.Any())
            {
                (from item in ___tglCtrlType.Select((val, idx) => new
                {
                    val,
                    idx
                })
                 where item.val != null
                 select item).ToList().ForEach((item) =>
                {
                    disposable = (from isOn in item.val.OnValueChangedAsObservable()
                                  where isOn
                                  select isOn).Subscribe(isOn =>
                    {
                        CustomBase.Instance.customSettingSave.acsCtrlSetting.correctSetting[__instance.correctNo].type = item.idx;
                        if (___cmpGuid)
                        {
                            ___cmpGuid.SetMode(item.idx);
                        }
                    });
                    ___lstDisposable.Add(disposable);
                });
            }
            disposable = ___sldCtrlSpeed.OnValueChangedAsObservable().Subscribe(val =>
            {
                CustomBase.Instance.customSettingSave.acsCtrlSetting.correctSetting[__instance.correctNo].speed = val;
                if (___cmpGuid)
                {
                    ___cmpGuid.speedMove = val;
                }
            });
            ___lstDisposable.Add(disposable);
            disposable = ___sldCtrlSpeed.OnScrollAsObservable().Subscribe(scl =>
            {
                if (CustomBase.Instance.sliderControlWheel)
                {
                    ___sldCtrlSpeed.value = Mathf.Clamp(___sldCtrlSpeed.value + scl.scrollDelta.y * -0.01f, 0.1f, 1f);
                }
            });
            ___lstDisposable.Add(disposable);
            disposable = ___sldCtrlSize.OnValueChangedAsObservable().Subscribe(val =>
            {
                CustomBase.Instance.customSettingSave.acsCtrlSetting.correctSetting[__instance.correctNo].scale = val;
                if (___cmpGuid)
                {
                    ___cmpGuid.scaleAxis = val;
                    ___cmpGuid.UpdateScale();
                }
            });
            ___lstDisposable.Add(disposable);
            disposable = ___sldCtrlSize.OnScrollAsObservable().Subscribe(scl =>
            {
                if (CustomBase.Instance.sliderControlWheel)
                {
                    ___sldCtrlSize.value = Mathf.Clamp(___sldCtrlSize.value + scl.scrollDelta.y * -0.01f, 0.3f, 3f);
                }
            });
            ___lstDisposable.Add(disposable);
            __instance.UpdateDrawControllerState();
            return false;
        }
        #endregion
    }
}
