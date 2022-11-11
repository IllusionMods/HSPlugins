using System;
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
                var enabledCorrections = HSUS.AutoJointCorrectionValues.Value;
                for (int i = 0; i < __instance.expression.Length; i++)
                    __instance.expression[i] = ((int)enabledCorrections >> i & 1) == 1;
            }
        }

        [Flags]
        public enum JointCorrectionArea
        {
            LeftArm = 1 << 0,
            RightArm = 1 << 1,
            LeftLeg = 1 << 2,
            RightLeg = 1 << 3,
            LeftForearm = 1 << 4,
            RightForearm = 1 << 5,
            LeftThigh = 1 << 6,
            RightThigh = 1 << 7,
            All = 0b11111111
            //Ankle and crotch correction are features of PE(Pose Editor). Setting defaults is also handled by the PE.
            //Crotch = 1 << 8,
            //LeftAnkle = 1 << 9,
            //RightAnkle = 1 << 10,
        }
#endif
    }
}
