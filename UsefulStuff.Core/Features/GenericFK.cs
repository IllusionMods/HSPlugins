using Studio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ToolBox;
using ToolBox.Extensions;
using UnityEngine;
#if IPA
using Harmony;
#elif BEPINEX
using HarmonyLib;
#endif

namespace HSUS.Features
{
    public class GenericFK : IFeature
    {
        public void Awake()
        {
        }

        public void LevelLoaded()
        {
        }

        [HarmonyPatch(typeof(AddObjectItem), "Load", new[] { typeof(OIItemInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) })]
        private static class AddObjectItem_Load_Patches
        {
            private static Type _itemFKCtrl;
            private static MethodInfo _initBone;
            private static bool Prepare()
            {
                _itemFKCtrl = Type.GetType("Studio.ItemFKCtrl,Assembly-CSharp");
                if (_itemFKCtrl != null)
                    _initBone = _itemFKCtrl.GetMethod("InitBone", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                return HSUS.GenericFK.Value && HSUS._self.binary == Binary.Studio && _itemFKCtrl != null;
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                FieldInfo itemFKCtrl = typeof(OCIItem).GetField("itemFKCtrl");
                List<CodeInstruction> instructionList = instructions.ToList();
                for (int i = 0; i < instructionList.Count; i++)
                {
                    CodeInstruction inst = instructionList[i];
                    if (inst.opcode == OpCodes.Ldnull && instructionList[i + 1].opcode == OpCodes.Stfld && (instructionList[i + 1].operand as FieldInfo) == itemFKCtrl)
                    {
                        yield return new CodeInstruction(OpCodes.Ldloc_0);
                        yield return new CodeInstruction(OpCodes.Ldarg_3);
                        yield return new CodeInstruction(OpCodes.Call, typeof(AddObjectItem_Load_Patches).GetMethod(nameof(InitGenericBone), BindingFlags.NonPublic | BindingFlags.Static));
                    }
                    else
                        yield return inst;
                }
            }

            private static object InitGenericBone(OCIItem ociItem, bool isNew)
            {
                object itemFKCtrl = ociItem.objectItem.AddComponent(_itemFKCtrl);
                _initBone.Invoke(itemFKCtrl, new object[] { ociItem, null, isNew });
#if HONEYSELECT
                ociItem.dynamicBones = ociItem.objectItem.GetComponentsInChildren<DynamicBone>();
#endif
                return itemFKCtrl;
            }
        }

        [HarmonyPatch]
        public static class ItemFKCtrl_InitBone_Patches
        {
            private static ConstructorInfo _targetInfoConstructor;
            private static PropertyInfo _countProperty;

            private static bool Prepare()
            {
                if (HSUS._self.binary == Binary.Studio && HSUS.GenericFK.Value && Type.GetType("Studio.ItemFKCtrl,Assembly-CSharp") != null)
                {
                    _targetInfoConstructor = Type.GetType("Studio.ItemFKCtrl+TargetInfo,Assembly-CSharp").GetConstructor(new[] { typeof(GameObject), typeof(ChangeAmount), typeof(bool) });
                    _countProperty = Type.GetType("Studio.ItemFKCtrl,Assembly-CSharp").GetProperty("count", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                    return true;
                }
                return false;
            }

            private static MethodInfo TargetMethod()
            {
                return AccessTools.Method(Type.GetType("Studio.ItemFKCtrl,Assembly-CSharp"), "InitBone", new[] { typeof(OCIItem), typeof(Info.ItemLoadInfo), typeof(bool) });
            }

            public static bool Prefix(object __instance, OCIItem _ociItem, Info.ItemLoadInfo _loadInfo, bool _isNew, object ___listBones)
            {
                if (_loadInfo != null && _loadInfo.bones.Count > 0)
                    return true;
                Transform transform = _ociItem.objectItem.transform;
                HashSet<Transform> activeBones = new HashSet<Transform>();
                foreach (MeshRenderer renderer in transform.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (activeBones.Contains(renderer.transform) == false)
                    {
                        if (renderer.name.Substring(0, renderer.name.Length - 1).EndsWith("MeshPart") == false)
                        {
                            if (renderer.transform != transform)
                                activeBones.Add(renderer.transform);
                        }
                        else if (activeBones.Contains(renderer.transform.parent) == false)
                        {
                            if (renderer.transform.parent != transform)
                                activeBones.Add(renderer.transform.parent);
                        }
                    }
                }
                foreach (SkinnedMeshRenderer renderer in transform.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    foreach (Transform bone in renderer.bones)
                    {
                        if (bone == null || activeBones.Contains(bone) || bone == transform)
                            continue;
                        activeBones.Add(bone);
                    }

                }
                _ociItem.listBones = new List<OCIChar.BoneInfo>();
                IList listBones = (IList)___listBones;
                int i = 0;
                object[] constructorParams = new object[3];
                foreach (Transform t in activeBones)
                {
                    OIBoneInfo oIBoneInfo = null;
                    string path = t.GetPathFrom(transform);
                    if (!_ociItem.itemInfo.bones.TryGetValue(path, out oIBoneInfo))
                    {
                        oIBoneInfo = new OIBoneInfo(Studio.Studio.GetNewIndex())
                        {
                            changeAmount =
                            {
                                pos = t.localPosition,
                                rot = t.localEulerAngles,
                                scale = t.localScale
                            }
                        };
                        _ociItem.itemInfo.bones.Add(path, oIBoneInfo);
                    }
                    GuideObject guideObject = Singleton<GuideObjectManager>.Instance.Add(t, oIBoneInfo.dicKey);
                    guideObject.enablePos = false;
                    guideObject.enableScale = false;
                    guideObject.enableMaluti = false;
                    guideObject.calcScale = false;
                    guideObject.scaleRate = 0.5f;
                    guideObject.scaleRot = 0.025f;
                    guideObject.scaleSelect = 0.05f;
                    guideObject.parentGuide = _ociItem.guideObject;
                    _ociItem.listBones.Add(
                            new OCIChar.BoneInfo(
                                    guideObject,
                                    oIBoneInfo
#if KOIKATSU || AISHOUJO || HONEYSELECT2
                                                            , i
#endif
                            )
                    );
                    guideObject.SetActive(false, true);

                    constructorParams[0] = t.gameObject;
                    constructorParams[1] = oIBoneInfo.changeAmount;
                    constructorParams[2] = _isNew;
                    object instance = _targetInfoConstructor.Invoke(constructorParams);
                    listBones.Add(instance);
                    ++i;
                }
                _countProperty.SetValue(__instance, i, null);
                return false;
            }
        }
    }
}