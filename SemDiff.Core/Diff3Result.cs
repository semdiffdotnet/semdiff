// Copyright (c) 2015 semdiffdotnet. Distributed under the MIT License.
// See LICENSE file or opensource.org/licenses/MIT.
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
        public IEnumerable<Diff> Local { get; set; }
        public SyntaxTree LocalTree { get; set; }
        public IEnumerable<Diff> Remote { get; set; }
        public SyntaxTree RemoteTree { get; set; }
    }
}