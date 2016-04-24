// Copyright (c) 2015 semdiffdotnet. Distributed under the MIT License.
// See LICENSE file or opensource.org/licenses/MIT.
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using SemDiff.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            var repoLoc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            github = new Repo(repoLoc, owner, repository, authUsername, authToken);
            github.UpdateLimitAsync().Wait();
            var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), nameof(SemDiff));
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
            github.AssertRateLimit();
            var requests = github.GetPullRequestsAsync().Result;
            Assert.AreEqual(5, requests.Count);
            var r = requests.ElementAt(requests.Count - 4);
            if (r.Number == 4)
            {
                Assert.AreEqual(r.State, "open");
                Assert.AreEqual(r.ValidFiles.Count(), 1);
                foreach (var f in r.ValidFiles)
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
            github.AssertRateLimit();
            var requests = github.GetPullRequestsAsync().Result;
            requests = github.GetPullRequestsAsync().Result;
            Assert.AreEqual(null, requests);
        }

        [TestMethod]
        public void Pagination()
        {
            github.AssertRateLimit();
            github.Owner = "dotnet";
            github.RepoName = "roslyn";
            var roslynPRs = github.GetPullRequestsAsync().Result;
            Assert.IsTrue(roslynPRs.Count > 30);
        }

        [TestMethod]
        public void FilesPagination()
        {
            github.AssertRateLimit();
            github.Owner = "semdiffdotnet";
            github.RepoName = "50states";
            var PRs = github.GetPullRequestsAsync().Result;
            Assert.AreEqual(1, PRs.Count);
            var pr = PRs.First();
            Assert.AreEqual(84, pr.ValidFiles.Count());
        }

        [TestMethod]
        public void GetFilesFromGitHub()
        {
            github.AssertRateLimit();
            var requests = github.GetPullRequestsAsync().Result;
            var fourWasFound = false;
            foreach (var r in requests)
            {
                r.GetFilesAsync().Wait();
                if (r.Number == 4)
                {
                    fourWasFound = true;
                    var counter = 0;
                    string[] directoryTokens;
                    var dir = "";
                    var f = r.ValidFiles.Last();
                    directoryTokens = f.Filename.Split('/');
                    dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/SemDiff/semdiffdotnet/curly-broccoli/".ToLocalPath();

                    dir = Path.Combine(dir, r.Number.ToString(), f.Filename.ToLocalPath());
                    var text = File.ReadAllText(dir);
                    foreach (var line in File.ReadAllLines(dir))
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
                    Assert.AreEqual(34, counter);
                }
            }
            Assert.IsTrue(fourWasFound);
        }

        [TestMethod]
        public void UpdateLocalSaved()
        {
            github.AssertRateLimit();
            github.GetPullRequestsAsync().Wait();
            var path = github.CacheDirectory.Replace('/', Path.DirectorySeparatorChar);
            path = Path.Combine(path, github.CachedLocalPullRequestListPath);
            new FileInfo(path).Directory.Create();
            if (File.Exists(path))
                File.Delete(path);
            github.UpdateLocalSavedList();
            Assert.IsTrue(File.Exists(path));
            var json = File.ReadAllText(path);
            var currentSaved = JsonConvert.DeserializeObject<IList<PullRequest>>(json);
            Assert.AreEqual(github.PullRequests.Count, currentSaved.Count);
            var local = currentSaved.First();
            var gPR = github.PullRequests.First();
            Assert.AreEqual(local.Number, gPR.Number);
            Assert.AreEqual(local.State, gPR.State);
            Assert.AreEqual(local.Title, gPR.Title);
            Assert.AreEqual(local.Updated, gPR.Updated);
            Assert.AreEqual(local.LastWrite, gPR.LastWrite);
            Assert.AreEqual(local.Url, gPR.Url);
            Assert.IsNotNull(local.Head);
            Assert.IsNotNull(local.Base);
            Assert.IsNotNull(local.ValidFiles);
        }

        [TestMethod]
        public void RemoveUnusedLocalFiles()
        {
            github.AssertRateLimit();
            var requests = github.GetPullRequestsAsync().Result;
            var zeroDir = Path.Combine(github.CacheDirectory.Replace('/', Path.DirectorySeparatorChar), "0");
            var prZero = requests.OrderBy(p => p.Number).First().Clone();
            prZero.Number = 0;
            prZero.ParentRepo = github;
            foreach (var f in prZero.ValidFiles) //Add files to fool it!
            {
                f.ParentPullRequst = prZero;
                new FileInfo(f.CachePathBase).Directory.Create();
                File.WriteAllText(f.CachePathBase, "<BAD>");
                File.WriteAllText(f.CachePathHead, "<BAD>");
            }
            github.PullRequests.Add(prZero);
            var json = github.CachedLocalPullRequestListPath;
            File.WriteAllText(json, JsonConvert.SerializeObject(github.PullRequests));
            github.GetCurrentSaved();
            github.EtagNoChanges = null;
            requests = github.GetPullRequestsAsync().Result;
            //Task.Delay(1000).Wait(); //http://stackoverflow.com/a/25421332/2899390
            Assert.IsFalse(Directory.Exists(zeroDir));
        }

        [TestMethod]
        public void LastSessionLocalFiles()
        {
            github.AssertRateLimit();
            var requests = github.GetPullRequestsAsync().Result;
            github.UpdateLocalSavedList();
            var newgithub = new Repo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), nameof(SemDiff)), owner, repository);
            newgithub.GetCurrentSaved();
            Assert.IsNotNull(newgithub.PullRequests);
        }

        [TestMethod]
        public void NoUnnecessaryDownloading()
        {
            github.AssertRateLimit();
            var requests = github.GetPullRequestsAsync().Result;
            var path = "";
            foreach (var r in requests)
            {
                r.GetFilesAsync().Wait();
                if (r.Number == 1)
                {
                    foreach (var files in r.ValidFiles)
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
                r.GetFilesAsync().Wait();
            }
            Assert.IsTrue(fileLastUpdated == File.GetLastWriteTimeUtc(path));
        }

        [TestMethod]
        public void DownloadLineEndingsTest()
        {
            var requests = github.GetPullRequestsAsync().Result;
            Assert.IsTrue(github.LineEndings == LineEndingType.crlf);
            foreach (var r in requests)
            {
                r.GetFilesAsync().Wait();
                Assert.IsTrue(r.Files.All(f => f.BaseTree.ToString().Contains("\r\n") && f.HeadTree.ToString().Contains("\r\n")));
            }
        }
    }
}