using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace SemDiff.Core
{
    /// <summary>
    /// This class provides our interface for reporting diagnostics using roslyn
    /// </summary>
    public class Diagnostics
    {
        /// <summary>
        /// The Diagnostics we support, provided for the SupportedDiagnostics property of DiagnosticAnalyzer
        /// </summary>
        public static ImmutableArray<DiagnosticDescriptor> Supported { get; internal set; }

        /// <summary>
        /// Converts `DetectedFalsePositive`s to Diagnostics (the class provided by Roslyn) and sends them to the function provided
        /// </summary>
        public static void Report(IEnumerable<DetectedFalsePositive> fps, Action<Diagnostic> reporter)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Converts `DetectedFalseNegative`s to Diagnostics (the class provided by Roslyn) and sends them to the function provided
        /// </summary>
        public static void Report(IEnumerable<DetectedFalseNegative> fps, Action<Diagnostic> reporter)
        {
            throw new NotImplementedException();
        }
    }
}