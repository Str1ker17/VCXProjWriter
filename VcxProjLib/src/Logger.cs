using System;

namespace VcxProjLib {
    public static class Logger {
        public static Boolean DebugOutput { get; set; } = false;

        public static void WriteLine(String line) {
            if (!DebugOutput) {
                return;
            }

            Console.WriteLine(line);
        }
    }
}