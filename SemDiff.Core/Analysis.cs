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
        public static IEnumerable<DetectedFalsePositive> ForFalsePositive(Repo repo, SyntaxTree tree, string filePath) //TODO: Remove filePath, retrieve from SyntaxTree instead
        {
            var relativePath = GetRelativePath(repo.LocalDirectory, filePath).Replace('\\', '/'); //Standardize Directory Separator!
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

                    var localRemoved = GetInnerMethodConflicts(ancestor, c.Remote, c.Local, locs);

                    if (localRemoved != null && !localRemoved.DiffResult.Conflicts.Any()) //TODO: this doesn't filter out adding comments yet
                    {
                        yield return new DetectedFalsePositive
                        {
                            Location = Location.Create(tree, localRemoved.Changed.Identifier.Span),
                            RemoteFile = f,
                            RemoteChange = p,
                            ConflictType = DetectedFalsePositive.ConflictTypes.LocalMethodRemoved,
                        };
                    }

                    var remoteRemoved = GetInnerMethodConflicts(ancestor, c.Local, c.Remote, rems);

                    if (remoteRemoved != null && !remoteRemoved.DiffResult.Conflicts.Any())
                    {
                        yield return new DetectedFalsePositive
                        {
                            Location = Location.Create(tree, remoteRemoved.Changed.Identifier.Span),
                            RemoteFile = f,
                            RemoteChange = p,
                            ConflictType = DetectedFalsePositive.ConflictTypes.LocalMethodChanged,
                        };
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
            if (relativePath == null || repo == null)
            {
                return Enumerable.Empty<Tuple<RemoteFile, RemoteChanges>>();
            }
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

        private static InnerMethodConflict GetInnerMethodConflicts(MethodDeclarationSyntax ancestor, SpanDetails changed, SpanDetails removed, List<MethodDeclarationSyntax> insertedMethods)
        {
            if (!string.IsNullOrWhiteSpace(removed.Text))
            {
                return null;
            }
            var change = changed.Node as MethodDeclarationSyntax;
            if (change == null)
            {
                return null;
            }
            var moved = GetMovedMethod(insertedMethods, change);
            var diffRes = Diff3.Compare(ancestor, moved, change);
            return new InnerMethodConflict(ancestor, change, moved, diffRes);
        }

        private static MethodDeclarationSyntax GetMovedMethod(List<MethodDeclarationSyntax> insertedMethods, MethodDeclarationSyntax change)
        {
            //A unique method seems to be defined by its name and its parameter types (including type params)

            var namesMatch = insertedMethods.Where(method => method.Identifier.Text == change.Identifier.Text);
            //Note: will cause problems if parts of the signature were changed, including renaming params
            //Note: this is a textual checking, it may or may not be better to do only compare the order and type of the parameters (same for type params)
            var paramsMatch = namesMatch.Where(method => AreSame(method.ParameterList, change.ParameterList));
            var typeParamsMatch = paramsMatch.Where(m => AreSame(m.TypeParameterList, change.TypeParameterList));

            return typeParamsMatch.SingleOrDefault();
        }

        private static bool AreSame(SyntaxNode syntax1, SyntaxNode syntax2)
        {
            if (syntax1 != null && syntax2 != null)
            {
                return !syntax1.ToSyntaxTree().GetChanges(syntax2.ToSyntaxTree()).Any();
            }
            else return syntax1 == syntax2; //If both are null true, else false
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

        //Essentially a named tuple. This inherits is because Tuple gives us useful things like the GetHashCode, Equals, and ToString
        private class InnerMethodConflict : Tuple<MethodDeclarationSyntax, MethodDeclarationSyntax, MethodDeclarationSyntax, Diff3Result>
        {
            public InnerMethodConflict(MethodDeclarationSyntax ancestor, MethodDeclarationSyntax changed, MethodDeclarationSyntax removed, Diff3Result diffresult)
                : base(ancestor, changed, removed, diffresult)
            { }

            public MethodDeclarationSyntax Ancestor => Item1;
            public MethodDeclarationSyntax Changed => Item2;
            public MethodDeclarationSyntax Removed => Item3;
            public Diff3Result DiffResult => Item4;

            public enum Type
            {
                LocalRemoved,
                LocalChanged
            }
        }
    }
}