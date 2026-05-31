using System;
using UnityEngine;
using VideoExport.AudioExtensions;

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

        public string GetArguments(int bitrate)
        {
            return AudioCodecCommon.GetArguments(bitrate, Name);
        }
    }
}
