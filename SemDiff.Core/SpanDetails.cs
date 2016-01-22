using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;

namespace SemDiff.Core
{
    /// <summary>
    /// Contains info about one side of a conflict or diff.
    /// </summary>
    public class SpanDetails
    {
        public TextSpan Span { get; set; }
        public string Text => Tree?.GetText().ToString(Span);
        public SyntaxNode Node => Tree?.GetRoot().FindNode(Span, true, false);
        public SyntaxTree Tree { get; set; }

        private SpanDetails()
        {
        }

        //TODO: Determine if this kind of function is nessasary
        public IEnumerable<SpanDetails> Split(int index)
        {
            throw new NotImplementedException();
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

        public override string ToString()
        {
            return Text;
        }
    }
}