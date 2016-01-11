using Microsoft.CodeAnalysis;

namespace SemDiff.Core
{
    public class RemoteFile //TODO: Abstract to File system
    {
        /// <summary>
        /// The ancestor that the pull request will be merged with
        /// </summary>
        public SyntaxTree Base { get; set; }
        /// <summary>
        /// The file from the open pull request
        /// </summary>
        public SyntaxTree File { get; set; }
    }
}