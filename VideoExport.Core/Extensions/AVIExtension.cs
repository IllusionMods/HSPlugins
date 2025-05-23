using System.IO;
using ToolBox.Extensions;
using UnityEngine;
using VideoExport.ScreenshotPlugins;

namespace VideoExport.Extensions
{
    public class AVIExtension : AFFMPEGBasedExtension
    {
        private int _quality;

        public AVIExtension() : base()
        {
            this._quality = VideoExport._configFile.AddInt("aviQuality", 3, true);
        }

        public override bool IsCompatibleWithPlugin(IScreenshotPlugin plugin, out string reason)
        {
            if (plugin.bitDepth != 8)
            {
                reason = VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.BitDepthError);
                return false;
            }
            reason = null;
            return true;
        }

        public override string GetArguments(string framesFolder, string inputExtension, byte bitDepth, int fps, bool transparency, bool resize, int resizeX, int resizeY, string fileName)
        {
            this._progress = 1;
            return $"-loglevel error -r {fps} -f image2 -i \"{framesFolder}\\%d.{inputExtension}\" {this.CompileFilters(resize, resizeX, resizeY)} -pix_fmt yuv420p -c:v libxvid -qscale:v {this._quality} -threads {_coreCount} -progress pipe:1 \"{fileName}.avi\"";
        }

        public override void DisplayParams()
        {
            GUILayout.Label(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.AVIQuality));
            GUILayout.BeginHorizontal();
            {
                this._quality = Mathf.RoundToInt(GUILayout.HorizontalSlider(this._quality, 1, 31));
                GUILayout.Label(this._quality.ToString("00"), GUILayout.ExpandWidth(false));
            }
            GUILayout.EndHorizontal();
            base.DisplayParams();
        }

        public override void SaveParams()
        {
            VideoExport._configFile.SetInt("aviQuality", this._quality);
            base.SaveParams();
        }
    }
}
