using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using VideoExport.AudioPlugins;

namespace VideoExport.AudioCodecs
{
    internal interface IAudioCodec
    {
        string Name { get; }

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
            int i = 1; // Starting from 1 because video already used one input
            var sb = new StringBuilder();
            foreach (var kvp in getFrom)
            {
                sb.Append($"[{i}:a]volume={kvp.Value.volume.ToString(CultureInfo.InvariantCulture)}[a_{kvp.Key.SafeName}]; ");
                i++;
            }
            sb.Append(string.Concat(getFrom.Select(x => $"[a_{x.Key.SafeName}]").ToArray()));
            sb.Append($"amix=inputs={getFrom.Count}:normalize=0,atrim=duration={duration.ToString(CultureInfo.InvariantCulture)},aresample={sampleRate}[aud]");
            filterArgs = sb.ToString();

            // Mapping
            mapArgs = "-map [aud]";

            // Meta
            numInputsUsed = getFrom.Count + 1; // + 1 due to video
        }
    }
}
