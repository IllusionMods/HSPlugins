using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using VideoExport.AudioCodecs;
using VideoExport.AudioPlugins;
using VideoExport.VideoExtensions;
using TimelineCompatibility = ToolBox.TimelineCompatibility;

namespace VideoExport
{
    public partial class VideoExport
    {
        #region Fields
        private readonly List<IAudioPlugin> _audioPlugins = new List<IAudioPlugin>();

        private bool _includeAudio;
        private int _exportSampleRate;
        private int _selectedCodec;
        private readonly List<IAudioCodec> _codecs = new List<IAudioCodec>();
        private readonly int[] _presetSampleRate = { 8000, 11025, 16000, 22050, 32000, 44100, 88200 };
        private string[] _presetSampleRateLabels;
        private readonly Dictionary<IAudioPlugin, AudioPluginConfig> _audioPluginConfigs = new Dictionary<IAudioPlugin, AudioPluginConfig>();

        private readonly List<int> _compatibleCodecs = new List<int>();
        // ReSharper disable once UseArrayEmptyMethod
        private string[] _compatibleCodecNames = new string[0];
        private ExtensionsType _cachedCodecExtension;
        private bool _codecCacheValid;

        private bool _showAudioSection;

        // Audio codecs each video container can mux. Containers not listed (GIF/WEBP/AVIF) carry no audio.
        private static readonly Dictionary<ExtensionsType, HashSet<AudioCodecType>> _containerAudioCodecs = new Dictionary<ExtensionsType, HashSet<AudioCodecType>>
        {
            { ExtensionsType.MP4,  new HashSet<AudioCodecType> { AudioCodecType.AAC, AudioCodecType.AC3, AudioCodecType.Opus } },
            { ExtensionsType.WEBM, new HashSet<AudioCodecType> { AudioCodecType.Opus, AudioCodecType.Vorbis } },
            { ExtensionsType.MOV,  new HashSet<AudioCodecType> { AudioCodecType.AAC, AudioCodecType.AC3 } },
            { ExtensionsType.MKV,  new HashSet<AudioCodecType> { AudioCodecType.AAC, AudioCodecType.AC3, AudioCodecType.FLAC, AudioCodecType.Opus, AudioCodecType.Vorbis } },
        };
        #endregion

        #region Init / teardown
        private void InitAudio()
        {
            _codecs.Add(new AACCodec());
            _codecs.Add(new AC3Codec());
            _codecs.Add(new FLACCodec());
            _codecs.Add(new OpusCodec());
            _codecs.Add(new VorbisCodec());

            if (_selectedCodec >= _codecs.Count)
                _selectedCodec = 0;

            _presetSampleRateLabels = _presetSampleRate.Select(r => (r / 1000f).ToString("0.0")).ToArray();
        }

        private void SaveAudioConfig()
        {
            _configFile.SetBool("includeAudio", _includeAudio);
            _configFile.SetInt("selectedCodec", _selectedCodec);
            _configFile.SetInt("exportSampleRate", _exportSampleRate);

            foreach (IAudioCodec codec in _codecs)
                codec.SaveParams();
        }
        #endregion

        #region Container / codec compatibility
        private static bool ContainerSupportsAudio(ExtensionsType ext)
        {
            return _containerAudioCodecs.TryGetValue(ext, out HashSet<AudioCodecType> codecs) && codecs.Count > 0;
        }

        private static bool CodecCompatibleWithContainer(ExtensionsType ext, AudioCodecType codec)
        {
            return _containerAudioCodecs.TryGetValue(ext, out HashSet<AudioCodecType> codecs) && codecs.Contains(codec);
        }
        #endregion

        #region Public API

        /// <summary>
        /// Register a new audio provider, which must implement the IAudioPlugin interface.
        /// </summary>
        /// <typeparam name="T">Type of the registered audio provider, must implement IAudioPlugin</typeparam>
        /// <param name="plugin">The instance of the registered audio provider</param>
        /// <param name="error">On success, set to "-". On failure, set to a reason the
        /// registration was rejected (e.g. duplicate plugin/type, invalid or already-used SafeName).</param>
        /// <returns>Whether the registration was successful</returns>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedMethodReturnValue.Global
        public bool AddAudioPlugin<T>(T plugin, out string error) where T : IAudioPlugin
        {
            try
            {
                if (_audioPlugins.Contains(plugin))
                    error = "Plugin already registered!";
                else if (_audioPlugins.Select(x => x.GetType()).Contains(plugin.GetType()))
                    error = "A plugin of the same type has already been registered!";
                else if (!Regex.IsMatch(plugin.SafeName, @"^[a-zA-Z0-9_\.]{3,20}$"))
                    error = "The plugin's safe name doesn't match the criteria (alphanumeric, '_' and '.', 3-20 chars)!";
                else if (_audioPlugins.Any(x => x.SafeName == plugin.SafeName))
                    error = "The plugin's safe name is already in use!";
                else
                {
                    _audioPlugins.Add(plugin);
                    _audioPluginConfigs.Add(plugin, new AudioPluginConfig(plugin));

                    Logger.LogInfo($"Registered audio plugin '{plugin.SafeName}' ({typeof(T).FullName}).");
                    error = "-";
                    return true;
                }

                Logger.LogWarning($"Audio plugin registration rejected for {typeof(T).FullName}: {error}");
                return false;
            }
            catch (Exception e)
            {
                error = "Couldn't add audio plugin " + typeof(T).FullName + "!\n" + e;
                Logger.LogError(error);
                return false;
            }
        }
        #endregion

