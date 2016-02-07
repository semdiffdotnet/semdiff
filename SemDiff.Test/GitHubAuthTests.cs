namespace SemDiff.Test
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Core;
    using System.Collections.Generic;
    using Core.Configuration;
    using System.Configuration;

    [TestClass]
    public class GitHubAuthTest
    {
        string owner = "semdiffdotnet";
        string repository = "curly-broccoli";

        IList<GitHub.PullRequest> pullRequests;
        GitHub github;

        public GitHubAuthTest()
        {
            this.github = new GitHub(this.owner, this.repository);
        }

        [TestMethod]
        public void AuthorizedPullRequests()
        {
            this.pullRequests = this.github.GetPullRequests().Result;
        }
    }
}