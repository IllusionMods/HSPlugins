#if HONEYSELECT
using System;
using System.Reflection;
using Harmony;
using IllusionPlugin;
using UnityEngine;

namespace VideoExport.ScreenshotPlugins
{
    public class Screencap : IScreenshotPlugin
    {
        private delegate byte[] CaptureFunctionDelegate();

        private CaptureFunctionDelegate _captureFunction;
        private Vector2 _currentSize;

        public string name { get { return "Screencap"; } }
        public Vector2 currentSize { get { return this._currentSize; } }
        public bool transparency { get { return false; } }
        public string extension { get { return "png"; } }
        public byte bitDepth { get { return 8; } }

        public bool Init(HarmonyInstance harmony)
        {
            Type screencapType = Type.GetType("ScreencapMB,Screencap");
            if (screencapType == null)
                return false;
            object plugin = GameObject.FindObjectOfType(screencapType);
            if (plugin == null)
                return false;
            MethodInfo captureOpaque = screencapType.GetMethod("CaptureOpaque", BindingFlags.NonPublic | BindingFlags.Instance);
            if (captureOpaque == null)
            {
                UnityEngine.Debug.LogError("VideoExport: Screencap was found but seems out of date, please update it.");
                return false;
            }
            this._captureFunction = (CaptureFunctionDelegate)Delegate.CreateDelegate(typeof(CaptureFunctionDelegate), plugin, captureOpaque);

            int downscalingRate = ModPrefs.GetInt("Screencap", "DownscalingRate", 1, true);
            this._currentSize = new Vector2(ModPrefs.GetInt("Screencap", "Width", 1280, true) * downscalingRate, ModPrefs.GetInt("Screencap", "Height", 720, true) * downscalingRate);
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

#elif KOIKATSU
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Illusion.Game;
using ToolBox.Extensions;
using UnityEngine;

namespace VideoExport.ScreenshotPlugins
{
    public class Screencap : IScreenshotPlugin
    {
        #region Private Types
        private enum CaptureType
        {
            Normal,
            ThreeHundredSixty
        }
        #endregion

        #region Accessors
        public string name { get { return "Screenshot Manager"; } }
        public Vector2 currentSize
        {
            get
            {
                switch (this._captureType)
                {
                    case CaptureType.Normal:
                        Vector3 size = new Vector2(this._resolutionX.Value * this._downscalingRate.Value, this._resolutionY.Value * this._downscalingRate.Value);
                        if (this._in3d)
                            size.x = (int)((size.x - size.x * this._imageSeparationOffset.Value) * 2);
                        return size;
                    case CaptureType.ThreeHundredSixty:
                        size = new Vector2(this._resolution360.Value, this._resolution360.Value / 2);
                        if (this._in3d)
                            size.x *= 2;
                        return size;
                }
                return Vector2.zero;
            }
        }
        public bool transparency { get { return this._captureAlpha.Value; } }
        public string extension { get { return this._useJpg.Value ? "jpg" : "png"; } }
        public byte bitDepth { get { return 8; } }
        #endregion

        #region Private Variables
        private static readonly HarmonyExtensions.Replacement[] _replacements = 
        {
            new HarmonyExtensions.Replacement() 
            {
                pattern = new[] 
                {
                    new CodeInstruction(OpCodes.Call, typeof(File).GetMethod("WriteAllBytes", BindingFlags.Public | BindingFlags.Static)),
                },
                replacer = new[] 
                {
                    new CodeInstruction(OpCodes.Call, typeof(Screencap).GetMethod(nameof(WriteAllBytesReplacement), BindingFlags.NonPublic | BindingFlags.Static)),
                }
            },
            new HarmonyExtensions.Replacement() 
            {
                pattern = new[] 
                {
                    new CodeInstruction(OpCodes.Callvirt, typeof(ManualLogSource).GetMethod("Log", BindingFlags.Instance | BindingFlags.Public)),
                },
                replacer = new[] 
                {
                    new CodeInstruction(OpCodes.Call, typeof(Screencap).GetMethod(nameof(LogReplacement), BindingFlags.NonPublic | BindingFlags.Static)),
                }
            },
            new HarmonyExtensions.Replacement() 
            {
                pattern = new[] 
                {
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Utils.Sound), "Play", new[]{typeof(SystemSE)})),
                },
                replacer = new[] 
                {
                    new CodeInstruction(OpCodes.Call, typeof(Screencap).GetMethod(nameof(PlayReplacement), BindingFlags.NonPublic | BindingFlags.Static)),
                }
            },
            new HarmonyExtensions.Replacement() 
            {
                pattern = new[] 
                {
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GL), "Clear", new[]{typeof(bool), typeof(bool), typeof(Color)})),
                },
                replacer = new[] 
                {
                    new CodeInstruction(OpCodes.Call, typeof(Screencap).GetMethod(nameof(ClearReplacement), BindingFlags.NonPublic | BindingFlags.Static)),
                }
            }
        };
        private static bool _videoExportCapture = false;
        private static byte[] _imageBytes;

        private Func<bool, IEnumerator> _takeCharScreenshot;
        private Func<bool, IEnumerator> _take360Screenshot;

        private CaptureType _captureType = CaptureType.Normal;
        private string[] _captureTypeNames;
        private bool _in3d;
        private ConfigEntry<int> _resolutionX;
        private ConfigEntry<int> _resolutionY;
        private ConfigEntry<int> _resolution360;
        private ConfigEntry<float> _imageSeparationOffset;
        private ConfigEntry<int> _downscalingRate;
        private ConfigEntry<bool> _captureAlpha;
        private ConfigEntry<bool> _useJpg;
        #endregion

        #region Public Methods
        public bool Init(Harmony harmony)
        {
            Type screencapType = Type.GetType("Screencap.ScreenshotManager,Screencap");
            if (screencapType == null)
                return false;
            object screenshotManager = GameObject.FindObjectOfType(screencapType);
            if (screenshotManager == null)
                return false;
            this._captureType = (CaptureType)VideoExport._configFile.AddInt("Screencap_captureType", 0, true);
            this._in3d = VideoExport._configFile.AddBool("Screencap_in3d", false, true);
            this._resolutionX = (ConfigEntry<int>)screencapType.GetPrivateProperty("ResolutionX");
            this._resolutionY = (ConfigEntry<int>)screencapType.GetPrivateProperty("ResolutionY");
            this._resolution360 = (ConfigEntry<int>)screencapType.GetPrivateProperty("Resolution360");
            this._imageSeparationOffset = (ConfigEntry<float>)screencapType.GetPrivateProperty("ImageSeparationOffset");
            this._downscalingRate = (ConfigEntry<int>)screencapType.GetPrivateProperty("DownscalingRate");
            this._captureAlpha = (ConfigEntry<bool>)screencapType.GetPrivateProperty("CaptureAlpha");
            this._useJpg = (ConfigEntry<bool>)screencapType.GetPrivateProperty("UseJpg");
            MethodInfo takeCharScreenshot = screencapType.GetMethod("TakeCharScreenshot", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo take360Screenshot = screencapType.GetMethod("Take360Screenshot", BindingFlags.NonPublic | BindingFlags.Instance);
            if (takeCharScreenshot == null || take360Screenshot == null)
                return false;
            this._takeCharScreenshot = (Func<bool, IEnumerator>)Delegate.CreateDelegate(typeof(Func<bool, IEnumerator>), screenshotManager, takeCharScreenshot);
            this._take360Screenshot = (Func<bool, IEnumerator>)Delegate.CreateDelegate(typeof(Func<bool, IEnumerator>), screenshotManager, take360Screenshot);
            if (this._take360Screenshot == null || this._takeCharScreenshot == null)
                return false;
            try
            {
                harmony.Patch(screencapType.GetCoroutineMethod("TakeCharScreenshot"), transpiler: new HarmonyMethod(typeof(Screencap).GetMethod(nameof(GeneralTranspiler), BindingFlags.Static | BindingFlags.NonPublic)));
                harmony.Patch(screencapType.GetCoroutineMethod("Take360Screenshot"), transpiler: new HarmonyMethod(typeof(Screencap).GetMethod(nameof(GeneralTranspiler), BindingFlags.Static | BindingFlags.NonPublic)));
                harmony.Patch(Type.GetType("alphaShot.AlphaShot2,Screencap").GetMethod("PerformCapture"), transpiler: new HarmonyMethod(typeof(Screencap).GetMethod(nameof(GeneralTranspiler), BindingFlags.Static | BindingFlags.NonPublic)));
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("VideoExport: Couldn't patch method.\n" + e);
                return false;
            }
            return true;
        }

        public void UpdateLanguage()
        {
            this._captureTypeNames = new[]
            {
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.ScreencapCaptureTypeNormal),
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.ScreencapCaptureType360)
            };
        }

        public void OnStartRecording()
        {
        }

        public byte[] Capture(string path)
        {
            _videoExportCapture = true;
            IEnumerator e = null;
            switch (this._captureType)
            {
                case CaptureType.Normal:
                    e = this._takeCharScreenshot(this._in3d);
                    break;
                case CaptureType.ThreeHundredSixty:
                    e = this._take360Screenshot(this._in3d);
                    break;
            }
            while (e.MoveNext())
                ;
            _videoExportCapture = false;
            return _imageBytes;
        }

        public void OnEndRecording()
        {
        }

        public void DisplayParams()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.ScreencapCaptureType), GUILayout.ExpandWidth(false));
                this._captureType = (CaptureType)GUILayout.SelectionGrid((int)this._captureType, this._captureTypeNames, 2);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            this._in3d = GUILayout.Toggle(this._in3d, VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.Screencap3D));
            GUILayout.EndHorizontal();
        }

        public void SaveParams()
        {
            VideoExport._configFile.SetInt("Screencap_captureType", (int)this._captureType);
            VideoExport._configFile.SetBool("Screencap_in3d", this._in3d);
        }
        #endregion

        #region Private Methods
        private static void WriteAllBytesReplacement(string path, byte[] bytes)
        {
            if (_videoExportCapture)
                _imageBytes = bytes;
            else
                File.WriteAllBytes(path, bytes);
        }

        private static void LogReplacement(ManualLogSource self, BepInEx.Logging.LogLevel level, object obj)
        {
            if (!_videoExportCapture)
                self.Log(level, obj);
        }

        private static void PlayReplacement(SystemSE se)
        {
            if (!_videoExportCapture)
                Utils.Sound.Play(se);
        }

        private static void ClearReplacement(bool clearDepth, bool clearColor, Color backgroundColor)
        {
            if (!_videoExportCapture)
                GL.Clear(clearDepth, clearColor, backgroundColor);
        }

        private static IEnumerable<CodeInstruction> GeneralTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return HarmonyExtensions.ReplaceCodePattern(instructions, _replacements);
        }
        #endregion
    }
}

