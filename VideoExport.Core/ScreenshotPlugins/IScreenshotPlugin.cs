using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if IPA
using Harmony;
#elif BEPINEX
using HarmonyLib;
#endif
using UnityEngine;

namespace VideoExport.ScreenshotPlugins
{
    public interface IScreenshotPlugin
    {
        string name { get; }
        Vector2 currentSize { get; }
        bool transparency { get; }
        string extension { get; }
        byte bitDepth { get; }
#if !HONEYSELECT
        VideoExport.ImgFormat imageFormat { get; }
#endif
#if IPA
        bool Init(HarmonyInstance harmony);
#elif BEPINEX
        bool Init(Harmony harmony);
#endif
        void UpdateLanguage();
        void OnStartRecording();
        byte[] Capture(string saveTo);
#if !HONEYSELECT
        Texture2D CaptureTexture();
        bool IsTextureCaptureAvailable();
#endif
        void OnEndRecording();
        void DisplayParams();
        void SaveParams();
    }
}
