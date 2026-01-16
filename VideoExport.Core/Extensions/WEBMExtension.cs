using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ToolBox.Extensions;
using UnityEngine;
using VideoExport.ScreenshotPlugins;

namespace VideoExport.Extensions
{
    public class WEBMExtension : AFFMPEGBasedExtension
    {
        private enum Codec
        {
            VP8,
            VP9
        }

        private enum Deadline
        {
            Best,
            Good,
            Realtime
        }

        private readonly string[] _codecNames = { "VP8", "VP9" };
        private string[] _deadlineNames;
        private readonly string[] _deadlineCLIOptions;

        private Codec _codec = Codec.VP9;
        private int _quality;
        private Deadline _deadline;
        private string _maxBitrate;

        public WEBMExtension() : base()
        {
            this._codec = (Codec)VideoExport._configFile.AddInt("webmCodec", (int)Codec.VP9, true);
            this._quality = VideoExport._configFile.AddInt("webmQuality", 15, true);
            this._deadline = (Deadline)VideoExport._configFile.AddInt("webmDeadline", (int)Deadline.Best, true);
            this._deadlineCLIOptions = Enum.GetNames(typeof(Deadline)).Select(n => n.ToLowerInvariant()).ToArray();
            this._maxBitrate = VideoExport._configFile.AddString("webmMaxBitrate", "10M", true);
        }

        public override string GetArguments(string framesFolder, string prefix, string postfix, string inputExtension, byte bitDepth, int fps, bool transparency, bool resize, int resizeX, int resizeY, string fileName)
        {
            int coreCount = _coreCount;
            string pixFmt;
            switch (bitDepth)
            {
                default:
                case 8:
                    pixFmt = transparency ? "yuva420p -metadata:s:v:0 alpha_mode=\"1\"" : "yuv420p";
                    break;
                case 10:
                    pixFmt = transparency ? "yuva420p10le -metadata:s:v:0 alpha_mode=\"1\"" : "yuv420p10le";
                    break;
            }
            string channelTypeArg = ((ChannelType)channelType).ToString().ToLower();
            string autoAltRef = this._codec == Codec.VP8 ? "-auto-alt-ref 0" : "";
            string videoFilterArgument = this.CompileFilters(resize, resizeX, resizeY);

            string ffmpegArgs = $"-loglevel error -r {fps} -f rawvideo -threads {coreCount}";
            string inputArgs = $"-pix_fmt {channelTypeArg} -i {framesFolder}";
            string codecArgs = $"-c:v libvpx{(this._codec == Codec.VP9 ? "-vp9" : "")} -pix_fmt {pixFmt} {autoAltRef} -b:v {(_codec == Codec.VP9 ? "0" : _maxBitrate)} -crf {this._quality} -deadline {this._deadlineCLIOptions[(int)this._deadline]} -vf \"{videoFilterArgument}\"";
            string outputArgs = $"\"{fileName}.webm\"";

            return $"{ffmpegArgs} {inputArgs} {codecArgs} {outputArgs}";
        }

        public override void UpdateLanguage()
        {
            base.UpdateLanguage();
            this._deadlineNames = new[]
            {
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.WEBMDeadlineBest),
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.WEBMDeadlineGood),
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.WEBMDeadlineRealtime)
            };
        }

        public override bool IsCompatibleWithPlugin(IScreenshotPlugin plugin, out string reason)
        {
            if (this._codec == Codec.VP8 && plugin.bitDepth != 8)
            {
                reason = VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.BitDepthError);
                return false;
            }
            reason = null;
            return true;
        }

        public override void DisplayParams()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.Codec), VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.WEBMCodecTooltip).Replace("\\n", "\n")), GUILayout.ExpandWidth(false));
                this._codec = (Codec)GUILayout.SelectionGrid((int)this._codec, this._codecNames, 2);
            }
            GUILayout.EndHorizontal();

            switch (this._codec)
            {
                case Codec.VP8:
                    GUILayout.Label(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.VP8Quality));
                    break;
                case Codec.VP9:
                    GUILayout.Label(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.VP9Quality));
                    break;
            }
            GUILayout.BeginHorizontal();
            {
                this._quality = Mathf.RoundToInt(GUILayout.HorizontalSlider(this._quality, this._codec == Codec.VP9 ? 0 : 4, 63));
                GUILayout.Label(this._quality.ToString("00"), GUILayout.ExpandWidth(false));
            }
            GUILayout.EndHorizontal();

            if (this._codec == Codec.VP8)
            {
                if (_quality < 4)
                {
                    _quality = 4;
                }
                
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.WebmMaxBitrate), VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.WebmMaxBitrateTooltip).Replace("\\n", "\n")));
                
                if (GUILayout.Button("3M", Styles.ButtonStyle, GUILayout.Width(40)))
                {
                    _maxBitrate = "3M";
                }
                if (GUILayout.Button("5M", Styles.ButtonStyle, GUILayout.Width(40)))
                {
                    _maxBitrate = "5M";
                }
                if (GUILayout.Button("10M", Styles.ButtonStyle, GUILayout.Width(40)))
                {
                    _maxBitrate = "10M";
                }
                if (GUILayout.Button("1G", Styles.ButtonStyle, GUILayout.Width(40)))
                {
                    _maxBitrate = "1G";
                }
                
                string bitRate = GUILayout.TextField(_maxBitrate, GUILayout.Width(70), GUILayout.Height(30));

                if (Regex.IsMatch(bitRate, @"\d+[kMG]"))
                {
                    _maxBitrate = bitRate;
                }
                
                GUILayout.EndHorizontal();
            }

            GUILayout.Label(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.WEBMDeadline), GUILayout.ExpandWidth(false));
            this._deadline = (Deadline)GUILayout.SelectionGrid((int)this._deadline, this._deadlineNames, 3);

            base.DisplayParams();
        }

        public override void SaveParams()
        {
            VideoExport._configFile.SetInt("webmCodec", (int)this._codec);
            VideoExport._configFile.SetInt("webmQuality", this._quality);
            VideoExport._configFile.SetInt("webmDeadline", (int)this._deadline);
            VideoExport._configFile.SetString("webmMaxBitrate", _maxBitrate);
            base.SaveParams();
        }
    }
}
