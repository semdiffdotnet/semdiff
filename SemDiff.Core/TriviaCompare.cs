// Copyright (c) 2015 semdiffdotnet. Distributed under the MIT License.
// See LICENSE file or opensource.org/licenses/MIT.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SemDiff.Core
{
    /// <summary>
    /// Handles analysis related to making the distinction between Trivia and other nodes
    /// </summary>
    public static class TriviaCompare
    {
        /// <summary>
        /// Traverses a Node and it's children to determine if the two nodes match (ignoring any
        /// trivia attached to the nodes). This can be used to filter out changes to comments or whitespace
        /// </summary>
        /// <param name="a">Node to compare</param>
        /// <param name="b">Node to compare</param>
        /// <returns>
        /// true if nodes are semantically different; false if they are the same or only differ by trivia
        /// </returns>
        public static bool IsSemanticChange(SyntaxNode a, SyntaxNode b)
        {
            if (a.GetType() != b.GetType())
                return true; //Change is significant enough to change what the node is

            var tokens = Map(a, b, c => c.ChildTokens().Select(n => n.MakeNullable()));
            var childs = Map(a, b, c => c.ChildNodes());
            var tmismatch = tokens.Any(p => OneNull(p) || p.Item1.Value.GetType() != p.Item2.Value.GetType() || p.Item1.Value.IsMissing != p.Item2.Value.IsMissing);
            var ret = tmismatch || childs.Any(p => OneNull(p) || IsSemanticChange(p.Item1, p.Item2));
            return ret;
        }

        //returns true if one of the items in the tuple is null
        private static bool OneNull<T>(Tuple<T, T> pair) => pair.Item1 == null || pair.Item2 == null;

        //applies a function to two types to get two lists that are mapped together and returned
        private static IEnumerable<Tuple<T, T>> Map<T, R>(R source1, R source2, Func<R, IEnumerable<T>> selector)
        {
            return selector?.Invoke(source1).Map(selector?.Invoke(source2));
        }

        /// <summary>
        /// Determines if the span inside of the SyntaxNode is trivia (whitespace or comment)
        /// </summary>
        /// <param name="span">span that is contained within the node's FullSpan</param>
        /// <param name="node">node that contains the span</param>
        /// <returns>true if the span represents part of a trivia</returns>
        public static bool IsSpanInNodeTrivia(TextSpan span, SyntaxNode node)
        {
            return span.Length == 0
                || node.IsStructuredTrivia
                || node.IsPartOfStructuredTrivia()
                || node.GetLeadingTrivia().Span.Contains(span)
                || node.GetTrailingTrivia().Span.Contains(span);
        }
    }
}