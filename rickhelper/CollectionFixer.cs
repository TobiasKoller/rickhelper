using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace rickhelper
{
    public class CollectionFixer : FixerBase
    {
        public CollectionFixer(Configuration configuration): base(configuration)
        {
        }
        public override void Run()
        {
            var outDir = Config.CollectionFixer.CollectionOutDirectory;

            if (!Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }

            var files = GetGameListXmlFiles(false);

            var games = new Dictionary<string, List<Game>>();

            Cmd.Write("Reading game-information...");
            foreach(var gamelistFile in files)
            {
                var system = Directory.GetParent(gamelistFile).Name;
                
                games.Add(system, CreateGameList(gamelistFile, false).Games);
            }

            
            Cmd.Write("creating cfg-files...");

            var cfgFileLists = new Dictionary<string, List<string>>();

            foreach (var genre in Config.Genres)
            {
                //var genreGameList = new List<string>();
                if (!cfgFileLists.ContainsKey(genre.CfgFileName)) cfgFileLists.Add(genre.CfgFileName, new List<string>());

                
                foreach (var entry in games)
                {
                    var system = entry.Key;
                    var gameList = entry.Value;

                    var genreGames = gameList.Where(g => g.Genre == genre.ReplaceWith).ToList();


                    var paths = genreGames.Select(g => $"/home/pi/RetroPie/roms/{system}/{Path.GetFileName(g.Path)}").ToList();
                    cfgFileLists[genre.CfgFileName].AddRange(paths);
                }

            }

            foreach(var cfg in cfgFileLists)
            {
                var cfgFile = Path.Combine(outDir, cfg.Key);
                cfg.Value.Sort();

                if (Config.CollectionFixer.Verbose) Cmd.Write($"Creating file {cfgFile}");
                File.WriteAllText(cfgFile, string.Join("\n", cfg.Value));
            }
        }

    }
}
