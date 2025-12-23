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
            Slower,
            Medium,
            Faster,
        }

        private readonly string[] _codecNames = { "H.264", "H.265" };
        private readonly string[] _codecCLIOptions = { "libx264", "libx265" };
        private string[] _presetNames;
        private readonly string[] _presetCLIOptions;

        private Codec _codec;
        private int _quality;
        private bool _hwAccel;
        private Preset _preset;

        public MP4Extension() : base()
        {
            this._codec = (Codec)VideoExport._configFile.AddInt("mp4Codec", (int)Codec.H264, true);
            this._quality = VideoExport._configFile.AddInt("mp4Quality", 18, true);
            this._hwAccel = VideoExport._configFile.AddBool("mp4HwAccel", false, true);
            this._preset = (Preset)VideoExport._configFile.AddInt("mp4Preset", (int)Preset.Slower, true);
            if ((int)this._preset >= Enum.GetValues(typeof(Preset)).Length)
                this._preset = Preset.Slower;

            this._presetCLIOptions = Enum.GetNames(typeof(Preset)).Select(n => n.ToLowerInvariant()).ToArray();
        }

        public override string GetArguments(string framesFolder, string prefix, string postfix, string inputExtension, byte bitDepth, int fps, bool transparency, bool resize, int resizeX, int resizeY, string fileName)
        {
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
            string channelTypeArg = ((ChannelType)channelType).ToString().ToLower();

            string[] codecOptions = _codecCLIOptions;
            string tuneArgument = "-tune animation";
            string presetArgument = $"-preset {this._presetCLIOptions[(int)this._preset]}";
            string rateControlArgument = $"-crf {_quality}";
            string videoFilterArgument = this.CompileFilters(resize, resizeX, resizeY);

            if (_hwAccel)
            {
                const int NVIDIA = 0x10DE;
                const int AMD = 0x1002;
                const int INTEL = 0x8086;

                int gpuVendorId = SystemInfo.graphicsDeviceVendorID;

                // Narrow down preset choice because the GPU codecs have their own
                string preset = this._presetCLIOptions[(int)this._preset];
                if (this._preset < Preset.Medium)
                {
                    preset = (gpuVendorId == INTEL) ? "slow" : "quality";
                    preset = (gpuVendorId == AMD) ? "quality" : "slow";
                }
                else if (this._preset > Preset.Medium)
                {
                    preset = (gpuVendorId == INTEL) ? "fast" : "speed";
                    preset = (gpuVendorId == AMD) ? "speed" : "fast";
                }
                else
                {
                    preset = (gpuVendorId == INTEL) ? "medium" : "balanced";
                    preset = (gpuVendorId == AMD) ? "balanced" : "medium";
                }
                    
                switch (gpuVendorId)
                {
                    case NVIDIA:
                        codecOptions = new string[] { "h264_nvenc", "hevc_nvenc" };
                        tuneArgument = "-tune hq";
                        rateControlArgument = $"-qp {_quality}";
                        presetArgument = $"-preset {preset}";
                        break;
                    case AMD:
                        codecOptions = new string[] { "h264_amf", "hevc_amf" }; // "amf" is not a typo :)
                        tuneArgument = "";
                        rateControlArgument = $"-rc cqp -qp_i {_quality} -qp_p {_quality} -qp_b {_quality}";
                        presetArgument = $"-quality {preset}";
                        break;
                    case INTEL:
                        codecOptions = new string[] { "h264_qsv", "hevc_qsv" };
                        tuneArgument = "";
                        rateControlArgument = $"-global_quality {_quality}";
                        presetArgument = $"-preset {preset}";
                        break;
                    default:
                        break;
                }
            }

            string codec = codecOptions[(int)this._codec];

            string ffmpegArgs = $"-loglevel error -r {fps} -f rawvideo -threads {coreCount} -progress pipe:1";
            string inputArgs = $"-pix_fmt {channelTypeArg} -i {framesFolder}";
            string codecArgs = $"-vcodec {codec} {tuneArgument} {presetArgument} {rateControlArgument} -pix_fmt {pixFmt} -vf \"{videoFilterArgument}\"";
            string outputArgs = $"\"{fileName}.mp4\"";

            return $"{ffmpegArgs} {inputArgs} {codecArgs} {outputArgs}";
        }

        public override void UpdateLanguage()
        {
            base.UpdateLanguage();
            this._presetNames = new[]
            {
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.MP4PresetSlower),
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.MP4PresetMedium),
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.MP4PresetFaster),
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
                GUILayout.Label(new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.Codec), VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.MP4CodecTooltip).Replace("\\n", "\n")), GUILayout.ExpandWidth(false));
                this._codec = (Codec)GUILayout.SelectionGrid((int)this._codec, this._codecNames, 2);
            }
            GUILayout.EndHorizontal();
            this._hwAccel = GUILayout.Toggle(this._hwAccel, new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.HwAccelCodec), VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.HwAccelCodecTooltip).Replace("\\n", "\n")), GUILayout.ExpandWidth(false));

            GUILayout.Label(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.MP4Quality));
            GUILayout.BeginHorizontal();
            {
                this._quality = Mathf.RoundToInt(GUILayout.HorizontalSlider(this._quality, 1, 51));
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
            VideoExport._configFile.SetBool("mp4HwAccel", (bool)this._hwAccel);
            base.SaveParams();
        }
    }
}
