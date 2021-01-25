using System;

namespace VcxProjGUI {
    [Serializable]
    class PersistentConfiguration {
        // save there:
        //    1. all known remotes
        //    2. last configuration file path, or last 15 configuration files
        // load at startup, save every change
    }
}