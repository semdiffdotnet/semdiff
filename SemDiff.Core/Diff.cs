using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SemDiff.Core
{
    /// <summary>
    /// This class acts as a more detailed version of the TextChanged class that is better suited
    /// for our purposes
    /// </summary>
    public class Diff
    {
        public SpanDetails Ancestor { get; set; }
        public SpanDetails Changed { get; set; }

        public int OffsetEnd => Changed.Span.End - Ancestor.Span.End;
        public int OffsetStart => Changed.Span.Start - Ancestor.Span.Start;

        /// <summary>
        /// Generates Diff results for each change that occurred from the ancestor to the changed
        /// </summary>
        /// <param name="ancestor">
        /// The abstract syntax tree that the changed version originated from
        /// </param>
        /// <param name="changed">The abstract syntax tree that resulted from changing the ancestor</param>
        /// <returns>Changes between the ancestor and changed</returns>
        public static IEnumerable<Diff> Compare(SyntaxTree ancestor, SyntaxTree changed)
        {
            var offset = 0; //Tracks differences in indexes moving through the changed syntax tree
            foreach (var change in changed.GetChanges(ancestor))
            { //Assumes that this list is sorted by place in file
                var origLength = change.Span.Length;
                var offsetChange = (change.NewText.Length - origLength);

                var chanStart = change.Span.Start + offset;
                var chanLength = origLength + offsetChange;

                offset += offsetChange;

                var d = new Diff
                {
                    Ancestor = SpanDetails.Create(change.Span, ancestor),
                    Changed = SpanDetails.Create(new TextSpan(chanStart, chanLength), changed)
                };

                yield return d;
            }
        }

        /// <summary>
        /// Determine if the two diffs overlap
        /// </summary>
        /// <param name="diff1">One of the objects to test</param>
        /// <param name="diff2">One of the objects to test</param>
        /// <returns>true if the diffs overlap else false</returns>
        public static bool Intersects(Diff diff1, Diff diff2)
        {
            var start1 = diff1.Ancestor.Span.Start;
            var end1 = diff1.Ancestor.Span.End;
            var start2 = diff2.Ancestor.Span.Start;
            var end2 = diff2.Ancestor.Span.End;

            //true if start of one is within start of the other (inclusive)
            return (start2 <= start1 && start1 <= end2)
                || (start1 <= start2 && start2 <= end1);
        }

        /// <summary>
        /// Get a string that represents the differences in the file in a way that is readable
        /// </summary>
        /// <param name="ancestor">SyntaxTree that changed was modified from</param>
        /// <param name="changed">SyntaxTree that contains the changes</param>
        /// <returns>a string that can be inspected to see the changes</returns>
        public static string VisualDiff(SyntaxTree ancestor, SyntaxTree changed)
        {
            var diffs = Compare(ancestor, changed);
            return VisualDiff(diffs, ancestor);
        }

        /// <summary>
        /// Get a string that represents the differences in the file in a way that is readable
        /// </summary>
        /// <param name="ancestor">SyntaxTree that changed was modified from</param>
        /// <param name="diffs">all the changes to the ancestor</param>
        /// <returns>a string that can be inspected to see the changes</returns>
        public static string VisualDiff(IEnumerable<Diff> diffs, SyntaxTree ancestor)
        {
            var builder = new StringBuilder();
            var currentAncestorPos = 0;
            foreach (var d in diffs)
            {
                var dStart = d.Ancestor.Span.Start;
                if (dStart > currentAncestorPos)
                {
                    builder.Append(ancestor
                                    .GetText()
                                    .ToString(TextSpan.FromBounds(currentAncestorPos, dStart)));
                }
                builder.Append(d);
                currentAncestorPos = d.Ancestor.Span.End;
            }
            builder.Append(ancestor
                            .GetText()
                            .ToString(TextSpan.FromBounds(currentAncestorPos, ancestor.Length)));
            return builder.ToString();
        }

        public override string ToString()
        {
            return $"<<<<<<<{Ancestor.Text}|||||||{Changed.Text}>>>>>>>";
        }

        /// <summary>
        /// Determines if a single diff intersects with any number of other diffs
        /// </summary>
        /// <param name="diff">single diff that will be compared against many</param>
        /// <param name="diffs">multiple diffs that will be compared against a single diff</param>
        /// <returns>true if an intersection occurs else false</returns>
        public static bool IntersectsAny(Diff diff, IEnumerable<Diff> diffs)
                                                        => diffs.Any(d => Intersects(diff, d));
    }
}