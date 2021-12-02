using System;
using System.Reflection;
using System.Xml;
using ToolBox;
using ToolBox.Extensions;
#if IPA
using Harmony;
#elif BEPINEX
using HarmonyLib;
#endif
#if HONEYSELECT
using Studio;
#endif

namespace HSUS.Features
{
    public class AnimationOptionDisplay : IFeature
    {
#if HONEYSELECT
        private static bool _enabled = true;
#endif

        public void Awake()
        {
        }

        public void LevelLoaded()
        {
        }

        public void LoadParams(XmlNode node)
        {
#if HONEYSELECT
            node = node.FindChildNode("animationOptionDisplay");
            if (node == null)
                return;
            if (node.Attributes["enabled"] != null)
                _enabled = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
#endif
        }

        public void SaveParams(XmlTextWriter writer)
        {
#if HONEYSELECT
            writer.WriteStartElement("animationOptionDisplay");
            writer.WriteAttributeString("enabled", XmlConvert.ToString(_enabled));
            writer.WriteEndElement();
#endif
        }

#if HONEYSELECT
        [HarmonyPatch]
        private static class OICharInfo_Ctor_Patches
        {
            private static bool Prepare()
            {
                return HSUS._self._binary == Binary.Studio;
            }

            private static MethodBase TargetMethod()
            {
                return typeof(OICharInfo).GetConstructor(new[] {typeof(CharFile), typeof(int)});
            }

            private static void Postfix(OICharInfo __instance)
            {
                __instance.animeOptionVisible = _enabled;
            }
        }
#endif
    }
}