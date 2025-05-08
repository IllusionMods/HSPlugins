using System;
using System.Runtime.InteropServices;
using UnityEngine;
using VideoExport.Core;
using System.Reflection;
using System.Linq;


#if IPA
using Harmony;
#elif BEPINEX
using HarmonyLib;
#endif

namespace VideoExport.ScreenshotPlugins
{
    class ReshadeAPI
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public unsafe struct Screenshot
        {
            public uint width;
            public uint height;
            public uint channels;
            // followed by [width * height * channels] bytes
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr OpenFileMapping(uint dwDesiredAccess, bool bInheritHandle, string lpName);
        [DllImport("kernel32.dll")]
        private static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);
        [DllImport("kernel32.dll")]
        private static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);
        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        const uint GENERIC_READ = 0x80000000;
        const uint FILE_MAP_READ = 0x04;

        const uint COLOR_CHANNELS = 4;
        const uint MAX_IMAGE_SIZE = 7680 * 4320 * COLOR_CHANNELS;

        private static IntPtr _shmFile = IntPtr.Zero;
        private static IntPtr _shm = IntPtr.Zero;
        private static bool _initialized = false;

        private static string sharedMemoryName = "KKReshade_Screenshot_SHM";

        public static bool OpenSharedMemory()
        {
            if (_initialized) return true;

            _shmFile = OpenFileMapping(GENERIC_READ, false, sharedMemoryName);
            if (_shmFile == IntPtr.Zero)
            {
                VideoExport.Logger.LogError($"Could not open file mapping for shared memory.");
                return false;
            }

            _shm = MapViewOfFile(_shmFile, FILE_MAP_READ, 0, 0, (uint)(Marshal.SizeOf(typeof(Screenshot)) + MAX_IMAGE_SIZE));
            if (_shm == IntPtr.Zero)
            {
                VideoExport.Logger.LogError($"Could not map view of the shared memory.");
                CloseHandle(_shmFile);
                return false;
            }

            _initialized = true;
            return true;
        }

        public static void CloseSharedMemory()
        {
            if (_shm != IntPtr.Zero)
            {
                UnmapViewOfFile(_shm);
                _shm = IntPtr.Zero;
            }

            if (_shmFile != IntPtr.Zero)
            {
                CloseHandle(_shmFile);
                _shmFile = IntPtr.Zero;
            }

            _initialized = false;
        }

        public static bool IsAvailable()
        {
            return _initialized && _shmFile != IntPtr.Zero && _shm != IntPtr.Zero;
        }

        public static Texture2D RequestScreenshot(bool removeAlpha)
        {
            Screenshot screenshot = (Screenshot)Marshal.PtrToStructure(_shm, typeof(Screenshot));
            uint imageSize = screenshot.width * screenshot.height * screenshot.channels;
            IntPtr imageBytesPtr = new IntPtr(_shm.ToInt64() + Marshal.SizeOf(typeof(Screenshot)));
            byte[] rawImageBytes = new byte[imageSize];
            Marshal.Copy(imageBytesPtr, rawImageBytes, 0, (int) imageSize);

            // Raw image comes flipped vertically, so we flip it back to normal
            byte[] bytesRGBA = new byte[imageSize];
            uint rowSize = screenshot.width * screenshot.channels;
            for (uint y = 0; y < screenshot.height; y++)
            {
                uint srcRow = (screenshot.height - 1 - y) * rowSize;
                uint dstRow = y * rowSize;
                Buffer.BlockCopy(rawImageBytes, (int)srcRow, bytesRGBA, (int)dstRow, (int)rowSize);
            }

            if (removeAlpha)
            {
                uint rgbSize = screenshot.width * screenshot.height * 3;
                byte[] bytesRGB = new byte[rgbSize];

                for (int src = 0, dst = 0; src < bytesRGBA.Length; src += 4, dst += 3)
                {
                    bytesRGB[dst] = bytesRGBA[src];
                    bytesRGB[dst + 1] = bytesRGBA[src + 1];
                    bytesRGB[dst + 2] = bytesRGBA[src + 2];
                }

                Texture2D texture = new Texture2D((int)screenshot.width, (int)screenshot.height, TextureFormat.RGB24, false, false);
                texture.LoadRawTextureData(bytesRGB);
                texture.Apply();
                return texture;
            }
            else
            {
                Texture2D texture = new Texture2D((int)screenshot.width, (int)screenshot.height, TextureFormat.RGBA32, false, false);
                texture.LoadRawTextureData(bytesRGBA);
                texture.Apply();
                return texture;
            }
        }
    }

    public class ReshadePlugin : IScreenshotPlugin
    {
        public string name { get { return "Reshade"; } }

        public Vector2 currentSize
        {
            get
            {
                int width = Mathf.RoundToInt(Screen.width);
                int height = Mathf.RoundToInt(Screen.height);

                if (width % 2 != 0)
                    width += 1;
                if (height % 2 != 0)
                    height += 1;

                return new Vector2(width, height);
            }
        }
        public bool transparency { get { return !_removeAlphaChannel; } }
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
                    case VideoExport.ImgFormat.JPG:
                        return "jpg";
                }
            }
        }
        public byte bitDepth { get { return 8; } }

        public VideoExport.ImgFormat imageFormat { get { return _imageFormat; } }
        private VideoExport.ImgFormat _imageFormat;
        private bool _autoHideUI;
        private bool _removeAlphaChannel;
        private string[] _imageFormatNames;

        Action _toggleUI = null;
        bool _initializedToggleUI = false;

