using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;

namespace SemDiff.Core
{
    /// <summary>
    /// This class provides our interface for reporting diagnostics using roslyn
    /// </summary>
    public class Diagnostics
    {
        //Shared Values
        private const string Category = "Conflicts"; //Not sure where this shows up yet

        //False-Negative
        public const string FalseNegativeDiagnosticId = "SemDiffFN"; //Like the error code (just like missing semicolon is CS1002)

        private const string FalseNegativeDescription = "False-Negatives occur text based tools fail to detect a conflict that affects the semantics of the application. i.e., when the semantics of a dependant item (a called method, a base class, a variable used) has been changed in a way that changes the runtime of the application when the changes are merged.";

        private const string FalseNegativeMessageFormat = "False-Negatives for type '{0}' between '{1}' and '({2})[{3}]'";

        private const string FalseNegativeTitle = "Possible False-Negative condition detected";

        //False-Positive
        public const string FalsePositiveDiagnosticId = "SemDiffFP";

        private const string FalsePositiveDescription = "False-Positives occur text based tools detect a conflict that affects the semantics of the application. i.e., when merge conflicts may occur, but there are no semantic differences between the conflicting changes.";

        private const string FalsePositiveMessageFormat = "False-Positive between '{0}' and '({1})[{2}]'";

        private const string FalsePositiveTitle = "Possible False-Positive condition detected"; //Not sure where this comes up yet

        //InternalError
        public const string InternalErrorDiagnosticId = "SemDiff";

        private const string InternalErrorMessageFormat = "SemDiff Error: {0}";

        private const string InternalErrorTitle = "SemDiff Internal Error";

        //similar to fp
        private static readonly DiagnosticDescriptor FalseNegative = new DiagnosticDescriptor(FalseNegativeDiagnosticId, FalseNegativeTitle, FalseNegativeMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: FalseNegativeDescription);

        //0 is name of file, 1 is title of pull request, 2 is url of pull request //Actual error message text (formatable string)
        //There is a option to show this in the error list for more info
        private static readonly DiagnosticDescriptor FalsePositive = new DiagnosticDescriptor(FalsePositiveDiagnosticId, FalsePositiveTitle, FalsePositiveMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: FalsePositiveDescription);

        private static readonly DiagnosticDescriptor InternalError = new DiagnosticDescriptor(InternalErrorDiagnosticId, InternalErrorTitle, InternalErrorMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        /// <summary>
        /// The Diagnostics we support, provided for the SupportedDiagnostics property of DiagnosticAnalyzer
        /// </summary>
        public static ImmutableArray<DiagnosticDescriptor> Supported { get; } = ImmutableArray.Create(FalsePositive, FalseNegative, InternalError);

        /// <summary>
        /// Converts `DetectedFalsePositive`s to Diagnostics (the class provided by Roslyn) and sends them to the function provided
        /// </summary>
        /// <param name="fps">todo: describe fps parameter on Convert</param>
        public static Diagnostic Convert(DetectedFalsePositive fps)
        {
            return Diagnostic.Create(FalsePositive, fps.Location, fps.RemoteFile.Filename, fps.RemoteChange.Title, fps.RemoteChange.Url);
        }

        /// <summary>
        /// Converts `DetectedFalseNegative`s to Diagnostics (the class provided by Roslyn) and sends them to the function provided
        /// </summary>
        /// <param name="fps">todo: describe fps parameter on Convert</param>
        public static Diagnostic Convert(DetectedFalseNegative fns)
        {
            return Diagnostic.Create(FalseNegative, fns.Location, fns.TypeName, fns.RemoteFile.Filename, fns.RemoteChange.Title, fns.RemoteChange.Url);
        }

        /// <summary>
        /// Returns a diagnostic that represents a message that a repo was found but could a github url was not found.
        /// </summary>
        /// <param name="message">The exception message that should contain the path</param>
        /// <returns></returns>
        internal static Diagnostic NotGitHubRepo(string message)
        {
            return Diagnostic.Create(InternalError, Location.None, message);
        }

        /// <summary>
        /// Returns a diagnostic that represents a friendly message that the rate limit has been exceeded and a link to our documention.
        /// </summary>
        /// <returns></returns>
        internal static Diagnostic RateLimit()
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
        /// Should create a diagnostic that represents something like this: Unexpected Error Occured while ((verbNounPhrase))
        /// </summary>
        /// <param name="verbNounPhrase">Something like: Washing the Car, Mowing the Lawn, or Burning out a Fuze up here Alone</param>
        /// <returns></returns>
        internal static Diagnostic UnexpectedError(string verbNounPhrase)
        {
            return Diagnostic.Create(InternalError, Location.None, $"Unknown Error {verbNounPhrase}");
        }
    }
}