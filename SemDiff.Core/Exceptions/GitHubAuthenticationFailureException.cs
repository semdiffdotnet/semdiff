// Copyright (c) 2015 semdiffdotnet. Distributed under the MIT License.
// See LICENSE file or opensource.org/licenses/MIT.
using System;

namespace SemDiff.Core.Exceptions
{
    /// <summary>
    /// Will be thrown when credentials are rejected by GitHub
    /// </summary>
    [Serializable]
    public class GitHubAuthenticationFailureException : Exception
    {
        public GitHubAuthenticationFailureException(string githubMessage) : base("Authentication Failure")
        {
            GithubMessage = githubMessage; //Likely "Bad credentials"
        }

        public string GithubMessage { get; }
    }
}