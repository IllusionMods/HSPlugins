using System.Reflection;
using Studio;
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
    public class AutoJointCorrection : IFeature
    {
        public void Awake()
        {
        }

        public void LevelLoaded()
        {
        }

#if !PLAYHOME
        [HarmonyPatch]
        public class OICharInfo_Ctor_Patches
        {
            internal static MethodBase TargetMethod()
            {
#if HONEYSELECT
                return typeof(OICharInfo).GetConstructor(new[] { typeof(CharFile), typeof(int) });
#elif KOIKATSU || AISHOUJO || HONEYSELECT2
                return typeof(OICharInfo).GetConstructor(new[] { typeof(ChaFileControl), typeof(int) });
#endif
            }

            public static bool Prepare()
            {
                return HSUS.AutoJointCorrection.Value && HSUS._self.binary == Binary.Studio;
            }

            public static void Postfix(OICharInfo __instance)
            {
                for (int i = 0; i < __instance.expression.Length; i++)
                    __instance.expression[i] = true;
            }
        }
#endif
    }
}
