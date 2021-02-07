using System;
using System.IO;
using System.Text.RegularExpressions;
using Renci.SshNet;

namespace VcxProjLib {
    [Serializable]
    public class RemoteHost {
        private String _username;
        private String _password;
        private String _host;
        private UInt16 _port;
        private ConnectionInfo _connectionInfo;
        private SshClient _sshClientConnection;
        private SftpClient _sftpClientConnection;

        public String Username {
            get { return _username; }
            set { _username = value; }
        }

        public String Password {
            get { return _password; }
            set { _password = value; }
        }

        public String Host {
            get { return _host; }
            set { _host = value; }
        }

        public UInt16 Port {
            get { return _port; }
            set { _port = value; }
        }

        public static readonly int Success = 0;

        public static RemoteHost Parse(String str) {
            // in format "user:pass@host:port". pass and port are optional
            Regex r = new Regex("^([\\w-]+):?(.*)@([A-z\\d\\.-]+):?([\\d]*)$");
            Match match = r.Match(str);
            if (!match.Success) {
                throw new ApplicationException("invalid format for remote");
            }

            RemoteHost remote = new RemoteHost {
                    Username = match.Groups[1].Value
                  , Password = match.Groups[2].Value
                  , Host = match.Groups[3].Value
                  , Port = UInt16.TryParse(match.Groups[4].Value, out UInt16 port) ? port : (UInt16) 22
            };

            return remote;
        }

        public override String ToString() {
            return $"{Username}@{Host}:{Port}";
        }

        public void PrepareForConnection() {
            _connectionInfo = new PasswordConnectionInfo(_host, _port, _username, _password);
            _sshClientConnection = new SshClient(_connectionInfo);
            _sshClientConnection.KeepAliveInterval = TimeSpan.FromSeconds(15);
            _sftpClientConnection = new SftpClient(_connectionInfo);
        }

        public int Execute(String cmd, out String result) {
            if (_connectionInfo == null) {
                PrepareForConnection();
            }

            if (!_sshClientConnection.IsConnected) {
                _sshClientConnection.Connect();
            }

            using (SshCommand sshcmd = _sshClientConnection.CreateCommand(cmd)) {
                sshcmd.CommandTimeout = TimeSpan.FromSeconds(3);
                result = sshcmd.Execute();
                return sshcmd.ExitStatus;
            }
        }

        public void DownloadFile(String filename) {
            if (_connectionInfo == null) {
                PrepareForConnection();
            }

            if (!_sshClientConnection.IsConnected) {
                _sshClientConnection.Connect();
            }

            _sftpClientConnection.DownloadFile(filename, Stream.Null);
        }

        public void ExtractInfoFromCompiler(Compiler compiler) {
            compiler.ExtractAdditionalInfo(this);
        }
    }
}