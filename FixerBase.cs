using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace rickhelper
{
    public abstract class FixerBase : ITool
    {
        protected Configuration Config;
        protected FixerBase(Configuration configuration)
        {
            Config = configuration;
        }
        public abstract void Run();

        protected GameList CreateGameList(string gamelistFile, bool removeDuplicates)
        {
            var gamelistNodes = ReadGameList(gamelistFile);
            var gamelistSerializer = new XmlSerializer(typeof(GameList), new XmlRootAttribute { ElementName = "gameList", IsNullable = true });
            var gameSerializer = new XmlSerializer(typeof(GameList), new XmlRootAttribute { ElementName = "game", IsNullable = true });


            //StringReader rdr = new StringReader(gameNodes[0].OuterXml);
            var gamelist = (GameList)gamelistSerializer.Deserialize(new StringReader(gamelistNodes.OuterXml));

            return removeDuplicates ? RemoveDuplicates(gamelist) : gamelist;
        }

        protected bool IsAnswerPositive(string res)
        {
            return AreEqual(res, "y") || string.IsNullOrWhiteSpace(res);
        }

        protected bool AreEqual(string string1, string string2)
        {
            return string.Equals(string1, string2, StringComparison.OrdinalIgnoreCase);
        }
        private GameList RemoveDuplicates(GameList gamelist)
        {
            if (gamelist.Provider == null) return gamelist;

            var processedFiles = new List<string>();

            var games = new List<Game>();
            foreach(var game in gamelist.Games)
            {
                if (processedFiles.Contains(game.Path?.ToLower()))
                {
                    Cmd.WriteError($"duplicate removed: system={gamelist.Provider.System}; game={game.Name}; {game.Path}");
                    continue;
                }
                processedFiles.Add(game.Path.ToLower());
                games.Add(game);
            }

            gamelist.Games = games;
            return gamelist;
        }
        protected List<string> GetGameListXmlFiles(bool outputResult, List<DirectoryInfo> directories=null)
        {
            var gamelistXmlDir = "";
            var files = new List<string>();

            if (directories == null)
            {
                gamelistXmlDir = Config.GameListFixer.GamelistXmlDirectory;
                if (!Directory.Exists(gamelistXmlDir))
                {
                    Cmd.WriteError($"[{gamelistXmlDir}] is no valid directory for gamelist_dir");
                    return new List<string>();
                }

                files = Directory.GetFiles(gamelistXmlDir, "gamelist.xml", SearchOption.AllDirectories).ToList();
            }
            else
            {
                foreach(var dir in directories)
                {
                    try
                    {
                        var gamelistFiles = Directory.GetFiles(dir.FullName, "gamelist.xml");
                        foreach (var file in gamelistFiles)
                        {
                            if (files.Contains(file)) continue;
                            files.Add(file);
                        }
                    }
                    catch(Exception) { }
                    
                }
            }
            Cmd.Write($"found [{files.Count}] gamelist.xml-files.");

            if (outputResult)
            {
                foreach (var file in files)
                    Cmd.Write($"{file}", ConsoleColor.Green);
            }


            return files;
        }

        private XmlNode ReadGameList(string gamelistFile)
        {
            try
            {
                var doc = new XmlDocument();
                doc.Load(gamelistFile);

                return doc.SelectSingleNode("//gameList");
            }
            catch(Exception ex)
            {
                Cmd.WriteError("File: " + gamelistFile);
                throw;
            }
        }

    }
}
