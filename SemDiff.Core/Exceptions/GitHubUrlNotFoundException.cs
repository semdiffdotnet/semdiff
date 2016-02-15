using System;
using System.Runtime.Serialization;

namespace SemDiff.Core.Exceptions
{
    /// <summary>
    /// Thrown if we find a gitconfig file, but it doesnt match our regex for a github url
    /// </summary>
    [Serializable]
    internal class GitHubUrlNotFoundException : Exception
    {
        public GitHubUrlNotFoundException(string path) : base($"Repo '{path}' dosn't seem to be a GitHub repository.")
        {
        }

        protected GitHubUrlNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}