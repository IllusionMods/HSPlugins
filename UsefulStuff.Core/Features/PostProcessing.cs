#if !KOIKATSU
using Studio;
using ToolBox.Extensions;
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

namespace HSUS.Features
{
    public class PostProcessing : IFeature
    {
        //#if HONEYSELECT
        //        private static bool _ssaoEnabled = true;
        //        private static bool _bloomEnabled = true;
        //        private static bool _ssrEnabled = true;
        //        private static bool _dofEnabled = true;
        //        private static bool _vignetteEnabled = true;
        //        private static bool _fogEnabled = true;
        //        private static bool _sunShaftsEnabled = false;

        public void Awake()
        {

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
                __instance.GetPrivate("amplifyOcculusionEffectInfo").CallPrivate("OnValueChangedEnable", HSUS.PostProcessingSSAO.Value);
                __instance.GetPrivate("selfShadowInfo").CallPrivate("OnValueChangedEnable", HSUS.PostProcessingSelfShadow.Value);
#endif
                __instance.GetPrivate("sunShaftsInfo").CallPrivate("OnValueChangedEnable", HSUS.PostProcessingSunShafts.Value);
                __instance.GetPrivate("fogInfo").CallPrivate("OnValueChangedEnable", HSUS.PostProcessingFog.Value);
                __instance.GetPrivate("dofInfo").CallPrivate("OnValueChangedEnable", HSUS.PostProcessingDepthOfField.Value);
                __instance.GetPrivate("bloomInfo").CallPrivate("OnValueChangedEnable", HSUS.PostProcessingBloom.Value);
                __instance.GetPrivate("vignetteInfo").CallPrivate("OnValueChangedEnable", HSUS.PostProcessingVignette.Value);

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
#endif