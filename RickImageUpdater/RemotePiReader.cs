using Renci.SshNet;
using Renci.SshNet.Sftp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RickImageUpdater
{
    public class RemotePiReader : IDisposable
    {
        private SftpClient _piUserClient;
        private SftpClient _rootUserClient;

        public void Connect(string piUnc, LoginData rootUser, LoginData piUser = null)
        {
            var rootInfo = new ConnectionInfo(piUnc, rootUser.User, new PasswordAuthenticationMethod(rootUser.User, rootUser.Password));
            Cmd.Write("Connecting user " + rootUser.User, ConsoleColor.Yellow);
            _rootUserClient = new SftpClient(rootInfo);
            _rootUserClient.Connect();
            Cmd.Write("Ok", ConsoleColor.Green);

            if (piUser != null)
            {
                var piInfo = new ConnectionInfo(piUnc, piUser.User, new PasswordAuthenticationMethod(piUser.User, piUser.Password));

                Cmd.Write("Connecting user " + piUser.User, ConsoleColor.Yellow);
                _piUserClient = new SftpClient(piInfo);
                _piUserClient.Connect();
                Cmd.Write("Ok", ConsoleColor.Green);
            }
        }

        public void UploadFile(string localFile, string remoteFile, bool asRoot)
        {
            var client = asRoot ? _rootUserClient : _piUserClient;

            using (var stream = File.OpenRead(localFile))
            {
                client.UploadFile(stream, remoteFile);
            }
        }

        public void DeleteFile(string remoteFile)
        {
            if (!_rootUserClient.Exists(remoteFile)) return;
            _rootUserClient.Delete(remoteFile);
        }

        public void Dispose()
        {
            if (_rootUserClient != null) _rootUserClient.Dispose();
            if (_piUserClient != null) _piUserClient.Dispose();
        }

    }

}
