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
            var relativePath = GetRelativePath(repo.LocalDirectory, filePath).Replace('\\', '/'); //Standardize Directory Separator!
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                yield break;
            }
            var pulls = GetPulls(repo, relativePath);
            foreach (var fp in pulls)
            {
                var f = fp.Item1;
                var p = fp.Item2;

                var conflicts = Diff3.Compare(f.Base, tree, f.File);
                var locs = GetInsertedMethods(conflicts.Local);
                var rems = GetInsertedMethods(conflicts.Remote);
                foreach (var c in conflicts.Conflicts.Where(con => con.Ancestor.Node is MethodDeclarationSyntax)) //Warning: do we need to convert Conflicts to list first?
                {
                    var ancestor = (MethodDeclarationSyntax)c.Ancestor.Node;
                    if (ancestor == null)
                        break;

                    var localRemoved = GetInnerMethodConflicts(ancestor, c.Remote, c.Local, locs);
                    foreach (var t in localRemoved)
                    {
                        var diff3 = t.Item3;
                        var local = t.Item2; //local removed
                        if (!diff3.Conflicts.Any()) //TODO: this doesn't filter out things like adding comments yet
                        {
                            //Since Inner method actually has no conflicts, we found a false positive
                            yield return new DetectedFalsePositive
                            {
                                Location = Location.Create(tree, local.Span), //TODO: how does one Figure out the affected region? This is difficult because it is different for if the local file removed the method or changed it!
                                RemoteFile = f,
                                RemoteChange = p,
                                ConflictType = DetectedFalsePositive.ConflictTypes.LocalMethodRemoved,
                            };
                        }
                    }
                    var remoteRemoved = GetInnerMethodConflicts(ancestor, c.Local, c.Remote, rems);
                    foreach (var t in remoteRemoved)
                    {
                        var diff3 = t.Item3;
                        var local = t.Item1; //If the remote was removed then that means that the local was changed
                        if (!diff3.Conflicts.Any()) //TODO: this doesn't filter out things like adding comments yet
                        {
                            //Since Inner method actually has no conflicts, we found a false positive
                            yield return new DetectedFalsePositive
                            {
                                Location = Location.Create(tree, local.Span), //TODO: how does one Figure out the affected region? This is difficult because it is different for if the local file removed the method or changed it!
                                RemoteFile = f,
                                RemoteChange = p,
                                ConflictType = DetectedFalsePositive.ConflictTypes.LocalMethodChanged,
                            };
                        }
                    }
                }
            }
        }

        //Tuple is Changed, Removed, and the diff result
        private static IEnumerable<Tuple<MethodDeclarationSyntax, MethodDeclarationSyntax, Diff3Result>> GetInnerMethodConflicts(MethodDeclarationSyntax ancestor, SpanDetails changed, SpanDetails removed, List<MethodDeclarationSyntax> insertedMethods)
        {
            if (string.IsNullOrWhiteSpace(removed.Text))
            {
                var change = changed.Node as MethodDeclarationSyntax;
                if (change != null)
                {
                    var methodName = change.Identifier.Text;
                    foreach (var moved in insertedMethods.Where(method => method.Identifier.Text == methodName))
                    {
                        var diffRes = Diff3.Compare(ancestor, moved, change);
                        if (!diffRes.Conflicts.Any())
                        {
                            yield return Tuple.Create(change, moved, diffRes);
                        }
                    }
                }
            }
        }

        //A move looks like a deleted method and then an added method in another place, this find the added methods
        private static List<MethodDeclarationSyntax> GetInsertedMethods(List<Diff> diffs)
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
            yield break;
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

        internal static IEnumerable<Tuple<RemoteFile, RemoteChanges>> GetPulls(Repo repo, string relativePath)
        {
            return repo.RemoteChangesData.Select(kvp => kvp.Value).SelectMany(p => p.Files.Select(f => new { n = f.Filename, f, p })).Where(a => a.n == relativePath).Select(a => Tuple.Create(a.f, a.p)).ToList();
        }
    }
}