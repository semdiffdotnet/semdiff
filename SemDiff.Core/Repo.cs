using System;
using System.Collections.Generic;

namespace SemDiff.Core
{
    /// <summary>
    /// Local representation of a github repo
    /// </summary>
    public class Repo
    {
        public static Repo GetRepoFor(string filePath)
        {
            //Code for much of this is in prototypes
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets Pull Requests and the master branch if it has been modified
        /// </summary>
        /// <returns></returns>
        public IEnumerable<RemoteChanges> GetRemoteChanges()
        {
            TriggerUpdate();
            throw new NotImplementedException();
        }

        public void TriggerUpdate()
        {
            throw new NotImplementedException();
        }

        public void Update()
        {
            throw new NotImplementedException();
        }
    }
}