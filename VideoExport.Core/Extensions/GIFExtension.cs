using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using VideoExport.ScreenshotPlugins;

namespace VideoExport.Extensions
{
    public class GIFExtension : AFFMPEGBasedExtension
    {
        private readonly string _gifskiFolder;
        private readonly string _gifskiExe;

        private StringBuilder _errorBuilder = new StringBuilder();

        public override bool canProcessStandardOutput { get { return _gifTool == GifTool.FFmpeg; } }

        private enum GifTool
        {
            FFmpeg,
            Gifski,
        }

        private enum Dithering
        {
            None,
            Bayer,
            FloydSteinberg,
        }

        private GifTool _gifTool;
        private Dithering _ffmpegDithering;
        private readonly string[] _gifToolNames = Enum.GetValues(typeof(GifTool)).Cast<GifTool>().Select(x => GetGifToolString(x)).ToArray();
        private readonly string[] _ffmpegDitheringNames = Enum.GetValues(typeof(Dithering)).Cast<Dithering>().Select(x => GetDitheringString(x)).ToArray();
        private int[] _presetMaxColors = new[] { 8, 16, 32, 64, 128, 256 };
        private int _maxColors;

        public GIFExtension()
        {
            this._gifskiFolder = Path.Combine(VideoExport._pluginFolder, "gifski");
            this._gifskiExe = Path.GetFullPath(Path.Combine(this._gifskiFolder, "gifski.exe"));
            this._gifTool = (GifTool)VideoExport._configFile.AddInt("gifTool", (int)GifTool.FFmpeg, true);
            this._maxColors = VideoExport._configFile.AddInt("gifMaxColors", 256, true);
            this._ffmpegDithering = (Dithering)VideoExport._configFile.AddInt("gifDithering", (int)Dithering.None, true);
        }

        public bool IsPaletteGenRequired()
        {
            return this._gifTool == GifTool.FFmpeg;
        }

        public override void UpdateLanguage()
        {
            if (_gifTool == GifTool.Gifski)
                return;
            base.UpdateLanguage();
        }

