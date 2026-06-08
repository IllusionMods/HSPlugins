using System.IO;

namespace VideoExport.AudioPlugins
{
    public interface IAudioPlugin
    {
        /// <summary>
        /// Name of the plugin for display purposes
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Name of the plugin for ID purposes. Alphanumeric, _ and . only, 3-20 characters!
        /// </summary>
        string SafeName { get; }

        /// <summary>
        /// Assembles and returns the audio that should be included alongside the video file.
        /// <para>
        /// The returned stream's bytes are written to a temporary file that is then passed to ffmpeg
        /// with no format hints (<c>-i file</c>), so the data MUST be a complete,
        /// ffmpeg-demuxable audio container (e.g. WAV or MP3). Raw headerless PCM will be misdetected and fail.
        /// </para>
        /// <para>
        /// Prefer a stereo (2-channel) stream: multiple enabled providers are mixed together (amix), and an
        /// unusual channel layout may not be supported by the chosen codec/container.
        /// </para>
        /// <para>
        /// Ownership of the returned <see cref="BinaryReader"/> (and its underlying stream) transfers to the
        /// caller, which disposes it after reading. Do not reuse or dispose it yourself.
        /// </para>
        /// </summary>
        /// <param name="startTime">Start time of export in Timeline in seconds</param>
        /// <param name="duration">Total length of the final mix in seconds</param>
        /// <param name="sampleRate">Sample rate that'll be used in the final mix</param>
        /// <returns>A BinaryReader over a ffmpeg-demuxable audio container.</returns>
        BinaryReader MakeAudioStream(float startTime, float duration, int sampleRate);
    }

    internal class AudioPluginConfig
    {
        public readonly IAudioPlugin plugin;

        public bool enabled;
        public float volume;

        public AudioPluginConfig(IAudioPlugin plugin)
        {
            this.plugin = plugin;

            enabled = true;
            volume = 1.0f;
        }
    }
}
