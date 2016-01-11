using System;
using System.Collections.Generic;

namespace SemDiff.Core
{
    public class Conflict
    {
        public ConflictInfo Ancestor { get; set; }
        public ConflictInfo Local { get; set; }
        public ConflictInfo Remote { get; set; }
        //Looks for changes in whitespace so that change is not added to the conflict list
        public bool IsWhiteSpaceChange
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        //Looks for changes in comments so that change is not added to the conflict list
        public bool IsNonCodeChange
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

        //TODO: Add static Create method when nessasary
    }
}