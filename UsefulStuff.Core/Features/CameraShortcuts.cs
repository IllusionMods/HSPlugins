using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Input = UnityEngine.Input;
using MethodInfo = System.Reflection.MethodInfo;
#if IPA
using Harmony;
#elif BEPINEX
using HarmonyLib;
#endif

namespace HSUS.Features
{
    public class CameraShortcuts : IFeature
    {
        public void Awake()
        {
#if !PLAYHOME
            InputMouseProc_Patches.Patch();
            InputKeyProc_Patches.Patch();
#endif
        }

        public void LevelLoaded()
        {
        }

#if !PLAYHOME
        public static class InputMouseProc_Patches
        {
            public static void Patch()
            {
                MethodInfo[] toPatch = new[]
                {
                    typeof(Studio.CameraControl).GetMethod("InputMouseProc", BindingFlags.NonPublic | BindingFlags.Instance),
                    typeof(BaseCameraControl_Ver2).GetMethod("InputMouseProc", BindingFlags.NonPublic | BindingFlags.Instance),
                    typeof(BaseCameraControl).GetMethod("InputMouseProc", BindingFlags.NonPublic | BindingFlags.Instance),
                };
                foreach (MethodInfo mi in toPatch)
                    HSUS._self._harmonyInstance.Patch(mi, transpiler: new HarmonyMethod(typeof(InputMouseProc_Patches).GetMethod(nameof(Transpiler), BindingFlags.NonPublic | BindingFlags.Static)));
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo inputGetAxis = typeof(UnityEngine.Input).GetMethod("GetAxis", BindingFlags.Public | BindingFlags.Static);
                List<CodeInstruction> instructionsList = instructions.ToList();
                foreach (CodeInstruction inst in instructionsList)
                {
                    if (inst.opcode == OpCodes.Call && inst.operand == inputGetAxis)
                        yield return new CodeInstruction(OpCodes.Call, typeof(InputMouseProc_Patches).GetMethod(nameof(GetCameraMultiplier)));
                    else
                        yield return inst;
                }
            }

            public static float GetCameraMultiplier(string axis)
            {
                if (Input.GetKey(KeyCode.LeftControl) && HSUS.CameraShortcuts.Value)
                    return Input.GetAxis(axis) * HSUS.CamSpeedSlow.Value;
                if (Input.GetKey(KeyCode.LeftShift) && HSUS.CameraShortcuts.Value)
                    return Input.GetAxis(axis) * HSUS.CamSpeedFast.Value;
                return Input.GetAxis(axis);
            }
        }

        public static class InputKeyProc_Patches
        {
            public static void Patch()
            {
                MethodInfo[] toPatch = new[]
                {
                    typeof(Studio.CameraControl).GetMethod("InputKeyProc", AccessTools.all),
                    typeof(BaseCameraControl_Ver2).GetMethod("InputKeyProc", AccessTools.all),
                    typeof(BaseCameraControl).GetMethod("InputKeyProc", AccessTools.all),
                };
                foreach (MethodInfo mi in toPatch)
                    HSUS._self._harmonyInstance.Patch(mi, transpiler: new HarmonyMethod(typeof(InputKeyProc_Patches).GetMethod(nameof(Transpiler), BindingFlags.NonPublic | BindingFlags.Static)));
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> instructionsList = instructions.ToList();
                MethodInfo timeDeltaTime = typeof(UnityEngine.Time).GetMethod("get_deltaTime", BindingFlags.Public | BindingFlags.Static);
                foreach (CodeInstruction inst in instructionsList)
                {
                    if (inst.opcode == OpCodes.Call && inst.operand == timeDeltaTime)
                        yield return new CodeInstruction(OpCodes.Call, typeof(InputKeyProc_Patches).GetMethod(nameof(GetCameraMultiplier))) { labels = new List<Label>(inst.labels) };
                    else
                        yield return inst;
                }
            }

            public static float GetCameraMultiplier()
            {
                float _adjDeltaTime = Time.deltaTime * HSUS.CamSpeedBaseFactor.Value
#if AISHOUJO || HONEYSELECT2
                    * 10f // to compensate for the 10x scale increse in AI and HS2
#endif
                    ;
                if (Input.GetKey(KeyCode.LeftControl) && HSUS.CameraShortcuts.Value)
                    return _adjDeltaTime * HSUS.CamSpeedSlow.Value;
                if (Input.GetKey(KeyCode.LeftShift) && HSUS.CameraShortcuts.Value)
                    return _adjDeltaTime * HSUS.CamSpeedFast.Value;
                return _adjDeltaTime;
            }
        }
#endif
            }
}
