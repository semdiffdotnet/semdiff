using Microsoft.CodeAnalysis;

namespace SemDiff.Core
{
    public class RemoteFile
    {
        /// <summary>
        /// The ancestor that the pull request will be merged with
        /// </summary>
        public SyntaxTree Base { get; set; }

        /// <summary>
        /// The file from the open pull request
        /// </summary>
        public SyntaxTree File { get; set; }

        public string Filename { get; internal set; }
    }
}