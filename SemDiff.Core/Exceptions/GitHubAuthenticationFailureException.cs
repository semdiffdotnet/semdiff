using System;
using System.Runtime.Serialization;

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

        public GitHubAuthenticationFailureException(string message) : base(message)
        {
        }

        public GitHubAuthenticationFailureException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected GitHubAuthenticationFailureException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}