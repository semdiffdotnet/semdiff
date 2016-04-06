using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MoreLinq;
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

                    if (!conflict.DiffResult.Conflicts.Any(con => TriviaCompare.IsSemanticChange(con.Local.Node, con.Remote.Node)))
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
            //TODO: Check if local file has been edited before proceeding!

            var bases = GetBaseClasses(semanticModel);

            foreach (var bt in bases) //a file may have multiple classes
            {
                foreach (var b in bt.Bases) //Partial classes could span multiple files
                {
                    var relativePath = GetRelativePath(repo.LocalDirectory, b.SyntaxTree.FilePath);
                    var pulls = GetPulls(repo, relativePath);

                    foreach (var p in pulls)
                    {
                        var diffs = DiffClassVersion(b, p.File);

                        if (diffs.Any())
                        {
                            yield return new DetectedFalseNegative
                            {
                                Location = Location.Create(bt.Derived.SyntaxTree, bt.Derived.Identifier.Span),
                                RemoteChange = p.Change,
                                RemoteFile = p.File,
                                TypeName = bt.Derived.Identifier.ToString(),
                            };
                        }
                    }
                }
            }
        }

        //Given the base class and a file that contains another version of the file
        private static IEnumerable<Diff> DiffClassVersion(ClassDeclarationSyntax b, RemoteFile f)
        {
            var ancestorDecs = f.Base
                                   .GetRoot()
                                   .DescendantNodes()
                                   .OfType<ClassDeclarationSyntax>();

            var remoteDecs = f.File
                                    .GetRoot()
                                    .DescendantNodes()
                                    .OfType<ClassDeclarationSyntax>();

            var merged = MergeClassDeclarationSyntaxes(ancestorDecs, remoteDecs).FirstOrDefault(t => AreSameClass(t.Item1, b));

            return Diff.Compare(merged.Item1, merged.Item2).Where(d => TriviaCompare.IsSemanticChange(d.Ancestor.Node, d.Changed.Node));
        }

        private static IEnumerable<BaseClass> GetBaseClasses(SemanticModel semanticModel)
        {
            var classDeclarations = semanticModel.SyntaxTree
                                        .GetRoot()
                                        .DescendantNodes()
                                        .OfType<ClassDeclarationSyntax>();

            foreach (var cd in classDeclarations)
            {
                var ds = semanticModel.GetDeclaredSymbol(cd) as INamedTypeSymbol;
                var srs = ds?.BaseType?.DeclaringSyntaxReferences ?? Enumerable.Empty<SyntaxReference>();
                var baseNodes = srs.Select(sr => sr.GetSyntax()).OfType<ClassDeclarationSyntax>();
                yield return new BaseClass(cd, baseNodes);
            }
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
                .Cache();
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

        private static InnerMethodConflict GetInnerMethodConflicts(MethodDeclarationSyntax ancestor, SpanDetails changed, SpanDetails removed, IEnumerable<MethodDeclarationSyntax> insertedMethods, InnerMethodConflict.Local type)
        {
            if (TriviaCompare.IsSpanInNodeTrivia(removed.Span, removed.Node))
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

        private static MethodDeclarationSyntax GetMovedMethod(IEnumerable<MethodDeclarationSyntax> insertedMethods, MethodDeclarationSyntax change)
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

        private static bool AreSameClass(ClassDeclarationSyntax class1, ClassDeclarationSyntax class2)
        {
            //will likely have problems with Template classes
            //TODO: add more checks to be sure it is the correct class
            return class1.Identifier.Text == class2.Identifier.Text;
        }

        //A move looks like a deleted method and then an added method in
        // another place, this find the added methods
        private static IEnumerable<MethodDeclarationSyntax> GetInsertedMethods(IEnumerable<Diff> diffs)
        {
            return diffs
                     .Where(diff => TriviaCompare.IsSpanInNodeTrivia(diff.Ancestor.Span, diff.Ancestor.Node))
                     .Select(diff => diff.Changed.Node as MethodDeclarationSyntax)
                     .Where(node => node != null).Cache();
        }

        private static IEnumerable<Tuple<ClassDeclarationSyntax, ClassDeclarationSyntax>>
            MergeClassDeclarationSyntaxes(IEnumerable<ClassDeclarationSyntax> left,
            IEnumerable<ClassDeclarationSyntax> rightE)
        {
            var right = rightE.ToList();
            foreach (var l in left)
            {
                //Name of class should give a reasonably good idea of if they are the same class
                //This will ignore any renaming of the class
                var r = right.FirstOrDefault(rc => AreSameClass(rc, l));
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
        private class BaseClass : Tuple<ClassDeclarationSyntax, IEnumerable<ClassDeclarationSyntax>>
        {
            public BaseClass(ClassDeclarationSyntax derived, IEnumerable<ClassDeclarationSyntax> bases) : base(derived, bases)
            { }

            public ClassDeclarationSyntax Derived => Item1;
            public IEnumerable<ClassDeclarationSyntax> Bases => Item2;
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