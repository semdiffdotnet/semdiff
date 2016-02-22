using System;
using System.Runtime.Serialization;

namespace SemDiff.Core.Exceptions
{
    /// <summary>
    /// Thrown when deserialization fails, this could happen because an error was returned and we did not catch in or it
    /// could be any kind of corruped file
    /// </summary>
    [Serializable]
    public class GitHubDeserializationException : Exception
    {
        public GitHubDeserializationException(Exception innerException) : base("Failed to Deserialize GitHub Data", innerException)
        {
        }

        public GitHubDeserializationException(string message) : base(message)
        {
        }

        public GitHubDeserializationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected GitHubDeserializationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}