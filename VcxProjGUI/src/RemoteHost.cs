using System;
using Renci.SshNet;

namespace VcxProjGUI {
    [Serializable]
    internal class RemoteHost {
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
            set {
                _password = value;
            }
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
            return null;
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