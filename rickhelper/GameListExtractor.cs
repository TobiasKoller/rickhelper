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

            var onlyExtractCurrentGames = Cmd.Ask("Only extract current games (no comparison with ultimate-orig-image)? [y/n]").ToUpper()=="Y";
            if (onlyExtractCurrentGames) diff = false;

            if (!onlyExtractCurrentGames && !File.Exists(xlsxInFile))
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
                games.Add(system, CreateGameList(gamelistFile, true).Games);
            }

            ExportXls(games, diff);
        }

        private bool IsNewGame(Workbook workbook, string system, string gameName)
        {
            var worksheet = workbook.Worksheets[0];

            var lastColumn = worksheet.GetLastColumnNumber();
            var columnNo = -1;
            for(var i = 0; i <= lastColumn; i++)
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
            for(var i = 1; i <= lastRow; i++)
            {
                if (!worksheet.HasCell(columnNo, i)) 
                    return true;

                var cell = worksheet.GetCell(columnNo, i);
                if (string.Compare(cell.Value?.ToString(), gameName, true) == 0) 
                    return false;
            }

            return true;
        }

        private void FormatOriginalWorksheet(Workbook workbook)
        {
            var worksheet = workbook.Worksheets[0];

            var lastColumn = worksheet.GetLastColumnNumber();
            if (lastColumn == 0) return;

            for (var i = 0; i <= lastColumn; i++)
            {
                var headerCell = worksheet.GetCell(i, 0);
                headerCell.SetStyle(BasicStyles.Bold);

                var max = 0;
                var lastRow = worksheet.GetLastRowNumber();
                for (var r = 0; r <= lastRow; r++)
                {
                    if (!worksheet.HasCell(i, r))
                        continue;

                    var cell = worksheet.GetCell(i, r);
                    var len = cell.Value?.ToString().Length ?? 0;
                    if (len > max) max = len;
                }
                worksheet.SetColumnWidth(i, max);
            }


        }

        private void ExportXls(Dictionary<string, List<Game>> systems, bool diff)
        {
            var dir = Path.GetDirectoryName(Config.GameListExtractor.XlsxOutFile);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            Cmd.Write($"Exporting xlsx to {Config.GameListExtractor.XlsxOutFile}...");
            //var workbook = new Workbook(Config.GameListExtractor.XlsxOutFile, diff? "GameList (Inc Additions)" : "GameList");
            var workbook = diff? Workbook.Load(Config.GameListExtractor.XlsxInFile) : new Workbook(Config.GameListExtractor.XlsxOutFile, "GameList");

            var sheets = new List<int>();
            if (diff)
            {
                workbook.AddWorksheet("Compared to Ultimate (games)");
                workbook.AddWorksheet("Compared to Ultimate (files)");
                sheets.Add(1);
                sheets.Add(2);
            }


            FormatOriginalWorksheet(workbook);


            var gamesTotal = 0;
            var gameCounter = 1;
            var newGames = 0; 
            var newGamesTotal = 0; 
            var systemCounter = 0;

            foreach (var sheet in sheets)
            {

                systemCounter = 0;
                newGamesTotal = 0;
                gamesTotal = 0; 
                gameCounter = 1;
                newGames = 0;

                foreach (var systemEntry in systems.OrderBy(s => s.Key))
                {
                    var system = systemEntry.Key;
                    var gameList = systemEntry.Value.OrderBy(v => v.Name);

                    gameCounter = 1;
                    newGames = 0;
                    foreach (var game in gameList)
                    {
                        var gameName = game.Name?.Trim();
                        var fileName = Path.GetFileName(game.Path)?.Trim();
                        var content = sheet == 2 ? fileName : gameName;

                        workbook.Worksheets[sheet].AddCell(content, systemCounter, gameCounter);
                        if (diff && IsNewGame(workbook, system, gameName))
                        {
                            workbook.Worksheets[sheet].GetCell(systemCounter, gameCounter).SetStyle(BasicStyles.ColorizedBackground(Config.GameListExtractor.DiffBackgroundColor ?? "F0A500"));
                        }
                        else
                        {
                            newGames++;
                            newGamesTotal++;
                        }
                        gameCounter++;
                        gamesTotal++;
                    }

                    if (diff) workbook.Worksheets[sheet].AddCell($"{system?.Trim()} (total: {gameList.Count()}; new: {newGames})", systemCounter, 0, BasicStyles.Bold);
                    else workbook.Worksheets[sheet].AddCell($"{system?.Trim()} (total: {gameList.Count()})", systemCounter, 0, BasicStyles.Bold);

                    var max = gameList.Max(g => g.Name.Length);
                    workbook.Worksheets[sheet].SetColumnWidth(systemCounter, max);
                    systemCounter++;
                }
            }
            if (diff) workbook.SaveAs(Config.GameListExtractor.XlsxOutFile);
            else workbook.Save();

            Cmd.Write($"Exported {systems.Count()} systems; {gamesTotal} games; {newGamesTotal} new games.",ConsoleColor.Cyan);
        }
    }
}
