using Microsoft.CodeAnalysis;
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

        //similar to fp
        private static readonly DiagnosticDescriptor FalseNegative = new DiagnosticDescriptor(FalseNegativeDiagnosticId, FalseNegativeTitle, FalseNegativeMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: FalseNegativeDescription);

        //0 is name of file, 1 is title of pull request, 2 is url of pull request //Actual error message text (formatable string)
        //There is a option to show this in the error list for more info
        private static readonly DiagnosticDescriptor FalsePositive = new DiagnosticDescriptor(FalsePositiveDiagnosticId, FalsePositiveTitle, FalsePositiveMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: FalsePositiveDescription);

        /// <summary>
        /// The Diagnostics we support, provided for the SupportedDiagnostics property of DiagnosticAnalyzer
        /// </summary>
        public static ImmutableArray<DiagnosticDescriptor> Supported { get; } = ImmutableArray.Create(FalsePositive, FalseNegative);

        /// <summary>
        /// Converts `DetectedFalseNegative`s to Diagnostics (the class provided by Roslyn) and sends them to the function provided
        /// </summary>
        /// <param name="fps">todo: describe fps parameter on Convert</param>
        public static Diagnostic Convert(DetectedFalseNegative fps)
        {
            //TODO: NotImplimented
            return Diagnostic.Create(FalseNegative, Location.None, "FileName", @"dir\dir\FileName.cs", "My pull request title", "https://github.com/semdiffdotnet/semdiff/pull/33");
        }

        /// <summary>
        /// Converts `DetectedFalsePositive`s to Diagnostics (the class provided by Roslyn) and sends them to the function provided
        /// </summary>
        /// <param name="fps">todo: describe fps parameter on Convert</param>
        public static Diagnostic Convert(DetectedFalsePositive fps)
        {
            //TODO: NotImplimented
            return Diagnostic.Create(FalsePositive, Location.None, @"dir\dir\FileName.cs", "My pull request title", "https://github.com/semdiffdotnet/semdiff/pull/33");
        }
    }
}