using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
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
        bool Init(Harmony harmony);
        void UpdateLanguage();
        void OnStartRecording();
        byte[] Capture(string saveTo);
#if !HONEYSELECT
        Texture2D CaptureTexture();
        RenderTexture CaptureRenderTexture();
        bool IsTextureCaptureAvailable();
        bool IsRenderTextureCaptureAvailable();
        bool IsVFlipNeeded();
#endif
        void OnEndRecording();
        void DisplayParams();
        void SaveParams();
    }
}
