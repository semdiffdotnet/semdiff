using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SemDiff.Core
{
    public static class Diff3
    {
        /// <summary>
        /// Perform 3-way diff, this is done by comparing the changes to local and remote. This may result in conflicts if the changes overlap
        /// </summary>
        /// <param name="ancestor">The version that both local and remote decended from</param>
        /// <param name="local">One set of changes, the local and remote params can be swapped without changing the result</param>
        /// <param name="remote">One set of changes, the local and remote params can be swapped without changing the result</param>
        /// <returns></returns>
        public static Diff3Result Compare(SyntaxTree ancestor, SyntaxTree local, SyntaxTree remote)
        {
            var localChanges = Diff.Compare(ancestor, local).ToList();
            var remoteChanges = Diff.Compare(ancestor, remote).ToList();

            return new Diff3Result
            {
                Conflicts = GetConflicts(localChanges, remoteChanges, ancestor, local, remote),
                Local = localChanges,
                Remote = remoteChanges,
                AncestorTree = ancestor,
                LocalTree = local,
                RemoteTree = remote,
            };
        }

        public static IEnumerable<Conflict> GetConflicts(IEnumerable<Diff> local, IEnumerable<Diff> remote, SyntaxTree ancestorTree, SyntaxTree localTree, SyntaxTree remoteTree)
        {
            var localChanges = local.Select(DiffWithOrigin.Local).ToList();
            var remoteChanges = remote.Select(DiffWithOrigin.Remote).ToList();

            var changes = Extensions.GetMergedChangeQueue(localChanges, remoteChanges, d => d.Diff.Ancestor.Span.Start);
            var potentialConflict = new List<DiffWithOrigin>();
            while (changes.Count > 0)
            {
                potentialConflict.Clear();
                do
                {
                    var change = changes.Dequeue();
                    potentialConflict.Add(change);
                }
                while (changes.Count > 0 && Diff.IntersectsAny(changes.Peek().Diff, potentialConflict.Select(dwo => dwo.Diff)));

                if (potentialConflict.Count >= 2)
                    yield return Conflict.Create(potentialConflict, ancestorTree, localTree, remoteTree);
            }
            yield break;
        }

        internal static Diff3Result Compare(SyntaxNode ancestor, SyntaxNode local, SyntaxNode remote)
        {
            return Compare(SyntaxFactory.SyntaxTree(ancestor), SyntaxFactory.SyntaxTree(local), SyntaxFactory.SyntaxTree(remote));
        }
    }
}