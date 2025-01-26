using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using UnityEngine;
using Graphics = System.Drawing.Graphics;
using System.Linq;

#if IPA
using Harmony;
#elif BEPINEX
using HarmonyLib;
#endif

namespace VideoExport.ScreenshotPlugins
{
    public class Win32Plugin : IScreenshotPlugin
    {
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

        public string name { get { return VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.Win32CaptureTool); } }
        public Vector2 currentSize
        {
            get
            {
                int width = 0;
                int height = 0;

                if (this._windowHandle == IntPtr.Zero)
                    this._windowHandle = GetActiveWindow();
                Rect rect = new Rect();
                GetClientRect(this._windowHandle, ref rect);

                width = rect.right - rect.left;
                height = rect.bottom - rect.top;

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
                }
            }
        }
        public byte bitDepth { get { return (byte)8; } }

        public VideoExport.ImgFormat imageFormat { get { return _imageFormat; } }

        private VideoExport.ImgFormat _imageFormat;
        private string[] _imageFormatNames;
        private IntPtr _windowHandle;
        private System.Drawing.Bitmap _bitmap;
        private System.Drawing.Graphics _graphics;

#if IPA
        public bool Init(HarmonyInstance harmony)
#elif BEPINEX
        public bool Init(Harmony harmony)
#endif
        {
            this._imageFormat = (VideoExport.ImgFormat)VideoExport._configFile.AddInt("win32ImageFormat", (int)VideoExport.ImgFormat.BMP, true);
            this._imageFormatNames = Enum.GetNames(typeof(VideoExport.ImgFormat)).Where(x => x != nameof(VideoExport.ImgFormat.EXR)).ToArray();

            return true;
        }

        public void UpdateLanguage()
        {
        }

        public void OnStartRecording()
        {
            this._windowHandle = GetActiveWindow();
            Rect rect = new Rect();
            GetClientRect(this._windowHandle, ref rect);
            int width = rect.right - rect.left;
            int height = rect.bottom - rect.top;
            this._bitmap = new System.Drawing.Bitmap(width, height, PixelFormat.Format32bppArgb);
            this._graphics = Graphics.FromImage(this._bitmap);
            VideoExport._showUi = false;
        }

        public byte[] Capture(string saveTo)
        {
            return this.CaptureWin32(saveTo);
        }

        public bool IsTextureCaptureAvailable()
        {
            return false;
        }

        public Texture2D CaptureTexture()
        {
            return null;
        }

        public void OnEndRecording()
        {
            this._graphics.Dispose();
            this._graphics = null;
            VideoExport._showUi = true;
        }

        public void DisplayParams()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.ImageFormat), GUILayout.ExpandWidth(false));
                this._imageFormat = (VideoExport.ImgFormat)GUILayout.SelectionGrid((int)this._imageFormat, this._imageFormatNames, this._imageFormatNames.Length);
            }
            GUILayout.EndHorizontal();
        }

        public void SaveParams()
        {
            VideoExport._configFile.SetInt("win32ImageFormat", (int)this._imageFormat);
        }

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

            this._graphics.Clear(System.Drawing.Color.Black);
            this._graphics.CopyFromScreen(rect.left, rect.top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);

            switch (this._imageFormat)
            {
                default:
                case VideoExport.ImgFormat.BMP:
                    this._bitmap.Save(saveTo, ImageFormat.Bmp);
                    break;
                case VideoExport.ImgFormat.PNG:
                    this._bitmap.Save(saveTo, ImageFormat.Png);
                    break;
            }
            return null;
        }
    }
}
