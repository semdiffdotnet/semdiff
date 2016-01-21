using System.Collections.Generic;

namespace SemDiff.Core
{
    public class Diff3Result
    {
        public IEnumerable<Conflict> Conflicts { get; internal set; }
        public List<Diff> Local { get; internal set; }
        public List<Diff> Remote { get; internal set; }
    }
}