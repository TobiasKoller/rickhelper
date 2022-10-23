using Microsoft.WindowsAPICodePack.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace rickhelper
{

    public class Validator : FixerBase
    {
        public Validator(Configuration configuration) : base(configuration)
        {

        }
        public override void Run()
        {
            string mountedDrive = null;
            while (true)
            {
                mountedDrive = Cmd.Ask("Mounted img-file drive-letter (like g:): ");
                if (mountedDrive.Length > 1) mountedDrive = mountedDrive[0] + ":\\";
                else mountedDrive = mountedDrive + ":\\";

                if (Directory.Exists(mountedDrive)) break;
                Cmd.WriteError($"Directory [{mountedDrive}] not found.");
            }

            var homeDir = Path.Combine(mountedDrive, "home", "pi", "RetroPie", "roms");

            var dirInfo = new DirectoryInfo(homeDir);
            var directories = dirInfo.GetDirectories().Where(x => x.Attributes == FileAttributes.Directory && x.Attributes != FileAttributes.Hidden && x.Attributes != FileAttributes.System).ToList();

            var gamelistXmlFiles = GetGameListXmlFiles(false, directories);

            bool? checkFiles = null;
            bool? checkPixelSizes = null;
            bool? checkFilesWithoutRef = null;

            for (var i = 0; i < gamelistXmlFiles.Count; i++)
            {
                var gamelistXml = gamelistXmlFiles[i];
                Cmd.Write(gamelistXml);

                var gameList = CreateGameList(gamelistXml, false);

                if (checkFiles == null) checkFiles = IsAnswerPositive(Cmd.Ask("Check filenames? [y/n]"));
                if (checkPixelSizes == null) checkPixelSizes = IsAnswerPositive(Cmd.Ask("Check video-pixel-sizes? [y/n]"));
                if (checkFilesWithoutRef == null) checkFilesWithoutRef = IsAnswerPositive(Cmd.Ask("Check files without references? [y/n]"));

                if (checkFiles==true)
                {
                    Cmd.Write("Checking Rom/Image/Video-filenames...", ConsoleColor.Green);
                    if (gameList.Provider == null)
                    {
                        Cmd.WriteError($"gamelist [{gamelistXml}] has no valid provider-tag.");
                    }

                    ValidateGameTitle(gameList, Path.GetDirectoryName(gamelistXml));
                }
               

                if (checkPixelSizes==true)
                {
                    Cmd.Write("Checking Rom/Image/Video-pixel-sizes...", ConsoleColor.Green);
                    ValidateVideoResolution(gameList, Path.GetDirectoryName(gamelistXml));
                }

                if (checkFilesWithoutRef==true)
                {
                    Cmd.Write("Checking files with no references...", ConsoleColor.Green);
                    checkFilesWithoutReferences(gameList, Path.GetDirectoryName(gamelistXml));
                }
                
            }

            Cmd.Ask("Validation done.");
            
        }

        private void ValidateGameTitle(GameList gameList, string romDirectory)
        {

            foreach(var game in gameList.Games)
            {
                var rom = Path.GetFileNameWithoutExtension(game.Path);
                var video = Path.GetFileNameWithoutExtension(game.Video);
                var image = Path.GetFileNameWithoutExtension(game.Image);

                var errors = false;
                if(rom!=video || rom != image)
                {
                    errors = true;
                    Cmd.WriteError("Path=" + Path.GetFileName(game.Path));
                    Cmd.WriteError("Image=" + Path.GetFileName(game.Image));
                    Cmd.WriteError("Video=" + Path.GetFileName(game.Video));

                }

                var gamePath = Path.Combine(romDirectory, game.Path??"");
                var imagePath = Path.Combine(romDirectory, game.Image??"");
                var videoPath = Path.Combine(romDirectory, game.Video??"");

                if (!File.Exists(gamePath))
                {
                    errors = true;
                    Cmd.WriteError($"File [{gamePath}] not found.");
                }
                if (!File.Exists(imagePath))
                {
                    errors = true;
                    Cmd.WriteError($"File [{imagePath}] not found.");
                }
                if (!File.Exists(videoPath))
                {
                    errors = true;
                    Cmd.WriteError($"File [{videoPath}] not found.");
                }
                if (errors) Cmd.WriteError("------------------------");     
            }
        }

        private void ValidateVideoResolution(GameList gameList, string romDirectory)
        {
            foreach (var game in gameList.Games)
            {
                try
                {
                    var video = game.Video;
                    if (string.IsNullOrWhiteSpace(video))
                    {
                        Cmd.WriteError($"No Video defined for {game.Path}");
                        continue;
                    }


                    var videoPath = Path.GetFullPath(Path.Combine(romDirectory, game.Video ?? ""));

                    if (!File.Exists(videoPath))
                    {
                        Cmd.WriteError($"File [{videoPath}] not found.");
                        continue;
                    }

                    var shellFile = ShellFile.FromFilePath(videoPath);
                    var width = shellFile.Properties.DefaultPropertyCollection.FirstOrDefault(p => p.CanonicalName == "System.Video.FrameWidth")?.ValueAsObject as uint?;
                    var height = shellFile.Properties.DefaultPropertyCollection.FirstOrDefault(p => p.CanonicalName == "System.Video.FrameHeight")?.ValueAsObject as uint?;

                    if (width != 640 || height != 480)
                    {
                        Cmd.WriteError($"wrong video-pixel-size [{width}x{height}] for file [{videoPath}].");
                        continue;
                    }
                }
                catch (Exception) { }
            }
        }

        private void checkFilesWithoutReferences(GameList gameList, string romDirectory)
        {
            var paths = gameList.Games.Select(g => Path.GetFullPath(Path.Combine(romDirectory, g.Path ?? "")).ToLower());
            var videos = gameList.Games.Select(g => Path.GetFullPath(Path.Combine(romDirectory, g.Video ?? "")).ToLower());
            var images = gameList.Games.Select(g => Path.GetFullPath(Path.Combine(romDirectory, g.Image ?? "")).ToLower());

            var allFiles = new List<string>();
            allFiles.AddRange(paths);
            allFiles.AddRange(videos);
            allFiles.AddRange(images);

            var directories = new List<string>();
            foreach(var file in allFiles)
            {
                var dir = Path.GetDirectoryName(file);
                if (directories.Contains(dir)) continue;
                directories.Add(dir);
            }

            foreach(var dir in directories)
            {
                var files = Directory.GetFiles(dir);
                foreach(var file in files)
                {
                    if (Path.GetFileName(file) == "gamelist.xml") continue;
                    if (allFiles.Contains(file.ToLower())) continue;
                    Cmd.WriteError("File without reference: "+file);
                }
            }
        }
    }
}
