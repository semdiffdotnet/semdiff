using System.Collections.Generic;

namespace SemDiff.Core
{
    /// <summary>
    /// Represents Pull Requests
    /// </summary>
    public class RemoteChanges //TODO: Abstract to File system
    {
        public string Title { get; set; }
        public IEnumerable<ChangedFile> Files { get; set; }
    }
}