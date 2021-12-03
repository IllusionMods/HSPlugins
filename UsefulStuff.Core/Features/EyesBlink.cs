#if !KOIKATSU
using System;
using System.Reflection;
using ToolBox;
#if IPA
using Harmony;
#elif BEPINEX
using HarmonyLib;
#endif
#if AISHOUJO || HONEYSELECT2
using AIChara;
#endif

namespace HSUS.Features
{
    public class EyesBlink : IFeature
    {
        public void Awake()
        {
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
                return HSUS._self.binary == Binary.Studio;
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
                __instance.eyesBlink = HSUS.EyesBlink.Value;
            }
        }
#endif

    }
}
#endif