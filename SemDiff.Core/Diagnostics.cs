// Copyright (c) 2015 semdiffdotnet. Distributed under the MIT License.
// See LICENSE file or opensource.org/licenses/MIT.
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;

namespace SemDiff.Core
{
    /// <summary>
    /// This class provides our interface for reporting diagnostics using Roslyn
    /// </summary>
    public class Diagnostics
    {
        //Shared Values
        private const string Category = nameof(SemDiff); //Not sure where this shows up yet

        #region SD0001

        public const string Sd0001Id = nameof(SD0001);

        private const string Sd0001Description =
            "SemDiff detected a moved method within the local repository that " +
            "was also changed in a pull request. This could create a " +
            "False-Positive merge conflict when merging both into the master " +
            "branch. False-Positives occur when text-based tools detect a " +
            "conflict but there are no semantic differences between the " +
            "conflicting changes.";

        private const string Sd0001MessageFormat =
            "Method '{0}' was moved, but was also changed in a pull request - '{1}' (#{2})";

        private const string Sd0001Title = "Local Method Moved, Remote Method Changed";

        ///<summary>Local Method Moved, Remote Method Changed</summary>
        private static readonly DiagnosticDescriptor SD0001 =
            new DiagnosticDescriptor(Sd0001Id, Sd0001Title, Sd0001MessageFormat, Category,
                DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Sd0001Description);

        #endregion SD0001

        #region SD0002

        public const string Sd0002Id = nameof(SD0002);

        private const string Sd0002Description =
            "SemDiff detected a changed method within the local repository " +
            "that was also moved in a pull request. This could create a " +
            "False-Positive merge conflict when merging both into the master " +
            "branch. False-Positives occur when text-based tools detect a " +
            "conflict but there are no semantic differences between the " +
            "conflicting changes.";

        private const string Sd0002MessageFormat =
            "Method '{0}' was changed, but was also moved in a pull request - '{1}' (#{2})";

        private const string Sd0002Title = "Local Method Changed, Remote Method Moved";

        ///<summary>Local Method Changed, Remote Method Moved</summary>
        private static readonly DiagnosticDescriptor SD0002 =
            new DiagnosticDescriptor(Sd0002Id, Sd0002Title, Sd0002MessageFormat, Category,
                DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Sd0002Description);

        #endregion SD0002

        #region SD0003

        public const string Sd0003Id = nameof(SD0003);

        private const string Sd0003Description =
            "SemDiff detected that the base class of a class changed within " +
            "the local repository was also modified in a pull request. A " +
            "False-Negative merge conflict could be created when both are " +
            "merged into the master branch. False-Negatives occur when " +
            "text-based tools fail to detect a conflict that affects the " +
            "semantics of the application. In other words, when merging " +
            "changes the runtime behavior of the application.";

        private const string Sd0003MessageFormat =
            "The base class of '{0}' ('{1}') was changed in a pull request - '{2}' (#{3})";

        private const string Sd0003Title = "Base Class of Locally Modified Class Changed Remotely";

        ///<summary>Base Class of Locally Modified Class Changed Remotely</summary>
        private static readonly DiagnosticDescriptor SD0003 =
            new DiagnosticDescriptor(Sd0003Id, Sd0003Title, Sd0003MessageFormat, Category,
                DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Sd0003Description);

        #endregion SD0003

        #region SD0004

        public const string Sd0004Id = nameof(SD0004);

        private const string Sd0004Description =
            "SemDiff detected that changes in the local file and GitHub " +
            "indicated that the file has been either changed or renamed in a " +
            "pull request. No further analysis is done on changed or renamed " +
            "files, but a file that has been changed or renamed in another " +
            "pull request could indicate a possible conflict. A changed file " +
            "could have had its permission changed or was part of a truncated " +
            "diff (GitHub will not process all of the files in a large pull " +
            "request).";

        private const string Sd0004MessageFormat =
            "File has been modified locally as well as changed or renamed in a pull request - '{0}' (#{1})";

        private const string Sd0004Title = "Locally Changed File Changed or Renamed Remotely";

        ///<summary>Locally Changed File Changed or Renamed Remotely</summary>
        private static readonly DiagnosticDescriptor SD0004 =
            new DiagnosticDescriptor(Sd0004Id, Sd0004Title, Sd0004MessageFormat, Category,
                DiagnosticSeverity.Info, isEnabledByDefault: true, description: Sd0004Description);

        #endregion SD0004

        #region SD1001

        public const string Sd1001Id = nameof(SD1001);

        private const string Sd1001MessageFormat =
            "SemDiff Internal Error While {0} - {1}";

        private const string Sd1001Title = "SemDiff Internal Error";

        ///<summary>SemDiff Internal Error</summary>
        private static readonly DiagnosticDescriptor SD1001 =
            new DiagnosticDescriptor(Sd1001Id, Sd1001Title, Sd1001MessageFormat, Category,
                DiagnosticSeverity.Warning, isEnabledByDefault: true);

        #endregion SD1001

        #region SD2001

        public const string Sd2001Id = nameof(SD2001);

        private const string Sd2001Description =
            "SemDiff uses the GitHub API to request information about pull " +
            "requests. However, GitHub limits the number of requests to the " +
            "API using a rate limit. The rate limit can be increased by using " +
            "authentication. See the SemDiff wiki for more information.";

