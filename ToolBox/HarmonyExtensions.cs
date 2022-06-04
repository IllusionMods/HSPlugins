using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
#if IPA
using Harmony;
#elif BEPINEX
using HarmonyLib;
#endif

namespace ToolBox.Extensions {
    internal static class HarmonyExtensions
    {
#if IPA
        public static HarmonyInstance CreateInstance(string guid)
#elif BEPINEX
        public static Harmony CreateInstance(string guid)
#endif
        {
#if IPA
            return HarmonyInstance.Create(guid);
#elif BEPINEX
            return new Harmony(guid);
#endif
        }

//#if IPA
//        public static void PatchAllSafe(this HarmonyInstance self)
//#elif BEPINEX
//        public static void PatchAllSafe(this Harmony self)
//#endif
//        {
//            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
//            {
//                try
//                {
//#if IPA
//                    List<HarmonyMethod> harmonyMethods = type.GetHarmonyMethods();
//#elif BEPINEX
//                    List<HarmonyMethod> harmonyMethods = HarmonyMethodExtensions.GetFromType(type);
//#endif
//                    if (harmonyMethods == null || harmonyMethods.Count <= 0)
//                        continue;
//                    HarmonyMethod attributes = HarmonyMethod.Merge(harmonyMethods);
//                    new PatchProcessor(self, type, attributes).Patch();
//                }
//                catch (Exception e)
//                {
//                    UnityEngine.Debug.LogError(self.Id + ": Exception occured when patching: " + e);
//                }
//            }
//        }

        public class Replacement
        {
            public CodeInstruction[] pattern = null;
            public CodeInstruction[] replacer = null;
        }

        public static IEnumerable<CodeInstruction> ReplaceCodePattern(IEnumerable<CodeInstruction> instructions, IList<Replacement> replacements)
        {
            List<CodeInstruction> codeInstructions = instructions.ToList();
            foreach (Replacement replacement in replacements)
            {
                for (int i = 0; i < codeInstructions.Count; i++)
                {
                    int j = 0;
                    while (j < replacement.pattern.Length && i + j < codeInstructions.Count &&
                           CompareCodeInstructions(codeInstructions[i + j], replacement.pattern[j]))
                        ++j;
                    if (j == replacement.pattern.Length)
                    {
                        for (int k = 0; k < replacement.replacer.Length; k++)
                        {
                            int finalIndex = i + k;
                            //codeInstructions[finalIndex] = new CodeInstruction(replacement.replacer[k]) { labels = new List<Label>(codeInstructions[finalIndex].labels) };
                            codeInstructions[finalIndex].opcode = replacement.replacer[k].opcode; 
                            codeInstructions[finalIndex].operand= replacement.replacer[k].operand; 
                        }
                        i += replacement.replacer.Length;
                    }
                }
            }
            return codeInstructions;
        }

        private static bool CompareCodeInstructions(CodeInstruction first, CodeInstruction second)
        {
            return first.opcode == second.opcode && first.operand == second.operand;
        }
    }
}