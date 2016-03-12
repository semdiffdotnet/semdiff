using System;

namespace SemDiff.Core.Exceptions
{
    /// <summary>
    /// Thrown if we find a .gitconfig file, but it doesn't match our reg-ex for a GitHub url
    /// </summary>
    [Serializable]
    internal class GitHubUrlNotFoundException : Exception
    {
        public GitHubUrlNotFoundException(string path)
            : base($"Repository at '{path}' doesn't seem to be a GitHub repository.")
        {
        }
    }
}