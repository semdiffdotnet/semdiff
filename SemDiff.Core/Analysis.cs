using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
        /// <param name="repo">Repo that has the remote changes that need to be checked</param>
        /// <param name="tree">The syntax tree that will be compared with syntax trees from repo</param>
        /// <param name="filePath">The path that the syntax tree was parsed from</param>
        public static IEnumerable<DetectedFalsePositive> ForFalsePositive(Repo repo,
            SyntaxTree tree, string filePath) //TODO: Remove filePath, retrieve from SyntaxTree instead
        {
            var relativePath = GetRelativePath(repo.LocalDirectory, filePath)
                                        .Replace('\\', '/'); //Standardize Directory Separator!
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
                foreach (var c in conflicts.Conflicts //TODO: do we need to convert Conflicts to list first?
                                .Where(con => con.Ancestor.Node is MethodDeclarationSyntax))
                {
                    var ancestor = (MethodDeclarationSyntax)c.Ancestor.Node;
                    if (ancestor == null)
                        break;

                    var localRemoved = GetInnerMethodConflicts(ancestor, c.Remote, c.Local, locs);
                    foreach (var t in localRemoved)
                    {
                        var diff3 = t.Item3;
                        var local = t.Item2; //local removed
                        if (!diff3.Conflicts.Any()) //TODO: this doesn't filter out adding comments yet
                        {
                            //Since Inner method actually has no conflicts, we found a false positive
                            yield return new DetectedFalsePositive
                            {
                                //TODO: how does one Figure out the affected region? This is difficult
                                // because it is different for if the local file removed the method or
                                // changed it!
                                Location = Location.Create(tree, local.Span),
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
                        var local = t.Item1; //If the remote was removed then the local was changed
                        if (!diff3.Conflicts.Any()) //TODO: filter
                        {
                            //Since Inner method actually has no conflicts, we found a false positive
                            yield return new DetectedFalsePositive
                            {
                                //TODO: return more accurate location
                                Location = Location.Create(tree, local.Span),
                                RemoteFile = f,
                                RemoteChange = p,
                                ConflictType = DetectedFalsePositive.ConflictTypes.LocalMethodChanged,
                            };
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Given a Repo and a Semantic Model find any possible FalseNegatives
        /// </summary>
        /// <param name="repo">Repo that has the remote changes that need to be checked</param>
        /// <param name="semanticModel">the semantic model that will be used to find the base class</param>
        public static IEnumerable<DetectedFalseNegative> ForFalseNegative(Repo repo,
                                                                        SemanticModel semanticModel)
        {
            var classDeclarations = semanticModel.SyntaxTree
                                        .GetRoot()
                                        .DescendantNodes()
                                        .OfType<ClassDeclarationSyntax>();

            var declaredSymbol = classDeclarations.Select(cds => semanticModel.GetDeclaredSymbol(cds));

            var classBases = declaredSymbol.SelectMany(
                    t => (t as INamedTypeSymbol)?.BaseType
                                    ?.DeclaringSyntaxReferences ?? Enumerable.Empty<SyntaxReference>());

            var classBaseNodes = Task.WhenAll(classBases.Select(sr => sr.GetSyntaxAsync()))
                                                            .Result.OfType<ClassDeclarationSyntax>();

            return classBaseNodes.SelectMany(c =>
            {
                var relativePath = GetRelativePath(repo.LocalDirectory, c.SyntaxTree.FilePath)
                                            .Replace('\\', '/'); //Standardize Directory Separator!

                return GetPulls(repo, relativePath).SelectMany(t =>
                {
                    var file = t.Item1;
                    var remotechanges = t.Item2;

                    var ancestorDecs = file.Base
                                            .GetRoot()
                                            .DescendantNodes()
                                            .OfType<ClassDeclarationSyntax>();

                    var remoteDecs = file.File
                                            .GetRoot()
                                            .DescendantNodes()
                                            .OfType<ClassDeclarationSyntax>();

                    return MergeClassDeclarationSyntaxes(ancestorDecs, remoteDecs)
                            .Select(ar => Diff3.Compare(ar.Item1, c, ar.Item2))
                            .Where(dr => dr.Conflicts.Any())
                            .Select(dr => new DetectedFalseNegative
                            {
                                Location = Location.None,
                                RemoteChange = remotechanges,
                                RemoteFile = file,
                                TypeName = c.Identifier.ToString(),
                            });
                });
            });
        }

        internal static IEnumerable<Tuple<RemoteFile, RemoteChanges>> GetPulls(Repo repo,
                                                                            string relativePath)
        {
            return repo.RemoteChangesData
                .Select(kvp => kvp.Value)
                .SelectMany(p => p.Files
                                   .Select(f => new { n = f.Filename, f, p }))
                                   .Where(a => a.n == relativePath)
                                   .Select(a => Tuple.Create(a.f, a.p))
                .ToList();
        }

        internal static string GetRelativePath(string localDirectory, string filePath)
        {
            var local = Path.GetFullPath(localDirectory);
            var file = Path.GetFullPath(filePath);
            if (file.StartsWith(local))
            {
                var relative = file.Substring(local.Length);
                return relative.StartsWith(Path.DirectorySeparatorChar.ToString())
                    ? relative.Substring(1)
                    : relative;
            }
            else
            {
                return null;
            }
        }

        //Tuple is Changed, Removed, and the diff result
        private static IEnumerable<Tuple<MethodDeclarationSyntax, MethodDeclarationSyntax, Diff3Result>>
            GetInnerMethodConflicts(MethodDeclarationSyntax ancestor, SpanDetails changed,
            SpanDetails removed, List<MethodDeclarationSyntax> insertedMethods)
        {
            if (string.IsNullOrWhiteSpace(removed.Text))
            {
                var change = changed.Node as MethodDeclarationSyntax;
                if (change != null)
                {
                    var methodName = change.Identifier.Text;
                    foreach (var moved in insertedMethods
                        .Where(method => method.Identifier.Text == methodName))
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

        //A move looks like a deleted method and then an added method in
        // another place, this find the added methods
        private static List<MethodDeclarationSyntax> GetInsertedMethods(List<Diff> diffs)
        {
            return diffs
                     .Where(diff => string.IsNullOrWhiteSpace(diff.Ancestor.Text))
                     .Select(diff => diff.Changed.Node as MethodDeclarationSyntax)
                     .Where(node => node != null).ToList();
        }

        private static IEnumerable<Tuple<ClassDeclarationSyntax, ClassDeclarationSyntax>>
            MergeClassDeclarationSyntaxes(IEnumerable<ClassDeclarationSyntax> left,
            IEnumerable<ClassDeclarationSyntax> rightE)
        {
            var right = rightE.ToList();
            foreach (var l in left)
            {
                //Name of class should give a reasonably good idea of if they are the same class
                //This will ignore any renaming of the class, and will likely have problems with
                //Template classes
                var r = right.FirstOrDefault(rc => rc.Identifier.Text == l.Identifier.Text);
                if (r != null)
                {
                    right.Remove(r);
                    yield return Tuple.Create(l, r);
                }
            }
        }
    }
}