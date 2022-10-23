using Renci.SshNet;
using Renci.SshNet.Sftp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace rickhelper
{
    public abstract class DirectoryReader
    {
        public string BaseDirectory { get; set; }
        protected DirectoryReader()
        {

        }

        public abstract bool CheckIsValidDirectory(List<string> paths);
        public abstract List<string> GetAllDirectories(string directory);
        public abstract List<HashedFile> GetAllFiles(string directory, string relativeTo);
        public string CalculateMD5(string filePath)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }

    public class LocalDirectoryReader : DirectoryReader
    {
        public override bool CheckIsValidDirectory(List<string> paths)
        {
            foreach (var path in paths)
            {
                var dir = Path.GetFullPath(Path.Combine(BaseDirectory, path));
                if (!Directory.Exists(dir))
                {
                    Cmd.WriteError($"Directory [{dir}] not found. No valid directory.");
                    return false;
                }
            }
            return true;
        }

        public override List<string> GetAllDirectories(string directory)
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

        public override List<HashedFile> GetAllFiles(string directory, string relativeTo)
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
                    var files = GetFiles(dir);
                    foreach (var file in files)
                    {
                        var fileInfo = new FileInfo(file);

                        allFiles.Add(new HashedFile
                        {
                            File = Path.GetRelativePath(relativeTo, file),
                            Hash = GetFileSizeInMb(file) > 1 ? "" : CalculateMD5(file),
                            Length = fileInfo.Length
                        });
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


        private long GetFileSizeInMb(string filePath)
        {
            return new FileInfo(filePath).Length / (1024 * 1024);
        }

        private List<string> GetFiles(string dir)
        {
            return Directory.GetFiles(dir, "*").ToList();
        }
    }

    public class RemotePiReader : DirectoryReader, IDisposable
    {
        private SftpClient _piUserClient;
        private SftpClient _rootUserClient;

        public void Connect(string piUnc, LoginData rootUser, LoginData piUser=null)
        {
            var rootInfo = new ConnectionInfo(piUnc, rootUser.User, new PasswordAuthenticationMethod(rootUser.User, rootUser.Password));
            Cmd.Write("Connecting user " + rootUser.User, ConsoleColor.Yellow);
            _rootUserClient = new SftpClient(rootInfo);
            _rootUserClient.Connect();
            Cmd.Write("Ok", ConsoleColor.Green);

            if(piUser!=null)
            {
                var piInfo = new ConnectionInfo(piUnc, piUser.User, new PasswordAuthenticationMethod(piUser.User, piUser.Password));

                Cmd.Write("Connecting user " + piUser.User, ConsoleColor.Yellow);
                _piUserClient = new SftpClient(piInfo);
                _piUserClient.Connect();
                Cmd.Write("Ok", ConsoleColor.Green);
            }
        }

        public override bool CheckIsValidDirectory(List<string> paths)
        {
            foreach (var path in paths)
            {
                var dir = Path.Combine(BaseDirectory, path);
                
                if (!_rootUserClient.ListDirectory(dir).Any())
                {
                    Cmd.WriteError($"Directory [{dir}] not found. No valid directory.");
                    return false;
                }
            }
            return true;
        }

        public void Dispose()
        {
            if (_rootUserClient != null) _rootUserClient.Dispose();
            if (_piUserClient != null) _piUserClient.Dispose();
        }

        public override List<string> GetAllDirectories(string directory)
        {
            var allDirectories = new List<string>();
            try
            {
                var directories = _rootUserClient.ListDirectory(directory).Where(d => d.IsDirectory && !(new[] { ".",".."}).Contains(d.Name));
                foreach (var dir in directories)
                {
                    allDirectories.Add(dir.FullName);
                    var subDirs = GetAllDirectories(dir.FullName);
                    allDirectories.AddRange(subDirs);
                }
            }
            catch (FileNotFoundException) { }
            catch (Exception e)
            {
                Cmd.WriteError(e.Message);
                Cmd.Spacer();
            }
            return allDirectories.Distinct().ToList();
        }

        public override List<HashedFile> GetAllFiles(string directory, string relativeTo)
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

                    var files = GetFiles(dir);
                    foreach (var file in files)
                    {
                        try{
                            allFiles.Add(new HashedFile
                            {
                                File = file.FullName,
                                Hash = GetFileSizeInMb(file.Length) > 1 ? "" : CalculateMD5(file),
                                Length = file.Length
                            });
                        }
                        catch (Exception ex)
                        {
                            //allFiles.Add(new HashedFile
                            //{
                            //    File = file.FullName,
                            //    Hash = "",
                            //    Length = file.Length
                            //});
                            Cmd.WriteError("Error reading file [" + file.FullName + "]: " + ex.Message);
                            Cmd.Spacer();
                        }
                    }
                }
                catch (FileNotFoundException) { } //if directory is empty
                catch (Exception e)
                {
                    Cmd.WriteError("Error reading directory ["+dir+"]: "+e.Message);
                    Cmd.Spacer();
                }
            }

            return allFiles;
        }
        private long GetFileSizeInMb(long length)
        {
            return length / (1024 * 1024);
        }

        private string CalculateMD5(SftpFile file)
        {
            using (var md5 = MD5.Create())
            using (var stream = _rootUserClient.OpenRead(file.FullName))
            {
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        public List<SftpFile> GetFiles(string directory)
        {
            return _rootUserClient.ListDirectory(directory).Where(d => !d.IsDirectory).ToList();
        }
    }

}
