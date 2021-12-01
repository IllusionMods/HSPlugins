using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using RootMotion.FinalIK;
#if IPA
using Harmony;
#elif BEPINEX
using HarmonyLib;
#endif
#if PLAYHOME
using SEXY;
#endif
using Studio;

namespace HSPE
{
    [HarmonyPatch(typeof(Studio.Studio), "Duplicate")]
    internal class Studio_Duplicate_Patches
    {
        public static void Postfix(Studio.Studio __instance)
        {
            foreach (KeyValuePair<int, int> pair in SceneInfo_Import_Patches._newToOldKeys)
            {
                ObjectCtrlInfo source;
                if (__instance.dicObjectCtrl.TryGetValue(pair.Value, out source) == false)
                    continue;
                ObjectCtrlInfo destination;
                if (__instance.dicObjectCtrl.TryGetValue(pair.Key, out destination) == false)
                    continue;
                if (source is OCIChar && destination is OCIChar || source is OCIItem && destination is OCIItem)
                    MainWindow._self.OnDuplicate(source, destination);
            }
        }
    }

    [HarmonyPatch(typeof(ObjectInfo), "Load", new []{typeof(BinaryReader), typeof(Version), typeof(bool), typeof(bool)})]
    internal static class ObjectInfo_Load_Patches
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            int count = 0;
            List<CodeInstruction> instructionsList = instructions.ToList();
            foreach (CodeInstruction inst in instructionsList)
            {
                yield return inst;
                if (count != 2 && inst.ToString().Contains("ReadInt32"))
                {
                    ++count;
                    if (count == 2)
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Call, typeof(ObjectInfo_Load_Patches).GetMethod(nameof(Injected), BindingFlags.NonPublic | BindingFlags.Static));
                    }
                }
            }
        }

        private static int Injected(int originalIndex, ObjectInfo __instance)
        {
            SceneInfo_Import_Patches._newToOldKeys.Add(__instance.dicKey, originalIndex);
            return originalIndex; //Doing this so other transpilers can use this value if they want
        }
    }



    [HarmonyPatch(typeof(SceneInfo), "Import", new []{ typeof(BinaryReader), typeof(Version) } )]
    internal static class SceneInfo_Import_Patches //This is here because I fucked up the save format making it impossible to import scenes correctly
    {
        internal static Dictionary<int, int> _newToOldKeys = new Dictionary<int, int>();

        private static void Prefix()
        {
            _newToOldKeys.Clear();
        }
    }

    [HarmonyPatch(typeof(OCIChar), "LoadClothesFile", new[] { typeof(string) })]
    internal static class OCIChar_LoadClothesFile_Patches
    {
        public static event Action<OCIChar> onLoadClothesFile;
        public static void Postfix(OCIChar __instance, string _path)
        {
            if (onLoadClothesFile != null)
                onLoadClothesFile(__instance);
        }
    }

    [HarmonyPatch(typeof(OCIChar), "ChangeChara", new[] { typeof(string) })]
    internal static class OCIChar_ChangeChara_Patches
    {
        public static event Action<OCIChar> onChangeChara;
        public static void Postfix(OCIChar __instance, string _path)
        {
            if (onChangeChara != null)
                onChangeChara(__instance);
        }
    }

#if HONEYSELECT || KOIKATSU
#if HONEYSELECT
    [HarmonyPatch(typeof(OCIChar), "SetCoordinateInfo", new[] { typeof(CharDefine.CoordinateType), typeof(bool) })]
#elif KOIKATSU
        [HarmonyPatch(typeof(OCIChar), "SetCoordinateInfo", new[] {typeof(ChaFileDefine.CoordinateType), typeof(bool) })]        
#endif
    internal static class OCIChar_SetCoordinateInfo_Patches
    {
#if HONEYSELECT
        public static event Action<OCIChar, CharDefine.CoordinateType, bool> onSetCoordinateInfo;
        public static void Postfix(OCIChar __instance, CharDefine.CoordinateType _type, bool _force)
#elif KOIKATSU
            public static event Action<OCIChar, ChaFileDefine.CoordinateType, bool> onSetCoordinateInfo;
            public static void Postfix(OCIChar __instance, ChaFileDefine.CoordinateType _type, bool _force)
#endif
        {
            if (onSetCoordinateInfo != null)
                onSetCoordinateInfo(__instance, _type, _force);
        }
    }
#endif

}
