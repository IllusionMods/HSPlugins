using UnityEngine;
using VideoExport.ScreenshotPlugins;

namespace VideoExport.Extensions
{
    public class MOVExtension : AFFMPEGBasedExtension
    {
        private enum Codec
        {
            ProRes4444
        }

        private readonly string[] _codecNames = { "Apple ProRes 4444" };
        private readonly string[] _codecCLIOptions = { "prores_ks" };

        private Codec _codec;

        public MOVExtension() : base()
        {
            this._codec = (Codec)VideoExport._configFile.AddInt("movCodec", (int)Codec.ProRes4444, true);
        }

        public override string GetArguments(string framesFolder, string prefix, string postfix, string inputExtension, byte bitDepth, int fps, bool transparency, bool resize, int resizeX, int resizeY, string fileName)
        {
            this._progress = 1;
            int coreCount = _coreCount;
            string pixFmt = transparency ? "yuva444p10le" : "yuv444p10le";

            string videoFilterArgument = this.CompileFilters(resize, resizeX, resizeY);

            string codec = _codecCLIOptions[(int)this._codec];
            string codecExtraArgs = "-profile 4 -alpha_bits 8 -bits_per_mb 250 -mbs_per_slice 4";

            string ffmpegArgs = $"-loglevel error -r {fps} -f image2 -threads {coreCount} -progress pipe:1";
            string inputArgs = $"-i \"{framesFolder}\\{prefix}%d{postfix}.{inputExtension}\" -pix_fmt {pixFmt} {videoFilterArgument}";
            string codecArgs = $"-c:v {codec} {codecExtraArgs}";
            string outputArgs = $"\"{fileName}.mov\"";

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
                GUILayout.Label(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.MOVCodec), GUILayout.ExpandWidth(false));
                this._codec = (Codec)GUILayout.SelectionGrid((int)this._codec, this._codecNames, this._codecNames.Length);
            }
            GUILayout.EndHorizontal();

            Color c = GUI.color;
            GUI.color = Color.yellow;
            GUILayout.Label(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.MOVProResWarning));
            GUI.color = c;

            base.DisplayParams();
        }

        public override void SaveParams()
        {
            VideoExport._configFile.SetInt("movCodec", (int)this._codec);
            base.SaveParams();
        }
    }
}
