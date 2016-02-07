using System;
using System.Collections.Generic;

namespace SemDiff.Core
{
    /// <summary>
    /// Represents Pull Requests
    /// </summary>
    public class RemoteChanges //TODO: Abstract to File system
    {
        public string Title { get; set; }
        public IEnumerable<RemoteFile> Files { get; set; }
        public DateTime Date { get; set; }
    }
}