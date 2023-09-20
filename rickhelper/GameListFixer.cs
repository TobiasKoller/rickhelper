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
using static System.Windows.Forms.Design.AxImporter;

namespace rickhelper
{

    public class GameListFixer : FixerBase
    {
        private string _sourceDirectory;
        private bool _verbose = false;
        public GameListFixer(Configuration configuration) : base(configuration)
        {
            _sourceDirectory = Path.GetDirectoryName(Config.GameListFixer.GamelistXmlDirectory);
        }

        //private string GetOrigin()
        //{
        //    string origin = "";
        //    var color = ConsoleColor.Yellow;

        //    var options = new Dictionary<int, string>
        //    {
        //        {1,"Insanium" },
        //        {2, "R.P.E." }
        //    };

        //    int answer;
        //    while(true)
        //    {
        //        Cmd.Spacer();
        //        foreach(var option in options) Cmd.Write($"[{option.Key}] {option.Value}", color);
                

        //        if (!int.TryParse(Cmd.Ask("your choice: "), out answer) || !options.ContainsKey(answer))
        //        {
        //            Cmd.WriteError("Invalid answer.");
        //            continue;
        //        }
        //        return options[answer];
        //    }
        //}
        
        public override void Run()
        {
            Cmd.Write("GameList Fixer");
            //var origin = GetOrigin();
            
            Config.GameListFixer.ToConsole();

            var res = Cmd.Ask("are these options correct (y/n)", ConsoleColor.Cyan);
            if (!IsAnswerPositive(res))
            {
                Cmd.Write("Aborted. Please modify config.json to your needs.", ConsoleColor.Red);
                return;
            }

            var checkFileExists = Cmd.Ask("check if files exists on \\\\retropie\\roms? (could be very slow) (y/n)").ToUpper()=="Y";


            var gamelistXmlFiles = GetGameListXmlFiles(true);
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
                Start(gamelistXml, checkFileExists);
            }
        }


        private void Start(string gamelistFile, bool checkFileExists)
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

            var gameList = CreateGameList(gamelistFile, true);

            Cmd.Write("Next system: " + gameList?.Provider?.System, ConsoleColor.Yellow);
           // Cmd.NextTopic("Deleting *.cfg-file if it exists", ConsoleColor.Green);
            //if (Config.GameListFixer.DeleteCfgFile) DeleteCfgFile();
            if(!Directory.Exists("\\\\retropie"))
            {
                Cmd.WriteError("couldnt open directory \\\\retropie");
                checkFileExists = false;
            }

           // foreach (XmlNode gameNode in gameNodes)
            foreach(var game in gameList.Games)
            {
                if(_verbose) Cmd.Spacer();
                if(_verbose) Cmd.NextTopic($"Next Game: {game.Name}", ConsoleColor.Green);
                if(_verbose) Cmd.NextTopic("fixing description format");
                FixDescription(game);

                //game.Origin = origin;

                if(_verbose) Cmd.NextTopic("Fixing genre");
                FixGenre(game);

                if (_verbose) Cmd.NextTopic("Fixing players");
                FixPlayers(game);

                if (_verbose) Cmd.NextTopic("Fixing name");
                FixName(game);

                if (_verbose) Cmd.NextTopic("Fixing image");
                FixImageExtension(game);

                if(_verbose) Cmd.NextTopic("Adding video-tag");
                AddingVideoTag(game);

                if(checkFileExists)
                {
                    var romName = game.Path.Replace("./", "");
                    var absolutePath = @$"\\retropie\roms\{gameList.Provider.System}\{romName}";
                    if (_verbose) Cmd.NextTopic("Checking if file exists on pi");
                    if (!File.Exists(absolutePath)) Cmd.WriteError($"File [{absolutePath}] not found.");
                }
                
            }

            if(_verbose) Cmd.NextTopic("Creating output-file");
            OutputResult(gameList, gamelistFile);
        }


        private void AddingVideoTag(Game game)
        {
            if (string.IsNullOrWhiteSpace(game.Video))
            {
                var image = Path.GetFileNameWithoutExtension(game.Image);
                game.Video = $"./snaps/{image}.mp4";
            }
        }
        private void OutputResult(GameList gameList, string sourceGameListFile)
        {
            var sourceDirLength = Config.GameListFixer.GamelistXmlDirectory.Length;
            var subDir = Path.GetDirectoryName(sourceGameListFile.Substring(sourceDirLength+1));

            var outDir = Path.Combine(Config.GameListFixer.GamelistXmlOutDirectory, subDir);

            if (!Directory.Exists(outDir))
                Directory.CreateDirectory(outDir);

            var gameListFile = Path.Combine(outDir, "gamelist.xml");
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
        
        private void FixName(Game game)
        {
            var name = game.Name?.Trim() ?? "";
            name = new Regex(@"\s+").Replace(name, " "); //remove multi spaces
            name = new Regex(@"\s+-\s*").Replace(name,": ",1); //replace first - with : and remove leading and trailing spaces
            name = name.Replace(" :", ":");
            game.Name = name;
        }

        private void FixDescription(Game game)
        {
            var desc = game.Description ?? "";
            desc = Regex.Replace(desc, @"\s+", " ");

            var maxLen = Config.GameListFixer.MaxDescriptionLength > 0 ? Config.GameListFixer.MaxDescriptionLength : 850;
            if(desc.Length > maxLen)
            {
                var trimmed = desc.Substring(0, maxLen);

                var lastDotIndex = GetLastIndex(trimmed, ".", ";", "!", "?");
                if (lastDotIndex != -1) desc = desc.Substring(0, lastDotIndex+1);
            }
            game.Description = desc.Trim();
        }

        private void FixPlayers(Game game)
        {

            if (string.IsNullOrWhiteSpace(game.Players))
            {
                Cmd.NextTopic($"Game: {game.Name}", ConsoleColor.Green);
                while (true)
                {
                    var players = Cmd.Ask("Number of players: ");
                    var regex = new Regex(@"^\d(-\d)?$");
                    if (regex.IsMatch(players))
                    {
                        game.Players = players;
                        break;
                    }

                    Cmd.WriteError("Invalid input. Type a single number or something like [1-4]");
                }
            }
            if(game.Players?.Trim().Length==1)
                game.Players = string.Format("{0}-{0}", game.Players);
        }

        private void FixGenre(Game game)
        {
            if (Config.GameListFixer.IgnoreGames.Any(g => string.Equals(g, game.Name)))
            {
                Cmd.Write($"Ignoring game [{game.Name}]..", ConsoleColor.Yellow);
                return;
            }
            var genres = Config.Genres;
            
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
                        var genre = Config.Genres.First(g => AreEqual(g.ReplaceWith, newGenre));
                        if (!genre.Originals.Any(g => AreEqual(g, game.Genre))) genre.Originals.Add(game.Genre);
                    }
                    game.Genre = newGenre;
                    break;
                }
            }

            if(_verbose) Cmd.Write($"Updated Genre: [{game.Genre}]", ConsoleColor.Cyan);
        }

        

        private void FixImageExtension(Game game)
        {
            var newExtension = Config.GameListFixer.ImageExtension.Trim();
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
