#if IPA
using Harmony;
#elif BEPINEX
using HarmonyLib;
#endif
#if AISHOUJO || HONEYSELECT2
using AIChara;
#endif
using System;
using System.Reflection;
using System.Xml;
using ToolBox;
using ToolBox.Extensions;

namespace HSUS.Features
{
    public class EyesBlink : IFeature
    {
        private static bool _eyesBlink = false;
        public void Awake()
        {
        }

        public void LoadParams(XmlNode node)
        {
#if !PLAYHOME
            node = node.FindChildNode("eyesBlink");
            if (node == null)
                return;
            if (node.Attributes["enabled"] != null)
                _eyesBlink = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
#endif
        }

        public void SaveParams(XmlTextWriter writer)
        {
#if !PLAYHOME
            writer.WriteStartElement("eyesBlink");
            writer.WriteAttributeString("enabled", XmlConvert.ToString(_eyesBlink));
            writer.WriteEndElement();
#endif
        }

        public void LevelLoaded()
        {
        }

#if !PLAYHOME
        [HarmonyPatch]
        private static class CharFileInfoStatus_Ctor_Patches
        {
            private static bool Prepare()
            {
                return HSUS._self._binary == Binary.Studio;
            }

            private static MethodBase TargetMethod()
            {
#if HONEYSELECT
                return typeof(CharFileInfoStatus).GetConstructor(new Type[] { });
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
                return typeof(ChaFileStatus).GetConstructor(new Type[] { });
#endif
            }

#if HONEYSELECT
            private static void Postfix(CharFileInfoStatus __instance)
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
            private static void Postfix(ChaFileStatus __instance)
#endif
            {
                __instance.eyesBlink = _eyesBlink;
            }
        }
#endif

    }
}