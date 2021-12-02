using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using Graphics = System.Drawing.Graphics;
#if IPA
using Harmony;
#elif BEPINEX
using HarmonyLib;
#endif

namespace VideoExport.ScreenshotPlugins
{
    public class Bitmap : IScreenshotPlugin
    {
#if !KOIKATSU
        [DllImport("user32.dll")]
        static extern IntPtr GetActiveWindow();
        [DllImport("user32.dll")]
        public static extern IntPtr GetClientRect(IntPtr hWnd, ref Rect rect);
        [DllImport("user32.dll")]
        public static extern bool ClientToScreen(IntPtr hWnd, ref System.Drawing.Point lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }
#endif

        private enum CaptureMode
        {
            Normal,
            Immediate,
#if !KOIKATSU
            Win32
#endif
        }

        private enum ImgFormat
        {
            BMP,
            PNG,
#if !HONEYSELECT //Someday I hope...
            EXR
#endif
        }

        public string name { get { return VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.BuiltInCaptureTool); } }
        public Vector2 currentSize
        {
            get
            {
                int width = 0;
                int height = 0;
                switch (this._captureMode)
                {
                    case CaptureMode.Normal:
                    case CaptureMode.Immediate:
                        width = Mathf.RoundToInt(Screen.width * this._scaleFactor);
                        height = Mathf.RoundToInt(Screen.height * this._scaleFactor);
                        break;
#if !KOIKATSU
                    case CaptureMode.Win32:
                        if (this._windowHandle == IntPtr.Zero)
                            this._windowHandle = GetActiveWindow();
                        Rect rect = new Rect();
                        GetClientRect(this._windowHandle, ref rect);
                        width = rect.right - rect.left;
                        height = rect.bottom - rect.top;
                        break;
#endif
                }
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
                    case ImgFormat.BMP:
                        return "bmp";
                    case ImgFormat.PNG:
                        return "png";
#if !HONEYSELECT
                    case ImgFormat.EXR:
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
                return (byte)(this._imageFormat == ImgFormat.EXR ? 10 : 8);
#endif
            }
        }

        private float _scaleFactor;
        private CaptureMode _captureMode;
        private string[] _captureModeNames;
        private ImgFormat _imageFormat;
        private string[] _imageFormatNames;
        private IntPtr _windowHandle;
        private System.Drawing.Bitmap _bitmap;
        private System.Drawing.Graphics _graphics;
        private RenderTexture _cachedRenderTexture;
        private Texture2D _texture;
        private readonly byte[] _bmpHeader = {
            0x42, 0x4D,
            0, 0, 0, 0,
            0, 0,
            0, 0,
            54, 0, 0, 0,
            40, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            1, 0,
            24, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0
        };
        private byte[] _fileBytes = new byte[0];

#if IPA
        public bool Init(HarmonyInstance harmony)
#elif BEPINEX
        public bool Init(Harmony harmony)
#endif
        {
            this._scaleFactor = VideoExport._configFile.AddFloat("builtInSizeMultiplier", 1f, true);
            this._captureMode = (CaptureMode)VideoExport._configFile.AddInt("builtInCaptureMode", (int)CaptureMode.Normal, true);
            this._imageFormat = (ImgFormat)VideoExport._configFile.AddInt("builtInImageFormat", (int)ImgFormat.BMP, true);
            this._imageFormatNames = Enum.GetNames(typeof(ImgFormat));

            return true;
        }

