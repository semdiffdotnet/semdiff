using Microsoft.CodeAnalysis;

namespace SemDiff.Core
{
    /// <summary>
    /// This class will hold all the important information about what kind of False Positive was
    /// detected and the files, pull requests, and changes involved
    /// </summary>
    public class DetectedFalsePositive
    {
        public enum ConflictTypes
        {
            LocalMethodRemoved,
            LocalMethodChanged
        }

        public Location Location { get; set; }
        public PullRequest RemoteChange { get; set; }
        public RepoFile RemoteFile { get; set; }
        public ConflictTypes ConflictType { get; set; }

        public override string ToString()
        {
            return $@"{nameof(DetectedFalsePositive)}: Location = {Location}, RemoteChange ={ ""
                } {RemoteChange?.Url}, RemoteFile = {RemoteFile.Filename}, ConflictType = {ConflictType}";
        }
    }
}