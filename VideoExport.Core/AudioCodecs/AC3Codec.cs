using System.Collections.Generic;
using VideoExport.AudioPlugins;
using UnityEngine;

namespace VideoExport.AudioCodecs
{
    internal class AC3Codec : IAudioCodec
    {

        public string Name { get { return "AC3"; } }

        public AC3Codec()
        {
        }

        public void DisplayParams()
        {
            GUILayout.Space(30);
            GUILayout.Label(new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.AC3NoOpts).Replace("\\n", "\n")), Styles.CenteredLabelStyle);
        }

        public void SaveParams()
        {
        }

        public void GetArguments(
            int sampleRate,
            float duration,
            Dictionary<IAudioPlugin, AudioPluginConfig> audioPlugins,
            Dictionary<IAudioPlugin, string> audioFiles,
            out int numInputsUsed,
            out string inputArgs,
            out string filterArgs,
            out string mapArgs,
            out string codecArgs)
        {
            codecArgs = $"-c:a ac3";

            AudioCodecCommon.GetArguments(sampleRate, duration, audioPlugins, audioFiles, out numInputsUsed, out inputArgs, out filterArgs, out mapArgs);
        }
    }
}
