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

        private void CreateUpdate(string oldImageDrive, string newImageDrive, string updateDirectory)
        {
            var allFiles = new List<string>();
            foreach(var path in Config.UpdateCreator.Paths)
            {
                Cmd.Write("Next path: " + path);
                var newOrUpdatedFiles = GetNewOrUpdatedFiles(oldImageDrive, newImageDrive, path);
                Cmd.Write("Affected files: " + newOrUpdatedFiles.Count);
                allFiles.AddRange(newOrUpdatedFiles);
            }

            Cmd.Write($"Copying files to [{updateDirectory}]", ConsoleColor.Green);
            foreach(var file in allFiles)
            {
                var relative = Path.GetRelativePath(newImageDrive, file);
                var destFile = Path.Combine(updateDirectory, relative);

                var directory = Path.GetDirectoryName(destFile);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                File.Copy(file, destFile);
            }
        }

        private List<string> GetNewOrUpdatedFiles(string oldImageDrive, string newImageDrive, string path)
        {
            var sourceDir = Path.Combine(oldImageDrive, path);
            var destDir = Path.Combine(newImageDrive, path);

            var oldFiles = GetAllFiles(sourceDir, oldImageDrive);
            var newFiles = GetAllFiles(destDir, newImageDrive);
            var allFiles = new List<string>();

            Cmd.Write("Number of files: " + newFiles.Count);
            var fileCounter = 0;
            var totalCount = newFiles.Count;

            foreach(var newFile in newFiles)
            {
                fileCounter++;
                Cmd.Write($"[{fileCounter}/{totalCount}] {newFile}",ConsoleColor.Cyan, true);


                var newAbsolute = Path.Combine(newImageDrive, newFile);
                if (!oldFiles.Contains(newFile)) allFiles.Add(newAbsolute);
                else
                {
                    var oldRelative = oldFiles.First(f => f == newFile);
                    var oldAbsolute = Path.Combine(oldImageDrive, oldRelative);

                    var oldFileInfo = new FileInfo(oldAbsolute);
                    var newFileInfo = new FileInfo(newAbsolute);
                    if ((oldFileInfo.Length  / (1024 * 1024))>1 && AreLargeFilesEqual(oldFileInfo, newFileInfo)) continue; //>1MB
                    else if(AreSmallFilesEqual(oldFileInfo, newFileInfo)) continue;
                    allFiles.Add(newAbsolute);
                }
            }
            return allFiles;
            //var diffs = newFiles.Except(oldFiles).Distinct().ToArray();
        }
        private bool AreSmallFilesEqual(FileInfo first, FileInfo second)
        {
             return File.ReadAllBytes(first.FullName).SequenceEqual(File.ReadAllBytes(second.FullName));
            //byte[] firstHash = MD5.Create().ComputeHash(first.OpenRead());
            //byte[] secondHash = MD5.Create().ComputeHash(second.OpenRead());

            //for (int i = 0; i < firstHash.Length; i++)
            //{
            //    if (firstHash[i] != secondHash[i])
            //        return false;
            //}
            //return true;
        }

        private bool AreLargeFilesEqual(FileInfo first, FileInfo second)
        {
            return first.Length == second.Length;
        }
        private List<string> GetAllFiles(string directory, string relativeTo)
        {
            var dirInfo = new DirectoryInfo(directory);
            var directories = dirInfo.GetDirectories().Where(x => x.Attributes == FileAttributes.Directory && x.Attributes != FileAttributes.Hidden && x.Attributes != FileAttributes.System).ToList();

            directories.Add(dirInfo);
            var allFiles = new List<string>();
            foreach(var dir in directories)
            {
                try
                {
                    var isDir = Directory.Exists(dir.FullName);
                    var files = Directory.GetFiles(dir.FullName, "*");
                    if (string.IsNullOrWhiteSpace(relativeTo)) allFiles.AddRange(files);
                    else foreach (var file in files) allFiles.Add(Path.GetRelativePath(relativeTo, file));
                }
                catch(FileNotFoundException){} //if directory is empty
                catch (Exception e)
                {
                    Cmd.WriteError(e.Message);
                }
            }

            return allFiles;
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
                foreach(var path in Config.UpdateCreator.Paths)
                {
                    var dir = Path.GetFullPath(Path.Combine(mountedDrive, path));
                    if (!Directory.Exists(dir))
                    {
                        Cmd.WriteError($"Directory [{dir}] not found. No valid img-mounted-drive.");
                        allDirectoriesExists = false;                        break;
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
                } else  Cmd.WriteError($"Directory [{dir}] not found.");
            }
            return dir;
        }

        private bool IsDirectoryEmpty(string directory, bool outputError)
        {
            var empty = (!Directory.GetFiles(directory).Any() && !Directory.GetDirectories(directory).Any());
            if(!empty && outputError) Cmd.WriteError($"Direcotry [{directory}] is not empty.");
            return empty;
        }
        public override void Run()
        {
            var oldMountedDrive = GetMountedDrive("Enter old-img mounted drive (like e:)");
            var newMountedDrive = GetMountedDrive("Enter new-img mounted drive (like f:)");
            var updateDirectory = Config.UpdateCreator.OutputDirectory;


            if (!Directory.Exists(updateDirectory) || !IsDirectoryEmpty(updateDirectory, true))
                updateDirectory = GetDirectory("Enter Update Destination Directory:", true);


            Cmd.Write("Source Image Drive: " + oldMountedDrive, ConsoleColor.Yellow);
            Cmd.Write("Compare Image Drive: " + newMountedDrive, ConsoleColor.Yellow);

            if (!IsAnswerPositive(Cmd.Ask("Is this correct? [y/n]"))) return;

            CreateUpdate(oldMountedDrive, newMountedDrive, updateDirectory);
        }
    }
}
