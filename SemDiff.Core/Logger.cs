using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace SemDiff.Core
{
    /// <summary>
    /// Used for logging important events, uses the Caller* attributes to collect more data than a
    /// simple print. A date is also added to the ouput along with the severity (Debug, Info, Error)
    /// matching the function called
    /// </summary>
    /// <example>Logger.Error(exception.Message)</example>
    internal static class Logger
    {
        private static void Log(Severities severity, string message, string callerFilePath, string callerMemberName, int callerLineNumber)
        {
            Trace.WriteLine($@"{severity.ToString().ToUpper().PadRight(5)}: {DateTime.Now.ToString("s")} | ""{message}"" @ {Path.GetFileName(callerFilePath)}:{callerLineNumber} {callerMemberName}");
        }

        public static void Debug(string m, [CallerFilePath] string cfp = "", [CallerMemberName] string cmn = "", [CallerLineNumber] int cln = 0) => Log(Severities.Debug, m, cfp, cmn, cln);

        public static void Info(string m, [CallerFilePath] string cfp = "", [CallerMemberName] string cmn = "", [CallerLineNumber] int cln = 0) => Log(Severities.Info, m, cfp, cmn, cln);

        public static void Error(string m, [CallerFilePath] string cfp = "", [CallerMemberName] string cmn = "", [CallerLineNumber] int cln = 0) => Log(Severities.Error, m, cfp, cmn, cln);

        /// <summary>
        /// A Severity is a tag that is added to the error message. Levels of errors that get
        /// progressivly worse.
        /// </summary>
        private enum Severities
        {
            Debug,
            Info,
            Error,
        }
    }
}