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
        public SpanDetails Ancestor { get; set; }
        public SpanDetails Changed { get; set; }

        public int OffsetStart => Changed.Span.Start - Ancestor.Span.Start;
        public int OffsetEnd => Changed.Span.End - Ancestor.Span.End;

        public static bool Intersects(Diff diff1, Diff diff2)
        {
            var start1 = diff1.Ancestor.Span.Start;
            var end1 = diff1.Ancestor.Span.End;
            var start2 = diff2.Ancestor.Span.Start;
            var end2 = diff2.Ancestor.Span.End;
            return (start2 <= start1 && start1 <= end2) || (start1 <= start2 && start2 <= end1); //true if start of one is within start of the other (inclusive)
        }

        public static IEnumerable<Diff> Compare(SyntaxTree ancestor, SyntaxTree changed)
        {
            var offset = 0; //Tracks the difference in indexes as we move through the changed syntax tree
            foreach (var change in changed.GetChanges(ancestor)) //Assumption: I assume that this will allways be given sorted by place in file
            {
                Logger.Debug($"{change}");

                var origLength = change.Span.Length;
                var offsetChange = (change.NewText.Length - origLength);

                var chanStart = change.Span.Start + offset;
                var chanLength = origLength + offsetChange;

                offset += offsetChange;

                var d = new Diff { Ancestor = SpanDetails.Create(change.Span, ancestor), Changed = SpanDetails.Create(new TextSpan(chanStart, chanLength), changed) };

                yield return d;
            }
        }

        public override string ToString()
        {
            return $"<<{Ancestor.Text}||{Changed.Text}>>";
        }
    }
}