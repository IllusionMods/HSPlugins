using System;
using System.IO;

namespace PackPlugins
{
    public class ReleaseFile
    {
        public string Name;
        public string Path;
        public PackArchive Archive;
        public string DestinationPath;
        public string NewName;

        public void Release(string version)
        {
            Console.WriteLine($"Releasing {this.Name}");
            string path;
            if (string.IsNullOrEmpty(this.Path) == false)
                path = this.Path;
            else
                path = System.IO.Path.Combine(this.Archive.DestinationDirectory, this.Archive.Name + ".zip");
            string destinationFileName = (this.NewName != null ? this.NewName : System.IO.Path.GetFileNameWithoutExtension(path)) + System.IO.Path.GetExtension(path);
            string destinationDirectory = System.IO.Path.Combine(this.DestinationPath, this.Name + " " + version);
            string destination = System.IO.Path.Combine(destinationDirectory, destinationFileName);
            if (Directory.Exists(destinationDirectory) == false)
                Directory.CreateDirectory(destinationDirectory);
            Console.WriteLine($"\tCopying {path} to {destination}");
            File.Copy(path, destination, true);
            Console.WriteLine("Done\n");
        }
    }
}
