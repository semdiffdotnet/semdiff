using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace SemDiff.Core
{
    public class Diff3Result
    {
        public SyntaxTree AncestorTree { get; internal set; }
        public IEnumerable<Conflict> Conflicts { get; internal set; }
        public List<Diff> Local { get; internal set; }
        public SyntaxTree LocalTree { get; internal set; }
        public List<Diff> Remote { get; internal set; }
        public SyntaxTree RemoteTree { get; internal set; }
    }
}