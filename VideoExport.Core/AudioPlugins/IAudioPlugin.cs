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
        /// </summary>
        /// <param name="startTime">Start time of export in Timeline in seconds</param>
        /// <param name="duration">Total length of the final mix in seconds</param>
        /// <param name="sampleRate">Sample rate that'll be used in the final mix</param>
        /// <returns>The BinaryReader of the assembled audio</returns>
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
