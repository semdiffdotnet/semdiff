using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SemDiff.Core
{
    /// <summary>
    /// Class that handles all higher level analysis like finding FalsePositives and FalseNegatives
    /// </summary>
    public class Analysis
    {
        /// <summary>
        /// Given a Repo and a Tree find any possible FalsePositives
        /// </summary>
        public static IEnumerable<DetectedFalsePositive> ForFalsePositive(Repo repo, SyntaxTree tree, string filePath)
        {
            var pulls = GetPulls(repo, filePath);
            foreach (var fp in pulls)
            {
                var f = fp.Item1;
                var p = fp.Item2;

                var conflicts = Diff3.Compare(f.Base, tree, f.File);
                var locs = GetRemovedMethods(conflicts.Local);
                var rems = GetRemovedMethods(conflicts.Remote);
                foreach (var c in conflicts.Conflicts.Where(con => con.Ancestor.Node is MethodDeclarationSyntax))
                {
                    var orig = (MethodDeclarationSyntax)c.Ancestor.Node;
                    if (orig != null && c.Local.Span.Length == 0)
                    {
                        var changed = c.Remote.Node as MethodDeclarationSyntax;
                        var methodName = changed.Identifier.Text;
                        if (changed != null)
                        {
                            foreach (var loc in locs.Where(method => method.Identifier.Text == methodName))
                            {
                            }
                        }
                    }
                }
            }
            throw new NotImplementedException();
        }

        private static List<MethodDeclarationSyntax> GetRemovedMethods(List<Diff> diffs)
        {
            return diffs
                     .Where(diff => string.IsNullOrWhiteSpace(diff.Ancestor.Text))
                     .Select(diff => diff.Changed.Node as MethodDeclarationSyntax)
                     .Where(node => node != null).ToList();
        }

        /// <summary>
        /// Given a Repo and a Semantic Model find any possible FalseNegatives
        /// </summary>
        public static IEnumerable<DetectedFalseNegative> ForFalseNegative(Repo repo, SemanticModel semanticModel)
        {
            var baseClassPath = ""; //TODO: find using semantic model
            var pulls = GetPulls(repo, baseClassPath);
            throw new NotImplementedException();
        }

        internal static string GetRelativePath(string localDirectory, string filePath)
        {
            var local = Path.GetFullPath(localDirectory);
            var file = Path.GetFullPath(filePath);
            if (file.StartsWith(local))
            {
                var relative = file.Substring(local.Length);
                return relative.StartsWith(Path.DirectorySeparatorChar.ToString()) ? relative.Substring(1) : relative;
            }
            else
            {
                return null;
            }
        }

        internal static IEnumerable<Tuple<RemoteFile, RemoteChanges>> GetPulls(Repo repo, string lookForFile)
        {
            var relativePath = GetRelativePath(repo.LocalDirectory, lookForFile);
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return Enumerable.Empty<Tuple<RemoteFile, RemoteChanges>>();
            }
            return repo.RemoteChangesData.Select(kvp => kvp.Value).SelectMany(p => p.Files.Select(f => new { n = f.Filename, f, p })).Where(a => a.n == relativePath).Select(a => Tuple.Create(a.f, a.p)).ToList();
        }
    }
}