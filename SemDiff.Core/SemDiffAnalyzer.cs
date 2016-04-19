// Copyright (c) 2015 semdiffdotnet. Distributed under the MIT License.
// See LICENSE file or opensource.org/licenses/MIT.
using LibGit2Sharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SemDiff.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SemDiff.Core
{
    /// <summary>
    /// Diagnostic Analyzer that uses semantics to detects potential conflicts with GitHub pull requests
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SemDiffAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Diagnostics.Supported;
        internal static DataStore Store { get; } = new DataStore();

        /// <summary>
        /// Called once at session start to register actions in the analysis context.
        /// </summary>
        /// <param name="context">object that allows the assignment of callbacks</param>
        public override void Initialize(AnalysisContext context)
        {
            SetupLibGit2Sharp();
#if DEBUG
            Logger.AddHooks(Logger.LogToFile($@"C:\Users\Public\Documents\semdiff_logs_{Guid.NewGuid()}.txt"));
            Logger.Suppress(Logger.Severities.Trace);
#endif
            context.RegisterCompilationAction(OnCompilation);
        }

        private int libGit2SharpNativePathSet;

        private void SetupLibGit2Sharp()
        {
            // It is important to only set the native library path before they are accessed for the first time.
            // Otherwise exceptions will be thrown. So this code is only executed once.
            if (Interlocked.Exchange(ref libGit2SharpNativePathSet, 1) == 0)
            {
                if (Platform.OperatingSystem != OperatingSystemType.Windows)
                {
                    if (Directory.Exists(GlobalSettings.NativeLibraryPath))
                        return; //We are likely debugging, no change needed

                    //"C:\Users\crhoe\.nuget\packages\SemDiff\0.6.0-rc1\analyzers\dotnet\cs\SemDiff.Core.dll"
                    var semdiffPath = new Uri(Assembly.GetExecutingAssembly().EscapedCodeBase).LocalPath;

                    var csdir = Path.GetDirectoryName(semdiffPath);
                    var dotnetdir = Path.GetDirectoryName(csdir);
                    var analyzersdir = Path.GetDirectoryName(dotnetdir);
                    var packagedir = Path.GetDirectoryName(analyzersdir);
                    //these are placed in a nested directory to avoid possible name conflicts in the future
                    var libgit2sharpresources = Path.Combine(packagedir, "libgit2sharpresources");

                    GlobalSettings.NativeLibraryPath = Path.Combine(libgit2sharpresources, "NativeBinaries");

                    if (!Directory.Exists(GlobalSettings.NativeLibraryPath))
                        throw new NotImplementedException(nameof(LibGit2Sharp) + " native libraries could not be found");
                }
            }
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
                        return new[] { Diagnostics.AuthenticationFailure() };
                    }
                    catch (GitHubRateLimitExceededException)
                    {
                        return new[] { Diagnostics.RateLimit() };
                    }
                    catch (GitHubUrlNotFoundException ex)
                    {
                        return new[] { Diagnostics.NotGitHubRepo(ex.Message) };
                    }
                    catch (GitHubUnknownErrorException ex)
                    {
                        return new[] { Diagnostics.UnexpectedError($"Communicating with GitHub: '{ex.Message}'") };
                    }
                    catch (GitHubDeserializationException ex)
                    {
                        return new[] { Diagnostics.UnexpectedError($"Deserializing Data: '{ex.Message}'") };
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Unhandled Exception from {nameof(repo.UpdateRemoteChangesAsync)}: {ex.GetType().Name}: {ex.Message} << {ex.StackTrace} >>");
                        return new[] { Diagnostics.UnexpectedError($"Communicating with GitHub: '{ex.Message}'") };
                    }
                }

                var diagnostics = new List<Diagnostic>();
                foreach (var r in repos)
                {
                    foreach (var t in data.GetTreesForRepo(r))
                    {
                        diagnostics.AddRange(Analyze(comp.GetSemanticModel(t), r));
                    }
                }
                return diagnostics;
            }
            catch (Exception ex)
            {
                Logger.Error($"Unhandled Exception: {ex.GetType().Name}: {ex.Message} << {ex.StackTrace} >>");
                return new[] { Diagnostics.UnexpectedError("Running Analysis") };
            }
            finally
            {
                Logger.Trace($"Entering {nameof(OnCompilationAsync)}: {comp.AssemblyName}");
            }
        }

        private static Repo GetRepo(SyntaxTree tree) => Repo.GetRepoFor(tree.FilePath);

        private static IEnumerable<Diagnostic> Analyze(SemanticModel semanticModel, Repo repo)
        {
            Logger.Trace($"Entering {nameof(Analyze)}: {semanticModel?.SyntaxTree?.FilePath}");

            var fps = Analysis.ForFalsePositive(repo, semanticModel.SyntaxTree);
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