        #region GUI
        private void WindowAudioSection()
        {
            GUILayout.BeginVertical("Box");
            {
                GUILayout.BeginHorizontal(Styles.headerStyle);
                {
                    GUILayout.Space(30);
                    GUI.backgroundColor = Color.gray;
                    GUILayout.Label(_currentDictionary.GetString(TranslationKey.AudioSectionHeading), Styles.SectionLabelStyle);
                    if (GUILayout.Button(_showAudioSection ? "▲" : "▼", GUILayout.Width(30)))
                    {
                        _showAudioSection = !_showAudioSection;
                    }
                    GUI.backgroundColor = Color.white;
                }
                GUILayout.EndHorizontal();

                if (!_showAudioSection)
                {
                    GUILayout.EndVertical();
                    return;
                }

                GUILayout.BeginHorizontal(Styles.EmptyBoxStyle);
                {
                    _includeAudio = GUILayout.Toggle(_includeAudio, new GUIContent(_currentDictionary.GetString(TranslationKey.InclAudio), _currentDictionary.GetString(TranslationKey.InclAudioTooltip).Replace("\\n", "\n")));
                    if (!_includeAudio)
                    {
                        GUILayout.EndHorizontal();
                        GUILayout.EndVertical();
                        return;
                    }
                }
                GUILayout.EndHorizontal();

                if (!ContainerSupportsAudio(_selectedExtension))
                {
                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label(_currentDictionary.GetString(TranslationKey.AudioUnsupportedFormat), Styles.CenteredLabelStyle);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(20);

                    GUILayout.EndVertical();
                    return;
                }

                if (_audioPlugins.Count == 0)
                {
                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label(_currentDictionary.GetString(TranslationKey.NoAudioPlugins), Styles.CenteredLabelStyle);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(20);

                    GUILayout.EndVertical();
                    return;
                }

                bool prevEnabled;
                foreach (var plugin in _audioPlugins)
                {
                    var config = _audioPluginConfigs[plugin];
                    string displayName;
                    try { displayName = plugin.Name; }
                    catch { displayName = "<broken plugin>"; }
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label(displayName);
                        config.enabled = GUILayout.Toggle(config.enabled, _currentDictionary.GetString(TranslationKey.Enabled), GUILayout.ExpandWidth(false));
                    }
                    GUILayout.EndHorizontal();

                    prevEnabled = GUI.enabled;
                    GUI.enabled = config.enabled;
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label(new GUIContent(_currentDictionary.GetString(TranslationKey.AudioPluginVolume), _currentDictionary.GetString(TranslationKey.AudioPluginVolumeTooltip)), GUILayout.ExpandWidth(false));
                        float newVol = Mathf.RoundToInt(GUILayout.HorizontalSlider(config.volume, 0, 2) * 100) / 100f;
                        if (!Mathf.Approximately(newVol, config.volume))
                            config.volume = newVol;
                        GUILayout.Label(config.volume.ToString("0.00"), Styles.CenteredLabelStyle, GUILayout.Width(50));
                    }
                    GUILayout.EndHorizontal();
                    GUI.enabled = prevEnabled;
                }

                GUILayout.Space(10);

                prevEnabled = GUI.enabled;
                GUI.enabled = _audioPluginConfigs.Any(x => x.Value.enabled);
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(_currentDictionary.GetString(TranslationKey.AudioSamples));

                    for (int i = 0; i < _presetSampleRate.Length; i++)
                        if (GUILayout.Button(_presetSampleRateLabels[i], Styles.ButtonStyle, GUILayout.Width(42)))
                            _exportSampleRate = _presetSampleRate[i];

                    string sampleString = GUILayout.TextField(_exportSampleRate.ToString(), GUILayout.Width(50), GUILayout.Height(30));
                    if (int.TryParse(sampleString, out int res))
                        _exportSampleRate = Mathf.Clamp(res, 8000, 192000);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.BeginVertical(GUILayout.Width(Styles.WindowWidth / 7f));
                    {
                        GUILayout.Label(new GUIContent(_currentDictionary.GetString(TranslationKey.AudioEncoder), _currentDictionary.GetString(TranslationKey.AudioEncoderTooltip).Replace("\\n", "\n")), Styles.CenteredLabelStyle);

                        if (!_codecCacheValid || _cachedCodecExtension != _selectedExtension)
                        {
                            _compatibleCodecs.Clear();
                            for (int codecIndex = 0; codecIndex < _codecs.Count; codecIndex++)
                                if (CodecCompatibleWithContainer(_selectedExtension, _codecs[codecIndex].CodecType))
                                    _compatibleCodecs.Add(codecIndex);
                            _compatibleCodecNames = _compatibleCodecs.Select(codecIndex => _codecs[codecIndex].Name).ToArray();
                            _cachedCodecExtension = _selectedExtension;
                            _codecCacheValid = true;
                        }
                        if (!_compatibleCodecs.Contains(_selectedCodec))
                            _selectedCodec = _compatibleCodecs[0];
                        int gridSelection = _compatibleCodecs.IndexOf(_selectedCodec);
                        gridSelection = GUILayout.SelectionGrid(gridSelection, _compatibleCodecNames, 1);
                        _selectedCodec = _compatibleCodecs[gridSelection];
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical(Styles.BoxStyle);
                    IAudioCodec codec = _codecs[_selectedCodec];
                    codec.DisplayParams();
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
                GUI.enabled = prevEnabled;
            }
            GUILayout.EndVertical();
        }
        #endregion

