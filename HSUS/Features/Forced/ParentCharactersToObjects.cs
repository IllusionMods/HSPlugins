#if HONEYSELECT
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
#if IPA
using Harmony;
#elif BEPINEX
using HarmonyLib;
#endif
using Manager;
using Studio;
using ToolBox;

namespace HSUS.Features
{
    public static class ParentCharactersToObjects
    {

        [HarmonyPatch(typeof(AddObjectFemale), "Add", typeof(global::CharFemale), typeof(OICharInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int))]
        internal static class AddObjectFemale_Add_Patches
        {
            private static bool Prepare()
            {
                return HSUS._self._binary == Binary.Studio;
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                bool set = false;
                bool set2 = false;
                List<CodeInstruction> instructionsList = instructions.ToList();
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    CodeInstruction inst = instructionsList[i];
                    if (set == false && inst.opcode == OpCodes.Ldnull && instructionsList[i + 1].ToString().Equals("call Studio.TreeNodeObject AddNode(System.String, Studio.TreeNodeObject)"))
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_2); //_parent
                        yield return new CodeInstruction(OpCodes.Ldarg_3); //_parentNode
                        yield return new CodeInstruction(OpCodes.Call, typeof(AddObjectFemale_Add_Patches).GetMethod(nameof(Injected), BindingFlags.NonPublic | BindingFlags.Static));

                        set = true;
                    }
                    else if (set2 == false && inst.opcode == OpCodes.Ldc_I4_0 && instructionsList[i + 1].ToString().Equals("callvirt Void set_enableChangeParent(Boolean)"))
                    {
                        yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                        set2 = true;
                    }
                    else
                        yield return inst;
                }
            }

            private static TreeNodeObject Injected(ObjectCtrlInfo _parent, TreeNodeObject _parentNode)
            {
                return _parentNode == null ? (_parent == null ? null : _parent.treeNodeObject) : _parentNode;
            }
        }

        [HarmonyPatch(typeof(AddObjectMale), "Add", typeof(global::CharMale), typeof(OICharInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int))]
        internal static class AddObjectMale_Add_Patches
        {
            private static bool Prepare()
            {
                return HSUS._self._binary == Binary.Studio;
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                bool set = false;
                bool set2 = false;
                List<CodeInstruction> instructionsList = instructions.ToList();
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    CodeInstruction inst = instructionsList[i];
                    if (set == false && inst.opcode == OpCodes.Ldnull && instructionsList[i + 1].ToString().Equals("call Studio.TreeNodeObject AddNode(System.String, Studio.TreeNodeObject)"))
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_2); //_parent
                        yield return new CodeInstruction(OpCodes.Ldarg_3); //_parentNode
                        yield return new CodeInstruction(OpCodes.Call, typeof(AddObjectMale_Add_Patches).GetMethod(nameof(Injected), BindingFlags.NonPublic | BindingFlags.Static));

                        set = true;
                    }
                    else if (set2 == false && inst.opcode == OpCodes.Ldc_I4_0 && instructionsList[i + 1].ToString().Equals("callvirt Void set_enableChangeParent(Boolean)"))
                    {
                        yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                        set2 = true;
                    }
                    else
                        yield return inst;
                }
            }

            private static TreeNodeObject Injected(ObjectCtrlInfo _parent, TreeNodeObject _parentNode)
            {
                return _parentNode == null ? (_parent == null ? null : _parent.treeNodeObject) : _parentNode;
            }
        }

        [HarmonyPatch(typeof(OCIChar), "OnDetach")]
        internal static class OCIChar_OnDetach_Patches
        {
            private static bool Prepare()
            {
                return HSUS._self._binary == Binary.Studio;
            }

            private static void Postfix(OCIChar __instance)
            {
                __instance.parentInfo.OnDetachChild(__instance);
                __instance.guideObject.parent = null;
                Studio.Studio.AddInfo(__instance.objectInfo, __instance);
                __instance.guideObject.transformTarget.SetParent(Scene.Instance.commonSpace.transform);
                __instance.objectInfo.changeAmount.pos = __instance.guideObject.transformTarget.localPosition;
                __instance.objectInfo.changeAmount.rot = __instance.guideObject.transformTarget.localEulerAngles;
                __instance.guideObject.calcMode = GuideObject.Mode.Local;
            }
        }

        [HarmonyPatch(typeof(OCIChar), "OnDelete")]
        internal static class OCIChar_OnDelete_Patches
        {
            private static bool Prepare()
            {
                return HSUS._self._binary == Binary.Studio;
            }

            private static void Postfix(OCIChar __instance)
            {
                if (__instance.parentInfo != null)
                    __instance.parentInfo.OnDetachChild(__instance);
            }
        }
    }
}
#endif
