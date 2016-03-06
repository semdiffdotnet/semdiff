using Microsoft.VisualStudio.TestTools.UnitTesting;
using SemDiff.Core;
using System;
using System.IO;
using System.Linq;

namespace SemDiff.Test
{
    [TestClass]
    public class PullRequestListTests : TestBase
    {
        private const string owner = "semdiffdotnet";
        private const string repository = "curly-broccoli";
        public static GitHub github;

        [TestInitialize]
        public void TestInit()
        {
            github = new GitHub(owner, repository);
            github.UpdateLimitAsync().Wait();
            var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), nameof(SemDiff));
            if (new FileInfo(appDataFolder).Exists)
                Directory.Delete(appDataFolder, recursive: true);
        }

        [TestMethod]
        public void NewGitHub()
        {
            Assert.AreEqual(github.RepoName, repository);
            Assert.AreEqual(github.RepoOwner, owner);
        }

        [TestMethod]
        public void PullRequestFromTestRepo()
        {
            if (github.RequestsRemaining == 0)
            {
                Assert.Inconclusive("Thou hast ran out of requests");
            }
            var requests = github.GetPullRequestsAsync().Result;
            Assert.AreEqual(4, requests.Count);
            var r = requests.First();
            if (r.Number == 4)
            {
                Assert.AreEqual(r.Locked, false);
                Assert.AreEqual(r.State, "open");
                Assert.AreEqual(r.User.Login, "haroldhues");
                Assert.AreEqual(r.Files.Count, 1);
                foreach (var f in r.Files)
                {
                    Assert.AreEqual("Curly-Broccoli/Curly/Logger.cs", f.Filename);
                }
            }
            else
            {
                Assert.Fail();
            }
            Assert.AreEqual("895d2ca038344aacfbcf3902e978de73a7a763fe", r.Head.Sha);
        }
        [TestMethod]
        public void EtagNotModified()
        {
            if (github.RequestsRemaining == 0)
            {
                Assert.Inconclusive("Thou hast ran out of requests");
            }
            var requests = github.GetPullRequestsAsync().Result;
            requests = github.GetPullRequestsAsync().Result;
            Assert.AreEqual(null, requests);
        }

        [TestMethod]
        public void Pagination()
        {
            if (github.RequestsRemaining == 0)
            {
                Assert.Inconclusive("Thou hast ran out of requests");
            }
            github.RepoOwner = "dotnet";
            github.RepoName = "roslyn";
            var roslynPRs = github.GetPullRequestsAsync().Result;
            Assert.IsTrue(roslynPRs.Count > 30);
        }
        [TestMethod]
        public void GetFilesFromGitHub()
        {
            if (github.RequestsRemaining == 0)
            {
                Assert.Inconclusive("Thou hast ran out of requests");
            }
            var requests = github.GetPullRequestsAsync().Result;
            var fourWasFound = false;
            foreach (var r in requests)
            {
                github.DownloadFilesAsync(r).Wait();
                if (r.Number == 4)
                {
                    fourWasFound = true;
                    string line;
                    var counter = 0;
                    string[] directoryTokens;
                    var dir = "";
                    foreach (var f in r.Files)
                    {
                        directoryTokens = f.Filename.Split('/');
                        dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/SemDiff/semdiffdotnet/curly-broccoli/";

                        dir = Path.Combine(dir, r.Number.ToString(), f.Filename);
                    }
                    using (var file = new System.IO.StreamReader(dir))
                    {
                        while ((line = file.ReadLine()) != null)
                        {
                            if (counter == 6)
                            {
                                var expected = "namespace Curly";
                                Assert.AreEqual(line, expected);
                            }
                            if (counter == 9)
                            {
                                var expected = "    /// Utility for logging to an internal list of Log entities";
                                Assert.AreEqual(line, expected);
                            }
                            counter++;
                        }

                        file.Close();
                    }
                    Assert.AreEqual(counter, 34);
                }
            }
            Assert.AreEqual(fourWasFound, true);
        }
    }
}