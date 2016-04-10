using Microsoft.CodeAnalysis;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SemDiff.Core
{
    /// <summary>
    /// Represents a conflicting changes between two files using a third ancestor file
    /// </summary>
    public class Conflict
    {
        public SpanDetails Ancestor { get; set; }
        public SpanDetails Local { get; set; }
        public SpanDetails Remote { get; set; }

        private Conflict()
        {
        }

        internal static Conflict Create(List<DiffWithOrigin> potentialConflict, SyntaxTree ancestorTree, SyntaxTree localTree, SyntaxTree remoteTree)
        {
            var fllocal = FindStartEnd(potentialConflict.Where(c => c.Origin == DiffWithOrigin.OriginEnum.Local).Select(c => c.Diff));
            var flremote = FindStartEnd(potentialConflict.Where(c => c.Origin == DiffWithOrigin.OriginEnum.Remote).Select(c => c.Diff));

            var localStartOffset = fllocal.Item1.OffsetStart;
            var localEndOffset = fllocal.Item2.OffsetEnd;

            var remoteStartOffset = flremote.Item1.OffsetStart;
            var remoteEndOffset = flremote.Item2.OffsetEnd;

            var first = MinAncestorStart(fllocal.Item1, flremote.Item1).Ancestor.Span.Start;
            var last = MaxAncestorEnd(fllocal.Item2, flremote.Item2).Ancestor.Span.End;

            var con = new Conflict
            {
                Ancestor = SpanDetails.Create(first, last, ancestorTree),
                Local = SpanDetails.Create(first + localStartOffset, last + localEndOffset, localTree),
                Remote = SpanDetails.Create(first + remoteStartOffset, last + remoteEndOffset, remoteTree)
            };
            return con;
        }

        private static Diff MinAncestorStart(Diff d1, Diff d2) => d1.Ancestor.Span.Start <= d2.Ancestor.Span.Start ? d1 : d2;

        private static Diff MaxAncestorEnd(Diff d1, Diff d2) => d1.Ancestor.Span.End >= d2.Ancestor.Span.End ? d1 : d2;

        private static Tuple<Diff, Diff> FindStartEnd(IEnumerable<Diff> inp)
        {
            var local = inp.CacheEnumerable();
            var firstLocal = local.MinBy(d => d.Changed.Span.Start);
            var lastLocal = local.MaxBy(d => d.Changed.Span.End);
            var fllocal = Tuple.Create(firstLocal, lastLocal);
            return fllocal;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append('<', 7);
            sb.Append(Local);
            sb.Append('|', 7);
            sb.Append(Ancestor);
            sb.Append('=', 7);
            sb.Append(Remote);
            sb.Append('>', 7);
            return sb.ToString();
        }
    }
}