using System;
using System.Text.RegularExpressions;
using Renci.SshNet;

namespace VcxProjLib {
    [Serializable]
    public class RemoteHost {
        private String _username;
        private String _password;
        private String _host;
        private UInt16 _port;
        private SshClient _clientConnection;

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

        public void Connect() {
            ConnectionInfo ci = new PasswordConnectionInfo(_host, _port, _username, _password);
            _clientConnection = new SshClient(ci);
            _clientConnection.Connect();
        }

        public int Execute(String cmd, out String result) {
            if (!_clientConnection.IsConnected) {
                Connect();
            }

            SshCommand sshcmd = _clientConnection.CreateCommand(cmd);
            sshcmd.CommandTimeout = TimeSpan.FromMinutes(1);
            result = sshcmd.Execute();
            return sshcmd.ExitStatus;
        }
    }
}