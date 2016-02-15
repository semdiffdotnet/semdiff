using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

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
            var semanticModel = context.SemanticModel;
            var diags = AnalyzeAsync(semanticModel).Result;

            foreach (var d in diags)
            {
                context.ReportDiagnostic(d);
            }
        }

        private static async Task<IEnumerable<Diagnostic>> AnalyzeAsync(SemanticModel semanticModel)
        {
            var diags = Enumerable.Empty<Diagnostic>();
            try
            {
                var filePath = semanticModel.SyntaxTree.FilePath;
                var repo = Repo.GetRepoFor(filePath);
                if (repo != null)
                {
                    await repo.UpdateRemoteChangesAsync();
                    var fps = Analysis.ForFalsePositive(repo, semanticModel.SyntaxTree, filePath);
                    var fns = Analysis.ForFalseNegative(repo, semanticModel);
                    diags = fns.Select(Diagnostics.Convert).Concat(fns.Select(Diagnostics.Convert));
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Unhandled Exception: {ex.GetType().Name}: {ex.Message} << {ex.StackTrace} >>");
            }

            return diags;
        }
    }
}