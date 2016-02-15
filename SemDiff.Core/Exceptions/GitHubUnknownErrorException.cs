using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SemDiff.Core.Exceptions
{
    /// <summary>
    /// This exception is thrown if an error is recieved that is unhandled or un documented
    /// </summary>
    [Serializable]
    public class GitHubUnknownErrorException : Exception
    {
        internal GitHubUnknownErrorException(string message) : base(message)
        {
        }

        public GitHubUnknownErrorException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected GitHubUnknownErrorException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}