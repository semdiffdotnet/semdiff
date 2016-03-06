using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SemDiff.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
#if DEBUG
            Logger.AddHooks(Logger.LogToFile($@"C:\Users\Public\Documents\semdiff_logs_{Guid.NewGuid()}.txt"));
#endif
            context.RegisterSemanticModelAction(OnSemanticModel);
        }

        /// <summary>
        /// A call back set in initialize that is called when ever a file is compiled
        /// </summary>
        /// <param name="context">context provided by roslyn that contains the SyntaxTree, SemanticModel, FilePath, etc.</param>
        private static void OnSemanticModel(SemanticModelAnalysisContext context)
        {
            var semanticModel = context.SemanticModel;
            //This should be only blocking call in this project, this is needed because Roslyn
            //does *not* provide a way to assign a callback that returns Task
            var diags = AnalyzeAsync(semanticModel).Result;

            foreach (var d in diags)
            {
                context.ReportDiagnostic(d);
            }
        }

        private static async Task<IEnumerable<Diagnostic>> AnalyzeAsync(SemanticModel semanticModel)
        {
            var timer = Stopwatch.StartNew();
            Logger.Debug($"Entering {nameof(AnalyzeAsync)}: {semanticModel?.SyntaxTree?.FilePath}");
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
            catch (GitHubAuthenticationFailureException ex)
            {
                diags = new[] { Diagnostics.AuthenticationFailure() };
            }
            catch (GitHubRateLimitExceededException ex)
            {
                diags = new[] { Diagnostics.RateLimit() };
            }
            catch (GitHubUrlNotFoundException ex)
            {
                diags = new[] { Diagnostics.NotGitHubRepo(ex.Message) };
            }
            catch (GitHubUnknownErrorException ex)
            {
                diags = new[] { Diagnostics.UnexpectedError($"Communicating with GitHub: '{ex.Message}'") };
            }
            catch (GitHubDeserializationException ex)
            {
                diags = new[] { Diagnostics.UnexpectedError("Deserializing Data") };
            }
            catch (Exception ex)
            {
                diags = new[] { Diagnostics.UnexpectedError("Performing Analysis") };
                Logger.Error($"Unhandled Exception: {ex.GetType().Name}: {ex.Message} << {ex.StackTrace} >>");
            }

            Logger.Debug($"Exiting {nameof(AnalyzeAsync)}: {semanticModel?.SyntaxTree?.FilePath} in {timer.Elapsed}");
            return diags;
        }
    }
}