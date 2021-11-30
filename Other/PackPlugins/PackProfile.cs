using System;

namespace PackPlugins
{
    public class PackProfile
    {
        public string Name;
        public PackArchive[] Archives;

        public void Pack()
        {
            Console.WriteLine($"Using profile \"{this.Name}\"");
            foreach (PackArchive archive in this.Archives)
                archive.Pack();
        }
    }
}
