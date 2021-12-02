#if HONEYSELECT
using System;
using System.Collections.Generic;
using System.Reflection;
using Harmony;
using IllusionPlugin;
using ToolBox.Extensions;
using UnityEngine;

namespace VideoExport.ScreenshotPlugins
{
    public class HoneyShot : IScreenshotPlugin
    {
        private delegate byte[] CaptureFunctionDelegate();

        private CaptureFunctionDelegate _captureFunction;

        public string name { get { return "HoneyShot"; } }
        public Vector2 currentSize { get { return new Vector2(ModPrefs.GetInt("HoneyShot", "output_width"), ModPrefs.GetInt("HoneyShot", "output_height")); } }
        public bool transparency { get { return false; } }
        public string extension { get { return ModPrefs.GetBool("HoneyShot", "use_jpeg") ? "jpg" : "png"; } }
        public byte bitDepth { get { return 8; } }

        public bool Init(HarmonyInstance harmony)
        {
            Type honeyShotType = Type.GetType("HoneyShot.HoneyShotPlugin,HoneyShot");
            if (honeyShotType == null)
                return false;
            Component c = GameObject.Find("IPA_PluginManager").GetComponent(Type.GetType("IllusionInjector.PluginComponent,IllusionInjector"));
            if (c == null)
                return false;
            IPlugin honeyShotPlugin = null;
            foreach (IPlugin plugin in (List<IPlugin>)c.GetPrivate("plugins").GetPrivate("plugins"))
            {
                if (plugin.GetType() == honeyShotType)
                {
                    honeyShotPlugin = plugin;
                    break;
                }
            }
            if (honeyShotPlugin == null)
                return false;
            MethodInfo captureUsingCameras = honeyShotType.GetMethod("CaptureUsingCameras", BindingFlags.NonPublic | BindingFlags.Instance);
            if (captureUsingCameras == null)
            {
                UnityEngine.Debug.LogError("VideoExport: HoneyShot was found but seems out of date, please update it.");
                return false;                
            }
            this._captureFunction = (CaptureFunctionDelegate)Delegate.CreateDelegate(typeof(CaptureFunctionDelegate), honeyShotPlugin, captureUsingCameras);
            return true;
        }

        public void UpdateLanguage()
        {
        }

        public void OnStartRecording()
        {
        }

        public byte[] Capture(string saveTo)
        {
            return this._captureFunction();
        }

        public void OnEndRecording()
        {
        }

        public void DisplayParams()
        {
        }

        public void SaveParams()
        {
        }
    }
}
#endif