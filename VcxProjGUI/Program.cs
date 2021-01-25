using System;
using System.Windows.Forms;

namespace VcxProjGUI {
    static class Program {
        internal static FormMain MainForm = null;

        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // create forms
            MainForm = new FormMain();

            // here we go
            Application.Run(MainForm);
        }
    }
}