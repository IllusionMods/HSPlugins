using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace VideoExport.AudioPlugins
{
    public interface IAudioPlugin
    {
        string name { get; }

        VideoExport.ImgFormat imageFormat { get; }

        bool Init(Harmony harmony);
        void UpdateLanguage();
        void OnStartRecording();
        byte[] Capture(string saveTo);

        Texture2D CaptureTexture();
        RenderTexture CaptureRenderTexture();
        bool IsTextureCaptureAvailable();
        bool IsRenderTextureCaptureAvailable();
        bool IsVFlipNeeded();

        void OnEndRecording();
        void DisplayParams();
        void SaveParams();
    }

    public struct AudioPluginConfig
    {
        readonly IAudioPlugin plugin;

        bool enabled;
        float volume;

        public AudioPluginConfig(IAudioPlugin plugin)
        {
            this.plugin = plugin;

            enabled = true;
            volume = 1.0f;
        }
    }
}
