using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SemDiff.Core.Exceptions;
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
        internal static DataStore Store { get; } = new DataStore();

        //Called once per solution, to provide us an oportunity to assign callbacks
        public override void Initialize(AnalysisContext context)
        {
#if DEBUG
            Logger.AddHooks(Logger.LogToFile($@"C:\Users\Public\Documents\semdiff_logs_{Guid.NewGuid()}.txt"));
            Logger.Suppress(Logger.Severities.Trace);
#endif
            context.RegisterCompilationAction(OnCompilation);
        }

        private static void OnCompilation(CompilationAnalysisContext context)
        {
            var diags = OnCompilationAsync(context.Compilation).Result;
            foreach (var d in diags)
            {
                context.ReportDiagnostic(d);
            }
        }

        private async static Task<IEnumerable<Diagnostic>> OnCompilationAsync(Compilation comp)
        {
            Logger.Trace($"Entering {nameof(OnCompilationAsync)}: {comp.AssemblyName}");
            IEnumerable<Diagnostic> diags;
            try
            {
                var data = Store.InterlockedAddOrUpdate(comp.AssemblyName, comp.SyntaxTrees, GetRepo);
                var repos = data.Repos;
                foreach (var repo in repos)
                {
                    try
                    {
                        await repo.UpdateRemoteChangesAsync();
                    }
                    catch (GitHubAuthenticationFailureException)
                    {
                        diags = new[] { Diagnostics.AuthenticationFailure() };
                    }
                    catch (GitHubRateLimitExceededException)
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
                    catch (GitHubDeserializationException)
                    {
                        diags = new[] { Diagnostics.UnexpectedError("Deserializing Data") };
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Unhandled Exception from {nameof(repo.UpdateRemoteChangesAsync)}: {ex.GetType().Name}: {ex.Message} << {ex.StackTrace} >>");
                        diags = new[] { Diagnostics.UnexpectedError("Communicating with GitHub") };
                    }
                }

                diags = repos.SelectMany(r => data.GetTreesForRepo(r).Select(t => new { t, r }))
                                                 .SelectMany(tr => Analyze(comp.GetSemanticModel(tr.t), tr.r));
                return diags;
            }
            catch (Exception ex)
            {
                Logger.Error($"Unhandled Exception: {ex.GetType().Name}: {ex.Message} << {ex.StackTrace} >>");
                diags = new[] { Diagnostics.UnexpectedError("Running Analysis") };
            }
            Logger.Trace($"Entering {nameof(OnCompilationAsync)}: {comp.AssemblyName}");
            return diags;
        }

        private static Repo GetRepo(SyntaxTree tree) => Repo.GetRepoFor(tree.FilePath);

        private static IEnumerable<Diagnostic> Analyze(SemanticModel semanticModel, Repo repo)
        {
            Logger.Trace($"Entering {nameof(Analyze)}: {semanticModel?.SyntaxTree?.FilePath}");
            var filePath = semanticModel.SyntaxTree.FilePath;

            var fps = Analysis.ForFalsePositive(repo, semanticModel.SyntaxTree, filePath);
            var fns = Analysis.ForFalseNegative(repo, semanticModel);
#if DEBUG
            fps = fps.Log(fp => Logger.Info($"{fp}"));
            fns = fns.Log(fn => Logger.Info($"{fn}"));
#endif
            var diags = fps.Select(Diagnostics.Convert).Concat(fns.Select(Diagnostics.Convert));

            Logger.Trace($"Exiting {nameof(Analyze)}: {semanticModel?.SyntaxTree?.FilePath}");
            return diags;
        }
    }
}