        public void UpdateLanguage()
        {
            this._captureModeNames = new[]
            {
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.CaptureModeNormal),
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.CaptureModeImmediate),
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.CaptureModeWin32)
            };
        }

        public void OnStartRecording()
        {
            Vector2 size = this.currentSize;
            TextureFormat textureFormat = TextureFormat.RGB24;
            RenderTextureFormat renderTextureFormat = RenderTextureFormat.ARGB32;
            int renderTextureDepth = 0;
#if !HONEYSELECT
            if (this._imageFormat == ImgFormat.EXR)
            {
                textureFormat = TextureFormat.RGBAHalf;
                renderTextureFormat = RenderTextureFormat.ARGBHalf;
                renderTextureDepth = 16;
            }
#endif
            switch (this._captureMode)
            {
                case CaptureMode.Normal:
                    this._texture = new Texture2D((int)size.x, (int)size.y, textureFormat, false, true);
                    break;
                case CaptureMode.Immediate:
                    this._texture = new Texture2D((int)size.x, (int)size.y, textureFormat, false, true);
                    this._cachedRenderTexture = Camera.main.targetTexture;
                    Camera.main.targetTexture = RenderTexture.GetTemporary((int)size.x, (int)size.y, renderTextureDepth, renderTextureFormat);
                    break;
#if !KOIKATSU
                case CaptureMode.Win32:
                    this._windowHandle = GetActiveWindow();
                    Rect rect = new Rect();
                    GetClientRect(this._windowHandle, ref rect);
                    int width = rect.right - rect.left;
                    int height = rect.bottom - rect.top;
                    this._bitmap = new System.Drawing.Bitmap(width, height, PixelFormat.Format32bppArgb);
                    this._graphics = Graphics.FromImage(this._bitmap);
                    VideoExport._showUi = false;
                    break;
#endif
            }
        }

        public byte[] Capture(string saveTo)
        {
            switch (this._captureMode)
            {
                case CaptureMode.Normal:
                    return this.CaptureNormal();
                case CaptureMode.Immediate:
                    return this.CaptureImmediate();
#if !KOIKATSU
                case CaptureMode.Win32:
                    return this.CaptureWin32(saveTo);
#endif
            }
            return null;
        }

        public void OnEndRecording()
        {
            switch (this._captureMode)
            {
                case CaptureMode.Normal:
                    UnityEngine.Object.Destroy(this._texture);
                    break;
                case CaptureMode.Immediate:
                    UnityEngine.Object.Destroy(this._texture);
                    RenderTexture.ReleaseTemporary(Camera.main.targetTexture);
                    Camera.main.targetTexture = this._cachedRenderTexture;
                    break;
#if !KOIKATSU
                case CaptureMode.Win32:
                    this._graphics.Dispose();
                    this._graphics = null;
                    VideoExport._showUi = true;
                    break;
#endif
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
                this._imageFormat = (ImgFormat)GUILayout.SelectionGrid((int)this._imageFormat, this._imageFormatNames, this._imageFormatNames.Length);
            }
            GUILayout.EndHorizontal();

#if !HONEYSELECT && !KOIKATSU
            if (this._captureMode == CaptureMode.Win32 && this._imageFormat == ImgFormat.EXR)
                this._imageFormat = ImgFormat.BMP;
#endif
        }

        public void SaveParams()
        {
            VideoExport._configFile.SetFloat("bmpSizeMultiplier", this._scaleFactor);
            VideoExport._configFile.SetInt("builtInCaptureMode", (int)this._captureMode);
            VideoExport._configFile.SetInt("builtInImageFormat", (int)this._imageFormat);
        }

        private byte[] CaptureNormal()
        {
            Vector2 size = this.currentSize;
            int width = (int)size.x;
            int height = (int)size.y;

            RenderTextureFormat renderTextureFormat = RenderTextureFormat.ARGB32;
            int renderTextureDepth = 0;
#if !HONEYSELECT
            if (this._imageFormat == ImgFormat.EXR)
            {
                renderTextureFormat = RenderTextureFormat.ARGBHalf;
                renderTextureDepth = 16;
            }
#endif

            RenderTexture cached = Camera.main.targetTexture;
            Camera.main.targetTexture = RenderTexture.GetTemporary(width, height, renderTextureDepth, renderTextureFormat);
            Camera.main.Render();

            RenderTexture cached2 = RenderTexture.active;
            RenderTexture.active = Camera.main.targetTexture;
            this._texture.ReadPixels(new UnityEngine.Rect(0, 0, width, height), 0, 0, false);
            RenderTexture.active = cached2;

            RenderTexture.ReleaseTemporary(Camera.main.targetTexture);
            Camera.main.targetTexture = cached;

            switch (this._imageFormat)
            {
                default:
                case ImgFormat.BMP:
                    return this.EncodeToBMP(this._texture, width, height);
                case ImgFormat.PNG:
                    return this._texture.EncodeToPNG();
#if !HONEYSELECT
                case ImgFormat.EXR:
                    return this._texture.EncodeToEXR();
#endif
            }
        }

        private byte[] CaptureImmediate()
        {
            Vector2 size = this.currentSize;
            int width = (int)size.x;
            int height = (int)size.y;

            RenderTexture cached2 = RenderTexture.active;
            RenderTexture.active = Camera.main.targetTexture;
            this._texture.ReadPixels(new UnityEngine.Rect(0, 0, width, height), 0, 0, false);
            RenderTexture.active = cached2;

            switch (this._imageFormat)
            {
                default:
                case ImgFormat.BMP:
                    return this.EncodeToBMP(this._texture, width, height);
                case ImgFormat.PNG:
                    return this._texture.EncodeToPNG();
#if !HONEYSELECT
                case ImgFormat.EXR:
                    return this._texture.EncodeToEXR();
#endif
            }
        }

