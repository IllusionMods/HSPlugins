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
                __instance.expression[0] = HSUS.LeftArmAJC.Value;
                __instance.expression[1] = HSUS.RightArmAJC.Value;
                __instance.expression[2] = HSUS.LeftLegAJC.Value;
                __instance.expression[3] = HSUS.RightLegAJC.Value;
                __instance.expression[4] = HSUS.LeftForearmAJC.Value;
                __instance.expression[5] = HSUS.RightForearmAJC.Value;
                __instance.expression[6] = HSUS.LeftThighAJC.Value;
                __instance.expression[7] = HSUS.RightThighAJC.Value;
            }
        }
#endif
    }
}
