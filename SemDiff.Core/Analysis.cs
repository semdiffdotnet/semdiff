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
        /// <param name="local">The syntax tree that will be compared with syntax trees from repo</param>
        public static IEnumerable<DetectedFalsePositive> ForFalsePositive(Repo repo, SyntaxTree local)
        {
            var relativePath = GetRelativePath(repo.LocalDirectory, local.FilePath);
            var pulls = GetPulls(repo, relativePath);
            foreach (var pull in pulls)
            {
                var conflicts = Diff3.Compare(pull.File.Base, local, pull.File.File);
                var locs = GetInsertedMethods(conflicts.Local);
                var rems = GetInsertedMethods(conflicts.Remote);

                var methodConflicts = conflicts.Conflicts.Where(con => con.Ancestor.Node is MethodDeclarationSyntax);

                foreach (var c in methodConflicts)
                {
                    var ancestor = (MethodDeclarationSyntax)c.Ancestor.Node;

                    var conflict = GetInnerMethodConflicts(ancestor, c.Remote, c.Local, locs, InnerMethodConflict.Local.Moved);

                    //If the local was not moved, the local could have still been changed
                    if (conflict == null)
                    {
                        conflict = GetInnerMethodConflicts(ancestor, c.Local, c.Remote, rems, InnerMethodConflict.Local.Changed);
                        if (conflict == null)
                        {
                            continue;
                        }
                    }

                    if (!conflict.DiffResult.Conflicts.Any()) //TODO: this doesn't filter adding comments yet
                    {
                        yield return new DetectedFalsePositive
                        {
                            Location = Location.Create(local, conflict.GetLocal().Identifier.Span),
                            RemoteFile = pull.File,
                            RemoteChange = pull.Change,
                            ConflictType = conflict.LocalLocation == InnerMethodConflict.Local.Changed
                                         ? DetectedFalsePositive.ConflictTypes.LocalMethodChanged
                                         : DetectedFalsePositive.ConflictTypes.LocalMethodRemoved,
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
                var relativePath = GetRelativePath(repo.LocalDirectory, c.SyntaxTree.FilePath);

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

        private static IEnumerable<Pull> GetPulls(Repo repo, string relativePath)
        {
            if (relativePath == null || repo == null)
            {
                return Enumerable.Empty<Pull>();
            }
            relativePath = relativePath.ToStandardPath();
            return repo.RemoteChangesData
                .Select(kvp => kvp.Value)
                .SelectMany(p => p.Files
                                   .Select(f => new { n = f.Filename, f, p }))
                                   .Where(a => a.n == relativePath)
                                   .Select(a => new Pull(a.f, a.p))
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

        private static InnerMethodConflict GetInnerMethodConflicts(MethodDeclarationSyntax ancestor, SpanDetails changed, SpanDetails removed, List<MethodDeclarationSyntax> insertedMethods, InnerMethodConflict.Local type)
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
            if (moved == null)
            {
                return null;
            }
            var diffRes = Diff3.Compare(ancestor, moved, change);
            return new InnerMethodConflict(ancestor, change, moved, diffRes, type);
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
            return syntax1 == null || syntax2 == null
                ? syntax1 == syntax2 //Since one is null, reference equality will work
                : !syntax1.ToSyntaxTree().GetChanges(syntax2.ToSyntaxTree()).Any();
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

        //Essentially a named tuple with helpers. This inherits is because Tuple gives us useful things like the GetHashCode, Equals, and ToString
        private class Pull : Tuple<RemoteFile, RemoteChanges>
        {
            public Pull(RemoteFile file, RemoteChanges changes) : base(file, changes)
            {
            }

            public RemoteFile File => Item1;
            public RemoteChanges Change => Item2;
        }

        //Essentially a named tuple with helpers. This inherits is because Tuple gives us useful things like the GetHashCode, Equals, and ToString
        private class InnerMethodConflict : Tuple<MethodDeclarationSyntax, MethodDeclarationSyntax, MethodDeclarationSyntax, Diff3Result, InnerMethodConflict.Local>
        {
            public InnerMethodConflict(MethodDeclarationSyntax ancestor, MethodDeclarationSyntax changed, MethodDeclarationSyntax removed, Diff3Result diffresult, InnerMethodConflict.Local type)
                : base(ancestor, changed, removed, diffresult, type)
            { }

            public MethodDeclarationSyntax Ancestor => Item1;
            public MethodDeclarationSyntax Changed => Item2;
            public MethodDeclarationSyntax Removed => Item3;
            public Diff3Result DiffResult => Item4;
            public Local LocalLocation => Item5;

            public enum Local //Which field is the local method? Needed later
            {
                Moved,
                Changed
            }

            internal MethodDeclarationSyntax GetLocal()
            {
                return LocalLocation == Local.Changed ? Changed : Removed;
            }
        }
    }
}