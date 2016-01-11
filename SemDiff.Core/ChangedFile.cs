using Microsoft.CodeAnalysis;

namespace SemDiff.Core
{
    public class ChangedFile //TODO: Abstract to File system
    {
        /// <summary>
        /// The ancestor that the pull request will be merged with
        /// </summary>
        public SyntaxTree Base { get; set; }

        public SyntaxTree File { get; set; }
    }
}