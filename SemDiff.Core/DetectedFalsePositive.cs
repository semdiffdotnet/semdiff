// Copyright (c) 2015 semdiffdotnet. Distributed under the MIT License.
// See LICENSE file or opensource.org/licenses/MIT.
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
        public string MethodName { get; internal set; }

        public override string ToString()
        {
            return $@"{nameof(DetectedFalsePositive)}: Location = {Location}, RemoteChange ={ ""
                } {RemoteChange?.Url}, RemoteFile = {RemoteFile.Filename}, ConflictType = {ConflictType}";
        }
    }
}