#if !KOIKATSU
        private byte[] CaptureWin32(string saveTo)
        {
            Rect rect = new Rect();
            GetClientRect(this._windowHandle, ref rect);
            System.Drawing.Point point = new System.Drawing.Point(0, 0);
            ClientToScreen(this._windowHandle, ref point);
            rect.left += point.X;
            rect.top += point.Y;
            rect.right += point.X;
            rect.bottom += point.Y;

            int width = rect.right - rect.left;
            int height = rect.bottom - rect.top;

            if (width % 2 != 0)
                width += 1;
            if (height % 2 != 0)
                height += 1;

            this._graphics.CopyFromScreen(rect.left, rect.top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);

            switch (this._imageFormat)
            {
                default:
                case ImgFormat.BMP:
                    this._bitmap.Save(saveTo, ImageFormat.Bmp);
                    break;
                case ImgFormat.PNG:
                    this._bitmap.Save(saveTo, ImageFormat.Png);
                    break;
            }
            return null;
        }
#endif

        private byte[] EncodeToBMP(Texture2D texture, int width, int height)
        {
            unsafe
            {
                uint byteSize = (uint)(width * height * 3);
                uint fileSize = (uint)(this._bmpHeader.Length + byteSize);
                if (this._fileBytes.Length != fileSize)
                {
                    this._fileBytes = new byte[fileSize];
                    Array.Copy(this._bmpHeader, this._fileBytes, this._bmpHeader.Length);

                    this._fileBytes[2] = ((byte*)&fileSize)[0];
                    this._fileBytes[3] = ((byte*)&fileSize)[1];
                    this._fileBytes[4] = ((byte*)&fileSize)[2];
                    this._fileBytes[5] = ((byte*)&fileSize)[3];

                    this._fileBytes[18] = ((byte*)&width)[0];
                    this._fileBytes[19] = ((byte*)&width)[1];
                    this._fileBytes[20] = ((byte*)&width)[2];
                    this._fileBytes[21] = ((byte*)&width)[3];

                    this._fileBytes[22] = ((byte*)&height)[0];
                    this._fileBytes[23] = ((byte*)&height)[1];
                    this._fileBytes[24] = ((byte*)&height)[2];
                    this._fileBytes[25] = ((byte*)&height)[3];

                    this._fileBytes[34] = ((byte*)&byteSize)[0];
                    this._fileBytes[35] = ((byte*)&byteSize)[1];
                    this._fileBytes[36] = ((byte*)&byteSize)[2];
                    this._fileBytes[37] = ((byte*)&byteSize)[3];
                }

                int i = this._bmpHeader.Length;
                Color32[] pixels = texture.GetPixels32();
                foreach (Color32 c in pixels)
                {
                    this._fileBytes[i++] = c.b;
                    this._fileBytes[i++] = c.g;
                    this._fileBytes[i++] = c.r;
                }
                return this._fileBytes;
            }
        }
    }
}
