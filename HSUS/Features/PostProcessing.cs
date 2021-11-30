using System.Xml;
#if IPA
using Harmony;
#elif BEPINEX
using HarmonyLib;
#endif
#if HONEYSELECT
using System;
using System.Reflection;
using UnityEngine;
using UnityStandardAssets.ImageEffects;
#endif
using Studio;
using ToolBox.Extensions;

namespace HSUS.Features
{
    public class PostProcessing : IFeature
    {
#if HONEYSELECT
        private static bool _ssaoEnabled = true;
        private static bool _bloomEnabled = true;
        private static bool _ssrEnabled = true;
        private static bool _dofEnabled = true;
        private static bool _vignetteEnabled = true;
        private static bool _fogEnabled = true;
        private static bool _sunShaftsEnabled = false;
#elif KOIKATSU
        private static bool _ssaoEnabled = true;
        private static bool _bloomEnabled = true;
        private static bool _selfShadowEnabled = true;
        private static bool _dofEnabled = false;
        private static bool _vignetteEnabled = true;
        private static bool _fogEnabled = false;
        private static bool _sunShaftsEnabled = false;
#endif

        public void Awake()
        {

        }

        public void LoadParams(XmlNode node)
        {
#if HONEYSELECT || KOIKATSU
            node = node.FindChildNode("postProcessing");
            if (node == null)
                return;
            foreach (XmlNode childNode in node.ChildNodes)
            {
                switch (childNode.Name)
                {
                    case "depthOfField":
                        if (childNode.Attributes["enabled"] != null)
                            _dofEnabled = XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value);
                        break;
                    case "ssao":
                        if (childNode.Attributes["enabled"] != null)
                            _ssaoEnabled = XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value);
                        break;
                    case "bloom":
                        if (childNode.Attributes["enabled"] != null)
                            _bloomEnabled = XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value);
                        break;
#if HONEYSELECT
                    case "ssr":
                        if (childNode.Attributes["enabled"] != null)
                            _ssrEnabled = XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value);
                        break;
#elif KOIKATSU
                    case "selfShadow":
                        if (childNode.Attributes["enabled"] != null)
                            _selfShadowEnabled = XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value);
                        break;
#endif
                    case "vignette":
                        if (childNode.Attributes["enabled"] != null)
                            _vignetteEnabled = XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value);
                        break;
                    case "fog":
                        if (childNode.Attributes["enabled"] != null)
                            _fogEnabled = XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value);
                        break;
                    case "sunShafts":
                        if (childNode.Attributes["enabled"] != null)
                            _sunShaftsEnabled = XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value);
                        break;
                }
            }
#endif
        }

        public void SaveParams(XmlTextWriter writer)
        {
#if HONEYSELECT || KOIKATSU
            writer.WriteStartElement("postProcessing");

            {
                writer.WriteStartElement("depthOfField");
                writer.WriteAttributeString("enabled", XmlConvert.ToString(_dofEnabled));
                writer.WriteEndElement();
            }

            {
                writer.WriteStartElement("ssao");
                writer.WriteAttributeString("enabled", XmlConvert.ToString(_ssaoEnabled));
                writer.WriteEndElement();
            }

            {
                writer.WriteStartElement("bloom");
                writer.WriteAttributeString("enabled", XmlConvert.ToString(_bloomEnabled));
                writer.WriteEndElement();
            }

#if HONEYSELECT
            {
                writer.WriteStartElement("ssr");
                writer.WriteAttributeString("enabled", XmlConvert.ToString(_ssrEnabled));
                writer.WriteEndElement();
            }
#elif KOIKATSU
            {
                writer.WriteStartElement("selfShadow");
                writer.WriteAttributeString("enabled", XmlConvert.ToString(_selfShadowEnabled));
                writer.WriteEndElement();
            }
#endif

            {
                writer.WriteStartElement("vignette");
                writer.WriteAttributeString("enabled", XmlConvert.ToString(_vignetteEnabled));
                writer.WriteEndElement();
            }

            {
                writer.WriteStartElement("fog");
                writer.WriteAttributeString("enabled", XmlConvert.ToString(_fogEnabled));
                writer.WriteEndElement();
            }

            {
                writer.WriteStartElement("sunShafts");
                writer.WriteAttributeString("enabled", XmlConvert.ToString(_sunShaftsEnabled));
                writer.WriteEndElement();
            }

            writer.WriteEndElement();

#endif
        }

        public void LevelLoaded()
        {
        }

#if HONEYSELECT || KOIKATSU
        [HarmonyPatch(typeof(SystemButtonCtrl), "Init")]
        public class SystemButtonCtrl_Init_Patches
        {
            public static void Postfix(SystemButtonCtrl __instance)
            {
                ResetPostProcessing(__instance);
            }

            internal static void ResetPostProcessing(SystemButtonCtrl __instance)
            {
#if HONEYSELECT
                __instance.GetPrivate("ssaoInfo").CallPrivate("OnValueChangedEnable", _ssaoEnabled);
                __instance.GetPrivate("ssrInfo").CallPrivate("OnValueChangedEnable", _ssrEnabled);
#elif KOIKATSU
                __instance.GetPrivate("amplifyOcculusionEffectInfo").CallPrivate("OnValueChangedEnable", _ssaoEnabled);
                __instance.GetPrivate("selfShadowInfo").CallPrivate("OnValueChangedEnable", _selfShadowEnabled);
#endif
                __instance.GetPrivate("sunShaftsInfo").CallPrivate("OnValueChangedEnable", _sunShaftsEnabled);
                __instance.GetPrivate("fogInfo").CallPrivate("OnValueChangedEnable", _fogEnabled);
                __instance.GetPrivate("dofInfo").CallPrivate("OnValueChangedEnable", _dofEnabled);
                __instance.GetPrivate("bloomInfo").CallPrivate("OnValueChangedEnable", _bloomEnabled);
                __instance.GetPrivate("vignetteInfo").CallPrivate("OnValueChangedEnable", _vignetteEnabled);

#if KOIKATSU
                __instance.GetPrivate("amplifyOcculusionEffectInfo").CallPrivate("UpdateInfo"); //No I don't care about caching the results the first time, it's annoying.
                __instance.GetPrivate("selfShadowInfo").CallPrivate("UpdateInfo");
                __instance.GetPrivate("sunShaftsInfo").CallPrivate("UpdateInfo");
                __instance.GetPrivate("fogInfo").CallPrivate("UpdateInfo");
                __instance.GetPrivate("dofInfo").CallPrivate("UpdateInfo");
                __instance.GetPrivate("bloomInfo").CallPrivate("UpdateInfo");
                __instance.GetPrivate("vignetteInfo").CallPrivate("UpdateInfo");
#endif
            }
        }

        [HarmonyPatch(typeof(SystemButtonCtrl), "OnSelectInitYes")]
        public class SystemButtonCtrl_OnSelectInitYes_Patches
        {
            public static void Postfix(SystemButtonCtrl __instance)
            {
                SystemButtonCtrl_Init_Patches.ResetPostProcessing(__instance);
            }
        }
#endif
    }
}