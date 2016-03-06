using Microsoft.CodeAnalysis;

namespace SemDiff.Core
{
    /// <summary>
    /// This class will hold all the important information about what kind of False Negative was detected and the files, pull requests, and changes involved
    /// </summary>
    public class DetectedFalseNegative
    {
        public Location Location { get; internal set; }
        public RemoteChanges RemoteChange { get; internal set; }
        public RemoteFile RemoteFile { get; internal set; }
        public string TypeName { get; internal set; }
    }
}