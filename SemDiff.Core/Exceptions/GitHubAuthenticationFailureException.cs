using System;

namespace SemDiff.Core.Exceptions
{
    /// <summary>
    /// Will be thrown when credentials are rejected by GitHub
    /// </summary>
    [Serializable]
    public class GitHubAuthenticationFailureException : Exception
    {
        public GitHubAuthenticationFailureException() : base("Authentication Failure")
        {
        }
    }
}