using System;
using System.Collections.Generic;
using UnityEngine;
using VideoExport.AudioPlugins;

namespace VideoExport.AudioCodecs
{
    internal class AACCodec : IAudioCodec
    {
        public enum CodingMethod
        {
            TwoLoop,
            ANMR,
            Fast
        }
        public enum CodingProfile
        {
            Low,
            MPEG2Low,
            LTP
        }

        public string Name { get { return "AAC"; } }
        public AudioCodecType CodecType { get { return AudioCodecType.AAC; } }
        public int Bitrate { get; set; }
        public CodingMethod Method { get; set; }
        public CodingProfile Profile { get; set; }

        public AACCodec()
        {
            Bitrate = VideoExport._configFile.AddInt("aacBitrate", 128, true);
            Method = (CodingMethod)VideoExport._configFile.AddInt("aacCodingMethod", (int)CodingMethod.TwoLoop, true);
            Profile = (CodingProfile)VideoExport._configFile.AddInt("aacCodingProfile", (int)CodingProfile.Low, true);
        }

        public void DisplayParams()
        {
            GUILayout.Label(new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.AudioBitrate)));
            GUILayout.BeginHorizontal();
            {
                Bitrate = Mathf.RoundToInt(GUILayout.HorizontalSlider(Bitrate, 32, 256));
                GUILayout.Label(Bitrate.ToString("000"), GUILayout.ExpandWidth(false));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.AACMethod)), GUILayout.Width(69));
                Method = (CodingMethod)GUILayout.SelectionGrid((int)Method, Enum.GetNames(typeof(CodingMethod)), 3);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.AACProfile)), GUILayout.Width(69));
                Profile = (CodingProfile)GUILayout.SelectionGrid((int)Profile, Enum.GetNames(typeof(CodingProfile)), 3);
            }
            GUILayout.EndHorizontal();
        }

        public void SaveParams()
        {
            VideoExport._configFile.SetInt("aacBitrate", Bitrate);
            VideoExport._configFile.SetInt("aacCodingMethod", (int)Method);
            VideoExport._configFile.SetInt("aacCodingProfile", (int)Profile);
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
            string profile = "aac_low";
            switch (Profile)
            {
                case CodingProfile.MPEG2Low:
                    profile = "mpeg2_aac_low";
                    break;
                case CodingProfile.LTP:
                    profile = "aac_ltp";
                    break;
            }

            codecArgs = $"-c:a aac -b:a {Bitrate * 1000} -aac_coder {Method.ToString().ToLower()} -profile:a {profile}";

            AudioCodecCommon.GetArguments(sampleRate, duration, audioPlugins, audioFiles, out numInputsUsed, out inputArgs, out filterArgs, out mapArgs);
        }
    }
}
