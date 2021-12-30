using System.IO;
using System.Text;
using UnityEngine;
using VideoExport.ScreenshotPlugins;

namespace VideoExport.Extensions
{
    public class GIFExtension : IExtension
    {
        private readonly string _gifskiFolder;
        private readonly string _gifskiExe;

        private StringBuilder _errorBuilder = new StringBuilder();

        public int progress { get { return 0; } }
        public bool canProcessStandardOutput { get { return false; } }
        public bool canProcessStandardError { get { return true; } }

        public GIFExtension()
        {
            this._gifskiFolder = Path.Combine(VideoExport._pluginFolder, "gifski");
            this._gifskiExe = Path.GetFullPath(Path.Combine(this._gifskiFolder, "gifski.exe"));
        }

        public void UpdateLanguage()
        {
        }

        public bool IsCompatibleWithPlugin(IScreenshotPlugin plugin, out string reason)
        {
            if (plugin.extension != "png")
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

        public string GetExecutable()
        {
            return this._gifskiExe;
        }

        public string GetArguments(string framesFolder, string prefix, string postfix, string inputExtension, byte bitDepth, int fps, bool transparency, bool resize, int resizeX, int resizeY, string fileName)
        {
            return $"{(resize ? $"-W {resizeX} -H {resizeY}" : "")} --fps {fps} -o \"{fileName}.gif\" \"{framesFolder}\"\\{prefix}*{postfix}.{inputExtension} --quiet";
        }

        public void ProcessStandardOutput(char c)
        {
        }

        public void ProcessStandardError(char c)
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

        public void DisplayParams()
        {
            Color c = GUI.color;
            GUI.color = Color.yellow;
            GUILayout.Label(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.GIFWarning));
            GUI.color = c;
        }

        public void SaveParams() { }
    }
}
