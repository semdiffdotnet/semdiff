// Copyright (c) 2015 semdiffdotnet. Distributed under the MIT License.
// See LICENSE file or opensource.org/licenses/MIT.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace SemDiff.Core
{
    /// <summary>
    /// Contains info about one side of a conflict or diff.
    /// </summary>
    public class SpanDetails
    {
        private SpanDetails()
        {
        }

        public SyntaxNode Node => Tree?.GetRoot().FindNode(Span, true, false);
        public TextSpan Span { get; set; }
        public string Text => Tree?.GetText().ToString(Span);
        public SyntaxTree Tree { get; set; }

        public override string ToString()
        {
            return Text;
        }

        internal static SpanDetails Create(int start, int end, SyntaxTree tree)
        {
            return Create(new TextSpan(start, end - start), tree);
        }

        internal static SpanDetails Create(TextSpan span, SyntaxTree tree)
        {
            return new SpanDetails
            {
                Tree = tree,
                Span = span
            };
        }
    }
}