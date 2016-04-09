namespace SemDiff.Test
{
    using Core;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    [TestClass]
    public class GitHubAuthTest : TestBase
    {
        private string owner = "semdiffdotnet";
        private string repository = "curly-broccoli";
        private const string authUsername = "haroldhues";
        private const string authToken = "9db4f2de497905dc5a5b2c597869a55a9ae05d9b";

        private IList<Repo.PullRequest> pullRequests;
        private Repo github;

        public GitHubAuthTest()
        {
            var repoLoc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
            github = new Repo(repoLoc, owner, repository, authUsername, authToken);
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