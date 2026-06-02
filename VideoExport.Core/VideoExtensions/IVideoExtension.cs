using VideoExport.ScreenshotPlugins;

namespace VideoExport.VideoExtensions
{
    public interface IVideoExtension
    {
        int progress { get; }
        bool canProcessStandardOutput { get; }
        bool canProcessStandardError { get; }
        int channelType { get; set; }

        bool IsCompatibleWithPlugin(IScreenshotPlugin plugin, out string reason);
        string GetExecutable();
        void GetArguments(string framesFolder, string prefix, string postfix, string inputExtension, byte bitDepth, int fps, bool transparency, bool resize, int resizeX, int resizeY, string fileName,
            out string inputArgs, out string filterArgs, out string mapArgs, out string codecArgs, out string outputArgs);
        void GetArguments(string framesFolder, string inputExtension, byte bitDepth, int fps, bool transparency, bool resize, int resizeX, int resizeY, string fileName,
            out string inputArgs, out string filterArgs, out string mapArgs, out string codecArgs, out string outputArgs);
        void ResetProgress();
        void ProcessStandardOutput(char c);
        void ProcessStandardError(char c);
        void UpdateLanguage();
        void DisplayParams();
        void SaveParams();
        void SetVFlipNeeded(bool needsVFlip);
    }

    public enum ExtensionsType
    {
        MP4,
        WEBM,
        GIF,
        MOV,
        WEBP,
        AVIF,
        MKV
    }

    public enum ChannelType
    {
        ARGB,
        RGBA
    }

}
