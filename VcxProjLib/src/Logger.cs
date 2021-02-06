using System;

namespace VcxProjLib {
    public enum LogLevel {
        Fatal
      , Error
      , Warning
      , Info
      , Debug
      , Trace
    }

    public static class Logger {
        public static readonly LogLevel DefaultLogLevel = LogLevel.Info;
        public static LogLevel Level { get; set; } = DefaultLogLevel;

        /// <summary>
        /// Log a record to console.
        /// </summary>
        /// <param name="level">Log severity</param>
        /// <param name="line">Line to log</param>
        public static void WriteLine(LogLevel level, String line) {
            if (level > Level) {
                return;
            }

            Console.WriteLine(line);
        }
    }
}