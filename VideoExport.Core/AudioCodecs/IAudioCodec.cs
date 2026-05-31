using System.Collections.Generic;
using System.Text;
using System;

namespace VideoExport.AudioExtensions
{
    internal interface IAudioCodec
    {
        string Name { get; }

        void DisplayParams();
        void SaveParams();
        string GetArguments(int bitrate);
    }

    internal static class AudioCodecCommon
    {
        internal static string GetArguments(int sampleRate)
        {
            StringBuilder sb = new StringBuilder("");
            return "";
        }
    }
}
