using System;

namespace SemDiff.Core.Exceptions
{
    /// <summary>
    /// This exception is thrown if an error is received that is unhandled or undocumented
    /// </summary>
    [Serializable]
    public class GitHubUnknownErrorException : Exception
    {
        internal GitHubUnknownErrorException(string message) : base(message)
        {
        }
    }
}