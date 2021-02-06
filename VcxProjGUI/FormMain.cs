using System;
using System.Windows.Forms;
using VcxProjLib;

namespace VcxProjGUI {
    public partial class FormMain : Form {
        public FormMain() {
            InitializeComponent();
        }

        private void manageRemotesToolStripMenuItem_Click(object sender, System.EventArgs e) {
        }

        private void doSomethingToolStripMenuItem_Click(object sender, System.EventArgs e) {
            RemoteHost remote = new RemoteHost();
            remote.Username = "striker";
            remote.Password = "4040";
            remote.Host = "192.168.75.20";
            remote.Port = 22;
            remote.Connect();
            remote.Execute("echo 1", out String result);
            MessageBox.Show(result);
        }
    }
}
