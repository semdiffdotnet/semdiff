// Copyright (c) 2015 semdiffdotnet. Distributed under the MIT License.
// See LICENSE file or opensource.org/licenses/MIT.
using SemDiff.Core.Configuration;

namespace SemDiff.Core
{
    internal struct GitHubConfiguration
    {
        private readonly string authenticationToken;
        private readonly string username;

        public GitHubConfiguration(AuthenticationSection section)
        {
            if (section == null)
            {
                this.authenticationToken = null;
                this.username = null;
            }
            else
            {
                this.authenticationToken = section.Authentication.Token;
                this.username = section.Authentication.Username;
            }
        }

        public string AuthenicationToken
        {
            get
            {
                return this.authenticationToken;
            }
        }

        public string Username
        {
            get
            {
                return this.username;
            }
        }
    }
}