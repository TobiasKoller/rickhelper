using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace rickhelper
{

    public class Helper
    {
        private Dictionary<int, ITool> _helper = new Dictionary<int, ITool>();
        private string _configFile = Path.Combine(Directory.GetCurrentDirectory(),"config.json");

        private TaskType PrintChoice()
        {
            var options = new Dictionary<int, TaskType>
                {
                    {1, TaskType.GameListFix },
                    {2, TaskType.GameListExtract },
                    {3, TaskType.Validate },
                    {4, TaskType.GenerateImageHashes },
                    {5, TaskType.CreateUpdate },
                    {6, TaskType.CheckGamesExist }

                };
            int answer;
            while (true)
            {
                Cmd.Write("What do you want to do:", ConsoleColor.Green);
                Cmd.Write("[1]  Gamelist fixer", ConsoleColor.Yellow);
                Cmd.Write("     Fixes formatting and some missing tags in the xml-file", ConsoleColor.Gray);
                Cmd.Write("[2]  Extract gamelist.xml", ConsoleColor.Yellow);
                Cmd.Write("     Exports all games and system to an xlsx-file.", ConsoleColor.Gray);
                Cmd.Write("[3]  validate", ConsoleColor.Yellow);
                Cmd.Write("     checks if the filename matches, if video is 640x480, Files without references", ConsoleColor.Gray);
                Cmd.Write("[4]  Generate Image Hashes", ConsoleColor.Yellow);
                Cmd.Write("     Analyses all files on the image/pi an creates hashes for each.", ConsoleColor.Gray);
                Cmd.Write("     These Hashes are needed to create the Update-Package", ConsoleColor.Gray);
                Cmd.Write("[5]  Create Update Package", ConsoleColor.Yellow);
                Cmd.Write("     Compares two Hash-Result-Files and creates an Update-Package", ConsoleColor.Gray);
                Cmd.Write("[6]  Check existing Games", ConsoleColor.Yellow);
                Cmd.Write("     Checks all games in the given list if they exist in the current gamelists.", ConsoleColor.Gray);
                
                if (!int.TryParse(Cmd.Ask("your choice: "), out answer) || !options.ContainsKey(answer))
                {
                    Cmd.WriteError("Invalid answer.");
                    continue;
                }
                break;
            }

            return options[answer];

        }
        public void Run(string[] arguments)
        {
            var config = GetConfiguration();

            var type = PrintChoice();

            var tools = new List<ITool>();

            switch (type)
            {
                case TaskType.GameListExtract: tools.Add(new GameListExtractor(config));break;
                case TaskType.GameListFix:
                    tools.Add(new GameListFixer(config));
                    tools.Add(new CollectionFixer(config));
                    break;
                case TaskType.Validate:
                    tools.Add(new Validator(config));
                    break;
                case TaskType.GenerateImageHashes:
                    tools.Add(new ImageHashGenerator(config));
                    break;
                case TaskType.CreateUpdate:
                    tools.Add(new UpdateCreator(config));
                    break;
                case TaskType.CheckGamesExist:
                    tools.Add(new CheckGamesExist(config));
                    break;
            }

            foreach(var tool in tools) tool.Run();


            UpdateConfiguration(config);
        }

        private Configuration GetConfiguration()
        {
            var json = File.ReadAllText(_configFile);
            return JsonConvert.DeserializeObject<Configuration>(json);
        }

        private void UpdateConfiguration(Configuration config)
        {
            if (!File.Exists(_configFile))
            {
                Cmd.Write($"File [{_configFile}] not found. Can't update it.");
                return;
            }
            
            File.WriteAllText(_configFile, JsonConvert.SerializeObject(config, Formatting.Indented));
        }
    }
}
