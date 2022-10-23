using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RickImageUpdater
{
    public class Updater
    {
        private string _progressFile;
        private List<string> _allFiles;
        private string _updateDir;
        private RemotePiReader _piReader;

        private void UpdateProgressFile(List<string> files) => File.WriteAllLines(_progressFile, files);

        public void Run()
        {
            _updateDir = GetUpdateDirectory();
            var files = GetFiles(_updateDir);
            var filesToDelete = GetFilesToDelete();

            if (!Cmd.AskBool($"Found [{files.Count}] files for update. Start update now? [y/n]")) return;

            UpdateFiles(files);
            DeleteFiles(filesToDelete);

            Cmd.Ask("Update completed.");
        }

        private void DeleteFiles(List<string> files)
        {

            var counter = 0;
            var total = files.Count;

            if (!Cmd.AskBool($"Found [{total}] files to delete. Continue? [y/n]")) return;

            foreach(var file in files)
            {
                try
                {
                    counter++;
                    Cmd.Write($"[{counter}/{total}] Deleting file [{file}]",ConsoleColor.Green,true);
                    _piReader.DeleteFile(file);
                }
                catch(Exception exception)
                {
                    Cmd.WriteError(file);
                    Cmd.WriteError(exception.Message);
                    Cmd.Spacer();
                }
            }
        }

        private List<string> GetFilesToDelete()
        {
            var rickDeleteFile = Path.Combine(_updateDir, "rick.deleted");
            return File.ReadAllLines(rickDeleteFile).ToList();
        }
        private void UploadFile(string counter, RemotePiReader piReader, string file, bool asRoot)
        {
            var relativePath = "/" + Path.GetRelativePath(_updateDir, file).Replace("\\", "/");
            Cmd.Write(counter+" "+relativePath, ConsoleColor.Green, true);

            try
            {
                piReader.UploadFile(file, relativePath, asRoot);
                _allFiles.Remove(file);
            }
            catch (Exception exception)
            {
                Cmd.WriteError(relativePath);
                Cmd.WriteError(exception.Message);
                Cmd.Spacer();
            }
        }

        private void UpdateFiles(List<string> files)
        {
            var rootFiles = files.Where(f => Path.GetRelativePath(_updateDir, f).Contains(@"etc\emulationstation\themes")).ToList();
            var piFiles = files.Where(f => rootFiles.All(r => r != f)).ToList();

            var ip = Cmd.Ask("Raspberry Pi IP/Name: ");
            var rootPassword = Cmd.Ask("Password (root-User): ");
            var piPassword = Cmd.Ask("Password (Pi-User): ");

            try
            {
                _piReader = new RemotePiReader();

                _piReader.Connect(ip, new LoginData("root", rootPassword), new LoginData("pi", piPassword));

                if(rootFiles.Any())
                {
                    Cmd.Write($"Updating [{rootFiles.Count}] files as root-user...", ConsoleColor.Yellow);
                    Cmd.Spacer();
                }
                

                var counter = 0;
                var total = rootFiles.Count;

                foreach(var file in rootFiles)
                {
                    counter++;
                    UploadFile($"[{counter}/{total}]", _piReader, file, true);
                }

                if (piFiles.Any())
                {
                    Cmd.Write($"Updating [{piFiles.Count}] files as Pi-user...", ConsoleColor.Yellow);
                    Cmd.Spacer();
                }
                

                counter = 0;
                total = piFiles.Count;
                foreach (var file in piFiles)
                {
                    counter++;
                    UploadFile($"[{counter}/{total}]", _piReader, file, false);
                }
            }
            catch (Exception e)
            {
                
                Cmd.WriteError(e.Message);
            }
            finally
            {
                UpdateProgressFile(_allFiles);
            }
        }
        

        private List<string> GetFiles(string updateDirectory)
        {
            _progressFile = Path.Combine(updateDirectory, "progress.run");
            if (!File.Exists(_progressFile))
            {
                var files = new List<string>();
                foreach(var dir in Directory.GetDirectories(updateDirectory))
                {
                    files.AddRange(Directory.GetFiles(dir, "*", SearchOption.AllDirectories));
                }
                UpdateProgressFile(files.ToList());
            }

            _allFiles = File.ReadAllLines(_progressFile).ToList();
            return _allFiles;
        }

        private string GetUpdateDirectory()
        {
            var cwd = Directory.GetCurrentDirectory();
            var directories = Directory.GetDirectories(cwd);
            var updateDir = directories.FirstOrDefault(d => d.ToLower() == "update");

            while (true)
            {                
                if (updateDir != null && File.Exists(Path.Combine(updateDir, "rick.update"))) break;
                
                if(updateDir != null)Cmd.WriteError($"Directory [{updateDir}] is no valid Update-Directory.");
                updateDir = Cmd.Ask("Enter Path to the Update-Directory:");
            }

            return updateDir;
        }
    }
}
