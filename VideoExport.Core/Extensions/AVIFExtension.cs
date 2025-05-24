using UnityEngine;
using VideoExport.ScreenshotPlugins;

namespace VideoExport.Extensions
{
    public class AVIFExtension : AFFMPEGBasedExtension
    {
        private enum Codec
        {
            Libaomav1,
            Libsvtav1
        }

        private readonly string[] _codecNames = { "Libaom-av1", "Libsvt-av1" };
        private readonly string[] _codecCLIOptions = { "libaom-av1", "libsvtav1" };

        private Codec _codec;
        private int _quality;

        public AVIFExtension() : base()
        {
            this._codec = (Codec)VideoExport._configFile.AddInt("avifCodec", (int)Codec.Libaomav1, true);
            this._quality = VideoExport._configFile.AddInt("avifQuality", 28, true);
        }

        public override string GetArguments(string framesFolder, string prefix, string postfix, string inputExtension, byte bitDepth, int fps, bool transparency, bool resize, int resizeX, int resizeY, string fileName)
        {
            int coreCount = _coreCount;
            string pixFmt = transparency ? "yuva420p" : "yuv420p";

            string videoFilterArgument = this.CompileFilters(resize, resizeX, resizeY);

            string codec = _codecCLIOptions[(int)this._codec];
            string codecExtraArgs = $"-loop 0";
            if (this._codec == Codec.Libaomav1)
            {
                codecExtraArgs = $"{codecExtraArgs} -cpu-used 6 -threads 8 -row-mt 1";
            }
            string rateControlArgument = $"-crf {_quality} -b:v 0";

            string ffmpegArgs = $"-loglevel error -r {fps} -f image2 -threads {coreCount} -progress pipe:1";
            string inputArgs = $"-i \"{framesFolder}\\{prefix}%d{postfix}.{inputExtension}\" -pix_fmt {pixFmt} {videoFilterArgument}";
            string codecArgs = $"-c:v {codec} {codecExtraArgs} {rateControlArgument}";
            string outputArgs = $"\"{fileName}.avif\"";

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
                GUILayout.Label(new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.Codec), VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.AVIFCodecTooltip).Replace("\\n", "\n")), GUILayout.ExpandWidth(false));
                this._codec = (Codec)GUILayout.SelectionGrid((int)this._codec, this._codecNames, this._codecNames.Length);
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.AVIFQuality));
            GUILayout.BeginHorizontal();
            {
                this._quality = Mathf.RoundToInt(GUILayout.HorizontalSlider(this._quality, 0, 63));
                GUILayout.Label(this._quality.ToString("00"), GUILayout.ExpandWidth(false));
            }
            GUILayout.EndHorizontal();

            base.DisplayParams();
        }

        public override void SaveParams()
        {
            VideoExport._configFile.SetInt("avifCodec", (int)this._codec);
            base.SaveParams();
        }
    }
}
