using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using VideoExport.AudioPlugins;

namespace VideoExport.AudioCodecs
{
    internal enum AudioCodecType
    {
        AAC,
        AC3,
        FLAC,
        Opus,
        Vorbis
    }

    internal interface IAudioCodec
    {
        string Name { get; }
        AudioCodecType CodecType { get; }

        void DisplayParams();
        void SaveParams();
        void GetArguments(
            int sampleRate,
            float duration,
            Dictionary<IAudioPlugin, AudioPluginConfig> audioPlugins,
            Dictionary<IAudioPlugin, string> audioFiles,
            out int numInputsUsed,
            out string inputArgs,
            out string filterArgs,
            out string mapArgs,
            out string codecArgs);
    }

    internal static class AudioCodecCommon
    {
        internal static void GetArguments(
            int sampleRate,
            float duration,
            Dictionary<IAudioPlugin, AudioPluginConfig> audioPlugins,
            Dictionary<IAudioPlugin, string> audioFiles,
            out int numInputsUsed,
            out string inputArgs,
            out string filterArgs,
            out string mapArgs)
        {
            if (audioPlugins.Values.All(x => !x.enabled))
            {
                numInputsUsed = 0;
                inputArgs = filterArgs = mapArgs = "";
                return;
            }

            var getFrom = audioPlugins.Where(x => x.Value.enabled).ToList();

            // Input
            inputArgs = string.Concat(getFrom.Select(x => $"-i \"{audioFiles[x.Key]}\" ").ToArray());

            // Filtering
            var sb = new StringBuilder();
            for (int i = 0; i < getFrom.Count; i++)
            {
                // i + 1 because the video already used input 0.
                sb.Append($"[{i + 1}:a]volume={getFrom[i].Value.volume.ToString(CultureInfo.InvariantCulture)}[a_{i}]; ");
            }
            for (int i = 0; i < getFrom.Count; i++)
                sb.Append($"[a_{i}]");
            sb.Append($"amix=inputs={getFrom.Count}:normalize=0,atrim=duration={duration.ToString(CultureInfo.InvariantCulture)},aresample={sampleRate}[aud]");
            filterArgs = sb.ToString();

            // Mapping
            mapArgs = "-map [aud]";

            // Meta
            numInputsUsed = getFrom.Count + 1; // + 1 due to video
        }
    }
}
