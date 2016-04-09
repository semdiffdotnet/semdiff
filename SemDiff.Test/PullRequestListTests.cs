using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using SemDiff.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SemDiff.Test
{
    [TestClass]
    public class PullRequestListTests : TestBase
    {
        private const string owner = "semdiffdotnet";
        private const string repository = "curly-broccoli";
        private const string authUsername = "haroldhues";
        private const string authToken = "9db4f2de497905dc5a5b2c597869a55a9ae05d9b";
        public static Repo github;

        [TestInitialize]
        public void TestInit()
        {
            
            var repoLoc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
            github = new Repo(repoLoc,owner, repository, authUsername, authToken);
            github.UpdateLimitAsync().Wait();
            var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), nameof(SemDiff));
            if (new FileInfo(appDataFolder).Exists)
                Directory.Delete(appDataFolder, recursive: true);
        }

        [TestMethod]
        public void NewGitHub()
        {
            Assert.AreEqual(github.RepoName, repository);
            Assert.AreEqual(github.Owner, owner);
        }

        [TestMethod]
        public void PullRequestFromTestRepo()
        {
            if (github.RequestsRemaining == 0)
            {
                Assert.Inconclusive("Thou hast ran out of requests");
            }
            var requests = github.GetPullRequestsAsync().Result;
            Assert.AreEqual(5, requests.Count);
            var r = requests.ElementAt(requests.Count - 4);
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
            github.Owner = "dotnet";
            github.RepoName = "roslyn";
            var roslynPRs = github.GetPullRequestsAsync().Result;
            Assert.IsTrue(roslynPRs.Count > 30);
        }

        [TestMethod]
        public void FilesPagination()
        {
            if (github.RequestsRemaining == 0)
            {
                Assert.Inconclusive("Thou hast ran out of requests");
            }
            var PRs = github.GetPullRequestsAsync().Result;
            Assert.IsTrue(PRs.Count >= 5);
            foreach (var pr in PRs)
            {
                if (pr.Number == 5)
                    Assert.AreEqual(40, pr.Files.Count);
            }
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
                        dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/SemDiff/semdiffdotnet/curly-broccoli/";

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

        [TestMethod]
        public void UpdateLocalSaved()
        {
            github.GetPullRequestsAsync().Wait();
            var path = github.RepoFolder.Replace('/', Path.DirectorySeparatorChar);
            path = Path.Combine(path, github.JsonFileName);
            new FileInfo(path).Directory.Create();
            if (File.Exists(path))
                File.Delete(path);
            github.UpdateLocalSavedList();
            Assert.IsTrue(File.Exists(path));
            var json = File.ReadAllText(path);
            var currentSaved = JsonConvert.DeserializeObject<IList<Repo.PullRequest>>(json);
            Assert.AreEqual(github.CurrentSaved.Count, currentSaved.Count);
            var local = currentSaved.First();
            var gPR = github.CurrentSaved.First();
            Assert.AreEqual(local.Number, gPR.Number);
            Assert.AreEqual(local.State, gPR.State);
            Assert.AreEqual(local.Title, gPR.Title);
            Assert.AreEqual(local.Locked, gPR.Locked);
            Assert.AreEqual(local.Updated, gPR.Updated);
            Assert.AreEqual(local.LastWrite, gPR.LastWrite);
            Assert.AreEqual(local.Url, gPR.Url);
            Assert.IsNotNull(local.User);
            Assert.IsNotNull(local.Head);
            Assert.IsNotNull(local.Base);
            Assert.IsNotNull(local.Files);
        }

        [TestMethod]
        public void RemoveUnusedLocalFiles()
        {
            var requests = github.GetPullRequestsAsync().Result;
            var zeroDir = Path.Combine(github.RepoFolder.Replace('/', Path.DirectorySeparatorChar), "0");
            Directory.CreateDirectory(zeroDir);
            var prZero = requests.First().Clone();
            prZero.Number = 0;
            github.CurrentSaved.Add(prZero);
            var json = Path.Combine(github.RepoFolder.Replace('/', Path.DirectorySeparatorChar), github.JsonFileName);
            File.WriteAllText(json, JsonConvert.SerializeObject(github.CurrentSaved));
            github.GetCurrentSaved();
            github.EtagNoChanges = null;
            requests = github.GetPullRequestsAsync().Result;
            //Task.Delay(1000).Wait(); //http://stackoverflow.com/a/25421332/2899390
            Assert.IsFalse(Directory.Exists(zeroDir));
        }

        [TestMethod]
        public void LastSessionLocalFiles()
        {
            var requests = github.GetPullRequestsAsync().Result;
            github.UpdateLocalSavedList();
            var newgithub = new Repo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), nameof(SemDiff)), owner, repository);
            Assert.IsNotNull(newgithub.CurrentSaved);
        }

        [TestMethod]
        public void NoUnnecessaryDownloading()
        {
            var requests = github.GetPullRequestsAsync().Result;
            var path = "";
            foreach (var r in requests)
            {
                github.DownloadFilesAsync(r).Wait();
                if (r.Number == 1)
                {
                    foreach (var files in r.Files)
                    {
                        path = files.Filename.Replace('/', Path.DirectorySeparatorChar);
                    }
                }
            }
            var fileLastUpdated = File.GetLastWriteTimeUtc(path);
            github.EtagNoChanges = null;
            requests = github.GetPullRequestsAsync().Result;
            foreach (var r in requests)
            {
                github.DownloadFilesAsync(r).Wait();
            }
            Assert.IsTrue(fileLastUpdated == File.GetLastWriteTimeUtc(path));
        }
    }
}
