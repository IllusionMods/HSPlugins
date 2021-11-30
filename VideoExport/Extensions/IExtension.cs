using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VideoExport.ScreenshotPlugins;

namespace VideoExport.Extensions
{
    public interface IExtension
    {
        int progress { get; }
        bool canProcessStandardOutput { get; }
        bool canProcessStandardError { get; }

        bool IsCompatibleWithPlugin(IScreenshotPlugin plugin, out string reason);
        string GetExecutable();
        string GetArguments(string framesFolder, string prefix, string postfix, string inputExtension, byte bitDepth, int fps, bool transparency, bool resize, int resizeX, int resizeY, string fileName);
        void ProcessStandardOutput(char c);
        void ProcessStandardError(char c);
        void UpdateLanguage();
        void DisplayParams();
        void SaveParams();
    }

    public enum ExtensionsType
    {
        MP4,
        WEBM,
        GIF,
        AVI
    }

}
