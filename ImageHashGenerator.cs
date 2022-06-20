using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace rickhelper
{
    public class ImageHashGenerator : FixerBase
    {
        public ImageHashGenerator(Configuration configuration) : base(configuration)
        {

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

        public override void Run()
        {
            Cmd.Write("Image Hash Generator");
            var mountedDrive = GetMountedDrive("Enter image mounted drive (like e:)");
            string outFile;
            while (true)
            {
                outFile = Cmd.Ask("Out-file: (like c:\\tmp\\out.csv): ");
                if (File.Exists(outFile))
                {
                    Cmd.WriteError($"File [{outFile}] already exist.");
                    continue;
                }
                break;
            }

            //var mountedDrive = "g:\\";
            ////var outFile = @"D:\retropie\generated_hashes_ultimate.csv";
            //var mountedDrive = "e:\\";
            //var outFile = @"D:\retropie\generated_hashes_latest.csv";

            GenerateHashes(mountedDrive, outFile);
        }

        private void GenerateHashes(string mountedDrive, string outFile)
        {
            var allFiles = new List<HashedFile>();
            foreach (var path in Config.UpdateCreator.Paths)
            {
                Cmd.Write("Next path: " + path);
                var directory = Path.Combine(mountedDrive, path);
                var files = GetAllFiles(directory, mountedDrive);
                allFiles.AddRange(files);
            }

            

            using (var fs = new StreamWriter(outFile))
            {
                foreach (var file in allFiles.OrderBy(f => f.File)) fs.WriteLine($"{file.File};{file.Hash};{file.Length}");
            }
        }

        private List<string> GetAllDirectories(string directory)
        {
            var allDirectories = new List<string>();
            try
            {
                var dirInfo = new DirectoryInfo(directory);
                var directories = dirInfo.GetDirectories().Where(x => x.Attributes == FileAttributes.Directory && x.Attributes != FileAttributes.Hidden && x.Attributes != FileAttributes.System).ToList();

                foreach (var dir in directories)
                {
                    allDirectories.Add(dir.FullName);
                    var subDirs = GetAllDirectories(dir.FullName);
                    allDirectories.AddRange(subDirs);
                }
            }
            catch (FileNotFoundException) { } //if directory is empty
            catch (Exception e)
            {
                Cmd.WriteError(e.Message);
                Cmd.Spacer();
            }
            return allDirectories.Distinct().ToList();
        }

        private List<HashedFile> GetAllFiles(string directory, string relativeTo)
        {
            var directories = GetAllDirectories(directory);

            directories.Add(directory);
            var allFiles = new List<HashedFile>();

            var counter = 0;
            var totalCount = directories.Count;
            Cmd.Spacer();

            foreach (var dir in directories)
            {
                counter++;
                Cmd.Write($"[{counter}/{totalCount}] {dir}", ConsoleColor.Cyan, true);
                try
                {
                    var isDir = Directory.Exists(dir);
                    var files = Directory.GetFiles(dir, "*");
                    foreach (var file in files)
                    {
                        var fileInfo = new FileInfo(file);

                        allFiles.Add(new HashedFile
                        {
                            File = Path.GetRelativePath(relativeTo, file),
                            Hash = GetFileSizeInMb(fileInfo) > 1? "" : CalculateMD5(file),
                            Length = fileInfo.Length
                        }) ;
                    }
                }
                catch (FileNotFoundException) { } //if directory is empty
                catch (Exception e)
                {
                    Cmd.WriteError(e.Message);
                }
            }

            return allFiles;
        }

        private long GetFileSizeInMb(FileInfo fileInfo)
        {
            return fileInfo.Length / (1024 * 1024);
        }
        private string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filename))
            {
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
            
        }
    }

    public class HashedFile
    {
        public string File { get; set; }
        public string Hash { get; set; }
        public long Length { get; set; }
    }
}
