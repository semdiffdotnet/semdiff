using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;

namespace SemDiff.Core
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SemDiffAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Diagnostics.Supported;

        //Called once per solution, to provide us an oportunity to assign callbacks
        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxTreeAction(OnSyntaxTree);
            context.RegisterSemanticModelAction(OnSemanticModel);
        }

        private static void OnSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            var filePath = context.Tree.FilePath;
            var repo = Repo.GetRepoFor(filePath);
            if (repo == null)
            {
                var fps = Analysis.ForFalsePositive(repo, context.Tree, filePath);
                Diagnostics.Report(fps, context.ReportDiagnostic);
            }
        }

        private static void OnSemanticModel(SemanticModelAnalysisContext context)
        {
            var filePath = context.SemanticModel.SyntaxTree.FilePath;
            var repo = Repo.GetRepoFor(filePath);
            if (repo == null)
            {
                var fns = Analysis.ForFalseNegative(repo, context.SemanticModel);
                Diagnostics.Report(fns, context.ReportDiagnostic);
            }
        }
    }
}