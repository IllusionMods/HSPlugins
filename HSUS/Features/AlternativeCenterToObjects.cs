#if IPA
using System;
using System.Reflection;
using Harmony;
#elif BEPINEX
using HarmonyLib;
#endif
using System.Xml;
using Manager;
using Studio;
using ToolBox;
using ToolBox.Extensions;
using UnityEngine;

namespace HSUS.Features
{
    public class AlternativeCenterToObjects : IFeature
    {
        private static bool _alternativeCenterToObject = true;
        
        public void Awake()
        {
        }

        public void LoadParams(XmlNode node)
        {
#if !PLAYHOME
            node = node.FindChildNode("alternativeCenterToObject");
            if (node == null)
                return;
            if (node.Attributes["enabled"] != null)
                _alternativeCenterToObject = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
#endif
        }

        public void SaveParams(XmlTextWriter writer)
        {
#if !PLAYHOME
            writer.WriteStartElement("alternativeCenterToObject");
            writer.WriteAttributeString("enabled", XmlConvert.ToString(_alternativeCenterToObject));
            writer.WriteEndElement();
#endif
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
                return HSUS._self._binary == Binary.Studio && _alternativeCenterToObject;
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