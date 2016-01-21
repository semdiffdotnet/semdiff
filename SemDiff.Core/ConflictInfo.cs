using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;

namespace SemDiff.Core
{
    /// <summary>
    /// Contains info about one side of a conflict. In this context, Surounding means that the span
    /// is in between the node, Containing means that the node is in between the span, and
    /// Intersecting means the node has one side in the span and one side out
    /// </summary>
    public class ConflictInfo
    {
        public TextSpan Span { get; set; }
        public string Text => Tree?.GetText().ToString(Span);
        public SyntaxNode Node => Tree?.GetRoot().FindNode(Span, true, false);
        public SyntaxTree Tree { get; set; }

        private ConflictInfo()
        {
        }

        //TODO: Determine if this kind of function is nessasary
        public IEnumerable<ConflictInfo> Split(int index)
        {
            throw new NotImplementedException();
        }

        internal static ConflictInfo Create(int start, int end, SyntaxTree tree)
        {
            return new ConflictInfo
            {
                Tree = tree,
                Span = new TextSpan(start, end - start)
            };
        }

        public override string ToString()
        {
            return Text;
        }
    }
}