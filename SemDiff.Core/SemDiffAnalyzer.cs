// Copyright (c) 2015 semdiffdotnet. Distributed under the MIT License.
// See LICENSE file or opensource.org/licenses/MIT.
using LibGit2Sharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SemDiff.Core.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
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
        private static readonly ConcurrentDictionary<string, Repo> _treePathLookup = new ConcurrentDictionary<string, Repo>();
        private static int libGit2SharpNativePathSet;
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Diagnostics.Supported;

        /// <summary>
        /// Called once at session start to register actions in the analysis context.
        /// </summary>
        /// <param name="context">object that allows the assignment of callbacks</param>
        public override void Initialize(AnalysisContext context)
        {
#if DEBUG
            Logger.AddHooks(Logger.LogToFile($@"C:\Users\Public\Documents\semdiff_logs_{Guid.NewGuid()}.txt"));
            Logger.Suppress();
#endif

            SetupLibGit2Sharp();
            context.RegisterCompilationAction(OnCompilation);
        }

        private static void SetupLibGit2Sharp()
        {
            // It is important to only set the native library path before they are accessed for the first time.
            // Otherwise exceptions will be thrown. So this code is only executed once.
            if (Interlocked.Exchange(ref libGit2SharpNativePathSet, 1) == 0)
            {
                if (Platform.OperatingSystem == OperatingSystemType.Windows)
                {
                    if (Directory.Exists(GlobalSettings.NativeLibraryPath))
                        return; //We are likely debugging, no change needed

                    //"C:\Users\crhoe\.nuget\packages\SemDiff\0.6.0-rc1\analyzers\dotnet\cs\SemDiff.Core.dll"
                    var semdiffPath = new Uri(Assembly.GetExecutingAssembly().EscapedCodeBase).LocalPath;
                    Logger.Debug(nameof(semdiffPath) + ": " + semdiffPath);

                    var containingdir = Path.GetDirectoryName(semdiffPath);

                    var zipFile = Path.Combine(containingdir, "NativeBinaries.zip");
                    File.WriteAllBytes(zipFile, NativeDlls.NativeBinaries);
                    Directory.CreateDirectory(GlobalSettings.NativeLibraryPath);
                    ZipFile.ExtractToDirectory(zipFile, GlobalSettings.NativeLibraryPath);

                    Logger.Debug(nameof(GlobalSettings.NativeLibraryPath) + ": " + GlobalSettings.NativeLibraryPath);
                    if (!Directory.Exists(GlobalSettings.NativeLibraryPath))
                        throw new NotImplementedException(nameof(LibGit2Sharp) + " native libraries could not be found");
                }
            }
        }

        private static Repo GetRepo(SyntaxTree tree)
        {
            return _treePathLookup.AddOrUpdate(tree.FilePath,
                p => Repo.GetRepoFor(p),
                (p, o) => o ?? Repo.GetRepoFor(p)); //Always check again if null
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
                var data = comp.GetRepoAndTrees(GetRepo);
                foreach (var repo in data.Keys)
                {
                    try
                    {
                        await repo.UpdateRemoteChangesAsync();
                    }
                    catch (GitHubAuthenticationFailureException ex)
                    {
                        return new[] { Diagnostics.AuthenticationFailure(ex.GithubMessage) };
                    }
                    catch (GitHubRateLimitExceededException ex)
                    {
                        return new[] { Diagnostics.RateLimit(ex.Limit, ex.Authenticated) };
                    }
                    catch (GitHubUrlNotFoundException ex)
                    {
                        return new[] { Diagnostics.NotGitHubRepo(ex.Path) };
                    }
                    catch (GitHubUnknownErrorException ex)
                    {
                        return new[] { Diagnostics.UnexpectedError($"Communicating with GitHub", ex.Message) };
                    }
                    catch (GitHubDeserializationException ex)
                    {
                        return new[] { Diagnostics.UnexpectedError($"Deserializing Data", ex.Message) };
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Unhandled Exception from {nameof(repo.UpdateRemoteChangesAsync)}: {ex.GetType().Name}: {ex.Message} << {ex.StackTrace} >>");
                        return new[] { Diagnostics.UnexpectedError($"Communicating with GitHub", ex.Message) };
                    }
                }

                var diagnostics = new List<Diagnostic>();
                foreach (var rt in data)
                {
                    var repo = rt.Key;
                    foreach (var t in rt.Value) //Foreach tree
                    {
                        diagnostics.AddRange(Analyze(comp.GetSemanticModel(t), repo));
                    }
                }
                return diagnostics;
            }
            catch (Exception ex)
            {
                Logger.Error($"Unhandled Exception: {ex.GetType().Name}: {ex.Message} << {ex.StackTrace} >>");
                return new[] { Diagnostics.UnexpectedError("Running Analysis", ex.Message) };
            }
            finally
            {
                Logger.Trace($"Entering {nameof(OnCompilationAsync)}: {comp.AssemblyName}");
            }
        }

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