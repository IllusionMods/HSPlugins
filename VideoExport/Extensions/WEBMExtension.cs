using System;
using System.IO;
using System.Linq;
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

        public WEBMExtension() : base()
        {
            this._codec = (Codec)VideoExport._configFile.AddInt("webmCodec", (int)Codec.VP9, true);
            this._quality = VideoExport._configFile.AddInt("webmQuality", 15, true);
            this._deadline = (Deadline)VideoExport._configFile.AddInt("webmDeadline", (int)Deadline.Best, true);
            this._deadlineCLIOptions = Enum.GetNames(typeof(Deadline)).Select(n => n.ToLowerInvariant()).ToArray();
        }

        public override string GetArguments(string framesFolder, string prefix, string postfix, string inputExtension, byte bitDepth, int fps, bool transparency, bool resize, int resizeX, int resizeY, string fileName)
        {
            this._progress = 1;
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
            return $"-loglevel error -r {fps} -f image2 -i \"{framesFolder}\\{prefix}%d{postfix}.{inputExtension}\" {this.CompileFilters(resize, resizeX, resizeY)} -c:v libvpx{(this._codec == Codec.VP9 ? "-vp9" : " -qmin 0")} -pix_fmt {pixFmt} -auto-alt-ref 0 -crf {this._quality} {(this._codec == Codec.VP8 ? "-b:v 10M" : "-b:v 0")} -deadline {this._deadlineCLIOptions[(int)this._deadline]} -threads {_coreCount} -progress pipe:1 \"{fileName}.webm\"";
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
                GUILayout.Label(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.WEBMCodec), GUILayout.ExpandWidth(false));
                this._codec = (Codec)GUILayout.SelectionGrid((int)this._codec, this._codecNames, 2);
            }
            GUILayout.EndHorizontal();

            if (this._codec == Codec.VP9)
            {
                Color c = GUI.color;
                GUI.color = Color.yellow;
                GUILayout.Label(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.VP9Warning));
                GUI.color = c;
            }

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
                this._quality = Mathf.RoundToInt(GUILayout.HorizontalSlider(this._quality, 0, 63));
                GUILayout.Label(this._quality.ToString("00"), GUILayout.ExpandWidth(false));
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.WEBMDeadline), GUILayout.ExpandWidth(false));
            this._deadline = (Deadline)GUILayout.SelectionGrid((int)this._deadline, this._deadlineNames, 3);

            base.DisplayParams();
        }

        public override void SaveParams()
        {
            //_configFile.SetInt("webmCodec", (int)this._codec);
            VideoExport._configFile.SetInt("webmQuality", this._quality);
            VideoExport._configFile.SetInt("webmDeadline", (int)this._deadline);
            base.SaveParams();
        }
    }
}
