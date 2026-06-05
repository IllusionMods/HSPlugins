using System.IO;

#if DEBUG
namespace VideoExport.AudioPlugins
{
    internal class DummyPlugin : IAudioPlugin
    {
        public string Name { get { return "Dummy Plugin"; } }
        public string SafeName { get { return "DummyPlugin"; } }

        public BinaryReader MakeAudioStream(float startTime, float duration, int sampleRate)
        {
            return new BinaryReader(File.OpenRead(@"<yourExamplePathHere>.mp3"));
        }
    }
}
#endif
