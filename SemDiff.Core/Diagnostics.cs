// Copyright (c) 2015 semdiffdotnet. Distributed under the MIT License.
// See LICENSE file or opensource.org/licenses/MIT.
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace SemDiff.Core
{
    /// <summary>
    /// This class provides our interface for reporting diagnostics using Roslyn
    /// </summary>
    public class Diagnostics
    {
        //Shared Values
        private const string Category = "Conflicts"; //Not sure where this shows up yet

        #region False-Positive

        public const string FalsePositiveDiagnosticId = "SemDiffFP";

        private const string FalsePositiveDescription = "False-Positives occur text based tools detect a conflict that affects the semantics of the application. i.e., when merge conflicts may occur, but there are no semantic differences between the conflicting changes.";

        private const string FalsePositiveMessageFormat = "False-Positive between '{0}' and '({1})[{2}]'";

        private const string FalsePositiveTitle = "Possible False-Positive condition detected";

        #endregion False-Positive

        #region False-Negative

        public const string FalseNegativeDiagnosticId = "SemDiffFN"; //Like the error code (just like missing semicolon is CS1002)

        private const string FalseNegativeDescription = "False-Negatives occur text based tools fail to detect a conflict that affects the semantics of the application. i.e., when the semantics of a dependant item (a called method, a base class, a variable used) has been changed in a way that changes the runtime of the application when the changes are merged.";

        private const string FalseNegativeMessageFormat = "False-Negatives for type '{0}' between '{1}' and '({2})[{3}]'";

        private const string FalseNegativeTitle = "Possible False-Negative condition detected";

        #endregion False-Negative

        #region InternalError

        public const string InternalErrorDiagnosticId = nameof(SemDiff);

        private const string InternalErrorMessageFormat = "SemDiff Error - {0}";

        private const string InternalErrorTitle = "SemDiff Internal Error";

        #endregion InternalError

        private static readonly DiagnosticDescriptor FalsePositive = new DiagnosticDescriptor(FalsePositiveDiagnosticId, FalsePositiveTitle, FalsePositiveMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: FalsePositiveDescription);
        private static readonly DiagnosticDescriptor FalseNegative = new DiagnosticDescriptor(FalseNegativeDiagnosticId, FalseNegativeTitle, FalseNegativeMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: FalseNegativeDescription);
        private static readonly DiagnosticDescriptor InternalError = new DiagnosticDescriptor(InternalErrorDiagnosticId, InternalErrorTitle, InternalErrorMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        /// <summary>
        /// The Diagnostics we support, provided for the SupportedDiagnostics property of DiagnosticAnalyzer
        /// </summary>
        public static ImmutableArray<DiagnosticDescriptor> Supported { get; } = ImmutableArray.Create(FalsePositive, FalseNegative, InternalError);

        /// <summary>
        /// Converts `DetectedFalsePositive`s to Diagnostics (the class provided by Roslyn) and
        /// sends them to the function provided
        /// </summary>
        /// <param name="fps">the object that contains the information to populate the error message</param>
        public static Diagnostic Convert(DetectedFalsePositive fps)
        {
            return Diagnostic.Create(FalsePositive, fps.Location, fps.RemoteFile.Filename, fps.RemoteChange.Title, fps.RemoteChange.Url);
        }

        /// <summary>
        /// Converts `DetectedFalseNegative`s to Diagnostics (the class provided by Roslyn) and
        /// sends them to the function provided
        /// </summary>
        /// <param name="fns">the object that contains the information to populate the error message</param>
        public static Diagnostic Convert(DetectedFalseNegative fns)
        {
            return Diagnostic.Create(FalseNegative, fns.Location, fns.TypeName, fns.RemoteFile.Filename, fns.RemoteChange.Title, fns.RemoteChange.Url);
        }

        /// <summary>
        /// Returns a diagnostic that represents a message that a repo was found but could a GitHub
        /// url was not found.
        /// </summary>
        /// <param name="message">The exception message that should contain the path</param>
        public static Diagnostic NotGitHubRepo(string message)
        {
            return Diagnostic.Create(InternalError, Location.None, message);
        }

        /// <summary>
        /// Returns a diagnostic that represents a friendly message that the rate limit has been
        /// exceeded and a link to our documentation.
        /// </summary>
        public static Diagnostic RateLimit()
        {
            return Diagnostic.Create(InternalError, Location.None, "The Rate Limit has been exceeded, please check documentation for information on how to increase the limit");
        }

        /// <summary>
        /// Returns a Diagnostic that represents authentication failure
        /// </summary>
        public static Diagnostic AuthenticationFailure()
        {
            return Diagnostic.Create(InternalError, Location.None, "GitHub Authentication Error, please check your credentials");
        }

        /// <summary>
        /// Should create a diagnostic that represents something like this: Unexpected Error
        /// Occurred while ((verbNounPhrase))
        /// </summary>
        /// <param name="verbNounPhrase">
        /// Something like: Washing the Car, Mowing the Lawn, or Burning out a Fuse up here Alone
        /// </param>
        public static Diagnostic UnexpectedError(string verbNounPhrase)
        {
            return Diagnostic.Create(InternalError, Location.None, $"Unknown Error {verbNounPhrase}");
        }
    }
}