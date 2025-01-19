using System.Reflection;
using BepInEx.Logging;
using ToolBox;
using UnityEngine;
using UnityEngine.SceneManagement;
#if IPA
using Harmony;
using IllusionPlugin;
#elif BEPINEX
using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;
#endif

namespace HSPE
{
#if BEPINEX
    [BepInPlugin(GUID, Name, Version)]
    [BepInDependency("com.bepis.bepinex.extendedsave")]
#if KOIKATSU
    [BepInProcess("CharaStudio")]
#elif AISHOUJO || HONEYSELECT2
    [BepInProcess("StudioNEOV2")]
#endif
#endif
    internal class HSPE : GenericPlugin
#if HONEYSELECT || PLAYHOME
    , IEnhancedPlugin
#endif
    {
#if HONEYSELECT
        public const string Name = "HSPE";
        public const string GUID = "com.joan6694.illusionplugins.poseeditor";
#elif PLAYHOME
        public const string Name = "PHPE";
        public const string GUID = "com.joan6694.illusionplugins.poseeditor";
#elif KOIKATSU || SUNSHINE //This must be the same for KK/KKS cross compatibility
        public const string Name = "KKPE";
        public const string GUID = "com.joan6694.kkplugins.kkpe";
        internal const int saveVersion = 0;
#elif AISHOUJO
        public const string Name = "AIPE";
        public const string GUID = "com.joan6694.illusionplugins.poseeditor";
        internal const int saveVersion = 0;
#elif HONEYSELECT2
        public const string Name = "HS2PE";
        public const string GUID = "com.joan6694.illusionplugins.poseeditor";
        internal const int saveVersion = 0;
#endif
        public const string Version = "2.19";

#if IPA
        public override string Name { get { return _name; } }
        public override string Version { get { return _versionNum; } }
#if HONEYSELECT
        public override string[] Filter { get { return new[] {"StudioNEO_32", "StudioNEO_64"}; } }
#elif PLAYHOME
        public override string[] Filter { get { return new[] { "PlayHomeStudio32bit", "PlayHomeStudio64bit" }; } }
#endif
#endif
        internal static new ManualLogSource Logger;

        internal static ConfigEntry<float> ConfigMainWindowSize { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> ConfigMainWindowShortcut { get; private set; }
        internal static ConfigEntry<bool> ConfigCrotchCorrectionByDefault { get; private set; }
        internal static ConfigEntry<bool> ConfigAnklesCorrectionByDefault { get; private set; }
        internal static ConfigEntry<bool> ConfigDisableAdvancedModeOnCopy { get; private set; }

        internal static ConfigEntry<KeyboardShortcut> ConfigReorderFKBones { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Logger = base.Logger;

            ConfigMainWindowSize = Config.Bind("Config", "Main Window Size", 1f);
            ConfigMainWindowShortcut = Config.Bind("Config", "Main Window Shortcut", new KeyboardShortcut(KeyCode.H));
            ConfigCrotchCorrectionByDefault = Config.Bind("Config", "Crotch Correction By Default", false);
            ConfigAnklesCorrectionByDefault = Config.Bind("Config", "AnklesCorrection By Default", false);
            ConfigDisableAdvancedModeOnCopy = Config.Bind("Config", "Disable advanced mode on copied objects", false, "If disabled, advanced mode state is copied from the original studio object. If enabled, advanced mode is always disabled on the newly copied items.");

            ConfigReorderFKBones = Config.Bind(
                "Config",
                "Reorganize bones",
                new KeyboardShortcut(KeyCode.R, KeyCode.RightControl),
                new ConfigDescription("Reorganizes the bones within the selected studio items " +
                    "according to their transform positions."));

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), GUID);
        }

#if AISHOUJO || HONEYSELECT2
        protected override void LevelLoaded(Scene scene, LoadSceneMode mode)
        {
            base.LevelLoaded(scene, mode);
            if (mode == LoadSceneMode.Single && scene.name.Equals("Studio"))
                this.gameObject.AddComponent<MainWindow>();
        }
#else
        protected override void LevelLoaded(int level)
        {
            base.LevelLoaded(level);
#if HONEYSELECT
            if (level == 3)
#elif SUNSHINE
            if (level == 2)
#elif KOIKATSU
            if (level == 1)
#elif PLAYHOME
            if (level == 1)
#endif
                gameObject.AddComponent<MainWindow>();
        }
#endif
    }
}