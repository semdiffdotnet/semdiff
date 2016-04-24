// Copyright (c) 2015 semdiffdotnet. Distributed under the MIT License.
// See LICENSE file or opensource.org/licenses/MIT.
using Microsoft.CodeAnalysis;

namespace SemDiff.Core
{
    /// <summary>
    /// This class will hold all the important information about what kind of False Negative was
    /// detected and the files, pull requests, and changes involved
    /// </summary>
    public class DetectedFalseNegative
    {
        public Location Location { get; internal set; }
        public PullRequest RemoteChange { get; internal set; }
        public RepoFile RemoteFile { get; internal set; }
        public string DerivedTypeName { get; internal set; }
        public string BaseTypeName { get; internal set; }

        public override string ToString()
        {
            return $@"{nameof(DetectedFalseNegative)}: Location = {Location}, RemoteChange ={ ""
                } {RemoteChange?.Url}, RemoteFile = {RemoteFile.Filename}, TypeName = {DerivedTypeName}";
        }
    }
}