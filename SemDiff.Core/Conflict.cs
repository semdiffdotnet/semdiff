using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SemDiff.Core
{
    public class Conflict
    {
        public SpanDetails Ancestor { get; set; }
        public SpanDetails Local { get; set; }
        public SpanDetails Remote { get; set; }

        private Conflict()
        {
        }

        //Looks for changes in whitespace so that change is not added to the conflict list
        public bool IsWhiteSpaceChange
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        //Looks for changes in comments so that change is not added to the conflict list
        public bool IsNonCodeChange
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        //TODO: Determine if this kind of function is nessasary
        public IEnumerable<Conflict> Split(int ancestorIndex, int localIndex, int remoteIndex)
        {
            throw new NotImplementedException();
        }

        internal static Conflict Create(List<DiffWithOrigin> potentialConflict)
        {
            var local = potentialConflict.Where(c => c.Origin == DiffWithOrigin.OriginEnum.Local).Select(c => c.Diff).ToList();
            var firstLocal = local.MinBy(d => d.Changed.Span.Start);
            var lastLocal = local.MaxBy(d => d.Changed.Span.End);
            var remote = potentialConflict.Where(c => c.Origin == DiffWithOrigin.OriginEnum.Remote).Select(c => c.Diff).ToList();
            var firstRemote = remote.MinBy(d => d.Changed.Span.Start);
            var lastRemote = remote.MaxBy(d => d.Changed.Span.End);

            var first = new[] { firstLocal, firstRemote }.MinBy(c => c.Ancestor.Span.Start);
            var last = new[] { lastLocal, lastRemote }.MaxBy(c => c.Ancestor.Span.End);
            var con = new Conflict
            {
                Ancestor = SpanDetails.Create(first.Ancestor.Span.Start, last.Ancestor.Span.End, first.Ancestor.Tree),
                Local = SpanDetails.Create(first.Ancestor.Span.Start + firstLocal.OffsetStart, last.Ancestor.Span.End + lastLocal.OffsetEnd, firstLocal.Changed.Tree),
                Remote = SpanDetails.Create(first.Ancestor.Span.Start + firstRemote.OffsetStart, last.Ancestor.Span.End + lastRemote.OffsetEnd, firstRemote.Changed.Tree)
            };
            Logger.Debug($"Conflict: {con}");
            return con;
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