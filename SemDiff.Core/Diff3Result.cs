using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace SemDiff.Core
{
    /// <summary>
    /// Used to store the results of a Diff3.Compare
    /// </summary>
    public class Diff3Result
    {
        public SyntaxTree AncestorTree { get; set; }
        public IEnumerable<Conflict> Conflicts { get; set; }
        public List<Diff> Local { get; set; }
        public SyntaxTree LocalTree { get; set; }
        public List<Diff> Remote { get; set; }
        public SyntaxTree RemoteTree { get; set; }
    }
}