        ///<summary>This should be shown only when the rate limit is hit AND the user is unauthenticated
        private const string RateLimitWikiTipText =
            " (enable authentication to raise rate limit https://goo.gl/W19V7U)";

        private const string Sd2001MessageFormat =
            "SemDiff has exceeded the rate limit of {0} requests per hour imposed by GitHub.{1}";

        private const string Sd2001Title = "GitHub Rate Limit Exceeded";

        ///<summary>GitHub Rate Limit Exceeded</summary>
        private static readonly DiagnosticDescriptor SD2001 =
            new DiagnosticDescriptor(Sd2001Id, Sd2001Title, Sd2001MessageFormat, Category,
                DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Sd2001Description);

        #endregion SD2001

        #region SD2002

        public const string Sd2002Id = nameof(SD2002);

        private const string Sd2002Description =
            "The .git/config file found does not contain a GitHub remote. " +
            "The repo is likely not a GitHub repo.";

        private const string Sd2002MessageFormat =
            "Git repo found, but has no GitHub remote - {0}";

        private const string Sd2002Title = "GitHub Remote not Found";

        ///<summary>GitHub Remote not Found</summary>
        private static readonly DiagnosticDescriptor SD2002 =
            new DiagnosticDescriptor(Sd2002Id, Sd2002Title, Sd2002MessageFormat, Category,
                DiagnosticSeverity.Info, isEnabledByDefault: true, description: Sd2002Description);

        #endregion SD2002

        #region SD2003

        public const string Sd2003Id = nameof(SD2003);

        private const string Sd2003MessageFormat =
            "GitHub Authentication - {0}";

        private const string Sd2003Title = "GitHub Authentication Problem";

        ///<summary>GitHub Authentication Problem</summary>
        private static readonly DiagnosticDescriptor SD2003 =
            new DiagnosticDescriptor(Sd2003Id, Sd2003Title, Sd2003MessageFormat, Category,
                DiagnosticSeverity.Warning, isEnabledByDefault: true);

        #endregion SD2003

        /// <summary>
        /// The Diagnostics we support, provided for the SupportedDiagnostics property of DiagnosticAnalyzer
        /// </summary>
        public static ImmutableArray<DiagnosticDescriptor> Supported { get; } =
            ImmutableArray.Create(SD0001, SD0002, SD0003, SD0004, SD1001, SD2001, SD2002, SD2003);

        /// <summary>
        /// Converts `DetectedFalsePositive`s to Diagnostics (the class provided by Roslyn) and
        /// sends them to the function provided
        /// </summary>
        /// <param name="fps">the object that contains the information to populate the error message</param>
        public static Diagnostic Convert(DetectedFalsePositive fps)
        {
            if (fps.ConflictType == DetectedFalsePositive.ConflictTypes.LocalMethodRemoved)
            {
                return Diagnostic.Create(SD0001, fps.Location, fps.MethodName, fps.RemoteChange.Title, fps.RemoteChange.Number);
            }
            else if (fps.ConflictType == DetectedFalsePositive.ConflictTypes.LocalMethodChanged)
            {
                return Diagnostic.Create(SD0002, fps.Location, fps.MethodName, fps.RemoteChange.Title, fps.RemoteChange.Number);
            }
            throw new NotImplementedException(fps.ConflictType.ToString());
        }

        /// <summary>
        /// Converts `DetectedFalseNegative`s to Diagnostics (the class provided by Roslyn) and
        /// sends them to the function provided
        /// </summary>
        /// <param name="fns">the object that contains the information to populate the error message</param>
        public static Diagnostic Convert(DetectedFalseNegative fns)
        {
            return Diagnostic.Create(SD0003, fns.Location, fns.DerivedTypeName, fns.BaseTypeName, fns.RemoteChange.Title, fns.RemoteChange.Number);
        }

        /// <summary>
        /// Returns a diagnostic that represents a message that a repo was found but could a GitHub
        /// url was not found.
        /// </summary>
        /// <param name="path">The exception message that should contain the path</param>
        public static Diagnostic NotGitHubRepo(string path)
        {
            return Diagnostic.Create(SD2002, Location.None, path);
        }

        /// <summary>
        /// Returns a diagnostic that represents a friendly message that the rate limit has been
        /// exceeded and a link to our documentation.
        /// </summary>
        /// <param name="rateLimit">Maximum rate limit value</param>
        /// <param name="authenticated">true if user is authenticated</param>
        public static Diagnostic RateLimit(int rateLimit, bool authenticated)
        {
            return Diagnostic.Create(SD2001, Location.None, rateLimit, authenticated ? "" : RateLimitWikiTipText);
        }

        /// <summary>
        /// Returns a Diagnostic that represents authentication failure
        /// </summary>
        /// <param name="githubMessage">Message returned by the github api</param>
        public static Diagnostic AuthenticationFailure(string githubMessage)
        {
            return Diagnostic.Create(SD2003, Location.None, githubMessage);
        }

        /// <summary>
        /// Should create a diagnostic that represents something like this: Unexpected Error
        /// Occurred while ((verbNounPhrase))
        /// </summary>
        /// <param name="verbNounPhrase">
        /// Something like: Washing the Car, Mowing the Lawn, or Burning out a Fuse up here Alone
        /// </param>
        /// <param name="message">
        /// Exception message
        /// </param>
        public static Diagnostic UnexpectedError(string verbNounPhrase, string message)
        {
            return Diagnostic.Create(SD1001, Location.None, verbNounPhrase, message);
        }
    }
}