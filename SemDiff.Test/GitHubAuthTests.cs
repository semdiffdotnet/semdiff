namespace SemDiff.Test
{
    using Core;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    [TestClass]
    public class GitHubAuthTest : TestBase
    {
        private string owner = "semdiffdotnet";
        private string repository = "curly-broccoli";

        private IList<Repo.PullRequest> pullRequests;
        private Repo github;

        public GitHubAuthTest()
        {
            github = new Repo(owner, repository, Repo.gitHubConfig.Username, Repo.gitHubConfig.AuthenicationToken);
        }

        [TestMethod]
        public async Task AuthorizedPullRequests()
        {
            try
            {
                pullRequests = await github.GetPullRequestsAsync();
            }
            catch (UnauthorizedAccessException)
            {
                Assert.Inconclusive("Try adding your credetials to the AppConfig :)");
            }
        }
    }
}