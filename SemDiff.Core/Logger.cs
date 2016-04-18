// Copyright (c) 2015 semdiffdotnet. Distributed under the MIT License.
// See LICENSE file or opensource.org/licenses/MIT.
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using SysTrace = System.Diagnostics.Trace;

namespace SemDiff.Core
{
    /// <summary>
    /// Used for logging important events, uses the Caller* attributes to collect more data than a
    /// simple print. A date is also added to the output along with the severity (Debug, Info, Error)
    /// matching the function called
    /// </summary>
    /// <example>Logger.Error(exception.Message)</example>
    internal static class Logger
    {
        public static ImmutableArray<Action<string>> Hooks { get; private set; } = ImmutableArray<Action<string>>.Empty;
        private static bool enabled;
        private static Severities[] enabledSeverities = Array.Empty<Severities>();

        private static void Log(Severities severity, string message, string callerFilePath, string callerMemberName, int callerLineNumber)
        {
            if (enabled && enabledSeverities.Contains(severity))
            {
                DoHooks($@"{severity.ToString().ToUpper().PadRight(5)}: {DateTime.Now.ToString("s")} | ""{message}"" @ {Path.GetFileName(callerFilePath)}:{callerLineNumber} {callerMemberName}");
            }
        }

        /// <summary>
        /// Suppress the supplied severities from being logged. Every time this command is run it will
        /// replace the previously suppressed commands
        /// </summary>
        /// <param name="supressed">Severity levels to suppress</param>
        public static void Suppress(params Severities[] supressed)
        {
            enabledSeverities = Enum.GetValues(typeof(Severities))
                    .Cast<Severities>()
                    .Where(s => !supressed.Contains(s))
                    .ToArray();
        }

        /// <summary>
        /// Add a way to log each message
        /// </summary>
        /// <param name="hook">a function that will take the string to log and perform some action with it</param>
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

        /// <summary>
        /// Produces a Hook that will log to file
        /// </summary>
        /// <param name="fileName">path to the log file</param>
        /// <returns>an action that can be passed to AddHooks</returns>
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

        public static Action<string> LogToTrace { get; } = m => SysTrace.WriteLine(m); //SysTrace is alias for Trace

        /// <summary>
        /// Log a event with Trace severity, such as logging the entry and exit of functions or other very verbose information
        /// </summary>
        /// <param name="m">string that will be logged</param>
        /// <param name="cfp">supplied by compiler</param>
        /// <param name="cmn">supplied by compiler</param>
        /// <param name="cln">supplied by compiler</param>
        public static void Trace(string m, [CallerFilePath] string cfp = "", [CallerMemberName] string cmn = "", [CallerLineNumber] int cln = 0) => Log(Severities.Trace, m, cfp, cmn, cln);

        /// <summary>
        /// Log a event with Debug severity, such as hitting a special condition or location in the program, can be used for "printf" debugging
        /// </summary>
        /// <param name="m">string that will be logged</param>
        /// <param name="cfp">supplied by compiler</param>
        /// <param name="cmn">supplied by compiler</param>
        /// <param name="cln">supplied by compiler</param>
        public static void Debug(string m, [CallerFilePath] string cfp = "", [CallerMemberName] string cmn = "", [CallerLineNumber] int cln = 0) => Log(Severities.Debug, m, cfp, cmn, cln);

        /// <summary>
        /// Log a event with Info severity, such as an important event like a password change
        /// </summary>
        /// <param name="m">string that will be logged</param>
        /// <param name="cfp">supplied by compiler</param>
        /// <param name="cmn">supplied by compiler</param>
        /// <param name="cln">supplied by compiler</param>
        public static void Info(string m, [CallerFilePath] string cfp = "", [CallerMemberName] string cmn = "", [CallerLineNumber] int cln = 0) => Log(Severities.Info, m, cfp, cmn, cln);

        /// <summary>
        /// Log a event with Error severity, such as exceptions or unhandled conditions.
        /// </summary>
        /// <param name="m">string that will be logged</param>
        /// <param name="cfp">supplied by compiler</param>
        /// <param name="cmn">supplied by compiler</param>
        /// <param name="cln">supplied by compiler</param>
        public static void Error(string m, [CallerFilePath] string cfp = "", [CallerMemberName] string cmn = "", [CallerLineNumber] int cln = 0) => Log(Severities.Error, m, cfp, cmn, cln);

        /// <summary>
        /// A Severity is a tag that is added to the error message. Levels of errors that get
        /// progressively worse.
        /// </summary>
        public enum Severities
        {
            Trace,
            Debug,
            Info,
            Error,
        }
    }
}