        public override bool IsCompatibleWithPlugin(IScreenshotPlugin plugin, out string reason)
        {
            if (this._gifTool == GifTool.Gifski && plugin.extension != "png")
            {
                reason = VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.GIFError);
                return false;
            }
            if (plugin.bitDepth != 8)
            {
                reason = VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.BitDepthError);
                return false;
            }
            reason = null;
            return true;
        }

        public override string GetExecutable()
        {
            if (_gifTool == GifTool.Gifski)
                return this._gifskiExe;
            return base.GetExecutable();
        }

        public string GetArgumentsPaletteGen(string framesFolder, string prefix, string postfix, string inputExtension, byte bitDepth, int fps, bool transparency, bool resize, int resizeX, int resizeY, string fileName)
        {
            int coreCount = _coreCount;
            string videoFilterArgument = $"-vf \"palettegen=stats_mode=diff:max_colors={_maxColors}\"";

            string ffmpegArgs = $"-loglevel error -r {fps} -f image2 -threads {coreCount} -progress pipe:1";
            string inputArgs = $"-i \"{framesFolder}\\{prefix}%d{postfix}.{inputExtension}\" {videoFilterArgument}";
            string outputArgs = $"\"{fileName}.palette.png\"";

            return $"{ffmpegArgs} {inputArgs} {outputArgs}";
        }

        public override string GetArguments(string framesFolder, string prefix, string postfix, string inputExtension, byte bitDepth, int fps, bool transparency, bool resize, int resizeX, int resizeY, string fileName)
        {
            if (this._gifTool == GifTool.Gifski)
            {
                return $"{(resize ? $"-W {resizeX} -H {resizeY}" : "")} --fps {fps} -o \"{fileName}.gif\" \"{framesFolder}\"\\{prefix}*{postfix}.{inputExtension} --quiet";
            }
            else
            {
                int coreCount = _coreCount;

                string videoFilterArgument = CompileFiltersComplex(resize, resizeX, resizeY);

                string ffmpegArgs = $"-loglevel error -r {fps} -f image2 -threads {coreCount} -progress pipe:1";
                string inputArgs = $"-i \"{framesFolder}\\{prefix}%d{postfix}.{inputExtension}\" -i {fileName}.palette.png {videoFilterArgument}";
                string outputArgs = $"\"{fileName}.gif\"";

                return $"{ffmpegArgs} {inputArgs} {outputArgs}";
            }
        }

        public override void ProcessStandardOutput(char c)
        {
            if (this._gifTool == GifTool.Gifski)
                return;
            base.ProcessStandardOutput(c);
        }

        public override void ProcessStandardError(char c)
        {
            if (this._gifTool == GifTool.Gifski)
            {
                if (c == '\n')
                {
                    string line = this._errorBuilder.ToString();
                    VideoExport.Logger.LogWarning(line);
                    this._errorBuilder = new StringBuilder();
                }
                else
                    this._errorBuilder.Append(c);
            }
            else
            {
                base.ProcessStandardError(c);
            }
        }

        public override void DisplayParams()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.GIFTool), VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.GIFToolTooltip).Replace("\\n", "\n")), GUILayout.ExpandWidth(false));
                this._gifTool = (GifTool)GUILayout.SelectionGrid((int)this._gifTool, this._gifToolNames, 2);
            }
            GUILayout.EndHorizontal();

            if (this._gifTool == GifTool.FFmpeg)
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.GIFMaxColors));

                    if (GUILayout.Button("-", GUILayout.Width(30)))
                    {
                        int index = Array.BinarySearch(_presetMaxColors, _maxColors);
                        if (index < 0)
                            index = ~index;
                        _maxColors = _presetMaxColors[Math.Max(0, index - 1)];
                    }
                    GUILayout.Label(_maxColors.ToString(), GUILayout.Width(40));
                    if (GUILayout.Button("+", GUILayout.Width(30)))
                    {
                        int index = Array.BinarySearch(_presetMaxColors, _maxColors);
                        if (index < 0)
                            index = ~index;
                        _maxColors = _presetMaxColors[Math.Min(_presetMaxColors.Length - 1, index + 1)];
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginVertical();
                {
                    GUILayout.Label(new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.GIFDithering), VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.GIFDitheringTooltip).Replace("\\n", "\n")), GUILayout.ExpandWidth(false));
                    this._ffmpegDithering = (Dithering)GUILayout.SelectionGrid((int)this._ffmpegDithering, this._ffmpegDitheringNames, 3);
                }
                GUILayout.EndVertical();

                base.DisplayParams();
            }
        }

        public override void SaveParams()
        {
            VideoExport._configFile.SetInt("gifTool", (int)this._gifTool);
            VideoExport._configFile.SetInt("gifMaxColors", _maxColors);
            VideoExport._configFile.SetInt("gifDithering", (int)this._ffmpegDithering);
            base.SaveParams();
        }

        private string CompileFiltersComplex(bool resize, int resizeX, int resizeY)
        {
            string dithering = _ffmpegDitheringNames[(int)_ffmpegDithering];
            string palette = $"paletteuse=dither={dithering}";

            string rotate = string.Join(",", Enumerable.Repeat("transpose=1", (int)_rotation).ToArray());
            string scale = resize ? $"scale={resizeX}x{resizeY}:flags=lanczos" : "";
            string transform = $"{rotate}{(rotate != "" ? "," : "")}{scale}";

            string complex_filter = $"-filter_complex \"[0:v]{transform}[v];[v][1:v]{palette}\"";
            return complex_filter;
        }

        private static string GetDitheringString(Dithering dithering)
        {
            switch (dithering)
            {
                case Dithering.None: return "none";
                case Dithering.Bayer: return "bayer";
                case Dithering.FloydSteinberg: return "floyd_steinberg";
            }
            throw new NotImplementedException("Invalid dithering setting");
        }

        private static string GetGifToolString(GifTool tool)
        {
            switch (tool)
            {
                case GifTool.FFmpeg: return "FFmpeg";
                case GifTool.Gifski: return "Gifski";
            }
            throw new NotImplementedException("Invalid gif tool");
        }
    }
}
