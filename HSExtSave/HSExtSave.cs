using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Xml;
using Harmony;
using IllusionPlugin;
#if PLAYHOME
using SEXY;
#endif

namespace HSExtSave
{
    public class HSExtSave : IPlugin
    {
        #region Public Types
        public delegate void ExtSaveCharReadHandler(CharFile charFile, XmlNode node);
        public delegate void ExtSaveCharWriteHandler(CharFile charFile, XmlTextWriter writer);
        public delegate void ExtSaveSceneReadHandler(string path, XmlNode node);
        public delegate void ExtSaveSceneWriteHandler(string path, XmlTextWriter writer);
        public delegate void ExtSaveClothesReadHandler(CharFileInfoClothes clothesInfo, XmlNode node);
        public delegate void ExtSaveClothesWriteHandler(CharFileInfoClothes clothesInfo, XmlTextWriter writer);
        #endregion

        #region Private Types
        internal class HandlerGroup
        {
            public ExtSaveCharReadHandler onCharRead;
            public ExtSaveCharWriteHandler onCharWrite;
            public ExtSaveSceneReadHandler onSceneReadLoad;
            public ExtSaveSceneReadHandler onSceneReadImport;
            public ExtSaveSceneWriteHandler onSceneWrite;
            public ExtSaveClothesReadHandler onClothesRead;
            public ExtSaveClothesWriteHandler onClothesWrite;
        }
        internal enum Binary
        {
            Neo,
            Game,
        }
        #endregion

        #region Private Variables
        internal static Dictionary<string, HandlerGroup> _handlers = new Dictionary<string, HandlerGroup>();
        internal static Binary _binary = Binary.Neo;
#if HONEYSELECT
        internal const string _logPrefix = "HSExtSave: ";
#elif PLAYHOME
        internal const string _logPrefix = "PHExtSave: ";
#endif
        #endregion

        #region Public Accessors
#if HONEYSELECT
        public string Name { get { return "HSExtSave"; } }
        public string Version { get { return "1.0.1"; } }
#elif PLAYHOME
        public string Name { get { return "PHExtSave"; } }
        public string Version { get { return "1.0.1"; } }
#endif
        #endregion

        #region Unity Methods
        public void OnApplicationStart()
        {
            switch (Process.GetCurrentProcess().ProcessName)
            {
                case "PlayHome32bit":
                case "PlayHome64bit":
                case "HoneySelect_32":
                case "HoneySelect_64":
                    _binary = Binary.Game;
                    break;
                case "PlayHomeStudio32bit":
                case "PlayHomeStudio64bit":
                case "StudioNEO_32":
                case "StudioNEO_64":
                    _binary = Binary.Neo;
                    break;
            }

            HarmonyInstance harmony = HarmonyInstance.Create("com.joan6694.hsplugins.charextsave");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public void OnLevelWasInitialized(int level)
        {
        }

        public void OnLevelWasLoaded(int level)
        {
        }

        public void OnApplicationQuit()
        {
        }

        public void OnUpdate()
        {
        }

        public void OnLateUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Used to register a handler.
        /// </summary>
        /// <param name="name">Name of the handler: it must be unique and without special characters.</param>
        /// <param name="onCharRead">Callback that will be called when the plugin reads a character card.</param>
        /// <param name="onCharWrite">Callback that will be called when the plugin saves a character card.</param>
        /// <param name="onSceneLoad">Callback that will be called when the plugin reads a scene card.</param>
        /// <param name="onSceneImport">Callback that will be called when the plugin reads a scene card (import).</param>
        /// <param name="onSceneWrite">Callback that will be called when the plugin saves a scene card.</param>
        /// <returns>true if the operation was successful, otherwise false</returns>
        public static bool RegisterHandler(string name, ExtSaveCharReadHandler onCharRead, ExtSaveCharWriteHandler onCharWrite, ExtSaveSceneReadHandler onSceneLoad, ExtSaveSceneReadHandler onSceneImport, ExtSaveSceneWriteHandler onSceneWrite, ExtSaveClothesReadHandler onClothesRead, ExtSaveClothesWriteHandler onClothesWrite)
        {
            if (string.IsNullOrEmpty(name))
            {
                UnityEngine.Debug.LogError(HSExtSave._logPrefix + "Name of the handler must not be null or empty...");
                return false;
            }
            HandlerGroup group;
            if (_handlers.TryGetValue(name, out group))
                UnityEngine.Debug.LogWarning(HSExtSave._logPrefix + "Handler is already registered, updating callbacks...");
            else
            {
                group = new HandlerGroup();
                _handlers.Add(name, group);
            }
            group.onCharRead = onCharRead;
            group.onCharWrite = onCharWrite;
            group.onSceneReadLoad = onSceneLoad;
            group.onSceneReadImport = onSceneImport;
            group.onSceneWrite = onSceneWrite;
            group.onClothesRead = onClothesRead;
            group.onClothesWrite = onClothesWrite;
            return true;
        }

        /// <summary>
        /// Used to unregister a handler.
        /// </summary>
        /// <param name="name">Name of the handler: it must be unique and without special characters.</param>
        /// <returns>true if the operation was successful, false otherwise</returns>
        public static bool UnregisterHandler(string name)
        {
            if (_handlers.ContainsKey(name))
            {
                _handlers.Remove(name);
                return true;
            }
            UnityEngine.Debug.LogWarning(HSExtSave._logPrefix + "Handler is not registered, operation will be ignored...");
            return false;
        }
        #endregion
    }
}
