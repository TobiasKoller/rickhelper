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

        //private string GetMountedDrive(string question)
        //{
        //    var mountedDrive = "";
        //    while (true)
        //    {
        //        mountedDrive = Cmd.Ask(question);
        //        if (mountedDrive.Length > 1) mountedDrive = mountedDrive[0] + ":\\";
        //        else mountedDrive = mountedDrive + ":\\";

        //        var allDirectoriesExists = true;
        //        foreach (var path in Config.UpdateCreator.Paths)
        //        {
        //            var dir = Path.GetFullPath(Path.Combine(mountedDrive, path));
        //            if (!Directory.Exists(dir))
        //            {
        //                Cmd.WriteError($"Directory [{dir}] not found. No valid img-mounted-drive.");
        //                allDirectoriesExists = false; break;
        //            }
        //        }

        //        if (allDirectoriesExists) break;
        //    }
        //    return mountedDrive;
        //}

        public override void Run()
        {
            Cmd.Write("Hash Generator", ConsoleColor.Yellow);

            DirectoryReader directoryReader;
            while (true)
            {
                Cmd.Write("[1] From local directory", ConsoleColor.Gray);
                Cmd.Write("[2] Remote from Pi", ConsoleColor.Gray);
                
                if(!int.TryParse(Cmd.Ask("Your choice: "), out int answer) || (answer != 1 && answer != 2))
                {
                    Cmd.WriteError("Invalid choice.");
                    continue;
                }
                if (Config.HashGenerator == null) Config.HashGenerator = new HashGenerator();
                if (answer == 1)
                {
                    directoryReader = new LocalDirectoryReader();
                    var directory = Cmd.Ask("Retropie-Directory: ");
                    directoryReader.BaseDirectory = directory;
                    Config.HashGenerator.Directory = directory;
                }
                else
                {
                    var ip = Config.HashGenerator.PiServer;

                    if (!string.IsNullOrWhiteSpace(ip) && (!IsAnswerPositive(Cmd.Ask($"Use Raspberry Pi [{ip}]? [y/n]"))))
                        ip = ""; 

                    if(string.IsNullOrWhiteSpace(ip)) ip = Cmd.Ask("Raspberry Pi IP/Name: ");
                    //var piPassword = Cmd.Ask("Password (Pi-User): ");
                    //var rootPassword = Cmd.Ask("Password (root-User): ");
                    var user = Cmd.Ask("Username: ");
                    var password = Cmd.Ask("Password: ");
                    Config.HashGenerator.PiServer = ip;

                    try
                    {
                        var piReader = new RemotePiReader();

                        piReader.Connect(ip, new LoginData(user, password));
                        piReader.BaseDirectory = "/";

                        directoryReader = piReader;
                    }
                    catch(Exception e)
                    {
                        Cmd.WriteError(e.Message);
                        continue;
                    }
                }

                if(directoryReader.CheckIsValidDirectory(Config.UpdateCreator.Paths))
                break;
            }
            
            
            //var mountedDrive = GetMountedDrive("Enter image mounted drive (like e:)");
            string outFile;
            while (true)
            {
                outFile = Config.HashGenerator.OutputFile;

                if (!string.IsNullOrWhiteSpace(outFile) && (!IsAnswerPositive(Cmd.Ask($"output to [{outFile}] ? [y/n]")))) 
                    outFile = "";
                
                if(string.IsNullOrWhiteSpace(outFile)) outFile = Cmd.Ask("Out-file: (like c:\\tmp\\out.csv): ");
                if (outFile.StartsWith("\"")) outFile = outFile.Substring(1, outFile.Length - 2);

                if(Path.GetExtension(outFile).ToUpper() != ".CSV")
                {
                    Cmd.WriteError("Filename has to end with .csv");
                    continue;
                }
                if (File.Exists(outFile))
                {
                    Cmd.WriteError($"File [{outFile}] already exist.");
                    continue;
                }
                var outDir = Path.GetDirectoryName(outFile);
                if (!Directory.Exists(outDir))
                {
                    Cmd.Write("Creating directory [" + outDir + "]");
                    Directory.CreateDirectory(outDir);
                }
                break;
            }

            Config.HashGenerator.OutputFile = outFile;
            GenerateHashes(directoryReader, outFile);
        }

        private void GenerateHashes(DirectoryReader directoryReader, string outFile)
        {
            var allFiles = new List<HashedFile>();
            foreach (var path in Config.UpdateCreator.Paths)
            {
                Cmd.Write("Next path: " + path);
                //var directory = Path.Combine(mountedDrive, path);
                //var files = GetAllFiles(directory, mountedDrive);
                var directory = Path.Combine(directoryReader.BaseDirectory, path);
                var files = directoryReader.GetAllFiles(directory, directoryReader.BaseDirectory);
                allFiles.AddRange(files);
            }

            

            using (var fs = new StreamWriter(outFile))
            {

                foreach (var file in allFiles.OrderBy(f => f.File))
                {
                    file.File = file.File.Replace("\\", "/");
                    if (!file.File.StartsWith("/")) file.File = "/" + file.File;

                    fs.WriteLine($"{file.File};{file.Hash};{file.Length}");
                }
            }
        }

        //private List<string> GetAllDirectories(string directory)
        //{
        //    var allDirectories = new List<string>();
        //    try
        //    {
        //        var dirInfo = new DirectoryInfo(directory);
        //        var directories = dirInfo.GetDirectories().Where(x => x.Attributes == FileAttributes.Directory && x.Attributes != FileAttributes.Hidden && x.Attributes != FileAttributes.System).ToList();

        //        foreach (var dir in directories)
        //        {
        //            allDirectories.Add(dir.FullName);
        //            var subDirs = GetAllDirectories(dir.FullName);
        //            allDirectories.AddRange(subDirs);
        //        }
        //    }
        //    catch (FileNotFoundException) { } //if directory is empty
        //    catch (Exception e)
        //    {
        //        Cmd.WriteError(e.Message);
        //        Cmd.Spacer();
        //    }
        //    return allDirectories.Distinct().ToList();
        //}

        //private List<HashedFile> GetAllFiles(string directory, string relativeTo)
        //{
        //    var directories = GetAllDirectories(directory);

        //    directories.Add(directory);
        //    var allFiles = new List<HashedFile>();

        //    var counter = 0;
        //    var totalCount = directories.Count;
        //    Cmd.Spacer();

        //    foreach (var dir in directories)
        //    {
        //        counter++;
        //        Cmd.Write($"[{counter}/{totalCount}] {dir}", ConsoleColor.Cyan, true);
        //        try
        //        {
        //            var isDir = Directory.Exists(dir);
        //            var files = Directory.GetFiles(dir, "*");
        //            foreach (var file in files)
        //            {
        //                var fileInfo = new FileInfo(file);

        //                allFiles.Add(new HashedFile
        //                {
        //                    File = Path.GetRelativePath(relativeTo, file),
        //                    Hash = GetFileSizeInMb(fileInfo) > 1? "" : CalculateMD5(file),
        //                    Length = fileInfo.Length
        //                }) ;
        //            }
        //        }
        //        catch (FileNotFoundException) { } //if directory is empty
        //        catch (Exception e)
        //        {
        //            Cmd.WriteError(e.Message);
        //        }
        //    }

        //    return allFiles;
        //}

        //private long GetFileSizeInMb(FileInfo fileInfo)
        //{
        //    return fileInfo.Length / (1024 * 1024);
        //}
        //private string CalculateMD5(string filename)
        //{
        //    using (var md5 = MD5.Create())
        //    using (var stream = File.OpenRead(filename))
        //    {
        //        var hash = md5.ComputeHash(stream);
        //        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        //    }
            
        //}
    }
}
