using System;
using System.Collections.Immutable;
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
        public static ImmutableArray<Action<string>> Hooks { get; private set; } = ImmutableArray<Action<string>>.Empty;
        private static bool enabled = false;

        private static void Log(Severities severity, string message, string callerFilePath, string callerMemberName, int callerLineNumber)
        {
            if (enabled)
            {
                DoHooks($@"{severity.ToString().ToUpper().PadRight(5)}: {DateTime.Now.ToString("s")} | ""{message}"" @ {Path.GetFileName(callerFilePath)}:{callerLineNumber} {callerMemberName}");
            }
        }

        public static void AddHooks(Action<string> hook)
        {
            enabled = true;
            Hooks = Hooks.Add(hook);
        }

        private static void DoHooks(string message)
        {
            foreach (var h in Hooks)
            {
                h?.Invoke(message);
            }
        }

        //This is a dirty way to log, because we have to lock around the writing and the file never closed. A better way would be a small database.
        public static Action<string> LogToFile(string fileName)
        {
            var file = File.AppendText(fileName);
            return s =>
            {
                lock (file)
                {
                    file.WriteLine(s);
                    file.Flush();
                }
            };
        }

        public static Action<string> LogToTrace { get; } = m => Trace.WriteLine(m);

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