using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace rickhelper
{

    public class ExistingGameComparision : CompareResult
    {
        public string CheckGame { get; set; }
    }

    public class CheckGamesExist : FixerBase
    {
        public CheckGamesExist(Configuration configuration) : base(configuration)
        {
        }

        public override void Run()
        {
            var file = GetGameList();

            var gamesToCheck = ReadGamesFromFile(file);
            var gameList = GetSystemGamelist();

            var results = CompareGames(gameList, gamesToCheck);

            CreateOutputFile(file, results);

        }

        private void CreateOutputFile(string file, List<ExistingGameComparision> results)
        {
            var header = "checkedGame;closest Game found;CompareRate in %";
            var lines = new List<string> { header };

            lines.AddRange(results.OrderByDescending(o => o.Rate).Select(r => $"{r.CheckGame};{r.Game};{r.Rate}"));

            var fileWithoutExt = Path.GetFileNameWithoutExtension(file);
            var path = Path.GetDirectoryName(file);
            var outFile = $"{Path.Combine(path, fileWithoutExt)}_out.csv";

            Cmd.Write($"Saving result in file [{outFile}]");
            File.WriteAllLines(outFile, lines);
        }

        private List<ExistingGameComparision> CompareGames(GameList gameList, List<string> gamesToCheck)
        {
            var results = new List<ExistingGameComparision>();

            foreach(var game in gamesToCheck)
            {
                var result = GameComparer.Compare(gameList, game);
                if (result.Rate == 100) continue;

                results.Add(new ExistingGameComparision {CheckGame=game, Game=result.Game, Rate=result.Rate });
            }

            return results;
        }

        

        private GameList GetSystemGamelist()
        {
            Cmd.Write("Listing all systems...");
            var gamelistXmlFiles = GetGameListXmlFiles(false);
            var gameLists = gamelistXmlFiles.Select(g => CreateGameList(g, false)).ToList();

            var systems = new Dictionary<string, GameList>();
            var counter = 1;

            foreach(var gameList in gameLists.Where(g => !string.IsNullOrEmpty(g.Provider?.System)))
            {
                systems.Add(counter.ToString(), gameList);
                Cmd.Write($"[{counter}] {gameList.Provider.System}");
                counter++;
            }

            while (true)
            {
                var selectedSystem = Cmd.Ask("Your choice: ").Trim();
                if (systems.ContainsKey(selectedSystem)) return systems[selectedSystem];

                Cmd.WriteError("Invalid input.");
            }
        }

        private List<string> ReadGamesFromFile(string file)
        {
            var games = File.ReadAllLines(file);
            return games.ToList();
        }

        private string GetGameList()
        {
            while (true)
            {
                var file = Cmd.Ask("Enter file with games to check: ").Replace("\"","");
                if (File.Exists(file)) return file;

                Cmd.WriteError($"File [{file}] not found.");
            }
        }
    }
}
