using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace SemDiff.Core
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SemDiffAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Diagnostics.Supported;

        //Called once per solution, to provide us an oportunity to assign callbacks
        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSemanticModelAction(OnSemanticModel);
        }

        /// <summary>
        /// A call back set in initialize that is called when ever a file is compiled
        /// </summary>
        /// <param name="context">context provided by roslyn that contains the SyntaxTree, SemanticModel, FilePath, etc.</param>
        private static void OnSemanticModel(SemanticModelAnalysisContext context)
        {
            try
            {
                var filePath = context.SemanticModel.SyntaxTree.FilePath;
                var repo = Repo.GetRepoFor(filePath);
                if (repo != null)
                {
                    var fps = Analysis.ForFalsePositive(repo, context.SemanticModel.SyntaxTree, filePath);
                    Diagnostics.Report(fps, context.ReportDiagnostic);

                    var fns = Analysis.ForFalseNegative(repo, context.SemanticModel);
                    Diagnostics.Report(fns, context.ReportDiagnostic);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Unhandled Exception: {ex.GetType().Name}: {ex.Message} << {ex.StackTrace} >>");
            }
        }
    }
}