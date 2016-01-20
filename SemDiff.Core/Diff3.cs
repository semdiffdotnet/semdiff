using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemDiff.Core
{
    public static class Diff3
    {
        public static Conflict Compare(SyntaxTree ancestor, SyntaxTree local, SyntaxTree remote)
        {
            var localChanges = Diff.Compare(ancestor, local);
            var remoteChanges = Diff.Compare(ancestor, remote);
            throw new NotImplementedException();
        }
    }
}