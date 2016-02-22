namespace SemDiff.Core
{
    /// <summary>
    /// This class will hold all the important information about what kind of False Positive was detected and the files, pull requests, and changes involved
    /// </summary>
    public class DetectedFalsePositive
    {
        public string LocalFile { get; internal set; }
        public RemoteChanges RemoteChange { get; internal set; }
        public RemoteFile RemoteFile { get; internal set; }
    }
}