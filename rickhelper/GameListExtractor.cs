using NanoXLSX;
using NanoXLSX.Styles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace rickhelper
{
    public class GameListExtractor : FixerBase
    {
        public GameListExtractor(Configuration configuration) : base(configuration)
        {
        }

        public override void Run()
        {
            var xlsxInFile = Config.GameListExtractor.XlsxInFile;
            var xlsxOutFile = Config.GameListExtractor.XlsxOutFile;
            var diff = true;

            if (!File.Exists(xlsxInFile))
            {
                Cmd.WriteError($"File [{xlsxInFile}] was not found. Will only create Default-Gamelist");
                diff = false;
            }
            if (!xlsxOutFile.EndsWith("xlsx"))
            {
                Cmd.WriteError("config-entry GameListExtractor-->xlsx_out_file has to end with .xlsx");
                Console.ReadLine();
                return;
            }
            

            Cmd.Write($"Extracting games from gamelists to [{xlsxOutFile}]...");
            var files = GetGameListXmlFiles(true);

            var games = new Dictionary<string, List<Game>>();

            Cmd.Write("Reading game-information...");
            foreach (var gamelistFile in files)
            {
                var system = Directory.GetParent(gamelistFile).Name;
                games.Add(system, CreateGameList(gamelistFile).Games);
            }

            ExportXls(games, diff);
        }

        private bool IsNewGame(Workbook workbook, string system, string gameName)
        {
            var worksheet = workbook.Worksheets[0];

            var lastColumn = worksheet.GetLastColumnNumber();
            var columnNo = -1;
            for(var i = 0; i < lastColumn; i++)
            {
                var cell = worksheet.GetCell(i, 0);
                if(string.Compare(cell.Value?.ToString(), system, true)==0)
                {
                    columnNo = i;
                    break;
                }
            }
            if (columnNo == -1) return true; //whole new system

            var lastRow = worksheet.GetLastRowNumber();
            for(var i = 1; i < lastRow; i++)
            {
                if (!worksheet.HasCell(columnNo, i)) return true;

                var cell = worksheet.GetCell(columnNo, i);
                if (string.Compare(cell.Value?.ToString(), gameName, true) == 0) 
                    return false;
            }

            return true;
        }

        private void ExportXls(Dictionary<string, List<Game>> systems, bool diff)
        {
            
            var dir = Path.GetDirectoryName(Config.GameListExtractor.XlsxOutFile);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            Cmd.Write($"Exporting xlsx to {Config.GameListExtractor.XlsxOutFile}...");
            //var workbook = new Workbook(Config.GameListExtractor.XlsxOutFile, diff? "GameList (Inc Additions)" : "GameList");
            var workbook = diff? Workbook.Load(Config.GameListExtractor.XlsxInFile) : new Workbook(Config.GameListExtractor.XlsxOutFile, "GameList");
            var systemCounter = 0;
            if (diff) workbook.AddWorksheet("GameList (Inc Additions)");

            var newGamesTotal = 0;
            var gamesTotal = 0;
            foreach (var systemEntry in systems.OrderBy(s => s.Key))
            {
                var system = systemEntry.Key;
                var gameList = systemEntry.Value.OrderBy(v => v.Name);

                var gameCounter = 1;
                var newGames = 0;
                foreach(var game in gameList)
                {
                    var gameName = game.Name?.Trim();
                    workbook.CurrentWorksheet.AddCell(gameName, systemCounter, gameCounter);
                    if (IsNewGame(workbook, system, gameName))
                    {
                        workbook.CurrentWorksheet.GetCell(systemCounter, gameCounter).SetStyle(BasicStyles.ColorizedBackground(Config.GameListExtractor.DiffBackgroundColor ?? "F0A500"));
                        newGames++;
                        newGamesTotal++;
                    }
                    gameCounter++;
                    gamesTotal++;
                }

                workbook.CurrentWorksheet.AddCell($"{system?.Trim()} (total: {gameList.Count()}; new: {newGames})", systemCounter, 0, BasicStyles.Bold);

                var max = gameList.Max(g => g.Name.Length);
                workbook.CurrentWorksheet.SetColumnWidth(systemCounter, max);
                systemCounter++;
            }

            if (diff) workbook.SaveAs(Config.GameListExtractor.XlsxOutFile);
            else workbook.Save();

            Cmd.Write($"Exported {systems.Count()} systems; {gamesTotal} games; {newGamesTotal} new games.",ConsoleColor.Cyan);
        }
    }
}
