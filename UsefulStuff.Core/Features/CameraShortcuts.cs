using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System;
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
        private static bool _isStudioCam;
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
                Type[] typesToPatch = new[]
                {
                    typeof(Studio.CameraControl),
                    typeof(BaseCameraControl_Ver2),
                    typeof(BaseCameraControl),
                };
                foreach (Type t in typesToPatch)
                {
                    _isStudioCam = (t == typeof(Studio.CameraControl)) ? true : false;
                    MethodInfo mi = t.GetMethod("InputKeyProc", AccessTools.all);
                    HSUS._self._harmonyInstance.Patch(mi, transpiler: new HarmonyMethod(typeof(InputKeyProc_Patches).GetMethod(nameof(Transpiler), BindingFlags.NonPublic | BindingFlags.Static)));
                }
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> instructionsList = instructions.ToList();
                MethodInfo timeDeltaTime = typeof(UnityEngine.Time).GetMethod("get_deltaTime", BindingFlags.Public | BindingFlags.Static);
                foreach (CodeInstruction inst in instructionsList)
                {
                    if (inst.opcode == OpCodes.Call && inst.operand == timeDeltaTime)
                    {
                        string _getCameraMultiplierMethod = (_isStudioCam) ? nameof(GetCameraMultiplierStudio) : nameof(GetCameraMultiplier);
                        yield return new CodeInstruction(OpCodes.Call, typeof(InputKeyProc_Patches).GetMethod(_getCameraMultiplierMethod)) { labels = new List<Label>(inst.labels) };
                    }
                    else
                        yield return inst;
                }
            }

            public static float GetCameraMultiplier()
            {
                float _adjDeltaTime = Time.deltaTime * HSUS.CamSpeedBaseFactor.Value;
                return CameraMultiplier(_adjDeltaTime);
            }

            public static float GetCameraMultiplierStudio()
            {
                float _adjDeltaTime = Time.deltaTime * HSUS.CamSpeedBaseFactor.Value
#if AISHOUJO || HONEYSELECT2
                    * 10f // to compensate for the 10x scale increse in AI and HS2
#endif
                    ;
                return CameraMultiplier(_adjDeltaTime);
            }

            private static float CameraMultiplier(float delta)
            {
                if (Input.GetKey(KeyCode.LeftControl) && HSUS.CameraShortcuts.Value)
                    return delta * HSUS.CamSpeedSlow.Value;
                if (Input.GetKey(KeyCode.LeftShift) && HSUS.CameraShortcuts.Value)
                    return delta * HSUS.CamSpeedFast.Value;
                return delta;
            }
        }
#endif
    }
}
