using VideoExport.ScreenshotPlugins;
using System.Text;
using UnityEngine;
using System.IO;
using System;

namespace VideoExport.VideoExtensions
{
    public abstract class AFFMPEGBasedExtension : IVideoExtension 
    {
        protected enum Rotation
        {
            None,
            CW90,
            CW180,
            CW270
        }

        private readonly string _ffmpegFolder;
        private readonly string _ffmpegExe;
        private string[] _rotationNames;

        protected static Rotation _rotation = Rotation.None;
        protected int _progress;
        protected static readonly int _coreCount;
        protected bool _needsVFlip = true; 

        private StringBuilder _outputBuilder = new StringBuilder();
        private StringBuilder _errorBuilder = new StringBuilder();

        public virtual int progress { get { return this._progress; } }
        public virtual bool canProcessStandardOutput { get { return true; } }
        public bool canProcessStandardError { get { return true; } }
        public virtual int channelType { get; set; }

        static AFFMPEGBasedExtension()
        {
            _coreCount = Environment.ProcessorCount;
        }

        protected AFFMPEGBasedExtension()
        {
            this._ffmpegFolder = Path.Combine(VideoExport._pluginFolder, "ffmpeg");
            if (IntPtr.Size == 8)
                this._ffmpegExe = Path.GetFullPath(Path.Combine(this._ffmpegFolder, "ffmpeg.exe"));
            else
            {
                // Since KK is the oldest supported game now, all games are 64bit
                throw new InvalidOperationException("We're somehow running in a 32bit environment?");
            }
            _rotation = (Rotation)VideoExport._configFile.AddInt("ffmpegRotation", (int)Rotation.None, true);
        }

        public virtual void UpdateLanguage()
        {
            this._rotationNames = new[]
            {
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.RotationNone),
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.Rotation90),
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.Rotation180),
                VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.Rotation270)
            };
        }

        public abstract bool IsCompatibleWithPlugin(IScreenshotPlugin plugin, out string reason);

        public virtual string GetExecutable()
        {
            return this._ffmpegExe;
        }

        public abstract void GetArguments(string framesFolder, string prefix, string postfix, string inputExtension, byte bitDepth, int fps, bool transparency, bool resize, int resizeX, int resizeY, string fileName,
            out string inputArgs, out string filterArgs, out string mapArgs, out string codecArgs, out string outputArgs);

        public void GetArguments(string framesFolder, string inputExtension, byte bitDepth, int fps, bool transparency, bool resize, int resizeX, int resizeY, string fileName,
            out string inputArgs, out string filterArgs, out string mapArgs, out string codecArgs, out string outputArgs)
        {
            GetArguments(framesFolder, "", "", inputExtension, bitDepth, fps, transparency, resize, resizeX, resizeY, fileName,
                out inputArgs, out filterArgs, out mapArgs, out codecArgs, out outputArgs);
        }

        public virtual void ResetProgress()
        {
            this._progress = 1;
        }

        public virtual void ProcessStandardOutput(char c)
        {
            if (c == '\n')
            {
                string line = this._outputBuilder.ToString();
                if (line.IndexOf("frame=", StringComparison.Ordinal) == 0)
                {
                    string frameString = line.Substring(6);
                    int frame;
                    if (int.TryParse(frameString, out frame))
                        this._progress = frame;
                }
                this._outputBuilder = new StringBuilder();
            }
            else
                this._outputBuilder.Append(c);
        }

        public virtual void ProcessStandardError(char c)
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

        public virtual void DisplayParams()
        {
            GUILayout.Label(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.Rotation), GUILayout.ExpandWidth(false));
            _rotation = (Rotation)GUILayout.SelectionGrid((int)_rotation, this._rotationNames, 4);
        }

        public virtual void SaveParams()
        {
            VideoExport._configFile.SetInt("ffmpegRotation", (int)_rotation);
        }

        protected string CompileFilters(bool resize, int resizeX, int resizeY, string additionalFiltersPre = null, string additionalFiltersPost = null)
        {
            bool hasFilters = false;
            string res = "";

            if (additionalFiltersPre != null)
            {
                res += additionalFiltersPre;
                hasFilters = true;
            }

            if (resize)
            {
                if (hasFilters) res += ",";
                res += $"scale={resizeX}x{resizeY}:flags=lanczos";
                hasFilters = true;
            }

            if (_needsVFlip)
            {
                if (hasFilters)
                {
                    res += ",";
                }

                res += "vflip";
                hasFilters = true;
            }

            if (_rotation != Rotation.None)
            {
                if (hasFilters == true)
                    res += ", ";

                switch ((int)_rotation)
                {
                    case 0:
                        Debug.Log("something gone wrong with _rotation in CompileFilters()");
                        break;
                    case 1:
                        res += $"transpose={(int)_rotation}";
                        break;
                    case 2:
                        res += $"rotate=PI";
                        break;
                    case 3:
                        // transpose=2 = 90 CCW (= 270 CW)
                        res += "transpose=2";
                        break;
                }

                hasFilters = true;
            }

            if (additionalFiltersPost != null)
            {
                if (hasFilters)
                    res += ",";
                res += additionalFiltersPost;
                hasFilters = true;
            }

            return res;
        }

        public void SetVFlipNeeded(bool needsVFlip)
        {
            _needsVFlip = needsVFlip;
        }
    }
}
