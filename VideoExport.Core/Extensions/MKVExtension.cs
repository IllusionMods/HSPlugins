using System;
using UnityEngine;
using VideoExport.ScreenshotPlugins;

namespace VideoExport.Extensions
{
    public class MKVExtension : AFFMPEGBasedExtension
    {
        private enum Codec
        {
            FFV1
        }

        private readonly string[] _codecNames = { "FFV1" };
        private readonly string[] _codecCLIOptions = { "ffv1" };

        private Codec _codec;
        private int _gopSize;
        private int _slices;
        private int[] _slicesPresets = new[] { 4, 6, 9, 12, 16, 24, 30 };

        public MKVExtension() : base()
        {
            this._codec = (Codec)VideoExport._configFile.AddInt("ffv1Codec", (int)Codec.FFV1, true);
            this._gopSize = VideoExport._configFile.AddInt("ffv1GopSize", 1, true);
            this._slices = VideoExport._configFile.AddInt("ffv1Slices", 4, true);
        }

        public override string GetArguments(string framesFolder, string prefix, string postfix, string inputExtension, byte bitDepth, int fps, bool transparency, bool resize, int resizeX, int resizeY, string fileName)
        {
            int coreCount = _coreCount;
            string pixFmt = transparency ? "yuva444p" : "yuv444p";
            string channelTypeArg = ((ChannelType)channelType).ToString().ToLower();

            string videoFilterArgument = this.CompileFilters(resize, resizeX, resizeY);
            videoFilterArgument = videoFilterArgument + $", format={pixFmt}";

            string codec = _codecCLIOptions[(int)this._codec] + " -level 3";
            string codecExtraArgs = $"-g {this._gopSize} -slices {this._slices}";

            string ffmpegArgs = $"-loglevel error -r {fps} -f rawvideo -threads {coreCount}";
            string inputArgs = $"-pix_fmt {channelTypeArg} -i {framesFolder}";
            string codecArgs = $"-c:v {codec} {codecExtraArgs} -vf \"{videoFilterArgument}\"";
            string outputArgs = $"\"{fileName}.mkv\"";

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
                GUILayout.Label(new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.Codec), VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.FFV1CodecTooltip)), GUILayout.ExpandWidth(false));
                this._codec = (Codec)GUILayout.SelectionGrid((int)this._codec, this._codecNames, this._codecNames.Length);
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.FFV1GopSize), VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.FFV1GopSizeTooltip).Replace("\\n", "\n")), GUILayout.ExpandWidth(false));;
            GUILayout.BeginHorizontal();
            {
                this._gopSize = Mathf.RoundToInt(GUILayout.HorizontalSlider(this._gopSize, 1, 300));
                GUILayout.Label(this._gopSize.ToString("000"), GUILayout.ExpandWidth(false));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.FFV1Slices), VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.FFV1SlicesTooltip).Replace("\\n", "\n")));
                {
                    if (GUILayout.Button("-", GUILayout.Width(30)))
                    {
                        int index = Array.BinarySearch(_slicesPresets, _slices);
                        if (index < 0)
                            index = ~index;
                        _slices = _slicesPresets[Math.Max(0, index - 1)];
                    }
                    GUILayout.Label(Math.Max(_slicesPresets[0], _slices).ToString(), GUILayout.Width(40));
                    if (GUILayout.Button("+", GUILayout.Width(30)))
                    {
                        int index = Array.BinarySearch(_slicesPresets, _slices);
                        if (index < 0)
                            index = ~index;
                        _slices = _slicesPresets[Math.Min(_slicesPresets.Length - 1, index + 1)];
                    }
                }
            }
            GUILayout.EndHorizontal();

            base.DisplayParams();
        }

        public override void SaveParams()
        {
            VideoExport._configFile.SetInt("ffv1Codec", (int)this._codec);
            VideoExport._configFile.SetInt("ffv1GopSize", _gopSize);
            VideoExport._configFile.SetInt("ffv1Slices", _slices);
            base.SaveParams();
        }
    }
}
