using Studio;
using ToolBox;
using UnityEngine;
#if IPA
using System;
using System.Reflection;
using Harmony;
#elif BEPINEX
using HarmonyLib;
#endif

namespace HSUS.Features
{
    public class AlternativeCenterToObjects : IFeature
    {
        public void Awake()
        {
        }

        public void LevelLoaded()
        {
        }

#if HONEYSELECT
        [HarmonyPatch]
        public static class HSSNAShortcutKeyCtrlOverride_Update_Patches
        {
            private static MethodInfo _getKeyDownMethod;
            private static readonly object[] _params = { 4 };

            private static bool Prepare()
            {
                Type t = Type.GetType("HSStudioNEOAddon.ShortcutKey.HSSNAShortcutKeyCtrlOverride,HSStudioNEOAddon");
                if (HSUS._self._binary == Binary.Studio && _alternativeCenterToObject && t != null)
                {
                    _getKeyDownMethod = t.GetMethod("GetKeyDown", BindingFlags.NonPublic | BindingFlags.Instance);
                    return true;
                }
                return false;
            }

            private static MethodInfo TargetMethod()
            {
                return Type.GetType("HSStudioNEOAddon.ShortcutKey.HSSNAShortcutKeyCtrlOverride,HSStudioNEOAddon").GetMethod("Update");
            }

            public static void Postfix(object __instance, Studio.CameraControl ___cameraControl)
            {
                if (!Studio.Studio.IsInstance() && Studio.Studio.Instance.isInputNow && !Scene.IsInstance() && Scene.Instance.AddSceneName != string.Empty)
                    return;
                if (!Studio.Studio.Instance.isVRMode && _getKeyDownMethod != null && (bool)_getKeyDownMethod.Invoke(__instance, _params) && GuideObjectManager.Instance.selectObject != null)
                    ___cameraControl.targetPos = GuideObjectManager.Instance.selectObject.transformTarget.position;
            }
        }
#elif !PLAYHOME
        [HarmonyPatch(typeof(ShortcutKeyCtrl), "Update")]
        public static class ShortcutKeyCtrl_Update_Patches
        {
            private static bool Prepare()
            {
                return HSUS._self.binary == Binary.Studio && HSUS.AlternativeCenterToObjects.Value;
            }

            private static void Postfix(Studio.CameraControl ___cameraControl)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.F))
                {
                    GuideObject selectedObject = GuideObjectManager.Instance.selectObject;
                    if (selectedObject != null)
                        ___cameraControl.targetPos = selectedObject.transformTarget.position;
                }

            }
        }
#endif
    }
}