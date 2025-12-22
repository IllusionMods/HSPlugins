using System;
using HarmonyLib;
using Screencap;
using UnityEngine;

namespace VideoExport.ScreenshotPlugins
{
    using static ScreenshotManager;

    public class ScreencapPlugin : IScreenshotPlugin
    {
        #region Privates
        private enum CaptureType
        {
            Normal,
            ThreeHundredSixty
        }
        private CaptureType _captureType = CaptureType.Normal;
        private string[] _captureTypeNames;
        private bool _in3d;
        private bool uiShow;
        private Traverse PluginInstance;
        private Type _screenshotManagerType;
        private bool _isPluginInstanceInitialized;
        #endregion

        #region Interface
        public string name => "Screenshot Manager";

        public Vector2 currentSize
        {
            get
            {
                switch (_captureType)
                {
                    case CaptureType.Normal:
                        Vector3 size = new Vector2(ResolutionX.Value, ResolutionY.Value);
                        if (_in3d) size.x = (size.x - (int)(size.x * ImageSeparationOffset.Value)) * 2;
                        return size;
                    case CaptureType.ThreeHundredSixty:
                        size = new Vector2(Resolution360.Value, Mathf.FloorToInt(Resolution360.Value / 2f));
                        if (_in3d) size.x *= 2;
                        return size;
                    default:
                        throw new NotSupportedException($"Unsupported capture type: {_captureType}");
                }
            }
        }
        public VideoExport.ImgFormat imageFormat => UseJpg.Value ? VideoExport.ImgFormat.JPG : VideoExport.ImgFormat.PNG;

        public bool transparency => CaptureAlphaMode.Value != AlphaMode.None;
        public string extension => UseJpg.Value ? "jpg" : "png";
        public byte bitDepth => 8;

        public bool Init(Harmony harmony)
        {
            _captureType = (CaptureType)VideoExport._configFile.AddInt("Screencap_captureType", 0, true);
            _in3d = VideoExport._configFile.AddBool("Screencap_in3d", false, true);

            InitializePluginInstance();

            return true;
        }

        private object _pluginInstanceObject;

        private void InitializePluginInstance()
        {
            if (_isPluginInstanceInitialized) return;

            _screenshotManagerType = AccessTools.TypeByName("Screencap.ScreenshotManager");
            if (_screenshotManagerType != null)
            {
                var allObjects = Resources.FindObjectsOfTypeAll(_screenshotManagerType);
                if (allObjects != null && allObjects.Length > 0)
                {
                    _pluginInstanceObject = allObjects[0];
                    PluginInstance = Traverse.Create(_pluginInstanceObject);
                    _isPluginInstanceInitialized = true;
                }
            }
        }

        public void SaveParams()
        {
            VideoExport._configFile.SetInt("Screencap_captureType", (int)_captureType);
            VideoExport._configFile.SetBool("Screencap_in3d", _in3d);
        }

        public void UpdateLanguage()
        {
            _captureTypeNames = new[]
            {
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.ScreencapCaptureTypeNormal),
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.ScreencapCaptureType360)
            };
        }

        public void OnStartRecording()
        {
            FirePreCapture();
        }

        public byte[] Capture(string path)
        {
            var tex = CaptureTexture();
            var bytes = UseJpg.Value ? tex.EncodeToJPG(JpgQuality.Value) : tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);
            return bytes;
        }

        public bool IsTextureCaptureAvailable()
        {
            return true;
        }

        public bool IsRenderTextureCaptureAvailable()
        {
#if (!KOIKATSU || SUNSHINE)
            return true;
#else
            return false;
#endif
        }

        public Texture2D CaptureTexture()
        {
            RenderTexture result;
            switch (_captureType)
            {
                case CaptureType.Normal:
                    result = !_in3d ? CaptureRender() : Do3DCapture(() => CaptureRender());
                    break;
                case CaptureType.ThreeHundredSixty:
                    result = !_in3d ? Capture360() : Do3DCapture(() => Capture360(), overlapOffset: 0);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported capture type: {_captureType}");
            }

            if (!result) return null;
            var texture = ToTexture2D(result);
            return texture;
        }

        public RenderTexture CaptureRenderTexture()
        {
            RenderTexture result;
            switch (_captureType)
            {
                case CaptureType.Normal:
                    result = !_in3d ? CaptureRender() : Do3DCapture(() => CaptureRender());
                    break;
                case CaptureType.ThreeHundredSixty:
                    result = !_in3d ? Capture360() : Do3DCapture(() => Capture360(), overlapOffset: 0);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported capture type: {_captureType}");
            }

            if (!result) return null;
            return result;
        }

        public void OnEndRecording()
        {
            FirePostCapture();
        }

        public void DisplayParams()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.ScreencapCaptureType), GUILayout.ExpandWidth(false));
                _captureType = (CaptureType)GUILayout.SelectionGrid((int)_captureType, _captureTypeNames, 2);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            _in3d = GUILayout.Toggle(_in3d, VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.Screencap3D));
            GUILayout.EndHorizontal();


            if (!_isPluginInstanceInitialized)
            {
                InitializePluginInstance();
            }

            if (_isPluginInstanceInitialized && PluginInstance != null)
            {
                GUILayout.BeginHorizontal();
                {
                    uiShow = PluginInstance.Field("_uiShow").GetValue<bool>();

                    if (GUILayout.Button(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.ToggleScreencapUI)))
                    {
                        var keyGuiField = PluginInstance.Field("KeyGui").GetValue();

                        bool currentShow = PluginInstance.Field("_uiShow").GetValue<bool>();
                        Rect currentRect = PluginInstance.Field("_uiRect").GetValue<Rect>();
                        PluginInstance.Field("_uiShow").SetValue(!currentShow);
                        PluginInstance.Field("_uiRect").SetValue(currentRect);
                    }
                }
                GUILayout.EndHorizontal();
            }

        }
        #endregion

        private static Texture2D ToTexture2D(RenderTexture rt)
        {
            var cached = RenderTexture.active;
            RenderTexture.active = rt;

            var texture = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false, true);
            texture.ReadPixels(new Rect(0f, 0f, rt.width, rt.height), 0, 0, false);

            RenderTexture.active = cached;

            RenderTexture.ReleaseTemporary(rt);

            texture.Apply();
            return texture;
        }
    }
}