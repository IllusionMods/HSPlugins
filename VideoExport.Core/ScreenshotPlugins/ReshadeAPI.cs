using System;

using System.Runtime.InteropServices;
using UnityEngine;

namespace VideoExport.ScreenshotPlugins
{
    internal class ReshadeAPI
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Screenshot
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
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr CreateFileMapping(IntPtr hFile, IntPtr lpFileMappingAttributes, uint flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);

        const uint GENERIC_READ = 0x80000000;
        const uint FILE_MAP_READ = 0x04;
        const uint GENERIC_WRITE = 0x40000000;
        const uint FILE_MAP_WRITE = 0x02;
        const uint PAGE_READWRITE = 0x04;

        const uint COLOR_CHANNELS = 4;
        const uint MAX_IMAGE_SIZE = 7680 * 4320 * COLOR_CHANNELS;

        private static IntPtr _shmFile = IntPtr.Zero;
        private static IntPtr _shm = IntPtr.Zero;
        private static bool _initialized;

        private static readonly string SharedMemoryName = "KKReshade_Screenshot_SHM";

        private static byte[] _frameDataBuffer;
        private static int _frameBufferSize;
        
        private static readonly string ControlMapName = "KKReshade_Screenshot_Control_SHM";
        private static IntPtr _controlShmFile = IntPtr.Zero;
        private static IntPtr _controlShm = IntPtr.Zero;

        public static bool OpenSharedMemory()
        {
            if (_initialized) return true;

            _shmFile = OpenFileMapping(GENERIC_READ, false, SharedMemoryName);
            if (_shmFile == IntPtr.Zero)
            {
                VideoExport.Logger.LogWarning("Failed to set up KKReshade, make sure it is properly installed and functioning.");
                return false;
            }

            _shm = MapViewOfFile(_shmFile, FILE_MAP_READ, 0, 0, (uint)(Marshal.SizeOf(typeof(Screenshot)) + MAX_IMAGE_SIZE));
            if (_shm == IntPtr.Zero)
            {
                VideoExport.Logger.LogError("Failed to set up KKReshade - Could not map view of the shared memory.");
                CloseHandle(_shmFile);
                return false;
            }

            _controlShmFile = CreateFileMapping(new IntPtr(-1), IntPtr.Zero, PAGE_READWRITE, 0, 1, ControlMapName);
            
            if (_controlShmFile != IntPtr.Zero)
            {
                _controlShm = MapViewOfFile(_controlShmFile, FILE_MAP_WRITE, 0, 0, 1);
                if (_controlShm != IntPtr.Zero)
                {
                    Marshal.WriteByte(_controlShm, 0);
                }
                else
                {
                     VideoExport.Logger.LogError($"Failed to map view for Reshade Control SHM.");
                }
            }
            else
            {
                int error = Marshal.GetLastWin32Error();
                VideoExport.Logger.LogError($"Failed to create or open Reshade Control SHM. Error Code: {error}");
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
            
            if (_controlShm != IntPtr.Zero)
            {
                UnmapViewOfFile(_controlShm);
                _controlShm = IntPtr.Zero;
            }

            if (_controlShmFile != IntPtr.Zero)
            {
                CloseHandle(_controlShmFile);
                _controlShmFile = IntPtr.Zero;
            }

            _initialized = false;
        }
        
        public static void SetCapture(bool enabled)
        {
            if (_controlShm != IntPtr.Zero)
            {
                try
                {
                    Marshal.WriteByte(_controlShm, (byte)(enabled ? 1 : 0));
                }
                catch (Exception ex)
                {
                    VideoExport.Logger.LogError($"Failed to write to Reshade Control SHM: {ex}");
                }
            }
        }

        public static bool IsAvailable()
        {
            return _initialized && _shmFile != IntPtr.Zero && _shm != IntPtr.Zero;
        }

        public static Texture2D RequestScreenshot(bool removeAlpha, bool vFlip)
        {
            Screenshot screenshot = (Screenshot)Marshal.PtrToStructure(_shm, typeof(Screenshot));
            uint imageSize = screenshot.width * screenshot.height * screenshot.channels;

            InitializeRecoder((int)imageSize);

            IntPtr imageBytesPtr = new IntPtr(_shm.ToInt64() + Marshal.SizeOf(typeof(Screenshot)));
            Marshal.Copy(imageBytesPtr, _frameDataBuffer, 0, (int)imageSize);

            if (vFlip)
            {
                int rowSize = (int)screenshot.width * (int)screenshot.channels; 
                byte[] rowBuffer = new byte[rowSize];
                int height = (int)screenshot.height;
                int halfHeight = height / 2;

                for (int i = 0; i < halfHeight; i++)
                {
                    int topRowOffset = i * rowSize;
                    int bottomRowOffset = (height - i - 1) * rowSize;

                    Array.Copy(_frameDataBuffer, topRowOffset, rowBuffer, 0, rowSize);
                    Array.Copy(_frameDataBuffer, bottomRowOffset, _frameDataBuffer, topRowOffset, rowSize);
                    Array.Copy(rowBuffer, 0, _frameDataBuffer, bottomRowOffset, rowSize);
                }
            }

            Texture2D texture = new Texture2D((int)screenshot.width, (int)screenshot.height, TextureFormat.RGBA32, false, false);
            texture.LoadRawTextureData(_frameDataBuffer);
            texture.Apply();

            return texture;
        }

        private static void InitializeRecoder(int imageSize)
        {
            _frameBufferSize = imageSize;

            if (_frameDataBuffer == null || _frameDataBuffer.Length != _frameBufferSize)
            {
                _frameDataBuffer = new byte[_frameBufferSize];
            }
        }
    }
}
