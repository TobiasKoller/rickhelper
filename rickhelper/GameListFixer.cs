using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace rickhelper
{
    public class GameListFixer : ITool
    {
        private Configuration _config;
        private string _sourceDirectory;
        private bool _verbose = false;
        public GameListFixer(Configuration configuration)
        {
            _config = configuration;
            _sourceDirectory = Path.GetDirectoryName(_config.GameListFixer.GamelistXmlDirectory);
        }

        public bool IsAnswerPositive(string res)
        {
            return AreEqual(res, "y") || string.IsNullOrWhiteSpace(res);
        }
        public void Run()
        {
            Cmd.Write("GameList Fixer");

            _config.GameListFixer.ToConsole();
            var res = Cmd.Ask("are these options correct (y/n)", ConsoleColor.Cyan);
            if (!IsAnswerPositive(res))
            {
                Cmd.Write("Aborted. Please modify config.json to your needs.", ConsoleColor.Red);
                return;
            }

            var gamelistXmlFiles = GetGameListXmlFiles();
            if (!gamelistXmlFiles.Any()) return;

            var allAtOnce = Cmd.Ask("Do you want to process all at once? (y/n)", ConsoleColor.Cyan);
            for(var i=0;i<gamelistXmlFiles.Count;i++)
            {
                var gamelistXml = gamelistXmlFiles[i];
                if (!IsAnswerPositive(allAtOnce))
                {
                    var next = Cmd.Ask($"Next file [{gamelistXml}]. Process now? (y/n)");
                    if (!IsAnswerPositive(next)) continue;
                }
                else
                {
                    var counter = i + 1;
                    Cmd.Write($"({counter}/{gamelistXmlFiles.Count}) {gamelistXml}");
                }
                Start(gamelistXml);
            }
        }

        private List<string> GetGameListXmlFiles()
        {
            var gamelistXmlDir = _config.GameListFixer.GamelistXmlDirectory;
            if (!Directory.Exists(gamelistXmlDir))
            {
                Cmd.WriteError($"[{gamelistXmlDir}] is no valid directory for gamelist_dir");
                return new List<string>();
            }

            var files = Directory.GetFiles(gamelistXmlDir, "gamelist.xml", SearchOption.AllDirectories).ToList();

            Cmd.Write($"found [{files.Count}] gamelist.xml-files.");

            foreach(var file in files)
                Cmd.Write($"{file}", ConsoleColor.Green);

            return files;
        }
        private void Start(string gamelistFile)
        {
            Cmd.Spacer();
            if (!File.Exists(gamelistFile))
            {
                Cmd.WriteError($"Gamelist-file [{gamelistFile}] not found.");
                return;
            }

            var xRoot = new XmlRootAttribute
            {
                ElementName = "gameList",
                IsNullable = true
            };
            
            
            var gamelistNodes = ReadGameList(gamelistFile);
            var gamelistSerializer = new XmlSerializer(typeof(GameList), new XmlRootAttribute { ElementName = "gameList", IsNullable = true });
            var gameSerializer = new XmlSerializer(typeof(GameList), new XmlRootAttribute { ElementName = "game", IsNullable = true });
            
            
            //StringReader rdr = new StringReader(gameNodes[0].OuterXml);
            var gameList = (GameList)gamelistSerializer.Deserialize(new StringReader(gamelistNodes.OuterXml));

           // Cmd.NextTopic("Deleting *.cfg-file if it exists", ConsoleColor.Green);
            //if (_config.GameListFixer.DeleteCfgFile) DeleteCfgFile();


           // foreach (XmlNode gameNode in gameNodes)
            foreach(var game in gameList.Games)
            {
                if(_verbose) Cmd.Spacer();
                if(_verbose) Cmd.NextTopic($"Next Game: {game.Name}", ConsoleColor.Green);
                if(_verbose) Cmd.NextTopic("fixing description format");
                FixDescription(game);

                if(_verbose) Cmd.NextTopic("Fixing genre");
                FixGenre(game);

                if(_verbose) Cmd.NextTopic("Fixing image");
                FixImageExtension(game);

                if(_verbose) Cmd.NextTopic("Adding video-tag");
                AddingVideoTag(game);
            }

            if(_verbose) Cmd.NextTopic("Creating output-file");
            OutputResult(gameList);
        }

       private XmlNode  ReadGameList(string gamelistFile)
        {
            var doc = new XmlDocument();
            doc.Load(gamelistFile);

            return doc.SelectSingleNode("//gameList");
        }


        private void AddingVideoTag(Game game)
        {
            if (string.IsNullOrWhiteSpace(game.Video))
            {
                var image = Path.GetFileNameWithoutExtension(game.Image);
                game.Video = $"./snaps/{image}.mp4";
            }
        }
        private void OutputResult(GameList gameList)
        {

            var gameListFile = Path.Combine(_config.GameListFixer.GamelistXmlOutDirectory, "gamelist.xml");
            if(_verbose) Cmd.Write($"Creating file [{gameListFile}]");

            var emptyNamespaces = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
            //var serializer = new XmlSerializer(typeof(GameList));
            var settings = new XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = true,
                IndentChars = "\t"
            };

           
            using (var stream = new StringWriter())
            using (var writer = XmlWriter.Create(stream, settings))
            {
                var serializer = new XmlSerializer(typeof(GameList));
                serializer.Serialize(writer, gameList, emptyNamespaces);

                var result = $"<?xml version=\"1.0\"?>{Environment.NewLine}{stream}";
                File.WriteAllText(gameListFile, result);
            }
         

        }

        private int GetLastIndex(string str, params string[] values)
        {
            var max = -1;
            foreach(var value in values)
            {
                var index = str.LastIndexOf(value);
                if (index > max) max=index;
            }
            return max;
        }
        private void FixDescription(Game game)
        {
            var desc = game.Description ?? "";
            desc = Regex.Replace(desc, @"\s+", " ");

            var maxLen = _config.GameListFixer.MaxDescriptionLength > 0 ? _config.GameListFixer.MaxDescriptionLength : 850;
            if(desc.Length > maxLen)
            {
                var trimmed = desc.Substring(0, maxLen);

                var lastDotIndex = GetLastIndex(trimmed, ".", ";", "!", "?");
                if (lastDotIndex != -1) desc = desc.Substring(0, lastDotIndex+1);
            }
            game.Description = desc.Trim();
        }

        private void FixGenre(Game game)
        {
            var genres = _config.Genres;
            if(genres.Any(g => string.Equals(g.ReplaceWith, game.Genre)))
            {
                if(_verbose) Cmd.Write($"Genre [{game.Genre}] is already valid.", ConsoleColor.Cyan);
                return;
            }

            
            var replaceGenre = genres.FirstOrDefault(g => g.Originals.Any(o => 
                                                        !string.IsNullOrWhiteSpace(o) && 
                                                        string.Equals(o, game.Genre, StringComparison.OrdinalIgnoreCase)
                                                        )
                                                    );
            if (replaceGenre != null)
            {
                if (_verbose) Cmd.Write($"Found genre [{replaceGenre.ReplaceWith}]. Will use it.", ConsoleColor.Cyan);
                game.Genre = replaceGenre.ReplaceWith.Trim();
            }
            else
            {
                Cmd.NextTopic($"Game: {game.Name}", ConsoleColor.Green);
                Cmd.Write($"Current Genre: {game.Genre}", ConsoleColor.Yellow);

                var dic = new Dictionary<int, string>();
                for (var i = 0; i < genres.Count; i++) dic.Add(i + 1, genres[i].ReplaceWith);
                var options = dic.Select(d => new Option { Number = d.Key, Text = d.Value }).ToList();
                
                Cmd.WriteOptions(options);
                Cmd.Write($"[x] ignore");

                while (true)
                {
                    Cmd.Spacer();
                    var choice = Cmd.Ask("your choice: ", ConsoleColor.Cyan);
                    if (choice == "x") break;

                    if (!int.TryParse(choice, out int res) || !dic.ContainsKey(res))
                    {
                        Cmd.WriteError($"Invalid option [{choice}].");
                        continue;
                    }

                    var newGenre = dic[res].Trim();
                    if (!string.IsNullOrWhiteSpace(game.Genre))
                    {
                        var genre = _config.Genres.First(g => AreEqual(g.ReplaceWith, newGenre));
                        if (!genre.Originals.Any(g => AreEqual(g, game.Genre))) genre.Originals.Add(game.Genre);
                    }
                    game.Genre = newGenre;
                    break;
                }
            }

            if(_verbose) Cmd.Write($"Updated Genre: [{game.Genre}]", ConsoleColor.Cyan);
        }

        private bool AreEqual(string string1, string string2)
        {
            return string.Equals(string1, string2, StringComparison.OrdinalIgnoreCase);
        }

        private void FixImageExtension(Game game)
        {
            var newExtension = _config.GameListFixer.ImageExtension.Trim();
            if (!newExtension.StartsWith(".")) newExtension = $".{newExtension}";
            if (string.IsNullOrWhiteSpace(game.Image)) return;

            game.Image = game.Image.Replace(".png", newExtension);
        }

        //private void DeleteCfgFile()
        //{
        //    var file = Directory.GetFiles(_sourceDirectory, "*.cfg").FirstOrDefault();
        //    if (File.Exists(file))
        //    {
        //        if(_verbose) Cmd.Write($"Deleting [{file}]..");
        //        File.Delete(file);
        //        if(_verbose) Cmd.Write($"file deleted.");
        //    }         
        //}
    }
}
