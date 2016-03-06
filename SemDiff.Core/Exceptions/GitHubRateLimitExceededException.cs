using System;
using System.Runtime.Serialization;

namespace SemDiff.Core.Exceptions
{
    /// <summary>
    /// This can ALSO be thrown if the maximum login attempts is exceeded
    /// Thrown when github reports that the rate limit has been exceeded, this could happen with an unauthenticated client or authenticated clients. The latter is more likely though.
    /// </summary>
    [Serializable]
    public class GitHubRateLimitExceededException : Exception
    {
        public GitHubRateLimitExceededException() : base("Rate Limit Exceeded")
        {
        }

        public GitHubRateLimitExceededException(string message) : base(message)
        {
        }

        public GitHubRateLimitExceededException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected GitHubRateLimitExceededException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}