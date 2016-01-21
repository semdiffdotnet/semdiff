using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SemDiff.Core
{
    public class Conflict
    {
        public ConflictInfo Ancestor { get; set; }
        public ConflictInfo Local { get; set; }
        public ConflictInfo Remote { get; set; }

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
            var firstLocal = local.MinBy(d => d.ChangedSpan.Start);
            var lastLocal = local.MaxBy(d => d.ChangedSpan.End);
            var remote = potentialConflict.Where(c => c.Origin == DiffWithOrigin.OriginEnum.Remote).Select(c => c.Diff).ToList();
            var firstRemote = remote.MinBy(d => d.ChangedSpan.Start);
            var lastRemote = remote.MaxBy(d => d.ChangedSpan.End);

            var first = new[] { firstLocal, firstRemote }.MinBy(c => c.AncestorSpan.Start);
            var last = new[] { lastLocal, lastRemote }.MaxBy(c => c.AncestorSpan.End);
            var con = new Conflict
            {
                Ancestor = ConflictInfo.Create(first.AncestorSpan.Start, last.AncestorSpan.End, first.AncestorTree),
                Local = ConflictInfo.Create(first.AncestorSpan.Start + firstLocal.OffsetStart, last.AncestorSpan.End + lastLocal.OffsetEnd, firstLocal.ChangedTree),
                Remote = ConflictInfo.Create(first.AncestorSpan.Start + firstRemote.OffsetStart, last.AncestorSpan.End + lastRemote.OffsetEnd, firstRemote.ChangedTree)
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