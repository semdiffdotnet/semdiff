using System;

namespace SemDiff.Core.Exceptions
{
    /// <summary>
    /// Thrown when GitHub reports that the rate limit has been exceeded, this could happen with an
    /// unauthenticated client or authenticated clients. This can also be thrown if the maximum
    /// login attempts is exceeded. The latter is more likely though.
    /// </summary>
    [Serializable]
    public class GitHubRateLimitExceededException : Exception
    {
        public GitHubRateLimitExceededException() : base("Rate Limit Exceeded")
        {
        }
    }
}