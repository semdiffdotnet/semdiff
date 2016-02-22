﻿using SemDiff.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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