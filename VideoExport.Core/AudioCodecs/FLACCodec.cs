using VideoExport.AudioExtensions;
using System.Collections.Generic;
using VideoExport.AudioPlugins;
using UnityEngine;
using System;

namespace VideoExport.AudioCodecs
{
    internal class FLACCodec : IAudioCodec
    {
        public enum LPCAlgoType
        {
            None,
            Fixed,
            Levinson,
            Cholesky
        }
        public enum PredictionOrder
        {
            _Estimation,
            _2Level,
            _4Level,
            _8Level,
            _search,
            _log
        }
        public readonly string[] PredictionOrderLabels = new string[] { "Est", "2L", "4L", "8L", "Srch", "Log" };

        public string Name { get { return "FLAC"; } }
        public int Compression { get; set; }
        public int LPCPrecision { get; set; }
        public LPCAlgoType LPCType { get; set; }
        public int CholeskyPasses { get; set; }
        public PredictionOrder PredOrder { get; set; }
        public bool ExactRice { get; set; }

        public FLACCodec()
        {
            Compression = VideoExport._configFile.AddInt("flacCompression", 5, true);
            LPCPrecision = VideoExport._configFile.AddInt("flacLPCPrecision", 15, true);
            LPCType = (LPCAlgoType)VideoExport._configFile.AddInt("flacLPCType", (int)LPCAlgoType.None, true);
            CholeskyPasses = VideoExport._configFile.AddInt("flacCholeskyPasses", 1, true);
            PredOrder = (PredictionOrder)VideoExport._configFile.AddInt("flacPredOrder", (int)PredictionOrder._Estimation, true);
            ExactRice = VideoExport._configFile.AddBool("flacExactRice", true, true);
        }

        public void DisplayParams()
        {
            GUILayout.Label(new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.FlacCompression)));
            GUILayout.BeginHorizontal();
            {
                Compression = Mathf.RoundToInt(GUILayout.HorizontalSlider(Compression, 0, 12));
                GUILayout.Label(Compression.ToString("00"), GUILayout.ExpandWidth(false));
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.FlacLPCPrecision)));
            GUILayout.BeginHorizontal();
            {
                LPCPrecision = Mathf.RoundToInt(GUILayout.HorizontalSlider(LPCPrecision, 1, 15));
                GUILayout.Label(LPCPrecision.ToString("00"), GUILayout.ExpandWidth(false));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.FlacLPCType)), GUILayout.Width(69));
                LPCType = (LPCAlgoType)GUILayout.SelectionGrid((int)LPCType, Enum.GetNames(typeof(LPCAlgoType)), 4);
            }
            GUILayout.EndHorizontal();

            if (LPCType == LPCAlgoType.Cholesky)
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.FlacCholeskyPasses)), GUILayout.Width(69));
                    CholeskyPasses = Mathf.RoundToInt(GUILayout.HorizontalSlider(CholeskyPasses, 1, 8));
                    GUILayout.Label(CholeskyPasses.ToString("0"), GUILayout.ExpandWidth(false));
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.FlacPredictionOrder)), GUILayout.Width(69));
                PredOrder = (PredictionOrder)GUILayout.SelectionGrid((int)PredOrder, PredictionOrderLabels, 6);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent(VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.FlacExactRice)), GUILayout.Width(69));
                ExactRice = 0 == GUILayout.SelectionGrid(ExactRice ? 0 : 1, new[] { VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.Enabled), VideoExport._currentDictionary.GetString(VideoExport.TranslationKey.Disabled) }, 2);
            }
            GUILayout.EndHorizontal();
        }

        public void SaveParams()
        {
            VideoExport._configFile.SetInt("flacCompression", Compression);
            VideoExport._configFile.SetInt("flacLPCPrecision", LPCPrecision);
            VideoExport._configFile.SetInt("flacLPCType", (int)LPCType);
            VideoExport._configFile.SetInt("flacCholeskyPasses", CholeskyPasses);
            VideoExport._configFile.SetInt("flacPredOrder", (int)PredOrder);
            VideoExport._configFile.SetBool("flacExactRice", ExactRice);
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
            codecArgs = $"";

            AudioCodecCommon.GetArguments(sampleRate, duration, audioPlugins, audioFiles, out numInputsUsed, out inputArgs, out filterArgs, out mapArgs);
        }
    }
}
