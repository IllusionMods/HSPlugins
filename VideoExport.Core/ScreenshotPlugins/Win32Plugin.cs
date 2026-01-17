using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using UnityEngine;
using Graphics = System.Drawing.Graphics;
using System.Linq;
using Color = System.Drawing.Color;

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
        private Bitmap _bitmap;
        private Graphics _graphics;
        private Bitmap _flipBuffer;
        private Graphics _flipGraphics;
        private static byte[] _frameDataBuffer;
        private static int _frameBufferSize;
        private Vector2 _lastSize;
        private readonly object _bitmapLock = new object();

#if IPA
        public bool Init(HarmonyInstance harmony)
#elif BEPINEX
        public bool Init(Harmony harmony)
#endif
        {
            _imageFormat = (VideoExport.ImgFormat)VideoExport._configFile.AddInt("win32ImageFormat", (int)VideoExport.ImgFormat.BMP, true);
            _imageFormatNames = Enum.GetNames(typeof(VideoExport.ImgFormat)).Where(x => x != nameof(VideoExport.ImgFormat.EXR)).ToArray();
            return true;
        }

        public void UpdateLanguage() { }

        public void OnStartRecording()
        {
            _windowHandle = GetActiveWindow();
            lock (_bitmapLock)
            {
                ResizeBuffersIfNeeded();
                _lastSize = currentSize;
            }
            VideoExport.ShowUI = false;
        }

        private void ResizeBuffersIfNeeded()
        {
            int w = (int)currentSize.x;
            int h = (int)currentSize.y;
            int size = w * h * 4;

            _bitmap?.Dispose();
            _graphics?.Dispose();
            _flipBuffer?.Dispose();
            _flipGraphics?.Dispose();

            _bitmap = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
            _graphics = Graphics.FromImage(_bitmap);
            _graphics.CompositingMode = CompositingMode.SourceCopy;

            _flipBuffer = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            _flipGraphics = Graphics.FromImage(_flipBuffer);
            _flipGraphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            _flipGraphics.CompositingMode = CompositingMode.SourceCopy;

            InitializeRecoder(size);
        }

        private void PerformCapture(out int width, out int height)
        {
            Rect rect = new Rect();
            GetClientRect(_windowHandle, ref rect);
            Point point = new Point(0, 0);
            ClientToScreen(_windowHandle, ref point);
            rect.left += point.X;
            rect.top += point.Y;
            rect.right += point.X;
            rect.bottom += point.Y;

            width = rect.right - rect.left;
            height = rect.bottom - rect.top;

            if (width % 2 != 0) width += 1;
            if (height % 2 != 0) height += 1;

            Vector2 size = new Vector2(width, height);
            if (Vector2.Distance(size, _lastSize) > 1f)
            {
                ResizeBuffersIfNeeded();
                _lastSize = size;
            }

            _graphics.Clear(Color.Black);
            _graphics.CopyFromScreen(rect.left, rect.top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);

            _flipGraphics.Transform = new Matrix(1, 0, 0, -1, 0, height);
            _flipGraphics.DrawImage(_bitmap, 0, 0);
        }

        public byte[] Capture(string saveTo)
        {
            Rect rect = new Rect();
            GetClientRect(_windowHandle, ref rect);
            Point point = new Point(0, 0);
            ClientToScreen(_windowHandle, ref point);
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

            _graphics.Clear(System.Drawing.Color.Black);
            _graphics.CopyFromScreen(rect.left, rect.top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);

            switch (_imageFormat)
            {
                default:
                case VideoExport.ImgFormat.BMP:
                    _bitmap.Save(saveTo, ImageFormat.Bmp);
                    break;
                case VideoExport.ImgFormat.PNG:
                    _bitmap.Save(saveTo, ImageFormat.Png);
                    break;
            }
            return null;
        }

        public bool IsTextureCaptureAvailable() { return true; }
        public bool IsRenderTextureCaptureAvailable() { return false; }
        public bool IsVFlipNeeded() { return true; }

        public Texture2D CaptureTexture()
        {
            lock (_bitmapLock)
            {
                PerformCapture(out var width, out var height);

                BitmapData data = _flipBuffer.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try
                {
                    if (data.Scan0 == IntPtr.Zero) return null;

                    unsafe
                    {
                        byte* src = (byte*)data.Scan0;
                        fixed (byte* dst = _frameDataBuffer)
                        {
                            uint* srcPixels = (uint*)src;
                            uint* dstPixels = (uint*)dst;
                            int pixelCount = width * height;

                            for (int i = 0; i < pixelCount; i++)
                            {
                                if (srcPixels != null)
                                {
                                    uint bgra = srcPixels[i];
                                    dstPixels[i] = (bgra & 0xFF00FF00u) |
                                                   ((bgra & 0x00FF0000u) >> 16) |
                                                   ((bgra & 0x000000FFu) << 16);
                                }
                            }
                        }
                    }

                    Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
                    tex.LoadRawTextureData(_frameDataBuffer);
                    tex.Apply(false);

                    return tex;
                }
                finally
                {
                    _flipBuffer.UnlockBits(data);
                }
            }
        }

        public RenderTexture CaptureRenderTexture() { return null; }

        public void OnEndRecording()
        {
            lock (_bitmapLock)
            {
                _graphics?.Dispose(); _bitmap?.Dispose();
                _flipGraphics?.Dispose(); _flipBuffer?.Dispose();
                _graphics = null;
                _bitmap = null;
                _flipGraphics = null;
                _flipBuffer = null;
            }
            VideoExport.ShowUI = true;
        }

        public void DisplayParams()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.ImageFormat), 
                    VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.ImageFormatTooltip).Replace("\\n", "\n")), GUILayout.ExpandWidth(false));
                this._imageFormat = (VideoExport.ImgFormat)GUILayout.SelectionGrid((int)this._imageFormat, this._imageFormatNames, this._imageFormatNames.Length);
            }
            GUILayout.EndHorizontal();
        }

        public void SaveParams()
        {
            VideoExport._configFile.SetInt("win32ImageFormat", (int)this._imageFormat);
        }
        
        private static void InitializeRecoder(int imageSize)
        {
            _frameBufferSize = imageSize;
            if (_frameDataBuffer == null || _frameDataBuffer.Length != _frameBufferSize)
                _frameDataBuffer = new byte[_frameBufferSize];
        }
    }
}