#elif AISHOUJO || HONEYSELECT2
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Illusion.Game;
using ToolBox.Extensions;
using UnityEngine;

namespace VideoExport.ScreenshotPlugins
{
    public class Screencap : IScreenshotPlugin
    {
        #region Accessors
        public string name { get { return "Screenshot Manager"; } }
        public Vector2 currentSize { get { return new Vector2(this._captureWidth.Value, this._captureHeight.Value); } }
        public bool transparency { get { return this._alpha.Value; } }
        public string extension { get { return "png"; } }
        public byte bitDepth { get { return 8; } }
        #endregion

        #region Private Variables
        private static bool _videoExportCapture = false;
        private static byte[] _imageBytes;
        private static Texture2D _texture = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, false, true);

        private Action<bool> _capture;

        private ConfigEntry<int> _captureWidth;
        private ConfigEntry<int> _captureHeight;
        private ConfigEntry<bool> _alpha;
        #endregion

        #region Public Methods
        public bool Init(Harmony harmony)
        {
#if AISHOUJO
            Type screencapType = Type.GetType("Screencap.ScreenshotManager,AI_Screencap");
#elif HONEYSELECT2
            Type screencapType = Type.GetType("Screencap.ScreenshotManager,HS2_Screencap");
#endif
            if (screencapType == null)
                return false;
            object screenshotManager = GameObject.FindObjectOfType(screencapType);
            if (screenshotManager == null)
                return false;
            this._captureWidth = (ConfigEntry<int>)screenshotManager.GetPrivateProperty("CaptureWidth");
            this._captureHeight = (ConfigEntry<int>)screenshotManager.GetPrivateProperty("CaptureHeight");
            this._alpha = (ConfigEntry<bool>)screenshotManager.GetPrivateProperty("Alpha");

            MethodInfo capture = screencapType.GetMethod("CaptureAndWrite", BindingFlags.NonPublic | BindingFlags.Instance);
            if (capture == null)
                return false;
            this._capture = (Action<bool>)Delegate.CreateDelegate(typeof(Action<bool>), screenshotManager, capture);
            if (this._capture == null)
                return false;
            try
            {
                harmony.Patch(screencapType.GetMethod("WriteTex", BindingFlags.NonPublic | BindingFlags.Instance), new HarmonyMethod(typeof(Screencap).GetMethod(nameof(WriteTex_Prefix), BindingFlags.Static | BindingFlags.NonPublic)));
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("VideoExport: Couldn't patch method.\n" + e);
                return false;
            }
            return true;
        }

