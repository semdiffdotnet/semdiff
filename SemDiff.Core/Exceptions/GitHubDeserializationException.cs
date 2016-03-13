using System;

namespace SemDiff.Core.Exceptions
{
    /// <summary>
    /// Thrown when deserialization fails, this could happen because an error was returned and we
    /// did not catch in or it could be any kind of corrupted file
    /// </summary>
    [Serializable]
    public class GitHubDeserializationException : Exception
    {
        public GitHubDeserializationException(Exception innerException)
            : base("Failed to Deserialize GitHub Data", innerException)
        {
        }
    }
}