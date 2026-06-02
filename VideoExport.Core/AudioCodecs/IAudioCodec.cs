using System.Collections.Generic;
using System.Text;
using VideoExport.AudioPlugins;
using System.Linq;

namespace VideoExport.AudioExtensions
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
                sb.Append($"[{i}:a]volume={kvp.Value.volume}[{kvp.Key.SafeName}]; ");
                i++;
            }
            sb.Append(string.Concat(getFrom.Select(x => $"[{x.Key.SafeName}]").ToArray()));
            sb.Append($"amerge=inputs={getFrom.Count},atrim=duration={duration},aresample={sampleRate}[aud]");
            filterArgs = sb.ToString();

            // Mapping
            mapArgs = "-map [aud]";

            // Meta
            numInputsUsed = getFrom.Count + 1; // + 1 due to video
        }
    }
}