        public void UpdateLanguage()
        {
        }

        public void OnStartRecording() { }

        public byte[] Capture(string saveTo)
        {
            _videoExportCapture = true;
            this._capture(this._alpha.Value);
            _videoExportCapture = false;
            return _imageBytes;
        }

        public void OnEndRecording() { }

        public void DisplayParams()
        {
        }

        public void SaveParams()
        {
        }
        #endregion

        #region Private Methods
        private static bool WriteTex_Prefix(RenderTexture rt, bool alpha, ref IEnumerator __result)
        {
            if (_videoExportCapture)
            {
                RenderTexture cached = RenderTexture.active;
                RenderTexture.active = rt;
                if (_texture.width != rt.width || _texture.height != rt.height)
                    _texture = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false, true);
                _texture.ReadPixels(new Rect(0f, 0f, rt.width, rt.height), 0, 0, false);
                RenderTexture.ReleaseTemporary(RenderTexture.active);
                RenderTexture.active = cached;
                _texture.Apply(false);
                _imageBytes = _texture.EncodeToPNG();
                __result = RoutineReplacement();
                GL.Clear(false, true, Color.clear);
                return false;
            }
            return true;
        }

        private static IEnumerator RoutineReplacement()
        {
            yield break;
        }
        #endregion
    }
}
#endif