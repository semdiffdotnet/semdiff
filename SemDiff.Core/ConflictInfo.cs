using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        public int SpanStart { get; set; }
        public int SpanEnd { get; set; } //TODO: SpanLength may be better
        public string Text { get; set; }
        public SyntaxTree Tree { get; set; }

        //I found some builtin functions in SyntaxNode of all places that could replace these. Need to experement a little first
        public IEnumerable<ClassDeclarationSyntax> SuroundingClass { get; private set; }

        public IEnumerable<ClassDeclarationSyntax> ContainingClass { get; private set; }
        public IEnumerable<ClassDeclarationSyntax> IntersectingClass { get; private set; }

        public IEnumerable<MethodDeclarationSyntax> SuroundingMethod { get; private set; }
        public IEnumerable<MethodDeclarationSyntax> ContainingMethod { get; private set; }
        public IEnumerable<MethodDeclarationSyntax> IntersectingMethod { get; private set; }

        /// <summary>
        /// Stores the smallest node, trivia, or syntax that completely surounding the conflicting span
        /// </summary>
        public object Surrounding { get; set; }

        //TODO: Determine if this kind of function is nessasary
        public IEnumerable<ConflictInfo> Split(int index)
        {
            throw new NotImplementedException();
        }

        //TODO: Add static Create method when nessasary
    }
}