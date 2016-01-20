using System;
using System.Collections.Generic;

namespace SemDiff.Core
{
    public class Conflict
    {
        public ConflictInfo Ancestor { get; set; }
        public ConflictInfo Local { get; set; }
        public ConflictInfo Remote { get; set; }

        private Conflict()
        {
        }

        public bool IsWhiteSpaceChange
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsSemanticChange
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        //TODO: Determine if this kind of function is nessasary
        public IEnumerable<Conflict> Split(int ancestorIndex, int localIndex, int remoteIndex)
        {
            throw new NotImplementedException();
        }

        internal static Conflict Create(List<DiffWithOrigin> potentialConflict)
        {
            throw new NotImplementedException();
        }

        //TODO: Add static Create method when nessasary
    }
}