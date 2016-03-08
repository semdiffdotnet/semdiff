using Microsoft.CodeAnalysis;

namespace SemDiff.Core
{
    /// <summary>
    /// This class will hold all the important information about what kind of False Positive was detected and the files, pull requests, and changes involved
    /// </summary>
    public class DetectedFalsePositive
    {
        public enum ConflictTypes
        {
            LocalMethodRemoved,
            LocalMethodChanged
        }

        public Location Location { get; internal set; }
        public RemoteChanges RemoteChange { get; internal set; }
        public RemoteFile RemoteFile { get; internal set; }
        public ConflictTypes ConflictType { get; internal set; }

        public override string ToString()
        {
            return $@"{nameof(DetectedFalsePositive)}: Location = {Location}, RemoteChange = {RemoteChange?.Url}, { ""
                }RemoteFile = {RemoteFile.Filename}, ConflictType = {ConflictType}";
        }
    }
}