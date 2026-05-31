using System;
using System.Linq;
using UnityEngine;
using VideoExport.AudioExtensions;

namespace VideoExport.AudioCodecs
{
    internal class OpusCodec : IAudioCodec
    {
        public enum VBRType
        {
            Off,
            On,
            Constrained
        }

        public readonly float[] frameSizeOpts = new[] { 2.5f, 5f, 10f, 20f, 40f, 60f };

        public string Name { get { return "Opus"; } }
        public int Compression { get; set; }
        public int Bitrate { get; set; }
        public VBRType VBR { get; set; }
        public int Framesize { get; set; }

        public OpusCodec()
        {
            Compression = VideoExport._configFile.AddInt("opusCompression", 10, true);
            Bitrate = VideoExport._configFile.AddInt("opusBitrate", 128, true);
            VBR = (VBRType)VideoExport._configFile.AddInt("opusVBR", (int)VBRType.Off, true);
            Framesize = VideoExport._configFile.AddInt("opusFramesize", 3, true);
        }

        public void DisplayParams()
        {
            GUILayout.Label(new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.OpusCompression)));
            GUILayout.BeginHorizontal();
            {
                Compression = Mathf.RoundToInt(GUILayout.HorizontalSlider(Compression, 0, 10));
                GUILayout.Label(Compression.ToString("00"), GUILayout.ExpandWidth(false));
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.AudioBitrate)));
            GUILayout.BeginHorizontal();
            {
                Bitrate = Mathf.RoundToInt(GUILayout.HorizontalSlider(Bitrate, 32, 256));
                GUILayout.Label(this.Bitrate.ToString("000"), GUILayout.ExpandWidth(false));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.OpusVBR)), GUILayout.Width(80));
                VBR = (VBRType)GUILayout.SelectionGrid((int)VBR, Enum.GetNames(typeof(VBRType)), 3);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.OpusFramesize)), GUILayout.Width(80));
                Framesize = GUILayout.SelectionGrid(Framesize, frameSizeOpts.Select(x => x.ToString()).ToArray(), 6);
            }
            GUILayout.EndHorizontal();
        }

        public void SaveParams()
        {
            VideoExport._configFile.SetInt("opusCompression", Compression);
            VideoExport._configFile.SetInt("opusBitrate", Bitrate);
            VideoExport._configFile.SetInt("opusVBR", (int)VBR);
            VideoExport._configFile.SetInt("opusFramesize", Framesize);
        }

        public string GetArguments(int bitrate)
        {
            return AudioCodecCommon.GetArguments(bitrate, Name);
        }
    }
}
