namespace PackPlugins
{
    public class ReleaseProfile
    {
        public string Name;
        public ReleaseFile[] Files;

        public void Release(string version)
        {
            foreach (ReleaseFile file in this.Files)
                file.Release(version);
        }
    }
}
