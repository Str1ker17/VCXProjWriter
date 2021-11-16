using System;
using System.Windows.Forms;
using VcxProjLib;

namespace VcxProjGUI {
    static class GUIApp {
        internal static FormMain mainForm;
        internal static Configuration config;
        internal static PersistentConfiguration persistConfig;

        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // load settings
            config = Configuration.Default();
            persistConfig = new PersistentConfiguration(); // FIXME

            // create forms
            mainForm = new FormMain();

            // here we go
            Application.Run(mainForm);
        }
    }
}