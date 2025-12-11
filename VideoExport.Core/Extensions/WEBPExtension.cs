using UnityEngine;
using VideoExport.ScreenshotPlugins;

namespace VideoExport.Extensions
{
    public class WEBPExtension : AFFMPEGBasedExtension
    {
        private enum Codec
        {
            LibWebP
        }

        private readonly string[] _codecNames = { "LibWEBP" };
        private readonly string[] _codecCLIOptions = { "libwebp" };

        private Codec _codec;
        private int _quality;

        public WEBPExtension() : base()
        {
            this._codec = (Codec)VideoExport._configFile.AddInt("webpCodec", (int)Codec.LibWebP, true);
            this._quality = VideoExport._configFile.AddInt("webpQuality", 75, true);
        }

        public override string GetArguments(string framesFolder, string prefix, string postfix, string inputExtension, byte bitDepth, int fps, bool transparency, bool resize, int resizeX, int resizeY, string fileName)
        {
            int coreCount = _coreCount;
            string pixFmt = transparency ? "yuva420p" : "yuv420p";

            string videoFilterArgument = this.CompileFilters(resize, resizeX, resizeY);

            string codec = _codecCLIOptions[(int)this._codec];
            string codecExtraArgs = $"-qscale {this._quality} -loop 0";

            string ffmpegArgs = $"-loglevel error -r {fps} -f rawvideo -threads {coreCount} -progress pipe:1";
            string inputArgs = $"-pix_fmt argb -i {framesFolder}";
            string codecArgs = $"-c:v {codec} {codecExtraArgs} -pix_fmt {pixFmt} -vf \"{videoFilterArgument}\"";
            string outputArgs = $"\"{fileName}.webp\"";

            return $"{ffmpegArgs} {inputArgs} {codecArgs} {outputArgs}";
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
                GUILayout.Label(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.Codec), GUILayout.ExpandWidth(false));
                this._codec = (Codec)GUILayout.SelectionGrid((int)this._codec, this._codecNames, this._codecNames.Length);
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.WEBPQuality));
            GUILayout.BeginHorizontal();
            {
                this._quality = Mathf.RoundToInt(GUILayout.HorizontalSlider(this._quality, 0, 100));
                GUILayout.Label(this._quality.ToString("00"), GUILayout.ExpandWidth(false));
            }
            GUILayout.EndHorizontal();

            base.DisplayParams();
        }

        public override void SaveParams()
        {
            VideoExport._configFile.SetInt("webpCodec", (int)this._codec);
            base.SaveParams();
        }
    }
}
