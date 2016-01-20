using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;

namespace SemDiff.Core
{
    /// <summary>
    /// This class acts as a more detailed version of the TextChanged class that is better suited for our purposes
    /// </summary>
    public class Diff
    {
        public SyntaxTree AncestorTree { get; set; }
        public SyntaxTree ChangedTree { get; set; }
        public TextSpan AncestorSpan { get; set; }
        public TextSpan ChangedSpan { get; set; }

        public int OffsetStart => AncestorSpan.Start - ChangedSpan.Start;
        public int OffsetEnd => AncestorSpan.End - ChangedSpan.End;

        public string AncestorText => AncestorTree?.GetText().ToString(AncestorSpan);
        public string ChangedText => ChangedTree?.GetText().ToString(ChangedSpan);

        public static IEnumerable<Diff> Compare(SyntaxTree ancestor, SyntaxTree changed)
        {
            var changes = changed.GetChanges(ancestor).Select(c => new
            {
                Original = c,
                Detail = new Diff { AncestorTree = ancestor, ChangedTree = changed }
            }).ToList();

            var offset = 0; //Tracks the difference in indexes as we move through the changed syntax tree
            foreach (var change in changes) //Assumption: I assume that this will allways be given sorted by place in file
            {
                change.Detail.AncestorSpan = change.Original.Span;

                var origLength = change.Original.Span.Length;
                var offsetChange = (change.Original.NewText.Length - origLength);

                var chanStart = change.Original.Span.Start + offset;
                var chanLength = origLength + offsetChange;
                change.Detail.ChangedSpan = new TextSpan(chanStart, chanLength);

                offset += offsetChange;

                Logger.Debug($"{change}");
            }
            return changes.Select(c => c.Detail); //yield function instead?
        }

        public override string ToString()
        {
            return $"<<{AncestorText}||{ChangedText}>>";
        }
    }
}