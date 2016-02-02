using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;

namespace SemDiff.Core
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SemDiffAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get //This will call into our message component to get the kinds it will produce
            {
                throw new NotImplementedException();
            }
        }

        //Called once per solution, to provide us an oportunity to assign callbacks
        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxTreeAction(OnSyntaxTree);
            context.RegisterSemanticModelAction(OnSemanticModel);
        }

        private static void OnSyntaxTree(SyntaxTreeAnalysisContext obj)
        {
            var filePath = obj.Tree.FilePath;
            var repo = Repo.GetRepoFor(filePath);
            if (repo == null)
                throw new NotImplementedException(); //This is where the false positive detection will be called
        }

        private static void OnSemanticModel(SemanticModelAnalysisContext obj)
        {
            var filePath = obj.SemanticModel.SyntaxTree.FilePath;
            var repo = Repo.GetRepoFor(filePath);
            if (repo == null)
                throw new NotImplementedException(); //This is where the false negative detection will be called
        }
    }
}