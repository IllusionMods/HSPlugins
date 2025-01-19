#if KOIKATSU
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Illusion.Game;
using Screencap;
using ToolBox.Extensions;
using UnityEngine;

namespace VideoExport.ScreenshotPlugins
{
    public class ScreencapPlugin : IScreenshotPlugin
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
        public VideoExport.ImgFormat imageFormat { get { return VideoExport.ImgFormat.PNG; } }

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
                    new CodeInstruction(OpCodes.Call, typeof(File).GetMethod(nameof(File.WriteAllBytes), BindingFlags.Public | BindingFlags.Static)),
                },
                replacer = new[]
                {
                    new CodeInstruction(OpCodes.Call, typeof(ScreencapPlugin).GetMethod(nameof(WriteAllBytesReplacement), BindingFlags.NonPublic | BindingFlags.Static)),
                }
            },
            new HarmonyExtensions.Replacement()
            {
                pattern = new[]
                {
                    new CodeInstruction(OpCodes.Callvirt, typeof(ManualLogSource).GetMethod(nameof(ManualLogSource.Log), BindingFlags.Instance | BindingFlags.Public)),
                },
                replacer = new[]
                {
                    new CodeInstruction(OpCodes.Call, typeof(ScreencapPlugin).GetMethod(nameof(LogReplacement), BindingFlags.NonPublic | BindingFlags.Static)),
                }
            },
            new HarmonyExtensions.Replacement()
            {
                pattern = new[]
                {
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Utils.Sound), nameof(Utils.Sound.Play), new[]{typeof(SystemSE)})),
                },
                replacer = new[]
                {
                    new CodeInstruction(OpCodes.Call, typeof(ScreencapPlugin).GetMethod(nameof(PlayReplacement), BindingFlags.NonPublic | BindingFlags.Static)),
                }
            },
            new HarmonyExtensions.Replacement()
            {
                pattern = new[]
                {
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GL), nameof(GL.Clear), new[]{typeof(bool), typeof(bool), typeof(Color)})),
                },
                replacer = new[]
                {
                    new CodeInstruction(OpCodes.Call, typeof(ScreencapPlugin).GetMethod(nameof(ClearReplacement), BindingFlags.NonPublic | BindingFlags.Static)),
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
            var screenshotManager = Screencap.ScreenshotManager.Instance;
            if (screenshotManager == null)
                return false;
            this._captureType = (CaptureType)VideoExport._configFile.AddInt("Screencap_captureType", 0, true);
            this._in3d = VideoExport._configFile.AddBool("Screencap_in3d", false, true);
            this._resolutionX = Screencap.ScreenshotManager.ResolutionX;
            this._resolutionY = Screencap.ScreenshotManager.ResolutionY;
            this._resolution360 = Screencap.ScreenshotManager.Resolution360;
            this._imageSeparationOffset = Screencap.ScreenshotManager.ImageSeparationOffset;
            this._downscalingRate = Screencap.ScreenshotManager.DownscalingRate;
            this._captureAlpha = Screencap.ScreenshotManager.CaptureAlpha;
            this._useJpg = Screencap.ScreenshotManager.UseJpg;

            var tv = Traverse.Create(screenshotManager);
            var takeCharScreenshot = tv.Method("TakeCharScreenshot", new[] { typeof(bool) });
            var take360Screenshot = tv.Method("Take360Screenshot", new[] { typeof(bool) });
            if (!takeCharScreenshot.MethodExists() || !take360Screenshot.MethodExists())
                return false;
            this._takeCharScreenshot = in3D => takeCharScreenshot.GetValue<IEnumerator>(in3D);
            this._take360Screenshot = in3D => take360Screenshot.GetValue<IEnumerator>(in3D);
            try
            {
                harmony.Patch(typeof(Screencap.ScreenshotManager).GetCoroutineMethod("TakeCharScreenshot"), transpiler: new HarmonyMethod(typeof(ScreencapPlugin).GetMethod(nameof(GeneralTranspiler), BindingFlags.Static | BindingFlags.NonPublic)));
                harmony.Patch(typeof(Screencap.ScreenshotManager).GetCoroutineMethod("Take360Screenshot"), transpiler: new HarmonyMethod(typeof(ScreencapPlugin).GetMethod(nameof(GeneralTranspiler), BindingFlags.Static | BindingFlags.NonPublic)));
                harmony.Patch(typeof(alphaShot.AlphaShot2).GetMethod("PerformCapture"), transpiler: new HarmonyMethod(typeof(ScreencapPlugin).GetMethod(nameof(GeneralTranspiler), BindingFlags.Static | BindingFlags.NonPublic)));
            }
            catch (Exception e)
            {
                VideoExport.Logger.LogError("VideoExport: Couldn't patch method.\n" + e);
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

        public bool IsTextureCaptureAvailable()
        {
            return _captureType == CaptureType.Normal && !_in3d;
        }

        public Texture2D CaptureTexture()
        {
            _videoExportCapture = true;
            Texture2D texture = ScreenshotManager.Instance.Capture(
                ScreenshotManager.ResolutionX.Value,
                ScreenshotManager.ResolutionY.Value,
                ScreenshotManager.DownscalingRate.Value,
                ScreenshotManager.CaptureAlpha.Value
            );
            _videoExportCapture = false;
            return texture;
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

#if !SUNSHINE
        private static void PlayReplacement(SystemSE se)
        {
            if (!_videoExportCapture)
                Utils.Sound.Play(se);
        }
#else
        private static AudioSource PlayReplacement(SystemSE se)
        {
            if (!_videoExportCapture)
                return Utils.Sound.Play(se);
            return null;
        }
#endif

        private static void ClearReplacement(bool clearDepth, bool clearColor, Color backgroundColor)
        {
            if (!_videoExportCapture)
                GL.Clear(clearDepth, clearColor, backgroundColor);
        }

        private static IEnumerable<CodeInstruction> GeneralTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            //return instructions;
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
    public class ScreencapPlugin : IScreenshotPlugin
    {
#region Accessors
        public string name { get { return "Screenshot Manager"; } }
        public Vector2 currentSize { get { return new Vector2(this._captureWidth.Value, this._captureHeight.Value); } }
        public bool transparency { get { return this._alpha.Value; } }
        public string extension { get { return "png"; } }
        public byte bitDepth { get { return 8; } }
        public VideoExport.ImgFormat imageFormat { get { return VideoExport.ImgFormat.PNG; } }
#endregion

#region Private Variables
        private static bool _videoExportCapture = false;
        private static byte[] _imageBytes;
        private static Texture2D _texture = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, false, true);

        private Action<bool> _capture;
        private Func<int, int, int, bool, RenderTexture> _captureTexture;

        private ConfigEntry<int> _captureWidth;
        private ConfigEntry<int> _captureHeight;
        private ConfigEntry<int> _downscaling;
        private ConfigEntry<bool> _alpha;
#endregion

#region Public Methods
        public bool Init(Harmony harmony)
        {
            var screencapType = typeof(Screencap.ScreenshotManager);
            var screenshotManager = GameObject.FindObjectOfType(screencapType);
            if (screenshotManager == null)
                return false;
            this._captureWidth = (ConfigEntry<int>)screenshotManager.GetPrivateProperty("CaptureWidth");
            this._captureHeight = (ConfigEntry<int>)screenshotManager.GetPrivateProperty("CaptureHeight");
            this._alpha = (ConfigEntry<bool>)screenshotManager.GetPrivateProperty("Alpha");

            PropertyInfo downscalingProperty = screencapType.GetProperty("Downscaling", BindingFlags.Static | BindingFlags.NonPublic);
            if (downscalingProperty != null)
                _downscaling = downscalingProperty.GetValue(null) as ConfigEntry<int>;

            MethodInfo capture = screencapType.GetMethod("CaptureAndWrite", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo captureTexture = screencapType.GetMethod("Capture", BindingFlags.Public | BindingFlags.Instance);
            if (capture == null || captureTexture == null)
                return false;
            this._capture = (Action<bool>)Delegate.CreateDelegate(typeof(Action<bool>), screenshotManager, capture);
            this._captureTexture = (Func<int, int, int, bool, RenderTexture>)Delegate.CreateDelegate(typeof(Func<int, int, int, bool, RenderTexture>), screenshotManager, captureTexture);
            if (this._capture == null || this._captureTexture == null)
                return false;
            try
            {
                harmony.Patch(screencapType.GetMethod("WriteTex", BindingFlags.NonPublic | BindingFlags.Instance), new HarmonyMethod(typeof(ScreencapPlugin).GetMethod(nameof(WriteTex_Prefix), BindingFlags.Static | BindingFlags.NonPublic)));
            }
            catch (Exception e)
            {
                VideoExport.Logger.LogError("VideoExport: Couldn't patch method.\n" + e);
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
            Texture2D texture = this.CaptureTexture();
            _videoExportCapture = false;
            byte[] result = texture.EncodeToPNG();
            UnityEngine.Object.Destroy(texture);
            return result;
        }

        public bool IsTextureCaptureAvailable()
        {
            return true;
        }

        public Texture2D CaptureTexture()
        {
            RenderTexture rt = this._captureTexture(_captureWidth.Value, _captureHeight.Value, _downscaling.Value, _alpha.Value);
            RenderTexture cached = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D texture = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false, true);
            texture.ReadPixels(new Rect(0f, 0f, rt.width, rt.height), 0, 0, false);
            RenderTexture.ReleaseTemporary(RenderTexture.active);
            RenderTexture.active = cached;
            texture.Apply();

            return texture;
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