#if IPA
        public bool Init(HarmonyInstance harmony)
#elif BEPINEX
        public bool Init(Harmony harmony)
#endif
        {
            this._imageFormat = (VideoExport.ImgFormat)VideoExport._configFile.AddInt("reshadeImageFormat", (int)VideoExport.ImgFormat.BMP, true);
            this._imageFormatNames = Enum.GetNames(typeof(VideoExport.ImgFormat)).Where(x => x != nameof(VideoExport.ImgFormat.EXR)).ToArray();
            this._autoHideUI = VideoExport._configFile.AddBool("autoHideUI", true, true);
            this._removeAlphaChannel = VideoExport._configFile.AddBool("removeAlphaChannel", true, true);

            return ReshadeAPI.OpenSharedMemory();
        }

        public void UpdateLanguage()
        {
        }

        public byte[] Capture(string saveTo)
        {
            Texture2D texture = CaptureTexture();
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
            return ReshadeAPI.RequestScreenshot(_removeAlphaChannel);
        }

        public void OnStartRecording()
        {
            if (_autoHideUI)
            {
                if (!_initializedToggleUI)
                {
                    _initializedToggleUI = true;
                    InitializeToggleUI();
                }

                VideoExport._showUi = false;
                if (_toggleUI != null) _toggleUI();
            }
        }

        public void OnEndRecording()
        {
            if (_autoHideUI)
            {
                VideoExport._showUi = true;
                if (_toggleUI != null) _toggleUI();
            }
        }

        public void DisplayParams()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.ImageFormat), GUILayout.ExpandWidth(false));
                this._imageFormat = (VideoExport.ImgFormat)GUILayout.SelectionGrid((int)this._imageFormat, this._imageFormatNames, this._imageFormatNames.Length);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                this._autoHideUI = GUILayout.Toggle(_autoHideUI, "Auto Hide UI");
                this._removeAlphaChannel = GUILayout.Toggle(_removeAlphaChannel, "Remove Alpha Channel");
            }
            GUILayout.EndHorizontal();
        }

        public void SaveParams()
        {
            VideoExport._configFile.SetInt("reshadeImageFormat", (int)this._imageFormat);
            VideoExport._configFile.SetBool("autoHideUI", (bool)this._autoHideUI);
            VideoExport._configFile.SetBool("removeAlphaChannel", (bool)this._removeAlphaChannel);
        }

        private void InitializeToggleUI()
        {
#if KOIKATSU
            Type hideUI = Type.GetType("HideAllUI.HideAllUICore,HideAllUI.Koikatu");
#elif SUNSHINE
            Type hideUI = Type.GetType("HideAllUI.HideAllUICore,HideAllUI.KoikatsuSunshine");
#elif HONEYSELECT2
            Type hideUI = Type.GetType("HideAllUI.HideAllUICore,HideAllUI.HoneySelect2");
#elif AISHOUJO
            Type hideUI = Type.GetType("HideAllUI.HideAllUICore,HideAllUI.AISyoujyo");
#endif
            if (hideUI!= null)
            {
                FieldInfo handlerField = hideUI.GetField("currentUIHandler", BindingFlags.NonPublic | BindingFlags.Static);
                if (handlerField != null)
                {
                    object handlerInstance = handlerField.GetValue(null);
                    if (handlerInstance != null)
                    {
                        MethodInfo toggleMethod = handlerInstance.GetType().GetMethod("ToggleUI", BindingFlags.Public | BindingFlags.Instance);
                        if (toggleMethod != null)
                        {
                            _toggleUI = (Action)Delegate.CreateDelegate(typeof(Action), handlerInstance, toggleMethod);
                        }
                    }
                }
            }
        }
    }
}
