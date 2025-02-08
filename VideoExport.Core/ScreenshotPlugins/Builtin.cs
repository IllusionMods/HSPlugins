using System;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Linq;
using VideoExport.Core;


#if IPA
using Harmony;
#elif BEPINEX
using HarmonyLib;
#endif

namespace VideoExport.ScreenshotPlugins
{
    public class Builtin : IScreenshotPlugin
    {
        private enum CaptureMode
        {
            Normal,
            Immediate,
        }

        public string name { get { return VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.BuiltInCaptureTool); } }
        public Vector2 currentSize
        {
            get
            {
                int width = Mathf.RoundToInt(Screen.width * this._scaleFactor);
                int height = Mathf.RoundToInt(Screen.height * this._scaleFactor);

                if (width % 2 != 0)
                    width += 1;
                if (height % 2 != 0)
                    height += 1;

                return new Vector2(width, height);
            }
        }
        public bool transparency { get { return false; } }
        public string extension
        {
            get
            {
                switch (this._imageFormat)
                {
                    default:
                    case VideoExport.ImgFormat.BMP:
                        return "bmp";
                    case VideoExport.ImgFormat.PNG:
                        return "png";
#if !HONEYSELECT
                    case VideoExport.ImgFormat.EXR:
                        return "exr";
#endif
                }
            }
        }
        public byte bitDepth
        {
            get
            {
#if HONEYSELECT
                return 8;
#else
                return (byte)(this._imageFormat == VideoExport.ImgFormat.EXR ? 10 : 8);
#endif
            }
        }

        public VideoExport.ImgFormat imageFormat { get { return _imageFormat; } }

        private float _scaleFactor;
        private CaptureMode _captureMode;
        private string[] _captureModeNames;
        private VideoExport.ImgFormat _imageFormat;
        private string[] _imageFormatNames;

        private RenderTexture _cachedRenderTexture;

#if IPA
        public bool Init(HarmonyInstance harmony)
#elif BEPINEX
        public bool Init(Harmony harmony)
#endif
        {
            this._scaleFactor = VideoExport._configFile.AddFloat("builtinSizeMultiplier", 1f, true);
            this._captureMode = (CaptureMode)VideoExport._configFile.AddInt("builtinCaptureMode", (int)CaptureMode.Normal, true);
            this._imageFormat = (VideoExport.ImgFormat)VideoExport._configFile.AddInt("builtinImageFormat", (int)VideoExport.ImgFormat.BMP, true);
            this._imageFormatNames = Enum.GetNames(typeof(VideoExport.ImgFormat));
#if HONEYSELECT
            this._imageFormatNames = this._imageFormatNames.Where(x => x != nameof(VideoExport.ImgFormat.EXR)).ToArray();
#endif

            return true;
        }

