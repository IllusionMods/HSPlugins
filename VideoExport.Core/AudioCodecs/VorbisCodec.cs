using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using VideoExport.AudioPlugins;

namespace VideoExport.AudioCodecs
{
    internal class VorbisCodec : IAudioCodec
    {
        public string Name { get { return "Vorbis"; } }
        public bool UseVBR { get; set; }
        public int Bitrate { get; set; }
        public float Quality { get; set; }
        public int MinRate { get; set; }
        public int MaxRate { get; set; }
        public float IBlock { get; set; }

        public VorbisCodec()
        {
            UseVBR = VideoExport._configFile.AddBool("vorbisVBR", false, true);
            Bitrate = VideoExport._configFile.AddInt("vorbisBitrate", 128, true);
            Quality = VideoExport._configFile.AddFloat("vorbisQuality", 3f, true);
            MinRate = VideoExport._configFile.AddInt("vorbisMinRate", 32, true);
            MaxRate = VideoExport._configFile.AddInt("vorbisMaxRate", 192, true);
            IBlock = VideoExport._configFile.AddFloat("vorbisIBlock", 0f, true);
        }

        public void DisplayParams()
        {
            GUILayout.BeginHorizontal(Styles.EmptyBoxStyle);
            {
                UseVBR = GUILayout.Toggle(UseVBR, new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.VorbisVBR)));
            }
            GUILayout.EndHorizontal();

            if (!UseVBR)
            {
                GUILayout.Label(new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.AudioBitrate)));
                GUILayout.BeginHorizontal();
                {
                    Bitrate = Mathf.RoundToInt(GUILayout.HorizontalSlider(Bitrate, 32, 256));
                    GUILayout.Label(Bitrate.ToString("0"), Styles.CenteredLabelStyle, GUILayout.Width(50));
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label(new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.VorbisQuality)));
                GUILayout.BeginHorizontal();
                {
                    Quality = GUILayout.HorizontalSlider(Quality, -1, 10);
                    GUILayout.Label(Quality.ToString("0.0"), Styles.CenteredLabelStyle, GUILayout.Width(50));
                }
                GUILayout.EndHorizontal();

                GUILayout.Label(new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.VorbisMinRate)));
                GUILayout.BeginHorizontal();
                {
                    int newMin = Mathf.RoundToInt(GUILayout.HorizontalSlider(MinRate, 32, 256));
                    if (newMin > MaxRate && newMin != MinRate)
                        MaxRate = newMin;
                    MinRate = newMin;
                    GUILayout.Label(MinRate.ToString("0"), Styles.CenteredLabelStyle, GUILayout.Width(50));
                }
                GUILayout.EndHorizontal();

                GUILayout.Label(new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.VorbisMaxRate)));
                GUILayout.BeginHorizontal();
                {
                    int newMax = Mathf.RoundToInt(GUILayout.HorizontalSlider(MaxRate, 32, 256));
                    if (newMax < MinRate && newMax != MaxRate)
                        MinRate = newMax;
                    MaxRate = newMax;
                    GUILayout.Label(MaxRate.ToString("0"), Styles.CenteredLabelStyle, GUILayout.Width(50));
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Label(new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.VorbisIBlock)));
            GUILayout.BeginHorizontal();
            {
                IBlock = GUILayout.HorizontalSlider(IBlock, -15, 0);
                GUILayout.Label(IBlock.ToString("0.0"), Styles.CenteredLabelStyle, GUILayout.Width(50));
            }
            GUILayout.EndHorizontal();
        }

        public void SaveParams()
        {
            VideoExport._configFile.SetBool("vorbisVBR", UseVBR);
            VideoExport._configFile.SetInt("vorbisBitrate", Bitrate);
            VideoExport._configFile.SetFloat("vorbisQuality", Quality);
            VideoExport._configFile.SetInt("vorbisMinRate", MinRate);
            VideoExport._configFile.SetInt("vorbisMaxRate", MaxRate);
            VideoExport._configFile.SetFloat("vorbisIBlock", IBlock);
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
            var sb = new StringBuilder("-c:a libvorbis ");
            if (UseVBR)
            {
                sb.Append($"-q:a {Quality.ToString(CultureInfo.InvariantCulture)} ");
                sb.Append($"-minrate:a {MinRate * 1000} ");
                sb.Append($"-maxrate:a {MaxRate * 1000} ");
            }
            else
            {
                sb.Append($"-b:a {Bitrate * 1000} ");
            }
            sb.Append($"-iblock {IBlock.ToString(CultureInfo.InvariantCulture)} ");
            codecArgs = sb.ToString();

            AudioCodecCommon.GetArguments(sampleRate, duration, audioPlugins, audioFiles, out numInputsUsed, out inputArgs, out filterArgs, out mapArgs);
        }
    }
}
