using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace SemDiff.Core
{
    public static class Diff3
    {
        public static IEnumerable<Conflict> Compare(SyntaxTree ancestor, SyntaxTree local, SyntaxTree remote)
        {
            var localChanges = Diff.Compare(ancestor, local).Select(DiffWithOrigin.Local).ToList();
            var remoteChanges = Diff.Compare(ancestor, remote).Select(DiffWithOrigin.Remote).ToList();
            var changes = Extensions.GetMergedChangeQueue(localChanges, remoteChanges, d => d.Diff.AncestorSpan.Start);
            var potentialConflict = new List<DiffWithOrigin>();
            while (changes.Count > 0)
            {
                potentialConflict.Clear();
                DiffWithOrigin change;
                do
                {
                    change = changes.Dequeue();
                    potentialConflict.Add(change);
                }
                while (changes.Count > 0 && Diff.Intersects(change.Diff, changes.Peek().Diff));

                if (potentialConflict.Count >= 2)
                    yield return Conflict.Create(potentialConflict);
            }
            yield break;
        }
    }
}