        public void UpdateLanguage()
        {
            this._captureModeNames = new[]
            {
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.CaptureModeNormal),
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.CaptureModeImmediate),
            };
        }

        public void OnStartRecording()
        {
            Vector2 size = this.currentSize;
            RenderTextureFormat renderTextureFormat = RenderTextureFormat.ARGB32;
            int renderTextureDepth = 0;
#if !HONEYSELECT
            if (this._imageFormat == VideoExport.ImgFormat.EXR)
            {
                renderTextureFormat = RenderTextureFormat.ARGBHalf;
                renderTextureDepth = 16;
            }
#endif
            switch (this._captureMode)
            {
                case CaptureMode.Normal:
                    break;
                case CaptureMode.Immediate:
                    this._cachedRenderTexture = Camera.main.targetTexture;
                    Camera.main.targetTexture = RenderTexture.GetTemporary((int)size.x, (int)size.y, renderTextureDepth, renderTextureFormat);
                    break;
            }
        }

        public byte[] Capture(string saveTo)
        {
            Texture2D texture = null;
            switch (this._captureMode)
            {
                case CaptureMode.Normal:
                    texture = this.CaptureNormal();
                    break;
                case CaptureMode.Immediate:
                    texture = this.CaptureImmediate();
                    break;
            }

            byte[] result = TextureEncoder.EncodeTexture(texture, imageFormat);
            UnityEngine.Object.Destroy(texture);
            return result;
        }

        public bool IsTextureCaptureAvailable()
        {
            return true;
        }

        public Texture2D CaptureTexture()
        {
            Texture2D texture = null;
            switch (this._captureMode)
            {
                case CaptureMode.Normal:
                    texture = this.CaptureNormal();
                    break;
                case CaptureMode.Immediate:
                    texture = this.CaptureImmediate();
                    break;
            }
            return texture;
        }

        public void OnEndRecording()
        {
            switch (this._captureMode)
            {
                case CaptureMode.Normal:
                    break;
                case CaptureMode.Immediate:
                    RenderTexture.ReleaseTemporary(Camera.main.targetTexture);
                    Camera.main.targetTexture = this._cachedRenderTexture;
                    break;
            }
        }

        public void DisplayParams()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.SizeMultiplier), GUILayout.ExpandWidth(false));
                this._scaleFactor = GUILayout.HorizontalSlider(this._scaleFactor, 1, 8);
                string s = this._scaleFactor.ToString("0.000");
                string newS = GUILayout.TextField(s, GUILayout.Width(40));
                if (newS != s)
                {
                    float res;
                    if (float.TryParse(newS, out res))
                        this._scaleFactor = res;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.CaptureMode), GUILayout.ExpandWidth(false));
                this._captureMode = (CaptureMode)GUILayout.SelectionGrid((int)this._captureMode, this._captureModeNames, this._captureModeNames.Length);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.ImageFormat), GUILayout.ExpandWidth(false));
                this._imageFormat = (VideoExport.ImgFormat)GUILayout.SelectionGrid((int)this._imageFormat, this._imageFormatNames, this._imageFormatNames.Length);
            }
            GUILayout.EndHorizontal();
        }

        public void SaveParams()
        {
            VideoExport._configFile.SetFloat("builtinSizeMultiplier", this._scaleFactor);
            VideoExport._configFile.SetInt("builtinCaptureMode", (int)this._captureMode);
            VideoExport._configFile.SetInt("builtinImageFormat", (int)this._imageFormat);
        }

        private Texture2D CaptureNormal()
        {
            Vector2 size = this.currentSize;
            int width = (int)size.x;
            int height = (int)size.y;

            TextureFormat textureFormat = TextureFormat.RGB24;
            RenderTextureFormat renderTextureFormat = RenderTextureFormat.ARGB32;
            int renderTextureDepth = 0;
#if !HONEYSELECT
            if (this._imageFormat == VideoExport.ImgFormat.EXR)
            {
                textureFormat = TextureFormat.RGBAHalf;
                renderTextureFormat = RenderTextureFormat.ARGBHalf;
                renderTextureDepth = 16;
            }
#endif

            RenderTexture cached = Camera.main.targetTexture;
            Camera.main.targetTexture = RenderTexture.GetTemporary(width, height, renderTextureDepth, renderTextureFormat);
            Camera.main.Render();

            RenderTexture cached2 = RenderTexture.active;
            RenderTexture.active = Camera.main.targetTexture;

            Texture2D texture = new Texture2D((int)size.x, (int)size.y, textureFormat, false, true);
            texture.ReadPixels(new UnityEngine.Rect(0, 0, width, height), 0, 0, false);
            RenderTexture.active = cached2;

            RenderTexture.ReleaseTemporary(Camera.main.targetTexture);
            Camera.main.targetTexture = cached;

            return texture;
        }

        private Texture2D CaptureImmediate()
        {
            Vector2 size = this.currentSize;
            int width = (int)size.x;
            int height = (int)size.y;

            TextureFormat textureFormat = TextureFormat.RGB24;
#if !HONEYSELECT
            if (this._imageFormat == VideoExport.ImgFormat.EXR)
            {
                textureFormat = TextureFormat.RGBAHalf;
            }
#endif

            RenderTexture cached2 = RenderTexture.active;
            RenderTexture.active = Camera.main.targetTexture;

            Texture2D texture = new Texture2D((int)size.x, (int)size.y, textureFormat, false, true);
            texture.ReadPixels(new UnityEngine.Rect(0, 0, width, height), 0, 0, false);
            RenderTexture.active = cached2;

            return texture;
        }
    }
}
