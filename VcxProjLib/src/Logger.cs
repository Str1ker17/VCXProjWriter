using System;
using System.IO;

namespace VcxProjLib {
    public enum LogLevel {
        Fatal
      , Error
      , Warning
      , Info
      , Debug
      , Trace
    }

    public class Logger {
        public static readonly LogLevel DefaultLogLevel = LogLevel.Info;
        public static LogLevel Level { get; set; } = DefaultLogLevel;

        protected static String backingStorePath = "VCXProjWriter_LastRun.log";
        protected static StreamWriter backingStore;
        protected static Boolean alreadyTriedToOpenBackingStore;

        /// <summary>
        /// Log a record to console.
        /// </summary>
        /// <param name="level">Log severity</param>
        /// <param name="line">Line to log</param>
        public static void WriteLine(LogLevel level, String line) {
            if (level > Level) {
                return;
            }

            if (backingStore == null && !alreadyTriedToOpenBackingStore) {
                alreadyTriedToOpenBackingStore = true;
                try {
                    backingStore = new StreamWriter(backingStorePath);
                    backingStore.AutoFlush = true;
                }
                catch {
                    Logger.WriteLine(LogLevel.Warning, "could not open ");
                }
            }

            if (backingStore != null) {
                backingStore.WriteLine(line);
            }
            Console.WriteLine(line);
        }
    }
}