        #region Export
        private sealed class AudioFeedResult
        {
            public Dictionary<IAudioPlugin, string> TmpFiles = new Dictionary<IAudioPlugin, string>();
            public string InputArgs = "";
            public string FilterArgs = "";
            public string MapArgs = "";
            public string CodecArgs = "";
        }

        private AudioFeedResult BuildAudioFeed(float audioLength)
        {
            var result = new AudioFeedResult();

            bool audioMuxAllowed = _includeAudio && CodecCompatibleWithContainer(_selectedExtension, _codecs[_selectedCodec].CodecType);
            if (_includeAudio && !audioMuxAllowed)
                Logger.LogWarning($"Audio not muxed: codec {_codecs[_selectedCodec].Name} is not compatible with the {_selectedExtension} container.");

            var pipeFrom = audioMuxAllowed
                ? _audioPluginConfigs.Where(x => x.Value.enabled).ToList()
                : null;
            float audioStart = TimelineCompatibility.GetPlaybackTime();

            if (pipeFrom?.Count() > 0)
            {
                Logger.LogDebug("Starting audio feed...");

                Logger.LogInfo(
                    BuildAlignedLogBlock("Audio feed details:",
                        new[]
                        {
                        "# of active plugins",
                        "Start time",
                        "Duration",
                        "Sampling rate",
                        "Codec"
                        },
                        new[]
                        {
                        pipeFrom.Count().ToString(),
                        audioStart.ToString("0.00"),
                        audioLength.ToString("0.00"),
                        _exportSampleRate.ToString(),
                        _codecs[_selectedCodec].Name
                        }
                    )
                );

                foreach (var kvp in pipeFrom)
                {
                    string pluginName;
                    try { pluginName = kvp.Key.Name; }
                    catch { pluginName = kvp.Key.GetType().FullName; }

                    string tmpAudioFileName;
                    try
                    {
                        tmpAudioFileName = SimplifyPath(Path.Combine(_outputFolder.Value, $"_tmp_{kvp.Key.SafeName}_{_tempDateTime}.bin"));
                    }
                    catch (Exception e)
                    {
                        Logger.LogWarning($"Audio plugin '{pluginName}' threw while resolving its SafeName. Skipping its audio.\n{e}");
                        continue;
                    }

                    int bufferSize = 30000;
                    bool fileCreated = false;

                    try
                    {
                        var br = kvp.Key.MakeAudioStream(audioStart, audioLength, _exportSampleRate);
                        if (br == null)
                        {
                            Logger.LogWarning($"Audio plugin '{pluginName}' returned no audio stream. Skipping its audio.");
                            continue;
                        }

                        using (br)
                        using (var fs = File.Open(tmpAudioFileName, FileMode.Create, FileAccess.Write, FileShare.Read))
                        {
                            fileCreated = true;
                            while (true)
                            {
                                byte[] data = br.ReadBytes(bufferSize);
                                if (data.Length == 0)
                                    break;
                                fs.Write(data, 0, data.Length);
                            }
                        }

                        result.TmpFiles[kvp.Key] = tmpAudioFileName;
                    }
                    catch (Exception e)
                    {
                        Logger.LogWarning($"Audio plugin '{pluginName}' threw while producing audio. Skipping its audio. The rest of the export will continue.\n{e}");
                        if (fileCreated)
                        {
                            try { File.Delete(tmpAudioFileName); } catch { /* best-effort cleanup of partial temp file */ }
                        }
                    }
                }

                Logger.LogDebug("Audio streams finished writing.");
            }

            if (audioMuxAllowed && result.TmpFiles.Count > 0)
            {
                _codecs[_selectedCodec].GetArguments(_exportSampleRate, audioLength,
                    pipeFrom.Where(x => result.TmpFiles.ContainsKey(x.Key)).ToDictionary(x => x.Key, x => x.Value), result.TmpFiles,
                    out _, out string inputArgs, out string filterArgs, out string mapArgs, out string codecArgs);
                result.InputArgs = inputArgs;
                result.FilterArgs = filterArgs;
                result.MapArgs = mapArgs;
                result.CodecArgs = codecArgs;
            }

            return result;
        }
        #endregion
    }
}
