using UnityEngine;
using VideoExport.ScreenshotPlugins;

namespace VideoExport.Extensions
{
    public class MOVExtension : AFFMPEGBasedExtension
    {
        private enum Codec
        {
            //ProRes4444
            ProRes
        }

        private enum CodecProfile
        {
            ProRes422Proxy,
            ProRes422LT,
            ProRes422,
            ProRes422HQ,
            ProRes4444,
            ProRes4444XQ
        }

        private readonly string[] _codecNames = { "ProRes" };
        private readonly string[] _codecProfiles =
            { "0", "1", "2", "3", "4", "5" };
        private string[] _presetNames;
        //private readonly string[] _codecCLIOptions = { "prores_ks" };
        private readonly string[] _codecCLIOptions = { "prores" };

        private Codec _codec;
        private CodecProfile _codecProfile;

        public MOVExtension() : base()
        {
            this._codec = (Codec)VideoExport._configFile.AddInt("movCodec", (int)Codec.ProRes, true);
        }

        public override string GetArguments(string framesFolder, string prefix, string postfix, string inputExtension, byte bitDepth, int fps, bool transparency, bool resize, int resizeX, int resizeY, string fileName)
        {
            int coreCount = _coreCount;
            string pixFmt = transparency ? "yuva444p10le" : "yuv444p10le";

            string videoFilterArgument = this.CompileFilters(resize, resizeX, resizeY);

            string codec = _codecCLIOptions[(int)this._codec];
            string codecProfileName = _codecProfiles[(int)this._codecProfile];
            //string codecExtraArgs = "-profile 4 -alpha_bits 8 -bits_per_mb 250 -mbs_per_slice 4";
            //string codecExtraArgs = "-profile:v 3";
            string codecExtraArgs = "-profile:v " + codecProfileName;

            //string ffmpegArgs = $"-loglevel error -r {fps} -f image2 -threads {coreCount} -progress pipe:1";
            string ffmpegArgs = $"-loglevel error -r {fps} -f rawvideo -threads {coreCount} -progress pipe:1";
            //string inputArgs = $"-i \"{framesFolder}\\{prefix}%d{postfix}.{inputExtension}\" -pix_fmt {pixFmt} {videoFilterArgument}";
            string inputArgs = $"-pix_fmt argb -i {framesFolder} {videoFilterArgument}";

            string videoPixelFormatArg = (int)this._codecProfile > 3 ? "yuva444p10le" : "yuv422p10le";
            string codecArgs = $"-vf format={videoPixelFormatArg},vflip -c:v {codec} {codecExtraArgs}";
            string outputArgs = $"\"{fileName}.mov\"";

            return $"{ffmpegArgs} {inputArgs} {codecArgs} {outputArgs}";
        }

        public override void UpdateLanguage()
        {
            base.UpdateLanguage();
            this._presetNames = new[]
            {
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.MOVPresetProRes422Proxy),
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.MOVPresetProRes422LT),
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.MOVPresetProRes422),
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.MOVPresetProRes422HQ),
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.MOVPresetProRes4444),
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.MOVPresetProRes4444XQ)
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
                GUILayout.Label(new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.Codec), VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.MOVCodecTooltip)), GUILayout.ExpandWidth(false));
                this._codec = (Codec)GUILayout.SelectionGrid((int)this._codec, this._codecNames, this._codecNames.Length);
            }
            GUILayout.EndHorizontal();

            //GUILayout.Label(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.MP4Preset), GUILayout.ExpandWidth(false));
            GUILayout.Label(new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.MOVPreset), VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.MOVPresetTooltip).Replace("\\n", "\n")), GUILayout.ExpandWidth(false));
            this._codecProfile = (CodecProfile)GUILayout.SelectionGrid((int)this._codecProfile, this._presetNames, 3);

            base.DisplayParams();
        }

        public override void SaveParams()
        {
            VideoExport._configFile.SetInt("movCodec", (int)this._codec);
            base.SaveParams();
        }
    }
}
