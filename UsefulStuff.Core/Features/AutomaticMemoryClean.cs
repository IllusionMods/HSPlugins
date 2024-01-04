using System;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HSUS.Features
{
    public class AutomaticMemoryClean : IFeature
    {
        private float _lastCleanup;
        private float _lastCheckedForceCleanup;
        private int _countForceCleanup;

        public void Awake()
        {
            HSUS._self._onUpdate += Update;
        }

        public void LevelLoaded()
        {
        }

        private void CleanMemory()
        {
            _lastCleanup = Time.unscaledTime;
            Resources.UnloadUnusedAssets();
            GC.Collect();
        }

        private void Update()
        {
#if HONEYSELECT
            if( OptimizeNEO._isCleaningResources )
                return;
#endif

            if (HSUS.AutomaticMemoryClean.Value)
            {
                if (Time.unscaledTime - _lastCleanup > HSUS.AutomaticMemoryCleanInterval.Value)
                {
                    CleanMemory();
                    if (EventSystem.current.sendNavigationEvents)
                        EventSystem.current.sendNavigationEvents = false;
                }
            }

            if (Time.unscaledTime - _lastCheckedForceCleanup > HSUS.ForceMemoryCleanInterval.Value && _countForceCleanup <= 8 )
            {
                _lastCheckedForceCleanup = Time.unscaledTime;

                if (GetSystemMemoryLoad() >= HSUS.ForceMemoryCleanPercent.Value)
                {
                    ++_countForceCleanup;   //If a forced memory clear occurs several times in a row, clear is aborted.
                    CleanMemory();
                }
                else
                {
                    _countForceCleanup = 0;
                }                    
            }
        }

        /// <summary>
        /// Returns OS memory usage (0~100%)
        /// </summary>
        /// <returns>0-100</returns>
        public static uint GetSystemMemoryLoad()
        {
            try
            {
                MEMORYSTATUSEX stat = new MEMORYSTATUSEX();
                stat.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));

                if (GlobalMemoryStatusEx(ref stat))
                    return stat.dwMemoryLoad;
            }
            catch{}
            
            return 0;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);
    }
}
