using System;
using System.Linq;

namespace PackPlugins
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Console.WriteLine("0: Pack");
                Console.WriteLine("1: Release");
                Console.WriteLine("2: Quit");
                string line = Console.ReadLine();
                if (int.TryParse(line, out int index))
                {
                    switch (index)
                    {
                        case 0:
                            Pack();
                            break;
                        case 1:
                            Release();
                            break;
                        case 2:
                            goto EXIT_PROGRAM;
                    }
                }
                else
                    Console.WriteLine("Try again");
            }
            EXIT_PROGRAM: 
            ;
        }

        private static void Pack()
        {
            Console.WriteLine("Select a pack profile:");
            for (int i = 0; i < _packProfiles.Length; i++)
            {
                PackProfile profile = _packProfiles[i];
                Console.WriteLine($"{i}: {profile.Name}");
            }
            string line = Console.ReadLine();
            if (int.TryParse(line, out int index))
            {
                _packProfiles[index].Pack();
                Console.WriteLine("Successfully packed");
            }
            else
                Console.WriteLine("Try again");
        }

        private static void Release()
        {
            Console.WriteLine("Select a release profile:");
            for (int i = 0; i < _releaseProfiles.Length; i++)
            {
                ReleaseProfile profile = _releaseProfiles[i];
                Console.WriteLine($"{i}: {profile.Name}");
            }
            string line = Console.ReadLine();
            if (int.TryParse(line, out int index))
            {
                Console.WriteLine("Type in version");
                line = Console.ReadLine();
                line = line.Replace("\n", "");
                try
                {
                    new Version(line);
                }
                catch
                {
                    Console.WriteLine("Try again");
                    return;
                }
                _releaseProfiles[index].Release(line);
                Console.WriteLine("Successfully released");
            }
            else
                Console.WriteLine("Try again");

        }

        private static readonly PackProfile[] _packProfiles = 
        {
            new PackProfile()
            {
                Name = "HSPE",
                Archives = new []
                {
                    new PackArchive()
                    {
                        Name = "HSPE",
                        RootDirectory = @"D:\Program Files (x86)\HoneySelect",
                        DestinationDirectory = @"D:\Program Files (x86)\HoneySelect\Modding",
                        Files = new []
                        {
                            @"Plugins\HSPENeo.dll",
                            @"abdata\studioneo\Joan6694\dynamic_bone_collider.unity3d",
                            @"abdata\studioneo\HoneyselectItemResolver\Joan6694 Dynamic Bone Collider.txt",
                        }
                    },
                    new PackArchive()
                    {
                        Name = "KKPE",
                        RootDirectory = @"D:\Program Files (x86)\Koikatu",
                        DestinationDirectory = @"D:\Program Files (x86)\Koikatu\Modding",
                        Files = new []
                        {
                            @"BepInEx\plugins\KKPE.dll",
                            @"mods\Joan6694DynamicBoneCollider.zipmod",
                        }
                    },
                    new PackArchive()
                    {
                        Name = "AIPE",
                        RootDirectory = @"D:\Program Files (x86)\AI-Syoujyo",
                        DestinationDirectory = @"D:\Program Files (x86)\AI-Syoujyo\Modding",
                        Files = new []
                        {
                            @"BepInEx\plugins\AIPE.dll",
                            @"mods\Joan6694DynamicBoneColliders.zipmod",
                        }
                    },
                    new PackArchive()
                    {
                        Name = "HS2PE",
                        RootDirectory = @"D:\Program Files (x86)\HoneySelect2",
                        DestinationDirectory = @"D:\Program Files (x86)\HoneySelect2\Modding",
                        Files = new []
                        {
                            @"BepInEx\plugins\HS2PE.dll",
                            @"mods\Joan6694DynamicBoneColliders.zipmod",
                        }
                    }
                }
            },
            new PackProfile()
            {
                Name = "RendererEditor",
                Archives = new []
                {
                    new PackArchive()
                    {
                        Name = "HSRendererEditor",
                        RootDirectory = @"D:\Program Files (x86)\HoneySelect",
                        DestinationDirectory = @"D:\Program Files (x86)\HoneySelect\Modding",
                        Files = new []
                        {
                            @"Plugins\RendererEditor.dll",
                            @"Plugins\RendererEditor\Textures\ReplaceMe.png",
                            @"abdata\studioneo\Joan6694\projector.unity3d",
                            @"abdata\studioneo\HoneyselectItemResolver\Joan6694 Projectors.txt"
                        }
                    }, 
                    new PackArchive()
                    {
                        Name = "KKRendererEditor",
                        RootDirectory = @"D:\Program Files (x86)\Koikatu",
                        DestinationDirectory = @"D:\Program Files (x86)\Koikatu\Modding",
                        Files = new []
                        {
                            @"BepInEx\plugins\RendererEditor.dll",
                            @"BepInEx\plugins\RendererEditor\Textures\ReplaceMe.png",
                            @"mods\Joan6694Projectors.zipmod",
                        }
                    }, 
                    new PackArchive()
                    {
                        Name = "AIRendererEditor",
                        RootDirectory = @"D:\Program Files (x86)\AI-Syoujyo",
                        DestinationDirectory = @"D:\Program Files (x86)\AI-Syoujyo\Modding",
                        Files = new []
                        {
                            @"BepInEx\plugins\RendererEditor.dll",
                            @"BepInEx\plugins\RendererEditor\Textures\ReplaceMe.png",
                            @"mods\Joan6694Projectors.zipmod",
                        }
                    }, 
                    new PackArchive()
                    {
                        Name = "HS2RendererEditor",
                        RootDirectory = @"D:\Program Files (x86)\HoneySelect2",
                        DestinationDirectory = @"D:\Program Files (x86)\HoneySelect2\Modding",
                        Files = new []
                        {
                            @"BepInEx\plugins\RendererEditor.dll",
                            @"BepInEx\plugins\RendererEditor\Textures\ReplaceMe.png",
                            @"mods\Joan6694Projectors.zipmod",
                        }
                    }, 
                }
            },
            new PackProfile()
            {
                Name = "VideoExport",
                Archives = new []
                {
                    new PackArchive()
                    {
                        Name = "HSVideoExport",
                        RootDirectory = @"D:\Program Files (x86)\HoneySelect",
                        DestinationDirectory = @"D:\Program Files (x86)\HoneySelect\Modding",
                        Files = new []
                        {
                            @"Plugins\VideoExport.dll",
                            @"Plugins\VideoExport\ffmpeg\ffmpeg.exe",
                            @"Plugins\VideoExport\ffmpeg\ffmpeg-64.exe",
                            @"Plugins\VideoExport\ffmpeg\LICENSE.txt",
                            @"Plugins\VideoExport\gifski\gifski.exe",
                            @"Plugins\VideoExport\gifski\LICENSE",
                        }
                    }, 
                    new PackArchive()
                    {
                        Name = "KKVideoExport",
                        RootDirectory = @"D:\Program Files (x86)\Koikatu",
                        DestinationDirectory = @"D:\Program Files (x86)\Koikatu\Modding",
                        Files = new []
                        {
                            @"BepInEx\plugins\VideoExport.dll",
                            @"BepInEx\plugins\VideoExport\ffmpeg\ffmpeg.exe",
                            @"BepInEx\plugins\VideoExport\ffmpeg\ffmpeg-64.exe",
                            @"BepInEx\plugins\VideoExport\ffmpeg\LICENSE.txt",
                            @"BepInEx\plugins\VideoExport\gifski\gifski.exe",
                            @"BepInEx\plugins\VideoExport\gifski\LICENSE",
                        }
                    }, 
                    new PackArchive()
                    {
                        Name = "AIVideoExport",
                        RootDirectory = @"D:\Program Files (x86)\AI-Syoujyo",
                        DestinationDirectory = @"D:\Program Files (x86)\AI-Syoujyo\Modding",
                        Files = new []
                        {
                            @"BepInEx\plugins\VideoExport.dll",
                            @"BepInEx\plugins\VideoExport\ffmpeg\ffmpeg.exe",
                            @"BepInEx\plugins\VideoExport\ffmpeg\ffmpeg-64.exe",
                            @"BepInEx\plugins\VideoExport\ffmpeg\LICENSE.txt",
                            @"BepInEx\plugins\VideoExport\gifski\gifski.exe",
                            @"BepInEx\plugins\VideoExport\gifski\LICENSE",
                        }
                    }, 
                    new PackArchive()
                    {
                        Name = "HS2VideoExport",
                        RootDirectory = @"D:\Program Files (x86)\HoneySelect2",
                        DestinationDirectory = @"D:\Program Files (x86)\HoneySelect2\Modding",
                        Files = new []
                        {
                            @"BepInEx\plugins\VideoExport.dll",
                            @"BepInEx\plugins\VideoExport\ffmpeg\ffmpeg.exe",
                            @"BepInEx\plugins\VideoExport\ffmpeg\ffmpeg-64.exe",
                            @"BepInEx\plugins\VideoExport\ffmpeg\LICENSE.txt",
                            @"BepInEx\plugins\VideoExport\gifski\gifski.exe",
                            @"BepInEx\plugins\VideoExport\gifski\LICENSE",
                        }
                    }, 
                }
            },
        };

        private static readonly ReleaseProfile[] _releaseProfiles = new[]
        {
            new ReleaseProfile()
            {
                Name = "Timeline",
                Files = new []
                {
                    new ReleaseFile()
                    {
                        Name = "Timeline",
                        Path = @"D:\Program Files (x86)\HoneySelect\Plugins\Timeline.dll",
                        DestinationPath = @"D:\Joan\Mega\Illusion Stuff\HS\HS Plugins\Timeline"
                    }, 
                    new ReleaseFile()
                    {
                        Name = "Timeline",
                        Path = @"D:\Program Files (x86)\Koikatu\BepInEx\plugins\Timeline.dll",
                        DestinationPath = @"D:\Joan\Mega\Illusion Stuff\KK\KK Plugins\Timeline"
                    }, 
                }
            }, 
            new ReleaseProfile()
            {
                Name = "RendererEditor",
                Files = new []
                {
                    new ReleaseFile()
                    {
                        Name = "RendererEditor",
                        Archive = _packProfiles.First(p => p.Name.Equals("RendererEditor")).Archives.First(a => a.Name.Equals("HSRendererEditor")),
                        DestinationPath = @"D:\Joan\Mega\Illusion Stuff\HS\HS Plugins\RendererEditor",
                        NewName = "RendererEditor"
                    }, 
                    new ReleaseFile()
                    {
                        Name = "RendererEditor",
                        Archive = _packProfiles.First(p => p.Name.Equals("RendererEditor")).Archives.First(a => a.Name.Equals("KKRendererEditor")),
                        DestinationPath = @"D:\Joan\Mega\Illusion Stuff\KK\KK Plugins\RendererEditor",
                        NewName = "RendererEditor"
                    }, 
                    new ReleaseFile()
                    {
                        Name = "RendererEditor",
                        Archive = _packProfiles.First(p => p.Name.Equals("RendererEditor")).Archives.First(a => a.Name.Equals("AIRendererEditor")),
                        DestinationPath = @"D:\Joan\Mega\Illusion Stuff\AI\AI Plugins\RendererEditor",
                        NewName = "RendererEditor"
                    }, 
                    new ReleaseFile()
                    {
                        Name = "RendererEditor",
                        Archive = _packProfiles.First(p => p.Name.Equals("RendererEditor")).Archives.First(a => a.Name.Equals("HS2RendererEditor")),
                        DestinationPath = @"D:\Joan\Mega\Illusion Stuff\HS2\HS2 Plugins\RendererEditor",
                        NewName = "RendererEditor"
                    }, 
                }
            },
            new ReleaseProfile()
            {
                Name = "VideoExport",
                Files = new []
                {
                    new ReleaseFile()
                    {
                        Name = "VideoExport",
                        Archive = _packProfiles.First(p => p.Name.Equals("VideoExport")).Archives.First(a => a.Name.Equals("HSVideoExport")),
                        DestinationPath = @"D:\Joan\Mega\Illusion Stuff\HS\HS Plugins\VideoExport",
                        NewName = "VideoExport"
                    }, 
                    new ReleaseFile()
                    {
                        Name = "VideoExport",
                        Archive = _packProfiles.First(p => p.Name.Equals("VideoExport")).Archives.First(a => a.Name.Equals("KKVideoExport")),
                        DestinationPath = @"D:\Joan\Mega\Illusion Stuff\KK\KK Plugins\VideoExport",
                        NewName = "VideoExport"
                    }, 
                    new ReleaseFile()
                    {
                        Name = "VideoExport",
                        Archive = _packProfiles.First(p => p.Name.Equals("VideoExport")).Archives.First(a => a.Name.Equals("AIVideoExport")),
                        DestinationPath = @"D:\Joan\Mega\Illusion Stuff\AI\AI Plugins\VideoExport",
                        NewName = "VideoExport"
                    }, 
                    new ReleaseFile()
                    {
                        Name = "VideoExport",
                        Archive = _packProfiles.First(p => p.Name.Equals("VideoExport")).Archives.First(a => a.Name.Equals("HS2VideoExport")),
                        DestinationPath = @"D:\Joan\Mega\Illusion Stuff\HS2\HS2 Plugins\VideoExport",
                        NewName = "VideoExport"
                    }, 
                }
            },
            new ReleaseProfile()
            {
                Name = "NodesConstraints",
                Files = new []
                {
                    new ReleaseFile()
                    {
                        Name = "NodesConstraints",
                        Path = @"D:\Program Files (x86)\HoneySelect\Plugins\NodesConstraints.dll",
                        DestinationPath = @"D:\Joan\Mega\Illusion Stuff\HS\HS Plugins\NodesConstraints"
                    }, 
                    new ReleaseFile()
                    {
                        Name = "NodesConstraints",
                        Path = @"D:\Program Files (x86)\Koikatu\BepInEx\plugins\NodesConstraints.dll",
                        DestinationPath = @"D:\Joan\Mega\Illusion Stuff\KK\KK Plugins\NodesConstraints"
                    }, 
                    new ReleaseFile()
                    {
                        Name = "NodesConstraints",
                        Path = @"D:\Program Files (x86)\AI-Syoujyo\BepInEx\plugins\NodesConstraints.dll",
                        DestinationPath = @"D:\Joan\Mega\Illusion Stuff\AI\AI Plugins\NodesConstraints"
                    }, 
                    new ReleaseFile()
                    {
                        Name = "NodesConstraints",
                        Path = @"D:\Program Files (x86)\HoneySelect2\BepInEx\plugins\NodesConstraints.dll",
                        DestinationPath = @"D:\Joan\Mega\Illusion Stuff\HS2\HS2 Plugins\NodesConstraints"
                    }, 
                }
            },
            new ReleaseProfile()
            {
                Name = "MoreAccessoriesKOI/EC",
                Files = new []
                {
                    new ReleaseFile()
                    {
                        Name = "MoreAccessories",
                        Path = @"D:\Program Files (x86)\Koikatu\BepInEx\plugins\MoreAccessories.dll",
                        DestinationPath = @"D:\Joan\Mega\Illusion Stuff\KK\KK Plugins\MoreAccessories"
                    }, 
                    new ReleaseFile()
                    {
                        Name = "MoreAccessories",
                        Path = @"D:\Program Files (x86)\EmotionCreators\BepInEx\plugins\MoreAccessories.dll",
                        DestinationPath = @"D:\Joan\Mega\Illusion Stuff\EC\EC Plugins\MoreAccessories"
                    }, 
                }
            }, 
            new ReleaseProfile()
            {
                Name = "MoreAccessoriesAI/HS2",
                Files = new []
                {
                    new ReleaseFile()
                    {
                        Name = "MoreAccessories",
                        Path = @"D:\Program Files (x86)\AI-Syoujyo\BepInEx\plugins\MoreAccessories.dll",
                        DestinationPath = @"D:\Joan\Mega\Illusion Stuff\AI\AI Plugins\MoreAccessories"
                    }, 
                    new ReleaseFile()
                    {
                        Name = "MoreAccessories",
                        Path = @"D:\Program Files (x86)\HoneySelect2\BepInEx\plugins\MoreAccessories.dll",
                        DestinationPath = @"D:\Joan\Mega\Illusion Stuff\HS2\HS2 Plugins\MoreAccessories"
                    }, 
                }
            }, 
        };
    }
}
