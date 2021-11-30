using System;
using System.Linq;
using UnityEngine;
using VideoExport.ScreenshotPlugins;

namespace VideoExport.Extensions
{
    public class MP4Extension : AFFMPEGBasedExtension
    {
        private enum Codec
        {
            H264,
            H265
        }

        private enum Preset
        {
            VerySlow,
            Slower,
            Slow,
            Medium,
            Fast,
            Faster,
            VeryFast,
            SuperFast,
            UltraFast
        }

        private readonly string[] _codecNames = {"H.264", "H.265"};
        private readonly string[] _codecCLIOptions = {"libx264", "libx265" };
        private string[] _presetNames;
        private readonly string[] _presetCLIOptions;

        private Codec _codec;
        private int _quality;
        private Preset _preset;

        public MP4Extension() : base()
        {
            this._codec = (Codec)VideoExport._configFile.AddInt("mp4Codec", (int)Codec.H264, true);
            this._quality = VideoExport._configFile.AddInt("mp4Quality", 18, true);
            this._preset = (Preset)VideoExport._configFile.AddInt("mp4Preset", (int)Preset.Slower, true);
            this._presetCLIOptions = Enum.GetNames(typeof(Preset)).Select(n => n.ToLowerInvariant()).ToArray();
        }

        public override string GetArguments(string framesFolder, string prefix, string postfix, string inputExtension, byte bitDepth, int fps, bool transparency, bool resize, int resizeX, int resizeY, string fileName)
        {
            this._progress = 1;
            string pixFmt;
            switch (bitDepth)
            {
                default:
                case 8:
                    pixFmt = "yuv420p";
                    break;
                case 10:
                    pixFmt = "yuv420p10le"; 
                    break;
            }
            int coreCount = _coreCount;
            if (this._codec == Codec.H265 && coreCount > 16)
                coreCount = 16;
            return $"-loglevel error -r {fps} -f image2 -i \"{framesFolder}\\{prefix}%d{postfix}.{inputExtension}\" -tune animation {this.CompileFilters(resize, resizeX, resizeY)} -vcodec {this._codecCLIOptions[(int)this._codec]} -pix_fmt {pixFmt} -crf {this._quality} -preset {this._presetCLIOptions[(int)this._preset]} -threads {coreCount} -progress pipe:1 \"{fileName}.mp4\"";
        }

        public override void UpdateLanguage()
        {
            base.UpdateLanguage();
            this._presetNames = new[]
            {
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.MP4PresetVerySlow),
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.MP4PresetSlower),
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.MP4PresetSlow),
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.MP4PresetMedium),
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.MP4PresetFast),
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.MP4PresetFaster),
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.MP4PresetVeryFast),
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.MP4PresetSuperFast),
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.MP4PresetUltraFast)
            };
        }

        public override bool IsCompatibleWithPlugin(IScreenshotPlugin plugin, out string reason)
        {
            reason = null;
            return true;
        }

        public override void DisplayParams()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.MP4Codec), GUILayout.ExpandWidth(false));
                this._codec = (Codec)GUILayout.SelectionGrid((int)this._codec, this._codecNames, 2);
            }
            GUILayout.EndHorizontal();

            if (this._codec == Codec.H265)
            {
                Color c = GUI.color;
                GUI.color = Color.yellow;
                GUILayout.Label(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.H265Warning));
                GUI.color = c;
            }

            GUILayout.Label(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.MP4Quality));
            GUILayout.BeginHorizontal();
            {
                this._quality = Mathf.RoundToInt(GUILayout.HorizontalSlider(this._quality, 0, 51));
                GUILayout.Label(this._quality.ToString("00"), GUILayout.ExpandWidth(false));
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.MP4Preset), GUILayout.ExpandWidth(false));
            this._preset = (Preset)GUILayout.SelectionGrid((int)this._preset, this._presetNames, 3);

            base.DisplayParams();
        }

        public override void SaveParams()
        {
            VideoExport._configFile.SetInt("mp4Codec", (int)this._codec);
            VideoExport._configFile.SetInt("mp4Quality", this._quality);
            VideoExport._configFile.SetInt("mp4Preset", (int)this._preset);
            base.SaveParams();
        }
    }
}
