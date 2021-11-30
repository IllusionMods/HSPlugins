#if IPA
using IllusionPlugin;
#elif BEPINEX
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using System.Collections.Generic;
#endif

namespace ToolBox
{
    public class GenericConfig
    {
        private readonly string _name;
#if BEPINEX
        private readonly ConfigFile _configFile;
#endif

        public GenericConfig(string name, GenericPlugin plugin = null)
        {
            this._name = name;
#if BEPINEX
            if (plugin != null && plugin.Config != null)
                this._configFile = plugin.Config;
            else
                this._configFile = new ConfigFile(Path.Combine(Paths.ConfigPath, this._name + ".cfg"), true);
#endif
        }

#if BEPINEX
        private ConfigEntry<T> GetOrAddEntry<T>(string key, T defaultValue, string description = null)
        {
            ConfigEntry<T> entry;
            if (this._configFile.TryGetEntry(this._name, key, out entry) == false)
            {
                if (description == null)
                    entry = this._configFile.Bind(this._name, key, defaultValue, new ConfigDescription("", null, "Advanced"));
                else
                    entry = this._configFile.Bind(this._name, key, defaultValue, new ConfigDescription(description));
            }
            return entry;
        }

        private ConfigEntry<T> GetEntry<T>(string key)
        {
            ConfigEntry<T> entry;
            if (this._configFile.TryGetEntry(this._name, key, out entry))
                return entry;
            return null;
        }
#endif

        public string AddString(string key, string defaultValue, bool autoSave, string description = null)
        {
#if IPA
            return ModPrefs.GetString(this._name, key, defaultValue, autoSave);
#elif BEPINEX
            return this.GetOrAddEntry(key, defaultValue, description).Value;
#endif
        }

        public void SetString(string key, string value)
        {
#if IPA
            ModPrefs.SetString(this._name, key, value);
#elif BEPINEX
            this.GetEntry<string>(key).Value = value;
#endif
        }

        public int AddInt(string key, int defaultValue, bool autoSave, string description = null)
        {
#if IPA
            return ModPrefs.GetInt(this._name, key, defaultValue, autoSave);
#elif BEPINEX
            return this.GetOrAddEntry(key, defaultValue, description).Value;
#endif
        }

        public void SetInt(string key, int value)
        {
#if IPA
            ModPrefs.SetInt(this._name, key, value);
#elif BEPINEX
            this.GetEntry<int>(key).Value = value;
#endif
        }

        public bool AddBool(string key, bool defaultValue, bool autoSave, string description = null)
        {
#if IPA
            return ModPrefs.GetBool(this._name, key, defaultValue, autoSave);
#elif BEPINEX
            return this.GetOrAddEntry(key, defaultValue, description).Value;
#endif
        }

        public void SetBool(string key, bool value)
        {
#if IPA
            ModPrefs.SetBool(this._name, key, value);
#elif BEPINEX
            this.GetEntry<bool>(key).Value = value;
#endif
        }

        public float AddFloat(string key, float defaultValue, bool autoSave, string description = null)
        {
#if IPA
            return ModPrefs.GetFloat(this._name, key, defaultValue, autoSave);
#elif BEPINEX
            return this.GetOrAddEntry(key, defaultValue, description).Value;
#endif
        }

        public void SetFloat(string key, float value)
        {
#if IPA
            ModPrefs.SetFloat(this._name, key, value);
#elif BEPINEX
            this.GetEntry<float>(key).Value = value;
#endif
        }

        public void Save()
        {
#if BEPINEX
            this._configFile.Save();
#endif
        }
    }
}
