using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace rickhelper
{
    public class UpdateCreator : FixerBase
    {
        public UpdateCreator(Configuration configuration) : base(configuration)
        {
        }

        public override void Run()
        {
            var ultimateHashedFiles = GetHashedFiles("ultimate-hash-csv-file: (like c:\\temp\\ultimate_hash.csv)");
            var latestHashedFiles = GetHashedFiles("latest-hash-csv-file: (like c:\\temp\\latest_hash.csv)");
            var latestMountedImgDrive = GetMountedDrive("Enter mounted drive of the latest image (like e:)");

            var updateDirectory = Config.UpdateCreator.OutputDirectory;

            if(!IsDirectoryEmpty(updateDirectory, false))
                Cmd.Write($"Warning. The folder [{updateDirectory}] is not empty. Make sure its the right directory.", ConsoleColor.Yellow);
            else
            {
                if (!Directory.Exists(updateDirectory) || !IsDirectoryEmpty(updateDirectory, false))
                    updateDirectory = GetDirectory("Enter Update Destination Directory:", false);
            }
            

            if (!IsAnswerPositive(Cmd.Ask("Is this correct? [y/n]"))) return;

            CreateUpdate(latestMountedImgDrive, ultimateHashedFiles, latestHashedFiles, updateDirectory);
        }
        
        private void CreateRickDeletedFile(string updateDirectory, List<string> deleted)
        {
            var file = Path.Combine(updateDirectory, "rick.deleted");
            File.WriteAllLines(file, deleted);
        }

        private void CreateRickUpdateFile(string updateDirectory)
        {
            File.Create(Path.Combine(updateDirectory, "rick.update"));
        }

        private void CreateUpdate(string latestMountedImageDrive, List<HashedFile> ultimateHashedFiles, List<HashedFile> latestHashedFiles, string updateDirectory)
        {
            var newOrUpdatedFiles = GetNewOrUpdatedFiles(ultimateHashedFiles, latestHashedFiles);
            var deletedFiles = GetDeletedFiles(ultimateHashedFiles, latestHashedFiles);

            CreateRickDeletedFile(updateDirectory, deletedFiles);

            CreateRickUpdateFile(updateDirectory);
            Cmd.Write("New/Updated files: " + newOrUpdatedFiles.Count,ConsoleColor.Cyan);
            Cmd.Write("Deleted files: " + deletedFiles.Count, ConsoleColor.Cyan);

            Cmd.Write($"Copying files to [{updateDirectory}]", ConsoleColor.Green);
            var total = newOrUpdatedFiles.Count;
            var counter = 0;
            foreach(var relative in newOrUpdatedFiles)
            {
                counter++;
                Cmd.Write($"[{counter}/{total}]", ConsoleColor.Cyan, true);
                var sourceFile = Path.Combine(latestMountedImageDrive, relative);
                var destFile = Path.Combine(updateDirectory, relative);

                var directory = Path.GetDirectoryName(destFile);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                
                if(!File.Exists(destFile)) File.Copy(sourceFile, destFile);
            }

            Cmd.Write("TODO: creating delete-bat file? ");

            foreach (var deleted in deletedFiles)
            {
                Cmd.Write(deleted, ConsoleColor.Yellow);                
            }
        }

        private List<string> GetNewOrUpdatedFiles(List<HashedFile> ultimateHashedFiles, List<HashedFile> latestHashedFiles)
        {
            var allFiles = new List<string>();
            Cmd.Write($"Finding new or updated files...checking [{latestHashedFiles.Count}] files now...");
            var counter = 0;
            var total = latestHashedFiles.Count;
            var dic = new Dictionary<string, HashedFile>();

            foreach(var ultimate in ultimateHashedFiles)
            {
                if (!dic.ContainsKey(ultimate.File)) dic.Add(ultimate.File, ultimate);
            }
            Cmd.Write("checking new or updated files...");
            foreach(var latest in latestHashedFiles)
            {
                counter++;
                Cmd.Write($"[{counter}/{total}]",ConsoleColor.Cyan,true);
                var ultimateFile = dic.ContainsKey(latest.File) ? dic[latest.File] : null; //ultimateHashedFiles.FirstOrDefault(f => f.File == latest.File);
                if (ultimateFile?.Hash != latest.Hash || ultimateFile?.Length != latest.Length) allFiles.Add(latest.File);
            }

            return allFiles;
        }

        private List<string> GetDeletedFiles(List<HashedFile> ultimateHashedFiles, List<HashedFile> latestHashedFiles)
        {
            var allFiles = new List<string>();
            var dic = new Dictionary<string, HashedFile>();

            foreach (var latest in latestHashedFiles)
            {
                if (!dic.ContainsKey(latest.File)) dic.Add(latest.File, latest);
            }

            Cmd.Write($"checking deleted files...", ConsoleColor.Cyan, true);
            foreach (var ultimate in ultimateHashedFiles)
            {
                if(!dic.ContainsKey(ultimate.File)) allFiles.Add(ultimate.File);
            }

            return allFiles;
        }

        private List<HashedFile> GetHashedFiles(string question)
        {
            var csvFile = "";
            var hashedFiles = new List<HashedFile>();

            while (true)
            {
                csvFile = Cmd.Ask(question);
                if (csvFile.StartsWith("\"")) csvFile = csvFile.Substring(1, csvFile.Length - 2);
                if (!File.Exists(csvFile))
                {
                    Cmd.WriteError($"File [{csvFile}] not found.");
                    continue;
                }

                if (!csvFile.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    Cmd.WriteError($"File [{csvFile}] has to end with .csv");
                    continue;
                }
                var lines = File.ReadAllLines(csvFile);
                if (lines.Length == 0 || lines[0].Split(";").Length != 3)
                {
                    Cmd.WriteError("Invalid csv-file.");
                    continue;
                }

                foreach (var line in lines)
                {
                    if (line.Split(";").Length != 3)
                    {
                        Cmd.WriteError($"Invalid csv-line [{line}]");
                        continue;
                    }
                    hashedFiles.Add(new HashedFile(line));
                }
                break;
            }


            return hashedFiles;
        }

        private string GetMountedDrive(string question)
        {
            var mountedDrive = "";
            while (true)
            {
                mountedDrive = Cmd.Ask(question);
                if (mountedDrive.Length > 1) mountedDrive = mountedDrive[0] + ":\\";
                else mountedDrive = mountedDrive + ":\\";

                var allDirectoriesExists = true;
                foreach (var path in Config.UpdateCreator.Paths)
                {
                    var dir = Path.GetFullPath(Path.Combine(mountedDrive, path));
                    if (!Directory.Exists(dir))
                    {
                        Cmd.WriteError($"Directory [{dir}] not found. No valid img-mounted-drive.");
                        allDirectoriesExists = false; break;
                    }
                }

                if (allDirectoriesExists) break;
            }
            return mountedDrive;
        } 

        private string GetDirectory(string question, bool hasToBeEmpty)
        {
            string dir;
            while (true)
            {
                dir = Cmd.Ask(question);

                if (Directory.Exists(dir))
                {
                    if (hasToBeEmpty && IsDirectoryEmpty(dir, true)) break;
                    if(!hasToBeEmpty && IsDirectoryEmpty(dir, false))
                        Cmd.Write($"Warning. The folder [{dir}] is not empty. Make sure its the right directory.", ConsoleColor.Yellow);
                    
                } else  Cmd.WriteError($"Directory [{dir}] not found.");
            }
            return dir;
        }

        private bool IsDirectoryEmpty(string directory, bool outputError)
        {
            if (!Directory.Exists(directory)) return true;
            var empty = (!Directory.GetFiles(directory).Any() && !Directory.GetDirectories(directory).Any());
            if(!empty && outputError) Cmd.WriteError($"Direcotry [{directory}] is not empty.");
            return empty;
        }
        
    }
}
